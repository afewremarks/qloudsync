namespace GreenQloud.UI.Setup
{
    partial class Sync
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Sync));
            this.pcbSync = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.ProgressSync = new System.Windows.Forms.ProgressBar();
            this.label4 = new System.Windows.Forms.Label();
            this.LblEstimatedTimeRemaining = new System.Windows.Forms.Label();
            this.BtnFinish = new System.Windows.Forms.Button();
            this.BtnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pcbSync)).BeginInit();
            this.SuspendLayout();
            // 
            // pcbSync
            // 
            this.pcbSync.Dock = System.Windows.Forms.DockStyle.Left;
            this.pcbSync.Image = global::GreenQloud.Backgrounds.side_splash;
            this.pcbSync.Location = new System.Drawing.Point(0, 0);
            this.pcbSync.Name = "pcbSync";
            this.pcbSync.Size = new System.Drawing.Size(150, 482);
            this.pcbSync.TabIndex = 0;
            this.pcbSync.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(190, 48);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(378, 40);
            this.label1.TabIndex = 1;
            this.label1.Text = "Winning! QloudSync is now connected to your \nGreenQloud account…";
            // 
            // label2
            // 
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(190, 104);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(458, 85);
            this.label2.TabIndex = 3;
            this.label2.Text = "You have successfully logged into GreenQloud and now QloudSync will find your tru" +
    "ly green™ files in StorageQloud and sync them to your computer in a folder named" +
    " StorageQloud.";
            // 
            // ProgressSync
            // 
            this.ProgressSync.Location = new System.Drawing.Point(194, 208);
            this.ProgressSync.Name = "ProgressSync";
            this.ProgressSync.Size = new System.Drawing.Size(454, 23);
            this.ProgressSync.Step = 1;
            this.ProgressSync.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(195, 252);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(120, 20);
            this.label4.TabIndex = 6;
            this.label4.Text = "Time remaining:";
            // 
            // LblEstimatedTimeRemaining
            // 
            this.LblEstimatedTimeRemaining.AutoSize = true;
            this.LblEstimatedTimeRemaining.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LblEstimatedTimeRemaining.Location = new System.Drawing.Point(321, 252);
            this.LblEstimatedTimeRemaining.Name = "LblEstimatedTimeRemaining";
            this.LblEstimatedTimeRemaining.Size = new System.Drawing.Size(82, 20);
            this.LblEstimatedTimeRemaining.TabIndex = 7;
            this.LblEstimatedTimeRemaining.Text = "estimating";
            // 
            // BtnFinish
            // 
            this.BtnFinish.Enabled = false;
            this.BtnFinish.Location = new System.Drawing.Point(573, 447);
            this.BtnFinish.Name = "BtnFinish";
            this.BtnFinish.Size = new System.Drawing.Size(75, 23);
            this.BtnFinish.TabIndex = 8;
            this.BtnFinish.Text = "Finish";
            this.BtnFinish.UseVisualStyleBackColor = true;
            this.BtnFinish.Click += new System.EventHandler(this.BtnFinish_Click);
            // 
            // BtnCancel
            // 
            this.BtnCancel.Location = new System.Drawing.Point(471, 447);
            this.BtnCancel.Name = "BtnCancel";
            this.BtnCancel.Size = new System.Drawing.Size(75, 23);
            this.BtnCancel.TabIndex = 9;
            this.BtnCancel.Text = "Cancel";
            this.BtnCancel.UseVisualStyleBackColor = true;
            this.BtnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // Sync
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 482);
            this.ControlBox = false;
            this.Controls.Add(this.BtnCancel);
            this.Controls.Add(this.BtnFinish);
            this.Controls.Add(this.LblEstimatedTimeRemaining);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.ProgressSync);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pcbSync);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(700, 498);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(700, 498);
            this.Name = "Sync";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)(this.pcbSync)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pcbSync;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ProgressBar ProgressSync;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label LblEstimatedTimeRemaining;
        private System.Windows.Forms.Button BtnFinish;
        private System.Windows.Forms.Button BtnCancel;
    }
}