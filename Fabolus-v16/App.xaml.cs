using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ControlzEx.Theming;
using Fabolus_v16.MVVM.ViewModels;
using Fabolus_v16.Stores;
using Serilog;

namespace Fabolus_v16 {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Information()
				.WriteTo.File("Logs\\log.txt", rollingInterval: RollingInterval.Day)
				.CreateLogger();

			ThemeManager.Current.ChangeTheme(this, "Dark.Steel");
			Log.Information("Application started.");
		}

        protected override void OnExit(ExitEventArgs e) {
			Log.Information("Application closing.");
			Log.CloseAndFlush();
			base.OnExit(e);
        }
    }
}
