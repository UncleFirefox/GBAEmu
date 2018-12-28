using System;
using System.Collections.Generic;

namespace GarboDev.CrossCutting
{
    public class Memory
    {
        public const uint REG_BASE = 0x4000000;
        public const uint PAL_BASE = 0x5000000;
        public const uint VRAM_BASE = 0x6000000;
        public const uint OAM_BASE = 0x7000000;

        public const uint DISPCNT = 0x0;
        public const uint DISPSTAT = 0x4;
        public const uint VCOUNT = 0x6;

        public const uint BG0CNT = 0x8;
        public const uint BG1CNT = 0xA;
        public const uint BG2CNT = 0xC;
        public const uint BG3CNT = 0xE;

        public const uint BG0HOFS = 0x10;
        public const uint BG0VOFS = 0x12;
        public const uint BG1HOFS = 0x14;
        public const uint BG1VOFS = 0x16;
        public const uint BG2HOFS = 0x18;
        public const uint BG2VOFS = 0x1A;
        public const uint BG3HOFS = 0x1C;
        public const uint BG3VOFS = 0x1E;

        public const uint BG2PA = 0x20;
        public const uint BG2PB = 0x22;
        public const uint BG2PC = 0x24;
        public const uint BG2PD = 0x26;
        public const uint BG2X_L = 0x28;
        public const uint BG2X_H = 0x2A;
        public const uint BG2Y_L = 0x2C;
        public const uint BG2Y_H = 0x2E;
        public const uint BG3PA = 0x30;
        public const uint BG3PB = 0x32;
        public const uint BG3PC = 0x34;
        public const uint BG3PD = 0x36;
        public const uint BG3X_L = 0x38;
        public const uint BG3X_H = 0x3A;
        public const uint BG3Y_L = 0x3C;
        public const uint BG3Y_H = 0x3E;

        public const uint WIN0H = 0x40;
        public const uint WIN1H = 0x42;
        public const uint WIN0V = 0x44;
        public const uint WIN1V = 0x46;
        public const uint WININ = 0x48;
        public const uint WINOUT = 0x4A;

        public const uint BLDCNT = 0x50;
        public const uint BLDALPHA = 0x52;
        public const uint BLDY = 0x54;

        public const uint SOUNDCNT_L = 0x80;
        public const uint SOUNDCNT_H = 0x82;
        public const uint SOUNDCNT_X = 0x84;

        public const uint FIFO_A_L = 0xA0;
        public const uint FIFO_A_H = 0xA2;
        public const uint FIFO_B_L = 0xA4;
        public const uint FIFO_B_H = 0xA6;

        public const uint DMA0SAD = 0xB0;
        public const uint DMA0DAD = 0xB4;
        public const uint DMA0CNT_L = 0xB8;
        public const uint DMA0CNT_H = 0xBA;
        public const uint DMA1SAD = 0xBC;
        public const uint DMA1DAD = 0xC0;
        public const uint DMA1CNT_L = 0xC4;
        public const uint DMA1CNT_H = 0xC6;
        public const uint DMA2SAD = 0xC8;
        public const uint DMA2DAD = 0xCC;
        public const uint DMA2CNT_L = 0xD0;
        public const uint DMA2CNT_H = 0xD2;
        public const uint DMA3SAD = 0xD4;
        public const uint DMA3DAD = 0xD8;
        public const uint DMA3CNT_L = 0xDC;
        public const uint DMA3CNT_H = 0xDE;

        public const uint TM0D = 0x100;
        public const uint TM0CNT = 0x102;
        public const uint TM1D = 0x104;
        public const uint TM1CNT = 0x106;
        public const uint TM2D = 0x108;
        public const uint TM2CNT = 0x10A;
        public const uint TM3D = 0x10C;
        public const uint TM3CNT = 0x10E;

        public const uint KEYINPUT = 0x130;
        public const uint KEYCNT = 0x132;
        public const uint IE = 0x200;
        public const uint IF = 0x202;
        public const uint IME = 0x208;

        public const uint HALTCNT = 0x300;

        private const uint biosRamMask = 0x3FFF;
        private const uint ewRamMask = 0x3FFFF;
        private const uint iwRamMask = 0x7FFF;
        private const uint ioRegMask = 0x4FF;
        private const uint vRamMask = 0x1FFFF;
        private const uint palRamMask = 0x3FF;
        private const uint oamRamMask = 0x3FF;
        private const uint sRamMask = 0xFFFF;

        private readonly byte[] biosRam = new byte[biosRamMask + 1];
        private readonly byte[] ewRam = new byte[ewRamMask + 1];
        private readonly byte[] iwRam = new byte[iwRamMask + 1];
        private readonly byte[] ioReg = new byte[ioRegMask + 1];
        private readonly byte[] sRam = new byte[sRamMask + 1];

        public byte[] VideoRam { get; } = new byte[vRamMask + 1];

        public byte[] PaletteRam { get; } = new byte[palRamMask + 1];

        public byte[] OamRam { get; } = new byte[oamRamMask + 1];

        public byte[] IORam => ioReg;

        public ushort KeyState { get; set; } = 0x3FF;

        // Processor callbacks
        public Action<int> RequestProcessorIrq { get; set; }
        public Action UpdateProcessorTimers { get; set; }
        public Action HaltProcessor { get; set; }
        public Func<uint> GetProcessorRegister15 { get; set; }
        public Func<bool> ArmProcessorState { get; set; }

        // Sound callbacks
        public Action IncrementSoundFifoA { get; set; }
        public Action IncrementSoundFifoB { get; set; }
        public Action ResetSoundFifoA { get; set; }
        public Action ResetSoundFifoB { get; set; }

        private byte[] romBank1;
        private byte[] romBank2;
        private uint romBank1Mask;
        private uint romBank2Mask;

        private readonly int[] bankSTimes = new int[0x10];
        private int[] _bankNTimes = new int[0x10];

        private int _waitCycles;
        
        public int WaitCycles
        {
            get { var tmp = _waitCycles; _waitCycles = 0; return tmp; }
        }

        private bool _inUnreadable;

        private delegate byte ReadU8Delegate(uint address);
        private delegate void WriteU8Delegate(uint address, byte value);
        private delegate ushort ReadU16Delegate(uint address);
        private delegate void WriteU16Delegate(uint address, ushort value);
        private delegate uint ReadU32Delegate(uint address);
        private delegate void WriteU32Delegate(uint address, uint value);

        private readonly ReadU8Delegate[] ReadU8Funcs;
        private readonly WriteU8Delegate[] WriteU8Funcs;
        private readonly ReadU16Delegate[] ReadU16Funcs;
        private readonly WriteU16Delegate[] WriteU16Funcs;
        private readonly ReadU32Delegate[] ReadU32Funcs;
        private readonly WriteU32Delegate[] WriteU32Funcs;

        private readonly uint[,] dmaRegs = new uint[4, 4];
        private readonly uint[] timerCnt = new uint[4];
        private readonly int[] bgx = new int[2];
        private readonly int[] bgy = new int[2];

        public uint[] TimerCnt => timerCnt;

        public int[] Bgx => bgx;

