﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace p4codechurn.core
{
    public interface IStreamFactory
    {
        Stream createFileStream(string fileName, FileMode fileMode, FileAccess fileAccess);
        Stream readFile(string fileName);
    }
}
