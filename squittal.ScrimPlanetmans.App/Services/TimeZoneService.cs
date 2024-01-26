using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace squittal.ScrimPlanetmans.App.Services
{
    public class TimeZoneService
    {
        public const string JS_FUNC_NAME = "getUserTimeZone";

        private readonly IJSRuntime _jsRuntime;

        public TimeZoneService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
            Task.Run(SetTimeZone);
        }

        public TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Utc;

        public DateTimeOffset LocalNow => Localize(DateTime.UtcNow);

        public DateTimeOffset Localize(DateTime dateTime)
            => TimeZoneInfo.ConvertTime(dateTime, TimeZone);

        private async Task SetTimeZone()
        {
            string ianaTimeZone = await _jsRuntime.InvokeAsync<string>(JS_FUNC_NAME);

            try
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZone);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or SecurityException or InvalidTimeZoneException)
            {
                TimeZone = TimeZoneInfo.Utc;
            }
        }
    }
}
