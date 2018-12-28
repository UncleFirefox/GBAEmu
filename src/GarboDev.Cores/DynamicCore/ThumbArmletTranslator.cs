//#define ARM_DEBUG

using System;
using GarboDev.CrossCutting;

namespace GarboDev.Cores.DynamicCore
{
    public class ThumbArmletTranslator
    {
        private const int COND_EQ = 0;	    // Z set
        private const int COND_NE = 1;	    // Z clear
        private const int COND_CS = 2;	    // C set
        private const int COND_CC = 3;	    // C clear
        private const int COND_MI = 4;	    // N set
        private const int COND_PL = 5;	    // N clear
        private const int COND_VS = 6;	    // V set
        private const int COND_VC = 7;	    // V clear
        private const int COND_HI = 8;	    // C set and Z clear
        private const int COND_LS = 9;	    // C clear or Z set
        private const int COND_GE = 10;	    // N equals V
        private const int COND_LT = 11;	    // N not equal to V
        private const int COND_GT = 12; 	// Z clear AND (N equals V)
        private const int COND_LE = 13; 	// Z set OR (N not equal to V)
        private const int COND_AL = 14; 	// Always
        private const int COND_NV = 15; 	// Never execute

        private const int OP_AND = 0x0;
        private const int OP_EOR = 0x1;
        private const int OP_LSL = 0x2;
        private const int OP_LSR = 0x3;
        private const int OP_ASR = 0x4;
        private const int OP_ADC = 0x5;
        private const int OP_SBC = 0x6;
        private const int OP_ROR = 0x7;
        private const int OP_TST = 0x8;
        private const int OP_NEG = 0x9;
        private const int OP_CMP = 0xA;
        private const int OP_CMN = 0xB;
        private const int OP_ORR = 0xC;
        private const int OP_MUL = 0xD;
        private const int OP_BIC = 0xE;
        private const int OP_MVN = 0xF;

        private readonly Arm7Processor parent;
        private readonly Memory memory;
        private readonly uint[] registers;

        // CPU flags
        private uint zero, carry, negative, overflow;
        private ushort curInstruction, instructionQueue;

        private delegate void ExecuteInstruction();
        private readonly ExecuteInstruction[] NormalOps;

