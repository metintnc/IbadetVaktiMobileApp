namespace hadis.Helpers
{
    /// <summary>
    /// Tekrar kullanýlabilir animasyon yardýmcý sýnýfý
    /// Allocation overhead'i azaltmak için optimize edilmiþ
    /// </summary>
    public static class AnimationHelpers
    {
        // Sabit deðerler - her seferinde yeniden oluþturulmuyor
        private const uint DefaultFadeInDuration = 400;
        private const uint DefaultScaleInDuration = 500;
        private const uint DefaultFadeOutDuration = 300;
        private const uint DefaultScaleOutDuration = 400;
        private const uint SequentialDelay = 80;

        /// <summary>
        /// Element'i fade ve scale animasyonu ile görünür yapar
        /// </summary>
        public static Task AnimateIn(this VisualElement element, 
            uint fadeDuration = DefaultFadeInDuration, 
            uint scaleDuration = DefaultScaleInDuration)
        {
            element.CancelAnimations();
            element.Opacity = 0;
            element.Scale = 0.7;
            
            return Task.WhenAll(
                element.FadeTo(1, fadeDuration, Easing.CubicOut),
                element.ScaleTo(1.0, scaleDuration, Easing.SpringOut)
            );
        }

        /// <summary>
        /// Element'i fade ve scale animasyonu ile gizler
        /// </summary>
        public static Task AnimateOut(this VisualElement element,
            uint fadeDuration = DefaultFadeOutDuration,
            uint scaleDuration = DefaultScaleOutDuration)
        {
            element.CancelAnimations();
            
            return Task.WhenAll(
                element.FadeTo(0, fadeDuration, Easing.CubicIn),
                element.ScaleTo(0.7, scaleDuration, Easing.CubicIn)
            );
        }

        /// <summary>
        /// Birden fazla elementi sýrayla animasyonlu olarak görünür yapar
        /// Fire-and-forget pattern ile allocation minimize edilir
        /// </summary>
        public static async Task AnimateInSequential(uint delay = SequentialDelay, params VisualElement[] elements)
        {
            foreach (var element in elements)
            {
                _ = element.AnimateIn(); // Fire-and-forget
                await Task.Delay((int)delay);
            }
        }

        /// <summary>
        /// Birden fazla elementi ayný anda animasyonlu olarak görünür yapar
        /// </summary>
        public static Task AnimateInParallel(params VisualElement[] elements)
        {
            var tasks = new Task[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                tasks[i] = elements[i].AnimateIn();
            }
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Birden fazla elementi ayný anda animasyonlu olarak gizler
        /// </summary>
        public static Task AnimateOutParallel(params VisualElement[] elements)
        {
            var tasks = new Task[elements.Length];
            for (int i = 0; i < elements.Length; i++)
            {
                tasks[i] = elements[i].AnimateOut();
            }
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Tüm animasyonlarý iptal eder
        /// </summary>
        public static void CancelAllAnimations(params VisualElement[] elements)
        {
            foreach (var element in elements)
            {
                element.CancelAnimations();
            }
        }

        /// <summary>
        /// Elementleri baþlangýç durumuna getirir (görünmez, küçük)
        /// </summary>
        public static void PrepareForAnimation(params VisualElement[] elements)
        {
            foreach (var element in elements)
            {
                element.Opacity = 0;
                element.Scale = 0.7;
            }
        }

        /// <summary>
        /// Basýldýðýnda küçülüp büyüme efekti (buton/kart için)
        /// </summary>
        public static async Task TapBounce(this VisualElement element, 
            double scaleDown = 0.85, 
            uint duration = 150)
        {
            await element.ScaleTo(scaleDown, duration, Easing.CubicIn);
            await element.ScaleTo(1.0, duration, Easing.CubicOut);
        }

        /// <summary>
        /// Kýble oku gibi sürekli dönen elementler için smooth rotation
        /// </summary>
        public static Task SmoothRotateTo(this VisualElement element, 
            double targetRotation, 
            uint duration = 100)
        {
            double currentRotation = element.Rotation;
            
            // Normalize target rotation
            targetRotation = targetRotation % 360;
            if (targetRotation < 0) targetRotation += 360;

            // En kýsa yolu bul
            double diff = targetRotation - currentRotation;
            while (diff < -180) diff += 360;
            while (diff > 180) diff -= 360;

            double finalTarget = currentRotation + diff;
            
            return element.RotateTo(finalTarget, duration, Easing.Linear);
        }
    }
}
