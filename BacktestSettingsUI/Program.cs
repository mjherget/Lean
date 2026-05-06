using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using QuantConnect.Lean.BacktestSettingsUI.Models;
using QuantConnect.Lean.BacktestSettingsUI.Services;

namespace QuantConnect.Lean.BacktestSettingsUI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<LeanBacktestPaths>();
            builder.Services.AddSingleton<LeanConfigFileService>();
            builder.Services.AddSingleton<LeanResultFileService>();
            builder.Services.AddSingleton<IProcessRunner, SystemProcessRunner>();
            builder.Services.AddSingleton<LeanRunService>();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapGet("/api/settings", (LeanConfigFileService configFileService) =>
            {
                return Results.Ok(configFileService.Load());
            });

            app.MapPost("/api/settings", (BacktestSettingsRequest request, LeanConfigFileService configFileService) =>
            {
                if (!request.TryNormalize(out var settings, out var errors))
                {
                    return Results.ValidationProblem(errors);
                }

                return Results.Ok(configFileService.Save(settings));
            });

            app.MapPost("/api/run", async (BacktestSettingsRequest request, LeanConfigFileService configFileService, LeanRunService runService, CancellationToken cancellationToken) =>
            {
                if (!request.TryNormalize(out var settings, out var errors))
                {
                    return Results.ValidationProblem(errors);
                }

                var savedSettings = configFileService.Save(settings);
                var runStatus = await runService.StartAsync(savedSettings.ToSettings(), cancellationToken);
                if (runStatus == null)
                {
                    return Results.Conflict(new { message = "A backtest is already running." });
                }

                return Results.Ok(runStatus);
            });

            app.MapPost("/api/build", async (LeanRunService runService, CancellationToken cancellationToken) =>
            {
                var runStatus = await runService.BuildAsync(cancellationToken);
                if (runStatus == null)
                {
                    return Results.Conflict(new { message = "Another action is already in progress." });
                }

                return Results.Ok(runStatus);
            });

            app.MapPost("/api/rebuild", async (LeanRunService runService, CancellationToken cancellationToken) =>
            {
                var runStatus = await runService.RebuildAsync(cancellationToken);
                if (runStatus == null)
                {
                    return Results.Conflict(new { message = "Another action is already in progress." });
                }

                return Results.Ok(runStatus);
            });

            app.MapPost("/api/stop", async (LeanRunService runService, CancellationToken cancellationToken) =>
            {
                return Results.Ok(await runService.StopAsync(cancellationToken));
            });

            app.MapGet("/api/run-status", (LeanRunService runService) =>
            {
                return Results.Ok(runService.GetStatus());
            });

            app.MapGet("/api/results/latest", (LeanResultFileService resultFileService) =>
            {
                return Results.Ok(resultFileService.LoadLatest());
            });

            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
