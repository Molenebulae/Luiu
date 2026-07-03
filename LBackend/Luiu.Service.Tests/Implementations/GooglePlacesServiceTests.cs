using Luiu.Service.Implementations;
using System.Text.Json;

namespace Luiu.Service.Tests.Implementations
{
    public class GooglePlacesServiceTests
    {
        [Fact]
        public void FormatOpeningHoursJsonForResponse_EnglishWeekdays_ReturnsChineseWeekdaysAndTwentyFourHourTimes()
        {
            // Arrange
            var weekdayDescriptions = new[]
            {
                "Monday: 9:00 AM – 6:00 PM",
                "Tuesday: 9:00 AM – 6:00 PM",
                "Wednesday: 9:00 AM – 6:00 PM",
                "Thursday: 9:00 AM – 6:00 PM",
                "Friday: 9:00 AM – 6:00 PM",
                "Saturday: 10:30 AM – 8:15 PM",
                "Sunday: 12:00 AM – 12:00 PM"
            };

            // Act
            var result = GooglePlacesService.FormatOpeningHoursJsonForResponse(weekdayDescriptions);

            // Assert
            var hours = JsonSerializer.Deserialize<List<string>>(result!);
            Assert.Equal(new[]
            {
                "星期一: 09:00–18:00",
                "星期二: 09:00–18:00",
                "星期三: 09:00–18:00",
                "星期四: 09:00–18:00",
                "星期五: 09:00–18:00",
                "星期六: 10:30–20:15",
                "星期日: 00:00–12:00"
            }, hours);
        }

        [Fact]
        public void FormatOpeningHoursJsonForResponse_ChineseFullWidthColon_ReturnsHalfWidthColon()
        {
            // Arrange
            var weekdayDescriptions = new[]
            {
                "星期一：09:00 – 18:00",
                "週二：上午9:00 – 下午6:00"
            };

            // Act
            var result = GooglePlacesService.FormatOpeningHoursJsonForResponse(weekdayDescriptions);

            // Assert
            var hours = JsonSerializer.Deserialize<List<string>>(result!);
            Assert.Equal(new[]
            {
                "星期一: 09:00–18:00",
                "星期二: 09:00–18:00"
            }, hours);
        }

        [Fact]
        public void NormalizeOpeningHoursJsonForResponse_OldJsonArray_ReturnsNormalizedJsonArray()
        {
            // Arrange
            var oldJson = JsonSerializer.Serialize(new[]
            {
                "Mon: 9:00 AM - 6:00 PM",
                "星期日：休息"
            });

            // Act
            var result = GooglePlacesService.NormalizeOpeningHoursJsonForResponse(oldJson);

            // Assert
            var hours = JsonSerializer.Deserialize<List<string>>(result!);
            Assert.Equal(new[]
            {
                "星期一: 09:00–18:00",
                "星期日: 休息"
            }, hours);
        }

        [Fact]
        public void FormatOpeningHoursJsonForResponse_EmptyOrNull_ReturnsNull()
        {
            Assert.Null(GooglePlacesService.FormatOpeningHoursJsonForResponse(null));
            Assert.Null(GooglePlacesService.FormatOpeningHoursJsonForResponse(Array.Empty<string>()));
            Assert.Null(GooglePlacesService.FormatOpeningHoursJsonForResponse(new[] { "", " " }));
        }

        [Fact]
        public void NormalizeOpeningHoursJsonForResponse_UnrecognizedOrInvalidValue_DoesNotThrowAndPreservesValue()
        {
            // Arrange
            var unrecognizedJson = JsonSerializer.Serialize(new[] { "Holiday hours vary" });
            var invalidJson = "Holiday hours vary";

            // Act
            var unrecognizedResult = GooglePlacesService.NormalizeOpeningHoursJsonForResponse(unrecognizedJson);
            var invalidResult = GooglePlacesService.NormalizeOpeningHoursJsonForResponse(invalidJson);

            // Assert
            Assert.Equal(new[] { "Holiday hours vary" }, JsonSerializer.Deserialize<List<string>>(unrecognizedResult!));
            Assert.Equal(invalidJson, invalidResult);
        }
    }
}
