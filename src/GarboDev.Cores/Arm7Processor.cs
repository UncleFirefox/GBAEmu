using System;
using System.Collections.Generic;
using GarboDev.Cores.InterpretedCore;
using GarboDev.CrossCutting;
using GarboDev.Sound;

namespace GarboDev.Cores
{
    public class Arm7Processor
    {
        private readonly Memory _memory;
        private readonly SoundManager _sound;
        private readonly FastArmCore _armCore;
        private readonly ThumbCore _thumbCore;

        private int _timerCycles;
        private int _soundCycles;

        // CPU mode definitions
        public const uint USR = 0x10;
        public const uint FIQ = 0x11;
        public const uint IRQ = 0x12;
        public const uint SVC = 0x13;
        public const uint ABT = 0x17;
        public const uint UND = 0x1B;
        public const uint SYS = 0x1F;

        // CPSR bit definitions
        public const int N_BIT = 31;
        public const int Z_BIT = 30;
        public const int C_BIT = 29;
        public const int V_BIT = 28;
        public const int I_BIT = 7;
        public const int F_BIT = 6;
        public const int T_BIT = 5;

        public const uint N_MASK = 1U << N_BIT;
        public const uint Z_MASK = 1U << Z_BIT;
        public const uint C_MASK = 1U << C_BIT;
        public const uint V_MASK = 1U << V_BIT;
        public const uint I_MASK = 1U << I_BIT;
        public const uint F_MASK = 1U << F_BIT;
        public const uint T_MASK = 1U << T_BIT;

        // Standard registers

        // Banked registers
        private readonly uint[] bankedFIQ = new uint[7];
        private readonly uint[] bankedIRQ = new uint[2];
        private readonly uint[] bankedSVC = new uint[2];
        private readonly uint[] bankedABT = new uint[2];
        private readonly uint[] bankedUND = new uint[2];

        // Saved CPSR's
        private uint _spsrFiq;
        private uint _spsrIrq;
        private uint _spsrSvc;
        private uint _spsrAbt;
        private uint _spsrUnd;

        private ushort _keyState;

        private bool _cpuHalted;

        public ushort KeyState
        {
            set => _keyState = value;
        }

        public int Cycles { get; set; }

        public bool ArmState => (CPSR & T_MASK) != T_MASK;

        public uint[] Registers { get; } = new uint[16];

        public uint CPSR { get; set; }

        public bool SPSRExists
        {
            get
            {
                switch (CPSR & 0x1F)
                {
                    case USR:
                    case SYS:
                        return false;
                    case FIQ:
                    case SVC:
                    case ABT:
                    case IRQ:
                    case UND:
                        return true;
                    default:
                        return false;
                }
            }
        }

        public uint SPSR
        {
            get
            {
                switch (CPSR & 0x1F)
                {
                    case USR:
                    case SYS:
                        return 0xFFFFFFFF;
                    case FIQ:
                        return _spsrFiq;
                    case SVC:
                        return _spsrSvc;
                    case ABT:
                        return _spsrAbt;
                    case IRQ:
                        return _spsrIrq;
                    case UND:
                        return _spsrUnd;
                    default:
                        throw new Exception("Unhandled CPSR state...");
                }
            }
            set
            {
                switch (CPSR & 0x1F)
                {
                    case USR:
                    case SYS:
                        break;
                    case FIQ:
                        _spsrFiq = value;
                        break;
                    case SVC:
                        _spsrSvc = value;
                        break;
                    case ABT:
                        _spsrAbt = value;
                        break;
                    case IRQ:
                        _spsrIrq = value;
                        break;
                    case UND:
                        _spsrUnd = value;
                        break;
                    default:
                        throw new Exception("Unhandled CPSR state...");
                }
            }
        }

        public Dictionary<uint, bool> Breakpoints { get; }

        public bool BreakpointHit { get; set; }

        public Arm7Processor(Memory memory, SoundManager sound)
        {
            _memory = memory;
            _sound = sound;

            // Processor callbacks
            _memory.ArmProcessorState = () => ArmState;
            _memory.RequestProcessorIrq = RequestIrq;
            _memory.UpdateProcessorTimers = UpdateTimers;
            _memory.HaltProcessor = Halt;
            _memory.GetProcessorRegister15 = () => Registers[15];

            _armCore = new FastArmCore(this, _memory);
            _thumbCore = new ThumbCore(this, _memory);
            Breakpoints = new Dictionary<uint, bool>();
            BreakpointHit = false;
        }

