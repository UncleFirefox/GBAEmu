//#define ARM_DEBUG

using System;
using GarboDev.CrossCutting;

namespace GarboDev.Cores.InterpretedCore
{
    public class ThumbCore
    {
        private const int CondEq = 0;	    // Z set
        private const int CondNe = 1;	    // Z clear
        private const int CondCs = 2;	    // C set
        private const int CondCc = 3;	    // C clear
        private const int CondMi = 4;	    // N set
        private const int CondPl = 5;	    // N clear
        private const int CondVs = 6;	    // V set
        private const int CondVc = 7;	    // V clear
        private const int CondHi = 8;	    // C set and Z clear
        private const int CondLs = 9;	    // C clear or Z set
        private const int CondGe = 10;	    // N equals V
        private const int CondLt = 11;	    // N not equal to V
        private const int CondGt = 12; 	// Z clear AND (N equals V)
        private const int CondLe = 13; 	// Z set OR (N not equal to V)
        private const int CondAl = 14; 	// Always
        private const int CondNv = 15; 	// Never execute

        private const int OpAnd = 0x0;
        private const int OpEor = 0x1;
        private const int OpLsl = 0x2;
        private const int OpLsr = 0x3;
        private const int OpAsr = 0x4;
        private const int OpAdc = 0x5;
        private const int OpSbc = 0x6;
        private const int OpRor = 0x7;
        private const int OpTst = 0x8;
        private const int OpNeg = 0x9;
        private const int OpCmp = 0xA;
        private const int OpCmn = 0xB;
        private const int OpOrr = 0xC;
        private const int OpMul = 0xD;
        private const int OpBic = 0xE;
        private const int OpMvn = 0xF;

        private readonly Arm7Processor _parent;
        private readonly Memory _memory;
        private readonly uint[] _registers;

        // CPU flags
        private uint _zero, _carry, _negative, _overflow;
        private ushort _curInstruction, _instructionQueue;

        private delegate void ExecuteInstruction();
        private readonly ExecuteInstruction[] _normalOps;

        public ThumbCore(Arm7Processor parent, Memory memory)
        {
            _parent = parent;
            _memory = memory;
            _registers = _parent.Registers;

            _normalOps = new ExecuteInstruction[256]
                {
                    OpLslImm, OpLslImm, OpLslImm, OpLslImm, OpLslImm, OpLslImm, OpLslImm, OpLslImm,
                    OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm, OpLsrImm,
                    OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm, OpAsrImm,
                    OpAddRegReg, OpAddRegReg, OpSubRegReg, OpSubRegReg, OpAddRegImm, OpAddRegImm, OpSubRegImm, OpSubRegImm,
                    OpMovImm, OpMovImm, OpMovImm, OpMovImm, OpMovImm, OpMovImm, OpMovImm, OpMovImm,
                    OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm, OpCmpImm,
                    OpAddImm, OpAddImm, OpAddImm, OpAddImm, OpAddImm, OpAddImm, OpAddImm, OpAddImm,
                    OpSubImm, OpSubImm, OpSubImm, OpSubImm, OpSubImm, OpSubImm, OpSubImm, OpSubImm,
                    OpArith, OpArith, OpArith, OpArith, OpAddHi, OpCmpHi, OpMovHi, OpBx,
                    OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc, OpLdrPc,
                    OpStrReg, OpStrReg, OpStrhReg, OpStrhReg, OpStrbReg, OpStrbReg, OpLdrsbReg, OpLdrsbReg,
                    OpLdrReg, OpLdrReg, OpLdrhReg, OpLdrhReg, OpLdrbReg, OpLdrbReg, OpLdrshReg, OpLdrshReg,
                    OpStrImm, OpStrImm, OpStrImm, OpStrImm, OpStrImm, OpStrImm, OpStrImm, OpStrImm,
                    OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm, OpLdrImm,
                    OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm, OpStrbImm,
                    OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm, OpLdrbImm,
                    OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm, OpStrhImm,
                    OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm, OpLdrhImm,
                    OpStrSp, OpStrSp, OpStrSp, OpStrSp, OpStrSp, OpStrSp, OpStrSp, OpStrSp,
                    OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp, OpLdrSp,
                    OpAddPc, OpAddPc, OpAddPc, OpAddPc, OpAddPc, OpAddPc, OpAddPc, OpAddPc, 
                    OpAddSp, OpAddSp, OpAddSp, OpAddSp, OpAddSp, OpAddSp, OpAddSp, OpAddSp,
                    OpSubSp, OpUnd, OpUnd, OpUnd, OpPush, OpPushLr, OpUnd, OpUnd,
                    OpUnd, OpUnd, OpUnd, OpUnd, OpPop, OpPopPc, OpUnd, OpUnd,
                    OpStmia, OpStmia, OpStmia, OpStmia, OpStmia, OpStmia, OpStmia, OpStmia, 
                    OpLdmia, OpLdmia, OpLdmia, OpLdmia, OpLdmia, OpLdmia, OpLdmia, OpLdmia,
                    OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpBCond,
                    OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpBCond, OpUnd, OpSwi,
                    OpB, OpB, OpB, OpB, OpB, OpB, OpB, OpB,
                    OpUnd, OpUnd, OpUnd, OpUnd, OpUnd, OpUnd, OpUnd, OpUnd,
                    OpBl1, OpBl1, OpBl1, OpBl1, OpBl1, OpBl1, OpBl1, OpBl1, 
                    OpBl2, OpBl2, OpBl2, OpBl2, OpBl2, OpBl2, OpBl2, OpBl2
                };
        }

