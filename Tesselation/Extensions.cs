using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tesselation
{
    internal static class Extensions
    {
        public static int CountBits(int value)
        {
            int count = 0;
            while (value != 0)
            {
                count++;
                value &= value - 1;
            }
            return count;
        }
        public static List<T> Shuffle<T>(this List<T> list)
        {
            List<T> result = new List<T>();
            Random r = new Random();
            int firstcount = list.Count();
            for (int i = 0; i < firstcount; ++i)
            {
                result.Add(list[r.Next(0,list.Count)]);
            }
            return result;
        }
    }
}