        private void SwapRegsHelper(uint[] swapRegs)
        {
            for (var i = 14; i > 14 - swapRegs.Length; i--)
            {
                var tmp = Registers[i];
                Registers[i] = swapRegs[swapRegs.Length - (14 - i) - 1];
                swapRegs[swapRegs.Length - (14 - i) - 1] = tmp;
            }
        }

        private void SwapRegisters(uint bank)
        {
            switch (bank & 0x1F)
            {
                case FIQ:
                    SwapRegsHelper(bankedFIQ);
                    break;
                case SVC:
                    SwapRegsHelper(bankedSVC);
                    break;
                case ABT:
                    SwapRegsHelper(bankedABT);
                    break;
                case IRQ:
                    SwapRegsHelper(bankedIRQ);
                    break;
                case UND:
                    SwapRegsHelper(bankedUND);
                    break;
            }
        }

        public void WriteCpsr(uint newCpsr)
        {
            if ((newCpsr & 0x1F) != (CPSR & 0x1F))
            {
                // Swap out the old registers
                SwapRegisters(CPSR);
                // Swap in the new registers
                SwapRegisters(newCpsr);
            }

            CPSR = newCpsr;
        }

        public void EnterException(uint mode, uint vector, bool interruptsDisabled, bool fiqDisabled)
        {
            var oldCpsr = CPSR;

            if ((oldCpsr & T_MASK) != 0)
            {
                Registers[15] += 2U;
            }

            // Clear T bit, and set mode
            var newCpsr = (oldCpsr & ~0x3FU) | mode;
            if (interruptsDisabled) newCpsr |= 1 << 7;
            if (fiqDisabled) newCpsr |= 1 << 6;
            WriteCpsr(newCpsr);

            SPSR = oldCpsr;
            Registers[14] = Registers[15];
            Registers[15] = vector;

            ReloadQueue();
        }

        public void RequestIrq(int irq)
        {
            var iflag = Memory.ReadU16(_memory.IORam, Memory.IF);
            iflag |= (ushort)(1 << irq);
            Memory.WriteU16(_memory.IORam, Memory.IF, iflag);
        }

        public void FireIrq()
        {
            var ime = Memory.ReadU16(_memory.IORam, Memory.IME);
            var ie = Memory.ReadU16(_memory.IORam, Memory.IE);
            var iflag = Memory.ReadU16(_memory.IORam, Memory.IF);

            if ((ie & (iflag)) != 0 && (ime & 1) != 0 && (CPSR & (1 << 7)) == 0)
            {
                // Off to the irq exception vector
                EnterException(IRQ, 0x18, true, false);
            }
        }

        public void Reset(bool skipBios)
        {
            BreakpointHit = false;
            _cpuHalted = false;

            // Default to ARM state
            Cycles = 0;
            _timerCycles = 0;
            _soundCycles = 0;

            bankedSVC[0] = 0x03007FE0;
            bankedIRQ[0] = 0x03007FA0;

            CPSR = SYS;
            _spsrSvc = CPSR;
            for (var i = 0; i < 15; i++) Registers[i] = 0;

            if (skipBios)
            {
                Registers[15] = 0x8000000;
            }
            else
            {
                Registers[15] = 0;
            }

            _armCore.BeginExecution();
        }

        public void Halt()
        {
            _cpuHalted = true;
            Cycles = 0;
        }

        public void Step()
        {
            BreakpointHit = false;

            if ((CPSR & T_MASK) == T_MASK)
            {
                _thumbCore.Step();
            }
            else
            {
                _armCore.Step();
            }

            UpdateTimers();
        }

        public void ReloadQueue()
        {
            if ((CPSR & T_MASK) == T_MASK)
            {
                _thumbCore.BeginExecution();
            }
            else
            {
                _armCore.BeginExecution();
            }
        }

