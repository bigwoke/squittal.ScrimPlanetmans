using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;

namespace squittal.ScrimPlanetmans.App.SignalR.Hubs
{
    public interface IMatchDataHub
    {
        public Task MatchStateChanged(MatchStateUpdateMessage match);
    }

    public class MatchDataHub : Hub<IMatchDataHub>
    {
        public Task MatchStateChanged(MatchStateUpdateMessage match) => Clients.All.MatchStateChanged(match);
    }
}
