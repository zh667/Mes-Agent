# MES Copilot Phase 2 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal**: Upgrade MES Copilot from demo-level MVP to near-production-ready tool with authentication, Agent prompt engineering, streaming responses, 3 new Agent tools, report export, conversation history, and document versioning.

**Architecture**: Extends Phase 1 single-repo fusion architecture. Adds JWT authentication layer, SSE streaming endpoint, conversation persistence, and report generation services.

**Tech Stack (New in Phase 2)**:
- Authentication: ASP.NET Core Identity + JWT Bearer, next-auth v4
- Streaming: Server-Sent Events (SSE), OpenAI-compatible format
- Reports: QuestPDF (PDF), ClosedXML (Excel)
- Conversation: PostgreSQL JSONB + GIN full-text search
- State: React Context (auth) + TanStack Query (server state)

## Global Constraints

- All Phase 1 constraints remain in effect
- All new endpoints must require `[Authorize]` unless explicitly public
- SSE events must follow OpenAI streaming format: `data: {"type":"...","content":"..."}\n\n`
- All new entities must have corresponding EF Core configurations
- Frontend must use next-auth session for auth state (no localStorage tokens)
- Refresh token rotation: each refresh invalidates the previous token
- Agent tools must support `debugMode` parameter
- New migrations must be additive (never drop existing tables)

---

## Sub-Phase 2A: JWT Authentication + RBAC

### Task 2A.1: Add Identity Domain Model

**Files:**
- Create: `src/MesCopilot.Domain/Entities/Identity/AppUser.cs`
- Create: `src/MesCopilot.Domain/Enums/UserRole.cs`
- Create: `tests/MesCopilot.UnitTests/Domain/Enums/UserRoleTests.cs`

**Interfaces:**
- Consumes: None
- Produces: AppUser entity, UserRole enum

- [ ] **Step 1: Write test for UserRole enum**

Create file `tests/MesCopilot.UnitTests/Domain/Enums/UserRoleTests.cs`:

```csharp
using MesCopilot.Domain.Enums;
using Xunit;

namespace MesCopilot.UnitTests.Domain.Enums;

public class UserRoleTests
{
    [Fact]
    public void UserRole_ShouldHaveAllRequiredValues()
    {
        var values = Enum.GetValues<UserRole>();

        Assert.Contains(UserRole.Admin, values);
        Assert.Contains(UserRole.TeamLead, values);
        Assert.Contains(UserRole.QAInspector, values);
        Assert.Contains(UserRole.Operator, values);
    }

    [Theory]
    [InlineData(UserRole.Admin, 0)]
    [InlineData(UserRole.TeamLead, 1)]
    [InlineData(UserRole.QAInspector, 2)]
    [InlineData(UserRole.Operator, 3)]
    public void UserRole_ShouldHaveCorrectValues(UserRole role, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)role);
    }

    [Theory]
    [InlineData(UserRole.Admin, "Admin")]
    [InlineData(UserRole.TeamLead, "TeamLead")]
    [InlineData(UserRole.QAInspector, "QAInspector")]
    [InlineData(UserRole.Operator, "Operator")]
    public void UserRole_ShouldHaveCorrectStringRepresentation(UserRole role, string expected)
    {
        Assert.Equal(expected, role.ToString());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~UserRoleTests"
```

Expected: FAIL - UserRole type not found

- [ ] **Step 3: Create UserRole enum**

Create file `src/MesCopilot.Domain/Enums/UserRole.cs`:

```csharp
namespace MesCopilot.Domain.Enums;

public enum UserRole
{
    Admin = 0,
    TeamLead = 1,
    QAInspector = 2,
    Operator = 3
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~UserRoleTests"
```

Expected: PASS - All tests green

- [ ] **Step 5: Create AppUser entity**

Create file `src/MesCopilot.Domain/Entities/Identity/AppUser.cs`:

```csharp
using Microsoft.AspNetCore.Identity;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Identity;

public class AppUser : IdentityUser
{
    public string DisplayName { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Operator;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }
}
```

- [ ] **Step 6: Add Microsoft.AspNetCore.Identity.EntityFrameworkCore to Domain project**

```bash
cd src/MesCopilot.Domain
dotnet add package Microsoft.Extensions.Identity.Stores --version 8.0.0
cd ../..
```

Expected: Package added (we only need the base IdentityUser class in Domain)

- [ ] **Step 7: Build to verify**

```bash
dotnet build src/MesCopilot.Domain/MesCopilot.Domain.csproj
```

Expected: Build succeeded

- [ ] **Step 8: Commit**

```bash
git add src/MesCopilot.Domain/Entities/Identity/ src/MesCopilot.Domain/Enums/UserRole.cs tests/MesCopilot.UnitTests/Domain/Enums/UserRoleTests.cs
git commit -m "feat(domain): add AppUser entity and UserRole enum for Phase 2 authentication

- AppUser extends IdentityUser with DisplayName, Role, RefreshToken
- UserRole: Admin, TeamLead, QAInspector, Operator
- Unit tests for UserRole enum values"
```

Expected: Changes committed

---

### Task 2A.2: Add Identity Infrastructure (TokenService + DbContext Update)

**Files:**
- Modify: `src/MesCopilot.Infrastructure/MesCopilot.Infrastructure.csproj` (add Identity packages)
- Modify: `src/MesCopilot.Infrastructure/Data/MesDbContext.cs` (inherit IdentityDbContext)
- Create: `src/MesCopilot.Infrastructure/Identity/TokenService.cs`
- Create: `src/MesCopilot.Infrastructure/Identity/ITokenService.cs`
- Create: `src/MesCopilot.Infrastructure/Data/Configurations/IdentityConfiguration.cs`
- Create: `tests/MesCopilot.UnitTests/Infrastructure/Identity/TokenServiceTests.cs`

**Interfaces:**
- Consumes: AppUser, UserRole
- Produces: ITokenService (generates JWT access + refresh tokens), updated DbContext

- [ ] **Step 1: Add Identity NuGet packages to Infrastructure**

```bash
cd src/MesCopilot.Infrastructure
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 8.0.0
cd ../..
```

Expected: Package added

- [ ] **Step 2: Write tests for TokenService**

Create file `tests/MesCopilot.UnitTests/Infrastructure/Identity/TokenServiceTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MesCopilot.UnitTests.Infrastructure.Identity;

public class TokenServiceTests
{
    private readonly TokenService _tokenService;

    public TokenServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "ThisIsATestSecretKeyThatIsAtLeast32BytesLong!!",
                ["Jwt:Issuer"] = "MesCopilot",
                ["Jwt:Audience"] = "MesCopilotClient",
                ["Jwt:AccessTokenExpirationMinutes"] = "15",
                ["Jwt:RefreshTokenExpirationDays"] = "7"
            })
            .Build();

        _tokenService = new TokenService(config);
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwt()
    {
        var user = new AppUser
        {
            Id = "user-123",
            Email = "test@example.com",
            UserName = "test@example.com",
            DisplayName = "Test User",
            Role = UserRole.Admin
        };

        var token = _tokenService.GenerateAccessToken(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("user-123", jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("test@example.com", jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Test User", jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("Admin", jwt.Claims.First(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateAccessToken_ShouldHaveCorrectExpiration()
    {
        var user = new AppUser
        {
            Id = "user-123",
            Email = "test@example.com",
            UserName = "test@example.com",
            DisplayName = "Test User",
            Role = UserRole.Operator
        };

        var token = _tokenService.GenerateAccessToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
        Assert.True(jwt.ValidTo <= expectedExpiry.AddSeconds(5));
        Assert.True(jwt.ValidTo >= expectedExpiry.AddSeconds(-5));
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        var token = _tokenService.GenerateRefreshToken();

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.True(token.Length >= 32);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldBeUnique()
    {
        var token1 = _tokenService.GenerateRefreshToken();
        var token2 = _tokenService.GenerateRefreshToken();

        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ShouldReturnClaims()
    {
        var user = new AppUser
        {
            Id = "user-456",
            Email = "expired@example.com",
            UserName = "expired@example.com",
            DisplayName = "Expired User",
            Role = UserRole.TeamLead
        };

        var token = _tokenService.GenerateAccessToken(user);
        var principal = _tokenService.GetPrincipalFromExpiredToken(token);

        Assert.NotNull(principal);
        Assert.Equal("user-456", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~TokenServiceTests"
```

Expected: FAIL - TokenService type not found

- [ ] **Step 4: Create ITokenService interface**

Create file `src/MesCopilot.Infrastructure/Identity/ITokenService.cs`:

```csharp
using System.Security.Claims;
using MesCopilot.Domain.Entities.Identity;

namespace MesCopilot.Infrastructure.Identity;

public interface ITokenService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
```

- [ ] **Step 5: Create TokenService implementation**

Create file `src/MesCopilot.Infrastructure/Identity/TokenService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MesCopilot.Domain.Entities.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MesCopilot.Infrastructure.Identity;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("sub", user.Id)
        };

        var secret = _configuration["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "15");

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var secret = _configuration["Jwt:Secret"]!;
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidateIssuer = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateLifetime = false,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            throw new SecurityTokenException("Invalid token");
        }

        return principal;
    }
}
```

- [ ] **Step 6: Add required NuGet packages to Infrastructure for JWT**

```bash
cd src/MesCopilot.Infrastructure
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.0
cd ../..
```

Expected: Packages added

- [ ] **Step 7: Run tests to verify they pass**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~TokenServiceTests"
```

Expected: PASS - All tests green

- [ ] **Step 8: Update MesDbContext to inherit IdentityDbContext**

Modify `src/MesCopilot.Infrastructure/Data/MesDbContext.cs`:

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Knowledge;

namespace MesCopilot.Infrastructure.Data;

public class MesDbContext : IdentityDbContext<AppUser>
{
    public MesDbContext(DbContextOptions<MesDbContext> options) : base(options)
    {
    }

    // Products
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Bom> Boms => Set<Bom>();
    public DbSet<BomItem> BomItems => Set<BomItem>();

    // Production
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<ProcessRoute> ProcessRoutes => Set<ProcessRoute>();
    public DbSet<ProcessStep> ProcessSteps => Set<ProcessStep>();
    public DbSet<Workstation> Workstations => Set<Workstation>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();
    public DbSet<WorkOrderOperation> WorkOrderOperations => Set<WorkOrderOperation>();
    public DbSet<ProductionReport> ProductionReports => Set<ProductionReport>();

    // Quality
    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();
    public DbSet<DefectRecord> DefectRecords => Set<DefectRecord>();
    public DbSet<DefectType> DefectTypes => Set<DefectType>();

    // Equipment
    public DbSet<Domain.Entities.Equipment.Equipment> Equipment => Set<Domain.Entities.Equipment.Equipment>();
    public DbSet<EquipmentStatus> EquipmentStatuses => Set<EquipmentStatus>();
    public DbSet<EquipmentAlarm> EquipmentAlarms => Set<EquipmentAlarm>();
    public DbSet<DowntimeRecord> DowntimeRecords => Set<DowntimeRecord>();

    // Knowledge
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MesDbContext).Assembly);
    }
}
```

- [ ] **Step 9: Create Identity table configuration**

Create file `src/MesCopilot.Infrastructure/Data/Configurations/IdentityConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MesCopilot.Domain.Entities.Identity;

namespace MesCopilot.Infrastructure.Data.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
```

- [ ] **Step 10: Create EF Core migration for Identity**

```bash
cd src/MesCopilot.Api
dotnet ef migrations add AddIdentity --project ../MesCopilot.Infrastructure/MesCopilot.Infrastructure.csproj
cd ../..
```

Expected: Migration file created in Infrastructure/Migrations/

- [ ] **Step 11: Build to verify**

```bash
dotnet build
```

Expected: All projects build successfully

- [ ] **Step 12: Commit**

```bash
git add src/MesCopilot.Infrastructure/ tests/MesCopilot.UnitTests/Infrastructure/Identity/
git commit -m "feat(infrastructure): add JWT token service and Identity DbContext

- TokenService: generate access token (15min) + refresh token (7d)
- GetPrincipalFromExpiredToken for refresh flow
- MesDbContext now inherits IdentityDbContext<AppUser>
- AppUserConfiguration with Role as string conversion
- EF Migration: AddIdentity (AspNetUsers, AspNetRoles, etc.)
- Unit tests for TokenService (generation, expiry, uniqueness)"
```

Expected: Changes committed

---

### Task 2A.3: Create AuthController

**Files:**
- Modify: `src/MesCopilot.Api/MesCopilot.Api.csproj` (add JWT Bearer package)
- Create: `src/MesCopilot.Api/Controllers/AuthController.cs`
- Create: `src/MesCopilot.Api/Dtos/Auth/RegisterRequest.cs`
- Create: `src/MesCopilot.Api/Dtos/Auth/LoginRequest.cs`
- Create: `src/MesCopilot.Api/Dtos/Auth/AuthResponse.cs`
- Create: `src/MesCopilot.Api/Dtos/Auth/RefreshRequest.cs`
- Create: `tests/MesCopilot.IntegrationTests/Controllers/AuthControllerTests.cs`

**Interfaces:**
- Consumes: ITokenService, UserManager<AppUser>, SignInManager<AppUser>
- Produces: Auth endpoints (register, login, refresh, me)

- [ ] **Step 1: Add JWT Bearer package to Api project**

```bash
cd src/MesCopilot.Api
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
cd ../..
```

Expected: Package added

- [ ] **Step 2: Create Auth DTO classes**

Create file `src/MesCopilot.Api/Dtos/Auth/RegisterRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace MesCopilot.Api.Dtos.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    public string Role { get; set; } = "Operator";
}
```

Create file `src/MesCopilot.Api/Dtos/Auth/LoginRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace MesCopilot.Api.Dtos.Auth;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
```

Create file `src/MesCopilot.Api/Dtos/Auth/AuthResponse.cs`:

```csharp
namespace MesCopilot.Api.Dtos.Auth;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public UserInfo User { get; set; } = new();
}

public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
```

Create file `src/MesCopilot.Api/Dtos/Auth/RefreshRequest.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace MesCopilot.Api.Dtos.Auth;

public class RefreshRequest
{
    [Required]
    public string AccessToken { get; set; } = string.Empty;

    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
```

- [ ] **Step 3: Create AuthController**

Create file `src/MesCopilot.Api/Controllers/AuthController.cs`:

```csharp
using System.Security.Claims;
using MesCopilot.Api.Dtos.Auth;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;

    public AuthController(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return Conflict(new { message = "Email already registered" });
        }

        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            role = UserRole.Operator;
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return CreatedAtAction(nameof(Me), new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString()
            }
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        user.LastLoginAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString()
            }
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token" });
        }

        var newAccessToken = _tokenService.GenerateAccessToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString()
            }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfo>> Me()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new UserInfo
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Role = user.Role.ToString()
        });
    }
}
```

- [ ] **Step 4: Write integration tests for AuthController**

Create file `tests/MesCopilot.IntegrationTests/Controllers/AuthControllerTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Auth;
using Xunit;

namespace MesCopilot.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var request = new RegisterRequest
        {
            Email = $"test-{Guid.NewGuid():N}@example.com",
            Password = "Test123!",
            DisplayName = "Test User",
            Role = "Operator"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.NotEmpty(auth.AccessToken);
        Assert.NotEmpty(auth.RefreshToken);
        Assert.Equal(request.Email, auth.User.Email);
        Assert.Equal("Operator", auth.User.Role);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"dup-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequest
        {
            Email = email,
            Password = "Test123!",
            DisplayName = "User 1"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        var email = $"login-{Guid.NewGuid():N}@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Test123!",
            DisplayName = "Login User"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Assert.NotEmpty(auth.AccessToken);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "wrong"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewTokens()
    {
        var email = $"refresh-{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Test123!",
            DisplayName = "Refresh User"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
        {
            AccessToken = auth!.AccessToken,
            RefreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var newAuth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(newAuth);
        Assert.NotEqual(auth.AccessToken, newAuth.AccessToken);
        Assert.NotEqual(auth.RefreshToken, newAuth.RefreshToken);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsUserInfo()
    {
        var email = $"me-{Guid.NewGuid():N}@example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Test123!",
            DisplayName = "Me User",
            Role = "Admin"
        });
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserInfo>();
        Assert.NotNull(user);
        Assert.Equal(email, user.Email);
        Assert.Equal("Admin", user.Role);
    }
}
```

- [ ] **Step 5: Run integration tests**

```bash
dotnet test tests/MesCopilot.IntegrationTests/MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~AuthControllerTests"
```

Expected: PASS - All 7 tests green

- [ ] **Step 6: Commit**

```bash
git add src/MesCopilot.Api/Controllers/AuthController.cs src/MesCopilot.Api/Dtos/Auth/ tests/MesCopilot.IntegrationTests/Controllers/AuthControllerTests.cs
git commit -m "feat(api): add AuthController with register, login, refresh, me endpoints

- POST /api/auth/register: create user with role, return tokens
- POST /api/auth/login: validate credentials, return tokens
- POST /api/auth/refresh: rotate refresh token, return new pair
- GET /api/auth/me: return current user info (requires auth)
- Integration tests: 7 scenarios covering happy + error paths
- DTO validation with DataAnnotations"
```

Expected: Changes committed

---

### Task 2A.4: Configure Authentication in Program.cs + Add Authorization Policies

**Files:**
- Modify: `src/MesCopilot.Api/Program.cs`
- Modify: `src/MesCopilot.Infrastructure/DependencyInjection.cs`
- Create: `src/MesCopilot.Api/appsettings.json` (add Jwt section)

**Interfaces:**
- Consumes: ITokenService, AppUser, MesDbContext
- Produces: JWT authentication middleware, 4 authorization policies

- [ ] **Step 1: Add Jwt configuration to appsettings.json**

Add the following section to `src/MesCopilot.Api/appsettings.json`:

```json
{
  "Jwt": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32BytesLongForHmacSha256!!",
    "Issuer": "MesCopilot",
    "Audience": "MesCopilotClient",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

- [ ] **Step 2: Register Identity and TokenService in DependencyInjection**

Modify `src/MesCopilot.Infrastructure/DependencyInjection.cs` to add:

```csharp
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

// Inside AddMesCopilotInfrastructure method, add:
services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MesDbContext>()
.AddDefaultTokenProviders();

services.AddScoped<ITokenService, TokenService>();
```

- [ ] **Step 3: Configure JWT authentication in Program.cs**

Add to `src/MesCopilot.Api/Program.cs` after `builder.Services` setup:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// After AddMesCopilotInfrastructure():
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("RequireTeamLead", policy =>
        policy.RequireRole("Admin", "TeamLead"));
    options.AddPolicy("RequireQAInspector", policy =>
        policy.RequireRole("Admin", "TeamLead", "QAInspector"));
    options.AddPolicy("RequireOperator", policy =>
        policy.RequireRole("Admin", "TeamLead", "QAInspector", "Operator"));
});
```

Add middleware in the pipeline (after `app.UseRouting()`, before `app.MapControllers()`):

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

- [ ] **Step 4: Add [Authorize] to existing controllers**

Modify `src/MesCopilot.Api/Controllers/WorkOrdersController.cs`:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WorkOrdersController : ControllerBase
```

Modify `src/MesCopilot.Api/Controllers/EquipmentController.cs`:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EquipmentController : ControllerBase
```

Modify `src/MesCopilot.Api/Controllers/QualityController.cs`:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QualityController : ControllerBase
```

Modify `src/MesCopilot.Api/Controllers/DocumentsController.cs`:
```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DocumentsController : ControllerBase
```

- [ ] **Step 5: Add CORS configuration for frontend**

Add to Program.cs:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// In pipeline:
app.UseCors("AllowFrontend");
```

- [ ] **Step 6: Build and verify**

```bash
dotnet build
```

Expected: All projects build successfully

- [ ] **Step 7: Commit**

```bash
git add src/MesCopilot.Api/Program.cs src/MesCopilot.Infrastructure/DependencyInjection.cs src/MesCopilot.Api/appsettings.json src/MesCopilot.Api/Controllers/
git commit -m "feat(api): configure JWT authentication and RBAC authorization

- JWT Bearer authentication with 15min access token
- 4 authorization policies: RequireAdmin, RequireTeamLead, RequireQAInspector, RequireOperator
- All existing controllers now require [Authorize]
- CORS configured for frontend (localhost:3000)
- Identity registered with password policy
- TokenService registered as scoped service"
```

Expected: Changes committed

---

### Task 2A.5: Add TestAuthHandler for Integration Tests

**Files:**
- Create: `tests/MesCopilot.IntegrationTests/Auth/TestAuthHandler.cs`
- Modify: `tests/MesCopilot.IntegrationTests/IntegrationTestFactory.cs`

**Interfaces:**
- Consumes: None
- Produces: Test authentication bypass for existing integration tests

- [ ] **Step 1: Create TestAuthHandler**

Create file `tests/MesCopilot.IntegrationTests/Auth/TestAuthHandler.cs`:

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MesCopilot.IntegrationTests.Auth;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "TestScheme";
    public const string DefaultUserId = "test-user-id";
    public const string DefaultEmail = "test@example.com";
    public const string DefaultRole = "Admin";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, DefaultUserId),
            new Claim(ClaimTypes.Email, DefaultEmail),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, DefaultRole)
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

- [ ] **Step 2: Update IntegrationTestFactory to use TestAuthHandler**

Modify `tests/MesCopilot.IntegrationTests/IntegrationTestFactory.cs` to add:

```csharp
using MesCopilot.IntegrationTests.Auth;
using Microsoft.AspNetCore.Authentication;

// In ConfigureWebHost -> ConfigureTestServices:
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
    options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
})
.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
    TestAuthHandler.AuthenticationScheme, _ => { });
```

- [ ] **Step 3: Run all existing integration tests to verify they still pass**

```bash
dotnet test tests/MesCopilot.IntegrationTests/MesCopilot.IntegrationTests.csproj
```

Expected: PASS - All existing tests (WorkOrders, Equipment, Quality, Documents, Health, EquipmentHub) pass with TestAuthHandler

- [ ] **Step 4: Commit**

```bash
git add tests/MesCopilot.IntegrationTests/
git commit -m "test: add TestAuthHandler to bypass auth in existing integration tests

- TestAuthHandler auto-authenticates as Admin user
- Existing 6 integration test files continue to pass
- AuthControllerTests bypass TestAuthHandler to test real auth flow"
```

Expected: Changes committed

---

### Task 2A.6: Frontend Authentication with next-auth

**Files:**
- Modify: `web/package.json` (add next-auth)
- Create: `web/app/api/auth/[...nextauth]/route.ts`
- Create: `web/lib/auth.ts`
- Create: `web/app/login/page.tsx`
- Create: `web/middleware.ts`
- Create: `web/components/providers.tsx`
- Modify: `web/app/layout.tsx`

**Interfaces:**
- Consumes: Backend /api/auth/login, /api/auth/me, /api/auth/refresh
- Produces: Authenticated session, login page, route protection

- [ ] **Step 1: Install next-auth**

```bash
cd web
pnpm add next-auth@4.24.7
cd ..
```

Expected: next-auth added to package.json

- [ ] **Step 2: Create NextAuth configuration**

Create file `web/lib/auth.ts`:

