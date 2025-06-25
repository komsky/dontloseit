using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FleaMarket.FrontEnd.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IConfiguration _configuration;

        public RegisterModel(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            ILogger<RegisterModel> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _configuration = configuration;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ReturnUrl { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Site Password")]
            public string SitePassword { get; set; } = string.Empty;
        }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var expectedPassword = _configuration["RegistrationPassword"];
            if (expectedPassword == null || Input.SitePassword != expectedPassword)
            {
                ModelState.AddModelError(string.Empty, "Invalid site password.");
                return Page();
            }

            var user = new IdentityUser { UserName = Input.Email, Email = Input.Email };
            var result = await _userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User created a new account with password.");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl ?? Url.Content("~/"));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }
    }
}

