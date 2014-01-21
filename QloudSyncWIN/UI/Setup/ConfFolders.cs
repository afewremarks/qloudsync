using GreenQloud.Model;
using System;
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
using GreenQloud.Repository;
using GreenQloud.Persistence.SQLite;


namespace GreenQloud.UI.Setup
{
    public partial class ConfFolders : Form
    {

        private List<System.Windows.Forms.CheckBox> remoteFoldersCheckboxes;

        public ConfFolders()
        {
            InitializeComponent();
            this.label2.Text = RuntimeSettings.DefaultHomePath;
            remoteFoldersCheckboxes = new List<System.Windows.Forms.CheckBox>();
            InitializeComponentCheckboxesFolders();
            this.FormClosed += (sender, args) =>
            {
                OnExit(sender, args);
            };
        }

        private void InitializeComponentCheckboxesFolders()
        {
            foreach (System.Windows.Forms.CheckBox check in remoteFoldersCheckboxes) {
                this.Controls.Remove(check);
            }
            remoteFoldersCheckboxes.Clear();
            // 
            // CheckBoxes
            // 
            List<RepositoryItem> remoteItems = new RemoteRepositoryController(null).RootFolders;
            List<RepositoryItem> localItems = new PhysicalRepositoryController(new LocalRepository(label2.Text, "", true, true)).RootFolders;
            List<RepositoryItem> totalItems = new List<RepositoryItem>();

            totalItems = remoteItems;
            foreach (RepositoryItem i in localItems) {
                if (!remoteItems.Contains(i)) {
                    totalItems.Add(i);
                }
            }

            foreach (RepositoryItem item in totalItems)
            {
                System.Windows.Forms.CheckBox checkBox1 = new System.Windows.Forms.CheckBox();
                checkBox1.BackColor = System.Drawing.Color.Transparent;
                checkBox1.Checked = true;
                checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
                checkBox1.Location = new System.Drawing.Point(82, 100 + ((remoteFoldersCheckboxes.Count + 1) * 17));
                checkBox1.Name = "checkBox1";
                checkBox1.Size = new System.Drawing.Size(250, 17);
                checkBox1.TabIndex = 17;
                checkBox1.Text = item.Key;
                checkBox1.UseVisualStyleBackColor = false;
                remoteFoldersCheckboxes.Add(checkBox1);
            }
            foreach (CheckBox item in remoteFoldersCheckboxes)
            {
                this.Controls.Add(item);
            }
        }

        public void OnExit(Object sender, EventArgs e)
        {
            this.Dispose();
            UIManager.GetInstance().OnExit(sender, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
            
            List<string> ignores = new List<string>();
            foreach (CheckBox check in remoteFoldersCheckboxes) {
                if(!check.Checked)
                    ignores.Add(check.Text);
            }
            Program.Controller.CreateDefaultRepo(this.label2.Text, ignores);
            UIManager.GetInstance().ReadyToSync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if ((new Invoker(folderBrowserDialog1).Invoke()) == DialogResult.OK)
            {
                this.label2.Text = Path.Combine(folderBrowserDialog1.SelectedPath, GlobalSettings.HomeFolderName) + Path.DirectorySeparatorChar;
                InitializeComponentCheckboxesFolders();
                this.Refresh();
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

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
