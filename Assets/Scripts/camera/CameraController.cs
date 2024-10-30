using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
   public static CameraController Instance;
   public ChunkManager chunkManager;

   private void Awake()
   {
      if (Instance == null)
      {
         Instance = this;
         DontDestroyOnLoad(gameObject);
      }
      else
      {
         Destroy(gameObject);
         return;
      }

      FindChunkManager();
   }

   private void Start()
   {
      if (chunkManager == null)
      {
         FindChunkManager();
      }
   }

   private void FindChunkManager()
   {
      chunkManager = FindObjectOfType<ChunkManager>();
   }

   public void CameraStartResetPosition(Vector3 playerPosition)
   {
      // 计算玩家所在的区块坐标
      int chunkX = Mathf.FloorToInt((playerPosition.x + ChunkManager.chunkWidth / 2) / ChunkManager.chunkWidth);
      int chunkY = Mathf.FloorToInt((playerPosition.y + ChunkManager.chunkHeight / 2) / ChunkManager.chunkHeight);

      // 计算相机应该在的位置（区块中心）
      Vector3 newPosition = new Vector3(
         chunkX * ChunkManager.chunkWidth,
         chunkY * ChunkManager.chunkHeight,
         transform.position.z
      );
      
      transform.position = newPosition;
      
      if (chunkManager != null)
      {
         chunkManager.ForceUpdateChunks(newPosition);
      }
      else
      {
         FindChunkManager();
         if (chunkManager != null)
         {
            chunkManager.ForceUpdateChunks(newPosition);
         }
      }
   }
}
