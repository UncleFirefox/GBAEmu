using System;
using GarboDev.CrossCutting;

namespace GarboDev.Graphics
{
    public partial class Renderer : IRenderer
    {
        private Memory _memory;
        private readonly uint[] _scanline = new uint[240];
        private readonly byte[] _blend = new byte[240];
        private readonly byte[] _windowCover = new byte[240];
        private readonly uint[] _back = new uint[240 * 160];
        private readonly uint[] _front = new uint[240 * 160];
        private const uint Pitch = 240;

        // Convenience variable as I use it everywhere, set once in RenderLine
        private ushort _dispCnt;

        // Window helper variables
        private byte _win0X1, _win0X2, _win0Y1, _win0Y2;
        private byte _win1X1, _win1X2, _win1Y1, _win1Y2;
        private byte _win0Enabled, _win1Enabled, _winObjEnabled, _winOutEnabled;
        private bool _winEnabled;

        private byte _blendSource, _blendTarget;
        private byte _blendA, _blendB, _blendY;
        private int _blendType;

        private int _curLine;

        private static readonly uint[] ColorLut;

        static Renderer()
        {
            ColorLut = new uint[0x10000];
            // Pre-calculate the color LUT
            for (uint i = 0; i <= 0xFFFF; i++)
            {
                var r = (i & 0x1FU);
                var g = (i & 0x3E0U) >> 5;
                var b = (i & 0x7C00U) >> 10;
                r = (r << 3) | (r >> 2);
                g = (g << 3) | (g >> 2);
                b = (b << 3) | (b >> 2);
                ColorLut[i] = (r << 16) | (g << 8) | b;
            }
        }

        public Memory Memory
        {
            set => _memory = value;
        }

        public void Initialize(object data)
        {
        }

        public void Reset()
        {
        }

        public object ShowFrame()
        {
            Array.Copy(_back, _front, _front.Length);

            return _front;
        }

        public void RenderLine(int line)
        {
            _curLine = line;

            // Render the line
            _dispCnt = Memory.ReadU16(_memory.IORam, Memory.DISPCNT);

            if ((_dispCnt & (1 << 7)) != 0)
            {
                var bgColor = GbaTo32(0x7FFF);
                for (var i = 0; i < 240; i++) _scanline[i] = bgColor;
            }
            else
            {
                _winEnabled = false;

                if ((_dispCnt & (1 << 13)) != 0)
                {
                    // Calculate window 0 information
                    var winy = Memory.ReadU16(_memory.IORam, Memory.WIN0V);
                    _win0Y1 = (byte)(winy >> 8);
                    _win0Y2 = (byte)(winy & 0xff);
                    var winx = Memory.ReadU16(_memory.IORam, Memory.WIN0H);
                    _win0X1 = (byte)(winx >> 8);
                    _win0X2 = (byte)(winx & 0xff);

                    if (_win0X2 > 240 || _win0X1 > _win0X2)
                    {
                        _win0X2 = 240;
                    }

                    if (_win0Y2 > 160 || _win0Y1 > _win0Y2)
                    {
                        _win0Y2 = 160;
                    }

                    _win0Enabled = _memory.IORam[Memory.WININ];
                    _winEnabled = true;
                }

                if ((_dispCnt & (1 << 14)) != 0)
                {
                    // Calculate window 1 information
                    var winy = Memory.ReadU16(_memory.IORam, Memory.WIN1V);
                    _win1Y1 = (byte)(winy >> 8);
                    _win1Y2 = (byte)(winy & 0xff);
                    var winx = Memory.ReadU16(_memory.IORam, Memory.WIN1H);
                    _win1X1 = (byte)(winx >> 8);
                    _win1X2 = (byte)(winx & 0xff);

                    if (_win1X2 > 240 || _win1X1 > _win1X2)
                    {
                        _win1X2 = 240;
                    }

                    if (_win1Y2 > 160 || _win1Y1 > _win1Y2)
                    {
                        _win1Y2 = 160;
                    }

                    _win1Enabled = _memory.IORam[Memory.WININ + 1];
                    _winEnabled = true;
                }

                if ((_dispCnt & (1 << 15)) != 0 && (_dispCnt & (1 << 12)) != 0)
                {
                    // Object windows are enabled
                    _winObjEnabled = _memory.IORam[Memory.WINOUT + 1];
                    _winEnabled = true;
                }

                if (_winEnabled)
                {
                    _winOutEnabled = _memory.IORam[Memory.WINOUT];
                }

                // Calculate blending information
                var bldcnt = Memory.ReadU16(_memory.IORam, Memory.BLDCNT);
                _blendType = (bldcnt >> 6) & 0x3;
                _blendSource = (byte)(bldcnt & 0x3F);
                _blendTarget = (byte)((bldcnt >> 8) & 0x3F);

                var bldalpha = Memory.ReadU16(_memory.IORam, Memory.BLDALPHA);
                _blendA = (byte)(bldalpha & 0x1F);
                if (_blendA > 0x10) _blendA = 0x10;
                _blendB = (byte)((bldalpha >> 8) & 0x1F);
                if (_blendB > 0x10) _blendB = 0x10;

                _blendY = (byte)(_memory.IORam[Memory.BLDY] & 0x1F);
                if (_blendY > 0x10) _blendY = 0x10;

                switch (_dispCnt & 0x7)
                {
                    case 0: RenderMode0Line(); break;
                    case 1: RenderMode1Line(); break;
                    case 2: RenderMode2Line(); break;
                    case 3: RenderMode3Line(); break;
                    case 4: RenderMode4Line(); break;
                    case 5: RenderMode5Line(); break;
                }
            }

            Array.Copy(_scanline, 0, _back, _curLine * Pitch, Pitch);
        }

