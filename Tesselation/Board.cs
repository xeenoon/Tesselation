using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tesselation
{
    internal class Board
    {
        public byte[] data;
        public int width;
        public int height;
        public bool GetData(int x, int y)
        {
            int idx = x + y * width;
            int byteidx = idx / 8;
            int bitoffset = idx % 8;
            return (data[byteidx] & ((byte)1 << bitoffset)) == (byte)1<<bitoffset;
        }
    }
}
