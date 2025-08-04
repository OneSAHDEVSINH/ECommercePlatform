using ECommercePlatform.Application.Interfaces.IUserAuth;
using ECommercePlatform.Domain.Entities;
using ECommercePlatform.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ECommercePlatform.Infrastructure
{
    public class AppDbContext : IdentityDbContext<User, Role, Guid, IdentityUserClaim<Guid>, UserRole, IdentityUserLogin<Guid>, IdentityRoleClaim<Guid>, IdentityUserToken<Guid>>
    {
        private readonly ICurrentUserService? _currentUserService;

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService currentUserService)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        //public DbSet<User> Users { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }
        public DbSet<State> States { get; set; }
        public DbSet<Category> Categories { get; set; }
        //public DbSet<Role> Roles { get; set; }
        //public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<Module> Modules { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            //UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            //UpdateAuditFields();
            return base.SaveChanges();
        }

        private void UpdateAuditFields()
        {
            var now = DateTime.Now;
            var userId = _currentUserService?.IsAuthenticated ?? false
                ? _currentUserService.UserId ?? _currentUserService.Email ?? "system"
                : "system";

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.SetCreatedBy(userId);
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.SetModifiedBy(userId);
                }
            }

            // Handle Role entity separately as it doesn't inherit from BaseEntity
            foreach (var entry in ChangeTracker.Entries<Role>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.CreatedOn = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedBy = userId;
                    entry.Entity.ModifiedOn = now;
                }
            }

            // Handle User entity separately
            foreach (var entry in ChangeTracker.Entries<User>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.CreatedOn = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedBy = userId;
                    entry.Entity.ModifiedOn = now;
                }
            }

            // Handle UserRole entity separately
            foreach (var entry in ChangeTracker.Entries<UserRole>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.CreatedOn = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.ModifiedBy = userId;
                    entry.Entity.ModifiedOn = now;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Change default ASP.NET Core Identity table names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Role>().ToTable("Roles");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
            modelBuilder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
            modelBuilder.Entity<RolePermission>()
                .HasIndex(rp => new { rp.RoleId, rp.ModuleId, rp.IsDeleted })
                .HasFilter("[IsDeleted] = 0");

            modelBuilder.Entity<UserRole>()
                .HasIndex(ur => new { ur.UserId, ur.IsDeleted })
                .HasFilter("[IsDeleted] = 0");

            // Configure many-to-many relationships
            modelBuilder.Entity<UserRole>(userRole =>
            {
                userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

                userRole.HasOne(ur => ur.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(ur => ur.RoleId)
                    .IsRequired();

                userRole.HasOne(ur => ur.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired();
            });

            // Configure RolePermission
            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(rp => rp.Role)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(rp => rp.RoleId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Module)
                    .WithMany(m => m.RolePermissions)
                    .HasForeignKey(rp => rp.ModuleId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Ensure unique combination of Role and Module
                entity.HasIndex(rp => new { rp.RoleId, rp.ModuleId })
                    .IsUnique();
            });

            // Apply entity configurations
            ApplyEntityConfigurations(modelBuilder);

            // Seed data
            //SeedData(modelBuilder);
        }

        private static void ApplyEntityConfigurations(ModelBuilder modelBuilder)
        {
            // Configure entity relationships
            //ConfigureUserEntity(modelBuilder);
            ConfigureAddressEntity(modelBuilder);
            ConfigureCityEntity(modelBuilder);
            ConfigureCountryEntity(modelBuilder);
            ConfigureStateEntity(modelBuilder);
            ConfigureOrderEntity(modelBuilder);
            ConfigureProductEntity(modelBuilder);
            ConfigureCategoryEntity(modelBuilder);
            //ConfigureUserRoleRelationships(modelBuilder);
            ConfigureCouponEntity(modelBuilder);
            ConfigureOrderItemEntity(modelBuilder);
            ConfigureProductVariantEntity(modelBuilder);
            ConfigureModuleEntity(modelBuilder);
            // Add more configuration methods as needed
        }

        //private static void ConfigureUserEntity(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<User>(entity =>
        //    {
        //        entity.HasKey(e => e.Id);
        //        entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
        //        entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
        //        entity.Property(e => e.Email).IsRequired().HasMaxLength(254);
        //        entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(100);
        //        entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);

        //        entity.HasMany(u => u.Addresses)
        //            .WithOne(a => a.User)
        //            .HasForeignKey(a => a.UserId)
        //            .OnDelete(DeleteBehavior.Restrict);

        //        entity.HasMany(u => u.Orders)
        //            .WithOne(o => o.User)
        //            .HasForeignKey(o => o.UserId)
        //            .OnDelete(DeleteBehavior.Restrict);

        //        entity.HasMany(u => u.Reviews)
        //            .WithOne(r => r.User)
        //            .HasForeignKey(r => r.UserId)
        //            .OnDelete(DeleteBehavior.Restrict);
        //    });
        //}

        private static void ConfigureModuleEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Module>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Route).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Icon).HasMaxLength(50);

                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.Route).IsUnique();
            });
        }

        private static void ConfigureAddressEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Address>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Line1).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Line2).HasMaxLength(100);
                entity.Property(e => e.Line3).HasMaxLength(100);
                entity.Property(e => e.ZipCode).IsRequired().HasMaxLength(18);

                entity.HasOne(a => a.City)
                    .WithMany(c => c.Addresses)
                    .HasForeignKey(a => a.CityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.State)
                    .WithMany(s => s.Addresses)
                    .HasForeignKey(a => a.StateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Country)
                    .WithMany(c => c.Addresses)
                    .HasForeignKey(a => a.CountryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCityEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<City>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);

                entity.HasOne(c => c.State)
                    .WithMany(s => s.Cities)
                    .HasForeignKey(c => c.StateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCountryEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Country>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
            });
        }

        private static void ConfigureStateEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<State>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);

                entity.HasOne(s => s.Country)
                    .WithMany(c => c.States)
                    .HasForeignKey(s => s.CountryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureOrderEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.OrderNumber).IsRequired().HasMaxLength(50);
                entity.Property(e => e.SubTotal).HasPrecision(18, 2);
                entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
                entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
                entity.Property(e => e.ShippingAmount).HasPrecision(18, 2);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

                entity.HasOne(o => o.ShippingAddress)
                    .WithMany(sa => sa.Orders)
                    .HasForeignKey(o => o.ShippingAddressId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(o => o.Coupon)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(o => o.CouponId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureProductEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.SKU).IsRequired().HasMaxLength(50);

                entity.HasOne(p => p.Category)
                    .WithMany(c => c.Products)
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        private static void ConfigureCategoryEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);

                entity.HasOne(c => c.ParentCategory)
                    .WithMany(c => c.Subcategories)
                    .HasForeignKey(c => c.ParentCategoryId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        //private static void ConfigureUserRoleRelationships(ModelBuilder modelBuilder)
        //{
        //    //// Explicitly ignore the problematic property
        //    //modelBuilder.Entity<User>()
        //    //    .Ignore(u => u.Role);

        //    // Configure the many-to-many relationship through UserRole entity
        //    modelBuilder.Entity<UserRole>(entity =>
        //    {
        //        entity.HasKey(ur => ur.Id);

        //        entity.HasOne(ur => ur.User)
        //              .WithMany(u => u.UserRoles)
        //              .HasForeignKey(ur => ur.UserId)
        //              .OnDelete(DeleteBehavior.Cascade);

        //        entity.HasOne(ur => ur.Role)
        //              .WithMany(r => r.UserRoles)
        //              .HasForeignKey(ur => ur.RoleId)
        //              .OnDelete(DeleteBehavior.Cascade);
        //    });
        //}

        private static void ConfigureCouponEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Coupon>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Code).IsRequired();

                // Add precision/scale to decimal properties
                entity.Property(e => e.DiscountValue).HasPrecision(18, 2);
                entity.Property(e => e.MinimumValue).HasPrecision(18, 2);
                entity.Property(e => e.MaximumValue).HasPrecision(18, 2);
            });
        }

        private static void ConfigureOrderItemEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Add precision/scale to decimal properties
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.Discount).HasPrecision(18, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
            });
        }

        private static void ConfigureProductVariantEntity(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductVariant>(entity =>
            {
                entity.HasKey(e => e.Id);

                // Add precision/scale to decimal properties
                entity.Property(e => e.Price).HasPrecision(18, 2);
            });
        }

    //    private static void SeedData(ModelBuilder modelBuilder)
    //    {
    //        // Fixed IDs and date
    //        var adminUserId = Guid.Parse("E65A3A8A-2407-4965-9B71-B9A1D8E2C34F");
    //        var adminRoleId = Guid.Parse("D4DE1B4D-B43B-4A55-B47A-1E92E71C3143");
    //        var fixedDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    //        // Seed SuperAdmin role
    //        modelBuilder.Entity<Role>().HasData(
    //            new
    //            {
    //                Id = adminRoleId,
    //                Name = "SuperAdmin",
    //                NormalizedName = "SUPERADMIN",
    //                Description = "Super Administrator with all permissions",
    //                IsActive = true,
    //                CreatedBy = "System",
    //                CreatedOn = fixedDate,
    //                ModifiedBy = "System",
    //                ModifiedOn = fixedDate,
    //                IsDeleted = false,
    //                ConcurrencyStamp = "1c078e6d-2fb7-4a5d-b170-4a4eced5c4d5" // Fixed value
    //            }
    //        );


    //        // Seed SuperAdmin user with pre-computed password hash
    //        var superAdmin = new User
    //        {
    //            Id = adminUserId,
    //            UserName = "admin@admin.com",
    //            NormalizedUserName = "ADMIN@ADMIN.COM",
    //            Email = "admin@admin.com",
    //            NormalizedEmail = "ADMIN@ADMIN.COM",
    //            EmailConfirmed = true,
    //            FirstName = "Super",
    //            LastName = "Admin",
    //            Gender = Gender.Other,
    //            DateOfBirth = new DateOnly(1990, 1, 1),
    //            PhoneNumber = "1234567890",
    //            PhoneNumberConfirmed = true,
    //            Bio = "System Administrator",
    //            IsActive = true,
    //            CreatedBy = "System",
    //            CreatedOn = fixedDate,
    //            ModifiedBy = "System",
    //            ModifiedOn = fixedDate,
    //            IsDeleted = false,
    //            SecurityStamp = "f7426c48-c7c4-4c44-a30c-adcb4d1c8636",
    //            ConcurrencyStamp = "6e8b9d2c-79f8-4c0d-8c5b-b7e3d2e0fcc8",
    //            // Pre-computed password hash for "Admin@123" - fixed for seeding purposes only
    //            PasswordHash = "AQAAAAIAAYagAAAAEByw51YzuzngglBl/L2IhnXzbcYD217ogwcvO0NTporKYSbAUtKe7NOeNIHr4vzYKA=="
    //        };

    //        modelBuilder.Entity<User>().HasData(superAdmin);

    //        // Seed UserRole
    //        modelBuilder.Entity<UserRole>().HasData(
    //            new
    //            {
    //                UserId = adminUserId,
    //                RoleId = adminRoleId,
    //                IsActive = true,
    //                CreatedBy = "System",
    //                CreatedOn = fixedDate,
    //                ModifiedBy = "System",
    //                ModifiedOn = fixedDate,
    //                IsDeleted = false
    //            }
    //        );

    //        // Use fixed GUIDs for modules
    //        var moduleIds = new[]
    //        {
    //    Guid.Parse("9A3D7141-4E96-4A5A-B1C5-B6B757ABC0E7"), // Dashboard
    //    Guid.Parse("D5C05957-17E5-46F7-B32D-BE1D81E317AE"), // Users
    //    Guid.Parse("52136BB4-EE50-4175-9B40-836CAC5D587C"), // Roles
    //    Guid.Parse("4EA6364A-7F9E-4B3B-A138-BB9CAA2653A7"), // Modules
    //    Guid.Parse("F86A8772-D8E8-4256-9A17-453A9A65015F"), // Cities
    //    Guid.Parse("24203E63-035E-4119-9E6A-27B2CCBF5C79"), // States
    //    Guid.Parse("A095A01D-9E88-4570-BDFD-9E0BC05F14F4")  // Countries
    //};

    //        var modules = new[]
    //        {
    //    new { Id = moduleIds[0], Name = "Dashboard", Route = "dashboard", Description = "Main dashboard", DisplayOrder = 1, Icon = "fas fa-tachometer-alt" },
    //    new { Id = moduleIds[1], Name = "Users", Route = "users", Description = "User management", DisplayOrder = 2, Icon = "fas fa-users" },
    //    new { Id = moduleIds[2], Name = "Roles", Route = "roles", Description = "Role management", DisplayOrder = 3, Icon = "fas fa-user-shield" },
    //    new { Id = moduleIds[3], Name = "Modules", Route = "modules", Description = "Module management", DisplayOrder = 4, Icon = "fas fa-cubes" },
    //    new { Id = moduleIds[4], Name = "Cities", Route = "cities", Description = "City management", DisplayOrder = 5, Icon = "fas fa-city" },
    //    new { Id = moduleIds[5], Name = "States", Route = "states", Description = "State management", DisplayOrder = 6, Icon = "fas fa-map" },
    //    new { Id = moduleIds[6], Name = "Countries", Route = "countries", Description = "Country management", DisplayOrder = 7, Icon = "fas fa-globe" }
    //};

    //        // Fixed role permission GUIDs
    //        var permissionIds = new[]
    //        {
    //    Guid.Parse("E536A58D-09A3-4A69-8436-5725BF7FFBAD"), // Dashboard permission
    //    Guid.Parse("12D57212-45AC-4420-A202-7FF04DE2709E"), // Users permission
    //    Guid.Parse("4CF71F31-FB3F-4960-A352-4D29299B5D6F"), // Roles permission
    //    Guid.Parse("33EB3761-CE64-4720-9E0F-434887D40261"), // Modules permission
    //    Guid.Parse("D28F26B6-55D1-45A4-A69B-7D26A293D945"), // Cities permission
    //    Guid.Parse("86ACD9E0-16F1-474F-A85D-A4BDBBF27E8C"), // States permission
    //    Guid.Parse("B2CDAFC8-599B-45B5-AC32-FB84B0C896A5")  // Countries permission
    //};

    //        for (int i = 0; i < modules.Length; i++)
    //        {
    //            var module = modules[i];

    //            modelBuilder.Entity<Module>().HasData(new
    //            {
    //                module.Id,
    //                module.Name,
    //                module.Route,
    //                module.Description,
    //                module.DisplayOrder,
    //                module.Icon,
    //                IsActive = true,
    //                CreatedBy = "System",
    //                CreatedOn = fixedDate,
    //                ModifiedBy = "System",
    //                ModifiedOn = fixedDate,
    //                IsDeleted = false
    //            });

    //            // Seed RolePermissions with fixed GUIDs
    //            modelBuilder.Entity<RolePermission>().HasData(new
    //            {
    //                Id = permissionIds[i],
    //                RoleId = adminRoleId,
    //                ModuleId = module.Id,
    //                CanView = true,
    //                //CanAdd = true,
    //                //CanEdit = true,
    //                CanAddEdit = true,
    //                CanDelete = true,
    //                IsActive = true,
    //                CreatedBy = "System",
    //                CreatedOn = fixedDate,
    //                ModifiedBy = "System",
    //                ModifiedOn = fixedDate,
    //                IsDeleted = false
    //            });
    //        }
    //    }
    }
}