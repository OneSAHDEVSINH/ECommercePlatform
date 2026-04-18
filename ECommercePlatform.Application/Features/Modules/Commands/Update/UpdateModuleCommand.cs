using ECommercePlatform.Application.Common.Interfaces;
using ECommercePlatform.Application.Common.Models;
using MediatR;

namespace ECommercePlatform.Application.Features.Modules.Commands.Update
{
    public record UpdateModuleCommand(string Name, string Route, string Description, string Icon) : IRequest<AppResult>, ITransactionalBehavior
    {
        public required Guid Id { get; init; }
        public string? Name { get; init; } = Name?.Trim() ?? string.Empty;
        public string? Route { get; init; } = Route?.Trim() ?? string.Empty;
        public string? Description { get; init; } = Description?.Trim() ?? string.Empty;
        public string? Icon { get; init; } = Icon?.Trim() ?? string.Empty;
        public int DisplayOrder { get; init; }
        public bool IsActive { get; init; }
    }
}