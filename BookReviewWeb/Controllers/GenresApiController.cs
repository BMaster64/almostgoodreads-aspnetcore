using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookReviewWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookReviewWeb.Controllers
{
    [Route("api/genres")]
    [ApiController]
    public class GenresApiController : ControllerBase
    {
        private readonly AlmostGoodReadsContext _context;

        public GenresApiController(AlmostGoodReadsContext context)
        {
            _context = context;
        }
        
        // POST: api/genres
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGenre([FromBody] GenreCreateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, error = "Invalid genre data" });
            }

            // Check if genre already exists
            if (await _context.Genres.AnyAsync(g => g.GenreName == model.GenreName.Trim()))
            {
                return BadRequest(new { success = false, error = "A genre with this name already exists" });
            }

            var genre = new Genre
            {
                GenreName = model.GenreName.Trim()
            };

            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, genreId = genre.GenreId, genreName = genre.GenreName });
        }
        
        // Model for genre creation
        public class GenreCreateModel
        {
            public string GenreName { get; set; }
        }
    }
} 