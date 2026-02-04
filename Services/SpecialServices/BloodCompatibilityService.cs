namespace BloodDonationSystem.Services.SpecialServices;

public class BloodCompatibilityService
{
    // Dictionary of blood types and who they can receive blood from
    private static readonly Dictionary<string, List<string>> CompatibilityMap = new()
    {
        { "O-", new List<string> { "O-" } },
        { "O+", new List<string> { "O-", "O+" } },
        { "A-", new List<string> { "O-", "A-" } },
        { "A+", new List<string> { "O-", "O+", "A-", "A+" } },
        { "B-", new List<string> { "O-", "B-" } },
        { "B+", new List<string> { "O-", "O+", "B-", "B+" } },
        { "AB-", new List<string> { "O-", "A-", "B-", "AB-" } },
        { "AB+", new List<string> { "O-", "O+", "A-", "A+", "B-", "B+", "AB-", "AB+" } } // Universal recipient
    };

    public List<string> GetCompatibleDonors(string recipientBloodType)
    {
        if (CompatibilityMap.TryGetValue(recipientBloodType, out var compatibleDonors))
        {
            return compatibleDonors;
        }
        
        // If blood type not found, return empty list
        return new List<string>();
    }

    public bool IsCompatible(string donorBloodType, string recipientBloodType)
    {
        if (CompatibilityMap.TryGetValue(recipientBloodType, out var compatibleDonors))
        {
            return compatibleDonors.Contains(donorBloodType);
        }
        
        return false;
    }

    public List<string> GetFulfillmentPriority(string requestedBloodType)
    {
        var compatibleTypes = GetCompatibleDonors(requestedBloodType);
        
        // Exact match first
        var priority = new List<string>();
        if (compatibleTypes.Contains(requestedBloodType))
        {
            priority.Add(requestedBloodType);
        }

        // Then by scarcity (rarer types first to preserve universal donors like O-)
        var remainingTypes = compatibleTypes.Except(priority).ToList();
        
        // Priority order to preserve universal donors: 
        // Use specific types before universal types
        var scarcityOrder = new[] { "AB-", "B-", "A-", "AB+", "B+", "A+", "O+", "O-" };
        
        foreach (var bloodType in scarcityOrder)
        {
            if (remainingTypes.Contains(bloodType))
            {
                priority.Add(bloodType);
            }
        }
        
        return priority;
    }
}
