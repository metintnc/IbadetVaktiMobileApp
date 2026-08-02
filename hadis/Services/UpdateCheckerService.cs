using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;

namespace hadis.Services
{
    public class UpdateInfoModel
    {
        [JsonPropertyName("latest_version")]
        public string LatestVersion { get; set; } = "1.0.0";

        [JsonPropertyName("minimum_required_version")]
        public string MinimumRequiredVersion { get; set; } = "1.0.0";

        [JsonPropertyName("update_title")]
        public string UpdateTitle { get; set; } = "Yeni Güncelleme Mevcut! 🚀";

        [JsonPropertyName("update_message")]
        public string UpdateMessage { get; set; } = "Uygulamamıza yeni özellikler ve performans iyileştirmeleri eklendi. Kesintisiz bir deneyim için lütfen güncelleyin.";

        [JsonPropertyName("is_force_update")]
        public bool IsForceUpdate { get; set; } = false;
    }

    public enum UpdateStatus
    {
        None,
        Optional,
        Force
    }

    public class UpdateCheckResult
    {
        public UpdateStatus Status { get; set; } = UpdateStatus.None;
        public UpdateInfoModel Info { get; set; } = new UpdateInfoModel();
    }

    public class UpdateCheckerService
    {
        private readonly HttpClient _httpClient;
        
        // Paket adınız: com.metintnc.namazvakti
        public const string PackageName = "com.metintnc.namazvakti";

        // Yedek / Zorunlu Durum Kontrolü için GitHub URL
        public static string DefaultConfigUrl = "https://raw.githubusercontent.com/metintnc/namazvakti/main/version.json";

