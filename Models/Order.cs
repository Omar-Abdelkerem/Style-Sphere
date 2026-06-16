using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StyleSphere.Models;

public class Order
{
    [Key]
    public int OrderID { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime OrderDate { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Processing"; // Processing, Shipped, Completed, Cancelled

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalAmount { get; set; }

    [Required]
    public int UserID { get; set; }

    // Navigation properties
    [ForeignKey("UserID")]
    public virtual User? User { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}

