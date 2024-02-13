using iTextSharp.text.pdf;
using iTextSharp.text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.SqlServer.Server;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using RCBC.Interface;
using System.Diagnostics;
using System.Drawing.Printing;
using CsvHelper;
using System.Globalization;
using System.Text;
using System.IO;
using System.Drawing;
using RCBC.Models;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Data.SqlClient;
using System.Data;
using Dapper;

namespace RCBC.Controllers
{
    public class ReportsController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IGlobalRepository global;
        private readonly IWebHostEnvironment hostingEnvironment;
        public int GlobalUserId { get; set; }

        public ReportsController(IConfiguration _configuration, IGlobalRepository _global, IWebHostEnvironment _hostingEnvironment)
        {
            Configuration = _configuration;
            global = _global;
            hostingEnvironment = _hostingEnvironment;
        }

        private string GetConnectionString()
        {
            return Configuration.GetConnectionString("DefaultConnection");
        }
        public IActionResult LoadViews()
        {
            ViewBag.DateNow = DateTime.Now;
            ViewBag.Username = Request.Cookies["Username"];
            ViewBag.EmployeeName = Request.Cookies["EmployeeName"];
            ViewBag.UserRole = Request.Cookies["UserRole"];

            if (Request.Cookies["Username"] != null)
            {
                GlobalUserId = Request.Cookies["UserId"] != null ? Convert.ToInt32(Request.Cookies["UserId"]) : 0;

                if (GlobalUserId != 0)
                {
                    var chkStatus = global.CheckUserStatus(GlobalUserId);

                    if (chkStatus)
                    {
                        ViewBag.Modules = global.GetModulesByUserId(GlobalUserId);
                        ViewBag.SubModules = global.GetSubModulesByUserId(GlobalUserId);
                        ViewBag.ChildModules = global.GetChildModulesByUserId(GlobalUserId);

                        var user = global.GetUserInformation().Where(x => x.Id == GlobalUserId).FirstOrDefault();
                        ViewBag.Department = user.GroupDept;
                        ViewBag.DashboardDetails = global.GetDashboardDetails(user.GroupDept, user.UserRole);

                        var UserRoles = global.GetUserRole();
                        ViewBag.cmbUserRoles = new SelectList(UserRoles, "UserRole", "UserRole");

                        var Departments = global.GetDepartment();
                        ViewBag.cmbDepartments = new SelectList(Departments, "GroupDept", "GroupDept");

                        var EmailTypes = global.GetEmailType();
                        ViewBag.cmbEmailTypes = new SelectList(EmailTypes, "EmailType", "EmailType");

                        var Contacts = global.GetContacts().OrderBy(x => x.Id);
                        ViewBag.cmbContacts = new SelectList(Contacts, "Id", "ContactPerson");

                        var CorporateNames = global.GetCorporateClient();
                        ViewBag.cmbCorporateNames = new SelectList(CorporateNames, "CorporateName", "CorporateName");

                        var CorporateCodes = global.GetCorporateClient();
                        ViewBag.cmbCorporateCodes = new SelectList(CorporateCodes, "PartnerCode", "CorporateCode");

                        return View();
                    }
                    else
                    {
                        return RedirectToAction("LogoutAccount", "Home");
                    }
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public IActionResult AuditLogs()
        {
            return LoadViews();
        }

        public IActionResult ElectronicJournal()
        {
            return LoadViews();
        }

        public IActionResult DPUStatus()
        {
            return LoadViews();
        }

        public IActionResult AuditTrailSummary()
        {
            return LoadViews();
        }

        public IActionResult LoadAuditLogs(DateTime? DateFrom, DateTime? DateTo, string? EmployeeName, string? GroupDept, string? UserRole, string? Action)
        {
            var data = global.GetAuditlogsReport(DateFrom, DateTo, EmployeeName, GroupDept, UserRole, Action);

            return Json(new { data });
        }

        public IActionResult DownloadAuditLogs(string Type, DateTime? DateFrom, DateTime? DateTo, string? EmployeeName, string? GroupDept, string? UserRole, string? Status)
        {
            try
            {
                ////var contentRootPath = hostingEnvironment.ContentRootPath;

                var timestamp = DateTime.Now.ToString("hhmmss");
                var excelFileName = "AUDIT_REPORT_" + DateTime.Now.ToString("MMddyyyy") + timestamp + ".xlsx";
                var pdfFileName = "AUDIT_REPORT_" + DateTime.Now.ToString("MMddyyyy") + timestamp + ".pdf";
                var csvFileName = "AUDIT_REPORT_" + DateTime.Now.ToString("MMddyyyy") + timestamp + ".csv";

                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string downloadsFolderPath = Path.Combine(userProfilePath, "Downloads");

                var excelFullPath = Path.Combine(downloadsFolderPath, excelFileName);
                var pdfFullPath = Path.Combine(downloadsFolderPath, pdfFileName);
                var csvFullPath = Path.Combine(downloadsFolderPath, csvFileName);
                var DLFullPath = string.Empty;

                var data = global.GetAuditlogsReport(DateFrom, DateTo, EmployeeName, GroupDept, UserRole, Status);

                if (Type == "EXCEL")
                {
                    var fullPathWithName = Path.Combine(downloadsFolderPath, excelFileName);

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                        var headerCells = worksheet.Cells["A1:G1"];
                        headerCells.Style.Font.Bold = true;

                        // Merge and center cells A2:G2
                        worksheet.Cells["A2:G2"].Merge = true;
                        worksheet.Cells["A2:G2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["A2:G2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        worksheet.Cells["A2:G2"].Style.Font.Bold = true;

                        // Set the value for the merged and centered cell
                        worksheet.Cells["A2:G2"].Value = "DPU TELLERLESS USER AUDIT LOG REPORT";
                        worksheet.Cells["A4"].Value = "REPORT DATE: " + DateTime.Now.ToString("MM-dd-yyyy");
                        worksheet.Cells["A4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells["G4"].Value = "RUN DATE: " + DateTime.Now.ToString("MM-dd-yyyy");
                        worksheet.Cells["G4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                        worksheet.Cells["G5"].Value = "RUN TIME: " + DateTime.Now.ToString("HH:mm:ss");
                        worksheet.Cells["G5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                        worksheet.Cells["A7:G7"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["A7:G7"].Style.Font.Bold = true;

                        worksheet.Cells["A7"].Value = "Date Time";
                        worksheet.Column(1).Width = 25;
                        worksheet.Cells["B7"].Value = "IP Address";
                        worksheet.Column(2).Width = 25;
                        worksheet.Cells["C7"].Value = "User ID";
                        worksheet.Column(3).Width = 10;
                        worksheet.Cells["D7"].Value = "Module";
                        worksheet.Column(4).Width = 25;
                        worksheet.Cells["E7"].Value = "Modified By";
                        worksheet.Column(5).Width = 25;
                        worksheet.Cells["F7"].Value = "Activity";
                        worksheet.Column(6).Width = 20;
                        worksheet.Cells["G7"].Value = "Details";
                        worksheet.Column(7).Width = 25;

                        // Add data to the Excel worksheet
                        for (int i = 0; i < data.Count; i++)
                        {
                            var rowIndex = i + 8; // Starting from the eighth row (after the header)

                            // Format date using DateTime.ToString with a specific format
                            worksheet.Cells["A" + rowIndex].Value = data[i].DateModified.ToString("MM-dd-yyyy HH:mm:ss");
                            worksheet.Cells["A" + rowIndex].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                            worksheet.Cells["B" + rowIndex].Value = data[i].IP;
                            worksheet.Cells["C" + rowIndex].Value = data[i].ModifiedBy;
                            worksheet.Cells["D" + rowIndex].Value = data[i].Module;
                            worksheet.Cells["E" + rowIndex].Value = data[i].EmployeeName;
                            worksheet.Cells["F" + rowIndex].Value = data[i].Action;
                            worksheet.Cells["G" + rowIndex].Value = data[i].Details;

                            // Center the content in all cells
                            var dataCells = worksheet.Cells["A" + rowIndex + ":G" + rowIndex];
                            dataCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        }

                        package.SaveAs(fullPathWithName);

                        DLFullPath = excelFullPath;
                    }
                }
                else if (Type == "PDF")
                {
                    var pdfDocument = new Document();
                    var pdfWriter = PdfWriter.GetInstance(pdfDocument, new FileStream(pdfFullPath, FileMode.Create));
                    pdfDocument.Open();

                    // Define font style and size
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
                    var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                    // Header title
                    var hdrTitle = new PdfPTable(1);
                    hdrTitle.SetWidthPercentage(new float[] { 610f }, PageSize.A4);
                    var title = new PdfPCell(new Phrase("DPU TELLERLESS USER AUDIT LOG REPORT", titleFont));
                    title.HorizontalAlignment = Element.ALIGN_CENTER;
                    title.Border = PdfPCell.NO_BORDER;
                    hdrTitle.AddCell(title);
                    pdfDocument.Add(hdrTitle);

                    pdfDocument.Add(new Paragraph("\n"));

                    // Header table
                    var hdrTable = new PdfPTable(2);
                    hdrTable.SetWidthPercentage(new float[] { 305f, 305f }, PageSize.A4);
                    var reportDate = new PdfPCell(new Phrase("REPORT DATE: " + DateTime.Now.ToString("MM-dd-yyyy"), contentFont));
                    reportDate.HorizontalAlignment = Element.ALIGN_LEFT;
                    reportDate.Border = PdfPCell.NO_BORDER;
                    hdrTable.AddCell(reportDate);

                    var runDate = new PdfPCell(new Phrase("RUN DATE: " + DateTime.Now.ToString("MM-dd-yyyy"), contentFont));
                    runDate.HorizontalAlignment = Element.ALIGN_RIGHT;
                    runDate.Border = PdfPCell.NO_BORDER;
                    hdrTable.AddCell(runDate);
                    pdfDocument.Add(hdrTable);

                    // Header run time
                    var hdrRunTime = new PdfPTable(1);
                    hdrRunTime.SetWidthPercentage(new float[] { 610f }, PageSize.A4);
                    var runTime = new PdfPCell(new Phrase("RUN TIME: " + DateTime.Now.ToString("HH:mm:ss"), contentFont));
                    runTime.HorizontalAlignment = Element.ALIGN_RIGHT;
                    runTime.Border = PdfPCell.NO_BORDER;
                    hdrRunTime.AddCell(runTime);
                    pdfDocument.Add(hdrRunTime);

                    pdfDocument.Add(new Paragraph("\n"));

                    // Add a table to the PDF document with calculated width
                    var pdfTable = new PdfPTable(7);

                    // Set the width percentage of the table (relative to the page width)
                    pdfTable.SetWidthPercentage(new float[] { 100f, 110f, 60f, 100f, 80f, 80f, 80f }, PageSize.A4);

                    // Define font style and size for headers
                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11, BaseColor.BLACK);

                    // Add table headers
                    var headers = new string[] { "Date Time", "IP Address", "User ID", "Module", "Modified By", "Activity", "Details" };
                    foreach (var header in headers)
                    {
                        pdfTable.AddCell(new PdfPCell(new Phrase(header, headerFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        });
                    }

                    // Define font style and size for data cells
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                    // Add data to the PDF table
                    foreach (var logEntry in data)
                    {
                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.DateModified.ToString("MM-dd-yyyy HH:mm:ss"), dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.IP, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.ModifiedBy.ToString(), dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.Module, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.EmployeeName, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.Action, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.Details, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                    }

                    // Add the table to the PDF document
                    pdfDocument.Add(pdfTable);

                    // Close the PDF document
                    pdfDocument.Close();

                    DLFullPath = pdfFullPath;

                }
                else if (Type == "CSV")
                {
                    using (var writer = new StreamWriter(csvFullPath))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        // Add table headers
                        var headers = new string[] { "Date Time", "IP Address", "User ID", "Module", "Modified By", "Activity", "Details" };

                        // Write header with formatting
                        csv.WriteField("Date Time");
                        csv.WriteField("IP Address");
                        csv.WriteField("User ID");
                        csv.WriteField("Module");
                        csv.WriteField("Modified By");
                        csv.WriteField("Activity");
                        csv.WriteField("Details");
                        csv.NextRecord();

                        // Write data with formatting
                        for (int i = 0; i < data.Count; i++)
                        {
                            var rowIndex = i + 2; // Starting from the second row

                            csv.WriteField(data[i].DateModified);
                            csv.WriteField(data[i].IP);
                            csv.WriteField(data[i].ModifiedBy);
                            csv.WriteField(data[i].DateModified);
                            csv.WriteField(data[i].EmployeeName);
                            csv.WriteField(data[i].Action);
                            csv.WriteField(data[i].Details);
                            csv.NextRecord();

                        }
                    }

                    DLFullPath = csvFullPath;

                }
                return Json(new { success = true, message = $"Downloading Completed.\n File Path: {DLFullPath}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public IActionResult LoadDPUStatus(DateTime? DateFrom, DateTime? DateTo, string? LocationCode, string? BeneficiaryName, string? AccountNumber, string? Status)
        {
            var data = global.GetDPUStatusReport(DateFrom, DateTo, LocationCode, BeneficiaryName, AccountNumber, Status);

            return Json(new { data });
        }

        public IActionResult DownloadDPUStatus(string Type, DateTime? DateFrom, DateTime? DateTo, string? LocationCode, string? BeneficiaryName, string? AccountNumber, string? Status)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("hhmmss");
                var excelFileName = "DPU_REPORT_" + DateTime.Now.ToString("MMddyyyy") + timestamp + ".xlsx";
                var pdfFileName = "DPU_REPORT_" + DateTime.Now.ToString("MMddyyyy") + timestamp + ".pdf";
                var csvFileName = "DPU_REPORT_" + DateTime.Now.ToString("MMddyyyy") + timestamp + ".csv";

                string userProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string downloadsFolderPath = Path.Combine(userProfilePath, "Downloads");

                var excelFullPath = Path.Combine(downloadsFolderPath, excelFileName);
                var pdfFullPath = Path.Combine(downloadsFolderPath, pdfFileName);
                var csvFullPath = Path.Combine(downloadsFolderPath, csvFileName);
                var DLFullPath = string.Empty;

                var data = global.GetDPUStatusReport(DateFrom, DateTo, LocationCode, BeneficiaryName, AccountNumber, Status);

                if (Type == "EXCEL")
                {
                    var fullPathWithName = Path.Combine(downloadsFolderPath, excelFileName);

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                        var headerCells = worksheet.Cells["A1:K1"];
                        headerCells.Style.Font.Bold = true;

                        // Merge and center cells A2:G2
                        worksheet.Cells["A2:K2"].Merge = true;
                        worksheet.Cells["A2:K2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["A2:K2"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        worksheet.Cells["A2:K2"].Style.Font.Bold = true;
                        worksheet.Cells["A2:K2"].Value = "DPU TELLERLESS DPU STATUS REPORT";

                        worksheet.Cells["A4:K4"].Merge = true;
                        worksheet.Cells["A4:K4"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells["A4:K4"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        worksheet.Cells["A4:K4"].Value = "Subject: Credit Advice Report";

                        worksheet.Cells["A5:K5"].Merge = true;
                        worksheet.Cells["A5:K5"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells["A5:K5"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        worksheet.Cells["A5:K5"].Value = "Report Date: " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss");

                        worksheet.Cells["A6:K6"].Merge = true;
                        worksheet.Cells["A6:K6"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells["A6:K6"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        worksheet.Cells["A6:K6"].Value = "Value Date: " + DateTime.Now.ToString("MM-dd-yyyy");

                        worksheet.Cells["A7:K7"].Merge = true;
                        worksheet.Cells["A7:K7"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                        worksheet.Cells["A7:K7"].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                        worksheet.Cells["A7:K7"].Value = "Batch: 1";

                        worksheet.Cells["A9:K9"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["A9:K9"].Style.Font.Bold = true;

                        var headers = new string[] {
                            "No.",
                            "Location Code",
                            "Location Name",
                            "Transaction Date",
                            "Transaction Time",
                            "Card Number",
                            "Account Number",
                            "Beneficiary Name",
                            "Amount",
                            "Credit Description",
                            "External Reference"
                        };

                        worksheet.Cells["A9"].Value = "No.";
                        worksheet.Column(1).Width = 5;
                        worksheet.Cells["B9"].Value = "Location Code";
                        worksheet.Column(2).Width = 25;
                        worksheet.Cells["C9"].Value = "Location Name";
                        worksheet.Column(3).Width = 25;
                        worksheet.Cells["D9"].Value = "Transaction Date";
                        worksheet.Column(4).Width = 25;
                        worksheet.Cells["E9"].Value = "Transaction Time";
                        worksheet.Column(5).Width = 25;
                        worksheet.Cells["F9"].Value = "Card Number";
                        worksheet.Column(6).Width = 15;
                        worksheet.Cells["G9"].Value = "Account Number";
                        worksheet.Column(7).Width = 15;
                        worksheet.Cells["H9"].Value = "Beneficiary Name";
                        worksheet.Column(8).Width = 25;
                        worksheet.Cells["I9"].Value = "Amount";
                        worksheet.Column(9).Width = 10;
                        worksheet.Cells["J9"].Value = "Credit Description";
                        worksheet.Column(10).Width = 25;
                        worksheet.Cells["K9"].Value = "External Reference";
                        worksheet.Column(11).Width = 25;

                        int count = 1;
                        for (int i = 0; i < data.Count; i++)
                        {
                            var rowIndex = i + 10; // Starting from the 10th row

                            worksheet.Cells["A" + rowIndex].Value = count;
                            worksheet.Cells["B" + rowIndex].Value = data[i].LocationCode;
                            worksheet.Cells["C" + rowIndex].Value = data[i].LocationName;
                            worksheet.Cells["D" + rowIndex].Value = data[i].TransactionDate;
                            worksheet.Cells["D" + rowIndex].Style.Numberformat.Format = "MM/dd/yyyy";
                            worksheet.Cells["E" + rowIndex].Value = data[i].TransactionTime;
                            worksheet.Cells["E" + rowIndex].Style.Numberformat.Format = "HH:mm:ss";
                            worksheet.Cells["F" + rowIndex].Value = data[i].CardNumber;
                            worksheet.Cells["G" + rowIndex].Value = data[i].AccountNumber;
                            worksheet.Cells["H" + rowIndex].Value = data[i].BeneficiaryName;
                            worksheet.Cells["I" + rowIndex].Value = data[i].Amount;
                            worksheet.Cells["J" + rowIndex].Value = data[i].CreditDescription;
                            worksheet.Cells["K" + rowIndex].Value = data[i].ExternalReference;

                            // Center the content in all cells
                            var dataCells = worksheet.Cells["A" + rowIndex + ":K" + rowIndex];
                            worksheet.Cells["I" + rowIndex].Style.Numberformat.Format = "0.00";
                            dataCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                            count++;
                        }

                        // Calculate grand total
                        decimal? grandTotal = data.Sum(item => item.Amount); 

                        // Add grand total row
                        var grandTotalRowIndex = data.Count + 10; // Row index after the last data row
                        worksheet.Cells["H" + grandTotalRowIndex].Value = "Grand Total: ";
                        worksheet.Cells["I" + grandTotalRowIndex].Value = grandTotal;

                        // Center the content in grand total row
                        var grandTotalCells = worksheet.Cells["H" + grandTotalRowIndex + ":I" + (grandTotalRowIndex)];
                        grandTotalCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                        worksheet.Cells["H" + grandTotalRowIndex + ":I" + grandTotalRowIndex].Style.Font.Bold = true;

                        // Set money format for column H
                        worksheet.Cells["I" + grandTotalRowIndex].Style.Numberformat.Format = "0.00";

                        package.SaveAs(fullPathWithName);
                    }

                    DLFullPath = excelFullPath;

                }
                else if (Type == "PDF")
                {
                    var pdfDocument = new Document();
                    var pdfWriter = PdfWriter.GetInstance(pdfDocument, new FileStream(pdfFullPath, FileMode.Create));
                    pdfDocument.Open();

                    // Define font style and size
                    var titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.BLACK);
                    var contentFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);

                    // Header title
                    var hdrTitle = new PdfPTable(1);
                    hdrTitle.SetWidthPercentage(new float[] { 992f }, PageSize.LEGAL.Rotate());
                    var title = new PdfPCell(new Phrase("DPU TELLERLESS DPU STATUS REPORT", titleFont));
                    title.HorizontalAlignment = Element.ALIGN_CENTER;
                    title.Border = PdfPCell.NO_BORDER;
                    hdrTitle.AddCell(title);
                    pdfDocument.Add(hdrTitle);

                    pdfDocument.Add(new Paragraph("\n"));

                    // Header table
                    var hdrSubject = new PdfPTable(1);
                    hdrSubject.SetWidthPercentage(new float[] { 992f }, PageSize.LEGAL.Rotate());
                    var subject = new PdfPCell(new Phrase("Subject: Credit Advice Report", contentFont));
                    subject.HorizontalAlignment = Element.ALIGN_LEFT;
                    subject.Border = PdfPCell.NO_BORDER;
                    hdrSubject.AddCell(subject);
                    pdfDocument.Add(hdrSubject);

                    var hdrReportDate = new PdfPTable(1);
                    hdrReportDate.SetWidthPercentage(new float[] { 992f }, PageSize.LEGAL.Rotate());
                    var reportDate = new PdfPCell(new Phrase("Report Date: " + DateTime.Now.ToString("MM-dd-yyyy HH:mm:ss"), contentFont));
                    reportDate.HorizontalAlignment = Element.ALIGN_LEFT;
                    reportDate.Border = PdfPCell.NO_BORDER;
                    hdrReportDate.AddCell(reportDate);
                    pdfDocument.Add(hdrReportDate);

                    var hdrValueDate = new PdfPTable(1);
                    hdrValueDate.SetWidthPercentage(new float[] { 992f }, PageSize.LEGAL.Rotate());
                    var valueDate = new PdfPCell(new Phrase("Value Date: " + DateTime.Now.ToString("MM-dd-yyyy"), contentFont));
                    valueDate.HorizontalAlignment = Element.ALIGN_LEFT;
                    valueDate.Border = PdfPCell.NO_BORDER;
                    hdrValueDate.AddCell(valueDate);
                    pdfDocument.Add(hdrValueDate);

                    var hdrBatch = new PdfPTable(1);
                    hdrBatch.SetWidthPercentage(new float[] { 992f }, PageSize.LEGAL.Rotate());
                    var Batch = new PdfPCell(new Phrase("Batch: 1", contentFont));
                    Batch.HorizontalAlignment = Element.ALIGN_LEFT;
                    Batch.Border = PdfPCell.NO_BORDER;
                    hdrBatch.AddCell(Batch);
                    pdfDocument.Add(hdrBatch);

                    pdfDocument.Add(new Paragraph("\n"));

                    // Add a table to the PDF document
                    var pdfTable = new PdfPTable(11);


                    // Set the width percentage of the table (relative to the page width)
                    pdfTable.SetWidthPercentage(new float[] { 45f, 80f, 80f, 116f, 116f, 80f, 80f, 105f, 85f, 105f, 100f }, PageSize.LEGAL.Rotate());

                    // Define font style and size for headers
                    var headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.BLACK);

                    // Add table headers
                    var headers = new string[] { "No.", "Location Code", "Location Name", "Transaction Date", "Transaction Time", "Card Number", "Account Number", "Beneficiary Name", "Amount", "Credit Description", "External Reference" };
                    foreach (var header in headers)
                    {
                        pdfTable.AddCell(new PdfPCell(new Phrase(header, headerFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        });
                    }

                    // Define font style and size for data cells
                    var dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);

                    // Calculate grand total of TableId
                    decimal? grandTotal = 0;

                    // Add data to the PDF table
                    foreach (var logEntry in data)
                    {
                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.No.ToString(), dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.LocationCode, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });
                        
                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.LocationName, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.TransactionTime.Value.ToString("MM/dd/yyyy"), dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.TransactionTime.Value.ToString("HH:mm:ss"), dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.CardNumber.ToString(), dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.AccountNumber.ToString(), dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.BeneficiaryName, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.Amount.Value.ToString("C", CultureInfo.CreateSpecificCulture("en-PH")), dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_RIGHT
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.CreditDescription, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.ExternalReference, dataFont))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        // Add the TableId value to the grand total
                        grandTotal += logEntry.Amount;
                    }

                    // Add table footers
                    int[] footers = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
                    foreach (int footer in footers)
                    {
                        if (footer == 8)
                        {
                            pdfTable.AddCell(new PdfPCell(new Phrase("Grand Total:", dataFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Border = PdfPCell.RIGHT_BORDER | PdfPCell.BOTTOM_BORDER,
                            });
                        }
                        else if (footer == 9)
                        {
                            pdfTable.AddCell(new PdfPCell(new Phrase(grandTotal.Value.ToString("C", CultureInfo.CreateSpecificCulture("en-PH")), dataFont))
                            {
                                HorizontalAlignment = Element.ALIGN_RIGHT,
                                Border = PdfPCell.RIGHT_BORDER | PdfPCell.BOTTOM_BORDER,
                            });
                        }
                        else if (footer == 1)
                        {
                            pdfTable.AddCell(new PdfPCell(new Phrase("", dataFont))
                            {
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Border = PdfPCell.LEFT_BORDER | PdfPCell.BOTTOM_BORDER,
                            });
                        }
                        else if (footer == 11)
                        {
                            pdfTable.AddCell(new PdfPCell(new Phrase("", dataFont))
                            {
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Border = PdfPCell.RIGHT_BORDER | PdfPCell.BOTTOM_BORDER,
                            });
                        }
                        else
                        {
                            pdfTable.AddCell(new PdfPCell(new Phrase("", dataFont))
                            {
                                HorizontalAlignment = Element.ALIGN_CENTER,
                                Border = PdfPCell.BOTTOM_BORDER,
                            });
                        }
                    }

                    // Add the table to the PDF document
                    pdfDocument.Add(pdfTable);

                    // Close the PDF document
                    pdfDocument.Close();

                    DLFullPath = pdfFullPath;

                }
                else if (Type == "CSV")
                {
                    using (var writer = new StreamWriter(csvFullPath))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        var headers = new string[] {
                            "No.",
                            "Location Code",
                            "Location Name",
                            "Transaction Date",
                            "Transaction Time",
                            "Card Number",
                            "Account Number",
                            "Beneficiary Name",
                            "Amount",
                            "Credit Description",
                            "External Reference"
                        };

                        // Write header with formatting
                        csv.WriteField("No.");
                        csv.WriteField("Location Name");
                        csv.WriteField("Location Name");
                        csv.WriteField("Transaction Date");
                        csv.WriteField("Transaction Time");
                        csv.WriteField("Card Number");
                        csv.WriteField("Account Number");
                        csv.WriteField("Beneficiary Name");
                        csv.WriteField("Amount");
                        csv.WriteField("Credit Description");
                        csv.WriteField("External Reference");
                        csv.NextRecord();

                        int count = 1;
                        // Write data with formatting
                        for (int i = 0; i < data.Count; i++)
                        {
                            var rowIndex = i + 2; // Starting from the second row

                            // Format TransactionDate as date and time
                            string TransactionDate = data[i].TransactionDate.Value.ToString("MM/dd/yyyy");
                            string TransactionTime = data[i].TransactionTime.Value.ToString("HH:mm:ss");

                            csv.WriteField(count);
                            csv.WriteField(data[i].LocationCode);
                            csv.WriteField(data[i].LocationName);
                            csv.WriteField(TransactionDate);
                            csv.WriteField(TransactionTime);
                            csv.WriteField(data[i].CardNumber);
                            csv.WriteField(data[i].AccountNumber);
                            csv.WriteField(data[i].BeneficiaryName);
                            csv.WriteField(data[i].Amount);
                            csv.WriteField(data[i].CreditDescription);
                            csv.WriteField(data[i].ExternalReference);
                            csv.NextRecord();
                            count++;
                        }
                    }

                    DLFullPath = csvFullPath;

                }
                return Json(new { success = true, message = $"Downloading Completed.\n File Path: {DLFullPath}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


    }
}
