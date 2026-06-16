using System.ComponentModel.DataAnnotations;

namespace StyleSphere.Models;

public class Shipper
{
    [Key]
    public int ShipperID { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    // Navigation property
    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}

