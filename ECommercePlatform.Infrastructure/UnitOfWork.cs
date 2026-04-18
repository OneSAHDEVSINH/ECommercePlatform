using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Application.Interfaces.IRepositories;
using ECommercePlatform.Application.Interfaces.IServices;
using ECommercePlatform.Application.Interfaces.IUserAuth;
using ECommercePlatform.Domain.Entities;
using ECommercePlatform.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ECommercePlatform.Infrastructure
{
    public class UnitOfWork(AppDbContext context, UserManager<User> UserManager, RoleManager<Role> RoleManager, SignInManager<User> SignInManager, ISuperAdminService superAdminService, ICurrentUserService currentUserService) : IUnitOfWork, IDisposable
    {
        private readonly AppDbContext _context = context;
        private ICountryRepository? _countryRepository;
        private IUserRepository? _userRepository;
        private IStateRepository? _stateRepository;
        private ICityRepository? _cityRepository;
        private IRolePermissionRepository? _rolePermissionRepository;
        private IModuleRepository? _moduleRepository;
        private IRoleRepository? _roleRepository;
        private IUserRoleRepository? _userRoleRepository;

        private readonly UserManager<User> _userManager = UserManager;
        private readonly RoleManager<Role> _roleManager = RoleManager;
        private readonly SignInManager<User> _signInManager = SignInManager;
        private readonly ISuperAdminService _superAdminService = superAdminService;
        private readonly ICurrentUserService _currentUserService = currentUserService;

        public ICountryRepository Countries => _countryRepository ??= new CountryRepository(_context);

        public IUserRepository Users => _userRepository ??= new UserRepository(_context, _superAdminService, _currentUserService);

        public IStateRepository States => _stateRepository ??= new StateRepository(_context);

        public ICityRepository Cities => _cityRepository ??= new CityRepository(_context);

        public IRolePermissionRepository RolePermissions => _rolePermissionRepository ??= new RolePermissionRepository(_context);

        public IModuleRepository Modules => _moduleRepository ??= new ModuleRepository(_context);

        public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);

        public IUserRoleRepository UserRoles => _userRoleRepository ??= new UserRoleRepository(_context);

        public DbContext DbContext => _context;


        public UserManager<User> UserManager => _userManager;
        public RoleManager<Role> RoleManager => _roleManager;
        public SignInManager<User> SignInManager => _signInManager;

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}