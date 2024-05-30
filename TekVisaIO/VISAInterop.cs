//#define VISA32
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace TekVISAIO
{

    public class VISALock : IDisposable
    {
        private VISA _visa;
        public VISALock(VISA visa)
        {
            _visa = visa;
            if (visa.Session <= 0) return;
            while (!_visa.Lock(TekVISADefs.AccessModes.VI_EXCLUSIVE_LOCK))
                Thread.Sleep(10);
        }

        public void Dispose()
        {
            _visa?.Unlock();
        }
    }
    public class VISA : IDisposable
    {
        private uint _RMSession;
        public OnErrorProc ErrorHandlerProc;
        private string ErrorLocation = "";
        private bool ErrorThrowOnError;
        private ArrayList EventHandlerClearBlocks = new ArrayList();
        private ArrayList EventHandlerExceptionBlocks = new ArrayList();
        private ArrayList EventHandlerGPIBCICBlocks = new ArrayList();
        private ArrayList EventHandlerGPIBListenBlocks = new ArrayList();
        private ArrayList EventHandlerGPIBTalkBlocks = new ArrayList();
        private ArrayList EventHandlerIOCompletionBlocks = new ArrayList();
        private ArrayList EventHandlerServiceReqBlocks = new ArrayList();
        private ArrayList EventHandlerTCPIPConnectBlocks = new ArrayList();
        private ArrayList EventHandlerTrigBlocks = new ArrayList();
        private ArrayList EventHandlerVXISigPBlocks = new ArrayList();
        private ArrayList EventHandlerVXIVMEIntrBlocks = new ArrayList();
        private ArrayList EventHandlerVXIVMESysFailBlocks = new ArrayList();
        private ArrayList EventHandlerVXIVMESysResetBlocks = new ArrayList();
        private uint InstrSession;
        private bool IsDisposed;
        private readonly EventHandlerProcs MainEventHandlerProc;
        private string ResourceString = "";

        public VISA()
        {
            OnErrorHandler(viOpenDefaultRM(out _RMSession));
            OnError("Create VISA", false, null);
            MainEventHandlerProc = EventsHandler;
            InstrSession = 0U;
        }

        public string Location
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                return ErrorLocation;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                ErrorLocation = value;
            }
        }

        public bool ThrowOnError
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                return ErrorThrowOnError;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                ErrorThrowOnError = value;
            }
        }

        public IDisposable ExclusiveLock => new VISALock(this);

        public TekVISADefs.Status Status { get; set; } = TekVISADefs.Status.SUCCESS;

        public uint Timeout
        {
            get
            {
                uint AttributeState;
                GetAttribute(TekVISADefs.Attributes.TMO_VALUE, out AttributeState);
                return AttributeState;
            }
            set { SetAttribute(TekVISADefs.Attributes.TMO_VALUE, value); }
        }

        public uint TimeoutRM { get; set; } = 5000;

        public uint Session => InstrSession;

        public uint RMSession => _RMSession;

        public bool IsLocked
        {
            get
            {
                short AttributeState = 0;
                GetAttribute(TekVISADefs.Attributes.RSRC_LOCK_STATE, out AttributeState);
                return AttributeState != 0;
            }
        }

        public string ErrorDescription => StatusDesc(Status);

        public string[] AllResources
        {
            get
            {
                var FindList = new ArrayList();
                return FindResources("?*", out FindList) ? (string[])FindList.ToArray(typeof(string)) : new string[0];
            }
        }

        public string[] TCPIPResources
        {
            get
            {
                var FindList = new ArrayList();
                return FindResources("TCPIP?*INSTR", out FindList)
                    ? (string[])FindList.ToArray(typeof(string))
                    : new string[0];
            }
        }

        public string[] ASRLResources
        {
            get
            {
                var FindList = new ArrayList();
                return FindResources("ASRL?*INSTR", out FindList)
                    ? (string[])FindList.ToArray(typeof(string))
                    : new string[0];
            }
        }

        public string[] GPIBResources
        {
            get
            {
                var FindList = new ArrayList();
                return FindResources("GPIB?*INSTR", out FindList)
                    ? (string[])FindList.ToArray(typeof(string))
                    : new string[0];
            }
        }

        public string[] USBResources
        {
            get
            {
                var FindList = new ArrayList();
                return FindResources("USB?*INSTR", out FindList)
                    ? (string[])FindList.ToArray(typeof(string))
                    : new string[0];
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (IsDisposed)
                    return;
                IsDisposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viSetAttributeString(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            string AttributeState);
#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viSetAttributeLong(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            int AttributeState);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viSetAttributeShort(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            short AttributeState);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viSetAttributeByte(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            byte AttributeState);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viSetAttributeULong(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            uint AttributeState);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viSetAttributeUShort(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            ushort AttributeState);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viSetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern unsafe TekVISADefs.Status viSetAttributeVoid(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            void* AttrributeState);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viOpenDefaultRM(out uint Session);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viClose(uint Session);
#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viGetAttributeLong(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            out int AttrState);
#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viGetAttributeShort(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            out short AttrState);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern unsafe TekVISADefs.Status viGetAttributePByte(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            byte* pAttrState);
#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viGetAttributeByte(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            out byte AttrState);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viGetAttributeULong(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            out uint AttrState);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viGetAttributeUShort(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            out ushort AttrState);
#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viGetAttribute", CharSet = CharSet.Ansi)]
#endif
        internal static extern unsafe TekVISADefs.Status viGetAttributeVoid(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            out void* AttrState);
#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern unsafe TekVISADefs.Status viStatusDesc(
            uint Session,
            TekVISADefs.Status Status,
            byte* pOutData);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viLock(
            uint Session,
            TekVISADefs.AccessModes AccessMode,
            uint Timeout,
            string KeyID,
            StringBuilder AccessKeyID);
#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viUnlock(uint Session);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern unsafe TekVISADefs.Status viUsbControlOut(
            uint vi,
            short bmRequestType,
            short bRequest,
            ushort wValue,
            ushort wIndex,
            ushort wLength,
            void* pbuf);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern unsafe TekVISADefs.Status viUsbControlIn(
            uint vi,
            short bmRequestType,
            short bRequest,
            ushort wValue,
            ushort wIndex,
            ushort wLength,
            void* pbuf,
            out ushort retCnt);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viRead", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viRead", CharSet = CharSet.Ansi)]
