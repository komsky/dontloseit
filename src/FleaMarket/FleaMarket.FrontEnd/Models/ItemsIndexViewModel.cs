using System.Collections.Generic;

namespace FleaMarket.FrontEnd.Models
{
    public class ItemsIndexViewModel
    {
        public string? Search { get; set; }
        public string? Category { get; set; }
        public bool ShowArchived { get; set; }
        public List<Item> Items { get; set; } = new();
        public Dictionary<string, int> Categories { get; set; } = new();
    }

    public class CategoryInfo
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Icon { get; set; } = "fas fa-tag";
    }
}
