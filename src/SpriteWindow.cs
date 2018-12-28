using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GarboDev.Cores;
using GarboDev.CrossCutting;
using GarboDev.Graphics;

namespace GarboDev.WinForms
{
    public partial class SpriteWindow : Form
    {
        private Memory _memory;
        private readonly Bitmap _bitmap;

        public SpriteWindow(GbaManager.GbaManager gbaManager)
        {
            InitializeComponent();

            _bitmap = new Bitmap(128, 128, PixelFormat.Format32bppRgb);
            spriteDisplay.Image = _bitmap;
            
            Refresh();

            gbaManager.OnCpuUpdate += Update;
        }

        public void Update(Arm7Processor processor, Memory memory)
        {
            _memory = memory;

            Refresh();
        }

        private void SpriteSelector_Scroll(object sender, EventArgs e)
        {
            Refresh();
        }

        private new void Refresh()
        {
            for (var y = 0; y < 128; y++)
            {
                for (var x = 0; x < 128; x++)
                {
                    _bitmap.SetPixel(x, y, Color.Black);
                }
            }

            if (_memory == null)
            {
                spriteLeft.Text = "";
                spriteTop.Text = "";
                spriteWidth.Text = "";
                spriteHeight.Text = "";
                spriteBase.Text = "";
                spriteBlend.Text = "";
                spriteMode.Text = "";
                spritePal.Text = "";
                spritePri.Text = "";
                spriteWind.Text = "";
            }
            else
            {
                var oamNum = spriteSelector.Value;

                DrawSprite(oamNum);

                var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);
                var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                spriteLeft.Text = (attr1 & 0x1FF).ToString();
                spriteTop.Text = (attr0 & 0xFF).ToString();

                spritePri.Text = ((attr2 >> 10) & 3).ToString();

                spriteBlend.Text = "None";
                spriteWind.Text = "None";

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        spriteBlend.Text = "Blending";
                        break;
                    case 2:
                        // Obj window
                        spriteWind.Text = "Window";
                        break;
                    case 3:
                        // Invalid
                        break;
                }

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

                spriteWidth.Text = width.ToString();
                spriteHeight.Text = height.ToString();

                // Check double size flag here

                if ((attr0 & (1 << 8)) != 0)
                {
                    spriteMode.Text = "Rot/Scale";
                }
                else
                {
                    spriteMode.Text = "Normal";
                }
            }

            spriteNumber.Text = spriteSelector.Value.ToString();

            spriteDisplay.Image = _bitmap;
            spriteDisplay.Invalidate();
        }

        private void DrawSprite(int oamNum)
        {
            var palette = _memory.PaletteRam;
            var vram = _memory.VideoRam;

            var dispCnt = Memory.ReadU16(_memory.IORam, Memory.DISPCNT);

            var attr0 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
            var attr1 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);
            var attr2 = _memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

            var window = false;
            switch ((attr0 >> 10) & 3)
            {
                case 1:
                    // Semi-transparent
                    break;
                case 2:
                    window = true;
                    // Obj window
                    break;
                case 3:
                    return;
            }

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

            var scale = 1;
            if ((attr0 & (1 << 13)) != 0) scale = 2;

            for (var spritey = 0; spritey < height; spritey++)
            {
                if ((attr0 & (1 << 8)) == 0)
                {
                    if ((attr1 & (1 << 13)) != 0) spritey = (height - 1) - spritey;

                    int baseSprite;
                    if ((dispCnt & (1 << 6)) != 0)
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
                        for (var i = 0; i < width; i++)
                        {
                            if ((i & 0x1ff) < 240 && true)
                            {
                                var tx = i & 7;
                                if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                var curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                int lookup = vram[0x10000 + curIdx];
                                if (lookup != 0)
                                {
                                    var pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                    if (window) pixelColor = 0xFFFFFFFF;
                                    _bitmap.SetPixel(i & 0x1ff, spritey, Color.FromArgb((int)(0xFF000000 | pixelColor)));
                                }
                            }
                            if ((i & 7) == 7) baseSprite += baseInc;
                        }
                    }
                    else
                    {
                        // 16 colors
                        var palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                        for (var i = 0; i < width; i++)
                        {
                            if ((i & 0x1ff) < 240 && true)
                            {
                                var tx = i & 7;
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
                                    var pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                    if (window) pixelColor = 0xFFFFFFFF;
                                    _bitmap.SetPixel(i & 0x1ff, spritey, Color.FromArgb((int)(0xFF000000 | pixelColor)));
                                }
                            }
                            if ((i & 7) == 7) baseSprite += baseInc;
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

                    if ((dispCnt & (1 << 6)) != 0)
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
                        for (var i = 0; i < rwidth; i++)
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
                                    var pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                    if (window) pixelColor = 0xFFFFFFFF;
                                    _bitmap.SetPixel(i & 0x1ff, spritey, Color.FromArgb((int)(0xFF000000 | pixelColor)));
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
                        for (var i = 0; i < rwidth; i++)
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
                                    var pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                    if (window) pixelColor = 0xFFFFFFFF;
                                    _bitmap.SetPixel(i & 0x1ff, spritey, Color.FromArgb((int)(0xFF000000 | pixelColor)));
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
}