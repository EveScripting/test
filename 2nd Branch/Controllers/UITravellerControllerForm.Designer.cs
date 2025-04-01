namespace EVESharpCore.Controllers
{
    partial class UITravellerControllerForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnTravelToSetWaypoint = new System.Windows.Forms.Button();
            this.btnStopAllActions = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.BackColor = System.Drawing.Color.Blue;
            this.panel1.Controls.Add(this.btnTravelToSetWaypoint);
            this.panel1.Controls.Add(this.btnStopAllActions);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(650, 253);
            this.panel1.TabIndex = 6;
            // 
            // btnTravelToSetWaypoint
            // 
            this.btnTravelToSetWaypoint.BackColor = System.Drawing.Color.White;
            this.btnTravelToSetWaypoint.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnTravelToSetWaypoint.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTravelToSetWaypoint.Location = new System.Drawing.Point(221, 157);
            this.btnTravelToSetWaypoint.Name = "btnTravelToSetWaypoint";
            this.btnTravelToSetWaypoint.Size = new System.Drawing.Size(180, 23);
            this.btnTravelToSetWaypoint.TabIndex = 8;
            this.btnTravelToSetWaypoint.Text = "Travel to set waypoint";
            this.btnTravelToSetWaypoint.UseVisualStyleBackColor = false;
            this.btnTravelToSetWaypoint.Click += new System.EventHandler(this.BtnTravelToSetWaypoint_Click);
            // 
            // btnStopAllActions
            // 
            this.btnStopAllActions.BackColor = System.Drawing.Color.White;
            this.btnStopAllActions.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnStopAllActions.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStopAllActions.Location = new System.Drawing.Point(221, 186);
            this.btnStopAllActions.Name = "btnStopAllActions";
            this.btnStopAllActions.Size = new System.Drawing.Size(180, 23);
            this.btnStopAllActions.TabIndex = 7;
            this.btnStopAllActions.Text = "Stop all actions";
            this.btnStopAllActions.UseVisualStyleBackColor = false;
            this.btnStopAllActions.Click += new System.EventHandler(this.BtnStopAllActions_Click);
            // 
            // UITravellerControllerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, 253);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UITravellerControllerForm";
            this.Text = "Traveller";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btnStopAllActions;
        private System.Windows.Forms.Button btnTravelToSetWaypoint;
    }
}