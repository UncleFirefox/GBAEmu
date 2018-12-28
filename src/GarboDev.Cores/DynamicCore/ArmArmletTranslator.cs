using GarboDev.CrossCutting;

namespace GarboDev.Cores.DynamicCore
{
    public class ArmArmletTranslator
    {
        private readonly Arm7Processor _parent;
        private Memory _memory;
        private uint[] _registers;

        public ArmArmletTranslator(Arm7Processor parent, Memory memory)
        {
            _parent = parent;
            _memory = memory;
            _registers = _parent.Registers;
        }
    }
}
