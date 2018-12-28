using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using GarboDev.Cores;
using GarboDev.CrossCutting;

namespace GarboDev.WinForms
{
    partial class DissassemblyWindow : Form
    {
        private uint curPC = 0;
        private bool inArm = true;
        private bool prevInArm = true;
        private GbaManager.GbaManager gbaManager = null;

        public DissassemblyWindow(GbaManager.GbaManager gbaManager)
        {
            InitializeComponent();

            this.DisAsmScrollBar.Minimum = 0;
            this.DisAsmScrollBar.Maximum = (int)0x8ffffff;

            this.gbaManager = gbaManager;

            this.gbaManager.OnCpuUpdate += new GbaManager.GbaManager.CpuUpdateDelegate(Update);

            MenuItem setBreakpoint = new MenuItem("Set/Remove Breakpoint", new EventHandler(OnSetBreakpoint));
            MenuItem[] menuItems = new MenuItem[]
            {
                setBreakpoint
            };

            this.disassembly.ContextMenu = new ContextMenu(menuItems);
            this.disassembly.DrawItem += new DrawItemEventHandler(OnDrawItem);
        }

        private Arm7Processor processor = null;
        private Memory memory = null;

        public void Update(Arm7Processor processor, Memory memory)
        {
            this.processor = processor;
            this.memory = memory;

            if (processor != null)
            {
                this.inArm = (processor.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK;
            }

            this.UpdateRegisters(processor);

            this.UpdateDisassembly(processor, memory);

            this.prevInArm = this.inArm;
        }

        private void UpdateRegisters(Arm7Processor processor)
        {
            this.registerNames.BeginUpdate();
            this.registerValues.BeginUpdate();

            this.registerNames.Items.Clear();
            this.registerValues.Items.Clear();

            if (processor != null)
            {
                for (int i = 0; i <= 14; i++)
                {
                    this.registerNames.Items.Add("R" + i);
                    this.registerValues.Items.Add(string.Format("0x{0:X8}", processor.Registers[i]));
                }

                this.registerNames.Items.Add("");
                this.registerValues.Items.Add("");
                this.registerNames.Items.Add("R15 (PC)");
                this.registerValues.Items.Add(string.Format("0x{0:X8}", processor.Registers[15] + 4U));
                this.registerNames.Items.Add("Flags");
                this.registerValues.Items.Add(string.Format("{0}{1}{2}{3}{4}",
                    (processor.CPSR & Arm7Processor.N_MASK) == Arm7Processor.N_MASK ? "N" : "n",
                    (processor.CPSR & Arm7Processor.Z_MASK) == Arm7Processor.Z_MASK ? "Z" : "z",
                    (processor.CPSR & Arm7Processor.C_MASK) == Arm7Processor.C_MASK ? "C" : "c",
                    (processor.CPSR & Arm7Processor.V_MASK) == Arm7Processor.V_MASK ? "V" : "v",
                    (processor.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK ? "T" : "t"));
                this.registerNames.Items.Add("CPSR");
                this.registerValues.Items.Add(string.Format("0x{0:X8}", processor.CPSR));
                if (processor.SPSRExists)
                {
                    this.registerNames.Items.Add("SPSR");
                    this.registerValues.Items.Add(string.Format("0x{0:X8}", processor.SPSR));
                }
            }

            this.registerValues.EndUpdate();
            this.registerNames.EndUpdate();
        }

        private void UpdateDisassembly(Arm7Processor processor, Memory memory)
        {
            if (processor != null && memory != null)
            {
                if (!this.inArm)
                {
                    uint pc = (uint)(processor.Registers[15] - 0x2U);
                    int numItems = this.disassembly.Height / this.disassembly.ItemHeight;

                    if (pc > this.curPC && pc < this.curPC + numItems * 2 - 2 && !this.prevInArm && this.disassembly.Items.Count > 0)
                    {
                        this.disassembly.SelectedIndex = (int)(pc - this.curPC) / 2;
                        return;
                    }

                    this.curPC = pc - 0x2U;

                    this.RefreshDisassembly(processor, memory);

                    this.disassembly.SelectedIndex = (int)(pc - this.curPC) / 2;
                }
                else
                {
                    uint pc = (uint)(processor.Registers[15] - 0x4U);
                    int numItems = this.disassembly.Height / this.disassembly.ItemHeight;

                    if (pc > this.curPC && pc < this.curPC + numItems * 4 - 4 && this.prevInArm && this.disassembly.Items.Count > 0)
                    {
                        this.disassembly.SelectedIndex = (int)(pc - this.curPC) / 4;
                        return;
                    }

                    this.curPC = pc - 0x4U;

                    this.RefreshDisassembly(processor, memory);

                    this.disassembly.SelectedIndex = (int)(pc - this.curPC) / 4;
                }

                this.DisAsmScrollBar.Value = ((int)this.curPC) < 0 ? 0 : (int)this.curPC;
            }
        }

        private void RefreshDisassembly(Arm7Processor processor, Memory memory)
        {
            this.curLocation.Text = string.Format("{0:X8}", this.curPC);

            int numItems = this.disassembly.Height / this.disassembly.ItemHeight;
            this.disassembly.BeginUpdate();

            this.disassembly.Items.Clear();

            for (int i = 0; i < numItems; i++)
            {
                StringBuilder sb = new StringBuilder();

                uint memAddress;
                if (this.inArm)
                {
                    memAddress = (uint)(this.curPC + (i * 4));
                }
                else
                {
                    memAddress = (uint)(this.curPC + (i * 2));
                }

                sb.Append(string.Format("{0:X8}", memAddress));
                sb.Append("  ");
                if (this.inArm)
                {
                    sb.Append(this.DisassembleArmOpcode(memory, memAddress));
                }
                else
                {
                    sb.Append(this.DisassembleThumbOpcode(memory, memAddress));
                }
                this.disassembly.Items.Add(sb.ToString());
            }

            if (this.inArm)
            {
                uint pc = (uint)(processor.Registers[15] - 0x4U);
                if (pc >= this.curPC && pc < this.curPC + numItems * 4)
                {
                    this.disassembly.SelectedIndex = (int)(pc - this.curPC) / 4;
                }
            }
            else
            {
                uint pc = (uint)(processor.Registers[15] - 0x2U);
                if (pc >= this.curPC && pc < this.curPC + numItems * 2)
                {
                    this.disassembly.SelectedIndex = (int)(pc - this.curPC) / 2;
                }
            }

            this.disassembly.EndUpdate();
        }

        private void step_Click(object sender, EventArgs e)
        {
            this.gbaManager.Step();
        }

        private void go_Click(object sender, EventArgs e)
        {
            this.gbaManager.Resume();
        }

        private void ScanlineStep_Click(object sender, EventArgs e)
        {
            this.gbaManager.StepScanline();
        }

        private void gotoLocation_Click(object sender, EventArgs e)
        {
            this.curPC = UInt32.Parse(this.curLocation.Text, NumberStyles.HexNumber);
            this.RefreshDisassembly(this.processor, this.memory);
        }

        private void gotoPc_Click(object sender, EventArgs e)
        {
            if (!this.inArm)
            {
                this.curPC = (uint)(processor.Registers[15] - 0x4U);
            }
            else
            {
                this.curPC = (uint)(processor.Registers[15] - 0x8U);
            }

            this.RefreshDisassembly(this.processor, this.memory);
        }

        private void displayArmRadio_CheckedChanged(object sender, EventArgs e)
        {
            this.inArm = true;

            this.RefreshDisassembly(processor, memory);

            this.prevInArm = this.inArm;
        }

        private void displayThumbRadio_CheckedChanged(object sender, EventArgs e)
        {
            this.inArm = false;

            this.RefreshDisassembly(processor, memory);

            this.prevInArm = this.inArm;
        }

        private void displayAutoRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (processor != null)
            {
                this.inArm = (processor.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK;
            }

            this.RefreshDisassembly(processor, memory);

            this.prevInArm = this.inArm;
        }

        private void OnSetBreakpoint(object sender, EventArgs eventArgs)
        {
            Point point = Cursor.Position;
            point.Y -= 6;
            int selected = this.disassembly.IndexFromPoint(this.disassembly.PointToClient(point));
            uint address;
            if (this.inArm)
            {
                address = (uint)(this.curPC + selected * 4);
            }
            else
            {
                address = (uint)(this.curPC + selected * 2);
            }
            if (this.gbaManager.Breakpoints.ContainsKey(address))
            {
                this.gbaManager.Breakpoints.Remove(address);
            }
            else
            {
                this.gbaManager.Breakpoints[address] = true;
            }


            this.disassembly.Invalidate();
        }

        private void OnDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            PointF topLeft = new PointF(e.Bounds.Left, e.Bounds.Top);
            float bpWidth = this.disassembly.ItemHeight - 4;

            if (this.gbaManager.Breakpoints != null)
            {
                uint scale = 4;
                if (!this.inArm) scale = 2;
                if (this.gbaManager.Breakpoints.ContainsKey((uint)(this.curPC + e.Index * scale)))
                {
                    e.Graphics.FillEllipse(Brushes.Red, topLeft.X + 2, topLeft.Y + 2, bpWidth, bpWidth);
                }
            }

            topLeft.X += bpWidth;

            Brush textBrush = Brushes.Black;
            Brush opBrush = Brushes.Blue;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                opBrush = Brushes.Red;
                textBrush = Brushes.White;
            }

            string[] textChunks = this.disassembly.Items[e.Index].ToString().Split(' ');
            List<string> tmpChunk = new List<string>();
            int chunkCount = 0;
            for (int i = textChunks.Length - 1; i >= 0; i--)
            {
                if (textChunks[i].Length == 0) chunkCount++;
                else
                {
                    tmpChunk.Add(textChunks[i].PadRight(textChunks[i].Length + chunkCount, ' '));
                    chunkCount = 0;
                }
            }
            tmpChunk.Reverse();
            textChunks = tmpChunk.ToArray();

            e.Graphics.DrawString(textChunks[0], e.Font, Brushes.Gray, topLeft);
            topLeft.X += e.Graphics.MeasureString(textChunks[0], e.Font).Width;
            e.Graphics.DrawString(textChunks[1], e.Font, opBrush, topLeft);
            topLeft.X += 60;
            for (int i = 2; i < textChunks.Length; i++)
            {
                e.Graphics.DrawString(textChunks[i], e.Font, textBrush, topLeft);
                topLeft.X += e.Graphics.MeasureString(textChunks[i], e.Font).Width;
            }
        }

