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

namespace GreenQloud.UI.Setup
{
    public partial class ConfFolders : Form
    {
        public ConfFolders()
        {
            InitializeComponent();
            this.FormClosed += (sender, args) =>
            {
                OnExit(sender, args);
            };
            this.label2.Text = RuntimeSettings.HomePath;
        }

        public void OnExit(Object sender, EventArgs e)
        {
            this.Dispose();
            UIManager.GetInstance().OnExit(sender, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
            SelectedFoldersConfig.GetInstance().Write(this.label2.Text, "");
            Directory.CreateDirectory(this.label2.Text);
            //TODO COLOCAR O ICON....
            UIManager.GetInstance().ReadyToSync();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if ((new Invoker(folderBrowserDialog1).Invoke()) == DialogResult.OK)
            {
                this.label2.Text = Path.Combine(folderBrowserDialog1.SelectedPath, GlobalSettings.HomeFolderName) + Path.DirectorySeparatorChar;
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
}
