﻿using System;
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
            string explanation = "Eu estava apenas usando =[";
            Program.Controller.SendBugMessage( explanation);
        }

        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Program.Controller.OpenWebsite(string.Format("file://" + RuntimeSettings.LogFilePath));
        }

    }
}
