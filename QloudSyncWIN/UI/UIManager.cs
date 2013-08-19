using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using QloudSyncCore;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;

namespace GreenQloud.UI
{
    public class UIManager : Form, ApplicationUI
    {
        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;
        public GreenQloud.UI.Setup.Login LoginWindow;
        public AboutWindow About;
        private bool isLoged;
        private static UIManager instance;

        public static UIManager GetInstance(){
            if(instance == null)
                instance = new UIManager();

            return instance;
        }

        private UIManager()
        {
            this.AddToSystemTray();
            this.LoginWindow = new Setup.Login();
            this.About = new AboutWindow();

            Program.Controller.ShowAboutWindowEvent += (() => this.About.ShowDialog());
            Program.Controller.ShowSetupWindowEvent += ((PageType page_type) => this.LoginWindow.ShowDialog());
            this.LoginWindow.OnLoginDone += (() =>
            {
                this.isLoged = true;
                this.LoginWindow.Hide();
                this.LoginWindow.Close();
                UIManager.GetInstance().BuildMenu();
                Program.Controller.SyncStart();
            });
            this.LoginWindow.FormClosed += ((sender, args) =>
            {
                if (this.isLoged)
                {
                    Application.DoEvents();
                }
            });
        }

        public void Run() {
            Program.Controller.UIHasLoaded();
        }

        private void AddToSystemTray()
        {
            this.trayMenu = new ContextMenu();
            this.trayIcon = new NotifyIcon();
            this.trayIcon.Text = GlobalSettings.ApplicationName;
            this.trayIcon.Icon = Icon.FromHandle(((Bitmap)Icons.ResourceManager.GetObject("process_syncing_idle")).GetHicon());
            this.trayIcon.ContextMenu = trayMenu;
            this.trayIcon.Visible = true;
        }

        public void BuildMenu()
        {
            
                this.trayMenu.MenuItems.Clear();

                string savings = GetSavings();
                if (savings.Length > 0)
                    this.trayMenu.MenuItems.Add(GetSavings());
                this.trayMenu.MenuItems.Add("StorageQLoud Folder", OpenStorageQloudFolder);
                this.trayMenu.MenuItems.Add("Share/View Online...", OpenStorageQloudWebsite);
                this.trayMenu.MenuItems.Add("-");
                MenuItem recentlyChanged = new MenuItem("Recently Changed");
                recentlyChanged.Enabled = false;
                this.trayMenu.MenuItems.Add(recentlyChanged);
                this.trayMenu.MenuItems.Add("-");

                LoadRecentlyChangedItems();

                this.trayMenu.MenuItems.Add("-");

                this.trayMenu.MenuItems.Add("Help Center", OpenStorageQloudHelpCenter);
                this.trayMenu.MenuItems.Add("About QloudSync", ShowAboutWindow);

                this.trayMenu.MenuItems.Add("-");
                this.trayMenu.MenuItems.Add("Quit", OnExit);
            
        }

        private void LoadRecentlyChangedItems()
        {
            if (Program.Controller.DatabaseLoaded())
            {
                SQLiteEventDAO eventDao = new SQLiteEventDAO();
                List<Event> events = eventDao.LastEvents;
                string text = "";

                foreach (Event e in events)
                {
                    MenuItem current = new MenuItem();
                    current.Text = e.ItemName;
                    this.trayMenu.MenuItems.Add(current);
                }
            }
        }

        private string GetSavings()
        {
            try
            {
                CO2Savings saving = Statistics.EarlyCO2Savings;
                return string.Format("Yearly CO₂ Savings: {0}", saving.Saved);
            } catch {
                return ""; 
            }
        }

        public void OpenStorageQloudFolder(Object sender, EventArgs e)
        {
            Program.Controller.OpenSparkleShareFolder();
        }
        public void OpenStorageQloudWebsite(Object sender, EventArgs e)
        {
            string hash = Crypto.GetHMACbase64(Credential.SecretKey, Credential.PublicKey, true);
            Program.Controller.OpenWebsite(string.Format("https://my.greenqloud.com/qloudsync?username={0}&hashValue={1}&returnUrl=/storageQloud", Credential.Username, hash));
        }

        public void OpenStorageQloudHelpCenter(Object sender, EventArgs e)
        {
            Program.Controller.OpenWebsite("http://support.greenqloud.com");
        }

        public void ShowAboutWindow(Object sender, EventArgs e)
        {
            MessageBox.Show("QloudSync@ GreenQloud V.28 Placeholder.",
         "QloudSync");
        }

        protected override void OnLoad(EventArgs e)
        {
            this.Visible = false; // Hide form window.
            this.ShowInTaskbar = false; // Remove from taskbar.

            base.OnLoad(e);
        }

        public void OnExit(Object sender, EventArgs e)
        {
            Program.Controller.StopSynchronizers();
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