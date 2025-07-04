﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public interface IConfigMgr
    {
        T GetTable<T>() where T : TableBase, new();
        T GetTable<T>(string fileName) where T : TableBase, new();
    }
}
