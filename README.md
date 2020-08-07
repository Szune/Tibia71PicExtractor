# Tibia71PicExtractor
Reverse engineered as part of a bigger project to learn more about the Tibia 7.1 client, mostly for fun.

### .pic structure
- U32 Version
- U16 Sprite sheet count (SSC)
- SpriteSheet[SSC] sprite sheets

### Sprite sheet structure:
- U8 Width (W)
- U8 Height (H)
- U8 Transparent R
- U8 Transparent G
- U8 Transparent B
- U32[W * H] Byte offsets of the .pic file where the sprite sheet's parts start,
these are stored from left to right for every row, where a single sprite part is 32x32 pixels

### Reading a sprite sheet:
Every sprite sheet part is 32x32.

    Loop through the byte offsets (b)
        Seek (move the stream) to b, starting from the beginning
        Read U16 to get the byte length (len) of the sprite part definition
        Loop until you've read len bytes
            Read U16 to get the amount of transparent pixels until a pixel with color
            Read U16 to get the amount of pixels with color (col) before another transparent pixel
                Loop until col
                    Read U8 Red byte
                    Read U8 Green byte
                    Read U8 Blue byte
