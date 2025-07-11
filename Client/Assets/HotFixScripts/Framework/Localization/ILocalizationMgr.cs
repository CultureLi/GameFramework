using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    public interface ILocalizationMgr
    {
        string Get(string key);
        string Format(string key, object arg0);
        string Format(string key, object arg0, object arg1);
        string Format(string key, object arg0, object arg1, object arg2);
        string Format(string key, params object[] args);
        void InitLanguage();
        bool SetLanguage(string isoCode);
    }
}
