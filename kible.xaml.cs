// ...existing code...
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                _statusBarService.SetStatusBarColor("#000000");

                // Android'de cihaz "AccuracyHigh" gönderse bile sensör kalibrasyonu eskiyebiliyor. 
                // Bunu engellemek icin pusulaya her girildiginde 4 saniye boyunca uyariciyi gosterelim.
                if (CalibrationWarningFrame != null)
                {
                    _inInitialWarningPeriod = true;
                    CalibrationWarningFrame.IsVisible = true;
                    CalibrationWarningFrame.Opacity = 1;

                    Task.Delay(4000).ContinueWith(_ =>
                    {
                        _inInitialWarningPeriod = false;
                        OnCompassAccuracyChanged(_currentAccuracy);
                    });
                }

                await CheckAndStartCompass();
            }
// ...existing code...
        private void OnCompassAccuracyChanged(CompassAccuracy accuracy)
        {
            _currentAccuracy = accuracy;

            if (_inInitialWarningPeriod) return;

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                bool shouldBeVisible = (accuracy != CompassAccuracy.High);

                if (CalibrationWarningFrame != null)
// ...existing code...