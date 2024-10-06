using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SwitchPlatform : MonoBehaviour
{
    
    [SerializeField]private TileBase trueTile;
    [SerializeField]private TileBase falseTile;
    [SerializeField] private int platformID;
    private Tilemap _tilemap;
    private bool _tileState;

    private List<Vector3Int> _tilePositions=new List<Vector3Int>();//存储已放置的tile的位置信息
    
    private void OnDisable()
    {
        GameManager.instance.OnSwitchChange -= PlatformChange;
    }

    private void Start()
    {
        GameManager.instance.OnSwitchChange += PlatformChange;
        
    }

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        GetTilePosition();
        
        if (!PlayerPrefs.HasKey(name))
        {
            PlayerPrefs.SetInt(name, 0);
        }
        else if(PlayerPrefs.GetInt(name) == 1)
        {
            PlatformChange(platformID);
        }
    }
    void PlatformChange(int switchID)
    {
        if (switchID == platformID)
        {
            _tileState=!_tileState;
            PlayerPrefs.SetInt(name, _tileState?1:0);
           foreach (var position in _tilePositions)
           {
               //flip改变tile
               if (_tilemap.GetTile(position)==trueTile)
               {
                   _tilemap.SetTile(position, falseTile);
               }
               else
               {
                   _tilemap.SetTile(position, trueTile); 
               }
           }  
        }
       
      
    }

    private void GetTilePosition()//获取已放置的tile的位置信息
    {
        _tilePositions.Clear();
        foreach (var position in _tilemap.cellBounds.allPositionsWithin)
        {
            if (_tilemap.HasTile(position))
            {
                _tilePositions.Add(position);
            }
        }
    }
    
}
