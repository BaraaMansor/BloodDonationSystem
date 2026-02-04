using Microsoft.AspNetCore.Mvc;
using BloodDonationSystem.Models;
using BloodDonationSystem.Helpers;
using BloodDonationSystem.Services.ApplicationServices;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Controllers;
    public class AccountController(AccountService accountService) : Controller
    {
        private readonly AccountService _accountService = accountService;

    // GET: Account/Register
    public IActionResult Register()
        {
            if (HttpContext.Session.IsLoggedIn())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.BloodTypes = _accountService.GetBloodTypes();
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            // Validate that donors must have a blood type
            if (model.Role == "Donor" && !model.BloodTypeId.HasValue)
            {
                ModelState.AddModelError("BloodTypeId", "Blood Type is required for donors");
            }

            if (ModelState.IsValid)
            {
                // Check if email already exists
                if (_accountService.EmailExists(model.Email))
                {
                    ModelState.AddModelError("Email", "Email is already registered");
                    ViewBag.BloodTypes = _accountService.GetBloodTypes();
                    return View(model);
                }

                // Register user
                var user = _accountService.RegisterUser(
                    model.FullName,
                    model.Email,
                    model.Password,
                    model.Role,
                    model.Phone
                );

                // If registering as donor, create donor record
                if (model.Role == "Donor" && model.BloodTypeId.HasValue)
                {
                    _accountService.RegisterDonor(
                        user.Id,
                        model.BloodTypeId.Value,
                        model.DateOfBirth,
                        model.Gender,
                        model.Address
                    );
                }

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            }

            ViewBag.BloodTypes = _accountService.GetBloodTypes();
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
                var result = _accountService.AuthenticateUser(model.Email, model.Password);

                if (!result.Success)
                {
                    ModelState.AddModelError("", result.ErrorMessage!);
                    return View(model);
                }

                // Set session
                HttpContext.Session.SetUserSession(result.User!.Id, result.User.FullName, result.User.Email, result.User.Role);

                TempData["SuccessMessage"] = $"Welcome back, {result.User.FullName}!";

                // Redirect based on role
                var action = result.RedirectController == "Home" ? "Index" : "Dashboard";
                return RedirectToAction(action, result.RedirectController);
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
