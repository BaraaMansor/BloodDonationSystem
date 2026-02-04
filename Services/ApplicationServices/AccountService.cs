using BloodDonationSystem.Data;
using BloodDonationSystem.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace BloodDonationSystem.Services.ApplicationServices;

public class AccountService
{
    private readonly ApplicationDbContext _context;

    public AccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public List<BloodType> GetBloodTypes()
    {
        return _context.BloodTypes.ToList();
    }

    public bool EmailExists(string email)
    {
        return _context.Users.Any(u => u.Email == email);
    }

    public LoginResult AuthenticateUser(string email, string password)
    {
        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password"
            };
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Invalid email or password"
            };
        }

        if (!user.IsActive)
        {
            return new LoginResult
            {
                Success = false,
                ErrorMessage = "Your account is inactive. Please contact admin."
            };
        }

        return new LoginResult
        {
            Success = true,
            User = user,
            RedirectController = GetRedirectController(user.Role)
        };
    }

    private string GetRedirectController(string role)
    {
        return role switch
        {
            "Admin" => "Admin",
            "Donor" => "Donor",
            "Hospital" => "Hospital",
            _ => "Home"
        };
    }

    public User RegisterUser(string fullName, string email, string password, string role, string? phone)
    {
        var user = new User
        {
            FullName = fullName,
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Phone = phone,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return user;
    }

    public void RegisterDonor(int userId, int bloodTypeId, DateTime? dateOfBirth, string? gender, string? address)
    {
        var donor = new Donor
        {
            UserId = userId,
            BloodTypeId = bloodTypeId,
            DateOfBirth = dateOfBirth ?? DateTime.Now,
            Gender = gender ?? "Male",
            Address = address ?? "",
            IsAvailable = true,
            CreatedAt = DateTime.Now
        };

        _context.Donors.Add(donor);
        _context.SaveChanges();
    }
}

public class LoginResult
{
    public bool Success { get; set; }
    public User? User { get; set; }
    public string? ErrorMessage { get; set; }
    public string? RedirectController { get; set; }
}
