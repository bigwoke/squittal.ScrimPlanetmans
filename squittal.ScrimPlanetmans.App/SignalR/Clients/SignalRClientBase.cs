using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace squittal.ScrimPlanetmans.App.SignalR.Clients
{
    public interface ISignalRClient
    {
        bool IsConnected { get; }
        Task StartAsync();
    }

    public class SignalRClientBase : ISignalRClient, IAsyncDisposable
    {
        protected SignalRClientBase(NavigationManager navManager, string hubPattern)
        {
            Connection = new HubConnectionBuilder()
                .WithUrl(navManager.ToAbsoluteUri(hubPattern))
                .WithAutomaticReconnect()
                .Build();
        }

        public bool IsConnected => Connection.State == HubConnectionState.Connected;

        protected HubConnection Connection { get; private set; }

        protected bool Started { get; private set; }

        public async ValueTask DisposeAsync()
        {
            if (Connection != null)
            {
                await Connection.DisposeAsync();
                GC.SuppressFinalize(this);
            }
        }

        public async Task StartAsync()
        {
            if (!Started)
            {
                await Connection.StartAsync();
                Started = true;
            }
        }
    }
}
