
using System;

namespace TekVISAIO
{
    public class TekVISAException : Exception
    {
        public TekVISAException(VISA vi, string Location, TekVISADefs.Status status)
        {
            VISALocation = Location;
            VISAStatus = status;
            VISAError = vi.StatusDesc(status);
            VISAMessage = VISALocation + ": " + VISAError;
        }

        public string VISALocation { get; set; } = "";

        public string VISAError { get; set; } = "";

        public string VISAMessage { get; set; } = "";

        public TekVISADefs.Status VISAStatus { get; set; } = TekVISADefs.Status.SUCCESS;
    }
}