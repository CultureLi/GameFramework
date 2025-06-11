using Framework;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameEntry.Stage
{
    public class EntranceStartStage : FsmState
    {
        protected override void OnEnter()
        {
            ChangeState<DownloadHotfixDllStage>();
        }
    }
}
