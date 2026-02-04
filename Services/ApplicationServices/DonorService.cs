using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace BloodDonationSystem.Services.ApplicationServices;

public class DonorService
{
    private readonly ApplicationDbContext _context;

    public DonorService(ApplicationDbContext context)
    {
        _context = context;
    }

    public DashboardInfo? GetDashboardData(int userId)
    {
        var donor = _context.Donors
            .Include(d => d.User)
            .Include(d => d.BloodType)
            .Include(d => d.Donations)
            .FirstOrDefault(d => d.UserId == userId);

        if (donor == null)
            return null;

        var canDonateAgain = !donor.LastDonationDate.HasValue ||
                            (DateTime.Now - donor.LastDonationDate.Value).TotalDays >= 90;

        var daysUntilEligible = donor.LastDonationDate.HasValue
            ? Math.Max(0, 90 - (int)(DateTime.Now - donor.LastDonationDate.Value).TotalDays)
            : 0;

        return new DashboardInfo
        {
            Donor = donor,
            TotalDonations = donor.Donations.Count,
            CompletedDonations = donor.Donations.Count(d => d.Status == "Completed"),
            PendingDonations = donor.Donations.Count(d => d.Status == "Pending"),
            LastDonationDate = donor.LastDonationDate,
            CanDonateAgain = canDonateAgain,
            DaysUntilEligible = daysUntilEligible,
            RecentDonations = donor.Donations.OrderByDescending(d => d.DonationDate).ToList()
        };
    }

    public Donor? GetDonorProfile(int userId)
    {
        return _context.Donors
            .Include(d => d.User)
            .Include(d => d.BloodType)
            .FirstOrDefault(d => d.UserId == userId);
    }

    public Donor? GetDonorWithUser(int userId)
    {
        return _context.Donors
            .Include(d => d.User)
            .Include(d => d.BloodType)
            .FirstOrDefault(d => d.UserId == userId);
    }

    public OperationResult UpdateProfile(int userId, string fullName, string? phone, string address, string gender, string? medicalNotes)
    {
        var donor = _context.Donors
            .Include(d => d.User)
            .FirstOrDefault(d => d.UserId == userId);

        if (donor == null)
        {
            return new OperationResult { Success = false, Message = "Donor profile not found." };
        }

        donor.User.FullName = fullName;
        donor.User.Phone = phone;
        donor.Address = address;
        donor.Gender = gender;
        donor.MedicalNotes = medicalNotes;

        _context.SaveChanges();

        return new OperationResult
        {
            Success = true,
            Message = "Profile updated successfully.",
            Data = fullName
        };
    }

    public DonationEligibility CheckDonationEligibility(int userId)
    {
        var donor = _context.Donors
            .Include(d => d.BloodType)
            .FirstOrDefault(d => d.UserId == userId);

        if (donor == null)
        {
            return new DonationEligibility
            {
                IsEligible = false,
                ErrorMessage = "Donor profile not found."
            };
        }

        if (donor.LastDonationDate.HasValue)
        {
            var daysSinceLastDonation = (DateTime.Now - donor.LastDonationDate.Value).TotalDays;
            if (daysSinceLastDonation < 90)
            {
                var daysRemaining = 90 - (int)daysSinceLastDonation;
                return new DonationEligibility
                {
                    IsEligible = false,
                    ErrorMessage = $"You must wait {daysRemaining} more days before donating again.",
                    BloodTypeName = donor.BloodType.TypeName
                };
            }
        }

        return new DonationEligibility
        {
            IsEligible = true,
            BloodTypeName = donor.BloodType.TypeName
        };
    }

    public OperationResult CreateDonation(int userId, DateTime donationDate, int quantity, string? notes)
    {
        var donor = _context.Donors.FirstOrDefault(d => d.UserId == userId);

        if (donor == null)
        {
            return new OperationResult { Success = false, Message = "Donor profile not found." };
        }

        // Double-check eligibility
        if (donor.LastDonationDate.HasValue)
        {
            var daysSinceLastDonation = (DateTime.Now - donor.LastDonationDate.Value).TotalDays;
            if (daysSinceLastDonation < 90)
            {
                return new OperationResult
                {
                    Success = false,
                    Message = "You are not eligible to donate yet."
                };
            }
        }

        var donation = new Donation
        {
            DonorId = donor.Id,
            DonationDate = donationDate,
            Quantity = quantity,
            Notes = notes,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        _context.Donations.Add(donation);
        _context.SaveChanges();

        return new OperationResult
        {
            Success = true,
            Message = "Donation request submitted successfully. Please wait for admin approval."
        };
    }

    public OperationResult ToggleAvailability(int userId)
    {
        var donor = _context.Donors.FirstOrDefault(d => d.UserId == userId);

        if (donor == null)
        {
            return new OperationResult { Success = false, Message = "Donor profile not found." };
        }

        // If trying to mark as available, check eligibility
        if (!donor.IsAvailable)
        {
            var canDonate = !donor.LastDonationDate.HasValue ||
                           (DateTime.Now - donor.LastDonationDate.Value).TotalDays >= 90;

            if (!canDonate)
            {
                var daysUntilEligible = 90 - (int)(DateTime.Now - donor.LastDonationDate!.Value).TotalDays;
                return new OperationResult
                {
                    Success = false,
                    Message = $"You must wait {daysUntilEligible} more days before becoming available."
                };
            }
        }

        donor.IsAvailable = !donor.IsAvailable;
        _context.SaveChanges();

        return new OperationResult
        {
            Success = true,
            Message = $"Your availability has been updated to {(donor.IsAvailable ? "Available" : "Not Available")}."
        };
    }
}

public class DashboardInfo
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

public class DonationEligibility
{
    public bool IsEligible { get; set; }
    public string? ErrorMessage { get; set; }
    public string BloodTypeName { get; set; } = string.Empty;
}
