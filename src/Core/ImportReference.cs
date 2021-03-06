﻿#region License
/* 
 * Copyright (C) 1999-2017 John Källén.
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

using Reko.Core.Expressions;
using Reko.Core.Serialization;
using Reko.Core.Services;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.Core
{
    /// <summary>
    /// Represents a reference to an external symbol from another module.
    /// </summary>
    public abstract class ImportReference : IComparable<ImportReference>
    {
        public Address ReferenceAddress;
        public string ModuleName;

        public ImportReference(Address addr, string moduleName)
        {
            this.ReferenceAddress = addr;
            this.ModuleName = moduleName;
        }

        public abstract Identifier ResolveImportedGlobal(IImportResolver importResolver, IPlatform platform, AddressContext ctx);
        public abstract ExternalProcedure ResolveImportedProcedure(IImportResolver importResolver, IPlatform platform, AddressContext ctx);

        public abstract int CompareTo(ImportReference that);
    }

    public class NamedImportReference : ImportReference
    {
        public string ImportName;

        public NamedImportReference(Address addr, string moduleName, string importName)
            : base(addr, moduleName)
        {
            this.ImportName = importName;
        }

        public override int CompareTo(ImportReference that)
        {
            if (this.GetType() != that.GetType())
            {
                return this.GetType().FullName.CompareTo(this.GetType().FullName);
            }
            int cmp = this.ModuleName.CompareTo(that.ModuleName);
            if (cmp != 0)
                return cmp;
            cmp = string.Compare(
                this.ImportName,
                ((NamedImportReference)that).ImportName,
                StringComparison.InvariantCulture);
            return cmp;
        }

        public override Identifier ResolveImportedGlobal(
            IImportResolver resolver,
            IPlatform platform,
            AddressContext ctx)
        {
            var global = resolver.ResolveGlobal(ModuleName, ImportName, platform);
            if (global != null)
                return global;
            var t = platform.DataTypeFromImportName(ImportName);
            //$REVIEW: the way imported symbols are resolved as 
            // globals or functions needs a revisit.
            if (t != null && !(t.Item2 is FunctionType))
                return new Identifier(t.Item1, t.Item2, new MemoryStorage());
            else
                return null;
        }

        public override ExternalProcedure ResolveImportedProcedure(
            IImportResolver resolver, 
            IPlatform platform, 
            AddressContext ctx)
        {
            var ep = resolver.ResolveProcedure(ModuleName, ImportName, platform);
            if (ep != null)
                return ep;
            // Can we guess at the signature?
            ep = platform.SignatureFromName(ImportName);
            if (ep != null)
                return ep;
            
            ctx.Warn("Unable to resolve imported reference {0}.", this);
            return new ExternalProcedure(this.ToString(), null);
        }

        public override string ToString()
        {
            return string.Format(
                ModuleName != null ? "{0}!{1}" : "{1}",
                ModuleName, 
                ImportName);
        }
    }

    /// <summary>
    /// Windows likes to use imports by ordinal number, especially Win16.
    /// </summary>
    public class OrdinalImportReference : ImportReference
    {
        public int Ordinal;

        public OrdinalImportReference(Address addr, string moduleName, int ordinal)
            : base(addr, moduleName)
        {
            this.Ordinal = ordinal;
        }

        public override int CompareTo(ImportReference that)
        {
            if (this.GetType() != that.GetType())
            {
                return this.GetType().FullName.CompareTo(this.GetType().FullName);
            }
            int cmp = this.ModuleName.CompareTo(that.ModuleName);
            if (cmp != 0)
                return cmp;
            cmp = this.Ordinal.CompareTo(((OrdinalImportReference)that).Ordinal);
            return cmp;
        }

        public override Identifier ResolveImportedGlobal(IImportResolver importResolver, IPlatform platform, AddressContext ctx)
        {
            ctx.Warn("Ordinal global imports not supported. Please report this message to the Reko maintainers (https://github.com/uxmal/reko).");
            var id = importResolver.ResolveGlobal(ModuleName, Ordinal, platform);
            return null;
        }

        public override ExternalProcedure ResolveImportedProcedure(IImportResolver resolver, IPlatform platform, AddressContext ctx)
        {
            var ep = resolver.ResolveProcedure(ModuleName, Ordinal, platform);
            if (ep != null)
                return ep;
            ctx.Warn("Unable to resolve imported reference {0}.", this);
            return new ExternalProcedure(this.ToString(), null);
        }

        public override string ToString()
        {
            return string.Format("{0}!Ordinal_{1}", ModuleName, Ordinal);
        }
    }
}
