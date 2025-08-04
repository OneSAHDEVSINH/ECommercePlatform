using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using MediatR;

namespace ECommercePlatform.Application.Features.Countries.Commands.Update;

public record UpdateCountryCommand(string Name, string Code) : IRequest<AppResult>, ITransactionalBehavior
{
    public Guid Id { get; init; }
    public string? Name { get; init; } = Name?.Trim() ?? string.Empty;
    public string? Code { get; init; } = Code?.Trim() ?? string.Empty;
    public bool IsActive { get; init; }
}
