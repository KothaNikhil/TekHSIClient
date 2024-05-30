using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TekVISANet;

namespace VisaAndHSIwrapper
{
    public class TekVisaWrapper : IDisposable
    {
        private VISA visa_obj;
        public event EventHandler StatusMessageUpdated;

        public bool Connect(string address)
        {
            try
            {
                visa_obj = new VISA();
                visa_obj.Open(address);

                if (visa_obj.Status != TekVISADefs.Status.SUCCESS)
                {
                    StatusMessageUpdated?.Invoke(string.Format("Unable to connect to MSO at address {0}", address), null);
                    return false;
                }
                string retString;
                visa_obj.Query("*IDN?", out retString);
                StatusMessageUpdated?.Invoke("Connected to: " + address, null);
                return true;
            }
            catch(Exception e)
            {
                StatusMessageUpdated?.Invoke(e.Message, null);
                return false;
            }
        }

        public void SetScopeParams()
        {
            Write("*RST");
            Write("DISplay:WAVEform OFF");
            Write("HOR:MODE MAN");
            Write("HOR:MODE:RECO 2500");
            Write("ACQuire:STOPAfter RUNStop");
            Write("ACQuire:STATE ON");
        }

        public void TurnOnChannels(string[] channels)
        {
            foreach(var ch in channels)
            {
                Write(string.Format("DISplay:GLObal:{0}:STATE 1", ch.ToUpper()));
            }
        }

        public void SetRlen(string rlen)
        {
            Write("HOR:MODE:RECO " + rlen);
        }

        public void ResetScope()
        {
            Write("*RST");
        }

        public void Write(string message)
        {
            visa_obj.Write(message);
        }

        public string Query(string message)
        {
            string value;
            visa_obj.Query(message, out value);
            return value;
        }

        public void Dispose()
        {
            visa_obj.Close();
            visa_obj.Dispose();
        }
    }
}
