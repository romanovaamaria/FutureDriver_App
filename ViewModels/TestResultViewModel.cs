using MyApp.Models;

namespace MyApp.ViewModels
{
    public class TestResultDetailsViewModel
    {
        public TestResult TestResult { get; set; }
        public List<QuestionResultJson> QuestionResults { get; set; }
    }

    public class QuestionResultJson
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; }
        public List<AnswerOptionDto> AnswerOptions { get; set; }
        public int? SelectedAnswerId { get; set; }
        public bool IsCorrect { get; set; }
    }
    public class QuestionSummaryDto
    {
        public int QuestionId { get; set; }
        public int? SelectedAnswerId { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class AnswerOptionDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public bool IsCorrect { get; set; }
    }
}
