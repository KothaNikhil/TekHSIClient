using System;
using System.Collections.Generic;

namespace TekHighspeedAPI
{
    public enum Attributes : uint
    {
        RSRC_IMPL_VERSION = 1073676291, // 0x3FFF0003
        RSRC_LOCK_STATE = 1073676292, // 0x3FFF0004
        MAX_QUEUE_LENGTH = 1073676293, // 0x3FFF0005
        USER_DATA = 1073676295, // 0x3FFF0007
        FDC_CHNL = 1073676301, // 0x3FFF000D
        FDC_MODE = 1073676303, // 0x3FFF000F
        FDC_GEN_SIGNAL_EN = 1073676305, // 0x3FFF0011
        FDC_USE_PAIR = 1073676307, // 0x3FFF0013
        SEND_END_EN = 1073676310, // 0x3FFF0016
        TERMCHAR = 1073676312, // 0x3FFF0018
        TMO_VALUE = 1073676314, // 0x3FFF001A
        GPIB_READDR_EN = 1073676315, // 0x3FFF001B
        IO_PROT = 1073676316, // 0x3FFF001C
        DMA_ALLOW_EN = 1073676318, // 0x3FFF001E
        ASRL_BAUD = 1073676321, // 0x3FFF0021
        ASRL_DATA_BITS = 1073676322, // 0x3FFF0022
        ASRL_PARITY = 1073676323, // 0x3FFF0023
        ASRL_STOP_BITS = 1073676324, // 0x3FFF0024
        ASRL_FLOW_CNTRL = 1073676325, // 0x3FFF0025
        RD_BUF_OPER_MODE = 1073676330, // 0x3FFF002A
        WR_BUF_OPER_MODE = 1073676333, // 0x3FFF002D
        SUPPRESS_END_EN = 1073676342, // 0x3FFF0036
        TERMCHAR_EN = 1073676344, // 0x3FFF0038
        DEST_ACCESS_PRIV = 1073676345, // 0x3FFF0039
        DEST_BYTE_ORDER = 1073676346, // 0x3FFF003A
        SRC_ACCESS_PRIV = 1073676348, // 0x3FFF003C
        SRC_BYTE_ORDER = 1073676349, // 0x3FFF003D
        SRC_INCREMENT = 1073676352, // 0x3FFF0040
        DEST_INCREMENT = 1073676353, // 0x3FFF0041
        WIN_ACCESS_PRIV = 1073676357, // 0x3FFF0045
        WIN_BYTE_ORDER = 1073676359, // 0x3FFF0047
        GPIB_ATN_STATE = 1073676375, // 0x3FFF0057
        GPIB_ADDR_STATE = 1073676380, // 0x3FFF005C
        GPIB_CIC_STATE = 1073676382, // 0x3FFF005E
        GPIB_NDAC_STATE = 1073676386, // 0x3FFF0062
        GPIB_SRQ_STATE = 1073676391, // 0x3FFF0067
        GPIB_SYS_CNTRL_STATE = 1073676392, // 0x3FFF0068
        GPIB_HS488_CBL_LEN = 1073676393, // 0x3FFF0069
        CMDR_LA = 1073676395, // 0x3FFF006B
        VXI_DEV_CLASS = 1073676396, // 0x3FFF006C
        MAINFRAME_LA = 1073676400, // 0x3FFF0070
        VXI_VME_INTR_STATUS = 1073676427, // 0x3FFF008B
        VXI_TRIG_STATUS = 1073676429, // 0x3FFF008D
        VXI_VME_SYSFAIL_STATE = 1073676436, // 0x3FFF0094
        WIN_BASE_ADDR = 1073676440, // 0x3FFF0098
        WIN_SIZE = 1073676442, // 0x3FFF009A
        ASRL_AVAIL_NUM = 1073676460, // 0x3FFF00AC
        MEM_BASE = 1073676461, // 0x3FFF00AD
        ASRL_CTS_STATE = 1073676462, // 0x3FFF00AE
        ASRL_DCD_STATE = 1073676463, // 0x3FFF00AF
        ASRL_DSR_STATE = 1073676465, // 0x3FFF00B1
        ASRL_DTR_STATE = 1073676466, // 0x3FFF00B2
        ASRL_END_IN = 1073676467, // 0x3FFF00B3
        ASRL_END_OUT = 1073676468, // 0x3FFF00B4
        ASRL_REPLACE_CHAR = 1073676478, // 0x3FFF00BE
        ASRL_RI_STATE = 1073676479, // 0x3FFF00BF
        ASRL_RTS_STATE = 1073676480, // 0x3FFF00C0
        ASRL_XON_CHAR = 1073676481, // 0x3FFF00C1
        ASRL_XOFF_CHAR = 1073676482, // 0x3FFF00C2
        WIN_ACCESS = 1073676483, // 0x3FFF00C3
        RM_SESSION = 1073676484, // 0x3FFF00C4
        VXI_LA = 1073676501, // 0x3FFF00D5
        MANF_ID = 1073676505, // 0x3FFF00D9
        MEM_SIZE = 1073676509, // 0x3FFF00DD
        MEM_SPACE = 1073676510, // 0x3FFF00DE
        MODEL_CODE = 1073676511, // 0x3FFF00DF
        SLOT = 1073676520, // 0x3FFF00E8
        IMMEDIATE_SERV = 1073676544, // 0x3FFF0100
        INTF_PARENT_NUM = 1073676545, // 0x3FFF0101
        RSRC_SPEC_VERSION = 1073676656, // 0x3FFF0170
        INTF_TYPE = 1073676657, // 0x3FFF0171
        GPIB_PRIMARY_ADDR = 1073676658, // 0x3FFF0172
        GPIB_SECONDARY_ADDR = 1073676659, // 0x3FFF0173
        RSRC_MANF_ID = 1073676661, // 0x3FFF0175
        INTF_NUM = 1073676662, // 0x3FFF0176
        TRIG_ID = 1073676663, // 0x3FFF0177
        GPIB_REN_STATE = 1073676673, // 0x3FFF0181
        GPIB_UNADDR_EN = 1073676676, // 0x3FFF0184
        DEV_STATUS_BYTE = 1073676681, // 0x3FFF0189
        FILE_APPEND_EN = 1073676690, // 0x3FFF0192
        VXI_TRIG_SUPPORT = 1073676692, // 0x3FFF0194
        TCPIP_PORT = 1073676695, // 0x3FFF0197
        TCPIP_NODELAY = 1073676698, // 0x3FFF019A
        TCPIP_KEEPALIVE = 1073676699, // 0x3FFF019B
        USB_INTFC_NUM = 1073676705, // 0x3FFF01A1
        USB_PROTOCOL = 1073676711, // 0x3FFF01A7
        USB_MAX_INTR_SIZE = 1073676719, // 0x3FFF01AF
        JOB_ID = 1073692678, // 0x3FFF4006
        EVENT_TYPE = 1073692688, // 0x3FFF4010
        SIGP_STATUS_ID = 1073692689, // 0x3FFF4011
        RECV_TRIG_ID = 1073692690, // 0x3FFF4012
        INTR_STATUS_ID = 1073692707, // 0x3FFF4023
        STATUS = 1073692709, // 0x3FFF4025
        RET_COUNT = 1073692710, // 0x3FFF4026
        BUFFER = 1073692711, // 0x3FFF4027
        RECV_INTR_LEVEL = 1073692737, // 0x3FFF4041
        GPIB_RECV_CIC_STATE = 1073693075, // 0x3FFF4193
        USB_RECV_INTR_SIZE = 1073693104, // 0x3FFF41B0
        RSRC_CLASS = 3221159937, // 0xBFFF0001
        RSRC_NAME = 3221159938, // 0xBFFF0002
        MANF_NAME = 3221160050, // 0xBFFF0072
        MODEL_NAME = 3221160055, // 0xBFFF0077
        INTF_INST_NAME = 3221160169, // 0xBFFF00E9
        RSRC_MANF_NAME = 3221160308, // 0xBFFF0174
        TCPIP_ADDR = 3221160341, // 0xBFFF0195
        TCPIP_HOSTNAME = 3221160342, // 0xBFFF0196
        TCPIP_DEVICE_NAME = 3221160345, // 0xBFFF0199
        USB_SERIAL_NUM = 3221160352, // 0xBFFF01A0
        OPER_NAME = 3221176386, // 0xBFFF4042
        RECV_TCPIP_ADDR = 3221176728, // 0xBFFF4198
        USB_RECV_INTR_DATA = 3221176753, // 0xBFFF41B1
    }

