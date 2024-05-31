
namespace Plot
{
    partial class GUIForm
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
            this.LogTextBox = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.buttonStop = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.buttonRun = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.visaAddTextBox = new System.Windows.Forms.TextBox();
            this.channelsTextBox = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.ipTextBox = new System.Windows.Forms.TextBox();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // LogTextBox
            // 
            this.LogTextBox.Dock = System.Windows.Forms.DockStyle.Right;
            this.LogTextBox.Location = new System.Drawing.Point(502, 0);
            this.LogTextBox.Margin = new System.Windows.Forms.Padding(1);
            this.LogTextBox.Multiline = true;
            this.LogTextBox.Name = "LogTextBox";
            this.LogTextBox.Size = new System.Drawing.Size(298, 450);
            this.LogTextBox.TabIndex = 12;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.buttonStop);
            this.panel1.Controls.Add(this.buttonClear);
            this.panel1.Controls.Add(this.buttonRun);
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.visaAddTextBox);
            this.panel1.Controls.Add(this.channelsTextBox);
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.ipTextBox);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(502, 97);
            this.panel1.TabIndex = 13;
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(420, 63);
            this.buttonStop.Margin = new System.Windows.Forms.Padding(1);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(64, 22);
            this.buttonStop.TabIndex = 19;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // buttonClear
            // 
            this.buttonClear.Location = new System.Drawing.Point(420, 6);
            this.buttonClear.Margin = new System.Windows.Forms.Padding(1);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(64, 22);
            this.buttonClear.TabIndex = 18;
            this.buttonClear.Text = "Clear";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // buttonRun
            // 
            this.buttonRun.Location = new System.Drawing.Point(420, 33);
            this.buttonRun.Margin = new System.Windows.Forms.Padding(1);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(64, 22);
            this.buttonRun.TabIndex = 17;
            this.buttonRun.Text = "Run";
            this.buttonRun.UseVisualStyleBackColor = true;
            this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 38);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(70, 13);
            this.label3.TabIndex = 16;
            this.label3.Text = "Visa address:";
            // 
            // visaAddTextBox
            // 
            this.visaAddTextBox.Location = new System.Drawing.Point(92, 35);
            this.visaAddTextBox.Margin = new System.Windows.Forms.Padding(1);
            this.visaAddTextBox.Name = "visaAddTextBox";
            this.visaAddTextBox.Size = new System.Drawing.Size(231, 20);
            this.visaAddTextBox.TabIndex = 15;
            this.visaAddTextBox.Text = "USB::0X0699::0X0528::B011090::INSTR";
            // 
            // channelsTextBox
            // 
            this.channelsTextBox.Location = new System.Drawing.Point(92, 65);
            this.channelsTextBox.Margin = new System.Windows.Forms.Padding(1);
            this.channelsTextBox.Name = "channelsTextBox";
            this.channelsTextBox.Size = new System.Drawing.Size(161, 20);
            this.channelsTextBox.TabIndex = 14;
            this.channelsTextBox.Text = "ch1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(34, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 13);
            this.label2.TabIndex = 13;
            this.label2.Text = "Channels:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(29, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(59, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "Ip address:";
            // 
            // ipTextBox
            // 
            this.ipTextBox.Location = new System.Drawing.Point(92, 6);
            this.ipTextBox.Margin = new System.Windows.Forms.Padding(1);
            this.ipTextBox.Name = "ipTextBox";
            this.ipTextBox.Size = new System.Drawing.Size(161, 20);
            this.ipTextBox.TabIndex = 11;
            this.ipTextBox.Text = "134.64.244.152";
            this.ipTextBox.TextChanged += new System.EventHandler(this.ipTextBox_TextChanged);
            // 
            // GUIForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.LogTextBox);
            this.Name = "GUIForm";
            this.Text = "GUIForm";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox LogTextBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox visaAddTextBox;
        private System.Windows.Forms.TextBox channelsTextBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox ipTextBox;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.Button buttonRun;
    }
}