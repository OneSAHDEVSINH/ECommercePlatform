using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using MediatR;

namespace ECommercePlatform.Application.Features.Cities.Commands.Update
{
    public record UpdateCityCommand(string Name) : IRequest<AppResult>, ITransactionalBehavior
    {
        public Guid Id { get; init; }
        public required string Name { get; init; } = Name?.Trim() ?? string.Empty;
        public bool IsActive { get; init; }
        public Guid StateId { get; init; }
    }
}
