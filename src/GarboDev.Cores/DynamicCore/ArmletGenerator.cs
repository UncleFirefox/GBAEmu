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

        private List<byte> data = new List<byte>(60);
        private List<Label> labels = new List<Label>();

        public List<byte> Data
        {
            get { return this.data; }
        }

        public void AddUint(uint argument)
        {
            this.data.Add((byte)(argument & 0xff));
            this.data.Add((byte)((argument >> 8) & 0xff));
            this.data.Add((byte)((argument >> 16) & 0xff));
            this.data.Add((byte)((argument >> 24) & 0xff));
        }

        public Label DefineLabel()
        {
            Label tmp = new Label();
            this.labels.Add(tmp);
            return tmp;
        }

        public void MarkLabel(Label label)
        {
            if (label.Position != -1)
            {
                throw new Exception("Can't change label position after defined");
            }

            label.Position = this.data.Count;

            foreach (int parent in label.Parents)
            {
                this.data[parent] = (byte)(label.Position & 0xff);
                this.data[parent + 1] = (byte)((label.Position >> 8) & 0xff);
                this.data[parent + 2] = (byte)((label.Position >> 16) & 0xff);
                this.data[parent + 3] = (byte)((label.Position >> 24) & 0xff);
            }
        }

        public void Emit(Armlet armlet)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add(armlet.Flags);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add((byte)flags);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, uint argument)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add(armlet.Flags);
            this.AddUint(argument);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, uint argument)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add((byte)flags);
            this.AddUint(argument);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, byte argument)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add(armlet.Flags);
            this.data.Add(argument);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, byte argument)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add((byte)flags);
            this.data.Add(argument);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, byte argument1, byte argument2)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add(armlet.Flags);
            this.data.Add(argument1);
            this.data.Add(argument2);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, byte argument1, byte argument2)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add((byte)flags);
            this.data.Add(argument1);
            this.data.Add(argument2);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, byte argument1, byte argument2, byte argument3)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add(armlet.Flags);
            this.data.Add(argument1);
            this.data.Add(argument2);
            this.data.Add(argument3);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, byte argument1, byte argument2, byte argument3)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add((byte)flags);
            this.data.Add(argument1);
            this.data.Add(argument2);
            this.data.Add(argument3);
            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Label label)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add(armlet.Flags);

            label.Parents.Add(this.data.Count);
            this.AddUint((uint)label.Position);

            this.data.Add(armlet.Size);
        }

        public void Emit(Armlet armlet, Armlet.FlagDefinitions flags, Label label)
        {
            this.data.Add(armlet.Opcode);
            this.data.Add((byte)flags);

            label.Parents.Add(this.data.Count);
            this.AddUint((uint)label.Position);

            this.data.Add(armlet.Size);
        }
    }
}
