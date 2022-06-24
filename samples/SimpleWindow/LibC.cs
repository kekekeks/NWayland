using System;
using System.Runtime.InteropServices;

namespace SimpleWindow
{
    internal static class LibC
    {
        private const string C = "libc";

        [DllImport(C, SetLastError = true)]
        public static extern int close(int fd);

        [DllImport(C, SetLastError = true)]
        public static extern int read(int fd, IntPtr buffer, int count);

        [DllImport(C, SetLastError = true)]
        public static extern int write(int fd, IntPtr buffer, int count);

        [DllImport(C, SetLastError = true)]
        public static extern unsafe int pipe2(int* fds, FileDescriptorFlags flags);

        [DllImport(C, SetLastError = true)]
        public static extern IntPtr mmap(IntPtr addr, IntPtr length, MemoryProtection prot, SharingType flags, int fd, IntPtr offset);

        [DllImport(C, SetLastError = true)]
        public static extern int munmap(IntPtr addr, IntPtr length);

        [DllImport(C, SetLastError = true)]
        public static extern int memfd_create(string name, MemoryFileCreation flags);

        [DllImport(C, SetLastError = true)]
        public static extern int ftruncate(int fd, int size);

        [DllImport(C, SetLastError = true)]
        public static extern int fcntl(int fd, FileSealCommand cmd, FileSeals flags);
    }

    public enum Errno
    {
        EINTR = 4,
        EAGAIN = 11
    }

    [Flags]
    public enum MemoryProtection
    {
        PROT_NONE = 0,
        PROT_READ = 1,
        PROT_WRITE = 2,
        PROT_EXEC = 4
    }

    public enum SharingType
    {
        MAP_SHARED = 1,
        MAP_PRIVATE = 2
    }

    [Flags]
    public enum MemoryFileCreation : uint
    {
        MFD_CLOEXEC = 1,
        MFD_ALLOW_SEALING = 2,
        MFD_HUGETLB = 4
    }

    public enum FileSealCommand
    {
        F_ADD_SEALS = 1024 + 9,
        F_GET_SEALS = 1024 + 10
    }

    [Flags]
    public enum FileSeals
    {
        F_SEAL_SEAL = 1,
        F_SEAL_SHRINK = 2,
        F_SEAL_GROW = 4,
        F_SEAL_WRITE = 8,
        F_SEAL_FUTURE_WRITE = 16
    }
}
