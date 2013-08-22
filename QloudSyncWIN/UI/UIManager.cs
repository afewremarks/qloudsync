using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using QloudSyncCore;
using GreenQloud.Model;
using GreenQloud.Persistence.SQLite;
using System.Threading;

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
                //UIManager.GetInstance().BuildMenu();
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
            MenuItem savings = new MenuItem("");
            savings.Visible = false;
            this.trayMenu.MenuItems.Add(savings);
            this.trayMenu.MenuItems.Add("StorageQLoud Folder", OpenStorageQloudFolder);
            this.trayMenu.MenuItems.Add("Share/View Online...", OpenStorageQloudWebsite);
            this.trayMenu.MenuItems.Add("-");

            MenuItem recentlyChanged = new MenuItem("Recently Changed");
            recentlyChanged.Enabled = false;
            this.trayMenu.MenuItems.Add(recentlyChanged);
            
            //Dont remove this separators
            MenuItem recentlyChangedSeparator = new MenuItem("-");
            this.trayMenu.MenuItems.Add(recentlyChangedSeparator);
            //place to load recently changes
            MenuItem recentlyChangedFinalSeparator = new MenuItem("-");
            this.trayMenu.MenuItems.Add(recentlyChangedFinalSeparator);
            
            this.trayMenu.MenuItems.Add("Help Center", OpenStorageQloudHelpCenter);
            this.trayMenu.MenuItems.Add("About QloudSync", ShowAboutWindow);
            this.trayMenu.MenuItems.Add("-");
            this.trayMenu.MenuItems.Add("Quit", OnExit);

            this.trayMenu.Popup += (sender, args) => {
                LoadExtraItems(recentlyChangedSeparator, recentlyChangedFinalSeparator, savings);
            };
        }

        private void LoadExtraItems(MenuItem separator,  MenuItem finalSeparator, MenuItem savings)
        {

            //First load the recently changes
            int begin = this.trayMenu.MenuItems.IndexOf(separator);
            int end = this.trayMenu.MenuItems.IndexOf(finalSeparator);
            while(begin+1 < end) {
                this.trayMenu.MenuItems.RemoveAt(begin + 1);
                begin = this.trayMenu.MenuItems.IndexOf(separator);
                end = this.trayMenu.MenuItems.IndexOf(finalSeparator);
            }


            if (Program.Controller.DatabaseLoaded())
            {
                SQLiteEventDAO eventDao = new SQLiteEventDAO();
                List<Event> events = eventDao.LastEvents;
                
                foreach (Event e in events)
                {
                    end = this.trayMenu.MenuItems.IndexOf(finalSeparator);

                    MenuItem current = new MenuItem();
                    current.Text = e.ItemName;
                    this.trayMenu.MenuItems.Add(end, current);
                }
            }


            //Load savings in the end...
            new Thread(() =>
            {
                savings.Text = GetSavings();
                if (savings.Text.Length > 0 && !savings.Visible)
                    savings.Visible = true;
            }).Start();
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
            throw new AbortedOperationException("Closed");
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