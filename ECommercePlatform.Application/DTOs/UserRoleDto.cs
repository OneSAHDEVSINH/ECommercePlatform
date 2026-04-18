using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.DTOs
{
    public record UserRoleDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public Guid RoleId { get; init; }
        public string? UserName { get; init; }
        public string? UserEmail { get; init; }
        public string? RoleName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }
        public string? CreatedBy { get; init; }

        // Explicit conversion operator from UserRole entity to UserRoleDto
        public static explicit operator UserRoleDto(UserRole userRole)
        {
            return new UserRoleDto
            {
                UserId = userRole.UserId,
                RoleId = userRole.RoleId,
                UserName = userRole.User != null ? $"{userRole.User.FirstName} {userRole.User.LastName}".Trim() : null,
                UserEmail = userRole.User?.Email,
                RoleName = userRole.Role?.Name,
                IsActive = userRole.IsActive,
                CreatedOn = userRole.CreatedOn,
                CreatedBy = userRole.CreatedBy
            };
        }
    }

    public record UserRoleListDto
    {
        public Guid Id { get; init; }
        public Guid UserId { get; init; }
        public Guid RoleId { get; init; }
        public string? UserName { get; init; }
        public string? RoleName { get; init; }
        public bool IsActive { get; init; }

        public static explicit operator UserRoleListDto(UserRole userRole)
        {
            return new UserRoleListDto
            {
                UserId = userRole.UserId,
                RoleId = userRole.RoleId,
                UserName = userRole.User != null ? $"{userRole.User.FirstName} {userRole.User.LastName}".Trim() : null,
                RoleName = userRole.Role?.Name,
                IsActive = userRole.IsActive
            };
        }
    }
}