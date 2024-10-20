using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Ladder))]
public class LadderTopColliderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        Ladder ladder = (Ladder)target;

        if (GUILayout.Button("生成梯子顶部碰撞器"))
        {
            var instantiatedObjects = ladder.InstantiateEditorObjects();
            if (instantiatedObjects.Count > 0)
            {
                Debug.Log($"已生成 {instantiatedObjects.Count} 个梯子顶部碰撞器");
            }
            else
            {
                Debug.Log("没有生成梯子顶部碰撞器");
            }
        }
    }
}
