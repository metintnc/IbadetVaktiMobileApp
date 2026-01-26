using System;
using hadis.Services;

namespace hadis.Services
{
    // Dummy implementation for non-Android platforms
    public class PlatformCompassService : INativeCompassService
    {
        public event Action<CompassAccuracy> AccuracyChanged;

        public void Start()
        {
            // No-op
            AccuracyChanged?.Invoke(CompassAccuracy.High); // Default to High on iOS etc where we don't handle it yet
        }

        public void Stop()
        {
            // No-op
        }
    }
}
