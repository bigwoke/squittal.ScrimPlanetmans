using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using squittal.ScrimPlanetmans.App.SignalR.Hubs;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.Services.ScrimMatch;

namespace squittal.ScrimPlanetmans.App.Services
{
    public class InterprocessService : IHostedService
    {
        private readonly IScrimMessageBroadcastService _messageService;
        private readonly IHubContext<MatchDataHub, IMatchDataHub> _dataHubContext;

        public InterprocessService(IScrimMessageBroadcastService messageService,
            IHubContext<MatchDataHub, IMatchDataHub> dataHubContext)
        {
            _messageService = messageService;
            _dataHubContext = dataHubContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _messageService.RaiseMatchStateUpdateEvent += OnMatchStateUpdate;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void OnMatchStateUpdate(object sender, ScrimMessageEventArgs<MatchStateUpdateMessage> e)
            => _dataHubContext.Clients.All.MatchStateChanged(e.Message);
    }
}