        private void DrawBackdrop()
        {
            var palette = _memory.PaletteRam;

            // Initialize window coverage buffer if neccesary
            if (_winEnabled)
            {
                for (var i = 0; i < 240; i++)
                {
                    _windowCover[i] = _winOutEnabled;
                }

                if ((_dispCnt & (1 << 15)) != 0)
                {
                    // Sprite window
                    DrawSpriteWindows();
                }

                if ((_dispCnt & (1 << 14)) != 0)
                {
                    // Window 1
                    if (_curLine >= _win1Y1 && _curLine < _win1Y2)
                    {
                        for (int i = _win1X1; i < _win1X2; i++)
                        {
                            _windowCover[i] = _win1Enabled;
                        }
                    }
                }

                if ((_dispCnt & (1 << 13)) != 0)
                {
                    // Window 0
                    if (_curLine >= _win0Y1 && _curLine < _win0Y2)
                    {
                        for (int i = _win0X1; i < _win0X2; i++)
                        {
                            _windowCover[i] = _win0Enabled;
                        }
                    }
                }
            }

            // Draw backdrop first
            var bgColor = GbaTo32((ushort)(palette[0] | (palette[1] << 8)));
            var modColor = bgColor;

            if (_blendType == 2 && (_blendSource & (1 << 5)) != 0)
            {
                // Brightness increase
                var r = bgColor & 0xFF;
                var g = (bgColor >> 8) & 0xFF;
                var b = (bgColor >> 16) & 0xFF;
                r = r + (((0xFF - r) * _blendY) >> 4);
                g = g + (((0xFF - g) * _blendY) >> 4);
                b = b + (((0xFF - b) * _blendY) >> 4);
                modColor = r | (g << 8) | (b << 16);
            }
            else if (_blendType == 3 && (_blendSource & (1 << 5)) != 0)
            {
                // Brightness decrease
                var r = bgColor & 0xFF;
                var g = (bgColor >> 8) & 0xFF;
                var b = (bgColor >> 16) & 0xFF;
                r = r - ((r * _blendY) >> 4);
                g = g - ((g * _blendY) >> 4);
                b = b - ((b * _blendY) >> 4);
                modColor = r | (g << 8) | (b << 16);
            }

            if (_winEnabled)
            {
                for (var i = 0; i < 240; i++)
                {
                    if ((_windowCover[i] & (1 << 5)) != 0)
                    {
                        _scanline[i] = modColor;
                    }
                    else
                    {
                        _scanline[i] = bgColor;
                    }
                    _blend[i] = 1 << 5;
                }
            }
            else
            {
                for (var i = 0; i < 240; i++)
                {
                    _scanline[i] = modColor;
                    _blend[i] = 1 << 5;
                }
            }
        }

