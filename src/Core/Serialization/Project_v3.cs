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

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Reko.Core.Serialization
{
    /// <summary>
    /// Seralization format for decompiler projects.
    /// </summary>
    /// <remarks>
    /// Note that you may safely *add* attributes and elements to the serialization
    /// format. However, should you *rename* or delete XML nodes, you must copy the serialization
    /// file format into a new file, bump the namespace identifier and the class name. You will
    /// also have to modify the ProjectSerializer to handle the new format.</remarks>
    [XmlRoot(ElementName = "project", Namespace = "http://schemata.jklnet.org/Reko/v3")]
    public class Project_v3
    {
        public const string FileExtension = ".dcproject";

        public Project_v3()
        {
            this.Inputs = new List<ProjectFile_v3>();
        }

        [XmlElement("input", typeof(DecompilerInput_v3))]
        [XmlElement("metadata",typeof(MetadataFile_v3))]
        [XmlElement("asm", typeof(AssemblerFile_v3))]
        public List<ProjectFile_v3> Inputs;
    }

    public abstract class ProjectFile_v3
    {
        [XmlElement("filename")]
        public string Filename;

        public abstract T Accept<T>(IProjectFileVisitor_v3<T> visitor);
    }

    public interface IProjectFileVisitor_v3<T>
    {
        T VisitInputFile(DecompilerInput_v3 input);
        T VisitMetadataFile(MetadataFile_v3 metadata);
        T VisitAssemblerFile(AssemblerFile_v3 asm);
    }

    public class UserData_v3
    {
        public UserData_v3()
        {
            Procedures = new List<Procedure_v1>();
            GlobalData = new List<GlobalDataItem_v2>();
            Heuristics = new List<Heuristic_v3>();
        }

        [XmlElement("onLoad")]
        public Script_v2 OnLoadedScript;

        [XmlElement("platform")]
        public PlatformOptions_v3 PlatformOptions;

        [XmlElement("procedure")]
        public List<Procedure_v1> Procedures;

        [XmlElement("call")]
        public List<SerializedCall_v1> Calls;

        [XmlElement("global")]
        public List<GlobalDataItem_v2> GlobalData;

        [XmlElement("heuristic")]
        public List<Heuristic_v3> Heuristics;
    }

    public class Heuristic_v3
    {
        [XmlAttribute("name")]
        public string Name;
    }

    public class DecompilerInput_v3 : ProjectFile_v3
    {
        public DecompilerInput_v3()
        {
            User = new UserData_v3();
        }

        [XmlElement("address")]
        public string Address;

        [XmlElement("comment")]
        public string Comment;

        [XmlElement("processor")]
        public string Processor;

        [XmlElement("disassembly")]
        public string DisassemblyFilename;

        [XmlElement("intermediate-code")]
        public string IntermediateFilename;

        [XmlElement("output")]
        public string OutputFilename;

        [XmlElement("types-file")]
        public string TypesFilename;

        [XmlElement("global-vars")]
        public string GlobalsFilename;

        [XmlElement("user")]
        public UserData_v3 User;
        public override T Accept<T>(IProjectFileVisitor_v3<T> visitor)
        {
            return visitor.VisitInputFile(this);
        }
    }

    public class MetadataFile_v3 : ProjectFile_v3
    {
        [XmlElement("loader")]
        public string LoaderTypeName;

        [XmlElement("module")]
        public string ModuleName;

        public override T Accept<T>(IProjectFileVisitor_v3<T> visitor)
        {
            return visitor.VisitMetadataFile(this);
        }
    }

    public class AssemblerFile_v3 : ProjectFile_v3
    {
        [XmlElement("assembler")]
        public string Assembler;

        public override T Accept<T>(IProjectFileVisitor_v3<T> visitor)
        {
            return visitor.VisitAssemblerFile(this);
        }
    }

    public class PlatformOptions_v3
    {
        [XmlAnyElement]
        public XmlElement[] Options;
    }
}
