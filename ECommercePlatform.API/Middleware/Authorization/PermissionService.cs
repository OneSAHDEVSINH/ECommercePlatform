using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Application.Interfaces.IServices;
using Microsoft.EntityFrameworkCore;

namespace ECommercePlatform.API.Middleware.Authorization
{
    public class PermissionService(IUnitOfWork unitOfWork, IServiceProvider serviceProvider) : IPermissionService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        //public async Task UpdateUserPermissionsAsync(Guid userId, List<RolePermission> newPermissions)
        //{
        //    // Update permissions in database
        //    // ... your existing update logic ...

        //    // Invalidate cache
        //    using var scope = _serviceProvider.CreateScope();
        //    var cacheService = scope.ServiceProvider.GetRequiredService<IPermissionCacheService>();
        //    await cacheService.InvalidateUserPermissionsAsync(userId);

        //    // Notify via SignalR
        //    var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<PermissionHub>>();
        //    await hubContext.Clients.Group($"user_{userId}").SendAsync("PermissionsUpdated", new
        //    {
        //        userId,
        //        timestamp = DateTime.Now
        //    });
        //}

        public async Task<bool> UserHasPermissionAsync(Guid userId, string moduleName, string permission)
        {
            // Get user roles
            var userRoles = await _unitOfWork.UserRoles.GetByUserIdAsync(userId);
            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

            // Get role permissions for these roles and the specified module
            var rolePermissions = await _unitOfWork.RolePermissions.AsQueryable()
                .Include(rp => rp.Module)
                .AsSplitQuery()
                .Where(rp => roleIds.Contains(rp.RoleId) &&
                            rp.Module!.Name == moduleName &&
                            rp.Module.IsActive &&
                            !rp.Module.IsDeleted &&
                            rp.IsActive &&
                            !rp.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            // Check if any role has the requested permission
            return permission.ToLower() switch
            {
                "view" => rolePermissions.Any(rp => rp.CanView),
                "addedit" => rolePermissions.Any(rp => rp.CanAddEdit),
                "delete" => rolePermissions.Any(rp => rp.CanDelete),
                _ => false
            };
        }

        public async Task<List<UserPermissionDto>> GetUserPermissionsAsync(Guid userId)
        {
            // Get user roles
            var userRoles = await _unitOfWork.UserRoles.GetByUserIdAsync(userId);
            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

            // Get all role permissions for these roles
            var rolePermissions = await _unitOfWork.RolePermissions.AsQueryable()
                .Include(rp => rp.Module)
                .AsSplitQuery()
                .Where(rp => roleIds.Contains(rp.RoleId) &&
                            rp.Module!.IsActive &&
                            !rp.Module.IsDeleted &&
                            rp.IsActive &&
                            !rp.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            // Group by module and aggregate permissions
            var permissions = rolePermissions
                .GroupBy(rp => new { rp.ModuleId, rp.Module!.Name })
                .Select(g => new UserPermissionDto
                {
                    ModuleName = g.Key.Name!,
                    CanView = g.Any(rp => rp.CanView),
                    CanAddEdit = g.Any(rp => rp.CanAddEdit),
                    CanDelete = g.Any(rp => rp.CanDelete)
                })
                .ToList();

            return permissions;
        }

        public async Task<bool> UserCanAccessModuleAsync(Guid userId, string moduleName)
        {
            return await UserHasPermissionAsync(userId, moduleName, "view");
        }

        public async Task<List<string>> GetUserPermissionClaimsAsync(Guid userId)
        {
            var permissions = await GetUserPermissionsAsync(userId);
            var claims = new List<string>();

            foreach (var permission in permissions)
            {
                if (permission.CanView)
                    claims.Add($"{permission.ModuleName}:View");
                if (permission.CanAddEdit)
                    claims.Add($"{permission.ModuleName}:AddEdit");
                if (permission.CanDelete)
                    claims.Add($"{permission.ModuleName}:Delete");
            }

            return claims;
        }
    }
}
