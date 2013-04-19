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

using Decompiler.Core.Configuration;
using Decompiler.Gui;
using Decompiler.Gui.Controls;
using Decompiler.Gui.Forms;
using NUnit.Framework;
using Rhino.Mocks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;

namespace Decompiler.UnitTests.Gui.Windows.Forms
{
    [TestFixture]
    public class OpenAsInteractorTests
    {
        private MockRepository repository;
        private IOpenAsDialog dlg;
        private ListOption[] archNames;
        private ListOption[] platformNames;
        private IDecompilerConfigurationService dcSvc;
        private IComboBox ddlPlatform;
        private IComboBox ddlArchitecture;
        private ServiceContainer sc;

        [SetUp]
        public void Setup()
        {
            repository = new MockRepository();
            dcSvc = repository.Stub<IDecompilerConfigurationService>();
            sc = new ServiceContainer();
            sc.AddService(typeof(IDecompilerConfigurationService), dcSvc);
        }

        [Test]
        public void OaiLoad()
        {
            Given_Dialog();
            Given_Platforms();
            Given_Architectures();
            Expect_PlatformDataSourceSet();
            Expect_ArchDatasourceSet();
            repository.ReplayAll();

            var interactor = new OpenAsInteractor();
            interactor.Attach(dlg);
            dlg.Raise(x => x.Load += null,
                dlg,
                EventArgs.Empty);

            repository.VerifyAll();
            Assert.AreEqual(2, archNames.Count());
            Assert.AreEqual(2, platformNames.Length);
            Assert.AreEqual("- None -", platformNames[0].Text);
        }

        private void Expect_ArchDatasourceSet()
        {
            ddlArchitecture = repository.StrictMock<IComboBox>();
            ddlArchitecture.Expect(d => d.DataSource = null)
                .IgnoreArguments()
                .WhenCalled(m => { this.archNames = ((IEnumerable)m.Arguments[0]).OfType<ListOption>().ToArray(); });
            dlg.Expect(d => d.Architectures).Return(ddlArchitecture);
        }

        private void Expect_PlatformDataSourceSet()
        {
            ddlPlatform = repository.StrictMock<IComboBox>();
            ddlPlatform.Expect(d => d.DataSource = null)
                .IgnoreArguments()
                .WhenCalled(m => { this.platformNames = ((IEnumerable)m.Arguments[0]).OfType<ListOption>().ToArray(); });
            dlg.Expect(d => d.Platforms).Return(ddlPlatform);
        }

        private void Given_Dialog()
        {
            dlg = repository.DynamicMock<IOpenAsDialog>();
            dlg.Stub(d => d.Services).Return(sc);
        }

        private void Given_Architectures()
        {
            var arch1 = repository.Stub<Architecture>();
            arch1.Stub(a => a.Name).Return("ARCH1");
            arch1.Stub(a => a.Description).Return("Arch 1");

            var arch2 = repository.Stub<Architecture>();
            arch2.Stub(a => a.Name).Return("ARCH2");
            arch2.Stub(a => a.Description).Return("Arch 2");

            dcSvc.Stub(d => d.GetArchitectures()).Return(new List<Architecture> { arch1, arch2 });
        }

        private void Given_Platforms()
        {
            var env1 = repository.Stub<OperatingEnvironment>();
            env1.Stub(e  => e.Name).Return("TECH");
            env1.Stub(e => e.Description).Return("Friendly");

            dcSvc.Stub(d => d.GetEnvironments()).Return(new List<OperatingEnvironment> { env1 });
        }


        [Test]
        public void Oai_OkPressed_ReturnSelectedThings()
        {
            Given_Dialog();
            Given_Platforms();
            Given_Architectures();
            Expect_PlatformDataSourceSet();
            Expect_ArchDatasourceSet();
            repository.ReplayAll();

            var interactor = new OpenAsInteractor();
            interactor.Attach(dlg);
            dlg.Raise(x => x.Load += null,
                dlg,
                EventArgs.Empty);

            repository.VerifyAll();
            Assert.AreEqual(2, archNames.Count());
            Assert.AreEqual(2, platformNames.Length);
            Assert.AreEqual("- None -", platformNames[0].Text);
        }
    }
}