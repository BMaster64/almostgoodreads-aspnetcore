using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using BookReviewWeb.Models;

namespace BookReviewWeb.Pages.Library
{
    public class CreateModel : PageModel
    {
        private readonly BookReviewWeb.Models.AlmostGoodReadsContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CreateModel(BookReviewWeb.Models.AlmostGoodReadsContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [BindProperty]
        public Book Book { get; set; } = default!;

        [BindProperty]
        public IFormFile? CoverImage { get; set; }

        [BindProperty]
        public List<int> SelectedGenreIds { get; set; } = new List<int>();

        public IActionResult OnGet()
        {
            ViewData["Genres"] = new MultiSelectList(_context.Genres, "GenreId", "GenreName");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                ViewData["Genres"] = new MultiSelectList(_context.Genres, "GenreId", "GenreName");
                return Page();
            }

            // Attach selected genres to the book
            if (SelectedGenreIds != null && SelectedGenreIds.Any())
            {
                foreach (var genreId in SelectedGenreIds)
                {
                    var genre = await _context.Genres.FindAsync(genreId);
                    if (genre != null)
                    {
                        Book.Genres.Add(genre);
                    }
                }
            }

            // Check if file upload is used (takes precedence if both are provided)
            if (CoverImage != null && CoverImage.Length > 0)
            {
                // Create a unique filename for the image
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "covers");

                // Ensure the directory exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Create unique filename using GUID
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + CoverImage.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await CoverImage.CopyToAsync(fileStream);
                }

                // Update the CoverImageUrl property with the relative path
                Book.CoverImageUrl = "/images/covers/" + uniqueFileName;
            }

            _context.Books.Add(Book);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
