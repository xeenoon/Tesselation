using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tesselation
{
    public unsafe class Board
    {
        public byte* data;
        public int width;
        public int height;
        public int size;

        public Board(int width, int height)
        {
            this.width = width;
            this.height = height;
            size = (width * height) / 8 + ((width * height) % 8 != 0 ? 1 : 0);
            data = (byte*)Marshal.AllocHGlobal(size);
        }

        public bool GetData(int x, int y)
        {
            int idx = x + y * width;
            int byteidx = idx / 8;
            int bitoffset = idx % 8;
            return (data[byteidx] & ((byte)1 << bitoffset)) == (byte)1<<bitoffset;
        }
        public void SetBit(int x, int y)
        {
            int idx = x + y * width;
            int byteidx = idx / 8;
            int bitoffset = idx % 8;
            data[byteidx] = (byte)(data[byteidx] | (byte)((byte)1 << bitoffset));
        }
        public void ClearBit(int x, int y)
        {
            int idx = x + y * width;
            int byteidx = idx / 8;
            int bitoffset = idx % 8;
            data[byteidx] = (byte)(data[byteidx] & (byte)(byte.MaxValue ^ ((byte)1 << bitoffset)));
        }
    }
}
