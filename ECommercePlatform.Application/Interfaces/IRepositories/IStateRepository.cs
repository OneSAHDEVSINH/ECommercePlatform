using CSharpFunctionalExtensions;
using ECommercePlatform.Application.DTOs;
using ECommercePlatform.Application.Models;
using ECommercePlatform.Domain.Entities;

namespace ECommercePlatform.Application.Interfaces.IRepositories
{
    public interface IStateRepository : IGenericRepository<State>
    {
        Task<List<State>> GetByCountryIdAsync(Guid countryId);
        Task<IReadOnlyList<State>> GetStatesByCountryIdAsync(Guid countryId);
        Task<Result<(string normalizedName, string normalizedCode)>> EnsureNameAndCodeAreUniqueInCountryAsync(string name, string code, Guid countryId, Guid? excludeId = null);

        // Add pagination methods
        Task<PagedResponse<State>> GetPagedStatesAsync(
            PagedRequest request,
            Guid? countryId = null,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

        Task<PagedResponse<StateDto>> GetPagedStateDtosAsync(
            PagedRequest request,
            Guid? countryId = null,
            bool activeOnly = true,
            CancellationToken cancellationToken = default);

    }
}
