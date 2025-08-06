using CSharpFunctionalExtensions;
using ECommercePlatform.Application.Common.Helpers;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces.IRepositories;
using ECommercePlatform.Application.Interfaces.IServices;
using ECommercePlatform.Application.Interfaces.IUserAuth;
using ECommercePlatform.Application.Models;
using ECommercePlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ECommercePlatform.Infrastructure.Repositories
{
    public class UserRepository(AppDbContext context, ISuperAdminService superAdminService, ICurrentUserService currentUserService) : GenericRepository<User>(context), IUserRepository
    {
        private readonly ISuperAdminService _superAdminService = superAdminService;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public async Task<User?> FindUserByEmailAsync(string email)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
        }

        public new async Task<User?> GetByIdAsync(Guid id) => await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r!.RolePermissions)
                            .ThenInclude(rp => rp.Module)
            .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);

        public new async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => !u.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersByRoleIdAsync(Guid roleId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                .Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId) && !u.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<User>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.IsActive && !u.IsDeleted)
                .AsNoTracking()
                .ToListAsync();
        }

        public Task<Result<(string normalizedEmail, string normalizedPhone)>> EnsureEmailAndPhoneAreUniqueAsync(string email, string phone, Guid? excludeId = null)
        {
            return Result.Success((email, phone))
                // Validate name is not empty
                .Ensure(tuple => !string.IsNullOrEmpty(tuple.email?.Trim()), "Email cannot be null or empty.")
                // Validate phone is not empty
                .Ensure(tuple => !string.IsNullOrEmpty(tuple.phone?.Trim()), "Phone cannot be null or empty.")
                // Normalize the inputs
                .Map(tuple => (
                    normalizedEmail: tuple.email.Trim().ToLower(),
                    normalizedPhone: tuple.phone.Trim().ToLower()
                ))
                // Check uniqueness against database
                .Bind(async tuple =>
                {
                    var emailQuery = _context.Users.Where(c =>
                        c.Email != null &&
                        c.Email.ToLower().Trim() == tuple.normalizedEmail &&
                        !c.IsDeleted);

                    var phoneQuery = _context.Users.Where(c =>
                        c.PhoneNumber != null &&
                        c.PhoneNumber.ToLower().Trim() == tuple.normalizedPhone &&
                        !c.IsDeleted);

                    // Apply ID exclusion if provided
                    if (excludeId.HasValue)
                    {
                        emailQuery = emailQuery.Where(c => c.Id != excludeId.Value);
                        phoneQuery = phoneQuery.Where(c => c.Id != excludeId.Value);
                    }

                    var emailExists = await emailQuery.AnyAsync();
                    var phoneExists = await phoneQuery.AnyAsync();

                    // Collect all uniqueness violations
                    var errors = new List<string>();

                    if (emailExists)
                        errors.Add($"email \"{email}\"");

                    if (phoneExists)
                        errors.Add($"phone number \"{phone}\"");

                    // Return failure with all collected errors if any exist
                    if (errors.Count > 0)
                        return Result.Failure<(string, string)>("User with " + string.Join(", ", errors) + " already exists.");

                    return Result.Success(tuple);
                });
        }

        // Search function for users
        private static IQueryable<User> ApplyUserSearch(IQueryable<User> query, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return query;

            var searchTerm = searchText.ToLower();
            return query.Where(u =>
                (u.FirstName != null && EF.Functions.Like(u.FirstName.ToLower(), $"%{searchTerm}%")) ||
                (u.LastName != null && EF.Functions.Like(u.LastName.ToLower(), $"%{searchTerm}%")) ||
                (u.Email != null && EF.Functions.Like(u.Email.ToLower(), $"%{searchTerm}%")) ||
                (u.PhoneNumber != null && EF.Functions.Like(u.PhoneNumber, $"%{searchTerm}%")));
        }

        // Get paginated users
        public async Task<PagedResponse<User>> GetPagedUsersAsync(
            PagedRequest request,
            bool activeOnly = true,
            bool includeRoles = true,
            Guid? roleId = null,
            CancellationToken cancellationToken = default)
        {
            // Create base filter
            Expression<Func<User, bool>> baseFilter;

            // Check if current user is the superadmin
            var currentUserIdStr = _currentUserService.UserId;
            var isSuperAdminViewing = false;

            if (!string.IsNullOrEmpty(currentUserIdStr) && Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                var currentUser = await _context.Users.FindAsync([currentUserId], cancellationToken: cancellationToken);
                if (currentUser != null)
                {
                    isSuperAdminViewing = _superAdminService.IsSuperAdminEmail(currentUser.Email!);
                }
            }

            // If not viewed by superadmin, exclude the superadmin account from results
            if (!isSuperAdminViewing)
            {
                var superAdminEmail = "admin@admin.com"; // This should match the configured superadmin email
                baseFilter = activeOnly
                    ? u => u.IsActive && !u.IsDeleted && u.Email != superAdminEmail
                    : u => !u.IsDeleted && u.Email != superAdminEmail;
            }

            if (roleId.HasValue)
            {
                baseFilter = activeOnly
                    ? u => u.IsActive && !u.IsDeleted && u.UserRoles.Any(ur => ur.RoleId == roleId.Value)
                    : u => !u.IsDeleted && u.UserRoles.Any(ur => ur.RoleId == roleId.Value);
            }
            else
            {
                baseFilter = activeOnly
                    ? u => u.IsActive && !u.IsDeleted
                    : u => !u.IsDeleted;
            }

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

            // Define a search function that also includes roles regardless of search text
            IQueryable<User> searchWithInclude(IQueryable<User> query, string? searchText)
            {
                // Always include related entities if includeRoles is true
                var queryWithInclude = includeRoles
                    ? query.Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .AsNoTracking()
                    : query;

                // Then apply search if text is provided
                if (!string.IsNullOrWhiteSpace(searchText))
                    return ApplyUserSearch(queryWithInclude, searchText);

                return queryWithInclude;
            }

            // Use the generic paging method
            return await GetPagedAsync(
                request,
                baseFilter,
                searchWithInclude,
                cancellationToken);
        }

        public async Task<PagedResponse<UserDto>> GetPagedUserDtosAsync(
            PagedRequest request,
            bool activeOnly = true,
            bool includeRoles = true,
            Guid? roleId = null,
            CancellationToken cancellationToken = default)
        {
            // DEBUGGING: Add logging here
            Console.WriteLine($"🔍 DEBUGGING: UserRepository.GetPagedUserDtosAsync called!");
            Console.WriteLine($"🔍 Stack Trace: {Environment.StackTrace}");

            var pagedEntities = await GetPagedUsersAsync(
                request,
                activeOnly,
                includeRoles,
                roleId,
                cancellationToken);

            // Explicitly load roles if we need them
            if (includeRoles)
            {
                var userIds = pagedEntities.Items.Select(u => u.Id).ToList();

                // Eagerly load all roles for these users to ensure they're properly populated
                var userRolesWithRoles = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .Where(ur => userIds.Contains(ur.UserId) && !ur.IsDeleted && ur.IsActive && ur.Role!.IsActive && !ur.Role.IsDeleted)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                // Group by user ID for easy lookup
                var userRolesLookup = userRolesWithRoles
                    .GroupBy(ur => ur.UserId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Create DTOs with explicit role loading
                var dtos = pagedEntities.Items.Select(user =>
                {
                    var dto = (UserDto)user;

                    // If roles should be included but are missing, use our lookup
                    if (userRolesLookup.TryGetValue(user.Id, out var userRoles) && (dto.Roles == null || dto.Roles.Count == 0))
                    {
                        dto = new UserDto
                        {
                            Id = user.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            Email = user.Email,
                            PhoneNumber = user.PhoneNumber,
                            Gender = user.Gender,
                            DateOfBirth = user.DateOfBirth,
                            Bio = user.Bio,
                            IsActive = user.IsActive,
                            CreatedOn = user.CreatedOn,
                            Roles = [.. userRoles
                                .Where(ur => ur.Role != null)
                                .Select(ur => (RoleDto)ur.Role!)]
                        };
                    }

                    return dto;
                }).ToList();

                return new PagedResponse<UserDto>
                {
                    Items = dtos,
                    TotalCount = pagedEntities.TotalCount,
                    PageNumber = pagedEntities.PageNumber,
                    PageSize = pagedEntities.PageSize
                };
            }
            else
            {
                // Basic mapping without roles
                var dtos = pagedEntities.Items.Select(u => (UserDto)u).ToList();
                return new PagedResponse<UserDto>
                {
                    Items = dtos,
                    TotalCount = pagedEntities.TotalCount,
                    PageNumber = pagedEntities.PageNumber,
                    PageSize = pagedEntities.PageSize
                };
            }
        }

        // Get paginated user list DTOs (simplified version for lists)
        public async Task<PagedResponse<UserListDto>> GetPagedUserListDtosAsync(
            PagedRequest request,
            bool activeOnly = true,
            CancellationToken cancellationToken = default)
        {
            var pagedEntities = await GetPagedUsersAsync(
                request,
                activeOnly,
                includeRoles: false,
                roleId: null, // Explicitly pass null for the roleId parameter
                cancellationToken);

            // Map entities to list DTOs
            var dtos = pagedEntities.Items.Select(u => (UserListDto)u).ToList();

            return new PagedResponse<UserListDto>
            {
                Items = dtos,
                TotalCount = pagedEntities.TotalCount,
                PageNumber = pagedEntities.PageNumber,
                PageSize = pagedEntities.PageSize
            };
        }
    }
}