        public UpdateCheckerService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Play Store ve/veya GitHub üzerinden güncelleme kontrolü yapar.
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdateAsync(string? customUrl = null)
        {
            var result = new UpdateCheckResult();

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                    return result;

                Version currentVersion = AppInfo.Current.Version;

                // 1. KONTROL: GitHub Remote Config (Zorunlu Güncelleme Şalteri Kontrolü)
                UpdateInfoModel? remoteConfig = await FetchRemoteConfigAsync(customUrl);

                // 2. KONTROL: Play Store Sayfasından En Son Versiyonu Çek
                string? playStoreVersionStr = await FetchLatestVersionFromPlayStoreAsync();

                Version? targetVersion = null;
                string latestVersionString = "";

                if (!string.IsNullOrWhiteSpace(playStoreVersionStr) && Version.TryParse(CleanVersionString(playStoreVersionStr), out Version? pVer))
                {
                    targetVersion = pVer;
                    latestVersionString = playStoreVersionStr;
                }
                else if (remoteConfig != null && Version.TryParse(CleanVersionString(remoteConfig.LatestVersion), out Version? rVer))
                {
                    targetVersion = rVer;
                    latestVersionString = remoteConfig.LatestVersion;
                }

                if (targetVersion != null && targetVersion > currentVersion)
                {
                    result.Info.LatestVersion = latestVersionString;
                    result.Info.UpdateTitle = remoteConfig?.UpdateTitle ?? "Yeni Güncelleme Mevcut! 🚀";
                    
                    bool isForceByConfig = remoteConfig != null && (remoteConfig.IsForceUpdate || 
                        (Version.TryParse(CleanVersionString(remoteConfig.MinimumRequiredVersion), out Version? minReq) && currentVersion < minReq));

                    bool isForceByVersionJump = (targetVersion.Major > currentVersion.Major) || (targetVersion.Minor > currentVersion.Minor);

                    if (isForceByConfig || isForceByVersionJump)
                    {
                        result.Status = UpdateStatus.Force;
                        result.Info.UpdateMessage = remoteConfig?.UpdateMessage ?? 
                            $"Namaz Vakti uygulamasının önemli bir yeni sürümü ({latestVersionString}) yayınlandı. Uygulamayı kullanmaya devam edebilmek için lütfen güncelleyin.";
                        return result;
                    }

                    if (ShouldShowUpdateAlert(latestVersionString))
                    {
                        result.Status = UpdateStatus.Optional;
                        result.Info.UpdateMessage = remoteConfig?.UpdateMessage ?? 
                            $"Namaz Vakti uygulamasının yeni sürümü ({latestVersionString}) Play Store'da yayınlandı. Daha iyi bir deneyim için lütfen uygulamanızı güncelleyin.";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateCheckerService hatası: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Google Play Resmî In-App Update (Alttan Açılan Bottom Sheet) Akışını Başlatır.
        /// </summary>
        public async Task TriggerInAppUpdateAsync(bool isForce)
        {
#if ANDROID
            try
            {
                var activity = Platform.CurrentActivity;
                if (activity != null)
                {
                    var appUpdateManager = Xamarin.Google.Android.Play.Core.AppUpdate.AppUpdateManagerFactory.Create(activity);
                    var appUpdateInfoTask = appUpdateManager.GetAppUpdateInfo();

                    var listener = new OnSuccessListenerHandler(result =>
                    {
                        try
                        {
                            var info = result as Xamarin.Google.Android.Play.Core.AppUpdate.AppUpdateInfo;
                            if (info != null && info.UpdateAvailability() == Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model.UpdateAvailability.UpdateAvailable)
                            {
                                int updateType = isForce 
                                    ? Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model.AppUpdateType.Immediate 
                                    : Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model.AppUpdateType.Flexible;

                                if (info.IsUpdateTypeAllowed(updateType))
                                {
                                    appUpdateManager.StartUpdateFlowForResult(info, updateType, activity, 999);
                                    return;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"StartUpdateFlowForResult hatası: {ex.Message}");
                        }

                        // Resmî akış açılamadıysa mağaza linkini aç (Fallback)
                        _ = OpenPlayStoreAsync();
                    });

                    var failureListener = new OnFailureListenerHandler(ex =>
                    {
                        _ = OpenPlayStoreAsync();
                    });

                    appUpdateInfoTask.AddOnSuccessListener(listener);
                    appUpdateInfoTask.AddOnFailureListener(failureListener);
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Native In-App Update Hatası: {ex.Message}");
            }
#endif

            // Android dışı veya hata durumunda doğrudan Play Store açılır
            await OpenPlayStoreAsync();
        }

        private async Task<UpdateInfoModel?> FetchRemoteConfigAsync(string? customUrl)
        {
            try
            {
                string url = !string.IsNullOrWhiteSpace(customUrl) ? customUrl : DefaultConfigUrl;
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("Cache-Control", "no-cache");

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                string jsonContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UpdateInfoModel>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> FetchLatestVersionFromPlayStoreAsync()
        {
            try
            {
                string storeUrl = $"https://play.google.com/store/apps/details?id={PackageName}&hl=tr&gl=US";
                
                using var request = new HttpRequestMessage(HttpMethod.Get, storeUrl);
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");

                using var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                    return null;

                string html = await response.Content.ReadAsStringAsync();

                var match1 = Regex.Match(html, @"\[\[\[\""(\d+\.\d+(\.\d+)?)\""\]\]");
                if (match1.Success) return match1.Groups[1].Value;

                var match2 = Regex.Match(html, @"\""softwareVersion\""\s*:\s*\""([0-9\.]+)\""");
                if (match2.Success) return match2.Groups[1].Value;

                var match3 = Regex.Match(html, @"htl2eb\"">([0-9\.]+)</span>");
                if (match3.Success) return match3.Groups[1].Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play Store versiyon çekme hatası: {ex.Message}");
            }

            return null;
        }

        private bool ShouldShowUpdateAlert(string latestVersion)
        {
            string lastDismissedVersion = Preferences.Get("LastDismissedUpdateVersion", "");
            if (lastDismissedVersion == latestVersion)
            {
                long lastDismissedTime = Preferences.Get("LastDismissedUpdateTime", 0L);
                DateTime dismissedDate = new DateTime(lastDismissedTime);
                if (DateTime.UtcNow - dismissedDate < TimeSpan.FromDays(1))
                {
                    return false;
                }
            }
            return true;
        }

        public void DismissUpdate(string version)
        {
            Preferences.Set("LastDismissedUpdateVersion", version);
            Preferences.Set("LastDismissedUpdateTime", DateTime.UtcNow.Ticks);
        }

        public async Task OpenPlayStoreAsync()
        {
            try
            {
                string marketUri = $"market://details?id={PackageName}";
                bool opened = await Launcher.OpenAsync(marketUri);

                if (!opened)
                {
                    string webUri = $"https://play.google.com/store/apps/details?id={PackageName}";
                    await Launcher.OpenAsync(webUri);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play Store açılırken hata oluştu: {ex.Message}");
                try
                {
                    string webUri = $"https://play.google.com/store/apps/details?id={PackageName}";
                    await Launcher.OpenAsync(webUri);
                }
                catch { }
            }
        }

        private string CleanVersionString(string ver)
        {
            if (string.IsNullOrWhiteSpace(ver)) return "1.0.0";
            ver = ver.Trim().TrimStart('v', 'V');
            int dashIdx = ver.IndexOf('-');
            if (dashIdx > 0) ver = ver.Substring(0, dashIdx);
            return ver;
        }
    }

#if ANDROID
    internal class OnSuccessListenerHandler : Java.Lang.Object, Android.Gms.Tasks.IOnSuccessListener
    {
        private readonly Action<Java.Lang.Object?> _onSuccess;
        public OnSuccessListenerHandler(Action<Java.Lang.Object?> onSuccess) => _onSuccess = onSuccess;
        public void OnSuccess(Java.Lang.Object? result) => _onSuccess(result);
    }

    internal class OnFailureListenerHandler : Java.Lang.Object, Android.Gms.Tasks.IOnFailureListener
    {
        private readonly Action<Java.Lang.Exception> _onFailure;
        public OnFailureListenerHandler(Action<Java.Lang.Exception> onFailure) => _onFailure = onFailure;
        public void OnFailure(Java.Lang.Exception e) => _onFailure(e);
    }
#endif
}
