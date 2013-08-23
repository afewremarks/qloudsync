using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace GreenQloud.UI.Setup
{
    public partial class Sync : Form
    {
        private Thread syncThread;

        public Sync()
        {
            InitializeComponent();
        }

        internal void RunSync()
        {
            Program.Controller.FolderFetched += new Controller.FolderFetchedEventHandler(Controller_FolderFetched);
            Program.Controller.FolderFetchError += new Controller.FolderFetchErrorHandler(Controller_FolderFetchError);
            Program.Controller.FolderFetching += new Controller.FolderFetchingHandler(Controller_FolderFetching);

            this.syncThread = new Thread(() => Program.Controller.SyncStart());
            this.syncThread.Start();
            this.ShowDialog();
        }

        public void Controller_FolderFetching(double percentage, double time)
        {
            if (this.Visible)
            {
                this.Invoke((MethodInvoker)delegate
                        {
                            this.ProgressSync.Value = (int)percentage;
                            this.LblEstimatedTimeRemaining.Text = time.ToString();
                            Application.DoEvents();
                        }); 
            }
        }

        public void Controller_FolderFetchError(string[] errors)
        {
            //TODO: Implementar um diálogo.
        }

        public void Controller_FolderFetched()
        {
            this.Invoke((MethodInvoker)delegate
                {
                    this.BtnFinish.Enabled = true;
                    this.LblEstimatedTimeRemaining.Text = "0";
                    Application.DoEvents();
                });
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.syncThread.Abort();
            this.Close();
        }

        private void BtnFinish_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}