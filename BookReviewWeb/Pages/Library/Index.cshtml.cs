using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using BookReviewWeb.Models;

namespace BookReviewWeb.Pages.Library
{
    public class IndexModel : PageModel
    {
        private readonly BookReviewWeb.Models.AlmostGoodReadsContext _context;

        public IndexModel(BookReviewWeb.Models.AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public IList<Book> Book { get;set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "newest";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchType { get; set; } = "title";

        [BindProperty(SupportsGet = true)]
        public int? GenreFilter { get; set; }
        public int TotalBooks { get; set; }
        public int TotalPages { get; set; }
        public int BooksPerPage { get; set; } = 25; // 5x5 grid
        public List<Genre> Genres { get; set; } = new List<Genre>();
        public async Task OnGetAsync()
        {
            // Get base query with Genre included
            var booksQuery = _context.Books
                .Include(b => b.Genres)
                .Include(b => b.Reviews)
                .AsQueryable();

            Genres = await _context.Genres.OrderBy(g => g.GenreName).ToListAsync();
            // Apply genre filter if provided
            if (GenreFilter.HasValue && GenreFilter > 0)
            {
                booksQuery = booksQuery.Where(b => b.Genres.Any(g => g.GenreId == GenreFilter));
            }

            // Apply search if term provided
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                SearchTerm = SearchTerm.Trim();
                booksQuery = SearchType?.ToLower() switch
                {
                    "author" => booksQuery.Where(b => b.Author.Contains(SearchTerm)),
                    _ => booksQuery.Where(b => b.Title.Contains(SearchTerm)) // default to title search
                };
            }

            // Apply sorting
            booksQuery = SortOrder switch
            {
                "oldest" => booksQuery.OrderBy(b => b.PublishYear),
                "title" => booksQuery.OrderBy(b => b.Title),
                "author" => booksQuery.OrderBy(b => b.Author),
                "rating" => booksQuery.OrderByDescending(b => b.Reviews.Any() ? (double)b.Reviews.Average(r => r.Rating) : 0),
                _ => booksQuery.OrderByDescending(b => b.PublishYear) // default to newest
            };

            // Count total for pagination
            TotalBooks = await booksQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalBooks / (double)BooksPerPage);

            // Ensure valid page number
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Get the books for current page
            Book = await booksQuery
                .Skip((PageNumber - 1) * BooksPerPage)
                .Take(BooksPerPage)
                .ToListAsync();
        }

        // Helper method to get sort order display text
        public string GetSortOrderDisplayText()
        {
            return SortOrder switch
            {
                "oldest" => "Oldest First",
                "title" => "Title (A-Z)",
                "author" => "Author (A-Z)",
                _ => "Newest First"
            };
        }
    }
}
