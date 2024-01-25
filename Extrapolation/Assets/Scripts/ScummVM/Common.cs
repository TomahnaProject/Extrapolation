using System;
using UnityEngine;

namespace Myst3
{
    public class Common
    {
        public static uint MKTAG(char a, char b, char c, char d)
        {
            return (uint)((d) | ((c) << 8) | ((b) << 16) | ((a) << 24));
        }
        public static Color int2col(uint col)
        {
            int col2 = (int)col;
            return new Color(
                (col2 & 0xff000000) >> 24,
                (col2 & 0xff0000) >> 16,
                (col2 & 0xff00) >> 8,
                (col2 & 0xff)
            );
        }
    }
}
