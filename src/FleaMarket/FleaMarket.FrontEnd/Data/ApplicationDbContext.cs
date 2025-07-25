﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using FleaMarket.FrontEnd.Models;

namespace FleaMarket.FrontEnd.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Item> Items { get; set; } = default!;
        public DbSet<ItemImage> ItemImages { get; set; } = default!;
        public DbSet<Reservation> Reservations { get; set; } = default!;
    }
}
