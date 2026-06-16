using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StyleSphere.Models;

public class Payment
{
    [Key]
    public int PaymentID { get; set; }

    [Required]
    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime PaymentDate { get; set; }

    [Required]
    [StringLength(50)]
    public string Method { get; set; } = string.Empty; // Credit Card, Debit Card, Cash

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

    [Required]
    public int OrderID { get; set; }

    // Navigation property
    [ForeignKey("OrderID")]
    public virtual Order? Order { get; set; }
}

