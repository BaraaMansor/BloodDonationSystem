using Microsoft.AspNetCore.Mvc;
using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using BloodDonationSystem.Helpers;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Controllers;
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Account/Register
        public IActionResult Register()
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.BloodTypes = _context.BloodTypes.ToList();
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered");
                    ViewBag.BloodTypes = _context.BloodTypes.ToList();
                    return View(model);
                }

                // Create user
                var user = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = model.Role,
                    Phone = model.Phone,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // If registering as donor, create donor record
                if (model.Role == "Donor" && model.BloodTypeId.HasValue)
                {
                    var donor = new Donor
                    {
                        UserId = user.Id,
                        BloodTypeId = model.BloodTypeId.Value,
                        DateOfBirth = model.DateOfBirth ?? DateTime.Now,
                        Gender = model.Gender ?? "Male",
                        Address = model.Address ?? "",
                        IsAvailable = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.Donors.Add(donor);
                    _context.SaveChanges();
                }

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            ViewBag.BloodTypes = _context.BloodTypes.ToList();
            return View(model);
        }

        // GET: Account/Login
        public IActionResult Login()
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    if (!user.IsActive)
                    {
                        ModelState.AddModelError("", "Your account is inactive. Please contact admin.");
                        return View(model);
                    }

                    // Set session
                    HttpContext.Session.SetUserSession(user.Id, user.FullName, user.Email, user.Role);

                    TempData["SuccessMessage"] = $"Welcome back, {user.FullName}!";

                    // Redirect based on role
                    return user.Role switch
                    {
                        "Admin" => RedirectToAction("Dashboard", "Admin"),
                        "Donor" => RedirectToAction("Dashboard", "Donor"),
                        "Hospital" => RedirectToAction("Dashboard", "Hospital"),
                        _ => RedirectToAction("Index", "Home")
                    };
                }

                ModelState.AddModelError("", "Invalid email or password");
            }

            return View(model);
        }

        // GET: Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.ClearUserSession();
            TempData["SuccessMessage"] = "You have been logged out successfully.";
            return RedirectToAction("Index", "Home");
        }
    }

    // View Models
    public class RegisterViewModel
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Donor"; // Donor or Hospital

        [Phone]
        public string? Phone { get; set; }

        // Donor-specific fields
        public int? BloodTypeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        public string? Gender { get; set; }

        public string? Address { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
