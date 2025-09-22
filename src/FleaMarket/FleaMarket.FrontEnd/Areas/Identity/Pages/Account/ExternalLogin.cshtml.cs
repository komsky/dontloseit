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
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

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

        public string ProviderDisplayName { get; set; } = string.Empty;

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; } = string.Empty;
            public string ProviderKey { get; set; } = string.Empty;
            public string ProviderDisplayName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public Dictionary<string, string> Claims { get; set; } = new();
        }

        public IActionResult OnGet() => RedirectToPage("./Login");

        public async Task<IActionResult> OnPostAsync(string provider, string? returnUrl = null)
        {
            try
            {
                // Clear the existing external cookie to ensure a clean login process
                await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

                // Request a redirect to the external login provider.
                var redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl });
                var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
                return new ChallengeResult(provider, properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating external login with provider {Provider}", provider);
                ErrorMessage = "Unable to start the login process. Please try again.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
        }

        public async Task<IActionResult> OnGetCallbackAsync(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            
            if (remoteError != null)
            {
                // Handle specific OAuth error cases
                var errorMessage = remoteError.ToLower() switch
                {
                    "access_denied" => "Login was cancelled. Please try again if you'd like to sign in.",
                    "user_denied" => "Login was cancelled. Please try again if you'd like to sign in.",
                    _ => $"Error from external provider: {remoteError}"
                };
                ErrorMessage = errorMessage;
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                // This usually happens when the user cancels the login process
                ErrorMessage = "Login was cancelled or external login information could not be retrieved. Please try again.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            try
            {
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
            }
            catch (Exception ex) when (ex.Message.Contains("Access was denied") || ex.Message.Contains("remote server"))
            {
                _logger.LogWarning(ex, "External login failed for provider {Provider}", info.LoginProvider);
                ErrorMessage = "Login was cancelled or denied by the external provider. Please try again.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during external login with provider {Provider}", info.LoginProvider);
                ErrorMessage = "An unexpected error occurred during login. Please try again.";
                return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
            }

            // Check if user already exists with the same email address
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (!string.IsNullOrEmpty(email))
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    // User exists with same email - merge the accounts automatically
                    _logger.LogInformation("Merging external login {Provider} with existing account for {Email}", info.LoginProvider, email);

                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        // Update avatar from external provider if user doesn't have one
                        if (string.IsNullOrEmpty(existingUser.ProfileImageFileName))
                        {
                            await UpdateUserAvatarFromProvider(existingUser, info);
                        }

                        // Sign in the merged account
                        await _signInManager.SignInAsync(existingUser, isPersistent: false);
                        await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
                        _logger.LogInformation("Successfully merged and signed in user {Email} with {Provider}", email, info.LoginProvider);
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        // If adding login fails (e.g., already exists), try to sign in anyway
                        var mergedSignInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
                        if (mergedSignInResult.Succeeded)
                        {
                            await _signInManager.UpdateExternalAuthenticationTokensAsync(info);
                            _logger.LogInformation("User {Email} signed in with existing {Provider} login", email, info.LoginProvider);
                            return LocalRedirect(returnUrl);
                        }
                        else
                        {
                            // If we still can't sign in, sign them in with the existing account
                            await _signInManager.SignInAsync(existingUser, isPersistent: false);
                            _logger.LogInformation("Signed in existing user {Email} after external login attempt", email);
                            return LocalRedirect(returnUrl);
                        }
                    }
                }
            }

            // User doesn't exist, proceed to registration
            {
                // Store the external login info in TempData for the confirmation step
                var externalLoginData = new ExternalLoginData
                {
                    LoginProvider = info.LoginProvider,
                    ProviderKey = info.ProviderKey,
                    ProviderDisplayName = info.ProviderDisplayName,
                    Claims = info.Principal.Claims.ToDictionary(c => c.Type, c => c.Value)
                };

                ProviderDisplayName = info.ProviderDisplayName;
                if (info.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
                {
                    Input.Email = info.Principal.FindFirstValue(ClaimTypes.Email)!;
                    externalLoginData.Email = Input.Email;
                }

                // Store in TempData for the POST request
                TempData["ExternalLoginData"] = JsonSerializer.Serialize(externalLoginData);

                return Page();
            }
        }

        public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ReturnUrl = returnUrl;
            
            // Try to get the external login info from the session first
            var info = await _signInManager.GetExternalLoginInfoAsync();
            
            // If it's null, try to get it from TempData
            if (info == null)
            {
                var tempDataValue = TempData["ExternalLoginData"] as string;
                if (string.IsNullOrEmpty(tempDataValue))
                {
                    ErrorMessage = "Session expired during registration. Please try the registration process again.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }

                try
                {
                    var externalLoginData = JsonSerializer.Deserialize<ExternalLoginData>(tempDataValue);
                    if (externalLoginData == null)
                    {
                        ErrorMessage = "Error loading external login information during confirmation.";
                        return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                    }

                    // We'll handle this case by creating the user and manually adding the external login
                    return await CreateUserFromExternalLoginData(externalLoginData, returnUrl);
                }
                catch
                {
                    ErrorMessage = "Error processing external login information.";
                    return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
                }
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
                // For external OAuth logins, we consider the email verified since it comes from the provider
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, emailToken);
                
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
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to download avatar for user {Email}", user.Email);
                            // Continue without avatar - don't fail the entire registration
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

        private async Task<IActionResult> CreateUserFromExternalLoginData(ExternalLoginData externalLoginData, string returnUrl)
        {
            var expectedPassword = _configuration["RegistrationPassword"];
            if (expectedPassword == null || Input.SitePassword != expectedPassword)
            {
                ModelState.AddModelError(string.Empty, "Invalid site password.");
                ProviderDisplayName = externalLoginData.ProviderDisplayName;
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
                // For external OAuth logins, we consider the email verified since it comes from the provider
                var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                await _userManager.ConfirmEmailAsync(user, emailToken);
                
                // Add the external login manually
                var loginInfo = new UserLoginInfo(externalLoginData.LoginProvider, externalLoginData.ProviderKey, externalLoginData.ProviderDisplayName);
                result = await _userManager.AddLoginAsync(user, loginInfo);
                
                if (result.Succeeded)
                {
                    // Try to capture avatar from the external provider
                    var avatarUrl = externalLoginData.Claims.GetValueOrDefault("urn:google:picture") ??
                                   externalLoginData.Claims.GetValueOrDefault("urn:facebook:picture") ??
                                   externalLoginData.Claims.GetValueOrDefault("picture");

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
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to download avatar for user {Email}", user.Email);
                            // Continue without avatar - don't fail the entire registration
                        }
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    _logger.LogInformation("User created an account using {Name} provider.", externalLoginData.LoginProvider);
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ProviderDisplayName = externalLoginData.ProviderDisplayName;
            return Page();
        }

        private async Task UpdateUserAvatarFromProvider(ApplicationUser user, ExternalLoginInfo info)
        {
            try
            {
                var avatarUrl =
                    info.Principal.FindFirstValue("urn:google:picture") ??
                    info.Principal.FindFirstValue("urn:facebook:picture") ??
                    info.Principal.FindFirstValue("picture");

                if (!string.IsNullOrEmpty(avatarUrl))
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
                        _logger.LogInformation("Updated avatar for user {Email} from {Provider}", user.Email, info.LoginProvider);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to download avatar for user {Email} from {Provider}", user.Email, info.LoginProvider);
                // Continue without avatar - don't fail the entire process
            }
        }
    }
}
