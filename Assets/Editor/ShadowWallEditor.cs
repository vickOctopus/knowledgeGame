using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShadowWall))] // 指定自定义编辑器用于 ShadowWall 脚本
public class ShadowWallEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 绘制默认的 Inspector
        DrawDefaultInspector();
    
        // 获取当前选中的 ShadowWall 组件实例
        ShadowWall shadowWall = (ShadowWall)target;
    
        // 创建一个按钮，点击时调用 RestoreHiddenTiles 方法
        if (GUILayout.Button("Restore Hidden Tiles"))
        {
            shadowWall.RestoreHiddenTiles(); // 调用还原方法
        }
    }
}