using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Pages.Reviews
{
    public class IndexModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public IndexModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public IList<Review> Reviews { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "newest";

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalReviews { get; set; }
        public int TotalPages { get; set; }
        public int ReviewsPerPage { get; set; } = 10;
        
        // Dictionary to store user review counts
        public Dictionary<int, int> UserReviewCounts { get; set; } = new Dictionary<int, int>();
        
        // Dictionary to store vote counts for each review
        public Dictionary<int, (int Upvotes, int Downvotes, int Total)> ReviewVoteCounts { get; set; } = 
            new Dictionary<int, (int, int, int)>();

        public async Task OnGetAsync()
        {
            var reviewsQuery = _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .Include(r => r.ReviewVotes)
                .AsQueryable();

            // Apply sorting
            reviewsQuery = SortOrder switch
            {
                "oldest" => reviewsQuery.OrderBy(r => r.CreatedAt),
                "highest" => reviewsQuery.OrderByDescending(r => r.Rating),
                "lowest" => reviewsQuery.OrderBy(r => r.Rating),
                "most_votes" => reviewsQuery.OrderByDescending(r => r.ReviewVotes.Count),
                "most_upvotes" => reviewsQuery.OrderByDescending(r => r.ReviewVotes.Count(v => v.VoteType == 1)),
                "most_downvotes" => reviewsQuery.OrderByDescending(r => r.ReviewVotes.Count(v => v.VoteType == -1)),
                _ => reviewsQuery.OrderByDescending(r => r.CreatedAt) // Default to newest
            };

            // Count total reviews for pagination
            TotalReviews = await reviewsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalReviews / (double)ReviewsPerPage);

            // Ensure valid page number
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Fetch reviews for current page
            Reviews = await reviewsQuery
                .Skip((PageNumber - 1) * ReviewsPerPage)
                .Take(ReviewsPerPage)
                .ToListAsync();
                
            // Get user IDs from the reviews
            var userIds = Reviews.Select(r => r.UserId).Distinct().ToList();
            
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
            foreach (var review in Reviews)
            {
                int upvotes = review.ReviewVotes.Count(v => v.VoteType == 1);
                int downvotes = review.ReviewVotes.Count(v => v.VoteType == -1);
                int total = upvotes - downvotes;
                ReviewVoteCounts[review.Id] = (upvotes, downvotes, total);
            }
        }
    }
} 