﻿using Microsoft.AspNetCore.Mvc;

namespace RCBC.Controllers
{
    public class CorporateManagementController : Controller
    {
        private readonly IConfiguration Configuration;
        public CorporateManagementController(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }
        private string GetConnectionString()
        {
            return Configuration.GetConnectionString("DefaultConnection");
        }
        public IActionResult LoadViews()
        {
            ViewBag.DateNow = DateTime.Now;
            ViewBag.Username = Request.Cookies["Username"];
            ViewBag.UserId = Request.Cookies["EmployeeName"];
            ViewBag.UserRole = Request.Cookies["UserRole"];

            return View();
        }

        public IActionResult CorporateManagement()
        {
            return LoadViews();
        }
    }
}