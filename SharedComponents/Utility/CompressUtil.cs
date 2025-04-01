using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.Utility
{
    public static class CompressUtil
    {
        public static string Compress(string inputText)
        {
            return Compress(System.Text.Encoding.UTF8.GetBytes(inputText));
        }

        public static string Compress(byte[] inputBytes)
        {
            byte[] compressed;

            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zip = new GZipStream(ms, CompressionMode.Compress))
                {
                    zip.Write(inputBytes, 0, inputBytes.Length);
                    zip.Close();

                    compressed = ms.ToArray();
                }
            }

            return Convert.ToBase64String(compressed);
        }

        public static string DecompressText(string compressedText)
        {
            return System.Text.Encoding.UTF8.GetString(DecompressBytes(compressedText));
        }

        public static byte[] DecompressBytes(string compressedText)
        {
            byte[] bytes = Convert.FromBase64String(compressedText);
            byte[] outputBytes;

            using (MemoryStream inputStream = new MemoryStream(bytes))
            {
                using (GZipStream zip = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        zip.CopyTo(outputStream);

                        outputBytes = outputStream.ToArray();
                    }
                }
            }
            return outputBytes;
        }
    }
}
