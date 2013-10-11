using GreenQloud.Model;
using QloudSyncCore.Core.Persistence;
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
            EventRaven eventDao = new EventRaven();
            List<Event> events = eventDao.LastEvents;

            float currentAmountOfBytesReceived = trafficMonitor.GetBytesReceived();
            totalBandwidthConsumptionLabel.Text = string.Format("Total Bandwidth Consumption: {0} kb", (currentAmountOfBytesReceived / 1024).ToString("0.00"));
            currentBandwidthConsumptionLabel.Text = string.Format("Current Bandwidth Consumption: {0} kb/sec", (((currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024)).ToString("0.00"));
            if (((currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024) > 1)
            {
                numberofitems.Text = string.Format("Items in Process: {0}", 1);
            }
            else
            {
                numberofitems.Text = string.Format("Items in Process: {0}", 0);
            }
            //Progress Bar
            lastAmountOfBytesReceived = currentAmountOfBytesReceived;
        }

        private void downloadSampleFileButton_Click_1(object sender, EventArgs e)
        {
            string url = @"http://www.microsoft.com/downloads/info.aspx?na=41&srcfamilyid=92ced922-d505-457a-8c9c-84036160639f&srcdisplaylang=en&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f2%2f9%2f6%2f296AAFA4-669A-46FE-9509-93753F7B0F46%2fVS-KB-Brochure-CSharp-Letter-HiRez.pdf";
            WebClient client = new WebClient();

            client.DownloadFileAsync(new Uri(url), Path.GetTempFileName());
        }
    }
}
