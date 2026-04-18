using CSharpFunctionalExtensions;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces.IRepositories;
using ECommercePlatform.Application.Models;
using ECommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommercePlatform.Infrastructure.Repositories
{
    public class StateRepository(AppDbContext context) : GenericRepository<State>(context), IStateRepository
    {
        public Task<List<State>> GetByCountryIdAsync(Guid countryId)
        {
            return _context.States
                .Where(s => s.CountryId == countryId && !s.IsDeleted)
                .Include(s => s.Country)
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<State>> GetStatesByCountryIdAsync(Guid countryId)
        {
            return await _context.States
                .Where(s => s.CountryId == countryId && !s.IsDeleted)
                .Include(s => s.Country)
                .OrderBy(s => s.Name)
                .AsNoTracking()
                .ToListAsync();
        }

        // Combined implementation with optional excludeId parameter
        public Task<Result<(string normalizedName, string normalizedCode)>> EnsureNameAndCodeAreUniqueInCountryAsync(string name, string code, Guid countryId, Guid? excludeId = null)
        {
            return Result.Success((name, code))
                // Validate name is not empty
                .Ensure(tuple => !string.IsNullOrEmpty(tuple.name?.Trim()), "Name cannot be null or empty.")
                // Validate code is not empty
                .Ensure(tuple => !string.IsNullOrEmpty(tuple.code?.Trim()), "Code cannot be null or empty.")
                // Normalize the inputs
                .Map(tuple => (
                    normalizedName: tuple.name.Trim().ToLower(),
                    normalizedCode: tuple.code.Trim().ToLower()
                ))
                // Check uniqueness against database
                .Bind(async tuple =>
                {
                    var nameQuery = _context.States.Where(s =>
                        s.Name != null &&
                        s.Name.ToLower().Trim() == tuple.normalizedName &&
                        s.CountryId == countryId &&
                        !s.IsDeleted);

                    var codeQuery = _context.States.Where(s =>
                        s.Code != null &&
                        s.Code.ToLower().Trim() == tuple.normalizedCode &&
                        s.CountryId == countryId &&
                        !s.IsDeleted);

                    // Apply ID exclusion if provided
                    if (excludeId.HasValue)
                    {
                        nameQuery = nameQuery.Where(s => s.Id != excludeId.Value);
                        codeQuery = codeQuery.Where(s => s.Id != excludeId.Value);
                    }

                    var nameExists = await nameQuery.AnyAsync();
                    var codeExists = await codeQuery.AnyAsync();

                    // Collect all uniqueness violations
                    var errors = new List<string>();

                    if (nameExists)
                        errors.Add($"name \"{name}\"");

                    if (codeExists)
                        errors.Add($"code \"{code}\"");

                    // Return failure with all collected errors if any exist
                    if (errors.Count > 0)
                        return Result.Failure<(string, string)>("State with " + string.Join(", ", errors) + " already exists.");

                    return Result.Success(tuple);
                });
        }

        // Search function for states (searching by name and code)
        private static IQueryable<State> ApplyStateSearch(IQueryable<State> query, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return query;

            var searchTerm = searchText.ToLower();
            return query.Where(s =>
                (s.Name != null && EF.Functions.Like(s.Name.ToLower(), $"%{searchTerm}%")) ||
                (s.Code != null && EF.Functions.Like(s.Code.ToLower(), $"%{searchTerm}%")));
        }

        // Get paginated states with optional country filter
        public async Task<PagedResponse<State>> GetPagedStatesAsync(
            PagedRequest request,
            Guid? countryId = null,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            // Create base filter
            Expression<Func<State, bool>> baseFilter;

            if (countryId.HasValue)
            {
                baseFilter = activeOnly
                    ? s => s.IsActive && !s.IsDeleted && s.CountryId == countryId.Value
                    : s => !s.IsDeleted && s.CountryId == countryId.Value;
            }
            else
            {
                baseFilter = activeOnly
                    ? s => s.IsActive && !s.IsDeleted
                    : s => !s.IsDeleted;
            }

            // Define a search function that also includes the Country navigation property

            // Use the updated GetPagedAsync method with the correct parameter signature
            return await GetPagedAsync(
                request,
                baseFilter,
                (query, searchText) =>
            {
                // First include the Country
                var queryWithInclude = query.Include(s => s.Country).AsNoTracking();

                // Then apply search if text is provided
                if (!string.IsNullOrWhiteSpace(searchText))
                    return ApplyStateSearch(queryWithInclude, searchText);

                return queryWithInclude;
            },
                cancellationToken);
        }

        // Get paginated state DTOs
        public async Task<PagedResponse<StateDto>> GetPagedStateDtosAsync(
            PagedRequest request,
            Guid? countryId = null,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var pagedEntities = await GetPagedStatesAsync(
                request,
                countryId,
                activeOnly,
                cancellationToken);

            // Map entities to DTOs with country information
            var dtos = pagedEntities.Items.Select(c => (StateDto)c).ToList();

            return new PagedResponse<StateDto>
            {
                Items = dtos,
                TotalCount = pagedEntities.TotalCount,
                PageNumber = pagedEntities.PageNumber,
                PageSize = pagedEntities.PageSize
            };
        }
    }
}