    public enum Status : int
    {
        ERROR_SYSTEM_ERROR = -1073807360, // 0xBFFF0000
        ERROR_INV_OBJECT = -1073807346, // 0xBFFF000E
        ERROR_RSRC_LOCKED = -1073807345, // 0xBFFF000F
        ERROR_INV_EXPR = -1073807344, // 0xBFFF0010
        ERROR_RSRC_NFOUND = -1073807343, // 0xBFFF0011
        ERROR_INV_RSRC_NAME = -1073807342, // 0xBFFF0012
        ERROR_INV_ACC_MODE = -1073807341, // 0xBFFF0013
        ERROR_TMO = -1073807339, // 0xBFFF0015
        ERROR_CLOSING_FAILED = -1073807338, // 0xBFFF0016
        ERROR_INV_DEGREE = -1073807333, // 0xBFFF001B
        ERROR_INV_JOB_ID = -1073807332, // 0xBFFF001C
        ERROR_NSUP_ATTR = -1073807331, // 0xBFFF001D
        ERROR_NSUP_ATTR_STATE = -1073807330, // 0xBFFF001E
        ERROR_ATTR_READONLY = -1073807329, // 0xBFFF001F
        ERROR_INV_LOCK_TYPE = -1073807328, // 0xBFFF0020
        ERROR_INV_ACCESS_KEY = -1073807327, // 0xBFFF0021
        ERROR_INV_EVENT = -1073807322, // 0xBFFF0026
        ERROR_INV_MECH = -1073807321, // 0xBFFF0027
        ERROR_HNDLR_NINSTALLED = -1073807320, // 0xBFFF0028
        ERROR_INV_HNDLR_REF = -1073807319, // 0xBFFF0029
        ERROR_INV_CONTEXT = -1073807318, // 0xBFFF002A
        ERROR_QUEUE_OVERFLOW = -1073807315, // 0xBFFF002D
        ERROR_NENABLED = -1073807313, // 0xBFFF002F
        ERROR_ABORT = -1073807312, // 0xBFFF0030
        ERROR_RAW_WR_PROT_VIOL = -1073807308, // 0xBFFF0034
        ERROR_RAW_RD_PROT_VIOL = -1073807307, // 0xBFFF0035
        ERROR_OUTP_PROT_VIOL = -1073807306, // 0xBFFF0036
        ERROR_INP_PROT_VIOL = -1073807305, // 0xBFFF0037
        ERROR_BERR = -1073807304, // 0xBFFF0038
        ERROR_IN_PROGRESS = -1073807303, // 0xBFFF0039
        ERROR_INV_SETUP = -1073807302, // 0xBFFF003A
        ERROR_QUEUE_ERROR = -1073807301, // 0xBFFF003B
        ERROR_ALLOC = -1073807300, // 0xBFFF003C
        ERROR_INV_MASK = -1073807299, // 0xBFFF003D
        ERROR_IO = -1073807298, // 0xBFFF003E
        ERROR_INV_FMT = -1073807297, // 0xBFFF003F
        ERROR_NSUP_FMT = -1073807295, // 0xBFFF0041
        ERROR_LINE_IN_USE = -1073807294, // 0xBFFF0042
        ERROR_NSUP_MODE = -1073807290, // 0xBFFF0046
        ERROR_SRQ_NOCCURRED = -1073807286, // 0xBFFF004A
        ERROR_INV_SPACE = -1073807282, // 0xBFFF004E
        ERROR_INV_OFFSET = -1073807279, // 0xBFFF0051
        ERROR_INV_WIDTH = -1073807278, // 0xBFFF0052
        ERROR_NSUP_OFFSET = -1073807276, // 0xBFFF0054
        ERROR_NSUP_VAR_WIDTH = -1073807275, // 0xBFFF0055
        ERROR_WINDOW_NMAPPED = -1073807273, // 0xBFFF0057
        ERROR_RESP_PENDING = -1073807271, // 0xBFFF0059
        ERROR_NLISTENERS = -1073807265, // 0xBFFF005F
        ERROR_NCIC = -1073807264, // 0xBFFF0060
        ERROR_NSYS_CNTLR = -1073807263, // 0xBFFF0061
        ERROR_NSUP_OPER = -1073807257, // 0xBFFF0067
        ERROR_INTR_PENDING = -1073807256, // 0xBFFF0068
        ERROR_ASRL_PARITY = -1073807254, // 0xBFFF006A
        ERROR_ASRL_FRAMING = -1073807253, // 0xBFFF006B
        ERROR_ASRL_OVERRUN = -1073807252, // 0xBFFF006C
        ERROR_TRIG_NMAPPED = -1073807250, // 0xBFFF006E
        ERROR_NSUP_ALIGN_OFFSET = -1073807248, // 0xBFFF0070
        ERROR_USER_BUF = -1073807247, // 0xBFFF0071
        ERROR_RSRC_BUSY = -1073807246, // 0xBFFF0072
        ERROR_NSUP_WIDTH = -1073807242, // 0xBFFF0076
        ERROR_INV_PARAMETER = -1073807240, // 0xBFFF0078
        ERROR_INV_PROT = -1073807239, // 0xBFFF0079
        ERROR_INV_SIZE = -1073807237, // 0xBFFF007B
        ERROR_WINDOW_MAPPED = -1073807232, // 0xBFFF0080
        ERROR_NIMPL_OPER = -1073807231, // 0xBFFF0081
        ERROR_INV_LENGTH = -1073807229, // 0xBFFF0083
        ERROR_INV_MODE = -1073807215, // 0xBFFF0091
        ERROR_SESN_NLOCKED = -1073807204, // 0xBFFF009C
        ERROR_MEM_NSHARED = -1073807203, // 0xBFFF009D
        ERROR_LIBRARY_NFOUND = -1073807202, // 0xBFFF009E
        ERROR_NSUP_INTR = -1073807201, // 0xBFFF009F
        ERROR_INV_LINE = -1073807200, // 0xBFFF00A0
        ERROR_FILE_ACCESS = -1073807199, // 0xBFFF00A1
        ERROR_FILE_IO = -1073807198, // 0xBFFF00A2
        ERROR_NSUP_LINE = -1073807197, // 0xBFFF00A3
        ERROR_NSUP_MECH = -1073807196, // 0xBFFF00A4
        ERROR_INTF_NUM_NCONFIG = -1073807195, // 0xBFFF00A5
        ERROR_CONN_LOST = -1073807194, // 0xBFFF00A6
        SUCCESS = 0,
        SUCCESS_EVENT_EN = 1073676290, // 0x3FFF0002
        SUCCESS_EVENT_DIS = 1073676291, // 0x3FFF0003
        SUCCESS_QUEUE_EMPTY = 1073676292, // 0x3FFF0004
        SUCCESS_TERM_CHAR = 1073676293, // 0x3FFF0005
        SUCCESS_MAX_CNT = 1073676294, // 0x3FFF0006
        WARN_CONFIG_NLOADED = 1073676407, // 0x3FFF0077
        SUCCESS_DEV_NPRESENT = 1073676413, // 0x3FFF007D
        SUCCESS_TRIG_MAPPED = 1073676414, // 0x3FFF007E
        SUCCESS_QUEUE_NEMPTY = 1073676416, // 0x3FFF0080
        WARN_NULL_OBJECT = 1073676418, // 0x3FFF0082
        WARN_NSUP_ATTR_STATE = 1073676420, // 0x3FFF0084
        WARN_UNKNOWN_STATUS = 1073676421, // 0x3FFF0085
        WARN_NSUP_BUF = 1073676424, // 0x3FFF0088
        SUCCESS_NCHAIN = 1073676440, // 0x3FFF0098
        SUCCESS_NESTED_SHARED = 1073676441, // 0x3FFF0099
        SUCCESS_NESTED_EXCLUSIVE = 1073676442, // 0x3FFF009A
        SUCCESS_SYNC = 1073676443, // 0x3FFF009B
    }

