using System.ComponentModel;

namespace Sim80C51.Processors
{
    public enum InstructionType
    {
        [Description("Absolute Call")]
        ACALL,
        [Description("Add Accumulator")]
        ADD,
        [Description("Add Accumulator With Carry")]
        ADDC,
        [Description("Absolute Jump")]
        AJMP,
        [Description("Bitwise AND")]
        ANL,
        [Description("Compare and Jump if Not Equal")]
        CJNE,
        [Description("Clear Register")]
        CLR,
        [Description("Complement Register")]
        CPL,
        [Description("Decimal Adjust")]
        DA,
        [Description("Decrement Register")]
        DEC,
        [Description("Divide Accumulator by B")]
        DIV,
        [Description("Decrement Register and Jump if Not Zero")]
        DJNZ,
        [Description("Increment Register")]
        INC,
        [Description("Jump if Bit Set")]
        JB,
        [Description("Jump if Bit Set and Clear Bit")]
        JBC,
        [Description("Jump if Carry Set")]
        JC,
        [Description("Jump to Address")]
        JMP,
        [Description("Jump if Bit Not Set")]
        JNB,
        [Description("Jump if Carry Not Set")]
        JNC,
        [Description("Jump if Accumulator Not Zero")]
        JNZ,
        [Description("Jump if Accumulator Zero")]
        JZ,
        [Description("Long Call")]
        LCALL,
        [Description("Long Jump")]
        LJMP,
        [Description("Move Memory")]
        MOV,
        [Description("Move Code Memory")]
        MOVC,
        [Description("Move Extended Memory")]
        MOVX,
        [Description("Multiply Accumulator by B")]
        MUL,
        [Description("No Operation")]
        NOP,
        [Description("Bitwise OR")]
        ORL,
        [Description("Pop Value From Stack")]
        POP,
        [Description("Push Value Onto Stack")]
        PUSH,
        [Description("Return From Subroutine")]
        RET,
        [Description("Return From Interrupt")]
        RETI,
        [Description("Rotate Accumulator Left")]
        RL,
        [Description("Rotate Accumulator Left Through Carry")]
        RLC,
        [Description("Rotate Accumulator Right")]
        RR,
        [Description("Rotate Accumulator Right Through Carry")]
        RRC,
        [Description("Set Bit")]
        SETB,
        [Description("Short Jump")]
        SJMP,
        [Description("Subtract From Accumulator With Borrow")]
        SUBB,
        [Description("Swap Accumulator Nibbles")]
        SWAP,
        [Description("Exchange Bytes")]
        XCH,
        [Description("Exchange Digits")]
        XCHD,
        [Description("Bitwise Exclusive OR")]
        XRL,

        // Assembler commands
        [Description("Define Byte")]
        DB,
        [Description("Define Word")]
        DW,
        [Description("Define a Bit")]
        DBIT,
        [Description("Origin")]
        ORG,
    }
}
