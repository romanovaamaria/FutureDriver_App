using MyApp.Models;

namespace MyApp.ViewModels
{
    public class GamificationViewModel
    {
        public int StreakDays { get; set; }
        public string MotivationalMessage { get; set; }

        public List<UserBadge> EarnedBadges { get; set; }
        public List<BadgeProgress> InProgressBadges { get; set; }
    }
    public class BadgeProgress
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string EmojiIcon { get; set; }
        public double ProgressPercentage { get; set; }
    }
}
