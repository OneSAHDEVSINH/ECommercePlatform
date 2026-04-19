using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ECommercePlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class General : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("12d57212-45ac-4420-a202-7ff04de2709e"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("33eb3761-ce64-4720-9e0f-434887d40261"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("4cf71f31-fb3f-4960-a352-4d29299b5d6f"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("86acd9e0-16f1-474f-a85d-a4bdbbf27e8c"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("b2cdafc8-599b-45b5-ac32-fb84b0c896a5"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("d28f26b6-55d1-45a4-a69b-7d26a293d945"));

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumn: "Id",
                keyValue: new Guid("e536a58d-09a3-4a69-8436-5725bf7ffbad"));

            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "RoleId", "UserId" },
                keyValues: new object[] { new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143"), new Guid("e65a3a8a-2407-4965-9b71-b9a1d8e2c34f") });

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: new Guid("24203e63-035e-4119-9e6a-27b2ccbf5c79"));

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: new Guid("4ea6364a-7f9e-4b3b-a138-bb9caa2653a7"));

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: new Guid("52136bb4-ee50-4175-9b40-836cac5d587c"));

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: new Guid("9a3d7141-4e96-4a5a-b1c5-b6b757abc0e7"));

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: new Guid("a095a01d-9e88-4570-bdfd-9e0bc05f14f4"));

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: new Guid("d5c05957-17e5-46f7-b32d-be1d81e317ae"));

            migrationBuilder.DeleteData(
                table: "Modules",
                keyColumn: "Id",
                keyValue: new Guid("f86a8772-d8e8-4256-9a17-453a9a65015f"));

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("e65a3a8a-2407-4965-9b71-b9a1d8e2c34f"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Modules",
                columns: new[] { "Id", "CreatedBy", "CreatedOn", "Description", "DisplayOrder", "Icon", "IsActive", "IsDeleted", "ModifiedBy", "ModifiedOn", "Name", "Route" },
                values: new object[,]
                {
                    { new Guid("24203e63-035e-4119-9e6a-27b2ccbf5c79"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "State management", 6, "fas fa-map", true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "States", "states" },
                    { new Guid("4ea6364a-7f9e-4b3b-a138-bb9caa2653a7"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Module management", 4, "fas fa-cubes", true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Modules", "modules" },
                    { new Guid("52136bb4-ee50-4175-9b40-836cac5d587c"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Role management", 3, "fas fa-user-shield", true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Roles", "roles" },
                    { new Guid("9a3d7141-4e96-4a5a-b1c5-b6b757abc0e7"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Main dashboard", 1, "fas fa-tachometer-alt", true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Dashboard", "dashboard" },
                    { new Guid("a095a01d-9e88-4570-bdfd-9e0bc05f14f4"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Country management", 7, "fas fa-globe", true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Countries", "countries" },
                    { new Guid("d5c05957-17e5-46f7-b32d-be1d81e317ae"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "User management", 2, "fas fa-users", true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Users", "users" },
                    { new Guid("f86a8772-d8e8-4256-9a17-453a9a65015f"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "City management", 5, "fas fa-city", true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Cities", "cities" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "ConcurrencyStamp", "CreatedBy", "CreatedOn", "Description", "IsActive", "IsDeleted", "ModifiedBy", "ModifiedOn", "Name", "NormalizedName" },
                values: new object[] { new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143"), "1c078e6d-2fb7-4a5d-b170-4a4eced5c4d5", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Super Administrator with all permissions", true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SuperAdmin", "SUPERADMIN" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "AccessFailedCount", "Avatar", "Bio", "ConcurrencyStamp", "CreatedBy", "CreatedOn", "DateOfBirth", "Email", "EmailConfirmed", "FirstName", "Gender", "IsActive", "IsDeleted", "LastName", "LockoutEnabled", "LockoutEnd", "ModifiedBy", "ModifiedOn", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { new Guid("e65a3a8a-2407-4965-9b71-b9a1d8e2c34f"), 0, null, "System Administrator", "6e8b9d2c-79f8-4c0d-8c5b-b7e3d2e0fcc8", "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateOnly(1990, 1, 1), "admin@admin.com", true, "Super", 2, true, false, "Admin", false, null, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ADMIN@ADMIN.COM", "ADMIN@ADMIN.COM", "AQAAAAIAAYagAAAAELST0qdl0q97wkBBDJGfpJbVjWOLG22r8WQZlTKUeeoQQbPsQj0rr9bvBUEJnk9Blw==", "1234567890", true, "f7426c48-c7c4-4c44-a30c-adcb4d1c8636", false, "admin@admin.com" });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "Id", "CanAddEdit", "CanDelete", "CanView", "CreatedBy", "CreatedOn", "IsActive", "IsDeleted", "ModifiedBy", "ModifiedOn", "ModuleId", "RoleId" },
                values: new object[,]
                {
                    { new Guid("12d57212-45ac-4420-a202-7ff04de2709e"), true, true, true, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d5c05957-17e5-46f7-b32d-be1d81e317ae"), new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143") },
                    { new Guid("33eb3761-ce64-4720-9e0f-434887d40261"), true, true, true, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("4ea6364a-7f9e-4b3b-a138-bb9caa2653a7"), new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143") },
                    { new Guid("4cf71f31-fb3f-4960-a352-4d29299b5d6f"), true, true, true, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("52136bb4-ee50-4175-9b40-836cac5d587c"), new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143") },
                    { new Guid("86acd9e0-16f1-474f-a85d-a4bdbbf27e8c"), true, true, true, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("24203e63-035e-4119-9e6a-27b2ccbf5c79"), new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143") },
                    { new Guid("b2cdafc8-599b-45b5-ac32-fb84b0c896a5"), true, true, true, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("a095a01d-9e88-4570-bdfd-9e0bc05f14f4"), new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143") },
                    { new Guid("d28f26b6-55d1-45a4-a69b-7d26a293d945"), true, true, true, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("f86a8772-d8e8-4256-9a17-453a9a65015f"), new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143") },
                    { new Guid("e536a58d-09a3-4a69-8436-5725bf7ffbad"), true, true, true, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("9a3d7141-4e96-4a5a-b1c5-b6b757abc0e7"), new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143") }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId", "CreatedBy", "CreatedOn", "IsActive", "IsDeleted", "ModifiedBy", "ModifiedOn" },
                values: new object[] { new Guid("d4de1b4d-b43b-4a55-b47a-1e92e71c3143"), new Guid("e65a3a8a-2407-4965-9b71-b9a1d8e2c34f"), "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, false, "System", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