    /// <summary>
    /// 
    /// </summary>
    public interface ISCPIAccess
    {
        /// <summary>
        /// Visa connection
        /// </summary>
        /// <param name="clientname">name of client.</param>
        /// <returns></returns>
        (Status, Guid) Connect(string clientname);


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Status Disconnect();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Status Write(string  message);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        (Status, byte[]) ReadRawBinary();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        (Status, byte[]) Query(string message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        (Status, byte[]) QueryBinary(string message);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        (Status, Int32) ReadSTB();

        /// <summary>
        /// 
        /// </summary>
        Status Clear();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        (Status, int) GetTimeout();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        Status SetTimeout(int timeout);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        Status Lock(uint timeout = uint.MaxValue);

        /// <summary>
        /// 
        /// </summary>
        Status Unlock();
    }

    /// <summary>
    /// Interface to instrument. This is switched between either the ADC connection (live instrument)
    /// or the simulated connection. Nothing above this cares about the details of the underlying implimentation.
    /// </summary>
    public interface IDataAccess
    {
        /// <summary>
        /// Specifies the client name
        /// </summary>
        string ClientName { get; set; }

        /// <summary>
        /// Open a connection with the client name.
        /// </summary>
        /// <param name="clientname"></param>
        /// <returns></returns>
        bool Connect(string clientname);

