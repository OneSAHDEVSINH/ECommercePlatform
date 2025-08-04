using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using MediatR;

namespace ECommercePlatform.Application.Features.States.Commands.Update
{
    public record UpdateStateCommand(string Name, string Code) : IRequest<AppResult>, ITransactionalBehavior
    {
        public Guid Id { get; init; }
        public string? Name { get; init; } = Name?.Trim() ?? string.Empty;
        public string? Code { get; init; } = Code?.Trim() ?? string.Empty;
        public Guid CountryId { get; init; }
        public bool IsActive { get; init; }
    }
}
