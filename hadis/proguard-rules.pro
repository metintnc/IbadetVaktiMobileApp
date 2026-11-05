# ==============================================================================
# .NET MAUI & Mono Runtime Temel Koruma Kuralları
# ==============================================================================
-keep class android.support.multidex.MultiDexApplication { <init>(); }
-keep class net.dot.jni.** { *; <init>(); }
-keep class mono.MonoRuntimeProvider* { *; <init>(...); }
-keep class mono.MonoPackageManager { *; <init>(...); }
-keep class mono.MonoPackageManager_Resources { *; <init>(...); }
-keep class mono.android.** { *; <init>(...); }
-keep class mono.java.** { *; <init>(...); }
-keep class mono.javax.** { *; <init>(...); }
-keep class net.dot.jni.ManagedPeer { *; <init>(...); }
-keep class net.dot.android.ApplicationRegistration { *; }
-keep class net.dot.android.** { *; }

# MAUI Platform UI bileşenleri
-keep class com.microsoft.maui.PlatformInterop { *; <init>(...); }
-keep class com.microsoft.maui.PlatformFontSpan { *; <init>(...); }

# ==============================================================================
# Android Temel Yapı Taşı Kuralları
# ==============================================================================
# JNI / Native metotlar
-keepclasseswithmembernames class * {
    native <methods>;
}

# Enum değerleri
-keepclassmembers enum * {
    public static **[] values();
    public static ** valueOf(java.lang.String);
}

# Parcelable nesneleri
-keepclassmembers class * implements android.os.Parcelable {
    public static final ** CREATOR;
}

# XML üzerinden oluşturulan View yapılandırıcıları
-keepclasseswithmembers class * extends android.view.View {
    public <init>(android.content.Context);
    public <init>(android.content.Context, android.util.AttributeSet);
    public <init>(android.content.Context, android.util.AttributeSet, int);
}

# ==============================================================================
# Üçüncü Parti Kütüphaneler (AdMob, Play Core, WorkManager)
# ==============================================================================
# Google Mobile Ads (AdMob)
-keep public class com.google.android.gms.ads.** {
    public *;
}
-keep public class com.google.ads.** {
    public *;
}

# Google Play In-App Update
-keep class com.google.android.play.core.appupdate.** { *; }
-keep class com.google.android.play.core.install.** { *; }

# AndroidX WorkManager (Arka plan bildirim ve alarm işçileri)
-keep class * extends androidx.work.Worker {
    public <init>(android.content.Context, androidx.work.WorkerParameters);
}
-keep class * extends androidx.work.ListenableWorker {
    public <init>(android.content.Context, androidx.work.WorkerParameters);
}

# Uyarı ve notları filtrele (R8 derleme hatalarını önlemek için)
-dontwarn com.google.android.gms.ads.**
-dontwarn androidx.**
-dontnote androidx.**
