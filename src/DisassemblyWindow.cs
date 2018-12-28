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
    partial class DisassemblyWindow : Form
    {
        private uint _curPc;
        private bool _inArm = true;
        private bool _prevInArm = true;
        private readonly GbaManager.GbaManager _gbaManager;

        public DisassemblyWindow(GbaManager.GbaManager gbaManager)
        {
            InitializeComponent();

            DisAsmScrollBar.Minimum = 0;
            DisAsmScrollBar.Maximum = 0x8ffffff;

            _gbaManager = gbaManager;

            _gbaManager.OnCpuUpdate += Update;

            var setBreakpoint = new MenuItem("Set/Remove Breakpoint", OnSetBreakpoint);
            MenuItem[] menuItems = 
            {
                setBreakpoint
            };

            disassembly.ContextMenu = new ContextMenu(menuItems);
            disassembly.DrawItem += OnDrawItem;
        }

        private Arm7Processor _processor;
        private Memory _memory;

        public void Update(Arm7Processor processor, Memory memory)
        {
            _processor = processor;
            _memory = memory;

            if (processor != null)
            {
                _inArm = (processor.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK;
            }

            UpdateRegisters(processor);

            UpdateDisassembly(processor, memory);

            _prevInArm = _inArm;
        }

        private void UpdateRegisters(Arm7Processor processor)
        {
            registerNames.BeginUpdate();
            registerValues.BeginUpdate();

            registerNames.Items.Clear();
            registerValues.Items.Clear();

            if (processor != null)
            {
                for (var i = 0; i <= 14; i++)
                {
                    registerNames.Items.Add("R" + i);
                    registerValues.Items.Add($"0x{processor.Registers[i]:X8}");
                }

                registerNames.Items.Add("");
                registerValues.Items.Add("");
                registerNames.Items.Add("R15 (PC)");
                registerValues.Items.Add($"0x{processor.Registers[15] + 4U:X8}");
                registerNames.Items.Add("Flags");
                registerValues.Items.Add(
                    $"{((processor.CPSR & Arm7Processor.N_MASK) == Arm7Processor.N_MASK ? "N" : "n")}{((processor.CPSR & Arm7Processor.Z_MASK) == Arm7Processor.Z_MASK ? "Z" : "z")}{((processor.CPSR & Arm7Processor.C_MASK) == Arm7Processor.C_MASK ? "C" : "c")}{((processor.CPSR & Arm7Processor.V_MASK) == Arm7Processor.V_MASK ? "V" : "v")}{((processor.CPSR & Arm7Processor.T_MASK) == Arm7Processor.T_MASK ? "T" : "t")}");
                registerNames.Items.Add("CPSR");
                registerValues.Items.Add($"0x{processor.CPSR:X8}");
                if (processor.SPSRExists)
                {
                    registerNames.Items.Add("SPSR");
                    registerValues.Items.Add($"0x{processor.SPSR:X8}");
                }
            }

            registerValues.EndUpdate();
            registerNames.EndUpdate();
        }

        private void UpdateDisassembly(Arm7Processor processor, Memory memory)
        {
            if (processor != null && memory != null)
            {
                if (!_inArm)
                {
                    var pc = processor.Registers[15] - 0x2U;
                    var numItems = disassembly.Height / disassembly.ItemHeight;

                    if (pc > _curPc && pc < _curPc + numItems * 2 - 2 && !_prevInArm && disassembly.Items.Count > 0)
                    {
                        disassembly.SelectedIndex = (int)(pc - _curPc) / 2;
                        return;
                    }

                    _curPc = pc - 0x2U;

                    RefreshDisassembly(processor, memory);

                    disassembly.SelectedIndex = (int)(pc - _curPc) / 2;
                }
                else
                {
                    var pc = processor.Registers[15] - 0x4U;
                    var numItems = disassembly.Height / disassembly.ItemHeight;

                    if (pc > _curPc && pc < _curPc + numItems * 4 - 4 && _prevInArm && disassembly.Items.Count > 0)
                    {
                        disassembly.SelectedIndex = (int)(pc - _curPc) / 4;
                        return;
                    }

                    _curPc = pc - 0x4U;

                    RefreshDisassembly(processor, memory);

                    disassembly.SelectedIndex = (int)(pc - _curPc) / 4;
                }

                DisAsmScrollBar.Value = ((int)_curPc) < 0 ? 0 : (int)_curPc;
            }
        }

        private void RefreshDisassembly(Arm7Processor processor, Memory memory)
        {
            curLocation.Text = $"{_curPc:X8}";

            var numItems = disassembly.Height / disassembly.ItemHeight;
            disassembly.BeginUpdate();

            disassembly.Items.Clear();

            for (var i = 0; i < numItems; i++)
            {
                var sb = new StringBuilder();

                uint memAddress;
                if (_inArm)
                {
                    memAddress = (uint)(_curPc + (i * 4));
                }
                else
                {
                    memAddress = (uint)(_curPc + (i * 2));
                }

                sb.Append($"{memAddress:X8}");
                sb.Append("  ");
                if (_inArm)
                {
                    sb.Append(DisassembleArmOpcode(memory, memAddress));
                }
                else
                {
                    sb.Append(DisassembleThumbOpcode(memory, memAddress));
                }
                disassembly.Items.Add(sb.ToString());
            }

            if (_inArm)
            {
                var pc = processor.Registers[15] - 0x4U;
                if (pc >= _curPc && pc < _curPc + numItems * 4)
                {
                    disassembly.SelectedIndex = (int)(pc - _curPc) / 4;
                }
            }
            else
            {
                var pc = processor.Registers[15] - 0x2U;
                if (pc >= _curPc && pc < _curPc + numItems * 2)
                {
                    disassembly.SelectedIndex = (int)(pc - _curPc) / 2;
                }
            }

            disassembly.EndUpdate();
        }

        private void step_Click(object sender, EventArgs e)
        {
            _gbaManager.Step();
        }

        private void go_Click(object sender, EventArgs e)
        {
            _gbaManager.Resume();
        }

        private void ScanlineStep_Click(object sender, EventArgs e)
        {
            _gbaManager.StepScanline();
        }

        private void gotoLocation_Click(object sender, EventArgs e)
        {
            _curPc = UInt32.Parse(curLocation.Text, NumberStyles.HexNumber);
            RefreshDisassembly(_processor, _memory);
        }

        private void gotoPc_Click(object sender, EventArgs e)
        {
            if (!_inArm)
            {
                _curPc = _processor.Registers[15] - 0x4U;
            }
            else
            {
                _curPc = _processor.Registers[15] - 0x8U;
            }

            RefreshDisassembly(_processor, _memory);
        }

        private void displayArmRadio_CheckedChanged(object sender, EventArgs e)
        {
            _inArm = true;

            RefreshDisassembly(_processor, _memory);

            _prevInArm = _inArm;
        }

        private void displayThumbRadio_CheckedChanged(object sender, EventArgs e)
        {
            _inArm = false;

            RefreshDisassembly(_processor, _memory);

            _prevInArm = _inArm;
        }

        private void displayAutoRadio_CheckedChanged(object sender, EventArgs e)
        {
            if (_processor != null)
            {
                _inArm = (_processor.CPSR & Arm7Processor.T_MASK) != Arm7Processor.T_MASK;
            }

            RefreshDisassembly(_processor, _memory);

            _prevInArm = _inArm;
        }

        private void OnSetBreakpoint(object sender, EventArgs eventArgs)
        {
            var point = Cursor.Position;
            point.Y -= 6;
            var selected = disassembly.IndexFromPoint(disassembly.PointToClient(point));
            uint address;
            if (_inArm)
            {
                address = (uint)(_curPc + selected * 4);
            }
            else
            {
                address = (uint)(_curPc + selected * 2);
            }
            if (_gbaManager.Breakpoints.ContainsKey(address))
            {
                _gbaManager.Breakpoints.Remove(address);
            }
            else
            {
                _gbaManager.Breakpoints[address] = true;
            }


            disassembly.Invalidate();
        }

        private void OnDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            e.DrawBackground();

            var topLeft = new PointF(e.Bounds.Left, e.Bounds.Top);
            float bpWidth = disassembly.ItemHeight - 4;

            if (_gbaManager.Breakpoints != null)
            {
                uint scale = 4;
                if (!_inArm) scale = 2;
                if (_gbaManager.Breakpoints.ContainsKey((uint)(_curPc + e.Index * scale)))
                {
                    e.Graphics.FillEllipse(Brushes.Red, topLeft.X + 2, topLeft.Y + 2, bpWidth, bpWidth);
                }
            }

            topLeft.X += bpWidth;

            var textBrush = Brushes.Black;
            var opBrush = Brushes.Blue;
            if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
            {
                opBrush = Brushes.Red;
                textBrush = Brushes.White;
            }

            var textChunks = disassembly.Items[e.Index].ToString().Split(' ');
            var tmpChunk = new List<string>();
            var chunkCount = 0;
            for (var i = textChunks.Length - 1; i >= 0; i--)
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
            for (var i = 2; i < textChunks.Length; i++)
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
                    _curPc += 4;
                    break;
                case ScrollEventType.SmallDecrement:
                    _curPc -= 4;
                    break;
                case ScrollEventType.LargeIncrement:
                    _curPc += 0x100;
                    break;
                case ScrollEventType.LargeDecrement:
                    _curPc -= 0x100;
                    break;
                case ScrollEventType.ThumbTrack:
                    _curPc = (uint)e.NewValue;
                    break;
            }

            RefreshDisassembly(_processor, _memory);
        }

        #region Thumb Opcode Formatting
        private string RegListThumb(ushort opcode)
        {
            var start = -1;
            var res = "";
            for (var i = 0; i < 9; i++)
            {
                if (((opcode >> i) & 1) != 0 && i != 8)
                {
                    if (start == -1) start = i;
                } else
                {
                    if (start != -1)
                    {
                        var end = i - 1;
                        if (start == end) res += "r" + start + ",";
                        else res += $"r{start}-r{end},";
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
            var aluOps = new string[16]
                {
                    "and","eor","lsl","lsr","asr","adc","sbc","ror","tst","neg","cmp","cmn","orr","mul","bic","mvn"
                };

            var cc = new string[]
            {
	            "eq","ne","cs","cc","mi","pl","vs","vc","hi","ls","ge","lt","gt","le","","nv"
            };

            var opcode = memory.ReadU16Debug(pc);

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
                    return $"lsl r{opcode & 0x7}, r{(opcode >> 3) & 0x7}, #{(opcode >> 6) & 0x1F:X}";

                case 0x08:
                case 0x09:
                case 0x0A:
                case 0x0B:
                case 0x0C:
                case 0x0D:
                case 0x0E:
                case 0x0F:
                    return $"lsr r{opcode & 0x7}, r{(opcode >> 3) & 0x7}, #{(opcode >> 6) & 0x1F:X}";

                case 0x10:
                case 0x11:
                case 0x12:
                case 0x13:
                case 0x14:
                case 0x15:
                case 0x16:
                case 0x17:
                    return $"asr r{opcode & 0x7}, r{(opcode >> 3) & 0x7}, #{(opcode >> 6) & 0x1F:X}";

                case 0x18:
                case 0x19:
                    return $"add r{opcode & 0x7}, r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}";

                case 0x1A:
                case 0x1B:
                    return $"sub r{opcode & 0x7}, r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}";

                case 0x1C:
                case 0x1D:
                    return $"add r{opcode & 0x7}, r{(opcode >> 3) & 0x7}, #{(opcode >> 6) & 0x7:X}";

                case 0x1E:
                case 0x1F:
                    return $"sub r{opcode & 0x7}, r{(opcode >> 3) & 0x7}, #{(opcode >> 6) & 0x7:X}";

                case 0x20:
                case 0x21:
                case 0x22:
                case 0x23:
                case 0x24:
                case 0x25:
                case 0x26:
                case 0x27:
                    return $"mov r{(opcode >> 8) & 0x7}, #{opcode & 0xFF:X}";

                case 0x28:
                case 0x29:
                case 0x2A:
                case 0x2B:
                case 0x2C:
                case 0x2D:
                case 0x2E:
                case 0x2F:
                    return $"cmp r{(opcode >> 8) & 0x7}, #{opcode & 0xFF:X}";

                case 0x30:
                case 0x31:
                case 0x32:
                case 0x33:
                case 0x34:
                case 0x35:
                case 0x36:
                case 0x37:
                    return $"add r{(opcode >> 8) & 0x7}, #{opcode & 0xFF:X}";

                case 0x38:
                case 0x39:
                case 0x3A:
                case 0x3B:
                case 0x3C:
                case 0x3D:
                case 0x3E:
                case 0x3F:
                    return $"sub r{(opcode >> 8) & 0x7}, #{opcode & 0xFF:X}";

                case 0x40:
                case 0x41:
                case 0x42:
                case 0x43:
                    return $"{aluOps[(opcode >> 6) & 0xf]} r{opcode & 0x7}, r{(opcode >> 3) & 0x7}";

                case 0x44:
                    return $"add r{(((opcode >> 7) & 1) << 3) | (opcode & 0x7)}, r{(opcode >> 3) & 0xF}";

                case 0x45:
                    return $"cmp r{(((opcode >> 7) & 1) << 3) | (opcode & 0x7)}, r{(opcode >> 3) & 0xF}";
                
                case 0x46:
                    return $"mov r{(((opcode >> 7) & 1) << 3) | (opcode & 0x7)}, r{(opcode >> 3) & 0xF}";
                
                case 0x47:
                    return $"bx r{(opcode >> 3) & 0xF}";

                case 0x48:
                case 0x49:
                case 0x4A:
                case 0x4B:
                case 0x4C:
                case 0x4D:
                case 0x4E:
                case 0x4F:
                    return $"ldr r{(opcode >> 8) & 0x7}, [pc, #{(opcode & 0xFF) * 4:X}]";

                case 0x50:
                case 0x51:
                    return $"str r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}]";

                case 0x52:
                case 0x53:
                    return $"strh r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}]";

                case 0x54:
                case 0x55:
                    return $"strb r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}]";

                case 0x56:
                case 0x57:
                    return $"ldrsb r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}]";

                case 0x58:
                case 0x59:
                    return $"ldr r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}]";

                case 0x5A:
                case 0x5B:
                    return $"ldrh r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}]";

                case 0x5C:
                case 0x5D:
                    return $"ldrb r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}]";

                case 0x5E:
                case 0x5F:
                    return $"ldrsh r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, r{(opcode >> 6) & 0x7}]";

                case 0x60:
                case 0x61:
                case 0x62:
                case 0x63:
                case 0x64:
                case 0x65:
                case 0x66:
                case 0x67:
                    return $"str r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, #{((opcode >> 6) & 0x1F) << 2:X}]";

                case 0x68:
                case 0x69:
                case 0x6A:
                case 0x6B:
                case 0x6C:
                case 0x6D:
                case 0x6E:
                case 0x6F:
                    return $"ldr r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, #{((opcode >> 6) & 0x1F) << 2:X}]";

                case 0x70:
                case 0x71:
                case 0x72:
                case 0x73:
                case 0x74:
                case 0x75:
                case 0x76:
                case 0x77:
                    return $"strb r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, #{((opcode >> 6) & 0x1F):X}]";

                case 0x78:
                case 0x79:
                case 0x7A:
                case 0x7B:
                case 0x7C:
                case 0x7D:
                case 0x7E:
                case 0x7F:
                    return $"ldrb r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, #{((opcode >> 6) & 0x1F):X}]";

                case 0x80:
                case 0x81:
                case 0x82:
                case 0x83:
                case 0x84:
                case 0x85:
                case 0x86:
                case 0x87:
                    return $"strh r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, #{((opcode >> 6) & 0x1F) << 1:X}]";

                case 0x88:
                case 0x89:
                case 0x8A:
                case 0x8B:
                case 0x8C:
                case 0x8D:
                case 0x8E:
                case 0x8F:
                    return $"ldrh r{opcode & 0x7}, [r{(opcode >> 3) & 0x7}, #{((opcode >> 6) & 0x1F) << 1:X}]";

                case 0x90:
                case 0x91:
                case 0x92:
                case 0x93:
                case 0x94:
                case 0x95:
                case 0x96:
                case 0x97:
                    return $"str r{(opcode >> 8) & 0x7}, [sp, #{(opcode & 0xFF) << 2:X}]";

                case 0x98:
                case 0x99:
                case 0x9A:
                case 0x9B:
                case 0x9C:
                case 0x9D:
                case 0x9E:
                case 0x9F:
                    return $"ldr r{(opcode >> 8) & 0x7}, [sp, #{(opcode & 0xFF) << 2:X}]";

                case 0xA0:
                case 0xA1:
                case 0xA2:
                case 0xA3:
                case 0xA4:
                case 0xA5:
                case 0xA6:
                case 0xA7:
                    return $"add r{(opcode >> 8) & 0x7}, [pc, #{(opcode & 0xFF) << 2:X}]";

                case 0xA8:
                case 0xA9:
                case 0xAA:
                case 0xAB:
                case 0xAC:
                case 0xAD:
                case 0xAE:
                case 0xAF:
                    return $"add r{(opcode >> 8) & 0x7}, [sp, #{(opcode & 0xFF) << 2:X}]";

                case 0xB0:
                    return $"sub sp, sp, #{(opcode & 0x7F) << 2:X}";

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
                    return $"push {{{RegListThumb(opcode)}{((((opcode >> 8) & 1) != 0) ? ",lr" : "")}}}";

                case 0xBC:
                case 0xBD:
                    return $"pop {{{RegListThumb(opcode)}{((((opcode >> 8) & 1) != 0) ? ",pc" : "")}}}";

                case 0xC0:
                case 0xC1:
                case 0xC2:
                case 0xC3:
                case 0xC4:
                case 0xC5:
                case 0xC6:
                case 0xC7:
                    return string.Format("stmia r{0}!, {{{0}}}", (opcode >> 8) & 0x7, RegListThumb(opcode));

                case 0xC8:
                case 0xC9:
                case 0xCA:
                case 0xCB:
                case 0xCC:
                case 0xCD:
                case 0xCE:
                case 0xCF:
                    return string.Format("ldmia r{0}!, {{{0}}}", (opcode >> 8) & 0x7, RegListThumb(opcode));

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
                        var offset = (uint)(opcode & 0xFF);
                        if ((offset & 0x80) != 0) offset |= 0xFFFFFF00;
                        return $"b{cc[(opcode >> 8) & 0xF]} #{pc + 4U + (offset << 1):X8}";
                    }

                case 0xDE:
                    return "Unknown";

                case 0xDF:
                    return $"swi #{opcode & 0xff:X}";

                case 0xE0:
                case 0xE1:
                case 0xE2:
                case 0xE3:
                case 0xE4:
                case 0xE5:
                case 0xE6:
                case 0xE7:
                    {
                        var offset = (uint)(opcode & 0x7FF);
                        if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;
                        return $"b #{pc + 4U + (offset << 1):X8}";
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
                        var offset = (uint)(opcode & 0x7FF);
                        if ((offset & (1 << 10)) != 0) offset |= 0xFFFFF800;
                        offset = offset << 12 | (uint)((memory.ReadU16(pc + 2U) & 0x7FF) << 1);
                        return $"bl #{pc + 4U + offset:X8}";
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
            var m = op & 0x70;
            var s = (op >> 7) & 31;
            var r = s >> 1;
            var rs = op & 0xf;

            switch (m)
            {
                case 0x00:
                    /* LSL (aka ASL) #0 .. 31 */
                    if (s > 0)
                        return $"r{rs} lsl #{s}";
                    else
                        return $"r{rs}";
                case 0x10:
                    /* LSL (aka ASL) R0 .. R15 */
                    return $"r{rs} lsl R{r}";
                case 0x20:
                    /* LSR #1 .. 32 */
                    if (s == 0) s = 32;
                    return $"r{rs} lsr 0x{s:X}";
                case 0x30:
                    /* LSR R0 .. R15 */
                    return $"r{rs} lsr R{r}";
                case 0x40:
                    /* ASR #1 .. 32 */
                    if (s == 0) s = 32;
                    return $"r{rs} asr 0x{s:X}";
                case 0x50:
                    /* ASR R0 .. R15 */
                    return $"r{rs} asr R{r}";
                case 0x60:
                    /* ASR #1 .. 32 */
                    if (s == 0)
                        return $"r{rs} rrx";
                    else
                        return $"r{rs} ror 0x{s:X}";
                case 0x70:
                    /* ROR R0 .. R15  */
                    return $"r{rs} ror r{r}";
            }
            throw new Exception("Unhandled decode state");
        }

        // Second operand is a shifted immediate value
        private string RS_S(uint op)
        {
            var m = (op >> 5) & 0x3;
            var s = (op >> 7) & 31;
            var rs = op & 15;

            switch (m)
            {
                case 0x0:
                    /* LSL (aka ASL) #0 .. 31 */
                    if (s > 0)
                        return $"r{rs} lsl #{s:X}";
                    else
                        return $"r{rs}";
                case 0x1:
                    /* LSR #1 .. 32 */
                    if (s == 0) s = 32;
                    return $"r{rs} lsr 0x{s:X}";
                case 0x2:
                    /* ASR #1 .. 32 */
                    if (s == 0) s = 32;
                    return $"r{rs} asr 0x{s:X}";
                case 0x3:
                    /* ROR #1 .. 32 */
                    if (s == 0)
                        return $"r{rs} rrx";
                    else
                        return $"r{rs} ror 0x{s:X}";
            }

            throw new Exception("Unhandled decode state");
        }

        // Immediate (shifted) value
        private string IM(uint op)
        {
            var val = op & 0xFF;
            var shift = (int)((op >> 8) & 0xF) * 2;
            val = (val >> shift) | (val << (32 - shift));
            return $"0x{val:X}";
        }

        private string LIM(uint op)
        {
            var val = op & 0xfFF;
            return $"0x{val:X}";
        }

        // Immediate (halfword transfers) value
        private string H_IM(uint op)
        {
            return $"0x{((op & 0xF00) >> 4) | (op & 0xF):X}";
        }

        private string MUL_RS(uint op)
        {
            return $"r{(op >> 8) & 0xF}";
        }

        // Register list
        private string RL(uint op)
        {
            int i, f;
            var set = new bool[17];
            var dst = "";

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

            var first = true;
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
            var cc = new string[]
            {
	            "EQ","NE","CS","CC","MI","PL","VS","VC","HI","LS","GE","LT","GT","LE","  ","NV"
            };

            var opcode = memory.ReadU32Debug(pc);
            var handled = false;

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
                            case 0x00: buffer =
                                $"MUL{cc[(opcode >> 28) & 15]}    {RN(opcode)},{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x01: buffer =
                                $"MULS{cc[(opcode >> 28) & 15]}   {RN(opcode)},{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x02: buffer =
                                $"MLA{cc[(opcode >> 28) & 15]}    {RN(opcode)},{RM(opcode)},{MUL_RS(opcode)},{RD(opcode)}"; break;
                            case 0x03: buffer =
                                $"MLAS{cc[(opcode >> 28) & 15]}   {RN(opcode)},{RM(opcode)},{MUL_RS(opcode)},{RD(opcode)}"; break;
                            case 0x08: buffer =
                                $"UMULL{cc[(opcode >> 28) & 15]}  [{RN(opcode)},{RD(opcode)}],{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x09: buffer =
                                $"UMULLS{cc[(opcode >> 28) & 15]} [{RN(opcode)},{RD(opcode)}],{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x0A: buffer =
                                $"UMLAL{cc[(opcode >> 28) & 15]}  [{RN(opcode)},{RD(opcode)}],{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x0B: buffer =
                                $"UMLALS{cc[(opcode >> 28) & 15]} [{RN(opcode)},{RD(opcode)}],{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x0C: buffer =
                                $"SMULL{cc[(opcode >> 28) & 15]}  [{RN(opcode)},{RD(opcode)}],{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x0D: buffer =
                                $"SMULLS{cc[(opcode >> 28) & 15]} [{RN(opcode)},{RD(opcode)}],{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x0E: buffer =
                                $"SMLAL{cc[(opcode >> 28) & 15]}  [{RN(opcode)},{RD(opcode)}],{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x0F: buffer =
                                $"SMLALS{cc[(opcode >> 28) & 15]} [{RN(opcode)},{RD(opcode)}],{RM(opcode)},{MUL_RS(opcode)}"; break;
                            case 0x10: buffer =
                                $"SWP{cc[(opcode >> 28) & 15]}    {RN(opcode)},{RM(opcode)},{RD(opcode)}"; break;
                            case 0x14: buffer =
                                $"SWPB{cc[(opcode >> 28) & 15]}   {RN(opcode)},{RM(opcode)},{RD(opcode)}"; break;
                            default: handled = false; break;
                        }
                        break;

                    case 0xB0:	  // Unsigned halfwords
                        {
                            switch ((opcode >> 20) & 0xFF)
                            {
                                case 0x00: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x01: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x02: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x03: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x04: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x05: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x06: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x07: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x08: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x09: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0A: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0B: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0C: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0D: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0E: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0F: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x10: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x11: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x12: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x13: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x14: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x15: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x16: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x17: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x18: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x19: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1A: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1B: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1C: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1D: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1E: buffer =
                                    $"STRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1F: buffer =
                                    $"LDRH{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                default: handled = false; break;
                            }
                        }
                        break;

                    case 0xD0:	// Signed byte
                        {
                            switch ((opcode >> 20) & 0xFF)
                            {
                                case 0x00: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x01: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x02: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x03: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x04: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x05: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x06: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x07: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x08: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x09: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0A: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0B: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0C: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0D: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0E: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0F: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x10: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x11: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x12: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x13: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x14: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x15: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x16: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x17: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x18: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x19: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1A: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1B: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1C: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1D: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1E: buffer =
                                    $"STRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1F: buffer =
                                    $"LDRSB{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                default: handled = false; break;
                            }
                        }
                        break;

                    case 0xF0:	// Signed halfwords
                        {
                            switch ((opcode >> 20) & 0xFF)
                            {
                                case 0x00: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x01: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x02: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x03: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},-{RM(opcode)}]"; break;
                                case 0x04: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x05: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x06: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x07: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},-{H_IM(opcode)}]"; break;
                                case 0x08: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x09: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0A: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0B: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},+{RM(opcode)}]"; break;
                                case 0x0C: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0D: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0E: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x0F: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)},+{H_IM(opcode)}]"; break;
                                case 0x10: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x11: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x12: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x13: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{RM(opcode)}"; break;
                                case 0x14: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x15: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x16: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x17: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{H_IM(opcode)}"; break;
                                case 0x18: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x19: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1A: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1B: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],+{RM(opcode)}"; break;
                                case 0x1C: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1D: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1E: buffer =
                                    $"STRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],+{H_IM(opcode)}"; break;
                                case 0x1F: buffer =
                                    $"LDRSH{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],+{H_IM(opcode)}"; break;
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
                    case 0x00: buffer = $"AND{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x01: buffer = $"ANDS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x02: buffer = $"EOR{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x03: buffer = $"EORS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x04: buffer = $"SUB{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x05: buffer = $"SUBS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x06: buffer = $"RSB{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x07: buffer = $"RSBS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x08: buffer = $"ADD{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x09: buffer = $"ADDS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x0a: buffer = $"ADC{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x0b: buffer = $"ADCS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x0c: buffer = $"SBC{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x0d: buffer = $"SBCS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x0e: buffer = $"RSC{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x0f: buffer = $"RSCS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;

                    case 0x10: buffer = $"MRS{cc[(opcode >> 28) & 15]}    {RD(opcode)}"; break;
                    case 0x11: buffer = $"TSTS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x12:
                        {
                            if ((opcode & 0x10) != 0)
                                buffer = $"BX{cc[(opcode >> 28) & 15]}     {RM(opcode)}";
                            else
                                buffer = $"MSR{cc[(opcode >> 28) & 15]}    {RM(opcode)}";
                        }
                        break;
                    case 0x13: buffer = $"TEQS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x14: buffer = $"MRSS{cc[(opcode >> 28) & 15]}   {RD(opcode)}"; break;
                    case 0x15: buffer = $"CMPS{cc[(opcode >> 28) & 15]}   {RN(opcode)},{RS(opcode)}"; break;
                    case 0x16: buffer = $"MSRS{cc[(opcode >> 28) & 15]}   {RM(opcode)}"; break;
                    case 0x17: buffer = $"CMNS{cc[(opcode >> 28) & 15]}   {RN(opcode)},{RS(opcode)}"; break;
                    case 0x18: buffer = $"ORR{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x19: buffer = $"ORRS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x1a: buffer = $"MOV{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RS(opcode)}"; break;
                    case 0x1b: buffer = $"MOVS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RS(opcode)}"; break;
                    case 0x1c: buffer = $"BIC{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x1d: buffer = $"BICS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{RS(opcode)}"; break;
                    case 0x1e: buffer = $"MVN{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RS(opcode)}"; break;
                    case 0x1f: buffer = $"MVNS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RS(opcode)}"; break;

                    case 0x20: buffer = $"AND{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x21: buffer = $"ANDS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x22: buffer = $"EOR{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x23: buffer = $"EORS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x24: buffer = $"SUB{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x25: buffer = $"SUBS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x26: buffer = $"RSB{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x27: buffer = $"RSBS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x28: buffer = $"ADD{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x29: buffer = $"ADDS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x2a: buffer = $"ADC{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x2b: buffer = $"ADCS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x2c: buffer = $"SBC{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x2d: buffer = $"SBCS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x2e: buffer = $"RSC{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x2f: buffer = $"RSCS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;

                    case 0x30: buffer = $"TST{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x31: buffer = $"TSTS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x32: buffer = $"MSRB{cc[(opcode >> 28) & 15]}   {IM(opcode)}"; break;
                    case 0x33: buffer = $"TEQS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x34: buffer = $"CMP{cc[(opcode >> 28) & 15]}    {RN(opcode)},{IM(opcode)}"; break;
                    case 0x35: buffer = $"CMPS{cc[(opcode >> 28) & 15]}   {RN(opcode)},{IM(opcode)}"; break;
                    case 0x36: buffer = $"MSRBS{cc[(opcode >> 28) & 15]}  {IM(opcode)}"; break;
                    case 0x37: buffer = $"CMNS{cc[(opcode >> 28) & 15]}   {RN(opcode)},{IM(opcode)}"; break;
                    case 0x38: buffer = $"ORR{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x39: buffer = $"ORRS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x3a: buffer = $"MOV{cc[(opcode >> 28) & 15]}    {RD(opcode)},{IM(opcode)}"; break;
                    case 0x3b: buffer = $"MOVS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{IM(opcode)}"; break;
                    case 0x3c: buffer = $"BIC{cc[(opcode >> 28) & 15]}    {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x3d: buffer = $"BICS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{RN(opcode)},{IM(opcode)}"; break;
                    case 0x3e: buffer = $"MVN{cc[(opcode >> 28) & 15]}    {RD(opcode)},{IM(opcode)}"; break;
                    case 0x3f: buffer = $"MVNS{cc[(opcode >> 28) & 15]}   {RD(opcode)},{IM(opcode)}"; break;

                    case 0x40: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],-{LIM(opcode)}"; break;
                    case 0x41: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],-{LIM(opcode)}"; break;
                    case 0x42: buffer = $"STRT{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{LIM(opcode)}"; break;
                    case 0x43: buffer = $"LDRT{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{LIM(opcode)}"; break;
                    case 0x44: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{LIM(opcode)}"; break;
                    case 0x45: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{LIM(opcode)}"; break;
                    case 0x46: buffer = $"STRBT{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{LIM(opcode)}"; break;
                    case 0x47: buffer = $"LDRBT{cc[(opcode >> 28) & 15]}  {RD(opcode)},[{RN(opcode)}],-{LIM(opcode)}"; break;
                    case 0x48: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],{LIM(opcode)}"; break;
                    case 0x49: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],{LIM(opcode)}"; break;
                    case 0x4a: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],{LIM(opcode)}"; break;
                    case 0x4b: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],{LIM(opcode)}"; break;
                    case 0x4c: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],{LIM(opcode)}"; break;
                    case 0x4d: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],{LIM(opcode)}"; break;
                    case 0x4e: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],{LIM(opcode)}"; break;
                    case 0x4f: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],{LIM(opcode)}"; break;

                    case 0x50: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)},-{LIM(opcode)}]"; break;
                    case 0x51: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)},-{LIM(opcode)}]"; break;
                    case 0x52: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)}!,[{RN(opcode)},-{LIM(opcode)}]"; break;
                    case 0x53: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)}!,[{RN(opcode)},-{LIM(opcode)}]"; break;
                    case 0x54: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},-{LIM(opcode)}]"; break;
                    case 0x55: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},-{LIM(opcode)}]"; break;
                    case 0x56: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},-{LIM(opcode)}]"; break;
                    case 0x57: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},-{LIM(opcode)}]"; break;
                    case 0x58: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)},{LIM(opcode)}]"; break;
                    case 0x59: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)},{LIM(opcode)}]"; break;
                    case 0x5a: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)}!,[{RN(opcode)},{LIM(opcode)}]"; break;
                    case 0x5b: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)}!,[{RN(opcode)},{LIM(opcode)}]"; break;
                    case 0x5c: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},{LIM(opcode)}]"; break;
                    case 0x5d: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},{LIM(opcode)}]"; break;
                    case 0x5e: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},{LIM(opcode)}]"; break;
                    case 0x5f: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},{LIM(opcode)}]"; break;

                    case 0x60: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],-{RS_S(opcode)}"; break;
                    case 0x61: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],-{RS_S(opcode)}"; break;
                    case 0x62: buffer = $"STRT{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],-{RS_S(opcode)}"; break;
                    case 0x63: buffer = $"LDRT{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],-{RS_S(opcode)}"; break;
                    case 0x64: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{RS_S(opcode)}"; break;
                    case 0x65: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],-{RS_S(opcode)}"; break;
                    case 0x66: buffer = $"STRBT{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{RS_S(opcode)}"; break;
                    case 0x67: buffer = $"LDRBT{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],-{RS_S(opcode)}"; break;
                    case 0x68: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],{RS_S(opcode)}"; break;
                    case 0x69: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)}],{RS_S(opcode)}"; break;
                    case 0x6a: buffer = $"STRT{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],{RS_S(opcode)}"; break;
                    case 0x6b: buffer = $"LDRT{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)}],{RS_S(opcode)}"; break;
                    case 0x6c: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],{RS_S(opcode)}"; break;
                    case 0x6d: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)}],{RS_S(opcode)}"; break;
                    case 0x6e: buffer = $"STRBT{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],{RS_S(opcode)}"; break;
                    case 0x6f: buffer = $"LDRBT{cc[(opcode >> 28) & 15]}  {RD(opcode)}!,[{RN(opcode)}],{RS_S(opcode)}"; break;

                    case 0x70: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)},-{RS_S(opcode)}]"; break;
                    case 0x71: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)},-{RS_S(opcode)}]"; break;
                    case 0x72: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)}!,[{RN(opcode)},-{RS_S(opcode)}]"; break;
                    case 0x73: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)}!,[{RN(opcode)},-{RS_S(opcode)}]"; break;
                    case 0x74: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},-{RS_S(opcode)}]"; break;
                    case 0x75: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},-{RS_S(opcode)}]"; break;
                    case 0x76: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},-{RS_S(opcode)}]"; break;
                    case 0x77: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},-{RS_S(opcode)}]"; break;
                    case 0x78: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)},{RS_S(opcode)}]"; break;
                    case 0x79: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)},[{RN(opcode)},{RS_S(opcode)}]"; break;
                    case 0x7a: buffer = $"STR{cc[(opcode >> 28) & 15]}    {RD(opcode)}!,[{RN(opcode)},{RS_S(opcode)}]"; break;
                    case 0x7b: buffer = $"LDR{cc[(opcode >> 28) & 15]}    {RD(opcode)}!,[{RN(opcode)},{RS_S(opcode)}]"; break;
                    case 0x7c: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},{RS_S(opcode)}]"; break;
                    case 0x7d: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)},[{RN(opcode)},{RS_S(opcode)}]"; break;
                    case 0x7e: buffer = $"STRB{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},{RS_S(opcode)}]"; break;
                    case 0x7f: buffer = $"LDRB{cc[(opcode >> 28) & 15]}   {RD(opcode)}!,[{RN(opcode)},{RS_S(opcode)}]"; break;

                    case 0x80: buffer = $"STMDA{cc[(opcode >> 28) & 15]}  {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x81: buffer = $"LDMDA{cc[(opcode >> 28) & 15]}  {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x82: buffer = $"STMDA{cc[(opcode >> 28) & 15]}  {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x83: buffer = $"LDMDA{cc[(opcode >> 28) & 15]}  {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x84: buffer = $"STMDAB{cc[(opcode >> 28) & 15]} {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x85: buffer = $"LDMDAB{cc[(opcode >> 28) & 15]} {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x86: buffer = $"STMDAB{cc[(opcode >> 28) & 15]} {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x87: buffer = $"LDMDAB{cc[(opcode >> 28) & 15]} {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x88: buffer = $"STMIA{cc[(opcode >> 28) & 15]}  {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x89: buffer = $"LDMIA{cc[(opcode >> 28) & 15]}  {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x8a: buffer = $"STMIA{cc[(opcode >> 28) & 15]}  {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x8b: buffer = $"LDMIA{cc[(opcode >> 28) & 15]}  {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x8c: buffer = $"STMIAB{cc[(opcode >> 28) & 15]} {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x8d: buffer = $"LDMIAB{cc[(opcode >> 28) & 15]} {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x8e: buffer = $"STMIAB{cc[(opcode >> 28) & 15]} {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x8f: buffer = $"LDMIAB{cc[(opcode >> 28) & 15]} {RN(opcode)}!,[{RL(opcode)}]"; break;

                    case 0x90: buffer = $"STMDB{cc[(opcode >> 28) & 15]}  {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x91: buffer = $"LDMDB{cc[(opcode >> 28) & 15]}  {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x92: buffer = $"STMDB{cc[(opcode >> 28) & 15]}  {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x93: buffer = $"LDMDB{cc[(opcode >> 28) & 15]}  {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x94: buffer = $"STMDBB{cc[(opcode >> 28) & 15]} {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x95: buffer = $"LDMDBB{cc[(opcode >> 28) & 15]} {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x96: buffer = $"STMDBB{cc[(opcode >> 28) & 15]} {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x97: buffer = $"LDMDBB{cc[(opcode >> 28) & 15]} {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x98: buffer = $"STMIB{cc[(opcode >> 28) & 15]}  {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x99: buffer = $"LDMIB{cc[(opcode >> 28) & 15]}  {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x9a: buffer = $"STMIB{cc[(opcode >> 28) & 15]}  {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x9b: buffer = $"LDMIB{cc[(opcode >> 28) & 15]}  {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x9c: buffer = $"STMIBB{cc[(opcode >> 28) & 15]} {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x9d: buffer = $"LDMIBB{cc[(opcode >> 28) & 15]} {RN(opcode)},[{RL(opcode)}]"; break;
                    case 0x9e: buffer = $"STMIBB{cc[(opcode >> 28) & 15]} {RN(opcode)}!,[{RL(opcode)}]"; break;
                    case 0x9f: buffer = $"LDMIBB{cc[(opcode >> 28) & 15]} {RN(opcode)}!,[{RL(opcode)}]"; break;

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
                            var offset = opcode & 0x00FFFFFF;

                            if ((offset & 0x800000) != 0)
                            {
                                offset = (offset | 0xFF000000) << 2;
                            }
                            else
                            {
                                offset <<= 2;
                            }

                            buffer = $"B{cc[(opcode >> 28) & 15]}      0x{8 + pc + offset:X}";
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
                            var offset = opcode & 0x00FFFFFF;

                            if ((offset & 0x800000) != 0)
                            {
                                offset = (offset | 0xFF000000) << 2;
                            }
                            else
                            {
                                offset <<= 2;
                            }
                            buffer = $"BL{cc[(opcode >> 28) & 15]}     0x{8 + pc + offset:X}";
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
                        buffer = $"ILL{cc[(opcode >> 28) & 15]}    0x{opcode:X8}";
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
                        buffer = $"SWI{cc[(opcode >> 28) & 15]}    0x{opcode & 0xFFFFFF:X8}";
                        break;
                    default:
                        buffer = $"???{cc[(opcode >> 28) & 15]}    0x{opcode:X8}";
                        break;
                }

            if (buffer == null) throw new Exception("Unhandled instruction decode");

            return buffer;
        }
        #endregion
    }
}