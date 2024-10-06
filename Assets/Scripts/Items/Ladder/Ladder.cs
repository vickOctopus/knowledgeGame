using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ladder : MonoBehaviour
{
  [SerializeField] private int height=1;
  [SerializeField]GameObject ladderPrefab;
  
  [ContextMenu("Build Ladder")]
  void BuildLadder()
  {
    for (int i = this.transform.childCount; i > 0; --i)
      DestroyImmediate(this.transform.GetChild(0).gameObject);
    
    var collider = GetComponent<BoxCollider2D>();
    var ladderHeight=GetComponent<SpriteRenderer>().bounds.size.y;
    collider.size = new Vector2(collider.size.x, height);
    collider.offset = new Vector2(collider.offset.x, collider.size.y/2);

    for (int i = 1; i < height; i++)
    {
      var child=Instantiate(ladderPrefab,new Vector3(transform.position.x,transform.position.y+ladderHeight*i,transform.position.z),Quaternion.identity);
      child.transform.parent = transform;
    }
  }
  
}
