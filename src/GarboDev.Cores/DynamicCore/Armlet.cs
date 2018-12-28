namespace GarboDev.Cores.DynamicCore
{
    public struct Armlet
    {
        public enum FlagDefinitions
        {
            V_BIT = 1 << 0,
            C_BIT = 1 << 1,
            Z_BIT = 1 << 2,
            N_BIT = 1 << 3
        }

        public string Name;
        public byte Opcode;
        public byte Flags;
        public byte Size;
        
        public byte InFlags
        {
            get { return (byte)(this.Flags >> 4); }
        }

        public byte OutFlags
        {
            get { return (byte)(this.Flags & 0xF); }
        }

        public Armlet(string name, ArmletOpcodes opcode, FlagDefinitions inFlags, FlagDefinitions outFlags, byte size)
        {
            this.Name = name;
            this.Opcode = (byte)opcode;
            this.Flags = MakeFlag(inFlags, outFlags);
            this.Size = size;
        }

        public static byte MakeFlag(FlagDefinitions inFlags, FlagDefinitions outFlags)
        {
            return (byte)(((byte)inFlags << 4) | (byte)outFlags);
        }
    }
}
