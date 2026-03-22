# .NET MAUI proguard kurallarý
-keep class com.metintnc.namazvakti.** { *; }

# Android AndroidX kütüphaneleri
-keep class androidx.** { *; }
-keep interface androidx.** { *; }

# Community Toolkit MVVM
-keep class CommunityToolkit.Mvvm.** { *; }

# Notification Plugin
-keep class Plugin.LocalNotification.** { *; }

# .NET Core reflection
-keepclassmembers class ** {
    public <methods>;
}

# Native methods
-keepclasseswithmembernames class * {
    native <methods>;
}

# Enum values
-keepclassmembers enum * {
    public static **[] values();
    public static ** valueOf(java.lang.String);
}

# Serialization compatibility
-keepclassmembers class * {
    *** **(java.lang.Object);
}

# View constructors for inflation
-keepclasseswithmembers class * {
    public <init>(android.content.Context, android.util.AttributeSet);
}

# Parcelable implementations
-keep class * implements android.os.Parcelable {
    public static final android.os.Parcelable$Creator *;
}
