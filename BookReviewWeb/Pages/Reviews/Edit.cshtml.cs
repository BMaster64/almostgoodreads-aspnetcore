using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Pages.Reviews
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public EditModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Review Review { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get the review with book information
            Review = await _context.Reviews
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (Review == null)
            {
                return NotFound();
            }

            // Get the current user ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Check if the user is authorized to edit this review
            if (Review.UserId != userId && !User.IsInRole("Admin"))
            {
                return RedirectToPage("/Auth/AccessDenied");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // Reload the book information if there's a validation error
                Review.Book = await _context.Books.FirstOrDefaultAsync(b => b.Id == Review.BookId);
                return Page();
            }

            // Get the current user ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Check if the user is authorized to edit this review
            var existingReview = await _context.Reviews.AsNoTracking().FirstOrDefaultAsync(r => r.Id == Review.Id);
            if (existingReview == null)
            {
                return NotFound();
            }

            if (existingReview.UserId != userId && !User.IsInRole("Admin"))
            {
                return RedirectToPage("/Auth/AccessDenied");
            }

            try
            {
                _context.Attach(Review).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewExists(Review.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            // Redirect to the book details page
            return RedirectToPage("/Library/Details", new { id = Review.BookId });
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.Id == id);
        }
    }
} 