using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RewardItemDisplay : MonoBehaviour
{
    [Header("动画设置")]
    [SerializeField] private float startScale = 0.5f;
    [SerializeField] private float targetScale = 1f;
    [SerializeField] private float scaleDuration = 0.5f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;
    
    [Header("额外动画效果")]
    [SerializeField] private float rotateAngle = 10f;
    [SerializeField] private float rotateDuration = 0.3f;
    [SerializeField] private Ease rotateEase = Ease.InOutSine;
    
    private Image itemImage;
    private RectTransform rectTransform;
    private Sequence animationSequence;

    private void Awake()
    {
        itemImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void DisplayReward(Sprite rewardSprite)
    {
        // 停止之前的动画
        if (animationSequence != null)
        {
            animationSequence.Kill();
        }

        // 设置sprite和初始状态
        itemImage.sprite = rewardSprite;
        rectTransform.localScale = Vector3.one * startScale;
        rectTransform.rotation = Quaternion.identity;

        // 创建动画序列
        animationSequence = DOTween.Sequence();
        
        // 缩放动画
        animationSequence.Append(
            rectTransform.DOScale(targetScale, scaleDuration)
            .SetEase(scaleEase)
        );
        
        // 添加轻微摇摆动画
        animationSequence.Append(
            rectTransform.DORotate(new Vector3(0, 0, rotateAngle), rotateDuration)
            .SetEase(rotateEase)
        );
        
        animationSequence.Append(
            rectTransform.DORotate(new Vector3(0, 0, -rotateAngle), rotateDuration * 2)
            .SetEase(rotateEase)
        );
        
        animationSequence.Append(
            rectTransform.DORotate(Vector3.zero, rotateDuration)
            .SetEase(rotateEase)
        );

        // 确保动画在Time.timeScale = 0时也能播放
        animationSequence.SetUpdate(true);
    }

    private void OnDisable()
    {
        // 确保在禁用时停止所有动画
        if (animationSequence != null)
        {
            animationSequence.Kill();
        }
    }
} 