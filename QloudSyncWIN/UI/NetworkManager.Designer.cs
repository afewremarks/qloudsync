namespace GreenQloud.UI
{
    partial class NetworkManager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NetworkManager));
            this.totalBandwidthConsumptionLabel = new System.Windows.Forms.Label();
            this.currentBandwidthDownloadLabel = new System.Windows.Forms.Label();
            this.numberofitems = new System.Windows.Forms.Label();
            this.listView1 = new System.Windows.Forms.ListView();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.processLine = new System.Windows.Forms.Label();
            this.currentBandwidthUploadLabel = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // totalBandwidthConsumptionLabel
            // 
            this.totalBandwidthConsumptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.totalBandwidthConsumptionLabel.AutoSize = true;
            this.totalBandwidthConsumptionLabel.BackColor = System.Drawing.Color.Transparent;
            this.totalBandwidthConsumptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.totalBandwidthConsumptionLabel.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.totalBandwidthConsumptionLabel.Location = new System.Drawing.Point(9, 10);
            this.totalBandwidthConsumptionLabel.Name = "totalBandwidthConsumptionLabel";
            this.totalBandwidthConsumptionLabel.Size = new System.Drawing.Size(179, 13);
            this.totalBandwidthConsumptionLabel.TabIndex = 0;
            this.totalBandwidthConsumptionLabel.Text = "Total Bandwidth Consumption:";
            // 
            // currentBandwidthDownloadLabel
            // 
            this.currentBandwidthDownloadLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.currentBandwidthDownloadLabel.AutoSize = true;
            this.currentBandwidthDownloadLabel.BackColor = System.Drawing.Color.Transparent;
            this.currentBandwidthDownloadLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.currentBandwidthDownloadLabel.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.currentBandwidthDownloadLabel.Location = new System.Drawing.Point(9, 33);
            this.currentBandwidthDownloadLabel.Name = "currentBandwidthDownloadLabel";
            this.currentBandwidthDownloadLabel.Size = new System.Drawing.Size(175, 13);
            this.currentBandwidthDownloadLabel.TabIndex = 1;
            this.currentBandwidthDownloadLabel.Text = "Current Download Bandwidth:";
            // 
            // numberofitems
            // 
            this.numberofitems.AutoSize = true;
            this.numberofitems.BackColor = System.Drawing.Color.Transparent;
            this.numberofitems.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numberofitems.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.numberofitems.Location = new System.Drawing.Point(499, 10);
            this.numberofitems.Name = "numberofitems";
            this.numberofitems.Size = new System.Drawing.Size(104, 13);
            this.numberofitems.TabIndex = 3;
            this.numberofitems.Text = "Items in Process:";
            // 
            // listView1
            // 
            this.listView1.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.listView1.Location = new System.Drawing.Point(12, 152);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(591, 78);
            this.listView1.TabIndex = 4;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(12, 85);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(591, 23);
            this.progressBar1.TabIndex = 6;
            // 
            // processLine
            // 
            this.processLine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.processLine.AutoSize = true;
            this.processLine.BackColor = System.Drawing.Color.Transparent;
            this.processLine.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.processLine.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.processLine.Location = new System.Drawing.Point(9, 136);
            this.processLine.Name = "processLine";
            this.processLine.Size = new System.Drawing.Size(84, 13);
            this.processLine.TabIndex = 7;
            this.processLine.Text = "Process Line:";
            // 
            // currentBandwidthUploadLabel
            // 
            this.currentBandwidthUploadLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.currentBandwidthUploadLabel.AutoSize = true;
            this.currentBandwidthUploadLabel.BackColor = System.Drawing.Color.Transparent;
            this.currentBandwidthUploadLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.currentBandwidthUploadLabel.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.currentBandwidthUploadLabel.Location = new System.Drawing.Point(9, 58);
            this.currentBandwidthUploadLabel.Name = "currentBandwidthUploadLabel";
            this.currentBandwidthUploadLabel.Size = new System.Drawing.Size(159, 13);
            this.currentBandwidthUploadLabel.TabIndex = 5;
            this.currentBandwidthUploadLabel.Text = "Current Upload Bandwidth:";
            // 
            // NetworkManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GreenQloud.Properties.Resources.Network;
            this.ClientSize = new System.Drawing.Size(624, 241);
            this.Controls.Add(this.processLine);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.currentBandwidthUploadLabel);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.numberofitems);
            this.Controls.Add(this.currentBandwidthDownloadLabel);
            this.Controls.Add(this.totalBandwidthConsumptionLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NetworkManager";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Network Status";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label totalBandwidthConsumptionLabel;
        private System.Windows.Forms.Label currentBandwidthDownloadLabel;
        private System.Windows.Forms.Label numberofitems;
        private System.Windows.Forms.ListView listView1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label processLine;
        private System.Windows.Forms.Label currentBandwidthUploadLabel;
    }
}