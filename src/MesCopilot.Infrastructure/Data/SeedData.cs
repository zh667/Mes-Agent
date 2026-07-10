using MesCopilot.Domain.Entities.Equipment;
using MesCopilot.Domain.Entities.Identity;
using MesCopilot.Domain.Entities.Products;
using MesCopilot.Domain.Entities.Production;
using MesCopilot.Domain.Entities.Quality;
using MesCopilot.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using EquipmentEntity = MesCopilot.Domain.Entities.Equipment.Equipment;

namespace MesCopilot.Infrastructure.Data;

public static class SeedData
{
    public const string DefaultTenantId = "00000000-0000-0000-0000-000000000001";

    public static async Task SeedAsync(MesDbContext context)
    {
        if (!await context.Tenants.AnyAsync(tenant => tenant.Id == DefaultTenantId))
        {
            context.Tenants.Add(new Tenant
            {
                Id = DefaultTenantId,
                Code = "DEFAULT",
                Name = "Default Tenant",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (await context.Products.AnyAsync())
        {
            await context.SaveChangesAsync();
            return;
        }

        var now = DateTime.UtcNow;

        var products = new[]
        {
            new Product { Id = 1, Code = "PROD-A", Name = "产品A", Unit = "个", CreatedAt = now },
            new Product { Id = 2, Code = "PROD-B", Name = "产品B", Unit = "个", CreatedAt = now },
            new Product { Id = 3, Code = "PROD-C", Name = "产品C", Unit = "个", CreatedAt = now }
        };
        context.Products.AddRange(products);

        var lines = new[]
        {
            new ProductionLine { Id = 1, Code = "LINE-1", Name = "一号线", IsActive = true, CreatedAt = now },
            new ProductionLine { Id = 2, Code = "LINE-2", Name = "二号线", IsActive = true, CreatedAt = now }
        };
        context.ProductionLines.AddRange(lines);

        var equipment = new[]
        {
            new EquipmentEntity { Id = 1, Code = "A101", Name = "冲压机1号", Model = "XYZ-2000", ProductionLineId = 1, RatedCapacity = 100, IdealCycleTime = 36, IsActive = true, CreatedAt = now },
            new EquipmentEntity { Id = 2, Code = "A102", Name = "冲压机2号", Model = "XYZ-2000", ProductionLineId = 1, RatedCapacity = 100, IdealCycleTime = 36, IsActive = true, CreatedAt = now },
            new EquipmentEntity { Id = 3, Code = "B201", Name = "焊接机1号", Model = "WLD-500", ProductionLineId = 2, RatedCapacity = 80, IdealCycleTime = 45, IsActive = true, CreatedAt = now },
            new EquipmentEntity { Id = 4, Code = "B202", Name = "焊接机2号", Model = "WLD-500", ProductionLineId = 2, RatedCapacity = 80, IdealCycleTime = 45, IsActive = true, CreatedAt = now },
            new EquipmentEntity { Id = 5, Code = "C301", Name = "检测设备", Model = "CHK-100", ProductionLineId = 1, RatedCapacity = 200, IdealCycleTime = 18, IsActive = true, CreatedAt = now }
        };
        context.Equipment.AddRange(equipment);

        var workOrders = new[]
        {
            new WorkOrder { Id = 1, Code = "WO-001", ProductId = 1, ProductionLineId = 1, PlannedQuantity = 1000, CompletedQuantity = 750, QualifiedQuantity = 740, Status = WorkOrderStatus.InProgress, PlannedStartTime = now.AddDays(-2), PlannedEndTime = now.AddDays(1), ActualStartTime = now.AddDays(-2), CreatedAt = now.AddDays(-3) },
            new WorkOrder { Id = 2, Code = "WO-002", ProductId = 2, ProductionLineId = 1, PlannedQuantity = 800, CompletedQuantity = 200, QualifiedQuantity = 195, Status = WorkOrderStatus.InProgress, PlannedStartTime = now.AddDays(-1), PlannedEndTime = now.AddDays(2), ActualStartTime = now.AddDays(-1), CreatedAt = now.AddDays(-2) },
            new WorkOrder { Id = 3, Code = "WO-003", ProductId = 3, ProductionLineId = 2, PlannedQuantity = 500, CompletedQuantity = 50, QualifiedQuantity = 48, Status = WorkOrderStatus.InProgress, PlannedStartTime = now.AddDays(-3), PlannedEndTime = now.AddDays(-1), ActualStartTime = now.AddDays(-2), CreatedAt = now.AddDays(-4) },
            new WorkOrder { Id = 4, Code = "WO-004", ProductId = 1, ProductionLineId = 2, PlannedQuantity = 600, CompletedQuantity = 0, QualifiedQuantity = 0, Status = WorkOrderStatus.Scheduled, PlannedStartTime = now.AddDays(-1), PlannedEndTime = now.AddDays(1), CreatedAt = now.AddDays(-2) },
            new WorkOrder { Id = 5, Code = "WO-005", ProductId = 2, ProductionLineId = 1, PlannedQuantity = 1200, CompletedQuantity = 300, QualifiedQuantity = 290, Status = WorkOrderStatus.InProgress, PlannedStartTime = now.AddDays(-2), PlannedEndTime = now, ActualStartTime = now.AddDays(-1), CreatedAt = now.AddDays(-3) },
            new WorkOrder { Id = 6, Code = "WO-006", ProductId = 3, ProductionLineId = 1, PlannedQuantity = 500, CompletedQuantity = 500, QualifiedQuantity = 495, Status = WorkOrderStatus.Completed, PlannedStartTime = now.AddDays(-5), PlannedEndTime = now.AddDays(-3), ActualStartTime = now.AddDays(-5), ActualEndTime = now.AddDays(-3), CreatedAt = now.AddDays(-6) },
            new WorkOrder { Id = 7, Code = "WO-007", ProductId = 1, ProductionLineId = 2, PlannedQuantity = 800, CompletedQuantity = 800, QualifiedQuantity = 790, Status = WorkOrderStatus.Completed, PlannedStartTime = now.AddDays(-4), PlannedEndTime = now.AddDays(-2), ActualStartTime = now.AddDays(-4), ActualEndTime = now.AddDays(-2), CreatedAt = now.AddDays(-5) },
            new WorkOrder { Id = 8, Code = "WO-008", ProductId = 2, ProductionLineId = 1, PlannedQuantity = 1000, CompletedQuantity = 500, QualifiedQuantity = 485, Status = WorkOrderStatus.InProgress, PlannedStartTime = now.AddDays(-1), PlannedEndTime = now.AddDays(2), ActualStartTime = now.AddDays(-1), CreatedAt = now.AddDays(-2) },
            new WorkOrder { Id = 9, Code = "WO-009", ProductId = 3, ProductionLineId = 2, PlannedQuantity = 600, CompletedQuantity = 600, QualifiedQuantity = 595, Status = WorkOrderStatus.Completed, PlannedStartTime = now.AddDays(-6), PlannedEndTime = now.AddDays(-4), ActualStartTime = now.AddDays(-6), ActualEndTime = now.AddDays(-4), CreatedAt = now.AddDays(-7) },
            new WorkOrder { Id = 10, Code = "WO-010", ProductId = 1, ProductionLineId = 1, PlannedQuantity = 900, CompletedQuantity = 450, QualifiedQuantity = 445, Status = WorkOrderStatus.InProgress, PlannedStartTime = now, PlannedEndTime = now.AddDays(3), ActualStartTime = now, CreatedAt = now.AddDays(-1) }
        };
        context.WorkOrders.AddRange(workOrders);

        var defectTypes = new[]
        {
            new DefectType { Id = 1, Code = "D001", Name = "尺寸超差", IsActive = true },
            new DefectType { Id = 2, Code = "D002", Name = "表面划伤", IsActive = true },
            new DefectType { Id = 3, Code = "D003", Name = "焊接不良", IsActive = true },
            new DefectType { Id = 4, Code = "D004", Name = "材料缺陷", IsActive = true }
        };
        context.DefectTypes.AddRange(defectTypes);

        context.EquipmentStatuses.AddRange(equipment.Select(item => new EquipmentStatus
        {
            EquipmentId = item.Id,
            State = EquipmentState.Running,
            StartTime = now,
            Remarks = "Initial seed status"
        }));

        await context.SaveChangesAsync();
    }

    public static async Task SeedBootstrapAdminAsync(
        MesDbContext context,
        UserManager<AppUser> userManager,
        IConfiguration configuration)
    {
        string? email = configuration["BootstrapAdminEmail"]?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        AppUser? user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            string password = configuration["BootstrapAdminPassword"] ??
                throw new InvalidOperationException("BootstrapAdminPassword is required when creating the bootstrap administrator.");
            user = new AppUser
            {
                UserName = email,
                Email = email,
                DisplayName = configuration["BootstrapAdminDisplayName"]?.Trim() ?? "Platform Administrator",
                IsPlatformAdmin = true,
                CreatedAt = DateTime.UtcNow
            };
            IdentityResult createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                string errors = string.Join("; ", createResult.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Bootstrap administrator creation failed: {errors}");
            }
        }
        else if (!user.IsPlatformAdmin)
        {
            user.IsPlatformAdmin = true;
            IdentityResult updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                throw new InvalidOperationException("Bootstrap administrator could not be promoted.");
            }
        }

        bool hasMembership = await context.UserTenantMemberships.AnyAsync(
            membership => membership.UserId == user.Id && membership.TenantId == DefaultTenantId);
        if (!hasMembership)
        {
            context.UserTenantMemberships.Add(new UserTenantMembership
            {
                UserId = user.Id,
                TenantId = DefaultTenantId,
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
    }
}