```typescript
import { type NextAuthOptions } from "next-auth";
import CredentialsProvider from "next-auth/providers/credentials";

export const authOptions: NextAuthOptions = {
  providers: [
    CredentialsProvider({
      name: "Credentials",
      credentials: {
        email: { label: "Email", type: "email" },
        password: { label: "Password", type: "password" },
      },
      async authorize(credentials) {
        if (!credentials?.email || !credentials?.password) {
          return null;
        }

        const res = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/auth/login`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              email: credentials.email,
              password: credentials.password,
            }),
          }
        );

        if (!res.ok) {
          return null;
        }

        const data = await res.json();

        return {
          id: data.user.id,
          email: data.user.email,
          name: data.user.displayName,
          role: data.user.role,
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
        };
      },
    }),
  ],
  session: {
    strategy: "jwt",
    maxAge: 7 * 24 * 60 * 60, // 7 days
  },
  callbacks: {
    async jwt({ token, user }) {
      if (user) {
        token.role = user.role;
        token.accessToken = user.accessToken;
        token.refreshToken = user.refreshToken;
        token.id = user.id;
      }
      return token;
    },
    async session({ session, token }) {
      if (session.user) {
        session.user.id = token.id as string;
        session.user.role = token.role as string;
        session.user.accessToken = token.accessToken as string;
      }
      return session;
    },
  },
  pages: {
    signIn: "/login",
  },
};
```

- [ ] **Step 3: Create NextAuth route handler**

Create file `web/app/api/auth/[...nextauth]/route.ts`:

```typescript
import NextAuth from "next-auth";
import { authOptions } from "@/lib/auth";

const handler = NextAuth(authOptions);
export { handler as GET, handler as POST };
```

- [ ] **Step 4: Create next-auth type extensions**

Create file `web/types/next-auth.d.ts`:

```typescript
import "next-auth";
import "next-auth/jwt";

declare module "next-auth" {
  interface User {
    id: string;
    role: string;
    accessToken: string;
    refreshToken: string;
  }

  interface Session {
    user: {
      id: string;
      name: string;
      email: string;
      role: string;
      accessToken: string;
    };
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    id: string;
    role: string;
    accessToken: string;
    refreshToken: string;
  }
}
```

- [ ] **Step 5: Create login page**

Create file `web/app/login/page.tsx`:

```typescript
"use client";

import { useState } from "react";
import { signIn } from "next-auth/react";
import { useRouter } from "next/navigation";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);

    const result = await signIn("credentials", {
      email,
      password,
      redirect: false,
    });

    setLoading(false);

    if (result?.error) {
      setError("邮箱或密码错误");
    } else {
      router.push("/agent");
      router.refresh();
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-xl shadow-lg p-8">
          {/* Logo */}
          <div className="text-center mb-8">
            <div className="inline-flex items-center justify-center w-16 h-16 rounded-full bg-teal-100 mb-4">
              <svg
                className="w-8 h-8 text-teal-600"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M9 3v2m6-2v2M9 19v2m6-2v2M5 9H3m2 6H3m18-6h-2m2 6h-2M7 19h10a2 2 0 002-2V7a2 2 0 00-2-2H7a2 2 0 00-2 2v10a2 2 0 002 2zM9 9h6v6H9V9z"
                />
              </svg>
            </div>
            <h1 className="text-2xl font-bold text-gray-900">MES Copilot</h1>
            <p className="text-sm text-gray-500 mt-1">智能制造运营助手</p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && (
              <div className="p-3 rounded-lg bg-red-50 border border-red-200 text-red-700 text-sm">
                {error}
              </div>
            )}

            <div>
              <label
                htmlFor="email"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                邮箱
              </label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none transition"
                placeholder="your@email.com"
              />
            </div>

            <div>
              <label
                htmlFor="password"
                className="block text-sm font-medium text-gray-700 mb-1"
              >
                密码
              </label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                className="w-full px-4 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none transition"
                placeholder="输入密码"
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="w-full py-2.5 px-4 bg-teal-600 hover:bg-teal-700 disabled:bg-teal-300 text-white font-medium rounded-lg transition duration-200"
            >
              {loading ? (
                <span className="inline-flex items-center">
                  <svg
                    className="animate-spin -ml-1 mr-2 h-4 w-4 text-white"
                    fill="none"
                    viewBox="0 0 24 24"
                  >
                    <circle
                      className="opacity-25"
                      cx="12"
                      cy="12"
                      r="10"
                      stroke="currentColor"
                      strokeWidth="4"
                    />
                    <path
                      className="opacity-75"
                      fill="currentColor"
                      d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                    />
                  </svg>
                  登录中...
                </span>
              ) : (
                "登录"
              )}
            </button>
          </form>
        </div>
      </div>
    </div>
  );
}
```

- [ ] **Step 6: Create middleware for route protection**

Create file `web/middleware.ts`:

```typescript
import { withAuth } from "next-auth/middleware";

export default withAuth({
  pages: {
    signIn: "/login",
  },
});

export const config = {
  matcher: [
    "/((?!login|api/auth|_next/static|_next/image|favicon.ico).*)",
  ],
};
```

- [ ] **Step 7: Create Providers wrapper**

Create file `web/components/providers.tsx`:

```typescript
"use client";

import { SessionProvider } from "next-auth/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";

export function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000,
            refetchOnWindowFocus: false,
          },
        },
      })
  );

  return (
    <SessionProvider>
      <QueryClientProvider client={queryClient}>
        {children}
      </QueryClientProvider>
    </SessionProvider>
  );
}
```

- [ ] **Step 8: Update layout.tsx to use Providers**

Modify `web/app/layout.tsx`:

```typescript
import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { Providers } from "@/components/providers";
import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "MES Copilot",
  description: "智能制造运营助手",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="zh-CN">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased`}
      >
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
```

- [ ] **Step 9: Add NEXTAUTH_SECRET to .env.local**

Create file `web/.env.local`:

```
NEXTAUTH_URL=http://localhost:3000
NEXTAUTH_SECRET=your-development-secret-change-in-production
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

- [ ] **Step 10: Build frontend to verify**

```bash
cd web
pnpm build
cd ..
```

Expected: Build succeeds with no errors

- [ ] **Step 11: Write frontend test for login page**

Create file `web/app/login/page.test.tsx`:

```typescript
import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import LoginPage from "./page";

vi.mock("next-auth/react", () => ({
  signIn: vi.fn(),
}));

vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: vi.fn(), refresh: vi.fn() }),
}));

describe("LoginPage", () => {
  it("renders login form", () => {
    render(<LoginPage />);

    expect(screen.getByText("MES Copilot")).toBeInTheDocument();
    expect(screen.getByLabelText("邮箱")).toBeInTheDocument();
    expect(screen.getByLabelText("密码")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "登录" })).toBeInTheDocument();
  });

  it("shows loading state on submit", async () => {
    const { getByRole, getByLabelText } = render(<LoginPage />);
    const { fireEvent } = await import("@testing-library/react");

    fireEvent.change(getByLabelText("邮箱"), {
      target: { value: "test@example.com" },
    });
    fireEvent.change(getByLabelText("密码"), {
      target: { value: "password" },
    });
    fireEvent.click(getByRole("button", { name: "登录" }));

    expect(screen.getByText("登录中...")).toBeInTheDocument();
  });
});
```

- [ ] **Step 12: Run frontend tests**

```bash
cd web
pnpm test
cd ..
```

Expected: PASS - Login page tests pass

- [ ] **Step 13: Commit**

```bash
git add web/
git commit -m "feat(web): add next-auth authentication with login page

- next-auth CredentialsProvider connecting to backend /api/auth/login
- JWT session strategy with role in token
- Login page: centered card, teal theme, loading/error states
- Route protection middleware (all routes except /login)
- Providers wrapper: SessionProvider + QueryClientProvider
- Type extensions for next-auth User/Session/JWT
- .env.local with NEXTAUTH_SECRET
- Login page unit tests"
```

Expected: Changes committed

---

### Task 2A.7: Manual Verification of Authentication Flow

**Files:**
- None (manual testing)

**Interfaces:**
- Consumes: All Phase 2A components
- Produces: Verified authentication system

- [ ] **Step 1: Apply database migration**

```bash
cd src/MesCopilot.Api
dotnet ef database update --project ../MesCopilot.Infrastructure/MesCopilot.Infrastructure.csproj
cd ../..
```

Expected: Identity tables created (AspNetUsers, AspNetRoles, etc.)

- [ ] **Step 2: Start backend**

```bash
dotnet run --project src/MesCopilot.Api
```

Expected: API running on http://localhost:5000

- [ ] **Step 3: Test register endpoint**

```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@mescopilot.com",
    "password": "Admin123!",
    "displayName": "System Admin",
    "role": "Admin"
  }'
```

Expected: 201 Created with accessToken and refreshToken

- [ ] **Step 4: Test login endpoint**

```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@mescopilot.com",
    "password": "Admin123!"
  }'
```

Expected: 200 OK with tokens

- [ ] **Step 5: Test protected endpoint without token**

```bash
curl http://localhost:5000/api/workorders
```

Expected: 401 Unauthorized

- [ ] **Step 6: Test protected endpoint with token**

```bash
TOKEN="<access_token_from_step_4>"
curl http://localhost:5000/api/workorders \
  -H "Authorization: Bearer $TOKEN"
```

Expected: 200 OK with work orders data

- [ ] **Step 7: Start frontend**

```bash
cd web
pnpm dev
```

Expected: Frontend running on http://localhost:3000

- [ ] **Step 8: Test frontend login flow**

1. Navigate to http://localhost:3000
2. Should auto-redirect to /login
3. Enter credentials: admin@mescopilot.com / Admin123!
4. Should redirect to /agent after successful login

Expected: Login successful, session established

- [ ] **Step 9: Verify session persistence**

1. Refresh the page
2. Should stay logged in (not redirect to /login)

Expected: Session persists across refresh

- [ ] **Step 10: Test logout (if implemented)**

Expected: Redirects to /login after logout

---

## Sub-Phase 2B: Agent Prompt Engineering Enhancement

### Task 2B.1: Create Prompt Template System

**Files:**
- Create: `src/MesCopilot.Agent/Prompts/IPromptBuilder.cs`
- Create: `src/MesCopilot.Agent/Prompts/MesPromptBuilder.cs`
- Create: `src/MesCopilot.Agent/Prompts/Templates/production_agent_template.txt`
- Create: `src/MesCopilot.Agent/Prompts/Templates/quality_agent_template.txt`
- Create: `src/MesCopilot.Agent/Prompts/Templates/oee_agent_template.txt`
- Create: `src/MesCopilot.Agent/Prompts/Templates/knowledge_agent_template.txt`
- Create: `tests/MesCopilot.UnitTests/Agent/Prompts/MesPromptBuilderTests.cs`

**Interfaces:**
- Consumes: None
- Produces: IPromptBuilder (builds Few-Shot + CoT prompts for each agent)

- [ ] **Step 1: Create IPromptBuilder interface**

Create file `src/MesCopilot.Agent/Prompts/IPromptBuilder.cs`:

```csharp
namespace MesCopilot.Agent.Prompts;

public interface IPromptBuilder
{
    string BuildSystemPrompt(string agentMode);
    string BuildUserPrompt(string question, Dictionary<string, object>? context = null);
}
```

- [ ] **Step 2: Create production agent template**

Create file `src/MesCopilot.Agent/Prompts/Templates/production_agent_template.txt`:

```
You are a Production Management Agent for a MES (Manufacturing Execution System). Your role is to assist with work order queries, production status analysis, and scheduling insights.

## Constraints
- Only answer questions related to production, work orders, production lines, and scheduling
- Always base your answers on data retrieved from tool calls
- If data is missing, explicitly state what information is unavailable
- Use metric units and ISO 8601 datetime format
- Round percentages to 2 decimal places

## Tools Available
- GetTodayWorkOrders: retrieves today's work orders with status
- AnalyzeDelayedOrders: identifies delayed orders with root cause analysis
- [Future: SuggestSchedule]

## Response Format
1. Structured Data: JSON object with relevant fields
2. Explanation: Natural language summary in Chinese (2-3 sentences)
3. Debug Info (if debugMode=true): Show tool calls and reasoning steps

## Few-Shot Examples

### Example 1: Today's Work Orders Query
User: "今天有哪些工单在生产？"

Reasoning (Chain-of-Thought):
1. User wants today's work orders filtered by InProgress status
2. Call GetTodayWorkOrders(status=InProgress)
3. Format results with code, product, line, and progress

Tool Call: GetTodayWorkOrders(status="InProgress")
Tool Result: [
  {"code": "WO-20260709-001", "productName": "产品A", "lineName": "产线1", "completedQty": 800, "plannedQty": 1000},
  {"code": "WO-20260709-003", "productName": "产品B", "lineName": "产线2", "completedQty": 450, "plannedQty": 500}
]

Response:
{
  "data": {
    "workOrders": [
      {"code": "WO-20260709-001", "product": "产品A", "line": "产线1", "progress": "80%"},
      {"code": "WO-20260709-003", "product": "产品B", "line": "产线2", "progress": "90%"}
    ],
    "totalCount": 2
  },
  "explanation": "今天有 2 个工单正在生产中：产线1 的产品A（进度 80%）和产线2 的产品B（进度 90%）。"
}

### Example 2: Delayed Orders Analysis
User: "有哪些工单延期了？原因是什么？"

Reasoning:
1. User wants delayed orders with root cause
2. Call AnalyzeDelayedOrders()
3. Group by delay reason and highlight top issues

Tool Call: AnalyzeDelayedOrders()
Tool Result: [
  {"code": "WO-20260708-002", "delayHours": 8, "reason": "设备故障 (E-201)"},
  {"code": "WO-20260707-005", "delayHours": 12, "reason": "物料短缺"}
]

Response:
{
  "data": {
    "delayedOrders": [
      {"code": "WO-20260708-002", "delayHours": 8, "reason": "设备故障"},
      {"code": "WO-20260707-005", "delayHours": 12, "reason": "物料短缺"}
    ],
    "totalCount": 2,
    "topReasons": ["物料短缺", "设备故障"]
  },
  "explanation": "当前有 2 个工单延期，主要原因是物料短缺（12小时延期）和设备故障（8小时延期）。建议优先处理物料采购和设备维修。"
}

### Example 3: Out-of-Scope Question
User: "质检批次 B12345 的不良品有多少？"

Response:
{
  "data": null,
  "explanation": "这个问题属于质量管理范畴，请切换到 Quality Agent 模式查询质检数据。"
}

## When You Don't Know
If no tool can answer the question, respond:
{
  "data": null,
  "explanation": "抱歉，我目前无法回答这个问题。我的工具仅支持查询今天的工单状态和延期分析。"
}
```

- [ ] **Step 3: Create quality agent template**

Create file `src/MesCopilot.Agent/Prompts/Templates/quality_agent_template.txt`:

```
You are a Quality Management Agent for a MES system. Your role is to assist with quality inspection queries, defect analysis, and batch traceability.

## Constraints
- Only answer questions related to quality inspections, defects, and traceability
- Always cite batch numbers when discussing specific inspections
- Defect rates should be calculated as (defect count / inspected quantity) × 100%
- Use ISO 8601 datetime format

## Tools Available
- TraceBatch: retrieve full traceability for a batch (work order, process steps, inspections, defects)
- GetDefectsByProcess: get defects grouped by process step
- AnalyzeDefectPattern: identify recurring defect types and root causes
- [Future: FiveWhyAnalysis]

## Response Format
Same as Production Agent (Structured Data + Explanation + Debug Info)

## Few-Shot Examples

### Example 1: Batch Traceability
User: "批次 B20260709001 的质量追溯信息"

Reasoning:
1. User wants full traceability for batch B20260709001
2. Call TraceBatch(batchNumber="B20260709001")
3. Present work order, process steps, inspections, and any defects

Tool Call: TraceBatch(batchNumber="B20260709001")
Tool Result: {
  "workOrder": {"code": "WO-20260709-001", "product": "产品A"},
  "processSteps": [
    {"name": "铣削", "operator": "OP001", "completedAt": "2026-07-09T08:30:00Z"},
    {"name": "热处理", "operator": "OP002", "completedAt": "2026-07-09T10:00:00Z"}
  ],
  "inspections": [
    {"type": "首件检验", "result": "Pass", "inspector": "QA001", "inspectedAt": "2026-07-09T08:35:00Z"},
    {"type": "终检", "result": "Pass", "inspector": "QA002", "inspectedAt": "2026-07-09T10:30:00Z"}
  ],
  "defects": []
}

Response:
{
  "data": {
    "batch": "B20260709001",
    "workOrder": "WO-20260709-001",
    "product": "产品A",
    "processSteps": ["铣削", "热处理"],
    "inspections": [
      {"step": "首件检验", "result": "Pass", "time": "08:35"},
      {"step": "终检", "result": "Pass", "time": "10:30"}
    ],
    "defectCount": 0
  },
  "explanation": "批次 B20260709001 来自工单 WO-20260709-001（产品A），经过铣削和热处理两道工序，首件检验和终检均合格，无不良品记录。"
}

### Example 2: Defect Pattern Analysis
User: "最近哪种缺陷类型最多？"

Reasoning:
1. User wants top defect types by frequency
2. Call AnalyzeDefectPattern()
3. Rank by count and show percentage

Tool Call: AnalyzeDefectPattern()
Tool Result: [
  {"defectType": "尺寸超差", "count": 45, "percentage": 38.5},
  {"defectType": "表面划伤", "count": 32, "percentage": 27.4},
  {"defectType": "毛刺", "count": 20, "percentage": 17.1}
]

Response:
{
  "data": {
    "topDefects": [
      {"type": "尺寸超差", "count": 45, "percentage": 38.5},
      {"type": "表面划伤", "count": 32, "percentage": 27.4},
      {"type": "毛刺", "count": 20, "percentage": 17.1}
    ],
    "totalDefects": 117
  },
  "explanation": "近期最常见的缺陷类型是尺寸超差（45次，占38.5%），其次是表面划伤（32次，27.4%）和毛刺（20次，17.1%）。建议重点检查加工精度控制。"
}
```

- [ ] **Step 4: Create OEE agent template**

Create file `src/MesCopilot.Agent/Prompts/Templates/oee_agent_template.txt`:

```
You are an OEE (Overall Equipment Effectiveness) Analysis Agent for a MES system. Your role is to provide equipment status monitoring, OEE calculation, and downtime analysis.

## Constraints
- OEE formula: Availability × Performance × Quality
- Availability = (Operating Time / Planned Production Time) × 100%
- Performance = (Actual Output / Theoretical Max Output) × 100%
- Quality = (Good Units / Total Units) × 100%
- Round OEE values to 2 decimal places

## Tools Available
- GetEquipmentStatus: current status of equipment (Running, Idle, Alarm, Maintenance, Offline)
- CalculateOee: calculate OEE for equipment over a date range
- AnalyzeLowOee: identify equipment with OEE < 75% and root causes
- [Future: PredictMaintenance]

## Few-Shot Examples

### Example 1: Equipment Status Query
User: "设备 E-201 现在状态如何？"

Reasoning:
1. User wants current status of E-201
2. Call GetEquipmentStatus(equipmentCode="E-201")
3. Present status, uptime, and recent alarms if any

Tool Call: GetEquipmentStatus(equipmentCode="E-201")
Tool Result: {"code": "E-201", "name": "CNC车床", "state": "Running", "uptime": 6.5, "lastAlarm": null}

Response:
{
  "data": {
    "equipment": "E-201",
    "name": "CNC车床",
    "status": "运行中",
    "uptime": "6.5小时",
    "hasAlarm": false
  },
  "explanation": "设备 E-201（CNC车床）目前运行正常，已持续运转 6.5 小时，无报警记录。"
}

### Example 2: OEE Calculation
User: "E-301 本周的 OEE 是多少？"

Reasoning:
1. User wants weekly OEE for E-301
2. Call CalculateOee(equipmentCode="E-301", from=<monday>, to=<today>)
3. Break down into Availability, Performance, Quality

Tool Call: CalculateOee(equipmentCode="E-301", from="2026-07-07", to="2026-07-09")
Tool Result: {"oee": 82.5, "availability": 92.0, "performance": 95.0, "quality": 94.5}

Response:
{
  "data": {
    "equipment": "E-301",
    "period": "2026-07-07 至 2026-07-09",
    "oee": 82.5,
    "breakdown": {
      "availability": 92.0,
      "performance": 95.0,
      "quality": 94.5
    }
  },
  "explanation": "设备 E-301 本周（7月7日至9日）的 OEE 为 82.5%，其中可用率 92.0%、性能率 95.0%、质量率 94.5%。整体表现良好。"
}
```

- [ ] **Step 5: Create knowledge agent template**

Create file `src/MesCopilot.Agent/Prompts/Templates/knowledge_agent_template.txt`:

```
You are a Knowledge Management Agent for a MES system. Your role is to search SOPs, maintenance manuals, process documents, and provide contextual answers using RAG (Retrieval-Augmented Generation).

## Constraints
- Always cite document sources with [来源: 文档名, 页码]
- If no relevant documents found, say so explicitly
- Use top-3 chunks for context (not just top-1)
- Do not fabricate information beyond retrieved context

## Tools Available
- SearchDocuments: vector similarity search across knowledge base
- GetSopByCode: retrieve specific SOP by code
- ContextualRagAnswer: generate answer from top-k document chunks

## Few-Shot Examples

### Example 1: SOP Query
User: "设备 E-201 的维护 SOP 是什么？"

Reasoning:
1. User wants maintenance SOP for E-201
2. Try GetSopByCode("E-201-MAINT")
3. If not found, fallback to SearchDocuments("E-201 维护")
4. Return SOP content with citation

Tool Call: SearchDocuments(query="E-201 维护", topK=3)
Tool Result: [
  {"docName": "CNC车床维护手册.pdf", "page": 12, "content": "E-201 每周需检查主轴润滑油位，每月更换切削液..."},
  {"docName": "设备保养计划.pdf", "page": 5, "content": "E-201 日常点检项目：电气连接、冷却系统、导轨清洁..."},
  {"docName": "CNC车床维护手册.pdf", "page": 13, "content": "润滑油更换周期：500小时或3个月..."}
]

Response:
{
  "data": {
    "equipment": "E-201",
    "maintenanceSteps": [
      "每周检查主轴润滑油位",
      "每月更换切削液",
      "日常点检：电气连接、冷却系统、导轨清洁",
      "润滑油更换：500小时或3个月"
    ]
  },
  "explanation": "E-201 的维护包括每周检查主轴润滑油、每月更换切削液、日常点检电气和冷却系统。润滑油更换周期为 500 小时或 3 个月。[来源: CNC车床维护手册.pdf, 第12-13页; 设备保养计划.pdf, 第5页]"
}

### Example 2: No Relevant Documents
User: "如何处理客户投诉？"

Reasoning:
1. User question is out of MES scope (customer service, not production)
2. SearchDocuments likely returns no relevant results
3. Explicitly state limitation

Response:
{
  "data": null,
  "explanation": "抱歉，知识库中未找到关于客户投诉处理的文档。知识库主要包含 SOP、设备维护手册和工艺文件。如需客服流程，请咨询质量部门或客服团队。"
}
```

- [ ] **Step 6: Create MesPromptBuilder implementation**

Create file `src/MesCopilot.Agent/Prompts/MesPromptBuilder.cs`:

```csharp
using System.Text;

