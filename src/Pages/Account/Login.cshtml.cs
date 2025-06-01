using Agent007.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Agent007.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly ChatDbContext _context;

        public LoginModel(ChatDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        [Required(ErrorMessage = "Username is required.")]  
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [DataType(DataType.Password)]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;

        [TempData]
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Handle GET requests (when page loads)
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Add this for debugging
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            Console.WriteLine($"Username: '{Username}', Password: '{Password}'");
            
            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fill in all fields.";
                return Page();
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == Username);

            if (user != null && VerifyPassword(Password, user.PasswordHash))
            {
                var claims = new List<Claim>
                {
                    new(ClaimTypes.Name, user.Username),
                    new(ClaimTypes.NameIdentifier, user.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return Redirect("/");
            }

            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        private bool VerifyPassword(string inputPassword, string storedPassword)
        {
            if (!storedPassword.StartsWith("$2"))
            {
                if (inputPassword == storedPassword)
                {
                    var user = _context.Users.First(u => u.PasswordHash == storedPassword);
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(inputPassword);
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(inputPassword, storedPassword);
        }
    }
}