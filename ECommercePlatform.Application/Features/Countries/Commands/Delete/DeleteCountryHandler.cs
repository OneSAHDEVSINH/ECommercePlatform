using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.Interfaces;
using MediatR;

namespace ECommercePlatform.Application.Features.Countries.Commands.Delete
{
    public class DeleteCountryHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteCountryCommand, AppResult>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult> Handle(DeleteCountryCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await Result.Success(request.Id)
                .Bind(async id =>
                {
                    var country = await _unitOfWork.Countries.GetByIdAsync(id);
                    return country == null
                        ? Result.Failure<Domain.Entities.Country>($"Country with ID {id} not found.")
                        : Result.Success(country);
                })
                .Bind(async country =>
                {
                    var states = await _unitOfWork.States.GetByCountryIdAsync(country.Id);
                    return states.Count != 0
                        ? Result.Failure<Domain.Entities.Country>("Cannot delete country. It has associated states.")
                        : Result.Success(country);
                })
                .Tap(async country => await _unitOfWork.Countries
                .DeleteAsync(country))
                .Map(_ => AppResult.Success())
                .Match(
                    success => success,
                    failure => AppResult.Failure(failure)
                );

            }
            catch (Exception ex)
            {
                return AppResult.Failure(ex.Message);
            }
        }
    }
}