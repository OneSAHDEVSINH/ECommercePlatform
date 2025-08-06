using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Models;
using MediatR;

namespace ECommercePlatform.Application.Features.Users.Queries.GetPagedUsers
{
    public record GetPagedUsersQuery : PagedRequest, IRequest<AppResult<PagedResponse<UserDto>>>
    {
        public bool ActiveOnly { get; set; } = true;
        public Guid? RoleId { get; set; }
        public bool IncludeRoles { get; set; } = true;
    }
}