        /// <summary>
        /// Disconnect from client
        /// </summary>
        /// <returns></returns>
        bool Disconnect();

        /// <summary>
        /// Wait for data to arrive
        /// </summary>
        /// <returns></returns>
        bool WaitForSequence();

        /// <summary>
        /// Indicate that data access is complete and the instrument can continue.
        /// The instrument holds off any updates until this is called,
        /// </summary>
        /// <param name="producedsomething"></param>
        /// <returns></returns>
        bool FinishedWithSequence(bool producedsomething);

        /// <summary>
        /// For a sequence to cycle. This is how you might get access to data
        /// after a connection on a stopped instrument.
        /// </summary>
        /// <returns></returns>
        bool StartSequence();

        /// <summary>
        /// Lightweight request for named data. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        object Open(string name);

        /// <summary>
        /// Return true if data is available.
        /// </summary>
        bool IsDataAvailable { get; }

        /// <summary>
        /// Returns true if connected to an instrument.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// AnyAcq ID
        /// </summary>
        ulong ID { get; }

        /// <summary>
        /// Names of available symbols. Though "available" has a flexible meaning between Riddick & Terminator.
        /// Available on Terminator means there is a slot in the datastore but it might not contain data. You need
        /// to check that.
        /// </summary>
        IEnumerable<string> Names { get; }

        /// <summary>
        /// A Watchdog looks for inactivity. When enabled, if the user pauses for too long during a datastore
        /// clients access, then the access is terminated.
        /// </summary>
        bool WatchDogEnabled { get; set; }

        /// <summary>
        /// Returns maximum inactivity time in milliseconds.
        /// </summary>
        int WatchDogTime { get; set; }
    }
}