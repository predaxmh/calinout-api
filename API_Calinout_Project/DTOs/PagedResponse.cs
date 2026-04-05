namespace API_Calinout_Project.DTOs
{
    public record PagedResponse<T>(
        List<T> Items,
        int TotalRecords,
        int PageNumber,
        int PageSize
    )
    {
        // Helpful metadata for the Flutter UI
        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }
}