using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public interface ILocalizationMgr
    {
        public string Language
        {
            get;set;
        }
        string Get(string key);
    }
}
