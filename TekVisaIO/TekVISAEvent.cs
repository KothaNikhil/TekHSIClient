
using System;

namespace TekVISAIO
{

    public class TekVISAEvent : IDisposable
    {
        public bool bEnabled;
        public uint Context = 0;
        public EventHandler Event;
        private bool IsDisposed;
        public TekVISADefs.EventMechanism Mechanism;
        public TekVISADefs.EventTypes Type;
        public uint UserHandle;

        public TekVISAEvent(
            EventHandler Handler,
            TekVISADefs.EventTypes EventType,
            uint EventUserHandle)
        {
            Event += Handler;
            Type = EventType;
            UserHandle = EventUserHandle;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;
            IsDisposed = true;
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;
            Event = null;
        }
    }
}