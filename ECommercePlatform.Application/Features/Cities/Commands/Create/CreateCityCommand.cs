using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using MediatR;

namespace ECommercePlatform.Application.Features.Cities.Commands.Create
{
    public record CreateCityCommand(string Name) : IRequest<AppResult<CityDto>>, ITransactionalBehavior
    {
        public required string Name { get; init; } = Name?.Trim() ?? string.Empty;
        public Guid StateId { get; init; }
    }
}
