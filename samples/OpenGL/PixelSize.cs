using System;

namespace OpenGL
{
    public struct PixelSize : IEquatable<PixelSize>
    {
        public int Width;
        public int Height;

        public PixelSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public bool Equals(PixelSize other)
        {
            return Width == other.Width && Height == other.Height;
        }

        public override bool Equals(object? obj)
        {
            return obj is PixelSize other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }

        public static bool operator ==(PixelSize left, PixelSize right) => left.Equals(right);

        public static bool operator !=(PixelSize left, PixelSize right) => !left.Equals(right);
    }
}
