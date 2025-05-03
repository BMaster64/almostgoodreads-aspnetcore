using System;
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
    [Authorize]
    public class MyBooksModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public MyBooksModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public Models.User CurrentUser { get; set; }
        public List<MyBook> MyBooks { get; set; } = new List<MyBook>();

        [BindProperty(SupportsGet = true)]
        public int? StatusFilter { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            CurrentUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (CurrentUser == null)
            {
                return NotFound();
            }

            // Get the user's books with their status and include book details
            var query = _context.MyBooks
                .Include(mb => mb.Book)
                .ThenInclude(b => b.Genres)
                .Include(mb => mb.Book.Reviews)
                .Where(mb => mb.UserId == userId);

            // Apply status filter if provided
            if (StatusFilter.HasValue)
            {
                query = query.Where(mb => mb.Status == StatusFilter.Value);
            }

            // Order by status and then by date added
            MyBooks = await query
                .OrderBy(mb => mb.DateAdded)
                .ToListAsync();

            return Page();
        }

        // Helper method to translate status code to display name
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