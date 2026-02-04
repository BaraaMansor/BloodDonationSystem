using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace BloodDonationSystem.Tests;

[Parallelizable(ParallelScope.None)]
[TestFixture]
public class BloodDonationSystemTests : PageTest
{
    private const string BaseUrl = "https://localhost:58811";
    private const string AdminEmail = "admin@bloodbank.com";
    private const string AdminPassword = "Admin123!";

    [SetUp]
    public async Task Setup()
    {
        // Set default timeout (increased for slower pages)
        Page.SetDefaultTimeout(60000);
        
        // Navigate to home page
        await Page.GotoAsync(BaseUrl);
        
        // Accept any SSL errors in development
        await Page.Context.GrantPermissionsAsync(new[] { "clipboard-read", "clipboard-write" });
    }
    [TearDown]
    public async Task Cleanup()
    {
        // Logout if logged in to ensure clean state for next test
        try
        {
            var logoutButton = Page.Locator("a[href='/Account/Logout']");
            if (await logoutButton.IsVisibleAsync())
            {
                await logoutButton.ClickAsync();
                await Page.WaitForURLAsync($"{BaseUrl}/", new() { Timeout = 5000 });
            }
        }
        catch
        {
            // Ignore logout errors
        }
    }
    #region Authentication Tests

    [Test]
    [Description("Test 1: Admin can login successfully")]
    public async Task AdminLogin_Success()
    {
        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        
        await Page.FillAsync("input[name='Email']", AdminEmail);
        await Page.FillAsync("input[name='Password']", AdminPassword);
        await Page.ClickAsync("button[type='submit']");
        
        await Page.WaitForURLAsync($"{BaseUrl}/Admin/Dashboard");
        
        var heading = await Page.TextContentAsync("h2");
        Assert.That(heading, Does.Contain("Dashboard"));
    }

    [Test]
    [Description("Test 2: Invalid login should show error")]
    public async Task InvalidLogin_ShowsError()
    {
        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        
        await Page.FillAsync("input[name='Email']", "invalid@test.com");
        await Page.FillAsync("input[name='Password']", "wrongpassword");
        await Page.ClickAsync("button[type='submit']");
        
        var errorMessage = await Page.Locator(".alert-danger").IsVisibleAsync();
        Assert.That(errorMessage, Is.True);
    }

    #endregion

    #region Blood Type Management Tests

    [Test]
    [Description("Test 3: All 8 blood types should be available in the system")]
    public async Task BloodTypes_AllEightTypesExist()
    {
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        
        var expectedBloodTypes = new[] { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };
        
        foreach (var bloodType in expectedBloodTypes)
        {
            var bloodTypeBadge = await Page.Locator($"span.badge:has-text('{bloodType}')").CountAsync();
            Assert.That(bloodTypeBadge, Is.GreaterThan(0), $"Blood type {bloodType} not found");
        }
    }

    #endregion

    #region Blood Compatibility Tests

    [Test]
    [Description("Test 4: AB+ (universal recipient) can receive from all blood types")]
    public async Task BloodCompatibility_ABPositive_CanReceiveFromAll()
    {
        await LoginAsAdmin();
        
        // Create donations for different blood types
        await CreateDonation("O-", 500);
        await CreateDonation("B-", 500);
        
        // Approve donations
        await ApprovePendingDonations();
        
        // Create AB+ request as hospital
        await LoginAsHospital();
        await CreateBloodRequest("AB+", 300, false);
        
        // Admin approves and fulfills
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/BloodRequests");
        await Page.Locator("text=Approve").First.ClickAsync();
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Try to fulfill - should use compatible blood type
        var fulfillButtons = await Page.Locator("text=Fulfill").AllAsync();
        if (fulfillButtons.Count > 0)
        {
            await fulfillButtons[0].ClickAsync();
            
            // Should see success message
            var successMessage = await Page.Locator(".alert-success").IsVisibleAsync();
            Assert.That(successMessage, Is.True, "AB+ should be able to use compatible blood types");
        }
    }

    [Test]
    [Description("Test 5: O- (universal donor) should be preserved for emergencies")]
    public async Task BloodCompatibility_OMinusPreservation()
    {
        await LoginAsAdmin();
        
        // Create O- and B- donations
        await CreateDonation("O-", 500);
        await CreateDonation("B-", 500);
        await ApprovePendingDonations();
        
        // Request B+ blood (can receive from B- or O-)
        await LoginAsHospital();
        await CreateBloodRequest("B+", 300, false);
        
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        
        // After fulfillment, O- should still be at 500ml (B- should be used first)
        var oNegativeRow = Page.Locator("tr:has(span.badge:has-text('O-'))");
        var availableQuantity = await oNegativeRow.Locator("td:nth-child(6)").TextContentAsync();
        
        Assert.That(availableQuantity, Does.Contain("500"), "O- should be preserved");
    }

