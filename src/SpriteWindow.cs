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
        private Memory memory = null;
        private Bitmap bitmap = null;
        private GbaManager.GbaManager gbaManager = null;

        public SpriteWindow(GbaManager.GbaManager gbaManager)
        {
            InitializeComponent();

            this.gbaManager = gbaManager;

            this.bitmap = new Bitmap(128, 128, PixelFormat.Format32bppRgb);
            this.spriteDisplay.Image = this.bitmap;
            
            this.Refresh();

            this.gbaManager.OnCpuUpdate += new GbaManager.GbaManager.CpuUpdateDelegate(Update);
        }

        public void Update(Arm7Processor processor, Memory memory)
        {
            this.memory = memory;

            this.Refresh();
        }

        private void SpriteSelector_Scroll(object sender, EventArgs e)
        {
            this.Refresh();
        }

        private new void Refresh()
        {
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    this.bitmap.SetPixel(x, y, Color.Black);
                }
            }

            if (this.memory == null)
            {
                this.spriteLeft.Text = "";
                this.spriteTop.Text = "";
                this.spriteWidth.Text = "";
                this.spriteHeight.Text = "";
                this.spriteBase.Text = "";
                this.spriteBlend.Text = "";
                this.spriteMode.Text = "";
                this.spritePal.Text = "";
                this.spritePri.Text = "";
                this.spriteWind.Text = "";
            }
            else
            {
                int oamNum = this.spriteSelector.Value;

                this.DrawSprite(oamNum);

                ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
                ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);
                ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

                this.spriteLeft.Text = (attr1 & 0x1FF).ToString();
                this.spriteTop.Text = (attr0 & 0xFF).ToString();

                this.spritePri.Text = ((attr2 >> 10) & 3).ToString();

                this.spriteBlend.Text = "None";
                this.spriteWind.Text = "None";

                switch ((attr0 >> 10) & 3)
                {
                    case 1:
                        // Semi-transparent
                        this.spriteBlend.Text = "Blending";
                        break;
                    case 2:
                        // Obj window
                        this.spriteWind.Text = "Window";
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

                this.spriteWidth.Text = width.ToString();
                this.spriteHeight.Text = height.ToString();

                // Check double size flag here

                if ((attr0 & (1 << 8)) != 0)
                {
                    this.spriteMode.Text = "Rot/Scale";
                }
                else
                {
                    this.spriteMode.Text = "Normal";
                }
            }

            this.spriteNumber.Text = this.spriteSelector.Value.ToString();

            this.spriteDisplay.Image = this.bitmap;
            this.spriteDisplay.Invalidate();
        }

        private void DrawSprite(int oamNum)
        {
            byte[] palette = this.memory.PaletteRam;
            byte[] vram = this.memory.VideoRam;

            ushort dispCnt = Memory.ReadU16(this.memory.IORam, Memory.DISPCNT);

            ushort attr0 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 0);
            ushort attr1 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 2);
            ushort attr2 = this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(oamNum * 8) + 4);

            bool window = false;
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

            int scale = 1;
            if ((attr0 & (1 << 13)) != 0) scale = 2;

            for (int spritey = 0; spritey < height; spritey++)
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

                    int baseInc = scale;
                    if ((attr1 & (1 << 12)) != 0)
                    {
                        baseSprite += ((width / 8) * scale) - scale;
                        baseInc = -baseInc;
                    }

                    if ((attr0 & (1 << 13)) != 0)
                    {
                        // 256 colors
                        for (int i = 0; i < width; i++)
                        {
                            if ((i & 0x1ff) < 240 && true)
                            {
                                int tx = i & 7;
                                if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                int curIdx = baseSprite * 32 + ((spritey & 7) * 8) + tx;
                                int lookup = vram[0x10000 + curIdx];
                                if (lookup != 0)
                                {
                                    uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                    if (window) pixelColor = 0xFFFFFFFF;
                                    this.bitmap.SetPixel(i & 0x1ff, spritey, Color.FromArgb((int)(0xFF000000 | pixelColor)));
                                }
                            }
                            if ((i & 7) == 7) baseSprite += baseInc;
                        }
                    }
                    else
                    {
                        // 16 colors
                        int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                        for (int i = 0; i < width; i++)
                        {
                            if ((i & 0x1ff) < 240 && true)
                            {
                                int tx = i & 7;
                                if ((attr1 & (1 << 12)) != 0) tx = 7 - tx;
                                int curIdx = baseSprite * 32 + ((spritey & 7) * 4) + (tx / 2);
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
                                    uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                    if (window) pixelColor = 0xFFFFFFFF;
                                    this.bitmap.SetPixel(i & 0x1ff, spritey, Color.FromArgb((int)(0xFF000000 | pixelColor)));
                                }
                            }
                            if ((i & 7) == 7) baseSprite += baseInc;
                        }
                    }
                }
                else
                {
                    int rotScaleParam = (attr1 >> 9) & 0x1F;

                    short dx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x6);
                    short dmx = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0xE);
                    short dy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x16);
                    short dmy = (short)this.memory.ReadU16Debug(Memory.OAM_BASE + (uint)(rotScaleParam * 8 * 4) + 0x1E);

                    int cx = rwidth / 2;
                    int cy = rheight / 2;

                    int baseSprite = attr2 & 0x3FF;
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

                    short rx = (short)((dmx * (spritey - cy)) - (cx * dx) + (width << 7));
                    short ry = (short)((dmy * (spritey - cy)) - (cx * dy) + (height << 7));

                    // Draw a rot/scale sprite
                    if ((attr0 & (1 << 13)) != 0)
                    {
                        // 256 colors
                        for (int i = 0; i < rwidth; i++)
                        {
                            int tx = rx >> 8;
                            int ty = ry >> 8;

                            if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                && true)
                            {
                                int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 8) + (tx & 7);
                                int lookup = vram[0x10000 + curIdx];
                                if (lookup != 0)
                                {
                                    uint pixelColor = Renderer.GbaTo32((ushort)(palette[0x200 + lookup * 2] | (palette[0x200 + lookup * 2 + 1] << 8)));
                                    if (window) pixelColor = 0xFFFFFFFF;
                                    this.bitmap.SetPixel(i & 0x1ff, spritey, Color.FromArgb((int)(0xFF000000 | pixelColor)));
                                }
                            }

                            rx += dx;
                            ry += dy;
                        }
                    }
                    else
                    {
                        // 16 colors
                        int palIdx = 0x200 + (((attr2 >> 12) & 0xF) * 16 * 2);
                        for (int i = 0; i < rwidth; i++)
                        {
                            int tx = rx >> 8;
                            int ty = ry >> 8;

                            if ((i & 0x1ff) < 240 && tx >= 0 && tx < width && ty >= 0 && ty < height
                                && true)
                            {
                                int curIdx = (baseSprite + ((ty / 8) * pitch) + ((tx / 8) * scale)) * 32 + ((ty & 7) * 4) + ((tx & 7) / 2);
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
                                    uint pixelColor = Renderer.GbaTo32((ushort)(palette[palIdx + lookup * 2] | (palette[palIdx + lookup * 2 + 1] << 8)));
                                    if (window) pixelColor = 0xFFFFFFFF;
                                    this.bitmap.SetPixel(i & 0x1ff, spritey, Color.FromArgb((int)(0xFF000000 | pixelColor)));
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