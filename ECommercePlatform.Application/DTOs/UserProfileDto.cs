// Application/DTOs/UserProfileDto.cs
using ECommercePlatform.Domain.Entities;
using ECommercePlatform.Domain.Enums;

namespace ECommercePlatform.Application.DTOs
{
    public record UserProfileDto
    {
        public Guid Id { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? PhoneNumber { get; init; }
        public Gender Gender { get; init; }
        public DateOnly DateOfBirth { get; init; }
        public string? Bio { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }
        public List<RoleDto>? Roles { get; init; }
        public List<UserPermissionDto>? Permissions { get; init; }

        public static explicit operator UserProfileDto(User user)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Bio = user.Bio,
                IsActive = user.IsActive,
                CreatedOn = user.CreatedOn,
                Roles = user.UserRoles?
                    .Where(ur => ur.Role != null)
                    .Select(ur => (RoleDto)ur.Role!)
                    .ToList()
            };
        }

        // Factory method instead of explicit operator
        public static UserProfileDto Create(User user, List<RoleDto>? roles = null, List<UserPermissionDto>? permissions = null)
        {
            return new UserProfileDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                DateOfBirth = user.DateOfBirth,
                Bio = user.Bio,
                IsActive = user.IsActive,
                CreatedOn = user.CreatedOn,
                Roles = roles ?? user.UserRoles?
                    .Where(ur => ur.Role != null)
                    .Select(ur => (RoleDto)ur.Role!)
                    .ToList(),
                Permissions = permissions
            };
        }
    }
}