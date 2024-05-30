//#define VERBOSE
using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using TekHighspeedAPI;
using Tekscope;
using Status = TekHighspeedAPI.Status;

namespace TekServer
{
    public class SCPICommandImpl : SCPIControl.SCPIControlBase
    {
        ConDictionary<string, ISCPIAccess> _connections = new ConDictionary<string, ISCPIAccess>();
        ConDictionary<string, ISCPIAccess> _ids = new ConDictionary<string, ISCPIAccess>();
        ConDictionary<string, string> _id2connection = new ConDictionary<string, string>();

        public IDataAccess DataAccess = null;

        public override Task<SCPIConnectResponse> Connect(SCPIConnectRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[SCPICommandImpl] - Connect('{request.Clientname}')");
#endif
            if (DataAccess == null) return base.Connect(request, context);
            if (_connections.ContainsKey(request.Clientname)) return base.Connect(request, context);
            _connections.Add(request.Clientname, (ISCPIAccess)DataAccess);
            var connection = ((ISCPIAccess)DataAccess).Connect(request.Clientname);
            var id = connection.Item2.ToString();
            _id2connection[id] = request.Clientname;
            _ids[id] = (ISCPIAccess)DataAccess;
            return Task.Run(() => new SCPIConnectResponse { ID = id });
        }


        public override Task<SCPIStatusResponse> Disconnect(SCPIDisconnectRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[SCPICommandImpl] - Disconnect('{request.ID}')");
#endif
            var id = request.ID;
            var connection = _ids[id];
            if (connection != null)
            {
                return Task.Run(() => new SCPIStatusResponse { Status = (int)connection.Disconnect() });
            }

            return Task.Run(() => new SCPIStatusResponse { Status = (int) Status.ERROR_SYSTEM_ERROR });
        }

        public override async Task Read(SCPIReadRequest request, IServerStreamWriter<SCPIReadResponse> responseStream, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[SCPICommandImpl] - Read('{request.ID}')");
#endif
            var starttime = BugLogger.CurrentTime;
            double data_transferred = 0;

            try
            {
                using (LogMetric.Metric("Time", "Read"))
                {
                    var connection = _ids[request.ID];
                    var chunksize = (request.Chunksize > 0 && request.Chunksize < 4e6
                        ? (request.Chunksize / 8) * 8
                        : 80000);
                    if (connection != null)
                    {
                        var r = new SCPIReadResponse();
                        var response = connection.ReadRawBinary();
                        if (response.Item1 < 0)
                            await responseStream.WriteAsync(new SCPIReadResponse
                                { Status = (int)response.Item1, Response = ByteString.Empty });
                        else
                        {

                            var bytes = response.Item2;
                            var buffers = bytes.Length % chunksize != 0
                                ? bytes.Length / chunksize + 1
                                : bytes.Length / chunksize;
                            var buffer = new byte[chunksize];
#if VERBOSE
                            EventLogger.AddEvent($"[SCPICommandImpl] - Read() - buffersize:{response.Item2.Length}, buffercount:{buffers}");
                            var starttime = BugLogger.CurrentTime;
#endif
                            for (int i = 0; i < buffers; i++)
                            {
                                if (context.CancellationToken.IsCancellationRequested)
                                    break;

                                var remaining = bytes.Length - chunksize * i;

                                if (remaining < chunksize)
                                {
                                    Array.Copy(bytes, i * chunksize, buffer, 0, remaining);
                                    r.Response = Common.CreateByteString(buffer);
                                    r.ResponseSize = remaining;
                                    r.Totalsize = bytes.Length;
                                    data_transferred += remaining;
                                    await responseStream.WriteAsync(r);
                                }
                                else
                                {
                                    Array.Copy(bytes, i * chunksize, buffer, 0, chunksize);
                                    r.Response = Common.CreateByteString(buffer);
                                    r.ResponseSize = chunksize;
                                    r.Totalsize = bytes.Length;
                                    data_transferred += chunksize;
                                    await responseStream.WriteAsync(r);
                                }
                            }
#if VERBOSE
                            var duration = BugLogger.CurrentTime - starttime;
                            EventLogger.AddEvent($"[SCPICommandImpl] - Read() - buffersize:{bytes.Length}, XRate:{(bytes.Length*8)/(1e6*duration)}, Time:{duration}");
#endif
                        }
                        LogMetric.UpdateMetric("DataRate", "Read", data_transferred / (BugLogger.CurrentTime - starttime));
                    }
                    else
                    {
                        await responseStream.WriteAsync(new SCPIReadResponse
                            { Status = -1, Totalsize = 0, Response = ByteString.Empty, ResponseSize = 0 });
                    }
                }
            }
            catch (Exception e)
            {
                BugLogger.CatchException(e);
            }
        }

