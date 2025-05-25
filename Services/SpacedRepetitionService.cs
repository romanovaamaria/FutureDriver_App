namespace MyApp.Services
{
    public class SpacedRepetitionService : IRepetitionSchedulerService
    {
        public RepetitionResult CalculateNext(DateTime today, int quality, int? previousRepetition, int? previousInterval, double? previousEFactor)
        {
            if (quality < 0 || quality > 5)
                throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 0 and 5.");

            int repetition = previousRepetition ?? 0;
            int interval = previousInterval ?? 1;
            double ef = previousEFactor ?? 2.5;

            if (quality >= 3)
            {
                if (repetition == 0)
                    interval = 1;
                else if (repetition == 1)
                    interval = 6;
                else
                    interval = (int)Math.Round(interval * ef);

                repetition += 1;
            }
            else
            {
                repetition = 0;
                interval = 1;
            }

            ef += (0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02));
            if (ef < 1.3)
                ef = 1.3;

            return new RepetitionResult
            {
                Repetition = repetition,
                Interval = interval,
                EFactor = ef,
                NextReview = today.AddDays(interval)
            };
        }
    }
}
