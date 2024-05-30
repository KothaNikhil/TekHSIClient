//#define VERBOSE
//#define METRIC
//#define WEBLOGGING
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
//using Grpc.Net.Client;
using Tek.Scope;
using Tek.Scope.Support;
using Tekscope;
using static Tekscope.NativeData;
using INormalizedVector = Tek.Scope.Support.INormalizedVector;

namespace TekHighspeedAPI
{
    /// <summary>
    /// UpdateCondition - used as input for the WaitForData function.
    /// </summary>
    [Flags]
    public enum UpdateConditionType : ushort
    {
        /// <summary>
        /// Return immediately.
        /// </summary>
        Nonblocking = 0,

        /// <summary>
        /// Return on arrival of any acquisition.
        /// </summary>
        AnyAcq = 1,

        /// <summary>
        /// Return when vertical spacing has changed on any channel or math.
        /// </summary>
        VSpacingChange = 2,

        /// <summary>
        /// Return with horizontal spacing has changed on any channel or math.
        /// </summary>
        HSpacingChange = 4,

        /// <summary>
        /// Return when the record length changes.
        /// </summary>
        RecordLengthChange = 8,

        /// <summary>
        /// Return after an acquisition arrives later than the specified time.
        /// </summary>
        AfterTime = 16,

        /// <summary>
        /// Return on the next acquisition.
        /// </summary>
        Next = 32,

        /// <summary>
        /// Return when any channel or math has a horizontal change.
        /// </summary>
        HorizontalChange = RecordLengthChange | HSpacingChange,

        /// <summary>
        /// Returns when any channel or math has a vertical change.
        /// </summary>
        VerticalChange = VSpacingChange,

        /// <summary>
        /// Return on any change.
        /// </summary>
        AnyChange = 0xffff
    }
    
    public enum HSIScopeClientState
    {
        /// <summary>   An enum constant representing the disconnected option. </summary>
        Disconnected,

        /// <summary>   An enum constant representing the connected option. </summary>
        Connected,

        /// <summary>   An enum constant representing the waiting option. </summary>
        Waiting,

        /// <summary>   An enum constant representing the reading option. </summary>
        Reading
    }

    /// <summary>   A simple client connection. </summary>
    public class HSIClient : IDisposable
    {
        /// <summary>   (Immutable) name of the client. </summary>
        private string _clientName = Guid.NewGuid().ToString();

        private readonly ConDictionary<string, WaveformHeader> _headerPrevCache = new ConDictionary<string, WaveformHeader>();

        private readonly ConDictionary<string, WaveformHeader> _currentNativeCache = new ConDictionary<string, WaveformHeader>();
        private double _currentNativeTime = double.NaN;

        /// <summary>   The opencache. </summary>
        private readonly ConDictionary<string, object> _opencache = new ConDictionary<string, object>();

        /// <summary>   The channel. </summary>
        //private GrpcChannel _channel;
        private Channel _channel;

        /// <summary>   The connect. </summary>
        private Connect.ConnectClient _connect;

        /// <summary>   The cts. </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>   The native data client. </summary>
        private NativeDataClient _nativeDataClient;

        /// <summary>   The normalized data client. </summary>
        private NormalizedData.NormalizedDataClient _normalizedDataClient;

        /// <summary>   (Immutable) the symbols to forward. </summary>
        private readonly ConList<string> _symbolsToForward = new ConList<string>();

        /// <summary>
        /// 
        /// </summary>
        private long _acqCount = 0;

        /// <summary>
        /// Current acquisition count from beginning of connection. 
        /// </summary>
        public long AcqCount => Interlocked.Read(ref _acqCount);

        /// <summary>
        /// When true, the DataArrival Callback doesn't block (it happens in it's own task).
        /// </summary>
        ///
        /// <value> True if non blocking, false if not. </value>
        public bool NonBlocking { get; set; } = false;

        /// <summary>
        ///     Constructor that prevents a default instance of this class from being created.
        /// </summary>
        private HSIClient()
        {
        }

        private UpdateConditionType _updateConditionType = UpdateConditionType.AnyAcq;

        private long _lastAcqSeen = 0;
        private ConLockWrapper _lock = new ConLockWrapper();
        private ConLockWrapper _acqTimeLock = new ConLockWrapper();
        private double _acqTime = -1;


        /// <summary>
        /// Returns current UpdateCondition state.
        /// </summary>
        public UpdateConditionType UpdateCondition
        {
            get { return _updateConditionType; }
            set { _updateConditionType = value; }
        }

        /// <summary>
        /// This returns true if on acquisition arrival, the available symbols are read in parallel.
        /// This will often saturate the LAN connections, so setting this to true is probably being a
        /// little rude on a public network. However, on a private lab network (or when behind a switch) this
        /// will increase your transfer performance.
        /// </summary>
        public bool IsMultiThreadedRead { get; set; } = true;

        /// <summary>   Gets or sets the state. </summary>
        /// <value> The state. </value>
        public HSIScopeClientState State { get; set; } = HSIScopeClientState.Disconnected;

