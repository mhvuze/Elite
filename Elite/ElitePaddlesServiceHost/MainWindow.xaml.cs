using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using System.ServiceModel;
using System.Windows.Threading;

namespace ElitePaddlesServiceHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ServiceHost serviceHost;

        public MainWindow()
        {
            InitializeComponent();
            serviceHost = new ServiceHost(typeof(EliteServiceLibrary.EliteService));
            serviceHost.Open();

            new MinimizeToTrayInstance(this);
        }

        private class MinimizeToTrayInstance
        {
            private Window _window;
            private NotifyIcon _notifyIcon;

            public MinimizeToTrayInstance(Window window)
            {
                Debug.Assert(window != null, "window parameter is null.");
                _window = window;
                _window.Activated += new EventHandler(HandleStateChanged);
            }

            private void HandleStateChanged(object sender, EventArgs e)
            {
                if (_notifyIcon == null)
                {
                    // Initialize NotifyIcon instance "on demand"
                    _notifyIcon = new NotifyIcon();
                    _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetEntryAssembly().Location);
                    _notifyIcon.MouseClick += new MouseEventHandler(HandleNotifyIconOrBalloonClicked);
                    // _notifyIcon.BalloonTipClicked += new EventHandler(HandleNotifyIconOrBalloonClicked);
                }

                // Update copy of Window Title in case it has changed
                _notifyIcon.Text = _window.Title;

                // Minimize window
                _window.WindowState = WindowState.Minimized;


                // Show/hide Window and NotifyIcon
                var minimized = (_window.WindowState == WindowState.Minimized);
                _window.ShowInTaskbar = !minimized;
                _notifyIcon.Visible = minimized;

                /* Not really necessary when minimized at startup
                if (minimized && !_balloonShown)
                {
                    // If this is the first time minimizing to the tray, show the user what happened
                    _notifyIcon.ShowBalloonTip(1000, null, _window.Title, ToolTipIcon.None);
                    _balloonShown = true;
                }
                */
            }

            private void HandleNotifyIconOrBalloonClicked(object sender, EventArgs e)
            {
                // Restore the Window
                _window.WindowState = WindowState.Normal;
            }
        }
    }
}
