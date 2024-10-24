using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldChunkCreator))]
public class WorladCreatorChunkEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldChunkCreator creator = (WorldChunkCreator)target;
        if (GUILayout.Button("创建区块"))
        {
            creator.CreateChunks();
        }
    }
    
    
}
