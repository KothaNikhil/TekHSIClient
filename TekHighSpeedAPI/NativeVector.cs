using System;
using System.Runtime.InteropServices;
using System.Text;
using Tek.Scope;

namespace TekHighspeedAPI
{
    public class NativeVertical<T> : INativeVertical<T>, INormalizedHorizontal, Tek.Scope.Support.INormalizedVertical
    {
        public double Position { get; set; }

        public double Offset { get; set; }

        public double Increment { get; set; } = 1.0;

        public string Units { get; set; } = "V";

        public double IndexToValue(double index)
        {
            return (index - ZeroIndex) * Spacing;
        }

        public double ValueToIndex(double hv)
        {
            return hv / Spacing + ZeroIndex;
        }

        public double Spacing
        {
            get { return Increment; }
            set { Increment = value; }
        }

        public long PrechargeCount { get; set; }

        public long PostchargeCount { get; set; }

        public double ZeroIndex { get; set; }

        public int IntegerZeroIndex { get; set; }

        public double FractionalZeroIndex { get; set; }
    }

    public unsafe class NativeVector8 : INativeVector<sbyte>, IRawInterface<sbyte>, INormalizedVectorEx,
        INativeHorizontal, Tek.Scope.Support.INormalizedVector, Tek.Scope.Support.INormalizedHorizontal, Tek.Scope.Support.INormalizedVertical,
        IDataStatus
    {
        private sbyte* _data = (sbyte*) IntPtr.Zero;
        private int _count = 0;
        private readonly NativeVertical<sbyte> _vertical = new NativeVertical<sbyte>();
        private bool _disposed = false;

        public double IndexToValue(double index)
        {
            return (index - ZeroIndex) * Spacing;
        }

        public double ValueToIndex(double hv)
        {
            return hv / Spacing + ZeroIndex;
        }

        public double Position
        {
            get;
            set;
        }

        public string Units { get; set; }

        public double Spacing { get; set; }

        public long PrechargeCount
        {
            get { return 0; }
            set { }
        }

        public long PostchargeCount
        {
            get { return 0; }
            set { }
        }

        public double ZeroIndex { get; set; }

        public int IntegerZeroIndex { get; set; }

        public double FractionalZeroIndex { get; set; }

        public INativeHorizontal NativeHorizontal => this;

        public INativeVertical<sbyte> NativeVertical => _vertical;

        public long MaxCount => int.MaxValue;

        Tek.Scope.Support.INormalizedHorizontal Tek.Scope.Support.INormalizedVector.Horizontal => this;

        public long Count
        {
            get { return _count; }
            set
            {
                if (((IntPtr)_data) == IntPtr.Zero)
                {
                    _data = (sbyte*) Marshal.AllocHGlobal((int) value);
                    _count = (int) value;
                }
                else
                {
                    Marshal.ReAllocHGlobal((IntPtr)_data, (IntPtr)(int)value);
                    _count = (int)value;
                }
            }
        }

        public double this[long index]
        {
            get { return _vertical.IndexToValue(_data[index]); }
            set { _data[index] = (sbyte)_vertical.ValueToIndex(value); }
        }

        sbyte INativeVector<sbyte>.this[long index]
        {
            get { return _data[index]; }
            set { _data[index] = value; }
        }

        public double[] ToArray()
        {
            var retval = new double[_count];
            for (int i = 0; i < _count; i++)
            {
                retval[i] = _vertical.IndexToValue(_data[i]);
            }
            return retval;
        }

        private byte[] _curve = null;

        public byte[] ToBytes()
        {
            int size = Count.ToString().Length;
            int total = (int) Count + size + 2;
            if (_curve != null && _curve.Length == total) 
                return _curve;
            _curve = new byte[Count + size + 2];
            byte[] s = Encoding.ASCII.GetBytes($"#{size}{Count}");
            Array.Copy(s, _curve, s.Length);
            Marshal.Copy((IntPtr)_data, _curve, s.Length, (int)Count);
            return _curve;
        }

        public void Commit()
        {
        }

        Tek.Scope.Support.INormalizedVertical Tek.Scope.Support.INormalizedVector.Vertical => _vertical;

        public INormalizedVertical Vertical => _vertical;

        public INormalizedHorizontal Horizontal => this;

        double INormalizedVector.this[long index]
        {
            get { return _vertical.IndexToValue(_data[index]); }
            set { _data[index] = (sbyte)_vertical.ValueToIndex(value); }
        }

        public string SourceName { get; set; }

        public long SizeOfViewInBytes => _count;

        public void* RawDataPtr => (void*) _data;

        public long SizeOfPreambleInBytes => 0;

        public void* RawPreamblePtr => (void*)IntPtr.Zero;

        public static INormalizedVector SineWave(double frequency, double cycles, double amplitude, double noise,
            long length)
        {
            var random = new Random();
            var duration = 1 / frequency * cycles;
            var phase = 0.0;
            var offset = 0.0;

            var v = new NativeVector8
            {
                Count = length,
                NativeHorizontal =
                {
                    Spacing = duration / length,
                    IntegerZeroIndex = (int)(length / 2),
                    FractionalZeroIndex = 0.0,
                    Units = "S"
                },
                _vertical =
                {
                    ZeroIndex = 0,
                    Spacing = (amplitude* 1.2 + noise*amplitude) / 255.0,
                    Units = "V"
                }
            };

            for (var index = 0; index < v.Count; ++index)
            {
                var num1 = v.Horizontal.IndexToValue(index);
                var num2 = random.NextDouble() * (amplitude * noise) - amplitude * noise / 2.0;
                v._data[index] = (sbyte)v._vertical.ValueToIndex(
                    Math.Sin(frequency * 3.14159265 * 2.0 * num1 + phase / 180.0 * 3.14159) * (amplitude / 2.0) +
                    offset + num2);
            }

            return v;
        }

        public bool IsMinMaxVector => false;

        protected void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            // Release any managed resources here.
            if (disposing && ((IntPtr) _data) != IntPtr.Zero)
            {
                Marshal.FreeHGlobal((IntPtr)_data);
                _data = (sbyte*) IntPtr.Zero;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool HasData { get; set; }

        public long TOC { get; set; }

        public long TID { get; set; }

        public QualEnum Qualifier { get; set; } = QualEnum.Valid;

        public string SymbolName { get; set; }
    }

    public unsafe class NativeVectorFloat : INativeVector<float>, IRawInterface<float>, INormalizedVectorEx,
        INativeHorizontal, Tek.Scope.Support.INormalizedVector, 
        Tek.Scope.Support.INormalizedHorizontal, Tek.Scope.Support.INormalizedVertical, IDataStatus
    {
        private float* _data = (float*)IntPtr.Zero;
        private int _count = 0;
        private readonly NativeVertical<float> _vertical = new NativeVertical<float>();
        private bool _disposed = false;

        public double IndexToValue(double index)
        {
            return (index - ZeroIndex) * Spacing;
        }

        public double ValueToIndex(double hv)
        {
            return hv / Spacing + ZeroIndex;
        }

        public double Position
        {
            get;
            set;
        }

        public string Units { get; set; }

        public double Spacing { get; set; }

        public long PrechargeCount
        {
            get { return 0; }
            set { }
        }

        public long PostchargeCount
        {
            get { return 0; }
            set { }
        }

        public double ZeroIndex { get; set; }

        public int IntegerZeroIndex { get; set; }

        public double FractionalZeroIndex { get; set; }

        public INativeHorizontal NativeHorizontal => this;

        INativeVertical<float> INativeVector<float>.NativeVertical => _vertical;

        public long MaxCount => int.MaxValue;

        Tek.Scope.Support.INormalizedHorizontal Tek.Scope.Support.INormalizedVector.Horizontal => this;

        public long Count
        {
            get { return _count; }
            set
            {
                if (((IntPtr)_data) == IntPtr.Zero)
                {
                    _data = (float*)Marshal.AllocHGlobal((int)value*sizeof(float));
                    _count = (int)value;
                }
                else
                {
                    Marshal.ReAllocHGlobal((IntPtr)_data, (IntPtr)((int)value*sizeof(float)));
                    _count = (int)value;
                }
            }
        }

        float INativeVector<float>.this[long index]
        {
            get { return _data[index]; }
            set { _data[index] = value; }
        }


        public double this[long index]
        {
            get { return _data[index]; }
            set { _data[index] = (float)value; }
        }

        public double[] ToArray()
        {
            var retval = new double[_count];
            for (int i = 0; i < _count; i++)
            {
                retval[i] = _data[i];
            }
            return retval;
        }

        public void Commit()
        {
        }

        Tek.Scope.Support.INormalizedVertical Tek.Scope.Support.INormalizedVector.Vertical => _vertical;

        public INormalizedVertical Vertical => _vertical;

        public INormalizedHorizontal Horizontal => this;

        double INormalizedVector.this[long index]
        {
            get { return _vertical.IndexToValue(_data[index]); }
            set { _data[index] = (sbyte)_vertical.ValueToIndex(value); }
        }

        public string SourceName { get; set; }

        public long SizeOfViewInBytes => _count;

        public void* RawDataPtr => (void*)_data;

        public long SizeOfPreambleInBytes => 0;

        public void* RawPreamblePtr => (void*)IntPtr.Zero;

        public static INormalizedVector SineWave(double frequency, double cycles, double amplitude, double noise,
            long length)
        {
            var random = new Random();
            var duration = 1 / frequency * cycles;
            var phase = 0.0;
            var offset = 0.0;

            var v = new NativeVectorFloat
            {
                Count = length,
                NativeHorizontal =
                {
                    Spacing = duration / length,
                    IntegerZeroIndex = (int)(length / 2),
                    FractionalZeroIndex = 0.0,
                    Units = "S"
                },
                _vertical =
                {
                    ZeroIndex = 0,
                    Spacing = 1.0,
                    Units = "V"
                }
            };

            for (var index = 0; index < v.Count; ++index)
            {
                var num1 = v.Horizontal.IndexToValue(index);
                var num2 = random.NextDouble() * (amplitude * noise) - amplitude * noise / 2.0;
                v._data[index] = (float) (Math.Sin(frequency * 3.14159265 * 2.0 * num1 + phase / 180.0 * 3.14159) * (amplitude / 2.0) + offset + num2);
            }

            return v;
        }

        public bool IsMinMaxVector => false;

        protected void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Release any managed resources here.
            if (disposing && ((IntPtr)_data) != IntPtr.Zero)
            {
                Marshal.FreeHGlobal((IntPtr)_data);
                _data = (float*)IntPtr.Zero;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public bool HasData { get; set; } 

        public long TOC { get; set; }

        public long TID { get; set; }

        public QualEnum Qualifier { get; set; } = QualEnum.Valid;

        public string SymbolName { get; set; }
    }
}