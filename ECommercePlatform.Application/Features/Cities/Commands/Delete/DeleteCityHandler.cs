﻿using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.Interfaces;
using MediatR;

namespace ECommercePlatform.Application.Features.Cities.Commands.Delete
{
    public class DeleteCityHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteCityCommand, AppResult>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult> Handle(DeleteCityCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await Result.Success(request.Id)
                    // Find the city
                    .Bind(async id =>
                    {
                        var city = await _unitOfWork.Cities.GetByIdAsync(id);
                        return city == null
                            ? Result.Failure<Domain.Entities.City>($"City with ID {id} not found.")
                            : Result.Success(city);
                    })
                    // Delete the city
                    .Tap(async city => await _unitOfWork.Cities.DeleteAsync(city))
                    // Map to final result
                    .Map(_ => AppResult.Success());

                return result.IsSuccess
                    ? result.Value
                    : AppResult.Failure(result.Error);
            }
            catch (Exception ex)
            {
                return AppResult.Failure($"An error occurred: {ex.Message}");
            }
        }
    }
}

//Old method without using C Sharp Functional Extension

//var city = await _unitOfWork.Cities.GetByIdAsync(request.Id);
//if (city == null)
//    return AppResult.Failure($"City with ID {request.Id} not found");

//await _unitOfWork.Cities.DeleteAsync(city);
//return AppResult.Success();