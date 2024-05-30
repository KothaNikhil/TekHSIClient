using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Tek.Scope.Support;
using TekHighspeedAPI;

namespace VisaAndHSIwrapper
{
    public class TekHSIwrapper: IDisposable
    {
        HSIClient hsiClient = null;
        private string connected_ip;
        public event EventHandler DataAvaialble;

        public void Dispose()
        {
            if (hsiClient != null)
            {
                hsiClient.DataAccess -= DataAccess;
                hsiClient.Dispose();
            }
        }

        public bool Connect(string ip, string[] channels)
        {
            hsiClient?.Dispose();
            try
            {
                hsiClient = HSIClient.Connect(ip, channels);
                if (hsiClient == null) return false;
                connected_ip = ip;
                hsiClient.DataAccess += DataAccess;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void StartCapturingData()
        {
            hsiClient.Start();
        }

        string[] rlens = new string[] {"1000","2000","5000","10000","20000","50000",
                "100000","200000","500000","1000000","2000000","5000000","10000000","20000000","50000000"};

        private void DataAccess(HSIClient hsi, CancellationToken tok, IEnumerable<object> data, double updateTime)
        {
            List<INormalizedVector> wfms = new List<INormalizedVector>();
            foreach (var datum in data.OrderBy(x => ((Tek.Scope.Support.INormalizedVector)x).SourceName))
            {
                if (!(datum is INormalizedVector wfm))
                    continue;

                wfms.Add(wfm);
            }
            DataAvaialble?.Invoke(wfms, null);
        }
    }
}
