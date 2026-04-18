using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using MediatR;
using System.Text.Json.Serialization;

namespace ECommercePlatform.Application.Features.Users.Commands.AssignRolesToUser
{
    public record AssignRolesToUserCommand : IRequest<AppResult>, ITransactionalBehavior
    {
        public required Guid UserId { get; init; }
        public required List<Guid> RoleIds { get; init; } = [];
    }
}