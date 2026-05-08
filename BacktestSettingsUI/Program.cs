using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using QuantConnect.Lean.BacktestSettingsUI.Models;
using QuantConnect.Lean.BacktestSettingsUI.Services;

namespace QuantConnect.Lean.BacktestSettingsUI
{
    public static class Program
    {
        private const int PreferredPort = 5000;
        private const string PreferredUrl = "http://127.0.0.1:5000";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureUrls(builder);

            builder.Services.AddSingleton<LeanBacktestPaths>();
            builder.Services.AddSingleton<LeanConfigFileService>();
            builder.Services.AddSingleton<LeanResultFileService>();
            builder.Services.AddSingleton<TickerSearchService>();
            builder.Services.AddSingleton<IProcessRunner, SystemProcessRunner>();
            builder.Services.AddSingleton<LeanRunService>();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = context =>
                {
                    context.Context.Response.Headers.CacheControl = "no-store";
                    context.Context.Response.Headers.Pragma = "no-cache";
                    context.Context.Response.Headers.Expires = "0";
                }
            });

            app.MapGet("/api/settings", (LeanConfigFileService configFileService) =>
            {
                return Results.Ok(configFileService.Load());
            });

            app.MapGet("/api/tickers", (HttpRequest request, TickerSearchService tickerSearchService) =>
            {
                return Results.Ok(tickerSearchService.Search(request.Query["q"].ToString()));
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

        private static void ConfigureUrls(WebApplicationBuilder builder)
        {
            if (HasExplicitUrlConfiguration(builder))
            {
                return;
            }

            builder.WebHost.UseUrls(IsLoopbackPortAvailable(PreferredPort)
                ? PreferredUrl
                : "http://127.0.0.1:0");
        }

        private static bool HasExplicitUrlConfiguration(WebApplicationBuilder builder)
        {
            return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"))
                || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DOTNET_URLS"))
                || !string.IsNullOrWhiteSpace(builder.Configuration["urls"])
                || !string.IsNullOrWhiteSpace(builder.Configuration["URLS"]);
        }

        private static bool IsLoopbackPortAvailable(int port)
        {
            TcpListener listener = null;

            try
            {
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                listener?.Stop();
            }
        }
    }
}