        public ThumbArmletTranslator(Arm7Processor parent, Memory memory)
        {
            this.parent = parent;
            this.memory = memory;
            registers = this.parent.Registers;

            NormalOps = new ExecuteInstruction[256]
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

            curInstruction = instructionQueue;
            instructionQueue = memory.ReadU16(registers[15]);
            registers[15] += 2;

            // Execute the instruction
            NormalOps[curInstruction >> 8]();

            parent.Cycles -= memory.WaitCycles;

            if ((parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
            {
                if ((curInstruction >> 8) != 0xDF) parent.ReloadQueue();
            }

            PackFlags();
        }

        public void Execute()
        {
            UnpackFlags();

            while (parent.Cycles > 0)
            {
                curInstruction = instructionQueue;
                instructionQueue = memory.ReadU16(registers[15]);
                registers[15] += 2;

                // Execute the instruction
                NormalOps[curInstruction >> 8]();

                parent.Cycles -= memory.WaitCycles;

                if ((parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
                {
                    if ((curInstruction >> 8) != 0xDF) parent.ReloadQueue();
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
            overflow = ((a & b & ~r) | (~a & ~b & r)) >> 31;
            carry = ((a & b) | (a & ~r) | (b & ~r)) >> 31;
        }

        public void OverflowCarrySub(uint a, uint b, uint r)
        {
            overflow = ((a & ~b & ~r) | (~a & b & r)) >> 31;
            carry = ((a & ~b) | (a & ~r) | (~b & ~r)) >> 31;
        }
        #endregion

        #region Opcodes
        private void OpLslImm()
        {
            // 0x00 - 0x07
            // lsl rd, rm, #immed
            var rd = curInstruction & 0x7;
            var rm = (curInstruction >> 3) & 0x7;
            var immed = (curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                registers[rd] = registers[rm];
            }
            else
            {
                carry = (registers[rm] >> (32 - immed)) & 0x1;
                registers[rd] = registers[rm] << immed;
            }

            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpLsrImm()
        {
            // 0x08 - 0x0F
            // lsr rd, rm, #immed
            var rd = curInstruction & 0x7;
            var rm = (curInstruction >> 3) & 0x7;
            var immed = (curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                carry = registers[rm] >> 31;
                registers[rd] = 0;
            }
            else
            {
                carry = (registers[rm] >> (immed - 1)) & 0x1;
                registers[rd] = registers[rm] >> immed;
            }

            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAsrImm()
        {
            // asr rd, rm, #immed
            var rd = curInstruction & 0x7;
            var rm = (curInstruction >> 3) & 0x7;
            var immed = (curInstruction >> 6) & 0x1F;

            if (immed == 0)
            {
                carry = registers[rm] >> 31;
                if (carry == 1) registers[rd] = 0xFFFFFFFF;
                else registers[rd] = 0;
            }
            else
            {
                carry = (registers[rm] >> (immed - 1)) & 0x1;
                registers[rd] = (uint)(((int)registers[rm]) >> immed);
            }

            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAddRegReg()
        {
            // add rd, rn, rm
            var rd = curInstruction & 0x7;
            var rn = (curInstruction >> 3) & 0x7;
            var rm = (curInstruction >> 6) & 0x7;

            var orn = registers[rn];
            var orm = registers[rm];

            registers[rd] = orn + orm;

            OverflowCarryAdd(orn, orm, registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubRegReg()
        {
            // sub rd, rn, rm
            var rd = curInstruction & 0x7;
            var rn = (curInstruction >> 3) & 0x7;
            var rm = (curInstruction >> 6) & 0x7;

            var orn = registers[rn];
            var orm = registers[rm];

            registers[rd] = orn - orm;

            OverflowCarrySub(orn, orm, registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpAddRegImm()
        {
            // add rd, rn, #immed
            var rd = curInstruction & 0x7;
            var rn = (curInstruction >> 3) & 0x7;
            var immed = (uint)((curInstruction >> 6) & 0x7);

            var orn = registers[rn];

            registers[rd] = orn + immed;

            OverflowCarryAdd(orn, immed, registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubRegImm()
        {
            // sub rd, rn, #immed
            var rd = curInstruction & 0x7;
            var rn = (curInstruction >> 3) & 0x7;
            var immed = (uint)((curInstruction >> 6) & 0x7);

            var orn = registers[rn];

            registers[rd] = orn - immed;

            OverflowCarrySub(orn, immed, registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpMovImm()
        {
            // mov rd, #immed
            var rd = (curInstruction >> 8) & 0x7;

            registers[rd] = (uint)(curInstruction & 0xFF);

            negative = 0;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpCmpImm()
        {
            // cmp rn, #immed
            var rn = (curInstruction >> 8) & 0x7;

            var alu = registers[rn] - (uint)(curInstruction & 0xFF);

            OverflowCarrySub(registers[rn], (uint)(curInstruction & 0xFF), alu);
            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
        }

        private void OpAddImm()
        {
            // add rd, #immed
            var rd = (curInstruction >> 8) & 0x7;

            var ord = registers[rd];

            registers[rd] += (uint)(curInstruction & 0xFF);

            OverflowCarryAdd(ord, (uint)(curInstruction & 0xFF), registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpSubImm()
        {
            // sub rd, #immed
            var rd = (curInstruction >> 8) & 0x7;

            var ord = registers[rd];

            registers[rd] -= (uint)(curInstruction & 0xFF);

            OverflowCarrySub(ord, (uint)(curInstruction & 0xFF), registers[rd]);
            negative = registers[rd] >> 31;
            zero = registers[rd] == 0 ? 1U : 0U;
        }

        private void OpArith()
        {
            var rd = curInstruction & 0x7;
            var rn = registers[(curInstruction >> 3) & 0x7];

            uint orig, alu;
            int shiftAmt;

            switch ((curInstruction >> 6) & 0xF)
            {
                case OP_ADC:
                    orig = registers[rd];
                    registers[rd] += rn + carry;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    OverflowCarryAdd(orig, rn, registers[rd]);
                    break;

                case OP_AND:
                    registers[rd] &= rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_ASR:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        carry = (registers[rd] >> (shiftAmt - 1)) & 0x1;
                        registers[rd] = (uint)(((int)registers[rd]) >> shiftAmt);
                    }
                    else
                    {
                        carry = (registers[rd] >> 31) & 1;
                        if (carry == 1) registers[rd] = 0xFFFFFFFF;
                        else registers[rd] = 0;
                    }

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_BIC:
                    registers[rd] &= ~rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_CMN:
                    alu = registers[rd] + rn;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    OverflowCarryAdd(registers[rd], rn, alu);
                    break;

                case OP_CMP:
                    alu = registers[rd] - rn;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    OverflowCarrySub(registers[rd], rn, alu);
                    break;

                case OP_EOR:
                    registers[rd] ^= rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_LSL:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        carry = (registers[rd] >> (32 - shiftAmt)) & 0x1;
                        registers[rd] <<= shiftAmt;
                    }
                    else if (shiftAmt == 32)
                    {
                        carry = registers[rd] & 0x1;
                        registers[rd] = 0;
                    }
                    else
                    {
                        carry = 0;
                        registers[rd] = 0;
                    }

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_LSR:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if (shiftAmt < 32)
                    {
                        carry = (registers[rd] >> (shiftAmt - 1)) & 0x1;
                        registers[rd] >>= shiftAmt;
                    }
                    else if (shiftAmt == 32)
                    {
                        carry = (registers[rd] >> 31) & 0x1;
                        registers[rd] = 0;
                    }
                    else
                    {
                        carry = 0;
                        registers[rd] = 0;
                    }

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_MUL:
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

                    parent.Cycles -= mulCycles;

                    registers[rd] *= rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_MVN:
                    registers[rd] = ~rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_NEG:
                    registers[rd] = 0 - rn;

                    OverflowCarrySub(0, rn, registers[rd]);
                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_ORR:
                    registers[rd] |= rn;

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_ROR:
                    shiftAmt = (int)(rn & 0xFF);
                    if (shiftAmt == 0)
                    {
                        // Do nothing
                    }
                    else if ((shiftAmt & 0x1F) == 0)
                    {
                        carry = registers[rd] >> 31;
                    }
                    else
                    {
                        shiftAmt &= 0x1F;
                        carry = (registers[rd] >> (shiftAmt - 1)) & 0x1;
                        registers[rd] = (registers[rd] >> shiftAmt) | (registers[rd] << (32 - shiftAmt));
                    }

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    break;

                case OP_SBC:
                    orig = registers[rd];
                    registers[rd] = (registers[rd] - rn) - (1U - carry);

                    negative = registers[rd] >> 31;
                    zero = registers[rd] == 0 ? 1U : 0U;
                    OverflowCarrySub(orig, rn, registers[rd]);
                    break;

                case OP_TST:
                    alu = registers[rd] & rn;

                    negative = alu >> 31;
                    zero = alu == 0 ? 1U : 0U;
                    break;

                default:
                    throw new Exception("The coder screwed up on the thumb alu op...");
            }
        }

        private void OpAddHi()
        {
            var rd = ((curInstruction & (1 << 7)) >> 4) | (curInstruction & 0x7);
            var rm = (curInstruction >> 3) & 0xF;

            registers[rd] += registers[rm];

            if (rd == 15)
            {
                registers[rd] &= ~1U;
                FlushQueue();
            }
        }

        private void OpCmpHi()
        {
            var rd = ((curInstruction & (1 << 7)) >> 4) | (curInstruction & 0x7);
            var rm = (curInstruction >> 3) & 0xF;

            var alu = registers[rd] - registers[rm];

            negative = alu >> 31;
            zero = alu == 0 ? 1U : 0U;
            OverflowCarrySub(registers[rd], registers[rm], alu);
        }

        private void OpMovHi()
        {
            var rd = ((curInstruction & (1 << 7)) >> 4) | (curInstruction & 0x7);
            var rm = (curInstruction >> 3) & 0xF;

            registers[rd] = registers[rm];

            if (rd == 15)
            {
                registers[rd] &= ~1U;
                FlushQueue();
            }
        }

        private void OpBx()
        {
            var rm = (curInstruction >> 3) & 0xf;

            PackFlags();

            parent.CPSR &= ~Arm7Processor.T_MASK;
            parent.CPSR |= (registers[rm] & 1) << Arm7Processor.T_BIT;

            registers[15] = registers[rm] & (~1U);

            UnpackFlags();

            // Check for branch back to Arm Mode
            if ((parent.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK)
            {
                return;
            }

            FlushQueue();
        }

        private void OpLdrPc()
        {
            var rd = (curInstruction >> 8) & 0x7;

            registers[rd] = memory.ReadU32((registers[15] & ~2U) + (uint)((curInstruction & 0xFF) * 4));

            parent.Cycles--;
        }

        private void OpStrReg()
        {
            memory.WriteU32(registers[(curInstruction >> 3) & 0x7] + registers[(curInstruction >> 6) & 0x7],
                registers[curInstruction & 0x7]);
        }

        private void OpStrhReg()
        {
            memory.WriteU16(registers[(curInstruction >> 3) & 0x7] + registers[(curInstruction >> 6) & 0x7],
                (ushort)(registers[curInstruction & 0x7] & 0xFFFF));
        }

        private void OpStrbReg()
        {
            memory.WriteU8(registers[(curInstruction >> 3) & 0x7] + registers[(curInstruction >> 6) & 0x7],
                (byte)(registers[curInstruction & 0x7] & 0xFF));
        }

        private void OpLdrsbReg()
        {
            registers[curInstruction & 0x7] =
                memory.ReadU8(registers[(curInstruction >> 3) & 0x7] + registers[(curInstruction >> 6) & 0x7]);

            if ((registers[curInstruction & 0x7] & (1 << 7)) != 0)
            {
                registers[curInstruction & 0x7] |= 0xFFFFFF00;
            }

            parent.Cycles--;
        }

        private void OpLdrReg()
        {
            registers[curInstruction & 0x7] =
                memory.ReadU32(registers[(curInstruction >> 3) & 0x7] + registers[(curInstruction >> 6) & 0x7]);

            parent.Cycles--;
        }

        private void OpLdrhReg()
        {
            registers[curInstruction & 0x7] =
                memory.ReadU16(registers[(curInstruction >> 3) & 0x7] + registers[(curInstruction >> 6) & 0x7]);

            parent.Cycles--;
        }

        private void OpLdrbReg()
        {
            registers[curInstruction & 0x7] =
                memory.ReadU8(registers[(curInstruction >> 3) & 0x7] + registers[(curInstruction >> 6) & 0x7]);

            parent.Cycles--;
        }

        private void OpLdrshReg()
        {
            registers[curInstruction & 0x7] =
                memory.ReadU16(registers[(curInstruction >> 3) & 0x7] + registers[(curInstruction >> 6) & 0x7]);

            if ((registers[curInstruction & 0x7] & (1 << 15)) != 0)
            {
                registers[curInstruction & 0x7] |= 0xFFFF0000;
            }

            parent.Cycles--;
        }

        private void OpStrImm()
        {
            memory.WriteU32(registers[(curInstruction >> 3) & 0x7] + (uint)(((curInstruction >> 6) & 0x1F) * 4),
                registers[curInstruction & 0x7]);
        }

        private void OpLdrImm()
        {
            registers[curInstruction & 0x7] =
                memory.ReadU32(registers[(curInstruction >> 3) & 0x7] + (uint)(((curInstruction >> 6) & 0x1F) * 4));

            parent.Cycles--;
        }

        private void OpStrbImm()
        {
            memory.WriteU8(registers[(curInstruction >> 3) & 0x7] + (uint)((curInstruction >> 6) & 0x1F),
                (byte)(registers[curInstruction & 0x7] & 0xFF));
        }

        private void OpLdrbImm()
        {
            registers[curInstruction & 0x7] =
                memory.ReadU8(registers[(curInstruction >> 3) & 0x7] + (uint)((curInstruction >> 6) & 0x1F));

            parent.Cycles--;
        }

        private void OpStrhImm()
        {
            memory.WriteU16(registers[(curInstruction >> 3) & 0x7] + (uint)(((curInstruction >> 6) & 0x1F) * 2),
                (ushort)(registers[curInstruction & 0x7] & 0xFFFF));
        }

        private void OpLdrhImm()
        {
            registers[curInstruction & 0x7] =
                memory.ReadU16(registers[(curInstruction >> 3) & 0x7] + (uint)(((curInstruction >> 6) & 0x1F) * 2));

            parent.Cycles--;
        }

        private void OpStrSp()
        {
            memory.WriteU32(registers[13] + (uint)((curInstruction & 0xFF) * 4),
                registers[(curInstruction >> 8) & 0x7]);
        }

        private void OpLdrSp()
        {
            registers[(curInstruction >> 8) & 0x7] =
                memory.ReadU32(registers[13] + (uint)((curInstruction & 0xFF) * 4));
        }

        private void OpAddPc()
        {
            registers[(curInstruction >> 8) & 0x7] =
                (registers[15] & ~2U) + (uint)((curInstruction & 0xFF) * 4);
        }

        private void OpAddSp()
        {
            registers[(curInstruction >> 8) & 0x7] =
                registers[13] + (uint)((curInstruction & 0xFF) * 4);
        }

        private void OpSubSp()
        {
            if ((curInstruction & (1 << 7)) != 0)
                registers[13] -= (uint)((curInstruction & 0x7F) * 4);
            else
                registers[13] += (uint)((curInstruction & 0x7F) * 4);
        }

        private void OpPush()
        {
            for (var i = 7; i >= 0; i--)
            {
                if (((curInstruction >> i) & 1) != 0)
                {
                    registers[13] -= 4;
                    memory.WriteU32(registers[13], registers[i]);
                }
            }
        }

        private void OpPushLr()
        {
            registers[13] -= 4;
            memory.WriteU32(registers[13], registers[14]);

            for (var i = 7; i >= 0; i--)
            {
                if (((curInstruction >> i) & 1) != 0)
                {
                    registers[13] -= 4;
                    memory.WriteU32(registers[13], registers[i]);
                }
            }
        }

        private void OpPop()
        {
            for (var i = 0; i < 8; i++)
            {
                if (((curInstruction >> i) & 1) != 0)
                {
                    registers[i] = memory.ReadU32(registers[13]);
                    registers[13] += 4;
                }
            }

            parent.Cycles--;
        }

        private void OpPopPc()
        {
            for (var i = 0; i < 8; i++)
            {
                if (((curInstruction >> i) & 1) != 0)
                {
                    registers[i] = memory.ReadU32(registers[13]);
                    registers[13] += 4;
                }
            }

            registers[15] = memory.ReadU32(registers[13]) & (~1U);
            registers[13] += 4;

            // ARM9 check here

            FlushQueue();

            parent.Cycles--;
        }

        private void OpStmia()
        {
            var rn = (curInstruction >> 8) & 0x7;

            for (var i = 0; i < 8; i++)
            {
                if (((curInstruction >> i) & 1) != 0)
                {
                    memory.WriteU32(registers[rn] & (~3U), registers[i]);
                    registers[rn] += 4;
                }
            }
        }

        private void OpLdmia()
        {
            var rn = (curInstruction >> 8) & 0x7;

            var address = registers[rn];

            for (var i = 0; i < 8; i++)
            {
                if (((curInstruction >> i) & 1) != 0)
                {
                    registers[i] = memory.ReadU32(address & (~3U));
                    address += 4;
                }
            }

            if (((curInstruction >> rn) & 1) == 0)
            {
                registers[rn] = address;
            }
        }

        private void OpBCond()
        {
            uint cond = 0;
            switch ((curInstruction >> 8) & 0xF)
            {
                case COND_AL: cond = 1; break;
                case COND_EQ: cond = zero; break;
                case COND_NE: cond = 1 - zero; break;
                case COND_CS: cond = carry; break;
                case COND_CC: cond = 1 - carry; break;
                case COND_MI: cond = negative; break;
                case COND_PL: cond = 1 - negative; break;
                case COND_VS: cond = overflow; break;
                case COND_VC: cond = 1 - overflow; break;
                case COND_HI: cond = carry & (1 - zero); break;
                case COND_LS: cond = (1 - carry) | zero; break;
                case COND_GE: cond = (1 - negative) ^ overflow; break;
                case COND_LT: cond = negative ^ overflow; break;
                case COND_GT: cond = (1 - zero) & (negative ^ (1 - overflow)); break;
                case COND_LE: cond = (negative ^ overflow) | zero; break;
            }

            if (cond == 1)
            {
                var offset = (uint)(curInstruction & 0xFF);
                if ((offset & (1 << 7)) != 0) offset |= 0xFFFFFF00;

                registers[15] += offset << 1;

                FlushQueue();
            }
        }

        private void OpSwi()
        {
            registers[15] -= 4U;
            parent.EnterException(Arm7Processor.SVC, 0x8, false, false);
        }

        private void OpB()
        {
            var offset = (uint)(curInstruction & 0x7FF);
            if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;

            registers[15] += offset << 1;

            FlushQueue();
        }

        private void OpBl1()
        {
            var offset = (uint)(curInstruction & 0x7FF);
            if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;

            registers[14] = registers[15] + (offset << 12);
        }

        private void OpBl2()
        {
            var tmp = registers[15];
            registers[15] = registers[14] + (uint)((curInstruction & 0x7FF) << 1);
            registers[14] = (tmp - 2U) | 1;

            FlushQueue();
        }

        private void OpUnd()
        {
            throw new Exception("Unknown opcode");
        }
        #endregion

        private void PackFlags()
        {
            parent.CPSR &= 0x0FFFFFFF;
            parent.CPSR |= negative << Arm7Processor.N_BIT;
            parent.CPSR |= zero << Arm7Processor.Z_BIT;
            parent.CPSR |= carry << Arm7Processor.C_BIT;
            parent.CPSR |= overflow << Arm7Processor.V_BIT;
        }

        private void UnpackFlags()
        {
            negative = (parent.CPSR >> Arm7Processor.N_BIT) & 1;
            zero = (parent.CPSR >> Arm7Processor.Z_BIT) & 1;
            carry = (parent.CPSR >> Arm7Processor.C_BIT) & 1;
            overflow = (parent.CPSR >> Arm7Processor.V_BIT) & 1;
        }

        private void FlushQueue()
        {
            instructionQueue = memory.ReadU16(registers[15]);
            registers[15] += 2;
        }
    }
}
