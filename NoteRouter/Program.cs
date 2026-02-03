using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NoteRouter.Configuration;
using NoteRouter.Data;
using NoteRouter.Services;
using NoteRouter.Workers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting NoteRouter");

    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog
    builder.Services.AddSerilog();

    // Configure AppSettings
    builder.Services.Configure<AppSettings>(
        builder.Configuration.GetSection(AppSettings.SectionName));

    // Configure Entity Framework
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Register services
    builder.Services.AddSingleton<ITelegramService, TelegramService>();
    builder.Services.AddSingleton<ITranscriptionService, TranscriptionService>();
    builder.Services.AddScoped<IObsidianService, ObsidianService>();
    builder.Services.AddScoped<IProcessingPipeline, ProcessingPipeline>();

    // Register worker
    builder.Services.AddHostedService<ProcessingWorker>();

    var host = builder.Build();

    // Apply migrations on startup
    using (var scope = host.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied");
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;
