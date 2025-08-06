using ECommercePlatform.API.Middleware;
using ECommercePlatform.API.Middleware.Authorization;
using ECommercePlatform.API.Swagger;
using ECommercePlatform.Application.Common;
using ECommercePlatform.Application.Common.Authorization.Policies;
using ECommercePlatform.Application.Common.Authorization.Requirements;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Application.Interfaces.IServices;
using ECommercePlatform.Application.Interfaces.IUserAuth;
using ECommercePlatform.Application.Mappings;
using ECommercePlatform.Application.Services;
using ECommercePlatform.Domain.Entities;
using ECommercePlatform.Infrastructure;
using ECommercePlatform.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.SuppressConsumesConstraintForFormFileParameters = true;
    options.SuppressInferBindingSourcesForParameters = true;
    options.SuppressModelStateInvalidFilter = true;
    options.SuppressMapClientErrors = true;
    options.ClientErrorMapping[StatusCodes.Status404NotFound].Link =
        "https://httpstatuses.com/404";
}).AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

// Register AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration
    .GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
    .EnableDetailedErrors()
    .EnableSensitiveDataLogging());

// Configure Identity
builder.Services.AddIdentity<User, Role>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Register MediatR, FluentValidation, and other application services
builder.Services.AddApplicationServices();

//Register repositories
//builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
//builder.Services.AddScoped<ICountryRepository, CountryRepository>();
//builder.Services.AddScoped<IUserRepository, UserRepository>();
//builder.Services.AddScoped<IStateRepository, StateRepository>();
//builder.Services.AddScoped<ICityRepository, CityRepository>();
//builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
//builder.Services.AddScoped<IRoleRepository, RoleRepository>();
//builder.Services.AddScoped<IRolePermissionRepository, RolePermissionRepository>();
//builder.Services.AddScoped<IModuleRepository, ModuleRepository>();

builder.Services.RegisterRepositories(); // Register all repositories using the extension method

// Register Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register services

builder.Services.AddScoped<IAuthService, IdentityAuthService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ISuperAdminService, SuperAdminService>();
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();

//JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    var jwtKey = builder.Configuration["Jwt:Key"] ??
    throw new InvalidOperationException("JWT key is not configured");
    //o.RequireHttpsMetadata = true;
    o.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // Allow HTTP in development
    o.SaveToken = true;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        //IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"] ?? "MyTemporarySecretKeyForDevelopment12345")),
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ECommercePlatform",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ECommerceUsers",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Register Authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, IdentityPermissionAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, AdminBypassHandler>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

// Configure Authorization
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Permission", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new PermissionRequirement("module", "action"));
    })
    .SetDefaultPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "JWTToken_Auth_API",
        Version = "v1",
        Description = "A clean architecture based ECommercePlatform API"
    });
    c.ResolveConflictingActions(
        apiDescriptions => apiDescriptions.First()
        );
    // Add schema filters to handle circular references
    c.CustomSchemaIds(type => type.FullName);
    c.OperationFilter<FileUploadOperationFilter>();
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddProblemDetails();

var app = builder.Build();

// Apply migrations at startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate(); // This will apply any pending migrations and create the database if it doesn't exist
        // Create super admin user with password
        var userManager = services.GetRequiredService<UserManager<User>>();
        var roleManager = services.GetRequiredService<RoleManager<Role>>();

        // Seed roles
        string[] roleNames = ["SuperAdmin", "Admin", "User"];
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new Role(roleName)
                {
                    Description = $"{roleName} role",
                    IsActive = true
                };
                await roleManager.CreateAsync(role);
            }
        }
        // Seed super admin user
        var adminEmail = "admin@admin.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                PhoneNumber = "1234567890",
                PhoneNumberConfirmed = true,
                IsActive = true,
                Gender = ECommercePlatform.Domain.Enums.Gender.Other,
                DateOfBirth = DateOnly.FromDateTime(DateTime.Now.AddYears(-30))
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
                Console.WriteLine("Super admin user created successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Check if admin user exists, if not create it
        var adminUser2 = await userManager.FindByEmailAsync("admin@admin.com");
        if (adminUser2 == null)
        {
            adminUser = new User
            {
                UserName = "admin@admin.com",
                Email = "admin@admin.com",
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                PhoneNumber = "1234567890",
                PhoneNumberConfirmed = true,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "SuperAdmin");
            }
        }

        Console.WriteLine("Database initialized and super admin created successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECommercePlatform API V1");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.MapStaticAssets();

app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAllOrigins");
// Use custom middleware
app.UseExceptionMiddleware();

app.UseAuthentication();
app.UseAuthorization();
app.UsePermissionMiddleware();
app.UseValidationMiddleware();
app.MapControllers();
app.MapFallbackToFile("/index.html");

await app.RunAsync();