using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Pages.Auth
{
    public class RegisterModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public RegisterModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            [StringLength(50, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 3)]
            public string UserName { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }
        }

        public void OnGet(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            
            if (ModelState.IsValid)
            {
                // Check if username is already taken
                var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == Input.UserName);
                if (existingUser != null)
                {
                    ModelState.AddModelError(string.Empty, "Username is already taken.");
                    return Page();
                }

                // Create a new user
                var user = new BookReviewWeb.Models.User
                {
                    UserName = Input.UserName,
                    PasswordHash = Input.Password, // In a production app, use a proper password hashing method
                    Role = "User" // By default, new users are regular Users
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Redirect to login page
                return RedirectToPage("./Login");
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
} 