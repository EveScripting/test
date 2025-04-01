namespace EVESharpCore.Controllers
{
    partial class UINavigateOnGridControllerForm
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
            this.btnStopAllActions = new System.Windows.Forms.Button();
            this.btnTravelToBookmark = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.btnRefreshEntitiesOngrid = new System.Windows.Forms.Button();
            this.btnTravelToSetWaypoint = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            //
            // panel1
            //
            this.panel1.AutoScroll = true;
            this.panel1.BackColor = System.Drawing.Color.Blue;
            this.panel1.Controls.Add(this.btnTravelToSetWaypoint);
            this.panel1.Controls.Add(this.btnStopAllActions);
            this.panel1.Controls.Add(this.btnTravelToBookmark);
            this.panel1.Controls.Add(this.comboBox1);
            this.panel1.Controls.Add(this.btnRefreshEntitiesOngrid);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(650, 253);
            this.panel1.TabIndex = 6;
            //
            // button5
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
            // button4
            //
            this.btnTravelToBookmark.BackColor = System.Drawing.Color.White;
            this.btnTravelToBookmark.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnTravelToBookmark.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnTravelToBookmark.Location = new System.Drawing.Point(221, 128);
            this.btnTravelToBookmark.Name = "btnTravelToBookmark";
            this.btnTravelToBookmark.Size = new System.Drawing.Size(180, 23);
            this.btnTravelToBookmark.TabIndex = 6;
            this.btnTravelToBookmark.Text = "Travel to bookmark";
            this.btnTravelToBookmark.UseVisualStyleBackColor = false;
            this.btnTravelToBookmark.Click += new System.EventHandler(this.BtnTravelToBookmark_Click);
            //
            // comboBox1
            //
            this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBox1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(47, 72);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(556, 21);
            this.comboBox1.TabIndex = 5;
            //
            // button1
            //
            this.btnRefreshEntitiesOngrid.BackColor = System.Drawing.Color.White;
            this.btnRefreshEntitiesOngrid.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnRefreshEntitiesOngrid.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRefreshEntitiesOngrid.Location = new System.Drawing.Point(221, 99);
            this.btnRefreshEntitiesOngrid.Name = "btnRefreshBookmarks";
            this.btnRefreshEntitiesOngrid.Size = new System.Drawing.Size(180, 23);
            this.btnRefreshEntitiesOngrid.TabIndex = 4;
            this.btnRefreshEntitiesOngrid.Text = "Refresh bookmarks";
            this.btnRefreshEntitiesOngrid.UseVisualStyleBackColor = false;
            this.btnRefreshEntitiesOngrid.Click += new System.EventHandler(this.BtnRefreshEntitiesOngrid_Click);
            //
            // button2
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
        private System.Windows.Forms.Button btnTravelToBookmark;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button btnRefreshEntitiesOngrid;
        private System.Windows.Forms.Button btnTravelToSetWaypoint;
    }
}