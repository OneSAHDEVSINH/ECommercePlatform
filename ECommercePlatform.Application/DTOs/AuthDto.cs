namespace ECommercePlatform.Application.DTOs
{
    public record LoginDto
    {
        public string? Email { get; init; }
        public string? Password { get; init; }
    }

    public record AuthResultDto
    {
        public string? Token { get; init; }
        public UserDto? User { get; init; }
        public List<UserPermissionDto>? Permissions { get; init; }
    }

    public record UserPermissionDto
    {
        public required string ModuleName { get; init; }
        public bool CanView { get; init; }
        public bool CanAddEdit { get; init; }
        public bool CanDelete { get; init; }
    }
}