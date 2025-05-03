using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
    public class EditGenreModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public EditGenreModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        [BindProperty]
        public GenreEditModel GenreInput { get; set; }

        public class GenreEditModel
        {
            public int GenreId { get; set; }

            [Required(ErrorMessage = "Genre name is required")]
            [StringLength(100, ErrorMessage = "Genre name cannot exceed 100 characters")]
            [Display(Name = "Genre Name")]
            public string GenreName { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var genre = await _context.Genres.FindAsync(id);

            if (genre == null)
            {
                return NotFound();
            }

            GenreInput = new GenreEditModel
            {
                GenreId = genre.GenreId,
                GenreName = genre.GenreName
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var genre = await _context.Genres.FindAsync(GenreInput.GenreId);

            if (genre == null)
            {
                return NotFound();
            }

            // Check if another genre with the same name already exists
            if (_context.Genres.Any(g => g.GenreName == GenreInput.GenreName.Trim() && g.GenreId != GenreInput.GenreId))
            {
                ModelState.AddModelError("GenreInput.GenreName", "Another genre with this name already exists");
                return Page();
            }

            genre.GenreName = GenreInput.GenreName.Trim();

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Genre '{genre.GenreName}' has been updated successfully";
                return RedirectToPage("./Genres");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GenreExists(GenreInput.GenreId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool GenreExists(int id)
        {
            return _context.Genres.Any(e => e.GenreId == id);
        }
    }
} 