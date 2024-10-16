using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
   
   public static CameraController Instance;

   // 将 chunkManager 改为 public
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

   public void CameraStartResetPosition(float x, float y)
   {
      Vector3 newPosition = new Vector3(
         transform.position.x + x * ChunkManager.chunkWidth,
         transform.position.y + y * ChunkManager.chunkHeight,
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
