#region License
/* 
 * Copyright (C) 1999-2014 John K�ll�n.
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

using Decompiler.Arch.X86;
using Decompiler.Core;
using Decompiler.Core.Serialization;
using Decompiler.Core.Services;
using Decompiler.Gui;
using Decompiler.Gui.Forms;
using Decompiler.Loading;
using Decompiler.UnitTests.Mocks;
using Decompiler.Gui.Windows;
using Decompiler.Gui.Windows.Forms;
using NUnit.Framework;
using Rhino.Mocks;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;

namespace Decompiler.UnitTests.Gui.Windows.Forms
{
    [TestFixture]
    public class LoadedPageInteractorTests
    {
        private IMainForm form;
        private Program prog;
        private LoadedPageInteractor interactor;
        private IDecompilerService decSvc;
        private ServiceContainer sc;
        private MockRepository repository;
        private ImageMapSegment mapSegment1;
        private ImageMapSegment mapSegment2;
        private IDecompilerShellUiService uiSvc;
        private ILowLevelViewService memSvc;

        [SetUp]
        public void Setup()
        {
            repository = new MockRepository();

            form = new MainForm();

            prog = new Program();
            prog.Architecture = new IntelArchitecture(ProcessorMode.Real);
            prog.Image = new LoadedImage(new Address(0xC00, 0), new byte[10000]);
            prog.ImageMap = prog.Image.CreateImageMap();

            prog.ImageMap.AddSegment(new Address(0x0C10, 0), "0C10", AccessMode.ReadWrite);
            prog.ImageMap.AddSegment(new Address(0x0C20, 0), "0C20", AccessMode.ReadWrite);
            mapSegment1 = prog.ImageMap.Segments.Values[0];
            mapSegment2 = prog.ImageMap.Segments.Values[1];

            sc = new ServiceContainer();
            decSvc = new DecompilerService();

            sc.AddService(typeof(IDecompilerService), decSvc);
            sc.AddService(typeof(IWorkerDialogService), new FakeWorkerDialogService());
            sc.AddService(typeof(DecompilerEventListener), new FakeDecompilerEventListener());
            sc.AddService(typeof(IStatusBarService), new FakeStatusBarService());
            uiSvc = AddService<IDecompilerShellUiService>();
            memSvc = AddService<ILowLevelViewService>();

            TestLoader ldr = new TestLoader(sc, new Project_v1(), prog);
            decSvc.Decompiler = new DecompilerDriver(ldr, new FakeDecompilerHost(), sc);
            decSvc.Decompiler.Load("test.exe");

            interactor = new LoadedPageInteractor(sc);
        }

        [TearDown]
        public void Teardown()
        {
            form.Dispose();
        }

        [Test]
        [Ignore]
        public void LpiPopulate()
        {
            var disSvc = AddService<IDisassemblyViewService>();

            interactor.EnterPage();
            //ListView lv = form.BrowserList;
            //Assert.AreEqual(3, lv.Items.Count, "There should be three segments in the image.");
        }

        private T AddService<T>() where T : class
        {
            var oldSvc = sc.GetService(typeof(T));
            if (oldSvc != null)
                sc.RemoveService(typeof(T));
            var svc = repository.DynamicMock<T>();
            sc.AddService(typeof(T), svc);
            return svc;
        }

        [Test]
        [Ignore]
        public void LpiPopulateBrowserWithScannedProcedures()
        {
            // Instead write expectations for the two added items.

            AddProcedure(new Address(0xC20, 0x0000), "Test1");
            AddProcedure(new Address(0xC20, 0x0002), "Test2");
            interactor.EnterPage();
            //Assert.AreEqual(3, form.BrowserList.Items.Count);
            //Assert.AreEqual("0C20", form.BrowserList.Items[2].Text);
        }

        private void AddProcedure(Address addr, string procName)
        {
            prog.Procedures.Add(addr,
                new Procedure(procName, prog.Architecture.CreateFrame()));
        }

        [Test]
        [Ignore("Move this to low-level? Or Decompiler?")]
        public void LpiMarkingProceduresShouldAddToUserProceduresList()
        {
            var disSvc = AddService<IDisassemblyViewService>();
            Assert.AreEqual(0, ((InputFile) decSvc.Decompiler.Project.InputFiles[0]).UserProcedures.Count);
            var addr = new Address(0x0C20, 0);
            memSvc.Expect(s => s.GetSelectedAddressRange()).Return(new AddressRange(addr, addr));
            memSvc.Expect(s => s.InvalidateWindow()).IgnoreArguments();
            repository.ReplayAll();

            //interactor.MarkAndScanProcedure(prog);

            repository.VerifyAll();
            //$REVIEW: Need to pass InputFile into the SelectedProcedureEntry piece.
            var inputFile = (InputFile)decSvc.Decompiler.Project.InputFiles[0];
            Assert.AreEqual(1, inputFile.UserProcedures.Count);
            Procedure_v1 uproc = (Procedure_v1)inputFile.UserProcedures.Values[0];
            Assert.AreEqual("0C20:0000", uproc.Address);
        }

        [Test]
        public void Lpi_QueryStatus()
        {
            Assert.AreEqual(MenuStatus.Enabled | MenuStatus.Visible, QueryStatus(CmdIds.ViewFindFragments));
        }


        [Test]
        public void Lpi_SetBrowserCaptionWhenEnteringPage()
        {
            AddService<IDecompilerShellUiService>();
            AddService<ILowLevelViewService>();
            AddService<IDisassemblyViewService>();
            AddService<IProjectBrowserService>();
            repository.ReplayAll();

            interactor.EnterPage();

            repository.VerifyAll();
        }

        [Test]
        public void Lpi_CallScanProgramWhenenteringPage()
        {
            var decSvc = AddService<IDecompilerService>();
            var decompiler = repository.Stub<IDecompiler>();
            var prog = new Program();
            prog.Image = new LoadedImage(new Address(0x3000), new byte[10]);
            prog.ImageMap = prog.Image.CreateImageMap();
            decompiler.Stub(x => x.Programs).Return(new [] {prog});
            decSvc.Stub(x => x.Decompiler).Return(decompiler);
            AddService<IDecompilerShellUiService>();
            AddService<ILowLevelViewService>();
            AddService<IDisassemblyViewService>();
            AddService<IProjectBrowserService>();
            repository.ReplayAll();

            Assert.IsNotNull(sc);
            interactor.EnterPage();

            repository.VerifyAll();
        }

        private MenuStatus QueryStatus(int cmdId)
        {
            CommandStatus status = new CommandStatus();
            interactor.QueryStatus(new CommandID(CmdSets.GuidDecompiler, cmdId), status, null);
            return status.Status;
        }

        //$TODO: use MockRepository for this.
        private class TestLoader : ILoader
        {
            private Project_v1 project;
            private Program prog;

            public TestLoader(IServiceProvider services, Project_v1 project, Program prog) 
            {
                this.project = project;
                this.prog = prog;
            }

            public byte[] LoadImageBytes(string fileName, int offset)
            {
                return new byte[400];
            }

            public Program LoadExecutable(InputFile file)
            {
                return prog;
            }

            public Program LoadExecutable(string fileName, byte[] bytes, Address loadAddress)
            {
                return prog;
            }

            public Program AssembleExecutable(string fileName, Decompiler.Core.Assemblers.Assembler asm, Address loadAddress)
            {
                throw new NotImplementedException();
            }

            public Program AssembleExecutable(string fileName, byte[] bytes, Decompiler.Core.Assemblers.Assembler asm, Address loadAddress)
            {
                throw new NotImplementedException();
            }

            public TypeLibrary LoadMetadata(string fileName)
            {
                throw new NotImplementedException();
            }
        }
    }
}
