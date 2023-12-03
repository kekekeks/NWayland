using System;

namespace NWayland.Interop
{
    public class NWaylandException : Exception
    {
        public NWaylandException(string message) : base(message) { }
    }
}
