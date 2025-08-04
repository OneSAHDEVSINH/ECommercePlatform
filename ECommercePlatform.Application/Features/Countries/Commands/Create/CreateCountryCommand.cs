using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using MediatR;

namespace ECommercePlatform.Application.Features.Countries.Commands.Create;

public record CreateCountryCommand(string Name, string Code) : IRequest<AppResult<CountryDto>>, ITransactionalBehavior
{
    public required string Name { get; init; } = Name?.Trim() ?? string.Empty;
    public required string Code { get; init; } = Code?.Trim() ?? string.Empty;
}
