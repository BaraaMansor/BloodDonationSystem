using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using BloodDonationSystem.Attributes;
using BloodDonationSystem.Helpers;
using System.ComponentModel.DataAnnotations;

namespace BloodDonationSystem.Controllers
{
    [AuthorizeRole("Donor")]
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard
        public IActionResult Dashboard()
        {
            var userId = HttpContext.Session.GetUserId();
            var donor = _context.Donors
                .Include(d => d.User)
                .Include(d => d.BloodType)
                .Include(d => d.Donations)
                .FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Index", "Home");
            }

            var model = new DonorDashboardViewModel
            {
                Donor = donor,
                TotalDonations = donor.Donations.Count,
                CompletedDonations = donor.Donations.Count(d => d.Status == "Completed"),
                PendingDonations = donor.Donations.Count(d => d.Status == "Pending"),
                LastDonationDate = donor.LastDonationDate,
                CanDonateAgain = !donor.LastDonationDate.HasValue || 
                                (DateTime.Now - donor.LastDonationDate.Value).TotalDays >= 56,
                DaysUntilEligible = donor.LastDonationDate.HasValue ? 
                                    Math.Max(0, 56 - (int)(DateTime.Now - donor.LastDonationDate.Value).TotalDays) : 0,
                RecentDonations = donor.Donations.OrderByDescending(d => d.DonationDate).Take(5).ToList()
            };

            return View(model);
        }

        // View Profile
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetUserId();
            var donor = _context.Donors
                .Include(d => d.User)
                .Include(d => d.BloodType)
                .FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Dashboard");
            }

            return View(donor);
        }

        // GET: Edit Profile
        public IActionResult EditProfile()
        {
            var userId = HttpContext.Session.GetUserId();
            var donor = _context.Donors
                .Include(d => d.User)
                .Include(d => d.BloodType)
                .FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Dashboard");
            }

            var model = new EditDonorProfileViewModel
            {
                FullName = donor.User.FullName,
                Phone = donor.User.Phone,
                Address = donor.Address,
                Gender = donor.Gender,
                MedicalNotes = donor.MedicalNotes
            };

            return View(model);
        }

        // POST: Edit Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditProfile(EditDonorProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetUserId();
                var donor = _context.Donors
                    .Include(d => d.User)
                    .FirstOrDefault(d => d.UserId == userId);

                if (donor == null)
                {
                    TempData["ErrorMessage"] = "Donor profile not found.";
                    return RedirectToAction("Dashboard");
                }

                // Update user info
                donor.User.FullName = model.FullName;
                donor.User.Phone = model.Phone;

                // Update donor info
                donor.Address = model.Address;
                donor.Gender = model.Gender;
                donor.MedicalNotes = model.MedicalNotes;

                _context.SaveChanges();

                // Update session name if changed
                HttpContext.Session.SetString("UserName", model.FullName);

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("Profile");
            }

            return View(model);
        }

        // GET: Create Donation
        public IActionResult CreateDonation()
        {
            var userId = HttpContext.Session.GetUserId();
            var donor = _context.Donors
                .Include(d => d.BloodType)
                .FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Dashboard");
            }

            // Check if donor can donate
            if (donor.LastDonationDate.HasValue)
            {
                var daysSinceLastDonation = (DateTime.Now - donor.LastDonationDate.Value).TotalDays;
                if (daysSinceLastDonation < 56)
                {
                    TempData["ErrorMessage"] = $"You must wait {56 - (int)daysSinceLastDonation} more days before donating again.";
                    return RedirectToAction("Dashboard");
                }
            }

            ViewBag.BloodType = donor.BloodType.TypeName;
            return View(new CreateDonationViewModel());
        }

        // POST: Create Donation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateDonation(CreateDonationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetUserId();
                var donor = _context.Donors.FirstOrDefault(d => d.UserId == userId);

                if (donor == null)
                {
                    TempData["ErrorMessage"] = "Donor profile not found.";
                    return RedirectToAction("Dashboard");
                }

                // Double-check eligibility
                if (donor.LastDonationDate.HasValue)
                {
                    var daysSinceLastDonation = (DateTime.Now - donor.LastDonationDate.Value).TotalDays;
                    if (daysSinceLastDonation < 56)
                    {
                        TempData["ErrorMessage"] = "You are not eligible to donate yet.";
                        return RedirectToAction("Dashboard");
                    }
                }

                var donation = new Donation
                {
                    DonorId = donor.Id,
                    DonationDate = model.DonationDate,
                    Quantity = model.Quantity,
                    Notes = model.Notes,
                    Status = "Pending",
                    CreatedAt = DateTime.Now
                };

                _context.Donations.Add(donation);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Donation request submitted successfully. Please wait for admin approval.";
                return RedirectToAction("Dashboard");
            }

            var userId2 = HttpContext.Session.GetUserId();
            var donor2 = _context.Donors.Include(d => d.BloodType).FirstOrDefault(d => d.UserId == userId2);
            ViewBag.BloodType = donor2?.BloodType.TypeName;

            return View(model);
        }

        // Donation History
        public IActionResult DonationHistory()
        {
            var userId = HttpContext.Session.GetUserId();
            var donor = _context.Donors
                .Include(d => d.BloodType)
                .Include(d => d.Donations)
                .FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Dashboard");
            }

            var donations = donor.Donations.OrderByDescending(d => d.DonationDate).ToList();
            return View(donations);
        }

        // Toggle Availability
        [HttpPost]
        public IActionResult ToggleAvailability()
        {
            var userId = HttpContext.Session.GetUserId();
            var donor = _context.Donors.FirstOrDefault(d => d.UserId == userId);

            if (donor == null)
            {
                TempData["ErrorMessage"] = "Donor profile not found.";
                return RedirectToAction("Dashboard");
            }

            // If trying to mark as available, check eligibility
            if (!donor.IsAvailable)
            {
                var canDonate = !donor.LastDonationDate.HasValue || 
                               (DateTime.Now - donor.LastDonationDate.Value).TotalDays >= 56;
                
                if (!canDonate)
                {
                    var daysUntilEligible = 56 - (int)(DateTime.Now - donor.LastDonationDate!.Value).TotalDays;
                    return RedirectToAction("Dashboard");
                }
            }

            donor.IsAvailable = !donor.IsAvailable;
            _context.SaveChanges();

            TempData["SuccessMessage"] = $"Your availability has been updated to {(donor.IsAvailable ? "Available" : "Not Available")}.";
            return RedirectToAction("Dashboard");
        }
    }

    // View Models
    public class DonorDashboardViewModel
    {
        public Donor Donor { get; set; } = null!;
        public int TotalDonations { get; set; }
        public int CompletedDonations { get; set; }
        public int PendingDonations { get; set; }
        public DateTime? LastDonationDate { get; set; }
        public bool CanDonateAgain { get; set; }
        public int DaysUntilEligible { get; set; }
        public List<Donation> RecentDonations { get; set; } = new();
    }

    public class EditDonorProfileViewModel
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }

        [Required]
        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Gender { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MedicalNotes { get; set; }
    }

    public class CreateDonationViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Preferred Donation Date")]
        public DateTime DonationDate { get; set; } = DateTime.Now.AddDays(1);

        [Required]
        [Range(350, 500)]
        [Display(Name = "Quantity (ml)")]
        public int Quantity { get; set; } = 450;

        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
