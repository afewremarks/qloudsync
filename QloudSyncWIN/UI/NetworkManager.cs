using GreenQloud.Model;
using GreenQloud.Repository;
using LitS3;
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
        private static NetworkManager instance;
        List<RepositoryItem> items;
        RemoteRepositoryController remoteRepositoryController;
        private bool makeStep;
        private bool isUpload;

        public static NetworkManager GetInstance()
        {
            if (instance == null)
                instance = new NetworkManager();

            return instance;
        }

        public NetworkManager()
        {
            InitializeComponent();

            trafficMonitor = new NetworkTraffic(Process.GetCurrentProcess().Id);
            bandwidthCalcTimer.Interval = 1000;
            bandwidthCalcTimer.Tick += new EventHandler(bandwidthCalcTimer_Tick);
            bandwidthCalcTimer.Enabled = true;
            items = new List<RepositoryItem>();
            numberofitems.Text = string.Format("Items in Process: {0}", 0);
            makeStep = false;
            isUpload = false;        
        }

        void bandwidthCalcTimer_Tick(object sender, EventArgs e)
        {
            float currentAmountOfBytesReceived = trafficMonitor.GetBytesReceived();
            float currentAmountofBytesSent = trafficMonitor.GetBytesSent();
            totalBandwidthConsumptionLabel.Text = string.Format("Total Bandwidth Consumption: {0} kb", (currentAmountOfBytesReceived / 1024).ToString("0.00"));
            currentBandwidthDownloadLabel.Text = string.Format("Current Download Bandwidth: {0} kb/sec", (((currentAmountOfBytesReceived - lastAmountOfBytesReceived) / 1024)).ToString("0.00"));
            currentBandwidthUploadLabel.Text = string.Format("Current Upload Bandwidth: {0} kb/sec", Math.Abs(((currentAmountofBytesSent - lastAmountOfBytesSent) / 1024)).ToString("0.00"));   
            OnItemEvent();
            UpdateProgressBar();
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

        private void progressBar1_Click_1(object sender, EventArgs e)
        {

        }

        public void OnItemEvent()
        {
            Event e = Program.Controller.GetCurrentEvent();
            ResetProgressBar();
            ResetItemList();
            if (e != null)
            {
                EventType eventType = e.EventType;
                
                if (e.Item != null && eventType != EventType.DELETE)
                {
                    if (!items.Contains(e.Item))
                    {
                        numberofitems.Text = string.Format("Items in Process: {0}", 1);

                        makeStep = true;
                        if (e.RepositoryType == RepositoryType.LOCAL)
                        {
                            FileInfo fi = new FileInfo(e.Item.LocalAbsolutePath);

                            if (e.EventType == EventType.MOVE)
                            {
                                fi = new FileInfo(e.Item.ResultItem.LocalAbsolutePath);
                                items.Add(e.Item.ResultItem);
                            }
                            else
                            {
                                items.Add(e.Item);
                            }

                            progressBar1.Maximum = (int)fi.Length;

                            isUpload = true;
                            textBox1.AppendText(" -> Upload: " + e.Item.Name + " ... ");
                        }
                        else
                        {
                            items.Add(e.Item);
                            remoteRepositoryController = new RemoteRepositoryController(e.Item.Repository);
                            GetObjectResponse meta = remoteRepositoryController.GetMetadata(e.Item.Key);
                            isUpload = false;
                            progressBar1.Maximum = (int)meta.ContentLength;
                            textBox1.AppendText(" -> Download: " + e.Item.Name + " ... ");
                        }

                    }
                }
            }
        }
        

        private void ResetItemList()
        {
            if (items.Count == 50)
            {
                items.Clear();
            }
        }

        private void ResetProgressBar()
        {
            if (progressBar1.Value == progressBar1.Maximum)
            {
                progressBar1.Value = 0;
                makeStep = false;
                numberofitems.Text = string.Format("Items in Process: {0}", 0);
                textBox1.AppendText(" Done " + Environment.NewLine);
            }
        }

        private void UpdateProgressBar()
        {
            if (makeStep)
            {
                progressBar1.Step = (int)(trafficMonitor.GetBytesReceived() - lastAmountOfBytesReceived);
                if (isUpload)
                {
                    progressBar1.Step = (int)(trafficMonitor.GetBytesSent() - lastAmountOfBytesSent);
                }

                progressBar1.PerformStep();
            }
        }
    }
}