        /// <summary>   Gets or sets WebURL of the document. </summary>
        /// <value> The WebURL. </value>
        public string WebURL { get; set; }

        /// <summary>   Gets or sets the chunk size. </summary>
        /// <value> The size of the chunk. </value>
        public uint ChunkSize { get; set; } = 128000;

        /// <summary>   Gets the symbols. </summary>
        /// <value> The symbols. </value>
        public IEnumerable<string> Symbols
        {
            get
            {
                var symbolreply = _connect?.RequestAvailableNames(new ConnectRequest());
                if (symbolreply?.Status == ConnectStatus.Success)
                    foreach (var name in symbolreply.Symbolnames.Distinct())
                        yield return name;
            }
        }

        #region SymbolsToReturn
        /// <summary>
        /// Sets the symbols to be read out of the instruments. If this is not set,
        /// then the interface will try to read all available items. 
        /// </summary>
        /// <param name="args">List of symbols to move from instrument.</param>
        public void SymbolsToReturn(IEnumerable<string> args)
        {
            _symbolsToForward.Clear();
            _symbolsToForward.AddRange(args);
        }

        /// <summary>
        /// A list of symbols that are moved from the instrument.
        /// </summary>
        public IEnumerable<string> ActiveSymbols => _symbolsToForward;
        #endregion

        /// <summary>   Gets or sets a value indicating whether this object is connected. </summary>
        /// <value> True if this object is connected, false if not. </value>
        public bool IsConnected { get; private set; }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        public void Dispose()
        {
            try
            {
                StatusMessage("Disconnecting...");
                IsConnected = false;
                if (_connect == null) return;
                _cts?.Cancel();
                Thread.Sleep(500);
                switch (State)
                {
                    case HSIScopeClientState.Connected:
                        _connect.Disconnect(new ConnectRequest());
                        break;
                    case HSIScopeClientState.Reading:
                        _connect.FinishedWithDataAccess(new ConnectRequest());
                        _connect.Disconnect(new ConnectRequest());
                        break;
                    case HSIScopeClientState.Waiting:
                        _connect.Disconnect(new ConnectRequest());
                        break;
                    case HSIScopeClientState.Disconnected:
                        break;
                }

                Thread.Sleep(10);
                State = HSIScopeClientState.Disconnected;
                StatusMessage("Disconnected");

                _connect = null;
            }
            catch (RpcException rpc)
            {
                ErrorMessage(rpc.Message);
            }
            catch (Exception e)
            {
                ErrorMessage(e.Message);
            }
        }

        /// <summary>   Event queue for all listeners interested in DataAccess events. </summary>
        public event Action<HSIClient, CancellationToken, IEnumerable<object>, double> DataAccess;

        /// <summary>   Event queue for all listeners interested in StatusMessageAction events. </summary>
        public static event Action<string> StatusMessageAction;

        /// <summary>   Error message. </summary>
        /// <param name="message">  The message. </param>
        private static void ErrorMessage(string message)
        {
            Debug.WriteLine(message);
            StatusMessage(message);
        }

        /// <summary>   Status message. </summary>
        /// <param name="message">  The message. </param>
        private static void StatusMessage(string message)
        {
            StatusMessageAction?.Invoke(message);
        }

        /// <summary>   Opens a normalized vector. </summary>
        /// <param name="name"> name of symbol to open. </param>
        /// <returns>   An INormalizedVector. </returns>
        public INormalizedVector OpenNormalized(string name)
        {
            var header = _currentNativeCache[name];
            if (header == null)
            {
                BugLogger.WriteErrorMessage($"header not cached");
#if VERBOSE
            EventLogger.AddEvent($"NormalizeData.GetHeader({name})");
#endif
                header = _currentNativeCache[name] = _normalizedDataClient?.GetHeader(new WaveformRequest
                    { Chunksize = ChunkSize, Sourcename = name }).Headerordata.Header;
                _currentNativeTime = BugLogger.CurrentTime;
            }

            var task = OpenNormalized(header, name);
            Task.WaitAll(task);
            return task.Result;
        }

