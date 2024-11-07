using UnityEngine;

namespace Items
{
    public class Chest : MonoBehaviour, ISceneInteraction
    {
        [SerializeField] private Sprite rewardSprite;
        [SerializeField] private Sprite openedSprite;
        private string openAnimationTrigger = "Open";
        
        private bool isOpened = false;
        private SpriteRenderer spriteRenderer;
        private BoxCollider2D boxCollider;
        private Animator animator;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            boxCollider = GetComponent<BoxCollider2D>();
            animator = GetComponent<Animator>();
        }

        private string GetChestId()
        {
            return transform.parent != null 
                ? transform.parent.name + "_" + gameObject.name 
                : gameObject.name;
        }

        private void Start()
        {
            string chestId = GetChestId();
            isOpened = PlayerPrefs.GetInt(chestId, 0) == 1;
            
            if (isOpened)
            {
                if (animator != null)
                {
                    animator.enabled = false;
                }
                
                if (spriteRenderer != null && openedSprite != null)
                {
                    spriteRenderer.sprite = openedSprite;
                }
                
                if (boxCollider != null)
                {
                    boxCollider.enabled = false;
                }
            }
        }

        public void Interact()
        {
            if (isOpened) return;
            
            isOpened = true;
            
            string chestId = GetChestId();
            PlayerPrefs.SetInt(chestId, 1);
            PlayerPrefs.Save();
            
            if (animator != null)
            {
                animator.enabled = true;
                animator.SetTrigger(openAnimationTrigger);
            }
            else
            {
                OnOpenComplete();
            }
        }

        public void OnOpenAnimationComplete()
        {
            OnOpenComplete();
        }

        private void OnOpenComplete()
        {
            if (spriteRenderer != null && openedSprite != null)
            {
                spriteRenderer.sprite = openedSprite;
            }
            
            if (boxCollider != null)
            {
                boxCollider.enabled = false;
            }

            if (animator != null)
            {
                animator.enabled = false;
            }
            
            if (rewardSprite != null)
            {
                UIManager.instance.ShowReward(rewardSprite);
            }
        }
    }
} 