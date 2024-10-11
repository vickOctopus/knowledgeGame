using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Template : MonoBehaviour
{
    private Tilemap _tilemap;
    private int _height = 28;
    private int _width = 50;
    
    [SerializeField] private TileBase tileFrame;
    [Space] 
    [SerializeField] private Vector2Int size;


    private void Start()
    {
        Destroy(gameObject);
    }

    [ContextMenu("Generate Template")]
    private void SetTemplate()
    {
        _tilemap = GetComponent<Tilemap>();
        _tilemap.ClearAllTiles();

        for (int i = 0; i > -size.y*_height; i-=_height)
        {
            for (int j = 0; j < size.x*_width; j++)
            {
                _tilemap.SetTile(new Vector3Int(j-_width/2, i+_height/2-1, 0), tileFrame);
                _tilemap.SetTile(new Vector3Int(j-_width/2, i-_height/2, 0), tileFrame);
            }
        }

        for (int i = 0; i < size.x*_width; i+=_width)
        {
            for (int j = 0; j > -_height*size.y; j--)
            {
                _tilemap.SetTile(new Vector3Int(i-_width/2, j+_height/2-1, 0), tileFrame);
                _tilemap.SetTile(new Vector3Int(i+_width/2-1, j+_height/2-1, 0), tileFrame);
            }
        }
    }
    
}
