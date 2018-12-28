namespace GarboDev.Cores.DynamicCore
{
    public enum ArmletOpcodes
    {
        // No variable opcodes
        Setpc = 0,

        // One variable opcodes
        Ldc,
        Ldcb,

        // Two variable opcodes
        Mov,
        Mvn,
        Tst,
        Teq,
        Cmp,
        Cmn,
        Rrx,

        Ldb,
        Ldsb,
        Ldh,
        Ldsh,
        Ldw,

        Stb,
        Sth,
        Stw,

        // Three variable opcodes
        Add,
        Adc,
        And,
        Asr,
        Eor,
        Lsl,
        Lsr,
        Mul,
        Or,
        Ror,
        Sbc,
        Sub,

        // Goto opcodes
        GotoEq,
        GotoNe,
        GotoCs,
        GotoCc,
        GotoMi,
        GotoPl,
        GotoVs,
        GotoVc,
        GotoHi,
        GotoLs,
        GotoGe,
        GotoLt,
        GotoGt,
        GotoLe,
        Goto,
        Leave
    }
}