namespace MesCopilot.Agent.Prompts;

public class MesPromptBuilder : IPromptBuilder
{
    private readonly Dictionary<string, string> _templates;

    public MesPromptBuilder()
    {
        _templates = LoadTemplates();
    }

    public string BuildSystemPrompt(string agentMode)
    {
        var normalizedMode = agentMode.ToLowerInvariant();
        if (_templates.TryGetValue(normalizedMode, out var template))
        {
            return template;
        }

        throw new ArgumentException($"Unknown agent mode: {agentMode}");
    }

    public string BuildUserPrompt(string question, Dictionary<string, object>? context = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"User: {question}");

        if (context != null && context.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Context:");
            foreach (var (key, value) in context)
            {
                sb.AppendLine($"- {key}: {value}");
            }
        }

        return sb.ToString();
    }

    private Dictionary<string, string> LoadTemplates()
    {
        var templates = new Dictionary<string, string>();
        var templateDir = Path.Combine(AppContext.BaseDirectory, "Prompts", "Templates");

        var files = new[]
        {
            ("production", "production_agent_template.txt"),
            ("quality", "quality_agent_template.txt"),
            ("oee", "oee_agent_template.txt"),
            ("knowledge", "knowledge_agent_template.txt")
        };

        foreach (var (mode, fileName) in files)
        {
            var filePath = Path.Combine(templateDir, fileName);
            if (File.Exists(filePath))
            {
                templates[mode] = File.ReadAllText(filePath);
            }
        }

        return templates;
    }
}
```

- [ ] **Step 7: Ensure template files are copied to output**

Modify `src/MesCopilot.Agent/MesCopilot.Agent.csproj` to add:

```xml
<ItemGroup>
  <None Update="Prompts\Templates\*.txt">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

- [ ] **Step 8: Write tests for MesPromptBuilder**

Create file `tests/MesCopilot.UnitTests/Agent/Prompts/MesPromptBuilderTests.cs`:

```csharp
using MesCopilot.Agent.Prompts;
using Xunit;

namespace MesCopilot.UnitTests.Agent.Prompts;

public class MesPromptBuilderTests
{
    private readonly MesPromptBuilder _builder = new();

    [Theory]
    [InlineData("Production")]
    [InlineData("Quality")]
    [InlineData("OEE")]
    [InlineData("Knowledge")]
    public void BuildSystemPrompt_WithValidMode_ReturnsTemplate(string mode)
    {
        var prompt = _builder.BuildSystemPrompt(mode);

        Assert.NotNull(prompt);
        Assert.NotEmpty(prompt);
        Assert.Contains("Agent", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_WithInvalidMode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _builder.BuildSystemPrompt("InvalidMode"));
    }

    [Fact]
    public void BuildUserPrompt_WithoutContext_ReturnsQuestionOnly()
    {
        var prompt = _builder.BuildUserPrompt("今天有哪些工单？");

        Assert.Contains("User: 今天有哪些工单？", prompt);
        Assert.DoesNotContain("Context:", prompt);
    }

    [Fact]
    public void BuildUserPrompt_WithContext_IncludesContextInfo()
    {
        var context = new Dictionary<string, object>
        {
            ["userId"] = "user-123",
            ["timestamp"] = "2026-07-09T10:00:00Z"
        };

        var prompt = _builder.BuildUserPrompt("今天有哪些工单？", context);

        Assert.Contains("User: 今天有哪些工单？", prompt);
        Assert.Contains("Context:", prompt);
        Assert.Contains("userId: user-123", prompt);
        Assert.Contains("timestamp:", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_Production_ContainsFewShotExamples()
    {
        var prompt = _builder.BuildSystemPrompt("Production");

        Assert.Contains("Few-Shot Examples", prompt);
        Assert.Contains("今天有哪些工单", prompt);
        Assert.Contains("Chain-of-Thought", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_Quality_ContainsTraceBatchExample()
    {
        var prompt = _builder.BuildSystemPrompt("Quality");

        Assert.Contains("TraceBatch", prompt);
        Assert.Contains("批次", prompt);
    }

    [Fact]
    public void BuildSystemPrompt_OEE_ContainsOeeFormula()
    {
        var prompt = _builder.BuildSystemPrompt("OEE");

        Assert.Contains("Availability × Performance × Quality", prompt);
    }
}
```

- [ ] **Step 9: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~MesPromptBuilderTests"
```

Expected: PASS - All prompt builder tests pass

- [ ] **Step 10: Build to verify**

```bash
dotnet build src/MesCopilot.Agent/MesCopilot.Agent.csproj
```

Expected: Build succeeds, template files copied to output

- [ ] **Step 11: Commit**

```bash
git add src/MesCopilot.Agent/Prompts/ tests/MesCopilot.UnitTests/Agent/Prompts/
git commit -m "feat(agent): add prompt engineering system with Few-Shot + CoT templates

- IPromptBuilder + MesPromptBuilder implementation
- 4 agent templates (Production, Quality, OEE, Knowledge)
- Each template includes: role definition, constraints, tool list, Few-Shot examples, CoT reasoning
- Templates show expected input/output format with Chinese explanations
- Template files copied to build output
- Unit tests for prompt builder (valid/invalid modes, context handling)"
```

Expected: Changes committed

---

### Task 2B.2: Implement Fact Verification System

**Files:**
- Create: `src/MesCopilot.Agent/Verification/IFactVerifier.cs`
- Create: `src/MesCopilot.Agent/Verification/FactVerifier.cs`
- Create: `src/MesCopilot.Agent/Verification/VerificationResult.cs`
- Create: `tests/MesCopilot.UnitTests/Agent/Verification/FactVerifierTests.cs`

**Interfaces:**
- Consumes: FunctionCallResult (Data field)
- Produces: VerificationResult (verified/discrepancies)

- [ ] **Step 1: Create VerificationResult model**

Create file `src/MesCopilot.Agent/Verification/VerificationResult.cs`:

```csharp
namespace MesCopilot.Agent.Verification;

public class VerificationResult
{
    public bool IsVerified { get; set; }
    public List<Discrepancy> Discrepancies { get; set; } = new();
    public int ClaimsChecked { get; set; }
    public int ClaimsVerified { get; set; }
}

public class Discrepancy
{
    public string Claim { get; set; } = string.Empty;
    public string ExpectedValue { get; set; } = string.Empty;
    public string ActualValue { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create IFactVerifier interface**

Create file `src/MesCopilot.Agent/Verification/IFactVerifier.cs`:

```csharp
using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Verification;

public interface IFactVerifier
{
    VerificationResult Verify(FunctionCallResult result);
}
```

- [ ] **Step 3: Create FactVerifier implementation**

Create file `src/MesCopilot.Agent/Verification/FactVerifier.cs`:

```csharp
using System.Text.Json;
using System.Text.RegularExpressions;
using MesCopilot.Agent.Models;

namespace MesCopilot.Agent.Verification;

public class FactVerifier : IFactVerifier
{
    private static readonly Regex NumberPattern = new(@"(\d+(?:\.\d+)?)\s*(%|个|件|次|台|小时|天)", RegexOptions.Compiled);
    private static readonly Regex CountPattern = new(@"(\d+)\s*(?:个|条|件|台|次)", RegexOptions.Compiled);
    private static readonly Regex PercentagePattern = new(@"(\d+(?:\.\d+)?)\s*%", RegexOptions.Compiled);

    public VerificationResult Verify(FunctionCallResult result)
    {
        var verification = new VerificationResult();

        if (result.Data == null || string.IsNullOrWhiteSpace(result.Explanation))
        {
            verification.IsVerified = true;
            return verification;
        }

        var dataJson = JsonSerializer.Serialize(result.Data);
        var claims = ExtractNumericalClaims(result.Explanation);
        verification.ClaimsChecked = claims.Count;

        foreach (var claim in claims)
        {
            if (VerifyClaim(claim, dataJson))
            {
                verification.ClaimsVerified++;
            }
            else
            {
                verification.Discrepancies.Add(new Discrepancy
                {
                    Claim = claim.Text,
                    ExpectedValue = claim.Value,
                    ActualValue = FindActualValue(claim, dataJson),
                    Field = claim.Field
                });
            }
        }

        verification.IsVerified = verification.Discrepancies.Count == 0;
        return verification;
    }

    private List<NumericalClaim> ExtractNumericalClaims(string explanation)
    {
        var claims = new List<NumericalClaim>();

        // Extract count claims: "有 5 个工单"
        foreach (Match match in CountPattern.Matches(explanation))
        {
            var context = GetSurroundingText(explanation, match.Index, 20);
            claims.Add(new NumericalClaim
            {
                Text = context,
                Value = match.Groups[1].Value,
                Type = ClaimType.Count,
                Field = InferField(context)
            });
        }

        // Extract percentage claims: "82.5%"
        foreach (Match match in PercentagePattern.Matches(explanation))
        {
            var context = GetSurroundingText(explanation, match.Index, 20);
            claims.Add(new NumericalClaim
            {
                Text = context,
                Value = match.Groups[1].Value,
                Type = ClaimType.Percentage,
                Field = InferField(context)
            });
        }

        return claims;
    }

    private bool VerifyClaim(NumericalClaim claim, string dataJson)
    {
        return dataJson.Contains(claim.Value);
    }

    private string FindActualValue(NumericalClaim claim, string dataJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(dataJson);
            var root = doc.RootElement;

            if (claim.Type == ClaimType.Count)
            {
                if (root.TryGetProperty("totalCount", out var totalCount))
                {
                    return totalCount.ToString();
                }
            }

            return "N/A";
        }
        catch
        {
            return "N/A";
        }
    }

    private string InferField(string context)
    {
        if (context.Contains("工单")) return "workOrders";
        if (context.Contains("缺陷") || context.Contains("不良")) return "defects";
        if (context.Contains("设备")) return "equipment";
        if (context.Contains("OEE")) return "oee";
        return "unknown";
    }

    private string GetSurroundingText(string text, int position, int radius)
    {
        var start = Math.Max(0, position - radius);
        var end = Math.Min(text.Length, position + radius);
        return text[start..end];
    }

    private class NumericalClaim
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public ClaimType Type { get; set; }
        public string Field { get; set; } = string.Empty;
    }

    private enum ClaimType
    {
        Count,
        Percentage
    }
}
```

- [ ] **Step 4: Write FactVerifier tests**

Create file `tests/MesCopilot.UnitTests/Agent/Verification/FactVerifierTests.cs`:

```csharp
using MesCopilot.Agent.Models;
using MesCopilot.Agent.Verification;
using Xunit;

namespace MesCopilot.UnitTests.Agent.Verification;

public class FactVerifierTests
{
    private readonly FactVerifier _verifier = new();

    [Fact]
    public void Verify_WhenDataMatchesExplanation_ReturnsVerified()
    {
        var result = new FunctionCallResult
        {
            Data = new { totalCount = 3, workOrders = new[] { "WO-001", "WO-002", "WO-003" } },
            Explanation = "今天有 3 个工单正在生产中。"
        };

        var verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
        Assert.Empty(verification.Discrepancies);
    }

    [Fact]
    public void Verify_WhenCountMismatch_ReturnsDiscrepancy()
    {
        var result = new FunctionCallResult
        {
            Data = new { totalCount = 5, workOrders = new[] { "WO-001", "WO-002", "WO-003", "WO-004", "WO-005" } },
            Explanation = "今天有 3 个工单正在生产中。"
        };

        var verification = _verifier.Verify(result);

        Assert.False(verification.IsVerified);
        Assert.NotEmpty(verification.Discrepancies);
    }

    [Fact]
    public void Verify_WhenPercentageMatchesData_ReturnsVerified()
    {
        var result = new FunctionCallResult
        {
            Data = new { oee = 82.5, availability = 92.0, performance = 95.0, quality = 94.5 },
            Explanation = "设备 OEE 为 82.5%，可用率 92.0%。"
        };

        var verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
    }

    [Fact]
    public void Verify_WhenNullData_ReturnsVerified()
    {
        var result = new FunctionCallResult
        {
            Data = null,
            Explanation = "没有找到相关数据。"
        };

        var verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
    }

    [Fact]
    public void Verify_WhenEmptyExplanation_ReturnsVerified()
    {
        var result = new FunctionCallResult
        {
            Data = new { count = 5 },
            Explanation = ""
        };

        var verification = _verifier.Verify(result);

        Assert.True(verification.IsVerified);
    }

    [Fact]
    public void Verify_TracksClaimCount()
    {
        var result = new FunctionCallResult
        {
            Data = new { totalCount = 2, delayHours = 8 },
            Explanation = "当前有 2 个工单延期，最长延期 8 小时。"
        };

        var verification = _verifier.Verify(result);

        Assert.True(verification.ClaimsChecked >= 2);
    }
}
```

- [ ] **Step 5: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~FactVerifierTests"
```

Expected: PASS - All fact verifier tests pass

- [ ] **Step 6: Register FactVerifier in DI**

Add to `src/MesCopilot.Agent/DependencyInjection.cs` (or create if not exists):

```csharp
using MesCopilot.Agent.Prompts;
using MesCopilot.Agent.Verification;
using Microsoft.Extensions.DependencyInjection;

namespace MesCopilot.Agent;

public static class DependencyInjection
{
    public static IServiceCollection AddMesCopilotAgent(this IServiceCollection services)
    {
        services.AddSingleton<IPromptBuilder, MesPromptBuilder>();
        services.AddScoped<IFactVerifier, FactVerifier>();
        return services;
    }
}
```

- [ ] **Step 7: Commit**

```bash
git add src/MesCopilot.Agent/Verification/ src/MesCopilot.Agent/DependencyInjection.cs tests/MesCopilot.UnitTests/Agent/Verification/
git commit -m "feat(agent): add fact verification system

- IFactVerifier + FactVerifier implementation
- Extracts numerical claims from explanation (counts, percentages)
- Cross-references claims against structured data
- Returns VerificationResult with discrepancies list
- Handles null data and empty explanation gracefully
- DI registration in AddMesCopilotAgent()
- Unit tests covering match/mismatch/edge cases"
```

Expected: Changes committed

---

### Task 2B.3: Upgrade RAG Answer Generator

**Files:**
- Create: `src/MesCopilot.Agent/Plugins/KnowledgeAgentPlugin/VerifiedRagAnswerGenerator.cs`
- Modify: `src/MesCopilot.Agent/Plugins/KnowledgeAgentPlugin/IRagAnswerGenerator.cs` (extend interface)
- Create: `tests/MesCopilot.UnitTests/Agent/Plugins/KnowledgeAgentPlugin/VerifiedRagAnswerGeneratorTests.cs`

**Interfaces:**
- Consumes: IVectorStore (top-3 chunks), IPromptBuilder (knowledge template)
- Produces: RAG answers with source citations

- [ ] **Step 1: Extend IRagAnswerGenerator interface**

Modify file `src/MesCopilot.Agent/Plugins/KnowledgeAgentPlugin/IRagAnswerGenerator.cs`:

```csharp
namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;

public interface IRagAnswerGenerator
{
    Task<RagAnswer> GenerateAnswerAsync(string question, IReadOnlyList<DocumentChunkResult> chunks);
}

public class RagAnswer
{
    public string Answer { get; set; } = string.Empty;
    public List<SourceCitation> Citations { get; set; } = new();
    public int ChunksUsed { get; set; }
    public bool IsConfident { get; set; }
}

public class SourceCitation
{
    public string DocumentName { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public float RelevanceScore { get; set; }
}

public class DocumentChunkResult
{
    public string Content { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public int? PageNumber { get; set; }
    public float Score { get; set; }
}
```

- [ ] **Step 2: Create VerifiedRagAnswerGenerator**

Create file `src/MesCopilot.Agent/Plugins/KnowledgeAgentPlugin/VerifiedRagAnswerGenerator.cs`:

```csharp
using System.Text;

namespace MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;

public class VerifiedRagAnswerGenerator : IRagAnswerGenerator
{
    private const int MaxTokenBudget = 2000;
    private const int TopK = 3;
    private const float MinRelevanceThreshold = 0.6f;

    public Task<RagAnswer> GenerateAnswerAsync(string question, IReadOnlyList<DocumentChunkResult> chunks)
    {
        // Filter by relevance threshold
        var relevantChunks = chunks
            .Where(c => c.Score >= MinRelevanceThreshold)
            .OrderByDescending(c => c.Score)
            .Take(TopK)
            .ToList();

        if (relevantChunks.Count == 0)
        {
            return Task.FromResult(new RagAnswer
            {
                Answer = "抱歉，知识库中未找到与您问题相关的文档。",
                Citations = new List<SourceCitation>(),
                ChunksUsed = 0,
                IsConfident = false
            });
        }

        // Build context from chunks (respecting token budget)
        var contextBuilder = new StringBuilder();
        var citations = new List<SourceCitation>();
        var tokenCount = 0;

        foreach (var chunk in relevantChunks)
        {
            var chunkTokens = EstimateTokens(chunk.Content);
            if (tokenCount + chunkTokens > MaxTokenBudget)
            {
                // Truncate to fit budget
                var remainingBudget = MaxTokenBudget - tokenCount;
                var truncatedContent = TruncateToTokens(chunk.Content, remainingBudget);
                contextBuilder.AppendLine(truncatedContent);
                citations.Add(new SourceCitation
                {
                    DocumentName = chunk.DocumentName,
                    PageNumber = chunk.PageNumber,
                    RelevanceScore = chunk.Score
                });
                break;
            }

            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();
            tokenCount += chunkTokens;

            citations.Add(new SourceCitation
            {
                DocumentName = chunk.DocumentName,
                PageNumber = chunk.PageNumber,
                RelevanceScore = chunk.Score
            });
        }

        // Build answer with citations
        var answer = BuildAnswerWithCitations(contextBuilder.ToString(), citations);

        return Task.FromResult(new RagAnswer
        {
            Answer = answer,
            Citations = citations,
            ChunksUsed = citations.Count,
            IsConfident = relevantChunks.First().Score >= 0.8f
        });
    }

    private string BuildAnswerWithCitations(string context, List<SourceCitation> citations)
    {
        var citationText = string.Join("; ",
            citations.Select(c =>
                c.PageNumber.HasValue
                    ? $"{c.DocumentName}, 第{c.PageNumber}页"
                    : c.DocumentName));

        return $"{context.Trim()}\n\n[来源: {citationText}]";
    }

    private int EstimateTokens(string text)
    {
        // Rough estimate: 1 Chinese char ≈ 1.5 tokens, 1 English word ≈ 1 token
        return (int)(text.Length * 1.2);
    }

    private string TruncateToTokens(string text, int maxTokens)
    {
        var maxChars = (int)(maxTokens / 1.2);
        if (text.Length <= maxChars) return text;
        return text[..maxChars] + "...";
    }
}
```

- [ ] **Step 3: Write tests for VerifiedRagAnswerGenerator**

Create file `tests/MesCopilot.UnitTests/Agent/Plugins/KnowledgeAgentPlugin/VerifiedRagAnswerGeneratorTests.cs`:

```csharp
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using Xunit;

namespace MesCopilot.UnitTests.Agent.Plugins.KnowledgeAgentPlugin;

public class VerifiedRagAnswerGeneratorTests
{
    private readonly VerifiedRagAnswerGenerator _generator = new();

    [Fact]
    public async Task GenerateAnswer_WithRelevantChunks_IncludesCitations()
    {
        var chunks = new List<DocumentChunkResult>
        {
            new() { Content = "E-201 每周需检查主轴润滑油位", DocumentName = "维护手册.pdf", PageNumber = 12, Score = 0.92f },
            new() { Content = "润滑油更换周期500小时", DocumentName = "维护手册.pdf", PageNumber = 13, Score = 0.85f },
            new() { Content = "日常点检项目列表", DocumentName = "保养计划.pdf", PageNumber = 5, Score = 0.78f }
        };

        var result = await _generator.GenerateAnswerAsync("E-201维护", chunks);

        Assert.NotEmpty(result.Answer);
        Assert.Contains("[来源:", result.Answer);
        Assert.Contains("维护手册.pdf", result.Answer);
        Assert.Equal(3, result.ChunksUsed);
        Assert.True(result.IsConfident);
    }

    [Fact]
    public async Task GenerateAnswer_WithNoRelevantChunks_ReturnsNotFound()
    {
        var chunks = new List<DocumentChunkResult>
        {
            new() { Content = "无关内容", DocumentName = "other.pdf", PageNumber = 1, Score = 0.3f }
        };

        var result = await _generator.GenerateAnswerAsync("E-201维护", chunks);

        Assert.Contains("未找到", result.Answer);
        Assert.Empty(result.Citations);
        Assert.Equal(0, result.ChunksUsed);
        Assert.False(result.IsConfident);
    }

    [Fact]
    public async Task GenerateAnswer_WithEmptyChunks_ReturnsNotFound()
    {
        var chunks = new List<DocumentChunkResult>();

        var result = await _generator.GenerateAnswerAsync("任何问题", chunks);

        Assert.Contains("未找到", result.Answer);
        Assert.False(result.IsConfident);
    }

    [Fact]
    public async Task GenerateAnswer_RespectsTopK()
    {
        var chunks = new List<DocumentChunkResult>
        {
            new() { Content = "内容1", DocumentName = "doc1.pdf", PageNumber = 1, Score = 0.95f },
            new() { Content = "内容2", DocumentName = "doc2.pdf", PageNumber = 2, Score = 0.90f },
            new() { Content = "内容3", DocumentName = "doc3.pdf", PageNumber = 3, Score = 0.85f },
            new() { Content = "内容4（不应被使用）", DocumentName = "doc4.pdf", PageNumber = 4, Score = 0.75f }
        };

        var result = await _generator.GenerateAnswerAsync("测试", chunks);

        Assert.Equal(3, result.ChunksUsed);
        Assert.DoesNotContain("doc4.pdf", result.Answer);
    }

    [Fact]
    public async Task GenerateAnswer_IncludesPageNumbers()
    {
        var chunks = new List<DocumentChunkResult>
        {
            new() { Content = "SOP内容", DocumentName = "SOP-001.pdf", PageNumber = 5, Score = 0.9f }
        };

        var result = await _generator.GenerateAnswerAsync("SOP", chunks);

        Assert.Contains("第5页", result.Answer);
    }

    [Fact]
    public async Task GenerateAnswer_HandlesNullPageNumber()
    {
        var chunks = new List<DocumentChunkResult>
        {
            new() { Content = "文本内容", DocumentName = "readme.txt", PageNumber = null, Score = 0.85f }
        };

        var result = await _generator.GenerateAnswerAsync("问题", chunks);

        Assert.Contains("readme.txt", result.Answer);
        Assert.DoesNotContain("第", result.Answer);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~VerifiedRagAnswerGeneratorTests"
```

Expected: PASS - All RAG tests pass

- [ ] **Step 5: Build entire solution**

```bash
dotnet build
```

Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/MesCopilot.Agent/Plugins/KnowledgeAgentPlugin/ tests/MesCopilot.UnitTests/Agent/Plugins/KnowledgeAgentPlugin/
git commit -m "feat(agent): upgrade RAG to VerifiedRagAnswerGenerator with citations

- Use top-3 chunks (was top-1) with relevance threshold 0.6
- Dynamic token budget truncation (2000 tokens max, not fixed 600 chars)
- Source citations in format [来源: 文档名, 页码]
- IsConfident flag when top chunk score >= 0.8
- Graceful handling when no relevant chunks found
- Unit tests covering all scenarios"
```

Expected: Changes committed

---

## Sub-Phase 2C: Streaming Response + Conversation Memory (5 days)

### Global Constraints (2C)

- SSE format follows OpenAI Chat Completions streaming: `data: {"type":"...","content":"..."}\n\n`
- Event types: `thinking`, `token`, `tool_result`, `done`, `error`
- Conversation context window: 10 messages default, auto-summarize when exceeding token limit
- All conversation data owned by authenticated user (requires 2A complete)
- ConversationMessage.ToolResults stored as JSONB

---

### Task 2C.1: Add Conversation Domain Entities

**Files:**
- Create: `src/MesCopilot.Domain/Entities/Conversations/Conversation.cs`
- Create: `src/MesCopilot.Domain/Entities/Conversations/ConversationMessage.cs`
- Create: `src/MesCopilot.Domain/Enums/MessageRole.cs`
- Create: `src/MesCopilot.Domain/Enums/AgentMode.cs`
- Create: `tests/MesCopilot.UnitTests/Domain/Entities/Conversations/ConversationTests.cs`

**Interfaces:**
- Consumes: None
- Produces: Conversation, ConversationMessage entities

- [ ] **Step 1: Create MessageRole enum**

Create file `src/MesCopilot.Domain/Enums/MessageRole.cs`:

```csharp
namespace MesCopilot.Domain.Enums;

public enum MessageRole
{
    User = 0,
    Assistant = 1,
    Tool = 2,
    System = 3
}
```

- [ ] **Step 2: Create AgentMode enum**

Create file `src/MesCopilot.Domain/Enums/AgentMode.cs`:

```csharp
namespace MesCopilot.Domain.Enums;

public enum AgentMode
{
    Production = 0,
    Quality = 1,
    Oee = 2,
    Knowledge = 3
}
```

- [ ] **Step 3: Create Conversation entity**

Create file `src/MesCopilot.Domain/Entities/Conversations/Conversation.cs`:

```csharp
using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Conversations;

public class Conversation
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AgentMode Mode { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}
```

- [ ] **Step 4: Create ConversationMessage entity**

Create file `src/MesCopilot.Domain/Entities/Conversations/ConversationMessage.cs`:

```csharp
using MesCopilot.Domain.Enums;

namespace MesCopilot.Domain.Entities.Conversations;

public class ConversationMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ToolResults { get; set; } // JSONB
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Conversation Conversation { get; set; } = null!;
}
```

- [ ] **Step 5: Write tests for Conversation entity**

Create file `tests/MesCopilot.UnitTests/Domain/Entities/Conversations/ConversationTests.cs`:

```csharp
using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Enums;
using Xunit;

namespace MesCopilot.UnitTests.Domain.Entities.Conversations;

public class ConversationTests
{
    [Fact]
    public void Conversation_ShouldHaveRequiredProperties()
    {
        var conv = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = "user-123",
            Mode = AgentMode.Production,
            Title = "工单查询",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Assert.NotEqual(Guid.Empty, conv.Id);
        Assert.Equal("user-123", conv.UserId);
        Assert.Equal(AgentMode.Production, conv.Mode);
        Assert.Equal("工单查询", conv.Title);
    }

    [Fact]
    public void Conversation_ShouldInitializeMessages()
    {
        var conv = new Conversation();
        Assert.NotNull(conv.Messages);
        Assert.Empty(conv.Messages);
    }

    [Fact]
    public void ConversationMessage_ShouldSupportAllRoles()
    {
        var roles = Enum.GetValues<MessageRole>();
        Assert.Equal(4, roles.Length);
        Assert.Contains(MessageRole.User, roles);
        Assert.Contains(MessageRole.Assistant, roles);
        Assert.Contains(MessageRole.Tool, roles);
        Assert.Contains(MessageRole.System, roles);
    }

    [Fact]
    public void ConversationMessage_ShouldStoreToolResults()
    {
        var msg = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = Guid.NewGuid(),
            Role = MessageRole.Tool,
            Content = "工单查询结果",
            ToolResults = """{"workOrders":[{"code":"WO-001"}],"totalCount":1}""",
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal(MessageRole.Tool, msg.Role);
        Assert.NotNull(msg.ToolResults);
        Assert.Contains("WO-001", msg.ToolResults);
    }
}
```

- [ ] **Step 6: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~ConversationTests"
```

Expected: PASS - All conversation entity tests pass

- [ ] **Step 7: Commit**

```bash
git add src/MesCopilot.Domain/Entities/Conversations/ src/MesCopilot.Domain/Enums/MessageRole.cs src/MesCopilot.Domain/Enums/AgentMode.cs tests/MesCopilot.UnitTests/Domain/Entities/Conversations/
git commit -m "feat(domain): add conversation and message entities

- Conversation: Id, UserId, Mode, Title, timestamps
- ConversationMessage: Id, Role (User/Assistant/Tool/System), Content, ToolResults (JSONB)
- AgentMode enum: Production, Quality, Oee, Knowledge
- MessageRole enum: User, Assistant, Tool, System
- Unit tests for entity properties and collections"
```

Expected: Changes committed

---

### Task 2C.2: Add Conversation EF Configuration and Migration

**Files:**
- Create: `src/MesCopilot.Infrastructure/Data/Configurations/ConversationConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/MesDbContext.cs` (add DbSets)
- Create: EF Migration `AddConversations`

**Interfaces:**
- Consumes: Conversation, ConversationMessage entities
- Produces: Database tables with JSONB support

- [ ] **Step 1: Create ConversationConfiguration**

Create file `src/MesCopilot.Infrastructure/Data/Configurations/ConversationConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MesCopilot.Domain.Entities.Conversations;

namespace MesCopilot.Infrastructure.Data.Configurations;

public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(c => c.Title)
            .HasMaxLength(200);

        builder.Property(c => c.Mode)
            .HasConversion<int>();

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.CreatedAt);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("ConversationMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Role)
            .HasConversion<int>();

        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.ToolResults)
            .HasColumnType("jsonb");

        builder.HasIndex(m => m.ConversationId);
        builder.HasIndex(m => m.CreatedAt);
    }
}
```

- [ ] **Step 2: Add DbSets to MesDbContext**

Add the following to `src/MesCopilot.Infrastructure/Data/MesDbContext.cs`:

```csharp
// Add using
using MesCopilot.Domain.Entities.Conversations;

// Add DbSets (after Knowledge section)
// Conversations
public DbSet<Conversation> Conversations => Set<Conversation>();
public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();
```

- [ ] **Step 3: Create migration**

```bash
cd src/MesCopilot.Api
dotnet ef migrations add AddConversations --project ../MesCopilot.Infrastructure/MesCopilot.Infrastructure.csproj
cd ../..
```

Expected: Migration `AddConversations` created

- [ ] **Step 4: Verify migration applies**

```bash
cd src/MesCopilot.Api
dotnet ef database update --project ../MesCopilot.Infrastructure/MesCopilot.Infrastructure.csproj
cd ../..
```

Expected: Database updated with Conversations and ConversationMessages tables

- [ ] **Step 5: Build**

```bash
dotnet build
```

Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/MesCopilot.Infrastructure/Data/ src/MesCopilot.Infrastructure/Migrations/
git commit -m "feat(infra): add Conversations tables with JSONB support

- ConversationConfiguration: index on UserId, CreatedAt
- ConversationMessageConfiguration: ToolResults as JSONB column
- Cascade delete messages when conversation deleted
- EF Migration: AddConversations"
```

Expected: Changes committed

---

### Task 2C.3: Implement Conversation Service

**Files:**
- Create: `src/MesCopilot.Application/Services/IConversationService.cs`
- Create: `src/MesCopilot.Application/Services/ConversationService.cs`
- Create: `src/MesCopilot.Application/Dtos/ConversationDto.cs`
- Create: `tests/MesCopilot.UnitTests/Application/Services/ConversationServiceTests.cs`

**Interfaces:**
- Consumes: MesDbContext, Conversation entities
- Produces: CRUD + context window loading for conversations

- [ ] **Step 1: Create DTOs**

Create file `src/MesCopilot.Application/Dtos/ConversationDto.cs`:

```csharp
using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Dtos;

public class ConversationDto
{
    public Guid Id { get; set; }
    public AgentMode Mode { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
}

public class ConversationMessageDto
{
    public Guid Id { get; set; }
    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;
    public object? ToolResults { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConversationDetailDto
{
    public Guid Id { get; set; }
    public AgentMode Mode { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<ConversationMessageDto> Messages { get; set; } = new();
}

public class CreateConversationRequest
{
    public AgentMode Mode { get; set; }
    public string InitialMessage { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create IConversationService**

Create file `src/MesCopilot.Application/Services/IConversationService.cs`:

```csharp
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Services;

public interface IConversationService
{
    Task<ConversationDto> CreateConversationAsync(string userId, AgentMode mode, string initialMessage);
    Task<ConversationDetailDto?> GetConversationAsync(Guid conversationId, string userId);
    Task<List<ConversationDto>> GetUserConversationsAsync(string userId, int page = 1, int pageSize = 20);
    Task<List<ConversationMessageDto>> GetContextWindowAsync(Guid conversationId, int maxMessages = 10);
    Task AddMessageAsync(Guid conversationId, MessageRole role, string content, string? toolResults = null);
    Task UpdateTitleAsync(Guid conversationId, string title);
    Task DeleteConversationAsync(Guid conversationId, string userId);
}
```

- [ ] **Step 3: Implement ConversationService**

Create file `src/MesCopilot.Application/Services/ConversationService.cs`:

```csharp
using System.Text.Json;
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Entities.Conversations;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class ConversationService : IConversationService
{
    private readonly MesDbContext _context;

    public ConversationService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<ConversationDto> CreateConversationAsync(string userId, AgentMode mode, string initialMessage)
    {
        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Mode = mode,
            Title = GenerateTitle(initialMessage),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Conversations.Add(conversation);

        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Role = MessageRole.User,
            Content = initialMessage,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConversationMessages.Add(message);
        await _context.SaveChangesAsync();

        return new ConversationDto
        {
            Id = conversation.Id,
            Mode = conversation.Mode,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            UpdatedAt = conversation.UpdatedAt,
            MessageCount = 1
        };
    }

    public async Task<ConversationDetailDto?> GetConversationAsync(Guid conversationId, string userId)
    {
        var conversation = await _context.Conversations
            .AsNoTracking()
            .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation == null) return null;

        return new ConversationDetailDto
        {
            Id = conversation.Id,
            Mode = conversation.Mode,
            Title = conversation.Title,
            CreatedAt = conversation.CreatedAt,
            Messages = conversation.Messages.Select(m => new ConversationMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                ToolResults = m.ToolResults != null ? JsonSerializer.Deserialize<object>(m.ToolResults) : null,
                CreatedAt = m.CreatedAt
            }).ToList()
        };
    }

    public async Task<List<ConversationDto>> GetUserConversationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _context.Conversations
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ConversationDto
            {
                Id = c.Id,
                Mode = c.Mode,
                Title = c.Title,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                MessageCount = c.Messages.Count
            })
            .ToListAsync();
    }

    public async Task<List<ConversationMessageDto>> GetContextWindowAsync(Guid conversationId, int maxMessages = 10)
    {
        var messages = await _context.ConversationMessages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(maxMessages)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ConversationMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                ToolResults = m.ToolResults != null ? JsonSerializer.Deserialize<object>(m.ToolResults) : null,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        return messages;
    }

    public async Task AddMessageAsync(Guid conversationId, MessageRole role, string content, string? toolResults = null)
    {
        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            ToolResults = toolResults,
            CreatedAt = DateTime.UtcNow
        };

        _context.ConversationMessages.Add(message);

        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpdateTitleAsync(Guid conversationId, string title)
    {
        var conversation = await _context.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.Title = title;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteConversationAsync(Guid conversationId, string userId)
    {
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

        if (conversation != null)
        {
            _context.Conversations.Remove(conversation);
            await _context.SaveChangesAsync();
        }
    }

    private string GenerateTitle(string message)
    {
        if (message.Length <= 30) return message;
        return message[..27] + "...";
    }
}
```

- [ ] **Step 4: Register in DI**

Add to `src/MesCopilot.Application/DependencyInjection.cs`:

```csharp
services.AddScoped<IConversationService, ConversationService>();
```

- [ ] **Step 5: Write ConversationService tests**

Create file `tests/MesCopilot.UnitTests/Application/Services/ConversationServiceTests.cs`:

```csharp
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MesCopilot.UnitTests.Application.Services;

