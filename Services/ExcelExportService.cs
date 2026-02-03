using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using BloodDonationSystem.Controllers;

namespace BloodDonationSystem.Services;

public class ExcelExportService
{
    public byte[] GenerateReportsExcel(ReportsViewModel model)
    {
        // EPPlus requires a license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();

        // Create Summary Sheet
        CreateSummarySheet(package, model);

        // Create Blood Type Distribution Sheet
        CreateBloodTypeSheet(package, model);

        // Create Monthly Donations Sheet
        CreateMonthlyDonationsSheet(package, model);

        return package.GetAsByteArray();
    }

    private void CreateSummarySheet(ExcelPackage package, ReportsViewModel model)
    {
        var worksheet = package.Workbook.Worksheets.Add("Summary");

        // Title
        worksheet.Cells["A1"].Value = "Blood Donation System - Summary Report";
        worksheet.Cells["A1:B1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Report Date
        worksheet.Cells["A2"].Value = "Generated On:";
        worksheet.Cells["B2"].Value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        worksheet.Cells["A2:B2"].Style.Font.Italic = true;

        int row = 4;

        // Donor Statistics
        AddSectionHeader(worksheet, $"A{row}:B{row}", "Donor Statistics");
        row++;
        worksheet.Cells[$"A{row}"].Value = "Total Donors";
        worksheet.Cells[$"B{row}"].Value = model.TotalDonors;
        row++;
        worksheet.Cells[$"A{row}"].Value = "Active Donors";
        worksheet.Cells[$"B{row}"].Value = model.ActiveDonors;
        row += 2;

        // Donation Statistics
        AddSectionHeader(worksheet, $"A{row}:B{row}", "Donation Statistics");
        row++;
        worksheet.Cells[$"A{row}"].Value = "Total Donations";
        worksheet.Cells[$"B{row}"].Value = model.TotalDonations;
        row++;
        worksheet.Cells[$"A{row}"].Value = "Completed Donations";
        worksheet.Cells[$"B{row}"].Value = model.CompletedDonations;
        row += 2;

        // Blood Request Statistics
        AddSectionHeader(worksheet, $"A{row}:B{row}", "Blood Request Statistics");
        row++;
        worksheet.Cells[$"A{row}"].Value = "Total Blood Requests";
        worksheet.Cells[$"B{row}"].Value = model.TotalBloodRequests;
        row++;
        worksheet.Cells[$"A{row}"].Value = "Fulfilled Requests";
        worksheet.Cells[$"B{row}"].Value = model.FulfilledRequests;

        // Auto-fit columns
        worksheet.Cells.AutoFitColumns();
        worksheet.Column(1).Width = 30;
        worksheet.Column(2).Width = 20;
    }

    private void CreateBloodTypeSheet(ExcelPackage package, ReportsViewModel model)
    {
        var worksheet = package.Workbook.Worksheets.Add("Blood Type Distribution");

        // Title
        worksheet.Cells["A1"].Value = "Blood Type Distribution & Availability";
        worksheet.Cells["A1:I1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Headers
        int row = 3;
        worksheet.Cells[$"A{row}"].Value = "Blood Type";
        worksheet.Cells[$"B{row}"].Value = "Donors";
        worksheet.Cells[$"C{row}"].Value = "Completed Donations";
        worksheet.Cells[$"D{row}"].Value = "Total Collected (ml)";
        worksheet.Cells[$"E{row}"].Value = "Fulfilled Requests (ml)";
        worksheet.Cells[$"F{row}"].Value = "Available Now (ml)";
        worksheet.Cells[$"G{row}"].Value = "Pending Hospital Requests";
        worksheet.Cells[$"H{row}"].Value = "Requested Quantity (ml)";
        worksheet.Cells[$"I{row}"].Value = "Status";

        // Style headers
        using (var range = worksheet.Cells[$"A{row}:I{row}"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 53, 69)); // Bootstrap danger color
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        // Data
        row++;
        foreach (var item in model.BloodTypeDistribution)
        {
            var availability = item.AvailableQuantity == 0 ? "Out of Stock" : item.AvailableQuantity >= item.RequestedQuantity ? "Sufficient" : "Low Stock";
            
            worksheet.Cells[$"A{row}"].Value = item.BloodType;
            worksheet.Cells[$"B{row}"].Value = item.DonorCount;
            worksheet.Cells[$"C{row}"].Value = item.CompletedDonations;
            worksheet.Cells[$"D{row}"].Value = item.TotalQuantity;
            worksheet.Cells[$"E{row}"].Value = item.FulfilledQuantity;
            worksheet.Cells[$"F{row}"].Value = item.AvailableQuantity;
            worksheet.Cells[$"G{row}"].Value = item.PendingRequests;
            worksheet.Cells[$"H{row}"].Value = item.RequestedQuantity;
            worksheet.Cells[$"I{row}"].Value = availability;

            // Center align blood type
            worksheet.Cells[$"A{row}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            
            // Color code the status cell
            var statusCell = worksheet.Cells[$"I{row}"];
            statusCell.Style.Font.Bold = true;
            statusCell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            if (item.AvailableQuantity == 0)
            {
                statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(220, 53, 69)); // Red
                statusCell.Style.Font.Color.SetColor(Color.White);
            }
            else if (item.AvailableQuantity >= item.RequestedQuantity)
            {
                statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(25, 135, 84)); // Green
                statusCell.Style.Font.Color.SetColor(Color.White);
            }
            else
            {
                statusCell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                statusCell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 193, 7)); // Yellow
                statusCell.Style.Font.Color.SetColor(Color.Black);
            }
            
            row++;
        }

        // Add borders
        using (var range = worksheet.Cells[$"A3:I{row - 1}"])
        {
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        worksheet.Cells.AutoFitColumns();
    }

    private void CreateMonthlyDonationsSheet(ExcelPackage package, ReportsViewModel model)
    {
        var worksheet = package.Workbook.Worksheets.Add("Monthly Donations");

        // Title
        worksheet.Cells["A1"].Value = "Monthly Donation Statistics";
        worksheet.Cells["A1:C1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        // Headers
        int row = 3;
        worksheet.Cells[$"A{row}"].Value = "Month";
        worksheet.Cells[$"B{row}"].Value = "Donations Count";
        worksheet.Cells[$"C{row}"].Value = "Total Quantity (ml)";

        // Style headers
        using (var range = worksheet.Cells[$"A{row}:C{row}"])
        {
            range.Style.Font.Bold = true;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(13, 110, 253)); // Bootstrap primary color
            range.Style.Font.Color.SetColor(Color.White);
            range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        // Data
        row++;
        foreach (var item in model.MonthlyDonations)
        {
            worksheet.Cells[$"A{row}"].Value = item.Month;
            worksheet.Cells[$"B{row}"].Value = item.Count;
            worksheet.Cells[$"C{row}"].Value = item.TotalQuantity;
            row++;
        }

        // Add borders
        using (var range = worksheet.Cells[$"A3:C{row - 1}"])
        {
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        worksheet.Cells.AutoFitColumns();
        worksheet.Column(1).Width = 15;
    }

    private void AddSectionHeader(ExcelWorksheet worksheet, string range, string text)
    {
        var cell = worksheet.Cells[range];
        cell.Merge = true;
        cell.Value = text;
        cell.Style.Font.Bold = true;
        cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
        cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
        cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
    }
}
