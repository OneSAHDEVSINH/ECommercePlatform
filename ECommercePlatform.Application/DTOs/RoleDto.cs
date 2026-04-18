using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.DTOs
{
    public record RoleDto
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }
        public List<RoleModulePermissionDto>? Permissions { get; init; }

        public static explicit operator RoleDto(Role role)
        {
            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedOn = role.CreatedOn,
                Permissions = role.RolePermissions?
                    .Where(rp => !rp.IsDeleted && rp.Module != null)
                    .GroupBy(rp => new { rp.ModuleId, rp.Module!.Name })
                    .Select(g => new RoleModulePermissionDto
                    {
                        ModuleId = g.Key.ModuleId,
                        ModuleName = g.Key.Name,
                        CanView = g.First().CanView,
                        CanAddEdit = g.First().CanAddEdit,
                        CanDelete = g.First().CanDelete
                    })
                    .ToList()
            };
        }
    }

    public record RoleModulePermissionDto
    {
        public Guid ModuleId { get; init; }
        public string? ModuleName { get; init; }
        public bool CanView { get; init; }
        //public bool CanAdd { get; init; }
        //public bool CanEdit { get; init; }
        public bool CanAddEdit { get; init; }
        public bool CanDelete { get; init; }
    }

    public record RoleListDto
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public bool IsActive { get; init; }
        public int PermissionCount { get; init; }
        public DateTime CreatedOn { get; init; }

        public static explicit operator RoleListDto(Role role)
        {
            return new RoleListDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                PermissionCount = role.RolePermissions?
                    .Where(rp => rp.CanView || rp.CanAddEdit || rp.CanDelete)
                    .Count() ?? 0,
                CreatedOn = role.CreatedOn
            };
        }
    }
}