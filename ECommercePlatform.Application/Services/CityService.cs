﻿using AutoMapper;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Application.Interfaces.ICity;
using ECommercePlatform.Application.Interfaces.IUserAuth;
using ECommercePlatform.Domain.Entities;
using ECommercePlatform.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace ECommercePlatform.Application.Services
{
    public class CityService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService) : ICityService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly IMapper _mapper = mapper;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public async Task<CityDto> CreateCityAsync(CreateCityDto createCityDto)
        {
            //check for regular expression
            if (!ValidationService.IsValidNameorCode(createCityDto.Name, out var errorMessage))
            {
                throw new ValidationException(errorMessage);
            }

            // Check for duplicate name in the same state
            bool isNameUnique = await _unitOfWork.Cities.IsNameUniqueInStateAsync(createCityDto.Name, createCityDto.StateId);
            if (!isNameUnique)
            {
                throw new DuplicateResourceException($"A city with the name '{createCityDto.Name}' already exists in the selected state.");
            }

            var city = _mapper.Map<City>(createCityDto);
            city.CreatedOn = DateTime.Now;
            city.IsActive = true;

            // Set the creator information
            if (_currentUserService.IsAuthenticated)
            {
                // You can use either the email or user ID as the CreatedBy value
                city.CreatedBy = _currentUserService.UserId ?? _currentUserService.Email;
                city.ModifiedBy = city.CreatedBy;
            }

            // Add the city to the database
            var result = await _unitOfWork.Cities.AddAsync(city);

            await _unitOfWork.CompleteAsync();
            return _mapper.Map<CityDto>(result);
        }

        public async Task DeleteCityAsync(Guid id)
        {
            var city = await _unitOfWork.Cities.GetByIdAsync(id) ?? throw new KeyNotFoundException($"City with ID {id} not found");
            await _unitOfWork.Cities.DeleteAsync(city);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IReadOnlyList<CityDto>> GetAllCitiesAsync()
        {
            var cities = await _unitOfWork.Cities.GetAllAsync();
            return _mapper.Map<IReadOnlyList<CityDto>>(cities);
        }

        public Task<IReadOnlyList<CityDto>> GetCitiesByCountryIdAsync(Guid countryId)
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<CityDto>> GetCitiesByStateIdAsync(Guid stateId)
        {
            // Verify that the state exists first
            //var state = await _unitOfWork.States.GetByIdAsync(stateId)
            //    ?? throw new KeyNotFoundException($"State with ID {stateId} not found");

            // Get cities by state ID using the repository method
            var cities = await _unitOfWork.Cities.GetCitiesByStateIdAsync(stateId);

            // Map the entities to DTOs and return
            return _mapper.Map<IReadOnlyList<CityDto>>(cities);
        }

        public async Task<CityDto> GetCityByIdAsync(Guid id)
        {
            var city = await _unitOfWork.Cities.GetByIdAsync(id);

            return city == null ? throw new KeyNotFoundException($"City with ID {id} not found")
                : _mapper.Map<CityDto>(city);
        }

        public Task<CityDto> GetCityWithStateAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateCityAsync(Guid id, UpdateCityDto updateCityDto)
        {
            var city = await _unitOfWork.Cities.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"City with ID {id} not found");

            // Check if Name is null before calling ValidationService
            if (updateCityDto.Name != null &&
                !ValidationService.IsValidNameorCode(updateCityDto.Name, out var errorMessage))
            {
                throw new ValidationException(errorMessage);
            }

            // Check for duplicates if name is being updated
            if (updateCityDto.Name != null && updateCityDto.Name != city.Name)
            {
                bool isNameUnique = await _unitOfWork.Cities.IsNameUniqueInStateAsync(updateCityDto.Name, updateCityDto.StateId, id);
                if (!isNameUnique)
                {
                    throw new DuplicateResourceException($"A city with the name '{updateCityDto.Name}' already exists in the selected state.");
                }
            }

            // If state is changing, also check uniqueness in the new state
            if (updateCityDto.StateId != city.StateId)
            {
                // Ensure city.Name is not null before passing it
                if (string.IsNullOrEmpty(city.Name))
                {
                    throw new InvalidOperationException("City name cannot be null when checking for uniqueness in the new state.");
                }

                bool isNameUniqueInNewState = await _unitOfWork.Cities.IsNameUniqueInStateAsync(city.Name, updateCityDto.StateId);
                if (!isNameUniqueInNewState)
                {
                    throw new DuplicateResourceException($"A city with the name '{city.Name}' already exists in the destination state.");
                }
            }

            _mapper.Map(updateCityDto, city);
            city.ModifiedOn = DateTime.Now;

            // Set the modifier information
            if (_currentUserService.IsAuthenticated)
            {
                city.ModifiedBy = _currentUserService.UserId ?? _currentUserService.Email;
            }

            await _unitOfWork.Cities.UpdateAsync(city);
            await _unitOfWork.CompleteAsync();
        }

        //public async Task<IReadOnlyList<CityDto>> GetCitiesByStateIdAsync(Guid stateId)
        //{
        //    var cities = await _unitOfWork.Cities.GetCitiesByStateIdAsync(stateId);
        //    return _mapper.Map<IReadOnlyList<CityDto>>(cities);
        //}

        //public Task<IReadOnlyList<CityDto>> GetCitiesByCountryIdAsync(Guid countryId)
        //{
        //    var cities = await _unitOfWork.Cities.GetCitiesByCountryIdAsync(countryId);
        //    return _mapper.Map<IReadOnlyList<CityDto>>(cities);
        //}

        //public async Task<CityDto> GetCityWithStateAsync(Guid id)
        //{
        //    var city = await _unitOfWork.Cities.GetCityWithStatesAsync(id);
        //    if (city == null)
        //        throw new KeyNotFoundException($"City with ID {id} not found");
        //    return _mapper.Map<CityDto>(city);
        //}
    }
}