        private void UpdateTimer(int timer, int cycles, bool countUp)
        {
            var control = Memory.ReadU16(_memory.IORam, Memory.TM0CNT + (uint)(timer * 4));

            // Make sure timer is enabled, or count up is disabled
            if ((control & (1 << 7)) == 0) return;
            if (!countUp && (control & (1 << 2)) != 0) return;

            if (!countUp)
            {
                switch (control & 3)
                {
                    case 0: cycles *= 1 << 10; break;
                    case 1: cycles *= 1 << 4; break;
                    case 2: cycles *= 1 << 2; break;
                    // Don't need to do anything for case 3
                }
            }

            _memory.TimerCnt[timer] += (uint)cycles;
            var timerCnt = _memory.TimerCnt[timer] >> 10;

            if (timerCnt > 0xffff)
            {
                var soundCntX = Memory.ReadU16(_memory.IORam, Memory.SOUNDCNT_X);
                if ((soundCntX & (1 << 7)) != 0)
                {
                    var soundCntH = Memory.ReadU16(_memory.IORam, Memory.SOUNDCNT_H);
                    if (timer == ((soundCntH >> 10) & 1))
                    {
                        // FIFO A overflow
                        _sound.DequeueA();
                        if (_sound.QueueSizeA < 16)
                        {
                            _memory.FifoDma(1);
                            // TODO
                            if (_sound.QueueSizeA < 16)
                            {
                            }
                        }
                    }
                    if (timer == ((soundCntH >> 14) & 1))
                    {
                        // FIFO B overflow
                        _sound.DequeueB();
                        if (_sound.QueueSizeB < 16)
                        {
                            _memory.FifoDma(2);
                        }
                    }
                }

                // Overflow, attempt to fire IRQ
                if ((control & (1 << 6)) != 0)
                {
                    RequestIrq(3 + timer);
                }

                if (timer < 3)
                {
                    var control2 = Memory.ReadU16(_memory.IORam, Memory.TM0CNT + (uint)((timer + 1) * 4));
                    if ((control2 & (1 << 2)) != 0)
                    {
                        // Count-up
                        UpdateTimer(timer + 1, (int)((timerCnt >> 16) << 10), true);
                    }
                }

                // Reset the original value
                uint count = Memory.ReadU16(_memory.IORam, Memory.TM0D + (uint)(timer * 4));
                _memory.TimerCnt[timer] = count << 10;
            }
        }

        public void UpdateTimers()
        {
            var cycles = _timerCycles - Cycles;

            for (var i = 0; i < 4; i++)
            {
                UpdateTimer(i, cycles, false);
            }

            _timerCycles = Cycles;
        }

        public void UpdateKeyState()
        {
            var KEYCNT = _memory.ReadU16Debug(Memory.REG_BASE + Memory.KEYCNT);

            if ((KEYCNT & (1 << 14)) != 0)
            {
                if ((KEYCNT & (1 << 15)) != 0)
                {
                    KEYCNT &= 0x3FF;
                    if (((~_keyState) & KEYCNT) == KEYCNT)
                        RequestIrq(12);
                }
                else
                {
                    KEYCNT &= 0x3FF;
                    if (((~_keyState) & KEYCNT) != 0)
                        RequestIrq(12);
                }
            }

            _memory.KeyState = _keyState;
        }

        public void UpdateSound()
        {
            _sound.Mix(_soundCycles);
            _soundCycles = 0;
        }

        public void Execute(int cycles)
        {
            Cycles += cycles;
            _timerCycles += cycles;
            _soundCycles += cycles;
            BreakpointHit = false;

            if (_cpuHalted)
            {
                var ie = Memory.ReadU16(_memory.IORam, Memory.IE);
                var iflag = Memory.ReadU16(_memory.IORam, Memory.IF);

                if ((ie & iflag) != 0)
                {
                    _cpuHalted = false;
                }
                else
                {
                    Cycles = 0;
                    UpdateTimers();
                    UpdateSound();
                    return;
                }
            }

            while (Cycles > 0)
            {
                if ((CPSR & T_MASK) == T_MASK)
                {
                    _thumbCore.Execute();
                }
                else
                {
                    _armCore.Execute();
                }

                UpdateTimers();
                UpdateSound();

                if (BreakpointHit)
                {
                    break;
                }
            }
        }
    }
}