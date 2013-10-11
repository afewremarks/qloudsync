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
            this.currentBandwidthConsumptionLabel = new System.Windows.Forms.Label();
            this.numberofitems = new System.Windows.Forms.Label();
            this.listView1 = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // totalBandwidthConsumptionLabel
            // 
            this.totalBandwidthConsumptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.totalBandwidthConsumptionLabel.AutoSize = true;
            this.totalBandwidthConsumptionLabel.BackColor = System.Drawing.Color.Transparent;
            this.totalBandwidthConsumptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.totalBandwidthConsumptionLabel.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.totalBandwidthConsumptionLabel.Location = new System.Drawing.Point(12, 9);
            this.totalBandwidthConsumptionLabel.Name = "totalBandwidthConsumptionLabel";
            this.totalBandwidthConsumptionLabel.Size = new System.Drawing.Size(179, 13);
            this.totalBandwidthConsumptionLabel.TabIndex = 0;
            this.totalBandwidthConsumptionLabel.Text = "Total Bandwidth Consumption:";
            // 
            // currentBandwidthConsumptionLabel
            // 
            this.currentBandwidthConsumptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.currentBandwidthConsumptionLabel.AutoSize = true;
            this.currentBandwidthConsumptionLabel.BackColor = System.Drawing.Color.Transparent;
            this.currentBandwidthConsumptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.currentBandwidthConsumptionLabel.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.currentBandwidthConsumptionLabel.Location = new System.Drawing.Point(12, 31);
            this.currentBandwidthConsumptionLabel.Name = "currentBandwidthConsumptionLabel";
            this.currentBandwidthConsumptionLabel.Size = new System.Drawing.Size(191, 13);
            this.currentBandwidthConsumptionLabel.TabIndex = 1;
            this.currentBandwidthConsumptionLabel.Text = "Current Bandwidth Consumption:";
            // 
            // numberofitems
            // 
            this.numberofitems.AutoSize = true;
            this.numberofitems.BackColor = System.Drawing.Color.Transparent;
            this.numberofitems.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.numberofitems.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.numberofitems.Location = new System.Drawing.Point(479, 9);
            this.numberofitems.Name = "numberofitems";
            this.numberofitems.Size = new System.Drawing.Size(104, 13);
            this.numberofitems.TabIndex = 3;
            this.numberofitems.Text = "Items in Process:";
            // 
            // listView1
            // 
            this.listView1.Location = new System.Drawing.Point(15, 66);
            this.listView1.Name = "listView1";
            this.listView1.Size = new System.Drawing.Size(591, 168);
            this.listView1.TabIndex = 4;
            this.listView1.UseCompatibleStateImageBehavior = false;
            // 
            // NetworkManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackgroundImage = global::GreenQloud.Properties.Resources.Network;
            this.ClientSize = new System.Drawing.Size(624, 266);
            this.Controls.Add(this.listView1);
            this.Controls.Add(this.numberofitems);
            this.Controls.Add(this.currentBandwidthConsumptionLabel);
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
        private System.Windows.Forms.Label currentBandwidthConsumptionLabel;
        private System.Windows.Forms.Label numberofitems;
        private System.Windows.Forms.ListView listView1;
    }
}