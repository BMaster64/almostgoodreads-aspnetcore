using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Pages.User
{
    [Authorize(Roles = "User,Admin")]
    public class MyReviewsModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public MyReviewsModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public Models.User CurrentUser { get; set; }
        public List<Review> Reviews { get; set; } = new List<Review>();

        public async Task<IActionResult> OnGetAsync()
        {
            // Get the current user ID
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            // Get the user with reviews
            CurrentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (CurrentUser == null)
            {
                return NotFound();
            }

            // Get all reviews by this user with book information
            Reviews = await _context.Reviews
                .Include(r => r.Book)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Page();
        }
    }
} 