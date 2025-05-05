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
        public double AverageRating => Book.Reviews.Any() ? Book.Reviews.Average(r => r.Rating) : 0;
        public int ReviewCount => Book.Reviews.Count;
        public bool HasReviews => Book.Reviews.Any();
        
        // Dictionary to store user review counts
        public Dictionary<int, int> UserReviewCounts { get; set; } = new Dictionary<int, int>();
        
        // Dictionary to store review votes
        private Dictionary<int, List<ReviewVote>> ReviewVotes { get; set; } = new Dictionary<int, List<ReviewVote>>();

        // Vote type constants
        public const int UPVOTE = 1;
        public const int DOWNVOTE = -1;  // Changed from 2 to -1

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
            
            // Load review votes
            var reviewIds = Book.Reviews.Select(r => r.Id).ToList();
            var votes = await _context.ReviewVotes
                .Where(rv => reviewIds.Contains(rv.ReviewId))
                .ToListAsync();
                
            // Group votes by review
            foreach (var reviewId in reviewIds)
            {
                ReviewVotes[reviewId] = votes.Where(v => v.ReviewId == reviewId).ToList();
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
        
        public async Task<IActionResult> OnPostVoteReviewAsync(int reviewId, int voteType)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = Request.Path });
            }
            
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Get the review to find its book ID for the redirect
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == reviewId);
                
            if (review == null)
            {
                return NotFound();
            }
            
            // Convert vote type from UI (1, 2) to database values (1, -1)
            int databaseVoteType = voteType == 1 ? UPVOTE : DOWNVOTE;
            
            // Check if the user has already voted on this review
            var existingVote = await _context.ReviewVotes
                .FirstOrDefaultAsync(rv => rv.ReviewId == reviewId && rv.UserId == userId);
                
            if (existingVote != null)
            {
                if (existingVote.VoteType == databaseVoteType)
                {
                    // If the user clicks the same vote type again, remove the vote (toggle off)
                    _context.ReviewVotes.Remove(existingVote);
                }
                else
                {
                    // Change vote type
                    existingVote.VoteType = databaseVoteType;
                    existingVote.CreatedAt = DateTime.Now;
                }
            }
            else
            {
                // Create a new vote
                var newVote = new ReviewVote
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    VoteType = databaseVoteType,
                    CreatedAt = DateTime.Now
                };
                
                _context.ReviewVotes.Add(newVote);
            }
            
            await _context.SaveChangesAsync();
            
            return RedirectToPage(new { id = review.BookId });
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
        
        // Get user's vote for a review (1 = upvote, -1 = downvote, 0 = no vote)
        public int GetUserVoteForReview(int reviewId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return 0;
            }
            
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Check if we have this review's votes loaded
            if (!ReviewVotes.ContainsKey(reviewId))
            {
                return 0;
            }
            
            var userVote = ReviewVotes[reviewId].FirstOrDefault(v => v.UserId == userId);
            if (userVote == null)
                return 0;
                
            // Convert database vote type to UI representation
            return userVote.VoteType == DOWNVOTE ? 2 : 1;
        }
        
        // Get count of votes by type for a review
        public int GetVoteCountForReview(int reviewId, int voteType)
        {
            // Check if we have this review's votes loaded
            if (!ReviewVotes.ContainsKey(reviewId))
            {
                return 0;
            }
            
            // Convert UI vote type to database value for counting
            int databaseVoteType = voteType == 1 ? UPVOTE : DOWNVOTE;
            
            return ReviewVotes[reviewId].Count(v => v.VoteType == databaseVoteType);
        }
    }
}
