//#define VERBOSE

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Tek.Scope;
using Tek.Scope.Support;
using INormalizedHorizontal = Tek.Scope.Support.INormalizedHorizontal;
using INormalizedVector = Tek.Scope.Support.INormalizedVector;
using INormalizedVectorEx = Tek.Scope.Support.INormalizedVectorEx;
using INormalizedVertical = Tek.Scope.Support.INormalizedVertical;

namespace TekHighspeedAPI
{
    public class ChunkVector<T> :
        IVector<T>,
        INormalizedVectorEx,
        IStatistics,
        IDataStatus,
        IHorizontal<T>,
        INormalizedHorizontal
        where T : unmanaged, IComparable<T>
    {
        #region Fields
        private List<ReadOnlyMemory<byte>> _list = new List<ReadOnlyMemory<byte>>();
        private readonly List<MemoryHandle> _memoryHandles = new List<MemoryHandle>();
        private readonly Histogram _histogram = new Histogram();
        private bool _disposed = false;
        private readonly int _itemWidth = 0;
        private readonly long _count = 0;
        private long _chunksize = 0;
        private long _chunkElementSize = 0;

        /// <exclude />
        protected double _maxLocation;

        /// <exclude />
        protected double _minLocation;

        /// <exclude />
        protected double? _max;

        /// <exclude />
        protected double? _mean;

        /// <exclude />
        protected double? _min;

        /// <exclude />
        protected double? _stddev;

        /// <exclude />
        protected double _sum;

        /// <exclude />
        protected double _sumsqrd;

        protected Vertical<T> _vertical = new Vertical<T>();
        #endregion

        #region Constructor

        /// <summary>Constructor</summary>
        /// <param name="vSpacing"> </param>
        /// <param name="vOffset"> </param>
        /// <param name="hSpacing"> </param>
        /// <param name="hZeroIndex"> </param>
        /// <param name="count"></param>
        /// 
        public ChunkVector(double vSpacing, double vOffset, double hSpacing, double hZeroIndex, long count)
        {
#if VERBOSE
        EventLogger.AddEvent($"ChunkVector:vSpace={vSpacing},vOffset={vOffset},hSpacing={hSpacing},hZeroIndex={hZeroIndex},count={count}");
#endif
            _vertical.GetHistogram += GetHistogram;
            _vertical.Spacing = vSpacing;
            _vertical.Offset = vOffset;
            ((IHorizontal<T>)this).Spacing = hSpacing;
            ((IHorizontal<T>)this).ZeroIndex = hZeroIndex;
            Type = VectorType.Sample;
            Access = AccessType.Raw;
            _itemWidth = Marshal.SizeOf(typeof(T));
            _count = count;
        }

        public void Add(ReadOnlyMemory<byte> item)
        {
            if (_list.Count == 0)
            {
                _chunksize = item.Length;
                _chunkElementSize = _chunksize / _itemWidth;
            }
            
            _list.Add(item);
            _memoryHandles.Add(item.Pin());
        }

        public T GetAt(long index)
        {
            var chunkIndex = (int) (index / _chunkElementSize);
            var chunkOffset = (int) (index % _chunkElementSize);
            var chunkItem = _list[chunkIndex]; 
            if (chunkItem.Length < chunkOffset) 
                throw new IndexOutOfRangeException();
            unsafe
            {
                return *(((T*)(_memoryHandles[chunkIndex]).Pointer) + chunkOffset);
            }
        }

        private IHistogram GetHistogram()
        {
            if (_histogram.Count != 0L)
                return _histogram;
            CalculateHistogram();
            return _histogram;
        }

        /// <summary>
        /// </summary>
        protected virtual void CalculateHistogram()
        {
            _histogram.Count = 200L;
            double minimum = Minimum;
            _histogram.Horizontal.Spacing = (Maximum - minimum) / (double)_histogram.Count;
            _histogram.Horizontal.ZeroIndex = -minimum / _histogram.Horizontal.Spacing;
            _histogram.Horizontal.Units = _vertical.Units;

            for (int i = 0; (long)i < Count; ++i)
            {
                int index = (int)Math.Round(_histogram.Horizontal.ValueToIndex(((INormalizedVector)this)[i]));
                if (index >= 0 && index < _histogram.Count)
                    ++_histogram[index];
            }
        }

        #endregion

        #region IDataStatus
        /// <summary>
        /// 
        /// </summary>
        public bool HasData { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public long TOC { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public long TID { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public QualEnum Qualifier { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SymbolName { get; set; }
        #endregion

        /// <summary>
        ///     Converts an index into a horizontal value (usually time).
        /// </summary>
        /// <param name="index"> an index into the data array. Non-integer values will use linear interpolation to return a value. </param>
        /// <returns> A double in the vertical units (usually volts) </returns>
        public double IndexToValue(double index)
        {
            return (index - ZeroIndex) * Spacing;
        }

        /// <summary>
        ///     Convert a horizontal value (usually time) into a array index.
        /// </summary>
        /// <param name="hv"> Horizontal value (usually time) </param>
        /// <returns> Array index </returns>
        public double ValueToIndex(double hv)
        {
            return hv / Spacing + ZeroIndex;
        }

        public long PrechargeCount { get; set; }

        public long PostchargeCount { get; set; }

        public double ZeroIndex { get; set; }

        public int IntegerZeroIndex { get; set; }

        public double FractionalZeroIndex { get; set; }

        /// <summary>
        /// 
        /// </summary>
        void INormalizedVector.Commit()
        {
        }

        INormalizedVerticalEx INormalizedVectorEx.Vertical => (INormalizedVerticalEx)_vertical;

        public AccessType Access { get; set; }

        INormalizedVertical INormalizedVector.Vertical => (INormalizedVertical)_vertical;

        INormalizedHorizontal INormalizedVector.Horizontal => this;

        public void Add(double item)
        {
            throw new ReadOnlyException();
        }

        public void Clear()
        {
            throw new ReadOnlyException();
        }

        bool INormalizedVectorEx.Contains(double v)
        {
            return v >= Begin && v <= End;
        }

        public void SetArray(double[] array)
        {
            throw new ReadOnlyException();
        }

        public VectorType Type { get; set; }

        bool System.Collections.Generic.ICollection<double>.Contains(double item)
        {
            var v = (INormalizedVector)this;
            for (int i = 0; i < Count; i++)
                if (item == v[i])
                    return true;
            return false;
        }

        /// <summary>
        ///     Returns 0 if v intersects this range, -1 if it's before and
        ///     1 if it's after.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        int IRange.Intersect(IRange v)
        {
            if (IntersectArea(v) > 0.0)
                return 0;
            return v.End <= Begin ? -1 : 1;
        }

        public double Duration => End - Begin;

        public double Focus
        {
            get => (Begin - End) / 2.0 + Begin;
            set => throw new ReadOnlyException();
        }

        public double Begin
        {
            get => ((IHorizontal<T>)this).IndexToValue(0);
            set => throw new ReadOnlyException();
        }

        public double End
        {
            get => Begin + Count * ((IHorizontal<T>)this).Spacing;
            set => throw new ReadOnlyException();
        }

        public void CopyTo(double[] array, int arrayIndex)
        {
            for (int index = arrayIndex; (long)index < Count && index - arrayIndex < array.Length; ++index)
                array[index - arrayIndex] = ((INormalizedVector)this)[index];
        }

        public bool Remove(double item)
        {
            throw new ReadOnlyException();
        }

        long INormalizedVectorEx.Count
        {
            get => _count;
            set => throw new ReadOnlyException();
        }

        public int Count => (int) _count;

        public bool IsReadOnly => true;

        long INormalizedVector.Count
        {
            get => _count;
            set => throw new ReadOnlyException();
        }

        double INormalizedVector.this[long index]
        {
            get => Convert.ToDouble(GetAt(index)) * _vertical.Spacing + _vertical.Offset;
            set => throw new ReadOnlyException();
        }

        double[] INormalizedVector.ToArray()
        {
            var retval = new double[Count];

            try
            {
                Parallel.ForEach(Partitioner.Create(0, Count), range =>
                {
                    for (var i = range.Item1; i < range.Item2; i++)
                        retval[i] = ((INormalizedVector)this)[i];
                });
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                //BugLogger.CatchException(e);
            }

            return retval;
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (long i = 0; i < Count; i++) 
                yield return ((INormalizedVector)this)[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        bool IRange.Contains(double v)
        {
            try
            {
                var vec = ((INormalizedVector)this);
                for (long i = 0; i < Count; ++i)
                {
                    if (vec[i] == v)
                        return true;
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                //BugLogger.CatchException(e);
            }
        
            return false;
        }

        /// <summary>Returns the average of the Values.</summary>
        public virtual double Mean
        {
            get
            {
                if (!_mean.HasValue)
                    CalcStats();
                return _mean ?? double.NaN;
            }
        }

        /// <summary>Return the minimum value</summary>
        public virtual double Minimum
        {
            get
            {
                if (!_min.HasValue)
                    CalcStats();
                return _min ?? double.NaN;
            }
        }

        /// <summary>Returns the minimum value.</summary>
        public virtual double Maximum
        {
            get
            {
                if (!_max.HasValue)
                    CalcStats();
                return !_max.HasValue ? double.NaN : _max.Value;
            }
        }

        /// <summary>Returns the Standard Deviation</summary>
        public virtual double StandardDeviation
        {
            get
            {
                if (!_stddev.HasValue && Count > 0L)
                    CalcStats();
                return _stddev ?? double.NaN;
            }
        }

        /// <summary>Peak2Peak measurement</summary>
        public virtual double PeakToPeak => Maximum - Minimum;

        public IHistogram Histogram => GetHistogram();

        long IStatistics.Count => Count;

        public string Units { get; set; }

        public double Sum => _sum;

        public double SumSquared => _sumsqrd;

        public double MinimumLocation => _minLocation;

        public double MaximumLocation => _maxLocation;

        /// <summary>
        ///     Return Raw Array
        /// </summary>
        /// <returns></returns>
        unsafe T[] IVector<T>.ToArray()
        {
            var retval = new T[Count];
            var dataHandle = GCHandle.Alloc(retval, GCHandleType.Pinned);
            try
            {
                var tPtr = (byte*)dataHandle.AddrOfPinnedObject().ToPointer();
                var maxSize = Count * sizeof(T);
                Parallel.ForEach(Partitioner.Create(0, _memoryHandles.Count), range =>
                {
                    for (var index = range.Item1; index < range.Item2; index++)
                    {
                        var bytePtrIndex = index * _chunksize;
                        var size = (index == _memoryHandles.Count - 1 ? maxSize - bytePtrIndex : _chunksize);

                        //Buffer.MemoryCopy(_memoryHandles[index].Pointer,
                        //    tPtr + bytePtrIndex, size, size);
                    }
                });
            }
            finally
            {
                dataHandle.Free();
            }
        
            return retval;
        }

        void IVector<T>.Commit()
        {
        }

        IVertical<T> IVector<T>.Vertical => _vertical;

        IHorizontal<T> IVector<T>.Horizontal => this;

        long IVector<T>.Count { get; set; }

        T IVector<T>.this[long index]
        {
            get => GetAt(index);
            set => throw new ReadOnlyException();
        }

        public string SourceName { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            foreach (var h in _memoryHandles)
            {
                h.Dispose();
            }
            _memoryHandles.Clear();
            _list.Clear();

            _vertical = null;
            _list = null;
            _disposed = true;
        }

        ~ChunkVector()
        {
            Dispose(false);
        }

        public double Spacing { get; set; }

        /// <summary>Returns the intersection Area</summary>
        /// <param name="v"></param>
        /// <returns></returns>
        private double IntersectArea(IRange v)
        {
            if (v.Contains(End) && Begin <= v.Begin)
                return End - v.Begin;
            if (v.Contains(Begin) && End >= v.End)
                return v.End - Begin;
            if (Begin <= v.Begin && End >= v.End)
                return v.Duration;
            return Begin >= v.Begin && End <= v.End ? Duration : 0.0;
        }

        /// <summary>
        /// </summary>
        protected virtual void CalcStats()
        {
            var v = (INormalizedVectorEx)this;
            if (_stddev.HasValue || Count <= 0L)
                return;
            var sum = 0.0;
            var count = 0;
            _minLocation = _maxLocation = IndexToValue(0);
            _max = _min = v[0L];
            for (long i = 0; i < Count; i++)
            {
                var d = v[i];
                if (double.IsNaN(d)) continue;
                var num3 = d;
                var min = _min;
                if ((num3 >= min.GetValueOrDefault() ? 0 : min.HasValue ? 1 : 0) != 0)
                {
                    _min = d;
                    _minLocation = IndexToValue(i);
                }

                var num4 = d;
                var max = _max;
                if ((num4 <= max.GetValueOrDefault() ? 0 : max.HasValue ? 1 : 0) != 0)
                {
                    _max = d;
                    _maxLocation = IndexToValue(i);
                }

                sum += d;
                ++count;
            }

            _sum = sum;
            _mean = sum / count;
            var sumsqrd = Math.Pow(v[0L] - _mean.Value, 2.0);
            for (long index = 1; index < v.Count; ++index)
                if (!double.IsNaN(v[index]))
                    sumsqrd += Math.Pow(v[index] - _mean.Value, 2.0);
            _sumsqrd = sumsqrd;
            _stddev = Math.Sqrt(sumsqrd / (count - 1));
        }

        public override string ToString()
        {
            return $"Mean={Tek.Scope.Support.EngineeringNotationFormatter.Format(Mean)}{_vertical.Units}, Min={Tek.Scope.Support.EngineeringNotationFormatter.Format(Minimum)}{_vertical.Units}, Max={Tek.Scope.Support.EngineeringNotationFormatter.Format(Maximum)}{_vertical.Units}, P2P={Tek.Scope.Support.EngineeringNotationFormatter.Format(PeakToPeak)}{_vertical.Units},Count={Count}";
        }
    }
}
