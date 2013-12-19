using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace GreenQloud.UI
{
    public partial class AboutWindow : Form
    {
        public AboutWindow()
        {

            InitializeComponent();
            this.label1.Text = "Version: "+ GlobalSettings.RunningVersion;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.Controller.OpenWebsite(string.Format("https://www.greenqloud.com"));
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.Controller.OpenWebsite(string.Format("https://github.com/greenqloud/qloudsync/tree/master/legal/Authors.txt"));
        }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.Controller.OpenWebsite("http://support.greenqloud.com");
        }


         // BugReport report = new BugReport();
         // private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
         // {
         // if (!report.Visible) 
         // {
         //   report.ShowDialog();
         // }
         // }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.Controller.OpenWebsite(string.Format("file://" + RuntimeSettings.LogFilePath));
        }

        private void AboutWindow_Load(object sender, EventArgs e)
        {

        }

    }
}
