# Blood Donation System - Test Runner Script

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Blood Donation System - Test Runner" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Check if application is running
Write-Host "Checking if application is running..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "https://localhost:58811" -Method Head -TimeoutSec 5 -SkipCertificateCheck
    Write-Host "✓ Application is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Application is NOT running" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start the application first:" -ForegroundColor Yellow
    Write-Host "  dotnet run" -ForegroundColor White
    Write-Host ""
    exit 1
}

Write-Host ""

# Check for test mode
$testMode = Read-Host "Select test mode: [1] All Tests  [2] Single Test  [3] By Category (default: 1)"
if ([string]::IsNullOrWhiteSpace($testMode)) { $testMode = "1" }

Write-Host ""

switch ($testMode) {
    "1" {
        Write-Host "Running all tests..." -ForegroundColor Yellow
        Write-Host ""
        
        # Create TestResults folder if it doesn't exist
        if (-not (Test-Path "TestResults")) {
            New-Item -ItemType Directory -Path "TestResults" | Out-Null
            Write-Host "Created TestResults folder" -ForegroundColor Green
        }
        
        dotnet test --settings Tests/playwright.runsettings --logger "console;verbosity=detailed" --logger "trx;LogFileName=test-results.trx" --results-directory TestResults
    }
    "2" {
        Write-Host "Available tests:" -ForegroundColor Cyan
        Write-Host "  1. AdminLogin_Success" -ForegroundColor White
        Write-Host "  2. InvalidLogin_ShowsError" -ForegroundColor White
        Write-Host "  3. BloodTypes_AllEightTypesExist" -ForegroundColor White
        Write-Host "  4. BloodCompatibility_ABPositive_CanReceiveFromAll" -ForegroundColor White
        Write-Host "  5. BloodCompatibility_OMinusPreservation" -ForegroundColor White
        Write-Host ""
        $testName = Read-Host "Enter test number or name"
        
        $testNames = @{
            "1" = "AdminLogin_Success"
            "2" = "InvalidLogin_ShowsError"
            "3" = "BloodTypes_AllEightTypesExist"
            "4" = "BloodCompatibility_ABPositive_CanReceiveFromAll"
            "5" = "BloodCompatibility_OMinusPreservation"
        }
        
        $selectedTest = if ($testNames.ContainsKey($testName)) { $testNames[$testName] } else { $testName }
        
        Write-Host ""
        Write-Host "Running test: $selectedTest" -ForegroundColor Yellow
        Write-Host ""
        
        # Create TestResults folder if it doesn't exist
        if (-not (Test-Path "TestResults")) {
            New-Item -ItemType Directory -Path "TestResults" | Out-Null
        }
        
        dotnet test --settings Tests/playwright.runsettings --filter "Name~$selectedTest" --logger "console;verbosity=detailed" --logger "trx;LogFileName=test-results.trx" --results-directory TestResults
    }
    "3" {
        Write-Host "Test categories:" -ForegroundColor Cyan
        Write-Host "  1. Authentication" -ForegroundColor White
        Write-Host "  2. BloodCompatibility" -ForegroundColor White
        Write-Host "  3. DonationManagement" -ForegroundColor White
        Write-Host "  4. BloodRequest" -ForegroundColor White
        Write-Host "  5. InventoryTracking" -ForegroundColor White
        Write-Host "  6. Reports" -ForegroundColor White
        Write-Host "  7. Dashboard" -ForegroundColor White
        Write-Host "  8. BloodAvailability" -ForegroundColor White
        Write-Host ""
        $category = Read-Host "Enter category number or name"
        
        $categories = @{
            "1" = "Login"
            "2" = "BloodCompatibility"
            "3" = "DonationManagement"
            "4" = "BloodRequest"
            "5" = "InventoryTracking"
            "6" = "Reports"
            "7" = "Dashboard"
            "8" = "BloodAvailability"
        }
        
        $selectedCategory = if ($categories.ContainsKey($category)) { $categories[$category] } else { $category }
        
        Write-Host ""
        Write-Host "Running category: $selectedCategory" -ForegroundColor Yellow
        Write-Host ""
        
        # Create TestResults folder if it doesn't exist
        if (-not (Test-Path "TestResults")) {
            New-Item -ItemType Directory -Path "TestResults" | Out-Null
        }
        
        dotnet test --settings Tests/playwright.runsettings --filter "Name~$selectedCategory" --logger "console;verbosity=detailed" --logger "trx;LogFileName=test-results.trx" --results-directory TestResults
    }
    default {
        Write-Host "Invalid selection. Running all tests..." -ForegroundColor Yellow
        Write-Host ""
        
        # Create TestResults folder if it doesn't exist
        if (-not (Test-Path "TestResults")) {
            New-Item -ItemType Directory -Path "TestResults" | Out-Null
        }
        
        dotnet test --settings Tests/playwright.runsettings --logger "console;verbosity=detailed" --logger "trx;LogFileName=test-results.trx" --results-directory TestResults
    }
}

Write-Host ""
Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Test execution complete!" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Show test results location and files
if (Test-Path "TestResults") {
    Write-Host "Test results saved to: $(Resolve-Path 'TestResults')" -ForegroundColor Green
    Write-Host ""
    Write-Host "Result files:" -ForegroundColor Yellow
    Get-ChildItem -Path "TestResults" -File | ForEach-Object {
        Write-Host "  - $($_.Name) ($([math]::Round($_.Length/1KB, 2)) KB)" -ForegroundColor White
    }
    Write-Host ""
} else {
    Write-Host "No test results folder found. Tests may not have run successfully." -ForegroundColor Red
    Write-Host ""
}

Write-Host "For detailed test reports, open .trx files in Visual Studio or use:" -ForegroundColor Cyan
Write-Host "  trx2html TestResults\test-results.trx" -ForegroundColor White
Write-Host ""
