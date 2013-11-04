using GreenQloud.Model;
using GreenQloud.Persistence;
using GreenQloud.Persistence.SQLite;
using GreenQloud.Repository;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
            int size = this.checkedListBox1.Items.Count;
            LocalRepository repo = repoDao.RootRepo();
            for (int i = 0; i < size; i++) {
                if (!this.checkedListBox1.GetItemChecked(i))
                {
                    repoIgnore.Create(repo, this.checkedListBox1.Items[i].ToString());
                }
                else {
                    repoIgnore.Remove(repo, this.checkedListBox1.Items[i].ToString());
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if ((new Invoker(folderBrowserDialog1).Invoke()) == DialogResult.OK)
            {
                string pathTo = Path.Combine(folderBrowserDialog1.SelectedPath, GlobalSettings.HomeFolderName) + Path.DirectorySeparatorChar;
                if (MessageBox.Show("Are you sure of this? All files will be moved", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    try
                    {
                        Program.Controller.MoveSQFolder(pathTo);
                    }
                    catch {
                        Program.Controller.Alert("Cannot move StoraQloud folder while making changes on directory");
                    }
                }
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
