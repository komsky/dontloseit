using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FleaMarket.FrontEnd.Models
{
    public class ItemCreateViewModel
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsFree { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? Deadline { get; set; }

        public List<IFormFile> Images { get; set; } = new List<IFormFile>();
    }
}
