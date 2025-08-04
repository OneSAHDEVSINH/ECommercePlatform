using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using MediatR;

namespace ECommercePlatform.Application.Features.Roles.Commands.Update
{
    public record UpdateRoleCommand(string Name, string Description) : IRequest<AppResult<RoleDto>>, ITransactionalBehavior
    {
        public required Guid Id { get; init; }
        public string? Name { get; init; } = Name?.Trim() ?? string.Empty;
        public string? Description { get; init; } = Description?.Trim() ?? string.Empty;
        public bool? IsActive { get; init; }
        public List<ModulePermissionDto>? Permissions { get; init; }
    }
}