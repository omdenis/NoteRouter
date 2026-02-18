using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NoteRouter.Services;

namespace NoteRouter.Workers;

public class ProcessingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessingWorker> _logger;

    public ProcessingWorker(IServiceScopeFactory scopeFactory, ILogger<ProcessingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Processing worker started");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IProcessingPipeline>();

            await pipeline.ProcessAllAsync(stoppingToken);

            _logger.LogInformation("Processing worker completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing worker failed");
            throw;
        }
    }
}
