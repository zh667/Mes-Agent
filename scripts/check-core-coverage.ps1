param(
    [Parameter(Mandatory = $true)]
    [string]$CoverageDirectory,
    [double]$MinimumPercent = 70
)

$ErrorActionPreference = "Stop"
$reports = @(Get-ChildItem -LiteralPath $CoverageDirectory -Recurse -Filter "coverage.cobertura.xml")
if ($reports.Count -ne 1) {
    throw "Expected exactly one Cobertura report under '$CoverageDirectory', found $($reports.Count)."
}

[xml]$coverage = Get-Content -LiteralPath $reports[0].FullName
$lineRate = [double]::Parse(
    $coverage.coverage.'line-rate',
    [System.Globalization.CultureInfo]::InvariantCulture)
$percent = [Math]::Round($lineRate * 100, 2)
Write-Host "Core line coverage: $percent% (required: $MinimumPercent%)."

if ($percent -lt $MinimumPercent) {
    throw "Core line coverage $percent% is below the required $MinimumPercent%."
}
