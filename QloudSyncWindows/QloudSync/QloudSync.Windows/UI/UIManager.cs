using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace GreenQloud.UI
{
    public class UIManager : Form
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        public GreenQloud.UI.Setup.Login LoginWindow;
        public GreenQloud.UI.Setup.Sync SyncWindow;
        public AboutWindow About;
        private bool isLoged;

        public UIManager()
        {
            this.AddToSystemTray();
            this.LoginWindow = new Setup.Login();
            this.SyncWindow = new Setup.Sync();
            this.About = new AboutWindow();

            Program.Controller.ShowAboutWindowEvent += (() => this.About.ShowDialog());
            Program.Controller.ShowSetupWindowEvent += (() => this.LoginWindow.ShowDialog());
            this.LoginWindow.OnLoginDone += (() => {
                this.isLoged = true;
                this.LoginWindow.Hide();
                this.LoginWindow.Close();
            });
            this.LoginWindow.FormClosed += ((sender, args) => {
                if (this.isLoged) 
                { 
                    Application.DoEvents();
                    this.SyncWindow.RunSync();
                }
            });
            
            Program.Controller.UIHasLoaded();
        }

        private void AddToSystemTray()
        {
            this.trayMenu = new ContextMenu();
            this.trayMenu.MenuItems.Add("Exit", OnExit);
            this.trayIcon = new NotifyIcon();
            this.trayIcon.Text = GlobalSettings.ApplicationName;
            this.trayIcon.Icon = Icon.FromHandle(((Bitmap)Icons.ResourceManager.GetObject("process_syncing_active")).GetHicon());
            this.trayIcon.ContextMenu = trayMenu;
            this.trayIcon.Visible = true;
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Visible = false; // Hide form window.
            this.ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        public void OnExit(Object sender, EventArgs e)
        {
            Program.Controller.SyncStop();
            Program.Controller.Quit();
            Application.Exit();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                trayIcon.Dispose();
            }

            base.Dispose(isDisposing);
        }
    }
}