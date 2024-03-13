using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tesselation
{
    public unsafe struct Board : IDisposable
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memcpy(IntPtr dest, IntPtr src, UIntPtr count);
        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr memset(IntPtr dest, byte c, int count);


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
            GC.SuppressFinalize(this);
            memset((nint)data, 0, size);
        }
        public void Dispose()
        {
            Marshal.FreeHGlobal((nint)data);
        }

        public bool GetData(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return false;
            }

            int idx = x + y * width;
            int byteidx = idx / 8;
            int bitoffset = idx % 8;

            // Ensure byteidx is within the bounds
            if (byteidx < 0 || byteidx >= size)
            {
                return false;
            }

            // Check the bit at the specified offset
            return (data[byteidx] & (1 << bitoffset)) != 0;
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
        public unsafe bool IsEqual(byte* ptr2, int length)
        {
            for (int i = 0; i < length; i++) //Inefficient, import memcpm from C
            {
                if (*(data + i) != *(ptr2 + i))
                {
                    return false;
                }
            }
            return true;
        }
        public unsafe bool IsEqual(Board board)
        {
            for (int i = 0; i < size; i++) //Inefficient, import memcpm from C
            {
                if (*(data + i) != *(board.data + i))
                    return false;
            }
            return true;
        }
        public string ToString()
        {
            byte[] tempArray = new byte[size];
            Marshal.Copy((IntPtr)data, tempArray, 0, size);
            return BitConverter.ToString(tempArray).Replace("-", "");
        }
    }
}
