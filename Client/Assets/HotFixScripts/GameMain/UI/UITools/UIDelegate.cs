using Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMain.UI
{
    public class UIDelegate : Singleton<UIDelegate>
    {
        public UICommonTips CommonTipsCtrl
        {
            get;set;
        }
        public void ShowCommonTips(string tip)
        {
            if (CommonTipsCtrl)
            {
                var data = ReferencePool.Acquire<UICommonTipsData>();
                data.content = tip;
                CommonTipsCtrl.ShowTips(data);
            }
        }
    }
}
