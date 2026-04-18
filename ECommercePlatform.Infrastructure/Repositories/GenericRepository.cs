using ECommercePlatform.Application.Common.Extensions;
using ECommercePlatform.Application.Interfaces.IRepositories;
using ECommercePlatform.Application.Models;
using ECommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommercePlatform.Infrastructure.Repositories
{
    public class GenericRepository<T>(AppDbContext context) : IGenericRepository<T> where T : class
    {
        protected readonly AppDbContext _context = context;

        public async Task<T> AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            _context.Entry(entity).State = EntityState.Added;
            //await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(T entity)
        {
            _context.Set<T>().Update(entity);
            _context.Entry(entity).State = EntityState.Modified;
            //await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            _context.Set<T>().Remove(entity);
            _context.Entry(entity).State = EntityState.Deleted;
            //await _context.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<T>> GetAllAsync()
        {
            return await _context.Set<T>()
                .OrderBy(e => EF.Property<object>(e, "CreatedOn"))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IReadOnlyList<T>> GetActiveAsync()
        {
            return await _context.Set<T>()
                .Where(e => EF.Property<bool>(e, "IsActive"))
                .OrderBy(e => EF.Property<object>(e, "CreatedOn"))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<T> GetByIdAsync(Guid id)
        {
            var entity = await _context.Set<T>().FindAsync(id)
                ?? throw new InvalidOperationException($"Entity of type {typeof(T).Name} with ID {id} was not found.");
            //_context.Entry(entity).State = EntityState.Detached; // Detach the entity to avoid tracking issues
            return entity;
        }

        // Streamlined paging method with support for search
        public async Task<PagedResponse<T>> GetPagedAsync(
            PagedRequest request,
            Expression<Func<T, bool>>? baseFilter = null,
            Func<IQueryable<T>, string?, IQueryable<T>>? searchFunction = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Start with base query
                var query = _context.Set<T>().AsQueryable();

                // Apply base filter if provided
                if (baseFilter != null)
                    query = query.Where(baseFilter);

                // Apply date range filter if entity is BaseEntity type
                if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
                {
                    // Apply start date filter if provided
                    if (request.StartDate.HasValue)
                    {
                        //var startDate = request.StartDate.Value;
                        //query = query.Where(e => EF.Property<DateTime>(e, nameof(BaseEntity.CreatedOn)) >= startDate);

                        var startDate = request.StartDate.Value.Date; // Use Date to ignore time component
                        query = query.Where(e => EF.Property<DateTime>(e, "CreatedOn").Date >= startDate);
                    }

                    // Apply end date filter if provided
                    if (request.EndDate.HasValue)
                    {
                        //// Add one day to include all records until the end of the day
                        //var endDate = request.EndDate.Value.AddDays(1);
                        //query = query.Where(e => EF.Property<DateTime>(e, nameof(BaseEntity.CreatedOn)) < endDate);

                        var endDate = request.EndDate.Value.Date.AddDays(1); // Include all records of the end date
                        query = query.Where(e => EF.Property<DateTime>(e, "CreatedOn") < endDate);
                    }
                }

                // Apply search if provided
                if (searchFunction != null && !string.IsNullOrWhiteSpace(request.SearchText))
                    query = searchFunction(query, request.SearchText);

                // Get total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply sorting or use default
                if (!string.IsNullOrWhiteSpace(request.SortColumn))
                    query = query.ApplyDynamicOrderBy(request.SortColumn, request.SortDirection ?? "asc");

                // Apply pagination
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Return paged response
                return new PagedResponse<T>
                {
                    Items = items,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetPagedAsync: {ex.Message}");
                throw;
            }
        }
    }
}