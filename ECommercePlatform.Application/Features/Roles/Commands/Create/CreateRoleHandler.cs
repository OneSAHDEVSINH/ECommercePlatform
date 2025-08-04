using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Domain.Entities;
using MediatR;

namespace ECommercePlatform.Application.Features.Roles.Commands.Create
{
    public class CreateRoleHandler(IUnitOfWork unitOfWork) : IRequestHandler<CreateRoleCommand, AppResult<RoleDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            try
            {
                return await _unitOfWork.Roles.EnsureNameIsUniqueAsync(request.Name)
            .Bind(_ =>
            {
                var role = Role.Create(
                    Guid.NewGuid(),
                    request.Name,
                    request.Description ?? string.Empty
                    );

                role.IsActive = request.IsActive;
                return Result.Success(role);
            })
            .Tap(async role => await _unitOfWork.Roles.AddAsync(role))
            .Bind(async role =>
            {
                if (request.Permissions == null || request.Permissions.Count == 0)
                    return Result.Success(role);

                foreach (var perm in request.Permissions)
                {
                    var module = await _unitOfWork.Modules.GetByIdAsync(perm.ModuleId);
                    if (module == null) continue;

                    var rolePermission = RolePermission.Create(
                        role.Id,
                        perm.ModuleId,
                        perm.CanView,
                        perm.CanAddEdit,
                        perm.CanDelete
                    );

                    //rolePermission.SetCreatedBy(request.CreatedBy ?? "system");
                    await _unitOfWork.RolePermissions.AddAsync(rolePermission);
                }

                return Result.Success(role);
            })
            .Bind(async role =>
            {
                var createdRole = await _unitOfWork.Roles.GetRoleWithPermissionsAsync(role.Id);
                return createdRole == null
                    ? Result.Failure<Role>("Role was created but could not be retrieved.")
                    : Result.Success(createdRole);
            })
            .Map(role => (RoleDto)role)
            .Map(roleDto => AppResult<RoleDto>.Success(roleDto))
            .Match(
                success => success,
                error => AppResult<RoleDto>.Failure(error)
            );

            }
            catch (Exception ex)
            {
                return AppResult<RoleDto>.Failure($"An error occurred while creating the role: {ex.Message}");
            }
        }
    }
}


//// Validate role name uniqueness
//var nameResult = await _unitOfWork.Roles.EnsureNameIsUniqueAsync(request.Name);
//if (nameResult.IsFailure)
//    return AppResult<RoleDto>.Failure(nameResult.Error);

//// Create the role entity
//var role = Role.Create(
//    Guid.NewGuid(),
//    request.Name,
//    request.Description ?? string.Empty,
//    request.CreatedBy ?? "system");

//role.IsActive = request.IsActive;

//await _unitOfWork.Roles.AddAsync(role);

//// Process permissions if provided
//if (request.Permissions != null && request.Permissions.Count != 0)
//{
//    foreach (var perm in request.Permissions)
//    {
//        // Verify module exists
//        var module = await _unitOfWork.Modules.GetByIdAsync(perm.ModuleId);
//        if (module == null) continue;

//        var rolePermission = RolePermission.Create(
//            role.Id,
//            perm.ModuleId,
//            perm.CanView,
//            perm.CanAddEdit,
//            perm.CanDelete
//        );

//        rolePermission.SetCreatedBy(request.CreatedBy ?? "system");
//        await _unitOfWork.RolePermissions.AddAsync(rolePermission);
//    }
//}

//// Reload the role with permissions for return
//var createdRole = await _unitOfWork.Roles.GetRoleWithPermissionsAsync(role.Id);
//if (createdRole == null)
//    return AppResult<RoleDto>.Failure("Role was created but could not be retrieved.");

//// Map to DTO using explicit operator
//var roleDto = (RoleDto)createdRole;

//return AppResult<RoleDto>.Success(roleDto);