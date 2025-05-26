using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyApp.Models;
using MyApp.Services;

namespace MyApp.Controllers
{
    [Authorize]
    public class CalendarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICalendarService _calendarService;
        private readonly IRepetitionSchedulerService _repetitionService;
        private readonly IGamificationService _gamificationService;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(
            ApplicationDbContext context,
            ICalendarService calendarService,
            IRepetitionSchedulerService repetitionService,
            IGamificationService gamificationService,
            ILogger<CalendarController> logger)
        {
            _context = context;
            _calendarService = calendarService;
            _repetitionService = repetitionService;
            _gamificationService = gamificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("User {User} accessed the Calendar Index page.", User.Identity.Name);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userSettings = await _context.UserSettings
                .Include(us => us.CalendarTasks)
                .FirstOrDefaultAsync(us => us.UserId == userId);

            if (userSettings == null)
            {
                _logger.LogWarning("User {User} does not have any settings configured.", User.Identity.Name);
                return RedirectToAction(nameof(Setup));
            }
            await _calendarService.GenerateOrUpdateCalendarAsync(userId);
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var tasks = userSettings.CalendarTasks;

            var masteredTasks = tasks.Where(t => t.EF >= 4).ToList();
            var newTasks = tasks.Where(t => t.Priority == 1).ToList();
            var overdueTasks = tasks.Where(t => t.NextReview < today ).ToList();
            var todayTasks = tasks.Where(t => t.NextReview == today ).ToList();
            var tomorrowTasks = tasks.Where(t => t.NextReview == tomorrow ).ToList();

            var futureTasks = tasks
                .Where(t => t.NextReview > tomorrow && t.Repetition > 0)
                .GroupBy(t => t.NextReview)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());

            _logger.LogInformation("User {User} has {Count} tasks for today.", User.Identity.Name, todayTasks.Count);

            ViewBag.MasteredTasks = masteredTasks;
            ViewBag.NewTasks = newTasks;
            ViewBag.OverdueTasks = overdueTasks;
            ViewBag.TodayTasks = todayTasks;
            ViewBag.TomorrowTasks = tomorrowTasks;
            ViewBag.FutureTasks = futureTasks;
            ViewBag.Today = today;
            ViewBag.Tomorrow = tomorrow;

            var gamification = await _gamificationService.GetOrCreateUserGamificationAsync(userId);

