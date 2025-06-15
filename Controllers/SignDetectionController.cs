using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MyApp.Models;

namespace MyApp.Controllers
{
    public class SignDetectionController : Controller
    {
        private readonly TrafficSignDetector _detector;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SignDetectionController> _logger;

        public SignDetectionController(
            IWebHostEnvironment environment,
            ILogger<SignDetectionController> logger,
            ILogger<TrafficSignDetector> detectorLogger)
        {
            _environment = environment;
            _logger = logger;
            // Використовуємо ContentRootPath для доступу до файлів програми
            string basePath = _environment.ContentRootPath;
            // Створюємо детектор з базовим шляхом
            _detector = new TrafficSignDetector(basePath, detectorLogger);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DetectSign(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Please select an image");
                return View("Index");
            }

            try
            {
                // Save uploaded file
                string fileName = Path.GetFileName(file.FileName);
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, fileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                _logger.LogInformation($"File saved: {filePath}");

                // Perform detection
                TrafficSignInfo signInfo = await _detector.DetectSignAsync(filePath);
                _logger.LogInformation($"Detection result: {signInfo.Name}");

                // Pass result to View
                ViewBag.SignInfo = signInfo;
                ViewBag.ImagePath = $"/uploads/{fileName}";

                return View("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during detection: {ex.Message}");
                ModelState.AddModelError("", $"Error: {ex.Message}");
                return View("Index");
            }
        }
    }
}
