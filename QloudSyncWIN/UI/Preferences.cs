using GreenQloud.Model;
using GreenQloud.Persistence;
using GreenQloud.Persistence.SQLite;
using GreenQloud.Repository;
using QloudSyncCore.Core.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GreenQloud.UI
{
    public partial class Preferences : Form
    {

        NetworkTraffic trafficMonitor = null;
        System.Windows.Forms.Timer bandwidthCalcTimer = new System.Windows.Forms.Timer();
        float lastAmountOfBytesReceived;
        float lastAmountOfBytesSent;
        List<RepositoryItem> items;
        RemoteRepositoryController remoteRepositoryController;
        private bool makeStep;
        private bool isUpload;

        public Preferences()
        {
            InitializeComponent();

            trafficMonitor = new NetworkTraffic(Process.GetCurrentProcess().Id);
            bandwidthCalcTimer.Interval = 1000;
            bandwidthCalcTimer.Tick += new EventHandler(bandwidthCalcTimer_Tick);
            bandwidthCalcTimer.Enabled = true;
            items = new List<RepositoryItem>();
            numberofitems.Text = string.Format("Items in Process: {0}", 0);
            makeStep = false;
            isUpload = false;
            LoadAccountInfo();
        }

        void bandwidthCalcTimer_Tick(object sender, EventArgs e)
        {
            float currentAmountOfBytesReceived = trafficMonitor.GetBytesReceived();
            float currentAmountofBytesSent = trafficMonitor.GetBytesSent();
            totalBandwidthConsumptionLabel.Text = string.Format("Total Bandwidth Used: {0} kb/sec", (currentAmountOfBytesReceived / 1024).ToString("0.00"));
            currentBandwidthDownloadLabel.Text = string.Format("Current Download Bandwidth: {0} kb/sec", (((currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024)).ToString("0.00"));
            currentBandwidthUploadLabel.Text = string.Format("Current Upload Bandwidth: {0} kb/sec", Math.Abs(((currentAmountofBytesSent - lastAmountOfBytesSent) / 1024)).ToString("0.00"));
            OnItemEvent();
            UpdateProgressBar();
            lastAmountOfBytesReceived = currentAmountOfBytesReceived;
            lastAmountOfBytesSent = currentAmountofBytesSent;

        }

        public void OnItemEvent()
        {
            Event e = Program.Controller.GetCurrentEvent();
            ResetProgressBar();
            ResetItemList();
            if (e != null)
            {
                EventType eventType = e.EventType;

                if (e.Item != null && eventType != EventType.DELETE)
                {
                    if (!items.Contains(e.Item))
                    {
                        numberofitems.Text = string.Format("Items Processed: {0}", 1);

                        makeStep = true;
                        if (e.RepositoryType == RepositoryType.LOCAL)
                        {
                            try {
                                FileInfo fi = new FileInfo(e.Item.LocalAbsolutePath);

                                if (e.EventType == EventType.MOVE)
                                {
                                    fi = new FileInfo(e.Item.ResultItem.LocalAbsolutePath);
                                    items.Add(e.Item.ResultItem);
                                }
                                else
                                {
                                    items.Add(e.Item);
                                }

                                progressBar1.Maximum = (int)fi.Length;

                                isUpload = true;
                                textBox1.AppendText(" ↓ " + e.Item.Name + " ... ");
                            }
                            catch
                            {
                                Logger.LogInfo("ERROR", "Network manager could not load informations");
                            }
                        }
                        else
                        {
                            try{
                                items.Add(e.Item);
                                remoteRepositoryController = new RemoteRepositoryController(e.Item.Repository);
                                isUpload = false;
                                progressBar1.Maximum = (int)remoteRepositoryController.GetContentLength(e.Item.Key);
                                textBox1.AppendText(" ↑ " + e.Item.Name + " ... ");
                            }
                            catch
                            {
                                Logger.LogInfo("ERROR", "Network manager could not load informations");
                            }
                        }

                    }
                }
            }
        }

        private void ResetItemList()
        {
            if (items.Count == 50)
            {
                items.Clear();
            }
        }

        private void ResetProgressBar()
        {
            if (progressBar1.Value == progressBar1.Maximum)
            {
                progressBar1.Value = 0;
                makeStep = false;
                numberofitems.Text = string.Format("Items in Process: {0}", 0);
                textBox1.AppendText(" Done " + Environment.NewLine);
            }
        }

        private void LoadAccountInfo()
        {
            qloudversion.Text = string.Format("QloudSync Version: {0}", GlobalSettings.RunningVersion);
            localpath.Text = string.Format("Local StorageQloud Folder Path: {0}", RuntimeSettings.HomePath);
        }


        private void UpdateProgressBar()
        {
            if (makeStep)
            {
                progressBar1.Step = (int)(trafficMonitor.GetBytesReceived() - lastAmountOfBytesReceived);
                if (isUpload)
                {
                    progressBar1.Step = (int)(trafficMonitor.GetBytesSent() - lastAmountOfBytesSent);
                }

                progressBar1.PerformStep();
            }
        }






        private List<System.Windows.Forms.CheckBox> remoteFoldersCheckboxes;
        private List<RepositoryIgnore> ignoreFolders;
        SQLiteRepositoryIgnoreDAO repoIgnore = new SQLiteRepositoryIgnoreDAO();
        SQLiteRepositoryDAO repoDao = new SQLiteRepositoryDAO();
        
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.checkedListBox1.Items.Clear();
            remoteFoldersCheckboxes = new List<System.Windows.Forms.CheckBox>();
            List<RepositoryItem> remoteItems = new RemoteRepositoryController(null).RootFolders;
            ignoreFolders = repoIgnore.All(repoDao.RootRepo());
            foreach (RepositoryItem item in remoteItems)
            {
                this.checkedListBox1.Items.Add(item.Key, !ignoreFolders.Any( i => i.Path.Equals(item.Key)));
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Wait wait = new Wait();
            wait.Show();
            wait.Focus();
            wait.BringToFront();
            int size = this.checkedListBox1.Items.Count;
            LocalRepository repo = repoDao.RootRepo();
            Program.Controller.KillSynchronizers();
            for (int i = 0; i < size; i++) {
                if (!this.checkedListBox1.GetItemChecked(i))
                {
                    repoIgnore.Create(repo, this.checkedListBox1.Items[i].ToString());
                }
                else {
                    repoIgnore.Remove(repo, this.checkedListBox1.Items[i].ToString());
                }
            }
            Program.Controller.InitializeSynchronizers();
            wait.Hide();
            this.Focus();
            this.BringToFront();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if ((new Invoker(folderBrowserDialog1).Invoke()) == DialogResult.OK)
            {
                string pathTo = Path.Combine(folderBrowserDialog1.SelectedPath, GlobalSettings.HomeFolderName) + Path.DirectorySeparatorChar;
                if (MessageBox.Show("Are you sure of this? All your files will be moved", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        Wait wait = new Wait();
                        wait.Show();
                        wait.Focus();
                        wait.BringToFront();
                        Program.Controller.MoveSQFolder(pathTo);
                        wait.Hide();
                        this.Focus();
                        this.BringToFront();
                    }
                    catch (Exception ex){
                        Logger.LogInfo("ERROR", ex);
                        Program.Controller.Alert("Cannot move StoraQloud");
                    }
                }
            }  
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to continue? You are unlinking your account to this computer", "Caution!", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    Program.Controller.UnlinkAccount();
                }
                catch (Exception ex)
                {
                    Logger.LogInfo("ERROR", ex);
                    Program.Controller.Alert("Cannot unlink accounts, please check your internet connection and try again");
                }
            }
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            greenusername.Text = string.Format("GreenQloud Username: {0}", Credential.Username);
            
        }
    }

    class Invoker
    {
        public CommonDialog InvokeDialog;
        private Thread InvokeThread;
        private DialogResult InvokeResult;

        public Invoker(CommonDialog dialog)
        {
            InvokeDialog = dialog;
            InvokeThread = new Thread(new ThreadStart(InvokeMethod));
            InvokeThread.SetApartmentState(ApartmentState.STA);
            InvokeResult = DialogResult.None;
        }

        public DialogResult Invoke()
        {
            InvokeThread.Start();
            InvokeThread.Join();
            return InvokeResult;
        }

        private void InvokeMethod()
        {
            InvokeResult = InvokeDialog.ShowDialog();

        }
    }
}
