using GarboDev.CrossCutting;

namespace GarboDev.Graphics
{
    partial class Renderer
    {
        #region Sprite Drawing
        private void DrawSpritesNormal(int priority)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            var blendMaskType = (byte)(1 << 4);

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

                var semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((_dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (_curLine >= ((y + rheight) & 0xff) && !(y < _curLine)) continue;
                }
                else
                {
                    if (_curLine < y || _curLine >= ((y + rheight) & 0xff)) continue;
                }

                var scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                var spritey = _curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesBlend(int priority)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            var blendMaskType = (byte)(1 << 4);

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

                var semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((_dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (_curLine >= ((y + rheight) & 0xff) && !(y < _curLine)) continue;
                }
                else
                {
                    if (_curLine < y || _curLine >= ((y + rheight) & 0xff)) continue;
                }

                var scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                var spritey = _curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesBrightInc(int priority)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            var blendMaskType = (byte)(1 << 4);

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

                var semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((_dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (_curLine >= ((y + rheight) & 0xff) && !(y < _curLine)) continue;
                }
                else
                {
                    if (_curLine < y || _curLine >= ((y + rheight) & 0xff)) continue;
                }

                var scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                var spritey = _curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        var r = pixelColor & 0xFF;
                                        var g = (pixelColor >> 8) & 0xFF;
                                        var b = (pixelColor >> 16) & 0xFF;
                                        r = r + (((0xFF - r) * _blendY) >> 4);
                                        g = g + (((0xFF - g) * _blendY) >> 4);
                                        b = b + (((0xFF - b) * _blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        var r = pixelColor & 0xFF;
                                        var g = (pixelColor >> 8) & 0xFF;
                                        var b = (pixelColor >> 16) & 0xFF;
                                        r = r + (((0xFF - r) * _blendY) >> 4);
                                        g = g + (((0xFF - g) * _blendY) >> 4);
                                        b = b + (((0xFF - b) * _blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        var r = pixelColor & 0xFF;
                                        var g = (pixelColor >> 8) & 0xFF;
                                        var b = (pixelColor >> 16) & 0xFF;
                                        r = r + (((0xFF - r) * _blendY) >> 4);
                                        g = g + (((0xFF - g) * _blendY) >> 4);
                                        b = b + (((0xFF - b) * _blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        var r = pixelColor & 0xFF;
                                        var g = (pixelColor >> 8) & 0xFF;
                                        var b = (pixelColor >> 16) & 0xFF;
                                        r = r + (((0xFF - r) * _blendY) >> 4);
                                        g = g + (((0xFF - g) * _blendY) >> 4);
                                        b = b + (((0xFF - b) * _blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesBrightDec(int priority)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            var blendMaskType = (byte)(1 << 4);

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

                var semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((_dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (_curLine >= ((y + rheight) & 0xff) && !(y < _curLine)) continue;
                }
                else
                {
                    if (_curLine < y || _curLine >= ((y + rheight) & 0xff)) continue;
                }

                var scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                var spritey = _curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                        {
                                            var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                            var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                            var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                            var sourceValue = _scanline[(i & 0x1ff)];
                                            r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                            g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                            b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                            if (r > 0xff) r = 0xff;
                                            if (g > 0xff) g = 0xff;
                                            if (b > 0xff) b = 0xff;
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        var r = pixelColor & 0xFF;
                                        var g = (pixelColor >> 8) & 0xFF;
                                        var b = (pixelColor >> 16) & 0xFF;
                                        r = r - ((r * _blendY) >> 4);
                                        g = g - ((g * _blendY) >> 4);
                                        b = b - ((b * _blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && true)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        var r = pixelColor & 0xFF;
                                        var g = (pixelColor >> 8) & 0xFF;
                                        var b = (pixelColor >> 16) & 0xFF;
                                        r = r - ((r * _blendY) >> 4);
                                        g = g - ((g * _blendY) >> 4);
                                        b = b - ((b * _blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        var r = pixelColor & 0xFF;
                                        var g = (pixelColor >> 8) & 0xFF;
                                        var b = (pixelColor >> 16) & 0xFF;
                                        r = r - ((r * _blendY) >> 4);
                                        g = g - ((g * _blendY) >> 4);
                                        b = b - ((b * _blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && true)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        var r = pixelColor & 0xFF;
                                        var g = (pixelColor >> 8) & 0xFF;
                                        var b = (pixelColor >> 16) & 0xFF;
                                        r = r - ((r * _blendY) >> 4);
                                        g = g - ((g * _blendY) >> 4);
                                        b = b - ((b * _blendY) >> 4);
                                        pixelColor = r | (g << 8) | (b << 16);
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesWindow(int priority)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            var blendMaskType = (byte)(1 << 4);

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

                var semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((_dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (_curLine >= ((y + rheight) & 0xff) && !(y < _curLine)) continue;
                }
                else
                {
                    if (_curLine < y || _curLine >= ((y + rheight) & 0xff)) continue;
                }

                var scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                var spritey = _curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesWindowBlend(int priority)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            var blendMaskType = (byte)(1 << 4);

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

                var semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((_dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (_curLine >= ((y + rheight) & 0xff) && !(y < _curLine)) continue;
                }
                else
                {
                    if (_curLine < y || _curLine >= ((y + rheight) & 0xff)) continue;
                }

                var scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                var spritey = _curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesWindowBrightInc(int priority)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            var blendMaskType = (byte)(1 << 4);

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

                var semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((_dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (_curLine >= ((y + rheight) & 0xff) && !(y < _curLine)) continue;
                }
                else
                {
                    if (_curLine < y || _curLine >= ((y + rheight) & 0xff)) continue;
                }

                var scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                var spritey = _curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            var r = pixelColor & 0xFF;
                                            var g = (pixelColor >> 8) & 0xFF;
                                            var b = (pixelColor >> 16) & 0xFF;
                                            r = r + (((0xFF - r) * _blendY) >> 4);
                                            g = g + (((0xFF - g) * _blendY) >> 4);
                                            b = b + (((0xFF - b) * _blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            var r = pixelColor & 0xFF;
                                            var g = (pixelColor >> 8) & 0xFF;
                                            var b = (pixelColor >> 16) & 0xFF;
                                            r = r + (((0xFF - r) * _blendY) >> 4);
                                            g = g + (((0xFF - g) * _blendY) >> 4);
                                            b = b + (((0xFF - b) * _blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            var r = pixelColor & 0xFF;
                                            var g = (pixelColor >> 8) & 0xFF;
                                            var b = (pixelColor >> 16) & 0xFF;
                                            r = r + (((0xFF - r) * _blendY) >> 4);
                                            g = g + (((0xFF - g) * _blendY) >> 4);
                                            b = b + (((0xFF - b) * _blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            var r = pixelColor & 0xFF;
                                            var g = (pixelColor >> 8) & 0xFF;
                                            var b = (pixelColor >> 16) & 0xFF;
                                            r = r + (((0xFF - r) * _blendY) >> 4);
                                            g = g + (((0xFF - g) * _blendY) >> 4);
                                            b = b + (((0xFF - b) * _blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        private void DrawSpritesWindowBrightDec(int priority)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            var blendMaskType = (byte)(1 << 4);

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                if (((attr2 >> 10) & 3) != priority) continue;

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

                var semiTransparent = false;

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        semiTransparent = true;
                        break;
                    case 2:
                        // Obj window
                        continue;
                    case 3:
                        continue;
                }

                if ((_dispCnt & 0x7) >= 3 && (attr2 & 0x3FF) < 0x200) continue;

                int width = -1, height = -1;
                switch ((attr0 >> 14) & 3)
                {
                    case 0:
                        // Square
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 8; break;
                            case 1: width = 16; height = 16; break;
                            case 2: width = 32; height = 32; break;
                            case 3: width = 64; height = 64; break;
                        }
                        break;
                    case 1:
                        // Horizontal Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 16; height = 8; break;
                            case 1: width = 32; height = 8; break;
                            case 2: width = 32; height = 16; break;
                            case 3: width = 64; height = 32; break;
                        }
                        break;
                    case 2:
                        // Vertical Rectangle
                        switch ((attr1 >> 14) & 3)
                        {
                            case 0: width = 8; height = 16; break;
                            case 1: width = 8; height = 32; break;
                            case 2: width = 16; height = 32; break;
                            case 3: width = 32; height = 64; break;
                        }
                        break;
                }

                // Check double size flag here

                int rwidth = width, rheight = height;
                if ((attr0 & (1 << 8)) != 0)
                {
                    // Rot-scale on
                    if ((attr0 & (1 << 9)) != 0)
                    {
                        rwidth *= 2;
                        rheight *= 2;
                    }
                }
                else
                {
                    // Invalid sprite
                    if ((attr0 & (1 << 9)) != 0)
                        width = -1;
                }

                if (width == -1)
                {
                    // Invalid sprite
                    continue;
                }

                // Y clipping
                if (y > ((y + rheight) & 0xff))
                {
                    if (_curLine >= ((y + rheight) & 0xff) && !(y < _curLine)) continue;
                }
                else
                {
                    if (_curLine < y || _curLine >= ((y + rheight) & 0xff)) continue;
                }

                var scale = 1;
                if ((attr0 & (1 << 13)) != 0) scale = 2;

                var spritey = _curLine - y;
                if (spritey < 0) spritey += 256;

                if (semiTransparent)
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            if ((_blend[i & 0x1ff] & _blendTarget) != 0 && _blend[i & 0x1ff] != blendMaskType)
                                            {
                                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                                var sourceValue = _scanline[(i & 0x1ff)];
                                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                                if (r > 0xff) r = 0xff;
                                                if (g > 0xff) g = 0xff;
                                                if (b > 0xff) b = 0xff;
                                                pixelColor = r | (g << 8) | (b << 16);
                                            }
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
                else
                {
                    if ((attr0 & (1 << 8)) == 0)
                    {
                        if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                        int baseSprite;
                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * (width / 8)) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            baseSprite = (attr2 & 0x3FF) + ((spritey / 8) * 0x20);
                        }

                        var baseInc = scale;
                        if ((attr1 & (1 << 12)) != 0)
                        {
                            baseSprite += ((width / 8) * scale) - scale;
                            baseInc = -baseInc;
                        }

                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            var r = pixelColor & 0xFF;
                                            var g = (pixelColor >> 8) & 0xFF;
                                            var b = (pixelColor >> 16) & 0xFF;
                                            r = r - ((r * _blendY) >> 4);
                                            g = g - ((g * _blendY) >> 4);
                                            b = b - ((b * _blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + width; i++)
                            {
                                if ((i & 0x1ff) < 240 && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var tx = (i - x) & 7;
                                    if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                    var curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            var r = pixelColor & 0xFF;
                                            var g = (pixelColor >> 8) & 0xFF;
                                            var b = (pixelColor >> 16) & 0xFF;
                                            r = r - ((r * _blendY) >> 4);
                                            g = g - ((g * _blendY) >> 4);
                                            b = b - ((b * _blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }
                                if (((i - x) & 7) == 7) baseSprite += baseInc;
                            }
                        }
                    }
                    else
                    {
                        var rotScaleParam = (attr1 >> 9) & 0x1F;

                        var dx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                        var dmx = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                        var dy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                        var dmy = (short)_memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                        var cx = rwidth / 2;
                        var cy = rheight / 2;

                        var baseSprite = attr2 & 0x3FF;
                        int pitch;

                        if ((_dispCnt & (1 << 6)) != 0)
                        {
                            // 1 dimensional
                            pitch = (width / 8) * scale;
                        }
                        else
                        {
                            // 2 dimensional
                            pitch = 0x20;
                        }

                        var rx = (dmx * (spritey - cy)) - (cx * dx) + (width << 7);
                        var ry = (dmy * (spritey - cy)) - (cx * dy) + (height << 7);

                        // Draw a rot/scale sprite
                        if ((attr0 & (1 << 13)) != 0)
                        {
                            // 256 colors
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                    int lookup = vram[0x10000 + curIdx];
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            var r = pixelColor & 0xFF;
                                            var g = (pixelColor >> 8) & 0xFF;
                                            var b = (pixelColor >> 16) & 0xFF;
                                            r = r - ((r * _blendY) >> 4);
                                            g = g - ((g * _blendY) >> 4);
                                            b = b - ((b * _blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                        else
                        {
                            // 16 colors
                            var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                            for (var i = x; i < x + rwidth; i++)
                            {
                                var tx = rx >> 8;
                                var ty = ry >> 8;

                                if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                    && (_windowCover[i & 0x1ff] & (1 << 4)) != 0)
                                {
                                    var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
                                    int lookup = vram[0x10000 + curIdx];
                                    if ((tx & 1) == 0)
                                    {
                                        lookup &= 0xf;
                                    }
                                    else
                                    {
                                        lookup >>= 4;
                                    }
                                    if (lookup != 0)
                                    {
                                        var pixelColor = GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                        if ((_windowCover[i & 0x1ff] & (1 << 5)) != 0)
                                        {
                                            var r = pixelColor & 0xFF;
                                            var g = (pixelColor >> 8) & 0xFF;
                                            var b = (pixelColor >> 16) & 0xFF;
                                            r = r - ((r * _blendY) >> 4);
                                            g = g - ((g * _blendY) >> 4);
                                            b = b - ((b * _blendY) >> 4);
                                            pixelColor = r | (g << 8) | (b << 16);
                                        }
                                        _scanline[(i & 0x1ff)] = pixelColor;
                                        _blend[(i & 0x1ff)] = blendMaskType;

                                    }
                                }

                                rx += dx;
                                ry += dy;
                            }
                        }
                    }
                }
            }
        }
        #endregion Sprite Drawing
        #region Rot/Scale Bg
        private void RenderRotScaleBgNormal(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var x = _memory.Bgx[bg - 2];
            var y = _memory.Bgy[bg - 2];

            var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            var transparent = (bgcnt & (1 << 13)) == 0;

            for (var i = 0; i < 240; i++)
            {
                if (true)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        var tmpTileIdx = screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8);
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgBlend(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var x = _memory.Bgx[bg - 2];
            var y = _memory.Bgy[bg - 2];

            var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            var transparent = (bgcnt & (1 << 13)) == 0;

            for (var i = 0; i < 240; i++)
            {
                if (true)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        var tmpTileIdx = screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8);
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((_blend[i] & _blendTarget) != 0)
                            {
                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                var sourceValue = _scanline[i];
                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                if (r > 0xff) r = 0xff;
                                if (g > 0xff) g = 0xff;
                                if (b > 0xff) b = 0xff;
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgBrightInc(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var x = _memory.Bgx[bg - 2];
            var y = _memory.Bgy[bg - 2];

            var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            var transparent = (bgcnt & (1 << 13)) == 0;

            for (var i = 0; i < 240; i++)
            {
                if (true)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        var tmpTileIdx = screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8);
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            var r = pixelColor & 0xFF;
                            var g = (pixelColor >> 8) & 0xFF;
                            var b = (pixelColor >> 16) & 0xFF;
                            r = r + (((0xFF - r) * _blendY) >> 4);
                            g = g + (((0xFF - g) * _blendY) >> 4);
                            b = b + (((0xFF - b) * _blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgBrightDec(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var x = _memory.Bgx[bg - 2];
            var y = _memory.Bgy[bg - 2];

            var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            var transparent = (bgcnt & (1 << 13)) == 0;

            for (var i = 0; i < 240; i++)
            {
                if (true)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        var tmpTileIdx = screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8);
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            var r = pixelColor & 0xFF;
                            var g = (pixelColor >> 8) & 0xFF;
                            var b = (pixelColor >> 16) & 0xFF;
                            r = r - ((r * _blendY) >> 4);
                            g = g - ((g * _blendY) >> 4);
                            b = b - ((b * _blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgWindow(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var x = _memory.Bgx[bg - 2];
            var y = _memory.Bgy[bg - 2];

            var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            var transparent = (bgcnt & (1 << 13)) == 0;

            for (var i = 0; i < 240; i++)
            {
                if ((_windowCover[i] & (1 << bg)) != 0)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        var tmpTileIdx = screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8);
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgWindowBlend(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var x = _memory.Bgx[bg - 2];
            var y = _memory.Bgy[bg - 2];

            var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            var transparent = (bgcnt & (1 << 13)) == 0;

            for (var i = 0; i < 240; i++)
            {
                if ((_windowCover[i] & (1 << bg)) != 0)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        var tmpTileIdx = screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8);
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                if ((_blend[i] & _blendTarget) != 0)
                                {
                                    var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                    var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                    var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                    var sourceValue = _scanline[i];
                                    r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                    g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                    b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                    if (r > 0xff) r = 0xff;
                                    if (g > 0xff) g = 0xff;
                                    if (b > 0xff) b = 0xff;
                                    pixelColor = r | (g << 8) | (b << 16);
                                }
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgWindowBrightInc(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var x = _memory.Bgx[bg - 2];
            var y = _memory.Bgy[bg - 2];

            var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            var transparent = (bgcnt & (1 << 13)) == 0;

            for (var i = 0; i < 240; i++)
            {
                if ((_windowCover[i] & (1 << bg)) != 0)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        var tmpTileIdx = screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8);
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                var r = pixelColor & 0xFF;
                                var g = (pixelColor >> 8) & 0xFF;
                                var b = (pixelColor >> 16) & 0xFF;
                                r = r + (((0xFF - r) * _blendY) >> 4);
                                g = g + (((0xFF - g) * _blendY) >> 4);
                                b = b + (((0xFF - b) * _blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        private void RenderRotScaleBgWindowBrightDec(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 128; height = 128; break;
                case 1: width = 256; height = 256; break;
                case 2: width = 512; height = 512; break;
                case 3: width = 1024; height = 1024; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var x = _memory.Bgx[bg - 2];
            var y = _memory.Bgy[bg - 2];

            var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA + (uint)(bg - 2) * 0x10);
            var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC + (uint)(bg - 2) * 0x10);

            var transparent = (bgcnt & (1 << 13)) == 0;

            for (var i = 0; i < 240; i++)
            {
                if ((_windowCover[i] & (1 << bg)) != 0)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if ((ax >= 0 && ax < width && ay >= 0 && ay < height) || !transparent)
                    {
                        var tmpTileIdx = screenBase + ((ay & (height - 1)) / 8) * (width / 8) + ((ax & (width - 1)) / 8);
                        int tileChar = vram[tmpTileIdx];

                        int lookup = vram[charBase + (tileChar * 64) + ((ay & 7) * 8) + (ax & 7)];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                var r = pixelColor & 0xFF;
                                var g = (pixelColor >> 8) & 0xFF;
                                var b = (pixelColor >> 16) & 0xFF;
                                r = r - ((r * _blendY) >> 4);
                                g = g - ((g * _blendY) >> 4);
                                b = b - ((b * _blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }

                x += dx;
                y += dy;
            }
        }
        #endregion Rot/Scale Bg
        #region Text Bg
        private void RenderTextBgNormal(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var hofs = Memory.ReadU16(_memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            var vofs = Memory.ReadU16(_memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 8;

                for (var i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 4;

                for (var i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            var palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            var pixelColor = GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgBlend(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var hofs = Memory.ReadU16(_memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            var vofs = Memory.ReadU16(_memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 8;

                for (var i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((_blend[i] & _blendTarget) != 0)
                            {
                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                var sourceValue = _scanline[i];
                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                if (r > 0xff) r = 0xff;
                                if (g > 0xff) g = 0xff;
                                if (b > 0xff) b = 0xff;
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 4;

                for (var i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            var palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            var pixelColor = GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            if ((_blend[i] & _blendTarget) != 0)
                            {
                                var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                var sourceValue = _scanline[i];
                                r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                if (r > 0xff) r = 0xff;
                                if (g > 0xff) g = 0xff;
                                if (b > 0xff) b = 0xff;
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgBrightInc(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var hofs = Memory.ReadU16(_memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            var vofs = Memory.ReadU16(_memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 8;

                for (var i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            var r = pixelColor & 0xFF;
                            var g = (pixelColor >> 8) & 0xFF;
                            var b = (pixelColor >> 16) & 0xFF;
                            r = r + (((0xFF - r) * _blendY) >> 4);
                            g = g + (((0xFF - g) * _blendY) >> 4);
                            b = b + (((0xFF - b) * _blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 4;

                for (var i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            var palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            var pixelColor = GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            var r = pixelColor & 0xFF;
                            var g = (pixelColor >> 8) & 0xFF;
                            var b = (pixelColor >> 16) & 0xFF;
                            r = r + (((0xFF - r) * _blendY) >> 4);
                            g = g + (((0xFF - g) * _blendY) >> 4);
                            b = b + (((0xFF - b) * _blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgBrightDec(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var hofs = Memory.ReadU16(_memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            var vofs = Memory.ReadU16(_memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 8;

                for (var i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            var r = pixelColor & 0xFF;
                            var g = (pixelColor >> 8) & 0xFF;
                            var b = (pixelColor >> 16) & 0xFF;
                            r = r - ((r * _blendY) >> 4);
                            g = g - ((g * _blendY) >> 4);
                            b = b - ((b * _blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 4;

                for (var i = 0; i < 240; i++)
                {
                    if (true)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            var palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            var pixelColor = GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            var r = pixelColor & 0xFF;
                            var g = (pixelColor >> 8) & 0xFF;
                            var b = (pixelColor >> 16) & 0xFF;
                            r = r - ((r * _blendY) >> 4);
                            g = g - ((g * _blendY) >> 4);
                            b = b - ((b * _blendY) >> 4);
                            pixelColor = r | (g << 8) | (b << 16);
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgWindow(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var hofs = Memory.ReadU16(_memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            var vofs = Memory.ReadU16(_memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 8;

                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << bg)) != 0)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 4;

                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << bg)) != 0)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            var palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            var pixelColor = GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgWindowBlend(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var hofs = Memory.ReadU16(_memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            var vofs = Memory.ReadU16(_memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 8;

                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << bg)) != 0)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                if ((_blend[i] & _blendTarget) != 0)
                                {
                                    var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                    var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                    var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                    var sourceValue = _scanline[i];
                                    r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                    g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                    b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                    if (r > 0xff) r = 0xff;
                                    if (g > 0xff) g = 0xff;
                                    if (b > 0xff) b = 0xff;
                                    pixelColor = r | (g << 8) | (b << 16);
                                }
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 4;

                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << bg)) != 0)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            var palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            var pixelColor = GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                if ((_blend[i] & _blendTarget) != 0)
                                {
                                    var r = ((pixelColor & 0xFF) * _blendA) >> 4;
                                    var g = (((pixelColor >> 8) & 0xFF) * _blendA) >> 4;
                                    var b = (((pixelColor >> 16) & 0xFF) * _blendA) >> 4;
                                    var sourceValue = _scanline[i];
                                    r += ((sourceValue & 0xFF) * _blendB) >> 4;
                                    g += (((sourceValue >> 8) & 0xFF) * _blendB) >> 4;
                                    b += (((sourceValue >> 16) & 0xFF) * _blendB) >> 4;
                                    if (r > 0xff) r = 0xff;
                                    if (g > 0xff) g = 0xff;
                                    if (b > 0xff) b = 0xff;
                                    pixelColor = r | (g << 8) | (b << 16);
                                }
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgWindowBrightInc(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var hofs = Memory.ReadU16(_memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            var vofs = Memory.ReadU16(_memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 8;

                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << bg)) != 0)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                var r = pixelColor & 0xFF;
                                var g = (pixelColor >> 8) & 0xFF;
                                var b = (pixelColor >> 16) & 0xFF;
                                r = r + (((0xFF - r) * _blendY) >> 4);
                                g = g + (((0xFF - g) * _blendY) >> 4);
                                b = b + (((0xFF - b) * _blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 4;

                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << bg)) != 0)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            var palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            var pixelColor = GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                var r = pixelColor & 0xFF;
                                var g = (pixelColor >> 8) & 0xFF;
                                var b = (pixelColor >> 16) & 0xFF;
                                r = r + (((0xFF - r) * _blendY) >> 4);
                                g = g + (((0xFF - g) * _blendY) >> 4);
                                b = b + (((0xFF - b) * _blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        private void RenderTextBgWindowBrightDec(int bg)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var blendMaskType = (byte)(1 << bg);

            var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)bg);

            int width = 0, height = 0;
            switch ((bgcnt >> 14) & 0x3)
            {
                case 0: width = 256; height = 256; break;
                case 1: width = 512; height = 256; break;
                case 2: width = 256; height = 512; break;
                case 3: width = 512; height = 512; break;
            }

            var screenBase = ((bgcnt >> 8) & 0x1F) * 0x800;
            var charBase = ((bgcnt >> 2) & 0x3) * 0x4000;

            var hofs = Memory.ReadU16(_memory.IORam, Memory.BG0HOFS + (uint)bg * 4) & 0x1FF;
            var vofs = Memory.ReadU16(_memory.IORam, Memory.BG0VOFS + (uint)bg * 4) & 0x1FF;

            if ((bgcnt & (1 << 7)) != 0)
            {
                // 256 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 8;

                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << bg)) != 0)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 56 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 64) + y + x];
                        if (lookup != 0)
                        {
                            var pixelColor = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                var r = pixelColor & 0xFF;
                                var g = (pixelColor >> 8) & 0xFF;
                                var b = (pixelColor >> 16) & 0xFF;
                                r = r - ((r * _blendY) >> 4);
                                g = g - ((g * _blendY) >> 4);
                                b = b - ((b * _blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
            else
            {
                // 16 color tiles
                var bgy = ((_curLine + vofs) & (height - 1)) / 8;

                var tileIdx = screenBase + (((bgy & 31) * 32) * 2);
                switch ((bgcnt >> 14) & 0x3)
                {
                    case 2: if (bgy >= 32) tileIdx += 32 * 32 * 2; break;
                    case 3: if (bgy >= 32) tileIdx += 32 * 32 * 4; break;
                }

                var tileY = ((_curLine + vofs) & 0x7) * 4;

                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << bg)) != 0)
                    {
                        var bgx = ((i + hofs) & (width - 1)) / 8;
                        var tmpTileIdx = tileIdx + ((bgx & 31) * 2);
                        if (bgx >= 32) tmpTileIdx += 32 * 32 * 2;
                        var tileChar = vram[tmpTileIdx] | (vram[tmpTileIdx + 1] << 8);
                        var x = (i + hofs) & 7;
                        var y = tileY;
                        if ((tileChar & (1 << 10)) != 0) x = 7 - x;
                        if ((tileChar & (1 << 11)) != 0) y = 28 - y;
                        int lookup = vram[charBase + ((tileChar & 0x3FF) * 32) + y + (x / 2)];
                        if ((x & 1) == 0)
                        {
                            lookup &= 0xf;
                        }
                        else
                        {
                            lookup >>= 4;
                        }
                        if (lookup != 0)
                        {
                            var palNum = ((tileChar >> 12) & 0xf) * 16 * 2;
                            var pixelColor = GbaTo32((ushort)(palette[palNum + lookup * 2] | (palette[palNum + lookup * 2 + 1] << 8)));
                            if ((_windowCover[i] & (1 << 5)) != 0)
                            {
                                var r = pixelColor & 0xFF;
                                var g = (pixelColor >> 8) & 0xFF;
                                var b = (pixelColor >> 16) & 0xFF;
                                r = r - ((r * _blendY) >> 4);
                                g = g - ((g * _blendY) >> 4);
                                b = b - ((b * _blendY) >> 4);
                                pixelColor = r | (g << 8) | (b << 16);
                            }
                            _scanline[i] = pixelColor; _blend[i] = blendMaskType;

                        }
                    }
                }
            }
        }
        #endregion Text Bg
    }
}
