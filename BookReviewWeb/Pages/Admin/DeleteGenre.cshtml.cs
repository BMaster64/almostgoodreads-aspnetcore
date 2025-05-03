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
    public class DeleteGenreModel : PageModel
    {
        private readonly AlmostGoodReadsContext _context;

        public DeleteGenreModel(AlmostGoodReadsContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Genre Genre { get; set; }
        
        public int AssociatedBooksCount { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Genre = await _context.Genres
                .Include(g => g.Books)
                .FirstOrDefaultAsync(m => m.GenreId == id);

            if (Genre == null)
            {
                return NotFound();
            }

            AssociatedBooksCount = Genre.Books.Count;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var genre = await _context.Genres
                .Include(g => g.Books)
                .FirstOrDefaultAsync(g => g.GenreId == Genre.GenreId);

            if (genre == null)
            {
                return NotFound();
            }

            // Remove this genre from all books that are associated with it
            foreach (var book in genre.Books.ToList())
            {
                book.Genres.Remove(genre);
            }

            // Now delete the genre
            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Genre '{genre.GenreName}' has been deleted successfully";

            return RedirectToPage("./Genres");
        }
    }
} 