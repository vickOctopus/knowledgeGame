using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
   public SceneField[] sceneUnload;
   public SceneField[] sceneLoad;

   private void OnTriggerEnter2D(Collider2D other)
   {
      if (other.CompareTag("Player"))
      {
         foreach (var VARIABLE in sceneLoad)//加载场景
         {
            if (VARIABLE==null)
            {
               break;
            }

            bool isLoaded=false;
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
               if (VARIABLE.SceneName == SceneManager.GetSceneAt(i).name)
               {
                  isLoaded = true;
                  break;
               }
            }
            
            if (!isLoaded)
            {
               SceneManager.LoadSceneAsync(VARIABLE, LoadSceneMode.Additive);
            }
                  
            
         }
         
         foreach (var VARIABLE in sceneUnload)//卸载场景
         {
            if (VARIABLE==null)
            {
               break;
            }
            
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
               if (VARIABLE.SceneName == SceneManager.GetSceneAt(i).name)
               {
                  SceneManager.UnloadSceneAsync(VARIABLE);
               }
            }
         }
      }
      
   }
}
