using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ApplicationMessage.Data;
using ApplicationMessage.Models;
using ApplicationMessage.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace ApplicationMessage.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewData["Title"] = "Register";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", "Fullfil all fields.");
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                ModelState.AddModelError("", "Username is not unique.");
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError("", "Email already used.");
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = PasswordHasher.HashPassword(password),
                ProfilePicturePath = "/profile_pictures/default.png"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewData["Title"] = "Login";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("", " All fields are required.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null || !PasswordHasher.VerifyPassword(password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Username and password not valid.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, "MyCookieAuth");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            await HttpContext.SignInAsync("MyCookieAuth", new ClaimsPrincipal(claimsIdentity), authProperties);

            user.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("MyCookieAuth");
            return RedirectToAction("Login");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateLastSeen()
        {
            var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(currentUserId);

            if (user != null)
            {
                user.LastSeen = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(currentUserId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string username, IFormFile profilePicture)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var user = await _context.Users.FindAsync(currentUserId);

            if (user == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                user.Username = username;
            }

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "profile_pictures");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                var permittedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!permittedTypes.Contains(profilePicture.ContentType))
                {
                    ModelState.AddModelError("ProfilePicture", "Only image files are allowed.");
                    return RedirectToAction("Index");
                }

                var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                user.ProfilePicturePath = "/profile_pictures/" + uniqueFileName;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Profile");
        }


    }
}
