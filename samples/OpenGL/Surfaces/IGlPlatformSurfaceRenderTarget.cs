using System;

namespace OpenGL.Surfaces
{
    public interface IGlPlatformSurfaceRenderTarget : IDisposable
    {
        IGlPlatformSurfaceRenderingSession BeginDraw();
    }

    public interface IGlPlatformSurfaceRenderTargetWithCorruptionInfo : IGlPlatformSurfaceRenderTarget
    {
        bool IsCorrupted { get; }
    }
}
