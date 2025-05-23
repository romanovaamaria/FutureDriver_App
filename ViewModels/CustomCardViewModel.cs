namespace MyApp.ViewModels
{
    public class CustomCardViewModel
    {
        public string QuestionText { get; set; } = string.Empty;
        public string AnswerText { get; set; } = string.Empty;
        public IFormFile? Image { get; set; }
    }
}
