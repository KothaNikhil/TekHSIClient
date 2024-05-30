using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Tek.Scope;
using Tek.Scope.Support;
using INormalizedHorizontal = Tek.Scope.Support.INormalizedHorizontal;
using INormalizedVector = Tek.Scope.Support.INormalizedVector;
using INormalizedVectorEx = Tek.Scope.Support.INormalizedVectorEx;
using INormalizedVertical = Tek.Scope.Support.INormalizedVertical;

namespace TekHighspeedAPI
{
    /// <summary>   A raw vector. No copy. Passed in array used directly.</summary>
    ///
    /// <typeparam name="T">    Generic type parameter. </typeparam>
    public class RawVector<T> :
        IVector<T>,
        INormalizedVectorEx,
        IStatistics,
        IDataStatus,
        IHorizontal<T>,
        INormalizedHorizontal
        where T : struct, IComparable<T>
    {
        #region Fields
        private T[] _array;
        private readonly Histogram _histogram = new Histogram();
        private bool _disposed;

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
        /// <param name="a"> </param>
        /// <param name="vSpacing"> </param>
        /// <param name="vOffset"> </param>
        /// <param name="hSpacing"> </param>
        /// <param name="hZeroIndex"> </param>
        public RawVector(T[] a, double vSpacing, double vOffset, double hSpacing, double hZeroIndex)
        {
            _vertical.GetHistogram += GetHistogram;
            _vertical.Spacing = vSpacing;
            _vertical.Offset = vOffset;
            ((IHorizontal<T>)this).Spacing = hSpacing;
            ((IHorizontal<T>)this).ZeroIndex = hZeroIndex;
            Type = VectorType.Sample;
            Access = AccessType.Raw;
            _array = a;
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
            for (int index1 = 0; (long)index1 < Count; ++index1)
            {
                int index2 = (int)Math.Round(this._histogram.Horizontal.ValueToIndex(((INormalizedVector)this)[index1]));
                if (index2 >= 0 && index2 < _histogram.Count)
                    ++_histogram[index2];
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
            get => _array.Length;
            set => throw new ReadOnlyException();
        }

        public int Count => _array.Length;

        public bool IsReadOnly => true;

        long INormalizedVector.Count
        {
            get => _array.Length;
            set => throw new ReadOnlyException();
        }

        double INormalizedVector.this[long index]
        {
            get => Convert.ToDouble(_array[index]) * _vertical.Spacing + _vertical.Offset;
            set => throw new ReadOnlyException();
        }

        double[] INormalizedVector.ToArray()
        {
            var retval = new double[Count];
            for (var i = 0; i < Count; i++) retval[i] = ((INormalizedVector)this)[i];
            return retval;
        }

        public IEnumerator<double> GetEnumerator()
        {
            for (long i = 0; i < Count; i++) yield return ((INormalizedVector)this)[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        bool IRange.Contains(double v)
        {
            throw new NotImplementedException();
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
        T[] IVector<T>.ToArray()
        {
            return _array;
        }

        void IVector<T>.Commit()
        {
        }

        IVertical<T> IVector<T>.Vertical => _vertical;

        IHorizontal<T> IVector<T>.Horizontal => this;

        long IVector<T>.Count { get; set; }

        T IVector<T>.this[long index]
        {
            get => _array[index];
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

            _vertical = null;
            _array = null;
            _disposed = true;
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