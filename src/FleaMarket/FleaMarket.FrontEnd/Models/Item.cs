using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FleaMarket.FrontEnd.Models
{
    public class Item
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public decimal? Price { get; set; }

        public bool IsArchived { get; set; }

        public bool IsReserved { get; set; }

        public bool IsSold { get; set; }

        public string? OwnerId { get; set; }
        public IdentityUser? Owner { get; set; }

        public ICollection<ItemImage> Images { get; set; } = new List<ItemImage>();
    }

    public class ItemImage
    {
        public int Id { get; set; }

        [Required]
        public string FileName { get; set; } = string.Empty;

        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;
    }
}
