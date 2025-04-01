using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedComponents.SharpLogLite.Model
{
    public static class Helper
    {
        public static string ByteArrayToStringNullTerminated(byte[] arr)
        {
            System.Text.UTF8Encoding enc = new System.Text.UTF8Encoding();
            string s = enc.GetString(arr.TakeWhile(b => !b.Equals(0)).ToArray());
            return s;
        }
    }
}