public class ConversationServiceTests : IDisposable
{
    private readonly MesDbContext _context;
    private readonly ConversationService _service;

    public ConversationServiceTests()
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MesDbContext(options);
        _service = new ConversationService(_context);
    }

    [Fact]
    public async Task CreateConversation_ShouldCreateWithInitialMessage()
    {
        var result = await _service.CreateConversationAsync("user-1", AgentMode.Production, "今天工单情况如何？");

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(AgentMode.Production, result.Mode);
        Assert.Equal(1, result.MessageCount);
    }

    [Fact]
    public async Task CreateConversation_ShouldTruncateTitle()
    {
        var longMessage = "这是一个非常非常非常长的消息，它的标题应该被截断到合理的长度以便于显示";
        var result = await _service.CreateConversationAsync("user-1", AgentMode.Quality, longMessage);

        Assert.True(result.Title.Length <= 30);
        Assert.EndsWith("...", result.Title);
    }

    [Fact]
    public async Task GetConversation_ShouldReturnWithMessages()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Oee, "OEE是多少？");
        await _service.AddMessageAsync(created.Id, MessageRole.Assistant, "当前OEE为82.5%");

        var result = await _service.GetConversationAsync(created.Id, "user-1");

        Assert.NotNull(result);
        Assert.Equal(2, result!.Messages.Count);
        Assert.Equal(MessageRole.User, result.Messages[0].Role);
        Assert.Equal(MessageRole.Assistant, result.Messages[1].Role);
    }

    [Fact]
    public async Task GetConversation_ShouldReturnNullForWrongUser()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Production, "test");

        var result = await _service.GetConversationAsync(created.Id, "user-2");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetContextWindow_ShouldLimitMessages()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Production, "msg1");
        for (int i = 2; i <= 15; i++)
        {
            await _service.AddMessageAsync(created.Id, MessageRole.User, $"msg{i}");
        }

        var context = await _service.GetContextWindowAsync(created.Id, maxMessages: 5);

        Assert.Equal(5, context.Count);
        Assert.Equal("msg11", context[0].Content); // Latest 5
    }

    [Fact]
    public async Task GetUserConversations_ShouldPaginate()
    {
        for (int i = 0; i < 5; i++)
        {
            await _service.CreateConversationAsync("user-1", AgentMode.Production, $"conv{i}");
        }

        var page1 = await _service.GetUserConversationsAsync("user-1", page: 1, pageSize: 3);
        var page2 = await _service.GetUserConversationsAsync("user-1", page: 2, pageSize: 3);

        Assert.Equal(3, page1.Count);
        Assert.Equal(2, page2.Count);
    }

    [Fact]
    public async Task AddMessage_ShouldUpdateConversationTimestamp()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Production, "test");
        var initialUpdate = created.UpdatedAt;

        await Task.Delay(10);
        await _service.AddMessageAsync(created.Id, MessageRole.Assistant, "response");

        var conv = await _context.Conversations.FindAsync(created.Id);
        Assert.True(conv!.UpdatedAt > initialUpdate);
    }

    [Fact]
    public async Task DeleteConversation_ShouldRemoveForCorrectUser()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Production, "test");

        await _service.DeleteConversationAsync(created.Id, "user-1");

        var conv = await _context.Conversations.FindAsync(created.Id);
        Assert.Null(conv);
    }

    [Fact]
    public async Task DeleteConversation_ShouldNotRemoveForWrongUser()
    {
        var created = await _service.CreateConversationAsync("user-1", AgentMode.Production, "test");

        await _service.DeleteConversationAsync(created.Id, "user-2");

        var conv = await _context.Conversations.FindAsync(created.Id);
        Assert.NotNull(conv);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
```

- [ ] **Step 6: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests/MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~ConversationServiceTests"
```

Expected: PASS - All conversation service tests pass

- [ ] **Step 7: Commit**

```bash
git add src/MesCopilot.Application/Services/IConversationService.cs src/MesCopilot.Application/Services/ConversationService.cs src/MesCopilot.Application/Dtos/ConversationDto.cs src/MesCopilot.Application/DependencyInjection.cs tests/MesCopilot.UnitTests/Application/Services/ConversationServiceTests.cs
git commit -m "feat(app): implement ConversationService with context window

- IConversationService interface + full implementation
- Create/Get/List/Delete conversations (user-scoped)
- Context window: last N messages (default 10)
- Auto-generate title from first message (truncate to 30 chars)
- UpdatedAt tracking on new messages
- Pagination support for conversation list
- DTOs: ConversationDto, ConversationMessageDto, ConversationDetailDto
- Unit tests: 8 scenarios covering CRUD + access control + pagination"
```

Expected: Changes committed

---

### Task 2C.4: Create AgentController with SSE Streaming Endpoint

**Files:**
- Create: `src/MesCopilot.Api/Controllers/AgentController.cs`
- Create: `src/MesCopilot.Api/Dtos/Agent/ChatRequest.cs`
- Create: `src/MesCopilot.Api/Dtos/Agent/SseEvent.cs`
- Create: `tests/MesCopilot.IntegrationTests/Controllers/AgentControllerTests.cs`

**Interfaces:**
- Consumes: IConversationService, Agent plugins, IPromptBuilder, IFactVerifier
- Produces: `POST /api/agent/chat` (SSE stream), `GET /api/agent/conversations`

- [ ] **Step 1: Create DTOs**

Create file `src/MesCopilot.Api/Dtos/Agent/ChatRequest.cs`:

```csharp
using MesCopilot.Domain.Enums;

namespace MesCopilot.Api.Dtos.Agent;

public class ChatRequest
{
    public Guid? ConversationId { get; set; }
    public AgentMode Mode { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool DebugMode { get; set; } = false;
}
```

Create file `src/MesCopilot.Api/Dtos/Agent/SseEvent.cs`:

```csharp
using System.Text.Json;

namespace MesCopilot.Api.Dtos.Agent;

public class SseEvent
{
    public string Type { get; set; } = string.Empty;
    public string? Content { get; set; }
    public object? Data { get; set; }

    public string ToSseFormat()
    {
        var json = JsonSerializer.Serialize(new
        {
            type = Type,
            content = Content,
            data = Data
        });
        return $"data: {json}\n\n";
    }
}
```

- [ ] **Step 2: Create AgentController (Part 1: Structure + Chat endpoint)**

Create file `src/MesCopilot.Api/Controllers/AgentController.cs`:

```csharp
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text.Json;
using MesCopilot.Agent.Plugins.ProductionAgentPlugin;
using MesCopilot.Agent.Plugins.QualityAgentPlugin;
using MesCopilot.Agent.Plugins.OeeAgentPlugin;
using MesCopilot.Agent.Plugins.KnowledgeAgentPlugin;
using MesCopilot.Agent.Prompts;
using MesCopilot.Agent.Verification;
using MesCopilot.Api.Dtos.Agent;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MesCopilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AgentController : ControllerBase
{
    private readonly IConversationService _conversationService;
    private readonly IPromptBuilder _promptBuilder;
    private readonly IFactVerifier _factVerifier;
    private readonly ProductionAgentPlugin _productionAgent;
    private readonly QualityAgentPlugin _qualityAgent;
    private readonly OeeAgentPlugin _oeeAgent;
    private readonly KnowledgeAgentPlugin _knowledgeAgent;

    public AgentController(
        IConversationService conversationService,
        IPromptBuilder promptBuilder,
        IFactVerifier factVerifier,
        ProductionAgentPlugin productionAgent,
        QualityAgentPlugin qualityAgent,
        OeeAgentPlugin oeeAgent,
        KnowledgeAgentPlugin knowledgeAgent)
    {
        _conversationService = conversationService;
        _promptBuilder = promptBuilder;
        _factVerifier = factVerifier;
        _productionAgent = productionAgent;
        _qualityAgent = qualityAgent;
        _oeeAgent = oeeAgent;
        _knowledgeAgent = knowledgeAgent;
    }

    [HttpPost("chat")]
    public async IAsyncEnumerable<string> Chat(
        [FromBody] ChatRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            yield return new SseEvent { Type = "error", Content = "Unauthorized" }.ToSseFormat();
            yield break;
        }

        Guid conversationId;
        if (request.ConversationId.HasValue)
        {
            conversationId = request.ConversationId.Value;
            await _conversationService.AddMessageAsync(conversationId, MessageRole.User, request.Message);
        }
        else
        {
            var conv = await _conversationService.CreateConversationAsync(userId, request.Mode, request.Message);
            conversationId = conv.Id;
        }

        var agent = GetAgentForMode(request.Mode);
        var tools = agent.GetTools();

        // Simulate LLM orchestration (Phase 2 uses rule-based tool selection)
        var selectedTool = SelectTool(request.Message, tools);

        if (selectedTool == null)
        {
            yield return new SseEvent { Type = "token", Content = "抱歉，我无法理解您的问题。" }.ToSseFormat();
            yield return new SseEvent { Type = "done" }.ToSseFormat();
            yield break;
        }

        // Thinking event
        yield return new SseEvent
        {
            Type = "thinking",
            Content = $"正在调用工具: {selectedTool.Name}"
        }.ToSseFormat();

        await Task.Delay(500, cancellationToken); // Simulate processing

        // Execute tool
        var toolResult = await selectedTool.ExecuteAsync(request.DebugMode);

        // Fact verification
        var verification = _factVerifier.Verify(toolResult);

        // Tool result event
        yield return new SseEvent
        {
            Type = "tool_result",
            Data = new
            {
                tool = selectedTool.Name,
                data = toolResult.Data,
                verified = verification.IsVerified,
                discrepancies = verification.Discrepancies
            }
        }.ToSseFormat();

        // Stream explanation token by token
        var explanation = toolResult.Explanation ?? "完成";
        foreach (var token in ChunkText(explanation, 5))
        {
            if (cancellationToken.IsCancellationRequested) break;

            yield return new SseEvent
            {
                Type = "token",
                Content = token
            }.ToSseFormat();

            await Task.Delay(50, cancellationToken);
        }

        // Save assistant message
        await _conversationService.AddMessageAsync(
            conversationId,
            MessageRole.Assistant,
            explanation,
            JsonSerializer.Serialize(toolResult.Data));

        // Done event
        yield return new SseEvent { Type = "done" }.ToSseFormat();
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var conversations = await _conversationService.GetUserConversationsAsync(userId, page, pageSize);
        return Ok(conversations);
    }

    [HttpGet("conversations/{id}")]
    public async Task<IActionResult> GetConversation(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        var conversation = await _conversationService.GetConversationAsync(id, userId);
        if (conversation == null) return NotFound();

        return Ok(conversation);
    }

    [HttpDelete("conversations/{id}")]
    public async Task<IActionResult> DeleteConversation(Guid id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId == null) return Unauthorized();

        await _conversationService.DeleteConversationAsync(id, userId);
        return NoContent();
    }

    private object GetAgentForMode(AgentMode mode)
    {
        return mode switch
        {
            AgentMode.Production => _productionAgent,
            AgentMode.Quality => _qualityAgent,
            AgentMode.Oee => _oeeAgent,
            AgentMode.Knowledge => _knowledgeAgent,
            _ => _productionAgent
        };
    }

    private dynamic? SelectTool(string message, IEnumerable<dynamic> tools)
    {
        // Simple keyword matching (replace with LLM in future)
        var lowerMessage = message.ToLower();

        if (lowerMessage.Contains("工单") || lowerMessage.Contains("今天"))
            return tools.FirstOrDefault(t => t.Name == "GetTodayWorkOrders");
        if (lowerMessage.Contains("延期"))
            return tools.FirstOrDefault(t => t.Name == "AnalyzeDelayedOrders");
        if (lowerMessage.Contains("批次") || lowerMessage.Contains("追溯"))
            return tools.FirstOrDefault(t => t.Name == "TraceBatch");
        if (lowerMessage.Contains("oee") || lowerMessage.Contains("设备"))
            return tools.FirstOrDefault(t => t.Name == "GetEquipmentStatus");

        return tools.FirstOrDefault();
    }

    private IEnumerable<string> ChunkText(string text, int chunkSize)
    {
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
        }
    }
}
```

- [ ] **Step 3: Write integration test for SSE streaming**

Create file `tests/MesCopilot.IntegrationTests/Controllers/AgentControllerTests.cs`:

```csharp
using System.Net.Http.Json;
using MesCopilot.Api.Dtos.Agent;
using MesCopilot.Domain.Enums;
using Xunit;

namespace MesCopilot.IntegrationTests.Controllers;

public class AgentControllerTests : IClassFixture<IntegrationTestFactory>
{
    private readonly HttpClient _client;

    public AgentControllerTests(IntegrationTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Chat_WithoutAuth_ReturnsUnauthorized()
    {
        var unauthClient = _client; // No auth header
        var request = new ChatRequest
        {
            Mode = AgentMode.Production,
            Message = "今天有哪些工单？"
        };

        var response = await unauthClient.PostAsJsonAsync("/api/agent/chat", request);

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_WithAuth_ReturnsStream()
    {
        // Note: Full SSE stream testing requires specialized client
        // This test verifies endpoint accepts request and returns correct content type

        var request = new ChatRequest
        {
            Mode = AgentMode.Production,
            Message = "今天有哪些工单？"
        };

        var response = await _client.PostAsJsonAsync("/api/agent/chat", request);

        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetConversations_ReturnsUserConversations()
    {
        // Create a conversation first via chat
        var chatRequest = new ChatRequest
        {
            Mode = AgentMode.Production,
            Message = "测试消息"
        };
        await _client.PostAsJsonAsync("/api/agent/chat", chatRequest);

        var response = await _client.GetAsync("/api/agent/conversations?page=1&pageSize=10");

        Assert.True(response.IsSuccessStatusCode);
        var conversations = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(conversations);
        Assert.NotEmpty(conversations);
    }
}
```

- [ ] **Step 4: Build and run manual test**

```bash
dotnet build
dotnet run --project src/MesCopilot.Api
```

Test with curl (in separate terminal):
```bash
TOKEN="<your_jwt_token>"
curl -N -X POST http://localhost:5000/api/agent/chat \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"mode":0,"message":"今天有哪些工单？","debugMode":false}'
```

Expected: See SSE events stream (data: {...})

- [ ] **Step 5: Commit**

```bash
git add src/MesCopilot.Api/Controllers/AgentController.cs src/MesCopilot.Api/Dtos/Agent/ tests/MesCopilot.IntegrationTests/Controllers/AgentControllerTests.cs
git commit -m "feat(api): add AgentController with SSE streaming chat endpoint

- POST /api/agent/chat: SSE stream with thinking/token/tool_result/done events
- OpenAI-compatible event format: data: {\"type\":\"...\",\"content\":\"...\"}\n\n
- Rule-based tool selection (keyword matching, placeholder for LLM)
- Fact verification integrated into stream
- Token-by-token explanation streaming (50ms delay per chunk)
- GET /api/agent/conversations: list user's conversations (paginated)
- GET /api/agent/conversations/{id}: get single conversation with messages
- DELETE /api/agent/conversations/{id}: delete conversation
- Integration tests for auth + streaming"
```

Expected: Changes committed

---

### Task 2C.5: Frontend Streaming Hook and Chat Interface Update

**Files:**
- Create: `web/lib/hooks/use-agent-stream.ts`
- Modify: `web/components/agent/chat-interface.tsx`
- Create: `web/lib/api/agent-client.ts`

**Interfaces:**
- Consumes: Backend `/api/agent/chat` SSE stream
- Produces: Real-time streaming chat UI

- [ ] **Step 1: Create useAgentStream hook**

Create file `web/lib/hooks/use-agent-stream.ts`:

```typescript
import { useState, useCallback } from "react";
import { useSession } from "next-auth/react";

export type SseEventType = "thinking" | "token" | "tool_result" | "done" | "error";

export interface SseEvent {
  type: SseEventType;
  content?: string;
  data?: any;
}

export interface UseAgentStreamOptions {
  onEvent?: (event: SseEvent) => void;
  onComplete?: () => void;
  onError?: (error: Error) => void;
}

export function useAgentStream(options: UseAgentStreamOptions = {}) {
  const { data: session } = useSession();
  const [isStreaming, setIsStreaming] = useState(false);
  const [abortController, setAbortController] = useState<AbortController | null>(null);

  const streamChat = useCallback(
    async (
      mode: number,
      message: string,
      conversationId?: string,
      debugMode: boolean = false
    ) => {
      if (!session?.user?.accessToken) {
        options.onError?.(new Error("Not authenticated"));
        return;
      }

      const controller = new AbortController();
      setAbortController(controller);
      setIsStreaming(true);

      try {
        const response = await fetch(
          `${process.env.NEXT_PUBLIC_API_URL}/agent/chat`,
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              Authorization: `Bearer ${session.user.accessToken}`,
            },
            body: JSON.stringify({
              conversationId,
              mode,
              message,
              debugMode,
            }),
            signal: controller.signal,
          }
        );

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`);
        }

        const reader = response.body?.getReader();
        const decoder = new TextDecoder();

        if (!reader) {
          throw new Error("No response body");
        }

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;

          const chunk = decoder.decode(value, { stream: true });
          const lines = chunk.split("\n");

          for (const line of lines) {
            if (line.startsWith("data: ")) {
              const jsonStr = line.slice(6);
              try {
                const event: SseEvent = JSON.parse(jsonStr);
                options.onEvent?.(event);

                if (event.type === "done") {
                  options.onComplete?.();
                  setIsStreaming(false);
                  return;
                }
              } catch (e) {
                console.error("Failed to parse SSE event:", e);
              }
            }
          }
        }
      } catch (error: any) {
        if (error.name !== "AbortError") {
          options.onError?.(error);
        }
      } finally {
        setIsStreaming(false);
        setAbortController(null);
      }
    },
    [session, options]
  );

  const stopStreaming = useCallback(() => {
    if (abortController) {
      abortController.abort();
      setAbortController(null);
      setIsStreaming(false);
    }
  }, [abortController]);

  return {
    streamChat,
    stopStreaming,
    isStreaming,
  };
}
```

- [ ] **Step 2: Update chat-interface.tsx to use real streaming**

Replace the mock streaming code in `web/components/agent/chat-interface.tsx`:

```typescript
// Remove the old setTimeout mock code