        public override async Task Query(SCPIQueryRequest request, IServerStreamWriter<SCPIReadResponse> responseStream, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[SCPICommandImpl] - Query({request.ID}, '{Encoding.ASCII.GetString(request.Message.ToByteArray())}')");
#endif
            var starttime = BugLogger.CurrentTime;
            double data_transferred = 0;
            try
            {
                using (LogMetric.Metric("Time", "Query"))
                {
                   
                    var command = request.Message;
                    var connection = _ids[request.ID];
                    if (connection != null && !command.IsEmpty)
                    {
                        if (context.CancellationToken.IsCancellationRequested)
                        {
#if VERBOSE
                            EventLogger.AddEvent($"[SCPICommandImpl] - Query() - operation cancelled - 1");
#endif
                        }

                        var status = connection.Write(Encoding.ASCII.GetString(command.ToByteArray()));
                        if (status >= 0)
                        {
                            var chunksize = (request.Chunksize > 0 && request.Chunksize < 4e6
                                ? (request.Chunksize / 8) * 8
                                : 80000);
                            var r = new SCPIReadResponse();
                            var response = connection.ReadRawBinary();
                            if (response.Item1 < 0 && response.Item2.Length <= 0)
                            {
                                await responseStream.WriteAsync(new SCPIReadResponse
                                    { Status = (int)response.Item1, Response = ByteString.Empty });

#if VERBOSE
                                EventLogger.AddEvent($"[SCPICommandImpl] - Query() - {response.Item1} - {request.ID}");
#endif
                                return;
                            }

                            var bytes = response.Item2;
                            var buffers = bytes.Length % chunksize != 0
                                ? bytes.Length / chunksize + 1
                                : bytes.Length / chunksize;
                            var buffer = new byte[chunksize];
                            for (int i = 0; i < buffers; i++)
                            {

                                if (context.CancellationToken.IsCancellationRequested)
                                {
#if VERBOSE
                                    EventLogger.AddEvent($"[SCPICommandImpl] - Query() - operation cancelled - 2");
#endif
                                    break;
                                }

                                var remaining = bytes.Length - chunksize * i;

                                if (remaining < chunksize)
                                {
#if VERBOSE
                                    EventLogger.AddEvent($"[SCPICommandImpl] -  last buffer:{i}");
#endif
                                    buffer = new byte[remaining];
                                    Array.Copy(bytes, i * chunksize, buffer, 0, remaining);
                                    r.Response = Common.CreateByteString(buffer);
                                    r.ResponseSize = remaining;
                                    r.Totalsize = bytes.Length;
                                    data_transferred += remaining;
                                    await responseStream.WriteAsync(r);
                                }
                                else
                                {
#if VERBOSE
                                    EventLogger.AddEvent($"[SCPICommandImpl] -  buffer:{i}");
#endif
                                    Array.Copy(bytes, i * chunksize, buffer, 0, chunksize);
                                    r.Response = Common.CreateByteString(buffer);
                                    r.ResponseSize = chunksize;
                                    r.Totalsize = bytes.Length;
                                    data_transferred += chunksize;
                                    await responseStream.WriteAsync(r);
                                }
                            }
                            LogMetric.UpdateMetric("DataRate", "Query", data_transferred / (BugLogger.CurrentTime - starttime));
                            return;
                        }

                        await responseStream.WriteAsync(new SCPIReadResponse
                            { Status = (int)status, Totalsize = 0, Response = ByteString.Empty, ResponseSize = 0 });
                        
                        return;
                    }

                    await responseStream.WriteAsync(new SCPIReadResponse
                        { Status = -1, Totalsize = 0, Response = ByteString.Empty, ResponseSize = 0 });

                }
            }
            catch (Exception ex)
            {
                BugLogger.CatchException(ex);
            }
        }

