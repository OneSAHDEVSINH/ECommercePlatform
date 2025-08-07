using CSharpFunctionalExtensions;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces.IRepositories;
using ECommercePlatform.Application.Models;
using ECommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommercePlatform.Infrastructure.Repositories
{
    public class UserRoleRepository(AppDbContext context) : GenericRepository<UserRole>(context), IUserRoleRepository
    {
        public async Task<List<UserRole>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                    .ThenInclude(r => r!.RolePermissions)
                        .ThenInclude(rp => rp.Module)
                        .AsSplitQuery()
                .Include(ur => ur.User)
                .AsSplitQuery()
                .Where(ur => ur.UserId == userId && !ur.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<UserRole>> GetByRoleIdAsync(Guid roleId)
        {
            return await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .Where(ur => ur.RoleId == roleId && !ur.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task DeleteByUserIdAsync(Guid userId)
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .AsNoTracking()
                .ToListAsync();

            _context.UserRoles.RemoveRange(userRoles);
            await _context.SaveChangesAsync();
        }

        // Search function for user roles
        private static IQueryable<UserRole> ApplyUserRoleSearch(IQueryable<UserRole> query, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return query;

            var searchTerm = searchText.ToLower();
            return query.Where(ur =>
                (ur.User != null && ur.User.FirstName != null &&
                    EF.Functions.Like(ur.User.FirstName.ToLower(), $"%{searchTerm}%")) ||
                (ur.User != null && ur.User.LastName != null &&
                    EF.Functions.Like(ur.User.LastName.ToLower(), $"%{searchTerm}%")) ||
                (ur.User != null && ur.User.Email != null &&
                    EF.Functions.Like(ur.User.Email.ToLower(), $"%{searchTerm}%")) ||
                (ur.Role != null && ur.Role.Name != null &&
                    EF.Functions.Like(ur.Role.Name.ToLower(), $"%{searchTerm}%")));
        }

        // Get paginated user roles with filters
        public async Task<PagedResponse<UserRole>> GetPagedUserRolesAsync(
            PagedRequest request,
            Guid? userId = null,
            Guid? roleId = null,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            // Create base filter based on provided parameters
            Expression<Func<UserRole, bool>> baseFilter;

            if (userId.HasValue && roleId.HasValue)
            {
                baseFilter = activeOnly
                    ? ur => ur.IsActive && !ur.IsDeleted && ur.UserId == userId.Value && ur.RoleId == roleId.Value
                    : ur => !ur.IsDeleted && ur.UserId == userId.Value && ur.RoleId == roleId.Value;
            }
            else if (userId.HasValue)
            {
                baseFilter = activeOnly
                    ? ur => ur.IsActive && !ur.IsDeleted && ur.UserId == userId.Value
                    : ur => !ur.IsDeleted && ur.UserId == userId.Value;
            }
            else if (roleId.HasValue)
            {
                baseFilter = activeOnly
                    ? ur => ur.IsActive && !ur.IsDeleted && ur.RoleId == roleId.Value
                    : ur => !ur.IsDeleted && ur.RoleId == roleId.Value;
            }
            else
            {
                baseFilter = activeOnly
                    ? ur => ur.IsActive && !ur.IsDeleted
                    : ur => !ur.IsDeleted;
            }

            // Define a search function that includes related entities
            static IQueryable<UserRole> searchWithInclude(IQueryable<UserRole> query, string? searchText)
            {
                // First include related entities
                var queryWithInclude = query
                    .Include(ur => ur.User)
                    .Include(ur => ur.Role);

                // Then apply search if text is provided
                if (!string.IsNullOrWhiteSpace(searchText))
                    return ApplyUserRoleSearch(queryWithInclude, searchText);

                return queryWithInclude;
            }

            // Use the generic paging method
            return await GetPagedAsync(
                request,
                baseFilter,
                searchWithInclude,
                cancellationToken);
        }

        // Get paginated user role DTOs
        public async Task<PagedResponse<UserRoleDto>> GetPagedUserRoleDtosAsync(
            PagedRequest request,
            Guid? userId = null,
            Guid? roleId = null,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var pagedEntities = await GetPagedUserRolesAsync(
                request,
                userId,
                roleId,
                activeOnly,
                cancellationToken);

            // Map entities to DTOs with conversion
            var dtos = pagedEntities.Items.Select(ur => new UserRoleDto
            {
                UserId = ur.UserId,
                RoleId = ur.RoleId,
                UserName = $"{ur.User?.FirstName} {ur.User?.LastName}",
                UserEmail = ur.User?.Email,
                RoleName = ur.Role?.Name,
                IsActive = ur.IsActive,
                CreatedOn = ur.CreatedOn,
                CreatedBy = ur.CreatedBy
            }).ToList();

            return new PagedResponse<UserRoleDto>
            {
                Items = dtos,
                TotalCount = pagedEntities.TotalCount,
                PageNumber = pagedEntities.PageNumber,
                PageSize = pagedEntities.PageSize
            };
        }
    }
}