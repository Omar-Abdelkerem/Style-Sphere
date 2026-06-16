using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StyleSphere.Models;

public class Shipment
{
    [Key]
    public int ShipmentID { get; set; }

    [Required]
    [StringLength(50)]
    public string Delivery { get; set; } = "Processing"; // Processing, In Transit, Delivered

    public int? ShipperID { get; set; }

    [Required]
    public int OrderID { get; set; }

    // Navigation properties
    [ForeignKey("ShipperID")]
    public virtual Shipper? Shipper { get; set; }

    [ForeignKey("OrderID")]
    public virtual Order? Order { get; set; }
}

