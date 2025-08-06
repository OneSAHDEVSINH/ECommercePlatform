using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommercePlatform.Application.Features.Roles.Queries.GetRolesByModule
{
    public class GetRolesByModuleHandler(IUnitOfWork unitOfWork) : IRequestHandler<GetRolesByModuleQuery, AppResult<List<RoleDto>>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult<List<RoleDto>>> Handle(GetRolesByModuleQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Verify module exists
                var module = await _unitOfWork.Modules.GetByIdAsync(request.ModuleId);
                if (module == null)
                    return AppResult<List<RoleDto>>.Failure($"Module with ID {request.ModuleId} not found.");

                // Get role permissions for this module
                var rolePermissions = await _unitOfWork.RolePermissions.AsQueryable()
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Module)
                    .Where(rp => rp.ModuleId == request.ModuleId && !rp.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Get unique roles
                var roleIds = rolePermissions.Select(rp => rp.RoleId).Distinct().ToList();
                var roles = await _unitOfWork.Roles.AsQueryable()
                    .Where(r => roleIds.Contains(r.Id))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Filter by active if requested
                if (request.ActiveOnly)
                    roles = [.. roles.Where(r => r.IsActive && !r.IsDeleted)];

                // Map to DTOs
                var roleDtos = new List<RoleDto>();
                foreach (var role in roles)
                {
                    var dto = await BuildRoleDtoWithPermissions(role.Id, cancellationToken);
                    if (dto != null)
                        roleDtos.Add(dto);
                }

                return AppResult<List<RoleDto>>.Success(roleDtos);
            }
            catch (Exception ex)
            {
                return AppResult<List<RoleDto>>.Failure($"An error occurred: {ex.Message}");
            }
        }

        private async Task<RoleDto?> BuildRoleDtoWithPermissions(Guid roleId, CancellationToken cancellationToken)
        {
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role == null) return null;

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

            return new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedOn = role.CreatedOn,
                Permissions = permissionsDto
            };
        }
    }
}