        public int[] Bgy => bgy;

        public Memory()
        {
            ReadU8Funcs = new ReadU8Delegate[]
                {
                    ReadBiosRam8,
                    ReadNop8,
                    ReadEwRam8,
                    ReadIwRam8,
                    ReadIO8,
                    ReadPalRam8,
                    ReadVRam8,
                    ReadOamRam8,
                    ReadNop8,
                    ReadNop8,
                    ReadNop8,
                    ReadNop8,
                    ReadNop8,
                    ReadNop8,
                    ReadSRam8,
                    ReadNop8
                };

            WriteU8Funcs = new WriteU8Delegate[]
                {
                    WriteNop8,
                    WriteNop8,
                    WriteEwRam8,
                    WriteIwRam8,
                    WriteIO8,
                    WritePalRam8,
                    WriteVRam8,
                    WriteOamRam8,
                    WriteNop8,
                    WriteNop8,
                    WriteNop8,
                    WriteNop8,
                    WriteNop8,
                    WriteNop8,
                    WriteSRam8,
                    WriteNop8
                };

            ReadU16Funcs = new ReadU16Delegate[]
                {
                    ReadBiosRam16,
                    ReadNop16,
                    ReadEwRam16,
                    ReadIwRam16,
                    ReadIO16,
                    ReadPalRam16,
                    ReadVRam16,
                    ReadOamRam16,
                    ReadNop16,
                    ReadNop16,
                    ReadNop16,
                    ReadNop16,
                    ReadNop16,
                    ReadNop16,
                    ReadSRam16,
                    ReadNop16
                };

            WriteU16Funcs = new WriteU16Delegate[]
                {
                    WriteNop16,
                    WriteNop16,
                    WriteEwRam16,
                    WriteIwRam16,
                    WriteIO16,
                    WritePalRam16,
                    WriteVRam16,
                    WriteOamRam16,
                    WriteNop16,
                    WriteNop16,
                    WriteNop16,
                    WriteNop16,
                    WriteNop16,
                    WriteNop16,
                    WriteSRam16,
                    WriteNop16
                };

            ReadU32Funcs = new ReadU32Delegate[]
                {
                    ReadBiosRam32,
                    ReadNop32,
                    ReadEwRam32,
                    ReadIwRam32,
                    ReadIO32,
                    ReadPalRam32,
                    ReadVRam32,
                    ReadOamRam32,
                    ReadNop32,
                    ReadNop32,
                    ReadNop32,
                    ReadNop32,
                    ReadNop32,
                    ReadNop32,
                    ReadSRam32,
                    ReadNop32
                };

            WriteU32Funcs = new WriteU32Delegate[]
                {
                    WriteNop32,
                    WriteNop32,
                    WriteEwRam32,
                    WriteIwRam32,
                    WriteIO32,
                    WritePalRam32,
                    WriteVRam32,
                    WriteOamRam32,
                    WriteNop32,
                    WriteNop32,
                    WriteNop32,
                    WriteNop32,
                    WriteNop32,
                    WriteNop32,
                    WriteSRam32,
                    WriteNop32
                };
        }

        public void Reset()
        {
            Array.Clear(ewRam, 0, ewRam.Length);
            Array.Clear(iwRam, 0, iwRam.Length);
            Array.Clear(ioReg, 0, ioReg.Length);
            Array.Clear(VideoRam, 0, VideoRam.Length);
            Array.Clear(PaletteRam, 0, PaletteRam.Length);
            Array.Clear(OamRam, 0, OamRam.Length);
            Array.Clear(sRam, 0, sRam.Length);

            WriteU16(ioReg, BG2PA, 0x0100);
            WriteU16(ioReg, BG2PD, 0x0100);
            WriteU16(ioReg, BG3PA, 0x0100);
            WriteU16(ioReg, BG3PD, 0x0100);
        }

        public void HBlankDma()
        {
            for (var i = 0; i < 4; i++)
            {
                if (((dmaRegs[i, 3] >> 12) & 0x3) == 2)
                {
                    DmaTransfer(i);
                }
            }
        }

        public void VBlankDma()
        {
            for (var i = 0; i < 4; i++)
            {
                if (((dmaRegs[i, 3] >> 12) & 0x3) == 1)
                {
                    DmaTransfer(i);
                }
            }
        }

        public void FifoDma(int channel)
        {
            if (((dmaRegs[channel, 3] >> 12) & 0x3) == 0x3)
            {
                DmaTransfer(channel);
            }
        }

        public void DmaTransfer(int channel)
        {
            // Check if DMA is enabled
            if ((dmaRegs[channel, 3] & (1 << 15)) != 0)
            {
                var wideTransfer = (dmaRegs[channel, 3] & (1 << 10)) != 0;

                uint srcDirection = 0, destDirection = 0;
                var reload = false;

                switch ((dmaRegs[channel, 3] >> 5) & 0x3)
                {
                    case 0: destDirection = 1; break;
                    case 1: destDirection = 0xFFFFFFFF; break;
                    case 2: destDirection = 0; break;
                    case 3: destDirection = 1; reload = true;  break;
                }

                switch ((dmaRegs[channel, 3] >> 7) & 0x3)
                {
                    case 0: srcDirection = 1; break;
                    case 1: srcDirection = 0xFFFFFFFF; break;
                    case 2: srcDirection = 0; break;
                    case 3: if (channel == 3)
                        {
                            // TODO
                            return;
                        }
                        throw new Exception("Unhandled DMA mode.");
                }

                var numElements = (int)dmaRegs[channel, 2];
                if (numElements == 0) numElements = 0x4000;

                if (((dmaRegs[channel, 3] >> 12) & 0x3) == 0x3)
                {
                    // Sound FIFO mode
                    wideTransfer = true;
                    destDirection = 0;
                    numElements = 4;
                    reload = false;
                }

                if (wideTransfer)
                {
                    srcDirection *= 4;
                    destDirection *= 4;
                    while (numElements-- > 0)
                    {
                        WriteU32(dmaRegs[channel, 1], ReadU32(dmaRegs[channel, 0]));
                        dmaRegs[channel, 1] += destDirection;
                        dmaRegs[channel, 0] += srcDirection;
                    }
                }
                else
                {
                    srcDirection *= 2;
                    destDirection *= 2;
                    while (numElements-- > 0)
                    {
                        WriteU16(dmaRegs[channel, 1], ReadU16(dmaRegs[channel, 0]));
                        dmaRegs[channel, 1] += destDirection;
                        dmaRegs[channel, 0] += srcDirection;
                    }
                }

                // If not a repeating DMA, then disable the DMA
                if ((dmaRegs[channel, 3] & (1 << 9)) == 0)
                {
                    dmaRegs[channel, 3] &= 0x7FFF;
                }
                else
                {
                    // Reload dest and count
                    switch (channel)
                    {
                        case 0:
                            if (reload) dmaRegs[0, 1] = ReadU32(ioReg, DMA0DAD) & 0x07FFFFFF;
                            dmaRegs[0, 2] = ReadU16(ioReg, DMA0CNT_L);
                            break;
                        case 1:
                            if (reload) dmaRegs[1, 1] = ReadU32(ioReg, DMA1DAD) & 0x07FFFFFF;
                            dmaRegs[1, 2] = ReadU16(ioReg, DMA1CNT_L);
                            break;
                        case 2:
                            if (reload) dmaRegs[2, 1] = ReadU32(ioReg, DMA2DAD) & 0x07FFFFFF;
                            dmaRegs[2, 2] = ReadU16(ioReg, DMA2CNT_L);
                            break;
                        case 3:
                            if (reload) dmaRegs[3, 1] = ReadU32(ioReg, DMA3DAD) & 0x0FFFFFFF;
                            dmaRegs[3, 2] = ReadU16(ioReg, DMA3CNT_L);
                            break;
                    }
                }

                if ((dmaRegs[channel, 3] & (1 << 14)) != 0)
                {
                    RequestProcessorIrq(8 + channel);
                }
            }
        }

