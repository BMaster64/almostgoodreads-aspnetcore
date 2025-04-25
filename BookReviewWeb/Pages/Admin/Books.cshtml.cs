using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class BooksModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public BooksModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public IList<Book> Books { get; set; } = default!;
        public List<Genre> Genres { get; set; } = new List<Genre>();

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "title";

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? GenreFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalBooks { get; set; }
        public int TotalPages { get; set; }
        public int BooksPerPage { get; set; } = 10;

        [TempData]
        public string SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Load all genres for the filter dropdown
            Genres = await _context.Genres.OrderBy(g => g.GenreName).ToListAsync();

            // Start building the query
            var booksQuery = _context.Books
                .Include(b => b.Genre)
                .AsQueryable();

            // Apply genre filter if provided
            if (GenreFilter.HasValue && GenreFilter > 0)
            {
                booksQuery = booksQuery.Where(b => b.GenreId == GenreFilter);
            }

            // Apply search if term provided
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                SearchTerm = SearchTerm.Trim();
                booksQuery = booksQuery.Where(b => 
                    b.Title.Contains(SearchTerm) || 
                    b.Author.Contains(SearchTerm));
            }

            // Apply sorting
            booksQuery = SortOrder switch
            {
                "title_desc" => booksQuery.OrderByDescending(b => b.Title),
                "author" => booksQuery.OrderBy(b => b.Author),
                "newest" => booksQuery.OrderByDescending(b => b.PublishYear),
                "oldest" => booksQuery.OrderBy(b => b.PublishYear),
                _ => booksQuery.OrderBy(b => b.Title) // Default to title
            };

            // Count total for pagination
            TotalBooks = await booksQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalBooks / (double)BooksPerPage);

            // Ensure valid page number
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Get the books for current page
            Books = await booksQuery
                .Skip((PageNumber - 1) * BooksPerPage)
                .Take(BooksPerPage)
                .ToListAsync();
        }
    }
} 