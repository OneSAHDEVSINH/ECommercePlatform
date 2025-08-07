using ECommercePlatform.Application.Features.Users.Commands.Update;
using ECommercePlatform.Domain.Entities;
using ECommercePlatform.Domain.Enums;

namespace ECommercePlatform.Application.DTOs
{
    public record UserDto
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

        // Explicit conversion operator from User entity to UserDto
        public static explicit operator UserDto(User user)
        {
            return new UserDto
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
    }

    public record UpdateUserDto
    {
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Password { get; init; }
        public string? PhoneNumber { get; init; }
        public string? Gender { get; init; }
        public string? DateOfBirth { get; init; }
        public string? Bio { get; init; }
        public List<Guid>? RoleIds { get; init; }
        public bool? IsActive { get; init; }

        public static explicit operator UpdateUserDto(UpdateUserCommand command)
        {
            return new UpdateUserDto
            {
                FirstName = command.FirstName,
                LastName = command.LastName,
                Email = command.Email,
                Password = command.Password,
                PhoneNumber = command.PhoneNumber,
                Gender = command.Gender,
                DateOfBirth = command.DateOfBirth,
                Bio = command.Bio,
                RoleIds = command.RoleIds,
                IsActive = command.IsActive
            };
        }
    }

    public record UserListDto
    {
        public Guid Id { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? PhoneNumber { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }
        public int RoleCount { get; init; }

        public static explicit operator UserListDto(User user)
        {
            return new UserListDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedOn = user.CreatedOn,
                RoleCount = user.UserRoles?.Count ?? 0
            };
        }
    }
}