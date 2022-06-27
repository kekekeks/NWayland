using System.Runtime.InteropServices;

namespace NWayland.Interop
{
    public struct WlFixed
    {
        private readonly int _value;

        public WlFixed(int value)
        {
            _value = value * 256;
        }

        public WlFixed(double value)
        {
            Union u = new() { d = value + (3L << (51 - 8)) };
            _value = (int)u.i;
        }

        public static explicit operator int(WlFixed wlFixed) => wlFixed._value / 256;

        public static explicit operator double(WlFixed wlFixed)
        {
            Union u = new() { i = ((1023L + 44L) << 52) + (1L << 51) + wlFixed._value };
            return u.d - (3L << 43);
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct Union
        {
            [FieldOffset(0)]
            public double d;

            [FieldOffset(0)]
            public long i;
        }
    }
}
