using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Domain.Entities;
using MediatR;

namespace ECommercePlatform.Application.Features.Countries.Commands.Create;

public class CreateCountryHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateCountryCommand, AppResult<CountryDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<AppResult<CountryDto>> Handle(CreateCountryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await _unitOfWork.Countries
                    .EnsureNameAndCodeAreUniqueAsync(request.Name, request.Code)
                    .Bind(async tuple =>
                    {
                        var country = Country.Create(request.Name, request.Code);
                        country.IsActive = request.IsActive;
                        await _unitOfWork.Countries.AddAsync(country);
                        return Result.Success(country);
                    })
                    .Map(country => AppResult<CountryDto>.Success((CountryDto)country))
                    .Match(
                        success => success,
                        failure => AppResult<CountryDto>.Failure(failure)
                    );
        }
        catch (Exception ex)
        {
            return AppResult<CountryDto>.Failure($"An error occurred: {ex.Message}");
        }
    }
}