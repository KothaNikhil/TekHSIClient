
namespace Plot
{
    partial class Form1
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
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.zedGraphControl1 = new ZedGraph.ZedGraphControl();
            this.buttonRun = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.buttonStop = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(195, 13);
            this.textBox1.Margin = new System.Windows.Forms.Padding(2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(370, 35);
            this.textBox1.TabIndex = 1;
            this.textBox1.Text = "134.64.244.152";
            // 
            // zedGraphControl1
            // 
            this.zedGraphControl1.Location = new System.Drawing.Point(19, 136);
            this.zedGraphControl1.Margin = new System.Windows.Forms.Padding(16);
            this.zedGraphControl1.Name = "zedGraphControl1";
            this.zedGraphControl1.ScrollGrace = 0D;
            this.zedGraphControl1.ScrollMaxX = 0D;
            this.zedGraphControl1.ScrollMaxY = 0D;
            this.zedGraphControl1.ScrollMaxY2 = 0D;
            this.zedGraphControl1.ScrollMinX = 0D;
            this.zedGraphControl1.ScrollMinY = 0D;
            this.zedGraphControl1.ScrollMinY2 = 0D;
            this.zedGraphControl1.Size = new System.Drawing.Size(1253, 953);
            this.zedGraphControl1.TabIndex = 2;
            this.zedGraphControl1.UseExtendedPrintDialog = true;
            // 
            // buttonRun
            // 
            this.buttonRun.Location = new System.Drawing.Point(1608, 9);
            this.buttonRun.Margin = new System.Windows.Forms.Padding(2);
            this.buttonRun.Name = "buttonRun";
            this.buttonRun.Size = new System.Drawing.Size(149, 49);
            this.buttonRun.TabIndex = 3;
            this.buttonRun.Text = "Run";
            this.buttonRun.UseVisualStyleBackColor = true;
            this.buttonRun.Click += new System.EventHandler(this.buttonRun_Click);
            // 
            // buttonClear
            // 
            this.buttonClear.Location = new System.Drawing.Point(1430, 9);
            this.buttonClear.Margin = new System.Windows.Forms.Padding(2);
            this.buttonClear.Name = "buttonClear";
            this.buttonClear.Size = new System.Drawing.Size(149, 49);
            this.buttonClear.TabIndex = 4;
            this.buttonClear.Text = "Clear";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.buttonClear_Click);
            // 
            // buttonStop
            // 
            this.buttonStop.Location = new System.Drawing.Point(1785, 9);
            this.buttonStop.Margin = new System.Windows.Forms.Padding(2);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(149, 49);
            this.buttonStop.TabIndex = 5;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(48, 20);
            this.label1.Margin = new System.Windows.Forms.Padding(7, 0, 7, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 29);
            this.label1.TabIndex = 6;
            this.label1.Text = "Ip address:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(609, 20);
            this.label2.Margin = new System.Windows.Forms.Padding(7, 0, 7, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(120, 29);
            this.label2.TabIndex = 7;
            this.label2.Text = "Channels:";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(738, 14);
            this.textBox2.Margin = new System.Windows.Forms.Padding(2);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(370, 35);
            this.textBox2.TabIndex = 8;
            this.textBox2.Text = "ch1";
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(195, 83);
            this.textBox3.Margin = new System.Windows.Forms.Padding(2);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(453, 35);
            this.textBox3.TabIndex = 9;
            this.textBox3.Text = "USB::0X0699::0X0528::B011090::INSTR";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(22, 86);
            this.label3.Margin = new System.Windows.Forms.Padding(7, 0, 7, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(158, 29);
            this.label3.TabIndex = 10;
            this.label3.Text = "Visa address:";
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(1291, 136);
            this.textBox4.Multiline = true;
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(690, 953);
            this.textBox4.TabIndex = 11;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(14F, 29F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(2014, 1117);
            this.Controls.Add(this.textBox4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.buttonClear);
            this.Controls.Add(this.buttonRun);
            this.Controls.Add(this.zedGraphControl1);
            this.Controls.Add(this.textBox1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.TextBox textBox1;
        private ZedGraph.ZedGraphControl zedGraphControl1;
        private System.Windows.Forms.Button buttonRun;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.Button buttonStop;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBox4;
    }
}

