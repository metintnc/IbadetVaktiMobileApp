using System;

namespace hadis.Services
{
    public enum CompassAccuracy
    {
        Unreliable,
        Low,
        Medium,
        High
    }

    public interface INativeCompassService
    {
        event Action<CompassAccuracy> AccuracyChanged;
        void Start();
        void Stop();
    }
}
