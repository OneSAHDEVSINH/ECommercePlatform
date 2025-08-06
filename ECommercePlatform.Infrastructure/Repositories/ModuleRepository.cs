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
    public class ModuleRepository(AppDbContext context) : GenericRepository<Module>(context), IModuleRepository
    {
        public new async Task<Module?> GetByIdAsync(Guid id)
        {
            return await _context.Modules
                .Include(m => m.RolePermissions)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
        }

        public new async Task<List<Module>> GetAllAsync()
        {
            return await _context.Modules
                .Include(m => m.RolePermissions)
                .Where(m => !m.IsDeleted)
                .OrderBy(m => m.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Module>> GetActiveModulesAsync()
        {
            return await _context.Modules
                .Include(m => m.RolePermissions)
                .Where(m => m.IsActive && !m.IsDeleted)
                .OrderBy(m => m.DisplayOrder)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Module?> GetByRouteAsync(string route)
        {
            return await _context.Modules
                .Include(m => m.RolePermissions)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Route != null &&
                                         m.Route.ToLower() == route.ToLower() &&
                                         !m.IsDeleted);
        }

        public Task<Result<(string normalizedName, string normalizedRoute, string normalizedIcon, int normalizedDp)>> EnsureNameRouteIconDPAreUniqueAsync(string name, string route, string icon, int dp, Guid? excludeId = null)
        {
            return Result.Success((name, route, icon, dp))
                // Validate name is not empty
                .Ensure(tuple => !string.IsNullOrEmpty(tuple.name?.Trim()), "Name cannot be null or empty.")
                // Validate code is not empty
                .Ensure(tuple => !string.IsNullOrEmpty(tuple.route?.Trim()), "Route cannot be null or empty.")
                .Ensure(tuple => !string.IsNullOrEmpty(tuple.icon?.Trim()), "Icon cannot be null or empty.")
                .Ensure(tuple => tuple.dp >= 0, "Module display order must be a non-negative number.")
                // Normalize the inputs
                .Map(tuple => (
                    normalizedName: tuple.name.Trim().ToLower(),
                    normalizedRoute: tuple.route.Trim().ToLower(),
                    normalizedIcon: tuple.icon.Trim().ToLower(),
                    normalizedDp: tuple.dp
                ))
                // Check uniqueness against database
                .Bind(async tuple =>
                {
                    var nameQuery = _context.Modules.Where(m =>
                 m.Name != null &&
                 m.Name.ToLower().Trim() == tuple.normalizedName &&
                 !m.IsDeleted);

                    var routeQuery = _context.Modules.Where(m =>
                 m.Route != null &&
                 m.Route.ToLower().Trim() == tuple.normalizedRoute &&
                 !m.IsDeleted);

                    var iconQuery = _context.Modules.Where(m =>
                 m.Icon != null &&
                 m.Icon.ToLower().Trim() == tuple.normalizedIcon &&
                 !m.IsDeleted);

                    var dpQuery = _context.Modules.Where(m =>
                 m.DisplayOrder == tuple.normalizedDp &&
                 !m.IsDeleted);

                    // Apply ID exclusion if provided
                    if (excludeId.HasValue)
                    {
                        nameQuery = nameQuery.Where(c => c.Id != excludeId.Value);
                        routeQuery = routeQuery.Where(c => c.Id != excludeId.Value);
                        iconQuery = iconQuery.Where(c => c.Id != excludeId.Value);
                        dpQuery = dpQuery.Where(c => c.Id != excludeId.Value);
                    }

                    var nameExists = await nameQuery.AnyAsync();
                    var routeExists = await routeQuery.AnyAsync();
                    var iconExists = await iconQuery.AnyAsync();
                    var dpExists = await dpQuery.AnyAsync();

                    // Collect all uniqueness violations
                    var errors = new List<string>();

                    if (nameExists)
                        errors.Add($"name \"{name}\"");

                    if (routeExists)
                        errors.Add($"route \"{route}\"");

                    if (iconExists)
                        errors.Add($"icon \"{icon}\"");

                    if (dpExists)
                        errors.Add($"display order \"{dp}\"");

                    // Return failure with all collected errors if any exist
                    if (errors.Count > 0)
                        return Result.Failure<(string, string, string, int)>("Module with " + string.Join(", ", errors) + " already exists.");

                    return Result.Success(tuple);
                });
        }

        // Search function for modules
        private static IQueryable<Module> ApplyModuleSearch(IQueryable<Module> query, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return query;

            var searchTerm = searchText.ToLower();
            return query.Where(m =>
                (m.Name != null && EF.Functions.Like(m.Name.ToLower(), $"%{searchTerm}%")) ||
                (m.Description != null && EF.Functions.Like(m.Description.ToLower(), $"%{searchTerm}%")) ||
                (m.Route != null && EF.Functions.Like(m.Route.ToLower(), $"%{searchTerm}%")));
        }

        // Get paginated modules
        public async Task<PagedResponse<Module>> GetPagedModulesAsync(
            PagedRequest request,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            // Create base filter
            Expression<Func<Module, bool>> baseFilter = activeOnly
                ? m => m.IsActive && !m.IsDeleted
                : m => !m.IsDeleted;

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
            static IQueryable<Module> searchWithInclude(IQueryable<Module> query, string? searchText)
            {
                // First include related entities
                var queryWithInclude = query
                    .Include(m => m.RolePermissions)
                    .AsNoTracking();

                // Then apply search if text is provided
                if (!string.IsNullOrWhiteSpace(searchText))
                    return ApplyModuleSearch(queryWithInclude, searchText);

                return queryWithInclude;
            }

            // Use the generic paging method
            return await GetPagedAsync(
                request,
                baseFilter,
                searchWithInclude,
                cancellationToken);
        }

        // Get paginated module DTOs
        public async Task<PagedResponse<ModuleDto>> GetPagedModuleDtosAsync(
            PagedRequest request,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var pagedEntities = await GetPagedModulesAsync(
                request,
                activeOnly,
                cancellationToken);

            // Map entities to DTOs
            var dtos = pagedEntities.Items.Select(m => (ModuleDto)m).ToList();

            return new PagedResponse<ModuleDto>
            {
                Items = dtos,
                TotalCount = pagedEntities.TotalCount,
                PageNumber = pagedEntities.PageNumber,
                PageSize = pagedEntities.PageSize
            };
        }

        // Get paginated module list DTOs (simplified version for lists)
        public async Task<PagedResponse<ModuleListDto>> GetPagedModuleListDtosAsync(
            PagedRequest request,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var pagedEntities = await GetPagedModulesAsync(
                request,
                activeOnly,
                cancellationToken);

            // Map entities to list DTOs
            var dtos = pagedEntities.Items.Select(m => (ModuleListDto)m).ToList();

            return new PagedResponse<ModuleListDto>
            {
                Items = dtos,
                TotalCount = pagedEntities.TotalCount,
                PageNumber = pagedEntities.PageNumber,
                PageSize = pagedEntities.PageSize
            };
        }
    }
}