        public void WriteDmaControl(int channel)
        {
            switch (channel)
            {
                case 0:
                    if (((dmaRegs[0, 3] ^ ReadU16(ioReg, DMA0CNT_H)) & (1 << 15)) == 0) return;
                    dmaRegs[0, 0] = ReadU32(ioReg, DMA0SAD) & 0x07FFFFFF;
                    dmaRegs[0, 1] = ReadU32(ioReg, DMA0DAD) & 0x07FFFFFF;
                    dmaRegs[0, 2] = ReadU16(ioReg, DMA0CNT_L);
                    dmaRegs[0, 3] = ReadU16(ioReg, DMA0CNT_H);
                    break;
                case 1:
                    if (((dmaRegs[1, 3] ^ ReadU16(ioReg, DMA1CNT_H)) & (1 << 15)) == 0) return;
                    dmaRegs[1, 0] = ReadU32(ioReg, DMA1SAD) & 0x0FFFFFFF;
                    dmaRegs[1, 1] = ReadU32(ioReg, DMA1DAD) & 0x07FFFFFF;
                    dmaRegs[1, 2] = ReadU16(ioReg, DMA1CNT_L);
                    dmaRegs[1, 3] = ReadU16(ioReg, DMA1CNT_H);
                    break;
                case 2:
                    if (((dmaRegs[2, 3] ^ ReadU16(ioReg, DMA2CNT_H)) & (1 << 15)) == 0) return;
                    dmaRegs[2, 0] = ReadU32(ioReg, DMA2SAD) & 0x0FFFFFFF;
                    dmaRegs[2, 1] = ReadU32(ioReg, DMA2DAD) & 0x07FFFFFF;
                    dmaRegs[2, 2] = ReadU16(ioReg, DMA2CNT_L);
                    dmaRegs[2, 3] = ReadU16(ioReg, DMA2CNT_H);
                    break;
                case 3:
                    if (((dmaRegs[3, 3] ^ ReadU16(ioReg, DMA3CNT_H)) & (1 << 15)) == 0) return;
                    dmaRegs[3, 0] = ReadU32(ioReg, DMA3SAD) & 0x0FFFFFFF;
                    dmaRegs[3, 1] = ReadU32(ioReg, DMA3DAD) & 0x0FFFFFFF;
                    dmaRegs[3, 2] = ReadU16(ioReg, DMA3CNT_L);
                    dmaRegs[3, 3] = ReadU16(ioReg, DMA3CNT_H);
                    break;
            }

            // Channel start timing
            switch ((dmaRegs[channel, 3] >> 12) & 0x3)
            {
                case 0:
                    // Start immediately
                    DmaTransfer(channel);
                    break;
                case 1:
                case 2:
                    // Hblank and Vblank DMA's
                    break;
                case 3:
                    // TODO (DMA sound)
                    return;
            }
        }

        private void WriteTimerControl(int timer, ushort newCnt)
        {
            var control = ReadU16(ioReg, TM0CNT + (uint)(timer * 4));
            uint count = ReadU16(ioReg, TM0D + (uint)(timer * 4));

            if ((newCnt & (1 << 7)) != 0 && (control & (1 << 7)) == 0)
            {
                timerCnt[timer] = count << 10;
            }
        }

        #region Read/Write Helpers
        public static ushort ReadU16(byte[] array, uint position)
        {
            return (ushort)(array[position] | (array[position + 1] << 8));
        }

        public static uint ReadU32(byte[] array, uint position)
        {
            return (uint)(array[position] | (array[position + 1] << 8) |
                          (array[position + 2] << 16) | (array[position + 3] << 24));
        }

        public static void WriteU16(byte[] array, uint position, ushort value)
        {
            array[position] = (byte)(value & 0xff);
            array[position + 1] = (byte)(value >> 8);
        }

        public static void WriteU32(byte[] array, uint position, uint value)
        {
            array[position] = (byte)(value & 0xff);
            array[position + 1] = (byte)((value >> 8) & 0xff);
            array[position + 2] = (byte)((value >> 16) & 0xff);
            array[position + 3] = (byte)(value >> 24);
        }
        #endregion

        #region Memory Reads
        private uint ReadUnreadable()
        {
            if (_inUnreadable)
            {
                return 0;
            }

            _inUnreadable = true;

            uint res;

            if (ArmProcessorState())
            {
                res = ReadU32(GetProcessorRegister15());
            }
            else
            {
                var val = ReadU16(GetProcessorRegister15());
                res = (uint)(val | (val << 16));
            }

            _inUnreadable = false;

            return res;
        }

        private byte ReadNop8(uint address)
        {
            return (byte)(ReadUnreadable() & 0xFF);
        }

        private ushort ReadNop16(uint address)
        {
            return (ushort)(ReadUnreadable() & 0xFFFF);
        }

        private uint ReadNop32(uint address)
        {
            return ReadUnreadable();
        }

        private byte ReadBiosRam8(uint address)
        {
            _waitCycles++;
            if (GetProcessorRegister15() < 0x01000000)
            {
                return biosRam[address & biosRamMask];
            }
            return (byte)(ReadUnreadable() & 0xFF);
        }

        private ushort ReadBiosRam16(uint address)
        {
            _waitCycles++;
            if (GetProcessorRegister15() < 0x01000000)
            {
                return ReadU16(biosRam, address & biosRamMask);
            }
            return (ushort)(ReadUnreadable() & 0xFFFF);
        }

        private uint ReadBiosRam32(uint address)
        {
            _waitCycles++;
            if (GetProcessorRegister15() < 0x01000000)
            {
                return ReadU32(biosRam, address & biosRamMask);
            }
            return ReadUnreadable();
        }

        private byte ReadEwRam8(uint address)
        {
            _waitCycles += 3;
            return ewRam[address & ewRamMask];
        }

        private ushort ReadEwRam16(uint address)
        {
            _waitCycles += 3;
            return ReadU16(ewRam, address & ewRamMask);
        }

        private uint ReadEwRam32(uint address)
        {
            _waitCycles += 6;
            return ReadU32(ewRam, address & ewRamMask);
        }

        private byte ReadIwRam8(uint address)
        {
            _waitCycles++;
            return iwRam[address & iwRamMask];
        }

