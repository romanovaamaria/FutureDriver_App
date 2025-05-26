using MyApp.Models;
using Microsoft.EntityFrameworkCore;
namespace MyApp.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRepetitionSchedulerService _repetitionService;

        public CalendarService(ApplicationDbContext dbContext, IRepetitionSchedulerService repetitionService)
        {
            _dbContext = dbContext;
            _repetitionService = repetitionService;
        }

        public async Task GenerateOrUpdateCalendarAsync(string userId)
        {
            var userSettings = await _dbContext.UserSettings
                .Include(us => us.CalendarTasks)
                .FirstOrDefaultAsync(us => us.UserId == userId);

            if (userSettings == null)
                throw new Exception("User settings not found.");

            var today = DateTime.UtcNow.Date;

            var allCategories = await _dbContext.Questions
     .Select(q => q.Category)
     .Distinct()
     .ToListAsync();

            // Категорії, за якими вже є результати для цього користувача
            var userCategories = await _dbContext.TestResults
                .Where(tr => tr.UserId == userId)
                .Select(tr => tr.Category)
                .Distinct()
                .ToListAsync();

            // Категорії, яких ще немає у результатах користувача
            var newCategories = allCategories.Except(userCategories).ToList();

            var startDate = today;
            foreach (var category in newCategories)
            {
                // Перевіряємо, чи вже створено завдання для цієї категорії
                bool alreadyPlanned = userSettings.CalendarTasks
                    .Any(ct => ct.Category == category  && ct.TaskType == CalendarTaskType.NewTopic);

                if (!alreadyPlanned)
                {
                    userSettings.CalendarTasks.Add(new CalendarTask
                    {
                        TaskType = CalendarTaskType.NewTopic,
                        Category = category,
                        NextReview = startDate,
                        Interval = userSettings.DesiredNewTopicInterval,
                        Repetition = 0,
                        EF = 2.5,
                        Priority = 1
                    });

                    // Зсуваємо дату для наступної нової теми
                    startDate = startDate.AddDays(userSettings.DesiredNewTopicInterval);
                }
            }

            // 2️⃣ Повторення тем
            var userTestResults = await _dbContext.TestResults
                .Where(tr => tr.UserId == userId)
                .GroupBy(tr => tr.Category)
                .Select(g => g.OrderByDescending(tr => tr.DateTaken).FirstOrDefault())
                .ToListAsync();

            foreach (var result in userTestResults)
            {
                int quality = result.Percentage >= 90 ? 4 : 2;

                var existingTask = userSettings.CalendarTasks
                    .FirstOrDefault(ct => ct.Category == result.Category && ct.TaskType == CalendarTaskType.ReviewTopic);

                var repetition = existingTask?.Repetition ?? 0;
                var interval = existingTask?.Interval ?? 1;
                var ef = existingTask?.EF ?? 2.5;

                var next = _repetitionService.CalculateNext(today, quality, repetition, interval, ef);

                // ➡️ Якщо EF >= 4 – ставимо мінімальний пріоритет (5) 
                if (next.EFactor >= 4)
                {
                    // Якщо вже існує таке завдання — ставимо мінімальний пріоритет (5)
                    if (existingTask != null)
                        existingTask.Priority = 5;

                    continue; // Не створюємо нове завдання
                }

                if (existingTask == null)
                {
                    userSettings.CalendarTasks.Add(new CalendarTask
                    {
                        TaskType = CalendarTaskType.ReviewTopic,
                        Category = result.Category,
                        NextReview = next.NextReview,
                        Interval = next.Interval,
                        Repetition = next.Repetition,
                        EF = next.EFactor,
                        Priority = 2
                    });
                }
                else
                {
                    existingTask.NextReview = next.NextReview;
                    existingTask.Interval = next.Interval;
                    existingTask.Repetition = next.Repetition;
                    existingTask.EF = next.EFactor;
                    existingTask.Priority = 2;
                    existingTask.IsCompleted = false;
                }
            }

            // 3️⃣ Повторення карток
            var userCards = await _dbContext.SavedQuestions
                .Where(c => c.UserId == userId && c.NextReview.HasValue /*&& c.NextReview.Value.Date <= today*/)
                .ToListAsync();

            // Групуємо картки за датою повторення
            var cardsByDate = userCards
                .GroupBy(c => c.NextReview.Value.Date)
                .ToList();

            foreach (var group in cardsByDate)
            {
                var reviewDate = group.Key;
                var cardsCount = group.Count();

                // Перевіряємо, чи вже є завдання на цю дату
                bool alreadyPlanned = userSettings.CalendarTasks
                    .Any(ct => ct.TaskType == CalendarTaskType.ReviewCards && ct.NextReview.Date == reviewDate);

                if (!alreadyPlanned)
                {
                    userSettings.CalendarTasks.Add(new CalendarTask
                    {
                        TaskType = CalendarTaskType.ReviewCards,
                        Category = $"Повторення карток ({cardsCount})", // Кількість карток у категорії
                        NextReview = reviewDate,
                        Interval = 1,
                        Repetition = 0,
                        EF = 2.5,
                        Priority = 3
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
