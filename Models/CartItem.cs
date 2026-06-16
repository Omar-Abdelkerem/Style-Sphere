using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StyleSphere.Models;

public class CartItem
{
    [Key]
    public int CartItemID { get; set; }

    [Required]
    public int CartID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    // Navigation properties
    [ForeignKey("CartID")]
    public virtual Cart? Cart { get; set; }

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }
}

