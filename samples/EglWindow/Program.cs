using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgDecorationUnstableV1;
using NWayland.Protocols.XdgShell;

namespace EglWindow
{
    public static class Program
    {
        public static void Main()
        {
            var display = WlDisplay.Connect();
            var registry = display.GetRegistry();
            var registryHandler = new WlRegistryHandler(registry);
            registry.Events = registryHandler;
            display.Dispatch();
            display.Roundtrip();

            var compositor = registryHandler.BindRequiredInterface(WlCompositor.BindFactory, WlCompositor.InterfaceName, WlCompositor.InterfaceVersion);
            var wmBase = registryHandler.BindRequiredInterface(XdgWmBase.BindFactory, XdgWmBase.InterfaceName, XdgWmBase.InterfaceVersion);
            var decorationManager = registryHandler.BindRequiredInterface(ZxdgDecorationManagerV1.BindFactory, ZxdgDecorationManagerV1.InterfaceName, ZxdgDecorationManagerV1.InterfaceVersion);
            var surface = compositor.CreateSurface();
            var xdgSurface = wmBase.GetXdgSurface(surface);
            var toplevel = xdgSurface.GetToplevel();
            var decoration = decorationManager.GetToplevelDecoration(toplevel);

            var window = new WlEglWindow(display, surface);
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
