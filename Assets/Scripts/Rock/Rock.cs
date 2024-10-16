using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Rock : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float frictionCoefficient = 1f;
    private bool _isInWater = false;

    private const float CHECK_INTERVAL = 0.1f;
    private float lastCheckTime;
    private readonly RaycastHit2D[] raycastResults = new RaycastHit2D[1];
    private ContactFilter2D contactFilter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        contactFilter = new ContactFilter2D();
        contactFilter.SetLayerMask(LayerMask.GetMask("Player"));
        contactFilter.useLayerMask = true;
    }

    void FixedUpdate()
    {
        if (!_isInWater && Time.time - lastCheckTime >= CHECK_INTERVAL)
        {
            CheckPlayerAbove();
            lastCheckTime = Time.time;
        }
    }

    void CheckPlayerAbove()
    {
        Vector2 checkStart = (Vector2)transform.position + Vector2.up * 0.1f;
        int hitCount = Physics2D.RaycastNonAlloc(checkStart, Vector2.up, raycastResults, 1f, contactFilter.layerMask);
        
        Debug.DrawRay(checkStart, Vector2.up * 1f, Color.blue, CHECK_INTERVAL);
        
        if (hitCount > 0 && raycastResults[0].collider.CompareTag("Player"))
        {
            ApplyFrictionForce(raycastResults[0].collider.gameObject);
        }
    }

    void ApplyFrictionForce(GameObject player)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            Vector2 playerVelocity = new Vector2(playerRb.velocity.x, 0);
            Vector2 frictionForce = playerVelocity * frictionCoefficient;
            rb.AddForce(frictionForce, ForceMode2D.Force);

            Debug.DrawRay(transform.position, frictionForce, Color.red, CHECK_INTERVAL);
            Debug.DrawRay(transform.position, Vector2.up, Color.green, CHECK_INTERVAL);
        }
    }

    public void PrepareForSave() { }
    public void Save() { }
    public void Load() { }

    public void EnterWater()
    {
        if (!_isInWater)
        {
            _isInWater = true;
            this.enabled = false;
        }
    }
}
