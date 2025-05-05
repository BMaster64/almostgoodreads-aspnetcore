using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Pages.User
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public ProfileModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        public Models.User CurrentUser { get; set; }

        [BindProperty]
        public Models.User UserProfile { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; }

        [BindProperty]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [BindProperty]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [TempData]
        public string SuccessMessage { get; set; }

        public int ReviewCount { get; set; }
        public double AverageRating { get; set; }
        public int BooksInCollection { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            CurrentUser = await _context.Users
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (CurrentUser == null)
            {
                return NotFound();
            }

            UserProfile = new Models.User
            {
                Id = CurrentUser.Id,
                UserName = CurrentUser.UserName,
                Role = CurrentUser.Role
            };

            // Calculate review statistics
            ReviewCount = CurrentUser.Reviews.Count;
            AverageRating = CurrentUser.Reviews.Any() ? 
                CurrentUser.Reviews.Average(r => r.Rating) : 0;
            BooksInCollection = await _context.MyBooks.CountAsync(mb => mb.UserId == userId);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var user = await _context.Users
                .Include(u => u.Reviews)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return NotFound();
            }

            // Verify current password
            if (user.PasswordHash != CurrentPassword)
            {
                ModelState.AddModelError("CurrentPassword", "Current password is incorrect");
                
                // Reload user data
                CurrentUser = user;
                ReviewCount = user.Reviews.Count;
                AverageRating = user.Reviews.Any() ? 
                    user.Reviews.Average(r => r.Rating) : 0;
                BooksInCollection = await _context.MyBooks.CountAsync(mb => mb.UserId == userId);

                return Page();
            }

            // Check if username is already taken (by someone else)
            if (UserProfile.UserName != user.UserName)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserName == UserProfile.UserName && u.Id != userId);
                
                if (existingUser != null)
                {
                    ModelState.AddModelError("UserProfile.UserName", "Username is already taken");
                    
                    // Reload user data
                    CurrentUser = user;
                    ReviewCount = user.Reviews.Count;
                    AverageRating = user.Reviews.Any() ? 
                        user.Reviews.Average(r => r.Rating) : 0;
                    
                    return Page();
                }
            }

            // Update username
            user.UserName = UserProfile.UserName;

            // Update password if provided
            if (!string.IsNullOrEmpty(NewPassword))
            {
                user.PasswordHash = NewPassword;
            }

            try
            {
                await _context.SaveChangesAsync();
                SuccessMessage = "Profile updated successfully";

                // Update authentication cookie with new username
                await RefreshSignIn(user);
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Error updating profile. Please try again.");
                
                // Reload user data
                CurrentUser = user;
                ReviewCount = user.Reviews.Count;
                AverageRating = user.Reviews.Any() ? 
                    user.Reviews.Average(r => r.Rating) : 0;
                
                return Page();
            }

            return RedirectToPage();
        }

        private async Task RefreshSignIn(Models.User user)
        {
            // Sign out the current user
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Create a new claims identity
            var claims = new System.Collections.Generic.List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                RedirectUri = Request.Path
            };

            // Sign in the user with the new identity
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
} 