//#define VERBOSE
#define RAW
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Grpc.Core;
using Tek.Scope;
using Tekscope;

namespace TekHighspeedAPI.Server
{
    public class NativeDataImpl : NativeData.NativeDataBase
    {
        public IDataAccess DataAccess;

        public override Task<RawReply> GetHeader(WaveformRequest request, ServerCallContext context)
        {
#if VERBOSE
        EventLogger.AddEvent($"NativeDataImpl.GetHeader({request.Sourcename})");
#endif
            var start = Common.CurrentTime;
            var item = DataAccess.Open(request.Sourcename);

            var bConnected = DataAccess.IsConnected;
            var bDataIsAvailable = DataAccess.IsDataAvailable;
            var bDataIsExpectedType = item is INormalizedVector ||
                                    item is INativeVector<sbyte> ||
                                    item is INativeVector<float> ||
                                    item is INativeVector<short>;
            var bSourceNameAvailable =
                !string.IsNullOrEmpty(request
                    .Sourcename); // && (_matchDigital.IsMatch(request.Sourcename) || _matchChan.IsMatch(request.Sourcename));

            if (!bConnected)
            {
                var reply = new RawReply
                {
                    Status = WfmReplyStatus.NoConnectionFailure,
                    Headerordata = new RawReply.Types.DataOrHeaderAccess
                        { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                };
                return Task.Run(() => reply);
            }

            if (!bDataIsAvailable)
            {
                var reply = new RawReply
                {
                    Status = WfmReplyStatus.OutsideSequenceFailure,
                    Headerordata = new RawReply.Types.DataOrHeaderAccess
                        { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                };
                return Task.Run(() => reply);
            }

            if (!bDataIsExpectedType)
            {
                var reply = new RawReply
                {
                    Status = WfmReplyStatus.TypeMismatchFailure,
                    Headerordata = new RawReply.Types.DataOrHeaderAccess
                        { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                };
                return Task.Run(() => reply);
            }

            if (!bSourceNameAvailable)
            {
                var reply = new RawReply
                {
                    Status = WfmReplyStatus.SourcenameMissingFailure,
                    Headerordata = new RawReply.Types.DataOrHeaderAccess
                        { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                };
                return Task.Run(() => reply);
            }
            else
            {
                var reply = new RawReply
                {
                    Status = WfmReplyStatus.Success,
                    Headerordata = new RawReply.Types.DataOrHeaderAccess
                    {
                        Header = new WaveformHeader { Sourcename = request.Sourcename }
                    }
                };

                ExtractRawHeader(request.Sourcename, item, reply);
#if VERBOSE
            EventLogger.AddEvent(
                $"[Server] GetWaveformHeader({request.Sourcename}):{reply.Headerordata.Header.Wfmtype} - {Tek.Scope.Support.EngineeringNotationFormatter.Format(Common.CurrentTime - start)}S");
#endif
                return Task.Run(() => reply);
            }
        }

        public override async Task GetWaveform(WaveformRequest request, IServerStreamWriter<RawReply> responseStream,
            ServerCallContext context)
        {
            try
            {
#if VERBOSE
            EventLogger.AddEvent($"NativeDataImpl.GetWaveform({request.Sourcename})");
#endif
                long size = 0;

                var start = Common.CurrentTime;

                var obj = DataAccess.Open(request.Sourcename);

                var bConnected = DataAccess.IsConnected;
                var bDataIsAvailable = DataAccess.IsDataAvailable;
                var bDataIsExpectedType =   obj is Tek.Scope.Support.INormalizedVector || 
                                            obj is INativeVector<sbyte> || 
                                            obj is INativeVector<float> || 
                                            obj is INativeVector<short>;
                var bSourceNameAvailable =
                    !string.IsNullOrEmpty(request
                        .Sourcename); // && (_matchDigital.IsMatch(request.Sourcename) || _matchChan.IsMatch(request.Sourcename));

                if (!bConnected)
                {
                    var reply = new RawReply
                    {
                        Status = WfmReplyStatus.NoConnectionFailure,
                        Headerordata = new RawReply.Types.DataOrHeaderAccess
                            { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                    };
                    await responseStream.WriteAsync(reply);
                    return;
                }

                if (!bDataIsAvailable)
                {
                    var reply = new RawReply
                    {
                        Status = WfmReplyStatus.OutsideSequenceFailure,
                        Headerordata = new RawReply.Types.DataOrHeaderAccess
                            { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                    };
                    await responseStream.WriteAsync(reply);
                    return;
                }

                if (!bDataIsExpectedType)
                {
                    var reply = new RawReply
                    {
                        Status = WfmReplyStatus.TypeMismatchFailure,
                        Headerordata = new RawReply.Types.DataOrHeaderAccess
                            { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                    };
                    await responseStream.WriteAsync(reply);
                    return;
                }

                if (!bSourceNameAvailable)
                {
                    var reply = new RawReply
                    {
                        Status = WfmReplyStatus.SourcenameMissingFailure,
                        Headerordata = new RawReply.Types.DataOrHeaderAccess
                            { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                    };
                    await responseStream.WriteAsync(reply);
                    return;
                }

                var r = new RawReply
                {
                    Status = WfmReplyStatus.Success,
                    Headerordata = new RawReply.Types.DataOrHeaderAccess
                    {
                        Header = new WaveformHeader(),
                        Chunk = new RawReply.Types.WaveformSampleByteChunk()
                    }
                };

                ExtractRawHeader(request.Sourcename, obj, r);

                var chunksize = request.Chunksize > 0 ? request.Chunksize : Common.DefaultChunkSize;

                try
                {
                    if (obj is INativeVector<short>)
                    {
                        var v16 = obj as INativeVector<short>;
                        size = await v16WfmMethod(request, responseStream, context, size, obj, r, chunksize, v16);
                    }
                    else if (obj is INativeVector<sbyte>)
                    {
                        var v8 = obj as INativeVector<sbyte>;
                        size = await v8WfmMethod(request, responseStream, context, size, obj, r, chunksize, v8);
                    }
                    else if (obj is INativeVector<float>)
                    {
                        var vf = obj as INativeVector<float>;
                        size = await vfWfmMethod(request, responseStream, context, size, r, chunksize, vf);
                    }
                    else if (obj is INormalizedVector)
                    {
                        var nv = obj as Tek.Scope.Support.INormalizedVector;
                        size = await nvWfmMethod(request, responseStream, context, size, r, chunksize, nv);
                    }
                }
                catch
                {
                }

                var duration = Common.CurrentTime - start;
                LogMetric.UpdateMetric("Metric", request.Sourcename, size * 8.0 / duration, "bS");
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
            }
        }

        private async Task<long> nvWfmMethod(WaveformRequest request, IServerStreamWriter<RawReply> responseStream, ServerCallContext context, long size, RawReply r, uint chunksize, Tek.Scope.Support.INormalizedVector nv)
        {
            r.Status = WfmReplyStatus.Success;
            r.Headerordata.Header.Sourcename = request.Sourcename;
            r.Headerordata.Header.Sourcewidth = sizeof(float);
            r.Headerordata.Header.Wfmtype = WfmType.AnalogFloat;
            uint noofBytesPerData = sizeof(float);
            r.Headerordata.Chunk = new RawReply.Types.WaveformSampleByteChunk();

            var chunkSize = adjustedChunkSizeInBytes(chunksize, noofBytesPerData);
            var chunkItemCount = chunkSize / noofBytesPerData;
            var totalchuncks = totalNoofChunks((ulong)nv.Count, noofBytesPerData, chunkSize);

            for (long i = 0; i < (int)totalchuncks; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;
                var actualchunkSize =
                    chunkSliceSizeInBytes((ulong)i, (ulong)nv.Count, noofBytesPerData, chunkSize);
                var chunkelements = actualchunkSize / noofBytesPerData;
                var buffer = new byte[actualchunkSize];
                var basechunkindex = (long)chunkItemCount * i;

                unsafe
                {
                    float* fptr;
                    using (var fpinned = new Pinned(buffer))
                        fptr = (float*)fpinned.Pointer;
                    Parallel.ForEach(Partitioner.Create(0, (int)chunkelements), range =>
                    {
                        for (var j = range.Item1; j < range.Item2; j++)
                            *(fptr + j) = (float)nv[basechunkindex + j];
                    });
                }

                r.Headerordata.Chunk.Data = Common.CreateByteString(buffer);
                await responseStream.WriteAsync(r);
            }

            size = nv.Count * noofBytesPerData;
            return size;
        }

        private async Task<long> vfWfmMethod(WaveformRequest request, IServerStreamWriter<RawReply> responseStream, ServerCallContext context, long size, RawReply r, uint chunksize, INativeVector<float> vf)
        {
            r.Status = WfmReplyStatus.Success;
            r.Headerordata.Header.Sourcename = request.Sourcename;
            r.Headerordata.Header.Sourcewidth = sizeof(float);
            r.Headerordata.Header.Wfmtype = WfmType.AnalogFloat;
            uint noofBytesPerData = sizeof(float);
            r.Headerordata.Chunk = new RawReply.Types.WaveformSampleByteChunk();
#if VERBOSE
                        EventLogger.AddEvent($"vf:Precharge Count={vf.NativeHorizontal.PrechargeCount}");
#endif
            var chunkSize = adjustedChunkSizeInBytes(chunksize, noofBytesPerData);
            var chunkItemCount = chunkSize / noofBytesPerData;
            var totalchuncks = totalNoofChunks((ulong)vf.Count, noofBytesPerData, chunkSize);

            for (long i = 0; i < (int)totalchuncks; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;
                var actualchunkSize =
                    chunkSliceSizeInBytes((ulong)i, (ulong)vf.Count, noofBytesPerData, chunkSize);
                var chunkelements = actualchunkSize / noofBytesPerData;
                var buffer = new byte[actualchunkSize];
                var basechunkindex = (long)chunkItemCount * i;

                unsafe
                {
                    float* fptr;
                    using (var fpinned = new Pinned(buffer))
                        fptr = (float*)fpinned.Pointer;
                    Parallel.ForEach(Partitioner.Create(0, (int)chunkelements), range =>
                    {
                        for (var j = range.Item1; j < range.Item2; j++) *(fptr + j) = vf[basechunkindex + j];
                    });
                }

                r.Headerordata.Chunk.Data = Common.CreateByteString(buffer);
                await responseStream.WriteAsync(r);
            }

            size = vf.Count * sizeof(float);
            return size;
        }

        private async Task<long> v8WfmMethod(WaveformRequest request, IServerStreamWriter<RawReply> responseStream, ServerCallContext context, long size, object obj, RawReply r, uint chunksize, INativeVector<sbyte> v8)
        {
            r.Headerordata.Header.Sourcename = request.Sourcename;
            var noofBytesPerData = r.Headerordata.Header.Sourcewidth;
            r.Headerordata.Chunk = new RawReply.Types.WaveformSampleByteChunk();

            var chunkSize = adjustedChunkSizeInBytes(chunksize, noofBytesPerData);
            var totalchunks = totalNoofChunks((ulong)v8.Count, noofBytesPerData, chunkSize);
#if VERBOSE
                        EventLogger.AddEvent($"v8:Precharge Count={v8.NativeHorizontal.PrechargeCount}");
#endif
#if RAW
            var raw = obj as IRawInterface<sbyte>;
#endif
            for (ulong i = 0; i < totalchunks; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                var actualchunkSize = chunkSliceSizeInBytes(i, (ulong)v8.Count, noofBytesPerData, chunkSize);
                var chunkItemCount = chunkSize / noofBytesPerData;
                var chunkelements = actualchunkSize / noofBytesPerData;
                var basechunkindex = (long)(chunkItemCount * i);
                var arr = new byte[actualchunkSize];
#if RAW
                if (raw == null)
#endif
                {
                    unsafe
                    {
                        sbyte* bptr;
                        using (var bpinned = new Pinned(arr))
                            bptr = (sbyte*)bpinned.Pointer;
                        Parallel.ForEach(Partitioner.Create(0, (int)chunkelements), range =>
                        {
                            for (var j = range.Item1; j < range.Item2; j++)
                                *(bptr + j) = v8[basechunkindex + j];
                        });
                    }

                    r.Headerordata.Chunk.Data = Common.CreateByteString(arr);
                    await responseStream.WriteAsync(r);
                }
#if RAW
                else
                {
                    unsafe
                    {
                        var ptr = (IntPtr)(sbyte*)raw.RawDataPtr + (int)(chunkSize * i);
                        Marshal.Copy(ptr, arr, 0, (int)actualchunkSize);
                        r.Headerordata.Chunk.Data = Common.CreateByteString(arr);
                    }

                    await responseStream.WriteAsync(r);
                }
#endif
            }

            size = v8.Count * noofBytesPerData;
            return size;
        }

        private async Task<long> v16WfmMethod(WaveformRequest request, IServerStreamWriter<RawReply> responseStream, ServerCallContext context, long size, object obj, RawReply r, uint chunksize, INativeVector<short> v16)
        {
            r.Headerordata.Header.Sourcename = request.Sourcename;
            var noofBytesPerData = r.Headerordata.Header.Sourcewidth;
            r.Headerordata.Chunk = new RawReply.Types.WaveformSampleByteChunk();

            var chunkSize = adjustedChunkSizeInBytes(chunksize, noofBytesPerData);
            var totalchuncks = totalNoofChunks((ulong)v16.Count, noofBytesPerData, chunkSize);
#if VERBOSE
                        EventLogger.AddEvent($"v16:Precharge Count={v16.NativeHorizontal.PrechargeCount}");
#endif

#if RAW
            var raw = obj as IRawInterface<short>;
#endif
            for (ulong i = 0; i < totalchuncks; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    break;

                var actualchunkSize = chunkSliceSizeInBytes(i, (ulong)v16.Count, noofBytesPerData, chunkSize);
                var chunkItemCount = chunkSize / noofBytesPerData;
                var chunkelements = actualchunkSize / noofBytesPerData;
                var basechunkindex = (long)(chunkItemCount * i);

                var arr = new byte[actualchunkSize];
#if RAW
                if (raw == null)
#endif
                {
                    unsafe
                    {
                        short* sptr;
                        using (var spinned = new Pinned(arr))
                            sptr = (short*)spinned.Pointer;
                        Parallel.ForEach(Partitioner.Create(0, (int)chunkelements), range =>
                        {
                            for (var j = range.Item1; j < range.Item2; j++)
                                *(sptr + j) = v16[basechunkindex + j];
                        });
                    }

                    r.Headerordata.Chunk.Data = Common.CreateByteString(arr);
                    await responseStream.WriteAsync(r);
                }
#if RAW
                else
                {
                    unsafe
                    {
                        var ptr = (IntPtr)(short*)raw.RawDataPtr + (int)(chunkSize * i);
                        Marshal.Copy(ptr, arr, 0, (int)actualchunkSize);
                        r.Headerordata.Chunk.Data = Common.CreateByteString(arr);
                    }

                    await responseStream.WriteAsync(r);
                }
#endif
            }

            size = v16.Count * noofBytesPerData;
            return size;
        }

        private static void ExtractRawHeader(string name, object item, RawReply reply)
        {
            reply.Headerordata = reply.Headerordata ?? new RawReply.Types.DataOrHeaderAccess();
            reply.Headerordata.Header = reply.Headerordata.Header ?? new WaveformHeader();

            var status = item as IDataStatus;

            if (item is INativeVector<short>)
            {
                var v16 = item as INativeVector<short>;
                v16HeaderMethod(name, reply, status, v16);
            }
            else if (item is INativeVector<sbyte>)
            {
                var v8 = item as INativeVector<sbyte>;
                v8HeaderMethod(name, reply, status, v8);
            }
            else if (item is INativeVector<float>)
            {
                var vf = item as INativeVector<float>;
                vfHeaderMethod(name, reply, status, vf);
            }
            else if (item is Tek.Scope.Support.INormalizedVector)
            {
                var nv = item as Tek.Scope.Support.INormalizedVector;
                nvHeaderMethod(name, reply, status, nv);
            }
            else
            {
                BugLogger.WriteErrorMessage($"[Server] - GetHeader: unknown type {item.GetType()}");
            }
        }

        private static void nvHeaderMethod(string name, RawReply reply, IDataStatus status, Tek.Scope.Support.INormalizedVector nv)
        {
            reply.Headerordata.Header.Verticalspacing = 1;
            reply.Headerordata.Header.Verticaloffset = 0;
            reply.Headerordata.Header.Horizontalspacing = nv.Horizontal.Spacing;
            reply.Headerordata.Header.Horizontalzeroindex = nv.Horizontal.ZeroIndex;
            reply.Headerordata.Header.Horizontalfractionalzeroindex = nv.Horizontal.FractionalZeroIndex;
            reply.Headerordata.Header.Noofsamples = (ulong)nv.Count;
            reply.Headerordata.Header.Sourcewidth = sizeof(float);
            reply.Headerordata.Header.Sourcename = name;
            reply.Headerordata.Header.Dataid = status != null ? (ulong)status.TOC : 0UL;
            reply.Headerordata.Header.Transid = status != null ? (ulong)status.TID : 0UL;
            reply.Headerordata.Header.Pairtype = WfmPairType.None;
            reply.Headerordata.Header.Wfmtype = WfmType.AnalogFloat;
            reply.Headerordata.Header.Hasdata = status?.HasData ?? true;
            reply.Headerordata.Header.Verticalunits = nv.Vertical.Units;
            reply.Headerordata.Header.HorizontalUnits = nv.Horizontal.Units;
        }

        private static void vfHeaderMethod(string name, RawReply reply, IDataStatus status, INativeVector<float> vf)
        {
            reply.Headerordata.Header.Verticalspacing = vf.NativeVertical.Increment;
            reply.Headerordata.Header.Verticaloffset = vf.NativeVertical.Offset;
            reply.Headerordata.Header.Horizontalspacing = vf.NativeHorizontal.Spacing;
            reply.Headerordata.Header.Horizontalzeroindex = vf.NativeHorizontal.IntegerZeroIndex;
            reply.Headerordata.Header.Horizontalfractionalzeroindex = vf.NativeHorizontal.FractionalZeroIndex;
            reply.Headerordata.Header.Noofsamples = (ulong)vf.Count;
            reply.Headerordata.Header.Sourcewidth = sizeof(float);
            reply.Headerordata.Header.Sourcename = name;
            reply.Headerordata.Header.Dataid = status != null ? (ulong)status.TOC : 0UL;
            reply.Headerordata.Header.Transid = status != null ? (ulong)status.TID : 0UL;
            reply.Headerordata.Header.Pairtype = WfmPairType.None;
            reply.Headerordata.Header.Bitmask = 0xffff;
            reply.Headerordata.Header.Wfmtype = WfmType.AnalogFloat;
            reply.Headerordata.Header.Hasdata = status?.HasData ?? true;
            reply.Headerordata.Header.Verticalunits = vf.NativeVertical.Units;
            reply.Headerordata.Header.HorizontalUnits = vf.NativeHorizontal.Units;
        }

        private static void v8HeaderMethod(string name, RawReply reply, IDataStatus status, INativeVector<sbyte> v8)
        {
#if VERBOSE
                    EventLogger.AddEvent(
                    $"v8:{name}:Vertical.Increment={v8.NativeVertical.Increment},Vertical.Offset={v8.NativeVertical.Offset},Vertical.Position={v8.NativeVertical.Position}");
#endif
            reply.Headerordata.Header.Verticalspacing = v8.NativeVertical.Increment;

            // This goofiness is caused by the offset being in DL rather than the speced voltage values.
            // This is probably why the ADK only allowed NormalizedVectors through the interface.
            reply.Headerordata.Header.Verticaloffset = -v8.NativeVertical.Offset * v8.NativeVertical.Increment;

            reply.Headerordata.Header.Horizontalspacing = v8.NativeHorizontal.Spacing;
            reply.Headerordata.Header.Horizontalzeroindex = v8.NativeHorizontal.IntegerZeroIndex;
            reply.Headerordata.Header.Horizontalfractionalzeroindex = v8.NativeHorizontal.FractionalZeroIndex;
            reply.Headerordata.Header.Noofsamples = (ulong)v8.Count;
            reply.Headerordata.Header.Sourcewidth = sizeof(sbyte);
            reply.Headerordata.Header.Sourcename = name;
            reply.Headerordata.Header.Dataid = status != null ? (ulong)status.TOC : 0UL;
            reply.Headerordata.Header.Transid = status != null ? (ulong)status.TID : 0UL;
            reply.Headerordata.Header.Pairtype = WfmPairType.None;
            reply.Headerordata.Header.Bitmask = 0xffff;
            reply.Headerordata.Header.Wfmtype = WfmType.Analog8;
            reply.Headerordata.Header.Hasdata = status?.HasData ?? true;
            reply.Headerordata.Header.Verticalunits = v8.NativeVertical.Units;
            reply.Headerordata.Header.HorizontalUnits = v8.NativeHorizontal.Units;
        }

        private static void v16HeaderMethod(string name, RawReply reply, IDataStatus status, INativeVector<short> v16)
        {
#if VERBOSE
                    EventLogger.AddEvent(
                    $"v16:{name}:Vertical.Increment={v16.NativeVertical.Increment},Vertical.Offset={v16.NativeVertical.Offset},Vertical.Position={v16.NativeVertical.Position}");
#endif
            reply.Headerordata.Header.Verticalspacing = v16.NativeVertical.Increment;

            // This goofiness is caused by the offset being in DL rather than the speced voltage values.
            // This is probably why the ADK only allowed NormalizedVectors through the interface.
            reply.Headerordata.Header.Verticaloffset = -v16.NativeVertical.Offset * v16.NativeVertical.Increment;

            reply.Headerordata.Header.Horizontalspacing = v16.NativeHorizontal.Spacing;
            reply.Headerordata.Header.Horizontalzeroindex = v16.NativeHorizontal.IntegerZeroIndex;
            reply.Headerordata.Header.Horizontalfractionalzeroindex = v16.NativeHorizontal.FractionalZeroIndex;
            reply.Headerordata.Header.Noofsamples = (ulong)v16.Count;
            reply.Headerordata.Header.Sourcewidth = sizeof(short);
            reply.Headerordata.Header.Sourcename = name;
            reply.Headerordata.Header.Dataid = status != null ? (ulong)status.TOC : 0UL;
            reply.Headerordata.Header.Transid = status != null ? (ulong)status.TID : 0UL;
            reply.Headerordata.Header.Pairtype = WfmPairType.None;
            reply.Headerordata.Header.Bitmask = 0xffff;
            reply.Headerordata.Header.Wfmtype = WfmType.Analog16;
            reply.Headerordata.Header.Hasdata = status?.HasData ?? true;
            reply.Headerordata.Header.Verticalunits = v16.NativeVertical.Units;
            reply.Headerordata.Header.HorizontalUnits = v16.NativeHorizontal.Units;
        }

        #region FromNitin

        private ulong adjustedChunkSizeInBytes(ulong requestedChunkSizeInBytes, uint dataPointSize)
        {
            requestedChunkSizeInBytes = requestedChunkSizeInBytes - 10;
            var dataAlignedBytes = requestedChunkSizeInBytes % dataPointSize;
            return requestedChunkSizeInBytes - dataAlignedBytes;
        }

        private ulong totalNoofChunks(ulong dataPoints, uint dataPointSize, ulong chunkSizeInBytes)
        {
            var nonfullchuncks = (ulong)(dataPoints * dataPointSize % chunkSizeInBytes != 0 ? 1 : 0);
            return dataPoints * dataPointSize / chunkSizeInBytes + nonfullchuncks;
        }

        private ulong chunkSliceSizeInBytes(ulong sliceNumber, ulong dataPoints, uint dataPointSize,
            ulong requestedChunkSizeInBytes)
        {
            ulong actualChunkSizeInBytes = 0;
            if (dataPoints == 0) return actualChunkSizeInBytes;

            var totalBytes = dataPoints * dataPointSize;
            if (totalBytes <= requestedChunkSizeInBytes)
            {
                actualChunkSizeInBytes = totalBytes;
            }
            else
            {
                if ((sliceNumber + 1) * requestedChunkSizeInBytes <= totalBytes)
                    actualChunkSizeInBytes = requestedChunkSizeInBytes;
                else
                    actualChunkSizeInBytes = totalBytes - sliceNumber * requestedChunkSizeInBytes;
            }

            return actualChunkSizeInBytes;
        }

        #endregion
    }
}