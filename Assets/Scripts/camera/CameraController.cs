using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
   
   public static CameraController Instance;

   private ChunkManager chunkManager;

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
      }
   }

   

   private void Start()
   {
      chunkManager = FindObjectOfType<ChunkManager>();
      if (chunkManager == null)
      {
         Debug.LogError("ChunkManager not found in the scene!");
      }
   }

   public void CameraStartResetPosition(float x, float y)
   {
      transform.position = new Vector3(
         transform.position.x + x * ChunkManager.chunkWidth,
         transform.position.y + y * ChunkManager.chunkHeight,
         transform.position.z
      );
      
      if (chunkManager != null)
      {
         chunkManager.ForceUpdateChunks(transform.position);
      }
   }
   
   
}
