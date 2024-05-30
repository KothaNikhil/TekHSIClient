using System;

namespace TekVISAIO
{

    public class TekVISAErrorArgs : EventArgs
    {
        public TekVISAErrorArgs(VISA vi, string NewErrorLocation, TekVISADefs.Status status)
        {
            ErrorLocation = NewErrorLocation;
            ErrorStatus = status;
            ErrorDescription = vi.StatusDesc(status);
            ErrorMessage = ErrorLocation + ": " + ErrorDescription;
        }

        public string ErrorLocation { get; set; } = "";

        public string ErrorDescription { get; set; } = "";

        public string ErrorMessage { get; set; } = "";

        public TekVISADefs.Status ErrorStatus { get; set; } = TekVISADefs.Status.SUCCESS;
    }
}