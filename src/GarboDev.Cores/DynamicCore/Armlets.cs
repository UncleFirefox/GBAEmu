namespace GarboDev.Cores.DynamicCore
{
    public static class Armlets
    {
        static Armlets()
        {
            const Armlet.FlagDefinitions N = Armlet.FlagDefinitions.N_BIT;
            const Armlet.FlagDefinitions Z = Armlet.FlagDefinitions.Z_BIT;
            const Armlet.FlagDefinitions C = Armlet.FlagDefinitions.C_BIT;
            const Armlet.FlagDefinitions V = Armlet.FlagDefinitions.V_BIT;

            Definitions = new Armlet[]
                {
                    new Armlet("SetPc", ArmletOpcodes.Setpc, 0, 0, 2),

                    new Armlet("LoadConst", ArmletOpcodes.Ldc, 0, 0, 6),
                    new Armlet("LoadConstByte", ArmletOpcodes.Ldcb, 0, 0, 3),

                    new Armlet("Mov", ArmletOpcodes.Mov, 0, N|Z, 4),
                    new Armlet("Mvn", ArmletOpcodes.Mvn, 0, N|Z, 4),
                    new Armlet("Tst", ArmletOpcodes.Tst, 0, N|Z, 4),
                    new Armlet("Teq", ArmletOpcodes.Teq, 0, N|Z, 4),
                    new Armlet("Cmp", ArmletOpcodes.Cmp, 0, N|Z|C|V, 4),
                    new Armlet("Cmn", ArmletOpcodes.Cmn, 0, N|Z|C|V, 4),
                    new Armlet("Rrx", ArmletOpcodes.Rrx, C, C, 4),

                    new Armlet("LoadByte", ArmletOpcodes.Ldb, 0, 0, 4),
                    new Armlet("LoadSignedByte", ArmletOpcodes.Ldsb, 0, 0, 4),
                    new Armlet("LoadHalfword", ArmletOpcodes.Ldh, 0, 0, 4),
                    new Armlet("LoadSignedHalfword", ArmletOpcodes.Ldsh, 0, 0, 4),
                    new Armlet("LoadWord", ArmletOpcodes.Ldw, 0, 0, 4),

                    new Armlet("StoreByte", ArmletOpcodes.Stb, 0, 0, 4),
                    new Armlet("StoreHalfword", ArmletOpcodes.Sth, 0, 0, 4),
                    new Armlet("StoreWord", ArmletOpcodes.Stw, 0, 0, 4),

                    new Armlet("Add", ArmletOpcodes.Add, 0, N|Z|C|V, 5),
                    new Armlet("Adc", ArmletOpcodes.Adc, C, N|Z|C|V, 5),
                    new Armlet("And", ArmletOpcodes.And, 0, N|Z, 5),
                    new Armlet("Asr", ArmletOpcodes.Asr, 0, C, 5),
                    new Armlet("Eor", ArmletOpcodes.Eor, 0, N|Z, 5),
                    new Armlet("Lsl", ArmletOpcodes.Lsl, 0, C, 5),
                    new Armlet("Lsr", ArmletOpcodes.Lsr, 0, C, 5),
                    new Armlet("Mul", ArmletOpcodes.Mul, 0, N|Z, 5),
                    new Armlet("Or", ArmletOpcodes.Or, 0, N|Z, 5),
                    new Armlet("Ror", ArmletOpcodes.Ror, 0, C, 5),
                    new Armlet("Sbc", ArmletOpcodes.Sbc, C, N|Z|C|V, 5),
                    new Armlet("Sub", ArmletOpcodes.Sub, 0, N|Z|C|V, 5),

                    // Goto takes a constant operand, the number of bytes to jump (signed int)
                    new Armlet("GotoEq", ArmletOpcodes.GotoEq, Z, 0, 6),
                    new Armlet("GotoNe", ArmletOpcodes.GotoNe, Z, 0, 6),
                    new Armlet("GotoCs", ArmletOpcodes.GotoCs, C, 0, 6),
                    new Armlet("GotoCc", ArmletOpcodes.GotoCc, C, 0, 6),
                    new Armlet("GotoMi", ArmletOpcodes.GotoMi, N, 0, 6),
                    new Armlet("GotoPl", ArmletOpcodes.GotoPl, N, 0, 6),
                    new Armlet("GotoVs", ArmletOpcodes.GotoVs, V, 0, 6),
                    new Armlet("GotoVc", ArmletOpcodes.GotoVc, V, 0, 6),
                    new Armlet("GotoHi", ArmletOpcodes.GotoHi, C|Z, 0, 6),
                    new Armlet("GotoLs", ArmletOpcodes.GotoLs, C|Z, 0, 6),
                    new Armlet("GotoGe", ArmletOpcodes.GotoGe, N|V, 0, 6),
                    new Armlet("GotoLt", ArmletOpcodes.GotoLt, N|V, 0, 6),
                    new Armlet("GotoGt", ArmletOpcodes.GotoGt, Z|N|V, 0, 6),
                    new Armlet("GotoLe", ArmletOpcodes.GotoLe, Z|N|V, 0, 6),
                    new Armlet("Goto", ArmletOpcodes.Goto, 0, 0, 6),

                    // Leave leaves with the block in a completely correct state (i.e. interprative core could
                    // take over at that point)
                    new Armlet("Leave", ArmletOpcodes.Leave, N|Z|C|V, 0, 2)
                };
        }

        public static readonly Armlet[] Definitions;

        public static readonly Armlet Add = Definitions[(int)ArmletOpcodes.Add];
    }
}
