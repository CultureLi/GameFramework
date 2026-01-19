using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;


public class BuildTools2
{
    public static void Test()
    {
        try
        {
            Debug.Log("BuildTools2 START");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
        EditorApplication.Exit(0);

    }
}

