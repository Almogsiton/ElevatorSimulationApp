using ElevatorSimulationApi.Services;

namespace ElevatorSimulationApi.Services;

public class ElevatorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ElevatorBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public ElevatorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ElevatorBackgroundService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int>("ElevatorSimulation:IntervalSeconds", 30);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var simulationService = scope.ServiceProvider.GetRequiredService<IElevatorSimulationService>();
                
                await simulationService.ProcessElevatorSimulationAsync();
                
                _logger.LogInformation("Elevator simulation processed at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in elevator background service");
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }
    }
} 