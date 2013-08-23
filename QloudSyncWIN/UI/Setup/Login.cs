using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;



using System.Net;
using System.Threading;

namespace GreenQloud.UI.Setup
{
    public partial class Login : Form
    {
        public delegate void LoginDone();
        public event LoginDone OnLoginDone;
        string userMark = " Username";
        string passMark = " Password";



        public Login()
        {
            InitializeComponent();
            this.loadingGif.Visible = false;
            this.BtnRegister.TabIndex = 1;
            this.TxtUserName.Text = userMark ;
            this.TxtPassword.UseSystemPasswordChar = false;
            this.TxtPassword.Text = passMark ;
        }

        //cancel button if needed
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            UIManager.GetInstance().OpenStorageQloudRegistration(sender, e);
        }

        private void BtnContinue_Click(object sender, EventArgs e)
        {
            
                this.loadingGif.Visible = true;
                new Thread(() =>
                {
                    if (InvokeRequired)
                    {
                     
                        BeginInvoke(new Action(() =>{
                                        try
                                        {
                                            QloudSync.Repository.S3Connection.Authenticate(this.TxtUserName.Text, this.TxtPassword.Text);
                                            Credential.Username = this.TxtUserName.Text;
                                            if (this.OnLoginDone != null)
                                            {
                                                this.OnLoginDone();
                                            }
                                            else
                                            {
                                                this.loadingGif.Visible = false;
                                            }
                                        }
                                        catch (WebException)
                                        {
                                            this.loadingGif.Visible = false;
                                            MessageBox.Show("An error ocurred while trying authenticate. Please, try again.");
                                        }
                        }));
                        return;
                    }
                }).Start();
            
        }


        private void BtnContinue_KeyDown(object sender, KeyEventArgs e)
        {
            BtnContinue_Click(sender, e);
        }




        private void TxtUserName_Enter(object sender, EventArgs e)
        {
            if (this.TxtUserName.Text == userMark)
            {
                this.TxtUserName.Text = "";
                this.TxtUserName.ForeColor = System.Drawing.Color.Black;
            }
        }

        private void TxtUserName_Leave(object sender, EventArgs e)
        {
            if (this.TxtUserName.Text.Length == 0)
            {
                this.TxtUserName.Text = userMark;
                this.TxtUserName.ForeColor = System.Drawing.Color.DarkGray;
            }
        }


        private void TxtPassword_Enter(object sender, EventArgs e)
        {
            if (this.TxtPassword.Text == passMark)
            {
                this.TxtPassword.Text = "";
                this.TxtPassword.PasswordChar = '*'; 
                this.TxtPassword.ForeColor = System.Drawing.Color.Black;
            }

        }

        private void TxtPassword_Leave(object sender, EventArgs e)
        {
            if (this.TxtPassword.Text.Length == 0)
            {
                this.TxtPassword.UseSystemPasswordChar = false;
                this.TxtPassword.PasswordChar = new Char();
                this.TxtPassword.Text = passMark;
                this.TxtPassword.ForeColor = System.Drawing.Color.DarkGray;
            }

        }

        public void Done()
        {
            this.TxtPassword.Enabled = false;
            this.TxtUserName.Enabled = false;
            this.BtnContinue.Enabled = false;
            this.BtnRegister.Enabled = false;
        }

        private void TxtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == (char)Keys.Enter)
            {
                BtnContinue_Click(sender, e);
            }
        }
    }
}
