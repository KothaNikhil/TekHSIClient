
//#define VERBOSE
using Grpc.Core;
using System.Threading.Tasks;
using Tekscope;

namespace TekHighspeedAPI.Server
{
    /// <summary>   A connection implementation. </summary>
    public class ConnectionImpl : Connect.ConnectBase
    {
        /// <summary>   The data access. </summary>
        public IDataAccess DataAccess;
        /// <summary>   The previous. </summary>
        private double _prev = 0;

        /// <summary>   Connect to instrument. </summary>
        /// <param name="request">  The request. </param>
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   A ConnectReply. </returns>

        public override Task<ConnectReply> Connect(ConnectRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[gRPC] Connect:request={request.Name},context={context.Method}");
#endif
            string requestedName = string.IsNullOrEmpty(request.Name) ? "gRPCServer" : request.Name;

#if RESTRICT_TO_ONE_CONNECTION
        if (DataAccess.IsConnected)
        {
            return Task.Run(() =>
                new ConnectReply { Status = ConnectStatus.InuseFailure });
        }
#endif
            return Task.Run(() =>
                new ConnectReply { Status = DataAccess.Connect(requestedName) ? ConnectStatus.Success : ConnectStatus.UnknownFailure });
        }

        /// <summary>
        /// Waits for client access to data. When it returns you may access NormalizedData or NativeData.
        /// </summary>
        /// <param name="request">  The request. </param>
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   A ConnectReply. </returns>

        public override Task<ConnectReply> WaitForDataAccess(ConnectRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[gRPC] WaitForDataAccess:request={request.Name},context={context.Method}");
#endif
            return Task.Run(() => new ConnectReply { Status = DataAccess.WaitForSequence() ? ConnectStatus.Success : ConnectStatus.UnknownFailure });
        }

        /// <summary>   Releases access to data (must occur after WaitForDataAccess) </summary>
        /// <param name="request">  The request. </param>
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   A ConnectReply. </returns>

        public override Task<ConnectReply> FinishedWithDataAccess(ConnectRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[gRPC] FinishedWithDataAccess:request={request.Name},context={context.Method}");
#endif
            double current = Common.CurrentTime;
            if (_prev > 0)
            {
                double delta = current - _prev;
                if (delta > 0)
                {
                    LogMetric.UpdateMetric("Metric", "Update Rate", 1/delta, "/S");
                    LogMetric.UpdateMetric("Metric", "Update Time", delta, "S");
                }
            }
            _prev = current;
            return Task.Run(() => new ConnectReply { Status = DataAccess.FinishedWithSequence(true) ? ConnectStatus.Success : ConnectStatus.UnknownFailure });
        }

        /// <summary>   Returns a list of names of available data. </summary>
        /// <param name="request">  The request. </param>
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   An AvailableNamesReply. </returns>

        public override Task<AvailableNamesReply> RequestAvailableNames(ConnectRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[gRPC] RequestAvailableNames:request={request.Name},context={context.Method}");
#endif
            var reply = new AvailableNamesReply { Status = ConnectStatus.Success };
            reply.Symbolnames.Add(DataAccess.Names);
            return Task.Run(() => reply);
        }

        /// <summary>   Force new sequence. This requests access to data. </summary>
        /// <param name="request">  The request. </param>
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   A ConnectReply. </returns>

        public override Task<ConnectReply> RequestNewSequence(ConnectRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[gRPC] RequestNewSequence:request={request.Name},context={context.Method}");
#endif
            return Task.Run(() => new ConnectReply { Status = DataAccess.StartSequence() ? ConnectStatus.Success : ConnectStatus.UnknownFailure });
        }

        /// <summary>   Disconnect from instrument. </summary>
        /// <param name="request">  The request. </param>
        /// <param name="context">  The context. </param>
        ///
        /// <returns>   A ConnectReply. </returns>

        public override Task<ConnectReply> Disconnect(ConnectRequest request, ServerCallContext context)
        {
#if VERBOSE
            EventLogger.AddEvent($"[gRPC] Disconnect:request={request.Name},context={context.Method}");
#endif
            return Task.Run(() => new ConnectReply { Status = DataAccess.Disconnect() ? ConnectStatus.Success : ConnectStatus.UnknownFailure });
        }
    }
}