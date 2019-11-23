using System.IO;
using Microsoft.VisualStudio.OLE.Interop;

namespace TaskWorkspace.Helpers
{
    internal static class ServiceExtensions
    {
        public static void Rewind(this IStream stream)
        {
            var dlibMove = new LARGE_INTEGER();
            var plibNewPosition = new ULARGE_INTEGER[1]
            {
                new ULARGE_INTEGER()
            };
            dlibMove.QuadPart = 0L;
            stream.Seek(dlibMove, 0U, plibNewPosition);
        }

        public static byte[] ToByteArray(this IStream stream)
        {
            var numArray = new byte[20480];
            using (var memoryStream = new MemoryStream())
            {
                uint pcbRead;
                do
                {
                    stream.Read(numArray, (uint) numArray.Length, out pcbRead);
                    if (0U < pcbRead)
                        memoryStream.Write(numArray, 0, (int) pcbRead);
                } while (20480U == pcbRead);

                return memoryStream.ToArray();
            }
        }
    }
}