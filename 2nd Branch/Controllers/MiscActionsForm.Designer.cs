namespace EVESharpCore.Controllers
{
    partial class MiscActionsForm
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
            this.buttonApproachPointInSpaceUp = new System.Windows.Forms.Button();
            this.groupBoxMiscActions1 = new System.Windows.Forms.GroupBox();
            this.chkboxSalvagerAllowStealingOfLoot = new System.Windows.Forms.CheckBox();
            this.panel1.SuspendLayout();
            this.groupBoxMiscActions1.SuspendLayout();
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
            this.panel1.Controls.Add(this.groupBoxMiscActions1);
            this.panel1.Controls.Add(this.buttonApproachPointInSpaceUp);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(650, 253);
            this.panel1.TabIndex = 6;
            // 
            // buttonApproachPointInSpaceUp
            // 
            this.buttonApproachPointInSpaceUp.BackColor = System.Drawing.Color.White;
            this.buttonApproachPointInSpaceUp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonApproachPointInSpaceUp.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonApproachPointInSpaceUp.Location = new System.Drawing.Point(396, 23);
            this.buttonApproachPointInSpaceUp.Name = "buttonApproachPointInSpaceUp";
            this.buttonApproachPointInSpaceUp.Size = new System.Drawing.Size(242, 21);
            this.buttonApproachPointInSpaceUp.TabIndex = 154;
            this.buttonApproachPointInSpaceUp.Text = "Debug Approach Point In Space - Up";
            this.buttonApproachPointInSpaceUp.UseVisualStyleBackColor = false;
            this.buttonApproachPointInSpaceUp.Click += new System.EventHandler(this.ButtonApproachPointInSpaceUp_Click);
            // 
            // groupBoxMiscActions1
            // 
            this.groupBoxMiscActions1.BackColor = System.Drawing.Color.DimGray;
            this.groupBoxMiscActions1.Controls.Add(this.chkboxSalvagerAllowStealingOfLoot);
            this.groupBoxMiscActions1.Location = new System.Drawing.Point(12, 23);
            this.groupBoxMiscActions1.Name = "groupBoxMiscActions1";
            this.groupBoxMiscActions1.Size = new System.Drawing.Size(229, 78);
            this.groupBoxMiscActions1.TabIndex = 156;
            this.groupBoxMiscActions1.TabStop = false;
            this.groupBoxMiscActions1.Text = "Salvager Actions";
            // 
            // chkboxSalvagerAllowStealingOfLoot
            // 
            this.chkboxSalvagerAllowStealingOfLoot.AutoSize = true;
            this.chkboxSalvagerAllowStealingOfLoot.Location = new System.Drawing.Point(6, 19);
            this.chkboxSalvagerAllowStealingOfLoot.Name = "chkboxSalvagerAllowStealingOfLoot";
            this.chkboxSalvagerAllowStealingOfLoot.Size = new System.Drawing.Size(196, 17);
            this.chkboxSalvagerAllowStealingOfLoot.TabIndex = 156;
            this.chkboxSalvagerAllowStealingOfLoot.Text = "Salvager: Allow Stealing: SUSPECT";
            this.chkboxSalvagerAllowStealingOfLoot.UseVisualStyleBackColor = true;
            this.chkboxSalvagerAllowStealingOfLoot.CheckedChanged += new System.EventHandler(this.ChkboxSalvagerAllowStealingOfLoot_CheckedChanged);
            this.chkboxSalvagerAllowStealingOfLoot.Click += new System.EventHandler(this.ChkboxSalvagerAllowStealingOfLoot_Click);
            // 
            // MiscActionsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(650, 253);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MiscActionsForm";
            this.Text = "Debug";
            this.panel1.ResumeLayout(false);
            this.groupBoxMiscActions1.ResumeLayout(false);
            this.groupBoxMiscActions1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button buttonApproachPointInSpaceUp;
        private System.Windows.Forms.GroupBox groupBoxMiscActions1;
        private System.Windows.Forms.CheckBox chkboxSalvagerAllowStealingOfLoot;
    }
}