using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using MediatR;

namespace ECommercePlatform.Application.Features.Users.Commands.AssignRolesToUser
{
    public record AssignRolesToUserCommand : IRequest<AppResult>, ITransactionalBehavior
    {
        public required Guid UserId { get; init; }
        public required List<Guid> RoleIds { get; init; } = [];
    }
}