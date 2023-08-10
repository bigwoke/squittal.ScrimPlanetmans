using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace squittal.ScrimPlanetmans.App.SignalR.Clients
{
    internal interface IMatchControlClient : ISignalRClient
    {
        public void StartMatch(Action action);
        public void Rematch(Action action);
        public void ClearMatch(Action action);
        public void PauseOrResumeRound(Action action);
        public void EndRound(Action action);
        public void ResetRound(Action action);
    }

    public class MatchControlClient : SignalRClientBase, IMatchControlClient
    {
        public MatchControlClient(NavigationManager navManager) : base(navManager, "/Hubs/MatchControl")
        {
        }

        public void ClearMatch(Action action)
        {
            Connection.On(nameof(ClearMatch), action);
        }

        public void EndRound(Action action)
        {
            Connection.On(nameof(EndRound), action);
        }

        public void PauseOrResumeRound(Action action)
        {
            Connection.On(nameof(PauseOrResumeRound), action);
        }

        public void Rematch(Action action)
        {
            Connection.On(nameof(Rematch), action);
        }

        public void ResetRound(Action action)
        {
            Connection.On(nameof(ResetRound), action);
        }

        public void StartMatch(Action action)
        {
            Connection.On(nameof(StartMatch), action);
        }
    }
}
