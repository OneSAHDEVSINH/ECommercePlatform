using ECommercePlatform.Application.Features.Modules.Commands.Update;
using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.DTOs
{
    public record ModuleDto
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Route { get; init; }
        public string? Icon { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }

        // Explicit conversion operator from Module entity to ModuleDto
        public static explicit operator ModuleDto(Module module)
        {
            return new ModuleDto
            {
                Id = module.Id,
                Name = module.Name,
                Description = module.Description,
                Route = module.Route,
                Icon = module.Icon,
                DisplayOrder = module.DisplayOrder,
                IsActive = module.IsActive,
                CreatedOn = module.CreatedOn
            };
        }
    }

    public record UpdateModuleDto
    {
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Route { get; init; }
        public string? Icon { get; init; }
        public int? DisplayOrder { get; init; }
        public bool IsActive { get; init; }

        public static explicit operator UpdateModuleDto(UpdateModuleCommand command)
        {
            return new UpdateModuleDto
            {
                Name = command.Name,
                Description = command.Description,
                Route = command.Route,
                Icon = command.Icon,
                DisplayOrder = command.DisplayOrder,
                IsActive = command.IsActive
            };
        }
    }

    public record ModuleListDto
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Description { get; init; }
        public string? Route { get; init; }
        public string? Icon { get; init; }
        public int DisplayOrder { get; init; }
        public bool IsActive { get; init; }

        public static explicit operator ModuleListDto(Module module)
        {
            return new ModuleListDto
            {
                Id = module.Id,
                Name = module.Name,
                Description = module.Description,
                Route = module.Route,
                Icon = module.Icon,
                DisplayOrder = module.DisplayOrder,
                IsActive = module.IsActive
            };
        }
    }
}