        private ushort ReadIwRam16(uint address)
        {
            _waitCycles++;
            return ReadU16(iwRam, address & iwRamMask);
        }

        private uint ReadIwRam32(uint address)
        {
            _waitCycles++;
            return ReadU32(iwRam, address & iwRamMask);
        }

        private byte ReadIO8(uint address)
        {
            _waitCycles++;
            address &= 0xFFFFFF;
            if (address >= ioRegMask) return 0;

            switch (address)
            {
                case KEYINPUT:
                    return (byte)(KeyState & 0xFF);
                case KEYINPUT + 1:
                    return (byte)(KeyState >> 8);

                case DMA0CNT_H:
                    return (byte)(dmaRegs[0, 3] & 0xFF);
                case DMA0CNT_H + 1:
                    return (byte)(dmaRegs[0, 3] >> 8);
                case DMA1CNT_H:
                    return (byte)(dmaRegs[1, 3] & 0xFF);
                case DMA1CNT_H + 1:
                    return (byte)(dmaRegs[1, 3] >> 8);
                case DMA2CNT_H:
                    return (byte)(dmaRegs[2, 3] & 0xFF);
                case DMA2CNT_H + 1:
                    return (byte)(dmaRegs[2, 3] >> 8);
                case DMA3CNT_H:
                    return (byte)(dmaRegs[3, 3] & 0xFF);
                case DMA3CNT_H + 1:
                    return (byte)(dmaRegs[3, 3] >> 8);

                case TM0D:
                    UpdateProcessorTimers();
                    return (byte)((timerCnt[0] >> 10) & 0xFF);
                case TM0D + 1:
                    UpdateProcessorTimers();
                    return (byte)((timerCnt[0] >> 10) >> 8);
                case TM1D:
                    UpdateProcessorTimers();
                    return (byte)((timerCnt[1] >> 10) & 0xFF);
                case TM1D + 1:
                    UpdateProcessorTimers();
                    return (byte)((timerCnt[1] >> 10) >> 8);
                case TM2D:
                    UpdateProcessorTimers();
                    return (byte)((timerCnt[2] >> 10) & 0xFF);
                case TM2D + 1:
                    UpdateProcessorTimers();
                    return (byte)((timerCnt[2] >> 10) >> 8);
                case TM3D:
                    UpdateProcessorTimers();
                    return (byte)((timerCnt[3] >> 10) & 0xFF);
                case TM3D + 1:
                    UpdateProcessorTimers();
                    return (byte)((timerCnt[3] >> 10) >> 8);

                default:
                    return ioReg[address];
            }
        }

        private ushort ReadIO16(uint address)
        {
            _waitCycles++;
            address &= 0xFFFFFF;
            if (address >= ioRegMask) return 0;

            switch (address)
            {
                case KEYINPUT:
                    return KeyState;

                case DMA0CNT_H:
                    return (ushort)dmaRegs[0, 3];
                case DMA1CNT_H:
                    return (ushort)dmaRegs[1, 3];
                case DMA2CNT_H:
                    return (ushort)dmaRegs[2, 3];
                case DMA3CNT_H:
                    return (ushort)dmaRegs[3, 3];

                case TM0D:
                    UpdateProcessorTimers();
                    return (ushort)((timerCnt[0] >> 10) & 0xFFFF);
                case TM1D:
                    UpdateProcessorTimers();
                    return (ushort)((timerCnt[1] >> 10) & 0xFFFF);
                case TM2D:
                    UpdateProcessorTimers();
                    return (ushort)((timerCnt[2] >> 10) & 0xFFFF);
                case TM3D:
                    UpdateProcessorTimers();
                    return (ushort)((timerCnt[3] >> 10) & 0xFFFF);

                default:
                    return ReadU16(ioReg, address);
            }
        }

        private uint ReadIO32(uint address)
        {
            _waitCycles++;
            address &= 0xFFFFFF;
            if (address >= ioRegMask) return 0;

            switch (address)
            {
                case KEYINPUT:
                    return KeyState | ((uint)ReadU16(ioReg, address + 0x2) << 16);

                case DMA0CNT_L:
                    return ReadU16(ioReg, address) | (dmaRegs[0, 3] << 16);
                case DMA1CNT_L:
                    return ReadU16(ioReg, address) | (dmaRegs[1, 3] << 16);
                case DMA2CNT_L:
                    return ReadU16(ioReg, address) | (dmaRegs[2, 3] << 16);
                case DMA3CNT_L:
                    return ReadU16(ioReg, address) | (dmaRegs[3, 3] << 16);

                case TM0D:
                    UpdateProcessorTimers();
                    return ((timerCnt[0] >> 10) & 0xFFFF) | (uint)(ReadU16(ioReg, address + 2) << 16);
                case TM1D:
                    UpdateProcessorTimers();
                    return ((timerCnt[1] >> 10) & 0xFFFF) | (uint)(ReadU16(ioReg, address + 2) << 16);
                case TM2D:
                    UpdateProcessorTimers();
                    return ((timerCnt[2] >> 10) & 0xFFFF) | (uint)(ReadU16(ioReg, address + 2) << 16);
                case TM3D:
                    UpdateProcessorTimers();
                    return ((timerCnt[3] >> 10) & 0xFFFF) | (uint)(ReadU16(ioReg, address + 2) << 16);

                default:
                    return ReadU32(ioReg, address);
            }
        }

        private byte ReadPalRam8(uint address)
        {
            _waitCycles++;
            return PaletteRam[address & palRamMask];
        }

        private ushort ReadPalRam16(uint address)
        {
            _waitCycles++;
            return ReadU16(PaletteRam, address & palRamMask);
        }

        private uint ReadPalRam32(uint address)
        {
            _waitCycles += 2;
            return ReadU32(PaletteRam, address & palRamMask);
        }

        private byte ReadVRam8(uint address)
        {
            _waitCycles++;
            address &= vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            return VideoRam[address];
        }

        private ushort ReadVRam16(uint address)
        {
            _waitCycles++;
            address &= vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            return ReadU16(VideoRam, address);
        }

        private uint ReadVRam32(uint address)
        {
            _waitCycles += 2;
            address &= vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            return ReadU32(VideoRam, address & vRamMask);
        }

        private byte ReadOamRam8(uint address)
        {
            _waitCycles++;
            return OamRam[address & oamRamMask];
        }

        private ushort ReadOamRam16(uint address)
        {
            _waitCycles++;
            return ReadU16(OamRam, address & oamRamMask);
        }

        private uint ReadOamRam32(uint address)
        {
            _waitCycles++;
            return ReadU32(OamRam, address & oamRamMask);
        }

        private byte ReadRom1_8(uint address)
        {
            _waitCycles += bankSTimes[(address >> 24) & 0xf];
            return romBank1[address & romBank1Mask];
        }

        private ushort ReadRom1_16(uint address)
        {
            _waitCycles += bankSTimes[(address >> 24) & 0xf];
            return ReadU16(romBank1, address & romBank1Mask);
        }

        private uint ReadRom1_32(uint address)
        {
            _waitCycles += bankSTimes[(address >> 24) & 0xf] * 2 + 1;
            return ReadU32(romBank1, address & romBank1Mask);
        }

