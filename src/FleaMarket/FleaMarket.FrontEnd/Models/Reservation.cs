using System;
using System.ComponentModel.DataAnnotations;

namespace FleaMarket.FrontEnd.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        public int ItemId { get; set; }
        public Item Item { get; set; } = null!;

        public string BuyerId { get; set; } = string.Empty;
        public ApplicationUser Buyer { get; set; } = null!;

        public DateTime Created { get; set; } = DateTime.UtcNow;
    }
}
