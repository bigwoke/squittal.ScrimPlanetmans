using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

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
        public Task StartMatch() => Clients.All.StartMatch();
        public Task Rematch() => Clients.All.Rematch();
        public Task ClearMatch() => Clients.All.ClearMatch();
        public Task PauseOrResumeRound() => Clients.All.PauseOrResumeRound();
        public Task EndRound() => Clients.All.EndRound();
        public Task ResetRound() => Clients.All.ResetRound();
    }
}
