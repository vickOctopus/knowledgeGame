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

        if (GUILayout.Button("Add Top Collider"))
        {
            ladder.AddLadderColliders();
        }
    }
}
