using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework
{
    internal class LocalizationMgr : ILocalizationMgr, IFramework
    {
        IConfigMgr _cfgMgr;

        private string _language = "en";
        private string _tbName;

        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                _tbName = $"tbi18n_{value}";
            }
        }

        public LocalizationMgr()
        {
            _cfgMgr = FrameworkMgr.GetModule<IConfigMgr>();
        }

        public string Get(string key)
        {
            var table = _cfgMgr.GetTable<TbI18n>(_tbName);
            return table.Get(key).Value;
        }


        public void Shutdown()
        {
            
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            
        }
    }
}
