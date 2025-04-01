namespace EVESharpCore.Controllers
{
    partial class ProbeScanControllerForm
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
            this.components = new System.ComponentModel.Container();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.panel1 = new System.Windows.Forms.Panel();
            this.DebugCreateNewFleet = new System.Windows.Forms.Button();
            this.buttonStopMyShip = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceWest = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceEast = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceSouth = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceNorth = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceDown = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceUp = new System.Windows.Forms.Button();
            this.DebugAssetsButton = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.BackColor = System.Drawing.Color.Blue;
            this.panel1.Controls.Add(this.DebugCreateNewFleet);
            this.panel1.Controls.Add(this.buttonStopMyShip);
            this.panel1.Controls.Add(this.buttonApproachPointInSpaceWest);
            this.panel1.Controls.Add(this.buttonApproachPointInSpaceEast);
            this.panel1.Controls.Add(this.buttonApproachPointInSpaceSouth);
            this.panel1.Controls.Add(this.buttonApproachPointInSpaceNorth);
            this.panel1.Controls.Add(this.buttonApproachPointInSpaceDown);
            this.panel1.Controls.Add(this.buttonApproachPointInSpaceUp);
            this.panel1.Controls.Add(this.DebugAssetsButton);
            this.panel1.Controls.Add(this.button7);
            this.panel1.Controls.Add(this.button6);
            this.panel1.Controls.Add(this.button5);
            this.panel1.Controls.Add(this.button3);
            this.panel1.Controls.Add(this.button2);
            this.panel1.Controls.Add(this.button4);
            this.panel1.Controls.Add(this.button1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(650, 253);
            this.panel1.TabIndex = 6;
            
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.White;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.Location = new System.Drawing.Point(12, 104);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(123, 21);
            this.button3.TabIndex = 149;
            this.button3.Text = "Debug Scan";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.Button3_Click);
            // 
            // DebugControllerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, 253);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DebugControllerForm";
            this.Text = "Debug";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button DebugAssetsButton;
        private System.Windows.Forms.Button buttonApproachPointInSpaceUp;
        private System.Windows.Forms.Button buttonStopMyShip;
        private System.Windows.Forms.Button buttonApproachPointInSpaceWest;
        private System.Windows.Forms.Button buttonApproachPointInSpaceEast;
        private System.Windows.Forms.Button buttonApproachPointInSpaceSouth;
        private System.Windows.Forms.Button buttonApproachPointInSpaceNorth;
        private System.Windows.Forms.Button buttonApproachPointInSpaceDown;
        private System.Windows.Forms.Button DebugCreateNewFleet;
    }
}