using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArchivePoint : MonoBehaviour,ISceneInteraction
{
    public void Interact()
    {
        UIManager.instance.ShowSaveUI();
        SaveManager.instance.GetRespawnPosition(transform.position);
    }
}
