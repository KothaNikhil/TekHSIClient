//#define VERBOSE

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Tek.Scope;
using Tekscope;

namespace TekHighspeedAPI.Server
{
    public class NormDataImpl : NormalizedData.NormalizedDataBase
    {
        public IDataAccess DataAccess;

        public override Task<NormalizedReply> GetHeader(WaveformRequest request, ServerCallContext context)
        {
            EventLogger.AddEvent($"NormDataImpl.GetHeader({request.Sourcename})");
            var start = CurrentTime;
            var bConnected = DataAccess.IsConnected;
            var bDataIsAvailable = DataAccess.IsDataAvailable;
            var bDataIsExpectedType = DataAccess.Open(request.Sourcename) is INormalizedVector;
            var bSourceNameAvailable = !string.IsNullOrEmpty(request.Sourcename); // && _match.IsMatch(request.Sourcename);

            if (!bConnected)
            {
                var reply = new NormalizedReply
                {
                    Status = WfmReplyStatus.NoConnectionFailure,
                    Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                        { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                };
                return Task.Run(() => reply);
            }

            if (!bDataIsAvailable)
            {
                var reply = new NormalizedReply
                {
                    Status = WfmReplyStatus.OutsideSequenceFailure,
                    Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                        { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                };
                return Task.Run(() => reply);
            }

            if (!bDataIsExpectedType)
            {
                var reply = new NormalizedReply
                {
                    Status = WfmReplyStatus.TypeMismatchFailure,
                    Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                        { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                };
                return Task.Run(() => reply);
            }

            if (!bSourceNameAvailable)
            {
                var reply = new NormalizedReply
                {
                    Status = WfmReplyStatus.SourcenameMissingFailure,
                    Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                        { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                };
                return Task.Run(() => reply);
            }
            else
            {
                var reply = new NormalizedReply
                {
                    Status = WfmReplyStatus.Success,
                    Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                    {
                        Header = new WaveformHeader { Sourcename = request.Sourcename }
                    }
                };

                ExtractHeader(DataAccess.Open(request.Sourcename), reply);
                reply.Status = WfmReplyStatus.Success;


                EventLogger.AddEvent(
                    $"[Client] GetNormalizedVector({request.Sourcename}):{reply.Headerordata.Header.Wfmtype} - {Tek.Scope.Support.EngineeringNotationFormatter.Format(CurrentTime - start)}S");
                return Task.Run(() => reply);
            }
        }

        private void ExtractHeader(object obj, NormalizedReply reply)
        {
            var nv = obj as INormalizedVector;
            var status = obj as IDataStatus;
            if (nv == null) return;
            reply.Headerordata.Header.Horizontalfractionalzeroindex = nv.Horizontal.ZeroIndex;
            reply.Headerordata.Header.Horizontalspacing = nv.Horizontal.Spacing;
            reply.Headerordata.Header.Horizontalzeroindex = nv.Horizontal.IntegerZeroIndex;
            reply.Headerordata.Header.Sourcewidth = sizeof(float);
            reply.Headerordata.Header.Noofsamples = (ulong)nv.Count;
            reply.Headerordata.Header.Pairtype = WfmPairType.None;
            reply.Headerordata.Header.Wfmtype = WfmType.AnalogFloat;
            reply.Headerordata.Header.Hasdata = status?.HasData ?? true;
            reply.Headerordata.Header.Verticalunits = nv.Vertical.Units;
            reply.Headerordata.Header.HorizontalUnits = nv.Horizontal.Units;
        }

        public override async Task GetWaveform(WaveformRequest request, IServerStreamWriter<NormalizedReply> responseStream,
            ServerCallContext context)
        {
            EventLogger.AddEvent($"NativeDataImpl.GetWaveform({request.Sourcename})");
            var start = CurrentTime;
            try
            {
                var nv = DataAccess.Open(request.Sourcename) as INormalizedVector;

                var bConnected = DataAccess.IsConnected;
                var bDataIsAvailable = DataAccess.IsDataAvailable;
                var bDataIsExpectedType = DataAccess.Open(request.Sourcename) is INormalizedVector;
                var bSourceNameAvailable = !string.IsNullOrEmpty(request.Sourcename); // && _match.IsMatch(request.Sourcename);

                if (!bConnected)
                {
                    var reply = new NormalizedReply
                    {
                        Status = WfmReplyStatus.NoConnectionFailure,
                        Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                            { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                    };
                    await responseStream.WriteAsync(reply);
                }
                else if (!bDataIsAvailable)
                {
                    var reply = new NormalizedReply
                    {
                        Status = WfmReplyStatus.OutsideSequenceFailure,
                        Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                            { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                    };
                    await responseStream.WriteAsync(reply);
                }
                else if (!bDataIsExpectedType)
                {
                    var reply = new NormalizedReply
                    {
                        Status = WfmReplyStatus.TypeMismatchFailure,
                        Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                            { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                    };
                    await responseStream.WriteAsync(reply);
                }
                else if (!bSourceNameAvailable)
                {
                    var reply = new NormalizedReply
                    {
                        Status = WfmReplyStatus.SourcenameMissingFailure,
                        Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                            { Header = new WaveformHeader { Sourcename = request.Sourcename } }
                    };
                    await responseStream.WriteAsync(reply);
                }
                else
                {
                    var r = new NormalizedReply
                    {
                        Headerordata = new NormalizedReply.Types.DataOrHeaderAccess
                        {
                            Header = new WaveformHeader
                            {
                                Sourcename = request.Sourcename,
                                Noofsamples = (ulong)nv.Count
                            }
                        }
                    };

                    var chunksize = request.Chunksize > 0 ? request.Chunksize : Common.DefaultChunkSize;
                    r.Status = WfmReplyStatus.Success;
                    r.Headerordata.Header.Sourcename = request.Sourcename;
                    r.Headerordata.Header.Sourcewidth = sizeof(float);
                    r.Headerordata.Header.Wfmtype = WfmType.AnalogFloat;
                    uint noofBytesPerData = sizeof(float);
                    r.Headerordata.Chunk = new NormalizedReply.Types.WaveformSampleChunk();

                    var chunkSize = adjustedChunkSizeInBytes(chunksize, noofBytesPerData);
                    var totalchuncks = totalNoofChunks((ulong)nv.Count, noofBytesPerData, chunkSize);
                    var chunkbase = 0;

                    for (long i = 0; i < (int)totalchuncks; i++)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                            break;

                        var actualchunkSize = chunkSliceSizeInBytes((ulong)i, (ulong)nv.Count, noofBytesPerData, chunkSize);
                        var chunkelements = actualchunkSize / noofBytesPerData;
                        var outbuf = new float[chunkelements];

                        Parallel.ForEach(Partitioner.Create(0, (int)chunkelements), range =>
                        {
                            for (var j = range.Item1; j < range.Item2; j++)
                                outbuf[j] = (float)nv[chunkbase + j];
                        });

                        chunkbase += (int)chunkelements;

                        r.Headerordata.Chunk.Data.AddRange(outbuf);
                        await responseStream.WriteAsync(r);
                    }
                }
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
            }
        
            EventLogger.AddEvent(
                $"[Client] GetWaveform({request.Sourcename}) - {Tek.Scope.Support.EngineeringNotationFormatter.Format(CurrentTime - start)}S");
        }

        #region CurrentTime

        /// <exclude />
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        /// <summary>
        ///     Returns a current time indicator that is useful for doing time delta measurements.
        /// </summary>
        /// <returns></returns>
        public static double CurrentTime => _stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;

        #endregion

        #region FromNitin

        private ulong adjustedChunkSizeInBytes(ulong requestedChunkSizeInBytes, uint dataPointSize)
        {
            requestedChunkSizeInBytes = requestedChunkSizeInBytes - 10;
            var dataAlignedBytes = requestedChunkSizeInBytes % dataPointSize;
            return requestedChunkSizeInBytes - dataAlignedBytes;
        }

        private ulong totalNoofChunks(ulong dataPoints, uint dataPointSize, ulong chunkSizeInBytes)
        {
            var totalSizeInBytes = dataPoints * dataPointSize;
            var overflow = totalSizeInBytes % chunkSizeInBytes != 0 ? 1UL : 0UL;
            return totalSizeInBytes / chunkSizeInBytes + overflow;
        }

        private ulong chunkSliceSizeInBytes(ulong sliceNumber, ulong dataPoints, uint dataPointSize,
            ulong requestedChunkSizeInBytes)
        {
            ulong actualChunkSizeInBytes = 0;
            if (dataPoints == 0)
                return actualChunkSizeInBytes;

            var totalBytes = (long)(dataPoints * dataPointSize);

            if (sliceNumber == 0 && totalBytes < (long)requestedChunkSizeInBytes)
                return (ulong)totalBytes;

            var currentBytes = (long)(sliceNumber * requestedChunkSizeInBytes);
            var remainingBytes = totalBytes - currentBytes;

            if (remainingBytes >= (long)requestedChunkSizeInBytes)
                return requestedChunkSizeInBytes;

            if (remainingBytes > 0 && requestedChunkSizeInBytes > (ulong) remainingBytes)
                return (ulong) remainingBytes;

            if (remainingBytes < 0)
                return 0;

            return (ulong)(totalBytes - remainingBytes);
        }

        #endregion
    }
}