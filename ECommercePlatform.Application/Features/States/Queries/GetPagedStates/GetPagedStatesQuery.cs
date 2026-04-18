using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Models;
using MediatR;

namespace ECommercePlatform.Application.Features.States.Queries.GetPagedStates
{
    public record GetPagedStatesQuery : PagedRequest, IRequest<AppResult<PagedResponse<StateDto>>>
    {
        public Guid? CountryId { get; set; }
        public bool ActiveOnly { get; set; } = true;
    }
}
