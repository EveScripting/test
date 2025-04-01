namespace SharedComponents.Py.PythonBrowser
{
    partial class PythonBrowserFrm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PythonBrowserFrm));
            this.TypesButton = new System.Windows.Forms.RadioButton();
            this.btn_windows = new System.Windows.Forms.Button();
            this.btn_builtin = new System.Windows.Forms.Button();
            this.btn_session = new System.Windows.Forms.Button();
            this.btn_activeship = new System.Windows.Forms.Button();
            this.btn_getservice = new System.Windows.Forms.Button();
            this.btn_const = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button4 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.DictionaryButton = new System.Windows.Forms.RadioButton();
            this.ClassButton = new System.Windows.Forms.RadioButton();
            this.ListButton = new System.Windows.Forms.RadioButton();
            this.TupleButton = new System.Windows.Forms.RadioButton();
            this.ValueButton = new System.Windows.Forms.RadioButton();
            this.AutoButton = new System.Windows.Forms.RadioButton();
            this.NameHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ValueHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.TypeHeader = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.AttributesList = new System.Windows.Forms.ListView();
            this.EvaluateButton = new System.Windows.Forms.Button();
            this.EvaluateBox = new System.Windows.Forms.ComboBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label1 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.button1 = new System.Windows.Forms.Button();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.groupBox1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // TypesButton
            // 
            this.TypesButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.TypesButton.AutoSize = true;
            this.TypesButton.Location = new System.Drawing.Point(329, 47);
            this.TypesButton.Name = "TypesButton";
            this.TypesButton.Size = new System.Drawing.Size(46, 23);
            this.TypesButton.TabIndex = 17;
            this.TypesButton.Text = "Types";
            this.TypesButton.UseVisualStyleBackColor = true;
            this.TypesButton.Click += new System.EventHandler(this.RadioButton_Click);
            // 
            // btn_windows
            // 
            this.btn_windows.Location = new System.Drawing.Point(134, 19);
            this.btn_windows.Name = "btn_windows";
            this.btn_windows.Size = new System.Drawing.Size(64, 23);
            this.btn_windows.TabIndex = 0;
            this.btn_windows.Text = "Windows";
            this.btn_windows.UseVisualStyleBackColor = true;
            this.btn_windows.Click += new System.EventHandler(this.btn_windows_Click);
            // 
            // btn_builtin
            // 
            this.btn_builtin.Location = new System.Drawing.Point(6, 19);
            this.btn_builtin.Name = "btn_builtin";
            this.btn_builtin.Size = new System.Drawing.Size(52, 23);
            this.btn_builtin.TabIndex = 1;
            this.btn_builtin.Text = "Builtin";
            this.btn_builtin.UseVisualStyleBackColor = true;
            this.btn_builtin.Click += new System.EventHandler(this.btn_builtin_Click);
            // 
            // btn_session
            // 
            this.btn_session.Location = new System.Drawing.Point(204, 19);
            this.btn_session.Name = "btn_session";
            this.btn_session.Size = new System.Drawing.Size(64, 23);
            this.btn_session.TabIndex = 2;
            this.btn_session.Text = "Session";
            this.btn_session.UseVisualStyleBackColor = true;
            this.btn_session.Click += new System.EventHandler(this.btn_session_Click);
            // 
            // btn_activeship
            // 
            this.btn_activeship.Location = new System.Drawing.Point(274, 19);
            this.btn_activeship.Name = "btn_activeship";
            this.btn_activeship.Size = new System.Drawing.Size(75, 23);
            this.btn_activeship.TabIndex = 3;
            this.btn_activeship.Text = "ActiveShip";
            this.btn_activeship.UseVisualStyleBackColor = true;
            this.btn_activeship.Click += new System.EventHandler(this.btn_activeship_Click);
            // 
            // btn_getservice
            // 
            this.btn_getservice.Location = new System.Drawing.Point(64, 19);
            this.btn_getservice.Name = "btn_getservice";
            this.btn_getservice.Size = new System.Drawing.Size(64, 23);
            this.btn_getservice.TabIndex = 4;
            this.btn_getservice.Text = "Services";
            this.btn_getservice.UseVisualStyleBackColor = true;
            this.btn_getservice.Click += new System.EventHandler(this.btn_getservice_Click);
            // 
            // btn_const
            // 
            this.btn_const.Location = new System.Drawing.Point(355, 19);
            this.btn_const.Name = "btn_const";
            this.btn_const.Size = new System.Drawing.Size(64, 23);
            this.btn_const.TabIndex = 5;
            this.btn_const.Text = "Const";
            this.btn_const.UseVisualStyleBackColor = true;
            this.btn_const.Click += new System.EventHandler(this.btn_const_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.button4);
            this.groupBox1.Controls.Add(this.button3);
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.btn_const);
            this.groupBox1.Controls.Add(this.btn_getservice);
            this.groupBox1.Controls.Add(this.btn_activeship);
            this.groupBox1.Controls.Add(this.btn_session);
            this.groupBox1.Controls.Add(this.btn_builtin);
            this.groupBox1.Controls.Add(this.btn_windows);
            this.groupBox1.Location = new System.Drawing.Point(27, 76);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(662, 49);
            this.groupBox1.TabIndex = 16;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Shortcut";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(565, 19);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(91, 23);
            this.button4.TabIndex = 8;
            this.button4.Text = "Scene Objects";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(495, 19);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(64, 23);
            this.button3.TabIndex = 7;
            this.button3.Text = "Balls";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(425, 19);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(64, 23);
            this.button2.TabIndex = 6;
            this.button2.Text = "Modules";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // DictionaryButton
            // 
            this.DictionaryButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.DictionaryButton.AutoSize = true;
            this.DictionaryButton.Location = new System.Drawing.Point(259, 47);
            this.DictionaryButton.Name = "DictionaryButton";
            this.DictionaryButton.Size = new System.Drawing.Size(64, 23);
            this.DictionaryButton.TabIndex = 14;
            this.DictionaryButton.Text = "Dictionary";
            this.DictionaryButton.UseVisualStyleBackColor = true;
            this.DictionaryButton.Click += new System.EventHandler(this.RadioButton_Click);
            // 
            // ClassButton
            // 
            this.ClassButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.ClassButton.AutoSize = true;
            this.ClassButton.Location = new System.Drawing.Point(122, 47);
            this.ClassButton.Name = "ClassButton";
            this.ClassButton.Size = new System.Drawing.Size(42, 23);
            this.ClassButton.TabIndex = 13;
            this.ClassButton.Text = "Class";
            this.ClassButton.UseVisualStyleBackColor = true;
            this.ClassButton.Click += new System.EventHandler(this.RadioButton_Click);
            // 
            // ListButton
            // 
            this.ListButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.ListButton.AutoSize = true;
            this.ListButton.Location = new System.Drawing.Point(170, 47);
            this.ListButton.Name = "ListButton";
            this.ListButton.Size = new System.Drawing.Size(33, 23);
            this.ListButton.TabIndex = 12;
            this.ListButton.Text = "List";
            this.ListButton.UseVisualStyleBackColor = true;
            this.ListButton.Click += new System.EventHandler(this.RadioButton_Click);
            // 
            // TupleButton
            // 
            this.TupleButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.TupleButton.AutoSize = true;
            this.TupleButton.Location = new System.Drawing.Point(209, 47);
            this.TupleButton.Name = "TupleButton";
            this.TupleButton.Size = new System.Drawing.Size(44, 23);
            this.TupleButton.TabIndex = 11;
            this.TupleButton.Text = "Tuple";
            this.TupleButton.UseVisualStyleBackColor = true;
            this.TupleButton.Click += new System.EventHandler(this.RadioButton_Click);
            // 
            // ValueButton
            // 
            this.ValueButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.ValueButton.AutoSize = true;
            this.ValueButton.Location = new System.Drawing.Point(72, 47);
            this.ValueButton.Name = "ValueButton";
            this.ValueButton.Size = new System.Drawing.Size(44, 23);
            this.ValueButton.TabIndex = 10;
            this.ValueButton.Text = "Value";
            this.ValueButton.UseVisualStyleBackColor = true;
            this.ValueButton.Click += new System.EventHandler(this.RadioButton_Click);
            // 
            // AutoButton
            // 
            this.AutoButton.Appearance = System.Windows.Forms.Appearance.Button;
            this.AutoButton.AutoSize = true;
            this.AutoButton.Checked = true;
            this.AutoButton.Location = new System.Drawing.Point(27, 47);
            this.AutoButton.Name = "AutoButton";
            this.AutoButton.Size = new System.Drawing.Size(39, 23);
            this.AutoButton.TabIndex = 9;
            this.AutoButton.TabStop = true;
            this.AutoButton.Text = "Auto";
            this.AutoButton.UseVisualStyleBackColor = true;
            this.AutoButton.Click += new System.EventHandler(this.RadioButton_Click);
            // 
            // NameHeader
            // 
            this.NameHeader.Text = "Name";
            this.NameHeader.Width = 227;
            // 
            // ValueHeader
            // 
            this.ValueHeader.Text = "Value";
            this.ValueHeader.Width = 206;
            // 
            // TypeHeader
            // 
            this.TypeHeader.Text = "Type";
            this.TypeHeader.Width = 122;
            // 
            // AttributesList
            // 
            this.AttributesList.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.AttributesList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.NameHeader,
            this.ValueHeader,
            this.TypeHeader});
            this.AttributesList.FullRowSelect = true;
            this.AttributesList.HideSelection = false;
            this.AttributesList.Location = new System.Drawing.Point(27, 176);
            this.AttributesList.Name = "AttributesList";
            this.AttributesList.Size = new System.Drawing.Size(780, 322);
            this.AttributesList.TabIndex = 3;
            this.AttributesList.UseCompatibleStateImageBehavior = false;
            this.AttributesList.View = System.Windows.Forms.View.Details;
            this.AttributesList.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.AttributesList_ColumnClick);
            this.AttributesList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.AttributesList_MouseDoubleClick);
            // 
            // EvaluateButton
            // 
            this.EvaluateButton.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.EvaluateButton.Location = new System.Drawing.Point(738, 6);
            this.EvaluateButton.Name = "EvaluateButton";
            this.EvaluateButton.Size = new System.Drawing.Size(75, 23);
            this.EvaluateButton.TabIndex = 2;
            this.EvaluateButton.Text = "Evaluate";
            this.EvaluateButton.UseVisualStyleBackColor = true;
            this.EvaluateButton.Click += new System.EventHandler(this.EvaluateButton_Click);
            // 
            // EvaluateBox
            // 
            this.EvaluateBox.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.EvaluateBox.FormattingEnabled = true;
            this.EvaluateBox.Location = new System.Drawing.Point(20, 6);
            this.EvaluateBox.Name = "EvaluateBox";
            this.EvaluateBox.Size = new System.Drawing.Size(705, 21);
            this.EvaluateBox.TabIndex = 1;
            this.EvaluateBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.EvaluateBox_KeyPress);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(833, 551);
            this.tabControl1.TabIndex = 18;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.textBox1);
            this.tabPage1.Controls.Add(this.AutoButton);
            this.tabPage1.Controls.Add(this.TypesButton);
            this.tabPage1.Controls.Add(this.EvaluateBox);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.EvaluateButton);
            this.tabPage1.Controls.Add(this.DictionaryButton);
            this.tabPage1.Controls.Add(this.AttributesList);
            this.tabPage1.Controls.Add(this.ClassButton);
            this.tabPage1.Controls.Add(this.ValueButton);
            this.tabPage1.Controls.Add(this.ListButton);
            this.tabPage1.Controls.Add(this.TupleButton);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(825, 525);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Browser";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(704, 134);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 19;
            this.label1.Text = "Filter";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(707, 150);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(100, 20);
            this.textBox1.TabIndex = 18;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.button1);
            this.tabPage2.Controls.Add(this.richTextBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(825, 525);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "PyExec";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(357, 437);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Exec";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(33, 27);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(738, 393);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            // 
            // PythonBrowserFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(833, 551);
            this.Controls.Add(this.tabControl1);
            this.Name = "PythonBrowserFrm";
            this.Text = "Python Browser";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.PythonBrowserFrm_FormClosed);
            this.groupBox1.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RadioButton TypesButton;
        private System.Windows.Forms.Button btn_windows;
        private System.Windows.Forms.Button btn_builtin;
        private System.Windows.Forms.Button btn_session;
        private System.Windows.Forms.Button btn_activeship;
        private System.Windows.Forms.Button btn_getservice;
        private System.Windows.Forms.Button btn_const;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton DictionaryButton;
        private System.Windows.Forms.RadioButton ClassButton;
        private System.Windows.Forms.RadioButton ListButton;
        private System.Windows.Forms.RadioButton TupleButton;
        private System.Windows.Forms.RadioButton ValueButton;
        private System.Windows.Forms.RadioButton AutoButton;
        private System.Windows.Forms.ColumnHeader NameHeader;
        private System.Windows.Forms.ColumnHeader ValueHeader;
        private System.Windows.Forms.ColumnHeader TypeHeader;
        private System.Windows.Forms.ListView AttributesList;
        private System.Windows.Forms.Button EvaluateButton;
        private System.Windows.Forms.ComboBox EvaluateBox;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button4;
    }
}

