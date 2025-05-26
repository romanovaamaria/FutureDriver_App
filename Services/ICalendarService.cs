namespace MyApp.Services
{
    public interface ICalendarService
    {
        Task GenerateOrUpdateCalendarAsync(string userId);
    }
}
