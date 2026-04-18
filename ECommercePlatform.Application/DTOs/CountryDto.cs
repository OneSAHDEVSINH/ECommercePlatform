using ECommercePlatform.Application.Features.Countries.Commands.Update;
using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.DTOs
{
    public record CountryDto
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Code { get; init; }
        public bool IsActive { get; init; }
        public DateTime CreatedOn { get; init; }
        public List<StateDto>? States { get; init; }

        // Explicit conversion operator from Country to CountryDto
        public static explicit operator CountryDto(Country country)
        {
            return new CountryDto
            {
                Id = country.Id,
                Name = country.Name,
                Code = country.Code,
                IsActive = country.IsActive,
                CreatedOn = country.CreatedOn,
                States = country.States?.Select(state => (StateDto)state).ToList()
            };
        }
    }

    public record CreateCountryDto
    {
        public required string Name { get; init; }
        public required string Code { get; init; }
    }

    public record UpdateCountryDto
    {
        public string? Name { get; init; }
        public string? Code { get; init; }
        public bool IsActive { get; init; }

        public static explicit operator UpdateCountryDto(UpdateCountryCommand command)
        {
            return new UpdateCountryDto
            {
                Name = command.Name,
                Code = command.Code,
                IsActive = command.IsActive
            };
        }
    }
}