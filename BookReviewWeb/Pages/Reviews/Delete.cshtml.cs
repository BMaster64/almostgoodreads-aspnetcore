using System;
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
    public class DeleteModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public DeleteModel(AlmostGoodReadsContext context)
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

            // Get the review with book and user information
            Review = await _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (Review == null)
            {
                return NotFound();
            }

            // Get the current user ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Check if the user is authorized to delete this review
            if (Review.UserId != userId && !User.IsInRole("Admin"))
            {
                return RedirectToPage("/Auth/AccessDenied");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Review == null || Review.Id == 0)
            {
                return NotFound();
            }

            // Get the review
            var reviewToDelete = await _context.Reviews
                .Include(r => r.Book)
                .FirstOrDefaultAsync(r => r.Id == Review.Id);

            if (reviewToDelete == null)
            {
                return NotFound();
            }

            // Get the current user ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Check if the user is authorized to delete this review
            if (reviewToDelete.UserId != userId && !User.IsInRole("Admin"))
            {
                return RedirectToPage("/Auth/AccessDenied");
            }

            // Store the book ID before deletion
            var bookId = reviewToDelete.BookId;

            // Delete the review
            _context.Reviews.Remove(reviewToDelete);
            await _context.SaveChangesAsync();

            // Redirect to the book details page
            return RedirectToPage("/Library/Details", new { id = bookId });
        }
    }
} 