﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ZedGraph;
using System.Diagnostics;
using VisaAndHSIwrapper;
using Tek.Scope.Support;

namespace Plot
{
    public partial class Form1 : Form
    {
        private string ip = "";
        private string connected_ip = "-";
        TekHSIwrapper tekHSIwrapper = new TekHSIwrapper();
        private readonly GraphPane graphPane;

        static readonly Color[] Colors = {
            Color.Yellow, Color.Cyan, Color.Red, Color.Green,
            Color.Gold, Color.DarkBlue, Color.Magenta, Color.DarkGoldenrod
        };

        /// <summary>
        /// 
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            tekHSIwrapper.DataAvaialble += TekHSIwrapper_DataAvaialble;

            graphPane = zedGraphControl1.GraphPane;
            graphPane.Title.Text = "Plot";
            graphPane.XAxis.Title.Text = "X Axis";
            graphPane.YAxis.Title.Text = "Y Axis";
            ip = textBox1.Text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            tekHSIwrapper.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            ip = textBox1.Text;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (string.Compare(ip, connected_ip, StringComparison.CurrentCultureIgnoreCase) == 0) return;
            string[] channels = textBox2.Text.Split(',');
            bool isConnected = tekHSIwrapper.Connect(ip, channels);
            if (isConnected)
            {
                tekHSIwrapper.StartCapturingData();
            }
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            ClearGraph();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            tekHSIwrapper.Dispose();
        }

        private void TekHSIwrapper_DataAvaialble(object sender, EventArgs e)
        {
            var wfms = sender as List<INormalizedVector>;

            int color = 0;
            foreach (INormalizedVector wfm in wfms)
            {
                ClearGraph();

                int n = Convert.ToInt32(wfm.Count); // Replace with your desired value of n
                double[] dataX = Enumerable.Range(0, n).Select(x => (double)x).ToArray();

                Trace.WriteLine(wfm.SourceName);
                LineItem curve = graphPane.AddCurve(wfm.SourceName, dataX, wfm.ToArray(), color: Colors[color++], SymbolType.None);

                // Set the x-axis scale
                graphPane.XAxis.Scale.Min = 0; // Minimum value
                graphPane.XAxis.Scale.Max = wfm.Count; // Maximum value

                zedGraphControl1.AxisChange();
                zedGraphControl1.Invalidate();
            }
        }

        private void ClearGraph()
        {
            // Clear the GraphPane
            zedGraphControl1.GraphPane.CurveList.Clear();

            // Refresh the plot
            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();
        }
    }
}
