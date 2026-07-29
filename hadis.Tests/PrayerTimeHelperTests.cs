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
            Assert.Equal("Imsak", result.Key);
            Assert.Equal(0, result.Index);
        }

        [Fact]
        public void GetNextPrayer_WhenAfterImsak_ReturnsGunes()
        {
            // Arrange — İmsak geçmiş, diğerleri gelecekte
            var now = DateTime.Now;
            var times = new Dictionary<string, DateTime>
            {
                { "Imsak", now.AddMinutes(-30) },
                { "gunes", now.AddMinutes(30) },
                { "Ogle", now.AddHours(5) },
                { "Ikindi", now.AddHours(8) },
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
                { "Imsak", now.AddHours(-14) },
                { "gunes", now.AddHours(-12) },
                { "Ogle", now.AddHours(-7) },
                { "Ikindi", now.AddHours(-4) },
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
                { "Imsak", now.AddHours(-8) },
                { "gunes", now.AddHours(-6) },
                { "Ogle", now.AddMinutes(-30) },
                { "Ikindi", now.AddHours(2) },
                { "Aksam", now.AddHours(5) },
                { "Yatsi", now.AddHours(7) }
            };

            var result = PrayerTimeHelper.GetNextPrayer(times);

            Assert.Equal("İkindi Namazına", result.DisplayName);
            Assert.Equal("Ikindi", result.Key);
            Assert.Equal(3, result.Index);
        }

        // ================================================================
        // FormatTime Tests
        // ================================================================

        [Theory]
        [InlineData(5, 12, "05:12")]
        [InlineData(14, 5, "14:05")]
        [InlineData(0, 0, "00:00")]
        [InlineData(23, 59, "23:59")]
        public void FormatTime_ReturnsFormattedString(int hour, int minute, string expected)
        {
            var time = new DateTime(2025, 1, 1, hour, minute, 0);
            var result = PrayerTimeHelper.FormatTime(time);
            Assert.Equal(expected, result);
        }

        // ================================================================
        // FormatCountdown Tests
        // ================================================================

        [Fact]
        public void FormatCountdown_ReturnsFormattedString()
        {
            var remaining = new TimeSpan(2, 15, 30);
            var result = PrayerTimeHelper.FormatCountdown(remaining);
            Assert.Equal("02 : 15 : 30", result);
        }

        [Fact]
        public void FormatCountdown_ZeroTime_ReturnsZeroString()
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

        [Fact]
        public void GetHicriTarih_2026July29_ReturnsValidSaferDate()
        {
            var testDate = new DateTime(2026, 7, 29);
            var (gun, ay, yil) = DiyanetHicriHelper.GetHicriTarih(testDate);
            Assert.Equal(15, gun);
            Assert.Equal(2, ay); // Safer
            Assert.Equal(1448, yil);
        }

        [Fact]
        public void GetHicriTarih_FutureDate_FallsBackToUmAlQura()
        {
            var testDate = new DateTime(2035, 1, 1);
            var (gun, ay, yil) = DiyanetHicriHelper.GetHicriTarih(testDate);
            Assert.True(gun >= 1 && gun <= 30);
            Assert.True(ay >= 1 && ay <= 12);
            Assert.True(yil > 1450);
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

        // ================================================================
        // Helper
        // ================================================================

        private Dictionary<string, DateTime> CreatePrayerTimes(DateTime baseDate, int imsakHour, int gunesHour, int ogleHour, int ikindiHour, int aksamHour, int yatsiHour)
        {
            return new Dictionary<string, DateTime>
            {
                { "Imsak", baseDate.AddHours(imsakHour) },
                { "gunes", baseDate.AddHours(gunesHour) },
                { "Ogle", baseDate.AddHours(ogleHour) },
                { "Ikindi", baseDate.AddHours(ikindiHour) },
                { "Aksam", baseDate.AddHours(aksamHour) },
                { "Yatsi", baseDate.AddHours(yatsiHour) }
            };
        }
    }
}
