﻿#region License
/* 
 * Copyright (C) 1999-2013 John Källén.
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

using Decompiler.Scanning.Dfa;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Decompiler.UnitTests.Scanning.Dfa
{
    [TestFixture]
    public class DfAutomatonTests
    {
        [Test]
        public void Dfa_SingleState()
        {
            var automaton = Given_SingleStateAutomaton();

            Assert.AreEqual(
                new int[] { 1 },
                automaton.GetMatches(new byte[] {0, 2}, 0).ToArray());
        }

        [Test]
        public void Dfa_SingleState_NoMatch()
        {
            var automaton = Given_SingleStateAutomaton();

            Assert.AreEqual(
                new int[] { },
                automaton.GetMatches(new byte[] { 0, 3 }, 0).ToArray());
        }

        [Test]
        public void Dfa_SingleState_ThreeMatches()
        {
            var automaton = Given_SingleStateAutomaton();

            Assert.AreEqual(
                new int[] { 0, 1, 2 },
                automaton.GetMatches(new byte[] { 2, 2, 2 }, 0).ToArray());
        }

        [Test]
        public void Dfa_Longmatch()
        {
            var automaton = Given_LongMatchAutomaton();
            Assert.AreEqual(
                new int[] { 3 },
                automaton.GetMatches(new byte[] { 3, 3, 3, 2, 2, 0, 1, 3 }, 0).ToArray());
        }

        private static Automaton Given_LongMatchAutomaton()
        {
            var automaton = new Automaton(
                new State[] {
                    new State { Number = 0, Starts = false, Terminates = false },
                    new State { Number = 1, Starts = false, Terminates = false },
                    new State { Number = 2, Starts = true, Terminates = false },
                    new State { Number = 3, Starts = false, Terminates = true},
                },
                new int[,] {
                    { -1, -1, 0, -1 },
                    {  2, -1, 1, -1 },
                    { -1,  3,  1, -1 },
                    { -1, -1, -1, -1 },
                });
            return automaton;
        }

        private static Automaton Given_SingleStateAutomaton()
        {
            var automaton = new Automaton(
                new State[] { 
                    new State { Number = 0, Starts = true, Terminates= false },
                    new State { Number = 1, Starts = false, Terminates= true},
                },
                new int[,] {
                    { -1, -1, 1, -1 },
                    { -1, -1, -1, -1 }
                });
            return automaton;
        }
    }
}