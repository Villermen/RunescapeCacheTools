﻿namespace Villermen.RuneScapeCacheTools.FileProcessors.Enums
{
    public enum EnumValueType
    {
        UInt1 = 0x00, // 0x01280001, 0x01280002 | 0x000003e9, 0x000003ea
        UInt2 = 0x01, // 0x00000001, 0x00000001
        UInt3 = 0x17, // 0x00000885, 0x00000886
        UInt4 = 0x1f, //0x803a0001, 0x803e0002
        UInt5 = 0x21, // 0x10db0002, 0x10dd0003
        UInt6 = 0x2a, // 0x03e90000, 0x03ea0000
        NullTerminatedString = 0x24,
        UShort = 0x49 // 0x01fc	
    }
}