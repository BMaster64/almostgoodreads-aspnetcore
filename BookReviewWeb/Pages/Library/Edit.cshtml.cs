using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using BookReviewWeb.Models;

namespace BookReviewWeb.Pages.Library
{
    public class EditModel : PageModel
    {
        private readonly BookReviewWeb.Models.AlmostGoodReadsContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public EditModel(BookReviewWeb.Models.AlmostGoodReadsContext context, IWebHostEnvironment webHostEnvironment)
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

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var book = await _context.Books
                .Include(b => b.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (book == null)
            {
                return NotFound();
            }
            Book = book;
            
            // Populate the selected genres
            SelectedGenreIds = Book.Genres.Select(g => g.GenreId).ToList();
            
            // Use MultiSelectList for multiple selection
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
            // Load the existing book with genres
            var bookToUpdate = await _context.Books
                .Include(b => b.Genres)
                .FirstOrDefaultAsync(b => b.Id == Book.Id);
            
            if (bookToUpdate == null) return NotFound();
            
            // Update simple properties
            bookToUpdate.Title = Book.Title;
            bookToUpdate.Author = Book.Author;
            bookToUpdate.Description = Book.Description;
            bookToUpdate.PublishYear = Book.PublishYear;
            
            // Check if file upload is used
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
                bookToUpdate.CoverImageUrl = "/images/covers/" + uniqueFileName;
            }
            else
            {
                bookToUpdate.CoverImageUrl = Book.CoverImageUrl;
            }
            
            // Update genres - remove existing and add selected
            bookToUpdate.Genres.Clear();
            
            if (SelectedGenreIds != null && SelectedGenreIds.Any())
            {
                foreach (var genreId in SelectedGenreIds)
                {
                    var genre = await _context.Genres.FindAsync(genreId);
                    if (genre != null)
                    {
                        bookToUpdate.Genres.Add(genre);
                    }
                }
            }
            
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!BookExists(Book.Id)) return NotFound();
                else throw;
            }
            
            return RedirectToPage("./Index");
        }

        private bool BookExists(int id)
        {
            return _context.Books.Any(e => e.Id == id);
        }
    }
}
