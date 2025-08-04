using ECommercePlatform.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ECommercePlatform.Domain.Entities
{
    public class User : IdentityUser<Guid>
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public byte[]? Avatar { get; set; }
        public Gender Gender { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        //Navigation properties
        public virtual ICollection<Address>? Addresses { get; set; }
        public virtual ICollection<Order>? Orders { get; set; }
        public virtual ICollection<Review>? Reviews { get; set; }
        public virtual ICollection<UserRole> UserRoles { get; set; } = [];

        // Default parameterless constructor required by EF Core
        public User() { }

        // With method for creating modified copies
        public User With(
            string? firstName = null,
            string? lastName = null,
            Gender? gender = null,
            DateOnly? dateOfBirth = null,
            string? phoneNumber = null,
            string? email = null,
            string? bio = null,
            UserRole? role = null
        )
        {
            var user = new User
            {
                Id = this.Id,
                UserName = this.UserName,
                CreatedOn = this.CreatedOn,
                CreatedBy = this.CreatedBy,
                ModifiedOn = DateTime.Now,
                ModifiedBy = this.ModifiedBy,
                IsActive = this.IsActive,
                IsDeleted = this.IsDeleted,
                FirstName = firstName ?? this.FirstName,
                LastName = lastName ?? this.LastName,
                Gender = gender ?? this.Gender,
                DateOfBirth = dateOfBirth ?? this.DateOfBirth,
                PhoneNumber = phoneNumber ?? this.PhoneNumber,
                Email = email ?? this.Email,
                Bio = bio ?? this.Bio,
                Addresses = this.Addresses,
                Orders = this.Orders,
                Reviews = this.Reviews,
                UserRoles = this.UserRoles
            };

            if (role != null && !user.UserRoles.Any(r => r.RoleId == role.RoleId))
            {
                user.UserRoles.Add(role);
            }

            return user;
        }

        // Factory method for creating admin users
        public static User AdminCreate(
            Guid id,
            string firstName,
            string lastName,
            Gender gender,
            DateOnly dateOfBirth,
            string phoneNumber,
            string email,
            string bio,
            UserRole userRole,
            string createdBy,
            DateTime createdOn,
            bool isActive = true,
            bool isDeleted = false,
            string? modifiedBy = null,
            DateTime? modifiedOn = null
        )
        {
            var user = new User
            {
                Id = id,
                UserName = email, // Using email as username
                NormalizedUserName = email.ToUpper(),
                Email = email,
                NormalizedEmail = email.ToUpper(),
                FirstName = firstName,
                LastName = lastName,
                Gender = gender,
                DateOfBirth = dateOfBirth,
                PhoneNumber = phoneNumber,
                Bio = bio,
                IsActive = isActive,
                IsDeleted = isDeleted,
                CreatedBy = createdBy,
                CreatedOn = createdOn,
                ModifiedBy = modifiedBy ?? createdBy,
                ModifiedOn = modifiedOn ?? createdOn,
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            if (userRole != null)
            {
                user.UserRoles.Add(userRole);
            }

            return user;
        }
    }
}