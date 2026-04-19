using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Domain.Entities;
using MediatR;

namespace ECommercePlatform.Application.Features.Countries.Commands.Update;

public class UpdateCountryHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateCountryCommand, AppResult>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<AppResult> Handle(UpdateCountryCommand request, CancellationToken cancellationToken)
    {
        try
        {
            return await Result.Success(request)
                    .Bind(async req =>
                    {
                        var country = await _unitOfWork.Countries.GetByIdAsync(req.Id);
                        return country == null
                            ? Result.Failure<Country>($"Country with ID \"{req.Id}\" not found.")
                            : Result.Success(country);
                    })
                    .Bind(async country =>
                    {
                        var validationResult = await _unitOfWork.Countries.EnsureNameAndCodeAreUniqueAsync(
                        request.Name ?? string.Empty,
                        request.Code ?? string.Empty,
                        request.Id);

                        return validationResult.IsSuccess
                            ? Result.Success(country)
                            : Result.Failure<Country>(validationResult.Error);
                    })
                    .Tap(async country =>
                    {
                        country.Update(
                            request.Name ?? string.Empty,
                            request.Code ?? string.Empty
                        );
                        country.IsActive = request.IsActive;

                        await _unitOfWork.Countries.UpdateAsync(country);
                    })
                    .Map(_ => AppResult.Success())
                    .Match(
                        success => success,
                        error => AppResult.Failure(error)
                    );
        }
        catch (Exception ex)
        {
            return AppResult.Failure($"An error occurred: {ex.Message}");
        }
    }
}