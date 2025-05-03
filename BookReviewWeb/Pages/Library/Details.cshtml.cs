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
        public MyBook UserBookEntry { get; set; }

        public class ReviewInputModel
        {
            public int BookId { get; set; }

            [Required(ErrorMessage = "Please select a rating")]
            [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
            public int Rating { get; set; }

            [Required(ErrorMessage = "Please enter a comment")]
            [StringLength(10000, ErrorMessage = "Comment cannot exceed 10000 characters")]
            public string Comment { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Genres)
                .Include(b => b.Reviews)
                .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                return NotFound();
            }

            Book = book;
            // Check if the user is logged in and has a MyBook entry for this book
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                UserBookEntry = await _context.MyBooks
                    .FirstOrDefaultAsync(mb => mb.BookId == id && mb.UserId == userId);
            }
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
        public async Task<IActionResult> OnPostAddToMyBooksAsync(int bookId, int status)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = Request.Path });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var existingEntry = await _context.MyBooks
                .FirstOrDefaultAsync(mb => mb.BookId == bookId && mb.UserId == userId);

            if (existingEntry != null)
            {
                // Update status if already exists
                existingEntry.Status = status;
                existingEntry.DateAdded = DateTime.Now;
            }
            else
            {
                var newEntry = new MyBook
                {
                    UserId = userId,
                    BookId = bookId,
                    Status = status,
                    DateAdded = DateTime.Now
                };
                _context.MyBooks.Add(newEntry);
            }

            await _context.SaveChangesAsync();

            return RedirectToPage(new { id = bookId });
        }
        public async Task<IActionResult> OnPostRemoveFromMyBooksAsync(int bookId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = Request.Path });
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var existingEntry = await _context.MyBooks
                .FirstOrDefaultAsync(mb => mb.BookId == bookId && mb.UserId == userId);

            if (existingEntry != null)
            {
                _context.MyBooks.Remove(existingEntry);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { id = bookId });
        }
        public static string GetStatusName(int status)
        {
            return status switch
            {
                1 => "Plan to Read",
                2 => "Currently Reading",
                3 => "Dropped",
                4 => "Completed",
                _ => "Unknown"
            };
        }
    }
}
