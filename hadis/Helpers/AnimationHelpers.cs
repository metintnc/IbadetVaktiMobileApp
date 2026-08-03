namespace hadis.Helpers
{
    /// <summary>
    /// Tekrar kullanılabilir animasyon yardımcı sınıfı
    /// Allocation overhead'i azaltmak için optimize edilmiş
    /// </summary>
    public static class AnimationHelpers
    {
        // Sabit değerler - her seferinde yeniden oluşturulmuyor
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
            uint translationDuration = DefaultScaleInDuration)
        {
            element.CancelAnimations();
            element.Opacity = 0;
            element.TranslationY = 25;
            element.Scale = 1;
            
            return Task.WhenAll(
                element.FadeTo(1, fadeDuration, Easing.CubicOut),
                element.TranslateTo(0, 0, translationDuration, Easing.CubicOut)
            );
        }

        /// <summary>
        /// Element'i fade ve scale animasyonu ile gizler
        /// </summary>
        public static Task AnimateOut(this VisualElement element,
            uint fadeDuration = DefaultFadeOutDuration,
            uint translationDuration = DefaultScaleOutDuration)
        {
            element.CancelAnimations();
            
            return Task.WhenAll(
                element.FadeTo(0, fadeDuration, Easing.CubicIn),
                element.TranslateTo(0, 25, translationDuration, Easing.CubicIn)
            );
        }

        /// <summary>
        /// Birden fazla elementi sırayla animasyonlu olarak görünür yapar
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
        /// Birden fazla elementi aynı anda animasyonlu olarak görünür yapar
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
        /// Birden fazla elementi aynı anda animasyonlu olarak gizler
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
        /// Tüm animasyonları iptal eder
        /// </summary>
        public static void CancelAllAnimations(params VisualElement[] elements)
        {
            foreach (var element in elements)
            {
                element.CancelAnimations();
            }
        }

        /// <summary>
        /// Elementleri başlangıç durumuna getirir (görünmez, küçük)
        /// </summary>
        public static void PrepareForAnimation(params VisualElement[] elements)
        {
            foreach (var element in elements)
            {
                element.Opacity = 0;
                element.TranslationY = 25;
                element.Scale = 1;
            }
        }

        /// <summary>
        /// Basıldığında küçülüp büyüme efekti (buton/kart için)
        /// </summary>
        public static async Task TapBounce(this VisualElement element, 
            double scaleDown = 0.94, 
            uint duration = 80)
        {
            await element.ScaleTo(scaleDown, duration, Easing.CubicIn);
            await element.ScaleTo(1.0, duration, Easing.CubicOut);
        }

        /// <summary>
        /// Kıble oku gibi sürekli dönen elementler için smooth rotation
        /// </summary>
        public static Task SmoothRotateTo(this VisualElement element, 
            double targetRotation, 
            uint duration = 100)
        {
            double currentRotation = element.Rotation;
            
            // Normalize target rotation
            targetRotation = targetRotation % 360;
            if (targetRotation < 0) targetRotation += 360;

            // En kısa yolu bul
            double diff = targetRotation - currentRotation;
            while (diff < -180) diff += 360;
            while (diff > 180) diff -= 360;

            double finalTarget = currentRotation + diff;
            
            return element.RotateTo(finalTarget, duration, Easing.Linear);
        }
    }
}