            if (masteredTasks.Any())
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    "🏅", "Mastered Topics", "At least one mastered topic in the calendar");
                _logger.LogInformation("User {User} earned the Mastered Topics badge.", User.Identity.Name);
            }

            if (overdueTasks.Any())
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    "⏰", "First Overdue", "First overdue repetition task");
                _logger.LogInformation("User {User} has an overdue task.", User.Identity.Name);
            }

            if (todayTasks.Any())
            {
                await _gamificationService.AddBadgeIfNotExistsAsync(gamification,
                    "📆", "First Repetition", "First topic to repeat today");
                _logger.LogInformation("User {User} has tasks to repeat today.", User.Identity.Name);
            }

            return View();
        }

        public IActionResult Setup()
        {
            _logger.LogInformation("User {User} accessed the Calendar Setup page.", User.Identity.Name);
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Setup(UserSettingsForm formModel)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid form state for Setup by user {User}.", User.Identity.Name);
                return View(formModel);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var settings = new UserSettings
            {
                UserId = userId,
                DesiredNewTopicInterval = formModel.DesiredNewTopicInterval,
                PreferredStudyTime = formModel.PreferredStudyTime,
                NotificationsEnabled = formModel.NotificationsEnabled,
                CalendarTasks = new List<CalendarTask>()
            };

            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User {User} created new settings.", User.Identity.Name);

            await _calendarService.GenerateOrUpdateCalendarAsync(userId);
            _logger.LogInformation("Calendar generated or updated for user {User}.", User.Identity.Name);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> GetFullTask(int id)
        {
            _logger.LogInformation("User {User} requested full task with ID {Id}.", User.Identity.Name, id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.CalendarTasks
                .Include(ct => ct.UserSettings)
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.UserSettings.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Task with ID {Id} not found for user {User}.", id, User.Identity.Name);
                return NotFound();
            }

            var today = DateTime.UtcNow.Date;

            var testResult = await _context.TestResults
                .FirstOrDefaultAsync(tr => tr.UserId == userId && tr.Category == task.Category && tr.DateTaken.Date == today);

            if (testResult == null)
            {
                _logger.LogWarning("User {User} has not completed today's test for task {Id}.", User.Identity.Name, id);
                return Json(new { success = false, message = "You must complete the test first!" });
            }

            int quality = testResult.Percentage >= 90 ? 4 : 2;
            var next = _repetitionService.CalculateNext(today, quality, task.Repetition, task.Interval, task.EF);

            task.EF = next.EFactor;
            task.Repetition = next.Repetition;
            task.Interval = next.Interval;
            task.NextReview = next.NextReview;
            task.IsCompleted = true;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {Id} updated for user {User}.", id, User.Identity.Name);

            return Json(new
            {
                success = true,
                message = "Task updated based on test results.",
                ef = task.EF,
                repetition = task.Repetition,
                interval = task.Interval,
                nextReview = task.NextReview.ToShortDateString()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RateTask(int id)
        {
            _logger.LogInformation("User {User} rated task {Id}.", User.Identity.Name, id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.CalendarTasks
                .Include(ct => ct.UserSettings)
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.UserSettings.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Task {Id} not found for rating by user {User}.", id, User.Identity.Name);
                return NotFound();
            }

            if (task.TaskType != CalendarTaskType.NewTopic && task.TaskType != CalendarTaskType.ReviewTopic)
            {
                _logger.LogWarning("User {User} tried to rate a forbidden task {Id}.", User.Identity.Name, id);
                return Forbid();
            }

            var today = DateTime.UtcNow.Date;
            var testResult = await _context.TestResults
                .FirstOrDefaultAsync(tr => tr.UserId == userId && tr.Category == task.Category && tr.DateTaken.Date == today);

            if (testResult == null)
            {
                _logger.LogWarning("User {User} has not completed the test for task {Id}.", User.Identity.Name, id);
                return Json(new { success = false, message = "Please complete the test first." });
            }

            int quality = testResult.Percentage >= 90 ? 4 : 2;
            var next = _repetitionService.CalculateNext(today, quality, task.Repetition, task.Interval, task.EF);

            task.EF = next.EFactor;
            task.Repetition = next.Repetition;
            task.Interval = next.Interval;
            task.NextReview = next.NextReview;
            task.IsCompleted = true;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Task {Id} successfully rated by user {User}.", id, User.Identity.Name);

            return Json(new
            {
                success = true,
                message = "Task rating saved!",
                ef = task.EF,
                repetition = task.Repetition,
                interval = task.Interval,
                nextReview = task.NextReview.ToShortDateString()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteTask(int id)
        {
            _logger.LogInformation("User {User} completed task {Id}.", User.Identity.Name, id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.CalendarTasks
                .Include(ct => ct.UserSettings)
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.UserSettings.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Task {Id} not found for completion by user {User}.", id, User.Identity.Name);
                return NotFound();
            }

            task.IsCompleted = true;
            await _context.SaveChangesAsync();

            await _gamificationService.UpdateStreakAsync(userId);
            _logger.LogInformation("Streak updated for user {User}.", User.Identity.Name);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveTask(int id, string newDate)
        {
            _logger.LogInformation("User {User} requested to move task {Id} to {Date}.", User.Identity.Name, id, newDate);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.CalendarTasks
                .Include(ct => ct.UserSettings)
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.UserSettings.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Task {Id} not found for moving by user {User}.", id, User.Identity.Name);
                return NotFound();
            }

            if (DateTime.TryParse(newDate, out DateTime parsedDate))
            {
                task.NextReview = parsedDate.Date;
                await _context.SaveChangesAsync();
                _logger.LogInformation("Task {Id} moved to {Date} by user {User}.", id, newDate, User.Identity.Name);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTask(int id)
        {
            _logger.LogInformation("User {User} requested to delete task {Id}.", User.Identity.Name, id);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.CalendarTasks
                .Include(ct => ct.UserSettings)
                .FirstOrDefaultAsync(ct => ct.Id == id && ct.UserSettings.UserId == userId);

            if (task == null)
            {
                _logger.LogWarning("Task {Id} not found for deletion by user {User}.", id, User.Identity.Name);
                return NotFound();
            }

            _context.CalendarTasks.Remove(task);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Task {Id} deleted by user {User}.", id, User.Identity.Name);

            return RedirectToAction(nameof(Index));
        }
    }
}