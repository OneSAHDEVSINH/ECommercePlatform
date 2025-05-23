﻿namespace ECommercePlatform.Application.Common.Interfaces
{
    /// <summary>
    /// Marker interface for requests that require update audit fields (ModifiedBy, ModifiedOn)
    /// to be automatically populated.
    /// </summary>
    public interface IAuditableUpdateRequest
    {
        string? ModifiedBy { get; set; }
        DateTime ModifiedOn { get; set; }
    }
}
