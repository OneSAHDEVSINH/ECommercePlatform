using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using MediatR;

namespace ECommercePlatform.Application.Features.Roles.Commands.Create
{
    public record CreateRoleCommand(string Name, string Description) : IRequest<AppResult<RoleDto>>, ITransactionalBehavior
    {
        public required string Name { get; init; } = Name?.Trim() ?? string.Empty;
        public string? Description { get; init; } = Description?.Trim() ?? string.Empty;
        public bool IsActive { get; init; } = true;
        public List<ModulePermissionDto>? Permissions { get; init; }
    }
}