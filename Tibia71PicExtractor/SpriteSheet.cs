using System.Collections.Generic;

namespace Tibia71PicExtractor
{
    public class SpriteSheet
    {
        public byte Width;
        public byte Height;
        public byte TransparentR;
        public byte TransparentG;
        public byte TransparentB;
        public ushort SpriteCount;
        public List<uint> SpriteBytePositions = new List<uint>();
    }
}