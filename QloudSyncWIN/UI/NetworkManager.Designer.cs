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
            this.totalBandwidthConsumptionLabel = new System.Windows.Forms.Label();
            this.currentBandwidthConsumptionLabel = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.numberofitems = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // totalBandwidthConsumptionLabel
            // 
            this.totalBandwidthConsumptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.totalBandwidthConsumptionLabel.AutoSize = true;
            this.totalBandwidthConsumptionLabel.Location = new System.Drawing.Point(12, 9);
            this.totalBandwidthConsumptionLabel.Name = "totalBandwidthConsumptionLabel";
            this.totalBandwidthConsumptionLabel.Size = new System.Drawing.Size(151, 13);
            this.totalBandwidthConsumptionLabel.TabIndex = 0;
            this.totalBandwidthConsumptionLabel.Text = "Total Bandwidth Consumption:";
            // 
            // currentBandwidthConsumptionLabel
            // 
            this.currentBandwidthConsumptionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.currentBandwidthConsumptionLabel.AutoSize = true;
            this.currentBandwidthConsumptionLabel.Location = new System.Drawing.Point(12, 31);
            this.currentBandwidthConsumptionLabel.Name = "currentBandwidthConsumptionLabel";
            this.currentBandwidthConsumptionLabel.Size = new System.Drawing.Size(161, 13);
            this.currentBandwidthConsumptionLabel.TabIndex = 1;
            this.currentBandwidthConsumptionLabel.Text = "Current Bandwidth Consumption:";
            // 
            // panel1
            // 
            this.panel1.Location = new System.Drawing.Point(15, 57);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(576, 161);
            this.panel1.TabIndex = 2;
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(473, 25);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(100, 23);
            this.progressBar1.TabIndex = 0;
            // 
            // numberofitems
            // 
            this.numberofitems.AutoSize = true;
            this.numberofitems.Location = new System.Drawing.Point(470, 9);
            this.numberofitems.Name = "numberofitems";
            this.numberofitems.Size = new System.Drawing.Size(87, 13);
            this.numberofitems.TabIndex = 3;
            this.numberofitems.Text = "Items in Process:";
            // 
            // NetworkManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(603, 230);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.numberofitems);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.currentBandwidthConsumptionLabel);
            this.Controls.Add(this.totalBandwidthConsumptionLabel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NetworkManager";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Network Status";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label totalBandwidthConsumptionLabel;
        private System.Windows.Forms.Label currentBandwidthConsumptionLabel;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label numberofitems;
    }
}