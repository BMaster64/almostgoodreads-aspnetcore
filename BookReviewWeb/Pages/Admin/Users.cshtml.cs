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
    public class UsersModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public UsersModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public IList<Models.User> Users { get; set; } = new List<Models.User>();
        
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string StatusFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public string RoleFilter { get; set; }
        
        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;
        
        public int TotalPages { get; set; }
        public int UsersPerPage { get; set; } = 15;
        
        [TempData]
        public string SuccessMessage { get; set; }
        
        [TempData]
        public string PasswordErrorMessage { get; set; }
        
        public Dictionary<int, UserStatistics> UserStats { get; set; } = new Dictionary<int, UserStatistics>();
        
        public class UserStatistics
        {
            public int ReviewCount { get; set; }
            public int BookCount { get; set; }
        }

        public async Task OnGetAsync()
        {
            // Start with the base query
            var usersQuery = _context.Users.AsQueryable();
            
            // Apply search filter if provided
            if (!string.IsNullOrEmpty(SearchTerm))
            {
                SearchTerm = SearchTerm.Trim();
                usersQuery = usersQuery.Where(u => u.UserName.Contains(SearchTerm));
            }
            
            // Apply status filter if provided
            if (!string.IsNullOrEmpty(StatusFilter) && int.TryParse(StatusFilter, out int statusValue))
            {
                usersQuery = usersQuery.Where(u => u.Status == statusValue);
            }
            
            // Apply role filter if provided
            if (!string.IsNullOrEmpty(RoleFilter))
            {
                usersQuery = usersQuery.Where(u => u.Role == RoleFilter);
            }
            
            // Count total for pagination
            var totalUsers = await usersQuery.CountAsync();
            TotalPages = (int)Math.Ceiling(totalUsers / (double)UsersPerPage);
            
            // Ensure valid page number
            if (PageNumber < 1) PageNumber = 1;
            if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;
            
            // Get users for the current page
            Users = await usersQuery
                .OrderBy(u => u.Id)
                .Skip((PageNumber - 1) * UsersPerPage)
                .Take(UsersPerPage)
                .ToListAsync();
                
            // Get user IDs
            var userIds = Users.Select(u => u.Id).ToList();
            
            // Get reviews count for each user
            var reviewCounts = await _context.Reviews
                .Where(r => userIds.Contains(r.UserId))
                .GroupBy(r => r.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
                
            // Get books count for each user
            var bookCounts = await _context.MyBooks
                .Where(mb => userIds.Contains(mb.UserId))
                .GroupBy(mb => mb.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToListAsync();
                
            // Initialize statistics for all users
            foreach (var user in Users)
            {
                UserStats[user.Id] = new UserStatistics
                {
                    ReviewCount = 0,
                    BookCount = 0
                };
            }
            
            // Populate review counts
            foreach (var item in reviewCounts)
            {
                UserStats[item.UserId].ReviewCount = item.Count;
            }
            
            // Populate book counts
            foreach (var item in bookCounts)
            {
                UserStats[item.UserId].BookCount = item.Count;
            }
        }
        
        public async Task<IActionResult> OnPostSuspendAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            
            if (user == null)
            {
                return NotFound();
            }
            
            // Don't allow suspending an admin
            if (user.Role == "Admin")
            {
                return RedirectToPage();
            }
            
            user.Status = 2; // Suspended
            await _context.SaveChangesAsync();
            
            SuccessMessage = $"User '{user.UserName}' has been suspended.";
            return RedirectToPage();
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
                return RedirectToPage();
            }
            
            user.Status = 3; // Banned
            await _context.SaveChangesAsync();
            
            SuccessMessage = $"User '{user.UserName}' has been banned.";
            return RedirectToPage();
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
            
            string action = user.Status == 3 ? "unbanned" : "activated";
            SuccessMessage = $"User '{user.UserName}' has been {action}.";
            return RedirectToPage();
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
                return RedirectToPage();
            }
            
            // Get current admin user
            var adminId = int.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier));
            var adminUser = await _context.Users.FindAsync(adminId);
            
            if (adminUser == null)
            {
                PasswordErrorMessage = "Error verifying admin credentials.";
                return RedirectToPage();
            }
            
            // Verify admin password
            if (adminUser.PasswordHash != adminPassword)
            {
                PasswordErrorMessage = "Incorrect admin password. Please try again.";
                return RedirectToPage();
            }
            
            // Promote user to admin
            user.Role = "Admin";
            await _context.SaveChangesAsync();
            
            SuccessMessage = $"User '{user.UserName}' has been promoted to admin.";
            return RedirectToPage();
        }
    }
} 