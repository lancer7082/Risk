using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Finam.NativeConnect
{
    public class TableKey
    {
        public byte[] KeyData { get; private set; }

        internal TableKey(byte[] keyData)
        {
            KeyData = keyData;
        }

        private static unsafe bool UnsafeCompare(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;
            fixed (byte* p1 = a1, p2 = a2)
            {
                byte* x1 = p1, x2 = p2;
                int l = a1.Length;
                for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                    if (*((long*)x1) != *((long*)x2)) return false;
                if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
                if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
                if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
                return true;
            }
        }

        /*
        public override int GetHashCode()
        {
            int result = 0;
            for (int i = 0; i < (KeyData.Length > 4 ? 4 : KeyData.Length); i++)
                result = (result << 8) - Byte.MinValue + (int)KeyData[i];
            return result;
        }
         * */

        //public override bool Equals(object obj)
        //{
        //    if (obj is TableKey)
        //        return UnsafeCompare(KeyData, ((TableKey)obj).KeyData);
        //    else
        //        return false;
        //}
    }
}
