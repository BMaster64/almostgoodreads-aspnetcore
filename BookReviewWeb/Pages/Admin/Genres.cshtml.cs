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
    public class GenresModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public GenresModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public IList<Genre> Genres { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalGenres { get; set; }
        public int TotalPages { get; set; }
        public int GenresPerPage { get; set; } = 20;

        [TempData]
        public string SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Start building the query
            var genresQuery = _context.Genres
                .Include(g => g.Books)
                .AsQueryable();

            // Apply search if term provided
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                SearchTerm = SearchTerm.Trim();
                genresQuery = genresQuery.Where(g => g.GenreName.Contains(SearchTerm));
            }

            // Apply sorting - alphabetical by name
            genresQuery = genresQuery.OrderBy(g => g.GenreName);

            // Count total for pagination
            TotalGenres = await genresQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalGenres / (double)GenresPerPage);

            // Ensure valid page number
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Get the genres for current page
            Genres = await genresQuery
                .Skip((PageNumber - 1) * GenresPerPage)
                .Take(GenresPerPage)
                .ToListAsync();
        }
    }
} 