//#define PRODUCTION
#define FILELOG
#define LOGTODESKTOP
#define VERBOSE

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TekHighspeedAPI
{
    /// <summary>   Extension to IStatistics. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>
    public interface IStatisticsEx
    {
        /// <summary>   Used to accumulate Mean. </summary>
        ///
        /// <value> The sum. </value>

        double Sum { get; }

        /// <summary>   Used to accumulate stddev. </summary>
        ///
        /// <value> The total number of squared. </value>

        double SumSquared { get; }

        /// <summary>   Used to keep track of minimum location. </summary>
        ///
        /// <value> The minimum location. </value>

        double MinimumLocation { get; }

        /// <summary>   Used to keep track of maximum location. </summary>
        ///
        /// <value> The maximum location. </value>

        double MaximumLocation { get; }
    }

    /// <summary>
    /// Contains the standard statisics that can be returned for a population of data. This is always
    /// available on a IResultCollection (or derived) type.
    /// </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public interface IStatistics : IStatisticsEx
    {
        /// <summary>   Returns the average of the Values. </summary>
        ///
        /// <value> The mean value. </value>

        double Mean { get; }

        /// <summary>   Return the minimum value. </summary>
        ///
        /// <value> The minimum value. </value>

        double Minimum { get; }

        /// <summary>   Returns the minimum value. </summary>
        ///
        /// <value> The maximum value. </value>

        double Maximum { get; }

        /// <summary>   Returns the Standard Deviation. </summary>
        ///
        /// <value> The standard deviation. </value>

        double StandardDeviation { get; }

        /// <summary>   Peak2Peak measurement. </summary>
        ///
        /// <value> The peak to peak. </value>

        double PeakToPeak { get; }

        /// <summary>   Count of population. </summary>
        ///
        /// <value> The count. </value>

        long Count { get; }

        /// <summary>   Units. </summary>
        ///
        /// <value> The units. </value>

        string Units { get; set; }
    }

    /// <summary>   This class is used to accumulate statistics over acquistions. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class StatisticAccumulation : IStatistics
    {
        #region Constructor

        /// <summary>   Constructor. </summary>

        public StatisticAccumulation()
        {
            Reset();
        }

        #endregion

        #region Name

        /// <summary>   Gets or sets the name. </summary>
        ///
        /// <value> The name. </value>

        public string Name { get; set; }

        #endregion

        #region Mean

        /// <summary>   Mean of population. </summary>
        ///
        /// <value> The mean value. </value>

        public double Mean => Count <= 0 ? double.NaN : Sum / Count;

        #endregion

        #region Minimum

        /// <summary>   Minimum Value. </summary>
        ///
        /// <value> The minimum value. </value>

        public double Minimum { get; private set; } = double.NaN;

        #endregion

        #region Maximum

        /// <summary>   Maximum Value. </summary>
        ///
        /// <value> The maximum value. </value>

        public double Maximum { get; private set; } = double.NaN;

        #endregion

        #region Sum

        /// <summary>   Sum - Sum divided by count is mean. </summary>
        ///
        /// <value> The sum. </value>

        public double Sum { get; private set; }

        #endregion

        #region Sum2

        /// <summary>   Sum Squared. </summary>
        ///
        /// <value> The total number of squared. </value>

        public double SumSquared { get; private set; }

        #endregion

        #region MinimumLocation

        /// <summary>   N/A for Accumulation Mode. </summary>
        ///
        /// <value> The minimum location. </value>

        public double MinimumLocation { get; }

        #endregion

        #region MaximumLocation

        /// <summary>   N/A in Accumulation Mode. </summary>
        ///
        /// <value> The maximum location. </value>

        public double MaximumLocation { get; }

        #endregion

        #region StandardDeviation

        /// <summary>   Standard Deviation. </summary>
        ///
        /// <value> The standard deviation. </value>

        public double StandardDeviation =>
            Count > 1 ? Math.Sqrt((Count * SumSquared - Math.Pow(Sum, 2.0)) / (Count * (Count - 1))) : 0.0;

        #endregion

        #region PeakToPeak

        /// <summary>   Peak to Peak. </summary>
        ///
        /// <value> The peak to peak. </value>

        public double PeakToPeak => Maximum - Minimum;

        #endregion

        #region Count

        /// <summary>   Total Population. </summary>
        ///
        /// <value> The count. </value>

        public long Count { get; private set; }

        #endregion

        #region Units

        /// <summary>   Gets or sets the units. </summary>
        ///
        /// <value> The units. </value>

        public string Units { get; set; } = "";

        #endregion

        #region ClearAccumulation

        /// <summary>   Clears the accumulation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public void ClearAccumulation()
        {
            using (_dictionary.WriteLock)
            {
                foreach (var key in _dictionary.Keys) _dictionary[key].ClearAccumulation();
                Minimum = Maximum = double.NaN;
                Sum = SumSquared = 0.0;
                Count = 0;
                OverRangeCount = 0;
                UnderRangeCount = 0;
                MaxUpperRange = 0;
                MinLowerRange = 0;
                NaNCount = 0;
            }
        }

        #endregion

        #region Reset

        /// <summary>   Reset Accumulated Values. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public void Reset()
        {
            using (_dictionary.WriteLock)
            {
                Minimum = double.NaN;
                Maximum = double.NaN;
                Count = 0;
                Sum = SumSquared = 0;
                _dictionary.Clear();
            }
        }

        #endregion

        #region ToString

        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents this object. </returns>

        public override string ToString()
        {
            return string.Format(new EngineeringNotationFormatter(), "Mean={0:eng}{3}, Min={1:eng}{3}, Max={2:eng}{3}, ",
                       Mean, Minimum, Maximum, Units) +
                   string.Format(new EngineeringNotationFormatter(), "Stddev={0:eng}{2}, P2P={1:eng}{2}, Count=",
                       StandardDeviation, PeakToPeak, Units) + Count;
        }

        #endregion

        #region Fields

        #endregion

        #region Accumulate

        /// <summary>   Accumulate. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="mean">     . </param>
        /// <param name="units">    (Optional) </param>

        public void Accumulate(double mean, string units = "")
        {
            if (double.IsNaN(mean) || double.IsInfinity(mean)) return;
            using (_dictionary.WriteLock)
            {
                if (double.IsNaN(Minimum) || mean < Minimum) Minimum = mean;
                if (double.IsNaN(Maximum) || mean > Maximum) Maximum = mean;
                Sum += mean;
                SumSquared += Math.Pow(mean, 2.0);
                Count++;
                if (!string.IsNullOrEmpty(units)) Units = units;
            }
        }

        /// <summary>   Accumulates the given statistics. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="stats">    . </param>

        public void Accumulate(IStatistics stats)
        {
            if (stats == null) return;
            if (double.IsNaN(stats.Sum) || double.IsNaN(stats.SumSquared)) return;
            using (_dictionary.WriteLock)
            {
                if (double.IsNaN(Minimum) || stats.Minimum < Minimum) Minimum = stats.Minimum;
                if (double.IsNaN(Maximum) || stats.Maximum > Maximum) Maximum = stats.Maximum;
                Sum += stats.Sum;
                SumSquared += stats.SumSquared;
                Count += stats.Count;

                if (!string.IsNullOrEmpty(stats.Units)) Units = stats.Units;
            }
        }

        #endregion

        #region ITuple Support

        /// <summary>
        /// (Immutable)
        /// Dictionary holding statistics for ITuples.
        /// </summary>

        private readonly ConDictionary<string, StatisticAccumulation> _dictionary = new ConDictionary<string, StatisticAccumulation>();

        /// <summary>   Returns names for ITuples. </summary>
        ///
        /// <value> The names. </value>

        public IEnumerable<string> Names => _dictionary.Keys;

        /// <summary>   Finds names in ITuple. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name"> . </param>
        ///
        /// <returns>   An object. </returns>

        private object Find(string name)
        {
            using (_dictionary.ReadLock)
            {
                foreach (var key in _dictionary.Keys)
                    if (string.Compare(key, name, StringComparison.OrdinalIgnoreCase) == 0)
                        return _dictionary[key];
            }

            return null;
        }

        /// <summary>   Set value in ITuple. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name">     . </param>
        /// <param name="value">    . </param>

        private void Set(string name, object value)
        {
            if (!(value is StatisticAccumulation statistics)) return;

            using (_dictionary.WriteLock)
            {
                foreach (var key in _dictionary.Keys)
                    if (string.Compare(key, name, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        _dictionary[key] = statistics;
                        return;
                    }

                _dictionary.Add(name, statistics);
            }
        }

        /// <summary>   Get/Set ITuple values. </summary>
        ///
        /// <param name="name"> . </param>
        ///
        /// <returns>   The indexed item. </returns>

        public object this[string name]
        {
            get => Find(name);
            set => Set(name, value);
        }

        /// <summary>   Validate that tuple contains the specified key. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name"> . </param>
        ///
        /// <returns>   True if the object is in this collection, false if not. </returns>

        public bool Contains(string name)
        {
            using (_dictionary.ReadLock)
            {
                foreach (var key in _dictionary.Keys)
                    if (string.Compare(key, name, StringComparison.OrdinalIgnoreCase) == 0)
                        return true;
            }

            return false;
        }

        /// <summary>   Clear the ITuple. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public void Clear()
        {
            _dictionary.Clear();
            OverRangeCount = 0;
            UnderRangeCount = 0;
            NaNCount = 0;
        }

        #endregion

        #region IOutOfRangeInfo

        /// <inheritdoc/>
        public ulong OverRangeCount { get; set; }

        /// <inheritdoc/>
        public ulong UnderRangeCount { get; set; }

        /// <inheritdoc/>
        public double MaxUpperRange { get; set; } = double.NaN;

        /// <inheritdoc/>
        public double MinLowerRange { get; set; } = double.NaN;

        /// <inheritdoc/>
        public ulong NaNCount { get; set; }

        /// <inheritdoc/>
        public bool OutOfRange => OverRangeCount + UnderRangeCount > 0;

        /// <inheritdoc/>
        public bool ContainsNaN => NaNCount > 0;

        #endregion
    }

    #region IEvent

    /// <summary>   IEvent item. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public interface IEvent : IDisposable
    {
        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        DateTime TimeStamp { get; set; }

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        string Message { get; set; }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        double Time { get; set; }

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        string HTMLColor { get; set; }

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <returns>   This object as a string. </returns>

        string ToHTML();
    }

    #endregion

    /// <summary>   A duration item. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class DurationItem
    {
        /// <summary>   Name of item. </summary>
        ///
        /// <value> The name. </value>

        public string Name { get; set; }

        /// <summary>   Method containing item. </summary>
        ///
        /// <value> The method. </value>

        public string Method { get; set; }

        /// <summary>   Full File name of the containing item. </summary>
        ///
        /// <value> The full pathname of the file. </value>

        public string Path { get; set; }

        /// <summary>   Line number of the Line of code containing item. </summary>
        ///
        /// <value> The line. </value>

        public int Line { get; set; }

        /// <summary>   Statistic accumulation information. </summary>
        ///
        /// <value> The statistics. </value>

        public StatisticAccumulation Stats { get; set; } = new StatisticAccumulation() { Units = "s" };

        /// <summary>   Tests if this object is considered equal to another. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="obj">  . </param>
        ///
        /// <returns>   True if the objects are considered equal, false if they are not. </returns>

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>   Creates a unigue hash for a measurement start. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A hash code for this object. </returns>

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 29;
                hash = hash * 13 + (!ReferenceEquals(null, Name) ? Name.GetHashCode() : 0);
                hash = hash * 13 + (!ReferenceEquals(null, Method) ? Method.GetHashCode() : 0);
                hash = hash * 13 + (!ReferenceEquals(null, Path) ? Path.GetHashCode() : 0);
                hash = hash * 13 + Line.GetHashCode();
                return hash;
            }
        }
    }

    #region BugLogger

    /// <summary>   A bug. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class Bug : IEvent
    {
        /// <summary>   Gets or sets the exception. </summary>
        ///
        /// <value> The exception. </value>

        public Exception Exception { get; set; }

        /// <summary>   Gets or sets the file. </summary>
        ///
        /// <value> The file. </value>

        public string File { get; set; }

        /// <summary>   Gets or sets the line. </summary>
        ///
        /// <value> The line. </value>

        public int Line { get; set; }

        /// <summary>   Gets or sets the method. </summary>
        ///
        /// <value> The method. </value>

        public string Method { get; set; }

        /// <summary>   Gets or sets the stack. </summary>
        ///
        /// <value> The stack. </value>

        public StackFrame[] Stack { get; set; }

        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        public string Message { get; set; }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        public string HTMLColor { get; set; } = "red";

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   This object as a string. </returns>

        public string ToHTML()
        {
            return $"<p><font color=\"{HTMLColor}\">{ToString()}</font></p>";
        }

        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents this object. </returns>

        public override string ToString()
        {
            return
                $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] BUG - {Message}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases the unmanaged resources used by the Bug and optionally releases the managed
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>   Class for logging bugs (possibly to the cloud). </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>
    ///
    /// <example>
    ///    The LoggingPermission class is used to globally set logging permissions. Debug builds are
    ///    special in that that should not make it out of the Tek campus so Anonymous logging is not
    ///    allowed.
    ///    <code>
    /// // Enables/Disables Anonymous logging.
    /// // When off the users Mac Address and computer
    /// // name are logged. Other was the values are anonymised.
    /// LoggingPermission.Anonymous = false;
    /// 
    /// // Enable logging.
    /// LoggingPermission.Enabled = true;
    /// 
    /// // Application Name (required for cloud logging)
    /// LoggingPermission.ApplicationName = "TestAppExample";
    /// 
    /// // Application Version (required for cloud logging)
    /// LoggingPermission.ApplicationVersion = "1";
    /// 
    /// // Log a error message to the cloud
    /// BugLogger.WriteErrorMessage("This is an Error.");
    /// 
    /// try
    /// {
    ///     throw new Exception("test handler");
    /// }
    /// catch (Exception e)
    /// {
    ///     // Log a handled exception to the cloud.
    ///     BugLogger.WriteException(e);
    /// }
    /// </code>
    /// </example>

    public class BugLogger : IEnumerable<Bug>
    {
        #region Fields

        /// <summary>   (Immutable) the buffer. </summary>
        private static readonly CircularBuffer<Bug> _buffer = new CircularBuffer<Bug>(MaxEvents);

        /// <summary>   (Immutable) the exceptions. </summary>
        private static readonly ConDictionary<string, int> _exceptions = new ConDictionary<string, int>();

        /// <summary>   (Immutable) the messages. </summary>
        private static readonly ConDictionary<string, int> _messages = new ConDictionary<string, int>();

        /// <summary>   (Immutable) true to enable, false to disable the log file. </summary>
        private static readonly bool _logFileEnabled = true;
        /// <summary>   (Immutable) true to enable, false to disable the black box. </summary>
        private static readonly bool _blackBoxEnabled = true;

#if !DEBUG
#if NET48_OR_GREATER
    /// <summary>   (Immutable) true to enable, false to disable the weblogger. </summary>
    private static readonly bool _webloggerEnabled = true;
    private static bool _readreg = false;
#endif
#endif

        #endregion

        /// <summary>   Clears this object to its blank/initial state. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void Clear()
        {
            _messages.Clear();
            _buffer.Clear();
            _exceptions.Clear();
        }

        /// <summary>   Gets a stack. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="offset">   (Optional) The offset. </param>
        ///
        /// <returns>   The stack. </returns>

        public static string GetStack(int offset = 0)
        {
            var sb = new StringBuilder();

            var st = new StackTrace(true);
            var frames = st.GetFrames();

            // Frame 0 is ErrorMessage
            for (var i = 1 + offset; i < frames.Length; i++)
            {
                var line = frames[i].GetFileLineNumber();
                var file = frames[i].GetFileName();
                var method = frames[i].GetMethod().Name;
                sb.Append($"{method}-{Path.GetFileName(file)}@{line};");
            }

            return sb.ToString();
        }

        /// <summary>   Reads the registry. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        private static void ReadRegistry()
        {
#if !DEBUG
#if NET48_OR_GREATER
            if (_readreg) return;
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Tektronix\Sampling");
            _logFileEnabled = key?.GetValue("LogFile") != null;
            _blackBoxEnabled = key?.GetValue("BlackBox") != null;
            _webloggerEnabled = key?.GetValue("WebLogger") != null;
            _readreg = true;
#endif
#endif
        }

        /// <summary>   Gets a value indicating whether the log file is enabled. </summary>
        ///
        /// <value> True if log file enabled, false if not. </value>

        public static bool LogFileEnabled
        {
            get
            {
                ReadRegistry();
                return _logFileEnabled;
            }
        }

        /// <summary>   Gets a value indicating whether the black box is enabled. </summary>
        ///
        /// <value> True if black box enabled, false if not. </value>

        public static bool BlackBoxEnabled
        {
            get
            {
                ReadRegistry();
                return _blackBoxEnabled;
            }
        }

        /// <summary>   Gets a value indicating whether the web logger is enabled. </summary>
        ///
        /// <value> True if web logger enabled, false if not. </value>

        public static bool WebLoggerEnabled
        {
            get
            {
#if PRODUCTION
            return false;
#else
                return true;
#endif
            }
        }

        /// <summary>   Gets the read lock. </summary>
        ///
        /// <value> The read lock. </value>

        public static IDisposable ReadLock => _buffer.ReadLock;

        /// <summary>   Gets the write lock. </summary>
        ///
        /// <value> The write lock. </value>

        public static IDisposable WriteLock => _buffer.WriteLock;

        #region MaxEvents

        /// <summary>   (Immutable) the maximum events. </summary>
        public const int MaxEvents = 2000;

        #endregion

        #region Bugs

        /// <summary>   Gets the bugs. </summary>
        ///
        /// <value> The bugs. </value>

        public static IEnumerable<Bug> Bugs
        {
            get
            {
                foreach (var item in _buffer) yield return item;
            }
        }

        #endregion

        #region EnableStackTrace

        /// <summary>   Enable stack trace. </summary>
        ///
        /// <value> True if enable stack trace, false if not. </value>

        public static bool EnableStackTrace { get; set; }

        #endregion

        #region AddBug

        /// <summary>   Adds a bug. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="bug">  . </param>

        public static void AddBug(Bug bug)
        {
            using (_buffer.WriteLock)
            {
                _buffer.Add(bug);
            }

            FileLogger.Error(bug.ToString());
        }

        #endregion

        #region WriteErrorMessage

        /// <summary>   WriteErrorMessage - Log an error message. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">           format string. </param>
        /// <param name="memberName">       (Optional) Calling member. </param>
        /// <param name="sourceFilePath">   (Optional) File name of calling member. </param>
        /// <param name="sourceLineNumber"> (Optional) Source line of calling member. </param>
        ///
        /// <example>
        ///    The LoggingPermission class is used to globally set logging permissions. Debug builds are
        ///    special in that that should not make it out of the Tek campus so Anonymous logging is not
        ///    allowed.
        ///    <code>
        /// // Enables/Disables Anonymous logging.
        /// // When off the users Mac Address and computer
        /// // name are logged. Other was the values are anonymised.
        /// LoggingPermission.Anonymous = false;
        /// 
        /// // Enable logging.
        /// LoggingPermission.Enabled = true;
        /// 
        /// // Application Name (required for cloud logging)
        /// LoggingPermission.ApplicationName = "TestAppExample";
        /// 
        /// // Application Version (required for cloud logging)
        /// LoggingPermission.ApplicationVersion = "1";
        /// 
        /// // Log a error message to the cloud
        /// BugLogger.WriteErrorMessage("This is an Error.");
        /// 
        /// try
        /// {
        ///     throw new Exception("test handler");
        /// }
        /// catch (Exception e)
        /// {
        ///     // Log a handled exception to the cloud.
        ///     BugLogger.WriteException(e);
        /// }
        /// </code>
        /// </example>

        public static void WriteErrorMessage(string format, [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Debug.WriteLine($"[Bug] - {format}");
            var st = new StackTrace(true);

            var name = Path.GetFileName(sourceFilePath) + " " + sourceLineNumber;
            if (!_messages.ContainsKey(name))
                _messages.Add(name, 1);
            else
                _messages[name]++;

            AddBug(new Bug
            {
                Exception = null,
                File = sourceFilePath,
                Line = sourceLineNumber,
                Message = format,
                TimeStamp = DateTime.Now,
                Time = CurrentTime,
                Stack = st.GetFrames()
            });
        }

        #endregion

        #region WriteUnhandledException

        /// <summary>
        /// WriteUnhandledException - This is handled implicitly. You don't need to call this as a
        /// developer.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="e">    Exception info. </param>
        ///
        /// ### <param name="memberName">   Calling member. </param>
        ///
        /// ### <param name="sourceFilePath">   File name of calling member. </param>
        /// ### <param name="sourceLineNumber"> Source line of calling member. </param>

        public static void WriteUnhandledException(Exception e)
        {
            var st = new StackTrace(true);
            var frames = st.GetFrames();

            // Frame 0 is ErrorMessage
            for (var i = 1; i < frames.Length; i++)
            {
                var line = frames[i].GetFileLineNumber();
                var file = frames[i].GetFileName();
                var method = frames[i].GetMethod().Name;

                if (i == 2)
                {
                    AddBug(new Bug
                    {
                        Exception = e,
                        Method = method,
                        File = file,
                        Line = line,
                        Message = e.Message,
                        TimeStamp = DateTime.Now,
                        Time = CurrentTime,
                        Stack = frames
                    });
                    break;
                }
            }
        }

        #endregion

        #region WriteException

        /// <summary>   ThrowException - send exception info to logger (this call blocks) </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="e">                . </param>
        /// <param name="memberName">       (Optional) Calling member. </param>
        /// <param name="sourceFilePath">   (Optional) File name of calling member. </param>
        /// <param name="sourceLineNumber"> (Optional) Source line of calling member. </param>
        ///
        /// <returns>   An Exception. </returns>

        public static Exception ThrowException(Exception e,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (LocalHost.IsProduction) return e;
            var st = new StackTrace(true);
            AddBug(new Bug
            {
                Exception = e,
                Method = memberName,
                File = sourceFilePath,
                Line = sourceLineNumber,
                Message = e.Message,
                TimeStamp = DateTime.Now,
                Time = CurrentTime,
                Stack = st.GetFrames()
            });

            return e;
        }

        #endregion

        #region CurrentTime

        /// <summary>   (Immutable) <exclude /> </summary>
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Returns a current time indicator that is useful for doing time delta measurements.
        /// </summary>
        ///
        /// <value> The current time. </value>

        public static double CurrentTime => _stopwatch.ElapsedTicks / (double)Stopwatch.Frequency;

        #endregion

        #region WriteException

        /// <summary>   Values that represent Exception status. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public enum ExStatus
        {
            /// <summary>   An enum constant representing the ignored option. </summary>
            Ignored,
            /// <summary>   An enum constant representing the handled option. </summary>
            Handled,
            /// <summary>   An enum constant representing the rethrown option. </summary>
            Rethrown,
            /// <summary>   An enum constant representing the unknown option. </summary>
            Unknown
        }

        /// <summary>   CatchException - send exception info to logger (this call blocks) </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="e">                . </param>
        /// <param name="message">          (Optional) </param>
        /// <param name="status">           (Optional) The status. </param>
        /// <param name="memberName">       (Optional) Calling member. </param>
        /// <param name="sourceFilePath">   (Optional) File name of calling member. </param>
        /// <param name="sourceLineNumber"> (Optional) Source line of calling member. </param>

        public static void CatchException(Exception e, string message = "", ExStatus status = ExStatus.Unknown,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Debug.WriteLine($"[Bug] - {e.Message}");

            var st = new StackTrace(true);

            var name = Path.GetFileName(sourceFilePath) + " " + sourceLineNumber;
            using (_exceptions.WriteLock)
            {
                if (!string.IsNullOrEmpty(name))
                    if (!_exceptions.ContainsKey(name))
                        _exceptions.Add(name, 1);
                    else
                        _exceptions[name]++;
            }

            var bug = new Bug
            {
                Exception = e,
                Method = memberName,
                File = sourceFilePath,
                Line = sourceLineNumber,
                Message = e.Message,
                TimeStamp = DateTime.Now,
                Time = CurrentTime,
                Stack = st.GetFrames()
            };

            if (!string.IsNullOrEmpty(message)) bug.Message += " - " + message;

            AddBug(bug);
        }

        #endregion

        #region IsFalse

        /// <summary>   Is false. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="condition">        . </param>
        /// <param name="message">          (Optional) </param>
        /// <param name="memberName">       (Optional) Calling member. </param>
        /// <param name="sourceFilePath">   (Optional) File name of calling member. </param>
        /// <param name="sourceLineNumber"> (Optional) Source line of calling member. </param>
        ///
        /// ### <param name="action">   . </param>

        public static void IsFalse(bool condition, string message = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (condition)
                WriteErrorMessage(
                    $"Bug - IsFalse Failed - {message} : Method:{memberName}, File:{Path.GetFileName(sourceFilePath)}, Line:{sourceLineNumber}");
        }

        #endregion

        #region IsTrue

        /// <summary>   Is true. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="condition">        . </param>
        /// <param name="message">          (Optional) </param>
        /// <param name="memberName">       (Optional) Calling member. </param>
        /// <param name="sourceFilePath">   (Optional) File name of calling member. </param>
        /// <param name="sourceLineNumber"> (Optional) Source line of calling member. </param>
        ///
        /// ### <param name="action">   . </param>

        public static void IsTrue(bool condition, string message = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!condition)
                WriteErrorMessage(
                    $"Bug - IsTrue Failed - {message} : Method:{memberName}, File:{Path.GetFileName(sourceFilePath)}, Line:{sourceLineNumber}");
        }

        #endregion

        #region Assert

        /// <summary>   Asserts. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="condition">        . </param>
        /// <param name="message">          (Optional) </param>
        /// <param name="memberName">       (Optional) Calling member. </param>
        /// <param name="sourceFilePath">   (Optional) File name of calling member. </param>
        /// <param name="sourceLineNumber"> (Optional) Source line of calling member. </param>
        ///
        /// ### <param name="action">   . </param>

        public static void Assert(bool condition, string message = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!condition)
                WriteErrorMessage(
                    $"Bug - IsTrue Failed - {message} : Method:{memberName}, File:{Path.GetFileName(sourceFilePath)}, Line:{sourceLineNumber}");
        }

        #endregion

        #region Static Constructor

        /// <summary>
        /// Static Constructor - this assume if you get here - you wanted logging. Down stream
        /// applications need to disable this explicitly.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        static BugLogger()
        {
            EnableStackTrace = true;
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
        }

        /// <summary>   Gets the chart. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        public static string Chart()
        {
            if (_exceptions.Count <= 0 && _messages.Count <= 0) return "<p>No recorded exceptions.</p>";

            var sb = new StringBuilder();
            if (_exceptions.Count > 0)
            {
                sb.AppendLine("<h1>Exceptions</h1>");
                sb.AppendLine("<script type = \"text/javascript\" >");
                sb.AppendLine("google.charts.load(\'current\', { \'packages\':[\'bar\']});");
                sb.AppendLine("google.charts.setOnLoadCallback(drawBarChart);");
                sb.AppendLine("");
                sb.AppendLine("function drawBarChart()");
                sb.AppendLine("{");

                sb.AppendLine("var data = google.visualization.arrayToDataTable([");

                sb.AppendLine("[\'Location\', \'Count\'],");

                using (_exceptions.ReadLock)
                {
                    foreach (var key in from entry in _exceptions orderby entry.Value descending select entry.Key)
                        sb.AppendLine($"['{key}', {_exceptions[key]}],");
                }

                sb.AppendLine("]);");

                sb.AppendLine("var options = {");
                sb.AppendLine("legend: { position: 'none' },");
                sb.AppendLine("chart:");
                sb.AppendLine("{title: 'Exceptions'},");
                sb.AppendLine("bars: 'horizontal'};");
                sb.AppendLine("var chart = new google.charts.Bar(document.getElementById(\'ExceptionBarChart\'));");
                sb.AppendLine("");
                sb.AppendLine("chart.draw(data, options);");
                sb.AppendLine("}");
                sb.AppendLine("</script>");
                sb.AppendLine(
                    $"<div id=\"ExceptionBarChart\" style=\"width: 1200; height: {150 + 70 * _exceptions.Count}px; \"></div>");
            }

            if (_messages.Count > 0)
            {
                sb.AppendLine("<h1>Error Messages</h1>");
                sb.AppendLine("<script type = \"text/javascript\" >");
                sb.AppendLine("google.charts.load(\'current\', { \'packages\':[\'bar\']});");
                sb.AppendLine("google.charts.setOnLoadCallback(drawBarChart1);");
                sb.AppendLine("");
                sb.AppendLine("function drawBarChart1()");
                sb.AppendLine("{");

                sb.AppendLine("var data = google.visualization.arrayToDataTable([");

                sb.AppendLine("[\'Location\', \'Count\'],");

                using (_messages.ReadLock)
                {
                    foreach (var key in from entry in _messages orderby entry.Value descending select entry.Key)
                        sb.AppendLine($"['{key}', {_messages[key]}],");
                }

                sb.AppendLine("]);");

                sb.AppendLine("var options = {");
                sb.AppendLine("legend: { position: 'none' },");
                sb.AppendLine("chart:");
                sb.AppendLine("{title: 'Error Messages'},");
                sb.AppendLine("bars: 'horizontal'};");
                sb.AppendLine("var chart = new google.charts.Bar(document.getElementById(\'MessageBarChart\'));");
                sb.AppendLine("");
                sb.AppendLine("chart.draw(data, options);");
                sb.AppendLine("}");
                sb.AppendLine("</script>");
                sb.AppendLine(
                    $"<div id=\"MessageBarChart\" style=\"width: 1200; height: {150 + 70 * _messages.Count}; \"></div>");
            }

            return sb.ToString();
        }

        /// <summary>   Log Unhandled Exception. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="sender">   . </param>
        /// <param name="args">     . </param>

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = (Exception)args.ExceptionObject;
            WriteUnhandledException(e);
        }

        #endregion

        #region IEnumerable

        /// <summary>   Gets the enumerator. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   The enumerator. </returns>

        public IEnumerator<Bug> GetEnumerator()
        {
            foreach (var item in _buffer)
                yield return item;
        }

        /// <summary>   Gets the enumerator. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   The enumerator. </returns>

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    #endregion

    #region JobLogger

    /// <summary>   Values that represent job event types. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public enum JobEventType
    {
        /// <summary>   An enum constant representing the create job option. </summary>
        CreateJob,
        /// <summary>   An enum constant representing the complete job option. </summary>
        CompleteJob,
        /// <summary>   An enum constant representing the issue milestone option. </summary>
        IssueMilestone,
        /// <summary>   An enum constant representing the succeed milestone option. </summary>
        SucceedMilestone,
        /// <summary>   An enum constant representing the fail milestone option. </summary>
        FailMilestone,
        /// <summary>   An enum constant representing the create waypoint option. </summary>
        CreateWaypoint,
        /// <summary>   An enum constant representing the dispose waypoint option. </summary>
        DisposeWaypoint
    }

    /// <summary>   A job event. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class JobEvent : IEvent
    {
        /// <summary>   Gets or sets the file. </summary>
        ///
        /// <value> The file. </value>

        public string File { get; set; }

        /// <summary>   Gets or sets the line. </summary>
        ///
        /// <value> The line. </value>

        public int Line { get; set; }

        /// <summary>   Gets or sets the method. </summary>
        ///
        /// <value> The method. </value>

        public string Method { get; set; }

        /// <summary>   Gets or sets the identifier of the job. </summary>
        ///
        /// <value> The identifier of the job. </value>

        public long JobID { get; set; }

        /// <summary>   Gets or sets the identifier of the milestone. </summary>
        ///
        /// <value> The identifier of the milestone. </value>

        public Guid MilestoneID { get; set; }

        /// <summary>   Gets or sets the identifier of the waypoint. </summary>
        ///
        /// <value> The identifier of the waypoint. </value>

        public long WaypointID { get; set; }

        /// <summary>   Gets or sets the type. </summary>
        ///
        /// <value> The type. </value>

        public JobEventType Type { get; set; }

        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        public string Message { get; set; } = "";

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        public string HTMLColor { get; set; } = "black";

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   This object as a string. </returns>

        public string ToHTML()
        {
            return $"<p><font color=\"{HTMLColor}\">{ToString()}</font></p>";
            ;
        }

        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents this object. </returns>

        public override string ToString()
        {
            switch (Type)
            {
                case JobEventType.CreateJob:
                    return
                        $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] JOB - CreateJob: JobID: {JobID} - {Message}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
                case JobEventType.CompleteJob:
                    return
                        $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] JOB - CompleteJob: JobID: {JobID} - {Message}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
                case JobEventType.IssueMilestone:
                    return
                        $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] JOB - Issue Milestone: JobID: {JobID}, Milestone: {MilestoneID} - {Message}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
                case JobEventType.SucceedMilestone:
                    return
                        $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] JOB - Succeed Milestone: JobID: {JobID}, Milestone: {MilestoneID} - {Message}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
                case JobEventType.FailMilestone:
                    return
                        $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] JOB - Fail Milestone: JobID: {JobID}, Milestone: {MilestoneID} - {Message}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
                case JobEventType.CreateWaypoint:
                    return
                        $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] JOB - Create Waypoint: JobID: {JobID}, Source Milestone: {MilestoneID}, WaypointID: {WaypointID}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
                case JobEventType.DisposeWaypoint:
                    return
                        $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] JOB - Dispose Waypoint: JobID: {JobID}, WaypointID: {WaypointID}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
            }

            return
                $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] JOB - {Type}: JobID: {JobID} - {Message}, File: {Path.GetFileName(File)}, Line: {Line}, Method: {Method}";
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases the unmanaged resources used by the JobEvent and optionally releases the managed
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>   A job logger. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class JobLogger
    {
        /// <summary>   (Immutable) the buffer. </summary>
        private static readonly CircularBuffer<IEvent> _buffer = new CircularBuffer<IEvent>(BugLogger.MaxEvents);
        /// <summary>   (Immutable) the open jobs. </summary>
        private static readonly ConDictionary<long, IEvent> _openJobs = new ConDictionary<long, IEvent>();

        /// <summary>   Static constructor. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        static JobLogger()
        {
            if (!LocalHost.IsProduction)
                LocalHost.AddPage("Jobs", Page);
        }

        /// <summary>   Gets the read lock. </summary>
        ///
        /// <value> The read lock. </value>

        public static IDisposable ReadLock => _buffer.ReadLock;

        /// <summary>   Gets the write lock. </summary>
        ///
        /// <value> The write lock. </value>

        public static IDisposable WriteLock => _buffer.WriteLock;

        /// <summary>   Gets the events. </summary>
        ///
        /// <value> The events. </value>

        public static IEnumerable<IEvent> Events => _buffer;

        /// <summary>   Gets the page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string Page()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<p><a href=\"http://localhost:53141/Etf/\">Detailed Job Information</a></p>");

            sb.AppendLine("<h2>Open Jobs</h2>");
            using (_openJobs.ReadLock)
            {
                foreach (var item in _openJobs.Values.OrderByDescending(x => x.Time)) sb.AppendLine(item.ToHTML());
            }

            sb.AppendLine("<h2>Job Details</h2>");
            using (_buffer.ReadLock)
            {
                foreach (var item in _buffer.OrderByDescending(x => x.Time))
                    sb.AppendLine(item.ToHTML());
            }

            return sb.ToString();
        }

        /// <summary>   Creates a job. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="jobSerialNumber">  . </param>
        /// <param name="policy">           . </param>
        /// <param name="memberName">       (Optional) </param>
        /// <param name="sourceFilePath">   (Optional) </param>
        /// <param name="sourceLineNumber"> (Optional) </param>

        public static void CreateJob(long jobSerialNumber, string policy,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (LocalHost.IsProduction) return;
            var jobEvent = new JobEvent
            {
                HTMLColor = "DarkGreen",
                Type = JobEventType.CreateJob,
                Message = policy,
                File = Path.GetFileName(sourceFilePath),
                JobID = jobSerialNumber,
                Method = memberName,
                Line = sourceLineNumber
            };
            using (_openJobs.WriteLock)
            {
                _openJobs.Add(jobSerialNumber, jobEvent);
            }

            using (_buffer.WriteLock)
            {
                _buffer.Add(jobEvent);
            }
        }

        /// <summary>   Log that a milestone was issued. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="jobSerialNumber">  Is null if there was no current job (and so a dummy milestone
        ///                                 was issued). </param>
        /// <param name="milestoneid">      The milestoneid. </param>
        /// <param name="category">         MilestoneCategory enumerator converted to a string;
        ///                                 identifies the argument that was passed to
        ///                                 JobMgr.IssuePendingMilestone(). </param>
        /// <param name="isDummyMilestone"> True if the call to JobMgr.IssuePendingMilestone()
        ///                                 produced a dummy milestone, and false if the call produced a
        ///                                 real milestone. </param>
        /// <param name="memberName">       (Optional) </param>
        /// <param name="sourceFilePath">   (Optional) </param>
        /// <param name="sourceLineNumber"> (Optional) </param>

        public static void IssuePendingMilestone(
            long? jobSerialNumber,
            Guid milestoneid,
            string category,
            bool isDummyMilestone,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (LocalHost.IsProduction) return;
            using (_buffer.WriteLock)
            {
                _buffer.Add(new JobEvent
                {
                    HTMLColor = "DarkSeaGreen", Type = JobEventType.IssueMilestone, MilestoneID = milestoneid,
                    Message = category, File = Path.GetFileName(sourceFilePath), JobID = jobSerialNumber ?? 0,
                    Method = memberName, Line = sourceLineNumber
                });
            }
        }

        /// <summary>   Log that a Waypoint was created. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="waypointSerialNumber"> Serial number of the Waypoint. (If the Waypoint was
        ///                                     created by a dummy milestone, the serial number will be
        ///                                     0.) </param>
        /// <param name="jobSerialNumber">      Serial number of the associated job, or null if not
        ///                                     available. </param>
        /// <param name="parentMilestoneid">    ID of the parent Milestone from which the Waypoint was
        ///                                     created. </param>
        /// <param name="memberName">           (Optional) </param>
        /// <param name="sourceFilePath">       (Optional) </param>
        /// <param name="sourceLineNumber">     (Optional) </param>

        public static void CreateWaypoint(
            long waypointSerialNumber,
            long? jobSerialNumber,
            Guid parentMilestoneid,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (LocalHost.IsProduction) return;
            using (_buffer.WriteLock)
            {
                _buffer.Add(new JobEvent
                {
                    HTMLColor = "DarkSeaGreen", Type = JobEventType.CreateWaypoint, MilestoneID = parentMilestoneid,
                    WaypointID = waypointSerialNumber, File = Path.GetFileName(sourceFilePath),
                    JobID = jobSerialNumber ?? 0, Method = memberName, Line = sourceLineNumber
                });
            }
        }

        /// <summary>   Log that a Waypoint was disposed. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="waypointSerialNumber"> Serial number of the Waypoint. (If the Waypoint was
        ///                                     created by a dummy milestone, the serial number will be
        ///                                     0.) </param>
        /// <param name="jobSerialNumber">      Serial number of the associated job, or null if not
        ///                                     available. </param>
        /// <param name="memberName">           (Optional) </param>
        /// <param name="sourceFilePath">       (Optional) </param>
        /// <param name="sourceLineNumber">     (Optional) </param>

        public static void DisposeWaypoint(
            long waypointSerialNumber,
            long? jobSerialNumber,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (LocalHost.IsProduction) return;
            using (_buffer.WriteLock)
            {
                _buffer.Add(new JobEvent
                {
                    HTMLColor = "DarkSeaGreen", Type = JobEventType.DisposeWaypoint, WaypointID = waypointSerialNumber,
                    File = Path.GetFileName(sourceFilePath), JobID = jobSerialNumber ?? 0, Method = memberName,
                    Line = sourceLineNumber
                });
            }
        }

        /// <summary>   Log that a milestone was succeeded. </summary>
        ///
        /// <remarks>
        /// Dummy milestones occur when there is no current job at the time of milestone issue, when a
        /// job that has already failed issues a milestone, and when JobMgr.GetMilestoneByGuid()
        /// retrieves a milestone for a job that has already completed.
        /// </remarks>
        ///
        /// <param name="milestoneid">      The milestoneid. </param>
        /// <param name="category">         MilestoneCategory enumerator converted to a string. Is
        ///                                 "(dummy milestone)" for a dummy milestone. </param>
        /// <param name="jobSerialNumber">  Serial number of job associated with the milestone. For a
        ///                                 dummy milestone, the job serial number is null. </param>
        /// <param name="memberName">       (Optional) </param>
        /// <param name="sourceFilePath">   (Optional) </param>
        /// <param name="sourceLineNumber"> (Optional) </param>

        public static void SucceedMilestone(
            Guid milestoneid,
            string category,
            long? jobSerialNumber,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (LocalHost.IsProduction) return;
            using (_buffer.WriteLock)
            {
                _buffer.Add(new JobEvent
                {
                    HTMLColor = "DarkSeaGreen", Type = JobEventType.SucceedMilestone, MilestoneID = milestoneid,
                    Message = category, File = Path.GetFileName(sourceFilePath), JobID = jobSerialNumber ?? 0,
                    Method = memberName, Line = sourceLineNumber
                });
            }
        }

        /// <summary>   Log that a milestone was failed. </summary>
        ///
        /// <remarks>
        /// Dummy milestones occur when there is no current job at the time of milestone issue, when a
        /// job that has already failed issues a milestone, and when JobMgr.GetMilestoneByGuid()
        /// retrieves a milestone for a job that has already completed.
        /// </remarks>
        ///
        /// <param name="milestone">        The milestone. </param>
        /// <param name="category">         MilestoneCategory enumerator converted to a string. Is
        ///                                 "(dummy milestone)" for a dummy milestone. </param>
        /// <param name="jobSerialNumber">  Serial number of job associated with the milestone. For a
        ///                                 dummy milestone, the job serial number is null. </param>
        /// <param name="memberName">       (Optional) </param>
        /// <param name="sourceFilePath">   (Optional) </param>
        /// <param name="sourceLineNumber"> (Optional) </param>

        public static void FailMilestone(
            Guid milestone,
            string category,
            long? jobSerialNumber,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (LocalHost.IsProduction) return;
            using (_buffer.WriteLock)
            {
                _buffer.Add(new JobEvent
                {
                    HTMLColor = "red", Type = JobEventType.FailMilestone, MilestoneID = milestone, Message = category,
                    File = Path.GetFileName(sourceFilePath), JobID = jobSerialNumber ?? 0, Method = memberName,
                    Line = sourceLineNumber
                });
            }
        }

        /// <summary>   Log that a job has completed. </summary>
        ///
        /// <remarks>
        /// If "why" is "last Action completed", then memberName, sourceFilePath, and sourceLineNumber
        /// refer to the location where CreateJob() or ExtendJob() was used to add the Action to the job.
        /// If "why" is "acquisition state changed" then memberName and sourceFilePath are null, and
        /// sourceLineNumber is 0. Otherwise the location refers to the site of the call to
        /// Milestone.Succeed(), Milestone.Fail(), JobMgr.FailCurrentJob(), Waypoint.Dispose(), or the
        /// JobCompletionPolicy setter that caused the job to complete.
        /// </remarks>
        ///
        /// <param name="jobSerialNumber">      . </param>
        /// <param name="why">                  Is one of: "milestone succeeded", "milestone failed",
        ///                                     "FailCurrentJob was called", "last Action was completed",
        ///                                     "JobCompletionPolicy was changed", "acquisition state
        ///                                     changed", "Waypoint disposed". </param>
        /// <param name="milestoneId">          The ID of the milestone that succeeded or failed, if why
        ///                                     is one of "milestone succeeded" and "milestone failed",
        ///                                     and null otherwise. </param>
        /// <param name="newCompletionPolicy">  If why is "jobCompletionPolicy was changed", then is the
        ///                                     value of the new JobCompletionPolicy converted to a
        ///                                     string, and otherwise is null. </param>
        /// <param name="jobSucceeding">        True if the job is succeeding and false if the job is
        ///                                     failing. </param>
        /// <param name="memberName">           (Optional) </param>
        /// <param name="sourceFilePath">       (Optional) </param>
        /// <param name="sourceLineNumber">     (Optional) </param>

        public static void CompleteJob(
            long jobSerialNumber,
            string why,
            Guid? milestoneId,
            string newCompletionPolicy,
            bool jobSucceeding,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (LocalHost.IsProduction) return;
            using (_buffer.WriteLock)
            {
                _buffer.Add(new JobEvent
                {
                    HTMLColor = "DarkGreen", Type = JobEventType.CompleteJob, Message = why,
                    File = Path.GetFileName(sourceFilePath), JobID = jobSerialNumber, Method = memberName,
                    Line = sourceLineNumber
                });
            }

            using (_openJobs.WriteLock)
            {
                if (_openJobs.ContainsKey(jobSerialNumber))
                    _openJobs.Remove(jobSerialNumber);
            }
        }
    }

    #endregion

    #region RavenEvents

    /// <summary>   A raven event. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class RavenEvent : IEvent
    {
        /// <summary>   Gets or sets the name of the meas. </summary>
        ///
        /// <value> The name of the meas. </value>

        public string MeasName { get; set; }

        /// <summary>   Gets or sets the run time. </summary>
        ///
        /// <value> The run time. </value>

        public double RunTime { get; set; }

        /// <summary>   Gets or sets the run location. </summary>
        ///
        /// <value> The run location. </value>

        public string RunLoc { get; set; }

        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        public string Message { get; set; }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        public string HTMLColor { get; set; } = "black";

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   This object as a string. </returns>

        public string ToHTML()
        {
            return $"<p><font color=\"{HTMLColor}\">{ToString()}</font></p>";
        }

        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents the current object. </returns>

        public override string ToString()
        {
            return $"Measurment - {MeasName} | Run Loc - {RunLoc} | Run Time - {RunTime}";
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases the unmanaged resources used by the RavenEvent and optionally releases the managed
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    /// <summary>   The analysis service event. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class AnalysisServiceEvent : IEvent
    {
        /// <summary>   Gets or sets the port. </summary>
        ///
        /// <value> The port. </value>

        public int Port { get; set; }

        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        public string Message { get; set; }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        public string HTMLColor { get; set; } = "black";

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   This object as a string. </returns>

        public string ToHTML()
        {
            return $"<p><font color=\"{HTMLColor}\">{ToString()}</font></p>";
        }

        /// <summary>   Returns a string that represents the current object. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents the current object. </returns>

        public override string ToString()
        {
            return $"{Port}";
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases the unmanaged resources used by the AnalysisServiceEvent and optionally releases the
        /// managed resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #endregion

    #region LoggedEvent

    /// <summary>   A logged event. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class LoggedEvent : IEvent
    {
        /// <summary>   Method containing the measured item. </summary>
        ///
        /// <value> The method. </value>

        public string Method { get; set; } = "";

        /// <summary>   Full file name of the measured item. </summary>
        ///
        /// <value> The full pathname of the file. </value>

        public string FilePath { get; set; } = "";

        /// <summary>   Line number of measured item. </summary>
        ///
        /// <value> The line. </value>

        public int Line { get; set; } = -1;

        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        public string Message { get; set; }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        public string HTMLColor { get; set; } = "black";

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   This object as a string. </returns>

        public string ToHTML()
        {
            if (string.IsNullOrEmpty(Method))
                return $"<p><font color=\"{HTMLColor}\">{ToString()}</font></p>";
            return
                $"<p><font color=\"{HTMLColor}\">{ToString()}: Method: {Method}, Path: {Path.GetFileName(FilePath)}@{Line}</font></p>";
        }

        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents this object. </returns>

        public override string ToString()
        {
            return $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] EVENT - {Message}";
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases the unmanaged resources used by the LoggedEvent and optionally releases the managed
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #endregion

    #region TransactionEvent

    /// <summary>   A transaction event. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class TransactionEvent : IEvent
    {
        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        public string Message { get; set; }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        public string HTMLColor { get; set; } = "black";

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   This object as a string. </returns>

        public string ToHTML()
        {
            return $"<p><font color=\"{HTMLColor}\">{ToString()}</font></p>";
        }

        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents this object. </returns>

        public override string ToString()
        {
            return $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] TransactionEvent - {Message}";
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases the unmanaged resources used by the TransactionEvent and optionally releases the
        /// managed resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #endregion

    #region EventLogger

    /// <summary>   Easy way to wrap and log something. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class EventDuration : IDisposable
    {
        /// <summary>   (Immutable) the message. </summary>
        private readonly string _message;
        /// <summary>   (Immutable) the start. </summary>
        private readonly double _start;

        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="message">  The message. </param>

        public EventDuration(string message)
        {
            _start = BugLogger.CurrentTime;
            _message = message;
            EventLogger.AddEvent($"Enter: {_message}");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public void Dispose()
        {
            EventLogger.AddEvent(
                $"Exit {EngineeringNotationFormatter.Format(BugLogger.CurrentTime - _start) + "s"}: {_message}");
        }
    }

    /// <summary>   Values that represent event highlight types. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public enum EventHighlightType
    {
        /// <summary>   An enum constant representing the normal option. </summary>
        Normal,
        /// <summary>   An enum constant representing the red option. </summary>
        Red,
        /// <summary>   An enum constant representing the blue option. </summary>
        Blue,
        /// <summary>   An enum constant representing the brown option. </summary>
        Brown,
        /// <summary>   An enum constant representing the blue violet option. </summary>
        BlueViolet,
        /// <summary>   An enum constant representing the dark green option. </summary>
        DarkGreen,
        /// <summary>   An enum constant representing the green option. </summary>
        Green,
        /// <summary>   An enum constant representing the deep pink option. </summary>
        DeepPink,
        /// <summary>   An enum constant representing the violet option. </summary>
        Violet,
        /// <summary>   An enum constant representing the analysis option. </summary>
        Analysis,
        /// <summary>   An enum constant representing the data store option. </summary>
        DataStore,
        /// <summary>   An enum constant representing the acquisition option. </summary>
        Acquisition,
        /// <summary>   An enum constant representing the instrument option. </summary>
        Instrument,
        /// <summary>   An enum constant representing the pi option. </summary>
        PI
    }

    /// <summary>   An event logger. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class EventLogger : IEnumerable<LoggedEvent>
    {
        /// <summary>   (Immutable) the events. </summary>
        private static readonly CircularBuffer<LoggedEvent> _events = new CircularBuffer<LoggedEvent>(BugLogger.MaxEvents);

        #region Static Constructor

        /// <summary>
        /// Static Constructor - this assume if you get here - you wanted logging. Down stream
        /// applications need to disable this explicitly.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        static EventLogger()
        {
            EnableStackTrace = true;
        }

        #endregion

        #region EnableStackTrace

        /// <summary>   Enable stack trace. </summary>
        ///
        /// <value> True if enable stack trace, false if not. </value>

        public static bool EnableStackTrace { get; set; }

        #endregion

        /// <summary>   Gets the read lock. </summary>
        ///
        /// <value> The read lock. </value>

        public static IDisposable ReadLock => _events.ReadLock;

        /// <summary>   Gets the write lock. </summary>
        ///
        /// <value> The write lock. </value>

        public static IDisposable WriteLock => _events.WriteLock;

        /// <summary>   Gets the events. </summary>
        ///
        /// <value> The events. </value>

        public static IEnumerable<LoggedEvent> Events
        {
            get
            {
                using (_events.ReadLock)
                {
                    return _events.ToArray();
                }
            }
        }

        /// <summary>   Returns an enumerator that iterates through the collection. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   An enumerator that can be used to iterate through the collection. </returns>

        public IEnumerator<LoggedEvent> GetEnumerator()
        {
            return _events.GetEnumerator();
        }

        /// <summary>   Returns an enumerator that iterates through a collection. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through
        /// the collection.
        /// </returns>

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>   Clears this object to its blank/initial state. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void Clear()
        {
            _events.Clear();
        }

        /// <summary>   Adds an event. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="message">          The message. </param>
        /// <param name="highlightType">    (Optional) Type of the highlight. </param>
        /// <param name="memberName">       (Optional) Name of the member. </param>
        /// <param name="sourceFilePath">   (Optional) Full pathname of the source file. </param>
        /// <param name="sourceLineNumber"> (Optional) Source line number. </param>

        public static void AddEvent(string message, EventHighlightType highlightType = EventHighlightType.Normal,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Debug.WriteLine($"[Event]-{message}");
            switch (highlightType)
            {
                case EventHighlightType.Red:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"Red\">{message}</font>",
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.Blue:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"Blue\">{message}</font>",
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.Brown:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"Brown\">{message}</font>",
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.BlueViolet:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"BlueViolet\">{message}</font>",
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.DarkGreen:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"DarkGreen\">{message}</font>",
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.DeepPink:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"DeepPink\">{message}</font>",
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.Violet:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"Violet\">{message}</font>",
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.Green:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"Green\">{message}</font>",
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.Analysis:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"DodgerBlue\">{message}</font>", TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime, Method = memberName, FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.Acquisition:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"BlueViolet\">{message}</font>", TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime, Method = memberName, FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.Instrument:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"MediumVioletRed\">{message}</font>", TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime, Method = memberName, FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.DataStore:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"Maroon\">{message}</font>", TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime, Method = memberName, FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                case EventHighlightType.PI:
                    AddEvent(new LoggedEvent
                    {
                        Message = $"<font color=\"Blue\">{message}</font>", TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime, Method = memberName, FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
                default:
                    AddEvent(new LoggedEvent
                    {
                        Message = message,
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime,
                        Method = memberName,
                        FilePath = sourceFilePath,
                        Line = sourceLineNumber
                    });
                    break;
            }
        }

        /// <summary>   Adds an event. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="e">    A LoggedEvent to process. </param>

        public static void AddEvent(LoggedEvent e)
        {
            if (LocalHost.IsProduction) return;
            using (_events.WriteLock)
            {
                _events.Add(e);
            }

            FileLogger.Write(e.ToString());
        }
    }

    #endregion

    #region TransactionLogger

    /// <summary>   A transaction logger. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class TransactionLogger : IEnumerable<TransactionEvent>
    {
        /// <summary>   (Immutable) the events. </summary>
        private static readonly CircularBuffer<TransactionEvent> _events = new CircularBuffer<TransactionEvent>(BugLogger.MaxEvents);

        #region Static Constructor

        /// <summary>
        /// Static Constructor - this assume if you get here - you wanted logging. Down stream
        /// applications need to disable this explicitly.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        static TransactionLogger()
        {
            EnableStackTrace = true;
        }

        #endregion

        #region EnableStackTrace

        /// <summary>   Enable stack trace. </summary>
        ///
        /// <value> True if enable stack trace, false if not. </value>

        public static bool EnableStackTrace { get; set; }

        #endregion

        /// <summary>   Gets the events. </summary>
        ///
        /// <value> The events. </value>

        public static IEnumerable<TransactionEvent> Events
        {
            get
            {
                using (_events.ReadLock)
                {
                    return _events.ToArray();
                }
            }
        }

        /// <summary>   Returns an enumerator that iterates through the collection. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   An enumerator that can be used to iterate through the collection. </returns>

        public IEnumerator<TransactionEvent> GetEnumerator()
        {
            return _events.GetEnumerator();
        }

        /// <summary>   Returns an enumerator that iterates through a collection. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through
        /// the collection.
        /// </returns>

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>   Adds an event. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="message">  The message. </param>

        public static void AddEvent(string message)
        {
            AddEvent(new TransactionEvent { Message = message, TimeStamp = DateTime.Now, Time = BugLogger.CurrentTime });
        }

        /// <summary>   Adds an event. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="e">    A TransactionEvent to process. </param>

        public static void AddEvent(TransactionEvent e)
        {
            using (_events.WriteLock)
            {
                _events.Add(e);
            }
        }
    }

    #endregion

    #region PIItem

    /// <summary>   PI Command Type. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public enum PIItemType
    {
        /// <summary>
        ///     Command
        /// </summary>
        Command,

        /// <summary>
        ///     Response
        /// </summary>
        Response,

        /// <summary>
        ///     Control
        /// </summary>
        Control
    }

    /// <summary>   PI Command Log entry. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class PIItem : IEvent
    {
        /// <summary>   Command/Response string. </summary>
        public string Command;

        /// <summary>   Number of commands/responses to this pont. </summary>
        public int Count;

        /// <summary>   Name of parser this is happening on. </summary>
        public string Parser;


        /// <summary>   Type of PI action (Command/Response) </summary>
        public PIItemType Type;

        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.MinValue;

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        public string Message
        {
            get
            {
                switch (Type)
                {
                    case PIItemType.Command:
                        return $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] [PI-C] {Count} - {Command}";
                    case PIItemType.Response:
                        return $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] [PI-R] {Count} - {Command}";
                    case PIItemType.Control:
                        return $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] [PI-X] {Count} - {Command}";
                }

                return "";
            }

            set { }
        }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        public string HTMLColor { get; set; } = "blue";

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   This object as a string. </returns>

        public string ToHTML()
        {
            return $"<p><font color=\"{HTMLColor}\">{ToString()}</font></p>";
        }

        /// <summary>   ToString. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents this object. </returns>

        public override string ToString()
        {
            return Message;
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases the unmanaged resources used by the PIItem and optionally releases the managed
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }

    #endregion

    #region CircularBuffer

    /// <summary>   CircularBuffer - oddly, this is not defined in the .net libraries. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>
    ///
    /// <typeparam name="T">    Type to store. </typeparam>

    public class CircularBuffer<T> : IEnumerable<T>
    {
        #region Constructor

        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="length">   (Optional) Size of cirular queue. </param>

        public CircularBuffer(int length = 100)
        {
            _length = length > 0 ? length : 100;
        }

        #endregion

        #region Count

        /// <summary>
        /// Number of times in the buffer. This can be less than the size of the buffer.
        /// </summary>
        ///
        /// <value> The count. </value>

        public int Count
        {
            get
            {
                using (ReadLock)
                {
                    return _buffer.Count;
                }
            }
        }

        #endregion

        #region Indexer

        /// <summary>   Indexer - 0 is last command, 1 is the command before that, and so on. </summary>
        ///
        /// <param name="index">    . </param>
        ///
        /// <returns>   The indexed item. </returns>

        public T this[int index]
        {
            get
            {
                using (ReadLock)
                {
                    return _buffer[index];
                }
            }
        }

        #endregion

        /// <summary>   Gets the current. </summary>
        ///
        /// <value> The current. </value>

        public T Current
        {
            get
            {
                using (ReadLock)
                {
                    if (Count > 0) return _buffer[_buffer.Count - 1];
                    return default;
                }
            }
        }

        #region ReadLock

        /// <summary>
        /// Gives Read access to the dictionary. This is intended to be used in a using statement.
        /// Otherwise the object must be disposed to release the lock.
        /// </summary>
        ///
        /// <value> The read lock. </value>

        public ConReadLock ReadLock => new ConReadLock(_lock);

        #endregion

        #region WriteLock

        /// <summary>
        /// Gives Write access to the dictionary. This is intended to be used in a using statement.
        /// Otherwise the object must be disposed to release the lock.
        /// </summary>
        ///
        /// <value> The write lock. </value>

        public ConWriteLock WriteLock => new ConWriteLock(_lock);

        #endregion

        /// <summary>   Convert this object into an array representation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   An array that represents the data in this object. </returns>

        public T[] ToArray()
        {
            using (ReadLock)
            {
                return _buffer.ToArray();
            }
        }

        #region Add

        /// <summary>   Add a new item to circular buffer. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="v">    . </param>

        public void Add(T v)
        {
            using (WriteLock)
            {
                _buffer.Add(v);
                while (_buffer.Count > _length)
                {
                    var item = _buffer[0];
                    _buffer.RemoveAt(0);

                    // Not a great idea in general
                    if (item is IDisposable disposableItem) disposableItem.Dispose();
                }
            }
        }

        #endregion

        #region Clear

        /// <summary>   Clear buffer. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public void Clear()
        {
            _buffer.Clear();
        }

        #endregion

        #region Enumeration

        /// <summary>   Gets the enumerator. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   The enumerator. </returns>

        public IEnumerator<T> GetEnumerator()
        {
            using (ReadLock)
            {
                return _buffer.GetEnumerator();
            }
        }

        /// <summary>   Gets the enumerator. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   The enumerator. </returns>

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Fields

        /// <summary>   (Immutable) the buffer. </summary>
        private readonly List<T> _buffer = new List<T>();
        /// <summary>   (Immutable) the lock. </summary>
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        /// <summary>   (Immutable) the length. </summary>
        private readonly int _length = 100;

        #endregion
    }

    #endregion

    #region FileLogger

    /// <summary>   Log to a file. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class FileLogger
    {
        /// <summary>   (Immutable) the lock. </summary>
        private static readonly ConLockWrapper _lock = new ConLockWrapper();

        #region Static Constructor

        /// <summary>
        /// Initiate an instance of SimpleLogger class constructor. If log file does not exist, it will
        /// be created automatically.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        static FileLogger()
        {
            try
            {
                DatetimeFormat = LocalHost.DateTimeFormat;
            }
            catch (Exception e)
            {
                BugLogger.ThrowException(e);
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="FileLogger" /> is enabled.
        /// </summary>
        ///
        /// <value> <c>true</c> if enabled; otherwise, <c>false</c>. </value>

        public static bool Enabled { get; set; } = true;

        #region Debug

        /// <summary>   Debugs. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">   . </param>
        /// <param name="args">     . </param>

        public static void Debug(string format, params object[] args)
        {
#if FILELOG
            WriteFormattedLog(LogType.DEBUG, string.Format(format, args));
#endif
        }

        #endregion

        #region Error

        /// <summary>   Errors. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">   . </param>
        /// <param name="args">     . </param>

        public static void Error(string format, params object[] args)
        {
#if FILELOG
            WriteFormattedLog(LogType.ERROR, string.Format(format, args));
#endif
        }

        #endregion

        #region Fatal

        /// <summary>   Fatals. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">   . </param>
        /// <param name="args">     . </param>

        public static void Fatal(string format, params object[] args)
        {
#if FILELOG
            WriteFormattedLog(LogType.FATAL, string.Format(format, args));
#endif
        }

        #endregion

        #region Info

        /// <summary>   Infoes. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">   . </param>
        /// <param name="args">     . </param>

        public static void Info(string format, params object[] args)
        {
#if FILELOG
            WriteFormattedLog(LogType.INFO, string.Format(format, args));
#endif
        }

        #endregion

        #region GetStackTrace

        /// <summary>   Gets stack trace. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   The stack trace. </returns>

        public static string GetStackTrace()
        {
            var sb = new StringBuilder();
            var st = new StackTrace(true);
            for (var i = 1; i < st.FrameCount; i++)
            {
                var sf = st.GetFrame(i);
                sb.AppendLine(string.Format("\t{3}: Method: {0}, File: {1}, Line Number: {2}", sf.GetMethod(),
                    sf.GetFileName(), sf.GetFileLineNumber(), i));
            }

            return sb.ToString();
        }

        #endregion

        #region Trace

        /// <summary>   Traces. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">   . </param>
        /// <param name="args">     . </param>

        public static void Trace(string format, params object[] args)
        {
#if FILELOG
            WriteFormattedLog(LogType.TRACE, string.Format(format, args));
#endif
        }

        #endregion

        #region LogType

        /// <summary>   Type of log entries. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        [Flags]
        private enum LogType
        {
            /// <summary>
            /// </summary>
            TRACE,

            /// <summary>
            /// </summary>
            INFO,

            /// <summary>
            /// </summary>
            DEBUG,

            /// <summary>
            /// </summary>
            WARNING,

            /// <summary>
            /// </summary>
            ERROR,

            /// <summary>
            /// </summary>
            FATAL,

            /// <summary>
            /// </summary>
            NONE
        }

        #endregion

        #region Warning

        /// <summary>   Warnings. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">   . </param>
        /// <param name="args">     . </param>

        public static void Warning(string format, params object[] args)
        {
#if FILELOG
            WriteFormattedLog(LogType.WARNING, string.Format(format, args));
#endif
        }

        /// <summary>   Writes. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">   . </param>
        ///
        /// ### <param name="args"> . </param>

        public static void Write(string format)
        {
#if FILELOG
            try
            {
                WriteLine(format);
            }
            catch
            {
                // Ignore
            }
#endif
        }

        #endregion

        #region Fields

        /// <summary>   (Immutable) the datetime format. </summary>
        private static readonly string DatetimeFormat;
        /// <summary>   Filename of the file. </summary>
        private static string _filename;

        /// <summary>   Gets new file name. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="folder">   (Optional) Pathname of the folder. </param>
        ///
        /// <returns>   The new file name. </returns>

        private static string GetNewFileName(System.Environment.SpecialFolder folder = Environment.SpecialFolder.Desktop)
        {
            var processModule = Process.GetCurrentProcess().MainModule;
            string name = processModule == null ? "" : Path.GetFileNameWithoutExtension(processModule.FileName);
            return Path.Combine(Environment.GetFolderPath(folder), $"log{name}.txt");
        }

        /// <summary>   Gets or sets the filename of the log file. </summary>
        ///
        /// <value> The filename of the log file. </value>

        public static string LogFilename
        {
            get
            {
#if LOGTODESKTOP
                return _filename = _filename ?? GetNewFileName(Environment.SpecialFolder.Desktop);
#else
            return _filename ?? (_filename = GetNewFileName(Environment.SpecialFolder.MyDocuments));
#endif
            }
            set
            {
                if (File.Exists(_filename))
                    File.Delete(_filename);

                _filename = value;
                File.Delete(_filename);

                // Log file header line
                var logHeader = _filename + " is created.";

                WriteLine(DateTime.Now.ToString(DatetimeFormat) + " " + logHeader, false);
            }
        }

        #endregion

        #region WriteLog

        /// <summary>   Writes a formatted log. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="level">    The level. </param>
        /// <param name="text">     The text. </param>

        private static void WriteFormattedLog(LogType level, string text)
        {
            if (BugLogger.LogFileEnabled)
            {
                string pretext;

                if (LocalHost.IsProduction && level != LogType.ERROR && level != LogType.FATAL) return;

                switch (level)
                {
                    case LogType.TRACE:
                        pretext = "[" + DateTime.Now.ToString(DatetimeFormat) + "]" + " TRACE - ";
                        break;
                    case LogType.INFO:
                        pretext = "[" + DateTime.Now.ToString(DatetimeFormat) + "]" + " INFO  - ";
                        break;
                    case LogType.DEBUG:
                        pretext = "[" + DateTime.Now.ToString(DatetimeFormat) + "]" + " DEBUG - ";
                        break;
                    case LogType.WARNING:
                        pretext = "[" + DateTime.Now.ToString(DatetimeFormat) + "]" + " WARN  - ";
                        break;
                    case LogType.ERROR:
                        pretext = "[" + DateTime.Now.ToString(DatetimeFormat) + "]" + " ERROR - ";
                        break;
                    case LogType.FATAL:
                        pretext = "[" + DateTime.Now.ToString(DatetimeFormat) + "]" + " FATAL - ";
                        break;
                    default:
                        pretext = "";
                        break;
                }

                WriteLine(pretext + text);
            }
        }

        /// <summary>   Writes a line. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="text">     The text. </param>
        /// <param name="append">   (Optional) True to append. </param>

        private static void WriteLine(string text, bool append = true)
        {
            if (BugLogger.LogFileEnabled)
            {
                using (_lock.Lock)
                {
                    if (!Enabled || string.IsNullOrEmpty(text) || string.IsNullOrEmpty(LogFilename)) return;
                }

                Task.Run(() =>
                {
                    try
                    {
                        using (_lock.Lock)
                        {
                            try
                            {
#if VERBOSE
                                System.Diagnostics.Debug.WriteLine(text);
#endif
                                using (var writer = new StreamWriter(LogFilename, append, Encoding.UTF8))
                                {
                                    writer.WriteLine(text);
                                }
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                });
            }
        }

        #endregion
    }

    #endregion

    #region TaskMonitor

    /// <summary>
    /// Intended to be used in a using statement that wraps the actions in a task or thread.
    /// </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class TaskMonitor : IDisposable
    {
        #region Constructor

        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="message">  . </param>
        /// <param name="st">       (Optional) (Immutable) the st. </param>
        ///
        /// ### <param name="memberName">   . </param>
        ///
        /// ### <param name="sourceFilePath">   . </param>
        /// ### <param name="sourceLineNumber"> . </param>

        public TaskMonitor(string message,
            StackTrace st = null)
        {
            _message = message;
            var s = new StackTrace(true);
            if (s.FrameCount > 1)
            {
                var frame = s.GetFrame(1);
                _memberName = frame.GetMethod().ToString();
                _sourceLineNumber = frame.GetFileLineNumber();
                _sourceFilePath = Path.GetFileName(frame.GetFileName());
            }

            _st = st ?? new StackTrace(true);
            TaskLogger.Increment(ToString());
        }

        #endregion

        #region ToString

        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents this object. </returns>

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(
                $"\"{_message}\": Member: {_memberName}, Line {_sourceLineNumber}, Path: {Path.GetFileName(_sourceFilePath)}<br>");
            var count = 0;
            if (_st?.GetFrames() != null)
                foreach (var stack in _st.GetFrames())
                {
                    if (count != 0)
                    {
                        if (string.IsNullOrEmpty(Path.GetFileName(stack.GetFileName())))
                            sb.AppendLine($"     Method:{stack.GetMethod()}<br>");
                        else
                            sb.AppendLine(
                                $"     File:{Path.GetFileName(stack.GetFileName())}, Line:{stack.GetFileLineNumber()}, Method:{stack.GetMethod()}<br>");
                    }

                    count++;
                }

            return sb.ToString();
        }

        #endregion

        #region Fields

        /// <summary>   (Immutable) name of the member. </summary>
        private readonly string _memberName;
        /// <summary>   (Immutable) the message. </summary>
        private readonly string _message;
        /// <summary>   (Immutable) full pathname of the source file. </summary>
        private readonly string _sourceFilePath;
        /// <summary>   (Immutable) source line number. </summary>
        private readonly int _sourceLineNumber;
        /// <summary>   (Immutable) the st. </summary>
        private readonly StackTrace _st;
        /// <summary>   True if disposed. </summary>
        private bool _disposed;

        #endregion

        #region Dispose

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the TaskMonitor and optionally releases the managed
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    . </param>

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing) TaskLogger.Decrement(ToString());

            _disposed = true;
        }

        #endregion
    }

    #endregion

    #region TaskLogger

    /// <summary>   A task logger. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class TaskLogger
    {
        /// <summary>   TaskInfo. </summary>
        ///
        /// <value> Information describing the task. </value>

        public static IEnumerable<string> TaskInfo
        {
            get
            {
                if (LocalHost.IsProduction) return new string[0];
                lock (_lock)
                {
                    var list = new List<string>();

                    foreach (var key in _tasks.Keys)
                        if (_tasks[key] > 0)
                            list.Add($"{_tasks[key]} - {key}");

                    return list;
                }
            }
        }

        /// <summary>   Gets the read lock. </summary>
        ///
        /// <value> The read lock. </value>

        public static IDisposable ReadLock => _tasks.ReadLock;

        /// <summary>   Gets the write lock. </summary>
        ///
        /// <value> The write lock. </value>

        public static IDisposable WriteLock => _tasks.WriteLock;

        /// <summary>   Increments. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="s">    The string. </param>

        public static void Increment(string s)
        {
            lock (_lock)
            {
                if (_tasks.ContainsKey(s))
                    _tasks[s]++;
                else
                    _tasks.Add(s, 1);
            }
        }

        /// <summary>   Decrements. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="s">    The string. </param>

        public static void Decrement(string s)
        {
            lock (_lock)
            {
                if (!_tasks.ContainsKey(s)) return;

                if (_tasks[s] > 0) _tasks[s]--;
            }
        }

        #region Fields

        /// <summary>   (Immutable) the tasks. </summary>
        private static readonly ConDictionary<string, int> _tasks = new ConDictionary<string, int>();
        /// <summary>   (Immutable) the lock. </summary>
        private static readonly object _lock = new object();

        #endregion
    }

    #endregion

    #region LocalHost

    /// <summary>   Interface for information. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    internal interface IInfo
    {
        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        DateTime TimeStamp { get; set; }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        double Time { get; set; }
    }

    /// <summary>   Information about the memory. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    internal class MemoryInfo : IInfo
    {
        /// <summary>   Gets or sets the memory. </summary>
        ///
        /// <value> The memory. </value>

        public long Memory { get; set; }

        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;
    }

    /// <summary>   Information about the cpu. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    internal class CPUInfo : IInfo
    {
        /// <summary>   Gets or sets the CPU. </summary>
        ///
        /// <value> The CPU. </value>

        public double CPU { get; set; }

        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;
    }

    /// <summary>   A logged message. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    internal class LoggedMessage : IEvent
    {
        /// <summary>   Gets or sets the Date/Time of the time stamp. </summary>
        ///
        /// <value> The time stamp. </value>

        public DateTime TimeStamp { get; set; } = DateTime.Now;

        /// <summary>   Gets or sets the message. </summary>
        ///
        /// <value> The message. </value>

        public string Message { get; set; }

        /// <summary>   Gets or sets the time. </summary>
        ///
        /// <value> The time. </value>

        public double Time { get; set; } = BugLogger.CurrentTime;

        /// <summary>   Gets or sets the color of the HTML. </summary>
        ///
        /// <value> The color of the HTML. </value>

        public string HTMLColor { get; set; } = "black";

        /// <summary>   Converts this object to a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   This object as a string. </returns>

        public string ToHTML()
        {
            return $"<p><font color=\"{HTMLColor}\">{ToString()}</font></p>";
        }

        /// <summary>   Convert this object into a string representation. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string that represents this object. </returns>

        public override string ToString()
        {
            return $"[{TimeStamp.ToString(LocalHost.DateTimeFormat)}] Message - {Message}";
        }

        #region IDisposable Support

        /// <summary>   To detect redundant calls. </summary>
        private bool disposedValue;

        /// <summary>
        /// Releases the unmanaged resources used by the LoggedMessage and optionally releases the
        /// managed resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects).
                }

                disposedValue = true;
            }
        }

        /// <summary>   This code added to correctly implement the disposable pattern. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }

    /// <summary>   A page definition. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    internal class PageDefinition
    {
        /// <summary>   Gets or sets the name. </summary>
        ///
        /// <value> The name. </value>

        public string Name { get; set; }

        /// <summary>   Gets or sets the page. </summary>
        ///
        /// <value> A function delegate that yields a string. </value>

        public Func<string> Page { get; set; }
    }

    /// <summary>   A log metric item. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    internal class LogMetricItem : IDisposable
    {
        /// <summary>   (Immutable) the name. </summary>
        private readonly string _name = "";
        /// <summary>   (Immutable) the section. </summary>
        private readonly string _section = "";
        /// <summary>   (Immutable) the start time. </summary>
        private readonly double _startTime;

        /// <summary>   Constructor. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  The section. </param>
        /// <param name="name">     The name. </param>

        public LogMetricItem(string section, string name)
        {
            _name = name;
            _section = section;
            _startTime = BugLogger.CurrentTime;
        }

        #region IDisposable Support

        /// <summary>   True to disposed value. </summary>
        private bool disposedValue;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="disposing">    True to release both managed and unmanaged resources; false to
        ///                             release only unmanaged resources. </param>

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing) LogMetric.UpdateMetric(_section, _name, BugLogger.CurrentTime - _startTime, "S");

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources.
        /// </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion
    }

    /// <summary>   A log metric. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class LogMetric
    {
        #region Fields

        /// <summary>   (Immutable) the metrics. </summary>
        private static readonly ConDictionary<string, ConDictionary<string, StatisticAccumulation>> _metrics = new ConDictionary<string, ConDictionary<string, StatisticAccumulation>>();

        #endregion

        #region Sections

        /// <summary>   Gets the sections. </summary>
        ///
        /// <value> The sections. </value>

        public static IEnumerable<string> Sections
        {
            get
            {
                if (LocalHost.IsProduction) yield break;
                ;
                foreach (var key in _metrics.Keys.OrderBy(x => x)) yield return key;
            }
        }

        #endregion

        #region Section

        /// <summary>   Sections. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        ///
        /// <returns>   A ConDictionary&lt;string,StatisticAccumulation&gt; </returns>

        public static ConDictionary<string, StatisticAccumulation> Section(string section)
        {
            if (LocalHost.IsProduction) return null;
            if (_metrics.ContainsKey(section))
                return _metrics[section];
            return null;
        }

        #endregion

        #region Reset

        /// <summary>   Resets this object. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void Reset()
        {
            if (LocalHost.IsProduction) return;
            using (_metrics.WriteLock)
            {
                _metrics.Clear();
            }
        }

        #endregion

        #region Metric

        /// <summary>   Metrics. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        /// <param name="name">     . </param>
        ///
        /// <returns>   An IDisposable. </returns>

        public static IDisposable Metric(string section, string name)
        {
            return new LogMetricItem(section, name);
        }

        #endregion

        #region Page

        /// <summary>   Gets the page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        public static string Page()
        {
            var sb = new StringBuilder();
            using (_metrics.ReadLock)
            {
                foreach (var section in _metrics.Keys.OrderBy(x => x))
                {
                    sb.Append($"<h2>{section}</h2>");
                    sb.AppendLine("<div>");
                    sb.AppendLine(
                        @"<table><tr><th>Name</th><th>Mean</th><th>Min</th><th>Max</th><th>P2P</th><th>StdDev</th><th>Count</th><tr>");

                    foreach (var key in _metrics[section].Keys.OrderBy(x => x))
                        sb.AppendLine($@"<tr><td>{key}</td>
                                        <td>{EngineeringNotationFormatter.Format(_metrics[section][key].Mean)}{_metrics[section][key].Units}</td>
                                        <td>{EngineeringNotationFormatter.Format(_metrics[section][key].Minimum)}{_metrics[section][key].Units}</td>
                                        <td>{EngineeringNotationFormatter.Format(_metrics[section][key].Maximum)}{_metrics[section][key].Units}</td>
                                        <td>{EngineeringNotationFormatter.Format(_metrics[section][key].PeakToPeak)}</td>
                                        <td>{EngineeringNotationFormatter.Format(_metrics[section][key].StandardDeviation)}</td>
                                        <td>{EngineeringNotationFormatter.Format(_metrics[section][key].Count)}</td>
                                        </tr>");

                    sb.AppendLine("</table></div>");
                }
            }

            return sb.ToString();
        }

        #endregion

        #region DeleteSection

        /// <summary>   Deletes the section described by section. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>

        public static void DeleteSection(string section)
        {
            if (LocalHost.IsProduction) return;
            using (_metrics.WriteLock)
            {
                if (_metrics.ContainsKey(section)) _metrics.Remove(section);
            }
        }

        #endregion

        #region DeleteMetric

        /// <summary>   Deletes the metric. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        /// <param name="name">     . </param>

        public static void DeleteMetric(string section, string name)
        {
            if (LocalHost.IsProduction) return;
            using (_metrics.WriteLock)
            {
                if (_metrics.ContainsKey(section) && _metrics[section].ContainsKey(name)) _metrics[section].Remove(name);
            }
        }

        #endregion

        #region AddSection

        /// <summary>   Adds a section. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>

        public static void AddSection(string section)
        {
            if (LocalHost.IsProduction) return;
            using (_metrics.WriteLock)
            {
                if (!_metrics.ContainsKey(section))
                    _metrics.Add(section, new ConDictionary<string, StatisticAccumulation>());
            }
        }

        #endregion

        #region Lock

        /// <summary>   ReadLock. </summary>
        ///
        /// <value> The read lock. </value>

        public static IDisposable ReadLock => _metrics.ReadLock;

        /// <summary>   WriteLock. </summary>
        ///
        /// <value> The write lock. </value>

        public static IDisposable WriteLock => _metrics.WriteLock;

        /// <summary>   Lock. </summary>
        ///
        /// <value> The lock. </value>

        public static IDisposable Lock => _metrics.Lock;

        #endregion

        #region UpdateMetric

        /// <summary>   Updates the metric. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        /// <param name="name">     . </param>
        /// <param name="duration"> . </param>

        public static void UpdateMetric(string section, string name, double duration, string units = "")
        {
            if (LocalHost.IsProduction) return;
            Task.Run(() =>
                {
                    using (_metrics.WriteLock)
                    {
                        if (!_metrics.ContainsKey(section))
                            _metrics.Add(section, new ConDictionary<string, StatisticAccumulation>());
                        if (!_metrics[section].ContainsKey(name)) _metrics[section].Add(name, new StatisticAccumulation());
                        _metrics[section][name].Accumulate(duration, units);
                    }
                }
            );
        }

        /// <summary>   Updates the metric. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        /// <param name="name">     . </param>
        /// <param name="stats">    . </param>

        public static void UpdateMetric(string section, string name, IStatistics stats)
        {
            if (LocalHost.IsProduction) return;
            Task.Run(() =>
            {
                using (_metrics.WriteLock)
                {
                    if (!_metrics.ContainsKey(section))
                        _metrics.Add(section, new ConDictionary<string, StatisticAccumulation>());
                    if (!_metrics[section].ContainsKey(name)) _metrics[section].Add(name, new StatisticAccumulation());
                    _metrics[section][name].Accumulate(stats);
                }
            });
        }

        /// <summary>   Clears this object to its blank/initial state. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void Clear()
        {
            _metrics.Clear();
        }
        #endregion

    }

    /// <summary>   A local host. </summary>
    ///
    /// <remarks>   Keith, 12/1/2022. </remarks>

    public class LocalHost
    {
        public static string URL { get; set; }

        /// <summary>   (Immutable) the pages. </summary>
        private static readonly ConList<PageDefinition> Pages = new ConList<PageDefinition>();

        /// <summary>   Gets or sets the name of the application. </summary>
        ///
        /// <value> The name of the application. </value>

        public static string AppName { get; set; } = Process.GetCurrentProcess().ProcessName;

        /// <summary>   Gets or sets a context for the application. </summary>
        ///
        /// <value> The application context. </value>

        public static string AppContext { get; set; } = "";

        /// <summary>   Gets the date time format. </summary>
        ///
        /// <value> The date time format. </value>

        public static string DateTimeFormat => "yyyy-MM-dd HH:mm:ss.fffffff";

        /// <summary>   Gets a value indicating whether this object is production. </summary>
        ///
        /// <value> True if this object is production, false if not. </value>

        public static bool IsProduction { get; } = !BugLogger.WebLoggerEnabled;

        /// <summary>   Adds a page to 'page'. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name"> . </param>
        /// <param name="page"> The page. </param>

        public static void AddPage(string name, Func<string> page)
        {
            if (IsProduction) return;
            Pages.Add(new PageDefinition { Name = name, Page = page });
        }

        /// <summary>   Removes the page described by name. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name"> . </param>

        public static void RemovePage(string name)
        {
            if (IsProduction) return;
            using (Pages.WriteLock)
            {
                for (var i = Pages.Count - 1; i >= 0; i--)
                    if (string.Compare(Pages[i].Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                        Pages.RemoveAt(i);
            }
        }

        /// <summary>   Searches for the first page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name"> . </param>
        ///
        /// <returns>   The found page. </returns>

        internal static PageDefinition FindPage(string name)
        {
            using (Pages.ReadLock)
            {
                foreach (var page in Pages)
                    if (string.Compare(name, page.Name, StringComparison.CurrentCultureIgnoreCase) == 0 ||
                        string.Compare(name, "/" + page.Name, StringComparison.CurrentCultureIgnoreCase) == 0 ||
                        string.Compare(name, "/" + page.Name + "/", StringComparison.CurrentCultureIgnoreCase) == 0)
                        return page;
            }

            return new PageDefinition { Name = "Home", Page = IndexPage };
        }

        /// <summary>   Prepares this object for use. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void Startup()
        {
            try
            {
                AddPage("Home", TablePage);
                AddPage("Table", TablePage);
                AddPage("Chart", ChartPage);
                if (LockLedger.LockTrackingState)
                    AddPage("Locks", LocksPage);

                AddPage("Memory", MemoryPage);
                AddPage("Bugs", BugPage);
                AddPage("Bug Chart", BugLogger.Chart);
                AddPage("AllMessages", AllMessagePage);
                AddPage("Clear", ClearPage);

                var ct = _serverToken.Token;
                if (!IsProduction)
                {
                    _timer.Elapsed += UpdateMemory;
                    _timer.AutoReset = true;
                    _timer.Enabled = true;
                }

                _lock = new object();
                for (var port = GetPort; port < GetPort + 10; port++)
                {
                    if (IsProduction) continue;
                    _server = new HttpListener();
                    _server.Prefixes.Add($"http://localhost:{port}/");
                    try
                    {
                        _server.Start();
                        Task.Run(() => _serverLoop(ct), ct);
                        GetPort = port;
                        URL = $"http://localhost:{port}/";
                        return;
                    }
                    catch (Exception e)
                    {
                        FileLogger.Debug($"Logger - Server Statup Failure: \"{e.Message}\"");
                    }
                }
            }
            catch (Exception e)
            {
                FileLogger.Debug($"Logger - Statup Failure: \"{e.Message}\"");
            }
        }

        /// <summary>   Service startup. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="mainPort"> The main port. </param>

        public static int ServiceStartup(int mainPort)
        {
            try
            {
                AddPage("Home", TablePage);
                AddPage("Table", TablePage);
                AddPage("Chart", ChartPage);
                if (LockLedger.LockTrackingState)
                    AddPage("Locks", LocksPage);
                AddPage("Memory", MemoryPage);
                AddPage("Bugs", BugPage);
                AddPage("Bug Chart", BugLogger.Chart);
                AddPage("AllMessages", AllMessagePage);
                AddPage("Clear", ClearPage);

                var ct = _serverToken.Token;
                if (!IsProduction)
                {
                    _timer.Elapsed += UpdateMemory;
                    _timer.AutoReset = true;
                    _timer.Enabled = true;
                }

                _lock = new object();
                for (var port = mainPort; port < mainPort + 30; port++)
                {
                    if (IsProduction) continue;
                    _server = new HttpListener();
                    _server.Prefixes.Add($"http://localhost:{port}/");
                    try
                    {
                        _server.Start();
                        Task.Run(() => _serverLoop(ct), ct);
                        GetPort = port;
                        URL = $"http://localhost:{port}/";
                        FileLogger.Debug($"Logger - Server Statup Port: {GetPort}");
                        return port;
                    }
                    catch (Exception e)
                    {
                        FileLogger.Debug($"Logger - Server Statup Failure: \"{e.Message}\"");
                    }
                }
            }
            catch (Exception e)
            {
                FileLogger.Debug($"Logger - Statup Failure: \"{e.Message}\"");
            }

            return -1;
        }

        /// <summary>   Locks page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string LocksPage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<h2>Lock Status</h2>");
            sb.Append(LockLedger.HTMLReport());
            return sb.ToString();
        }

        /// <summary>   Clears the page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string ClearPage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<h2>Clear Complete</h2>");
            Clear();
            return sb.ToString();
        }

        /// <summary>   Shuts down this object and frees any resources it is using. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void Shutdown()
        {
            if (IsProduction) return;
            _serverToken?.Cancel(true);
            if (_server.IsListening)
                _server.Stop();
        }

        /// <summary>   Updates the memory. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="sender">   . </param>
        /// <param name="e">        . </param>

        private static void UpdateMemory(object sender, ElapsedEventArgs e)
        {
            if (IsProduction) return;
            _memoryUsage.Add(new MemoryInfo { Memory = GC.GetTotalMemory(false), TimeStamp = DateTime.Now });
            _physicalMemoryUsage.Add(new MemoryInfo
                { Memory = Process.GetCurrentProcess().PrivateMemorySize64, TimeStamp = DateTime.Now });
            _threadCountUsage.Add(new MemoryInfo { Memory = ThreadCount(), TimeStamp = DateTime.Now });
            _handleUsage.Add(new MemoryInfo { Memory = Process.GetCurrentProcess().HandleCount, TimeStamp = DateTime.Now });
#if CPU_MONITOR
            _cpuUsage.Add(new CPUInfo {CPU = CPU(), TimeStamp = DateTime.Now});
#endif
        }

        /// <summary>   Adds the statistics to 'stat'. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name"> . </param>
        /// <param name="stat"> . </param>

        public static void AddStats(string name, IStatistics stat)
        {
            lock (_updateInfo)
            {
                if (!_updateInfo.ContainsKey(name))
                {
                    _updateInfo.Add(name, stat);
                    return;
                }

                _updateInfo[name] = stat;
            }
        }

        /// <summary>   Deletes the section described by section. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>

        public static void DeleteSection(string section)
        {
            LogMetric.DeleteSection(section);
        }

        /// <summary>   Deletes the metric. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        /// <param name="name">     . </param>

        public static void DeleteMetric(string section, string name)
        {
            LogMetric.DeleteMetric(section, name);
        }

        /// <summary>   Adds a logged message to 'args'. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="format">   . </param>
        /// <param name="args">     . </param>

        public static void AddLoggedMessage(string format, params object[] args)
        {
            try
            {
                lock (_logList)
                {
                    _logList.Add(new LoggedMessage
                    {
                        Message = string.Format(format, args),
                        TimeStamp = DateTime.Now,
                        Time = BugLogger.CurrentTime
                    });
                    FileLogger.Write(string.Format(format, args));
                }
            }
            catch
            {
            }
        }

        /// <summary>   Removes the metric. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        /// <param name="name">     . </param>

        public static void RemoveMetric(string section, string name)
        {
            LogMetric.DeleteMetric(section, name);
        }

        /// <summary>   Adds a metric. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        /// <param name="name">     . </param>
        /// <param name="time">     . </param>

        public static void AddMetric(string section, string name, double time)
        {
            if (IsProduction) return;
            LogMetric.UpdateMetric(section, name, time);
        }

        /// <summary>   Adds a metric. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">  . </param>
        /// <param name="name">     . </param>
        /// <param name="stats">    . </param>

        public static void AddMetric(string section, string name, IStatistics stats)
        {
            if (IsProduction) return;
            LogMetric.UpdateMetric(section, name, stats);
        }

        /// <summary>   Index page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string IndexPage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<div>");
            sb.AppendLine(
                @"<table><tr><th>Name</th><th>Mean</th><th>Min</th><th>Max</th><th>P2P</th><th>Count</th><tr>");

            lock (_updateInfo)
            {
                foreach (var key in _updateInfo.Keys)
                    sb.AppendLine($@"<tr><td>{key}</td>
                                        <td>{EngineeringNotationFormatter.Format(_updateInfo[key].Mean)}{_updateInfo[key].Units}</td>
                                        <td>{EngineeringNotationFormatter.Format(_updateInfo[key].Minimum)}{_updateInfo[key].Units}</td>
                                        <td>{EngineeringNotationFormatter.Format(_updateInfo[key].Maximum)}{_updateInfo[key].Units}</td>
                                        <td>{EngineeringNotationFormatter.Format(_updateInfo[key].PeakToPeak)}</td>
                                        <td>{EngineeringNotationFormatter.Format(_updateInfo[key].Count)}</td>
                                        </tr>");
            }

            sb.AppendLine("</table></div>");
            return sb.ToString();
        }

        /// <summary>   The lock. </summary>
        private static object _lock;

        /// <summary>   The server. </summary>
        private static HttpListener _server;
        /// <summary>   (Immutable) list of logs. </summary>
        private static readonly CircularBuffer<LoggedMessage> _logList = new CircularBuffer<LoggedMessage>(BugLogger.MaxEvents);
        /// <summary>   (Immutable) the memory usage. </summary>
        private static readonly CircularBuffer<MemoryInfo> _memoryUsage = new CircularBuffer<MemoryInfo>(BugLogger.MaxEvents);
        /// <summary>   (Immutable) the physical memory usage. </summary>
        private static readonly CircularBuffer<MemoryInfo> _physicalMemoryUsage = new CircularBuffer<MemoryInfo>(BugLogger.MaxEvents);
        /// <summary>   (Immutable) the thread count usage. </summary>
        private static readonly CircularBuffer<MemoryInfo> _threadCountUsage = new CircularBuffer<MemoryInfo>(BugLogger.MaxEvents);
        /// <summary>   (Immutable) the handle usage. </summary>
        private static readonly CircularBuffer<MemoryInfo> _handleUsage = new CircularBuffer<MemoryInfo>(BugLogger.MaxEvents);

        /// <summary>   (Immutable) the timer. </summary>
        private static readonly Timer _timer = new Timer(15000);
        /// <summary>   (Immutable) information describing the update. </summary>
        private static readonly Dictionary<string, IStatistics> _updateInfo = new Dictionary<string, IStatistics>();
        /// <summary>   (Immutable) the server token. </summary>
        private static readonly CancellationTokenSource _serverToken = new CancellationTokenSource();
#if CPU_MONITOR
        private static readonly CircularBuffer<CPUInfo> _cpuUsage = new CircularBuffer<CPUInfo>(2000);
        private static readonly PerformanceCounter _total_cpu =
            new PerformanceCounter("Processor", "% Processor Time", "_Total");

        private static readonly PerformanceCounter _process_cpu =
            new PerformanceCounter("Process", "% Processor Time", Process.GetCurrentProcess().ProcessName);
#endif

        /// <summary>   Gets or sets the get port. </summary>
        ///
        /// <value> The get port. </value>

        public static int GetPort { get; private set; } = 55000;

        /// <summary>   Server loop. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="token">    A token that allows processing to be cancelled. </param>

        private static void _serverLoop(CancellationToken token)
        {
            if (IsProduction) return;
            while (true)
            {
                if (token.IsCancellationRequested) return;
                try
                {
                    var context = _server.GetContext();
                    Task.Run(() => _serveRequest(context), token);
                }
                catch
                {
                    // Ignore
                    _server.Stop();
                }
            }
        }

        /// <summary>   Serve request. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="context">  . </param>

        private static void _serveRequest(HttpListenerContext context)
        {
            _sendHtml(context, ToPage(FindPage(context.Request.Url.LocalPath)));
        }

        /// <summary>   Converts a page to a page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="page"> The page. </param>
        ///
        /// <returns>   Page as a string. </returns>

        private static string ToPage(PageDefinition page)
        {
            return $@"{Header(page.Name)}          
                      {page.Page()}
                      {Footer(page.Name)}";
        }

        /// <summary>   Headers. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name"> . </param>
        ///
        /// <returns>   A string. </returns>

        private static string Header(string name)
        {
            return $@"<html>
                         <head> 
                        {CSS()}
                        {ChartInfo()}
                        </head>
                        <title>{name}</title>
                         <body>
                            <h1>{AppName} {AppContext} Instrumentation</h1>
                            {Menu(name)}
                           <p>Reported: {DateTime.Now.ToString(CultureInfo.InvariantCulture)}</p>          
                         ";
        }

        /// <summary>   Footers. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name">     . </param>
        /// <param name="update">   (Optional) The update. </param>
        ///
        /// <returns>   A string. </returns>

        private static string Footer(string name, long update = 10000)
        {
            return $@"<script>
                    function refresh() {{ location.reload(); }}
                    var refreshTimer = setTimeout(refresh, {update});
                </script>
                </body>
                </html> ";
        }

        /// <summary>   Bug page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string BugPage()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<h2>Bug Messages</h2>");
            using (BugLogger.ReadLock)
            {
                if (!BugLogger.Bugs.Any()) return sb.ToString();
                foreach (var item in BugLogger.Bugs.OrderByDescending(x => x.Time))
                {
                    sb.AppendLine($"<p>{item}</p>");
                    sb.AppendLine("<p>Detailed Exception Information.</p>");
                    sb.AppendLine(ExceptionToTable(item.Exception));
                    sb.AppendLine("<p>Stack a more detailed stack trace.</p>");
                    sb.AppendLine(StackToTable(item.Stack));
                }
            }

            return sb.ToString();
        }

        /// <summary>   Enables the task message page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void EnableTaskMessagePage()
        {
#if RAVEN
            AddPage("Task", TaskMessagePage);
#endif
        }

        /// <summary>   Disables the task message page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void DisableTaskMessagePage()
        {
            RemovePage("Task");
        }

        /// <summary>   Task message page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string TaskMessagePage()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<h2>Task States</h2>");
            sb.AppendLine("<div>");

            using (TaskLogger.ReadLock)
            {
                foreach (var e in TaskLogger.TaskInfo)
                    sb.AppendLine($@"<p>{e}</p>");
            }

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        /// <summary>   Clears this object to its blank/initial state. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        public static void Clear()
        {
            _logList.Clear();
            EventLogger.Clear();
            LogMetric.Clear();
            BugLogger.Clear();
        }

        /// <summary>   All message page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string AllMessagePage()
        {
            var list = new List<IEvent>();

            using (_logList.ReadLock)
            {
                list.AddRange(_logList);
            }

            using (EventLogger.ReadLock)
            {
                list.AddRange(EventLogger.Events);
            }

            using (BugLogger.ReadLock)
            {
                list.AddRange(BugLogger.Bugs);
            }

#if JOBSINMESSAGE
            using (JobLogger.ReadLock)
                list.AddRange(JobLogger.Events);
#endif
            var sb = new StringBuilder();

            sb.AppendLine("<h2>All Messages</h2>");
            using (BugLogger.ReadLock)
            {
                sb.AppendLine(
                    $@"<p>This tab contains the last {BugLogger.MaxEvents} events recorded (except Transactions) in descending time order.</p>");
            }

            sb.AppendLine("<div>");

            var count = 0;
            foreach (var e in list.OrderByDescending(x => x.Time))
            {
                if (count >= BugLogger.MaxEvents) break;
                sb.Append(e.ToHTML());
                count++;
            }

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        /// <summary>   Memory page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string MemoryPage()
        {
#if CPU_MONITOR
            return $@"<h2>Garbage Collector Memory</h2>
                            {GCMemoryChart()}
                            <h2>Physical Memory</h2>
                            {PhysicalMemoryChart()}
                            <h2>Thread Count</h2>
                            {ThreadCountChart()}
                            <h2>CPU Usage</h2>
                            {CPUUsageChart()}
                            <h2>Memory</h2>
                            {MemorySection()}";
#else
            return $@"<h2>Garbage Collector Memory</h2>
                            {GCMemoryChart()}
                            <h2>Physical Memory</h2>
                            {PhysicalMemoryChart()}
                            <h2>Thread Count</h2>
                            {ThreadCountChart()}
                            <h2>Handles</h2>
                            {HandleChart()}
                            <h2>CPU Usage</h2>
                            {MemorySection()}";
#endif
        }

        /// <summary>   Gets the CSS. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string CSS()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<style>");
            sb.AppendLine("body { margin: 10; font-family: Arial, Helvetica, sans-serif;}");
            sb.AppendLine(".topnav {overflow: hidden; background-color: #333;}");
            sb.AppendLine(
                ".topnav a {float: left; color: #f2f2f2; text-align: center; padding: 14px 16px; text-decoration: none; font-size: 17px;}");
            sb.AppendLine(".topnav a:hover { background-color: #ddd; color: black; }");
            sb.AppendLine(".topnav a.active { background-color: #4CAF50; color: white;}");
            sb.AppendLine(_tableStyle());
            sb.AppendLine("</style>");
            return sb.ToString();
        }

        /// <summary>   Menus. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="current">  The current. </param>
        ///
        /// <returns>   A string. </returns>

        public static string Menu(string current)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<div class=\"topnav\">");

            using (Pages.ReadLock)
            {
                foreach (var page in Pages)
                    sb.AppendLine(string.Compare(current, page.Name, StringComparison.OrdinalIgnoreCase) == 0
                        ? $"<a class=\"active\" href=\"http://localhost:{GetPort}/{page.Name}\">{page.Name}</a>"
                        : $"<a href=\"http://localhost:{GetPort}/{page.Name}\">{page.Name}</a>");
            }

            sb.Append("</div>");

            return sb.ToString();
        }

        /// <summary>   Chart information. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string ChartInfo()
        {
            return "<script type=\"text/javascript\" src=\"https://www.gstatic.com/charts/loader.js\"></script>";
        }

        /// <summary>   Table style. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string _tableStyle()
        {
            return @"
                table, th, td {
                  border: 1px solid black;
                  border-collapse: collapse;
                  padding: 10px;
                  }
            ";
        }

        /// <summary>   Table page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string TablePage()
        {
            return LogMetric.Page();
        }

        /// <summary>   Message page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string MessagePage()
        {
            var sb = new StringBuilder();

            if (_logList.Count > 0)
            {
                sb.AppendLine("<h2>Runtime Messages</h2>");
                foreach (var item in _logList) sb.AppendLine($"<p>{item}</p>");
            }

            return sb.ToString();
        }

        /// <summary>   Event page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string EventPage()
        {
            var sb = new StringBuilder();

            sb.AppendLine("<h2>Event Messages</h2>");
            if (EventLogger.Events.Any())
                foreach (var item in EventLogger.Events.OrderByDescending(x => x.Time))
                    sb.AppendLine($"<p>{item}</p>");

            return sb.ToString();
        }

        /// <summary>   Transaction page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string TransPage()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<h2>Transaction Event Messages</h2>");
            var list = new List<IEvent>();
            foreach (var item in _logList) list.Add(item);

            foreach (var e in EventLogger.Events) list.Add(e);

            foreach (var bug in BugLogger.Bugs) list.Add(bug);

            foreach (var trans in TransactionLogger.Events) list.Add(trans);

            sb.AppendLine("<div>");

            var count = 0;
            foreach (var e in list.OrderByDescending(x => x.Time))
            {
                if (count >= BugLogger.MaxEvents) break;
                sb.AppendLine($@"<p>{e}</p>");
                count++;
            }

            sb.AppendLine("</div>");

            return sb.ToString();
        }

        /// <summary>   Memory section. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string MemorySection()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"<p>Total GC Memory: {EngineeringNotationFormatter.Format(GC.GetTotalMemory(false))}</p>");
            sb.AppendLine(
                $"<p>Total Physical Memory: {EngineeringNotationFormatter.Format(Process.GetCurrentProcess().PrivateMemorySize64)}</p>");
            for (var i = 0; i <= GC.MaxGeneration; i++)
                sb.AppendLine($"<p>GC Count Gen{i}: {EngineeringNotationFormatter.Format(GC.CollectionCount(i))}</p>");
            sb.AppendLine($"<p>Thread Count: {ThreadCount()}</p>");
            sb.AppendLine($"<p>Handle Count: {Process.GetCurrentProcess().HandleCount}</p>");
            return sb.ToString();
        }

        /// <summary>   Thread count. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A long. </returns>

        private static long ThreadCount()
        {
            return Process.GetCurrentProcess().Threads.Count;
        }

#if CPU_MONITOR
        public static double CPU()
        {
            try
            {
                return _process_cpu.NextValue();
            }
            catch
            {
                return 0.0;
            }
        }
#endif

        /// <summary>   Name fixup. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name"> . </param>
        ///
        /// <returns>   A string. </returns>

        private static string NameFixup(string name)
        {
            var retval = "";

            foreach (var c in name)
                if (char.IsLetterOrDigit(c))
                    retval += c;
                else
                    retval += "_";

            return retval;
        }

        /// <summary>   Chart page. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string ChartPage()
        {
            var sb = new StringBuilder();
            using (LogMetric.ReadLock)
            {
                foreach (var key in LogMetric.Sections)
                {
                    var section = LogMetric.Section(key);
                    if (section?.Count > 0)
                    {
                        sb.AppendLine("<div>");
                        sb.Append(PieChart(key, section));
                        sb.AppendLine("</div>");
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>   Pie chart. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="section">          . </param>
        /// <param name="sectionMetric">    . </param>
        ///
        /// <returns>   A string. </returns>

        private static string PieChart(string section, ConDictionary<string, StatisticAccumulation> sectionMetric)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<script type = \"text/javascript\" >");
            sb.AppendLine("google.charts.load(\'current\', { \'packages\':[\'corechart\']});");
            sb.AppendLine($"google.charts.setOnLoadCallback(drawChart{NameFixup(section)});");
            sb.AppendLine("");
            sb.AppendLine($"function drawChart{NameFixup(section)}()");
            sb.AppendLine("{");

            sb.AppendLine("var data = google.visualization.arrayToDataTable([");

            sb.AppendLine("[\'Metric\', \'Time\'],");
            foreach (var itemname in sectionMetric.Keys.OrderBy(x => x))
            {
                var item = sectionMetric[itemname];
                sb.AppendLine($"['{itemname}', {item.Mean}],");
            }

            sb.AppendLine("]);");

            sb.AppendLine("var options = {");
            sb.AppendLine($"title: \'{section}\',");
            sb.AppendLine("  pieHole: 0.15");

            sb.AppendLine("};");
            sb.AppendLine("");

            sb.AppendLine(
                $"var chart = new google.visualization.PieChart(document.getElementById(\'{NameFixup(section)}\'));");
            sb.AppendLine("");
            sb.AppendLine("chart.draw(data, options);");
            sb.AppendLine("}");
            sb.AppendLine("</script>");
            sb.AppendLine($"<div id=\"{NameFixup(section)}\" style=\"width: 1500px; height: 230px; \"></div>");
            return sb.ToString();
        }

        /// <summary>   GC memory chart. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>
        ///
        /// ### <param name="section">  . </param>
        ///
        /// ### <param name="sectionMetric">    . </param>

        private static string GCMemoryChart()
        {
            if (_memoryUsage.Count == 0) return "";

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<script type = \"text/javascript\" >");
                sb.AppendLine("google.charts.load(\'current\', { \'packages\':[\'corechart\']});");
                sb.AppendLine("google.charts.setOnLoadCallback(drawChartMemory);");
                sb.AppendLine("");
                sb.AppendLine("function drawChartMemory()");
                sb.AppendLine("{");

                sb.AppendLine("var data = google.visualization.arrayToDataTable([");

                sb.AppendLine("[\'Time\', \'Memory Usage\'],");

                using (_memoryUsage.ReadLock)
                {
                    foreach (var item in _memoryUsage)
                        sb.AppendLine($"['{item.TimeStamp.ToLongTimeString()}', {item.Memory}],");
                }

                sb.AppendLine("]);");

                sb.AppendLine("var options = {");
                sb.AppendLine("title: \'GC Memory Usage\'");
                sb.AppendLine("};");
                sb.AppendLine("");

                sb.AppendLine("var chart = new google.visualization.LineChart(document.getElementById(\'Memory\'));");
                sb.AppendLine("");
                sb.AppendLine("chart.draw(data, options);");
                sb.AppendLine("}");
                sb.AppendLine("</script>");
                sb.AppendLine("<div id=\"Memory\" style=\"width: 1500px; height: 230px; \"></div>");
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>   Physical memory chart. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>
        ///
        /// ### <param name="section">  . </param>
        ///
        /// ### <param name="sectionMetric">    . </param>

        private static string PhysicalMemoryChart()
        {
            if (_physicalMemoryUsage.Count == 0) return "";

            try
            {
                var sb = new StringBuilder();

                sb.AppendLine("<script type = \"text/javascript\" >");
                sb.AppendLine("google.charts.load(\'current\', { \'packages\':[\'corechart\']});");
                sb.AppendLine("google.charts.setOnLoadCallback(drawPhysicalChartMemory);");
                sb.AppendLine("");
                sb.AppendLine("function drawPhysicalChartMemory()");
                sb.AppendLine("{");

                sb.AppendLine("var data = google.visualization.arrayToDataTable([");

                sb.AppendLine("[\'Time\', \'Memory Usage\'],");

                using (_physicalMemoryUsage.ReadLock)
                {
                    foreach (var item in _physicalMemoryUsage)
                        sb.AppendLine($"['{item.TimeStamp.ToLongTimeString()}', {item.Memory}],");
                }

                sb.AppendLine("]);");

                sb.AppendLine("var options = {");
                sb.AppendLine("title: \'Physical Memory Usage\'");
                sb.AppendLine("};");
                sb.AppendLine("");

                sb.AppendLine(
                    "var chart = new google.visualization.LineChart(document.getElementById(\'PhysicalMemory\'));");
                sb.AppendLine("");
                sb.AppendLine("chart.draw(data, options);");
                sb.AppendLine("}");
                sb.AppendLine("</script>");
                sb.AppendLine("<div id=\"PhysicalMemory\" style=\"width: 1500px; height: 230px; \"></div>");
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>   Handles the chart. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>
        ///
        /// ### <param name="section">  . </param>
        ///
        /// ### <param name="sectionMetric">    . </param>

        private static string HandleChart()
        {
            if (_physicalMemoryUsage.Count == 0) return "";

            try
            {
                var sb = new StringBuilder();

                sb.AppendLine("<script type = \"text/javascript\" >");
                sb.AppendLine("google.charts.load(\'current\', { \'packages\':[\'corechart\']});");
                sb.AppendLine("google.charts.setOnLoadCallback(drawHandleMemory);");
                sb.AppendLine("");
                sb.AppendLine("function drawHandleMemory()");
                sb.AppendLine("{");

                sb.AppendLine("var data = google.visualization.arrayToDataTable([");

                sb.AppendLine("[\'Time\', \'Handles\'],");

                using (_handleUsage.ReadLock)
                {
                    foreach (var item in _handleUsage)
                        sb.AppendLine($"['{item.TimeStamp.ToLongTimeString()}', {item.Memory}],");
                }

                sb.AppendLine("]);");

                sb.AppendLine("var options = {");
                sb.AppendLine("title: \'Handles\'");
                sb.AppendLine("};");
                sb.AppendLine("");

                sb.AppendLine(
                    "var chart = new google.visualization.LineChart(document.getElementById(\'Handles\'));");
                sb.AppendLine("");
                sb.AppendLine("chart.draw(data, options);");
                sb.AppendLine("}");
                sb.AppendLine("</script>");
                sb.AppendLine("<div id=\"Handles\" style=\"width: 1500px; height: 230px; \"></div>");
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>   Thread count chart. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>
        ///
        /// ### <param name="section">  . </param>
        ///
        /// ### <param name="sectionMetric">    . </param>

        private static string ThreadCountChart()
        {
            if (_threadCountUsage.Count == 0) return "";

            try
            {
                var sb = new StringBuilder();

                sb.AppendLine("<script type = \"text/javascript\" >");
                sb.AppendLine("google.charts.load(\'current\', { \'packages\':[\'corechart\']});");
                sb.AppendLine("google.charts.setOnLoadCallback(drawThreadCountChartMemory);");
                sb.AppendLine("");
                sb.AppendLine("function drawThreadCountChartMemory()");
                sb.AppendLine("{");

                sb.AppendLine("var data = google.visualization.arrayToDataTable([");

                sb.AppendLine("[\'Time\', \'ThreadCount\'],");

                using (_threadCountUsage.ReadLock)
                {
                    foreach (var item in _threadCountUsage)
                        sb.AppendLine($"['{item.TimeStamp.ToLongTimeString()}', {item.Memory}],");
                }

                sb.AppendLine("]);");

                sb.AppendLine("var options = {");
                sb.AppendLine("title: \'Thread Count\'");
                sb.AppendLine("};");
                sb.AppendLine("");

                sb.AppendLine("var chart = new google.visualization.LineChart(document.getElementById(\'ThreadCount\'));");
                sb.AppendLine("");
                sb.AppendLine("chart.draw(data, options);");
                sb.AppendLine("}");
                sb.AppendLine("</script>");
                sb.AppendLine("<div id=\"ThreadCount\" style=\"width: 1500px; height: 230px; \"></div>");
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }
#if CPU_MONITOR
        /// <summary>
        /// </summary>
        /// <param name="section"></param>
        /// <param name="sectionMetric"></param>
        /// <returns></returns>
        private static string CPUUsageChart()
        {
            if (_cpuUsage.Count == 0) return "";

            var sb = new StringBuilder();

            sb.AppendLine("<script type = \"text/javascript\" >");
            sb.AppendLine("google.charts.load(\'current\', { \'packages\':[\'corechart\']});");
            sb.AppendLine("google.charts.setOnLoadCallback(drawCPUUsageChartMemory);");
            sb.AppendLine("");
            sb.AppendLine("function drawCPUUsageChartMemory()");
            sb.AppendLine("{");

            sb.AppendLine("var data = google.visualization.arrayToDataTable([");

            sb.AppendLine("[\'Time\', \'CPU Usage\'],");

            foreach (var item in _cpuUsage)
                sb.AppendLine($"['{item.TimeStamp.ToLongTimeString()}', {item.CPU}],");
            sb.AppendLine("]);");

            sb.AppendLine("var options = {");
            sb.AppendLine("title: \'CPU Usage\'");
            sb.AppendLine("};");
            sb.AppendLine("");

            sb.AppendLine("var chart = new google.visualization.LineChart(document.getElementById(\'CPUUsage\'));");
            sb.AppendLine("");
            sb.AppendLine("chart.draw(data, options);");
            sb.AppendLine("}");
            sb.AppendLine("</script>");
            sb.AppendLine("<div id=\"CPUUsage\" style=\"width: 1500px; height: 230px; \"></div>");
            return sb.ToString();
        }
#endif

        /// <summary>   Exception to table. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="ex">   The exception. </param>
        ///
        /// <returns>   A string. </returns>

        private static string ExceptionToTable(Exception ex)
        {
            if (ex == null) return "";
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<div>");
                sb.AppendLine(
                    @"<table><tr><th>Source</th><th>Message</th><th>Stack Trace</th><tr>");

                sb.AppendLine($@"<tr><td>{Path.GetFileName(ex.Source)}</td>
                                        <td>{ex.Message}</td>
                                        <td>{ex.StackTrace}</td>
                                        </tr>");

                sb.AppendLine("</table></div>");

                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>   Stack to table. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="stack">    The stack. </param>
        ///
        /// <returns>   A string. </returns>

        private static string StackToTable(StackFrame[] stack)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<div>");
                sb.AppendLine(
                    @"<table><tr><th>File Name</th><th>Line Number</th><th>Method</th><tr>");

                if (stack != null)
                    foreach (var item in stack)
                        sb.AppendLine($@"<tr><td>{Path.GetFileName(item.GetFileName())}</td>
                                        <td>{item.GetFileLineNumber()}</td>
                                        <td>{item.GetMethod()}</td>
                                        </tr>");

                sb.AppendLine("</table></div>");

                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>   Get's current thread status - not very useful. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <returns>   A string. </returns>

        private static string ThreadInfo()
        {
            try
            {
                var sb = new StringBuilder();
                var currentThreads = Process.GetCurrentProcess().Threads;
                sb.AppendLine("<h2>Threads</h2>");
                sb.AppendLine("<div>");
                sb.AppendLine(
                    @"<table><tr><th>ID</th><th>Thread State</th><th>User</th><tr>");

                foreach (ProcessThread thread in currentThreads)
                    sb.AppendLine($@"<tr><td>{thread.Id}</td>
                                        <td>{thread.ThreadState}</td>
                                        <td>{thread.UserProcessorTime}</td>
                                        </tr>");
                sb.AppendLine("</table></div>");

                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>   Sends a HTML. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="context">  . </param>
        /// <param name="html">     . </param>

        private static void _sendHtml(HttpListenerContext context, string html)
        {
            try
            {
                var response = Encoding.UTF8.GetBytes(html);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = MediaTypeNames.Text.Html;
                context.Response.ContentEncoding = Encoding.UTF8;
                context.Response.ContentLength64 = response.Length;
                context.Response.AddHeader("Cache-Control", "no-cache");
                using (var s = context.Response.OutputStream)
                {
                    s.Write(response, 0, response.Length);
                }
            }
            catch
            {
                // Ignore
            }
        }

        /// <summary>   Gauges. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>
        ///
        /// <param name="name">     . </param>
        /// <param name="value">    . </param>
        /// <param name="min">      (Optional) </param>
        /// <param name="max">      (Optional) </param>
        ///
        /// <returns>   A string. </returns>

        public static string Gauge(string name, double value, double min = 0.0, double max = 120.0)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("<script type = \"text/javascript\" >");
                sb.AppendLine("google.charts.load('current', {'packages':['gauge']});");
                sb.AppendLine("google.charts.setOnLoadCallback(drawGauge);");
                sb.AppendLine("function drawGauge() {");
                sb.AppendLine(
                    $"  var data = google.visualization.arrayToDataTable([['Label', 'Value'],['{name}', {(int)value}]]);");
                sb.AppendLine($"  var options = {{width: 200, height: 200, min: {(int)min}, max: {(int)max}}};");
                sb.AppendLine("  var chart = new google.visualization.Gauge(document.getElementById('gauge_div'));");
                sb.AppendLine("  chart.draw(data, options)");
                sb.AppendLine("}");
                sb.AppendLine("</script>");
                sb.AppendLine("<center><div id=\"gauge_div\" style=\"width: 200px; height: 200px;\" ></div></center>");
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        /// <summary>   A rectangle. </summary>
        ///
        /// <remarks>   Keith, 12/1/2022. </remarks>

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            /// <summary>   The left. </summary>
            public int left;
            /// <summary>   The top. </summary>
            public int top;
            /// <summary>   The right. </summary>
            public int right;
            /// <summary>   The bottom. </summary>
            public int bottom;
        }

#if NET48_OR_GREATER
        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr hWnd);

        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")]
        private static extern IntPtr ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        public static Bitmap CaptureApplication()
        {
            var proc = Process.GetCurrentProcess();

            // You need to focus on the application
            SetForegroundWindow(proc.MainWindowHandle);
            ShowWindow(proc.MainWindowHandle, SW_RESTORE);

            // You need some amount of delay, but 1 second may be overkill
            Thread.Sleep(1000);

            var rect = new Rect();
            var error = GetWindowRect(proc.MainWindowHandle, ref rect);

            // sometimes it gives error.
            while (error == (IntPtr)0) error = GetWindowRect(proc.MainWindowHandle, ref rect);

            var width = rect.right - rect.left;
            var height = rect.bottom - rect.top;

            var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Graphics.FromImage(bmp).CopyFromScreen(rect.left,
                rect.top,
                0,
                0,
                new Size(width, height),
                CopyPixelOperation.SourceCopy);

            return bmp;
        }
#endif
    }

    #endregion
}