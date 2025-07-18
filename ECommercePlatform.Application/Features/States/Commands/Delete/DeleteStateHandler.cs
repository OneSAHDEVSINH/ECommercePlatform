using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Domain.Entities;
using MediatR;

namespace ECommercePlatform.Application.Features.States.Commands.Delete
{
    public class DeleteStateHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteStateCommand, AppResult>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult> Handle(DeleteStateCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await Result.Success(request.Id)
                .Bind(async id =>
                {
                    var state = await _unitOfWork.States.GetByIdAsync(id);
                    return state == null
                        ? Result.Failure<State>($"State with ID {id} not found.")
                        : Result.Success(state);
                })
                .Bind(async state =>
                {
                    var cities = await _unitOfWork.Cities.GetByStateIdAsync(state.Id);
                    return cities.Count != 0
                        ? Result.Failure<State>("Cannot delete state. It has associated cities.")
                        : Result.Success(state);
                })
                .Tap(async state => await _unitOfWork.States.DeleteAsync(state))
                .Map(_ => AppResult.Success())
                .Match(
                    success => success,
                    failure => AppResult.Failure(failure)
                );
            }
            catch (Exception ex)
            {
                return AppResult.Failure($"An error occurred: {ex.Message}");
            }
        }
    }
}