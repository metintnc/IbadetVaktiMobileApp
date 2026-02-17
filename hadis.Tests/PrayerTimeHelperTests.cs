using hadis.Helpers;

namespace hadis.Tests
{
    public class PrayerTimeHelperTests
    {
        // ================================================================
        // GetNextPrayer Tests
        // ================================================================

        [Fact]
        public void GetNextPrayer_WhenBeforeImsak_ReturnsImsak()
        {
            // Arrange — tüm vakitler gelecekte (yarının vakitleri)
            var tomorrow = DateTime.Today.AddDays(1);
            var times = CreatePrayerTimes(tomorrow, 5, 7, 12, 15, 18, 20);

            // Act
            var result = PrayerTimeHelper.GetNextPrayer(times);

            // Assert
            Assert.Equal("İmsak Vaktine", result.DisplayName);
            Assert.Equal("İmsak", result.Key);
            Assert.Equal(0, result.Index);
        }

        [Fact]
        public void GetNextPrayer_WhenAfterImsak_ReturnsGunes()
        {
            // Arrange — İmsak geçmiş, diğerleri gelecekte
            var now = DateTime.Now;
            var times = new Dictionary<string, DateTime>
            {
                { "İmsak", now.AddMinutes(-30) },
                { "gunes", now.AddMinutes(30) },
                { "Ogle", now.AddHours(5) },
                { "İkindi", now.AddHours(8) },
                { "Aksam", now.AddHours(11) },
                { "Yatsi", now.AddHours(13) }
            };

            // Act
            var result = PrayerTimeHelper.GetNextPrayer(times);

            // Assert
            Assert.Equal("Güneşin Doğmasına", result.DisplayName);
            Assert.Equal("gunes", result.Key);
            Assert.Equal(1, result.Index);
        }

        [Fact]
        public void GetNextPrayer_WhenAfterYatsi_ReturnsNextDayImsak()
        {
            // Arrange — tüm vakitler geçmiş
            var now = DateTime.Now;
            var times = new Dictionary<string, DateTime>
            {
                { "İmsak", now.AddHours(-14) },
                { "gunes", now.AddHours(-12) },
                { "Ogle", now.AddHours(-7) },
                { "İkindi", now.AddHours(-4) },
                { "Aksam", now.AddHours(-2) },
                { "Yatsi", now.AddMinutes(-30) }
            };

            // Act
            var result = PrayerTimeHelper.GetNextPrayer(times);

            // Assert
            Assert.Equal("İmsak Vaktine", result.DisplayName);
            Assert.True(result.Remaining.TotalHours > 0);
        }

        [Fact]
        public void GetNextPrayer_WhenAfterOgle_ReturnsIkindi()
        {
            var now = DateTime.Now;
            var times = new Dictionary<string, DateTime>
            {
                { "İmsak", now.AddHours(-8) },
                { "gunes", now.AddHours(-6) },
                { "Ogle", now.AddMinutes(-30) },
                { "İkindi", now.AddHours(2) },
                { "Aksam", now.AddHours(5) },
                { "Yatsi", now.AddHours(7) }
            };

            var result = PrayerTimeHelper.GetNextPrayer(times);

            Assert.Equal("İkindi Namazına", result.DisplayName);
            Assert.Equal("İkindi", result.Key);
            Assert.Equal(3, result.Index);
        }

        // ================================================================
        // FormatTime Tests
        // ================================================================

        [Theory]
        [InlineData(5, 12, "05:12")]
        [InlineData(0, 0, "00:00")]
        [InlineData(23, 59, "23:59")]
        [InlineData(12, 5, "12:05")]
        public void FormatTime_ReturnsCorrectFormat(int hour, int minute, string expected)
        {
            var time = DateTime.Today.AddHours(hour).AddMinutes(minute);
            var result = PrayerTimeHelper.FormatTime(time);
            Assert.Equal(expected, result);
        }

        // ================================================================
        // FormatCountdown Tests
        // ================================================================

        [Fact]
        public void FormatCountdown_ReturnsCorrectFormat()
        {
            var remaining = new TimeSpan(2, 15, 30);
            var result = PrayerTimeHelper.FormatCountdown(remaining);
            Assert.Equal("02 : 15 : 30", result);
        }

        [Fact]
        public void FormatCountdown_ZeroValues_ReturnsZeroPadded()
        {
            var remaining = TimeSpan.Zero;
            var result = PrayerTimeHelper.FormatCountdown(remaining);
            Assert.Equal("00 : 00 : 00", result);
        }

        // ================================================================
        // GetHicriTarih Tests
        // ================================================================

        [Fact]
        public void GetHicriTarih_ReturnsNonEmptyString()
        {
            var result = PrayerTimeHelper.GetHicriTarih();
            Assert.NotEmpty(result);
            Assert.StartsWith("🌙", result);
        }

        // ================================================================
        // GetDailyAyet Tests
        // ================================================================

        [Fact]
        public void GetDailyAyet_ReturnsNonEmptyString()
        {
            var result = PrayerTimeHelper.GetDailyAyet();
            Assert.NotEmpty(result);
        }

        [Fact]
        public void GetDailyAyet_ReturnsSameAyetOnSameDay()
        {
            var result1 = PrayerTimeHelper.GetDailyAyet();
            var result2 = PrayerTimeHelper.GetDailyAyet();
            Assert.Equal(result1, result2);
        }

        // ================================================================
        // Helper
        // ================================================================

        private Dictionary<string, DateTime> CreatePrayerTimes(DateTime baseDate, int imsakHour, int gunesHour, int ogleHour, int ikindiHour, int aksamHour, int yatsiHour)
        {
            return new Dictionary<string, DateTime>
            {
                { "İmsak", baseDate.AddHours(imsakHour) },
                { "gunes", baseDate.AddHours(gunesHour) },
                { "Ogle", baseDate.AddHours(ogleHour) },
                { "İkindi", baseDate.AddHours(ikindiHour) },
                { "Aksam", baseDate.AddHours(aksamHour) },
                { "Yatsi", baseDate.AddHours(yatsiHour) }
            };
        }
    }
}
