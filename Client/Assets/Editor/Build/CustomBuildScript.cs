using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;


[CreateAssetMenu(fileName = "CustomBuildScript.asset", menuName = "Addressables/Content Builders/Custom Build Script")]
public class CustomBuildScript : BuildScriptPackedMode
{
    public override string Name => "Custom Build Script";

    protected override TResult DoBuild<TResult>(AddressablesDataBuilderInput builderInput, AddressableAssetsBuildContext aaContext)
    {
        // 1. ִ��ԭʼ��������
        var result = base.DoBuild<TResult>(builderInput, aaContext);


        // 2. ������������Լ����߼������ڹ����ɹ���
        if (result != null && result.Error == null)
        {
            PostProcess();
        }

        return result;
    }

    private void PostProcess()
    {
        //����catalog.hash�ļ�
        var originCatalogPath = Path.Combine(Addressables.RuntimePath, "catalog.json");
        var originCatalogHashPath = originCatalogPath.Replace(".json", ".hash");

        var jsonText = File.ReadAllText(originCatalogPath);
        var hashCode = HashingMethods.Calculate(jsonText).ToString();

        File.WriteAllText(originCatalogHashPath, hashCode);
    }
}

