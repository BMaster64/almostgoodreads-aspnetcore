namespace BookReviewWeb.Pages.Shared
{
    public class PaginationModel
    {
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public string QueryParams { get; set; } = string.Empty;

        public string GetPageUrl(int pageNumber)
        {
            // Start with the page number parameter
            string url = $"?PageNumber={pageNumber}";

            // Append other query parameters if they exist
            if (!string.IsNullOrEmpty(QueryParams))
            {
                url += "&" + QueryParams;
            }

            return url;
        }
    }
}
