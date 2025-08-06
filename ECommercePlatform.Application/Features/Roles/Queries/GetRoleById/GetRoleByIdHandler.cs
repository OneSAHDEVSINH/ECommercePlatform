using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommercePlatform.Application.Features.Roles.Queries.GetRoleById
{
    public class GetRoleByIdHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetRoleByIdQuery, AppResult<RoleDto>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult<RoleDto>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var role = await _unitOfWork.Roles.GetByIdAsync(request.Id);
                if (role == null)
                    return AppResult<RoleDto>.Failure($"Role with ID {request.Id} not found.");

                // Get role permissions with module info
                var rolePermissions = await _unitOfWork.RolePermissions.AsQueryable()
                    .Include(rp => rp.Module)
                    .Where(rp => rp.RoleId == role.Id && !rp.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                var permissionsDto = rolePermissions
                    .GroupBy(rp => new { rp.ModuleId, rp.Module?.Name })
                    .Select(g => new RoleModulePermissionDto
                    {
                        ModuleId = g.Key.ModuleId,
                        ModuleName = g.Key.Name,
                        CanView = g.First().CanView,
                        CanAddEdit = g.First().CanAddEdit,
                        CanDelete = g.First().CanDelete
                    })
                    .ToList();

                var roleDto = new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    IsActive = role.IsActive,
                    CreatedOn = role.CreatedOn,
                    Permissions = permissionsDto
                };

                return AppResult<RoleDto>.Success(roleDto);
            }
            catch (Exception ex)
            {
                return AppResult<RoleDto>.Failure($"An error occurred: {ex.Message}");
            }
        }
    }
}