// Add imports
import { useAgentStream, type SseEvent } from "@/lib/hooks/use-agent-stream";

// Inside ChatInterface component, replace state and mock logic:
const { streamChat, stopStreaming, isStreaming } = useAgentStream({
  onEvent: (event: SseEvent) => {
    if (event.type === "thinking") {
      setThinkingMessage(event.content || "");
    } else if (event.type === "token") {
      setMessages((prev) => {
        const lastMsg = prev[prev.length - 1];
        if (lastMsg?.role === "assistant") {
          // Append token to existing assistant message
          return [
            ...prev.slice(0, -1),
            { ...lastMsg, content: lastMsg.content + event.content },
          ];
        } else {
          // Create new assistant message
          return [...prev, { role: "assistant", content: event.content || "" }];
        }
      });
    } else if (event.type === "tool_result") {
      setStructuredResult(event.data?.data || null);
      setDebugInfo(event.data || null);
    }
  },
  onComplete: () => {
    setThinkingMessage("");
  },
  onError: (error) => {
    console.error("Stream error:", error);
    setMessages((prev) => [
      ...prev,
      { role: "assistant", content: "抱歉，发生了错误。请稍后重试。" },
    ]);
  },
});

// Replace handleSend function:
const handleSend = async () => {
  if (!input.trim()) return;

  setMessages((prev) => [...prev, { role: "user", content: input }]);
  setInput("");
  setStructuredResult(null);
  setDebugInfo(null);

  await streamChat(
    agentModes.indexOf(selectedMode),
    input,
    undefined,
    showDebug
  );
};
```

- [ ] **Step 3: Add stop button to chat interface**

Add stop button in the chat input area (after send button):

```typescript
{isStreaming && (
  <button
    onClick={stopStreaming}
    className="ml-2 p-2 text-red-600 hover:bg-red-50 rounded-lg transition"
    title="停止生成"
  >
    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
    </svg>
  </button>
)}
```

- [ ] **Step 4: Test frontend streaming**

```bash
cd web
pnpm dev
```

1. Navigate to http://localhost:3000
2. Login with test credentials
3. Go to /agent
4. Send a message
5. Verify:
   - "thinking..." appears
   - Text streams token by token
   - Structured result appears
   - "Stop" button works

Expected: Real-time streaming works end-to-end

- [ ] **Step 5: Write useAgentStream test**

Create file `web/lib/hooks/use-agent-stream.test.ts`:

```typescript
import { renderHook, waitFor } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import { useAgentStream } from "./use-agent-stream";

vi.mock("next-auth/react", () => ({
  useSession: () => ({
    data: {
      user: { accessToken: "test-token" },
    },
  }),
}));

describe("useAgentStream", () => {
  it("initializes with correct state", () => {
    const { result } = renderHook(() => useAgentStream());

    expect(result.current.isStreaming).toBe(false);
    expect(typeof result.current.streamChat).toBe("function");
    expect(typeof result.current.stopStreaming).toBe("function");
  });

  it("sets isStreaming during stream", async () => {
    global.fetch = vi.fn(() =>
      Promise.resolve({
        ok: true,
        body: {
          getReader: () => ({
            read: () => Promise.resolve({ done: true, value: undefined }),
          }),
        },
      } as any)
    );

    const { result } = renderHook(() => useAgentStream());

    result.current.streamChat(0, "test message");

    expect(result.current.isStreaming).toBe(true);

    await waitFor(() => {
      expect(result.current.isStreaming).toBe(false);
    });
  });
});
```

- [ ] **Step 6: Run frontend tests**

```bash
cd web
pnpm test
cd ..
```

Expected: PASS - Frontend streaming tests pass

- [ ] **Step 7: Commit**

```bash
git add web/
git commit -m "feat(web): implement real SSE streaming with useAgentStream hook

- useAgentStream hook: manages SSE connection, parses events, handles abort
- Real-time token-by-token streaming (replaces mock setTimeout)
- Stop generation button with AbortController
- Event handlers: thinking, token, tool_result, done, error
- Graceful error handling and connection cleanup
- Updated chat-interface.tsx to use real streaming
- Unit tests for useAgentStream hook"
```

Expected: Changes committed

---

## Sub-Phase 2D: 3 个新 Agent 工具（5 天）

### Task 2D.1: PredictMaintenanceTool（预测维护）

**Files:**
- Create: `src/MesCopilot.Agent/Plugins/OeeAgentPlugin/Tools/PredictMaintenanceTool.cs`
- Create: `src/MesCopilot.Application/Services/IMaintenancePredictionService.cs`
- Create: `src/MesCopilot.Application/Services/MaintenancePredictionService.cs`
- Create: `src/MesCopilot.Application/Dtos/MaintenancePredictionDto.cs`
- Create: `tests/MesCopilot.UnitTests/Agent/Plugins/OeeAgentPlugin/PredictMaintenanceToolTests.cs`
- Create: `tests/MesCopilot.UnitTests/Application/Services/MaintenancePredictionServiceTests.cs`

**Interfaces:**
- Consumes: `IEquipmentService` (existing), `MesDbContext` (existing)
- Produces: `IMaintenancePredictionService`, `PredictMaintenanceTool`

- [ ] **Step 1: Create MaintenancePredictionDto**

Create file `src/MesCopilot.Application/Dtos/MaintenancePredictionDto.cs`:

```csharp
namespace MesCopilot.Application.Dtos;

public class MaintenancePredictionDto
{
    public int EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public double HealthScore { get; set; }
    public string HealthLevel { get; set; } = string.Empty;
    public double Mtbf { get; set; }
    public double Mttr { get; set; }
    public DateTime? PredictedNextFailure { get; set; }
    public string MaintenanceRecommendation { get; set; } = string.Empty;
    public List<RecentFailureDto> RecentFailures { get; set; } = new();
    public TrendDto Trend { get; set; } = new();
}

public class RecentFailureDto
{
    public DateTime OccurredAt { get; set; }
    public string AlarmCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public double DurationMinutes { get; set; }
}

