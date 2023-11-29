using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using squittal.ScrimPlanetmans.App.Hubs;
using squittal.ScrimPlanetmans.App.SignalR.Hubs;
using squittal.ScrimPlanetmans.ScrimMatch;
using squittal.ScrimPlanetmans.ScrimMatch.Messages;
using squittal.ScrimPlanetmans.ScrimMatch.Models;
using squittal.ScrimPlanetmans.Services.ScrimMatch;

namespace squittal.ScrimPlanetmans.App.Services
{
    public class InterprocessService
    {
        private readonly IScrimMessageBroadcastService _messageService;
        private readonly IHubContext<MatchDataHub, IMatchDataHub> _dataHubContext;
        private readonly IScrimMatchEngine _scrimMatchEngine;

        public InterprocessService(IScrimMessageBroadcastService messageService,
            IHubContext<MatchDataHub, IMatchDataHub> dataHubContext, IScrimMatchEngine scrimMatchEngine)
        {
            _messageService = messageService;
            _dataHubContext = dataHubContext;
            _scrimMatchEngine = scrimMatchEngine;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _messageService.RaiseMatchStateUpdateEvent += OnMatchStateUpdate;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public async Task ReceivedMatchControl(string signal)
        {
            switch (signal)
            {
                case nameof(MatchControlHub.StartMatch):
                    await Task.Run(_scrimMatchEngine.Start);
                    break;

                case nameof(MatchControlHub.Rematch):
                    if (_scrimMatchEngine.GetMatchState() == MatchState.Stopped || _scrimMatchEngine.GetMatchState() == MatchState.Uninitialized)
                    {
                        await Task.Run(() => _scrimMatchEngine.ClearMatch(true));
                    }
                    break;

                case nameof(MatchControlHub.ClearMatch):
                    if (_scrimMatchEngine.GetMatchState() == MatchState.Stopped || _scrimMatchEngine.GetMatchState() == MatchState.Uninitialized)
                    {
                        await Task.Run(() => _scrimMatchEngine.ClearMatch(false));
                    }
                    break;

                case nameof(MatchControlHub.PauseOrResumeRound):
                    if (_scrimMatchEngine.GetMatchState() == MatchState.Paused)
                    {
                        _scrimMatchEngine.ResumeRound();
                    }
                    else if (_scrimMatchEngine.GetMatchState() == MatchState.Running)
                    {
                        _scrimMatchEngine.PauseRound();
                    }
                    break;

                case nameof(MatchControlHub.EndRound):
                    if (_scrimMatchEngine.GetMatchState() == MatchState.Running)
                    {
                        await Task.Run(_scrimMatchEngine.EndRound);
                    }
                    break;

                case nameof(MatchControlHub.ResetRound):
                    if (_scrimMatchEngine.GetMatchState() == MatchState.Stopped && _scrimMatchEngine.GetCurrentRound() > 0)
                    {
                        await Task.Run(_scrimMatchEngine.ResetRound);
                    }
                    break;
            }

            _messageService.BroadcastMatchControlReceipt(new MatchControlSignalReceiptMessage(signal, DateTime.UtcNow));
        }

        private void OnMatchStateUpdate(object sender, ScrimMessageEventArgs<MatchStateUpdateMessage> e)
            => _dataHubContext.Clients.All.MatchStateChanged(e.Message);
    }
}
