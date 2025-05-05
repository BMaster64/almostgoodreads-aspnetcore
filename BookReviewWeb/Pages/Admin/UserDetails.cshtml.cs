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
        public string PasswordErrorMessage { get; set; }

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
        
        public async Task<IActionResult> OnPostPromoteToAdminAsync(int id, string adminPassword)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            // Don't allow promoting if already an admin
            if (user.Role == "Admin")
            {
                return RedirectToPage(new { id });
            }
            
            // Get current admin user
            var adminId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var adminUser = await _context.Users.FindAsync(adminId);
            
            if (adminUser == null)
            {
                PasswordErrorMessage = "Error verifying admin credentials.";
                return RedirectToPage(new { id });
            }
            
            // Verify admin password
            if (adminUser.PasswordHash != adminPassword)
            {
                PasswordErrorMessage = "Incorrect admin password. Please try again.";
                return RedirectToPage(new { id });
            }
            
            // Promote user to admin
            user.Role = "Admin";
            await _context.SaveChangesAsync();
            
            SuccessMessage = $"User '{user.UserName}' has been promoted to admin.";
            return RedirectToPage(new { id });
        }
    }
} 