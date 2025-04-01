namespace EVESharpCore.Controllers
{
    partial class DebugControllerForm
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.General = new System.Windows.Forms.TabPage();
            this.DebugAssetsButton = new System.Windows.Forms.Button();
            this.button7 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.Movement = new System.Windows.Forms.TabPage();
            this.buttonStopMyShip = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceWest = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceEast = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceSouth = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceNorth = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceDown = new System.Windows.Forms.Button();
            this.buttonApproachPointInSpaceUp = new System.Windows.Forms.Button();
            this.Fleet = new System.Windows.Forms.TabPage();
            this.DebugCreateNewFleet = new System.Windows.Forms.Button();
            this.Windows = new System.Windows.Forms.TabPage();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.CharactersInLocal = new System.Windows.Forms.TabPage();
            this.CharChannels = new System.Windows.Forms.TabPage();
            this.buttonCloseEve = new System.Windows.Forms.Button();
            this.button8 = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.General.SuspendLayout();
            this.Movement.SuspendLayout();
            this.Fleet.SuspendLayout();
            this.Windows.SuspendLayout();
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
            this.panel1.Controls.Add(this.tabControl1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(650, 253);
            this.panel1.TabIndex = 6;
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.General);
            this.tabControl1.Controls.Add(this.Movement);
            this.tabControl1.Controls.Add(this.Fleet);
            this.tabControl1.Controls.Add(this.Windows);
            this.tabControl1.Controls.Add(this.CharactersInLocal);
            this.tabControl1.Controls.Add(this.CharChannels);
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(647, 253);
            this.tabControl1.TabIndex = 162;
            // 
            // General
            // 
            this.General.Controls.Add(this.button8);
            this.General.Controls.Add(this.buttonCloseEve);
            this.General.Controls.Add(this.DebugAssetsButton);
            this.General.Controls.Add(this.button7);
            this.General.Controls.Add(this.button6);
            this.General.Controls.Add(this.button5);
            this.General.Controls.Add(this.button3);
            this.General.Controls.Add(this.button2);
            this.General.Controls.Add(this.button4);
            this.General.Controls.Add(this.button1);
            this.General.Location = new System.Drawing.Point(4, 22);
            this.General.Name = "General";
            this.General.Padding = new System.Windows.Forms.Padding(3);
            this.General.Size = new System.Drawing.Size(639, 227);
            this.General.TabIndex = 0;
            this.General.Text = "General";
            this.General.UseVisualStyleBackColor = true;
            // 
            // DebugAssetsButton
            // 
            this.DebugAssetsButton.BackColor = System.Drawing.Color.White;
            this.DebugAssetsButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.DebugAssetsButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DebugAssetsButton.Location = new System.Drawing.Point(13, 197);
            this.DebugAssetsButton.Name = "DebugAssetsButton";
            this.DebugAssetsButton.Size = new System.Drawing.Size(123, 21);
            this.DebugAssetsButton.TabIndex = 161;
            this.DebugAssetsButton.Text = "Debug Assets";
            this.DebugAssetsButton.UseVisualStyleBackColor = false;
            this.DebugAssetsButton.Click += new System.EventHandler(this.DebugAssetsButton_Click);
            // 
            // button7
            // 
            this.button7.BackColor = System.Drawing.Color.White;
            this.button7.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button7.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button7.Location = new System.Drawing.Point(13, 170);
            this.button7.Name = "button7";
            this.button7.Size = new System.Drawing.Size(123, 21);
            this.button7.TabIndex = 160;
            this.button7.Text = "Debug Windows";
            this.button7.UseVisualStyleBackColor = false;
            this.button7.Click += new System.EventHandler(this.Button7_Click);
            // 
            // button6
            // 
            this.button6.BackColor = System.Drawing.Color.White;
            this.button6.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button6.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button6.Location = new System.Drawing.Point(13, 143);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(123, 21);
            this.button6.TabIndex = 159;
            this.button6.Text = "Debug Map";
            this.button6.UseVisualStyleBackColor = false;
            this.button6.Click += new System.EventHandler(this.Button6_Click);
            // 
            // button5
            // 
            this.button5.BackColor = System.Drawing.Color.White;
            this.button5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button5.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button5.Location = new System.Drawing.Point(13, 116);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(123, 21);
            this.button5.TabIndex = 158;
            this.button5.Text = "Debug Channels";
            this.button5.UseVisualStyleBackColor = false;
            this.button5.Click += new System.EventHandler(this.Button5_Click);
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.White;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button3.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.Location = new System.Drawing.Point(13, 89);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(123, 21);
            this.button3.TabIndex = 157;
            this.button3.Text = "Debug Scan";
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.Button3_Click);
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.White;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.Location = new System.Drawing.Point(13, 62);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(123, 21);
            this.button2.TabIndex = 156;
            this.button2.Text = "Debug Skills";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.Button2_Click);
            // 
            // button4
            // 
            this.button4.BackColor = System.Drawing.Color.White;
            this.button4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button4.Location = new System.Drawing.Point(13, 35);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(123, 21);
            this.button4.TabIndex = 155;
            this.button4.Text = "Debug Modules";
            this.button4.UseVisualStyleBackColor = false;
            this.button4.Click += new System.EventHandler(this.Button4_Click);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.White;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(13, 8);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(123, 21);
            this.button1.TabIndex = 154;
            this.button1.Text = "Debug Entities";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // Movement
            // 
            this.Movement.Controls.Add(this.buttonStopMyShip);
            this.Movement.Controls.Add(this.buttonApproachPointInSpaceWest);
            this.Movement.Controls.Add(this.buttonApproachPointInSpaceEast);
            this.Movement.Controls.Add(this.buttonApproachPointInSpaceSouth);
            this.Movement.Controls.Add(this.buttonApproachPointInSpaceNorth);
            this.Movement.Controls.Add(this.buttonApproachPointInSpaceDown);
            this.Movement.Controls.Add(this.buttonApproachPointInSpaceUp);
            this.Movement.Location = new System.Drawing.Point(4, 22);
            this.Movement.Name = "Movement";
            this.Movement.Padding = new System.Windows.Forms.Padding(3);
            this.Movement.Size = new System.Drawing.Size(639, 227);
            this.Movement.TabIndex = 1;
            this.Movement.Text = "Movement";
            this.Movement.UseVisualStyleBackColor = true;
            // 
            // buttonStopMyShip
            // 
            this.buttonStopMyShip.BackColor = System.Drawing.Color.White;
            this.buttonStopMyShip.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonStopMyShip.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStopMyShip.Location = new System.Drawing.Point(-4, 6);
            this.buttonStopMyShip.Name = "buttonStopMyShip";
            this.buttonStopMyShip.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.buttonStopMyShip.Size = new System.Drawing.Size(242, 21);
            this.buttonStopMyShip.TabIndex = 167;
            this.buttonStopMyShip.Text = "Debug StopMyShip()";
            this.buttonStopMyShip.UseVisualStyleBackColor = false;
            // 
            // buttonApproachPointInSpaceWest
            // 
            this.buttonApproachPointInSpaceWest.BackColor = System.Drawing.Color.White;
            this.buttonApproachPointInSpaceWest.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonApproachPointInSpaceWest.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonApproachPointInSpaceWest.Location = new System.Drawing.Point(240, 136);
            this.buttonApproachPointInSpaceWest.Name = "buttonApproachPointInSpaceWest";
            this.buttonApproachPointInSpaceWest.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.buttonApproachPointInSpaceWest.Size = new System.Drawing.Size(242, 21);
            this.buttonApproachPointInSpaceWest.TabIndex = 166;
            this.buttonApproachPointInSpaceWest.Text = "Debug Approach Point In Space - West";
            this.buttonApproachPointInSpaceWest.UseVisualStyleBackColor = false;
            // 
            // buttonApproachPointInSpaceEast
            // 
            this.buttonApproachPointInSpaceEast.BackColor = System.Drawing.Color.White;
            this.buttonApproachPointInSpaceEast.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonApproachPointInSpaceEast.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonApproachPointInSpaceEast.Location = new System.Drawing.Point(240, 109);
            this.buttonApproachPointInSpaceEast.Name = "buttonApproachPointInSpaceEast";
            this.buttonApproachPointInSpaceEast.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.buttonApproachPointInSpaceEast.Size = new System.Drawing.Size(242, 21);
            this.buttonApproachPointInSpaceEast.TabIndex = 165;
            this.buttonApproachPointInSpaceEast.Text = "Debug Approach Point In Space - East";
            this.buttonApproachPointInSpaceEast.UseVisualStyleBackColor = false;
            // 
            // buttonApproachPointInSpaceSouth
            // 
            this.buttonApproachPointInSpaceSouth.BackColor = System.Drawing.Color.White;
            this.buttonApproachPointInSpaceSouth.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonApproachPointInSpaceSouth.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonApproachPointInSpaceSouth.Location = new System.Drawing.Point(240, 82);
            this.buttonApproachPointInSpaceSouth.Name = "buttonApproachPointInSpaceSouth";
            this.buttonApproachPointInSpaceSouth.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.buttonApproachPointInSpaceSouth.Size = new System.Drawing.Size(242, 21);
            this.buttonApproachPointInSpaceSouth.TabIndex = 164;
            this.buttonApproachPointInSpaceSouth.Text = "Debug Approach Point In Space - South";
            this.buttonApproachPointInSpaceSouth.UseVisualStyleBackColor = false;
            // 
            // buttonApproachPointInSpaceNorth
            // 
            this.buttonApproachPointInSpaceNorth.BackColor = System.Drawing.Color.White;
            this.buttonApproachPointInSpaceNorth.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonApproachPointInSpaceNorth.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonApproachPointInSpaceNorth.Location = new System.Drawing.Point(240, 55);
            this.buttonApproachPointInSpaceNorth.Name = "buttonApproachPointInSpaceNorth";
            this.buttonApproachPointInSpaceNorth.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.buttonApproachPointInSpaceNorth.Size = new System.Drawing.Size(242, 21);
            this.buttonApproachPointInSpaceNorth.TabIndex = 163;
            this.buttonApproachPointInSpaceNorth.Text = "Debug Approach Point In Space - North";
            this.buttonApproachPointInSpaceNorth.UseVisualStyleBackColor = false;
            // 
            // buttonApproachPointInSpaceDown
            // 
            this.buttonApproachPointInSpaceDown.BackColor = System.Drawing.Color.White;
            this.buttonApproachPointInSpaceDown.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonApproachPointInSpaceDown.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonApproachPointInSpaceDown.Location = new System.Drawing.Point(240, 28);
            this.buttonApproachPointInSpaceDown.Name = "buttonApproachPointInSpaceDown";
            this.buttonApproachPointInSpaceDown.Size = new System.Drawing.Size(242, 21);
            this.buttonApproachPointInSpaceDown.TabIndex = 162;
            this.buttonApproachPointInSpaceDown.Text = "Debug Approach Point In Space - Down";
            this.buttonApproachPointInSpaceDown.UseVisualStyleBackColor = false;
            // 
            // buttonApproachPointInSpaceUp
            // 
            this.buttonApproachPointInSpaceUp.BackColor = System.Drawing.Color.White;
            this.buttonApproachPointInSpaceUp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonApproachPointInSpaceUp.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonApproachPointInSpaceUp.Location = new System.Drawing.Point(240, 3);
            this.buttonApproachPointInSpaceUp.Name = "buttonApproachPointInSpaceUp";
            this.buttonApproachPointInSpaceUp.Size = new System.Drawing.Size(242, 21);
            this.buttonApproachPointInSpaceUp.TabIndex = 155;
            this.buttonApproachPointInSpaceUp.Text = "Debug Approach Point In Space - Up";
            this.buttonApproachPointInSpaceUp.UseVisualStyleBackColor = false;
            // 
            // Fleet
            // 
            this.Fleet.Controls.Add(this.DebugCreateNewFleet);
            this.Fleet.Location = new System.Drawing.Point(4, 22);
            this.Fleet.Name = "Fleet";
            this.Fleet.Padding = new System.Windows.Forms.Padding(3);
            this.Fleet.Size = new System.Drawing.Size(639, 227);
            this.Fleet.TabIndex = 2;
            this.Fleet.Text = "Fleet";
            this.Fleet.UseVisualStyleBackColor = true;
            // 
            // DebugCreateNewFleet
            // 
            this.DebugCreateNewFleet.BackColor = System.Drawing.Color.White;
            this.DebugCreateNewFleet.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.DebugCreateNewFleet.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DebugCreateNewFleet.Location = new System.Drawing.Point(6, 6);
            this.DebugCreateNewFleet.Name = "DebugCreateNewFleet";
            this.DebugCreateNewFleet.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.DebugCreateNewFleet.Size = new System.Drawing.Size(242, 21);
            this.DebugCreateNewFleet.TabIndex = 169;
            this.DebugCreateNewFleet.Text = "CreateNewFleet";
            this.DebugCreateNewFleet.UseVisualStyleBackColor = false;
            // 
            // Windows
            // 
            this.Windows.Controls.Add(this.comboBox1);
            this.Windows.Location = new System.Drawing.Point(4, 22);
            this.Windows.Name = "Windows";
            this.Windows.Size = new System.Drawing.Size(639, 227);
            this.Windows.TabIndex = 3;
            this.Windows.Text = "Windows";
            this.Windows.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(3, 12);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(216, 21);
            this.comboBox1.TabIndex = 0;
            // 
            // CharactersInLocal
            // 
            this.CharactersInLocal.Location = new System.Drawing.Point(4, 22);
            this.CharactersInLocal.Name = "CharactersInLocal";
            this.CharactersInLocal.Size = new System.Drawing.Size(639, 227);
            this.CharactersInLocal.TabIndex = 4;
            this.CharactersInLocal.Text = "CharactersInLocal";
            this.CharactersInLocal.UseVisualStyleBackColor = true;
            // 
            // CharChannels
            // 
            this.CharChannels.Location = new System.Drawing.Point(4, 22);
            this.CharChannels.Name = "CharChannels";
            this.CharChannels.Size = new System.Drawing.Size(639, 227);
            this.CharChannels.TabIndex = 5;
            this.CharChannels.Text = "ChatChennels";
            this.CharChannels.UseVisualStyleBackColor = true;
            // 
            // buttonCloseEve
            // 
            this.buttonCloseEve.BackColor = System.Drawing.Color.White;
            this.buttonCloseEve.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCloseEve.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonCloseEve.Location = new System.Drawing.Point(142, 8);
            this.buttonCloseEve.Name = "buttonCloseEve";
            this.buttonCloseEve.Size = new System.Drawing.Size(123, 21);
            this.buttonCloseEve.TabIndex = 162;
            this.buttonCloseEve.Text = "Debug CloseEVE";
            this.buttonCloseEve.UseVisualStyleBackColor = false;
            this.buttonCloseEve.Click += new System.EventHandler(this.ButtonCloseEVE_Click);
            // 
            // button8
            // 
            this.button8.BackColor = System.Drawing.Color.White;
            this.button8.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.button8.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button8.Location = new System.Drawing.Point(142, 35);
            this.button8.Name = "button8";
            this.button8.Size = new System.Drawing.Size(123, 21);
            this.button8.TabIndex = 164;
            this.button8.Text = "Debug RestartEVE";
            this.button8.UseVisualStyleBackColor = false;
            this.button8.Click += new System.EventHandler(this.button8_Click);
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
            this.tabControl1.ResumeLayout(false);
            this.General.ResumeLayout(false);
            this.Movement.ResumeLayout(false);
            this.Fleet.ResumeLayout(false);
            this.Windows.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage General;
        private System.Windows.Forms.TabPage Movement;
        private System.Windows.Forms.Button buttonStopMyShip;
        private System.Windows.Forms.Button buttonApproachPointInSpaceWest;
        private System.Windows.Forms.Button buttonApproachPointInSpaceEast;
        private System.Windows.Forms.Button buttonApproachPointInSpaceSouth;
        private System.Windows.Forms.Button buttonApproachPointInSpaceNorth;
        private System.Windows.Forms.Button buttonApproachPointInSpaceDown;
        private System.Windows.Forms.Button buttonApproachPointInSpaceUp;
        private System.Windows.Forms.TabPage Fleet;
        private System.Windows.Forms.Button DebugCreateNewFleet;
        private System.Windows.Forms.TabPage Windows;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Button DebugAssetsButton;
        private System.Windows.Forms.Button button7;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabPage CharactersInLocal;
        private System.Windows.Forms.TabPage CharChannels;
        private System.Windows.Forms.Button buttonCloseEve;
        private System.Windows.Forms.Button button8;
    }
}