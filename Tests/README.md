# Blood Donation System - Automated Tests

## Overview
Automated end-to-end tests using Playwright to verify system requirements and functionality.

## Test Coverage

### Authentication (2 tests)
1. Admin can login successfully
2. Invalid login shows error message

### Blood Type Management (1 test)
3. All 8 blood types exist in system (A+, A-, B+, B-, AB+, AB-, O+, O-)

### Blood Compatibility (2 tests)
4. AB+ (universal recipient) can receive from all blood types
5. O- (universal donor) is preserved for emergencies

### Donation Management (2 tests)
6. Admin can approve pending donations
7. Completed donations increase blood stock

### Blood Request Management (3 tests)
8. Hospital can create blood requests
9. Emergency requests are marked and visible
10. Admin can fulfill blood requests

### Inventory Tracking (2 tests)
11. Inventory tracks actual blood type used (not requested) - **CRITICAL**
12. Available quantity calculation is accurate (Total - Fulfilled)

### Reports and Excel Export (2 tests)
13. Reports page displays all statistics
14. Excel export button is available and functional

### Dashboard (2 tests)
15. Admin dashboard shows key metrics
16. Hospital dashboard shows request statistics

### Blood Availability (2 tests)
17. Hospital can check blood availability
18. Blood availability matches admin reports page

## Requirements Verified

Based on the codebase analysis, these tests verify:

### Core Features
- ✅ User authentication (Admin, Hospital roles)
- ✅ Blood type management (all 8 types)
- ✅ Donation lifecycle (create, approve, complete)
- ✅ Blood request workflow (create, approve, fulfill)
- ✅ Emergency request prioritization
- ✅ Role-based access control

### Critical Business Logic
- ✅ Blood compatibility rules (ABO/Rh system)
- ✅ Universal recipient (AB+) can receive from all
- ✅ Universal donor (O-) preservation strategy
- ✅ Inventory tracking with actual blood type used
- ✅ Accurate stock calculations across views

### Reporting
- ✅ Reports page with 9 columns
- ✅ Excel export functionality
- ✅ Dashboard statistics
- ✅ Blood availability for hospitals

## Running the Tests

### Prerequisites
1. Application must be running on `https://localhost:58811`
2. Database must be initialized with seed data
3. Playwright browsers installed

### Run All Tests
```bash
# Start the application first
dotnet run

# In another terminal, run tests
dotnet test --settings Tests/playwright.runsettings
```

### Run Specific Test
```bash
dotnet test --filter "Name~AdminLogin_Success"
```

### Run Tests with UI
```bash
# Edit Tests/playwright.runsettings and set <Headless>false</Headless>
dotnet test --settings Tests/playwright.runsettings
```

## Test Configuration

`playwright.runsettings` controls:
- **Browser**: Chromium (can change to firefox/webkit)
- **Headless**: false (shows browser during test)
- **SlowMo**: 100ms delay between actions (for visibility)
- **Timeout**: 30 seconds per test
- **Workers**: 1 (sequential execution)

## Test Data

Tests use:
- **Admin Account**: admin@bloodbank.com / Admin123!
- **Dynamic Hospital Accounts**: Created during test execution
- **Sample Donations**: Created programmatically
- **Sample Requests**: Created via UI automation

## Known Limitations

1. **Test Data Isolation**: Tests currently share database state. Consider implementing:
   - Database transactions with rollback
   - Separate test database
   - Data cleanup after each test

2. **Donor Account Tests**: Currently limited because donor registration requires additional profile data. Can be extended.

3. **Async Operations**: Some tests may need additional wait times for slow systems.

## Extending Tests

To add new tests:

1. Create new test method with `[Test]` attribute
2. Add descriptive name and `[Description]` attribute
3. Follow AAA pattern (Arrange, Act, Assert)
4. Use helper methods for common operations
5. Clean up test data if needed

Example:
```csharp
[Test]
[Description("Test 19: Your test description")]
public async Task YourTestName()
{
    // Arrange
    await LoginAsAdmin();
    
    // Act
    await Page.GotoAsync($"{BaseUrl}/YourPage");
    
    // Assert
    var result = await Page.Locator("selector").IsVisibleAsync();
    Assert.That(result, Is.True);
}
```

## Continuous Integration

For CI/CD pipelines:

```yaml
# GitHub Actions example
- name: Install Playwright
  run: pwsh bin/Debug/net8.0/playwright.ps1 install chromium

- name: Run Tests
  run: dotnet test --settings Tests/playwright.runsettings --logger "trx;LogFileName=test-results.trx"

- name: Upload Results
  uses: actions/upload-artifact@v3
  with:
    name: test-results
    path: TestResults/
```

## Troubleshooting

### Application Not Running
Error: "Failed to connect to https://localhost:58811"
- Solution: Start the application with `dotnet run`

### SSL Certificate Errors
Error: "SSL certificate problem"
- Solution: Tests accept self-signed certificates by default

### Test Timeout
Error: "Timeout 30000ms exceeded"
- Solution: Increase timeout in playwright.runsettings or optimize test

### Element Not Found
Error: "Element not found: selector"
- Solution: Verify selectors, add wait conditions, check UI changes

## Test Results

After running tests, view results in:
- **Console**: Pass/fail summary
- **TestResults/**: Detailed TRX files
- **Screenshots**: (if enabled) on test failures

## Contact

For test-related issues or improvements, contact the development team.
