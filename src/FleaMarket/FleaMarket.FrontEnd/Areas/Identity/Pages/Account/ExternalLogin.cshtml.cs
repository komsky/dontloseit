using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FleaMarket.FrontEnd.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Net.Http;

namespace FleaMarket.FrontEnd.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ExternalLoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly ILogger<ExternalLoginModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public ExternalLoginModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            ILogger<ExternalLoginModel> logger,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _userStore = userStore;
            _logger = logger;
            _configuration = configuration;
            _env = env;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string? ProviderDisplayName { get; set; }

        public string? ReturnUrl { get; set; }

        public string? ErrorMessage { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Site Password")]
            public string SitePassword { get; set; } = string.Empty;
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor : true);
            if (signInResult.Succeeded)
            {
                await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return LocalRedirect(returnUrl);
            }
            if (signInResult.IsLockedOut)
            {
                return RedirectToPage("./Lockout");
            }
            else
            {
                ProviderDisplayName = info.ProviderDisplayName;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input.Email = info.Principal.FindFirstValue(ClaimTypes.Email)!;
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ErrorMessage = "Error loading external login information during confirmation.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            if (!ModelState.IsValid)
            {
                ProviderDisplayName = info.ProviderDisplayName;
                return Page();
            }

            var expectedPassword = _configuration["RegistrationPassword"];
            if (expectedPassword == null || Input.SitePassword != expectedPassword)
            {
                ModelState.AddModelError(string.Empty, "Invalid site password.");
                ProviderDisplayName = info.ProviderDisplayName;
                return Page();
            }

            var user = Activator.CreateInstance<ApplicationUser>();
            await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
            if (_userManager.SupportsUserEmail)
            {
                await _userManager.SetEmailAsync(user, Input.Email);
            }

            var result = await _userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await _userManager.AddLoginAsync(user, info);
                if (result.Succeeded)
                {
                    // Try to capture avatar from the external provider
                    var avatarUrl =
                        info.Principal.FindFirstValue("urn:google:picture") ??
                        info.Principal.FindFirstValue("urn:facebook:picture") ??
                        info.Principal.FindFirstValue("picture");

                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        try
                        {
                            using var httpClient = new HttpClient();
                            var response = await httpClient.GetAsync(avatarUrl);
                            if (response.IsSuccessStatusCode)
                            {
                                var bytes = await response.Content.ReadAsByteArrayAsync();
                                var uploadDir = Path.Combine(_env.WebRootPath, "uploads");
                                Directory.CreateDirectory(uploadDir);
                                var ext = Path.GetExtension(new Uri(avatarUrl).LocalPath);
                                if (string.IsNullOrWhiteSpace(ext))
                                {
                                    ext = ".jpg";
                                }
                                var fileName = Guid.NewGuid() + ext;
                                var filePath = Path.Combine(uploadDir, fileName);
                                await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                                user.ProfileImageFileName = fileName;
                                await _userManager.UpdateAsync(user);
                            }
                        }
                        catch
                        {
                            // Ignore avatar download failures
                        }
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
                    _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ProviderDisplayName = info.ProviderDisplayName;
            return Page();
        }
    }
}