        private void RenderTextBg(int bg)
        {
            if (_winEnabled)
            {
                switch (_blendType)
                {
                    case 0:
                        RenderTextBgWindow(bg);
                        break;
                    case 1:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderTextBgWindowBlend(bg);
                        else
                            RenderTextBgWindow(bg);
                        break;
                    case 2:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderTextBgWindowBrightInc(bg);
                        else
                            RenderTextBgWindow(bg);
                        break;
                    case 3:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderTextBgWindowBrightDec(bg);
                        else
                            RenderTextBgWindow(bg);
                        break;
                }
            }
            else
            {
                switch (_blendType)
                {
                    case 0:
                        RenderTextBgNormal(bg);
                        break;
                    case 1:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderTextBgBlend(bg);
                        else
                            RenderTextBgNormal(bg);
                        break;
                    case 2:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderTextBgBrightInc(bg);
                        else
                            RenderTextBgNormal(bg);
                        break;
                    case 3:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderTextBgBrightDec(bg);
                        else
                            RenderTextBgNormal(bg);
                        break;
                }
            }
        }

        private void RenderRotScaleBg(int bg)
        {
            if (_winEnabled)
            {
                switch (_blendType)
                {
                    case 0:
                        RenderRotScaleBgWindow(bg);
                        break;
                    case 1:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderRotScaleBgWindowBlend(bg);
                        else
                            RenderRotScaleBgWindow(bg);
                        break;
                    case 2:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderRotScaleBgWindowBrightInc(bg);
                        else
                            RenderRotScaleBgWindow(bg);
                        break;
                    case 3:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderRotScaleBgWindowBrightDec(bg);
                        else
                            RenderRotScaleBgWindow(bg);
                        break;
                }
            }
            else
            {
                switch (_blendType)
                {
                    case 0:
                        RenderRotScaleBgNormal(bg);
                        break;
                    case 1:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderRotScaleBgBlend(bg);
                        else
                            RenderRotScaleBgNormal(bg);
                        break;
                    case 2:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderRotScaleBgBrightInc(bg);
                        else
                            RenderRotScaleBgNormal(bg);
                        break;
                    case 3:
                        if ((_blendSource & (1 << bg)) != 0)
                            RenderRotScaleBgBrightDec(bg);
                        else
                            RenderRotScaleBgNormal(bg);
                        break;
                }
            }
        }

        private void DrawSprites(int pri)
        {
            if (_winEnabled)
            {
                switch (_blendType)
                {
                    case 0:
                        DrawSpritesWindow(pri);
                        break;
                    case 1:
                        if ((_blendSource & (1 << 4)) != 0)
                            DrawSpritesWindowBlend(pri);
                        else
                            DrawSpritesWindow(pri);
                        break;
                    case 2:
                        if ((_blendSource & (1 << 4)) != 0)
                            DrawSpritesWindowBrightInc(pri);
                        else
                            DrawSpritesWindow(pri);
                        break;
                    case 3:
                        if ((_blendSource & (1 << 4)) != 0)
                            DrawSpritesWindowBrightDec(pri);
                        else
                            DrawSpritesWindow(pri);
                        break;
                }
            }
            else
            {
                switch (_blendType)
                {
                    case 0:
                        DrawSpritesNormal(pri);
                        break;
                    case 1:
                        if ((_blendSource & (1 << 4)) != 0)
                            DrawSpritesBlend(pri);
                        else
                            DrawSpritesNormal(pri);
                        break;
                    case 2:
                        if ((_blendSource & (1 << 4)) != 0)
                            DrawSpritesBrightInc(pri);
                        else
                            DrawSpritesNormal(pri);
                        break;
                    case 3:
                        if ((_blendSource & (1 << 4)) != 0)
                            DrawSpritesBrightDec(pri);
                        else
                            DrawSpritesNormal(pri);
                        break;
                }
            }
        }