        private void DisAsmScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            switch (e.Type)
            {
                case ScrollEventType.SmallIncrement:
                    this.curPC += 4;
                    break;
                case ScrollEventType.SmallDecrement:
                    this.curPC -= 4;
                    break;
                case ScrollEventType.LargeIncrement:
                    this.curPC += 0x100;
                    break;
                case ScrollEventType.LargeDecrement:
                    this.curPC -= 0x100;
                    break;
                case ScrollEventType.ThumbTrack:
                    this.curPC = (uint)e.NewValue;
                    break;
            }

            this.RefreshDisassembly(this.processor, this.memory);
        }

        #region Thumb Opcode Formatting
        private string RegListThumb(ushort opcode)
        {
            int start = -1;
            string res = "";
            for (int i = 0; i < 9; i++)
            {
                if (((opcode >> i) & 1) != 0 && i != 8)
                {
                    if (start == -1) start = i;
                } else
                {
                    if (start != -1)
                    {
                        int end = i - 1;
                        if (start == end) res += "r" + start + ",";
                        else res += string.Format("r{0}-r{1},", start, end);
                        start = -1;
                    }
                }
            }
            if (res.Length == 0) return "";
            if (res[res.Length - 1] == ',') res = res.Remove(res.Length - 1);
            return res;
        }