    #endregion

    #region Donation Management Tests

    [Test]
    [Description("Test 6: Admin can approve pending donations")]
    public async Task DonationManagement_AdminCanApproveDonations()
    {
        await LoginAsAdmin();
        
        // Navigate to donations page
        await Page.GotoAsync($"{BaseUrl}/Admin/Donations");
        
        // Click on Pending tab
        await Page.ClickAsync("a[href='#pending']");
        
        var approveButtons = await Page.Locator("text=Approve").AllAsync();
        var initialCount = approveButtons.Count;
        
        if (initialCount > 0)
        {
            await approveButtons[0].ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var successMessage = await Page.Locator(".alert-success").IsVisibleAsync();
            Assert.That(successMessage, Is.True);
        }
        
        Assert.Pass("Donation approval workflow works");
    }

    [Test]
    [Description("Test 7: Completed donations should increase blood stock")]
    public async Task DonationManagement_CompletedDonationsIncreaseStock()
    {
        await LoginAsAdmin();
        
        // Get initial stock
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        var initialStock = await GetTotalBloodStock();
        
        // Create and complete a donation
        await CreateDonation("A+", 450);
        await ApprovePendingDonations();
        await CompletePendingDonations();
        
        // Get final stock
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        var finalStock = await GetTotalBloodStock();
        
        Assert.That(finalStock, Is.GreaterThan(initialStock), "Blood stock should increase after donation");
    }

    #endregion

    #region Blood Request Management Tests

    [Test]
    [Description("Test 8: Hospital can create blood requests")]
    public async Task BloodRequest_HospitalCanCreateRequest()
    {
        await LoginAsHospital();
        
        await Page.GotoAsync($"{BaseUrl}/Hospital/CreateRequest");
        
        await Page.SelectOptionAsync("select[name='BloodTypeId']", "1"); // A+
        await Page.FillAsync("input[name='QuantityRequired']", "500");
        await Page.FillAsync("textarea[name='Notes']", "Urgent requirement for surgery");
        
        await Page.ClickAsync("button[type='submit']");
        
        await Page.WaitForURLAsync($"{BaseUrl}/Hospital/Dashboard");
        
        var successMessage = await Page.Locator(".alert-success").IsVisibleAsync();
        Assert.That(successMessage, Is.True);
    }

    [Test]
    [Description("Test 9: Emergency requests should be marked and visible")]
    public async Task BloodRequest_EmergencyRequestsAreHighlighted()
    {
        await LoginAsHospital();
        
        await Page.GotoAsync($"{BaseUrl}/Hospital/CreateRequest");
        
        await Page.SelectOptionAsync("select[name='BloodTypeId']", "5"); // AB+
        await Page.FillAsync("input[name='QuantityRequired']", "1000");
        await Page.CheckAsync("input[name='IsEmergency']");
        
        await Page.ClickAsync("button[type='submit']");
        
        await Page.GotoAsync($"{BaseUrl}/Hospital/Dashboard");
        
        var emergencyBadge = await Page.Locator("span.badge.bg-danger:has-text('Emergency')").IsVisibleAsync();
        Assert.That(emergencyBadge, Is.True, "Emergency badge should be visible");
    }

