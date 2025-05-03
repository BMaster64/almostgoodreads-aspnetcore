using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BookReviewWeb.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class CreateGenreModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public CreateGenreModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        [BindProperty]
        public GenreInputModel GenreInput { get; set; } = new GenreInputModel();

        public class GenreInputModel
        {
            [Required(ErrorMessage = "Genre name is required")]
            [StringLength(100, ErrorMessage = "Genre name cannot exceed 100 characters")]
            [Display(Name = "Genre Name")]
            public string GenreName { get; set; }
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Check if genre already exists
            if (_context.Genres.Any(g => g.GenreName == GenreInput.GenreName.Trim()))
            {
                ModelState.AddModelError("GenreInput.GenreName", "A genre with this name already exists");
                return Page();
            }

            var genre = new Genre
            {
                GenreName = GenreInput.GenreName.Trim()
            };

            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Genre '{genre.GenreName}' has been created successfully";

            return RedirectToPage("./Genres");
        }
    }
} 