using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TekVISANet;

namespace VisaAndHSIwrapper
{
    public class TekVisaWrapper
    {
        private VISA visa_obj;

        public bool Connect(string address)
        {
            visa_obj = new VISA();
            visa_obj.Open(address);

            if (visa_obj.Status != TekVISADefs.Status.SUCCESS)
            {
                Console.WriteLine(string.Format("Unable to connect to MSO at address {0}", address));
                return false;
            }
            string retString;
            visa_obj.Query("*IDN?", out retString);
            Console.WriteLine("Connected to: " + address);
            Console.WriteLine("*IDN? returned " + retString);
            return true;
        }

        public void SetScopeParams()
        {
            Write("DISplay:GLObal:CH1:STATE 1");
            //Write("DISplay:GLObal:CH2:STATE 1");
            Write("DISplay:WAVEform OFF");
            Write("HOR:MODE MAN");
            Write("HOR:MODE:RECO 2500");
            Write("ACQuire:STOPAfter RUNStop");
            Write("ACQuire:STATE ON");
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
            visa_obj.Query(message, out string value);
            return value;
        }
    }
}
