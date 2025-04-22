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
    public class DetailsModel : PageModel
    {
        private readonly BookReviewWeb.Models.AlmostGoodReadsContext _context;

        public DetailsModel(BookReviewWeb.Models.AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public Book Book { get; set; } = default!;
        public double AverageRating => Book.Reviews.Any() ? Book.Reviews.Average(r => r.Rating ?? 0) : 0;
        public int ReviewCount => Book.Reviews.Count;
        public bool HasReviews => Book.Reviews.Any();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            Book = book;
            return Page();
        }
    }
}