        private byte ReadRom2_8(uint address)
        {
            _waitCycles += bankSTimes[(address >> 24) & 0xf];
            return romBank2[address & romBank2Mask];
        }

        private ushort ReadRom2_16(uint address)
        {
            _waitCycles += bankSTimes[(address >> 24) & 0xf];
            return ReadU16(romBank2, address & romBank2Mask);
        }

        private uint ReadRom2_32(uint address)
        {
            _waitCycles += bankSTimes[(address >> 24) & 0xf] * 2 + 1;
            return ReadU32(romBank2, address & romBank2Mask);
        }

        private byte ReadSRam8(uint address)
        {
            return sRam[address & sRamMask];
        }

        private ushort ReadSRam16(uint address)
        {
            // TODO
            return 0;
        }

        private uint ReadSRam32(uint address)
        {
            // TODO
            return 0;
        }
        #endregion

        #region Memory Writes
        private void WriteNop8(uint address, byte value)
        {
        }

        private void WriteNop16(uint address, ushort value)
        {
        }

        private void WriteNop32(uint address, uint value)
        {
        }

        private void WriteEwRam8(uint address, byte value)
        {
            _waitCycles += 3;
            ewRam[address & ewRamMask] = value;
        }

        private void WriteEwRam16(uint address, ushort value)
        {
            _waitCycles += 3;
            WriteU16(ewRam, address & ewRamMask, value);
        }

        private void WriteEwRam32(uint address, uint value)
        {
            _waitCycles += 6;
            WriteU32(ewRam, address & ewRamMask, value);
        }

        private void WriteIwRam8(uint address, byte value)
        {
            _waitCycles++;
            iwRam[address & iwRamMask] = value;
        }

        private void WriteIwRam16(uint address, ushort value)
        {
            _waitCycles++;
            WriteU16(iwRam, address & iwRamMask, value);
        }

        private void WriteIwRam32(uint address, uint value)
        {
            _waitCycles++;
            WriteU32(iwRam, address & iwRamMask, value);
        }

        private void WriteIO8(uint address, byte value)
        {
            _waitCycles++;
            address &= 0xFFFFFF;
            if (address >= ioRegMask) return;

            switch (address)
            {
                case BG2X_L:
                case BG2X_L + 1:
                case BG2X_L + 2:
                case BG2X_L + 3:
                    {
                        ioReg[address] = value;
                        var tmp = ReadU32(ioReg, BG2X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG2X_L, tmp);

                        bgx[0] = (int)tmp;
                    }
                    break;

                case BG3X_L:
                case BG3X_L + 1:
                case BG3X_L + 2:
                case BG3X_L + 3:
                    {
                        ioReg[address] = value;
                        var tmp = ReadU32(ioReg, BG3X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG3X_L, tmp);

                        bgx[1] = (int)tmp;
                    }
                    break;

                case BG2Y_L:
                case BG2Y_L + 1:
                case BG2Y_L + 2:
                case BG2Y_L + 3:
                    {
                        ioReg[address] = value;
                        var tmp = ReadU32(ioReg, BG2Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG2Y_L, tmp);

                        bgy[0] = (int)tmp;
                    }
                    break;

                case BG3Y_L:
                case BG3Y_L + 1:
                case BG3Y_L + 2:
                case BG3Y_L + 3:
                    {
                        ioReg[address] = value;
                        var tmp = ReadU32(ioReg, BG3Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG3Y_L, tmp);

                        bgy[1] = (int)tmp;
                    }
                    break;

                case DMA0CNT_H:
                case DMA0CNT_H + 1:
                    ioReg[address] = value;
                    WriteDmaControl(0);
                    break;

                case DMA1CNT_H:
                case DMA1CNT_H + 1:
                    ioReg[address] = value;
                    WriteDmaControl(1);
                    break;

                case DMA2CNT_H:
                case DMA2CNT_H + 1:
                    ioReg[address] = value;
                    WriteDmaControl(2);
                    break;

                case DMA3CNT_H:
                case DMA3CNT_H + 1:
                    ioReg[address] = value;
                    WriteDmaControl(3);
                    break;

                case TM0CNT:
                case TM0CNT + 1:
                    {
                        var oldCnt = ReadU16(ioReg, TM0CNT);
                        ioReg[address] = value;
                        WriteTimerControl(0, oldCnt);
                    }
                    break;

                case TM1CNT:
                case TM1CNT + 1:
                    {
                        var oldCnt = ReadU16(ioReg, TM1CNT);
                        ioReg[address] = value;
                        WriteTimerControl(1, oldCnt);
                    }
                    break;

                case TM2CNT:
                case TM2CNT + 1:
                    {
                        var oldCnt = ReadU16(ioReg, TM2CNT);
                        ioReg[address] = value;
                        WriteTimerControl(2, oldCnt);
                    }
                    break;

                case TM3CNT:
                case TM3CNT + 1:
                    {
                        var oldCnt = ReadU16(ioReg, TM3CNT);
                        ioReg[address] = value;
                        WriteTimerControl(3, oldCnt);
                    }
                    break;

                case FIFO_A_L:
                case FIFO_A_L+1:
                case FIFO_A_H:
                case FIFO_A_H+1:
                    ioReg[address] = value;
                    IncrementSoundFifoA();
                    break;

                case FIFO_B_L:
                case FIFO_B_L + 1:
                case FIFO_B_H:
                case FIFO_B_H + 1:
                    ioReg[address] = value;
                    IncrementSoundFifoB();
                    break;

                case IF:
                case IF + 1:
                    ioReg[address] &= (byte)~value;
                    break;

                case HALTCNT + 1:
                    ioReg[address] = value;
                    HaltProcessor();
                    break;

                default:
                    ioReg[address] = value;
                    break;
            }
        }

