﻿#region License
/* 
 * Copyright (C) 1999-2011 John Källén.
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
using Decompiler.Core.Rtl;
using Decompiler.Core.Code;
using Decompiler.Core.Expressions;
using Decompiler.Core.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Decompiler.UnitTests.Mocks
{
    public class RtlStatementStream : ExpressionEmitter

    {
        private Block block;
        private Frame frame;
        private List<RtlInstruction> stms;
        private IProcessorArchitecture arch;
        private uint linAddress;

        public RtlStatementStream(uint address, Block block)
        {
            this.linAddress = address;
            this.block = block;
            this.frame = block.Procedure.Frame;
            this.arch = new ArchitectureMock();
            this.stms = new List<RtlInstruction>();   
        }

        public RtlInstruction Emit(RtlInstruction instr)
        {
            stms.Add(instr);
            linAddress += 4;
            return instr;
        }

        public Block Block
        {
            get { return block; }
        }

        public RtlInstruction Assign(Expression dst, int n)
        {
             return Assign(dst, new Constant(dst.DataType, n));
        }

        public RtlInstruction Assign(Expression dst, Expression src)
        {
            var ass = new RtlAssignment(new Address(linAddress), 4, dst, src);
            return Emit(ass);
        }

        public RtlInstruction Branch(Expression cond, Address target)
        {
            var br = new RtlBranch(new Address(linAddress), 4, cond, target);
            return Emit(br);
        }

        public RtlInstruction Call(Expression target)
        {
            var call = new RtlCall(new Address(linAddress), 4, target);
            return Emit(call);
        }

        public Frame Frame
        {
            get { return frame; }
        }


        public IEnumerator<RtlInstruction> GetRewrittenInstructions()
        {
            foreach (var x in stms)
                yield return x;
        }


        internal RtlInstruction Goto(uint target)
        {
            var g = new RtlGoto(new Address(linAddress), 4, new Address(target));
            return Emit(g);
        }

        public Identifier Register(int iReg)
        {
            return frame.EnsureRegister(arch.GetRegister(iReg));
        }

        internal RtlInstruction Return()
        {
            var ret = new RtlReturn(new Address(linAddress), 4, 0, 0);
            return Emit(ret);
        }

        internal RtlInstruction SideEffect(Expression exp)
        {
            var side = new RtlSideEffect(new Address(linAddress), 4, exp);
            return Emit(side);
        }


    }
}