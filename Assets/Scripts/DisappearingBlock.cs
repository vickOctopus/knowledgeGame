using UnityEngine;

public class DisappearingBlock : MonoBehaviour, IDestroyable
{
    private BlockPoolManager poolManager;

    public void Initialize(BlockPoolManager manager)
    {
        poolManager = manager;
        // gameObject.tag = "DisappearingBlock";
    }

    public void Destroy()
    {
        if (poolManager != null)
        {
            poolManager.ReturnBlockToPool(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
