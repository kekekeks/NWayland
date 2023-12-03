using System;

namespace NWayland.Interop
{
    public unsafe interface IBindFactory<out T>
    {
        WlInterface* GetInterface();

        T Create(IntPtr handle, int version);
    }
}
