using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StyleSphere.Models;

public class Review
{
    [Key]
    public int ReviewID { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime ReviewDate { get; set; }

    public string? Comment { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [Required]
    public int UserID { get; set; }

    [Required]
    public int ProductID { get; set; }

    // Navigation properties
    [ForeignKey("UserID")]
    public virtual User? User { get; set; }

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }
}