        public void BeginExecution()
        {
            FlushQueue();
        }

        public void Step()
        {
            UnpackFlags();

            _curInstruction = _instructionQueue;
            _instructionQueue = _memory.ReadU16(_registers[15]);
            _registers[15] += 2;

            // Execute the instruction
            _normalOps[_curInstruction >> 8]();

            _parent.Cycles -= _memory.WaitCycles;

            if ((_parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
            {
                if ((_curInstruction >> 8) != 0xDF) _parent.ReloadQueue();
            }

            PackFlags();
        }

        public void Execute()
        {
            UnpackFlags();

            while (_parent.Cycles > 0)
            {
                _curInstruction = _instructionQueue;
                _instructionQueue = _memory.ReadU16(_registers[15]);
                _registers[15] += 2;

                // Execute the instruction
                _normalOps[_curInstruction >> 8]();

                _parent.Cycles -= _memory.WaitCycles;

                if ((_parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
                {
                    if ((_curInstruction >> 8) != 0xDF) _parent.ReloadQueue();
                    break;
                }

                // Check the current PC
#if ARM_DEBUG
                if (this.parent.Breakpoints.ContainsKey(registers[15] - 2U))
                {
                    this.parent.BreakpointHit = true;
                    break;
                }
#endif
            }

            PackFlags();
        }

        #region Flag helpers
        public void OverflowCarryAdd(uint a, uint b, uint r)
        {
            _overflow = ((a & b & ~r) | (~a & ~b & r)) >> 31;
            _carry = ((a & b) | (a & ~r) | (b & ~r)) >> 31;
        }

        public void OverflowCarrySub(uint a, uint b, uint r)
        {
            _overflow = ((a & ~b & ~r) | (~a & b & r)) >> 31;
            _carry = ((a & ~b) | (a & ~r) | (~b & ~r)) >> 31;
        }
        #endregion

        #region Opcodes
        private void OpLslImm()
        {
            // 0x00 - 0x07
            // lsl rd, rm, #immed
            var rd = _curInstruction & 0x7;
            var rm = (_curInstruction >> 3) & 0x7;
            var immed = (_curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                _registers[rd] = _registers[rm];
            } else
            {
                _carry = (_registers[rm] >> (32 - immed)) & 0x1;
                _registers[rd] = _registers[rm] << immed;
            }

            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpLsrImm()
        {
            // 0x08 - 0x0F
            // lsr rd, rm, #immed
            var rd = _curInstruction & 0x7;
            var rm = (_curInstruction >> 3) & 0x7;
            var immed = (_curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                _carry = _registers[rm] >> 31;
                _registers[rd] = 0;
            }
            else
            {
                _carry = (_registers[rm] >> (immed - 1)) & 0x1;
                _registers[rd] = _registers[rm] >> immed;
            }

            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAsrImm()
        {
            // asr rd, rm, #immed
            var rd = _curInstruction & 0x7;
            var rm = (_curInstruction >> 3) & 0x7;
            var immed = (_curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                _carry = _registers[rm] >> 31;
                if (_carry == 1) _registers[rd] = 0xFFFFFFFF;
                else _registers[rd] = 0;
            }
            else
            {
                _carry = (_registers[rm] >> (immed - 1)) & 0x1;
                _registers[rd] = (uint)(((int)_registers[rm]) >> immed);
            }

            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAddRegReg()
        {
            // add rd, rn, rm
            var rd = _curInstruction & 0x7;
            var rn = (_curInstruction >> 3) & 0x7;
            var rm = (_curInstruction >> 6) & 0x7;

            var orn = _registers[rn];
            var orm = _registers[rm];

            _registers[rd] = orn + orm;

            OverflowCarryAdd(orn, orm, _registers[rd]);
            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubRegReg()
        {
            // sub rd, rn, rm
            var rd = _curInstruction & 0x7;
            var rn = (_curInstruction >> 3) & 0x7;
            var rm = (_curInstruction >> 6) & 0x7;

            var orn = _registers[rn];
            var orm = _registers[rm];

            _registers[rd] = orn - orm;

            OverflowCarrySub(orn, orm, _registers[rd]);
            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAddRegImm()
        {
            // add rd, rn, #immed
            var rd = _curInstruction & 0x7;
            var rn = (_curInstruction >> 3) & 0x7;
            var immed = (uint)((_curInstruction >> 6) & 0x7);

            var orn = _registers[rn];

            _registers[rd] = orn + immed;

            OverflowCarryAdd(orn, immed, _registers[rd]);
            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubRegImm()
        {
            // sub rd, rn, #immed
            var rd = _curInstruction & 0x7;
            var rn = (_curInstruction >> 3) & 0x7;
            var immed = (uint)((_curInstruction >> 6) & 0x7);

            var orn = _registers[rn];

            _registers[rd] = orn - immed;

            OverflowCarrySub(orn, immed, _registers[rd]);
            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpMovImm()
        {
            // mov rd, #immed
            var rd = (_curInstruction >> 8) & 0x7;

            _registers[rd] = (uint)(_curInstruction & 0xFF);

            _negative = 0;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpCmpImm()
        {
            // cmp rn, #immed
            var rn = (_curInstruction >> 8) & 0x7;

            var alu = _registers[rn] - (uint)(_curInstruction & 0xFF);

            OverflowCarrySub(_registers[rn], (uint)(_curInstruction & 0xFF), alu);
            _negative = alu >> 31;
            _zero = alu == 0 ? 1U : 0U;
        }

        private void OpAddImm()
        {
            // add rd, #immed
            var rd = (_curInstruction >> 8) & 0x7;

            var ord = _registers[rd];

            _registers[rd] += (uint)(_curInstruction & 0xFF);

            OverflowCarryAdd(ord, (uint)(_curInstruction & 0xFF), _registers[rd]);
            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubImm()
        {
            // sub rd, #immed
            var rd = (_curInstruction >> 8) & 0x7;

            var ord = _registers[rd];

            _registers[rd] -= (uint)(_curInstruction & 0xFF);

            OverflowCarrySub(ord, (uint)(_curInstruction & 0xFF), _registers[rd]);
            _negative = _registers[rd] >> 31;
            _zero = _registers[rd] == 0 ? 1U : 0U;
        }

        private void OpArith()
        {
            var rd = _curInstruction & 0x7;
            var rn = _registers[(_curInstruction >> 3) & 0x7];

            uint orig, alu;
            int shiftAmt;

            switch ((_curInstruction >> 6) & 0xF)
            {
                case OpAdc:
                    orig = _registers[rd];
                    _registers[rd] += rn + _carry;

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    OverflowCarryAdd(orig, rn, _registers[rd]);
                    break;

                case OpAnd:
                    _registers[rd] &= rn;

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpAsr:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        _carry = (_registers[rd] >> (shiftAmt - 1)) & 0x1;
                        _registers[rd] = (uint)(((int)_registers[rd]) >> shiftAmt);
                    }
                    else
                    {
                        _carry = (_registers[rd] >> 31) & 1;
                        if (_carry == 1) _registers[rd] = 0xFFFFFFFF;
                        else _registers[rd] = 0;
                    }

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpBic:
                    _registers[rd] &= ~rn;

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpCmn:
                    alu = _registers[rd] + rn;

                    _negative = alu >> 31;
                    _zero = alu == 0 ? 1U : 0U;
                    OverflowCarryAdd(_registers[rd], rn, alu);
                    break;

                case OpCmp:
                    alu = _registers[rd] - rn;

                    _negative = alu >> 31;
                    _zero = alu == 0 ? 1U : 0U;
                    OverflowCarrySub(_registers[rd], rn, alu);
                    break;

                case OpEor:
                    _registers[rd] ^= rn;

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpLsl:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        _carry = (_registers[rd] >> (32 - shiftAmt)) & 0x1;
                        _registers[rd] <<= shiftAmt;
                    }
                    else if (shiftAmt == 32)
                    {
                        _carry = _registers[rd] & 0x1;
                        _registers[rd] = 0;
                    }
                    else
                    {
                        _carry = 0;
                        _registers[rd] = 0;
                    }

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpLsr:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        _carry = (_registers[rd] >> (shiftAmt - 1)) & 0x1;
                        _registers[rd] >>= shiftAmt;
                    }
                    else if (shiftAmt == 32)
                    {
                        _carry = (_registers[rd] >> 31) & 0x1;
                        _registers[rd] = 0;
                    }
                    else
                    {
                        _carry = 0;
                        _registers[rd] = 0;
                    }

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpMul:
                    var mulCycles = 4;
                    // Multiply cycle calculations
                    if ((rn & 0xFFFFFF00) == 0 || (rn & 0xFFFFFF00) == 0xFFFFFF00)
                    {
                        mulCycles = 1;
                    }
                    else if ((rn & 0xFFFF0000) == 0 || (rn & 0xFFFF0000) == 0xFFFF0000)
                    {
                        mulCycles = 2;
                    }
                    else if ((rn & 0xFF000000) == 0 || (rn & 0xFF000000) == 0xFF000000)
                    {
                        mulCycles = 3;
                    }

                    _parent.Cycles -= mulCycles;

                    _registers[rd] *= rn;

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpMvn:
                    _registers[rd] = ~rn;

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpNeg:
                    _registers[rd] = 0 - rn;

                    OverflowCarrySub(0, rn, _registers[rd]);
                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpOrr:
                    _registers[rd] |= rn;

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpRor:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if ((shiftAmt & 0x1F) == 0)
                    {
                        _carry = _registers[rd] >> 31;
                    }
                    else
                    {
                        shiftAmt &= 0x1F;
                        _carry = (_registers[rd] >> (shiftAmt - 1)) & 0x1;
                        _registers[rd] = (_registers[rd] >> shiftAmt) | (_registers[rd] << (32 - shiftAmt));
                    }

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    break;

                case OpSbc:
                    orig = _registers[rd];
                    _registers[rd] = (_registers[rd] - rn) - (1U - _carry);

                    _negative = _registers[rd] >> 31;
                    _zero = _registers[rd] == 0 ? 1U : 0U;
                    OverflowCarrySub(orig, rn, _registers[rd]);
                    break;

                case OpTst:
                    alu = _registers[rd] & rn;

                    _negative = alu >> 31;
                    _zero = alu == 0 ? 1U : 0U;
                    break;

                default:
                    throw new Exception("The coder screwed up on the thumb alu op...");
            }
        }

        private void OpAddHi()
        {
            var rd = ((_curInstruction & (1 << 7)) >> 4) | (_curInstruction & 0x7);
            var rm = (_curInstruction >> 3) & 0xF;

            _registers[rd] += _registers[rm];

            if (rd == 15)
            {
                _registers[rd] &= ~1U;
                FlushQueue();
            }
        }

        private void OpCmpHi()
        {
            var rd = ((_curInstruction & (1 << 7)) >> 4) | (_curInstruction & 0x7);
            var rm = (_curInstruction >> 3) & 0xF;

            var alu = _registers[rd] - _registers[rm];

            _negative = alu >> 31;
            _zero = alu == 0 ? 1U : 0U;
            OverflowCarrySub(_registers[rd], _registers[rm], alu);
        }

        private void OpMovHi()
        {
            var rd = ((_curInstruction & (1 << 7)) >> 4) | (_curInstruction & 0x7);
            var rm = (_curInstruction >> 3) & 0xF;

            _registers[rd] = _registers[rm];

            if (rd == 15)
            {
                _registers[rd] &= ~1U;
                FlushQueue();
            }
        }

        private void OpBx()
        {
            var rm = (_curInstruction >> 3) & 0xf;

            PackFlags();

            _parent.CPSR &= ~Arm7Processor.T_MASK;
            _parent.CPSR |= (_registers[rm] & 1) << Arm7Processor.T_BIT;

            _registers[15] = _registers[rm] & (~1U);

            UnpackFlags();

            // Check for branch back to Arm Mode
            if ((_parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
            {
                return;
            }

            FlushQueue();
        }

        private void OpLdrPc()
        {
            var rd = (_curInstruction >> 8) & 0x7;

            _registers[rd] = _memory.ReadU32((_registers[15] & ~2U) + (uint)((_curInstruction & 0xFF) * 4));

            _parent.Cycles--;
        }

        private void OpStrReg()
        {
            _memory.WriteU32(_registers[(_curInstruction >> 3) & 0x7] + _registers[(_curInstruction >> 6) & 0x7],
                _registers[_curInstruction & 0x7]);
        }

        private void OpStrhReg()
        {
            _memory.WriteU16(_registers[(_curInstruction >> 3) & 0x7] + _registers[(_curInstruction >> 6) & 0x7],
                (ushort)(_registers[_curInstruction & 0x7] & 0xFFFF));
        }

        private void OpStrbReg()
        {
            _memory.WriteU8(_registers[(_curInstruction >> 3) & 0x7] + _registers[(_curInstruction >> 6) & 0x7],
                (byte)(_registers[_curInstruction & 0x7] & 0xFF));
        }

        private void OpLdrsbReg()
        {
            _registers[_curInstruction & 0x7] =
                _memory.ReadU8(_registers[(_curInstruction >> 3) & 0x7] + _registers[(_curInstruction >> 6) & 0x7]);

            if ((_registers[_curInstruction & 0x7] & (1 << 7)) != 0)
            {
                _registers[_curInstruction & 0x7] |= 0xFFFFFF00;
            }

            _parent.Cycles--;
        }

        private void OpLdrReg()
        {
            _registers[_curInstruction & 0x7] =
                _memory.ReadU32(_registers[(_curInstruction >> 3) & 0x7] + _registers[(_curInstruction >> 6) & 0x7]);

            _parent.Cycles--;
        }

        private void OpLdrhReg()
        {
            _registers[_curInstruction & 0x7] =
                _memory.ReadU16(_registers[(_curInstruction >> 3) & 0x7] + _registers[(_curInstruction >> 6) & 0x7]);

            _parent.Cycles--;
        }

        private void OpLdrbReg()
        {
            _registers[_curInstruction & 0x7] =
                _memory.ReadU8(_registers[(_curInstruction >> 3) & 0x7] + _registers[(_curInstruction >> 6) & 0x7]);

            _parent.Cycles--;
        }

        private void OpLdrshReg()
        {
            _registers[_curInstruction & 0x7] =
                _memory.ReadU16(_registers[(_curInstruction >> 3) & 0x7] + _registers[(_curInstruction >> 6) & 0x7]);

            if ((_registers[_curInstruction & 0x7] & (1 << 15)) != 0)
            {
                _registers[_curInstruction & 0x7] |= 0xFFFF0000;
            }

            _parent.Cycles--;
        }

        private void OpStrImm()
        {
            _memory.WriteU32(_registers[(_curInstruction >> 3) & 0x7] + (uint)(((_curInstruction >> 6) & 0x1F) * 4),
                _registers[_curInstruction & 0x7]);
        }

        private void OpLdrImm()
        {
            _registers[_curInstruction & 0x7] = 
                _memory.ReadU32(_registers[(_curInstruction >> 3) & 0x7] + (uint)(((_curInstruction >> 6) & 0x1F) * 4));

            _parent.Cycles--;
        }

        private void OpStrbImm()
        {
            _memory.WriteU8(_registers[(_curInstruction >> 3) & 0x7] + (uint)((_curInstruction >> 6) & 0x1F),
                (byte)(_registers[_curInstruction & 0x7] & 0xFF));
        }

        private void OpLdrbImm()
        {
            _registers[_curInstruction & 0x7] =
                _memory.ReadU8(_registers[(_curInstruction >> 3) & 0x7] + (uint)((_curInstruction >> 6) & 0x1F));

            _parent.Cycles--;
        }

        private void OpStrhImm()
        {
            _memory.WriteU16(_registers[(_curInstruction >> 3) & 0x7] + (uint)(((_curInstruction >> 6) & 0x1F) * 2),
                (ushort)(_registers[_curInstruction & 0x7] & 0xFFFF));
        }

        private void OpLdrhImm()
        {
            _registers[_curInstruction & 0x7] =
                _memory.ReadU16(_registers[(_curInstruction >> 3) & 0x7] + (uint)(((_curInstruction >> 6) & 0x1F) * 2));

            _parent.Cycles--;
        }

        private void OpStrSp()
        {
            _memory.WriteU32(_registers[13] + (uint)((_curInstruction & 0xFF) * 4),
                _registers[(_curInstruction >> 8) & 0x7]);
        }

        private void OpLdrSp()
        {
            _registers[(_curInstruction >> 8) & 0x7] = 
                _memory.ReadU32(_registers[13] + (uint)((_curInstruction & 0xFF) * 4));
        }

        private void OpAddPc()
        {
            _registers[(_curInstruction >> 8) & 0x7] =
                (_registers[15] & ~2U) + (uint)((_curInstruction & 0xFF) * 4);
        }

        private void OpAddSp()
        {
            _registers[(_curInstruction >> 8) & 0x7] =
                _registers[13] + (uint)((_curInstruction & 0xFF) * 4);
        }

        private void OpSubSp()
        {
            if ((_curInstruction & (1 << 7)) != 0)
                _registers[13] -= (uint)((_curInstruction & 0x7F) * 4);
            else
                _registers[13] += (uint)((_curInstruction & 0x7F) * 4);
        }

        private void OpPush()
        {
            for (var i = 7; i >= 0; i--)
            {
                if (((_curInstruction >> i) & 1) != 0)
                {
                    _registers[13] -= 4;
                    _memory.WriteU32(_registers[13], _registers[i]);
                }
            }
        }

        private void OpPushLr()
        {
            _registers[13] -= 4;
            _memory.WriteU32(_registers[13], _registers[14]);

            for (var i = 7; i >= 0; i--)
            {
                if (((_curInstruction >> i) & 1) != 0)
                {
                    _registers[13] -= 4;
                    _memory.WriteU32(_registers[13], _registers[i]);
                }
            }
        }

        private void OpPop()
        {
            for (var i = 0; i < 8; i++)
            {
                if (((_curInstruction >> i) & 1) != 0)
                {
                    _registers[i] = _memory.ReadU32(_registers[13]);
                    _registers[13] += 4;
                }
            }

            _parent.Cycles--;
        }

        private void OpPopPc()
        {
            for (var i = 0; i < 8; i++)
            {
                if (((_curInstruction >> i) & 1) != 0)
                {
                    _registers[i] = _memory.ReadU32(_registers[13]);
                    _registers[13] += 4;
                }
            }

            _registers[15] = _memory.ReadU32(_registers[13]) & (~1U);
            _registers[13] += 4;

            // ARM9 check here

            FlushQueue();

            _parent.Cycles--;
        }

        private void OpStmia()
        {
            var rn = (_curInstruction >> 8) & 0x7;

            for (var i = 0; i < 8; i++)
            {
                if (((_curInstruction >> i) & 1) != 0)
                {
                    _memory.WriteU32(_registers[rn] & (~3U), _registers[i]);
                    _registers[rn] += 4;
                }
            }
        }

        private void OpLdmia()
        {
            var rn = (_curInstruction >> 8) & 0x7;

            var address = _registers[rn];

            for (var i = 0; i < 8; i++)
            {
                if (((_curInstruction >> i) & 1) != 0)
                {
                    _registers[i] = _memory.ReadU32Aligned(address & (~3U));
                    address += 4;
                }
            }

            if (((_curInstruction >> rn) & 1) == 0)
            {
                _registers[rn] = address;
            }
        }

        private void OpBCond()
        {
            uint cond = 0;
            switch ((_curInstruction >> 8) & 0xF)
            {
                case CondAl: cond = 1; break;
                case CondEq: cond = _zero; break;
                case CondNe: cond = 1 - _zero; break;
                case CondCs: cond = _carry; break;
                case CondCc: cond = 1 - _carry; break;
                case CondMi: cond = _negative; break;
                case CondPl: cond = 1 - _negative; break;
                case CondVs: cond = _overflow; break;
                case CondVc: cond = 1 - _overflow; break;
                case CondHi: cond = _carry & (1 - _zero); break;
                case CondLs: cond = (1 - _carry) | _zero; break;
                case CondGe: cond = (1 - _negative) ^ _overflow; break;
                case CondLt: cond = _negative ^ _overflow; break;
                case CondGt: cond = (1 - _zero) & (_negative ^ (1 - _overflow)); break;
                case CondLe: cond = (_negative ^ _overflow) | _zero; break;
            }

            if (cond == 1)
            {
                var offset = (uint)(_curInstruction & 0xFF);
                if ((offset & (1 << 7)) != 0) offset |= 0xFFFFFF00;

                _registers[15] += offset << 1;

                FlushQueue();
            }
        }

        private void OpSwi()
        {
            _registers[15] -= 4U;
            _parent.EnterException(Arm7Processor.SVC, 0x8, false, false);
        }

        private void OpB()
        {
            var offset = (uint)(_curInstruction & 0x7FF);
            if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;

            _registers[15] += offset << 1;

            FlushQueue();
        }

        private void OpBl1()
        {
            var offset = (uint)(_curInstruction & 0x7FF);
            if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;

            _registers[14] = _registers[15] + (offset << 12);
        }

        private void OpBl2()
        {
            var tmp = _registers[15];
            _registers[15] = _registers[14] + (uint)((_curInstruction & 0x7FF) << 1);
            _registers[14] = (tmp - 2U) | 1;

            FlushQueue();
        }

        private void OpUnd()
        {
            throw new Exception("Unknown opcode");
        }
        #endregion

        private void PackFlags()
        {
            _parent.CPSR &= 0x0FFFFFFF;
            _parent.CPSR |= _negative << Arm7Processor.N_BIT;
            _parent.CPSR |= _zero << Arm7Processor.Z_BIT;
            _parent.CPSR |= _carry << Arm7Processor.C_BIT;
            _parent.CPSR |= _overflow << Arm7Processor.V_BIT;
        }

        private void UnpackFlags()
        {
            _negative = (_parent.CPSR >> Arm7Processor.N_BIT) & 1;
            _zero = (_parent.CPSR >> Arm7Processor.Z_BIT) & 1;
            _carry = (_parent.CPSR >> Arm7Processor.C_BIT) & 1;
            _overflow = (_parent.CPSR >> Arm7Processor.V_BIT) & 1;
        }

        private void FlushQueue()
        {
            _instructionQueue = _memory.ReadU16(_registers[15]);
            _registers[15] += 2;
        }
    }
}
