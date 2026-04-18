using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.DTOs
{
    public record RolePermissionDto
    {
        public Guid Id { get; init; }
        public Guid RoleId { get; init; }
        public Guid ModuleId { get; init; }
        public string? RoleName { get; init; }
        public string? ModuleName { get; init; }
        public bool CanView { get; init; }
        public bool CanAddEdit { get; init; }
        public bool CanDelete { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }

        public static explicit operator RolePermissionDto(RolePermission rolePermission)
        {
            return new RolePermissionDto
            {
                Id = rolePermission.Id,
                RoleId = rolePermission.RoleId,
                ModuleId = rolePermission.ModuleId,
                RoleName = rolePermission.Role?.Name,
                ModuleName = rolePermission.Module?.Name,
                CanView = rolePermission.CanView,
                CanAddEdit = rolePermission.CanAddEdit,
                CanDelete = rolePermission.CanDelete,
                IsActive = rolePermission.IsActive,
                CreatedOn = rolePermission.CreatedOn
            };
        }
    }

    public record ModulePermissionDto
    {
        public required Guid ModuleId { get; init; }
        public bool CanView { get; init; }
        public bool CanAddEdit { get; init; }
        public bool CanDelete { get; init; }
    }
}