using Microsoft.AspNetCore.Identity;

namespace FleaMarket.FrontEnd.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? ProfileImageFileName { get; set; }
    }
}
