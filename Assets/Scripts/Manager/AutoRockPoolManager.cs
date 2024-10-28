using UnityEngine;

public class AutoRockPoolManager : RockPoolManager
{
    protected override void Start()
    {
        base.Start(); // 调用父类的Start方法以初始化对象池
        StartSpawning(); // 在开始时自动开始生成
    }

    private void OnDisable()
    {
        StopSpawning(); // 当对象被禁用时停止生成
    }
}
