namespace MyApp.ViewModels
{
    public class CalendarViewModel
    {
        public DateTime CurrentMonth { get; set; }
        public List<CalendarDayViewModel> Days { get; set; }
    }

    public class CalendarDayViewModel
    {
        public DateTime Date { get; set; }
        public List<CalendarTaskViewModel> Tasks { get; set; }
    }

    public class CalendarTaskViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Priority { get; set; } // "Critical", "High", etc.
        public string Type { get; set; } // "NewTopic", "TopicReview", "CardReview"
        public string Description { get; set; }
        public DateTime Date { get; set; }
    }
    public class CalendarTaskUpdateModel
    {
        public int TaskId { get; set; }
        public DateTime NewDate { get; set; }
        public int? NewOrder { get; set; }
    }
}