        private string DisassembleThumbOpcode(Memory memory, uint pc)
        {
            string[] aluOps = new string[16]
                {
                    "and","eor","lsl","lsr","asr","adc","sbc","ror","tst","neg","cmp","cmn","orr","mul","bic","mvn"
                };

            string[] cc = new string[]
            {
	            "eq","ne","cs","cc","mi","pl","vs","vc","hi","ls","ge","lt","gt","le","","nv"
            };

            ushort opcode = memory.ReadU16Debug(pc);

            switch (opcode >> 8)
            {
                case 0x00:
                case 0x01:
                case 0x02:
                case 0x03:
                case 0x04:
                case 0x05:
                case 0x06:
                case 0x07:
                    return string.Format("lsl r{0}, r{1}, #{2:X}", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x1F);

                case 0x08:
                case 0x09:
                case 0x0A:
                case 0x0B:
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                    return string.Format("lsr r{0}, r{1}, #{2:X}", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x1F);

                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x17:
                    return string.Format("asr r{0}, r{1}, #{2:X}", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x1F);

                case 0x18:
                case 0x19:
                    return string.Format("add r{0}, r{1}, r{2}", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x1A:
                case 0x1B:
                    return string.Format("sub r{0}, r{1}, r{2}", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x1C:
                case 0x1D:
                    return string.Format("add r{0}, r{1}, #{2:X}", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x1E:
                case 0x1F:
                    return string.Format("sub r{0}, r{1}, #{2:X}", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                    return string.Format("mov r{0}, #{1:X}", (opcode >> 8) & 0x7, opcode & 0xFF);

                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                case 0x2F:
                    return string.Format("cmp r{0}, #{1:X}", (opcode >> 8) & 0x7, opcode & 0xFF);

                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                    return string.Format("add r{0}, #{1:X}", (opcode >> 8) & 0x7, opcode & 0xFF);

                case 0x38:
                case 0x39:
                case 0x3A:
                case 0x3B:
                case 0x3C:
                case 0x3D:
                case 0x3E:
                case 0x3F:
                    return string.Format("sub r{0}, #{1:X}", (opcode >> 8) & 0x7, opcode & 0xFF);

                case 0x40:
                case 0x41:
                case 0x42:
                case 0x43:
                    return string.Format("{0} r{1}, r{2}", aluOps[(opcode >> 6) & 0xf], opcode & 0x7, (opcode >> 3) & 0x7);

                case 0x44:
                    return string.Format("add r{0}, r{1}", (((opcode >> 7) & 1) << 3) | (opcode & 0x7), (opcode >> 3) & 0xF);

                case 0x45:
                    return string.Format("cmp r{0}, r{1}", (((opcode >> 7) & 1) << 3) | (opcode & 0x7), (opcode >> 3) & 0xF);
                
                case 0x46:
                    return string.Format("mov r{0}, r{1}", (((opcode >> 7) & 1) << 3) | (opcode & 0x7), (opcode >> 3) & 0xF);
                
                case 0x47:
                    return string.Format("bx r{0}", (opcode >> 3) & 0xF);

                case 0x48:
                case 0x49:
                case 0x4A:
                case 0x4B:
                case 0x4C:
                case 0x4D:
                case 0x4E:
                case 0x4F:
                    return string.Format("ldr r{0}, [pc, #{1:X}]", (opcode >> 8) & 0x7, (opcode & 0xFF) * 4);

                case 0x50:
                case 0x51:
                    return string.Format("str r{0}, [r{1}, r{2}]", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x52:
                case 0x53:
                    return string.Format("strh r{0}, [r{1}, r{2}]", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x54:
                case 0x55:
                    return string.Format("strb r{0}, [r{1}, r{2}]", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x56:
                case 0x57:
                    return string.Format("ldrsb r{0}, [r{1}, r{2}]", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x58:
                case 0x59:
                    return string.Format("ldr r{0}, [r{1}, r{2}]", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x5A:
                case 0x5B:
                    return string.Format("ldrh r{0}, [r{1}, r{2}]", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x5C:
                case 0x5D:
                    return string.Format("ldrb r{0}, [r{1}, r{2}]", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x5E:
                case 0x5F:
                    return string.Format("ldrsh r{0}, [r{1}, r{2}]", opcode & 0x7, (opcode >> 3) & 0x7, (opcode >> 6) & 0x7);

                case 0x60:
                case 0x61:
                case 0x62:
                case 0x63:
                case 0x64:
                case 0x65:
                case 0x66:
                case 0x67:
                    return string.Format("str r{0}, [r{1}, #{2:X}]", opcode & 0x7, (opcode >> 3) & 0x7, ((opcode >> 6) & 0x1F) << 2);

                case 0x68:
                case 0x69:
                case 0x6A:
                case 0x6B:
                case 0x6C:
                case 0x6D:
                case 0x6E:
                case 0x6F:
                    return string.Format("ldr r{0}, [r{1}, #{2:X}]", opcode & 0x7, (opcode >> 3) & 0x7, ((opcode >> 6) & 0x1F) << 2);

                case 0x70:
                case 0x71:
                case 0x72:
                case 0x73:
                case 0x74:
                case 0x75:
                case 0x76:
                case 0x77:
                    return string.Format("strb r{0}, [r{1}, #{2:X}]", opcode & 0x7, (opcode >> 3) & 0x7, ((opcode >> 6) & 0x1F));

                case 0x78:
                case 0x79:
                case 0x7A:
                case 0x7B:
                case 0x7C:
                case 0x7D:
                case 0x7E:
                case 0x7F:
                    return string.Format("ldrb r{0}, [r{1}, #{2:X}]", opcode & 0x7, (opcode >> 3) & 0x7, ((opcode >> 6) & 0x1F));

                case 0x80:
                case 0x81:
                case 0x82:
                case 0x83:
                case 0x84:
                case 0x85:
                case 0x86:
                case 0x87:
                    return string.Format("strh r{0}, [r{1}, #{2:X}]", opcode & 0x7, (opcode >> 3) & 0x7, ((opcode >> 6) & 0x1F) << 1);

                case 0x88:
                case 0x89:
                case 0x8A:
                case 0x8B:
                case 0x8C:
                case 0x8D:
                case 0x8E:
                case 0x8F:
                    return string.Format("ldrh r{0}, [r{1}, #{2:X}]", opcode & 0x7, (opcode >> 3) & 0x7, ((opcode >> 6) & 0x1F) << 1);

                case 0x90:
                case 0x91:
                case 0x92:
                case 0x93:
                case 0x94:
                case 0x95:
                case 0x96:
                case 0x97:
                    return string.Format("str r{0}, [sp, #{1:X}]", (opcode >> 8) & 0x7, (opcode & 0xFF) << 2);

                case 0x98:
                case 0x99:
                case 0x9A:
                case 0x9B:
                case 0x9C:
                case 0x9D:
                case 0x9E:
                case 0x9F:
                    return string.Format("ldr r{0}, [sp, #{1:X}]", (opcode >> 8) & 0x7, (opcode & 0xFF) << 2);

                case 0xA0:
                case 0xA1:
                case 0xA2:
                case 0xA3:
                case 0xA4:
                case 0xA5:
                case 0xA6:
                case 0xA7:
                    return string.Format("add r{0}, [pc, #{1:X}]", (opcode >> 8) & 0x7, (opcode & 0xFF) << 2);

                case 0xA8:
                case 0xA9:
                case 0xAA:
                case 0xAB:
                case 0xAC:
                case 0xAD:
                case 0xAE:
                case 0xAF:
                    return string.Format("add r{0}, [sp, #{1:X}]", (opcode >> 8) & 0x7, (opcode & 0xFF) << 2);

                case 0xB0:
                    return string.Format("sub sp, sp, #{0:X}", (opcode & 0x7F) << 2);

                case 0xB1:
                case 0xB2:
                case 0xB3:
                case 0xB6:
                case 0xB7:
                case 0xB8:
                case 0xB9:
                case 0xBA:
                case 0xBB:
                case 0xBE:
                case 0xBF:
                    return "Unknown";

                case 0xB4:
                case 0xB5:
                    return string.Format("push {{{0}{1}}}", this.RegListThumb(opcode), (((opcode >> 8) & 1) != 0) ? ",lr" : "");

                case 0xBC:
                case 0xBD:
                    return string.Format("pop {{{0}{1}}}", this.RegListThumb(opcode), (((opcode >> 8) & 1) != 0) ? ",pc" : "");

                case 0xC0:
                case 0xC1:
                case 0xC2:
                case 0xC3:
                case 0xC4:
                case 0xC5:
                case 0xC6:
                case 0xC7:
                    return string.Format("stmia r{0}!, {{{0}}}", (opcode >> 8) & 0x7, this.RegListThumb(opcode));

                case 0xC8:
                case 0xC9:
                case 0xCA:
                case 0xCB:
                case 0xCC:
                case 0xCD:
                case 0xCE:
                case 0xCF:
                    return string.Format("ldmia r{0}!, {{{0}}}", (opcode >> 8) & 0x7, this.RegListThumb(opcode));

                case 0xD0:
                case 0xD1:
                case 0xD2:
                case 0xD3:
                case 0xD4:
                case 0xD5:
                case 0xD6:
                case 0xD7:
                case 0xD8:
                case 0xD9:
                case 0xDA:
                case 0xDB:
                case 0xDC:
                case 0xDD:
                    {
                        uint offset = (uint)(opcode & 0xFF);
                        if ((offset & 0x80) != 0) offset |= 0xFFFFFF00;
                        return string.Format("b{0} #{1:X8}", cc[(opcode >> 8) & 0xF], pc + 4U + (offset << 1));
                    }

                case 0xDE:
                    return "Unknown";

                case 0xDF:
                    return string.Format("swi #{0:X}", opcode & 0xff);

                case 0xE0:
                case 0xE1:
                case 0xE2:
                case 0xE3:
                case 0xE4:
                case 0xE5:
                case 0xE6:
                case 0xE7:
                    {
                        uint offset = (uint)(opcode & 0x7FF);
                        if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;
                        return string.Format("b #{0:X8}", pc + 4U + (offset << 1));
                    }

                case 0xE8:
                case 0xE9:
                case 0xEA:
                case 0xEB:
                case 0xEC:
                case 0xED:
                case 0xEE:
                case 0xEF:
                    return "Unknown (BLX)";

                case 0xF0:
                case 0xF1:
                case 0xF2:
                case 0xF3:
                case 0xF4:
                case 0xF5:
                case 0xF6:
                case 0xF7:
                    {
                        uint offset = (uint)(opcode & 0x7FF);
                        if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;
                        offset = (uint)(offset << 12) | (uint)((memory.ReadU16(pc + 2U) & 0x7FF) << 1);
                        return string.Format("bl #{0:X8}", pc + 4U + offset);
                    }

                case 0xF8:
                case 0xF9:
                case 0xFA:
                case 0xFB:
                case 0xFC:
                case 0xFD:
                case 0xFE:
                case 0xFF:
                    return "bl (cont.)";

                default:
                    return "Unknown opcode";
            }
        }
        #endregion

        #region Arm Opcode formatting
        private string RD(uint op)
        {
            return "r" + ((op >> 12) & 0xF);
        }

        private string RN(uint op)
        {
            return "r" + ((op >> 16) & 0xF);
        }

        private string RM(uint op)
        {
            return "r" + (op & 0xF);
        }

        private string RS(uint op)
        {
            uint m = op & 0x70;
            uint s = (op >> 7) & 31;
            uint r = s >> 1;
            uint rs = op & 0xf;

            switch (m)
            {
                case 0x00:
                    /* LSL (aka ASL) #0 .. 31 */
                    if (s > 0)
                        return string.Format("r{0} lsl #{1}", rs, s);
                    else
                        return string.Format("r{0}", rs);
                case 0x10:
                    /* LSL (aka ASL) R0 .. R15 */
                    return string.Format("r{0} lsl R{1}", rs, r);
                case 0x20:
                    /* LSR #1 .. 32 */
                    if (s == 0) s = 32;
                    return string.Format("r{0} lsr 0x{1:X}", rs, s);
                case 0x30:
                    /* LSR R0 .. R15 */
                    return string.Format("r{0} lsr R{1}", rs, r);
                case 0x40:
                    /* ASR #1 .. 32 */
                    if (s == 0) s = 32;
                    return string.Format("r{0} asr 0x{1:X}", rs, s);
                case 0x50:
                    /* ASR R0 .. R15 */
                    return string.Format("r{0} asr R{1}", rs, r);
                case 0x60:
                    /* ASR #1 .. 32 */
                    if (s == 0)
                        return string.Format("r{0} rrx", rs);
                    else
                        return string.Format("r{0} ror 0x{1:X}", rs, s);
                case 0x70:
                    /* ROR R0 .. R15  */
                    return string.Format("r{0} ror r{1}", rs, r);
            }
            throw new Exception("Unhandled decode state");
        }

        // Second operand is a shifted immediate value
        private string RS_S(uint op)
        {
            uint m = (op >> 5) & 0x3;
            uint s = (op >> 7) & 31;
            uint rs = op & 15;

            switch (m)
            {
                case 0x0:
                    /* LSL (aka ASL) #0 .. 31 */
                    if (s > 0)
                        return string.Format("r{0} lsl #{1:X}", rs, s);
                    else
                        return string.Format("r{0}", rs);
                case 0x1:
                    /* LSR #1 .. 32 */
                    if (s == 0) s = 32;
                    return string.Format("r{0} lsr 0x{1:X}", rs, s);
                case 0x2:
                    /* ASR #1 .. 32 */
                    if (s == 0) s = 32;
                    return string.Format("r{0} asr 0x{1:X}", rs, s);
                case 0x3:
                    /* ROR #1 .. 32 */
                    if (s == 0)
                        return string.Format("r{0} rrx", rs);
                    else
                        return string.Format("r{0} ror 0x{1:X}", rs, s);
            }

            throw new Exception("Unhandled decode state");
        }

        // Immediate (shifted) value
        private string IM(uint op)
        {
            uint val = op & 0xFF;
            int shift = (int)((op >> 8) & 0xF) * 2;
            val = (val >> shift) | (val << (32 - shift));
            return string.Format("0x{0:X}", val);
        }

        private string LIM(uint op)
        {
            uint val = op & 0xfFF;
            return string.Format("0x{0:X}", val);
        }

        // Immediate (halfword transfers) value
        private string H_IM(uint op)
        {
            return string.Format("0x{0:X}", ((op & 0xF00) >> 4) | (op & 0xF));
        }

        private string MUL_RS(uint op)
        {
            return string.Format("r{0}", (op >> 8) & 0xF);
        }

        // Register list
        private string RL(uint op)
        {
            int i, f;
            bool[] set = new bool[17];
            string dst = "";

            set[16] = false;

            for (i = 0; i < 16; i++)
            {
                if ((op & (1U << i)) != 0)
                    set[i] = true;
                else
                    set[i] = false;
            }

            f = 0;
            for (i = 0; i < 16; i++)
            {
                if (set[i]) f++;
            }
            if (f == 0) return "---";

            if (set[1]) f = 0;
            else
            {
                f = -1;
                if (set[0]) dst += "r0";
            }

            bool first = true;
            for (i = 1; i < 15; i++)
            {
                if (f != -1)
                {
                    if (set[i + 1]) continue;
                    if (first == false) dst += ",";
                    if (f == i)
                        dst += "r" + i;
                    else
                        dst += "r" + f + "-" + "r" + i;
                    first = false;
                    f = -1;
                }
                else
                {
                    if (set[i + 1]) f = i + 1;
                    else
                    {
                        f = -1;
                        if (set[i])
                        {
                            if (first == false) dst += ",";
                            dst += "r" + i;
                            first = false;
                        }
                    }
                }
            }

            if (set[15])
            {
                dst += ",pc";
            }

            return dst;
        }

        private string DisassembleArmOpcode(Memory memory, uint pc)
        {
            string[] cc = new string[]
            {
	            "EQ","NE","CS","CC","MI","PL","VS","VC","HI","LS","GE","LT","GT","LE","  ","NV"
            };

            uint opcode = memory.ReadU32Debug(pc);
            bool handled = false;

            string buffer = null;

            // Handle special opcodes
            if ((opcode & 0x80) != 0)
            {
                handled = true;
                switch (opcode & 0xF0)
                {
                    case 0x90:	// Multiply or swap instruction
                        switch ((opcode >> 20) & 0xFF)
                        {
                            case 0x00: buffer = string.Format("MUL{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RN(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x01: buffer = string.Format("MULS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RN(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x02: buffer = string.Format("MLA{0}    {1},{2},{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RM(opcode), MUL_RS(opcode), RD(opcode)); break;
                            case 0x03: buffer = string.Format("MLAS{0}   {1},{2},{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RM(opcode), MUL_RS(opcode), RD(opcode)); break;
                            case 0x08: buffer = string.Format("UMULL{0}  [{1},{2}],{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RD(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x09: buffer = string.Format("UMULLS{0} [{1},{2}],{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RD(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x0A: buffer = string.Format("UMLAL{0}  [{1},{2}],{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RD(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x0B: buffer = string.Format("UMLALS{0} [{1},{2}],{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RD(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x0C: buffer = string.Format("SMULL{0}  [{1},{2}],{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RD(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x0D: buffer = string.Format("SMULLS{0} [{1},{2}],{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RD(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x0E: buffer = string.Format("SMLAL{0}  [{1},{2}],{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RD(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x0F: buffer = string.Format("SMLALS{0} [{1},{2}],{3},{4}", cc[(opcode >> 28) & 15], RN(opcode), RD(opcode), RM(opcode), MUL_RS(opcode)); break;
                            case 0x10: buffer = string.Format("SWP{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RN(opcode), RM(opcode), RD(opcode)); break;
                            case 0x14: buffer = string.Format("SWPB{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RN(opcode), RM(opcode), RD(opcode)); break;
                            default: handled = false; break;
                        }
                        break;

                    case 0xB0:	  // Unsigned halfwords
                        {
                            switch ((opcode >> 20) & 0xFF)
                            {
                                case 0x00: buffer = string.Format("STRH{0}   {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x01: buffer = string.Format("LDRH{0}   {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x02: buffer = string.Format("STRH{0}   {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x03: buffer = string.Format("LDRH{0}   {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x04: buffer = string.Format("STRH{0}   {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x05: buffer = string.Format("LDRH{0}   {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x06: buffer = string.Format("STRH{0}   {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x07: buffer = string.Format("LDRH{0}   {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x08: buffer = string.Format("STRH{0}   {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x09: buffer = string.Format("LDRH{0}   {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0A: buffer = string.Format("STRH{0}   {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0B: buffer = string.Format("LDRH{0}   {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0C: buffer = string.Format("STRH{0}   {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0D: buffer = string.Format("LDRH{0}   {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0E: buffer = string.Format("STRH{0}   {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0F: buffer = string.Format("LDRH{0}   {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x10: buffer = string.Format("STRH{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x11: buffer = string.Format("LDRH{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x12: buffer = string.Format("STRH{0}   {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x13: buffer = string.Format("LDRH{0}   {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x14: buffer = string.Format("STRH{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x15: buffer = string.Format("LDRH{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x16: buffer = string.Format("STRH{0}   {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x17: buffer = string.Format("LDRH{0}   {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x18: buffer = string.Format("STRH{0}   {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x19: buffer = string.Format("LDRH{0}   {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1A: buffer = string.Format("STRH{0}   {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1B: buffer = string.Format("LDRH{0}   {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1C: buffer = string.Format("STRH{0}   {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1D: buffer = string.Format("LDRH{0}   {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1E: buffer = string.Format("STRH{0}   {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1F: buffer = string.Format("LDRH{0}   {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                default: handled = false; break;
                            }
                        }
                        break;

                    case 0xD0:	// Signed byte
                        {
                            switch ((opcode >> 20) & 0xFF)
                            {
                                case 0x00: buffer = string.Format("STRSB{0}  {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x01: buffer = string.Format("LDRSB{0}  {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x02: buffer = string.Format("STRSB{0}  {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x03: buffer = string.Format("LDRSB{0}  {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x04: buffer = string.Format("STRSB{0}  {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x05: buffer = string.Format("LDRSB{0}  {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x06: buffer = string.Format("STRSB{0}  {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x07: buffer = string.Format("LDRSB{0}  {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x08: buffer = string.Format("STRSB{0}  {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x09: buffer = string.Format("LDRSB{0}  {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0A: buffer = string.Format("STRSB{0}  {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0B: buffer = string.Format("LDRSB{0}  {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0C: buffer = string.Format("STRSB{0}  {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0D: buffer = string.Format("LDRSB{0}  {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0E: buffer = string.Format("STRSB{0}  {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0F: buffer = string.Format("LDRSB{0}  {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x10: buffer = string.Format("STRSB{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x11: buffer = string.Format("LDRSB{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x12: buffer = string.Format("STRSB{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x13: buffer = string.Format("LDRSB{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x14: buffer = string.Format("STRSB{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x15: buffer = string.Format("LDRSB{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x16: buffer = string.Format("STRSB{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x17: buffer = string.Format("LDRSB{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x18: buffer = string.Format("STRSB{0}  {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x19: buffer = string.Format("LDRSB{0}  {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1A: buffer = string.Format("STRSB{0}  {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1B: buffer = string.Format("LDRSB{0}  {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1C: buffer = string.Format("STRSB{0}  {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1D: buffer = string.Format("LDRSB{0}  {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1E: buffer = string.Format("STRSB{0}  {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1F: buffer = string.Format("LDRSB{0}  {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                default: handled = false; break;
                            }
                        }
                        break;

                    case 0xF0:	// Signed halfwords
                        {
                            switch ((opcode >> 20) & 0xFF)
                            {
                                case 0x00: buffer = string.Format("STRSH{0}  {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x01: buffer = string.Format("LDRSH{0}  {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x02: buffer = string.Format("STRSH{0}  {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x03: buffer = string.Format("LDRSH{0}  {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x04: buffer = string.Format("STRSH{0}  {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x05: buffer = string.Format("LDRSH{0}  {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x06: buffer = string.Format("STRSH{0}  {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x07: buffer = string.Format("LDRSH{0}  {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x08: buffer = string.Format("STRSH{0}  {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x09: buffer = string.Format("LDRSH{0}  {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0A: buffer = string.Format("STRSH{0}  {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0B: buffer = string.Format("LDRSH{0}  {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x0C: buffer = string.Format("STRSH{0}  {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0D: buffer = string.Format("LDRSH{0}  {1},[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0E: buffer = string.Format("STRSH{0}  {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x0F: buffer = string.Format("LDRSH{0}  {1}!,[{2},+{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x10: buffer = string.Format("STRSH{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x11: buffer = string.Format("LDRSH{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x12: buffer = string.Format("STRSH{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x13: buffer = string.Format("LDRSH{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x14: buffer = string.Format("STRSH{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x15: buffer = string.Format("LDRSH{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x16: buffer = string.Format("STRSH{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x17: buffer = string.Format("LDRSH{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x18: buffer = string.Format("STRSH{0}  {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x19: buffer = string.Format("LDRSH{0}  {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1A: buffer = string.Format("STRSH{0}  {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1B: buffer = string.Format("LDRSH{0}  {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RM(opcode)); break;
                                case 0x1C: buffer = string.Format("STRSH{0}  {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1D: buffer = string.Format("LDRSH{0}  {1},[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1E: buffer = string.Format("STRSH{0}  {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                case 0x1F: buffer = string.Format("LDRSH{0}  {1}!,[{2}],+{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), H_IM(opcode)); break;
                                default: handled = false; break;
                            }
                        }
                        break;

                    default: handled = false; break;
                }
            }

            if (handled == false)
                switch ((opcode >> 20) & 0xFF)
                {
                    case 0x00: buffer = string.Format("AND{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x01: buffer = string.Format("ANDS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x02: buffer = string.Format("EOR{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x03: buffer = string.Format("EORS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x04: buffer = string.Format("SUB{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x05: buffer = string.Format("SUBS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x06: buffer = string.Format("RSB{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x07: buffer = string.Format("RSBS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x08: buffer = string.Format("ADD{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x09: buffer = string.Format("ADDS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x0a: buffer = string.Format("ADC{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x0b: buffer = string.Format("ADCS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x0c: buffer = string.Format("SBC{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x0d: buffer = string.Format("SBCS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x0e: buffer = string.Format("RSC{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x0f: buffer = string.Format("RSCS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;

                    case 0x10: buffer = string.Format("MRS{0}    {1}", cc[(opcode >> 28) & 15], RD(opcode)); break;
                    case 0x11: buffer = string.Format("TSTS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x12:
                        {
                            if ((opcode & 0x10) != 0)
                                buffer = string.Format("BX{0}     {1}", cc[(opcode >> 28) & 15], RM(opcode));
                            else
                                buffer = string.Format("MSR{0}    {1}", cc[(opcode >> 28) & 15], RM(opcode));
                        }
                        break;
                    case 0x13: buffer = string.Format("TEQS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x14: buffer = string.Format("MRSS{0}   {1}", cc[(opcode >> 28) & 15], RD(opcode)); break;
                    case 0x15: buffer = string.Format("CMPS{0}   {1},{2}", cc[(opcode >> 28) & 15], RN(opcode), RS(opcode)); break;
                    case 0x16: buffer = string.Format("MSRS{0}   {1}", cc[(opcode >> 28) & 15], RM(opcode)); break;
                    case 0x17: buffer = string.Format("CMNS{0}   {1},{2}", cc[(opcode >> 28) & 15], RN(opcode), RS(opcode)); break;
                    case 0x18: buffer = string.Format("ORR{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x19: buffer = string.Format("ORRS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x1a: buffer = string.Format("MOV{0}    {1},{2}", cc[(opcode >> 28) & 15], RD(opcode), RS(opcode)); break;
                    case 0x1b: buffer = string.Format("MOVS{0}   {1},{2}", cc[(opcode >> 28) & 15], RD(opcode), RS(opcode)); break;
                    case 0x1c: buffer = string.Format("BIC{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x1d: buffer = string.Format("BICS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS(opcode)); break;
                    case 0x1e: buffer = string.Format("MVN{0}    {1},{2}", cc[(opcode >> 28) & 15], RD(opcode), RS(opcode)); break;
                    case 0x1f: buffer = string.Format("MVNS{0}   {1},{2}", cc[(opcode >> 28) & 15], RD(opcode), RS(opcode)); break;

                    case 0x20: buffer = string.Format("AND{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x21: buffer = string.Format("ANDS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x22: buffer = string.Format("EOR{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x23: buffer = string.Format("EORS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x24: buffer = string.Format("SUB{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x25: buffer = string.Format("SUBS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x26: buffer = string.Format("RSB{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x27: buffer = string.Format("RSBS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x28: buffer = string.Format("ADD{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x29: buffer = string.Format("ADDS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x2a: buffer = string.Format("ADC{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x2b: buffer = string.Format("ADCS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x2c: buffer = string.Format("SBC{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x2d: buffer = string.Format("SBCS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x2e: buffer = string.Format("RSC{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x2f: buffer = string.Format("RSCS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;

                    case 0x30: buffer = string.Format("TST{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x31: buffer = string.Format("TSTS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x32: buffer = string.Format("MSRB{0}   {1}", cc[(opcode >> 28) & 15], IM(opcode)); break;
                    case 0x33: buffer = string.Format("TEQS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x34: buffer = string.Format("CMP{0}    {1},{2}", cc[(opcode >> 28) & 15], RN(opcode), IM(opcode)); break;
                    case 0x35: buffer = string.Format("CMPS{0}   {1},{2}", cc[(opcode >> 28) & 15], RN(opcode), IM(opcode)); break;
                    case 0x36: buffer = string.Format("MSRBS{0}  {1}", cc[(opcode >> 28) & 15], IM(opcode)); break;
                    case 0x37: buffer = string.Format("CMNS{0}   {1},{2}", cc[(opcode >> 28) & 15], RN(opcode), IM(opcode)); break;
                    case 0x38: buffer = string.Format("ORR{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x39: buffer = string.Format("ORRS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x3a: buffer = string.Format("MOV{0}    {1},{2}", cc[(opcode >> 28) & 15], RD(opcode), IM(opcode)); break;
                    case 0x3b: buffer = string.Format("MOVS{0}   {1},{2}", cc[(opcode >> 28) & 15], RD(opcode), IM(opcode)); break;
                    case 0x3c: buffer = string.Format("BIC{0}    {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x3d: buffer = string.Format("BICS{0}   {1},{2},{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), IM(opcode)); break;
                    case 0x3e: buffer = string.Format("MVN{0}    {1},{2}", cc[(opcode >> 28) & 15], RD(opcode), IM(opcode)); break;
                    case 0x3f: buffer = string.Format("MVNS{0}   {1},{2}", cc[(opcode >> 28) & 15], RD(opcode), IM(opcode)); break;

                    case 0x40: buffer = string.Format("STR{0}    {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x41: buffer = string.Format("LDR{0}    {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x42: buffer = string.Format("STRT{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x43: buffer = string.Format("LDRT{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x44: buffer = string.Format("STRB{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x45: buffer = string.Format("LDRB{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x46: buffer = string.Format("STRBT{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x47: buffer = string.Format("LDRBT{0}  {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x48: buffer = string.Format("STR{0}    {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x49: buffer = string.Format("LDR{0}    {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x4a: buffer = string.Format("STR{0}    {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x4b: buffer = string.Format("LDR{0}    {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x4c: buffer = string.Format("STRB{0}   {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x4d: buffer = string.Format("LDRB{0}   {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x4e: buffer = string.Format("STRB{0}   {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x4f: buffer = string.Format("LDRB{0}   {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;

                    case 0x50: buffer = string.Format("STR{0}    {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x51: buffer = string.Format("LDR{0}    {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x52: buffer = string.Format("STR{0}    {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x53: buffer = string.Format("LDR{0}    {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x54: buffer = string.Format("STRB{0}   {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x55: buffer = string.Format("LDRB{0}   {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x56: buffer = string.Format("STRB{0}   {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x57: buffer = string.Format("LDRB{0}   {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x58: buffer = string.Format("STR{0}    {1},[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x59: buffer = string.Format("LDR{0}    {1},[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x5a: buffer = string.Format("STR{0}    {1}!,[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x5b: buffer = string.Format("LDR{0}    {1}!,[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x5c: buffer = string.Format("STRB{0}   {1},[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x5d: buffer = string.Format("LDRB{0}   {1},[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x5e: buffer = string.Format("STRB{0}   {1}!,[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;
                    case 0x5f: buffer = string.Format("LDRB{0}   {1}!,[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), LIM(opcode)); break;

                    case 0x60: buffer = string.Format("STR{0}    {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x61: buffer = string.Format("LDR{0}    {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x62: buffer = string.Format("STRT{0}   {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x63: buffer = string.Format("LDRT{0}   {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x64: buffer = string.Format("STRB{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x65: buffer = string.Format("LDRB{0}   {1},[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x66: buffer = string.Format("STRBT{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x67: buffer = string.Format("LDRBT{0}  {1}!,[{2}],-{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x68: buffer = string.Format("STR{0}    {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x69: buffer = string.Format("LDR{0}    {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x6a: buffer = string.Format("STRT{0}   {1}!,[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x6b: buffer = string.Format("LDRT{0}   {1}!,[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x6c: buffer = string.Format("STRB{0}   {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x6d: buffer = string.Format("LDRB{0}   {1},[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x6e: buffer = string.Format("STRBT{0}  {1}!,[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x6f: buffer = string.Format("LDRBT{0}  {1}!,[{2}],{3}", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;

                    case 0x70: buffer = string.Format("STR{0}    {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x71: buffer = string.Format("LDR{0}    {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x72: buffer = string.Format("STR{0}    {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x73: buffer = string.Format("LDR{0}    {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x74: buffer = string.Format("STRB{0}   {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x75: buffer = string.Format("LDRB{0}   {1},[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x76: buffer = string.Format("STRB{0}   {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x77: buffer = string.Format("LDRB{0}   {1}!,[{2},-{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x78: buffer = string.Format("STR{0}    {1},[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x79: buffer = string.Format("LDR{0}    {1},[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x7a: buffer = string.Format("STR{0}    {1}!,[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x7b: buffer = string.Format("LDR{0}    {1}!,[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x7c: buffer = string.Format("STRB{0}   {1},[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x7d: buffer = string.Format("LDRB{0}   {1},[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x7e: buffer = string.Format("STRB{0}   {1}!,[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;
                    case 0x7f: buffer = string.Format("LDRB{0}   {1}!,[{2},{3}]", cc[(opcode >> 28) & 15], RD(opcode), RN(opcode), RS_S(opcode)); break;

                    case 0x80: buffer = string.Format("STMDA{0}  {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x81: buffer = string.Format("LDMDA{0}  {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x82: buffer = string.Format("STMDA{0}  {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x83: buffer = string.Format("LDMDA{0}  {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x84: buffer = string.Format("STMDAB{0} {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x85: buffer = string.Format("LDMDAB{0} {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x86: buffer = string.Format("STMDAB{0} {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x87: buffer = string.Format("LDMDAB{0} {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x88: buffer = string.Format("STMIA{0}  {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x89: buffer = string.Format("LDMIA{0}  {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x8a: buffer = string.Format("STMIA{0}  {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x8b: buffer = string.Format("LDMIA{0}  {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x8c: buffer = string.Format("STMIAB{0} {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x8d: buffer = string.Format("LDMIAB{0} {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x8e: buffer = string.Format("STMIAB{0} {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x8f: buffer = string.Format("LDMIAB{0} {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;

                    case 0x90: buffer = string.Format("STMDB{0}  {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x91: buffer = string.Format("LDMDB{0}  {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x92: buffer = string.Format("STMDB{0}  {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x93: buffer = string.Format("LDMDB{0}  {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x94: buffer = string.Format("STMDBB{0} {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x95: buffer = string.Format("LDMDBB{0} {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x96: buffer = string.Format("STMDBB{0} {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x97: buffer = string.Format("LDMDBB{0} {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x98: buffer = string.Format("STMIB{0}  {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x99: buffer = string.Format("LDMIB{0}  {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x9a: buffer = string.Format("STMIB{0}  {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x9b: buffer = string.Format("LDMIB{0}  {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x9c: buffer = string.Format("STMIBB{0} {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x9d: buffer = string.Format("LDMIBB{0} {1},[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x9e: buffer = string.Format("STMIBB{0} {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;
                    case 0x9f: buffer = string.Format("LDMIBB{0} {1}!,[{2}]", cc[(opcode >> 28) & 15], RN(opcode), RL(opcode)); break;

                    case 0xa0:
                    case 0xa1:
                    case 0xa2:
                    case 0xa3:
                    case 0xa4:
                    case 0xa5:
                    case 0xa6:
                    case 0xa7:
                    case 0xa8:
                    case 0xa9:
                    case 0xaa:
                    case 0xaf:
                        {
                            uint offset = opcode & 0x00FFFFFF;

                            if ((offset & 0x800000) != 0)
                            {
                                offset = (offset | 0xFF000000) << 2;
                            }
                            else
                            {
                                offset <<= 2;
                            }

                            buffer = string.Format("B{0}      0x{1:X}", cc[(opcode >> 28) & 15], 8 + pc + offset);
                        }
                        break;

                    case 0xb0:
                    case 0xb1:
                    case 0xb2:
                    case 0xb3:
                    case 0xb4:
                    case 0xb5:
                    case 0xb6:
                    case 0xb7:
                    case 0xb8:
                    case 0xb9:
                    case 0xba:
                    case 0xbf:
                        {
                            uint offset = opcode & 0x00FFFFFF;

                            if ((offset & 0x800000) != 0)
                            {
                                offset = (offset | 0xFF000000) << 2;
                            }
                            else
                            {
                                offset <<= 2;
                            }
                            buffer = string.Format("BL{0}     0x{1:X}", cc[(opcode >> 28) & 15], 8 + pc + offset);
                        }
                        break;

                    case 0xc0:
                    case 0xc1:
                    case 0xc2:
                    case 0xc3:
                    case 0xc4:
                    case 0xc5:
                    case 0xc6:
                    case 0xc7:
                    case 0xc8:
                    case 0xc9:
                    case 0xca:
                    case 0xcf:
                    case 0xd0:
                    case 0xd1:
                    case 0xd2:
                    case 0xd3:
                    case 0xd4:
                    case 0xd5:
                    case 0xd6:
                    case 0xd7:
                    case 0xd8:
                    case 0xd9:
                    case 0xda:
                    case 0xdf:
                    case 0xe0:
                    case 0xe1:
                    case 0xe2:
                    case 0xe3:
                    case 0xe4:
                    case 0xe5:
                    case 0xe6:
                    case 0xe7:
                    case 0xe8:
                    case 0xe9:
                    case 0xea:
                    case 0xef:
                        buffer = string.Format("ILL{0}    0x{1:X8}", cc[(opcode >> 28) & 15], opcode);
                        break;
                    case 0xf0:
                    case 0xf1:
                    case 0xf2:
                    case 0xf3:
                    case 0xf4:
                    case 0xf5:
                    case 0xf6:
                    case 0xf7:
                    case 0xf8:
                    case 0xf9:
                    case 0xfa:
                    case 0xff:
                        buffer = string.Format("SWI{0}    0x{1:X8}", cc[(opcode >> 28) & 15], opcode & 0xFFFFFF);
                        break;
                    default:
                        buffer = string.Format("???{0}    0x{1:X8}", cc[(opcode >> 28) & 15], opcode);
                        break;
                }

            if (buffer == null) throw new Exception("Unhandled instruction decode");

            return buffer;
        }
        #endregion
    }
}