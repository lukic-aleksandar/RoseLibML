using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class LabeledNodeType
    {
        public string FullFragment { get; set; }
        public string Part1Fragment { get; set; }
        public string Part2Fragment { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as LabeledNodeType;

            if (other != null)
            {
                return TypesAreEqual(this, other);
            }
            return false;
        }

        public string GetMD5HashCodeStr()
        {
            var hashed = GetMD5HashCode();
            return System.Convert.ToBase64String(hashed);
        }

        private object hashLock = new object();
        private byte[] MD5HashCode = null;
        public byte[] GetMD5HashCode()
        {
            lock (hashLock)
            {
                if (MD5HashCode != null)
                {
                    return MD5HashCode;
                }

                MD5 md5Hasher = MD5.Create();
                var typeRepresentation = FullFragment + Part1Fragment + Part2Fragment;
                MD5HashCode = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(typeRepresentation));
                return MD5HashCode;
            }
        }

        public static bool TypesAreEqual(LabeledNodeType firstType, LabeledNodeType secondType)
        {
            var first = firstType?.GetMD5HashCode();
            var second = secondType?.GetMD5HashCode();

            if (first == second)
            {
                return true;
            }
            if (first == null || second == null)
            {
                return false;
            }
            
            /*if (first.Length != second.Length)
            {
                return false;
            }
            for(int i = 0; i < first.Length; i++)
            {
                if(first[i] != second[i])
                {
                    return false;
                }
            }

            return true;
            */
            return memcmp(first, second, first.Length) == 0;
        }

        
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);
        

    }
}
