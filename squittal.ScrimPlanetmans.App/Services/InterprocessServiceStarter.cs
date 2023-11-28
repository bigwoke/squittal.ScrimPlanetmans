using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace squittal.ScrimPlanetmans.App.Services
{
    public class InterprocessServiceStarter : IHostedService
    {
        private readonly InterprocessService _interprocessService;

        public InterprocessServiceStarter(InterprocessService interprocessService)
        {
            _interprocessService = interprocessService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
            => _interprocessService.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken)
            => _interprocessService.StopAsync(cancellationToken);
    }
}