public class TrendDto
{
    public string Direction { get; set; } = string.Empty;
    public double ChangePercent { get; set; }
    public string Description { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Create IMaintenancePredictionService interface**

Create file `src/MesCopilot.Application/Services/IMaintenancePredictionService.cs`:

```csharp
using MesCopilot.Application.Dtos;

namespace MesCopilot.Application.Services;

public interface IMaintenancePredictionService
{
    Task<MaintenancePredictionDto> PredictMaintenanceAsync(int equipmentId);
}
```

- [ ] **Step 3: Create MaintenancePredictionService implementation**

Create file `src/MesCopilot.Application/Services/MaintenancePredictionService.cs`:

```csharp
using MesCopilot.Application.Dtos;
using MesCopilot.Domain.Enums;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MesCopilot.Application.Services;

public class MaintenancePredictionService : IMaintenancePredictionService
{
    private readonly MesDbContext _context;

    public MaintenancePredictionService(MesDbContext context)
    {
        _context = context;
    }

    public async Task<MaintenancePredictionDto> PredictMaintenanceAsync(int equipmentId)
    {
        var equipment = await _context.Equipment
            .FirstOrDefaultAsync(e => e.Id == equipmentId)
            ?? throw new ArgumentException($"Equipment {equipmentId} not found");

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

        // Get downtime records for MTBF/MTTR calculation
        var downtimeRecords = await _context.DowntimeRecords
            .Where(d => d.EquipmentId == equipmentId && d.StartTime >= thirtyDaysAgo)
            .OrderByDescending(d => d.StartTime)
            .ToListAsync();

        // Get alarms
        var alarms = await _context.EquipmentAlarms
            .Where(a => a.EquipmentId == equipmentId && a.OccurredAt >= thirtyDaysAgo)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync();

        // Calculate MTBF (Mean Time Between Failures)
        double mtbf = 720; // default 720 hours (30 days) if no failures
        if (downtimeRecords.Count >= 2)
        {
            var intervals = new List<double>();
            for (int i = 0; i < downtimeRecords.Count - 1; i++)
            {
                var interval = (downtimeRecords[i].StartTime - downtimeRecords[i + 1].StartTime).TotalHours;
                intervals.Add(interval);
            }
            mtbf = intervals.Average();
        }
        else if (downtimeRecords.Count == 1)
        {
            mtbf = (DateTime.UtcNow - downtimeRecords[0].StartTime).TotalHours;
        }

        // Calculate MTTR (Mean Time To Repair)
        double mttr = 0;
        if (downtimeRecords.Count > 0)
        {
            var repairTimes = downtimeRecords
                .Where(d => d.EndTime.HasValue)
                .Select(d => (d.EndTime!.Value - d.StartTime).TotalHours)
                .ToList();
            mttr = repairTimes.Count > 0 ? repairTimes.Average() : 1.0;
        }

        // Calculate health score (0-100)
        double healthScore = CalculateHealthScore(mtbf, mttr, alarms.Count, downtimeRecords.Count);

        // Predict next failure using simple moving average
        DateTime? predictedNextFailure = null;
        if (downtimeRecords.Count >= 2 && mtbf > 0)
        {
            var lastFailure = downtimeRecords.First().StartTime;
            predictedNextFailure = lastFailure.AddHours(mtbf);
            if (predictedNextFailure < DateTime.UtcNow)
            {
                predictedNextFailure = DateTime.UtcNow.AddHours(mtbf * 0.1);
            }
        }

        // Determine trend
        var trend = CalculateTrend(downtimeRecords);

        // Generate recommendation
        var recommendation = GenerateRecommendation(healthScore, mtbf, predictedNextFailure, trend);

        return new MaintenancePredictionDto
        {
            EquipmentId = equipment.Id,
            EquipmentCode = equipment.Code,
            EquipmentName = equipment.Name,
            HealthScore = Math.Round(healthScore, 1),
            HealthLevel = healthScore >= 80 ? "良好" : healthScore >= 60 ? "注意" : healthScore >= 40 ? "警告" : "危险",
            Mtbf = Math.Round(mtbf, 1),
            Mttr = Math.Round(mttr, 1),
            PredictedNextFailure = predictedNextFailure,
            MaintenanceRecommendation = recommendation,
            RecentFailures = alarms.Take(5).Select(a => new RecentFailureDto
            {
                OccurredAt = a.OccurredAt,
                AlarmCode = a.AlarmCode,
                Message = a.Message,
                DurationMinutes = downtimeRecords
                    .Where(d => Math.Abs((d.StartTime - a.OccurredAt).TotalMinutes) < 5)
                    .Select(d => d.EndTime.HasValue ? (d.EndTime.Value - d.StartTime).TotalMinutes : 0)
                    .FirstOrDefault()
            }).ToList(),
            Trend = trend
        };
    }

    private static double CalculateHealthScore(double mtbf, double mttr, int alarmCount, int downtimeCount)
    {
        double score = 100;

        // Penalize low MTBF (frequent failures)
        if (mtbf < 24) score -= 40;
        else if (mtbf < 72) score -= 25;
        else if (mtbf < 168) score -= 10;

        // Penalize high MTTR (slow repairs)
        if (mttr > 8) score -= 20;
        else if (mttr > 4) score -= 10;
        else if (mttr > 2) score -= 5;

        // Penalize alarm frequency
        score -= Math.Min(alarmCount * 2, 20);

        // Penalize downtime frequency
        score -= Math.Min(downtimeCount * 3, 15);

        return Math.Max(0, Math.Min(100, score));
    }

    private static TrendDto CalculateTrend(List<Domain.Entities.Equipment.DowntimeRecord> records)
    {
        if (records.Count < 4)
        {
            return new TrendDto { Direction = "stable", ChangePercent = 0, Description = "数据不足，无法判断趋势" };
        }

        var half = records.Count / 2;
        var recentHalf = records.Take(half).ToList();
        var olderHalf = records.Skip(half).ToList();

        var recentFreq = recentHalf.Count / Math.Max(1, (DateTime.UtcNow - recentHalf.Last().StartTime).TotalDays);
        var olderFreq = olderHalf.Count / Math.Max(1, (recentHalf.Last().StartTime - olderHalf.Last().StartTime).TotalDays);

        var changePercent = olderFreq > 0 ? ((recentFreq - olderFreq) / olderFreq) * 100 : 0;

        if (changePercent > 20)
            return new TrendDto { Direction = "deteriorating", ChangePercent = Math.Round(changePercent, 1), Description = "故障频率上升，建议尽快安排维护" };
        if (changePercent < -20)
            return new TrendDto { Direction = "improving", ChangePercent = Math.Round(changePercent, 1), Description = "设备状态改善中" };

        return new TrendDto { Direction = "stable", ChangePercent = Math.Round(changePercent, 1), Description = "设备状态稳定" };
    }

    private static string GenerateRecommendation(double healthScore, double mtbf, DateTime? predictedFailure, TrendDto trend)
    {
        if (healthScore < 40)
            return "设备健康状态危险，建议立即安排停机检修。重点检查近期高频报警组件。";
        if (healthScore < 60)
            return $"设备需要关注。MTBF={mtbf:F0}小时，建议在{predictedFailure?.ToString("yyyy-MM-dd") ?? "近期"}前安排预防性维护。";
        if (trend.Direction == "deteriorating")
            return "虽然当前健康评分尚可，但趋势恶化。建议增加巡检频率，关注异常振动和温度变化。";

        return $"设备状态良好。当前 MTBF={mtbf:F0}小时，建议按常规计划维护即可。";
    }
}
```

- [ ] **Step 4: Create PredictMaintenanceTool**

Create file `src/MesCopilot.Agent/Plugins/OeeAgentPlugin/Tools/PredictMaintenanceTool.cs`:

```csharp
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;

public class PredictMaintenanceTool
{
    public string Name => "predict_maintenance";
    public string Description => "预测设备维护需求，计算 MTBF/MTTR，评估设备健康状态并给出维护建议";

    private readonly IMaintenancePredictionService _predictionService;

    public PredictMaintenanceTool(IMaintenancePredictionService predictionService)
    {
        _predictionService = predictionService;
    }

    public async Task<FunctionCallResult> ExecuteAsync(int equipmentId, bool debugMode = false)
    {
        var prediction = await _predictionService.PredictMaintenanceAsync(equipmentId);

        var explanation = $"设备 {prediction.EquipmentCode}（{prediction.EquipmentName}）健康评分：{prediction.HealthScore}/100（{prediction.HealthLevel}）。" +
                         $"MTBF={prediction.Mtbf}小时，MTTR={prediction.Mttr}小时。" +
                         (prediction.PredictedNextFailure.HasValue ? $"预测下次故障时间：{prediction.PredictedNextFailure:yyyy-MM-dd HH:mm}。" : "") +
                         $"建议：{prediction.MaintenanceRecommendation}";

        return new FunctionCallResult
        {
            Data = prediction,
            Explanation = explanation,
            Debug = debugMode ? new
            {
                algorithm = "Moving Average + Threshold",
                dataWindow = "30 days",
                downtimeRecordCount = prediction.RecentFailures.Count,
                trend = prediction.Trend
            } : null
        };
    }
}
```

- [ ] **Step 5: Write unit tests for MaintenancePredictionService**

Create file `tests/MesCopilot.UnitTests/Application/Services/MaintenancePredictionServiceTests.cs`:

```csharp
using MesCopilot.Application.Services;
using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MesCopilot.UnitTests.Application.Services;

public class MaintenancePredictionServiceTests
{
    private MesDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new MesDbContext(options);
    }

    [Fact]
    public async Task PredictMaintenanceAsync_WithNoDowntime_ReturnsHighHealthScore()
    {
        // Arrange
        var context = CreateContext();
        context.Equipment.Add(new Domain.Entities.Equipment.Equipment
        {
            Id = 1, Code = "EQ-001", Name = "CNC机床1", IsActive = true
        });
        await context.SaveChangesAsync();

        var service = new MaintenancePredictionService(context);

        // Act
        var result = await service.PredictMaintenanceAsync(1);

        // Assert
        Assert.True(result.HealthScore >= 80);
        Assert.Equal("良好", result.HealthLevel);
        Assert.Equal(720, result.Mtbf);
    }

    [Fact]
    public async Task PredictMaintenanceAsync_WithFrequentDowntime_ReturnsLowHealthScore()
    {
        // Arrange
        var context = CreateContext();
        context.Equipment.Add(new Domain.Entities.Equipment.Equipment
        {
            Id = 1, Code = "EQ-001", Name = "CNC机床1", IsActive = true
        });

        // Add frequent downtime records (every 12 hours)
        for (int i = 0; i < 10; i++)
        {
            context.DowntimeRecords.Add(new DowntimeRecord
            {
                Id = i + 1,
                EquipmentId = 1,
                StartTime = DateTime.UtcNow.AddHours(-i * 12),
                EndTime = DateTime.UtcNow.AddHours(-i * 12 + 2),
                Reason = "故障"
            });
        }
        await context.SaveChangesAsync();

        var service = new MaintenancePredictionService(context);

        // Act
        var result = await service.PredictMaintenanceAsync(1);

        // Assert
        Assert.True(result.HealthScore < 60);
        Assert.True(result.Mtbf < 24);
    }

    [Fact]
    public async Task PredictMaintenanceAsync_EquipmentNotFound_ThrowsArgumentException()
    {
        // Arrange
        var context = CreateContext();
        var service = new MaintenancePredictionService(context);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => service.PredictMaintenanceAsync(999));
    }
}
```

- [ ] **Step 6: Write unit tests for PredictMaintenanceTool**

Create file `tests/MesCopilot.UnitTests/Agent/Plugins/OeeAgentPlugin/PredictMaintenanceToolTests.cs`:

```csharp
using MesCopilot.Agent.Plugins.OeeAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using Moq;
using Xunit;

namespace MesCopilot.UnitTests.Agent.Plugins.OeeAgentPlugin;

public class PredictMaintenanceToolTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsStructuredPrediction()
    {
        // Arrange
        var mockService = new Mock<IMaintenancePredictionService>();
        mockService.Setup(s => s.PredictMaintenanceAsync(1))
            .ReturnsAsync(new MaintenancePredictionDto
            {
                EquipmentId = 1,
                EquipmentCode = "EQ-001",
                EquipmentName = "CNC机床1",
                HealthScore = 75.5,
                HealthLevel = "注意",
                Mtbf = 48.0,
                Mttr = 2.5,
                PredictedNextFailure = DateTime.UtcNow.AddDays(2),
                MaintenanceRecommendation = "建议在近期安排预防性维护"
            });

        var tool = new PredictMaintenanceTool(mockService.Object);

        // Act
        var result = await tool.ExecuteAsync(1);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Contains("EQ-001", result.Explanation);
        Assert.Contains("75.5", result.Explanation);
        Assert.Null(result.Debug);
    }

    [Fact]
    public async Task ExecuteAsync_DebugMode_IncludesDebugInfo()
    {
        // Arrange
        var mockService = new Mock<IMaintenancePredictionService>();
        mockService.Setup(s => s.PredictMaintenanceAsync(1))
            .ReturnsAsync(new MaintenancePredictionDto
            {
                EquipmentId = 1, EquipmentCode = "EQ-001", EquipmentName = "Test",
                HealthScore = 90, HealthLevel = "良好", Mtbf = 200, Mttr = 1
            });

        var tool = new PredictMaintenanceTool(mockService.Object);

        // Act
        var result = await tool.ExecuteAsync(1, debugMode: true);

        // Assert
        Assert.NotNull(result.Debug);
    }
}
```

- [ ] **Step 7: Register service in DI**

Modify `src/MesCopilot.Application/DependencyInjection.cs`, add line:

```csharp
services.AddScoped<IMaintenancePredictionService, MaintenancePredictionService>();
```

- [ ] **Step 8: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests --filter "FullyQualifiedName~PredictMaintenance"
```

Expected: All tests pass

- [ ] **Step 9: Commit**

```bash
git add src/MesCopilot.Application/Dtos/MaintenancePredictionDto.cs
git add src/MesCopilot.Application/Services/IMaintenancePredictionService.cs
git add src/MesCopilot.Application/Services/MaintenancePredictionService.cs
git add src/MesCopilot.Agent/Plugins/OeeAgentPlugin/Tools/PredictMaintenanceTool.cs
git add tests/MesCopilot.UnitTests/
git commit -m "feat(agent): add PredictMaintenanceTool with MTBF/MTTR analysis

- MaintenancePredictionService: calculate health score from downtime/alarm data
- Moving average trend detection (30-day window)
- Health levels: 良好/注意/警告/危险
- Predicted next failure time based on MTBF
- Auto-generated maintenance recommendations
- Unit tests for service and tool"
```

Expected: Changes committed

---

### Task 2D.2: Implement SuggestScheduleTool

**Files:**
- Create: `src/MesCopilot.Agent/Plugins/ProductionAgentPlugin/Tools/SuggestScheduleTool.cs`
- Modify: `src/MesCopilot.Agent/Plugins/ProductionAgentPlugin/ProductionAgentPlugin.cs`
- Create: `tests/MesCopilot.UnitTests/Agent/Plugins/ProductionAgentPlugin/SuggestScheduleToolTests.cs`

**Interfaces:**
- Consumes: IWorkOrderService, IEquipmentService
- Produces: Optimized work order schedule (EDF algorithm)

**Reference:** Classic EDF (Earliest Deadline First) scheduling — see Liu & Layland 1973, widely used in real-time OS and MES scheduling.

- [ ] **Step 1: Create SuggestScheduleTool**

Create file `src/MesCopilot.Agent/Plugins/ProductionAgentPlugin/Tools/SuggestScheduleTool.cs`:

```csharp
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;

public class SuggestScheduleTool
{
    public string Name => "suggest_schedule";
    public string Description => "优化生产排程，基于 EDF（最早截止时间优先）算法给出工单建议顺序和瓶颈识别";

    private readonly IWorkOrderService _workOrderService;
    private readonly IEquipmentService _equipmentService;

    public SuggestScheduleTool(IWorkOrderService workOrderService, IEquipmentService equipmentService)
    {
        _workOrderService = workOrderService;
        _equipmentService = equipmentService;
    }

    public async Task<FunctionCallResult> ExecuteAsync(int? productionLineId, DateTime? startDate, DateTime? endDate, bool debugMode = false)
    {
        startDate ??= DateTime.UtcNow;
        endDate ??= DateTime.UtcNow.AddDays(7);

        // Get pending work orders
        var pendingOrders = await _workOrderService.GetWorkOrdersAsync(
            status: WorkOrderStatus.Pending,
            lineId: productionLineId,
            from: startDate.Value,
            to: endDate.Value
        );

        if (pendingOrders.Count == 0)
        {
            return new FunctionCallResult
            {
                Data = new { message = "No pending work orders found" },
                Explanation = "指定范围内无待排程工单"
            };
        }

        // Get equipment availability
        var equipmentList = productionLineId.HasValue
            ? await _equipmentService.GetEquipmentByLineAsync(productionLineId.Value)
            : await _equipmentService.GetAllEquipmentAsync();

        var availableEquipment = equipmentList.Where(e => e.IsActive).ToList();

        // Apply EDF scheduling algorithm
        var scheduledOrders = ApplyEdfScheduling(pendingOrders, availableEquipment, startDate.Value);

        // Identify bottlenecks
        var bottlenecks = IdentifyBottlenecks(scheduledOrders, availableEquipment);

        var data = new
        {
            totalOrders = pendingOrders.Count,
            scheduledOrders = scheduledOrders.Select(s => new
            {
                workOrderId = s.WorkOrderId,
                workOrderCode = s.WorkOrderCode,
                productName = s.ProductName,
                suggestedStart = s.SuggestedStart,
                suggestedEnd = s.SuggestedEnd,
                priority = s.Priority,
                equipmentCode = s.EquipmentCode
            }),
            bottlenecks = bottlenecks.Select(b => new
            {
                equipmentCode = b.EquipmentCode,
                utilizationPercent = b.UtilizationPercent,
                queuedOrdersCount = b.QueuedOrdersCount
            }),
            estimatedCompletionDate = scheduledOrders.Any() ? scheduledOrders.Max(s => s.SuggestedEnd) : (DateTime?)null
        };

        var explanation = $"共 {pendingOrders.Count} 个待排程工单。" +
                         $"基于 EDF 算法优化后，预计完成时间：{data.estimatedCompletionDate:yyyy-MM-dd HH:mm}。" +
                         (bottlenecks.Any() ? $"识别到 {bottlenecks.Count} 个瓶颈设备：{string.Join("，", bottlenecks.Select(b => b.EquipmentCode))}。" : "无明显瓶颈。");

        return new FunctionCallResult
        {
            Data = data,
            Explanation = explanation,
            Debug = debugMode ? new { algorithm = "EDF (Earliest Deadline First)" } : null
        };
    }

    private List<ScheduledOrder> ApplyEdfScheduling(List<WorkOrderDto> orders, List<EquipmentDto> equipment, DateTime startTime)
    {
        // Sort by due date (EDF)
        var sortedOrders = orders.OrderBy(o => o.DueDate).ToList();
        var scheduled = new List<ScheduledOrder>();
        var equipmentSchedule = equipment.ToDictionary(e => e.Id, e => startTime);

        foreach (var order in sortedOrders)
        {
            // Simple heuristic: assign to first available equipment
            var assignedEquipment = equipment.FirstOrDefault(e => e.IsActive) ?? equipment.First();
            var earliestStart = equipmentSchedule[assignedEquipment.Id];
            var estimatedDuration = order.PlannedQuantity * 0.5; // Assume 0.5 hours per unit (placeholder)

            scheduled.Add(new ScheduledOrder
            {
                WorkOrderId = order.Id,
                WorkOrderCode = order.Code,
                ProductName = order.ProductName,
                SuggestedStart = earliestStart,
                SuggestedEnd = earliestStart.AddHours(estimatedDuration),
                Priority = (order.DueDate - earliestStart).TotalDays < 2 ? "高" : "中",
                EquipmentCode = assignedEquipment.Code
            });

            equipmentSchedule[assignedEquipment.Id] = earliestStart.AddHours(estimatedDuration);
        }

        return scheduled;
    }

    private List<Bottleneck> IdentifyBottlenecks(List<ScheduledOrder> scheduled, List<EquipmentDto> equipment)
    {
        var utilizationByEquipment = scheduled
            .GroupBy(s => s.EquipmentCode)
            .Select(g => new Bottleneck
            {
                EquipmentCode = g.Key,
                QueuedOrdersCount = g.Count(),
                UtilizationPercent = Math.Round(g.Sum(s => (s.SuggestedEnd - s.SuggestedStart).TotalHours) / 168 * 100, 1) // 7 days = 168 hours
            })
            .Where(b => b.UtilizationPercent > 80)
            .OrderByDescending(b => b.UtilizationPercent)
            .ToList();

        return utilizationByEquipment;
    }

    private class ScheduledOrder
    {
        public int WorkOrderId { get; set; }
        public string WorkOrderCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public DateTime SuggestedStart { get; set; }
        public DateTime SuggestedEnd { get; set; }
        public string Priority { get; set; } = string.Empty;
        public string EquipmentCode { get; set; } = string.Empty;
    }

    private class Bottleneck
    {
        public string EquipmentCode { get; set; } = string.Empty;
        public double UtilizationPercent { get; set; }
        public int QueuedOrdersCount { get; set; }
    }
}
```

- [ ] **Step 2: Register tool in ProductionAgentPlugin**

Modify `src/MesCopilot.Agent/Plugins/ProductionAgentPlugin/ProductionAgentPlugin.cs`:

```csharp
private readonly SuggestScheduleTool _suggestScheduleTool;

public ProductionAgentPlugin(..., IWorkOrderService workOrderService, IEquipmentService equipmentService)
{
    // existing
    _suggestScheduleTool = new SuggestScheduleTool(workOrderService, equipmentService);
}

public IEnumerable<object> GetTools()
{
    return new object[]
    {
        _getWorkOrderTool,
        _listWorkOrdersTool,
        _searchProductsTool,
        _suggestScheduleTool // new
    };
}
```

- [ ] **Step 3: Write tests**

Create file `tests/MesCopilot.UnitTests/Agent/Plugins/ProductionAgentPlugin/SuggestScheduleToolTests.cs`:

```csharp
using MesCopilot.Agent.Plugins.ProductionAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using MesCopilot.Domain.Enums;
using Moq;
using Xunit;

namespace MesCopilot.UnitTests.Agent.Plugins.ProductionAgentPlugin;

public class SuggestScheduleToolTests
{
    [Fact]
    public async Task Execute_WithPendingOrders_ReturnsOptimizedSchedule()
    {
        var mockWorkOrderService = new Mock<IWorkOrderService>();
        var mockEquipmentService = new Mock<IEquipmentService>();

        mockWorkOrderService.Setup(s => s.GetWorkOrdersAsync(
                It.IsAny<WorkOrderStatus?>(),
                It.IsAny<int?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<WorkOrderDto>
            {
                new() { Id = 1, Code = "WO-001", ProductName = "产品A", DueDate = DateTime.UtcNow.AddDays(1), PlannedQuantity = 100 },
                new() { Id = 2, Code = "WO-002", ProductName = "产品B", DueDate = DateTime.UtcNow.AddDays(3), PlannedQuantity = 200 }
            });

        mockEquipmentService.Setup(s => s.GetAllEquipmentAsync())
            .ReturnsAsync(new List<EquipmentDto>
            {
                new() { Id = 1, Code = "EQ-001", IsActive = true }
            });

        var tool = new SuggestScheduleTool(mockWorkOrderService.Object, mockEquipmentService.Object);

        var result = await tool.ExecuteAsync(null, null, null, false);

        Assert.NotNull(result.Data);
        dynamic data = result.Data;
        Assert.Equal(2, data.totalOrders);
        Assert.Contains("EDF", result.Explanation);
    }

    [Fact]
    public async Task Execute_WithNoOrders_ReturnsEmptyMessage()
    {
        var mockWorkOrderService = new Mock<IWorkOrderService>();
        var mockEquipmentService = new Mock<IEquipmentService>();

        mockWorkOrderService.Setup(s => s.GetWorkOrdersAsync(
                It.IsAny<WorkOrderStatus?>(),
                It.IsAny<int?>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .ReturnsAsync(new List<WorkOrderDto>());

        var tool = new SuggestScheduleTool(mockWorkOrderService.Object, mockEquipmentService.Object);

        var result = await tool.ExecuteAsync(null, null, null, false);

        Assert.Contains("无待排程工单", result.Explanation);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests --filter "FullyQualifiedName~SuggestScheduleToolTests"
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MesCopilot.Agent/Plugins/ProductionAgentPlugin/Tools/SuggestScheduleTool.cs tests/
git commit -m "feat(agent): add SuggestScheduleTool for production agent

- EDF (Earliest Deadline First) scheduling algorithm
- Assigns work orders to available equipment
- Identifies bottlenecks (>80% utilization)
- Returns suggested start/end times and priority levels
- Unit tests for scheduling logic and edge cases"
```

Expected: Changes committed

---

### Task 2D.3: Implement FiveWhyAnalysisTool

**Files:**
- Create: `src/MesCopilot.Agent/Plugins/QualityAgentPlugin/Tools/FiveWhyAnalysisTool.cs`
- Modify: `src/MesCopilot.Agent/Plugins/QualityAgentPlugin/QualityAgentPlugin.cs`
- Create: `tests/MesCopilot.UnitTests/Agent/Plugins/QualityAgentPlugin/FiveWhyAnalysisToolTests.cs`

**Interfaces:**
- Consumes: IQualityService, IEquipmentService, IWorkOrderService
- Produces: 5-Why causal chain with evidence

- [ ] **Step 1: Create FiveWhyAnalysisTool**

Create file `src/MesCopilot.Agent/Plugins/QualityAgentPlugin/Tools/FiveWhyAnalysisTool.cs`:

```csharp
using MesCopilot.Agent.Models;
using MesCopilot.Application.Services;

namespace MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;

public class FiveWhyAnalysisTool
{
    public string Name => "five_why_analysis";
    public string Description => "对质量缺陷进行 5-Why 根因分析，构建因果链并给出纠正措施建议";

    private readonly IQualityService _qualityService;
    private readonly IEquipmentService _equipmentService;
    private readonly IWorkOrderService _workOrderService;

    public FiveWhyAnalysisTool(
        IQualityService qualityService,
        IEquipmentService equipmentService,
        IWorkOrderService workOrderService)
    {
        _qualityService = qualityService;
        _equipmentService = equipmentService;
        _workOrderService = workOrderService;
    }

    public async Task<FunctionCallResult> ExecuteAsync(int? defectRecordId, string? symptomDescription, bool debugMode = false)
    {
        if (!defectRecordId.HasValue && string.IsNullOrWhiteSpace(symptomDescription))
        {
            throw new ArgumentException("Must provide either defectRecordId or symptomDescription");
        }

        var whyChain = new List<WhyLevel>();

        // Level 1: Symptom (observable defect)
        if (defectRecordId.HasValue)
        {
            var defect = await _qualityService.GetDefectRecordAsync(defectRecordId.Value);
            whyChain.Add(new WhyLevel
            {
                Level = 1,
                Question = "为什么出现缺陷？",
                Answer = $"{defect.DefectType}：{defect.Description}",
                Evidence = $"缺陷记录 ID: {defect.Id}, 发现时间: {defect.DiscoveredAt:yyyy-MM-dd HH:mm}"
            });

            // Level 2: Process deviation
            var inspection = await _qualityService.GetInspectionByDefectAsync(defectRecordId.Value);
            if (inspection != null)
            {
                whyChain.Add(new WhyLevel
                {
                    Level = 2,
                    Question = "为什么工序出现偏差？",
                    Answer = $"检验点 {inspection.InspectionPoint} 发现 {inspection.MeasuredValue} 超出规格（标准：{inspection.SpecValue}）",
                    Evidence = $"质检记录 ID: {inspection.Id}, 检验员: {inspection.InspectorName}"
                });

                // Level 3: Equipment/Material issue
                var workOrder = await _workOrderService.GetWorkOrderAsync(inspection.WorkOrderId);
                if (workOrder != null && workOrder.EquipmentId.HasValue)
                {
                    var equipment = await _equipmentService.GetEquipmentAsync(workOrder.EquipmentId.Value);
                    var recentAlarms = await _equipmentService.GetRecentAlarmsAsync(workOrder.EquipmentId.Value, hours: 24);

                    if (recentAlarms.Any())
                    {
                        whyChain.Add(new WhyLevel
                        {
                            Level = 3,
                            Question = "为什么设备出现异常？",
                            Answer = $"设备 {equipment.Code} 在 24 小时内触发 {recentAlarms.Count} 次报警",
                            Evidence = $"最近报警: {string.Join(", ", recentAlarms.Take(3).Select(a => a.AlarmCode))}"
                        });

                        // Level 4: Maintenance gap
                        var lastMaintenance = await _equipmentService.GetLastMaintenanceAsync(workOrder.EquipmentId.Value);
                        if (lastMaintenance != null)
                        {
                            var daysSinceMaintenance = (DateTime.UtcNow - lastMaintenance.CompletedAt).TotalDays;
                            whyChain.Add(new WhyLevel
                            {
                                Level = 4,
                                Question = "为什么设备未能及时维护？",
                                Answer = daysSinceMaintenance > 30
                                    ? $"距上次保养已 {daysSinceMaintenance:F0} 天，超出建议周期"
                                    : "设备保养周期正常，可能为部件老化",
                                Evidence = $"上次保养: {lastMaintenance.CompletedAt:yyyy-MM-dd}"
                            });
                        }

                        // Level 5: Root cause (management/process)
                        whyChain.Add(new WhyLevel
                        {
                            Level = 5,
                            Question = "为什么预防性维护计划未能执行？",
                            Answer = "可能原因：维护计划不完善、资源不足、或缺乏状态监控",
                            Evidence = "需要审查 TPM（全面生产维护）体系执行情况"
                        });
                    }
                    else
                    {
                        // No equipment alarms, likely material or operator issue
                        whyChain.Add(new WhyLevel
                        {
                            Level = 3,
                            Question = "为什么工艺参数偏离？",
                            Answer = "设备状态正常，疑似原料批次差异或操作不当",
                            Evidence = $"工单 {workOrder.Code}, 批次: {workOrder.BatchNumber}"
                        });
                    }
                }
            }
        }
        else
        {
            // Symptom description provided without defect record
            whyChain.Add(new WhyLevel
            {
                Level = 1,
                Question = "为什么出现该问题？",
                Answer = symptomDescription!,
                Evidence = "用户描述，无关联记录"
            });
        }

        // Generate corrective actions
        var correctiveActions = GenerateCorrectiveActions(whyChain);

        var data = new
        {
            defectRecordId,
            whyChain = whyChain.Select(w => new
            {
                level = w.Level,
                question = w.Question,
                answer = w.Answer,
                evidence = w.Evidence
            }),
            rootCause = whyChain.LastOrDefault()?.Answer ?? "未能识别根本原因",
            correctiveActions
        };

        var explanation = $"5-Why 分析完成，共 {whyChain.Count} 层。" +
                         $"根本原因：{data.rootCause}。" +
                         $"建议纠正措施：{string.Join("；", correctiveActions.Take(2))}。";

        return new FunctionCallResult
        {
            Data = data,
            Explanation = explanation,
            Debug = debugMode ? new { chainDepth = whyChain.Count } : null
        };
    }

    private List<string> GenerateCorrectiveActions(List<WhyLevel> chain)
    {
        var actions = new List<string>();

        // Tailor actions based on deepest cause identified
        if (chain.Any(w => w.Answer.Contains("维护计划") || w.Answer.Contains("保养")))
        {
            actions.Add("完善 TPM 预防性维护计划，设置自动提醒");
            actions.Add("增加关键设备的状态监控频率");
        }

        if (chain.Any(w => w.Answer.Contains("报警") || w.Answer.Contains("设备异常")))
        {
            actions.Add("检查并更换设备磨损部件");
            actions.Add("校准设备传感器和控制系统");
        }

        if (chain.Any(w => w.Answer.Contains("原料") || w.Answer.Contains("批次")))
        {
            actions.Add("加强来料检验，建立供应商评估机制");
            actions.Add("记录批次追溯信息，建立质量档案");
        }

        if (chain.Any(w => w.Answer.Contains("操作") || w.Answer.Contains("工艺参数")))
        {
            actions.Add("对操作员进行 SOP 再培训");
            actions.Add("实施首件检验和过程巡检");
        }

        if (actions.Count == 0)
        {
            actions.Add("需进一步调查，收集更多数据");
        }

        return actions;
    }

    private class WhyLevel
    {
        public int Level { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
    }
}
```

- [ ] **Step 2: Register tool in QualityAgentPlugin**

Modify `src/MesCopilot.Agent/Plugins/QualityAgentPlugin/QualityAgentPlugin.cs`:

```csharp
private readonly FiveWhyAnalysisTool _fiveWhyAnalysisTool;

public QualityAgentPlugin(..., IEquipmentService equipmentService, IWorkOrderService workOrderService)
{
    // existing
    _fiveWhyAnalysisTool = new FiveWhyAnalysisTool(_qualityService, equipmentService, workOrderService);
}

public IEnumerable<object> GetTools()
{
    return new object[]
    {
        _getDefectsTool,
        _analyzeDefectTrendsTool,
        _traceDefectSourceTool,
        _fiveWhyAnalysisTool // new
    };
}
```

- [ ] **Step 3: Write tests**

Create file `tests/MesCopilot.UnitTests/Agent/Plugins/QualityAgentPlugin/FiveWhyAnalysisToolTests.cs`:

```csharp
using MesCopilot.Agent.Plugins.QualityAgentPlugin.Tools;
using MesCopilot.Application.Dtos;
using MesCopilot.Application.Services;
using Moq;
using Xunit;

namespace MesCopilot.UnitTests.Agent.Plugins.QualityAgentPlugin;

public class FiveWhyAnalysisToolTests
{
    [Fact]
    public async Task Execute_WithDefectId_ReturnsWhyChain()
    {
        var mockQuality = new Mock<IQualityService>();
        var mockEquipment = new Mock<IEquipmentService>();
        var mockWorkOrder = new Mock<IWorkOrderService>();

        mockQuality.Setup(s => s.GetDefectRecordAsync(1))
            .ReturnsAsync(new DefectRecordDto
            {
                Id = 1,
                DefectType = "尺寸偏差",
                Description = "直径超差 0.5mm",
                DiscoveredAt = DateTime.UtcNow
            });

        mockQuality.Setup(s => s.GetInspectionByDefectAsync(1))
            .ReturnsAsync(new QualityInspectionDto
            {
                Id = 10,
                InspectionPoint = "精加工后检验",
                MeasuredValue = "50.5mm",
                SpecValue = "50±0.2mm",
                WorkOrderId = 100
            });

        mockWorkOrder.Setup(s => s.GetWorkOrderAsync(100))
            .ReturnsAsync(new WorkOrderDto { Id = 100, Code = "WO-100", EquipmentId = 5 });

        mockEquipment.Setup(s => s.GetEquipmentAsync(5))
            .ReturnsAsync(new EquipmentDto { Id = 5, Code = "EQ-005" });

        mockEquipment.Setup(s => s.GetRecentAlarmsAsync(5, 24))
            .ReturnsAsync(new List<EquipmentAlarmDto>
            {
                new() { AlarmCode = "TEMP_HIGH", Message = "温度过高" }
            });

        var tool = new FiveWhyAnalysisTool(mockQuality.Object, mockEquipment.Object, mockWorkOrder.Object);

        var result = await tool.ExecuteAsync(defectRecordId: 1, symptomDescription: null, debugMode: false);

        Assert.NotNull(result.Data);
        dynamic data = result.Data;
        Assert.True(data.whyChain.Count >= 3);
        Assert.Contains("根本原因", result.Explanation);
    }

    [Fact]
    public async Task Execute_WithSymptomOnly_ReturnsBasicAnalysis()
    {
        var mockQuality = new Mock<IQualityService>();
        var mockEquipment = new Mock<IEquipmentService>();
        var mockWorkOrder = new Mock<IWorkOrderService>();

        var tool = new FiveWhyAnalysisTool(mockQuality.Object, mockEquipment.Object, mockWorkOrder.Object);

        var result = await tool.ExecuteAsync(defectRecordId: null, symptomDescription: "产品表面有划痕", debugMode: false);

        Assert.NotNull(result.Data);
        dynamic data = result.Data;
        Assert.Single(data.whyChain);
    }
}
```

- [ ] **Step 4: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests --filter "FullyQualifiedName~FiveWhyAnalysisToolTests"
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/MesCopilot.Agent/Plugins/QualityAgentPlugin/Tools/FiveWhyAnalysisTool.cs tests/
git commit -m "feat(agent): add FiveWhyAnalysisTool for quality agent

- Constructs 5-Why causal chain from defect → inspection → equipment → maintenance
- Generates evidence-backed answers for each level
- Suggests corrective actions based on root cause category
- Supports defectRecordId or symptomDescription input modes
- Unit tests for complete chain and symptom-only scenarios"
```

Expected: Changes committed

---

## Sub-Phase 2E: Report Export (Excel + PDF)

### Task 2E.1: Add Report Export NuGet Packages and Service Interface

**Files:**
- Modify: `src/MesCopilot.Application/MesCopilot.Application.csproj`
- Create: `src/MesCopilot.Application/Services/IReportExportService.cs`
- Create: `src/MesCopilot.Application/DTOs/Reports/ProductionReportDto.cs`
- Create: `src/MesCopilot.Application/DTOs/Reports/QualityReportDto.cs`
- Create: `src/MesCopilot.Application/DTOs/Reports/OeeReportDto.cs`

**Interfaces:**
- Consumes: IWorkOrderService, IQualityService, IEquipmentService
- Produces: IReportExportService interface, Report DTOs

- [ ] **Step 1: Add NuGet packages**

```bash
cd src/MesCopilot.Application
dotnet add package QuestPDF --version 2024.3.0
dotnet add package ClosedXML --version 0.102.2
```

Expected: Packages added to .csproj

- [ ] **Step 2: Create Report DTOs**

Create file `src/MesCopilot.Application/DTOs/Reports/ProductionReportDto.cs`:

```csharp
namespace MesCopilot.Application.DTOs.Reports;

public class ProductionReportDto
{
    public DateTime ReportDate { get; set; }
    public int? ProductionLineId { get; set; }
    public string ProductionLineName { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int DelayedOrders { get; set; }
    public double CompletionRate { get; set; }
    public int TotalPlannedQuantity { get; set; }
    public int TotalActualQuantity { get; set; }
    public double OutputRate { get; set; }
    public List<ProductionReportLineItem> LineItems { get; set; } = new();
}

public class ProductionReportLineItem
{
    public string WorkOrderCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int PlannedQuantity { get; set; }
    public int ActualQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public bool IsDelayed { get; set; }
}
```


Create file `src/MesCopilot.Application/DTOs/Reports/QualityReportDto.cs`:

```csharp
namespace MesCopilot.Application.DTOs.Reports;

public class QualityReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalInspections { get; set; }
    public int PassedInspections { get; set; }
    public int FailedInspections { get; set; }
    public double PassRate { get; set; }
    public int TotalDefects { get; set; }
    public List<QualityReportLineItem> LineItems { get; set; } = new();
    public List<DefectSummaryItem> DefectSummary { get; set; } = new();
}

public class QualityReportLineItem
{
    public string InspectionCode { get; set; } = string.Empty;
    public string WorkOrderCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime InspectionDate { get; set; }
    public string Result { get; set; } = string.Empty;
    public int DefectCount { get; set; }
}

public class DefectSummaryItem
{
    public string DefectType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}
```

Create file `src/MesCopilot.Application/DTOs/Reports/OeeReportDto.cs`:

```csharp
namespace MesCopilot.Application.DTOs.Reports;

public class OeeReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<int> EquipmentIds { get; set; } = new();
    public double AverageOee { get; set; }
    public double AverageAvailability { get; set; }
    public double AveragePerformance { get; set; }
    public double AverageQuality { get; set; }
    public List<OeeReportLineItem> LineItems { get; set; } = new();
}

public class OeeReportLineItem
{
    public int EquipmentId { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double Availability { get; set; }
    public double Performance { get; set; }
    public double Quality { get; set; }
    public double Oee { get; set; }
    public double PlannedRuntime { get; set; }
    public double ActualRuntime { get; set; }
    public int DowntimeMinutes { get; set; }
}
```


- [ ] **Step 2: Implement IReportExportService interface**

Create file `src/MesCopilot.Application/Services/IReportExportService.cs`:

```csharp
using MesCopilot.Application.DTOs.Reports;

namespace MesCopilot.Application.Services;

public enum ReportFormat
{
    Excel,
    Pdf
}

public interface IReportExportService
{
    Task<byte[]> ExportProductionReportAsync(DateTime date, int? lineId, ReportFormat format);
    Task<byte[]> ExportQualityReportAsync(DateTime from, DateTime to, ReportFormat format);
    Task<byte[]> ExportOeeReportAsync(List<int> equipmentIds, DateTime from, DateTime to, ReportFormat format);
}
```
- [ ] **Step 3: Implement ReportExportService with ClosedXML (Excel)**

Create file `src/MesCopilot.Application/Services/ReportExportService.cs`:

```csharp
using ClosedXML.Excel;
using MesCopilot.Application.DTOs.Reports;
using MesCopilot.Domain.Enums;

namespace MesCopilot.Application.Services;

public class ReportExportService : IReportExportService
{
    private readonly IWorkOrderService _workOrderService;
    private readonly IQualityService _qualityService;
    private readonly IEquipmentService _equipmentService;

    public ReportExportService(
        IWorkOrderService workOrderService,
        IQualityService qualityService,
        IEquipmentService equipmentService)
    {
        _workOrderService = workOrderService;
        _qualityService = qualityService;
        _equipmentService = equipmentService;
    }

    public async Task<byte[]> ExportProductionReportAsync(DateTime date, int? lineId, ReportFormat format)
    {
        var orders = await _workOrderService.GetWorkOrdersByDateAsync(date);
        if (lineId.HasValue)
            orders = orders.Where(o => o.ProductionLineId == lineId.Value).ToList();

        var report = new ProductionReportDto
        {
            Date = date,
            ProductionLineId = lineId,
            TotalOrders = orders.Count,
            CompletedOrders = orders.Count(o => o.Status == WorkOrderStatus.Completed),
            TotalPlannedQuantity = orders.Sum(o => o.PlannedQuantity),
            TotalActualQuantity = orders.Sum(o => o.ActualQuantity),
            CompletionRate = orders.Count > 0
                ? (double)orders.Count(o => o.Status == WorkOrderStatus.Completed) / orders.Count * 100
                : 0,
            LineItems = orders.Select(o => new ProductionReportLineItem
            {
                WorkOrderCode = o.Code,
                ProductName = o.ProductName,
                PlannedQuantity = o.PlannedQuantity,
                ActualQuantity = o.ActualQuantity,
                Status = o.Status.ToString(),
                DueDate = o.DueDate
            }).ToList()
        };

        return format == ReportFormat.Excel
            ? GenerateProductionExcel(report)
            : GenerateProductionPdf(report);
    }

    public async Task<byte[]> ExportQualityReportAsync(DateTime from, DateTime to, ReportFormat format)
    {
        var inspections = await _qualityService.GetInspectionsByDateRangeAsync(from, to);

        var report = new QualityReportDto
        {
            FromDate = from,
            ToDate = to,
            TotalInspections = inspections.Count,
            PassedCount = inspections.Count(i => i.Result == InspectionResult.Pass),
            FailedCount = inspections.Count(i => i.Result == InspectionResult.Fail),
            PassRate = inspections.Count > 0
                ? (double)inspections.Count(i => i.Result == InspectionResult.Pass) / inspections.Count * 100
                : 0,
            LineItems = inspections.Select(i => new QualityReportLineItem
            {
                InspectionCode = i.Code,
                WorkOrderCode = i.WorkOrderCode,
                InspectorName = i.InspectorName,
                Result = i.Result.ToString(),
                InspectedAt = i.InspectedAt,
                DefectCount = i.DefectCount
            }).ToList()
        };

        return format == ReportFormat.Excel
            ? GenerateQualityExcel(report)
            : GenerateQualityPdf(report);
    }

    public async Task<byte[]> ExportOeeReportAsync(List<int> equipmentIds, DateTime from, DateTime to, ReportFormat format)
    {
        var oeeData = await _equipmentService.GetOeeDataByDateRangeAsync(equipmentIds, from, to);

        var report = new OeeReportDto
        {
            FromDate = from,
            ToDate = to,
            EquipmentIds = equipmentIds,
            AverageOee = oeeData.Average(d => d.Oee),
            AverageAvailability = oeeData.Average(d => d.Availability),
            AveragePerformance = oeeData.Average(d => d.Performance),
            AverageQuality = oeeData.Average(d => d.Quality),
            LineItems = oeeData.Select(d => new OeeReportLineItem
            {
                EquipmentId = d.EquipmentId,
                EquipmentName = d.EquipmentName,
                Date = d.Date,
                Availability = d.Availability,
                Performance = d.Performance,
                Quality = d.Quality,
                Oee = d.Oee,
                PlannedRuntime = d.PlannedRuntime,
                ActualRuntime = d.ActualRuntime,
                DowntimeMinutes = d.DowntimeMinutes
            }).ToList()
        };

        return format == ReportFormat.Excel
            ? GenerateOeeExcel(report)
            : GenerateOeePdf(report);
    }

    private byte[] GenerateProductionExcel(ProductionReportDto report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Production Report");

        // Header
        ws.Cell("A1").Value = "MES Copilot - Production Daily Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        ws.Cell("A2").Value = $"Date: {report.Date:yyyy-MM-dd}";
        ws.Cell("A3").Value = $"Total Orders: {report.TotalOrders} | Completed: {report.CompletedOrders} | Completion Rate: {report.CompletionRate:F1}%";

        // Table headers (row 5)
        var headers = new[] { "Work Order", "Product", "Planned Qty", "Actual Qty", "Status", "Due Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(5, i + 1).Value = headers[i];
            ws.Cell(5, i + 1).Style.Font.Bold = true;
            ws.Cell(5, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        for (int i = 0; i < report.LineItems.Count; i++)
        {
            var item = report.LineItems[i];
            ws.Cell(6 + i, 1).Value = item.WorkOrderCode;
            ws.Cell(6 + i, 2).Value = item.ProductName;
            ws.Cell(6 + i, 3).Value = item.PlannedQuantity;
            ws.Cell(6 + i, 4).Value = item.ActualQuantity;
            ws.Cell(6 + i, 5).Value = item.Status;
            ws.Cell(6 + i, 6).Value = item.DueDate.ToString("yyyy-MM-dd");
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private byte[] GenerateQualityExcel(QualityReportDto report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Quality Report");

        ws.Cell("A1").Value = "MES Copilot - Quality Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        ws.Cell("A2").Value = $"Period: {report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}";
        ws.Cell("A3").Value = $"Total: {report.TotalInspections} | Pass: {report.PassedCount} | Fail: {report.FailedCount} | Pass Rate: {report.PassRate:F1}%";

        var headers = new[] { "Inspection", "Work Order", "Inspector", "Result", "Inspected At", "Defects" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(5, i + 1).Value = headers[i];
            ws.Cell(5, i + 1).Style.Font.Bold = true;
            ws.Cell(5, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (int i = 0; i < report.LineItems.Count; i++)
        {
            var item = report.LineItems[i];
            ws.Cell(6 + i, 1).Value = item.InspectionCode;
            ws.Cell(6 + i, 2).Value = item.WorkOrderCode;
            ws.Cell(6 + i, 3).Value = item.InspectorName;
            ws.Cell(6 + i, 4).Value = item.Result;
            ws.Cell(6 + i, 5).Value = item.InspectedAt.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(6 + i, 6).Value = item.DefectCount;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private byte[] GenerateOeeExcel(OeeReportDto report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("OEE Report");

        ws.Cell("A1").Value = "MES Copilot - OEE Report";
        ws.Cell("A1").Style.Font.Bold = true;
        ws.Cell("A1").Style.Font.FontSize = 14;
        ws.Cell("A2").Value = $"Period: {report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}";
        ws.Cell("A3").Value = $"Average OEE: {report.AverageOee:F1}% | A: {report.AverageAvailability:F1}% | P: {report.AveragePerformance:F1}% | Q: {report.AverageQuality:F1}%";

        var headers = new[] { "Equipment", "Date", "Availability%", "Performance%", "Quality%", "OEE%", "Runtime(h)", "Downtime(min)" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(5, i + 1).Value = headers[i];
            ws.Cell(5, i + 1).Style.Font.Bold = true;
            ws.Cell(5, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (int i = 0; i < report.LineItems.Count; i++)
        {
            var item = report.LineItems[i];
            ws.Cell(6 + i, 1).Value = item.EquipmentName;
            ws.Cell(6 + i, 2).Value = item.Date.ToString("yyyy-MM-dd");
            ws.Cell(6 + i, 3).Value = Math.Round(item.Availability, 1);
            ws.Cell(6 + i, 4).Value = Math.Round(item.Performance, 1);
            ws.Cell(6 + i, 5).Value = Math.Round(item.Quality, 1);
            ws.Cell(6 + i, 6).Value = Math.Round(item.Oee, 1);
            ws.Cell(6 + i, 7).Value = Math.Round(item.ActualRuntime, 1);
            ws.Cell(6 + i, 8).Value = item.DowntimeMinutes;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // PDF generation methods - using QuestPDF
    private byte[] GenerateProductionPdf(ProductionReportDto report) => GeneratePdfReport("Production Daily Report", report.Date.ToString("yyyy-MM-dd"), report);
    private byte[] GenerateQualityPdf(QualityReportDto report) => GeneratePdfReport("Quality Report", $"{report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}", report);
    private byte[] GenerateOeePdf(OeeReportDto report) => GeneratePdfReport("OEE Report", $"{report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}", report);

    private byte[] GeneratePdfReport(string title, string period, object reportData)
    {
        // QuestPDF implementation in next step
        throw new NotImplementedException("See Step 4 for QuestPDF implementation");
    }
}
```
- [ ] **Step 4: Implement QuestPDF report generation**

Create file `src/MesCopilot.Application/Services/PdfReportGenerator.cs`:

```csharp
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using MesCopilot.Application.DTOs.Reports;

namespace MesCopilot.Application.Services;

public static class PdfReportGenerator
{
    static PdfReportGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] GenerateProductionReport(ProductionReportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Production Daily Report", report.Date.ToString("yyyy-MM-dd")));

                page.Content().Element(c =>
                {
                    c.PaddingVertical(10).Column(col =>
                    {
                        // Summary
                        col.Item().Text($"Total Orders: {report.TotalOrders} | Completed: {report.CompletedOrders} | Rate: {report.CompletionRate:F1}%");
                        col.Item().Text($"Planned Qty: {report.TotalPlannedQuantity} | Actual Qty: {report.TotalActualQuantity}");
                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Work Order
                                columns.RelativeColumn(2); // Product
                                columns.RelativeColumn(1); // Planned
                                columns.RelativeColumn(1); // Actual
                                columns.RelativeColumn(1); // Status
                                columns.RelativeColumn(1.5f); // Due Date
                            });

                            // Header row
                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeaderStyle).Text("Work Order");
                                header.Cell().Element(CellHeaderStyle).Text("Product");
                                header.Cell().Element(CellHeaderStyle).Text("Planned");
                                header.Cell().Element(CellHeaderStyle).Text("Actual");
                                header.Cell().Element(CellHeaderStyle).Text("Status");
                                header.Cell().Element(CellHeaderStyle).Text("Due Date");
                            });

                            // Data rows
                            foreach (var item in report.LineItems)
                            {
                                table.Cell().Element(CellStyle).Text(item.WorkOrderCode);
                                table.Cell().Element(CellStyle).Text(item.ProductName);
                                table.Cell().Element(CellStyle).Text(item.PlannedQuantity.ToString());
                                table.Cell().Element(CellStyle).Text(item.ActualQuantity.ToString());
                                table.Cell().Element(CellStyle).Text(item.Status);
                                table.Cell().Element(CellStyle).Text(item.DueDate.ToString("yyyy-MM-dd"));
                            }
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public static byte[] GenerateQualityReport(QualityReportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => ComposeHeader(c, "Quality Report", $"{report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}"));

                page.Content().Element(c =>
                {
                    c.PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Total: {report.TotalInspections} | Pass: {report.PassedCount} | Fail: {report.FailedCount} | Rate: {report.PassRate:F1}%");
                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeaderStyle).Text("Inspection");
                                header.Cell().Element(CellHeaderStyle).Text("Work Order");
                                header.Cell().Element(CellHeaderStyle).Text("Inspector");
                                header.Cell().Element(CellHeaderStyle).Text("Result");
                                header.Cell().Element(CellHeaderStyle).Text("Time");
                                header.Cell().Element(CellHeaderStyle).Text("Defects");
                            });

                            foreach (var item in report.LineItems)
                            {
                                table.Cell().Element(CellStyle).Text(item.InspectionCode);
                                table.Cell().Element(CellStyle).Text(item.WorkOrderCode);
                                table.Cell().Element(CellStyle).Text(item.InspectorName);
                                table.Cell().Element(CellStyle).Text(item.Result);
                                table.Cell().Element(CellStyle).Text(item.InspectedAt.ToString("MM-dd HH:mm"));
                                table.Cell().Element(CellStyle).Text(item.DefectCount.ToString());
                            }
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    public static byte[] GenerateOeeReport(OeeReportDto report)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, "OEE Report", $"{report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}"));

                page.Content().Element(c =>
                {
                    c.PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Avg OEE: {report.AverageOee:F1}% | A: {report.AverageAvailability:F1}% | P: {report.AveragePerformance:F1}% | Q: {report.AverageQuality:F1}%");
                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);   // Equipment
                                columns.RelativeColumn(1.5f); // Date
                                columns.RelativeColumn(1);   // A%
                                columns.RelativeColumn(1);   // P%
                                columns.RelativeColumn(1);   // Q%
                                columns.RelativeColumn(1);   // OEE%
                                columns.RelativeColumn(1);   // Runtime
                                columns.RelativeColumn(1);   // Downtime
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellHeaderStyle).Text("Equipment");
                                header.Cell().Element(CellHeaderStyle).Text("Date");
                                header.Cell().Element(CellHeaderStyle).Text("A%");
                                header.Cell().Element(CellHeaderStyle).Text("P%");
                                header.Cell().Element(CellHeaderStyle).Text("Q%");
                                header.Cell().Element(CellHeaderStyle).Text("OEE%");
                                header.Cell().Element(CellHeaderStyle).Text("Runtime(h)");
                                header.Cell().Element(CellHeaderStyle).Text("Down(min)");
                            });

                            foreach (var item in report.LineItems)
                            {
                                table.Cell().Element(CellStyle).Text(item.EquipmentName);
                                table.Cell().Element(CellStyle).Text(item.Date.ToString("yyyy-MM-dd"));
                                table.Cell().Element(CellStyle).Text($"{item.Availability:F1}");
                                table.Cell().Element(CellStyle).Text($"{item.Performance:F1}");
                                table.Cell().Element(CellStyle).Text($"{item.Quality:F1}");
                                table.Cell().Element(CellStyle).Text($"{item.Oee:F1}");
                                table.Cell().Element(CellStyle).Text($"{item.ActualRuntime:F1}");
                                table.Cell().Element(CellStyle).Text(item.DowntimeMinutes.ToString());
                            }
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string title, string period)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("MES Copilot").FontSize(16).Bold().FontColor(Colors.Teal.Darken2);
                col.Item().Text(title).FontSize(12).SemiBold();
                col.Item().Text($"Period: {period}").FontSize(9).FontColor(Colors.Grey.Darken1);
            });

            row.ConstantItem(100).AlignRight().Text(DateTime.Now.ToString("yyyy-MM-dd HH:mm")).FontSize(8);
        });
    }

    private static IContainer CellHeaderStyle(IContainer container) =>
        container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);

    private static IContainer CellStyle(IContainer container) =>
        container.PaddingVertical(3).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);
}
```
- [ ] **Step 5: Implement ClosedXML Excel generation**

Create file `src/MesCopilot.Application/Services/ExcelReportGenerator.cs`:

```csharp
using ClosedXML.Excel;
using MesCopilot.Application.DTOs.Reports;

namespace MesCopilot.Application.Services;

public static class ExcelReportGenerator
{
    public static byte[] GenerateProductionReport(ProductionReportDto report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Production");

        // Header
        ws.Cell(1, 1).Value = "MES Copilot - Production Daily Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 6).Merge();

        ws.Cell(2, 1).Value = $"Date: {report.Date:yyyy-MM-dd}";
        ws.Cell(3, 1).Value = $"Total: {report.TotalOrders} | Completed: {report.CompletedOrders} | Rate: {report.CompletionRate:F1}%";

        // Table header
        var headerRow = 5;
        var headers = new[] { "Work Order", "Product", "Planned Qty", "Actual Qty", "Status", "Due Date" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(headerRow, i + 1).Value = headers[i];
            ws.Cell(headerRow, i + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data
        var row = headerRow + 1;
        foreach (var item in report.LineItems)
        {
            ws.Cell(row, 1).Value = item.WorkOrderCode;
            ws.Cell(row, 2).Value = item.ProductName;
            ws.Cell(row, 3).Value = item.PlannedQuantity;
            ws.Cell(row, 4).Value = item.ActualQuantity;
            ws.Cell(row, 5).Value = item.Status;
            ws.Cell(row, 6).Value = item.DueDate.ToString("yyyy-MM-dd");
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] GenerateQualityReport(QualityReportDto report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Quality");

        ws.Cell(1, 1).Value = "MES Copilot - Quality Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 6).Merge();

        ws.Cell(2, 1).Value = $"Period: {report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}";
        ws.Cell(3, 1).Value = $"Pass Rate: {report.PassRate:F1}% ({report.PassedCount}/{report.TotalInspections})";

        var headerRow = 5;
        var headers = new[] { "Inspection", "Work Order", "Inspector", "Result", "Time", "Defects" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(headerRow, i + 1).Value = headers[i];
            ws.Cell(headerRow, i + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = headerRow + 1;
        foreach (var item in report.LineItems)
        {
            ws.Cell(row, 1).Value = item.InspectionCode;
            ws.Cell(row, 2).Value = item.WorkOrderCode;
            ws.Cell(row, 3).Value = item.InspectorName;
            ws.Cell(row, 4).Value = item.Result;
            ws.Cell(row, 5).Value = item.InspectedAt.ToString("MM-dd HH:mm");
            ws.Cell(row, 6).Value = item.DefectCount;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] GenerateOeeReport(OeeReportDto report)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("OEE");

        ws.Cell(1, 1).Value = "MES Copilot - OEE Report";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, 8).Merge();

        ws.Cell(2, 1).Value = $"Period: {report.FromDate:yyyy-MM-dd} ~ {report.ToDate:yyyy-MM-dd}";
        ws.Cell(3, 1).Value = $"Avg OEE: {report.AverageOee:F1}% | A: {report.AverageAvailability:F1}% | P: {report.AveragePerformance:F1}% | Q: {report.AverageQuality:F1}%";

        var headerRow = 5;
        var headers = new[] { "Equipment", "Date", "Availability%", "Performance%", "Quality%", "OEE%", "Runtime(h)", "Downtime(min)" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(headerRow, i + 1).Value = headers[i];
            ws.Cell(headerRow, i + 1).Style.Font.Bold = true;
            ws.Cell(headerRow, i + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        var row = headerRow + 1;
        foreach (var item in report.LineItems)
        {
            ws.Cell(row, 1).Value = item.EquipmentName;
            ws.Cell(row, 2).Value = item.Date.ToString("yyyy-MM-dd");
            ws.Cell(row, 3).Value = Math.Round(item.Availability, 1);
            ws.Cell(row, 4).Value = Math.Round(item.Performance, 1);
            ws.Cell(row, 5).Value = Math.Round(item.Quality, 1);
            ws.Cell(row, 6).Value = Math.Round(item.Oee, 1);
            ws.Cell(row, 7).Value = Math.Round(item.ActualRuntime, 1);
            ws.Cell(row, 8).Value = item.DowntimeMinutes;
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
```                MaintenanceRecommendation = "建议在近期安排预防性维护"
            });

        var tool = new PredictMaintenanceTool(mockService.Object);

        // Act
        var result = await tool.ExecuteAsync(1);

        // Assert
        Assert.NotNull(result.Data);
        Assert.Contains("EQ-001", result.Explanation);
        Assert.Contains("75.5", result.Explanation);
        Assert.Null(result.Debug);
    }

    [Fact]
    public async Task ExecuteAsync_DebugMode_IncludesDebugInfo()
    {
        // Arrange
        var mockService = new Mock<IMaintenancePredictionService>();
        mockService.Setup(s => s.PredictMaintenanceAsync(1))
            .ReturnsAsync(new MaintenancePredictionDto
            {
                EquipmentId = 1, EquipmentCode = "EQ-001", EquipmentName = "Test",
                HealthScore = 90, HealthLevel = "良好", Mtbf = 200, Mttr = 1
            });

        var tool = new PredictMaintenanceTool(mockService.Object);

        // Act
        var result = await tool.ExecuteAsync(1, debugMode: true);

        // Assert
        Assert.NotNull(result.Debug);
    }
}
```

- [ ] **Step 7: Register service in DI**

Modify `src/MesCopilot.Application/DependencyInjection.cs`, add line:

```csharp
services.AddScoped<IMaintenancePredictionService, MaintenancePredictionService>();
```

- [ ] **Step 8: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests --filter "FullyQualifiedName~PredictMaintenance"
```

Expected: All tests pass

- [ ] **Step 9: Commit**

```bash
git add src/MesCopilot.Application/Dtos/MaintenancePredictionDto.cs
git add src/MesCopilot.Application/Services/IMaintenancePredictionService.cs
git add src/MesCopilot.Application/Services/MaintenancePredictionService.cs
git add src/MesCopilot.Agent/Plugins/OeeAgentPlugin/Tools/PredictMaintenanceTool.cs
git add tests/MesCopilot.UnitTests/
git commit -m "feat(agent): add PredictMaintenanceTool with MTBF/MTTR analysis

- MaintenancePredictionService: calculate health score from downtime/alarm data
- Moving average trend detection (30-day window)
- Health levels: 良好/注意/警告/危险
- Predicted next failure time based on MTBF
- Auto-generated maintenance recommendations
- Unit tests for service and tool"
```

Expected: Changes committed

---

## Sub-Phase 2F: Conversation History Search (3 days)

### Task 2F.1: Add Full-Text Search to ConversationMessages

**Files:**
- Create: Migration `AddConversationFullTextSearch`
- Modify: `src/MesCopilot.Application/Services/IConversationService.cs`
- Modify: `src/MesCopilot.Application/Services/ConversationService.cs`

**Interfaces:**
- Consumes: MesDbContext, Conversation/ConversationMessage entities
- Produces: SearchConversationsAsync method

- [ ] **Step 1: Create migration for GIN full-text index**

```bash
cd src/MesCopilot.Infrastructure
dotnet ef migrations add AddConversationFullTextSearch
```

Manually edit the generated migration file:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        CREATE INDEX ix_conversation_messages_content_fulltext
        ON conversation_messages
        USING GIN (to_tsvector('simple', content));
    ");
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP INDEX IF EXISTS ix_conversation_messages_content_fulltext;");
}
```

- [ ] **Step 2: Apply migration**

```bash
dotnet ef database update
```

Expected: Migration applied, GIN index created

- [ ] **Step 3: Extend IConversationService**

Add to `src/MesCopilot.Application/Services/IConversationService.cs`:

```csharp
Task<List<ConversationSearchResultDto>> SearchConversationsAsync(string query, int userId, int skip = 0, int take = 20);
```

Create DTO in `src/MesCopilot.Application/Dtos/ConversationSearchResultDto.cs`:

```csharp
public class ConversationSearchResultDto
{
    public Guid ConversationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string MatchedSnippet { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public int MessageCount { get; set; }
}
```

- [ ] **Step 4: Implement full-text search in ConversationService**

Add method to `src/MesCopilot.Application/Services/ConversationService.cs`:

```csharp
public async Task<List<ConversationSearchResultDto>> SearchConversationsAsync(string query, int userId, int skip = 0, int take = 20)
{
    var results = await _context.ConversationMessages
        .Where(m => m.Conversation.UserId == userId)
        .Where(m => EF.Functions.ToTsVector("simple", m.Content)
            .Matches(EF.Functions.PlainToTsQuery("simple", query)))
        .GroupBy(m => m.ConversationId)
        .Select(g => new
        {
            ConversationId = g.Key,
            LastMessageAt = g.Max(m => m.CreatedAt),
            MessageCount = g.Count(),
            MatchedContent = g.First().Content
        })
        .OrderByDescending(x => x.LastMessageAt)
        .Skip(skip)
        .Take(take)
        .ToListAsync();

    var conversationIds = results.Select(r => r.ConversationId).ToList();
    var conversations = await _context.Conversations
        .Where(c => conversationIds.Contains(c.Id))
        .ToDictionaryAsync(c => c.Id, c => c.Title);

    return results.Select(r => new ConversationSearchResultDto
    {
        ConversationId = r.ConversationId,
        Title = conversations.GetValueOrDefault(r.ConversationId, "Untitled"),
        MatchedSnippet = r.MatchedContent.Length > 200 ? r.MatchedContent.Substring(0, 200) + "..." : r.MatchedContent,
        LastMessageAt = r.LastMessageAt,
        MessageCount = r.MessageCount
    }).ToList();
}
```

- [ ] **Step 5: Add search endpoint to AgentController**

Add to `src/MesCopilot.Api/Controllers/AgentController.cs`:

```csharp
[HttpGet("conversations/search")]
[Authorize]
public async Task<IActionResult> SearchConversations([FromQuery] string q, [FromQuery] int skip = 0, [FromQuery] int take = 20)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var results = await _conversationService.SearchConversationsAsync(q, userId, skip, take);
    return Ok(results);
}
```

- [ ] **Step 6: Write tests**

Create `tests/MesCopilot.IntegrationTests/Api/AgentControllerSearchTests.cs`:

```csharp
[Fact]
public async Task SearchConversations_ReturnsMatchingResults()
{
    // Arrange
    var conversation = await CreateConversationWithMessages("Test conversation", new[]
    {
        "How do I check equipment OEE?",
        "What is the defect rate for product X?"
    });

    // Act
    var response = await _client.GetAsync("/api/agent/conversations/search?q=defect");

    // Assert
    response.EnsureSuccessStatusCode();
    var results = await response.Content.ReadFromJsonAsync<List<ConversationSearchResultDto>>();
    Assert.Single(results);
    Assert.Contains("defect", results[0].MatchedSnippet, StringComparison.OrdinalIgnoreCase);
}
```

- [ ] **Step 7: Run tests**

```bash
dotnet test tests/MesCopilot.IntegrationTests --filter "FullyQualifiedName~AgentControllerSearchTests"
```

Expected: PASS

- [ ] **Step 8: Commit**

```bash
git add src/ tests/
git commit -m "feat(agent): add full-text search for conversation history

- PostgreSQL GIN index on ConversationMessages.Content
- SearchConversationsAsync with snippet extraction
- GET /api/agent/conversations/search endpoint
- Returns matching conversations ordered by recency
- Integration tests for search functionality"
```

Expected: Changes committed

---

## Sub-Phase 2G: Document Versioning (3 days)

### Task 2G.1: Add DocumentVersion Entity and Migration

**Files:**
- Create: `src/MesCopilot.Domain/Entities/Knowledge/DocumentVersion.cs`
- Create: Migration `AddDocumentVersions`
- Modify: `src/MesCopilot.Infrastructure/Data/MesDbContext.cs`

**Interfaces:**
- Consumes: Document entity
- Produces: DocumentVersion entity

- [ ] **Step 1: Create DocumentVersion entity**

Create file `src/MesCopilot.Domain/Entities/Knowledge/DocumentVersion.cs`:

```csharp
namespace MesCopilot.Domain.Entities.Knowledge;

public class DocumentVersion
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public int VersionNumber { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public int UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    public string ChangeNote { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
```

- [ ] **Step 2: Configure entity in DbContext**

Add to `src/MesCopilot.Infrastructure/Data/MesDbContext.cs`:

```csharp
public DbSet<DocumentVersion> DocumentVersions { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... existing configurations

    modelBuilder.Entity<DocumentVersion>(entity =>
    {
        entity.HasKey(e => e.Id);
        entity.HasOne(e => e.Document)
            .WithMany()
            .HasForeignKey(e => e.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasIndex(e => new { e.DocumentId, e.VersionNumber }).IsUnique();
        entity.Property(e => e.ChangeNote).HasMaxLength(500);
    });
}
```

- [ ] **Step 3: Create and apply migration**

```bash
cd src/MesCopilot.Infrastructure
dotnet ef migrations add AddDocumentVersions
dotnet ef database update
```

Expected: DocumentVersions table created

- [ ] **Step 4: Commit**

```bash
git add src/MesCopilot.Domain/Entities/Knowledge/DocumentVersion.cs src/MesCopilot.Infrastructure/
git commit -m "feat(knowledge): add DocumentVersion entity and migration

- DocumentVersion tracks file path, size, uploader, change note
- Unique constraint on (DocumentId, VersionNumber)
- IsActive flag indicates current version
- Cascade delete when parent Document is deleted"
```

Expected: Changes committed

---

### Task 2G.2: Implement Version Management Service Methods

**Files:**
- Modify: `src/MesCopilot.Application/Services/IKnowledgeService.cs`
- Modify: `src/MesCopilot.Application/Services/KnowledgeService.cs`
- Create: `src/MesCopilot.Application/Dtos/DocumentVersionDto.cs`

**Interfaces:**
- Consumes: DocumentVersion entity, IVectorStoreService
- Produces: Version management methods

- [ ] **Step 1: Create DocumentVersionDto**

```csharp
public class DocumentVersionDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string ChangeNote { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public long FileSizeBytes { get; set; }
    public bool IsActive { get; set; }
}
```

- [ ] **Step 2: Extend IKnowledgeService**

```csharp
Task<DocumentVersionDto> UploadNewVersionAsync(int documentId, Stream fileStream, string fileName, string changeNote, int uploadedBy);
Task<List<DocumentVersionDto>> GetVersionHistoryAsync(int documentId);
Task RevertToVersionAsync(int documentId, int versionId);
```

- [ ] **Step 3: Implement version methods in KnowledgeService**

```csharp
public async Task<DocumentVersionDto> UploadNewVersionAsync(int documentId, Stream fileStream, string fileName, string changeNote, int uploadedBy)
{
    var document = await _context.Documents.FindAsync(documentId)
        ?? throw new ArgumentException($"Document {documentId} not found");

    var lastVersion = await _context.DocumentVersions
        .Where(v => v.DocumentId == documentId)
        .OrderByDescending(v => v.VersionNumber)
        .FirstOrDefaultAsync();

    var newVersionNumber = (lastVersion?.VersionNumber ?? 0) + 1;
    var filePath = $"uploads/knowledge/doc_{documentId}_v{newVersionNumber}_{fileName}";

    // Save file to disk
    using (var fileOutput = File.Create(filePath))
    {
        await fileStream.CopyToAsync(fileOutput);
    }

    var fileSize = new FileInfo(filePath).Length;

    // Deactivate previous version
    if (lastVersion != null)
    {
        lastVersion.IsActive = false;
    }

    // Create new version
    var version = new DocumentVersion
    {
        DocumentId = documentId,
        VersionNumber = newVersionNumber,
        FilePath = filePath,
        FileSize = fileSize,
        UploadedBy = uploadedBy,
        UploadedAt = DateTime.UtcNow,
        ChangeNote = changeNote,
        IsActive = true
    };

    _context.DocumentVersions.Add(version);
    await _context.SaveChangesAsync();

    // Re-vectorize asynchronously
    _ = Task.Run(async () =>
    {
        await _vectorStoreService.DeleteDocumentChunksAsync(documentId);
        await _vectorStoreService.IndexDocumentAsync(documentId, filePath);
    });

    return new DocumentVersionDto
    {
        Id = version.Id,
        VersionNumber = version.VersionNumber,
        ChangeNote = version.ChangeNote,
        UploadedAt = version.UploadedAt,
        FileSizeBytes = version.FileSize,
        IsActive = version.IsActive
    };
}

public async Task<List<DocumentVersionDto>> GetVersionHistoryAsync(int documentId)
{
    return await _context.DocumentVersions
        .Where(v => v.DocumentId == documentId)
        .OrderByDescending(v => v.VersionNumber)
        .Select(v => new DocumentVersionDto
        {
            Id = v.Id,
            VersionNumber = v.VersionNumber,
            ChangeNote = v.ChangeNote,
            UploadedAt = v.UploadedAt,
            FileSizeBytes = v.FileSize,
            IsActive = v.IsActive
        })
        .ToListAsync();
}

public async Task RevertToVersionAsync(int documentId, int versionId)
{
    var targetVersion = await _context.DocumentVersions
        .FirstOrDefaultAsync(v => v.Id == versionId && v.DocumentId == documentId)
        ?? throw new ArgumentException($"Version {versionId} not found for document {documentId}");

    // Deactivate all versions
    var allVersions = await _context.DocumentVersions
        .Where(v => v.DocumentId == documentId)
        .ToListAsync();

    foreach (var v in allVersions)
    {
        v.IsActive = false;
    }

    // Activate target version
    targetVersion.IsActive = true;
    await _context.SaveChangesAsync();

    // Re-vectorize
    await _vectorStoreService.DeleteDocumentChunksAsync(documentId);
    await _vectorStoreService.IndexDocumentAsync(documentId, targetVersion.FilePath);
}
```

- [ ] **Step 4: Add API endpoints**

Add to `src/MesCopilot.Api/Controllers/DocumentsController.cs`:

```csharp
[HttpPost("{id}/versions")]
[Authorize(Policy = "RequireAdmin")]
public async Task<IActionResult> UploadNewVersion(int id, IFormFile file, [FromForm] string changeNote)
{
    var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    using var stream = file.OpenReadStream();
    var version = await _knowledgeService.UploadNewVersionAsync(id, stream, file.FileName, changeNote, userId);
    return Ok(version);
}

[HttpGet("{id}/versions")]
public async Task<IActionResult> GetVersionHistory(int id)
{
    var versions = await _knowledgeService.GetVersionHistoryAsync(id);
    return Ok(versions);
}

[HttpPost("{id}/versions/{versionId}/revert")]
[Authorize(Policy = "RequireAdmin")]
public async Task<IActionResult> RevertToVersion(int id, int versionId)
{
    await _knowledgeService.RevertToVersionAsync(id, versionId);
    return NoContent();
}
```

- [ ] **Step 5: Write tests**

```csharp
[Fact]
public async Task UploadNewVersion_CreatesNewVersion()
{
    // Arrange
    var doc = await CreateTestDocument("test.pdf");
    var stream = new MemoryStream(Encoding.UTF8.GetBytes("New content v2"));

    // Act
    var version = await _service.UploadNewVersionAsync(doc.Id, stream, "test_v2.pdf", "Added section 3", 1);

    // Assert
    Assert.Equal(2, version.VersionNumber);
    Assert.True(version.IsActive);
    var oldVersion = await _context.DocumentVersions.FirstAsync(v => v.DocumentId == doc.Id && v.VersionNumber == 1);
    Assert.False(oldVersion.IsActive);
}
```

- [ ] **Step 6: Run tests**

```bash
dotnet test tests/MesCopilot.UnitTests --filter "FullyQualifiedName~DocumentVersionTests"
```

Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/ tests/
git commit -m "feat(knowledge): implement document version management

- UploadNewVersionAsync: stores file, increments version, deactivates old
- GetVersionHistoryAsync: returns ordered version list
- RevertToVersionAsync: activates target version, re-vectorizes
- Async re-vectorization after version operations
- API endpoints for upload/history/revert
- Admin-only access for version mutations"
```

Expected: Changes committed

---

## Final Verification Checklist

Before marking Phase 2 complete, verify:

### Backend

- [ ] `dotnet build` passes without warnings
- [ ] `dotnet test` shows all tests passing (target: 200+ tests)
- [ ] `dotnet format --verify-no-changes` passes
- [ ] All migrations applied: `dotnet ef migrations list` shows all applied
- [ ] Swagger UI loads at `/swagger` with all new endpoints visible
- [ ] JWT authentication works: can login, refresh token, access protected endpoints
- [ ] SSE endpoint `/api/agent/chat` streams events correctly
- [ ] All 3 new Agent tools (PredictMaintenance, SuggestSchedule, FiveWhyAnalysis) callable via Agent
- [ ] Report endpoints return valid Excel/PDF files
- [ ] Conversation search returns results for test queries
- [ ] Document version upload/revert functions correctly

### Frontend

- [ ] `pnpm install` completes without errors
- [ ] `pnpm build` succeeds
- [ ] `pnpm lint` passes
- [ ] `pnpm typecheck` passes
- [ ] `pnpm test` passes (if tests exist)
- [ ] Login page redirects to `/agent` after successful login
- [ ] Logout clears session and redirects to `/login`
- [ ] Unauthorized access redirects to login
- [ ] Agent chat displays streaming responses token-by-token
- [ ] Stop generation button interrupts stream
- [ ] Tool calls show "Thinking..." indicator
- [ ] Structured Result panel updates on `tool_result` event
- [ ] Export buttons download valid files (Excel/PDF)
- [ ] Conversation history page loads and search works
- [ ] Document version UI shows history and allows revert (Admin only)

### Integration

- [ ] `docker compose up` builds and starts all services
- [ ] Database migrations run automatically on startup
- [ ] Health checks pass for all services
- [ ] End-to-end flow: login → ask Agent question → see streamed response → export report → logout

### Documentation

- [ ] README updated with Phase 2 features
- [ ] API documentation reflects new endpoints
- [ ] Environment variables documented (.env.example)
- [ ] Migration steps documented for Phase 1 → Phase 2 upgrade

---

## Phase 2 Complete

Total lines of code added (estimated):
- Backend: ~8,000 lines (Auth, Streaming, Tools, Reports, Search, Versions)
- Frontend: ~2,500 lines (Auth, Streaming hooks, Export buttons, History UI)
- Tests: ~3,500 lines (Unit, Integration, E2E)
- **Total: ~14,000 lines**

Total effort: ~29 working days (6 weeks with parallelization)

**Next**: Phase 3 will focus on multi-tenancy, real PLC integration, advanced scheduling, and mobile UI.
