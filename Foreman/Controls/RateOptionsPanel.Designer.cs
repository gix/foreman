namespace Foreman
{
    partial class RateOptionsPanel
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.autoOption = new System.Windows.Forms.RadioButton();
            this.fixedOption = new System.Windows.Forms.RadioButton();
            this.fixedTextBox = new System.Windows.Forms.TextBox();
            this.unitLabel = new System.Windows.Forms.Label();
            this.ratePanel = new System.Windows.Forms.Panel();
            this.assemblerPanel = new System.Windows.Forms.Panel();
            this.moduleButton4 = new System.Windows.Forms.Button();
            this.moduleButton3 = new System.Windows.Forms.Button();
            this.moduleButton2 = new System.Windows.Forms.Button();
            this.moduleButton1 = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.speedBonusTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.productivityBonusTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.modulesButton = new System.Windows.Forms.Button();
            this.assemblerButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.ratePanel.SuspendLayout();
            this.assemblerPanel.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // autoOption
            // 
            this.autoOption.AutoSize = true;
            this.autoOption.Checked = true;
            this.autoOption.Location = new System.Drawing.Point(4, 4);
            this.autoOption.Name = "autoOption";
            this.autoOption.Size = new System.Drawing.Size(47, 17);
            this.autoOption.TabIndex = 0;
            this.autoOption.TabStop = true;
            this.autoOption.Text = "Auto";
            this.autoOption.UseVisualStyleBackColor = true;
            this.autoOption.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyPressed);
            // 
            // fixedOption
            // 
            this.fixedOption.AutoSize = true;
            this.fixedOption.Location = new System.Drawing.Point(4, 27);
            this.fixedOption.Name = "fixedOption";
            this.fixedOption.Size = new System.Drawing.Size(50, 17);
            this.fixedOption.TabIndex = 1;
            this.fixedOption.Text = "Fixed";
            this.fixedOption.UseVisualStyleBackColor = true;
            this.fixedOption.CheckedChanged += new System.EventHandler(this.fixedOption_CheckedChanged);
            this.fixedOption.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyPressed);
            // 
            // fixedTextBox
            // 
            this.fixedTextBox.Location = new System.Drawing.Point(4, 51);
            this.fixedTextBox.Name = "fixedTextBox";
            this.fixedTextBox.Size = new System.Drawing.Size(65, 20);
            this.fixedTextBox.TabIndex = 2;
            this.fixedTextBox.TextChanged += new System.EventHandler(this.fixedTextBox_TextChanged);
            this.fixedTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyPressed);
            // 
            // unitLabel
            // 
            this.unitLabel.AutoSize = true;
            this.unitLabel.Location = new System.Drawing.Point(74, 54);
            this.unitLabel.Name = "unitLabel";
            this.unitLabel.Size = new System.Drawing.Size(17, 13);
            this.unitLabel.TabIndex = 3;
            this.unitLabel.Text = "/s";
            // 
            // ratePanel
            // 
            this.ratePanel.Controls.Add(this.autoOption);
            this.ratePanel.Controls.Add(this.unitLabel);
            this.ratePanel.Controls.Add(this.fixedOption);
            this.ratePanel.Controls.Add(this.fixedTextBox);
            this.ratePanel.Location = new System.Drawing.Point(3, 3);
            this.ratePanel.Name = "ratePanel";
            this.ratePanel.Size = new System.Drawing.Size(95, 76);
            this.ratePanel.TabIndex = 4;
            // 
            // assemblerPanel
            // 
            this.assemblerPanel.AutoSize = true;
            this.assemblerPanel.Controls.Add(this.moduleButton4);
            this.assemblerPanel.Controls.Add(this.moduleButton3);
            this.assemblerPanel.Controls.Add(this.moduleButton2);
            this.assemblerPanel.Controls.Add(this.moduleButton1);
            this.assemblerPanel.Controls.Add(this.label4);
            this.assemblerPanel.Controls.Add(this.speedBonusTextBox);
            this.assemblerPanel.Controls.Add(this.label3);
            this.assemblerPanel.Controls.Add(this.productivityBonusTextBox);
            this.assemblerPanel.Controls.Add(this.label2);
            this.assemblerPanel.Controls.Add(this.label1);
            this.assemblerPanel.Controls.Add(this.modulesButton);
            this.assemblerPanel.Controls.Add(this.assemblerButton);
            this.assemblerPanel.Location = new System.Drawing.Point(104, 3);
            this.assemblerPanel.Name = "assemblerPanel";
            this.assemblerPanel.Size = new System.Drawing.Size(213, 120);
            this.assemblerPanel.TabIndex = 5;
            // 
            // moduleButton4
            // 
            this.moduleButton4.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.moduleButton4.Location = new System.Drawing.Point(186, 50);
            this.moduleButton4.Name = "moduleButton4";
            this.moduleButton4.Size = new System.Drawing.Size(23, 23);
            this.moduleButton4.TabIndex = 11;
            this.moduleButton4.UseVisualStyleBackColor = true;
            this.moduleButton4.Click += new System.EventHandler(this.moduleButton_Click);
            // 
            // moduleButton3
            // 
            this.moduleButton3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.moduleButton3.Location = new System.Drawing.Point(161, 50);
            this.moduleButton3.Name = "moduleButton3";
            this.moduleButton3.Size = new System.Drawing.Size(23, 23);
            this.moduleButton3.TabIndex = 10;
            this.moduleButton3.UseVisualStyleBackColor = true;
            this.moduleButton3.Click += new System.EventHandler(this.moduleButton_Click);
            // 
            // moduleButton2
            // 
            this.moduleButton2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.moduleButton2.Location = new System.Drawing.Point(136, 50);
            this.moduleButton2.Name = "moduleButton2";
            this.moduleButton2.Size = new System.Drawing.Size(23, 23);
            this.moduleButton2.TabIndex = 9;
            this.moduleButton2.UseVisualStyleBackColor = true;
            this.moduleButton2.Click += new System.EventHandler(this.moduleButton_Click);
            // 
            // moduleButton1
            // 
            this.moduleButton1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.moduleButton1.Location = new System.Drawing.Point(111, 50);
            this.moduleButton1.Name = "moduleButton1";
            this.moduleButton1.Size = new System.Drawing.Size(23, 23);
            this.moduleButton1.TabIndex = 8;
            this.moduleButton1.UseVisualStyleBackColor = true;
            this.moduleButton1.Click += new System.EventHandler(this.moduleButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(29, 77);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(74, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Speed Bonus:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // speedBonusTextBox
            // 
            this.speedBonusTextBox.Location = new System.Drawing.Point(110, 75);
            this.speedBonusTextBox.Name = "speedBonusTextBox";
            this.speedBonusTextBox.Size = new System.Drawing.Size(100, 20);
            this.speedBonusTextBox.TabIndex = 6;
            this.speedBonusTextBox.TextChanged += new System.EventHandler(this.speedBonusTextBox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(5, 100);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Productivity Bonus:";
            this.label3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // productivityBonusTextBox
            // 
            this.productivityBonusTextBox.Location = new System.Drawing.Point(110, 97);
            this.productivityBonusTextBox.Name = "productivityBonusTextBox";
            this.productivityBonusTextBox.Size = new System.Drawing.Size(100, 20);
            this.productivityBonusTextBox.TabIndex = 4;
            this.productivityBonusTextBox.TextChanged += new System.EventHandler(this.productivityBonusTextBox_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(53, 31);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Modules:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(45, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Assembler:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // modulesButton
            // 
            this.modulesButton.AutoSize = true;
            this.modulesButton.Location = new System.Drawing.Point(110, 26);
            this.modulesButton.Name = "modulesButton";
            this.modulesButton.Size = new System.Drawing.Size(100, 23);
            this.modulesButton.TabIndex = 1;
            this.modulesButton.Text = "Best";
            this.modulesButton.UseVisualStyleBackColor = true;
            this.modulesButton.Click += new System.EventHandler(this.modulesButton_Click);
            // 
            // assemblerButton
            // 
            this.assemblerButton.AutoSize = true;
            this.assemblerButton.Location = new System.Drawing.Point(110, 3);
            this.assemblerButton.Name = "assemblerButton";
            this.assemblerButton.Size = new System.Drawing.Size(100, 23);
            this.assemblerButton.TabIndex = 0;
            this.assemblerButton.Text = "Best";
            this.assemblerButton.UseVisualStyleBackColor = true;
            this.assemblerButton.Click += new System.EventHandler(this.assemblerButton_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.ratePanel);
            this.flowLayoutPanel1.Controls.Add(this.assemblerPanel);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(320, 126);
            this.flowLayoutPanel1.TabIndex = 8;
            // 
            // RateOptionsPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "RateOptionsPanel";
            this.Size = new System.Drawing.Size(326, 132);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.KeyPressed);
            this.ratePanel.ResumeLayout(false);
            this.ratePanel.PerformLayout();
            this.assemblerPanel.ResumeLayout(false);
            this.assemblerPanel.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label unitLabel;
        private System.Windows.Forms.Panel ratePanel;
        private System.Windows.Forms.Panel assemblerPanel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button modulesButton;
        private System.Windows.Forms.Button assemblerButton;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox productivityBonusTextBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox speedBonusTextBox;
        private System.Windows.Forms.RadioButton autoOption;
        private System.Windows.Forms.RadioButton fixedOption;
        private System.Windows.Forms.TextBox fixedTextBox;
        private System.Windows.Forms.Button moduleButton1;
        private System.Windows.Forms.Button moduleButton4;
        private System.Windows.Forms.Button moduleButton3;
        private System.Windows.Forms.Button moduleButton2;
    }
}