        /// <summary>   Opens a normalized. </summary>
        /// <param name="header">   The header. </param>
        /// <param name="name">     . </param>
        /// <returns>   An INormalizedVector. </returns>
        private async Task<INormalizedVector> OpenNormalized(WaveformHeader header, string name)
        {
            var retval = new NormalizedVector(new double[header.Noofsamples]);
            retval.Horizontal.Spacing = header.Horizontalspacing;
            retval.Horizontal.IntegerZeroIndex = (int)header.Horizontalzeroindex;
            retval.Horizontal.FractionalZeroIndex = header.Horizontalfractionalzeroindex;
            retval.SourceName = header.Sourcename;

#if VERBOSE
        EventLogger.AddEvent($"NormalizeData.GetWaveform({name})");
#endif
            var response = _normalizedDataClient?.GetWaveform(new WaveformRequest { Chunksize = 128000, Sourcename = name })
                .ResponseStream;
            var chunkbase = 0;

            while(await response.MoveNext(_cts.Token))
            {
                try
                {
                    var reply = response.Current;
                    var buffer = reply.Headerordata.Chunk.Data;
                    var start = chunkbase;
                    Parallel.ForEach(Partitioner.Create(0, buffer.Count), range =>
                    {
                        for (var i = range.Item1; i < range.Item2; i++)
                            retval[start + i] = buffer[i];
                    });
                    Interlocked.Add(ref chunkbase, buffer.Count);
                }
                catch (Exception)
                {
                    // Ignore
                }
            }

            //await foreach (var reply in response?.ReadAllAsync(_cts.Token))
            //{
            //    try
            //    {
            //        var buffer = reply.Headerordata.Chunk.Data;
            //        var start = chunkbase;
            //        Parallel.ForEach(Partitioner.Create(0, buffer.Count), range =>
            //        {
            //            for (var i = range.Item1; i < range.Item2; i++)
            //                retval[start + i] = buffer[i];
            //        });
            //        Interlocked.Add(ref chunkbase, buffer.Count);
            //    }
            //    catch (Exception)
            //    {
            //        // Ignore
            //    }
            //}

            retval.SourceName = header.Sourcename;
            retval.Horizontal.Units = header.HorizontalUnits;
            retval.Vertical.Units = header.Verticalunits;
            IDataStatus ds;
            if (retval is IDataStatus)
                ds = retval as IDataStatus;
            else
                return retval;
            ds.TID = (long)header.Dataid;
            ds.HasData = header.Hasdata;
            ds.SymbolName = header.Sourcename;
            return retval;
        }

        /// <summary>   Opens analog float. </summary>
        /// <param name="header">   The header. </param>
        /// <param name="name">     . </param>
        /// <returns>   An INormalizedVector. </returns>
        private async Task<INormalizedVector> OpenAnalogFloatEx(WaveformHeader header, string name)
        {
            try
            {
                var count = 0;
#if VERBOSE
                EventLogger.AddEvent($"OpenAnalogFloatEx({name})");
#endif
                IAsyncStreamReader<RawReply> response = _nativeDataClient
                    ?.GetWaveform(new WaveformRequest { Chunksize = ChunkSize, Sourcename = name })
                    .ResponseStream;
                var v = new ChunkVector<float>(header.Verticalspacing, header.Verticaloffset, header.Horizontalspacing,
                    header.Horizontalzeroindex, (long)header.Noofsamples);
                v.SourceName = header.Sourcename;
                ((INormalizedVector)v).Horizontal.Units = header.HorizontalUnits;
                ((INormalizedVector)v).Vertical.Units = header.Verticalunits;
                IDataStatus ds;
                if (v is IDataStatus)
                    ds = v as IDataStatus;
                else
                    return v;
                ds.TID = (long)header.Dataid;
                ds.HasData = header.Hasdata;
                ds.SymbolName = header.Sourcename;

                var start = Common.CurrentTime;

                while (await response.MoveNext())
                {
                    var current = response.Current;
                    v.Add(current.Headerordata.Chunk.Data.Memory);
                    Interlocked.Add(ref count, current.Headerordata.Chunk.Data.Length);
                }

                //await foreach (var reply in response?.ReadAllAsync(_cts.Token)!)
                //{
                //    v.Add(reply.Headerordata.Chunk.Data.Memory);
                //    Interlocked.Add(ref count, reply.Headerordata.Chunk.Data.Length);
                //}

                var duration = Common.CurrentTime - start;

                LogMetric.UpdateMetric("Data Arrival - float", header.Sourcename, count * 8.0 / duration, "bS");

                return v;
            }
            catch (Exception ex)
            {
                BugLogger.CatchException(ex);
                return null;
            }
        }

        /// <summary>   Opens analog int 16. </summary>
        /// <param name="header">   The header. </param>
        /// <param name="name">     . </param>
        /// <returns>   An INormalizedVector. </returns>
        private async Task<INormalizedVector> OpenAnalogInt16Ex(WaveformHeader header, string name)
        {
            try
            {
                var count = 0;
#if VERBOSE
            EventLogger.AddEvent($"OpenAnalogInt16Ex({name})");
#endif
                var response = _nativeDataClient
                    ?.GetWaveform(new WaveformRequest { Chunksize = ChunkSize, Sourcename = name })
                    .ResponseStream;
#if VERBOSE
            //EventLogger.AddEvent($"[OpenAnalogInt16Ex] - VSpacing={header.Verticalspacing}, VOffset={header.Verticaloffset}, VUnits={header.Verticalunits}");
#endif
                var v = new ChunkVector<short>(header.Verticalspacing, header.Verticaloffset, header.Horizontalspacing,
                    header.Horizontalzeroindex, (long)header.Noofsamples);
                v.SourceName = header.Sourcename;
                ((INormalizedVector)v).Horizontal.Units = header.HorizontalUnits;
                ((INormalizedVector)v).Vertical.Units = header.Verticalunits;
                IDataStatus ds;
                if (v is IDataStatus)
                    ds = v as IDataStatus;
                else
                    return v;
                ds.TID = (long)header.Dataid;
                ds.HasData = header.Hasdata;
                ds.SymbolName = header.Sourcename;

                var start = Common.CurrentTime;

                while(await response.MoveNext(_cts.Token))
                {
                    var reply = response.Current;
                    v.Add(reply.Headerordata.Chunk.Data.Memory);
                    Interlocked.Add(ref count, reply.Headerordata.Chunk.Data.Length);
                }

                //await foreach (var reply in response?.ReadAllAsync(_cts.Token)!)
                //{
                //    v.Add(reply.Headerordata.Chunk.Data.Memory);
                //    Interlocked.Add(ref count, reply.Headerordata.Chunk.Data.Length);
                //}

                var duration = Common.CurrentTime - start;

                LogMetric.UpdateMetric("Data Arrival - short", header.Sourcename, count * 8.0 / duration, "bS");

                return v;
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
                return null;
            }
        }

