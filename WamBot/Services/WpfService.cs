using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using WamWooWam.Wpf;

namespace WamBot.Services
{
    class WpfService : IHostedService, IDisposable
    {
        private const string AERO2_NORMAL = "pack://application:,,,/PresentationFramework.Aero2, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35;component\\themes/aero2.normalcolor.xaml";

        private Thread _wpfThread;
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _wpfThread = new Thread(() =>
            {
                var app = new Application();
                //app.Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri(AERO2_NORMAL) });
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                app.Run();
            });

            _wpfThread.SetApartmentState(ApartmentState.STA);
            _wpfThread.Name = "WpfThread";
            _wpfThread.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Application.Current.Dispatcher.InvokeShutdown();
            _wpfThread.Join();
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
