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

using Decompiler.Analysis;
using Decompiler.Core;
using Decompiler.Core.Expressions;
using Decompiler.Core.Types;
using Decompiler.Typing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Decompiler.Typing
{
    public class ConstantPointerTraversal : IDataTypeVisitor<IEnumerable<ConstantPointerTraversal.WorkItem>>
    {
        private IProcessorArchitecture arch;
        private StructureType globalStr;
        private LoadedImage image;
        private HashSet<int> visited;
        private Stack<IEnumerator<WorkItem>> stack;
        private int gOffset;
        private IEnumerator<WorkItem> eCurrent;
        private Program program;

        public struct WorkItem
        {
            public int GlobalOffset;
            public DataType DataType;
        }

        public ConstantPointerTraversal(IProcessorArchitecture arch, StructureType globalStr, LoadedImage image)
        {
            this.arch = arch; Program p;
            this.globalStr =  globalStr;
            this.image = image;
            this.Discoveries = new List<StructureField>();
        }

        public ConstantPointerTraversal(Program program) 
            : this(program.Architecture, (StructureType) program.Globals.TypeVariable.DataType, program.Image)
        {
        }

        public List<StructureField> Discoveries { get; private set; }

        public void Traverse()
        {
            this.stack = new Stack<IEnumerator<WorkItem>>();
            this.visited = new HashSet<int>();
            stack.Push(Single(new WorkItem { GlobalOffset = 0, DataType = globalStr }).GetEnumerator());
            while (stack.Count > 0)
            {
                this.eCurrent = stack.Pop();
                if (!eCurrent.MoveNext())
                    continue;
                stack.Push(eCurrent);
                this.gOffset = eCurrent.Current.GlobalOffset;
                var children = eCurrent.Current.DataType.Accept(this);
                if (children != null)
                {
                    stack.Push(children.GetEnumerator());
                }
            }
        }

        private IEnumerable<T> Single<T>(T dt)
        {
            yield return dt;
        }

        public IEnumerable<WorkItem> VisitArray(ArrayType at)
        {
            int offset = gOffset;
            Debug.Print("Iterating array at {0:X}", gOffset);
            for (int i = 0; i < at.Length; ++i)
            {
                yield return new WorkItem { GlobalOffset = offset, DataType = at.ElementType };
                offset += at.ElementType.Size;
            }
        }

        public IEnumerable<WorkItem> VisitEnum(EnumType e)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WorkItem> VisitEquivalenceClass(EquivalenceClass eq)
        {
            return eq.DataType.Accept(this);
        }

        public IEnumerable<WorkItem> VisitFunctionType(FunctionType ft)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WorkItem> VisitPrimitive(PrimitiveType pt)
        {
            return null;
        }

        public IEnumerable<WorkItem> VisitMemberPointer(MemberPointer memptr)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WorkItem> VisitPointer(Pointer ptr)
        {
            Debug.Print("Iterating pointer at {0:X}", gOffset);
            var rdr = arch.CreateImageReader(image, (uint) gOffset - image.BaseAddress.Linear);
            if (!rdr.IsValidOffset((uint) ptr.Size))
                return null;
            var c = rdr.Read(PrimitiveType.Create(Domain.Pointer, ptr.Size));    //$REVIEW:Endianess?
            int offset = c.ToInt32();
            Debug.Print("  pointer value: {0:X}", offset);
            if (visited.Contains(offset))
                return null;

            // We've successfully traversed a pointer! The address must therefore be of type ptr.Pointee.
            visited.Add(offset);
            if (globalStr.Fields.AtOffset(offset) == null)
            {
                Discoveries.Add(new StructureField(offset, ptr.Pointee));
            }
            if (image.IsValidLinearAddress((uint) offset))
                return null;

            return Single(new WorkItem { DataType = ptr.Pointee, GlobalOffset = c.ToInt32() });
        }

        public IEnumerable<WorkItem> VisitString(StringType str)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WorkItem> VisitStructure(StructureType str)
        {
            int offset = gOffset;
            Debug.Print("Iterating structure at {0:X}", gOffset);
            foreach (var field in str.Fields)
            {
                int off = offset + field.Offset;
                Debug.Print("   Field {0} at {1:X}", field.Name, off);
                yield return new WorkItem { DataType = field.DataType, GlobalOffset = off };
            }
        }

        public IEnumerable<WorkItem> VisitTypeReference(TypeReference typeref)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WorkItem> VisitTypeVariable(TypeVariable tv)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WorkItem> VisitUnion(UnionType ut)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WorkItem> VisitUnknownType(UnknownType ut)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<WorkItem> VisitVoidType(VoidType voidType)
        {
            throw new NotImplementedException();
        }
    }
}