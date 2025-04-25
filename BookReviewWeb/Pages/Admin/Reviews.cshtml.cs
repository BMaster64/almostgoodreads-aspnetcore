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
    public class ReviewsModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public ReviewsModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public IList<Review> Reviews { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public string SortOrder { get; set; } = "newest";

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public string RatingFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        public int TotalReviews { get; set; }
        public int TotalPages { get; set; }
        public int ReviewsPerPage { get; set; } = 10;

        [TempData]
        public string SuccessMessage { get; set; }

        public async Task OnGetAsync()
        {
            // Start building the query
            var reviewsQuery = _context.Reviews
                .Include(r => r.Book)
                .Include(r => r.User)
                .AsQueryable();

            // Apply rating filter if provided
            if (!string.IsNullOrEmpty(RatingFilter) && int.TryParse(RatingFilter, out int ratingValue))
            {
                reviewsQuery = reviewsQuery.Where(r => r.Rating == ratingValue);
            }

            // Apply search if term provided
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                SearchTerm = SearchTerm.Trim();
                reviewsQuery = reviewsQuery.Where(r => 
                    r.Book.Title.Contains(SearchTerm) || 
                    r.Book.Author.Contains(SearchTerm) ||
                    r.User.UserName.Contains(SearchTerm) ||
                    r.Comment.Contains(SearchTerm));
            }

            // Apply sorting
            reviewsQuery = SortOrder switch
            {
                "oldest" => reviewsQuery.OrderBy(r => r.CreatedAt),
                "highest" => reviewsQuery.OrderByDescending(r => r.Rating),
                "lowest" => reviewsQuery.OrderBy(r => r.Rating),
                _ => reviewsQuery.OrderByDescending(r => r.CreatedAt) // Default to newest
            };

            // Count total for pagination
            TotalReviews = await reviewsQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(TotalReviews / (double)ReviewsPerPage);

            // Ensure valid page number
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;

            // Get the reviews for current page
            Reviews = await reviewsQuery
                .Skip((PageNumber - 1) * ReviewsPerPage)
                .Take(ReviewsPerPage)
                .ToListAsync();
        }
    }
} 