        /// <summary>   Opens analog int 8. </summary>
        /// <param name="header">   The header. </param>
        /// <param name="name">     . </param>
        /// <returns>   An INormalizedVector. </returns>
        private async Task<INormalizedVector> OpenAnalogInt8Ex(WaveformHeader header, string name)
        {
            try
            {
                var count = 0;
#if VERBOSE
            EventLogger.AddEvent($"OpenAnalogInt8Ex({name})");
#endif
                var response = _nativeDataClient
                    ?.GetWaveform(new WaveformRequest { Chunksize = ChunkSize, Sourcename = name })
                    .ResponseStream;

                var v = new ChunkVector<sbyte>(header.Verticalspacing, header.Verticaloffset, header.Horizontalspacing,
                    header.Horizontalzeroindex, (long)header.Noofsamples);
                v.SourceName = header.Sourcename;
                ((INormalizedVector)v).Horizontal.Units = header.HorizontalUnits;
                ((INormalizedVector)v).Vertical.Units = header.Verticalunits;
                IDataStatus ds;
                if (v is IDataStatus)
                    ds = v as IDataStatus;
                else
                    return v;
                ds.TID = (long)header.Dataid;
                ds.HasData = header.Hasdata;
                ds.SymbolName = header.Sourcename;

                var start = Common.CurrentTime;

                while(await response.MoveNext(_cts.Token))
                {
                    var reply = response.Current;
                    v.Add(reply.Headerordata.Chunk.Data.Memory);
                    Interlocked.Add(ref count, reply.Headerordata.Chunk.Data.Length);
                }

                //await foreach (var reply in response?.ReadAllAsync(_cts.Token)!)
                //{
                //    v.Add(reply.Headerordata.Chunk.Data.Memory);
                //    Interlocked.Add(ref count, reply.Headerordata.Chunk.Data.Length);
                //}

                var duration = Common.CurrentTime - start;
                LogMetric.UpdateMetric("Data Arrival - signed byte", header.Sourcename, count * 8.0 / duration, "bS");

                return v;
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
                return null;
            }
        }

