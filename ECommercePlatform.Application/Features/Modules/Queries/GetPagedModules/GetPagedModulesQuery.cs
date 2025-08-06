using ECommercePlatform.Application.Common.Models;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Models;
using MediatR;

namespace ECommercePlatform.Application.Features.Modules.Queries.GetPagedModules
{
    public record GetPagedModulesQuery : PagedRequest, IRequest<AppResult<PagedResponse<ModuleDto>>>
    {
        public bool ActiveOnly { get; set; } = true;
    }
}