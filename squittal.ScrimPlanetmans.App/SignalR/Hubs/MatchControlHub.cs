using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using squittal.ScrimPlanetmans.App.Services;

namespace squittal.ScrimPlanetmans.App.Hubs
{
    public interface IMatchControlHub
    {
        public Task StartMatch();
        public Task Rematch();
        public Task ClearMatch();
        public Task PauseOrResumeRound();
        public Task EndRound();
        public Task ResetRound();
    }

    public class MatchControlHub : Hub<IMatchControlHub>
    {
        private readonly InterprocessService _ipcService;

        public MatchControlHub(InterprocessService ipcService)
        {
            _ipcService = ipcService;
        }

        public Task StartMatch() => _ipcService.ReceivedMatchControl(nameof(StartMatch));
        public Task Rematch() => _ipcService.ReceivedMatchControl(nameof(Rematch));
        public Task ClearMatch() => _ipcService.ReceivedMatchControl(nameof(ClearMatch));
        public Task PauseOrResumeRound() => _ipcService.ReceivedMatchControl(nameof(PauseOrResumeRound));
        public Task EndRound() => _ipcService.ReceivedMatchControl(nameof(EndRound));
        public Task ResetRound() => _ipcService.ReceivedMatchControl(nameof(ResetRound));
    }
}
