﻿namespace IntelOrca.Biohazard.Script.Compilation
{
    internal enum TokenKind : byte
    {
        Unknown,
        Whitespace,
        NewLine,
        Comment,
        Number,
        String,
        Symbol,
        Label,
        Comma,
        Plus,
        Minus,
        Asterisk,
        Pipe,
        Ampersand,
        AmpersandAmpersand,
        LShift,
        RShift,
        Opcode,
        Directive,

        BackSlash,
        FowardSlash,
        Colon,
        Semicolon,
        OpenBlock,
        CloseBlock,
        OpenParen,
        CloseParen,
        Proc,
        Fork,
        If,
        Else,
        While,
        Do,
        Switch,
        Case,
        Default,
        Break,
        Repeat,

        EOF
    }
}
