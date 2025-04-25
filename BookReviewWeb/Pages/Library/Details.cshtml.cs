using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
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
        
        // Dictionary to store user review counts
        public Dictionary<int, int> UserReviewCounts { get; set; } = new Dictionary<int, int>();

        [BindProperty]
        public ReviewInputModel ReviewInput { get; set; }

        public class ReviewInputModel
        {
            public int BookId { get; set; }

            [Required(ErrorMessage = "Please select a rating")]
            [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
            public int Rating { get; set; }

            [Required(ErrorMessage = "Please enter a comment")]
            [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
            public string Comment { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Genre)
                .Include(b => b.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            Book = book;
            
            // Get user IDs from the reviews
            var userIds = Book.Reviews.Select(r => r.UserId).Distinct().ToList();
            
            // Query the database to get review counts for each user
            var userReviewCounts = await _context.Reviews
                .Where(r => userIds.Contains(r.UserId))
                .GroupBy(r => r.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
                
            // Populate the dictionary
            foreach (var item in userReviewCounts)
            {
                UserReviewCounts[item.UserId] = item.Count;
            }
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = Request.Path });
            }

            if (!ModelState.IsValid)
            {
                // Reload the book with related data
                await OnGetAsync(ReviewInput.BookId);
                return Page();
            }

            // Get the current user ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Check if the user has already reviewed this book
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookId == ReviewInput.BookId && r.UserId == userId);

            if (existingReview != null)
            {
                // Update existing review
                existingReview.Rating = ReviewInput.Rating;
                existingReview.Comment = ReviewInput.Comment;
                existingReview.CreatedAt = DateTime.Now; // Update timestamp
            }
            else
            {
                // Create a new review
                var newReview = new Review
                {
                    BookId = ReviewInput.BookId,
                    UserId = userId,
                    Rating = ReviewInput.Rating,
                    Comment = ReviewInput.Comment,
                    CreatedAt = DateTime.Now
                };

                _context.Reviews.Add(newReview);
            }

            await _context.SaveChangesAsync();

            // Redirect to the same page to see the new review
            return RedirectToPage(new { id = ReviewInput.BookId });
        }
    }
}
