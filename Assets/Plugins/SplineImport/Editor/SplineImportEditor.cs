using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineImport))]
public class SplineImportEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SplineImport p = target as SplineImport;
        if( GUILayout.Button("Import") )
        {
            string assetPath = EditorUtility.OpenFilePanel("Import", Application.dataPath, "dae");
            if( assetPath.Length > 5 )
            {
                if( p.readDAE( assetPath ) )
                {
                    Debug.Log("Import succeed!");
                }
                else
                {
                    Debug.Log("Import failed!");
                }
            }
        }

        if (p.dataList == null) return;
        if (p.dataList.Length == 0) return;

        //show spline object list
        var items = new string[p.dataList.Length];
        for (int i = 0; i < items.Length; i++) items[i] = p.dataList[i].name;
        int cid = p._selectId;
        p._selectId = EditorGUILayout.Popup("Select Spline", p._selectId, items);
        if (p._selectId != cid) p.CreateLine();

        //unit scale setting
        float scale = p._unitScale;
        p._unitScale = EditorGUILayout.FloatField("Scale",p._unitScale);
        if (p._unitScale < 0.0001f) p._unitScale = 0.0001f;
        if (!Mathf.Approximately(p._unitScale, scale)) p.CreateLine();
    }
}

