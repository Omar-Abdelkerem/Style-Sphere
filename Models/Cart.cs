using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StyleSphere.Models;

public class Cart
{
    [Key]
    public int CartID { get; set; }

    [Required]
    public int UserID { get; set; }

    // Navigation property
    [ForeignKey("UserID")]
    public virtual User? User { get; set; }

    // Navigation property for cart items
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
}