        /// <summary>   Opens symbol as INormalizedVector </summary>
        /// <param name="name"> name of symbol </param>
        /// <returns>  returns INormalizedVector or null. </returns>
        public INormalizedVector Open(string name)
        {
            if (_opencache.ContainsKey(name))
                return _opencache[name] as INormalizedVector;

            var header = _currentNativeCache[name];

            if (header == null) return null;

            try
            {
                switch (header.Wfmtype)
                {
                    case WfmType.AnalogFloat:
                        var t1 = OpenAnalogFloatEx(header, name);
                        Task.WaitAll(t1);
                        _opencache.Add(name, t1.Result); 
                        return t1.Result;
                    case WfmType.Analog16:
                        var t2 = OpenAnalogInt16Ex(header, name);
                        Task.WaitAll(t2);
                        _opencache.Add(name, t2.Result); 
                        return t2.Result;
                    case WfmType.Analog8:
                        var t3 = OpenAnalogInt8Ex(header, name);
                        Task.WaitAll(t3);
                        _opencache.Add(name, t3.Result);
                        return t3.Result;
                    case WfmType.Analog16Iq:
                        var t4 = OpenAnalogIQ16(header,name);
                        Task.WaitAll(t4);
                        _opencache.Add(name, t4.Result);
                        return t4.Result;
                    case WfmType.Analog32Iq:
                        var t5 = OpenAnalogIQ32(header, name);
                        Task.WaitAll(t5);
                        _opencache.Add(name, t5.Result);
                        return t5.Result;
                    case WfmType.Unspecified:
                    case WfmType.Digital8:
                    case WfmType.Digital16:
                        return null;
                    default:
                        return null;
                }
            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
                BugLogger.CatchException(e);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<INormalizedVector> OpenAnalogIQ32(WaveformHeader header, string name)
        {
            try
            {
                var count = 0;
#if VERBOSE
            EventLogger.AddEvent($"OpenAnalogIQ32({name})");
#endif
                var response = _nativeDataClient
                    ?.GetWaveform(new WaveformRequest { Chunksize = ChunkSize, Sourcename = name })
                    .ResponseStream;
#if VERBOSE
            EventLogger.AddEvent($"[OpenAnalogIQ32] - VSpacing={header.Verticalspacing}, VOffset={header.Verticaloffset}, VUnits={header.Verticalunits}");
#endif
                var v = new ChunkVector<Int32>(header.Verticalspacing, header.Verticaloffset, header.Horizontalspacing,
                    header.Horizontalzeroindex, (long)header.Noofsamples);
                v.SourceName = header.Sourcename;
                ((INormalizedVector)v).Horizontal.Units = header.HorizontalUnits;
                ((INormalizedVector)v).Vertical.Units = header.Verticalunits;
                IDataStatus ds;
                if (v is IDataStatus)
                    ds = v as IDataStatus;
                else
                    return v;
                ds.TID = (long)header.Dataid;
                ds.HasData = header.Hasdata;
                ds.SymbolName = header.Sourcename;

                var start = Common.CurrentTime;

                while(await response.MoveNext(_cts.Token))
                //await foreach (var reply in response?.ReadAllAsync(_cts.Token)!)
                {
                    var reply = response.Current;
                    v.Add(reply.Headerordata.Chunk.Data.Memory);
                    Interlocked.Add(ref count, reply.Headerordata.Chunk.Data.Length);
                }

                var duration = Common.CurrentTime - start;

                LogMetric.UpdateMetric("Data Arrival - Int32", header.Sourcename, count * 8.0 / duration, "bS");

                return v;
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="header"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<INormalizedVector> OpenAnalogIQ16(WaveformHeader header, string name)
        {
            try
            {
                var count = 0;
#if VERBOSE
            EventLogger.AddEvent($"OpenAnalogIQ16({name})");
#endif
                var response = _nativeDataClient
                    ?.GetWaveform(new WaveformRequest { Chunksize = ChunkSize, Sourcename = name })
                    .ResponseStream;
#if VERBOSE
            EventLogger.AddEvent($"[OpenAnalogIQ16] - VSpacing={header.Verticalspacing}, VOffset={header.Verticaloffset}, VUnits={header.Verticalunits}");
#endif
                var v = new ChunkVector<short>(header.Verticalspacing, header.Verticaloffset, header.Horizontalspacing,
                    header.Horizontalzeroindex, (long)header.Noofsamples);
                v.SourceName = header.Sourcename;
                ((INormalizedVector)v).Horizontal.Units = header.HorizontalUnits;
                ((INormalizedVector)v).Vertical.Units = header.Verticalunits;
                IDataStatus ds;
                if (v is IDataStatus)
                    ds = v as IDataStatus;
                else
                    return v;
                //if (v is not IDataStatus ds) return v;
                ds.TID = (long)header.Dataid;
                ds.HasData = header.Hasdata;
                ds.SymbolName = header.Sourcename;

                var start = Common.CurrentTime;

                while(await response.MoveNext(_cts.Token))
                //await foreach (var reply in response?.ReadAllAsync(_cts.Token)!)
                {
                    var reply = response.Current;
                    v.Add(reply.Headerordata.Chunk.Data.Memory);
                    Interlocked.Add(ref count, reply.Headerordata.Chunk.Data.Length);
                }

                var duration = Common.CurrentTime - start;

                LogMetric.UpdateMetric("Data Arrival - short", header.Sourcename, count * 8.0 / duration, "bS");

                return v;
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        private bool UpdateHeaderCache(params string[] names)
        {
            var readStart = CurrentTime;
            using (LogMetric.Metric("Read", "Header"))
            {
                foreach (var name in names)
                {
                    try
                    {
                        var h = _nativeDataClient.GetHeader(new WaveformRequest { Chunksize = ChunkSize, Sourcename = name.ToLower() });
                        if (h.Status != WfmReplyStatus.Success || !h.Headerordata.Header.Hasdata)
                        {
                            BugLogger.WriteErrorMessage($"{name}:{h.Status}:{h.Headerordata.Header.Hasdata}");
                            continue;
                        }
                        _currentNativeCache[name] = h.Headerordata.Header;
                        _currentNativeTime = readStart;
                    }
                    catch (Exception e)
                    {
                        BugLogger.CatchException(e);
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void FinishedWithCurrentHeaderCache()
        {
            using (_currentNativeCache.WriteLock)
            using (_headerPrevCache.WriteLock)
            {
                try
                {
                    if (!_currentNativeCache.Any())
                        return;
                    _headerPrevCache.Clear();
                    foreach (var item in _currentNativeCache)
                    {
                        _headerPrevCache[item.Key] = item.Value;
                    }
                    _currentNativeCache.Clear();
                }
                catch (Exception e)
                {
                    //Console.WriteLine(e.Message);
                    BugLogger.CatchException(e);
                }
            }
        }

        /// <summary>
        /// The current time in seconds since this client connected to the instrument.
        /// </summary>
        public double CurrentTime => BugLogger.CurrentTime;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        private bool UpdateCriterionMet(params string[] names)
        {
            foreach (var name in names)
            {
                var prev = _headerPrevCache[name];
                var current = _currentNativeCache[name];

#if VERBOSE
                ShowDifferences(prev, current);
#endif
                if ((UpdateCondition & UpdateConditionType.AnyAcq) != 0)
                {
#if VERBOSE
                    EventLogger.AddEvent($"UpdateCriterionMet-AnyAcq:prev={prev?.Transid},current={current?.Transid}");
#endif
                    return true;
                }

                if (((UpdateCondition & UpdateConditionType.RecordLengthChange) != 0) &&
                    prev != null && current != null && ((prev.Noofsamples != current.Noofsamples) ||
                                                        (current != null && prev == null)))
                {
#if VERBOSE
                    EventLogger.AddEvent($"UpdateCriterionMet-RecordLengthChange:prev={prev?.Noofsamples},current={current.Noofsamples}");
#endif
                    return true;
                }

                if (((UpdateCondition & UpdateConditionType.VSpacingChange) != 0) &&
                    prev != null && current != null && ((prev.Verticalspacing != current.Verticalspacing) ||
                                                        (current != null && prev == null)))
                {
#if VERBOSE
                    EventLogger.AddEvent($"UpdateCriterionMet-VSpacingChange:prev={prev.Verticalspacing},current={current.Verticalspacing}");
#endif
                    return true;
                }

                if (((UpdateCondition & UpdateConditionType.HSpacingChange) != 0) &&
                    prev != null && current != null && ((prev.Horizontalspacing != current.Horizontalspacing) ||
                                                        (current != null && prev == null)))
                {
#if VERBOSE
                    EventLogger.AddEvent($"UpdateCriterionMet-HSpacingChange:prev={prev.Horizontalspacing},current={current.Horizontalspacing}");
#endif
                    return true;
                }
            }

#if VERBOSE
            EventLogger.AddEvent($"UpdateCriterionMet-No Differences");
#endif
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="current"></param>
        private void ShowDifferences(WaveformHeader prev, WaveformHeader current)
        {
            var sb = new StringBuilder();

            if (!IsDifferent(prev, current)) return;

            if (prev == null) return;

            if (prev.Transid != current.Transid)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append($"TID:old={prev.Transid},current={current.Transid}");
            }

            if (prev.Noofsamples != current.Noofsamples)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append($"Count:old={prev.Noofsamples},current={current.Noofsamples}");
            }

            if (prev.Horizontalspacing != current.Horizontalspacing)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append($"HSpacing:old={prev.Horizontalspacing},current={current.Horizontalspacing}");
            }

            if (prev.Horizontalzeroindex != current.Horizontalzeroindex)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append($"HZeroIndex:old={prev.Horizontalzeroindex},current={current.Horizontalzeroindex}");
            }

            if (prev.Verticalspacing != current.Verticalspacing)
            {
                if (sb.Length > 0) sb.Append(",");
                sb.Append($"VSpacing:old={prev.Verticalspacing},current={current.Verticalspacing}");
            }

            if (sb.Length > 0)
            {
                EventLogger.AddEvent(sb.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="prev"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        private bool IsDifferent(WaveformHeader prev, WaveformHeader current)
        {
            if (prev == null && current != null)
                return true;

            if (current == null)
                return false;

            if (prev.Noofsamples != current.Noofsamples)
            {
                return true;
            }

            if (prev.Horizontalspacing != current.Horizontalspacing)
            {
                return true;
            }

            if (prev.Horizontalzeroindex != current.Horizontalzeroindex)
            {
                return true;
            }

            if (prev.Verticalspacing != current.Verticalspacing)
            {
                return true;
            }

            return false;
        }

        /// <summary>   Opens the symbol passed as name. </summary>
        /// <param name="name"> The name of the symbol to open </param>
        /// <param name="readHeaderTime">   [out] The read header time. This is for performance analysis. </param>
        /// <param name="readwfmTime">      [out] The readwfm time. This is for performance analysis. </param>
        /// <returns>   An object. </returns>
        public object Open(string name, out double readHeaderTime, out double readwfmTime)
        {
            readHeaderTime = 0;
            readwfmTime = 0;

            var start = Common.CurrentTime;

            // Returned cached items so we aren't moving the same data if 
            // not change in data has occurred.
            if (_opencache.ContainsKey(name))
                return _opencache[name];

            try
            {
                var startHeader = Common.CurrentTime;
                var header = _currentNativeCache[name];

                if (header == null) return null;

                readHeaderTime = Common.CurrentTime - startHeader;

                try
                {
                    switch (header.Wfmtype)
                    {
                        case WfmType.AnalogFloat:
                            var t1 = OpenAnalogFloatEx(header, name);
                            Task.WaitAll(t1);
                            _opencache.Add(name, t1.Result); // cache result
                            return t1.Result;
                        case WfmType.Analog16:
                            var t2 = OpenAnalogInt16Ex(header, name);
                            Task.WaitAll(t2);
                            _opencache.Add(name, t2.Result); // cache result
                            return t2.Result;
                        case WfmType.Analog8:
                            var t3 = OpenAnalogInt8Ex(header, name);
                            Task.WaitAll(t3);
                            _opencache.Add(name, t3.Result); // cache result
                            return t3.Result;
                        case WfmType.Analog16Iq:
                            var t4 = OpenAnalogIQ16(header, name);
                            Task.WaitAll(t4);
                            _opencache.Add(name, t4.Result); // cache result
                            return t4.Result;
                        case WfmType.Analog32Iq:
                            var t5 = OpenAnalogIQ32(header, name);
                            Task.WaitAll(t5);
                            _opencache.Add(name, t5.Result); // cache result
                            return t5.Result;
                        case WfmType.Unspecified:
                        case WfmType.Digital8:
                        case WfmType.Digital16:
                        default:
                            BugLogger.WriteErrorMessage(
                                $"Open-{name}:Unexpected Type-{header.Wfmtype}");
                            return null;
                    }
                }
                finally
                {
                    readwfmTime = Common.CurrentTime - start;
                }
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
                return null;
            }
        }

        /// <summary>
        /// Requests a connection to the specified instrument.
        /// </summary>
        /// <param name="ipAddress">The IP address of the instrument.</param>
        /// <param name="channels">The names of the symbols to be transferred. </param>
        /// <param name="port">The port number of the HSI server. Port 5000 is the default.</param>
        /// <returns>returns an HSIClient object</returns>
        public static HSIClient Connect(string ipAddress, IEnumerable<string> channels, int port = 5000)
        {
            EventLogger.AddEvent($"[ScopeClient] - Connect(http://{ipAddress}:{port})");
            var conn = new HSIClient { WebURL = $"http://{ipAddress}:{port}" };

            try
            {
                var channel = new Channel($"{ipAddress}:{port}", ChannelCredentials.Insecure);
                conn._channel = channel;
                conn._connect = new Connect.ConnectClient(channel);
                conn._normalizedDataClient = new NormalizedData.NormalizedDataClient(channel);
                conn._nativeDataClient = new NativeDataClient(channel);
                conn._connect.Connect(new ConnectRequest { Name = conn._clientName });
                conn.IsConnected = true;
                conn.State = HSIScopeClientState.Connected;

                var chans = channels.ToArray();
                conn.SymbolsToReturn(chans.Length > 0 ? chans : conn.Symbols);
            }
            catch (RpcException rpc)
            {
                BugLogger.CatchException(rpc);
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
            }

            return conn;
        }

        private static bool _running = false;

        /// <summary>   Starts this object. </summary>
        public void Start()
        {
            if (_running)
            {
                BugLogger.WriteErrorMessage($"HSIConnect.Start() called when running.");
                return;
            }
            
            _running = true;
#if VERBOSE
        EventLogger.AddEvent($"Start Enter");
#endif
            _cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                State = HSIScopeClientState.Waiting;
#if VERBOSE
            EventLogger.AddEvent($"WaitForDataAccess Enter");
#endif
                ConnectStatus status;
                while ((status = _connect.WaitForDataAccess(new ConnectRequest()).Status) == ConnectStatus.Success)
                    try
                    {
                        _lock.EnterWriteLock();
#if VERBOSE
                        EventLogger.AddEvent($"WaitForDataAccess Exit");
#endif
                        Debug.WriteLine($"WaitForDataAccess:{CurrentTime}");
                        double readTime = 0;

                        if (_cts.IsCancellationRequested) break;

                        var ctx = _cts.Token;
                    
                        var startRead = CurrentTime;

                        var names = _symbolsToForward.ToArray();

#if DEBUG
                        string namelist = "";
                        foreach (var name in names)
                        {
                            namelist += name + ",";
                        }

                        EventLogger.AddEvent($"SymbolsToForward: {namelist}");
#endif
                        if (!UpdateHeaderCache(names))
                            continue;

                        if (!UpdateCriterionMet(names))
                        {
                            FinishedWithCurrentHeaderCache();
                            continue;
                        }

                        try
                        {
                            if (!ReadCacheData(ctx))
                                continue;
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        finally
                        {
                            FinishedWithCurrentHeaderCache();
                        }

                        readTime = CurrentTime - startRead;

                        Interlocked.Increment(ref _acqCount);

                        _acqTime = startRead;

                        try
                        {
#if VERBOSE
                            EventLogger.AddEvent($"DataAccess Called");
#endif
                            if (DataAccess == null) continue;
                            if (NonBlocking)
                                Task.Run(() => DataAccess?.Invoke(this, ctx, _opencache.Values, readTime), ctx);
                            else
                                DataAccess(this, ctx, _opencache.Values, readTime);
#if VERBOSE
                            EventLogger.AddEvent($"DataAccess Returned");
#endif
                        }
                        catch (Exception e)
                        {
                            BugLogger.CatchException(e);
                        }
                    }
                    finally
                    {
                        _lock.ExitWriteLock();
#if VERBOSE
                    EventLogger.AddEvent($"FinishedWithDataAccess");
#endif
                        _connect?.FinishedWithDataAccess(new ConnectRequest());
                        var current = Common.CurrentTime;
                        State = HSIScopeClientState.Waiting;
                        _running = false;
                    }

                BugLogger.WriteErrorMessage($"Background Task Exited - Status: {status}");
            });

            // Give us a moment to get things running
            Thread.Sleep(500);
        }

        /// <summary>
        /// Last data rate seen during transfer. This is intended to monitor transfer
        /// performance.
        /// </summary>
        public double LastDataRate { get; set; } = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        private long DataSize(object o)
        {
            try
            {
                if (o is ChunkVector<sbyte>)
                {
                    var v8 = o as ChunkVector<sbyte>;
                    return v8.Count;
                }
                else if (o is ChunkVector<Int16>)
                {
                    var v16 = o as ChunkVector<Int16>;
                    return v16.Count * 2;
                }
                else if (o is ChunkVector<float>)
                {
                    var vf = o as ChunkVector<float>;
                    return vf.Count * 4;
                }
                else
                {
                    return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private bool ReadCacheData(CancellationToken ctx)
        {
            var names = _symbolsToForward.ToArray();
            using (LogMetric.Metric("Read", "Waveform"))
                try
                {
                    _opencache.Clear();
                    if (IsMultiThreadedRead && _symbolsToForward.Count > 1)
                    {
                        var start = CurrentTime;
                        long size = 0;
                        Parallel.ForEach(names, (symbol) =>
                        {
                            if (string.IsNullOrEmpty(symbol)) return;
                            ctx.ThrowIfCancellationRequested();
                            var o = Open(symbol);
                            if (o == null)
                                BugLogger.WriteErrorMessage($"Failed to read - {symbol}");
                            if (o != null)
                                Interlocked.Add(ref size, DataSize(o));
                        });
                        var duration = CurrentTime - start;
                        if (duration > 0.0 && size > 0)
                            LastDataRate = (size / duration);
                    }
                    else
                    {
                        var start = CurrentTime;
                        long size = 0;
                        foreach (var symbol in names)
                        {
                            if (string.IsNullOrEmpty(symbol)) continue;
                            ctx.ThrowIfCancellationRequested();
                            var o = Open(symbol);
                            if (o == null)
                                BugLogger.WriteErrorMessage($"Failed to read - {symbol}");
                            if (o != null) 
                                size += DataSize(o);
                        }
                        var duration = CurrentTime - start;
                        if (duration > 0.0 && size > 0)
                            LastDataRate = (size / duration);
                    }
                }
                finally
                {
                    FinishedWithCurrentHeaderCache();
                }

            return true;
        }

        /// <summary>   Starts a sequence. </summary>
        public void StartSequence()
        {
            _opencache.Clear();
            _connect?.RequestNewSequence(new ConnectRequest());
        }

        #region Wait

        /// <summary>
        /// Called after WaitForData has returned and the needed symbols have been opened. Forgetting to call
        /// this method will result in the instrument appearing to hang.
        /// </summary>
        public void DoneWithData()
        {
            _lastAcqSeen = AcqCount;
            DoneWithDataReleaseLock();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="after"></param>
        private void WaitForAcqTime(double after)
        {
            while (true)
            {
                if (_lastAcqSeen == AcqCount)
                {
                    WaitForNext();
                    if (after > AcqTime)
                        break;
                    DoneWithDataReleaseLock();
                } 
                else if (_lastAcqSeen != AcqCount && AcqTime > after)
                {
                    _lock.EnterWriteLock();
                    break;
                }
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Returns the time the last acquistion was received.
        /// </summary>
        public double AcqTime
        {
            get
            {
                using (_acqTimeLock.ReadLock)
                    return _acqTime;
            }
            set
            {
                using (_acqTimeLock.WriteLock)
                    _acqTime=value;
            }
        }

        private void WaitForNextAcq()
        {
            while (true)
            {
                WaitForNext();
                if (_opencache.Count > 0 && _lastAcqSeen < AcqCount)
                    break;
                DoneWithDataReleaseLock();
                Thread.Sleep(1);
            }
        }

        private void WaitForNext()
        {
            _lock.EnterWriteLock();
        }

        private void DoneWithDataReleaseLock()
        {
            _lock.ExitWriteLock();
        }

        /// <summary>
        /// This returns after any acquisition is received. 
        /// </summary>
        public void WaitForAnyAcq()
        {
            WaitForNextAcq();
        }

        /// <summary>
        /// This returns after the input criterion is met.
        /// </summary>
        /// <param name="type">Wait criterion.</param>
        /// <param name="time">Used with AfterType type. Indicates that we are not interested in acquisitions until after this time.</param>
        public void WaitForData(UpdateConditionType type = UpdateConditionType.Nonblocking, double time = double.NaN)
        {
#if WEBLOGGING
            EventLogger.AddEvent($"WaitForData {type}:", EventHighlightType.Red);
#endif

            switch (type)
            {
                case UpdateConditionType.Nonblocking:
                    break;
                case UpdateConditionType.AfterTime:
                    WaitForAcqTime(time);
                    break;
                case UpdateConditionType.Next:
                    WaitForNext();
                    break;
                case UpdateConditionType.AnyAcq:
                default:
                    WaitForAnyAcq();
                    break;
            }
        }

        #endregion
    }
}