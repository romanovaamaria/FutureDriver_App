using System.ComponentModel.DataAnnotations;
namespace MyApp.Models
{
    public class UserSettings
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public int DesiredNewTopicInterval { get; set; } = 3;

        public TimeSpan? PreferredStudyTime { get; set; }

        public bool NotificationsEnabled { get; set; } = true;

        public List<CalendarTask> CalendarTasks { get; set; } = new List<CalendarTask>();
    }

    public class CalendarTask
    {
        public int Id { get; set; }

        public int UserSettingsId { get; set; }
        public UserSettings UserSettings { get; set; }

        public CalendarTaskType TaskType { get; set; }

        public string Category { get; set; }

        public DateTime NextReview { get; set; }

        public int Interval { get; set; }

        public int Repetition { get; set; }

        public double EF { get; set; } = 2.5;

        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Пріоритет (1 - найвищий, 5 - найнижчий)
        /// </summary>
        public int Priority { get; set; } = 3;

        /// <summary>
        /// Додаткові нотатки (не обов'язкове)
        /// </summary>
        public string? Notes { get; set; }
    }

    public enum CalendarTaskType
    {
        NewTopic,
        ReviewTopic,
        ReviewCards
    }
    public class UserSettingsForm
    {
        [Range(1, 30, ErrorMessage = "Інтервал має бути від 1 до 30 днів.")]
        public int DesiredNewTopicInterval { get; set; } = 3;

        public TimeSpan? PreferredStudyTime { get; set; }

        public bool NotificationsEnabled { get; set; } = true;
    }
}
