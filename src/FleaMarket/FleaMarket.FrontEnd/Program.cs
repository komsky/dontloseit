using FleaMarket.FrontEnd.Data;
using FleaMarket.FrontEnd.Services;
using Microsoft.AspNetCore.Identity;
using FleaMarket.FrontEnd.Models;
using Microsoft.EntityFrameworkCore;

namespace FleaMarket.FrontEnd
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Configuration.AddEnvironmentVariables();
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddAuthentication()
                .AddMicrosoftAccount(o =>
                {
                    o.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"]!;
                    o.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"]!;
                })
                .AddGoogle(o =>
                {
                    o.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
                    o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
                    o.ClaimActions.MapJsonKey("picture", "picture", "url");
                })
                .AddFacebook(o =>
                {
                    o.AppId = builder.Configuration["Authentication:Facebook:AppId"]!;
                    o.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"]!;
                    o.Fields.Add("picture");
                    o.ClaimActions.MapJsonSubKey("urn:facebook:picture", "picture", "data", "url");
                });
            builder.Services.AddControllersWithViews();

            builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
            builder.Services.AddTransient<IEmailService, SmtpEmailService>();

            var app = builder.Build();

            ConfigureDatabase(app);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            app.Run();
        }

        private static void ConfigureDatabase(WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
            }
        }
    }
}
