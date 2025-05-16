using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    internal sealed partial class UIManager
    {
        private class UICreateInfo : IReference
        {
            public string Name => _name;
            public ViewData Data => _data;

            private string _name;
            private ViewData _data;

            public static UICreateInfo Create(string name, ViewData data)
            {
                var info = ReferencePool.Acquire<UICreateInfo>();
                info._name = name;
                info._data = data;
                return info;
            }

            public void Clear()
            {
                _name = null;
                _data = null;
            }
        }
    }
}
