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
        public double AverageRating => Book.Reviews.Any() ? (double)Book.Reviews.Average(r => r.Rating) : 0;
        public int ReviewCount => Book.Reviews.Count;
        public bool HasReviews => Book.Reviews.Any();
        
        // Dictionary to store user review counts
        public Dictionary<int, int> UserReviewCounts { get; set; } = new Dictionary<int, int>();
        
        // Dictionary to store vote counts
        public Dictionary<int, (int Upvotes, int Downvotes)> ReviewVoteCounts { get; set; } = new Dictionary<int, (int, int)>();
        
        // Dictionary to store current user's votes
        public Dictionary<int, int> UserVotes { get; set; } = new Dictionary<int, int>();
        
        public MyBook UserBookEntry { get; set; }

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

            Console.WriteLine($"Debug: OnGetAsync - Loading book with ID {id}");
            
            // Clear EF Core tracking to ensure we get fresh data
            _context.ChangeTracker.Clear();

            // Improved query to properly load all nested data (simplified without nested comments)
            var book = await _context.Books
                .Include(b => b.Genres)
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.User)
                .Include(b => b.Reviews)
                    .ThenInclude(r => r.ReviewVotes)
                // Force load fresh data
                .AsSplitQuery() // Split the query to avoid cartesian explosion
                .FirstOrDefaultAsync(m => m.Id == id);

            if (book == null)
            {
                Console.WriteLine($"Debug: Book with ID {id} not found");
                return NotFound();
            }

            Book = book;
            
            // Debug information about loaded reviews
            Console.WriteLine($"Debug: Book has {book.Reviews.Count} reviews");
            
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
            
            // Calculate vote counts for each review
            foreach (var review in Book.Reviews)
            {
                int upvotes = review.ReviewVotes.Count(v => v.VoteType == 1);
                int downvotes = review.ReviewVotes.Count(v => v.VoteType == -1);
                ReviewVoteCounts[review.Id] = (upvotes, downvotes);
            }
            
            // Get current user's votes if authenticated
            if (User.Identity.IsAuthenticated)
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Check if user has entry in MyBooks
                UserBookEntry = await _context.MyBooks
                    .FirstOrDefaultAsync(mb => mb.UserId == userId && mb.BookId == id);
                
                // Get user's votes for reviews in this book
                var userVotes = await _context.ReviewVotes
                    .Where(v => v.UserId == userId && Book.Reviews.Select(r => r.Id).Contains(v.ReviewId))
                    .ToListAsync();
                    
                foreach (var vote in userVotes)
                {
                    UserVotes[vote.ReviewId] = vote.VoteType;
                }
            }
            
            return Page();
        }
        
        public async Task<IActionResult> OnPostVoteAsync(int reviewId, int voteType)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Auth/Login", new { returnUrl = Request.Path });
            }
            
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Get the review
            var review = await _context.Reviews
                .Include(r => r.ReviewVotes)
                .FirstOrDefaultAsync(r => r.Id == reviewId);
                
            if (review == null)
            {
                return NotFound();
            }
            
            // Check if user has already voted on this review
            var existingVote = await _context.ReviewVotes
                .FirstOrDefaultAsync(v => v.ReviewId == reviewId && v.UserId == userId);
                
            if (existingVote != null)
            {
                // If same vote type, remove the vote (toggle)
                if (existingVote.VoteType == voteType)
                {
                    _context.ReviewVotes.Remove(existingVote);
                }
                else
                {
                    // Change vote type
                    existingVote.VoteType = voteType;
                    existingVote.CreatedAt = DateTime.UtcNow;
                }
            }
            else
            {
                // Create new vote
                var newVote = new ReviewVote
                {
                    ReviewId = reviewId,
                    UserId = userId,
                    VoteType = voteType,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.ReviewVotes.Add(newVote);
            }
            
            await _context.SaveChangesAsync();
            
            // Return to the book details page
            return RedirectToPage(new { id = review.BookId });
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
