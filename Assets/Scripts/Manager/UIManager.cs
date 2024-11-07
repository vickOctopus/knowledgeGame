using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    [SerializeField] private GameObject SaveUI;
    [SerializeField] private GameObject DiaryUI;    
    public GameObject[] PaperUI;
   // public GameObject journalUI;
    private int _paperId;
    private PlayerInput _playerInput;
    [SerializeField] private GameObject rewardPanel;
    [SerializeField] private RewardItemDisplay rewardItemDisplay;
    [SerializeField] private float rewardDisplayDuration = 3f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        DontDestroyOnLoad(this);
        _playerInput = new PlayerInput();
    }

    private void Start()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        _playerInput.Enable();
    }

    private void OnDisable()
    {
        _playerInput.Disable();
    }

    private void Update()
    {
        if (_playerInput.UI.Exit.triggered)
        {
            if (DiaryUI.activeInHierarchy)
            {
                
                PaperUI[_paperId].SetActive(false);
              // PaperUI[_paperId].transform.SetParent(journalUI.transform);
                DiaryUI.SetActive(false);
            }
        }
    }

    public  void OpenDiary(int paperId)
    {
        _paperId = paperId;
        DiaryUI.SetActive(true);
        PaperUI[paperId].SetActive(true);
    }

    public void HideSaveUI()
    {
        SaveUI.SetActive(false);
        Cursor.visible = false;
        PlayController.instance.EnableControl();
    }

    public void ShowSaveUI()
    {
        SaveUI.SetActive(true);
        Cursor.visible = true;
        PlayController.instance.DisableControl();
        
    }

    public void ShowReward(Sprite rewardSprite)
    {
        if (rewardPanel != null && rewardItemDisplay != null)
        {
            rewardPanel.SetActive(true);
            rewardItemDisplay.DisplayReward(rewardSprite);
            StartCoroutine(HideRewardAfterDelay());
        }
    }

    private IEnumerator HideRewardAfterDelay()
    {
        yield return new WaitForSeconds(rewardDisplayDuration);
        rewardPanel.SetActive(false);
    }
}
