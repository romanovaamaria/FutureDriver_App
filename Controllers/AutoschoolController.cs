using Microsoft.AspNetCore.Mvc;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SeleniumExtras.WaitHelpers;
using OpenQA.Selenium.Support.UI;
using MyApp.Models; 
using System.Threading;
using System.Drawing;

namespace WebApp.Controllers
{
    public class AutoSchoolController : Controller
    {
        private List<SchoolRegion> GetRegions()
        {
            return new List<SchoolRegion>
            {
                new SchoolRegion { Id = "1", Name = "Волинська обл." },
                new SchoolRegion { Id = "2", Name = "Рівненська обл." },
                new SchoolRegion { Id = "3", Name = "Житомирська" },
                new SchoolRegion { Id = "4", Name = "Київська обл." },
                new SchoolRegion { Id = "5", Name = "Чернігівська обл." },
                new SchoolRegion { Id = "6", Name = "Сумська обл." },
                new SchoolRegion { Id = "7", Name = "Львівська обл." },
                new SchoolRegion { Id = "8", Name = "Тернопільська обл." },
                new SchoolRegion { Id = "9", Name = "Хмельницька обл." },
                new SchoolRegion { Id = "10", Name = "Вінницька обл." },
                new SchoolRegion { Id = "11", Name = "Черкаська обл." },
                new SchoolRegion { Id = "12", Name = "Полтавська обл." },
                new SchoolRegion { Id = "13", Name = "Харківська обл." },
                new SchoolRegion { Id = "14", Name = "Луганська обл." },
                new SchoolRegion { Id = "15", Name = "Закарпатська обл." },
                new SchoolRegion { Id = "16", Name = "Івано-Франківська обл." },
                new SchoolRegion { Id = "17", Name = "Чернівецька обл." },
                new SchoolRegion { Id = "18", Name = "Кіровоградська обл." },
                new SchoolRegion { Id = "19", Name = "Дніпропетровська обл." },
                new SchoolRegion { Id = "20", Name = "Донецька обл." },
                new SchoolRegion { Id = "21", Name = "Одеська обл." },
                new SchoolRegion { Id = "22", Name = "Миколаївська обл." },
                new SchoolRegion { Id = "23", Name = "Херсонська обл." },
                new SchoolRegion { Id = "24", Name = "Запорізька обл." },
                new SchoolRegion { Id = "25", Name = "АР Крим та м. Севастополь" },
                new SchoolRegion { Id = "26", Name = "м. Київ" },

            };
        }

        public IActionResult Index()
        {
            ViewBag.Regions = GetRegions();
            return View();
        }

        [HttpPost]
        [HttpPost]
        public IActionResult GetSchools(string regionId, string city, string category)
        {
            var schools = ScrapeSchools(regionId);

            // Фільтрація результатів за містом і категорією
            if (!string.IsNullOrWhiteSpace(city))
            {
                schools = schools
                    .Where(s => s.Address.Contains(city, StringComparison.OrdinalIgnoreCase) ||
                                s.ClassroomsAddress.Contains(city, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                schools = schools
                    .Where(s => s.Categories.Contains(category, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            return PartialView("_SchoolsTable", schools);
        }

        private List<AutoSchool> ScrapeSchools(string regionId)
        {
            var schoolsData = new List<AutoSchool>();

            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--window-size=1920,1080");

            using (IWebDriver driver = new ChromeDriver(options))
            {
                try
                {
                    driver.Navigate().GoToUrl("https://lvv.hsc.gov.ua/avtoshkoli/perelik-avtoshkil/");
                    var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                    var area = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath($"//a[@data-areaid='{regionId}']")));
                    area.Click();
                    Thread.Sleep(3000);

                    string tableSelector = $"table.region_data.oblid{regionId}";
                    bool tableExists = (bool)(driver as IJavaScriptExecutor)
                        .ExecuteScript($"return document.querySelector('{tableSelector}') !== null;");

                    IReadOnlyCollection<IWebElement> rows;

                    if (tableExists)
                    {
                        rows = driver.FindElements(By.CssSelector($"{tableSelector} tr.region_show"));
                        if (!rows.Any())
                            rows = driver.FindElements(By.CssSelector($"{tableSelector} tbody tr"));
                    }
                    else
                    {
                        rows = driver.FindElements(By.CssSelector("tr.region_show"))
                            .Where(r =>
                            {
                                string classAttr = r.GetAttribute("class");
                                return classAttr.Contains($"regionid{regionId}") || classAttr.Contains($"oblid{regionId}");
                            }).ToList();
                    }

                    foreach (var row in rows)
                    {
                        var cells = row.FindElements(By.TagName("td"));
                        if (cells.Count >= 7)
                        {
                            schoolsData.Add(new AutoSchool
                            {
                                Name = cells[0].Text.Trim(),
                                EDRPOU = cells[1].Text.Trim(),
                                Categories = cells[2].Text.Trim(),
                                Address = cells[3].Text.Trim(),
                                ClassroomsAddress = cells[4].Text.Trim(),
                                Contacts = cells[5].Text.Trim(),
                                AccreditationTerm = cells[6].Text.Trim()
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка: {ex.Message}");
                }
            }

            return schoolsData;
        }
    }
}