        public override Task<SCPIStatusResponse> Write(SCPIWriteRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[SCPICommandImpl] - Write({request.ID},'{Encoding.ASCII.GetString(request.Message.ToByteArray())}') - {request.ID}");
#endif
            using (LogMetric.Metric("Time", "Write"))
            {
                ByteString command = request.Message;
                var connection = _ids[request.ID];
                if (connection != null && !command.IsEmpty)
                {
                    return Task.Run(() => new SCPIStatusResponse
                        { Status = (int)connection.Write(Encoding.ASCII.GetString(command.ToByteArray())) });
                }
                return Task.Run(() => new SCPIStatusResponse { Status = -1 });
            }
        }

        public override Task<SCPISTBReadResponse> ReadSTB(SCPIReadSTBRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[SCPICommandImpl] - ReadSTB({request.ID})");
#endif
            var connection = _ids[request.ID];
            if (connection != null)
            {
                return Task.Run(() =>
                {
                    var response = connection.ReadSTB();
                    return new SCPISTBReadResponse { Status = (int)response.Item1, Response = response.Item2 };
                });
            }

            return Task.Run(() => new SCPISTBReadResponse { Status = -1 });
        }

        public override Task<SCPIStatusResponse> Clear(SCPIClearRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[SCPICommandImpl] - Clear({request.ID})");
#endif
            using (LogMetric.Metric("Time", "Clear"))
            {
                var connection = _ids[request.ID];
                if (connection != null)
                {
                    return Task.Run(() =>
                    {
                        var response = connection.Clear();
                        return new SCPIStatusResponse { Status = (int)response };
                    });
                }

                return Task.Run(() => new SCPIStatusResponse { Status = -1 });
            }
        }

//        public override Task<SCPIStatusResponse> SetTimeout(SCPITimeoutSetRequest request, ServerCallContext context)
//        {
//#if VERBOSE
//            EventLogger.AddEvent($"[SCPICommandImpl] - SetTimeout({request.ID}, {request.Timeout})");
//#endif
//            var connection = _ids[request.ID];
//            if (connection != null)
//            {
//                return Task.Run(() =>
//                {
//                    var response = connection.SetTimeout(request.Timeout);
//                    return new SCPIStatusResponse { Status = (int)response };
//                });
//            }

//            return Task.Run(() => new SCPIStatusResponse { Status = -1 });
//        }

//        public override Task<SCPITimeoutGetResponse> GetTimeout(SCPITimeoutGetRequest request, ServerCallContext context)
//        {
//#if VERBOSE
//            EventLogger.AddEvent($"[SCPICommandImpl] - GetTimeout({request.ID})");
//#endif
//            var connection = _ids[request.ID];
//            if (connection != null)
//            {
//                return Task.Run(() =>
//                {
//                    var response = connection.GetTimeout();
//                    return new SCPITimeoutGetResponse { Status = (int)response.Item1, Timeout = response.Item2};
//                });
//            }

//            return Task.Run(() => new SCPITimeoutGetResponse { Status = -1, Timeout = 0 });
//        }

//        public override Task<SCPIStatusResponse> Lock(SCPILockRequest request, ServerCallContext context)
//        {
//            var connection = _ids[request.ID];
//            if (connection != null)
//            {
//                return Task.Run(() =>
//                {
//                    var response = connection.Lock(request.Timeout);
//                    return new SCPIStatusResponse { Status = (int)response };
//                });
//            }

//            return Task.Run(() => new SCPIStatusResponse { Status = -1 });
//        }

//        public override Task<SCPIStatusResponse> Unlock(SCPIUnlockRequest request, ServerCallContext context)
//        {
//            var connection = _ids[request.ID];
//            if (connection != null)
//            {
//                return Task.Run(() =>
//                {
//                    var response = connection.Unlock();
//                    return new SCPIStatusResponse { Status = (int)response };
//                });
//            }

//            return Task.Run(() => new SCPIStatusResponse { Status = -1 });
//        }
    }
}
