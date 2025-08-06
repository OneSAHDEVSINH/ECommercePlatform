using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommercePlatform.Application.Features.Roles.Queries.GetAllRoles
{
    public class GetAllRolesHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetAllRolesQuery, AppResult<List<RoleDto>>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult<List<RoleDto>>> Handle(GetAllRolesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var roles = request.ActiveOnly
                    ? await _unitOfWork.Roles.GetActiveRolesAsync()
                    : await _unitOfWork.Roles.GetAllAsync();

                var result = new List<RoleDto>();

                foreach (var role in roles)
                {
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

                    result.Add(new RoleDto
                    {
                        Id = role.Id,
                        Name = role.Name,
                        Description = role.Description,
                        IsActive = role.IsActive,
                        CreatedOn = role.CreatedOn,
                        Permissions = permissionsDto
                    });
                }

                return AppResult<List<RoleDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return AppResult<List<RoleDto>>.Failure($"An error occurred while retrieving roles: {ex.Message}");
            }
        }
    }
}