using System;
using NWayland.Protocols.Wayland;
using NWayland.Protocols.XdgShell;
using OpenGL;
using OpenGL.Egl;
using OpenGL.Surfaces;


namespace EglWindow
{
    public class WlEglWindow :  EglGlPlatformSurfaceBase.IEglWindowGlPlatformSurfaceInfo, IDisposable, WlCallback.IEvents, XdgWmBase.IEvents, XdgSurface.IEvents, XdgToplevel.IEvents
    {
        private readonly WlSurface _surface;
        private readonly EglPlatformOpenGlInterface _egl;
        private readonly IGlContext _glContext;
        private readonly EglGlPlatformSurface _glSurface;
        private readonly IGlPlatformSurfaceRenderTarget _renderTarget;

        public WlEglWindow(WlDisplay display, WlSurface surface)
        {
            _surface = surface;
            _egl = EglPlatformOpenGlInterface.TryCreate(() => new EglDisplay(new EglInterface(), false, 0x31D8, display.Handle, null));
            Handle = LibWaylandEgl.wl_egl_window_create(surface.Handle, 400, 600);
            _glSurface = new EglGlPlatformSurface(_egl, this);
            _renderTarget = _glSurface.CreateGlRenderTarget();
        }

        public bool Closed { get; private set; }

        public IntPtr Handle { get; }

        public PixelSize Size { get; private set; }

        public double Scaling => 1;

        public void OnDone(WlCallback eventSender, uint callbackData)
        {
            eventSender.Dispose();
            Draw();
        }

        public void OnPing(XdgWmBase eventSender, uint serial) => eventSender.Pong(serial);

        public void OnConfigure(XdgSurface eventSender, uint serial) => eventSender.AckConfigure(serial);

        public void OnConfigure(XdgToplevel eventSender, int width, int height, ReadOnlySpan<XdgToplevel.StateEnum> states)
        {
            if (width == 0 || height == 0)
                return;

            Size = new PixelSize(width, height);
            LibWaylandEgl.wl_egl_window_resize(Handle, width, height, 0, 0);
        }

        public void OnClose(XdgToplevel eventSender)
        {
            Closed = true;
        }

        public void OnConfigureBounds(XdgToplevel eventSender, int width, int height) { }

        public void Draw()
        {
            _surface.Frame().Events = this;
            using var session = _renderTarget.BeginDraw();
            session.Context.GlInterface.ClearColor(0.5f, 0f, 0, 1);
            session.Context.GlInterface.Clear(GlConsts.GL_COLOR_BUFFER_BIT);
        }

        public void Dispose()
        {
            LibWaylandEgl.wl_egl_window_destroy(Handle);
        }
    }
}
