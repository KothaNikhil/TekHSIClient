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
                SubscribeToDataAccess();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SubscribeToDataAccess()
        {
            hsiClient.DataAccess += DataAccess;
        }

        public void StartCapturingData()
        {
            hsiClient.Start();
        }

        private void DataAccess(HSIClient hsi, CancellationToken tok, IEnumerable<object> data, double updateTime)
        {
            hsiClient.DataAccess -= DataAccess;

            List<INormalizedVector> wfms = new List<INormalizedVector>();
            foreach (var datum in data.OrderBy(x => ((Tek.Scope.Support.INormalizedVector)x).SourceName))
            {
                INormalizedVector wfm;
                if (datum is INormalizedVector)
                    wfm = datum as INormalizedVector;
                else
                    continue;

                wfms.Add(wfm);
            }
            DataAvaialble?.Invoke(wfms, null);
        }
    }
}
