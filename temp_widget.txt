using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using System.Globalization;
using System.Text.Json;
using Android.Locations;

namespace hadis.Platforms.Android
{
    [BroadcastReceiver(Label = "Namaz Vakti Widget", Exported = true)]
    [IntentFilter(new string[] { 
        "android.appwidget.action.APPWIDGET_UPDATE",
        Intent.ActionTimeChanged,
        Intent.ActionTimeTick,
        Intent.ActionTimezoneChanged
    })]
    [MetaData("android.appwidget.provider", Resource = "@xml/clock_weather_widget_info")]
    public class ClockWeatherWidget : AppWidgetProvider
    {
        private const string ActionAutoUpdate = "AUTO_UPDATE_WIDGET";

        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context == null || appWidgetManager == null || appWidgetIds == null)
                return;

            foreach (var appWidgetId in appWidgetIds)
            {
                _ = UpdateAppWidgetAsync(context, appWidgetManager, appWidgetId);
            }

            SetupAutoUpdate(context);
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context == null || intent == null)
                return;

            var action = intent.Action;
            
            if (action == Intent.ActionTimeChanged || 
                action == Intent.ActionTimeTick || 
                action == Intent.ActionTimezoneChanged ||
                action == ActionAutoUpdate)
            {
                var appWidgetManager = AppWidgetManager.GetInstance(context);
                var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(ClockWeatherWidget)));
                var appWidgetIds = appWidgetManager?.GetAppWidgetIds(componentName);

                if (appWidgetIds != null)
                {
                    OnUpdate(context, appWidgetManager, appWidgetIds);
                }
            }
        }

        private static async Task UpdateAppWidgetAsync(Context context, AppWidgetManager appWidgetManager, int appWidgetId)
        {
            try
            {
                var packageName = context.PackageName;
                var layoutId = context.Resources?.GetIdentifier("clock_weather_widget", "layout", packageName) ?? 0;
                
                if (layoutId == 0)
                    return;

                var views = new RemoteViews(packageName, layoutId);

                // Saat ve tarih
                var now = DateTime.Now;
                var timeText = now.ToString("HH:mm");
                var dateText = now.ToString("dd MMMM, ddd", new CultureInfo("tr-TR"));

                var timeId = context.Resources?.GetIdentifier("widget_time", "id", packageName) ?? 0;
                var dateId = context.Resources?.GetIdentifier("widget_date", "id", packageName) ?? 0;
                var namazAdiId = context.Resources?.GetIdentifier("widget_namaz_adi", "id", packageName) ?? 0;
                var kalanSureId = context.Resources?.GetIdentifier("widget_kalan_sure", "id", packageName) ?? 0;
                var containerId = context.Resources?.GetIdentifier("widget_container", "id", packageName) ?? 0;

                if (timeId != 0)
                    views.SetTextViewText(timeId, timeText);
                if (dateId != 0)
                    views.SetTextViewText(dateId, dateText);

                // Namaz vakti bilgisini al
                var (namazAdi, kalanSure) = await GetNextPrayerTimeAsync(context);
                
                if (namazAdiId != 0)
                    views.SetTextViewText(namazAdiId, namazAdi);
                if (kalanSureId != 0)
                    views.SetTextViewText(kalanSureId, kalanSure);

                // Tiklama olayi
                var intent = new Intent(context, typeof(MainActivity));
                intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                var pendingIntent = PendingIntent.GetActivity(context, 0, intent, PendingIntentFlags.Immutable);
                
                if (containerId != 0)
                    views.SetOnClickPendingIntent(containerId, pendingIntent);

                appWidgetManager.UpdateAppWidget(appWidgetId, views);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Widget update error: {ex.Message}");
            }
        }

        private static async Task<(string namazAdi, string kalanSure)> GetNextPrayerTimeAsync(Context context)
        {
            try
            {
                // Konum al
                var (latitude, longitude) = await GetLocationAsync(context);
                
                if (latitude == 0 && longitude == 0)
                    return ("Konum Yok", "--");

                // API'den namaz vakitlerini cek
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                
                string url = $"https://api.aladhan.com/v1/timings?latitude={latitude}&longitude={longitude}&method=13";
                var response = await httpClient.GetAsync(url);
                
                if (!response.IsSuccessStatusCode)
                    return ("API Hatasi", "--");

                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = JsonDocument.Parse(content);
                var timings = jsonDoc.RootElement.GetProperty("data").GetProperty("timings");

                // Namaz vakitlerini al - MainPage ile ayni key'leri kullan
                var namazVakitleri = new Dictionary<string, DateTime>
                {
                    { "Ýmsak", ParsePrayerTime(timings.GetProperty("Fajr").GetString()) },
                    { "gunes", ParsePrayerTime(timings.GetProperty("Sunrise").GetString()) },
                    { "Ogle", ParsePrayerTime(timings.GetProperty("Dhuhr").GetString()) },
                    { "Ýkindi", ParsePrayerTime(timings.GetProperty("Asr").GetString()) },
                    { "Aksam", ParsePrayerTime(timings.GetProperty("Maghrib").GetString()) },
                    { "Yatsi", ParsePrayerTime(timings.GetProperty("Isha").GetString()) }
                };

                // Sonraki namazi bul - MainPage'deki mantikla ayni
                var now = DateTime.Now;
                
                if (namazVakitleri["Ýmsak"] > now)
                {
                    var kalan = namazVakitleri["Ýmsak"] - now;
                    return ("Imsak", FormatKalanSure(kalan));
                }
                else if (namazVakitleri["gunes"] > now)
                {
                    var kalan = namazVakitleri["gunes"] - now;
                    return ("Gunes", FormatKalanSure(kalan));
                }
                else if (namazVakitleri["Ogle"] > now)
                {
                    var kalan = namazVakitleri["Ogle"] - now;
                    return ("Ogle", FormatKalanSure(kalan));
                }
                else if (namazVakitleri["Ýkindi"] > now)
                {
                    var kalan = namazVakitleri["Ýkindi"] - now;
                    return ("Ikindi", FormatKalanSure(kalan));
                }
                else if (namazVakitleri["Aksam"] > now)
                {
                    var kalan = namazVakitleri["Aksam"] - now;
                    return ("Aksam", FormatKalanSure(kalan));
                }
                else if (namazVakitleri["Yatsi"] > now)
                {
                    var kalan = namazVakitleri["Yatsi"] - now;
                    return ("Yatsi", FormatKalanSure(kalan));
                }
                else
                {
                    // Yarin imsak
                    var yarinImsak = namazVakitleri["Ýmsak"].AddDays(1);
                    var kalan = yarinImsak - now;
                    return ("Imsak", FormatKalanSure(kalan));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Prayer time error: {ex.Message}");
                return ("Hata", "--");
            }
        }

        private static string FormatKalanSure(TimeSpan kalan)
        {
            return $"{kalan.Hours}s {kalan.Minutes}d";
        }

        private static DateTime ParsePrayerTime(string? timeString)
        {
            if (string.IsNullOrEmpty(timeString))
                return DateTime.Today;

            try
            {
                return DateTime.Today + TimeSpan.Parse(timeString);
            }
            catch
            {
                return DateTime.Today;
            }
        }

        private static async Task<(double latitude, double longitude)> GetLocationAsync(Context context)
        {
            try
            {
                // .NET MAUI Preferences klasor adi: PackageName + ".xamarinessentials"
                // Ama daha iyi yontem: Dogrudan Preferences.Default kullan
                
                // OtomatikKonum kontrolu
                bool otomatikKonum = true;
                double manuelLat = 0;
                double manuelLon = 0;
                
                // SharedPreferences'tan oku
                var prefsName = $"{context.PackageName}.xamarinessentials";
                var prefs = context.GetSharedPreferences(prefsName, FileCreationMode.Private);
                
                if (prefs != null)
                {
                    // Boolean degerleri oku
                    otomatikKonum = prefs.GetBoolean("OtomatikKonum", true);
                    
                    // Double degerleri oku - MAUI string olarak sakliyor
                    var latStr = prefs.GetString("ManuelLatitude", "0");
                    var lonStr = prefs.GetString("ManuelLongitude", "0");
                    
                    // Invariant culture ile parse et
                    if (!string.IsNullOrEmpty(latStr) && !string.IsNullOrEmpty(lonStr))
                    {
                        if (double.TryParse(latStr, System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out double parsedLat))
                        {
                            manuelLat = parsedLat;
                        }
                        
                        if (double.TryParse(lonStr, System.Globalization.NumberStyles.Any, 
                            System.Globalization.CultureInfo.InvariantCulture, out double parsedLon))
                        {
                            manuelLon = parsedLon;
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"Widget konum: otomatik={otomatikKonum}, lat={manuelLat}, lon={manuelLon}");
                }

                if (!otomatikKonum && manuelLat != 0 && manuelLon != 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Manuel konum kullaniliyor: {manuelLat}, {manuelLon}");
                    return (manuelLat, manuelLon);
                }

                // Varsayilan konum (Istanbul)
                System.Diagnostics.Debug.WriteLine("Varsayilan konum kullaniliyor: Istanbul");
                return (41.0082, 28.9784);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
                return (41.0082, 28.9784); // Istanbul
            }
        }

        private void SetupAutoUpdate(Context? context)
        {
            if (context == null)
                return;

            try
            {
                var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
                if (alarmManager == null)
                    return;

                var intent = new Intent(context, typeof(ClockWeatherWidget));
                intent.SetAction(ActionAutoUpdate);
                
                var pendingIntent = PendingIntent.GetBroadcast(
                    context, 
                    0, 
                    intent, 
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

                var triggerTime = Java.Lang.JavaSystem.CurrentTimeMillis() + 60000;
                
                alarmManager.SetRepeating(
                    AlarmType.RtcWakeup,
                    triggerTime,
                    60000,
                    pendingIntent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Setup alarm error: {ex.Message}");
            }
        }

        public override void OnEnabled(Context? context)
        {
            base.OnEnabled(context);
            SetupAutoUpdate(context);
        }

        public override void OnDisabled(Context? context)
        {
            base.OnDisabled(context);
            
            if (context != null)
            {
                try
                {
                    var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
                    var intent = new Intent(context, typeof(ClockWeatherWidget));
                    intent.SetAction(ActionAutoUpdate);
                    var pendingIntent = PendingIntent.GetBroadcast(
                        context, 
                        0, 
                        intent, 
                        PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
                        
                    alarmManager?.Cancel(pendingIntent);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Cancel alarm error: {ex.Message}");
                }
            }
        }
    }
}
