using System;
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
    public partial class BugReport : Form
    {
        public BugReport()
        {
            InitializeComponent();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Program.Controller.OpenWebsite("http://support.greenqloud.com");
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                new SendMail().SendBugMessage("<html><p>" + this.comboBox1.SelectedItem + "</p><p>" + this.textBox1.Text + "</p></html>");
                this.Close();
            }
            catch (Exception ex) {
                Logger.LogInfo("ERROR", ex);
                Program.Controller.Alert("Could not send the message. Please check your internet status and try again later."); 
            }
        }
    }
}
