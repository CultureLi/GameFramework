﻿using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public interface IConfigMgr
    {
        void Initialize();
        T GetTable<T>(string fileName=null) where T : TableBase, new();
    }
}
