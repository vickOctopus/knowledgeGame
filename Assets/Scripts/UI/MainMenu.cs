using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
  
    public GameObject mainMenu;
    public GameObject newGameMenu;
   public void StartGame()
   {
       Debug.Log("start");
       SaveManager.instance.GameStart();
       MySceneManager.instance.StartGame();
   }

   public void NewGameButtonClicked()
   {
       mainMenu.SetActive(false);
       newGameMenu.SetActive(true);
       
   }

   public void SlotOneSave()
   {
       PlayerPrefs.SetInt("CurrentSlotIndex",0);
       StartGame();
   }


   public void SlotTwoSave()
   {
       PlayerPrefs.SetInt("CurrentSlotIndex",1);
       StartGame();
   }

   public void SlotThreeSave()
   {
       PlayerPrefs.SetInt("CurrentSlotIndex",2);
       StartGame();
   }
}
