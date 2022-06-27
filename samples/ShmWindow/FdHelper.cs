using System.Runtime.InteropServices;

namespace ShmWindow
{
    internal static class FdHelper
    {
        public static int CreateAnonymousFile(int size)
        {
            var fd = LibC.memfd_create("wayland-shm", MemoryFileCreation.MFD_CLOEXEC | MemoryFileCreation.MFD_ALLOW_SEALING);
            if (fd == -1)
                return -1;
            LibC.fcntl(fd, FileSealCommand.F_ADD_SEALS, FileSeals.F_SEAL_SHRINK);
            return ResizeFd(fd, size);
        }

        public static int ResizeFd(int fd, int size)
        {
            int ret;
            do
                ret = LibC.ftruncate(fd, size);
            while (ret < 0 && Marshal.GetLastWin32Error() == (int)Errno.EINTR);
            if (ret >= 0)
                return fd;
            LibC.close(fd);
            return -1;
        }
    }
}
