using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeasureChest_Key : TreasureChest_Parent
{
   public GameObject key;
   public int keyID;

   public override void Interact()
   {
      base.Interact();

      var tem = Instantiate(key, new Vector3(transform.position.x, transform.position.y + 2.0f, transform.position.z), Quaternion.identity);
      tem.GetComponent<Key>().KeyID = keyID;
      StartCoroutine(WaitForKey(tem));
   }

   private IEnumerator WaitForKey(GameObject k)
   {
      yield return new WaitForSeconds(0.5f);
      k.AddComponent<Rigidbody2D>();
   }
}