        private void WriteIO16(uint address, ushort value)
        {
            _waitCycles++;
            address &= 0xFFFFFF;
            if (address >= ioRegMask) return;

            switch (address)
            {
                case BG2X_L:
                case BG2X_L + 2:
                    {
                        WriteU16(ioReg, address, value);
                        var tmp = ReadU32(ioReg, BG2X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG2X_L, tmp);

                        bgx[0] = (int)tmp;
                    }
                    break;

                case BG3X_L:
                case BG3X_L + 2:
                    {
                        WriteU16(ioReg, address, value);
                        var tmp = ReadU32(ioReg, BG3X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG3X_L, tmp);

                        bgx[1] = (int)tmp;
                    }
                    break;

                case BG2Y_L:
                case BG2Y_L + 2:
                    {
                        WriteU16(ioReg, address, value);
                        var tmp = ReadU32(ioReg, BG2Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG2Y_L, tmp);

                        bgy[0] = (int)tmp;
                    }
                    break;

                case BG3Y_L:
                case BG3Y_L + 2:
                    {
                        WriteU16(ioReg, address, value);
                        var tmp = ReadU32(ioReg, BG3Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG3Y_L, tmp);

                        bgy[1] = (int)tmp;
                    }
                    break;

                case DMA0CNT_H:
                    WriteU16(ioReg, address, value);
                    WriteDmaControl(0);
                    break;

                case DMA1CNT_H:
                    WriteU16(ioReg, address, value);
                    WriteDmaControl(1);
                    break;

                case DMA2CNT_H:
                    WriteU16(ioReg, address, value);
                    WriteDmaControl(2);
                    break;

                case DMA3CNT_H:
                    WriteU16(ioReg, address, value);
                    WriteDmaControl(3);
                    break;

                case TM0CNT:
                    {
                        var oldCnt = ReadU16(ioReg, TM0CNT);
                        WriteU16(ioReg, address, value);
                        WriteTimerControl(0, oldCnt);
                    }
                    break;

                case TM1CNT:
                    {
                        var oldCnt = ReadU16(ioReg, TM1CNT);
                        WriteU16(ioReg, address, value);
                        WriteTimerControl(1, oldCnt);
                    }
                    break;

                case TM2CNT:
                    {
                        var oldCnt = ReadU16(ioReg, TM2CNT);
                        WriteU16(ioReg, address, value);
                        WriteTimerControl(2, oldCnt);
                    }
                    break;

                case TM3CNT:
                    {
                        var oldCnt = ReadU16(ioReg, TM3CNT);
                        WriteU16(ioReg, address, value);
                        WriteTimerControl(3, oldCnt);
                    }
                    break;

                case FIFO_A_L:
                case FIFO_A_H:
                    WriteU16(ioReg, address, value);
                    IncrementSoundFifoA();
                    break;

                case FIFO_B_L:
                case FIFO_B_H:
                    WriteU16(ioReg, address, value);
                    IncrementSoundFifoB();
                    break;

                case SOUNDCNT_H:
                    WriteU16(ioReg, address, value);
                    if ((value & (1 << 11)) != 0)
                    {
                        ResetSoundFifoA();
                    }
                    if ((value & (1 << 15)) != 0)
                    {
                        ResetSoundFifoB();
                    }
                    break;

                case IF:
                    {
                        var tmp = ReadU16(ioReg, address);
                        WriteU16(ioReg, address, (ushort)(tmp & (~value)));
                    }
                    break;

                case HALTCNT:
                    WriteU16(ioReg, address, value);
                    HaltProcessor();
                    break;

                default:
                    WriteU16(ioReg, address, value);
                    break;
            }
        }

        private void WriteIO32(uint address, uint value)
        {
            _waitCycles++;
            address &= 0xFFFFFF;
            if (address >= ioRegMask) return;

            switch (address)
            {
                case BG2X_L:
                    {
                        WriteU32(ioReg, address, value);
                        var tmp = ReadU32(ioReg, BG2X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG2X_L, tmp);

                        bgx[0] = (int)tmp;
                    }
                    break;

                case BG3X_L:
                    {
                        WriteU32(ioReg, address, value);
                        var tmp = ReadU32(ioReg, BG3X_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG3X_L, tmp);

                        bgx[1] = (int)tmp;
                    }
                    break;

                case BG2Y_L:
                    {
                        WriteU32(ioReg, address, value);
                        var tmp = ReadU32(ioReg, BG2Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG2Y_L, tmp);

                        bgy[0] = (int)tmp;
                    }
                    break;

                case BG3Y_L:
                    {
                        WriteU32(ioReg, address, value);
                        var tmp = ReadU32(ioReg, BG3Y_L);
                        if ((tmp & (1 << 27)) != 0) tmp |= 0xF0000000;
                        WriteU32(ioReg, BG3Y_L, tmp);

                        bgy[1] = (int)tmp;
                    }
                    break;

                case DMA0CNT_L:
                    WriteU32(ioReg, address, value);
                    WriteDmaControl(0);
                    break;

                case DMA1CNT_L:
                    WriteU32(ioReg, address, value);
                    WriteDmaControl(1);
                    break;

                case DMA2CNT_L:
                    WriteU32(ioReg, address, value);
                    WriteDmaControl(2);
                    break;

                case DMA3CNT_L:
                    WriteU32(ioReg, address, value);
                    WriteDmaControl(3);
                    break;

                case TM0D:
                    {
                        var oldCnt = ReadU16(ioReg, TM0CNT);
                        WriteU32(ioReg, address, value);
                        WriteTimerControl(0, oldCnt);
                    }
                    break;

                case TM1D:
                    {
                        var oldCnt = ReadU16(ioReg, TM1CNT);
                        WriteU32(ioReg, address, value);
                        WriteTimerControl(1, oldCnt);
                    }
                    break;

                case TM2D:
                    {
                        var oldCnt = ReadU16(ioReg, TM2CNT);
                        WriteU32(ioReg, address, value);
                        WriteTimerControl(2, oldCnt);
                    }
                    break;

                case TM3D:
                    {
                        var oldCnt = ReadU16(ioReg, TM3CNT);
                        WriteU32(ioReg, address, value);
                        WriteTimerControl(3, oldCnt);
                    }
                    break;

                case FIFO_A_L:
                    WriteU32(ioReg, address, value);
                    IncrementSoundFifoA();
                    break;

                case FIFO_B_L:
                    WriteU32(ioReg, address, value);
                    IncrementSoundFifoB();
                    break;

                case SOUNDCNT_L:
                    WriteU32(ioReg, address, value);
                    if (((value >> 16) & (1 << 11)) != 0)
                    {
                        ResetSoundFifoA();
                    }
                    if (((value >> 16) & (1 << 15)) != 0)
                    {
                        ResetSoundFifoB();
                    }
                    break;

                case IE:
                    {
                        var tmp = ReadU32(ioReg, address);
                        WriteU32(ioReg, address, (value & 0xFFFF) | (tmp & (~(value & 0xFFFF0000))));
                    }
                    break;

                case HALTCNT:
                    WriteU32(ioReg, address, value);
                    HaltProcessor();
                    break;

                default:
                    WriteU32(ioReg, address, value);
                    break;
            }
        }

        private void WritePalRam8(uint address, byte value)
        {
            _waitCycles++;
            address &= palRamMask & ~1U;
            PaletteRam[address] = value;
            PaletteRam[address + 1] = value;
        }

        private void WritePalRam16(uint address, ushort value)
        {
            _waitCycles++;
            WriteU16(PaletteRam, address & palRamMask, value);
        }

        private void WritePalRam32(uint address, uint value)
        {
            _waitCycles += 2;
            WriteU32(PaletteRam, address & palRamMask, value);
        }

        private void WriteVRam8(uint address, byte value)
        {
            _waitCycles++;
            address &= vRamMask & ~1U;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            VideoRam[address] = value;
            VideoRam[address + 1] = value;
        }

        private void WriteVRam16(uint address, ushort value)
        {
            _waitCycles++;
            address &= vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            WriteU16(VideoRam, address, value);
        }

        private void WriteVRam32(uint address, uint value)
        {
            _waitCycles += 2;
            address &= vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            WriteU32(VideoRam, address, value);
        }

        private void WriteOamRam8(uint address, byte value)
        {
            _waitCycles++;
            address &= oamRamMask & ~1U;

            OamRam[address] = value;
            OamRam[address + 1] = value;
        }

        private void WriteOamRam16(uint address, ushort value)
        {
            _waitCycles++;
            WriteU16(OamRam, address & oamRamMask, value);
        }

