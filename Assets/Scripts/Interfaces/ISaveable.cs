using UnityEngine;

public interface ISaveable
{
    void Save(int slotIndex);
    void Load(int slotIndex);
}
