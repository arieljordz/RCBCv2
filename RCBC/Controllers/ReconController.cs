﻿using Microsoft.AspNetCore.Mvc;

namespace RCBC.Controllers
{
    public class ReconController : Controller
    {
        private readonly IConfiguration Configuration;
        public ReconController(IConfiguration _configuration)
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
            ViewBag.Username = Request.Cookies["rcbctellerlessusername"];
            ViewBag.UserId = Request.Cookies["rcbctellername"];

            return View();
        }

        public IActionResult Recon()
        {
            return LoadViews();
        }
    }
}