using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tek.Scope.Support;
using TekHighspeedAPI;
using VisaAndHSIwrapper;
using ZedGraph;

namespace Plot
{
    public partial class GUIForm : Form
    {
        HSIClient hsiClient = null;
        private string ip = "";
        private string connected_ip = "-";
        //TekHSIwrapper tekHSIwrapper = null;
        TekVisaWrapper tekVisaWrapper = null;
        private readonly GraphPane graphPane;

        string[] rlens = new string[] {"1000","2000","5000","10000","20000","50000",
                "100000","200000","500000","1000000","2000000", "5000000","10000000", "20000000", "50000000"};
        int rlenIndex = 0;

        static readonly Color[] Colors = {
            Color.Yellow, Color.Cyan, Color.Red, Color.Green,
            Color.Gold, Color.DarkBlue, Color.Magenta, Color.DarkGoldenrod
        };

        /// <summary>
        /// 
        /// </summary>
        public GUIForm()
        {
            InitializeComponent();

            graphPane = zedGraphControl1.GraphPane;
            graphPane.Title.Text = "Plot";
            graphPane.XAxis.Title.Text = "X Axis";
            graphPane.YAxis.Title.Text = "Y Axis";
            ip = ipTextBox.Text;
        }

        private void TekVisaWrapper_StatusMessageUpdated(object sender, EventArgs e)
        {
            string msg = sender as string;
            LogMsg(msg);
        }

        private void LogMsg(string msg)
        {
            Trace.WriteLine(msg + "\r\n");
            if (InvokeRequired)
                BeginInvoke(new Action(() => { LogTextBox.AppendText(msg + "\r\n"); }));
            else
                LogTextBox.AppendText(msg + "\r\n");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            hsiClient?.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRun_Click(object sender, EventArgs e)
        {
            if (string.Compare(ip, connected_ip, StringComparison.CurrentCultureIgnoreCase) == 0) return;
            rlenIndex = 0;
            string[] channels = channelsTextBox.Text.Split(',');
            string visaAddress = visaAddTextBox.Text;
            hsiClient?.Dispose();
            try
            {
                bool isConnected = InitializeTekVisaConnections(channels, visaAddTextBox.Text);
                if (!isConnected) return;
                Thread.Sleep(1000);
                hsiClient = HSIClient.Connect(ip, channels);
                if (hsiClient == null) return;
                connected_ip = ip;
                hsiClient.DataAccess += HsiClient_DataAccess;
                hsiClient.Start();
            }
            catch
            {
            }
        }

        private void HsiClient_DataAccess(HSIClient hsi, CancellationToken tok, IEnumerable<object> data, double updateTime)
        {
            hsiClient.DataAccess -= HsiClient_DataAccess;

            if(rlenIndex >= rlens.Length)
            {
                LogMsg("max rlen index reached");
                Thread.Sleep(1000);
                hsiClient?.Dispose();
                tekVisaWrapper?.Dispose();
                connected_ip = "-";
                return;
            }
            BeginInvoke(new Action(() =>
            {
                ClearGraph();
                List<INormalizedVector> wfms = new List<INormalizedVector>();
                foreach (var datum in data.OrderBy(x => ((Tek.Scope.Support.INormalizedVector)x).SourceName))
                {
                    INormalizedVector wfm;
                    if (datum is INormalizedVector)
                        wfm = datum as INormalizedVector;
                    else
                        continue;

                    wfms.Add(wfm);
                    LogMsg(wfm.SourceName + ":" + wfm.Count);
                    int n = Convert.ToInt32(wfm.Count); // Replace with your desired value of n
                    double[] dataX = new double[n];// = Enumerable.Range(0, n).Select(x => (double)x).ToArray();

                    Parallel.ForEach(Partitioner.Create(0, n), range =>
                    {
                        for (var i = range.Item1; i < range.Item2; i++)
                            dataX[i] = i;
                    });

                    var dataY = wfm.ToArray();

                    //LineItem curve = graphPane.AddCurve(wfm.SourceName, dataX, dataY, color: Colors[0]);

                    //graphPane.XAxis.Scale.Min = 0; // Minimum value
                    //graphPane.XAxis.Scale.Max = wfm.Count; // Maximum value
                }

                //zedGraphControl1.AxisChange();
                //zedGraphControl1.Invalidate();
            }));
            tekVisaWrapper.SetRlen(rlens[rlenIndex++]);
            Thread.Sleep(1000);
            hsiClient.DataAccess += HsiClient_DataAccess;
        }

        private bool InitializeTekVisaConnections(string[] channels, string visaAddress)
        {
            tekVisaWrapper = new TekVisaWrapper();
            tekVisaWrapper.StatusMessageUpdated += TekVisaWrapper_StatusMessageUpdated;
            bool isConnectToVisa = tekVisaWrapper.Connect(visaAddress);
            if (!isConnectToVisa)
            {
                LogMsg("Connection failed to visa");
                return false;
            }
            LogMsg("Connected to visa: " + isConnectToVisa);
            tekVisaWrapper.TurnOnChannels(channels);
            tekVisaWrapper.SetScopeParams();
            tekVisaWrapper.SetRlen(rlens[rlenIndex++]);
            string rlen = tekVisaWrapper.Query("HOR:MODE:RECO?");
            return true;
        }

        private void buttonClear_Click(object sender, EventArgs e)
        {
            ClearGraph();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            hsiClient?.Dispose();
            tekVisaWrapper?.Dispose();
            connected_ip = "-";
        }

        private void ClearGraph()
        {
            // Clear the GraphPane
            graphPane.CurveList.Clear();
            graphPane.GraphObjList.Clear();

            // Refresh the plot
            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();
        }

        private void ipTextBox_TextChanged(object sender, EventArgs e)
        {
            ip = ipTextBox.Text;
        }
    }
}
