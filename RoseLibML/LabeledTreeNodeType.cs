using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class LabeledTreeNodeType
    {
        public string FullFragment { get; set; }
        public string Part1Fragment { get; set; }
        public string Part2Fragment { get; set; }

        public override bool Equals(object obj)
        {
            var other = obj as LabeledTreeNodeType;
            
            if(other != null)
            {
                if(FullFragment == other.FullFragment
                   && Part1Fragment == other.Part1Fragment
                   && Part2Fragment == other.Part2Fragment)
                {
                    return true;
                }
            }

            return false;
        }

        public override int GetHashCode()
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(FullFragment + Part1Fragment + Part2Fragment));
            return BitConverter.ToInt32(hashed, 0);
        }

        public string GetMD5HashCode()
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(FullFragment + Part1Fragment + Part2Fragment));
            return System.Convert.ToBase64String(hashed);
        }
    }
}
