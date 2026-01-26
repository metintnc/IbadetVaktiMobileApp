using System;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.Runtime;
using hadis.Services;

namespace hadis.Platforms.Android.Services
{
    public class AndroidCompassService : Java.Lang.Object, INativeCompassService, ISensorEventListener
    {
        private SensorManager _sensorManager;
        private Sensor _magnetometer;
        public event Action<CompassAccuracy> AccuracyChanged;

        public AndroidCompassService()
        {
            _sensorManager = (SensorManager)global::Android.App.Application.Context.GetSystemService(Context.SensorService);
            _magnetometer = _sensorManager.GetDefaultSensor(SensorType.MagneticField);
        }

        public void Start()
        {
            if (_magnetometer != null)
            {
                _sensorManager.RegisterListener(this, _magnetometer, SensorDelay.Ui);
            }
        }

        public void Stop()
        {
            if (_sensorManager != null)
            {
                _sensorManager.UnregisterListener(this);
            }
        }

        public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
        {
            if (sensor.Type == SensorType.MagneticField)
            {
                // Map Android SensorStatus to shared CompassAccuracy
                CompassAccuracy mappedAccuracy = accuracy switch
                {
                    SensorStatus.AccuracyHigh => CompassAccuracy.High,
                    SensorStatus.AccuracyMedium => CompassAccuracy.Medium,
                    SensorStatus.AccuracyLow => CompassAccuracy.Low,
                    _ => CompassAccuracy.Unreliable
                };
                
                AccuracyChanged?.Invoke(mappedAccuracy);
            }
        }

        public void OnSensorChanged(SensorEvent e)
        {
            // We don't need the values here, just the accuracy event.
            // But sometimes accuracy updates come through here or are implicitly updated.
            // For SensorType.MagneticField, accuracy changes trigger OnAccuracyChanged.
        }
    }
}
