using System.Collections.Generic;

namespace FleaMarket.FrontEnd.Models
{
    public class ItemsIndexViewModel
    {
        public string? Search { get; set; }
        public bool ShowArchived { get; set; }
        public List<Item> Items { get; set; } = new();
    }
}
