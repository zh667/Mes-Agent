param(
    [switch]$StartDependencies,
    [switch]$Production,
    [string]$EnvironmentFile = ".env",
    [int]$HealthTimeoutSeconds = 120
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

function Get-EnvironmentValues([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Production environment file '$Path' was not found."
    }

    $values = @{}
    foreach ($line in Get-Content -LiteralPath $Path) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith("#")) { continue }
        $separator = $trimmed.IndexOf("=")
        if ($separator -le 0) { continue }
        $name = $trimmed.Substring(0, $separator).Trim()
        $value = $trimmed.Substring($separator + 1).Trim().Trim('"').Trim("'")
        $values[$name] = $value
    }
    return $values
}

function Assert-ProductionSecrets([hashtable]$Values) {
    $requiredSecrets = @(
        "POSTGRES_PASSWORD",
        "POSTGRES_REPLICATION_PASSWORD",
        "REDIS_PASSWORD",
        "MQTT_PASSWORD",
        "JWT_SECRET",
        "AUDIT_IP_HASH_KEY",
        "NEXTAUTH_SECRET"
    )

    foreach ($name in $requiredSecrets) {
        $value = $Values[$name]
        if ([string]::IsNullOrWhiteSpace($value)) {
            throw "Production secret '$name' is missing."
        }
        if ($value.Length -lt 16 -or $value -match '(?i)change-me|local-only|replicator-local|^postgres$|^password$|^secret$') {
            throw "Production secret '$name' still uses an unsafe placeholder or is too short."
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($Values["BOOTSTRAP_ADMIN_EMAIL"])) {
        $bootstrapPassword = $Values["BOOTSTRAP_ADMIN_PASSWORD"]
        if ([string]::IsNullOrWhiteSpace($bootstrapPassword) -or
            $bootstrapPassword.Length -lt 16 -or
            $bootstrapPassword -match '(?i)change-me|local-only|^password$|^secret$') {
            throw "BOOTSTRAP_ADMIN_PASSWORD must be a strong non-placeholder secret when BOOTSTRAP_ADMIN_EMAIL is configured."
        }
    }
}

Push-Location $root
try {
    $composeEnvironmentArgs = @()
    if ($Production) {
        $resolvedEnvironmentFile = (Resolve-Path -LiteralPath $EnvironmentFile).Path
        Assert-ProductionSecrets (Get-EnvironmentValues $resolvedEnvironmentFile)
        $composeEnvironmentArgs = @("--env-file", $resolvedEnvironmentFile)
    }

    & docker compose @composeEnvironmentArgs config --quiet
    if ($LASTEXITCODE -ne 0) { throw "Base Compose configuration is invalid." }
    & docker compose @composeEnvironmentArgs -f docker-compose.yml -f docker-compose.replica.yml config --quiet
    if ($LASTEXITCODE -ne 0) { throw "Replica Compose configuration is invalid." }

    $baseConfig = (& docker compose @composeEnvironmentArgs config) -join [Environment]::NewLine
    $replicaConfig = (& docker compose @composeEnvironmentArgs -f docker-compose.yml -f docker-compose.replica.yml config) -join [Environment]::NewLine
    foreach ($required in @("postgres_data", "redis_data", "data_protection_keys")) {
        if ($baseConfig -notmatch [regex]::Escape($required)) {
            throw "Compose configuration is missing required volume '$required'."
        }
    }
    if ($replicaConfig -notmatch [regex]::Escape("postgres_replica_data")) {
        throw "Replica Compose configuration is missing required volume 'postgres_replica_data'."
    }

    foreach ($placeholder in @("POSTGRES_PASSWORD", "REDIS_PASSWORD", "MQTT_PASSWORD")) {
        if (-not (Select-String -LiteralPath ".env.example" -Pattern "^$placeholder=" -Quiet)) {
            throw ".env.example is missing '$placeholder'."
        }
    }

    if ($StartDependencies) {
        & docker compose @composeEnvironmentArgs up -d postgres redis mqtt api
        if ($LASTEXITCODE -ne 0) { throw "Compose dependencies failed to start." }
        $deadline = (Get-Date).AddSeconds($HealthTimeoutSeconds)
        $apiPort = if ([string]::IsNullOrWhiteSpace($env:API_PORT)) { "5000" } else { $env:API_PORT }
        do {
            try {
                $response = Invoke-RestMethod -Uri "http://localhost:$apiPort/health" -TimeoutSec 3
                if ($response.status -eq "Healthy") {
                    Write-Host "Phase 3 dependencies are healthy."
                    exit 0
                }
            } catch {
                Start-Sleep -Seconds 2
            }
        } while ((Get-Date) -lt $deadline)

        & docker compose @composeEnvironmentArgs ps
        throw "API health endpoint did not become healthy within $HealthTimeoutSeconds seconds."
    }

    $mode = if ($Production) { "production" } else { "development" }
    Write-Host "Phase 3 $mode Compose configuration is valid. Use -StartDependencies for runtime health verification."
}
finally {
    Pop-Location
}
