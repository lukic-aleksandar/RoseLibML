using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace IdiomHtmlVisualizer
{
    public class NodeHasher
    {
        public static uint CalculateNodeHash(string sTInfo, int roslynSpanStart, int roslynSpanEnd)
        {
            byte[] encoded = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes($"{sTInfo}|{roslynSpanStart}|{roslynSpanEnd}"));
            var nodeHashValue = BitConverter.ToUInt32(encoded, 0);
            return nodeHashValue;
        }
    }
}
