using System;
using System.Collections.Generic;

namespace GarboDev.Cores.DynamicCore
{
    public class ArmletGenerator
    {
        public class Label
        {
            public List<int> Parents = new List<int>();
            public int Position = -1;
        }

        private readonly List<byte> _data = new List<byte>(60);
        private readonly List<Label> _labels = new List<Label>();

        public List<byte> Data => _data;

        public void AddUint(uint argument)
        {
            _data.Add((byte)(argument & 0xff));
            _data.Add((byte)((argument >> 8) & 0xff));
            _data.Add((byte)((argument >> 16) & 0xff));
            _data.Add((byte)((argument >> 24) & 0xff));
        }

        public Label DefineLabel()
        {
            var tmp = new Label();
            _labels.Add(tmp);
            return tmp;
        }

        public void MarkLabel(Label label)
        {
            if (label.Position != -1)
            {
                throw new Exception("Can't change label position after defined");
            }

            label.Position = _data.Count;

            foreach (var parent in label.Parents)
            {
                _data[parent] = (byte)(label.Position & 0xff);
                _data[parent + 1] = (byte)((label.Position >> 8) & 0xff);
                _data[parent + 2] = (byte)((label.Position >> 16) & 0xff);
                _data[parent + 3] = (byte)((label.Position >> 24) & 0xff);
            }
        }

        public void Emit(Armlet armlet)
        {
            _data.Add(armlet.Opcode);
            _data.Add(armlet.Flags);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags)
        {
            _data.Add(armlet.Opcode);
            _data.Add((byte)flags);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, uint argument)
        {
            _data.Add(armlet.Opcode);
            _data.Add(armlet.Flags);
            AddUint(argument);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, uint argument)
        {
            _data.Add(armlet.Opcode);
            _data.Add((byte)flags);
            AddUint(argument);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, byte argument)
        {
            _data.Add(armlet.Opcode);
            _data.Add(armlet.Flags);
            _data.Add(argument);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, byte argument)
        {
            _data.Add(armlet.Opcode);
            _data.Add((byte)flags);
            _data.Add(argument);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, byte argument1, byte argument2)
        {
            _data.Add(armlet.Opcode);
            _data.Add(armlet.Flags);
            _data.Add(argument1);
            _data.Add(argument2);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, byte argument1, byte argument2)
        {
            _data.Add(armlet.Opcode);
            _data.Add((byte)flags);
            _data.Add(argument1);
            _data.Add(argument2);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, byte argument1, byte argument2, byte argument3)
        {
            _data.Add(armlet.Opcode);
            _data.Add(armlet.Flags);
            _data.Add(argument1);
            _data.Add(argument2);
            _data.Add(argument3);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, byte argument1, byte argument2, byte argument3)
        {
            _data.Add(armlet.Opcode);
            _data.Add((byte)flags);
            _data.Add(argument1);
            _data.Add(argument2);
            _data.Add(argument3);
            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Label label)
        {
            _data.Add(armlet.Opcode);
            _data.Add(armlet.Flags);

            label.Parents.Add(_data.Count);
            AddUint((uint)label.Position);

            _data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, Label label)
        {
            _data.Add(armlet.Opcode);
            _data.Add((byte)flags);

            label.Parents.Add(_data.Count);
            AddUint((uint)label.Position);

            _data.Add(armlet.Size);
        }
    }
}
