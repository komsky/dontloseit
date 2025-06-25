using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace FleaMarket.FrontEnd.Models
{
    public class ItemEditViewModel : ItemCreateViewModel
    {
        public List<ItemImage> ExistingImages { get; set; } = new();
        public List<int> RemoveImageIds { get; set; } = new();
    }
}