        private void RenderMode0Line()
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            DrawBackdrop();

            for (var pri = 3; pri >= 0; pri--)
            {
                for (var i = 3; i >= 0; i--)
                {
                    if ((_dispCnt & (1 << (8 + i))) != 0)
                    {
                        var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)i);

                        if ((bgcnt & 0x3) == pri)
                        {
                            RenderTextBg(i);
                        }
                    }
                }

                DrawSprites(pri);
            }
        }

        private void RenderMode1Line()
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            DrawBackdrop();

            for (var pri = 3; pri >= 0; pri--)
            {
                if ((_dispCnt & (1 << (8 + 2))) != 0)
                {
                    var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG2CNT);

                    if ((bgcnt & 0x3) == pri)
                    {
                        RenderRotScaleBg(2);
                    }
                }

                for (var i = 1; i >= 0; i--)
                {
                    if ((_dispCnt & (1 << (8 + i))) != 0)
                    {
                        var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)i);

                        if ((bgcnt & 0x3) == pri)
                        {
                            RenderTextBg(i);
                        }
                    }
                }

                DrawSprites(pri);
            }
        }

        private void RenderMode2Line()
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            DrawBackdrop();

            for (var pri = 3; pri >= 0; pri--)
            {
                for (var i = 3; i >= 2; i--)
                {
                    if ((_dispCnt & (1 << (8 + i))) != 0)
                    {
                        var bgcnt = Memory.ReadU16(_memory.IORam, Memory.BG0CNT + 0x2 * (uint)i);

                        if ((bgcnt & 0x3) == pri)
                        {
                            RenderRotScaleBg(i);
                        }
                    }
                }

                DrawSprites(pri);
            }
        }

        private void RenderMode3Line()
        {
            var bg2Cnt = Memory.ReadU16(_memory.IORam, Memory.BG2CNT);

            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            DrawBackdrop();

            var blendMaskType = (byte)(1 << 2);

            var bgPri = bg2Cnt & 0x3;
            for (var pri = 3; pri > bgPri; pri--)
            {
                DrawSprites(pri);
            }

            if ((_dispCnt & (1 << 10)) != 0)
            {
                // Background enabled, render it
                var x = _memory.Bgx[0];
                var y = _memory.Bgy[0];

                var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA);
                var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC);

                for (var i = 0; i < 240; i++)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if (ax >= 0 && ax < 240 && ay >= 0 && ay < 160)
                    {
                        var curIdx = ((ay * 240) + ax) * 2;
                        _scanline[i] = GbaTo32((ushort)(vram[curIdx] | (vram[curIdx + 1] << 8)));
                        _blend[i] = blendMaskType;
                    }

                    x += dx;
                    y += dy;
                }
            }

            for (var pri = bgPri; pri >= 0; pri--)
            {
                DrawSprites(pri);
            }
        }

        private void RenderMode4Line()
        {
            var bg2Cnt = Memory.ReadU16(_memory.IORam, Memory.BG2CNT);

            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            DrawBackdrop();

            var blendMaskType = (byte)(1 << 2);

            var bgPri = bg2Cnt & 0x3;
            for (var pri = 3; pri > bgPri; pri--)
            {
                DrawSprites(pri);
            }

            if ((_dispCnt & (1 << 10)) != 0)
            {
                // Background enabled, render it
                var baseIdx = 0;
                if ((_dispCnt & (1 << 4)) == 1 << 4) baseIdx = 0xA000;

                var x = _memory.Bgx[0];
                var y = _memory.Bgy[0];

                var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA);
                var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC);

                for (var i = 0; i < 240; i++)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if (ax >= 0 && ax < 240 && ay >= 0 && ay < 160)
                    {
                        int lookup = vram[baseIdx + (ay * 240) + ax];
                        if (lookup != 0)
                        {
                            _scanline[i] = GbaTo32((ushort)(palette[lookup * 2] | (palette[lookup * 2 + 1] << 8)));
                            _blend[i] = blendMaskType;
                        }
                    }

                    x += dx;
                    y += dy;
                }
            }

            for (var pri = bgPri; pri >= 0; pri--)
            {
                DrawSprites(pri);
            }
        }

        private void RenderMode5Line()
        {
            var bg2Cnt = Memory.ReadU16(_memory.IORam, Memory.BG2CNT);

            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            DrawBackdrop();

            var blendMaskType = (byte)(1 << 2);

            var bgPri = bg2Cnt & 0x3;
            for (var pri = 3; pri > bgPri; pri--)
            {
                DrawSprites(pri);
            }

            if ((_dispCnt & (1 << 10)) != 0)
            {
                // Background enabled, render it
                var baseIdx = 0;
                if ((_dispCnt & (1 << 4)) == 1 << 4) baseIdx += 160 * 128 * 2;

                var x = _memory.Bgx[0];
                var y = _memory.Bgy[0];

                var dx = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PA);
                var dy = (short)Memory.ReadU16(_memory.IORam, Memory.BG2PC);

                for (var i = 0; i < 240; i++)
                {
                    var ax = x >> 8;
                    var ay = y >> 8;

                    if (ax >= 0 && ax < 160 && ay >= 0 && ay < 128)
                    {
                        var curIdx = (ay * 160 + ax) * 2;

                        _scanline[i] = GbaTo32((ushort)(vram[baseIdx + curIdx] | (vram[baseIdx + curIdx + 1] << 8)));
                        _blend[i] = blendMaskType;
                    }

                    x += dx;
                    y += dy;
                }
            }

            for (var pri = bgPri; pri >= 0; pri--)
            {
                DrawSprites(pri);
            }
        }

        private void DrawSpriteWindows()
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            // OBJ must be enabled in this.dispCnt
            if ((_dispCnt & (1 << 12)) == 0) return;

            for (var oamNum = 127; oamNum >= 0; oamNum--)
            {
                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                
                // Not an object window, so continue
                if (((attr0 >> 10) & 3) != 2) continue;

                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                var x = attr1 & 0x1FF;
                var y = attr0 & 0xFF;

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
                            if ((i & 0x1ff) < 240)
                            {
                                var tx = (i - x) & 7;
                                if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                int lookup = vram[0x10000 + curIdx];
                                if (lookup != 0)
                                {
                                    _windowCover[i & 0x1ff] = _winObjEnabled;
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
                            if ((i & 0x1ff) < 240)
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
                                    _windowCover[i & 0x1ff] = _winObjEnabled;
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

                    var rx = (short)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                    var ry = (short)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                    // Draw a rot/scale sprite
                    if ((attr0 & (1 << 13)) != 0)
                    {
                        // 256 colors
                        for (var i = x; i < x + rwidth; i++)
                        {
                            var tx = rx >> 8;
                            var ty = ry >> 8;

                            if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height)
                            {
                                var curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                int lookup = vram[0x10000 + curIdx];
                                if (lookup != 0)
                                {
                                    _windowCover[i & 0x1ff] = _winObjEnabled;
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

                            if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height)
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
                                    _windowCover[i & 0x1ff] = _winObjEnabled;
                                }
                            }

                            rx += dx;
                            ry += dy;
                        }
                    }
                }
            }
        }

        public static uint GbaTo32(ushort color)
        {
            // more accurate, but slower :(
            // return colorLUT[color];
            return ((color & 0x1FU) << 19) | ((color & 0x3E0U) << 6) | ((color & 0x7C00U) >> 7);
        }
    }
}
