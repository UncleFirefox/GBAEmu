using System.Drawing;
using System.Windows.Forms;
using GarboDev.Cores;
using GarboDev.CrossCutting;
using GarboDev.Graphics;

namespace GarboDev.WinForms
{
    partial class PaletteWindow : Form
    {
        private Memory _memory;

        public PaletteWindow(GbaManager.GbaManager gbaManager)
        {
            InitializeComponent();

            gbaManager.OnCpuUpdate += Update;

            Width = Width & (~15);
            Height = Height & (~15);
        }

        public void Update(Arm7Processor processor, Memory memory)
        {
            _memory = memory;

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_memory == null) return;

            var rectWidth = (Width - 5) / 32;
            var rectHeight = Height / 16;

            for (var i = 0; i < 256; i++)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb((int)(0xFF000000 | Renderer.GbaTo32((ushort)(_memory.PaletteRam[i * 2] | (_memory.PaletteRam[i * 2 + 1] << 8)))))),
                    (i % 16) * rectWidth, (i / 16) * rectHeight, rectWidth, rectHeight);

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb((int)(0xFF000000 | Renderer.GbaTo32((ushort)(_memory.PaletteRam[0x200 + i * 2] | (_memory.PaletteRam[0x200 + i * 2 + 1] << 8)))))),
                    5 + (16 + (i % 16)) * rectWidth, (i / 16) * rectHeight, rectWidth, rectHeight);
            }
        }
    }
}