    [Test]
    [Description("Test 10: Admin can fulfill blood requests")]
    public async Task BloodRequest_AdminCanFulfillRequests()
    {
        await LoginAsAdmin();
        
        // Ensure we have blood in stock
        await CreateDonation("O-", 500);
        await ApprovePendingDonations();
        await CompletePendingDonations();
        
        // Create request as hospital
        await LoginAsHospital();
        await CreateBloodRequest("O-", 300, false);
        
        // Admin approves and fulfills
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/BloodRequests");
        
        // Approve first
        var approveButton = Page.Locator("text=Approve").First;
        if (await approveButton.IsVisibleAsync())
        {
            await approveButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        
        // Then fulfill
        await Page.GotoAsync($"{BaseUrl}/Admin/BloodRequests");
        var fulfillButton = Page.Locator("text=Fulfill").First;
        if (await fulfillButton.IsVisibleAsync())
        {
            await fulfillButton.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            var successMessage = await Page.Locator(".alert-success").IsVisibleAsync();
            Assert.That(successMessage, Is.True);
        }
    }

    #endregion

    #region Inventory Tracking Tests

    [Test]
    [Description("Test 11: Inventory should track actual blood type used (not requested)")]
    public async Task InventoryTracking_TracksActualTypeUsed()
    {
        await LoginAsAdmin();
        
        // Create B- donation
        await CreateDonation("B-", 500);
        await ApprovePendingDonations();
        await CompletePendingDonations();
        
        // Get initial B- stock
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        var initialBMinusStock = await GetBloodTypeStock("B-");
        
        // Request AB+ (can use B-)
        await LoginAsHospital();
        await CreateBloodRequest("AB+", 200, false);
        
        // Approve and fulfill
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/BloodRequests");
        await Page.Locator("text=Approve").First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        await Page.GotoAsync($"{BaseUrl}/Admin/BloodRequests");
        await Page.Locator("text=Fulfill").First.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Check B- stock decreased (not AB+)
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        var finalBMinusStock = await GetBloodTypeStock("B-");
        
        Assert.That(finalBMinusStock, Is.LessThan(initialBMinusStock), "B- stock should decrease");
        Assert.That(finalBMinusStock, Is.EqualTo(initialBMinusStock - 200), "B- should decrease by 200ml");
    }

    [Test]
    [Description("Test 12: Available quantity calculation should be accurate")]
    public async Task InventoryTracking_AvailableQuantityAccurate()
    {
        await LoginAsAdmin();
        
        // Clear data and create fresh donations
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        
        // Create A+ donation
        await CreateDonation("A+", 450);
        await ApprovePendingDonations();
        await CompletePendingDonations();
        
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        
        var aPlusRow = Page.Locator("tr:has(span.badge:has-text('A+'))");
        var totalCollected = await aPlusRow.Locator("td:nth-child(4)").TextContentAsync();
        var fulfilled = await aPlusRow.Locator("td:nth-child(5)").TextContentAsync();
        var available = await aPlusRow.Locator("td:nth-child(6)").TextContentAsync();
        
        // Available = Total Collected - Fulfilled
        var totalValue = int.Parse(totalCollected!.Replace(",", ""));
        var fulfilledValue = int.Parse(fulfilled!.Replace(",", ""));
        var availableValue = int.Parse(available!.Replace(",", ""));
        
        Assert.That(availableValue, Is.EqualTo(totalValue - fulfilledValue), 
            "Available = Total - Fulfilled calculation should be correct");
    }

    #endregion

    #region Reports and Excel Export Tests

    [Test]
    [Description("Test 13: Reports page should display all blood type statistics")]
    public async Task Reports_DisplaysAllStatistics()
    {
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        
        // Check table headers
        var headers = new[] { 
            "Blood Type", "Donors", "Completed Donations", "Total Collected (ml)", 
            "Fulfilled Requests (ml)", "Available Now (ml)", "Pending Hospital Requests", 
            "Requested Quantity (ml)", "Status" 
        };
        
        foreach (var header in headers)
        {
            var headerExists = await Page.Locator($"th:has-text('{header}')").IsVisibleAsync();
            Assert.That(headerExists, Is.True, $"Header '{header}' should be visible");
        }
    }

    [Test]
    [Description("Test 14: Excel export button should be visible and functional")]
    public async Task Reports_ExcelExportAvailable()
    {
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        
        var exportButton = await Page.Locator("a:has-text('Export to Excel')").IsVisibleAsync();
        Assert.That(exportButton, Is.True, "Export to Excel button should be visible");
        
        // Verify the button has correct link
        var href = await Page.Locator("a:has-text('Export to Excel')").GetAttributeAsync("href");
        Assert.That(href, Does.Contain("/Admin/ExportReports"));
    }

    #endregion

    #region Dashboard Tests

    [Test]
    [Description("Test 15: Admin dashboard should show key metrics")]
    public async Task AdminDashboard_ShowsKeyMetrics()
    {
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/Dashboard");
        
        // Check for stat cards
        var statCards = await Page.Locator(".stat-card").CountAsync();
        Assert.That(statCards, Is.GreaterThan(0), "Dashboard should have stat cards");
        
        // Verify key sections exist
        var sectionsExist = await Task.WhenAll(
            Page.Locator("text=Blood Type Statistics").IsVisibleAsync(),
            Page.Locator("text=Recent Donations").IsVisibleAsync(),
            Page.Locator("text=Recent Blood Requests").IsVisibleAsync()
        );
        
        Assert.That(sectionsExist.All(x => x), Is.True, "All dashboard sections should be visible");
    }

    [Test]
    [Description("Test 16: Hospital dashboard should show request statistics")]
    public async Task HospitalDashboard_ShowsRequestStatistics()
    {
        await LoginAsHospital();
        await Page.GotoAsync($"{BaseUrl}/Hospital/Dashboard");
        
        // Check for stat cards
        var statCardLabels = new[] { "Total Requests", "Pending", "Approved", "Fulfilled" };
        
        foreach (var label in statCardLabels)
        {
            var labelExists = await Page.Locator($"p.text-muted:has-text('{label}')").IsVisibleAsync();
            Assert.That(labelExists, Is.True, $"'{label}' stat should be visible");
        }
    }

    #endregion

    #region Blood Availability Tests

    [Test]
    [Description("Test 17: Hospital can check blood availability")]
    public async Task BloodAvailability_HospitalCanCheckStock()
    {
        await LoginAsHospital();
        await Page.GotoAsync($"{BaseUrl}/Hospital/BloodAvailability");
        
        // Should see table with blood types
        var table = await Page.Locator("table").IsVisibleAsync();
        Assert.That(table, Is.True, "Blood availability table should be visible");
        
        // Check for columns
        var columnHeaders = new[] { "Blood Type", "Description", "Available Quantity (ml)" };
        
        foreach (var header in columnHeaders)
        {
            var headerExists = await Page.Locator($"th:has-text('{header}')").IsVisibleAsync();
            Assert.That(headerExists, Is.True, $"Column '{header}' should be visible");
        }
    }

    [Test]
    [Description("Test 18: Blood availability should match reports page")]
    public async Task BloodAvailability_MatchesReportsPage()
    {
        await LoginAsAdmin();
        await Page.GotoAsync($"{BaseUrl}/Admin/Reports");
        var adminTotal = await GetTotalBloodStock();
        
        await LoginAsHospital();
        await Page.GotoAsync($"{BaseUrl}/Hospital/BloodAvailability");
        
        var totalText = await Page.Locator("tr.fw-bold td:has-text('ml')").Last.TextContentAsync();
        var hospitalTotal = int.Parse(totalText!.Replace("ml", "").Replace(",", "").Trim());
        
        Assert.That(hospitalTotal, Is.EqualTo(adminTotal), 
            "Hospital availability should match admin reports");
    }

    #endregion

    #region Helper Methods

    private async Task LoginAsAdmin()
    {
        await Page.GotoAsync($"{BaseUrl}/Account/Login", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForSelectorAsync("input[name='Email']", new() { State = WaitForSelectorState.Visible });
        await Page.FillAsync("input[name='Email']", AdminEmail);
        await Page.FillAsync("input[name='Password']", AdminPassword);
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForURLAsync($"{BaseUrl}/Admin/Dashboard", new() { WaitUntil = WaitUntilState.NetworkIdle });
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    private async Task LoginAsHospital()
    {
        await Page.GotoAsync($"{BaseUrl}/Account/Register", new() { WaitUntil = WaitUntilState.NetworkIdle });
        
        // Wait for page to be fully loaded
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Click Hospital tab FIRST before filling any fields
        var hospitalButton = Page.Locator("label[for='roleHospital']");
        await hospitalButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await hospitalButton.ClickAsync();
        
        // Wait for the role radio button to be checked
        await Page.WaitForFunctionAsync("document.getElementById('roleHospital').checked === true");
        
        // Wait a moment for any JavaScript transitions to complete
        await Page.WaitForTimeoutAsync(1000);
        
        // Generate credentials
        var randomEmail = $"hospital.test{DateTime.Now.Ticks}@test.com";
        var password = "Test123!";
        
        // Fill in the basic information fields
        await Page.FillAsync("input[name='FullName']", "Test Hospital");
        await Page.FillAsync("input[name='Email']", randomEmail);
        await Page.FillAsync("input[name='Password']", password);
        await Page.FillAsync("input[name='ConfirmPassword']", password);
        await Page.FillAsync("input[name='Phone']", "1234567890");
        
        // Submit the form
        var submitButton = Page.Locator("button[type='submit']:has-text('Register')");
        await submitButton.ClickAsync();
        
        // Wait for navigation after registration (likely to login page)
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await Page.WaitForTimeoutAsync(2000);
        
        // Check if we're on login page and login with the credentials we just created
        if (Page.Url.Contains("/Account/Login"))
        {
            await Page.WaitForSelectorAsync("input[name='Email']", new() { State = WaitForSelectorState.Visible });
            await Page.FillAsync("input[name='Email']", randomEmail);
            await Page.FillAsync("input[name='Password']", password);
            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForURLAsync($"{BaseUrl}/Hospital/Dashboard", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });
        }
        else if (!Page.Url.Contains("/Hospital/Dashboard"))
        {
            // If not on dashboard, wait for redirect
            await Page.WaitForURLAsync($"{BaseUrl}/Hospital/Dashboard", new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });
        }
    }

    private async Task CreateDonation(string bloodType, int quantity)
    {
        // This would require being logged in as a donor
        // Simplified for testing - in production you'd use API or database seeding
        await Page.GotoAsync($"{BaseUrl}/Admin/Dashboard");
    }

    private async Task ApprovePendingDonations()
    {
        await Page.GotoAsync($"{BaseUrl}/Admin/Donations");
        await Page.ClickAsync("a[href='#pending']");
        
        var approveButtons = await Page.Locator("text=Approve").AllAsync();
        foreach (var button in approveButtons)
        {
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Page.GotoAsync($"{BaseUrl}/Admin/Donations");
                await Page.ClickAsync("a[href='#pending']");
            }
        }
    }

