﻿
using BEditor.Drawing.Pixel;

namespace BEditor.Drawing.Process
{
    public readonly unsafe struct SubtractProcess<T> : IPixelProcess
        where T : unmanaged, IPixel<T>
    {
        private readonly T* _dst;
        private readonly T* _src;
        private readonly T* _mask;

        public SubtractProcess(T* src, T* dst, T* mask)
        {
            _dst = dst;
            _src = src;
            _mask = mask;
        }

        public readonly void Invoke(int pos)
        {
            _dst[pos] = _src[pos].Subtract(_mask[pos]);
        }
    }
}