#endif
        internal static extern unsafe TekVISADefs.Status viReadByte(
            uint Session,
            byte* pOutData,
            uint Count,
            out uint ReturnCount);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viRead", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viRead", CharSet = CharSet.Ansi)]
#endif
        internal static extern unsafe TekVISADefs.Status viReadVoid(
            uint Session,
            void* pOutData,
            uint Count,
            out uint ReturnCount);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        private static extern TekVISADefs.Status viWrite(
            uint Session,
            string InputBuffer,
            uint Count,
            out uint ReturnCount);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        private static extern TekVISADefs.Status viAssertTrigger(
            uint Session,
            TekVISADefs.Definitions Prototcol);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viClear(uint Session);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viFindRsrc(
            uint RMSession,
            string FindString,
            out uint Session,
            out uint ReturnCount,
            StringBuilder ResourceArray);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viFindNext(uint Session, StringBuilder Resource);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viInstallHandler(
            uint Session,
            TekVISADefs.EventTypes EventType,
            EventHandlerProcs handler,
            uint UserHandle);

#if VISA32
    [DllImport("VISA32.Dll", EntryPoint = "viUninstallHandler", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", EntryPoint = "viUninstallHandler", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viUnInstallHandler(
            uint session,
            TekVISADefs.EventTypes EventType,
            EventHandlerProcs handler,
            uint UserHandle);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        private static extern TekVISADefs.Status viReadSTB(uint Session, out ushort Status);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viEnableEvent(
            uint Session,
            TekVISADefs.EventTypes EventType,
            TekVISADefs.EventMechanism Mechanism,
            uint Context);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viDisableEvent(
            uint Session,
            TekVISADefs.EventTypes EventType,
            TekVISADefs.EventMechanism Mechanism);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viDiscardEvents(
            uint Session,
            TekVISADefs.EventTypes EventType,
            TekVISADefs.EventMechanism Mechanism);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viWaitOnEvent(
            uint Session,
            TekVISADefs.EventTypes EventType,
            uint Timeout,
            out TekVISADefs.EventTypes EventTypeOut,
            out uint EventOut);

#if VISA32
    [DllImport("VISA32.Dll", CharSet = CharSet.Ansi)]
#else
        [DllImport("VISA64.Dll", CharSet = CharSet.Ansi)]
#endif
        internal static extern TekVISADefs.Status viOpen(
            uint RMSession,
            string Resource,
            TekVISADefs.AccessModes AccessMode,
            uint Timeout,
            out uint Session);

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                ResourceString = "";
                if (InstrSession != 0U)
                {
                    var num = (int)viClose(InstrSession);
                    InstrSession = 0U;
                }

                if (RMSession == 0U)
                    return;
                var num1 = (int)viClose(RMSession);
                _RMSession = 0U;
            }
            catch
            {
            }
        }

        public void Close()
        {
            Dispose();
        }

        private bool IsError(TekVISADefs.Status status)
        {
            return status < TekVISADefs.Status.SUCCESS;
        }

        public virtual void OnError(string Location, bool NewThrowOnError, OnErrorProc ErrorHandler)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            ErrorLocation = Location;
            ThrowOnError = NewThrowOnError;
            ErrorHandlerProc = ErrorHandler;
        }

        public bool OnErrorHandler(TekVISADefs.Status status)
        {
            lock (this)
            {
                Status = status;
                if (!IsError(status))
                    return false;
                if (ErrorHandlerProc != null)
                    ErrorHandlerProc(this, new TekVISAErrorArgs(this, ErrorLocation, status));
                if (ThrowOnError)
                    throw new TekVISAException(this, ErrorLocation, status);
                return true;
            }
        }

        public unsafe string StatusDesc(TekVISADefs.Status Status)
        {
            var Status1 = Status;
            var stringBuilder = new StringBuilder(8192);
            var numArray = new byte[8192];
            fixed (byte* pOutData = numArray)
            {
                if (TekVISADefs.Status.SUCCESS > viStatusDesc(InstrSession, Status1, pOutData))
                    return "Error code:\t" + Status.ToString("x");
            }

            for (var index = 0; index < 8191 && 0 != numArray[index]; ++index)
            {
                var ch = (char)numArray[index];
                if ('\r' != ch && '\t' != ch && '\n' != ch)
                    stringBuilder.Append(ch);
            }

            return stringBuilder.ToString();
        }

        public bool SetAttribute(
            TekVISADefs.Attributes AttributeName,
            TekVISADefs.Definitions AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeLong(InstrSession, AttributeName, (int)AttributeState));
        }

        public bool SetAttribute(TekVISADefs.Attributes AttributeName, string AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeString(InstrSession, AttributeName, AttributeState));
        }

        public bool SetAttribute(TekVISADefs.Attributes AttributeName, long AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeLong(InstrSession, AttributeName, (int)AttributeState));
        }

        public bool SetAttribute(TekVISADefs.Attributes AttributeName, short AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeShort(InstrSession, AttributeName, AttributeState));
        }

        public bool SetAttribute(TekVISADefs.Attributes AttributeName, byte AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeByte(InstrSession, AttributeName, AttributeState));
        }

        public bool SetAttribute(TekVISADefs.Attributes AttributeName, ulong AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeULong(InstrSession, AttributeName, (uint)AttributeState));
        }

        public bool SetAttribute(TekVISADefs.Attributes AttributeName, ushort AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeUShort(InstrSession, AttributeName, AttributeState));
        }

        public unsafe bool SetAttribute(TekVISADefs.Attributes AttributeName, void* AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeVoid(InstrSession, AttributeName, AttributeState));
        }

        public bool SetAttributeRM(TekVISADefs.Attributes AttributeName, string AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeString(RMSession, AttributeName, AttributeState));
        }

        public bool SetAttributeRM(TekVISADefs.Attributes AttributeName, long AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeLong(RMSession, AttributeName, (int)AttributeState));
        }

        public bool SetAttributeRM(TekVISADefs.Attributes AttributeName, short AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeShort(RMSession, AttributeName, AttributeState));
        }

        public bool SetAttributeRM(TekVISADefs.Attributes AttributeName, byte AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeByte(RMSession, AttributeName, AttributeState));
        }

        public bool SetAttributeRM(TekVISADefs.Attributes AttributeName, ulong AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeULong(RMSession, AttributeName, (uint)AttributeState));
        }

        public bool SetAttributeRM(TekVISADefs.Attributes AttributeName, ushort AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeUShort(RMSession, AttributeName, AttributeState));
        }

        public unsafe bool SetAttributeRM(TekVISADefs.Attributes AttributeName, void* AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viSetAttributeVoid(RMSession, AttributeName, AttributeState));
        }

        private unsafe bool GetAttribute(
            uint Session,
            TekVISADefs.Attributes AttributeName,
            int Count,
            out string OutputBuffer)
        {
            var stringBuilder = new StringBuilder(Count);
            var numArray = new byte[Count];
            var flag = false;
            numArray[0] = 0;
            fixed (byte* pAttrState = numArray)
            {
                flag = !OnErrorHandler(viGetAttributePByte(Session, AttributeName, pAttrState));
            }

            var index = 0;
            char ch;
            do
            {
                ch = (char)numArray[index];
                if ('\r' != ch && '\t' != ch && '\n' != ch)
                    stringBuilder.Append(ch);
                ++index;
            } while (char.MinValue != ch);

            OutputBuffer = stringBuilder.ToString();
            if (Count < OutputBuffer.Length)
                OutputBuffer = OutputBuffer.Substring(0, Count);
            return true;
        }

        public bool GetAttribute(
            TekVISADefs.Attributes AttributeName,
            int Count,
            out string OutputBuffer)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !GetAttribute(InstrSession, AttributeName, Count, out OutputBuffer);
        }

        public bool GetAttribute(TekVISADefs.Attributes AttributeName, out int AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeLong(InstrSession, AttributeName, out AttributeState));
        }

        public bool GetAttribute(TekVISADefs.Attributes AttributeName, out short AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeShort(InstrSession, AttributeName, out AttributeState));
        }

        public bool GetAttribute(TekVISADefs.Attributes AttributeName, out byte AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeByte(InstrSession, AttributeName, out AttributeState));
        }

        public bool GetAttribute(TekVISADefs.Attributes AttributeName, out uint AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeULong(InstrSession, AttributeName, out AttributeState));
        }

        public bool GetAttribute(TekVISADefs.Attributes AttributeName, out ushort AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeUShort(InstrSession, AttributeName, out AttributeState));
        }

        public unsafe bool GetAttribute(TekVISADefs.Attributes AttributeName, out void* pAttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeVoid(InstrSession, AttributeName, out pAttributeState));
        }

        public bool GetAttributeRM(
            TekVISADefs.Attributes AttributeName,
            int Count,
            out string OutputBuffer)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return GetAttribute(RMSession, AttributeName, Count, out OutputBuffer);
        }

        public bool GetAttributeRM(TekVISADefs.Attributes AttributeName, out int AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeLong(RMSession, AttributeName, out AttributeState));
        }

        public bool GetAttributeRM(TekVISADefs.Attributes AttributeName, out short AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeShort(RMSession, AttributeName, out AttributeState));
        }

        public bool GetAttributeRM(TekVISADefs.Attributes AttributeName, out byte AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeByte(RMSession, AttributeName, out AttributeState));
        }

        public bool GetAttributeRM(TekVISADefs.Attributes AttributeName, out uint AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeULong(RMSession, AttributeName, out AttributeState));
        }

        public bool GetAttributeRM(TekVISADefs.Attributes AttributeName, out ushort AttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeUShort(RMSession, AttributeName, out AttributeState));
        }

        public unsafe bool GetAttributeRM(
            TekVISADefs.Attributes AttributeName,
            out void* pAttributeState)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viGetAttributeVoid(RMSession, AttributeName, out pAttributeState));
        }

        public bool Lock(TekVISADefs.AccessModes AccessMode, uint TimeoutValue)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                return !OnErrorHandler(viLock(InstrSession, AccessMode, TimeoutValue, null, null));
            }
        }

        public bool Lock(TekVISADefs.AccessModes AccessMode)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                return !OnErrorHandler(viLock(InstrSession, AccessMode, TimeoutRM, null, null));
            }
        }

        public bool Lock(TekVISADefs.AccessModes AccessMode, uint TimeoutValue, int Retries)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                TekVISADefs.Status status;
                do
                {
                    status = viLock(InstrSession, AccessMode, TimeoutValue, null, null);
                } while (status < TekVISADefs.Status.SUCCESS && 0 < --Retries);

                return !OnErrorHandler(status);
            }
        }

        public bool Unlock()
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                return !OnErrorHandler(viUnlock(InstrSession));
            }
        }

        public unsafe bool UsbControlOut(
            short bmRequestType,
            short bRequest,
            ushort wValue,
            ushort wIndex,
            ushort wLength,
            byte[] InputBuffer)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            fixed (byte* pbuf = InputBuffer)
            {
                if (OnErrorHandler(
                        viUsbControlOut(InstrSession, bmRequestType, bRequest, wValue, wIndex, wLength, pbuf)))
                    return false;
            }

            return true;
        }

        public unsafe bool UsbControlIn(
            short bmRequestType,
            short bRequest,
            ushort wValue,
            ushort wIndex,
            ushort wLength,
            out byte[] OutputBuffer,
            out ushort retCnt)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            retCnt = 0;
            if (wLength <= 0)
            {
                OutputBuffer = new byte[1];
                return false;
            }

            var numArray = new byte[wLength + 16];
            fixed (byte* pbuf = numArray)
            {
                if (OnErrorHandler(viUsbControlIn(InstrSession, bmRequestType, bRequest, wValue, wIndex, wLength, pbuf,
                        out retCnt)))
                {
                    OutputBuffer = new byte[1];
                    return false;
                }

                retCnt = (ushort)Math.Min(retCnt / 1, wLength);
                if (0 < retCnt)
                {
                    OutputBuffer = new byte[retCnt];
                    for (var index = 0; index < retCnt; ++index)
                        OutputBuffer[index] = numArray[index];
                }
                else
                {
                    OutputBuffer = new byte[1];
                }

                return true;
            }
        }

        public unsafe bool ReadRawBinary(out byte[] OutBuffer)
        {
            const uint BufferSize = 64 * 1024;
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));

            OutBuffer = new byte[0];

            lock (this)
            {
                var numArray = new byte[BufferSize];
                uint ReturnCount = 0;
                var status = TekVISADefs.Status.SUCCESS;
                try
                {
                    do
                    {
                        fixed (byte* pOutData = numArray)
                        {
                            status = viReadByte(InstrSession, pOutData, BufferSize, out ReturnCount);
                        }

                        if (ReturnCount <= 0 || status < 0)
                            break;
                        int startIndex = OutBuffer.Length;
                        Array.Resize(ref OutBuffer, (int)(startIndex + ReturnCount));
                        Array.Copy(numArray, 0, OutBuffer, startIndex, ReturnCount);
                    } while (status == TekVISADefs.Status.SUCCESS_MAX_CNT);
                }
                catch
                {
                }

                return !OnErrorHandler(status);
            }
        }

        public unsafe bool Read(out string OutputBuffer)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            lock (this)
            {
                var stringBuilder = new StringBuilder();
                var numArray = new byte[2048];
                uint ReturnCount = 0;
                var status = TekVISADefs.Status.SUCCESS;
                try
                {
                    do
                    {
                        fixed (byte* pOutData = numArray)
                        {
                            status = viReadByte(InstrSession, pOutData, 2048U, out ReturnCount);
                        }

                        for (var index = 0; index < ReturnCount; ++index)
                        {
                            var ch = (char)numArray[index];
                            if ('\r' != ch && '\t' != ch && '\n' != ch)
                                stringBuilder.Append(ch);
                        }
                    } while (status == TekVISADefs.Status.SUCCESS_MAX_CNT);
                }
                catch
                {
                }

                OutputBuffer = stringBuilder.ToString();
                return !OnErrorHandler(status);
            }
        }

        public bool Read(int Count, out string OutputBuffer)
        {
            int x;
            return Read(Count, out x, out OutputBuffer);
        }

        public unsafe bool Read(int Count, out int ReturnCount, out string OutputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                var stringBuilder = new StringBuilder(Count);
                var numArray = new byte[Count];
                uint ReturnCount1 = 0;
                fixed (byte* pOutData = numArray)
                {
                    if (OnErrorHandler(viReadByte(InstrSession, pOutData, (uint)Count, out ReturnCount1)))
                    {
                        ReturnCount = 0;
                        OutputBuffer = "";
                        return false;
                    }

                    for (var index = 0; index < ReturnCount1; ++index)
                    {
                        var ch = (char)numArray[index];
                        if ('\r' != ch && '\t' != ch && '\n' != ch)
                            stringBuilder.Append(ch);
                    }

                    ReturnCount = (int)ReturnCount1;
                    OutputBuffer = stringBuilder.ToString();
                    if (Count < OutputBuffer.Length)
                        OutputBuffer = OutputBuffer.Substring(0, Count);
                    return true;
                }
            }
        }

        private bool GetGPIBBlockLength(out uint Count)
        {
            lock (this)
            {
                Count = 0U;
                string OutputBuffer;
                while (Read(1, out OutputBuffer))
                {
                    if ("#" != OutputBuffer) continue;
                    if (Read(int.Parse(OutputBuffer), out OutputBuffer))
                    {
                        Count = uint.Parse(OutputBuffer);
                        return true;
                    }
                    break;
                }
                return false;
            }
        }

        public unsafe bool ReadBinary(out byte[] OutputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                uint Count;
                GetGPIBBlockLength(out Count);
                if (Count <= 0U)
                {
                    OutputBuffer = new byte[0];
                    return false;
                }

                var numArray = new byte[Count];
                try
                {
                    fixed (byte* pOutData1 = numArray)
                    {
                        uint ReturnCount;
                        var status = viReadVoid(InstrSession, pOutData1, Count, out ReturnCount);
                        if (IsError(status))
                        {
                            OutputBuffer = new byte[0];
                            OnErrorHandler(status);
                            return false;
                        }

                        Debug.Assert((int)Count == (int)ReturnCount);
                        if ((int)Count != (int)ReturnCount)
                        {
                            OutputBuffer = new byte[0];
                            OnErrorHandler(TekVISADefs.Status.ERROR_SYSTEM_ERROR);
                            return false;
                        }

                        if (status == TekVISADefs.Status.SUCCESS_MAX_CNT)
                        {
                            var timeout = Timeout;
                            Timeout = 0U;
                            uint rcount;
                            fixed (byte* pOutData2 = new byte[256])
                            {
                                var num = (int)viReadVoid(InstrSession, pOutData2, 256U, out rcount);
                            }

                            Timeout = timeout;
                            OnErrorHandler(TekVISADefs.Status.SUCCESS);
                        }
                        else
                        {
                            OnErrorHandler(status);
                        }
                    }
                }
                catch
                {
                    OutputBuffer = new byte[0];
                    return false;
                }

                OutputBuffer = numArray;
                return true;
            }
        }

        public unsafe bool ReadBinary(out sbyte[] OutputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                uint Count;
                GetGPIBBlockLength(out Count);
                if (Count <= 0U)
                {
                    OutputBuffer = new sbyte[0];
                    return false;
                }

                var numArray = new sbyte[Count];
                try
                {
                    fixed (sbyte* pOutData1 = numArray)
                    {
                        uint ReturnCount;
                        var status = viReadVoid(InstrSession, pOutData1, Count, out ReturnCount);
                        if (IsError(status))
                        {
                            OutputBuffer = new sbyte[0];
                            OnErrorHandler(status);
                            return false;
                        }

                        Debug.Assert((int)Count == (int)ReturnCount);
                        if ((int)Count != (int)ReturnCount)
                        {
                            OutputBuffer = new sbyte[0];
                            OnErrorHandler(TekVISADefs.Status.ERROR_SYSTEM_ERROR);
                            return false;
                        }

                        if (status == TekVISADefs.Status.SUCCESS_MAX_CNT)
                        {
                            var timeout = Timeout;
                            Timeout = 0U;
                            uint rcount;
                            fixed (byte* pOutData2 = new byte[256])
                            {
                                var num = (int)viReadVoid(InstrSession, pOutData2, 256U, out rcount);
                            }

                            Timeout = timeout;
                            OnErrorHandler(TekVISADefs.Status.SUCCESS);
                        }
                        else
                        {
                            OnErrorHandler(status);
                        }
                    }
                }
                catch
                {
                    OutputBuffer = new sbyte[0];
                    return false;
                }

                OutputBuffer = numArray;
                return true;
            }
        }

        public unsafe bool ReadBinary(out short[] OutputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                uint Count;
                GetGPIBBlockLength(out Count);
                if (Count <= 0U)
                {
                    OutputBuffer = new short[0];
                    return false;
                }

                var numArray1 = new short[0];
                short[] numArray2;
                try
                {
                    numArray2 = new short[Count / 2U];
                    fixed (short* pOutData1 = numArray2)
                    {
                        uint ReturnCount;
                        var status = viReadVoid(InstrSession, pOutData1, (uint)(numArray2.Length * 2), out ReturnCount);
                        if (IsError(status))
                        {
                            OutputBuffer = new short[0];
                            OnErrorHandler(status);
                            return false;
                        }

                        Debug.Assert(ReturnCount == numArray2.Length * 2);
                        if ((int)Count != (int)(ReturnCount / 2U))
                        {
                            OutputBuffer = new short[0];
                            OnErrorHandler(TekVISADefs.Status.ERROR_SYSTEM_ERROR);
                            return false;
                        }

                        if (status == TekVISADefs.Status.SUCCESS_MAX_CNT)
                        {
                            var timeout = Timeout;
                            Timeout = 0U;
                            uint rcount;
                            fixed (byte* pOutData2 = new byte[256])
                            {
                                var num = (int)viReadVoid(InstrSession, pOutData2, 256U, out rcount);
                            }

                            Timeout = timeout;
                            OnErrorHandler(TekVISADefs.Status.SUCCESS);
                        }
                        else
                        {
                            OnErrorHandler(status);
                        }
                    }
                }
                catch
                {
                    OutputBuffer = new short[0];
                    return false;
                }

                OutputBuffer = numArray2;
                return true;
            }
        }

        public unsafe bool ReadBinary(out ushort[] OutputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                uint Count;
                GetGPIBBlockLength(out Count);
                if (Count <= 0U)
                {
                    OutputBuffer = new ushort[0];
                    return false;
                }

                var numArray1 = new ushort[0];
                ushort[] numArray2;
                try
                {
                    numArray2 = new ushort[Count / 2U];
                    fixed (ushort* pOutData1 = numArray2)
                    {
                        uint ReturnCount;
                        var status = viReadVoid(InstrSession, pOutData1, (uint)(numArray2.Length * 2), out ReturnCount);
                        if (IsError(status))
                        {
                            OutputBuffer = new ushort[0];
                            OnErrorHandler(status);
                            return false;
                        }

                        Debug.Assert(ReturnCount == numArray2.Length * 2);
                        if ((int)Count != (int)(ReturnCount / 2U))
                        {
                            OutputBuffer = new ushort[0];
                            OnErrorHandler(TekVISADefs.Status.ERROR_SYSTEM_ERROR);
                            return false;
                        }

                        if (status == TekVISADefs.Status.SUCCESS_MAX_CNT)
                        {
                            var timeout = Timeout;
                            Timeout = 0U;
                            uint rcount;
                            fixed (byte* pOutData2 = new byte[256])
                            {
                                var num = (int)viReadVoid(InstrSession, pOutData2, 256U, out rcount);
                            }

                            Timeout = timeout;
                            OnErrorHandler(TekVISADefs.Status.SUCCESS);
                        }
                        else
                        {
                            OnErrorHandler(status);
                        }
                    }
                }
                catch
                {
                    OutputBuffer = new ushort[0];
                    return false;
                }

                OutputBuffer = numArray2;
                return true;
            }
        }

        public unsafe bool ReadBinary(out int[] OutputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                uint Count;
                GetGPIBBlockLength(out Count);
                if (Count <= 0U)
                {
                    OutputBuffer = new int[0];
                    return false;
                }

                int[] numArray;
                try
                {
                    numArray = new int[Count / 4U];
                    fixed (int* pOutData1 = numArray)
                    {
                        uint ReturnCount;
                        var status = viReadVoid(InstrSession, pOutData1, (uint)(numArray.Length * 4), out ReturnCount);
                        if (IsError(status))
                        {
                            OutputBuffer = new int[0];
                            OnErrorHandler(status);
                            return false;
                        }

                        Debug.Assert(ReturnCount == numArray.Length * 4);
                        if ((int)Count != (int)(ReturnCount / 4U))
                        {
                            OutputBuffer = new int[0];
                            OnErrorHandler(TekVISADefs.Status.ERROR_SYSTEM_ERROR);
                            return false;
                        }

                        if (status == TekVISADefs.Status.SUCCESS_MAX_CNT)
                        {
                            var timeout = Timeout;
                            Timeout = 0U;
                            uint rcount;
                            fixed (byte* pOutData2 = new byte[256])
                            {
                                var num = (int)viReadVoid(InstrSession, pOutData2, 256U, out rcount);
                            }

                            Timeout = timeout;
                            OnErrorHandler(TekVISADefs.Status.SUCCESS);
                        }
                        else
                        {
                            OnErrorHandler(status);
                        }
                    }
                }
                catch
                {
                    OutputBuffer = new int[0];
                    return false;
                }

                OutputBuffer = numArray;
                return true;
            }
        }

        public unsafe bool ReadBinary(out uint[] OutputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                uint Count;
                GetGPIBBlockLength(out Count);
                if (Count <= 0U)
                {
                    OutputBuffer = new uint[0];
                    return false;
                }

                var numArray1 = new uint[0];
                uint[] numArray2;
                try
                {
                    numArray2 = new uint[Count / 4U];
                    fixed (uint* pOutData1 = numArray2)
                    {
                        uint ReturnCount;
                        var status = viReadVoid(InstrSession, pOutData1, (uint)(numArray2.Length * 4), out ReturnCount);
                        if (IsError(status))
                        {
                            OutputBuffer = new uint[0];
                            OnErrorHandler(status);
                            return false;
                        }

                        Debug.Assert(numArray2.Length == ReturnCount / 4U);
                        if ((int)Count != (int)(ReturnCount / 4U))
                        {
                            OutputBuffer = new uint[0];
                            OnErrorHandler(TekVISADefs.Status.ERROR_SYSTEM_ERROR);
                            return false;
                        }

                        if (status == TekVISADefs.Status.SUCCESS_MAX_CNT)
                        {
                            var timeout = Timeout;
                            Timeout = 0U;
                            uint rcount;
                            fixed (byte* pOutData2 = new byte[256])
                            {
                                var num = (int)viReadVoid(InstrSession, pOutData2, 256U, out rcount);
                            }

                            Timeout = timeout;
                            OnErrorHandler(TekVISADefs.Status.SUCCESS);
                        }
                        else
                        {
                            OnErrorHandler(status);
                        }
                    }
                }
                catch
                {
                    OutputBuffer = new uint[0];
                    return false;
                }

                OutputBuffer = numArray2;
                return true;
            }
        }

        public unsafe bool ReadBinary(out float[] OutputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                uint Count;
                GetGPIBBlockLength(out Count);
                if (Count <= 0U)
                {
                    OutputBuffer = new float[0];
                    return false;
                }

                var numArray1 = new float[0];
                float[] numArray2;
                try
                {
                    numArray2 = new float[Count / 4U];
                    fixed (float* pOutData1 = numArray2)
                    {
                        uint ReturnCount;
                        var status = viReadVoid(InstrSession, pOutData1, (uint)(numArray2.Length * 4), out ReturnCount);
                        if (IsError(status))
                        {
                            OutputBuffer = new float[0];
                            OnErrorHandler(status);
                            return false;
                        }

                        Debug.Assert(numArray2.Length == ReturnCount / 4U);
                        if ((int)Count != (int)(ReturnCount / 4U))
                        {
                            OutputBuffer = new float[0];
                            OnErrorHandler(TekVISADefs.Status.ERROR_SYSTEM_ERROR);
                            return false;
                        }

                        if (status == TekVISADefs.Status.SUCCESS_MAX_CNT)
                        {
                            var timeout = Timeout;
                            Timeout = 0U;
                            uint rcount;
                            fixed (byte* pOutData2 = new byte[256])
                            {
                                var num = (int)viReadVoid(InstrSession, pOutData2, 256U, out rcount);
                            }

                            Timeout = timeout;
                            OnErrorHandler(TekVISADefs.Status.SUCCESS);
                        }
                        else
                        {
                            OnErrorHandler(status);
                        }
                    }
                }
                catch
                {
                    OutputBuffer = new float[0];
                    return false;
                }

                OutputBuffer = numArray2;
                return true;
            }
        }

        public bool Query(string InputBuffer, out int v)
        {
            v = 0;
            var OutputBuffer = "";
            if (Query(InputBuffer, out OutputBuffer))
                try
                {
                    v = int.Parse(OutputBuffer);
                    return true;
                }
                catch
                {
                }

            return false;
        }

        public bool Query(string InputBuffer, out float v)
        {
            v = 0.0f;
            var OutputBuffer = "";
            if (Query(InputBuffer, out OutputBuffer))
                try
                {
                    v = float.Parse(OutputBuffer);
                    return true;
                }
                catch
                {
                }

            return false;
        }

        public bool Query(string InputBuffer, out double v)
        {
            v = 0.0;
            var OutputBuffer = "";
            if (Query(InputBuffer, out OutputBuffer))
                try
                {
                    v = double.Parse(OutputBuffer);
                    return true;
                }
                catch
                {
                }

            return false;
        }

        public bool Query(string InputBuffer, out long v)
        {
            v = 0L;
            var OutputBuffer = "";
            if (Query(InputBuffer, out OutputBuffer))
                try
                {
                    v = long.Parse(OutputBuffer);
                    return true;
                }
                catch
                {
                }

            return false;
        }

        public bool Query(string InputBuffer, out string OutputBuffer)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            lock (this)
            {
                OutputBuffer = "";

                while (viLock(InstrSession, TekVISADefs.AccessModes.VI_EXCLUSIVE_LOCK, TimeoutRM,
                           null, null) == TekVISADefs.Status.ERROR_RSRC_LOCKED)
                    ;
                try
                {
                    if (Write(InputBuffer))
                        return Read(out OutputBuffer);
                }
                finally
                {
                    var num = (int)viUnlock(InstrSession);
                }

                return false;
            }
        }

        public bool Write(string InputBuffer, ref uint ReturnCount)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                return !OnErrorHandler(viWrite(InstrSession, InputBuffer, (uint)InputBuffer.Length, out ReturnCount));
            }
        }

        public bool Write(string InputBuffer)
        {
            lock (this)
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                var ReturnCount = (uint)InputBuffer.Length;
                return !OnErrorHandler(viWrite(InstrSession, InputBuffer, ReturnCount, out ReturnCount));
            }
        }

        public bool AssertTrigger()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viAssertTrigger(InstrSession, TekVISADefs.Definitions.LOCAL_SPACE));
        }

        public bool Clear()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viClear(InstrSession));
        }

        public bool ReadSTB(out ushort Status)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viReadSTB(InstrSession, out Status));
        }

        private bool FindRsrc(
            string FindString,
            out uint Session,
            out uint ReturnCount,
            out string OutputBuffer)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            var ResourceArray = new StringBuilder(128, 1024);
            if (OnErrorHandler(viFindRsrc(RMSession, FindString, out Session, out ReturnCount, ResourceArray)))
            {
                OutputBuffer = "";
                return false;
            }

            OutputBuffer = ResourceArray.ToString();
            return true;
        }

        private bool FindNext(uint Session, out string Resource)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            var Resource1 = new StringBuilder(128, 1024);
            if (!OnErrorHandler(viFindNext(Session, Resource1)))
            {
                Resource = Resource1.ToString();
                return true;
            }

            Resource = "";
            return false;
        }

        public bool FindResources(string FindString, out ArrayList FindList)
        {
            lock (this)
            {
                uint ReturnCount = 0;
                uint Session = 0;
                if (IsDisposed)
                    throw new ObjectDisposedException(nameof(VISA));
                FindList = new ArrayList();
                string str;
                if (!FindRsrc(FindString, out Session, out ReturnCount, out str))
                    return false;
                FindList.Add(str);
                for (var index = 1; index < ReturnCount; ++index)
                    if (FindNext(Session, out str))
                        FindList.Add(str);
                return true;
            }
        }

        public bool EnableEvent(TekVISADefs.EventTypes EventType, TekVISADefs.EventMechanism Mechanism)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viEnableEvent(InstrSession, EventType, Mechanism, 0U));
        }

        public bool DisableEvent(TekVISADefs.EventTypes EventType, TekVISADefs.EventMechanism Mechanism)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viDisableEvent(InstrSession, EventType, Mechanism));
        }

        public bool DiscardEvents(
            TekVISADefs.EventTypes EventType,
            TekVISADefs.EventMechanism Mechanism)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viDiscardEvents(InstrSession, EventType, Mechanism));
        }

        public bool WaitOnEvent(
            TekVISADefs.EventTypes EventType,
            uint TimeoutValue,
            out TekVISADefs.EventTypes EventTypeOut,
            out uint EventOut)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return !OnErrorHandler(viWaitOnEvent(InstrSession, EventType, TimeoutValue, out EventTypeOut,
                out EventOut));
        }

        public bool InstallHandler(
            TekVISADefs.EventTypes EventType,
            EventHandlerProc Handler,
            uint UserHandle)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            var eventHandlerBlock = new EventHandlerBlock(EventType, Handler, UserHandle);
            switch (EventType)
            {
                case TekVISADefs.EventTypes.EVENT_TRIG:
                    EventHandlerTrigBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_VXI_VME_INTR:
                    EventHandlerVXIVMEIntrBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_IO_COMPLETION:
                    EventHandlerIOCompletionBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_SERVICE_REQ:
                    EventHandlerServiceReqBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_CLEAR:
                    EventHandlerClearBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_GPIB_CIC:
                    EventHandlerGPIBCICBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_GPIB_TALK:
                    EventHandlerGPIBTalkBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_GPIB_LISTEN:
                    EventHandlerGPIBListenBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_VXI_VME_SYSFAIL:
                    EventHandlerVXIVMESysFailBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_VXI_VME_SYSRESET:
                    EventHandlerVXIVMESysResetBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_VXI_SIGP:
                    EventHandlerVXISigPBlocks.Add(eventHandlerBlock);
                    break;
                case TekVISADefs.EventTypes.EVENT_TCPIP_CONNECT:
                    EventHandlerTCPIPConnectBlocks.Add(eventHandlerBlock);
                    break;
                default:
                    EventHandlerExceptionBlocks.Add(eventHandlerBlock);
                    break;
            }

            return !OnErrorHandler(viInstallHandler(InstrSession, EventType, MainEventHandlerProc, UserHandle));
        }

        private bool UnInstallEventHandler(
            ref ArrayList EventHandlerBlocks,
            TekVISADefs.EventTypes EventType,
            EventHandlerProc Handler,
            uint UserHandle)
        {
            foreach (EventHandlerBlock eventHandlerBlock in EventHandlerBlocks)
                if ((int)eventHandlerBlock.UserHandle == (int)UserHandle)
                {
                    eventHandlerBlock.Handler -= Handler;
                    EventHandlerBlocks.Remove(eventHandlerBlock);
                    return !OnErrorHandler(
                        viUnInstallHandler(InstrSession, EventType, MainEventHandlerProc, UserHandle));
                }

            return false;
        }

        public bool UnInstallHandler(
            TekVISADefs.EventTypes EventType,
            EventHandlerProc Handler,
            uint UserHandle)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            switch (EventType)
            {
                case TekVISADefs.EventTypes.EVENT_TRIG:
                    return UnInstallEventHandler(ref EventHandlerTrigBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_VXI_VME_INTR:
                    return UnInstallEventHandler(ref EventHandlerVXIVMEIntrBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_IO_COMPLETION:
                    return UnInstallEventHandler(ref EventHandlerIOCompletionBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_SERVICE_REQ:
                    return UnInstallEventHandler(ref EventHandlerServiceReqBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_CLEAR:
                    return UnInstallEventHandler(ref EventHandlerClearBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_GPIB_CIC:
                    return UnInstallEventHandler(ref EventHandlerGPIBCICBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_GPIB_TALK:
                    return UnInstallEventHandler(ref EventHandlerGPIBTalkBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_GPIB_LISTEN:
                    return UnInstallEventHandler(ref EventHandlerGPIBListenBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_VXI_VME_SYSFAIL:
                    return UnInstallEventHandler(ref EventHandlerVXIVMESysFailBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_VXI_VME_SYSRESET:
                    return UnInstallEventHandler(ref EventHandlerVXIVMESysResetBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_VXI_SIGP:
                    return UnInstallEventHandler(ref EventHandlerVXISigPBlocks, EventType, Handler, UserHandle);
                case TekVISADefs.EventTypes.EVENT_TCPIP_CONNECT:
                    return UnInstallEventHandler(ref EventHandlerTCPIPConnectBlocks, EventType, Handler, UserHandle);
                default:
                    return UnInstallEventHandler(ref EventHandlerExceptionBlocks, EventType, Handler, UserHandle);
            }
        }

        private void CallEventHandler(
            ArrayList CurrentEventHandlerBlocks,
            uint Context,
            uint UserHandle)
        {
            foreach (EventHandlerBlock eventHandlerBlock in CurrentEventHandlerBlocks)
                if ((int)UserHandle == (int)eventHandlerBlock.UserHandle && null != eventHandlerBlock.Handler)
                    eventHandlerBlock.Handler(this,
                        new TekVISAEventArgs(eventHandlerBlock.EventType, Context, UserHandle));
        }

        public virtual void EventsHandler(
            uint vi,
            TekVISADefs.EventTypes EventType,
            uint Context,
            uint UserHandle)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            switch (EventType)
            {
                case TekVISADefs.EventTypes.EVENT_TRIG:
                    CallEventHandler(EventHandlerTrigBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_VXI_VME_INTR:
                    CallEventHandler(EventHandlerVXIVMEIntrBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_IO_COMPLETION:
                    CallEventHandler(EventHandlerIOCompletionBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_SERVICE_REQ:
                    CallEventHandler(EventHandlerServiceReqBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_CLEAR:
                    CallEventHandler(EventHandlerClearBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_GPIB_CIC:
                    CallEventHandler(EventHandlerGPIBCICBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_GPIB_TALK:
                    CallEventHandler(EventHandlerGPIBTalkBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_GPIB_LISTEN:
                    CallEventHandler(EventHandlerGPIBListenBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_VXI_VME_SYSFAIL:
                    CallEventHandler(EventHandlerVXIVMESysFailBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_VXI_VME_SYSRESET:
                    CallEventHandler(EventHandlerVXIVMESysResetBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_VXI_SIGP:
                    CallEventHandler(EventHandlerVXISigPBlocks, Context, UserHandle);
                    break;
                case TekVISADefs.EventTypes.EVENT_TCPIP_CONNECT:
                    CallEventHandler(EventHandlerTCPIPConnectBlocks, Context, UserHandle);
                    break;
                default:
                    CallEventHandler(EventHandlerExceptionBlocks, Context, UserHandle);
                    break;
            }
        }

        public bool Open(string Resource, TekVISADefs.AccessModes AccessMode, uint TimeoutValue)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            lock (this)
            {
                ResourceString = Resource;
                if (InstrSession != 0U)
                {
                    var num = (int)viClose(InstrSession);
                    InstrSession = 0U;
                }

                if (OnErrorHandler(viOpen(RMSession, Resource, AccessMode, TimeoutValue, out InstrSession)))
                    return false;
                TimeoutRM = TimeoutValue;
                return true;
            }
        }

        public bool Open(
            string Resource,
            TekVISADefs.AccessModes AccessMode,
            uint TimeoutValue,
            int Retries)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            lock (this)
            {
                while (0 <= Retries--)
                {
                    if (Open(Resource, AccessMode, TimeoutValue))
                        return true;
                    Thread.Sleep(10);
                }

                return false;
            }
        }

        public bool Open(
            string Resource,
            TekVISADefs.AccessModes AccessMode,
            uint TimeoutValue,
            bool ThrowOnError,
            OnErrorProc ErrProc)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            OnError("Open " + Resource, ThrowOnError, ErrProc);
            return Open(Resource, AccessMode, TimeoutValue);
        }

        public bool Open(
            string Resource,
            TekVISADefs.AccessModes AccessMode,
            uint TimeoutValue,
            int Retries,
            bool ThrowOnError,
            OnErrorProc ErrProc)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            OnError("Open " + Resource, ThrowOnError, ErrProc);
            return Open(Resource, AccessMode, TimeoutValue, Retries);
        }

        public bool Open(string Resource)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(VISA));
            return Open(Resource, TekVISADefs.AccessModes.VI_NO_LOCK, TimeoutRM, 1);
        }

        internal delegate void EventHandlerProcs(
            uint vi,
            TekVISADefs.EventTypes EventType,
            uint Context,
            uint UserHandle);

        private class EventHandlerBlock
        {
            public EventHandlerBlock(
                TekVISADefs.EventTypes NewEventType,
                EventHandlerProc NewEventHandler,
                uint NewUserHandle)
            {
                EventType = NewEventType;
                Handler += NewEventHandler;
                Count = 1;
                UserHandle = NewUserHandle;
            }

            public TekVISADefs.EventTypes EventType { get; }

            public EventHandlerProc Handler { get; set; }

            public uint UserHandle { get; }

            public int Count { get; set; }

            public void AddEventHandler(EventHandlerProc NewEventHandler)
            {
                Handler += NewEventHandler;
                ++Count;
            }

            public void DeleteEventHandler(EventHandlerProc OldEventHandler)
            {
                Handler -= OldEventHandler;
            }
        }
    }
}