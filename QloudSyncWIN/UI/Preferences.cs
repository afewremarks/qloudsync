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
using System.Linq;
using System.Text;
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
    }
}
