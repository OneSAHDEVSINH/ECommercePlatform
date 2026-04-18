using ECommercePlatform.Application.Features.Cities.Commands.Update;
using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.DTOs
{
    public record CityDto
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public Guid StateId { get; init; }
        public string? StateName { get; init; }
        public Guid CountryId { get; init; }
        public string? CountryName { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }

        // Explicit conversion operator from City to CityDto
        public static explicit operator CityDto(City city)
        {
            return new CityDto
            {
                Id = city.Id,
                Name = city.Name,
                IsActive = city.IsActive,
                CreatedOn = city.CreatedOn,
                StateId = city.StateId,
                StateName = city.State?.Name,
                CountryId = city.State?.CountryId ?? Guid.Empty,
                CountryName = city.State?.Country?.Name
            };
        }
    }

    public record CreateCityDto
    {
        public required string Name { get; init; }
        public Guid StateId { get; init; }
    }

    public record UpdateCityDto
    {
        public string? Name { get; init; }
        public Guid StateId { get; init; }
        public bool IsActive { get; init; }

        public static explicit operator UpdateCityDto(UpdateCityCommand command)
        {
            return new UpdateCityDto
            {
                Name = command.Name,
                StateId = command.StateId,
                IsActive = command.IsActive
            };
        }
    }
}