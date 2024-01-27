using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace squittal.ScrimPlanetmans.App.Services
{
#nullable enable
    public class TimeZoneService
    {
        public const string JS_FUNC_NAME = "getUserTimeZone";
        
        private readonly IJSRuntime _jsRuntime;

        public TimeZoneService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public TimeZoneInfo? TimeZone { get; private set; }

        public async Task<DateTimeOffset> LocalizeAsync(DateTimeOffset dto)
        {
            TimeZone ??= await GetTimeZoneAsync();
            return TimeZoneInfo.ConvertTime(dto, TimeZone);
        }

        private async Task<TimeZoneInfo> GetTimeZoneAsync()
        {
            string ianaTimeZone = await _jsRuntime.InvokeAsync<string>(JS_FUNC_NAME);

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(ianaTimeZone);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or SecurityException or InvalidTimeZoneException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }
#nullable disable
}
