using UnityEngine;
using System.Collections.Generic;

public interface IEditorInstantiatedObject
{
    List<EditorInstantiatedObjectInfo> InstantiateEditorObjects();
}

[System.Serializable]
public class EditorInstantiatedObjectInfo
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    public string prefabPath;
}
