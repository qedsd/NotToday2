using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using NotToday.Helpers;
using NotToday.Notifications;
using NotToday.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace NotToday
{
    public partial class App : Application
    {
        private NotificationManager notificationManager;
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
            UnhandledException += App_UnhandledException;//UI线程
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;//后台线程
            Log.Init();
        }
        private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                Log.Error("发生致命错误");
            }
            Log.Error(e.ExceptionObject);
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            Log.Error(e.Exception);
        }
        private void OnProcessExit(object sender, EventArgs e)
        {
            notificationManager.Unregister();
        }
        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            SettingService.Load();
            m_window = new BaseWindow()
            {
                MainContent = new Views.LocalIntelPage()
            };
            m_window.Activate();
            m_window.AppWindow.Closing += AppWindow_Closing;
            WindowHelper.SetMainWindow(m_window);
            m_window.Activated += M_window_Activated;
            m_window.Activate();
        }

        private Window m_window;

        private void M_window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_window.Activated -= M_window_Activated;
            System.Threading.Tasks.Task.Run(() =>
            {
                notificationManager = new NotificationManager();
                notificationManager.Init();
            });
        }
        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            ((m_window as BaseWindow).MainContent as Views.LocalIntelPage).Close();
        }
    }
}
