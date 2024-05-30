//#define EnableLockLedger
//#define EnableLockLedgerLight

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace TekHighspeedAPI
{
#if EnableLockLedger || EnableLockLedgerLight
    #region LockLedgerType

    /// <summary>
    ///     Type of lock status
    /// </summary>
    public enum LockLedgerType
    {
        /// <summary>
        ///     Requesting to take a Read lock
        /// </summary>
        RequestRead,

        /// <summary>
        ///     Taking a Read Lock
        /// </summary>
        TakeRead,

        /// <summary>
        ///     Requesting a write lock
        /// </summary>
        RequestWrite,

        /// <summary>
        ///     Taking a write lock.
        /// </summary>
        TakeWrite
    }

    #endregion

    #region LedgerLockInfo

    /// <summary>
    ///     Lock Info for holding in a ledger
    /// </summary>
    public struct LockLedgerInfo
    {
        /// <summary>
        ///     Unique ID for the lock
        /// </summary>
        public Guid ID { get; set; }

        /// <summary>
        ///     Thread ID the request occured on
        /// </summary>
        public int ThreadID { get; set; }

        /// <summary>
        ///     Requested lock type
        /// </summary>
        public LockLedgerType Type { get; set; }

        /// <summary>
        ///     Signature for this ID and ThreadID
        /// </summary>
        public ulong Hash { get; set; }

#if !EnableLockLedgerLight
        // Stack frame
        public StackFrame[] Stack { get; set; }
#else
        public string File {get;set;}
        public string Method { get; set; }
        public int Line { get; set; }
#endif
    }

    #endregion
#endif

    #region LockLedger

    /// <summary>
    /// </summary>
    public class LockLedger
    {
#if EnableLockLedger || EnableLockLedgerLight
        public static Dictionary<ulong, Stack<LockLedgerInfo>>
            _tracker = new Dictionary<ulong, Stack<LockLedgerInfo>>();
        /// <summary>
        ///     Make Unique Hash
        /// </summary>
        /// <param name="id"></param>
        /// <param name="threadid"></param>
        /// <returns></returns>
        private static ulong ToHash(Guid id, int threadid)
        {
            unchecked
            {
                return ((ulong) id.GetHashCode() * 7919) ^ (ulong) threadid;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        public static void RequestsReadLock(Guid id, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
#if !EnableLockLedgerLight
            var st = new StackTrace(true);
            var frames = st.GetFrames();
#endif
            var threadid = Thread.CurrentThread.ManagedThreadId;
            var hash = ToHash(id, threadid);
            lock (_tracker)
            {
                if (!_tracker.ContainsKey(hash)) _tracker.Add(hash, new Stack<LockLedgerInfo>());
#if !EnableLockLedgerLight
                _tracker[hash].Push(new LockLedgerInfo
                    {ID = id, ThreadID = threadid, Type = LockLedgerType.RequestRead, Stack = frames, Hash = hash});
#else
                _tracker[hash].Push(new LockLedgerInfo
                { ID = id, ThreadID = threadid, Type = LockLedgerType.RequestRead, Method = memberName, File =
 sourceFilePath, Line = sourceLineNumber, Hash = hash });
#endif
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        public static void TakesReadLock(Guid id, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
#if !EnableLockLedgerLight
            var st = new StackTrace(true);
            var frames = st.GetFrames();
#endif
            var threadid = Thread.CurrentThread.ManagedThreadId;
            var hash = ToHash(id, threadid);
            lock (_tracker)
            {
                if (!_tracker.ContainsKey(hash))
                {
                    _tracker.Add(hash, new Stack<LockLedgerInfo>());
#if !EnableLockLedgerLight
                    _tracker[hash].Push(new LockLedgerInfo
                        {ID = id, ThreadID = threadid, Type = LockLedgerType.TakeRead, Stack = frames, Hash = hash});
#else
                    _tracker[hash].Push(new LockLedgerInfo
                    { ID = id, ThreadID = threadid, Type = LockLedgerType.TakeRead, Method = memberName, File =
 sourceFilePath, Line = sourceLineNumber, Hash = hash });
#endif
                }
                else
                {
                    _tracker[hash].Pop();
#if !EnableLockLedgerLight
                    _tracker[hash].Push(new LockLedgerInfo
                        {ID = id, ThreadID = threadid, Type = LockLedgerType.TakeRead, Stack = frames, Hash = hash});
#else
                    _tracker[hash].Push(new LockLedgerInfo
                    { ID = id, ThreadID = threadid, Type = LockLedgerType.TakeRead, Method = memberName, File =
 sourceFilePath, Line = sourceLineNumber, Hash = hash });
#endif
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        public static void ReleasesReadLock(Guid id, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var threadid = Thread.CurrentThread.ManagedThreadId;
            var hash = ToHash(id, threadid);
            lock (_tracker)
            {
                _tracker[hash].Pop();
                if (_tracker[hash].Count == 0)
                    _tracker.Remove(hash);
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        public static void RequestsWriteLock(Guid id, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
#if !EnableLockLedgerLight
            var st = new StackTrace(true);
            var frames = st.GetFrames();
#endif
            var threadid = Thread.CurrentThread.ManagedThreadId;
            var hash = ToHash(id, threadid);
            lock (_tracker)
            {
                if (!_tracker.ContainsKey(hash)) _tracker.Add(hash, new Stack<LockLedgerInfo>());
#if !EnableLockLedgerLight
                _tracker[hash].Push(new LockLedgerInfo
                    {ID = id, ThreadID = threadid, Type = LockLedgerType.RequestWrite, Stack = frames, Hash = hash});
#else
                _tracker[hash].Push(new LockLedgerInfo
                { ID = id, ThreadID = threadid, Type = LockLedgerType.RequestWrite, Method = memberName, File =
 sourceFilePath, Line = sourceLineNumber, Hash = hash });
#endif
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        public static void TakesWriteLock(Guid id,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
#if !EnableLockLedgerLight
            var st = new StackTrace(true);
            var frames = st.GetFrames();
#endif
            var threadid = Thread.CurrentThread.ManagedThreadId;
            var hash = ToHash(id, threadid);
            lock (_tracker)
            {
                if (!_tracker.ContainsKey(hash))
                {
                    _tracker.Add(hash, new Stack<LockLedgerInfo>());
#if !EnableLockLedgerLight
                    _tracker[hash].Push(new LockLedgerInfo
                        {ID = id, ThreadID = threadid, Type = LockLedgerType.TakeWrite, Stack = frames, Hash = hash});
#else
                    _tracker[hash].Push(new LockLedgerInfo
                    { ID = id, ThreadID = threadid, Type = LockLedgerType.TakeWrite, Method = memberName, File =
 sourceFilePath, Line = sourceLineNumber, Hash = hash });
#endif
                }
                else
                {
                    Debug.Assert(_tracker.Count > 0);
                    _tracker[hash].Pop();
#if !EnableLockLedgerLight
                    _tracker[hash].Push(new LockLedgerInfo
                        {ID = id, ThreadID = threadid, Type = LockLedgerType.TakeWrite, Stack = frames, Hash = hash});
#else
                    _tracker[hash].Push(new LockLedgerInfo
                    { ID = id, ThreadID = threadid, Type = LockLedgerType.TakeWrite, Method = memberName, File =
 sourceFilePath, Line = sourceLineNumber, Hash = hash });
#endif
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        public static void ReleasesWriteLock(Guid id, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var threadid = Thread.CurrentThread.ManagedThreadId;
            var hash = ToHash(id, threadid);
            lock (_tracker)
            {
                _tracker[hash].Pop();
                if (_tracker[hash].Count == 0)
                    _tracker.Remove(hash);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static string HTMLReport()
        {
            var sb = new StringBuilder();
            lock (_tracker)
            {
                foreach (var key in _tracker.Keys)
                {
                    sb.Append($@"<h3>Hash ID: <b>{key}</b></h3>");
                    var item = _tracker[key].Peek();
                    sb.Append($@"<p>Lock ID: <b>{item.ID}</b> - Lock Type: <b>{item.Type}</b> - Thread ID: <b>{item.ThreadID}</b></p>");
#if !EnableLockLedgerLight
                    if (item.Stack != null && item.Stack.Length > 0) sb.Append(StackToTable(item.Stack));
#else
                    if (!string.IsNullOrEmpty(item.Method) && !string.IsNullOrEmpty(item.File))
                        sb.Append($"Method:{item.Method}, File:{Path.GetFileName(item.File)}, Line:{item.Line}");
#endif
                }
            }

            return sb.ToString();
        }

#if !EnableLockLedgerLight
        /// <summary>
        /// </summary>
        /// <param name="stack"></param>
        /// <returns></returns>
        private static string StackToTable(StackFrame[] stack)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div>");
            sb.AppendLine(
                @"<table><tr><th>File Name</th><th>Line Number</th><th>Method</th><tr>");

            if (stack != null)
                for (var i = 0; i < stack.Length; i++)
                {
                    var item = stack[i];
                    sb.AppendLine($@"<tr><td>{Path.GetFileName(item.GetFileName())}</td>
                                        <td>{item.GetFileLineNumber()}</td>
                                        <td>{item.GetMethod()}</td>
                                        </tr>");
                }

            sb.AppendLine("</table></div>");

            return sb.ToString();
        }
#endif
        /// <summary>
        /// </summary>
        public static bool LockTrackingState => true;
#else
        /// <summary>
        /// </summary>
        public static bool LockTrackingState => false;

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static string HTMLReport()
        {
            return "<p>Lock tracking disabled</p>";
        }
#endif
    }

    #endregion

    #region ConLockWrapper

    /// <summary>
    ///     Wrapper for a ReaderWriterLockSlim that works with the LockLedger
    /// </summary>
    public class ConLockWrapper
    {
        #region Logged

        /// <summary>
        ///     Enabled LockLedger logging
        /// </summary>
        public bool Logged { get; set; } = true;

        #endregion

        #region IsLocked

        /// <summary>
        /// </summary>
        public bool IsLocked => IsWriteLocked;

        #endregion

        #region IsWriteLocked

        /// <summary>
        /// </summary>
        public bool IsWriteLocked => _lock.IsWriteLockHeld;

        #endregion

        #region IsReadLocked

        /// <summary>
        /// </summary>
        public bool IsReadLocked => _lock.IsReadLockHeld;

        #endregion

        #region ReadLock

        /// <summary>
        ///     Gives Read access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConReadLock ReadLock => new ConReadLock(_lock);

        #endregion

        #region WriteLock

        /// <summary>
        ///     Gives Write access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConWriteLock WriteLock => new ConWriteLock(_lock);

        #endregion

        #region Lock

        /// <summary>
        ///     Gives Write access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConWriteLock Lock => new ConWriteLock(_lock);

        #endregion

        /// <summary>
        /// </summary>
        public void EnterReadLock([CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
#if EnableLockLedger || EnableLockLedgerLight
            if (Logged && !LocalHost.IsProduction) LockLedger.RequestsReadLock(_id, memberName, sourceFilePath, sourceLineNumber);
#endif
            _lock.EnterReadLock();
#if EnableLockLedger || EnableLockLedgerLight
            if (Logged && !LocalHost.IsProduction) LockLedger.TakesReadLock(_id, memberName, sourceFilePath, sourceLineNumber);
#endif
        }


        /// <summary>
        /// </summary>
        public void ExitReadLock([CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            _lock.ExitReadLock();
#if EnableLockLedger || EnableLockLedgerLight
        if (Logged && !LocalHost.IsProduction) LockLedger.ReleasesReadLock(_id, memberName, sourceFilePath, sourceLineNumber);
#endif
        }


        /// <summary>
        /// </summary>
        public void EnterWriteLock([CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
#if EnableLockLedger || EnableLockLedgerLight
            if (Logged && !LocalHost.IsProduction) LockLedger.RequestsWriteLock(_id, memberName, sourceFilePath, sourceLineNumber);
#endif
            _lock.EnterWriteLock();
        }

        /// <summary>
        /// </summary>
        public void ExitWriteLock([CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            _lock.ExitWriteLock();
        }

        /// <summary>
        /// </summary>
        public void EnterExclusiveLock([CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            EnterWriteLock();
        }


        /// <summary>
        /// </summary>
        public void ExitExclusiveLock([CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            ExitWriteLock();
        }

        /// <summary>
        /// </summary>
        public void Clear()
        {
            try
            {
                ReadLock?.Dispose();
                WriteLock?.Dispose();
            }
            catch
            {
                // Ignore
            }

            _lock.Dispose();
        }

        #region Fields

        private readonly Guid _id = Guid.NewGuid();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        #endregion
    }

    #endregion

    #region ConDictionary

    /// <summary>
    ///     The .net ConcurrentDictionary support is close
    ///     but requires changes in usage from Dictionary to
    ///     be a replacement. This extends standard Dictionary behavior
    ///     such that if any change is required it is very minor. It also
    ///     allows users to request a lock and release it with a using statement.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="V"></typeparam>
    public class ConDictionary<T, V> : IDictionary<T, V>, IDisposable
    {
        #region Constructor
        public ConDictionary() 
        {
            _dictionary = new Dictionary<T, V>();
        }

        public ConDictionary(IEqualityComparer<T> comparer)
        {
            _dictionary = new Dictionary<T, V>(comparer);
        }
        #endregion

        #region ReadLock

        /// <summary>
        ///     Gives Read access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConReadLock ReadLock => _lock.ReadLock;

        #endregion

        #region WriteLock

        /// <summary>
        ///     Gives Write access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConWriteLock WriteLock => _lock.WriteLock;

        #endregion

        #region Lock

        /// <summary>
        ///     Gives Write access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConWriteLock Lock => _lock.Lock;

        #endregion

        /// <summary>
        ///     Enable/Disable Lock logging
        /// </summary>
        public bool Logged
        {
            get { return _lock.Logged; }
            set { _lock.Logged = value; }
        }

        #region Add

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        public void Add(KeyValuePair<T, V> item)
        {
            try
            {
                _lock.EnterWriteLock();
                if (_dictionary.ContainsKey(item.Key))
                    ((IDictionary<T, V>)_dictionary)[item.Key] = item.Value;
                else
                    ((IDictionary<T, V>)_dictionary).Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Clear

        /// <summary>
        /// </summary>
        public void Clear()
        {
            try
            {
                _lock.EnterWriteLock();
                _dictionary.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Contains

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(KeyValuePair<T, V> item)
        {
            try
            {
                _lock.EnterReadLock();
                return ((IDictionary<T, V>)_dictionary).Contains(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region CopyTo

        /// <summary>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(KeyValuePair<T, V>[] array, int arrayIndex)
        {
            try
            {
                _lock.EnterReadLock();
                ((IDictionary<T, V>)_dictionary).CopyTo(array, arrayIndex);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region Count

        /// <summary>
        /// </summary>
        public int Count
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return ((IDictionary<T, V>)_dictionary).Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        #endregion

        #region IsReadOnly

        /// <summary>
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return ((IDictionary<T, V>)_dictionary).IsReadOnly;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        #endregion

        #region ContainsKey

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(T key)
        {
            try
            {
                _lock.EnterReadLock();
                return ((IDictionary<T, V>)_dictionary).ContainsKey(key);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region Add

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(T key, V value)
        {
            try
            {
                _lock.EnterWriteLock();
                if (_dictionary.ContainsKey(key))
                    ((IDictionary<T, V>)_dictionary)[key] = value;
                else
                    ((IDictionary<T, V>)_dictionary).Add(new KeyValuePair<T, V>(key, value));
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region TryGetValue

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool TryGetValue(T key, out V value)
        {
            try
            {
                _lock.EnterReadLock();
                if (((IDictionary<T, V>)_dictionary).ContainsKey(key))
                {
                    value = ((IDictionary<T, V>)_dictionary)[key];
                    return true;
                }
                else
                {
                    if(default(V) != null)
                        value = default(V);
                    else
                        throw new InvalidOperationException();
                    return false;
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region Indexer

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public V this[T key]
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
#pragma warning disable CS8603 // Possible null reference return.
                    return ((IDictionary<T, V>)_dictionary).ContainsKey(key)
                        ? ((IDictionary<T, V>)_dictionary)[key]
                        : default(V);
#pragma warning restore CS8603 // Possible null reference return.
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set { Add(key, value); }
        }

        #endregion

        #region Keys

        /// <summary>
        /// </summary>
        public ICollection<T> Keys
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _dictionary.Keys.ToArray();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        #endregion

        #region Values

        /// <summary>
        /// </summary>
        public ICollection<V> Values
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _dictionary.Values.ToArray();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        #endregion

        #region Remove

        /// <summary>
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Remove(T key)
        {
            try
            {
                _lock.EnterWriteLock();
                return ((IDictionary<T, V>)_dictionary).Remove(key);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(KeyValuePair<T, V> item)
        {
            try
            {
                _lock.EnterWriteLock();
                return ((IDictionary<T, V>)_dictionary).Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Fields

#pragma warning disable CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        private readonly Dictionary<T, V> _dictionary = new Dictionary<T, V>();
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
        private readonly ConLockWrapper _lock = new ConLockWrapper();
        private bool _isDisposed;

        #endregion

        #region GetEnumerator

        /// <summary>
        ///     GetEnumerator
        /// </summary>
        /// <returns></returns>
        public IEnumerator<KeyValuePair<T, V>> GetEnumerator()
        {
            // This will prevent the changed while enumerating error
            return ((IEnumerable<KeyValuePair<T, V>>)_dictionary.ToArray()).GetEnumerator();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            Clear();

            //_lock.Clear();

            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #endregion

    #region ConWriteLock

    /// <summary>
    /// </summary>
    public interface IConLock : IDisposable
    {
    }

    /// <summary>
    ///     Used to communicate lock status
    /// </summary>
    public class ConWriteLock : IConLock
    {
        #region Fields

        private readonly ReaderWriterLockSlim _lock;

        #endregion

        #region Constructor

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="l"></param>
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public ConWriteLock(ReaderWriterLockSlim l)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            try
            {
                _lock = l;
                _lock.EnterWriteLock();
            }
            catch
            {
                // Ignore
            }
        }

        #endregion

        #region Dispose

        private bool _disposed;

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing) _lock.ExitWriteLock();

            _disposed = true;
        }

        #endregion
    }

    #endregion

    #region ConReadLock

    /// <summary>
    ///     Used to Read Lock status
    /// </summary>
    public class ConReadLock : IConLock
    {
        #region Fields

        private readonly ReaderWriterLockSlim _lock;

        #endregion

        #region Constructor

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="l"></param>
        public ConReadLock(ReaderWriterLockSlim l)
        {
            _lock = l;
            _lock.EnterReadLock();
        }

        #endregion

        #region Dispose

        private bool _disposed;

        /// <summary>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing) _lock.ExitReadLock();

            _disposed = true;
        }

        #endregion
    }

    #endregion

    #region ConList

    /// <summary>
    ///     Allows list to used in threads
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConList<T> : IList<T>, IEnumerable<T>
    {
        #region ReadLock

        /// <summary>
        ///     Gives Read access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConReadLock ReadLock => _lock.ReadLock;

        #endregion

        #region WriteLock

        /// <summary>
        ///     Gives Write access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConWriteLock WriteLock => _lock.WriteLock;

        #endregion

        #region Lock

        /// <summary>
        ///     Gives Write access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConWriteLock Lock => _lock.Lock;

        #endregion

        #region Logged

        /// <summary>
        ///     Enable/Disable Lock logging
        /// </summary>
        public bool Logged
        {
            get { return _lock.Logged; }
            set { _lock.Logged = value; }
        }

        #endregion

        #region Add

        /// <summary>
        ///     Add element to the list
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Add(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Clear

        /// <summary>
        ///     Clear items from list
        /// </summary>
        public void Clear()
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Clear();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Contains

        /// <summary>
        ///     Contains
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            try
            {
                _lock.EnterReadLock();
                return _list.Contains(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region CopyTo

        /// <summary>
        ///     CopyTo
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            try
            {
                _lock.EnterReadLock();
                _list.CopyTo(array, arrayIndex);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region Count

        /// <summary>
        ///     Count
        /// </summary>
        public int Count
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _list.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        #endregion

        #region IsReadOnly

        /// <summary>
        ///     IsReadOnly
        /// </summary>
        public bool IsReadOnly
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _list.IsReadOnly;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        #endregion

        #region IndexOf

        /// <summary>
        ///     IndexOf
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            try
            {
                _lock.EnterReadLock();
                return _list.IndexOf(item);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        #endregion

        #region Insert

        /// <summary>
        ///     Insert
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item)
        {
            try
            {
                _lock.EnterWriteLock();
                _list.Insert(index, item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Indexer

        /// <summary>
        ///     Indexer
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get
            {
                try
                {
                    _lock.EnterReadLock();
                    return _list[index];
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
            set
            {
                try
                {
                    _lock.EnterWriteLock();
                    _list[index] = value;
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        #endregion

        #region AddRange

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        public void AddRange(IEnumerable<T> value)
        {
            using (WriteLock)
            {
                foreach (var item in value) _list.Add(item);
            }
        }

        #endregion

        #region ToArray

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            using (ReadLock)
            {
                var retval = new T[_list.Count];
                _list.CopyTo(retval, 0);
                return retval;
            }
        }

        #endregion

        #region ToList

        /// <summary>
        /// </summary>
        /// <returns></returns>
        internal List<T> ToList()
        {
            using (ReadLock)
            {
                var retval = new List<T>();
                retval.AddRange(_list);
                ;
                return retval;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// </summary>
        public ConList()
        {
        }

        /// <summary>
        /// </summary>
        /// <param name="list"></param>
        public ConList(IEnumerable<T> list)
        {
            using (WriteLock)
            {
                foreach (var item in list)
                    Add(item);
            }
        }

        #endregion

        #region GetEnumerator

        /// <summary>
        ///     GetEnumerator - not thread safe
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in ToArray())
                yield return item;
        }

        /// <summary>
        ///     GetEnumerator
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Remove

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            try
            {
                _lock.EnterWriteLock();
                return _list.Remove(item);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            try
            {
                _lock.EnterWriteLock();
                _list.RemoveAt(index);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Sort

        /// <summary>
        ///     Sort
        /// </summary>
        /// <param name="compare"></param>
        public void Sort(Comparison<T> compare)
        {
            try
            {
                _lock.EnterWriteLock();
                ((List<T>)_list).Sort(compare);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="compare"></param>
        public void Sort(IComparer<T> compare)
        {
            try
            {
                _lock.EnterWriteLock();
                ((List<T>)_list).Sort(compare);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        /// </summary>
        public void Sort()
        {
            try
            {
                _lock.EnterWriteLock();
                ((List<T>)_list).Sort();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        #endregion

        #region Fields

        private readonly ConLockWrapper _lock = new ConLockWrapper();
        private readonly IList<T> _list = new List<T>();

        #endregion
    }

    #endregion

    /// <summary>
    ///     First in First out Class
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Fifo<T> : IEnumerable<T>
    {
        /// <summary>
        /// </summary>
        public IDisposable ReadLock => _lock.ReadLock;

        /// <summary>
        /// </summary>
        public IDisposable WriteLock => _lock.WriteLock;

        /// <summary>
        /// </summary>
        public bool IsAvailable
        {
            get
            {
                using (ReadLock)
                {
                    return _queue.Count > 0;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            using (ReadLock)
            {
                return _queue.GetEnumerator();
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// </summary>
        /// <param name="v"></param>
        public void Add(T v)
        {
            using (WriteLock)
            {
                _queue.Enqueue(v);
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public T Next()
        {
            using (WriteLock)
            {
                return _queue.Dequeue();
            }
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
            using (ReadLock)
            {
                return _queue.Peek();
            }
        }

        /// <summary>
        /// </summary>
        public void Clear()
        {
            using (WriteLock)
            {
                _queue.Clear();
            }

            _lock.Clear();
        }

        #region Fields

        private readonly Queue<T> _queue = new Queue<T>();
        private readonly ConLockWrapper _lock = new ConLockWrapper();

        #endregion
    }

    /// <summary>
    ///     A list with Queue-like entry points.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConListQueue<T> : IList<T>
    {
        #region Fields

        private readonly ConList<T> _list = new ConList<T>();

        #endregion

        #region ReadLock

        /// <summary>
        ///     Gives Read access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConReadLock ReadLock => _list.ReadLock;

        #endregion

        #region WriteLock

        /// <summary>
        ///     Gives Write access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConWriteLock WriteLock => _list.WriteLock;

        #endregion

        #region Lock

        /// <summary>
        ///     Gives Write access to the dictionary. This is intended to be
        ///     used in a using statement. Otherwise the object must be disposed
        ///     to release the lock.
        /// </summary>
        /// <returns></returns>
        public ConWriteLock Lock => _list.Lock;

        #endregion

        #region Enqueue

        /// <summary>
        ///     Enqueue item
        /// </summary>
        /// <param name="item"></param>
        public void Enqueue(T item)
        {
            _list.Add(item);
        }

        #endregion

        #region Dequeue

        /// <summary>
        ///     Dequeue top of list
        /// </summary>
        /// <returns></returns>
        public T Dequeue()
        {
            if (_list.Count == 0)
                throw new InvalidDataException("Attempt to Dequeue from an empty ConListQueue");
            var t = _list[0];
            _list.RemoveAt(0);
            return t;
        }

        #endregion

        #region Peek

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
#pragma warning disable CS8603 // Possible null reference return.
            if (_list.Count == 0) return default(T);
#pragma warning restore CS8603 // Possible null reference return.
            return _list[0];
        }

        #endregion

        #region List Entry Points

        /// <summary>
        /// </summary>
        public int Count => _list.Count;

        /// <summary>
        /// </summary>
        public bool IsReadOnly => _list.IsReadOnly;

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public T this[int index]
        {
            get { return _list[index]; }
            set { _list[index] = value; }
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
        }

        /// <summary>
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        public void Add(T item)
        {
            _list.Add(item);
        }

        /// <summary>
        /// </summary>
        public void Clear()
        {
            _list.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        /// <summary>
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        #endregion
    }
}