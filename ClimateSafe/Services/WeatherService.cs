using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClimateSafe
{
    internal sealed record WeatherData(double Temperature, int Code, bool IsDay, DateTime Time)
    {
        public string Condition => Code switch
        {
            0 => "Cerah",
            1 or 2 => "Cerah berawan",
            3 => "Berawan",
            45 or 48 => "Berkabut",
            51 or 53 or 55 or 56 or 57 => "Gerimis",
            61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => "Hujan",
            71 or 73 or 75 or 77 or 85 or 86 => "Salju",
            95 or 96 or 99 => "Badai petir",
            _ => "Tidak diketahui"
        };

        public string Icon => Code switch
        {
            0 => IsDay ? "☀" : "☾",
            1 or 2 or 3 => "☁",
            45 or 48 => "☁",
            51 or 53 or 55 or 56 or 57 or 61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => "☂",
            71 or 73 or 75 or 77 or 85 or 86 => "❄",
            95 or 96 or 99 => "ϟ",
            _ => "?"
        };
    }

    internal sealed class WeatherService
    {
        private const string Url = "https://api.open-meteo.com/v1/forecast?latitude=-7.7956&longitude=110.3695" +
            "&current=temperature_2m,weather_code,is_day&timezone=Asia%2FJakarta";
        private static readonly HttpClient SharedClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        private readonly HttpClient client;

        public WeatherService(HttpClient? client = null) => this.client = client ?? SharedClient;

        public async Task<WeatherData> GetYogyakartaAsync()
        {
            using var response = await client.GetAsync(Url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var json = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);
            if (!json.RootElement.TryGetProperty("current", out var current) ||
                current.ValueKind != JsonValueKind.Object ||
                !current.TryGetProperty("temperature_2m", out var temperature) || !temperature.TryGetDouble(out double value) ||
                !double.IsFinite(value) ||
                !current.TryGetProperty("weather_code", out var weatherCode) || !weatherCode.TryGetInt32(out int code) ||
                !current.TryGetProperty("is_day", out var isDay) || !isDay.TryGetInt32(out int day) || (day != 0 && day != 1) ||
                !current.TryGetProperty("time", out var time) || !time.TryGetDateTime(out DateTime timestamp))
                throw new InvalidDataException("Data cuaca dari Open-Meteo tidak lengkap.");

            return new WeatherData(value, code, day == 1, timestamp);
        }
    }
}
