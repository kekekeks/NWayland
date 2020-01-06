using System;
using NWayland.Protocols.Wayland;

namespace NWayland.Core
{
    public unsafe interface IBindFactory<T>
    {
        WlInterface* GetInterface();
        T Create(IntPtr handle, WlDisplay display);
    }
}