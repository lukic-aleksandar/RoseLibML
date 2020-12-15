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

        private string uniqueRepresetnation = null;
        public string GetUniqueRepresentation()
        {
            if(uniqueRepresetnation == null)
            {
                uniqueRepresetnation = $"{Part1Fragment}|{Part2Fragment}";
            }

            return uniqueRepresetnation;
        }

        public static bool TypesAreEqual(LabeledNodeType firstType, LabeledNodeType secondType)
        {
            if (firstType == secondType)
            {
                return true;
            }
            if (firstType == null || secondType == null)
            {
                return false;
            }

            if (firstType.FullFragment.Length == secondType.FullFragment.Length &&
                firstType.Part1Fragment.Length == secondType.Part1Fragment.Length &&
                firstType.Part2Fragment.Length == secondType.Part2Fragment.Length)
            {
                var utf8 = new UTF8Encoding();
                byte[] fullFragment1Bytes = utf8.GetBytes(firstType.FullFragment);
                byte[] fullFragment2Bytes = utf8.GetBytes(secondType.FullFragment);

                return memcmp(fullFragment1Bytes, fullFragment2Bytes, fullFragment1Bytes.Length) == 0;
            }

            return false;
        }

        
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);
        

    }
}
