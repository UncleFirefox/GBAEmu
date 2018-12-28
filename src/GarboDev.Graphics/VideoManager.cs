using System;
using GarboDev.Cores;
using GarboDev.CrossCutting;

namespace GarboDev.Graphics
{
    public class VideoManager
    {
        private readonly Action _onFramesRendered;

        public delegate void OnPresent(object data);

        private Memory memory;
        private IRenderer renderer;
        private OnPresent presenter;
        private int curLine;

        public Memory Memory
        {
            set => memory = value;
        }

        public IRenderer Renderer
        {
            set
            {
                renderer = value;
                renderer.Memory = memory;
            }
        }

        public OnPresent Presenter
        {
            set => presenter = value;
        }

        public VideoManager(Action onFramesRendered)
        {
            _onFramesRendered = onFramesRendered;
        }

        public void Reset()
        {
            curLine = 0;

            renderer.Memory = memory;
            renderer.Reset();
        }

        private void EnterVBlank(Arm7Processor processor)
        {
            var dispstat = Memory.ReadU16(memory.IORam, Memory.DISPSTAT);
            dispstat |= 1;
            Memory.WriteU16(memory.IORam, Memory.DISPSTAT, dispstat);

            // Render the frame
            _onFramesRendered();
            presenter(renderer.ShowFrame());

            if ((dispstat & (1 << 3)) != 0)
            {
                // Fire the vblank irq
                processor.RequestIrq(0);
            }

            // Check for DMA triggers
            memory.VBlankDma();
        }

        private void LeaveVBlank(Arm7Processor processor)
        {
            var dispstat = Memory.ReadU16(memory.IORam, Memory.DISPSTAT);
            dispstat &= 0xFFFE;
            Memory.WriteU16(memory.IORam, Memory.DISPSTAT, dispstat);

            processor.UpdateKeyState();

            // Update the rot/scale values
            memory.Bgx[0] = (int)Memory.ReadU32(memory.IORam, Memory.BG2X_L);
            memory.Bgx[1] = (int)Memory.ReadU32(memory.IORam, Memory.BG3X_L);
            memory.Bgy[0] = (int)Memory.ReadU32(memory.IORam, Memory.BG2Y_L);
            memory.Bgy[1] = (int)Memory.ReadU32(memory.IORam, Memory.BG3Y_L);
        }

        public void EnterHBlank(Arm7Processor processor)
        {
            var dispstat = Memory.ReadU16(memory.IORam, Memory.DISPSTAT);
            dispstat |= 1 << 1;
            Memory.WriteU16(memory.IORam, Memory.DISPSTAT, dispstat);

            // Advance the bgx registers
            for (var bg = 0; bg <= 1; bg++)
            {
                var dmx = (short)Memory.ReadU16(memory.IORam, Memory.BG2PB + (uint)bg * 0x10);
                var dmy = (short)Memory.ReadU16(memory.IORam, Memory.BG2PD + (uint)bg * 0x10);
                memory.Bgx[bg] += dmx;
                memory.Bgy[bg] += dmy;
            }

            if (curLine < 160)
            {
                memory.HBlankDma();

                // Trigger hblank irq
                if ((dispstat & (1 << 4)) != 0)
                {
                    processor.RequestIrq(1);
                }
            }
        }

        public void LeaveHBlank(Arm7Processor processor)
        {
            var dispstat = Memory.ReadU16(memory.IORam, Memory.DISPSTAT);
            dispstat &= 0xFFF9;
            Memory.WriteU16(memory.IORam, Memory.DISPSTAT, dispstat);

            // Move to the next line
            curLine++;

            if (curLine >= 228)
            {
                // Start again at the beginning
                curLine = 0;
            }

            // Update registers
            Memory.WriteU16(memory.IORam, Memory.VCOUNT, (ushort)curLine);

            // Check for vblank
            if (curLine == 160)
            {
                EnterVBlank(processor);
            }
            else if (curLine == 0)
            {
                LeaveVBlank(processor);
            }

            // Check y-line trigger
            if (((dispstat >> 8) & 0xff) == curLine)
            {
                dispstat = (ushort)(Memory.ReadU16(memory.IORam, Memory.DISPSTAT) | (1 << 2));
                Memory.WriteU16(memory.IORam, Memory.DISPSTAT, dispstat);

                if ((dispstat & (1 << 5)) != 0)
                {
                    processor.RequestIrq(2);
                }
            }
        }

        public void RenderLine()
        {
            if (curLine < 160)
            {
                renderer.RenderLine(curLine);
            }
        }
    }
}