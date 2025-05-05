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

namespace BookReviewWeb.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserDetailsModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public UserDetailsModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public Models.User User { get; set; }
        public List<Review> Reviews { get; set; } = new List<Review>();
        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }
        
        [TempData]
        public string DeletePasswordErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            User = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id);

            if (User == null)
            {
                return NotFound();
            }

            // Get user reviews with book information
            Reviews = await _context.Reviews
                .Include(r => r.Book)
                .Where(r => r.UserId == id)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Calculate statistics
            ReviewCount = Reviews.Count;
            AverageRating = Reviews.Any() ? 
                Reviews.Average(r => r.Rating) : 0;

            return Page();
        }
        
        public async Task<IActionResult> OnPostBanAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            // Don't allow banning an admin
            if (user.Role == "Admin")
            {
                return RedirectToPage(new { id });
            }
            
            user.Status = 3; // Banned
            await _context.SaveChangesAsync();
            
            SuccessMessage = $"User '{user.UserName}' has been banned.";
            return RedirectToPage(new { id });
        }
        
        public async Task<IActionResult> OnPostActivateAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            user.Status = 1; // Active
            await _context.SaveChangesAsync();
            
            SuccessMessage = $"User '{user.UserName}' has been unbanned.";
            return RedirectToPage(new { id });
        }
        
        public async Task<IActionResult> OnPostDeleteAccountAsync(int id, string adminPassword)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            // Don't allow deleting an admin
            if (user.Role == "Admin")
            {
                return RedirectToPage(new { id });
            }
            
            // Get current admin user
            var adminId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var adminUser = await _context.Users.FindAsync(adminId);
            
            if (adminUser == null)
            {
                DeletePasswordErrorMessage = "Error verifying admin credentials.";
                return RedirectToPage(new { id });
            }
            
            // Verify admin password
            if (adminUser.PasswordHash != adminPassword)
            {
                DeletePasswordErrorMessage = "Incorrect admin password. Please try again.";
                return RedirectToPage(new { id });
            }
            
            // First delete user's related data
            var userReviews = await _context.Reviews.Where(r => r.UserId == id).ToListAsync();
            var userBooks = await _context.MyBooks.Where(mb => mb.UserId == id).ToListAsync();
            var userVotes = await _context.ReviewVotes.Where(rv => rv.UserId == id).ToListAsync();
            
            // Remove related data first (to avoid foreign key constraint errors)
            _context.ReviewVotes.RemoveRange(userVotes);
            _context.Reviews.RemoveRange(userReviews);
            _context.MyBooks.RemoveRange(userBooks);
            
            // Finally, remove the user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            
            SuccessMessage = $"User '{user.UserName}' has been permanently deleted.";
            return RedirectToPage("Users");
        }
    }
} 