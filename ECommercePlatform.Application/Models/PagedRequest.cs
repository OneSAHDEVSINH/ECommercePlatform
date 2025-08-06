using System.Text.Json.Serialization;

namespace ECommercePlatform.Application.Models
{
    public record PagedRequest
    {
        private const int MaxPageSize = 100;
        private const int DefaultPageSize = 10;

        private int _pageSize = DefaultPageSize;
        private int _pageNumber = 1;

        public int PageNumber
        {
            get => _pageNumber;
            set => _pageNumber = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? DefaultPageSize : value);
        }

        public string? SearchText { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }

        // Add date range properties for filtering
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Dictionary for dynamic filters - key is property name, value is filter value
        [JsonExtensionData]
        public Dictionary<string, object>? Filters { get; set; }
    }
}