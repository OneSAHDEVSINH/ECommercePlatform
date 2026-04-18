using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Domain.Entities;
using MediatR;

namespace ECommercePlatform.Application.Features.Users.Commands.Delete
{
    public class DeleteUserHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserCommand, AppResult>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await Result.Success(request.Id)
                    .Bind(async id =>
                    {
                        var user = await _unitOfWork.Users.GetByIdAsync(id);
                        return user == null
                            ? Result.Failure<User>($"User with ID {id} not found.")
                            : Result.Success(user);
                    })
                    .Tap(async user => await _unitOfWork.UserRoles.DeleteByUserIdAsync(user.Id))
                    .Tap(async user => await _unitOfWork.Users.DeleteAsync(user))
                    .Map(_ => AppResult.Success())
                    .Match(
                        success => success,
                        failure => AppResult.Failure(failure)
                    );
            }
            catch (Exception ex)
            {
                return AppResult.Failure($"An error occurred while deleting the user: {ex.Message}");
            }
        }
    }
}




//old method

//var user = await _unitOfWork.Users.GetByIdAsync(request.Id);
//if (user == null)
//    return AppResult.Failure($"User with ID {request.Id} not found.");

//// First delete associated user roles
//await _unitOfWork.UserRoles.DeleteByUserIdAsync(request.Id);

//// Then delete the user
//await _unitOfWork.Users.DeleteAsync(user);

//return AppResult.Success();

//soft delete

//using ECommercePlatform.Application.Common.Models;
//using ECommercePlatform.Application.Interfaces;
//using MediatR;

//namespace ECommercePlatform.Application.Features.Users.Commands.Delete
//{
//    public class DeleteUserHandler(IUnitOfWork unitOfWork) : IRequestHandler<DeleteUserCommand, AppResult>
//    {
//        private readonly IUnitOfWork _unitOfWork = unitOfWork;

//        public async Task<AppResult> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
//        {
//            try
//            {
//                var user = await _unitOfWork.UserManager.FindByIdAsync(request.Id.ToString());
//                if (user == null)
//                    return AppResult.Failure($"User with ID {request.Id} not found.");

//                // Soft delete - mark as deleted instead of actually deleting
//                user.IsDeleted = true;
//                user.IsActive = false;
//                user.ModifiedBy = "system"; // Should come from current user
//                user.ModifiedOn = DateTime.Now;

//                var result = await _unitOfWork.UserManager.UpdateAsync(user);
//                if (!result.Succeeded)
//                {
//                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
//                    return AppResult.Failure($"Failed to delete user: {errors}");
//                }

//                return AppResult.Success();
//            }
//            catch (Exception ex)
//            {
//                return AppResult.Failure($"An error occurred while deleting the user: {ex.Message}");
//            }
//        }
//    }
//}