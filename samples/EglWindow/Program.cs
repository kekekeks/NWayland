using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgDecorationUnstableV1;
using NWayland.Protocols.XdgShell;


namespace EglWindow
{
    public static class Program
    {
        public static void Main()
        {
            WlDisplay display = WlDisplay.Connect();
            WlRegistry registry = display.GetRegistry();
            WlRegistryHandler registryHandler = new(registry);
            registry.Events = registryHandler;
            display.Dispatch();
            display.Roundtrip();

            WlCompositor compositor = registryHandler.BindRequiredInterface(WlCompositor.BindFactory, WlCompositor.InterfaceName, WlCompositor.InterfaceVersion);
            XdgWmBase wmBase = registryHandler.BindRequiredInterface(XdgWmBase.BindFactory, XdgWmBase.InterfaceName, XdgWmBase.InterfaceVersion);
            ZxdgDecorationManagerV1 decorationManager = registryHandler.BindRequiredInterface(ZxdgDecorationManagerV1.BindFactory, ZxdgDecorationManagerV1.InterfaceName, ZxdgDecorationManagerV1.InterfaceVersion);
            WlSurface surface = compositor.CreateSurface();
            XdgSurface xdgSurface = wmBase.GetXdgSurface(surface);
            XdgToplevel toplevel = xdgSurface.GetToplevel();
            ZxdgToplevelDecorationV1 decoration = decorationManager.GetToplevelDecoration(toplevel);

            WlEglWindow window = new(display, surface);
            wmBase.Events = window;
            xdgSurface.Events = window;
            toplevel.Events = window;

            toplevel.SetTitle("Egl Window");
            decoration.SetMode(ZxdgToplevelDecorationV1.ModeEnum.ServerSide);
            window.Draw();

            while (!window.Closed && display.Dispatch() >= 0) { }
        }
    }
}
