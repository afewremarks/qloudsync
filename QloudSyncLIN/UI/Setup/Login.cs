using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;

namespace GreenQloud.UI.Setup
{
    public partial class Login : Form
    {
        public delegate void LoginDone();
        public event LoginDone OnLoginDone;

        public Login()
        {
            InitializeComponent();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void BtnContinue_Click(object sender, EventArgs e)
        {
            try
            {
                QloudSync.Repository.S3Connection.Authenticate(this.TxtUserName.Text, this.TxtPassword.Text);
                Credential.Username = this.TxtUserName.Text;
                if (this.OnLoginDone != null)
                {
                    this.OnLoginDone();
                }
            }
            catch (WebException)
            {
                MessageBox.Show("An error ocurred while trying authenticate. Please, try again.");
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://my.greenqloud.com/registration");
        }
    }
}
