#region License
/* 
 * Copyright (C) 1999-2011 John K�ll�n.
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
using Decompiler.Core.Code;
using Decompiler.Core.Expressions;
using Decompiler.Core.Operators;
using Decompiler.Core.Types;
using System.Collections.Generic;
using System;

namespace Decompiler.UnitTests.Mocks
{
    /// <summary>
    /// Supports the building of a procedure without having to go through assembler.
    /// </summary>
    public class ProcedureBuilder : CodeEmitter2
    {
        private Block block;
        private Dictionary<string, Block> blocks;
        private Procedure proc;
        private Block branchBlock;
        private Block lastBlock;
        private int numBlock;
        private ProgramBuilder programMock;
        private List<ProcUpdater> unresolvedProcedures;

        public ProcedureBuilder()
        {
            Init(this.GetType().Name);
        }

        public ProcedureBuilder(string name)
        {
            Init(name);
        }

        private void Init(string name)
        {
            blocks = new Dictionary<string, Block>();
            proc = new Procedure(name, new Frame(PrimitiveType.Word32));
            unresolvedProcedures = new List<ProcUpdater>();
            BuildBody();
            proc.RenumberBlocks();
        }

        /// <summary>
        /// Current block, into which the next statement will be added.
        /// </summary>
        public Block Block
        {
            get { return block; }
        }

        private Block BlockOf(string label)
        {
            Block b;
            if (!blocks.TryGetValue(label, out b))
            {
                b = proc.AddBlock(label);
                blocks.Add(label, b);
            }
            return b;
        }

        public void BranchCc(ConditionCode cc, string label)
        {
            Identifier f;
            switch (cc)
            {
            case ConditionCode.EQ:
            case ConditionCode.NE: f = Flags("Z"); break;
            default: throw new ArgumentOutOfRangeException("Condition code: " + cc);
            }
            branchBlock = BlockOf(label);
            Emit(new Branch(new TestCondition(cc, f), branchBlock));
            TerminateBlock();
        }

        public Statement BranchIf(Expression expr, string label)
        {
            Block b = EnsureBlock(null);
            branchBlock = BlockOf(label);
            TerminateBlock();

            Statement stm = new Statement(new Branch(expr, branchBlock), b);
            b.Statements.Add(stm);
            return stm;
        }

        protected virtual void BuildBody()
        {
        }

        public Statement Call(string procedureName)
        {
            CallInstruction ci = new CallInstruction(null, new CallSite(0, 0));
            unresolvedProcedures.Add(new ProcedureConstantUpdater(procedureName, ci));
            return Emit(ci);
        }

        public Statement Call(ProcedureBase callee)
        {
            ProcedureConstant c = new ProcedureConstant(PrimitiveType.Pointer32, callee);
            CallInstruction ci = new CallInstruction(c, new CallSite(0, 0));
            return Emit(ci);
        }

        public void Compare(string flags, Expression a, Expression b)
        {
            Assign(Flags(flags), new ConditionOf(Sub(a, b)));
        }

        public Block CurrentBlock
        {
            get { return this.block; }
        }

        public Identifier Declare(DataType dt, string name)
        {
            return proc.Frame.CreateTemporary(name, dt);
        }

        public Identifier Declare(DataType dt, string name, Expression expr)
        {
            Identifier id = proc.Frame.CreateTemporary(name, dt);
            Emit(new Declaration(id, expr));
            return id;
        }


        public Statement Declare(Identifier id, Expression initial)
        {
            return Emit(new Declaration(id, initial));
        }

        public override Statement Emit(Instruction instr)
        {
            EnsureBlock(null);
            block.Statements.Add(instr);
            return block.Statements.Last;
        }

        public Identifier Flags(string s)
        {
            uint grf = 0;
            for (int i = 0; i < s.Length; ++i)
            {
                switch (s[i])
                {
                case 'S': grf |= 0x01; break;
                case 'Z': grf |= 0x02; break;
                case 'C': grf |= 0x04; break;
                }
            }
            return base.Flags(grf, s);
        }

        public Application Fn(string name, params Expression[] exps)
        {
            Application appl = new Application(
                new ProcedureConstant(PrimitiveType.Pointer32, new PseudoProcedure(name, PrimitiveType.Void, 0)),
                PrimitiveType.Word32, exps);
            unresolvedProcedures.Add(new ApplicationUpdater(name, appl));
            return appl;
        }

        public void Jump(string name)
        {
            EnsureBlock(null);
            Block blockTo = BlockOf(name);
            proc.AddEdge(block, blockTo);
            block = null;
        }

        public Block Label(string name)
        {
            TerminateBlock();
            return EnsureBlock(name);
        }

        private Block EnsureBlock(string name)
        {
            if (block != null)
                return block;

            if (name == null)
            {
                name = string.Format("l{0}", ++numBlock);
            }
            block = BlockOf(name);
            if (proc.EntryBlock.Succ.Count == 0)
            {
                proc.AddEdge(proc.EntryBlock, block);
            }

            if (lastBlock != null)
            {
                if (branchBlock != null)
                {
                    proc.AddEdge(lastBlock, block);
                    proc.AddEdge(lastBlock, branchBlock);
                    branchBlock = null;
                }
                else
                {
                    proc.AddEdge(lastBlock, block);
                }
                lastBlock = null;
            }
            return block;
        }



        public void FinishProcedure()
        {
            TerminateBlock();
            proc.AddEdge(lastBlock, proc.ExitBlock);
        }

        public ICollection<ProcUpdater> UnresolvedProcedures
        {
            get { return unresolvedProcedures; }
        }

        public override Frame Frame
        {
            get { return proc.Frame; }
        }

        public Procedure Procedure
        {
            get { return proc; }
        }

        public ProgramBuilder ProgramMock
        {
            get { return programMock; }
            set { programMock = value; }
        }

        public override Identifier Register(int i)
        {
            return Frame.EnsureRegister(ArchitectureMock.GetMachineRegister(i));
        }

        public override void Return(Expression exp)
        {
            base.Return(exp);
            proc.AddEdge(block, proc.ExitBlock);
            block = null;
        }

        public void Switch(Expression e, params string[] labels)
        {
            Block[] blox = new Block[labels.Length];
            for (int i = 0; i < blox.Length; ++i)
            {
                blox[i] = BlockOf(labels[i]);
            }

            Emit(new SwitchInstruction(e, blox));
            for (int i = 0; i < blox.Length; ++i)
            {
                proc.AddEdge(this.block, blox[i]);
            }
            lastBlock = null;
            block = null;
        }

        private void TerminateBlock()
        {
            if (block != null)
            {
                lastBlock = block;
                block = null;
            }
        }

    }

}