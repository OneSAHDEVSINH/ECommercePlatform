using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Helpers;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces.IRepositories;
using ECommercePlatform.Application.Models;
using ECommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommercePlatform.Infrastructure.Repositories
{
    public class RoleRepository(AppDbContext context) : GenericRepository<Role>(context), IRoleRepository
    {
        public new async Task<Role?> GetByIdAsync(Guid id)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Module)
                    .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        public new async Task<List<Role>> GetAllAsync()
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .Where(r => !r.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Role>> GetActiveRolesAsync()
        {
            return await _context.Roles
                .Where(r => r.IsActive && !r.IsDeleted)
                .OrderBy(r => r.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public IQueryable<Role> AsQueryable()
        {
            return _context.Roles.AsQueryable();
        }

        public async Task<Role?> GetRoleWithPermissionsAsync(Guid id)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Module)
                    .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        // Combined validation method that returns a Result object
        public Task<Result<string>> EnsureNameIsUniqueAsync(string name, Guid? excludeId = null)
        {
            return Result.Success(name)
                // Validate name is not empty
                .Ensure(n => !string.IsNullOrEmpty(n?.Trim()), "Role name cannot be null or empty.")
                // Normalize the input
                .Map(n => n.Trim().ToLower())
                // Check uniqueness against database
                .Bind(async normalizedName =>
                {
                    var query = _context.Roles.Where(r =>
                        r.Name != null &&
                        r.Name.ToLower().Trim() == normalizedName &&
                        !r.IsDeleted);

                    // Apply ID exclusion if provided
                    if (excludeId.HasValue)
                        query = query.Where(r => r.Id != excludeId.Value);

                    var exists = await query.AnyAsync();

                    return exists
                        ? Result.Failure<string>($"Role with name \"{name}\" already exists.")
                        : Result.Success(normalizedName);
                });
        }

        // Search function for roles
        private static IQueryable<Role> ApplyRoleSearch(IQueryable<Role> query, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return query;

            var searchTerm = searchText.ToLower();
            return query.Where(r =>
                (r.Name != null && EF.Functions.Like(r.Name.ToLower(), $"%{searchTerm}%")) ||
                (r.Description != null && EF.Functions.Like(r.Description.ToLower(), $"%{searchTerm}%")));
        }

        public async Task<Role?> GetRoleWithModulePermissionsAsync(Guid id)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Module)
                    .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted);
        }

        // Get paginated roles
        public async Task<PagedResponse<Role>> GetPagedRolesAsync(
    PagedRequest request,
    bool activeOnly = true,
    CancellationToken cancellationToken = default)
        {
            // Create base filter
            Expression<Func<Role, bool>> baseFilter = activeOnly
                ? r => r.IsActive && !r.IsDeleted
                : r => !r.IsDeleted;

            // Apply date filtering if provided
            if (request.StartDate.HasValue)
            {
                var startDate = request.StartDate.Value;
                baseFilter = baseFilter.And(m => m.CreatedOn >= startDate);
            }

            if (request.EndDate.HasValue)
            {
                var endDate = request.EndDate.Value;
                baseFilter = baseFilter.And(m => m.CreatedOn <= endDate);
            }

            // Define a search function that also includes permissions
            static IQueryable<Role> searchWithInclude(IQueryable<Role> query, string? searchText)
            {
                // First include related entities
                var queryWithInclude = query
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Module)
                        .AsNoTracking(); // Changed from rp.Permission

                // Then apply search if text is provided
                if (!string.IsNullOrWhiteSpace(searchText))
                    return ApplyRoleSearch(queryWithInclude, searchText);

                return queryWithInclude;
            }

            // Use the GetPagedAsync method
            return await GetPagedAsync(
                request,
                baseFilter,
                searchWithInclude,
                cancellationToken);
        }

        // Get paginated role DTOs
        public async Task<PagedResponse<RoleDto>> GetPagedRoleDtosAsync(
            PagedRequest request,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var pagedEntities = await GetPagedRolesAsync(
                request,
                activeOnly,
                cancellationToken);

            // Map entities to DTOs
            var dtos = pagedEntities.Items.Select(r => (RoleDto)r).ToList();

            return new PagedResponse<RoleDto>
            {
                Items = dtos,
                TotalCount = pagedEntities.TotalCount,
                PageNumber = pagedEntities.PageNumber,
                PageSize = pagedEntities.PageSize
            };
        }

        // Get paginated role list DTOs (simplified version for lists)
        public async Task<PagedResponse<RoleListDto>> GetPagedRoleListDtosAsync(
            PagedRequest request,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var pagedEntities = await GetPagedRolesAsync(
                request,
                activeOnly,
                cancellationToken);

            // Map entities to list DTOs
            var dtos = pagedEntities.Items.Select(r => (RoleListDto)r).ToList();

            return new PagedResponse<RoleListDto>
            {
                Items = dtos,
                TotalCount = pagedEntities.TotalCount,
                PageNumber = pagedEntities.PageNumber,
                PageSize = pagedEntities.PageSize
            };
        }
    }
}