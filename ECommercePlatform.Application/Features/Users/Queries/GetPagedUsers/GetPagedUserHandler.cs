using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Interfaces;
using ECommercePlatform.Application.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommercePlatform.Application.Features.Users.Queries.GetPagedUsers
{
    public class GetPagedUsersHandler(IUnitOfWork unitOfWork, ILogger<GetPagedUsersHandler> logger) : IRequestHandler<GetPagedUsersQuery, AppResult<PagedResponse<UserDto>>>
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<AppResult<PagedResponse<UserDto>>> Handle(GetPagedUsersQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var pagedResponse = await _unitOfWork.Users.GetPagedUserDtosAsync(
                    request,
                    request.ActiveOnly,
                    request.IncludeRoles,
                    request.RoleId,
                    cancellationToken);

                return AppResult<PagedResponse<UserDto>>.Success(pagedResponse);
            }
            catch (Exception ex)
            {
                return AppResult<PagedResponse<UserDto>>.Failure($"An error occurred: {ex.Message}");
            }
        }
    }
}