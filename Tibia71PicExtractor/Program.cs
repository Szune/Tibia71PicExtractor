using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Tibia71PicExtractor
{
    class Program
    {

        /*
         * Read the file using a BinaryReader
         * 
         * .pic Structure:
         * U32 Version
         * U16 Sprite sheet count (SSC)
         * SpriteSheet[SSC] sprite sheets
         * 
         * Sprite sheet structure:
         * U8 Width (W)
         * U8 Height (H)
         * U8 Transparent R
         * U8 Transparent G
         * U8 Transparent B
         * U32[W*H] Byte offsets of the .pic file where the sprite sheet's parts start,
         * these are stored from left to right for every row, where a single sprite part is 32x32 pixels
         *
         * Reading a sprite sheet:
         * Loop through the byte offsets (b)
         *     Seek (move the stream) to b, starting from the beginning
         *     Read U16 to get the byte length (len) of the sprite part definition
         *     Loop until you've read len bytes
         *         Read U16 to get the amount of transparent pixels until a pixel with color
         *         Read U16 to get the amount of pixels with color (col) before another transparent pixel
         *         Loop until col
         *             Read U8 Red byte
         *             Read U8 Green byte
         *             Read U8 Blue byte
         */
        
        static void Main(string[] args)
        {
            // read Tibia.pic from the same folder as this application (if run from the same folder etc etc, I know)
            using var br = new BinaryReader(File.OpenRead("Tibia.pic"));
            var version = br.ReadUInt32();
            var spriteSheetCount = br.ReadUInt16();
            Console.WriteLine($"Version: {version}\nSprite sheets: {spriteSheetCount}");
            var picObjs = new List<SpriteSheet>();
            for (int i = 0; i < spriteSheetCount; i++)
            {
                var tmpSpriteSheet = new SpriteSheet();
                tmpSpriteSheet.Width = br.ReadByte();
                tmpSpriteSheet.Height = br.ReadByte();
                tmpSpriteSheet.TransparentR = br.ReadByte();
                tmpSpriteSheet.TransparentG = br.ReadByte();
                tmpSpriteSheet.TransparentB = br.ReadByte();
                tmpSpriteSheet.SpriteCount = (ushort) (tmpSpriteSheet.Width * tmpSpriteSheet.Height);
                for (int p = 0; p < tmpSpriteSheet.SpriteCount; p++)
                {
                    tmpSpriteSheet.SpriteBytePositions.Add(br.ReadUInt32());
                }

                picObjs.Add(tmpSpriteSheet);
            }


            try
            {
                Directory.CreateDirectory("Sprites");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create 'Sprites' folder: {ex}");
                return;
            }
            
            const int spriteWidth = 32;
            const int spriteHeight = 32;

            for (int a = 0; a < picObjs.Count; a++)
            {
                var fullWidth = picObjs[a].Width * spriteWidth;
                var fullHeight = picObjs[a].Height * spriteHeight;

                // the following horrific code is used for performance reasons:
                // only allocate 1 bitmap per sprite sheet
                // only lock pixel bits once per sprite sheet
                
                // another way to do it would be to allocate (picObj.Width * picObj.Height) bitmaps
                // which would require locking pixel bits as many times
                // and then put them together afterwards
                // but it would be less performant
                
                // one way _not_ to do it would be to use Bitmap.SetPixel, it has very poor performance
                // because it has to lock the pixels once per pixel color assigned
                using (var sprite = new Bitmap(fullWidth, fullHeight))
                {
                    unsafe // need for speed
                    {
                        // lock bits
                        var sprData = sprite.LockBits(new Rectangle(0, 0, fullWidth, fullHeight),
                            ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                        // loop through all the parts of the sprite sheet
                        for (int b = 0; b < picObjs[a].SpriteBytePositions.Count; b++)
                        {
                            // go to the byte position in the file where the part starts
                            br.BaseStream.Seek(picObjs[a].SpriteBytePositions[b], SeekOrigin.Begin);
                            // calculate offsets (doesn't have to be done this way, it's just how I prefer to do it)
                            var boxX = (b % picObjs[a].Width) * spriteWidth;
                            var boxY = (b / picObjs[a].Width) * spriteHeight;
                            var boxXOffset = boxX * 4;
                            var boxYOffset = boxY * fullWidth * 4;
                            // go to the scanline of the sprite part where we are going to start assigning pixels
                            var bmpPtr = (byte*) sprData.Scan0 + boxXOffset + boxYOffset;
                            var endOfPic = br.BaseStream.Position + br.ReadUInt16();
                            var cPixel = 0;
                            while (br.BaseStream.Position < endOfPic)
                            {
                                var transparentPixels = br.ReadUInt16();
                                var colorfulPixels = br.ReadUInt16();
                                cPixel += transparentPixels;
                                bmpPtr += transparentPixels * 4;
                                // instead of just skipping the transparent pixels, we could use the TransparentR/G/B
                                // and actually write the pixels out in the format the client expects
                                // that way, we could edit the .pic file
                                for (var p = 0; p < colorfulPixels; p++)
                                {
                                    var x = cPixel % spriteWidth;
                                    var y = cPixel / spriteWidth;
                                    var xOffset = x * 4;
                                    var yOffset = y * fullWidth * 4;
                                    // go to the specific scanline and offset into it for the current pixel
                                    bmpPtr = (byte*) sprData.Scan0 + boxXOffset + boxYOffset + xOffset + yOffset;
                                    cPixel += 1;

                                    var (red, green, blue) = (br.ReadByte(), br.ReadByte(), br.ReadByte());
                                    // yes, they really are stored BGRA and not ARGB, which can be confusing
                                    bmpPtr[0] = blue; // blue
                                    bmpPtr[1] = green; // green
                                    bmpPtr[2] = red; // red
                                    bmpPtr[3] = 255; // alpha
                                    bmpPtr += 4;
                                }
                            }
                        }

                        // unlock the bits, save the bitmap and then dispose it
                        sprite.UnlockBits(sprData);
                        sprite.Save($"Sprites/{a + 1}.bmp");
                        Console.WriteLine($"Saved Sprites/{a + 1}.bmp");
                    }
                }
            }

            Console.WriteLine("All done, files have been extracted to the 'Sprites' folder.");
            Console.ReadLine();
        }
    }
}