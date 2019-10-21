using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.VoiceNext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WamBot.Commands;
using WamBot.Data;
using WamBot.Services;
using WamWooWam.Core;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace WamBot
{
    internal class Program
    {
        [STAThread]
        internal static async Task Main(string[] args)
        {
            try
            {
                var source = new CancellationTokenSource();
                var isService = !(Debugger.IsAttached || args.Contains("--console"));

                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;

                var rootDirectory = Path.GetDirectoryName(pathToExe);
                var contentDirectory = Path.Combine(rootDirectory, "content");
                if (!Directory.Exists(contentDirectory))
                    Directory.CreateDirectory(contentDirectory);

                Directory.SetCurrentDirectory(rootDirectory);

                var host = new HostBuilder();
                host.UseContentRoot(contentDirectory);

                host.ConfigureHostConfiguration(b =>
                {
                    b.SetBasePath(rootDirectory);
                    b.AddEnvironmentVariables("WAMBOT_");
                    b.AddJsonFile("hostsettings.json", optional: true);
                    b.AddCommandLine(args);
                });

                host.ConfigureAppConfiguration((h, b) =>
                {
                    //b.AddUserSecrets("0BED1FE3-DF88-44D8-BAA0-508E5189D3D0");
                    b.SetBasePath(rootDirectory);
                    b.AddCommandLine(args);
                    b.AddEnvironmentVariables("WAMBOT_");
                    b.AddJsonFile("appsettings.json");
                    b.AddJsonFile($"appsettings.{h.HostingEnvironment.EnvironmentName}.json", optional: true);
                });

                host.ConfigureServices(ConfigureServices);

                if (isService)
                {
                    host.ConfigureServices((hostContext, services) => services.AddSingleton<IHostLifetime, WindowsServiceLifetime>());
                    await host.StartAsync();
                }
                else
                {
                    host.UseConsoleLifetime();
                    await host.RunConsoleAsync();
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText("error.json", JsonConvert.SerializeObject(ex));
                throw;
            }
        }

        private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            var client = new DiscordClient(new DiscordConfiguration()
            {
                Token = context.Configuration["Bot:Token"],
                LogLevel = DSharpPlus.LogLevel.Debug
            });

            client.UseVoiceNext(new VoiceNextConfiguration() { EnableIncoming = true });

            services.AddSingleton(client);

            services.AddMemoryCache()
                .AddSingleton<HttpClient>()
                .AddTransient(r => new Random((int)DateTime.Now.Ticks))
                .AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Debug))
                .AddDbContext<WamBotDatabase>((p, o) =>
                {
                    if (context.HostingEnvironment.IsDevelopment())
                    {
                        o.UseInMemoryDatabase("WamBot");
                    }
                    else
                    {
                        o.UseNpgsql(context.Configuration.GetConnectionString("Default"));
                    }
                });

            services.AddHostedService<BotService>();
            services.AddHostedService<WpfService>();
        }
    }
}
