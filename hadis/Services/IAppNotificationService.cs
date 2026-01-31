using hadis.Models;

namespace hadis.Services
{
    public interface IAppNotificationService
    {
        Task InitializeAsync();
        Task ScheduleNotificationsAsync(Dictionary<string, DateTime> prayerTimes);
        void CancelAllNotifications();
        Task RescheduleAllAsync();
        
        // Persistent Notification
        Task ShowPersistentNotificationAsync(string title, string message);
        void CancelPersistentNotification();
    }
}
