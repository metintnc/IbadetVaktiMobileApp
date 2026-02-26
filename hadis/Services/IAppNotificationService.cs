using hadis.Models;

namespace hadis.Services
{
    public interface IAppNotificationService
    {
        Task InitializeAsync();
        Task ScheduleNotificationsAsync(Dictionary<string, DateTime> prayerTimes, int dayOffset = 0);
        void CancelAllNotifications();
        Task RescheduleAllAsync();
        Task ScheduleMultiDayNotificationsAsync(int days = 3);
        
        // Persistent Notification
        Task ShowPersistentNotificationAsync(string title, string message);
        void CancelPersistentNotification();
    }
}
