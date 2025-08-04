using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using MediatR;

namespace ECommercePlatform.Application.Features.Users.Commands.Create
{
    public record CreateUserCommand(string FirstName, string LastName, string Email, string PhoneNumber, string DateOfBirth, string Bio) : IRequest<AppResult<UserDto>>, ITransactionalBehavior
    {
        public required string FirstName { get; init; } = FirstName?.Trim() ?? string.Empty;
        public required string LastName { get; init; } = LastName?.Trim() ?? string.Empty;
        public required string Email { get; init; } = Email?.Trim() ?? string.Empty;
        public required string Password { get; init; }
        public string? PhoneNumber { get; init; } = PhoneNumber?.Trim() ?? string.Empty;
        //public Gender Gender { get; init; }
        public string? Gender { get; init; }
        //public DateOnly? DateOfBirth { get; init; }
        public string? DateOfBirth { get; init; } = DateOfBirth?.Trim() ?? string.Empty;
        public string? Bio { get; init; } = Bio?.Trim() ?? string.Empty;
        public List<Guid>? RoleIds { get; init; } = [];
        public bool IsActive { get; init; } = true;
    }
}