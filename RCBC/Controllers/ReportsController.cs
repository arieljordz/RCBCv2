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
            List<AuditLogsModel> data = new List<AuditLogsModel>();

            data = global.GetAuditLogs()
            .Where(x =>
                (!DateFrom.HasValue || x.DateModified.Date >= DateFrom.Value.Date) &&
                (!DateTo.HasValue || x.DateModified.Date <= DateTo.Value.Date) &&
                (EmployeeName == null || x.EmployeeName.ToString().ToLower().Contains(EmployeeName)) &&
                (GroupDept == null || x.GroupDept.ToLower().Contains(GroupDept)) &&
                (UserRole == null || x.UserRole.ToString().ToLower().Contains(UserRole)) &&
                (Action == null || x.Action.ToLower().Contains(Action)))
            .ToList();

            return Json(new { data });
        }


        public IActionResult DownloadAuditLogs(string Type)
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

                if (Type == "EXCEL")
                {
                    var fullPathWithName = Path.Combine(downloadsFolderPath, excelFileName);

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                        var data = global.GetAuditLogs().ToList();

                        var headerCells = worksheet.Cells["A1:G1"];
                        headerCells.Style.Font.Bold = true;
                        headerCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        worksheet.Cells["A1"].Value = "User ID";
                        worksheet.Column(1).Width = 10;
                        worksheet.Cells["B1"].Value = "Modified By";
                        worksheet.Column(2).Width = 25;
                        worksheet.Cells["C1"].Value = "IP Address";
                        worksheet.Column(3).Width = 20;
                        worksheet.Cells["D1"].Value = "Date Modified";
                        worksheet.Column(4).Width = 25;
                        worksheet.Cells["E1"].Value = "Group";
                        worksheet.Column(5).Width = 15;
                        worksheet.Cells["F1"].Value = "Role";
                        worksheet.Column(6).Width = 15;
                        worksheet.Cells["G1"].Value = "Action";
                        worksheet.Column(7).Width = 25;


                        for (int i = 0; i < data.Count; i++)
                        {
                            var rowIndex = i + 2; // Starting from the second row

                            worksheet.Cells["A" + rowIndex].Value = data[i].ModifiedBy;
                            worksheet.Cells["B" + rowIndex].Value = data[i].EmployeeName;
                            worksheet.Cells["C" + rowIndex].Value = data[i].IP;
                            worksheet.Cells["D" + rowIndex].Value = data[i].DateModified;
                            worksheet.Cells["E" + rowIndex].Value = data[i].GroupDept;
                            worksheet.Cells["F" + rowIndex].Value = data[i].UserRole;
                            worksheet.Cells["G" + rowIndex].Value = data[i].Action;

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

                    // Add a table to the PDF document
                    var pdfTable = new PdfPTable(7);

                    // Add table headers
                    var headers = new string[] { "User ID", "Modified By", "IP Address", "Date Modified", "Group", "Role", "Action" };
                    foreach (var header in headers)
                    {
                        pdfTable.AddCell(new PdfPCell(new Phrase(header))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        });
                    }

                    // Fetch audit log data
                    var data = global.GetAuditLogs().ToList();

                    // Add data to the PDF table
                    foreach (var logEntry in data)
                    {
                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.ModifiedBy.ToString()))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.EmployeeName))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.IP))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.DateModified.ToShortDateString()))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.GroupDept))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.UserRole))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.Action))
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
                        var data = global.GetAuditLogs().ToList();

                        // Write header with formatting
                        csv.WriteField("User ID");
                        csv.WriteField("Modified By");
                        csv.WriteField("IP Address");
                        csv.WriteField("Date Modified");
                        csv.WriteField("Group");
                        csv.WriteField("Role");
                        csv.WriteField("Action");
                        csv.NextRecord();

                        // Write data with formatting
                        for (int i = 0; i < data.Count; i++)
                        {
                            var rowIndex = i + 2; // Starting from the second row

                            csv.WriteField(data[i].ModifiedBy);
                            csv.WriteField(data[i].EmployeeName);
                            csv.WriteField(data[i].IP);
                            csv.WriteField(data[i].DateModified);
                            csv.WriteField(data[i].GroupDept);
                            csv.WriteField(data[i].UserRole);
                            csv.WriteField(data[i].Action);
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

        public IActionResult DownloadDPUStatus(string Type)
        {
            try
            {
                ////var contentRootPath = hostingEnvironment.ContentRootPath;

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

                if (Type == "EXCEL")
                {
                    var fullPathWithName = Path.Combine(downloadsFolderPath, excelFileName);

                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                    using (var package = new ExcelPackage())
                    {
                        var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                        var data = global.GetAuditLogs().Where(x => x.TableId != 0).Take(40).ToList();

                        var headerCells = worksheet.Cells["A1:F1"];
                        headerCells.Style.Font.Bold = true;
                        headerCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        worksheet.Cells["A1"].Value = "Module";
                        worksheet.Column(1).Width = 25;
                        worksheet.Cells["B1"].Value = "SubModule";
                        worksheet.Column(2).Width = 25;
                        worksheet.Cells["C1"].Value = "ChildModule";
                        worksheet.Column(3).Width = 25;
                        worksheet.Cells["D1"].Value = "TableName";
                        worksheet.Column(4).Width = 25;
                        worksheet.Cells["E1"].Value = "TableId";
                        worksheet.Column(5).Width = 10;
                        worksheet.Cells["F1"].Value = "Action";
                        worksheet.Column(6).Width = 20;


                        for (int i = 0; i < data.Count; i++)
                        {
                            var rowIndex = i + 2; // Starting from the second row

                            worksheet.Cells["A" + rowIndex].Value = data[i].Module;
                            worksheet.Cells["B" + rowIndex].Value = data[i].SubModule;
                            worksheet.Cells["C" + rowIndex].Value = data[i].ChildModule;
                            worksheet.Cells["D" + rowIndex].Value = data[i].TableName;
                            worksheet.Cells["E" + rowIndex].Value = data[i].TableId;
                            worksheet.Cells["F" + rowIndex].Value = data[i].Action;

                            // Center the content in all cells
                            var dataCells = worksheet.Cells["A" + rowIndex + ":F" + rowIndex];
                            dataCells.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                        }

                        package.SaveAs(fullPathWithName);
                    }

                    DLFullPath = excelFullPath;

                }
                else if (Type == "PDF")
                {
                    var pdfDocument = new Document();
                    var pdfWriter = PdfWriter.GetInstance(pdfDocument, new FileStream(pdfFullPath, FileMode.Create));
                    pdfDocument.Open();

                    // Add a table to the PDF document
                    var pdfTable = new PdfPTable(6); // 6 columns for Module, SubModule, ChildModule, TableName, TableId, Action

                    // Add table headers
                    var headers = new string[] { "Module", "SubModule", "ChildModule", "TableName", "TableId", "Action" };
                    foreach (var header in headers)
                    {
                        pdfTable.AddCell(new PdfPCell(new Phrase(header))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER,
                            BackgroundColor = BaseColor.LIGHT_GRAY
                        });
                    }

                    // Fetch audit log data
                    var data = global.GetAuditLogs().Where(x => x.TableId != 0).Take(40).ToList();

                    // Add data to the PDF table
                    foreach (var logEntry in data)
                    {
                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.Module))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.SubModule))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.ChildModule))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.TableName))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.TableId.ToString()))
                        {
                            HorizontalAlignment = Element.ALIGN_CENTER
                        });

                        pdfTable.AddCell(new PdfPCell(new Phrase(logEntry.Action))
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
                        var data = global.GetAuditLogs().Where(x => x.TableId != 0).Take(40).ToList();

                        // Write header with formatting
                        csv.WriteField("Module");
                        csv.WriteField("SubModule");
                        csv.WriteField("ChildModule");
                        csv.WriteField("TableName");
                        csv.WriteField("TableId");
                        csv.WriteField("Action");
                        csv.NextRecord();

                        // Write data with formatting
                        for (int i = 0; i < data.Count; i++)
                        {
                            var rowIndex = i + 2; // Starting from the second row

                            csv.WriteField(data[i].Module);
                            csv.WriteField(data[i].SubModule);
                            csv.WriteField(data[i].ChildModule);
                            csv.WriteField(data[i].TableName);
                            csv.WriteField(data[i].TableId);
                            csv.WriteField(data[i].Action);
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

        public IActionResult PrintAuditLogs(string Type)
        {
            var data = global.GetAuditLogs().ToList();

            return PartialView("~/Views/Shared/_PreviewAuditLogs.cshtml", data);

        }


        public IActionResult LoadDPUStatus(DateTime? DateFrom, DateTime? DateTo, string? MachineID, string? BeneficiaryName, string? AccountNumber, string? Status)
        {
            List<AuditLogsModel> data = new List<AuditLogsModel>();

            data = global.GetAuditLogs()
            .Where(x =>
                (!DateFrom.HasValue || x.DateModified.Date >= DateFrom.Value.Date) &&
                (!DateTo.HasValue || x.DateModified.Date <= DateTo.Value.Date) &&
                (MachineID == null || x.Id.ToString().ToLower().Contains(MachineID)) &&
                (BeneficiaryName == null || x.EmployeeName.ToLower().Contains(BeneficiaryName)) &&
                (AccountNumber == null || x.ModifiedBy.ToString().ToLower().Contains(AccountNumber)) &&
                (Status == null || x.Action.ToLower().Contains(Status)))
            .ToList();

            return Json(new { data });
        }


    }
}