        private void WriteOamRam32(uint address, uint value)
        {
            _waitCycles++;
            WriteU32(OamRam, address & oamRamMask, value);
        }

        private void WriteSRam8(uint address, byte value)
        {
            sRam[address & sRamMask] = value;
        }

        private void WriteSRam16(uint address, ushort value)
        {
            // TODO
        }

        private void WriteSRam32(uint address, uint value)
        {
            // TODO
        }

        private enum EepromModes
        {
            Idle,
            ReadData
        }

        private EepromModes eepromMode = EepromModes.Idle;
        private readonly byte[] eeprom = new byte[0xffff];
        private readonly byte[] eepromStore = new byte[0xff];
        private int curEepromByte;
        private int eepromReadAddress = -1;

        private void WriteEeprom8(uint address, byte value)
        {
            // EEPROM writes must be done by DMA 3
            if ((dmaRegs[3, 3] & (1 << 15)) == 0) return;
            // 0 length eeprom writes are bad
            if (dmaRegs[3, 2] == 0) return;

            if (eepromMode != EepromModes.ReadData)
            {
                curEepromByte = 0;
                eepromMode = EepromModes.ReadData;
                eepromReadAddress = -1;

                for (var i = 0; i < eepromStore.Length; i++) eepromStore[i] = 0;
            }

            eepromStore[curEepromByte >> 3] |= (byte)(value << (7 - (curEepromByte & 0x7)));
            curEepromByte++;

            if (curEepromByte == dmaRegs[3, 2])
            {
                if ((eepromStore[0] & 0x80) == 0) return;

                if ((eepromStore[0] & 0x40) != 0)
                {
                    // Read request
                    if (curEepromByte == 9)
                    {
                        eepromReadAddress = eepromStore[0] & 0x3F;
                    }
                    else
                    {
                        eepromReadAddress = ((eepromStore[0] & 0x3F) << 8) | eepromStore[1];
                    }
                    
                    curEepromByte = 0;
                }
                else
                {
                    // Write request
                    int eepromAddress, offset;
                    if (curEepromByte == 64 + 9)
                    {
                        eepromAddress = eepromStore[0] & 0x3F;
                        offset = 1;
                    }
                    else
                    {
                        eepromAddress = ((eepromStore[0] & 0x3F) << 8) | eepromStore[1];
                        offset = 2;
                    }

                    for (var i = 0; i < 8; i++)
                    {
                        eeprom[eepromAddress * 8 + i] = eepromStore[i + offset];
                    }

                    eepromMode = EepromModes.Idle;
                }
            }
        }

        private void WriteEeprom16(uint address, ushort value)
        {
            WriteEeprom8(address, (byte)(value & 0xff));
        }

        private void WriteEeprom32(uint address, uint value)
        {
            WriteEeprom8(address, (byte)(value & 0xff));
        }

        private byte ReadEeprom8(uint address)
        {
            if (eepromReadAddress == -1) return 1;

            byte retval = 0;

            if (curEepromByte >= 4)
            {
                retval = (byte)((eeprom[eepromReadAddress * 8 + ((curEepromByte - 4) / 8)] >> (7 - ((curEepromByte - 4) & 7))) & 1);
            }

            curEepromByte++;

            if (curEepromByte == dmaRegs[3, 2])
            {
                eepromReadAddress = -1;
                eepromMode = EepromModes.Idle;
            }

            return retval;
        }

        private ushort ReadEeprom16(uint address)
        {
            return ReadEeprom8(address);
        }

        private uint ReadEeprom32(uint address)
        {
            return ReadEeprom8(address);
        }
        #endregion

        #region Shader Renderer Vram Writes
        private List<uint> vramUpdated = new List<uint>();
        private List<uint> palUpdated = new List<uint>();
        public const int VramBlockSize = 64;
        public const int PalBlockSize = 32;
        private readonly bool[] vramHit = new bool[(vRamMask + 1) / VramBlockSize];
        private readonly bool[] palHit = new bool[(palRamMask + 1) / PalBlockSize];

        public List<uint> VramUpdated
        {
            get
            {
                var old = vramUpdated;
                for (var i = 0; i < old.Count; i++)
                {
                    vramHit[old[i]] = false;
                }
                vramUpdated = new List<uint>();
                return old;
            }
        }


        public List<uint> PalUpdated
        {
            get
            {
                var old = palUpdated;
                for (var i = 0; i < old.Count; i++)
                {
                    palHit[old[i]] = false;
                }
                palUpdated = new List<uint>();
                return old;
            }
        }

        private void UpdatePal(uint address)
        {
            var index = address / PalBlockSize;
            if (!palHit[index])
            {
                palHit[index] = true;
                palUpdated.Add(index);
            }
        }

        private void UpdateVram(uint address)
        {
            var index = address / VramBlockSize;
            if (!vramHit[index])
            {
                vramHit[index] = true;
                vramUpdated.Add(index);
            }
        }

        private void ShaderWritePalRam8(uint address, byte value)
        {
            _waitCycles++;
            address &= palRamMask & ~1U;
            PaletteRam[address] = value;
            PaletteRam[address + 1] = value;

            UpdatePal(address);
        }

        private void ShaderWritePalRam16(uint address, ushort value)
        {
            _waitCycles++;
            WriteU16(PaletteRam, address & palRamMask, value);

            UpdatePal(address & palRamMask);
        }

        private void ShaderWritePalRam32(uint address, uint value)
        {
            _waitCycles += 2;
            WriteU32(PaletteRam, address & palRamMask, value);

            UpdatePal(address & palRamMask);
        }

        private void ShaderWriteVRam8(uint address, byte value)
        {
            _waitCycles++;
            address &= vRamMask & ~1U;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            VideoRam[address] = value;
            VideoRam[address + 1] = value;
            
            UpdateVram(address);
        }

        private void ShaderWriteVRam16(uint address, ushort value)
        {
            _waitCycles++;
            address &= vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            WriteU16(VideoRam, address, value);

            UpdateVram(address);
        }

        private void ShaderWriteVRam32(uint address, uint value)
        {
            _waitCycles += 2;
            address &= vRamMask;
            if (address > 0x17FFF) address = 0x10000 + ((address - 0x17FFF) & 0x7FFF);
            WriteU32(VideoRam, address, value);

            UpdateVram(address);
        }

        public void EnableVramUpdating()
        {
            WriteU8Funcs[0x5] = ShaderWritePalRam8;
            WriteU16Funcs[0x5] = ShaderWritePalRam16;
            WriteU32Funcs[0x5] = ShaderWritePalRam32;
            WriteU8Funcs[0x6] = ShaderWriteVRam8;
            WriteU16Funcs[0x6] = ShaderWriteVRam16;
            WriteU32Funcs[0x6] = ShaderWriteVRam32;

            for (uint i = 0; i < (vRamMask + 1) / VramBlockSize; i++)
            {
                vramUpdated.Add(i);
            }

            for (uint i = 0; i < (palRamMask + 1) / PalBlockSize; i++)
            {
                palUpdated.Add(i);
            }
        }
        #endregion

