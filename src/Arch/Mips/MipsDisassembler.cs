﻿#region License
/* 
 * Copyright (C) 1999-2014 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using Decompiler.Core;
using Decompiler.Core.Expressions;
using Decompiler.Core.Lib;
using Decompiler.Core.Machine;
using Decompiler.Core.Rtl;
using Decompiler.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Decompiler.Arch.Mips
{
    public class MipsDisassembler : IDisassembler
    {
        private MipsProcessorArchitecture arch;
        private ImageReader rdr;

        public MipsDisassembler(MipsProcessorArchitecture arch, ImageReader imageReader)
        {
            this.arch = arch;
            this.rdr = imageReader;
        }

        public Address Address
        {
            get { return rdr.Address; }
        }

        public MachineInstruction DisassembleInstruction()
        {
            return Disassemble();
        }

        public MipsInstruction Disassemble()
        {
            var wInstr = rdr.ReadBeUInt32();
            var opRec = opRecs[wInstr >> 26];
            Debug.Print("Decoding {0:X8} => oprec {1} {2}", wInstr, wInstr >> 26, opRec == null ? "(null!)" : "");
            if (opRec == null)
                throw new NotImplementedException();    //$REVIEW: remove this when all oprecs are in place.
            return opRec.Decode(wInstr, this);
        }

        private static OpRec[] opRecs = new OpRec[]
        {
            new SpecialOpRec(),
            new CondOpRec(),
            new AOpRec(Opcode.j, "J"), 
            new AOpRec(Opcode.jal, "J"), 
            new AOpRec(Opcode.beq, "R1,R2,j"), 
            new AOpRec(Opcode.bne, "R1,R2,j"), 
            new AOpRec(Opcode.blez, "R1,j"), 
            new AOpRec(Opcode.bgtz, "R1,j"), 

            new AOpRec(Opcode.addi, "R2,R1,I"),
            new AOpRec(Opcode.addiu, "R2,R1,I"),
            null, 
            null,

            new AOpRec(Opcode.andi, "R2,R1,I"),
            null,
            null,
            new AOpRec(Opcode.lui, "R2,i"),
            // 10
            null,
            null,
            null, 
            null, 
            new AOpRec(Opcode.beql, "R1,R2,j"), 
            new AOpRec(Opcode.bnel, "R1,R2,j"), 
            new AOpRec(Opcode.blezl, "R1,j"), 
            new AOpRec(Opcode.bgtzl, "R1,j"), 

            new AOpRec(Opcode.daddi, "R2,R1,I"),
            new AOpRec(Opcode.daddiu, "R2,R1,I"),
            new AOpRec(Opcode.ldl, "R2,El"),
            new AOpRec(Opcode.ldr, "R2,El"),
            null,
            null,
            null,
            null, 
            // 20
            new AOpRec(Opcode.lb, "R2,EB"),
            new AOpRec(Opcode.lh, "R2,EH"),
            new AOpRec(Opcode.lwl, "R2,Ew"),
            new AOpRec(Opcode.lw, "R2,EW"),

            new AOpRec(Opcode.lbu, "R2,Eb"),
            new AOpRec(Opcode.lhu, "R2,Eh"),
            new AOpRec(Opcode.lwr, "R2,Ew"),
            new AOpRec(Opcode.lwu, "R2,Ew"),

            null, null, null, null, 
            null, null, null, null, 
            // 30
            new AOpRec(Opcode.ll, "R2,Ew"),
            null, 
            null, 
            null,

            new AOpRec(Opcode.lld, "R2,Ew"),
            null, 
            null,
            new AOpRec(Opcode.ld, "R2,El"),

            null, null, null, null,
            null, null, null, null,
        };

        public MipsInstruction DecodeOperands(Opcode opcode, uint wInstr, string opFmt)
        {
            var ops = new List<MachineOperand>();
            MachineOperand op = null;
            for (int i = 0; i < opFmt.Length; ++i)
            {
                switch (opFmt[i])
                {
                default: throw new NotImplementedException(string.Format("Operator format {0}", opFmt[i]));
                case ',':
                    continue;
                case 'R':
                    switch (opFmt[++i])
                    {
                    case '1': op = Reg(wInstr >> 21); break;
                    case '2': op = Reg(wInstr >> 16); break;
                    case '3': op = Reg(wInstr >> 11); break;
                    //case '4': op = MemOff(wInstr >> 6, wInstr); break;
                    default: throw new NotImplementedException(string.Format("Register field {0}.", opFmt[i]));
                    }
                    break;
                case 'I':
                    op = ImmediateOperand.Int32((short) wInstr);
                    break;
                case 'i':
                    op = ImmediateOperand.Int16((short) wInstr);
                    break;
                case 'j':
                    op = RelativeBranch(wInstr);
                    break;
                case 'J':
                    op = LargeBranch(wInstr);
                    break;
                case 'B':
                    op = ImmediateOperand.Word32((wInstr >> 6) & 0xFFFFF);
                    break;
                case 's':   // Shift amount
                    op = ImmediateOperand.Byte((byte)((wInstr >> 6) & 0x1F));
                    break;
                case 'E':   // effective address.
                    op = Ea(wInstr, opFmt[++i]);
                    break;
                }
                ops.Add(op);
            }
            return new MipsInstruction
            {
                opcode = opcode,
                op1 = ops.Count > 0 ? ops[0] : null,
                op2 = ops.Count > 1 ? ops[1] : null,
                op3 = ops.Count > 2 ? ops[2] : null,
            };
        }

        private RegisterOperand Reg(uint regNumber)
        {
            return new RegisterOperand(arch.GetRegister((int) regNumber & 0x1F));
        }

        [Obsolete("", true)]
        private ImmediateOperand Imm(uint uInstr)
        {
            return ImmediateOperand.Int32((short) uInstr);
        }

        private AddressOperand RelativeBranch(uint wInstr)
        {
            int off = (short) wInstr;
            off <<= 2;
            return AddressOperand.Ptr32((uint)(off + rdr.Address.Offset));
        }

        private AddressOperand LargeBranch(uint wInstr)
        {
            var off = (wInstr & 0x03FFFFFF) << 2;
            return AddressOperand.Ptr32((uint)((rdr.Address.Offset & 0xF0000000) | off));
        }

        private IndirectOperand Ea(uint wInstr, char wCode)
        {
            PrimitiveType dataWidth;
            switch (wCode)
            {
            default: throw new NotSupportedException(string.Format("Unknown width code '{0}'.", wCode));
            case 'b': dataWidth = PrimitiveType.Byte; break;
            case 'B': dataWidth = PrimitiveType.SByte; break;
            case 'h': dataWidth = PrimitiveType.Word16; break;
            case 'H': dataWidth = PrimitiveType.Int16; break;
            case 'w': dataWidth = PrimitiveType.Word32; break;
            case 'W': dataWidth = PrimitiveType.Int32; break;
            case 'l': dataWidth = PrimitiveType.Word64; break;
            case 'L': dataWidth = PrimitiveType.Int64; break;
            }
            var baseReg = arch.GetRegister((int)(wInstr >> 21) & 0x1F);
            int offset = (short) wInstr;
            return new IndirectOperand(dataWidth, offset, baseReg);
        }
    }
}