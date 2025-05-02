using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public IndexModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public IList<Book> LatestBooks { get; set; } = default!;
        public IList<Review> LatestReviews { get; set; } = default!;

        public async Task OnGetAsync()
        {
            // Get 5 latest books
            LatestBooks = await _context.Books
                .Include(b => b.Genre)
                .OrderByDescending(b => b.PublishYear) // Assuming newer books have higher IDs
                .Take(5)
                .ToListAsync();

            // Get 5 latest reviews
            LatestReviews = await _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
    }
}