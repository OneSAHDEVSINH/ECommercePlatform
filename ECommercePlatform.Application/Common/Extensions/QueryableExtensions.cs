using ECommercePlatform.Application.Models;
using ECommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace ECommercePlatform.Application.Common.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<RolePermission> IncludeRelated(this IQueryable<RolePermission> query)
        {
            return query
                .Include(rp => rp.Role)
                .AsSplitQuery()
                .Include(rp => rp.Module)
                .AsSplitQuery()
                .AsNoTracking();
        }

        public static IQueryable<RolePermission> WhereActive(this IQueryable<RolePermission> query)
        {
            return query.Where(rp => rp.IsActive && !rp.IsDeleted);
        }

        public static IQueryable<RolePermission> WhereModuleActive(this IQueryable<RolePermission> query)
        {
            return query.Where(rp => rp.Module!.IsActive && !rp.Module.IsDeleted);
        }

        public static IQueryable<T> ApplyFilter<T>(
            this IQueryable<T> query,
            Expression<Func<T, bool>> filterExpression)
        {
            return query.Where(filterExpression);
        }

        public static IQueryable<T> ApplySearch<T>(
            this IQueryable<T> query,
            string searchText,
            params Expression<Func<T, string?>>[] propertySelectors)
        {
            if (string.IsNullOrWhiteSpace(searchText) || propertySelectors.Length == 0)
                return query;

            var searchTerm = searchText.ToLower();
            Expression? combinedExpression = null;

            var parameter = Expression.Parameter(typeof(T), "x");
            foreach (var selector in propertySelectors)
            {
                // Get the property from the selector
                if (selector.Body is not MemberExpression memberExpression) continue;

                // Build x => x.Property != null && x.Property.ToLower().Contains(searchTerm)
                var property = Expression.Property(parameter, memberExpression.Member.Name);
                var notNull = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));
                var toLower = Expression.Call(property, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
                var contains = Expression.Call(toLower, typeof(string).GetMethod("Contains", [typeof(string)])!, Expression.Constant(searchTerm));
                var condition = Expression.AndAlso(notNull, contains);

                // Combine with OR
                if (combinedExpression == null)
                {
                    combinedExpression = condition;
                }
                else
                {
                    combinedExpression = Expression.OrElse(combinedExpression, condition);
                }
            }

            if (combinedExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                return query.Where(lambda);
            }

            return query;
        }

        public static IQueryable<T> ApplyPaging<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize)
        {
            return query.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }

        public static IQueryable<T> ApplyOrder<T>(
            this IQueryable<T> query,
            string sortColumn,
            string? sortDirection,
            string defaultColumn = "Id")
        {
            if (string.IsNullOrEmpty(sortColumn))
                sortColumn = defaultColumn;

            var entityType = typeof(T);
            var propertyName = sortColumn.First().ToString().ToUpper() + sortColumn[1..];

            var sortProperty = entityType.GetProperty(
                propertyName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (sortProperty == null)
            {
                // Try to get the default property
                sortProperty = entityType.GetProperty(
                    defaultColumn,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (sortProperty == null)
                    return query;
            }

            var parameter = Expression.Parameter(entityType, "x");
            var property = Expression.Property(parameter, sortProperty);
            var lambda = Expression.Lambda(property, parameter);

            var methodName = sortDirection?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";
            var orderByMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2);
            var genericMethod = orderByMethod.MakeGenericMethod(entityType, sortProperty.PropertyType);

            return (IQueryable<T>)genericMethod.Invoke(null, [query, lambda])!;
        }

        //new

        public static async Task<PagedResponse<T>> ToPaginatedListAsync<T>(
            this IQueryable<T> source,
            PagedRequest options,
            CancellationToken cancellationToken = default)
        {
            // Get the total count before pagination
            var totalCount = await source.CountAsync(cancellationToken);

            // Apply sorting if specified
            source = ApplySorting(source, options);

            // Apply pagination
            var items = await source
                .Skip((options.PageNumber - 1) * options.PageSize)
                .Take(options.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResponse<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = options.PageNumber,
                PageSize = options.PageSize
            };
        }

        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> source, PagedRequest options)
        {
            if (string.IsNullOrWhiteSpace(options.SortColumn))
                return source;

            // Convert the first letter to uppercase for property matching
            string normalizedPropertyName = options.SortColumn.First().ToString().ToUpper() + options.SortColumn[1..];

            // Get property info
            var propertyInfo = typeof(T).GetProperty(
                normalizedPropertyName,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
                return source; // Property doesn't exist, return unsorted

            // Create expression for the property
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propertyInfo);
            var lambda = Expression.Lambda(property, parameter);

            // Determine sort direction
            string methodName = options.SortDirection?.ToLower() == "desc"
                ? "OrderByDescending"
                : "OrderBy";

            // Get the appropriate OrderBy method
            var orderByMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2);

            // Make it generic for the entity type and property type
            var genericMethod = orderByMethod.MakeGenericMethod(typeof(T), propertyInfo.PropertyType);

            // Execute the sorting
            return (IQueryable<T>)genericMethod.Invoke(null, [source, lambda])!;
        }

        public static IQueryable<T> ApplySearch<T>(this IQueryable<T> source, string? searchText, params string[] searchProperties)
        {
            if (string.IsNullOrWhiteSpace(searchText) || searchProperties.Length == 0)
                return source;

            var searchLower = searchText.ToLower();
            var parameter = Expression.Parameter(typeof(T), "x");

            // Build an OR expression for each property
            Expression? combinedExpression = null;

            foreach (var propertyName in searchProperties)
            {
                var property = typeof(T).GetProperty(propertyName,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null && property.PropertyType == typeof(string))
                {
                    var propExpression = Expression.Property(parameter, property);
                    var nullCheck = Expression.NotEqual(propExpression, Expression.Constant(null, typeof(string)));

                    // x => x.Property != null && x.Property.ToLower().Contains(searchLower)
                    var toLower = Expression.Call(propExpression,
                        typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);

                    var contains = Expression.Call(toLower,
                        typeof(string).GetMethod("Contains", [typeof(string)])!,
                        Expression.Constant(searchLower));

                    var propertyExpression = Expression.AndAlso(nullCheck, contains);

                    // Combine with OR
                    combinedExpression = combinedExpression == null
                        ? propertyExpression
                        : Expression.OrElse(combinedExpression, propertyExpression);
                }
            }

            if (combinedExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                return source.Where(lambda);
            }

            return source;
        }

        public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> source, Dictionary<string, object>? filters)
        {
            if (filters == null || filters.Count == 0)
                return source;

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? combinedExpression = null;

            foreach (var filter in filters)
            {
                // Normalize property name for matching
                var propertyName = filter.Key.First().ToString().ToUpper() + filter.Key[1..];

                var property = typeof(T).GetProperty(propertyName,
                    BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property != null)
                {
                    var propExpression = Expression.Property(parameter, property);

                    // Create the appropriate comparison based on property and value type
                    Expression comparison;

                    if (filter.Value == null)
                    {
                        comparison = Expression.Equal(propExpression, Expression.Constant(null, property.PropertyType));
                    }
                    else
                    {
                        // Try to convert the filter value to the property type
                        object? convertedValue;
                        try
                        {
                            convertedValue = Convert.ChangeType(filter.Value, property.PropertyType);
                        }
                        catch
                        {
                            // Skip this filter if conversion fails
                            continue;
                        }

                        comparison = Expression.Equal(
                            propExpression,
                            Expression.Constant(convertedValue, property.PropertyType));
                    }

                    // Combine with AND
                    combinedExpression = combinedExpression == null
                        ? comparison
                        : Expression.AndAlso(combinedExpression, comparison);
                }
            }

            if (combinedExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                return source.Where(lambda);
            }

            return source;
        }

        /// <summary>
        /// Applies pagination and returns a paged list
        /// </summary>
        public static async Task<PagedResponse<T>> ToPagedListAsync<T>(
            this IQueryable<T> query,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResponse<T>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Applies dynamic sorting based on property name and direction
        /// </summary>
        public static IOrderedQueryable<T> ApplyDynamicOrderBy<T>(
            this IQueryable<T> query,
            string propertyName,
            string direction = "asc")
        {
            // Handle empty property name
            if (string.IsNullOrWhiteSpace(propertyName))
                return query.OrderBy(x => 0); // Default ordering

            // Normalize property name (camelCase to PascalCase)
            var normalizedProperty = propertyName.First().ToString().ToUpper() + propertyName[1..];

            // Get property through reflection
            var propInfo = typeof(T).GetProperty(normalizedProperty,
                BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (propInfo == null)
                return query.OrderBy(x => 0); // Default ordering if property not found

            // Create parameter expression
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, propInfo);
            var lambda = Expression.Lambda(property, parameter);

            // Get appropriate method (OrderBy/OrderByDescending)
            string methodName = direction?.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";
            var orderByMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2);

            // Create generic method
            var genericMethod = orderByMethod.MakeGenericMethod(typeof(T), propInfo.PropertyType);

            // Invoke method and return result
            return (IOrderedQueryable<T>)genericMethod.Invoke(null, [query, lambda])!;
        }

        /// <summary>
        /// Applies text search to string properties
        /// </summary>
        public static IQueryable<T> ApplyTextSearch<T>(
            this IQueryable<T> query,
            string searchTerm,
            params Expression<Func<T, string?>>[] propertySelectors)
        {
            if (string.IsNullOrWhiteSpace(searchTerm) || propertySelectors.Length == 0)
                return query;

            var searchText = searchTerm.ToLower();
            Expression<Func<T, bool>> combinedFilter = _ => false; // Start with false for OR operations

            foreach (var selector in propertySelectors)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var memberExpr = (MemberExpression)selector.Body;

                // Build: x => x.Property != null && x.Property.ToLower().Contains(searchText)
                var property = Expression.Property(parameter, memberExpr.Member.Name);
                var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));

                var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes)!;
                var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;

                var toLower = Expression.Call(property, toLowerMethod);
                var contains = Expression.Call(toLower, containsMethod, Expression.Constant(searchText));

                var condition = Expression.AndAlso(nullCheck, contains);
                var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);

                // Add condition with OR
                combinedFilter = CombineWithOr(combinedFilter, lambda);
            }

            return query.Where(combinedFilter);
        }

        private static Expression<Func<T, bool>> CombineWithOr<T>(
            Expression<Func<T, bool>> expr1,
            Expression<Func<T, bool>> expr2)
        {
            // Replace parameter in second expression with parameter from first
            var parameter = expr1.Parameters[0];
            var visitor = new ParameterReplacer(expr2.Parameters[0], parameter);
            var body2 = visitor.Visit(expr2.Body);

            // Combine bodies with OrElse
            var newBody = Expression.OrElse(expr1.Body, body2!);
            return Expression.Lambda<Func<T, bool>>(newBody, parameter);
        }

        private class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam) : ExpressionVisitor
        {
            private readonly ParameterExpression _oldParam = oldParam;
            private readonly ParameterExpression _newParam = newParam;

            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _oldParam ? _newParam : node;
            }
        }
    }
}