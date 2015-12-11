﻿#region License
/* 
 * Copyright (C) 1999-2015 John Källén.
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

using NUnit.Framework;
using Reko.Analysis;
using Reko.Core;
using Reko.UnitTests.Mocks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Reko.UnitTests.Analysis
{
    [TestFixture]
    public class RegisterPreservationTests
    {
        private DataFlow2 dataFlow;

        private void AssertProgram(string sExp, ProgramBuilder pb)
        {
            var sw = new StringWriter();
            pb.Program.Procedures.Values.First().Write(false, sw);
            sw.WriteLine();
            foreach (var de in dataFlow.ProcedureFlows.OrderBy(e => e.Key.Name))
            {
                sw.WriteLine("{0}:", de.Key.Name);
                DumpSet("Preserved: ", de.Value.Preserved, sw);
                DumpSet("Trashed:   ", de.Value.Trashed, sw);
            }

            var sActual = sw.ToString();
            if (sExp != sActual)
                Debug.WriteLine(sActual);
            Assert.AreEqual(sExp, sw.ToString());
        }

        private void DumpSet<T>(string caption, ISet<T> items, StringWriter sw)
        {
            sw.Write("    {0}", caption);
            sw.Write(string.Join(" ", items.Select(e => e.ToString()).OrderBy(e => e)));
        }

        private void DumpMap<K, V>(string caption, IDictionary<K, V> items, StringWriter sw)
        {
            sw.Write("    {0}", caption);
            sw.Write(string.Join(" ", items.Select(e => string.Format("{0}:{1}", e.Key, e.Value)).OrderBy(e => e)));
        }

        public void RunTest(ProgramBuilder pb)
        {
            var program = pb.BuildProgram();
            var scc = new Dictionary<Procedure, SsaState>();
            foreach (var proc in program.Procedures.Values)
            {
                var flow = new ProgramDataFlow(program);
                Aliases alias = new Aliases(proc, program.Architecture, flow);
                alias.Transform();

                // Transform the procedure to SSA state. When encountering 'call' instructions,
                // they can be to functions already visited. If so, they have a "ProcedureFlow" 
                // associated with them. If they have not been visited, or are computed destinations
                // (e.g. vtables) they will have no "ProcedureFlow" associated with them yet, in
                // which case the the SSA treats the call as a "hell node".
                var doms = proc.CreateBlockDominatorGraph();
                var sst = new SsaTransform(flow, proc, doms);
                var ssa = sst.SsaState;

                scc.Add(proc, ssa);
            }

            this.dataFlow = new DataFlow2();
            var regp = new RegisterPreservation(scc, dataFlow);
            regp.Compute();
        }

        [Test]
        public void Regp_Simple()
        {
            var pb = new ProgramBuilder(new FakeArchitecture());
            pb.Add("test", m =>
            {
                var r1 = m.Register(1);
                m.Assign(r1, 3);
                m.Return();
            });
            RunTest(pb);

            var sExp =
            #region Expected
@"// test
// Return size: 0
void test()
test_entry:
	// succ:  l1
l1:
	r1_0 = 0x00000003
	return
	// succ: test_exit
test_exit:

test:
    Preserved: @@@@
    Trashed:   @@@
";
            #endregion
            AssertProgram(sExp, pb);
        }
    }
}