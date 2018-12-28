using System.Drawing;
using System.Windows.Forms;
using GarboDev.Cores;
using GarboDev.CrossCutting;
using GarboDev.Graphics;

namespace GarboDev.WinForms
{
    partial class PaletteWindow : Form
    {
        private Memory memory = null;

        public PaletteWindow(GbaManager.GbaManager gbaManager)
        {
            InitializeComponent();

            gbaManager.OnCpuUpdate += new GbaManager.GbaManager.CpuUpdateDelegate(Update);

            this.Width = this.Width & (~15);
            this.Height = this.Height & (~15);
        }

        public void Update(Arm7Processor processor, Memory memory)
        {
            this.memory = memory;

            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.memory == null) return;

            int rectWidth = (this.Width - 5) / 32;
            int rectHeight = this.Height / 16;

            for (int i = 0; i < 256; i++)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb((int)(0xFF000000 | Renderer.GbaTo32((ushort)(this.memory.PaletteRam[i * 2] | (this.memory.PaletteRam[i * 2 + 1] << 8)))))),
                    (i % 16) * rectWidth, (i / 16) * rectHeight, rectWidth, rectHeight);

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb((int)(0xFF000000 | Renderer.GbaTo32((ushort)(this.memory.PaletteRam[0x200 + i * 2] | (this.memory.PaletteRam[0x200 + i * 2 + 1] << 8)))))),
                    5 + (16 + (i % 16)) * rectWidth, (i / 16) * rectHeight, rectWidth, rectHeight);
            }
        }
    }
}