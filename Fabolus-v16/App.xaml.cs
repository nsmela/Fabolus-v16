﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ControlzEx.Theming;
using Fabolus_v16.MVVM.ViewModels;
using Fabolus_v16.Stores;

namespace Fabolus_v16 {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			ThemeManager.Current.ChangeTheme(this, "Dark.Steel");
		}
	}
}
