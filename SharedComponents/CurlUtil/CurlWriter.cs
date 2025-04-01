using System;
using System.Collections.Generic;

namespace SharedComponents.CurlUtil
{
    public class CurlWriter
    {
        public CurlWriter()
        {
            ByteArr = new List<Byte>().ToArray(); // init empty
        }

        public string CurrentPage { get; set; }

        public Byte[] ByteArr { get; set; }


        public Int32 WriteData(Byte[] buf, Int32 size, Int32 nmemb, Object extraData)
        {
            try
            {
                ByteArr = Combine(ByteArr, buf);
                foreach (var b in buf)
                    try
                    {
                        CurrentPage += (char) b;
                    }
                    catch (Exception)
                    {
                    }
                return buf.Length;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        public byte[] Combine(byte[] first, byte[] second)
        {
            var ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }
    }
}