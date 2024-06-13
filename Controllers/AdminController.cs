using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;


namespace MyApp.Controllers
{
    [Authorize(Roles ="admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(new Question { AnswerOptions = new List<AnswerOption> { new AnswerOption() } });
        }

        [HttpPost]
        public IActionResult AddQuestion(Question question)
        {
            if (ModelState.IsValid)
            {
                if (question.ImageFile != null)
                {
                    var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(question.ImageFile.FileName).ToLowerInvariant();

                    if (!permittedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ImageFile", "Please upload a valid image file (jpg, jpeg, png, gif).");
                        return View("Index", question);
                    }
                    var uniqueFileName = Guid.NewGuid().ToString() + ext;
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        question.ImageFile.CopyTo(stream);
                    }
                    question.ImageUrl = "/images/" + uniqueFileName;
                    
                }
                _context.Questions.Add(question);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);

                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }
                return BadRequest("Model validation failed. See console for details.");
            }
        }

        [HttpPost]
        public IActionResult SaveAnswerOptions(List<AnswerOption> answerOptions)
        {
            foreach (var option in answerOptions)
            {
                if (Request.Form["AnswerOptions[" + option.Number + "].IsCorrect"] == "on")
                {
                    option.IsCorrect = true;
                }
                else
                {
                    option.IsCorrect = false;
                }
            }
            _context.AnswerOptions.AddRange(answerOptions);
            _context.SaveChanges();

            return RedirectToAction("Index");
        }

        public IActionResult Search(string query)
        {
            var results = _context.Questions
                .Include(q => q.AnswerOptions)
                .Where(q => q.Text.Contains(query) || q.Category.Contains(query))
                .ToList();

            return View(results);
        }

        public IActionResult Edit(int id)
        {
            var question = _context.Questions
                .Include(q => q.AnswerOptions)
                .FirstOrDefault(q => q.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        [HttpPost]
        public IActionResult Edit(Question question, bool removeImage)
        {
            if (ModelState.IsValid)
            {
                var existingQuestion = _context.Questions
                    .Include(q => q.AnswerOptions)
                    .FirstOrDefault(q => q.Id == question.Id);

                if (existingQuestion == null)
                {
                    return NotFound();
                }
                existingQuestion.Text = question.Text;
                existingQuestion.Category = question.Category;
                if (removeImage && !string.IsNullOrEmpty(existingQuestion.ImageUrl))
                {
                    existingQuestion.ImageFile = null;
                    existingQuestion.ImageUrl = null;
                }
                else if (question.ImageFile != null)
                {
                    var permittedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(question.ImageFile.FileName).ToLowerInvariant();

                    if (!permittedExtensions.Contains(ext))
                    {
                        ModelState.AddModelError("ImageFile", "Please upload a valid image file (jpg, jpeg, png, gif).");
                        return View(question);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + ext;
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        question.ImageFile.CopyTo(stream);
                    }

                    // Remove the old image file if exists
                    if (!string.IsNullOrEmpty(existingQuestion.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    existingQuestion.ImageUrl = "/images/" + uniqueFileName;
                }
                foreach (var existingOption in existingQuestion.AnswerOptions.ToList())
                {
                    if (!question.AnswerOptions.Any(o => o.Id == existingOption.Id))
                    {
                        _context.AnswerOptions.Remove(existingOption);
                    }
                }
                foreach (var option in question.AnswerOptions)
                {
                    var existingOption = existingQuestion.AnswerOptions.FirstOrDefault(o => o.Id == option.Id);
                    if (existingOption != null)
                    {
                        existingOption.Text = option.Text;
                        existingOption.IsCorrect = option.IsCorrect;
                    }
                    else
                    {
                        existingQuestion.AnswerOptions.Add(option);
                    }
                }

                _context.SaveChanges();
                return RedirectToAction("Search");
            }
            return View(question);
        }

        public IActionResult Delete(int id)
        {
            var question = _context.Questions
                .Include(q => q.AnswerOptions)
                .FirstOrDefault(q => q.Id == id);

            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var question = _context.Questions
                .Include(q => q.AnswerOptions)
                .FirstOrDefault(q => q.Id == id);

            if (question != null)
            {
                _context.Questions.Remove(question);
                _context.SaveChanges();
            }

            return RedirectToAction("Index", "Admin");
        }
    }
}


