#define SKETCHY
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Google.Protobuf;

namespace TekHighspeedAPI
{
    /// <summary>
    /// Class used to get a pin and get a pointer
    /// </summary>
    public class Pinned : IDisposable
    {
        #region Fields
        private GCHandle _pinnedItem;
        private bool _disposed = false;
        #endregion

        #region Pointer
        /// <summary>
        /// return pinned pointer to object
        /// </summary>
        public IntPtr Pointer => _pinnedItem.AddrOfPinnedObject();
        #endregion

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="obj"></param>
        public Pinned(Object obj)
        {
            _pinnedItem = GCHandle.Alloc(obj, GCHandleType.Pinned);
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _pinnedItem.Free();
            }
            _disposed = true;
        }
        #endregion
    }

    public class Common
    {
        /// <summary>   Larger than this is bad for GC. </summary>
        public static uint DefaultChunkSize = 80000;

#if SKETCHY
        private static MethodInfo attachbytes;
#endif

#if SKETCHY

        /// <summary>   Gets the get byte string attach. This is sketchy because
        ///             it calls an internal static method in ByteString to reduce
        ///             a copy. That copy is there for security reasons. </summary>
        ///
        /// <value> The get byte string attach. </value>

        private static MethodInfo GetByteStringAttach
        {
            get
            {
                if (attachbytes != null) return attachbytes;

                foreach (var method in typeof(ByteString).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
                    if (string.CompareOrdinal(method.Name, "AttachBytes") == 0)
                    {
                        var pars = method.GetParameters();
                        if (pars.Length == 1 && pars[0].ParameterType == typeof(byte[]))
                        {
                            attachbytes = method;
                            return method;
                        }
                    }

                return null;
            }
        }
#endif

        /// <summary>   Creates byte string. </summary>
        /// <param name="arg">  byte array to wrap with a ByteString. </param>
        ///
        /// <returns>   The new byte string. </returns>

        public static ByteString CreateByteString(byte[] arg)
        {
#if SKETCHY
            return (ByteString)GetByteStringAttach?.Invoke(null, new object[] { arg });
#else
        return ByteString.CopyFrom(arg);
#endif
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
    }
}