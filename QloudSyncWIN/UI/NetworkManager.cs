using QloudSyncCore.Core.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GreenQloud.UI
{
    public partial class NetworkManager : Form
    {
        NetworkTraffic trafficMonitor = null;
        Timer bandwidthCalcTimer = new Timer();
        float lastAmountOfBytesReceived;
        float lastAmountOfBytesSent;

        

        public NetworkManager()
        {
            InitializeComponent();

            trafficMonitor = new NetworkTraffic(Process.GetCurrentProcess().Id);
            bandwidthCalcTimer.Interval = 1000;
            bandwidthCalcTimer.Tick += new EventHandler(bandwidthCalcTimer_Tick);
            bandwidthCalcTimer.Enabled = true;
        }

        void bandwidthCalcTimer_Tick(object sender, EventArgs e)
        {

            float currentAmountOfBytesReceived = trafficMonitor.GetBytesReceived();
            float currentAmountofBytesSent = trafficMonitor.GetBytesSent();
            totalBandwidthConsumptionLabel.Text = string.Format("Total Bandwidth Consumption: {0} kb", (currentAmountOfBytesReceived / 1024).ToString("0.00"));
            currentBandwidthDownloadLabel.Text = string.Format("Current Download Bandwidth: {0} kb/sec", (((currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024)).ToString("0.00"));
            currentBandwidthUploadLabel.Text = string.Format("Current Upload Bandwidth: {0} kb/sec", Math.Abs(((currentAmountofBytesSent - lastAmountOfBytesSent) / 1024)).ToString("0.00"));
            if (((currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024) > 1)
            {
                numberofitems.Text = string.Format("Items in Process: {0}", 1);
            }
            else
            {
                numberofitems.Text = string.Format("Items in Process: {0}", 0);
            }
            lastAmountOfBytesReceived = currentAmountOfBytesReceived;
            lastAmountOfBytesSent = currentAmountofBytesSent;
        }

        private void downloadSampleFileButton_Click_1(object sender, EventArgs e)
        {
            string url = @"http://www.microsoft.com/downloads/info.aspx?na=41&srcfamilyid=92ced922-d505-457a-8c9c-84036160639f&srcdisplaylang=en&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f2%2f9%2f6%2f296AAFA4-669A-46FE-9509-93753F7B0F46%2fVS-KB-Brochure-CSharp-Letter-HiRez.pdf";
            WebClient client = new WebClient();

            client.DownloadFileAsync(new Uri(url), Path.GetTempFileName());
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

    }
}
