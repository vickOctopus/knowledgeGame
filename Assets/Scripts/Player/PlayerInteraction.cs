using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
   private ISceneInteraction _sceneInteraction;//符合逻辑的object将被调用interact方法，具体实现在对应object中
   private bool _canInteract = false;
   private PlayerInput _playerInput;

   public GameObject F_Key;

   private void Awake()
   {
      _playerInput = new PlayerInput();
   }

   private void Start()
   {
      F_Key.SetActive(false);
   }

   private void OnEnable()
   {
      _playerInput.Enable();
   }

   private void OnDisable()
   {
      _playerInput.Disable();
   }

   private void OnTriggerEnter2D(Collider2D other)
   {
      _sceneInteraction = other.gameObject.GetComponent<ISceneInteraction>();
      if (_sceneInteraction is not null)
      {
         _canInteract = true;
         F_Key.SetActive(true);
      }
   }

   private void OnTriggerExit2D(Collider2D other)
   {
      _canInteract = false;
      _sceneInteraction = null;
      F_Key.SetActive(false);
   }

   private void Update()
   {
      if (_canInteract)
      {
         if (_playerInput.GamePLay.Interaction.triggered)
         {
            _sceneInteraction?.Interact();
            F_Key.SetActive(false);
         }
      }
   }
}
