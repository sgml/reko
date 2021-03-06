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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace Reko.Core.Services
{
    /// <summary>
    /// Abstracts away file system accesses.
    /// </summary>
    public interface IFileSystemService
    {
        Stream CreateFileStream(string filename, FileMode mode);
        Stream CreateFileStream(string filename, FileMode mode, FileAccess access);
        Stream CreateFileStream(string filename, FileMode mode, FileAccess access, FileShare share);
        XmlWriter CreateXmlWriter(string filename);
        bool FileExists(string filePath);
        string MakeRelativePath(string fromPath, string toPath);
        byte[] ReadAllBytes(string filePath);
    }

    public class FileSystemServiceImpl : IFileSystemService
    {
        private char sepChar;

        public FileSystemServiceImpl()
        {
            this.sepChar = Path.DirectorySeparatorChar;
        }

        public FileSystemServiceImpl(char sepChar)
        {
            this.sepChar = sepChar;
        }

        public Stream CreateFileStream(string filename, FileMode mode)
        {
            return new FileStream(filename, mode);
        }

        public Stream CreateFileStream(string filename, FileMode mode, FileAccess access)
        {
            return new FileStream(filename, mode, access);
        }

        public Stream CreateFileStream(string filename, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(filename, mode, access, share);
        }

        public XmlWriter CreateXmlWriter(string filename)
        {
            return new XmlTextWriter(filename, new UTF8Encoding(false))
            {
                Formatting = Formatting.Indented
            };
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public string MakeRelativePath(string fromPath, string toPath)
        {
            int iLastDir = -1;
            int i;
            for (i = 0; i < fromPath.Length && i < toPath.Length; ++i)
            {
                if (fromPath[i] != toPath[i])
                    break;
                if (fromPath[i] == this.sepChar)
                    iLastDir = i + 1;
            }
            var sb = new StringBuilder();
            if (iLastDir <= 1)
                return toPath;
            for (i = iLastDir; i < fromPath.Length; ++i)
            {
                if (fromPath[i] == this.sepChar)
                {
                    sb.Append("..");
                    sb.Append(sepChar);
                }
            }
            sb.Append(toPath.Substring(iLastDir));
            return sb.ToString();
        }

        public byte[] ReadAllBytes(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }
        
    }
}
