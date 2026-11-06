using Framework;
using GameMain;
using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISetting : ViewBase
{
    public TMP_InputField speedInput;

    public TMP_InputField gateInput;

    void Awake()
    {

    }

    public override void OnShow(bool isInitShow, ViewData data)
    {
        base.OnShow(isInitShow, data);

    }


    public void OnBtnSpeedOk()
    {
        if (speedInput == null)
        {
            Debug.LogWarning("[Setting] speedInput 未绑定");
            return;
        }

        if (!float.TryParse(speedInput.text.Trim(), NumberStyles.Float,
                CultureInfo.InvariantCulture, out var speed))
        {
            Debug.LogWarning($"[Setting] 速度格式错误: {speedInput.text}");
            return;
        }

        speed = Mathf.Clamp(speed, 1f, 20f);
        if (PathfindingMgr.I == null)
        {
            Debug.LogWarning("[Setting] PathfindingMgr 未初始化");
            return;
        }

        PathfindingMgr.I.RequestMoveSpeed(speed);
        Debug.Log($"[Setting] 请求移动速度 {speed:0.##}");
    }

    public void OnBtnGateOk()
    {
        if (gateInput == null)
        {
            Debug.LogWarning("[Setting] gateInput 未绑定");
            return;
        }

        // 支持两种输入：
        //   1) 只填关卡 ID：按服务端当前状态切换打开/关闭
        //   2) "ID,open" / "ID,close" / "ID,1" / "ID,0"：显式设置
        string raw = gateInput.text.Trim();
        string[] parts = raw.Split(new[] { ',', ' ', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !int.TryParse(parts[0], out int gateId) || gateId <= 0)
        {
            Debug.LogWarning($"[Setting] 关卡 ID 格式错误: {raw}");
            return;
        }

        bool? open = null;
        if (parts.Length >= 2)
        {
            string state = parts[1].ToLowerInvariant();
            if (state == "1" || state == "open" || state == "true" || state == "打开" || state == "开")
                open = true;
            else if (state == "0" || state == "close" || state == "false" || state == "关闭" || state == "关")
                open = false;
            else
            {
                Debug.LogWarning($"[Setting] 关卡状态格式错误: {parts[1]}");
                return;
            }
        }

        if (PathfindingMgr.I == null)
        {
            Debug.LogWarning("[Setting] PathfindingMgr 未初始化");
            return;
        }

        if (open.HasValue)
        {
            PathfindingMgr.I.RequestGateState(gateId, open.Value);
            Debug.Log($"[Setting] 请求关卡 {gateId} {(open.Value ? "打开" : "关闭")}");
        }
        else
        {
            PathfindingMgr.I.RequestGateToggle(gateId);
            Debug.Log($"[Setting] 请求切换关卡 {gateId}");
        }
    }

}