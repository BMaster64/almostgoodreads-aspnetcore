using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BookReviewWeb.Models;

public partial class MyBook
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int BookId { get; set; }

    [Range(1, 4)]
    public int Status { get; set; }

    public DateTime DateAdded { get; set; }

    public virtual Book Book { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
