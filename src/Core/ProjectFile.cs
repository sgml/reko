﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Decompiler.Core
{
    /// <summary>
    /// Abstract class represeting all files that are in use by the project
    /// </summary>
    public abstract class ProjectFile
    {
        /// <summary>
        /// The file name of the file to be decompiled.
        /// </summary>
        public string Filename { get; set; }
    }
}