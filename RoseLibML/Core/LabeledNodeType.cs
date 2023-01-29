using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RoseLibML
{
    public class LabeledNodeType : IEquatable<LabeledNodeType>
    {
        private static MD5 md5Hasher = MD5.Create();
        
        private int fullFragmentLength = 0;
        private string fullFragmentHash = null;
        public string FullFragment { 
            get { return fullFragmentHash; } 
            set
            {
                fullFragmentLength = value.Length;
                fullFragmentHash = CalculateFragmentHash(value);
            }
        }

        private int part1FragmentLength = 0;
        private string part1FragmentHash = null;
        public string Part1Fragment 
        {
            get { return part1FragmentHash; }
            set
            {
                part1FragmentLength = value.Length;
                part1FragmentHash = CalculateFragmentHash(value);
            }
        }
        
        private int part2FragmentLength = 0;
        private string part2FragmentHash = null;
        public string Part2Fragment 
        {
            get { return part2FragmentHash; }
            set
            {
                part2FragmentLength = value.Length;
                part2FragmentHash = CalculateFragmentHash(value);
            }
        }

        
        
        private string typeHash = null;
        public string GetTypeHash()
        {
            if(typeHash == null)
            {
                var hash = GetMD5HashAsString(FullFragment + Part1Fragment + Part2Fragment);
                typeHash = $"{hash}|{fullFragmentLength}|{part1FragmentLength}|{part2FragmentLength}";
            }

            return typeHash;
        }

        
        public static string GetMD5HashAsString(string content)
        {
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashed);
        }

        public static string CalculateFragmentHash(string fragment)
        {
            var fragmentLength = fragment.Length;
            var initialSubstring = fragment.Substring(0, Math.Min(50, fragment.Length));
            var hash = GetMD5HashAsString(fragment);

            return $"{hash}|{fragmentLength}|{initialSubstring}";
        }

        public bool Equals(LabeledNodeType? other)
        {
            if(other == null)
            {
                return false;
            }
            
            if (GetTypeHash().Equals(other.GetTypeHash()))
            {
                return true;
            }
            
            return false;
        }

        public override bool Equals(object? other)
        {
            if (other == null)
            {
                return false;
            }
            
            var objectAsLNT = other as LabeledNodeType;
            if(objectAsLNT == null)
            {
                return false;
            }

            if (GetTypeHash().Equals(objectAsLNT.GetTypeHash()))
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (fullFragmentHash, part1FragmentHash, part2FragmentHash).GetHashCode();
        }
    }
}
