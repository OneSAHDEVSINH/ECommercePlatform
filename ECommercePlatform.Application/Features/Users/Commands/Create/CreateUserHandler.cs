using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Domain.Entities;
using ECommercePlatform.Domain.Enums;
using MediatR;

namespace ECommercePlatform.Application.Features.Users.Commands.Create
{
    public class CreateUserHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateUserCommand, AppResult<UserDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var uniqueResult = await _unitOfWork.Users.EnsureEmailAndPhoneAreUniqueAsync(request.Email, request.PhoneNumber!);
                if (uniqueResult.IsFailure)
                    return AppResult<UserDto>.Failure(uniqueResult.Error);

                // Parse gender
                Gender gender = Gender.Other;
                if (!string.IsNullOrEmpty(request.Gender))
                {
                    Enum.TryParse<Gender>(request.Gender, true, out gender);
                }

                // Parse date of birth
                DateOnly dateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-18));
                if (!string.IsNullOrEmpty(request.DateOfBirth))
                {
                    if (DateTime.TryParse(request.DateOfBirth, out var parsedDate))
                    {
                        dateOfBirth = DateOnly.FromDateTime(parsedDate);
                    }
                }

                // Create user with UserManager
                var user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Gender = gender,
                    DateOfBirth = dateOfBirth,
                    //Gender = request.Gender,
                    //DateOfBirth = request.DateOfBirth ?? DateOnly.FromDateTime(DateTime.Now.AddYears(-18)),
                    Bio = request.Bio,
                    IsActive = request.IsActive,
                    //CreatedBy = request.CreatedBy,
                    //CreatedOn = request.CreatedOn
                };

                var result = await _unitOfWork.UserManager.CreateAsync(user, request.Password);
                await _unitOfWork.Users.AddAsync(user);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return AppResult<UserDto>.Failure($"Failed to create user: {errors}");
                }

                // Assign roles if provided
                if (request.RoleIds != null && request.RoleIds.Count != 0)
                {
                    foreach (var roleId in request.RoleIds)
                    {
                        var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
                        if (role != null)
                        {
                            await _unitOfWork.UserManager.AddToRoleAsync(user, role.Name!);
                        }
                    }
                }

                // Reload user with roles
                var createdUser = await _unitOfWork.Users.GetByIdAsync(user.Id);
                if (createdUser == null)
                    return AppResult<UserDto>.Failure("User was created but could not be retrieved.");

                // Map to DTO without exposing password
                var userDto = new UserDto
                {
                    Id = createdUser.Id,
                    FirstName = createdUser.FirstName,
                    LastName = createdUser.LastName,
                    Email = createdUser.Email!,
                    PhoneNumber = createdUser.PhoneNumber,
                    Gender = createdUser.Gender,
                    DateOfBirth = createdUser.DateOfBirth,
                    Bio = createdUser.Bio,
                    IsActive = createdUser.IsActive,
                    CreatedOn = createdUser.CreatedOn,
                    Roles = new List<RoleDto>()
                };

                return AppResult<UserDto>.Success(userDto);
            }
            catch (Exception ex)
            {
                return AppResult<UserDto>.Failure($"An error occurred while creating the user: {ex.Message}");
            }
        }
    }
}