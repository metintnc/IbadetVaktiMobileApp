using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using System.Globalization;
using System.Text.Json;
using Android.Locations;
using System.Net.Http;

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
        public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
        {
            if (context == null || appWidgetManager == null || appWidgetIds == null)
                return;

            foreach (var appWidgetId in appWidgetIds)
            {
                _ = UpdateAppWidgetAsync(context, appWidgetManager, appWidgetId);
            }
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            base.OnReceive(context, intent);

            if (context == null || intent == null)
                return;

            var action = intent.Action;
            
            // Only update on standard widget events
            if (action == AppWidgetManager.ActionAppwidgetUpdate ||
                action == Intent.ActionTimezoneChanged) 
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

                // Time/Date is handled by TextClock automatically!
                // We only update Prayer Times

                var namazAdiId = context.Resources?.GetIdentifier("widget_namaz_adi", "id", packageName) ?? 0;
                var kalanSureId = context.Resources?.GetIdentifier("widget_kalan_sure", "id", packageName) ?? 0;
                var containerId = context.Resources?.GetIdentifier("widget_container", "id", packageName) ?? 0;

                // Namaz vakti bilgisini al
                var (namazAdi, kalanSureSpan) = await GetNextPrayerTimeAsync(context);
                
                if (namazAdiId != 0)
                    views.SetTextViewText(namazAdiId, namazAdi);
                
                if (kalanSureId != 0)
                {
                    // Chronometer'i ayarla
                    long remainingMillis = (long)kalanSureSpan.TotalMilliseconds;
                    // Eğer süre pozitifse (gelecekteyse)
                    if (remainingMillis > 0)
                    {
                        var elapsedRealtime = global::Android.OS.SystemClock.ElapsedRealtime();
                        var baseTime = elapsedRealtime + remainingMillis;
                        views.SetChronometer(kalanSureId, baseTime, null, true);
                        
                        // API 24+ için countdown özelliği
                        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.N)
                        {
                            views.SetChronometerCountDown(kalanSureId, true);
                        }
                    }
                    else
                    {
                        // Negatif veya 0 ise "--" göster (fallback olarak TextView olmasa da Chronometer 00:00 gösterebilir)
                        views.SetTextViewText(kalanSureId, "--");
                    }
                }

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

        private static async Task<(string namazAdi, TimeSpan kalanSure)> GetNextPrayerTimeAsync(Context context)
        {
            try
            {
                // 1. Bugunun verisini App Cache'den oku
                var cachedVakitler = await TryLoadFromAppCache(context, DateTime.Now);
                if (cachedVakitler != null)
                {
                    return FindNextPrayer(cachedVakitler);
                }

                // 2. Eger bugunun verisi yoksa (gece yarisi vb), yarininkini bulmaya calis
                var yarinVakitler = await TryLoadFromAppCache(context, DateTime.Now.AddDays(1));
                if (yarinVakitler != null)
                {
                    return FindNextPrayer(yarinVakitler);
                }

                return ("Veri Bekleniyor", TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Prayer time error: {ex.Message}");
                return ("Hata", TimeSpan.Zero);
            }
        }

        private static (string namazAdi, TimeSpan kalanSure) FindNextPrayer(Dictionary<string, DateTime> namazVakitleri)
        {
             var now = DateTime.Now;
                
             if (namazVakitleri["Imsak"] > now) return ("Imsak", namazVakitleri["Imsak"] - now);
             else if (namazVakitleri["Gunes"] > now) return ("Gunes", namazVakitleri["Gunes"] - now);
             else if (namazVakitleri["Ogle"] > now) return ("Ogle", namazVakitleri["Ogle"] - now);
             else if (namazVakitleri["Ikindi"] > now) return ("Ikindi", namazVakitleri["Ikindi"] - now);
             else if (namazVakitleri["Aksam"] > now) return ("Aksam", namazVakitleri["Aksam"] - now);
             else if (namazVakitleri["Yatsi"] > now) return ("Yatsi", namazVakitleri["Yatsi"] - now);
             else
             {
                 var yarinImsak = namazVakitleri["Imsak"].AddDays(1);
                 return ("Imsak", yarinImsak - now);
             }
        }

        // Yeni Diyanet API Onbellek dosyasini okumaya calis
        private static async Task<Dictionary<string, DateTime>> TryLoadFromAppCache(Context context, DateTime date)
        {
            try
            {
                // FileSystem.AppDataDirectory karsiligi Context.FilesDir
                var cachePath = System.IO.Path.Combine(context.FilesDir.AbsolutePath, "prayer_cache", $"prayer_{date:yyyy_MM_dd}.json");
                
                if (System.IO.File.Exists(cachePath))
                {
                    var json = await System.IO.File.ReadAllTextAsync(cachePath);
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    
                    DateTime ParseTime(string timeStr)
                    {
                        if (string.IsNullOrEmpty(timeStr)) return DateTime.MinValue;
                        try { return DateTime.Parse($"{date:yyyy-MM-dd} {timeStr}"); }
                        catch { return DateTime.MinValue; }
                    }

                    return new Dictionary<string, DateTime>
                    {
                        { "Imsak", ParseTime(root.GetProperty("fajr").GetString()) },
                        { "Gunes", ParseTime(root.GetProperty("sunrise").GetString()) },
                        { "Ogle", ParseTime(root.GetProperty("dhuhr").GetString()) },
                        { "Ikindi", ParseTime(root.GetProperty("asr").GetString()) },
                        { "Aksam", ParseTime(root.GetProperty("maghrib").GetString()) },
                        { "Yatsi", ParseTime(root.GetProperty("isha").GetString()) }
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Widget Cache Load Error: {ex.Message}");
            }
            return null;
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
                }

                if (manuelLat != 0 && manuelLon != 0)
                {
                    return (manuelLat, manuelLon);
                }

                // Varsayilan konum (Istanbul)
                return (41.0082, 28.9784);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Location error: {ex.Message}");
                return (41.0082, 28.9784); // Istanbul
            }
        }

        public override void OnEnabled(Context? context)
        {
            base.OnEnabled(context);
        }

        public override void OnDisabled(Context? context)
        {
            base.OnDisabled(context);
        }
    }
}
