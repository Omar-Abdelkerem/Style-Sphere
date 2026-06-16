using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StyleSphere.Models;

public class OrderItem
{
    [Key]
    public int OrderItemID { get; set; }

    [Required]
    public int OrderID { get; set; }

    [Required]
    public int ProductID { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Price { get; set; }

    // Navigation properties
    [ForeignKey("OrderID")]
    public virtual Order? Order { get; set; }

    [ForeignKey("ProductID")]
    public virtual Product? Product { get; set; }
}

