using System;

namespace TekVISAIO
{

    public class TekVISAEventArgs : EventArgs
    {
        public TekVISAEventArgs(TekVISADefs.EventTypes NewType, uint NewContext, uint NewUserHandle)
        {
            EventType = NewType;
            Context = NewContext;
            UserHandle = NewUserHandle;
        }

        public TekVISADefs.EventTypes EventType { get; set; }

        public uint Context { get; set; }

        public uint UserHandle { get; set; }
    }
}