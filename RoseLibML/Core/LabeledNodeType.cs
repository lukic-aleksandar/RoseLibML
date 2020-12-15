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

        private string quasiUniqueRepresetnation = null;
        public string GetQuasiUniqueRepresentation()
        {
            if(quasiUniqueRepresetnation == null)
            {
                var hashed = GetMD5HashCode();
                quasiUniqueRepresetnation = $"{Convert.ToBase64String(hashed)}|{FullFragment.Length}|{Part1Fragment.Length}|{Part2Fragment.Length}";
            }

            return quasiUniqueRepresetnation;
        }

        private byte[] MD5HashCode = null;
        public byte[] GetMD5HashCode()
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
}
