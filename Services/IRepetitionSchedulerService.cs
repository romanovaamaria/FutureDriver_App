namespace MyApp.Services
{
    public interface IRepetitionSchedulerService
    {
        RepetitionResult CalculateNext(DateTime today, int quality, int? previousRepetition, int? previousInterval, double? previousEFactor);
    }
    public class RepetitionResult
    {
        public int Repetition { get; set; }
        public int Interval { get; set; }
        public double EFactor { get; set; }
        public DateTime NextReview { get; set; }
    }
}