        public byte ReadU8(uint address)
        {
            var bank = (address >> 24) & 0xf;
            return ReadU8Funcs[bank](address);
        }

        public ushort ReadU16(uint address)
        {
            address &= ~1U;
            var bank = (address >> 24) & 0xf;
            return ReadU16Funcs[bank](address);
        }

        public uint ReadU32(uint address)
        {
            var shiftAmt = (int)((address & 3U) << 3);
            address &= ~3U;
            var bank = (address >> 24) & 0xf;
            var res = ReadU32Funcs[bank](address);
            return (res >> shiftAmt) | (res << (32 - shiftAmt));
        }

        public uint ReadU32Aligned(uint address)
        {
            var bank = (address >> 24) & 0xf;
            return ReadU32Funcs[bank](address);
        }

        public ushort ReadU16Debug(uint address)
        {
            address &= ~1U;
            var bank = (address >> 24) & 0xf;
            var oldWaitCycles = _waitCycles;
            var res = ReadU16Funcs[bank](address);
            _waitCycles = oldWaitCycles;
            return res;
        }

        public uint ReadU32Debug(uint address)
        {
            var shiftAmt = (int)((address & 3U) << 3);
            address &= ~3U;
            var bank = (address >> 24) & 0xf;
            var oldWaitCycles = _waitCycles;
            var res = ReadU32Funcs[bank](address);
            _waitCycles = oldWaitCycles;
            return (res >> shiftAmt) | (res << (32 - shiftAmt));
        }

        public void WriteU8(uint address, byte value)
        {
            var bank = (address >> 24) & 0xf;
            WriteU8Funcs[bank](address, value);
        }

        public void WriteU16(uint address, ushort value)
        {
            address &= ~1U;
            var bank = (address >> 24) & 0xf;
            WriteU16Funcs[bank](address, value);
        }

        public void WriteU32(uint address, uint value)
        {
            address &= ~3U;
            var bank = (address >> 24) & 0xf;
            WriteU32Funcs[bank](address, value);
        }

        public void WriteU8Debug(uint address, byte value)
        {
            var bank = (address >> 24) & 0xf;
            var oldWaitCycles = _waitCycles;
            WriteU8Funcs[bank](address, value);
            _waitCycles = oldWaitCycles;
        }

        public void WriteU16Debug(uint address, ushort value)
        {
            address &= ~1U;
            var bank = (address >> 24) & 0xf;
            var oldWaitCycles = _waitCycles;
            WriteU16Funcs[bank](address, value);
            _waitCycles = oldWaitCycles;
        }

        public void WriteU32Debug(uint address, uint value)
        {
            address &= ~3U;
            var bank = (address >> 24) & 0xf;
            var oldWaitCycles = _waitCycles;
            WriteU32Funcs[bank](address, value);
            _waitCycles = oldWaitCycles;
        }

        public void LoadBios(byte[] biosRom)
        {
            Array.Copy(biosRom, biosRam, biosRam.Length);
        }

        public void LoadCartridge(byte[] cartRom)
        {
            ResetRomBank1();
            ResetRomBank2();

            // Set up the appropriate cart size
            var cartSize = 1;
            while (cartSize < cartRom.Length)
            {
                cartSize <<= 1;
            }

            if (cartSize != cartRom.Length)
            {
                throw new Exception("Unable to load non power of two carts");
            }

            // Split across bank 1 and 2 if cart is too big
            if (cartSize > 1 << 24)
            {
                romBank1 = cartRom;
                romBank1Mask = (1 << 24) - 1;

                cartRom.CopyTo(romBank2, 1 << 24);
                romBank2Mask = (1 << 24) - 1;
            }
            else
            {
                romBank1 = cartRom;
                romBank1Mask = (uint)(cartSize - 1);
            }

            if (romBank1Mask != 0)
            {
                // TODO: Writes (i.e. eeprom, and other stuff)
                ReadU8Funcs[0x8] = ReadRom1_8;
                ReadU8Funcs[0xA] = ReadRom1_8;
                ReadU8Funcs[0xC] = ReadRom1_8;
                ReadU16Funcs[0x8] = ReadRom1_16;
                ReadU16Funcs[0xA] = ReadRom1_16;
                ReadU16Funcs[0xC] = ReadRom1_16;
                ReadU32Funcs[0x8] = ReadRom1_32;
                ReadU32Funcs[0xA] = ReadRom1_32;
                ReadU32Funcs[0xC] = ReadRom1_32;
            }

            if (romBank2Mask != 0)
            {
                ReadU8Funcs[0x9] = ReadRom2_8;
                ReadU8Funcs[0xB] = ReadRom2_8;
                ReadU8Funcs[0xD] = ReadRom2_8;
                ReadU16Funcs[0x9] = ReadRom2_16;
                ReadU16Funcs[0xB] = ReadRom2_16;
                ReadU16Funcs[0xD] = ReadRom2_16;
                ReadU32Funcs[0x9] = ReadRom2_32;
                ReadU32Funcs[0xB] = ReadRom2_32;
                ReadU32Funcs[0xD] = ReadRom2_32;
            }
        }

        private void ResetRomBank1()
        {
            romBank1 = null;
            romBank1Mask = 0;

            for (var i = 0; i < bankSTimes.Length; i++)
            {
                bankSTimes[i] = 2;
            }

            ReadU8Funcs[0x8] = ReadNop8;
            ReadU8Funcs[0xA] = ReadNop8;
            ReadU8Funcs[0xC] = ReadNop8;
            ReadU16Funcs[0x8] = ReadNop16;
            ReadU16Funcs[0xA] = ReadNop16;
            ReadU16Funcs[0xC] = ReadNop16;
            ReadU32Funcs[0x8] = ReadNop32;
            ReadU32Funcs[0xA] = ReadNop32;
            ReadU32Funcs[0xC] = ReadNop32;
        }

        private void ResetRomBank2()
        {
            romBank2 = null;
            romBank2Mask = 0;

            ReadU8Funcs[0x9] = ReadEeprom8;
            ReadU8Funcs[0xB] = ReadEeprom8;
            ReadU8Funcs[0xD] = ReadEeprom8;
            ReadU16Funcs[0x9] = ReadEeprom16;
            ReadU16Funcs[0xB] = ReadEeprom16;
            ReadU16Funcs[0xD] = ReadEeprom16;
            ReadU32Funcs[0x9] = ReadEeprom32;
            ReadU32Funcs[0xB] = ReadEeprom32;
            ReadU32Funcs[0xD] = ReadEeprom32;

            WriteU8Funcs[0x9] = WriteEeprom8;
            WriteU8Funcs[0xB] = WriteEeprom8;
            WriteU8Funcs[0xD] = WriteEeprom8;
            WriteU16Funcs[0x9] = WriteEeprom16;
            WriteU16Funcs[0xB] = WriteEeprom16;
            WriteU16Funcs[0xD] = WriteEeprom16;
            WriteU32Funcs[0x9] = WriteEeprom32;
            WriteU32Funcs[0xB] = WriteEeprom32;
            WriteU32Funcs[0xD] = WriteEeprom32;
        }
    }
}
