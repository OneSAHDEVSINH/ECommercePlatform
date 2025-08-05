using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Domain.Enums;
using MediatR;

namespace ECommercePlatform.Application.Features.Users.Commands.Update
{
    public class UpdateUserHandler(IUnitOfWork unitOfWork) : IRequestHandler<UpdateUserCommand, AppResult<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult<UserDto>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Get user from UserManager
                var user = await _unitOfWork.UserManager.FindByIdAsync(request.Id.ToString());
                if (user == null)
                    return AppResult<UserDto>.Failure($"User with ID {request.Id} not found.");

                // Check uniqueness if changing
                if ((!string.IsNullOrEmpty(request.Email) && request.Email != user.Email) ||
                    (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber != user.PhoneNumber))
                {
                    var uniqueResult = await _unitOfWork.Users.EnsureEmailAndPhoneAreUniqueAsync(request.Email!, request.PhoneNumber!, request.Id);
                    if (uniqueResult.IsFailure)
                        return AppResult<UserDto>.Failure(uniqueResult.Error);

                    user.Email = request.Email;
                    user.UserName = request.Email;
                    user.PhoneNumber = request.PhoneNumber;
                }


                // Update user properties
                if (!string.IsNullOrEmpty(request.FirstName))
                    user.FirstName = request.FirstName;
                if (!string.IsNullOrEmpty(request.LastName))
                    user.LastName = request.LastName;

                // Parse and update gender
                if (!string.IsNullOrEmpty(request.Gender))
                {
                    if (Enum.TryParse<Gender>(request.Gender, true, out var gender))
                        user.Gender = gender;
                }

                // Parse and update date of birth
                if (!string.IsNullOrEmpty(request.DateOfBirth))
                {
                    if (DateTime.TryParse(request.DateOfBirth, out var dateTime))
                        user.DateOfBirth = DateOnly.FromDateTime(dateTime);
                }

                if (request.PhoneNumber != null)
                    user.PhoneNumber = request.PhoneNumber;
                if (request.Bio != null)
                    user.Bio = request.Bio;
                if (request.IsActive.HasValue)
                    user.IsActive = request.IsActive.Value;

                // Update user
                var updateResult = await _unitOfWork.UserManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                    return AppResult<UserDto>.Failure($"Failed to update user: {errors}");
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(request.Password))
                {
                    await _unitOfWork.UserManager.RemovePasswordAsync(user);
                    var passwordResult = await _unitOfWork.UserManager.AddPasswordAsync(user, request.Password);
                    if (!passwordResult.Succeeded)
                    {
                        var errors = string.Join(", ", passwordResult.Errors.Select(e => e.Description));
                        return AppResult<UserDto>.Failure($"Failed to update password: {errors}");
                    }
                }

                // Update roles if provided
                if (request.RoleIds != null)
                {
                    // Remove existing roles
                    var currentRoles = await _unitOfWork.UserManager.GetRolesAsync(user);
                    if (currentRoles.Any())
                    {
                        await _unitOfWork.UserManager.RemoveFromRolesAsync(user, currentRoles);
                    }

                    // Add new roles
                    foreach (var roleId in request.RoleIds)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
                        if (role != null)
                        {
                            await _unitOfWork.UserManager.AddToRoleAsync(user, role.Name!);
                        }
                    }
                }
                await _unitOfWork.Users.UpdateAsync(user);

                // Reload user with updated data
                var updatedUser = await _unitOfWork.Users.GetByIdAsync(user.Id);
                if (updatedUser == null)
                    return AppResult<UserDto>.Failure("User was updated but could not be retrieved.");

                // Map to DTO
                var userDto = (UserDto)updatedUser;
                //userDto.Password = ""; // Don't expose password

                return AppResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                return AppResult<UserDto>.Failure($"An error occurred while updating the user: {ex.Message}");
            }
        }
    }
}