using GreenQloud.Model;
using GreenQloud.Persistence;
using GreenQloud.Persistence.SQLite;
using GreenQloud.Repository;
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
        public Preferences()
        {
            InitializeComponent();
            LoadAccountInfo();
        }

        private void LoadAccountInfo()
        {
            qloudversion.Text = string.Format("QloudSync Version: {0}", GlobalSettings.RunningVersion);
            localpath.Text = string.Format("Local StorageQloud Folder Path: {0}", RuntimeSettings.SelectedHomePath);
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
            backgroundWorker1.RunWorkerAsync();
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
            Program.Controller.InitializeSynchronizers(true);
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

        private void button4_Click(object sender, EventArgs e)
        {
            Program.Controller.CheckForUpdates();
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            while (this.Visible)
            {
                if (this.InvokeRequired)
                {

                    //this.listBox1.DataSource = null;
                    //this.listBox1.DataSource = RemoteRepositoryController.UnfinishedStatistics;
                    this.BeginInvoke(new Action(() => this.listBox1.Items.Clear()));
                    List<TransferStatistic> statistics = RemoteRepositoryController.UnfinishedStatistics;
                    foreach (TransferStatistic s in statistics)
                    {
                        this.BeginInvoke(new Action(() => this.listBox1.Items.Add(s.ToString())));
                    }
                    
                    this.BeginInvoke(new Action(() => this.listBox1.Refresh()));

                    this.BeginInvoke(new Action(() => this.listBox2.Items.Clear()));
                    List<TransferStatistic> statistics2 = RemoteRepositoryController.FinishedStatistics;
                    foreach (TransferStatistic s in statistics2)
                    {
                        this.BeginInvoke(new Action(() => this.listBox2.Items.Add(s.ToString())));
                    }

                    this.BeginInvoke(new Action(() => this.listBox2.Refresh()));

                }
                Thread.Sleep(1000);
            }
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
