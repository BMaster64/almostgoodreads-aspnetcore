using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Antiforgery;
using System.Security.Claims;
using System;
using System.Threading.Tasks;
using BookReviewWeb.Models;

namespace BookReviewWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Only logged-in users can access these endpoints
    public class MyBooksController : ControllerBase
    {
        private readonly AlmostGoodReadsContext _context;
        private readonly IAntiforgery _antiforgery;

        public MyBooksController(AlmostGoodReadsContext context, IAntiforgery antiforgery)
        {
            _context = context;
            _antiforgery = antiforgery;
        }

        [HttpPost("update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMyBook([FromBody] MyBookUpdateModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the current user's ID from claims
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Check if this book is already in the user's collection
            var existingBook = _context.MyBooks.FirstOrDefault(mb => mb.UserId == userId && mb.BookId == model.BookId);

            if (existingBook != null)
            {
                // Update existing record
                existingBook.Status = model.Status;
            }
            else
            {
                // Add new record
                _context.MyBooks.Add(new MyBook
                {
                    UserId = userId,
                    BookId = model.BookId,
                    Status = model.Status,
                    DateAdded = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost("remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveFromMyBooks([FromBody] MyBookRemoveModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the current user's ID from claims
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Find the book in the user's collection
            var myBook = _context.MyBooks.FirstOrDefault(mb => mb.UserId == userId && mb.BookId == model.BookId);

            if (myBook == null)
            {
                return NotFound("Book not found in your collection");
            }

            // Remove the book
            _context.MyBooks.Remove(myBook);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }

    // Model classes for the API requests
    public class MyBookUpdateModel
    {
        public int BookId { get; set; }
        public int Status { get; set; }
    }

    public class MyBookRemoveModel
    {
        public int BookId { get; set; }
    }
}