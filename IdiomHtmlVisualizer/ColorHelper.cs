using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace IdiomHtmlVisualizer
{
    public class ColorHelper
    {
        private Dictionary<string, ushort> idiomColorNumbers = new Dictionary<string, ushort>();
        private ushort GetColorNumber(string idiomMark)
        {
            var colorNumber = idiomColorNumbers.GetValueOrDefault(idiomMark);
            if (colorNumber == 0)
            {   
                // Try to add twice, to lower the possibility of encountering duplicate keys
                colorNumber = (ushort)new Random().NextInt64();
                var added = idiomColorNumbers.TryAdd(idiomMark, colorNumber);

                if (!added)
                {
                    colorNumber = (ushort)new Random().NextInt64();
                    idiomColorNumbers.Add(idiomMark, colorNumber);
                }
            }

            return colorNumber;
        }

        private string ChooseColor(int number)
        {
            var hue = number * 137.508; // use golden angle approximation
            return $"hsl({hue},50%,75%)";
        }

        public string GetIdiomColor(string idiomMark)
        {
            return ChooseColor(GetColorNumber(idiomMark));
        }
    }
}
