using ECommercePlatform.Application.Features.States.Commands.Update;
using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.DTOs
{
    public record StateDto
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Code { get; init; }
        public Guid CountryId { get; init; }
        public string? CountryName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }
        public List<CityDto>? Cities { get; init; }

        // Explicit conversion operator from State to StateDto
        public static explicit operator StateDto(State state)
        {
            return new StateDto
            {
                Id = state.Id,
                Name = state.Name,
                Code = state.Code,
                CountryId = state?.CountryId ?? Guid.Empty,
                CountryName = state?.Country?.Name,
                IsActive = state!.IsActive,
                CreatedOn = state.CreatedOn
            };
        }
    }

    public record CreateStateDto
    {
        public required string Name { get; init; }
        public required string Code { get; init; }
        public Guid CountryId { get; init; }
    }

    public record UpdateStateDto
    {
        public string? Name { get; init; }
        public string? Code { get; init; }
        public Guid CountryId { get; init; }
        public bool IsActive { get; init; }

        public static explicit operator UpdateStateDto(UpdateStateCommand command)
        {
            return new UpdateStateDto
            {
                Name = command.Name,
                Code = command.Code,
                CountryId = command.CountryId,
                IsActive = command.IsActive
            };
        }
    }
}