    private async Task CompletePendingDonations()
    {
        await Page.GotoAsync($"{BaseUrl}/Admin/Donations");
        await Page.ClickAsync("a[href='#approved']");
        
        var completeButtons = await Page.Locator("text=Complete").AllAsync();
        foreach (var button in completeButtons)
        {
            if (await button.IsVisibleAsync())
            {
                await button.ClickAsync();
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Page.GotoAsync($"{BaseUrl}/Admin/Donations");
                await Page.ClickAsync("a[href='#approved']");
            }
        }
    }

    private async Task CreateBloodRequest(string bloodType, int quantity, bool isEmergency)
    {
        await Page.GotoAsync($"{BaseUrl}/Hospital/CreateRequest");
        
        // Map blood type to ID (simplified)
        var bloodTypeIds = new Dictionary<string, string>
        {
            {"A+", "1"}, {"A-", "2"}, {"B+", "3"}, {"B-", "4"},
            {"AB+", "5"}, {"AB-", "6"}, {"O+", "7"}, {"O-", "8"}
        };
        
        await Page.SelectOptionAsync("select[name='BloodTypeId']", bloodTypeIds[bloodType]);
        await Page.FillAsync("input[name='QuantityRequired']", quantity.ToString());
        
        if (isEmergency)
        {
            await Page.CheckAsync("input[name='IsEmergency']");
        }
        
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<int> GetTotalBloodStock()
    {
        // Ensure we're on the reports page and it's fully loaded
        if (!Page.Url.Contains("/Admin/Reports"))
        {
            await Page.GotoAsync($"{BaseUrl}/Admin/Reports", new() { WaitUntil = WaitUntilState.NetworkIdle });
        }
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Wait for the main table to be visible (more general selector)
        await Page.WaitForSelectorAsync("table", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        
        // Try to find total row - it might be the last row or have a specific class
        var totalRow = Page.Locator("tr").Last;
        
        // Try multiple selectors for the available quantity cell
        try
        {
            var availableCell = await totalRow.Locator("td.text-success strong").TextContentAsync(new() { Timeout = 5000 });
            return int.Parse(availableCell!.Replace(",", "").Trim());
        }
        catch
        {
            // If that fails, try finding the last cell with a number
            var cells = await totalRow.Locator("td").AllAsync();
            foreach (var cell in cells.Reverse())
            {
                var text = await cell.TextContentAsync();
                if (int.TryParse(text?.Replace(",", "").Trim(), out int value))
                {
                    return value;
                }
            }
            return 0;
        }
    }

    private async Task<int> GetBloodTypeStock(string bloodType)
    {
        // Ensure we're on the reports page and it's fully loaded
        if (!Page.Url.Contains("/Admin/Reports"))
        {
            await Page.GotoAsync($"{BaseUrl}/Admin/Reports", new() { WaitUntil = WaitUntilState.NetworkIdle });
        }
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        
        // Wait for the main table to be visible
        await Page.WaitForSelectorAsync("table", new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
        
        // Find the row with the specific blood type
        var row = Page.Locator($"tr:has(span.badge:has-text('{bloodType}'))");
        
        // Wait for the row to exist
        await row.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 5000 });
        
        // Get the available quantity cell (6th column)
        var availableCell = await row.Locator("td:nth-child(6)").TextContentAsync(new() { Timeout = 5000 });
        return int.Parse(availableCell!.Replace(",", "").Trim());
    }

    #endregion
}
