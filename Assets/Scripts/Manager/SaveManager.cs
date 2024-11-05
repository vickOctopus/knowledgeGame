using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine.SceneManagement;

// 游戏加载状态枚举
public enum GameLoadState
{
    NotStarted,          // 未开始加载
    LoadingSaveData,     // 正在加载存档数据
    LoadingPlayerState,  // 正在加载玩家状态
    LoadingChunks,       // 正在加载区块
    WaitingForChunks,    // 等待区块加载完成
    Complete,            // 加载完成
    Failed              // 加载失败
}

// 加载进度类，用于追踪和显示加载状态
public class LoadingProgress
{
    public float Progress { get; private set; }           // 加载进度（0-1）
    public string CurrentOperation { get; private set; }  // 当前操作描述

    public LoadingProgress(float progress, string operation)
    {
        Progress = progress;
        CurrentOperation = operation;
    }
}

// 玩家重生状态枚举
public enum RespawnState
{
    NotStarted,         // 未开始重生
    PreparingRespawn,   // 准备重生中
    LoadingData,        // 加载数据中
    SettingPosition,    // 设置位置中
    WaitingForChunks,   // 等待区块加载
    ResettingState,     // 重置状态中
    Complete,           // 完成
    Failed             // 失败
}

public class SaveManager : MonoBehaviour
{
    // 单例实例
    public static SaveManager instance;
    private PlayerState playerState;  // 玩家状态组件引用
    
    public PlayerData playerData;     // 玩家数据
    private Vector2 _respawnPosition; // 重生位置
    private const int MaxSaveSlots = 3; // 最大存档槽数量

    // 当前选择的存档槽
    public static int CurrentSlotIndex { get; set; } = 0;

    // 默认出生点位置
    public Vector2 defaultSpawnPoint = new Vector2(0, 0);

    // 当前加载状态和相关事件
    private GameLoadState currentLoadState = GameLoadState.NotStarted;
    public event Action<GameLoadState> OnLoadStateChanged;        // 加载状态改变事件
    public event Action<LoadingProgress> OnLoadProgressChanged;   // 加载进度改变事件

    private bool isLoadingGame = false;                          // 是否正在加载游戏
    private TaskCompletionSource<bool> loadingComplete;          // 加载完成任务源

    // 玩家加载状态枚
    public enum PlayerLoadState
    {
        NotLoaded,         // 未加载
        LoadingData,       // 加载数据中
        ResettingState,    // 重置状态中
        WaitingForChunks,  // 等待区块加载
        Complete,          // 完成
        Failed            // 失败
    }

    private PlayerLoadState playerLoadState = PlayerLoadState.NotLoaded;
    public event Action<PlayerLoadState> OnPlayerLoadStateChanged;    // 玩家加载状态改变事件

    // 重生状态和相关事件
    private RespawnState currentRespawnState = RespawnState.NotStarted;
    public event Action<RespawnState> OnRespawnStateChanged;         // 重生状态改变事件
    private Vector2 lastValidRespawnPoint;                          // 最后有效的重生点

    private async Task LoadGameAsync(int slotIndex)
    {
        try {
            // 开始加载存档数据
            currentLoadState = GameLoadState.LoadingSaveData;
            OnLoadStateChanged?.Invoke(currentLoadState);
            
            // 1. 异步加载基础存档数据
            await LoadSaveDataAsync(slotIndex);
            
            // 2. 异步加载玩家状态
            currentLoadState = GameLoadState.LoadingPlayerState;
            OnLoadStateChanged?.Invoke(currentLoadState);
            await LoadPlayerStateAsync();
            
            // 3. 步加载区块
            currentLoadState = GameLoadState.LoadingChunks;
            OnLoadStateChanged?.Invoke(currentLoadState);
            await LoadChunksAsync();
            
            // 加载完成
            currentLoadState = GameLoadState.Complete;
            OnLoadStateChanged?.Invoke(currentLoadState);
        }
        catch (Exception ex) {
            // 处理加载过程中的错误
            Debug.LogError($"Load game failed: {ex}");
            currentLoadState = GameLoadState.Failed;
            OnLoadStateChanged?.Invoke(currentLoadState);
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        if (playerState == null)
        {
            playerState = gameObject.AddComponent<PlayerState>();
        }
        
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
        }
        
        StartCoroutine(InitializeGameDelayed());
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private IEnumerator InitializeGameDelayed()
    {
        yield return null;
        
        #if UNITY_EDITOR
        if (PlayController.instance != null)
        {
            Vector3 playerPosition = PlayController.instance.transform.position;
            PlayController.instance.gameObject.SetActive(true);
            
            if (ChunkManager.Instance != null)
            {
                ChunkManager.Instance.InitializeChunks(playerPosition);
            }
            
            if (CameraController.Instance != null)
            {
                CameraController.Instance.CameraStartResetPosition(playerPosition);
            }
            
            PlayController.instance.EnableControl();
            yield break;
        }
        #endif
        
        if (PlayController.instance != null)
        {
            PlayController.instance.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(0.05f);
            
            if (PlayController.instance != null)
            {
                PlayController.instance.gameObject.SetActive(false);
            }
            else
            {
                yield break;
            }
        }
        
        GameStart();
    }

    public void GameStart()
    {
        CurrentSlotIndex = PlayerPrefs.GetInt("CurrentSlotIndex");
        _respawnPosition = defaultSpawnPoint;
        
        #if UNITY_EDITOR
        if (PlayController.instance != null)
        {
            return;
        }
        #endif
        
        string saveFilePath = GetSavePath(CurrentSlotIndex, "playerData.json");
        bool saveExists = File.Exists(saveFilePath);
        
        if (saveExists)
        {
            try
            {
                File.ReadAllText(saveFilePath);
            }
            catch (Exception) { }
        }
        
        LoadGame(CurrentSlotIndex);
    }
    
    public void SaveGame()
    {
        SaveGame(CurrentSlotIndex);
    }

    public void SaveGame(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSaveSlots)
        {
            return;
        }

        ISaveable[] saveableObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISaveable>().ToArray();
        foreach (ISaveable saveable in saveableObjects)
        {
            saveable.Save(slotIndex);
        }
    }

    public async void LoadGame(int slotIndex)
    {
        if (isLoadingGame)
        {
            return;
        }

        #if UNITY_EDITOR
        if (PlayController.instance != null)
        {
            if (CameraController.Instance != null)
            {
                CameraController.Instance.CameraStartResetPosition(PlayController.instance.transform.position);
            }
            PlayController.instance.EnableControl();
            return;
        }
        #endif

        try
        {
            isLoadingGame = true;
            loadingComplete = new TaskCompletionSource<bool>();

            // 1. 开始加载，确保玩家处于禁用状态
            SetLoadState(GameLoadState.LoadingSaveData);
            UpdateLoadProgress(0.1f, "正在准备加载...");
            
            if (PlayController.instance != null)
            {
                PlayController.instance.gameObject.SetActive(false);
            }

            // 2. 读取存档数据
            string playerSaveFilePath = GetSavePath(slotIndex, "playerData.json");
            Vector3 playerPosition = defaultSpawnPoint;
            bool hasValidSaveData = false;

            if (File.Exists(playerSaveFilePath))
            {
                try
                {
                    string jsonContent = File.ReadAllText(playerSaveFilePath);
                    var saveData = JsonUtility.FromJson<PlayerSaveData>(jsonContent);
                    if (saveData != null)
                    {
                        playerPosition = new Vector3(saveData.respawnPointX, saveData.respawnPointY, 0);
                        hasValidSaveData = true;
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }

            // 3. 先加载区块
            SetLoadState(GameLoadState.LoadingChunks);
            UpdateLoadProgress(0.5f, "正在加载游戏区块...");
            
            if (ChunkManager.Instance != null)
            {
                bool chunksLoaded = false;
                ChunkManager.OnChunkLoadedEvent += OnChunksLoaded;
                
                void OnChunksLoaded()
                {
                    chunksLoaded = true;
                    ChunkManager.OnChunkLoadedEvent -= OnChunksLoaded;
                }

                ChunkManager.Instance.InitializeChunks(playerPosition);
                
                SetLoadState(GameLoadState.WaitingForChunks);
                UpdateLoadProgress(0.8f, hasValidSaveData ? "正在加载存档位置..." : "正在加载默认位置...");
                
                // 等待区块加载完成
                float timeoutTime = Time.time + 5f;
                while (!chunksLoaded && Time.time < timeoutTime)
                {
                    await Task.Yield();
                }
            }

            // 4. 设置玩家位置和状态
            SetLoadState(GameLoadState.LoadingPlayerState);
            UpdateLoadProgress(0.3f, "正在加载玩家状态...");

            if (PlayController.instance != null)
            {
                // 先设置位置
                PlayController.instance.transform.position = playerPosition;
                
                // 加载其他状态
                if (playerState != null)
                {
                    playerState.Load(slotIndex);
                }
                
                // 确保相机位置正确
                if (CameraController.Instance != null)
                {
                    CameraController.Instance.CameraStartResetPosition(playerPosition);
                }
                
                // 最后再激活玩家
                PlayController.instance.gameObject.SetActive(true);
                PlayController.instance.EnableControl();
            }

            // 5. 完成加载
            SetLoadState(GameLoadState.Complete);
            UpdateLoadProgress(1.0f, hasValidSaveData ? "存档加载完成" : "��用默认位置加载完成");
        }
        catch (Exception)
        {
            SetLoadState(GameLoadState.Failed);
            UpdateLoadProgress(0f, "加载失败");
            HandleLoadError();
            throw;
        }
        finally
        {
            isLoadingGame = false;
        }
    }

    private void OnChunksLoadedForGameLoad()
    {
        loadingComplete?.TrySetResult(true);
    }

    private void DisableAllControls()
    {
        try
        {
            if (PlayController.instance != null)
            {
                PlayController.instance.DisableControl();
                PlayController.instance.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[SaveManager] PlayController.instance is null when trying to disable controls");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Error disabling controls: {ex.Message}");
        }
    }

    private void EnableAllControls()
    {
        try
        {
            if (PlayController.instance != null)
            {
                PlayController.instance.gameObject.SetActive(true);
                PlayController.instance.EnableControl();
            }
            else
            {
                Debug.LogWarning("[SaveManager] PlayController.instance is null when trying to enable controls");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Error enabling controls: {ex.Message}");
        }
    }

    private void HandleLoadError()
    {
        // 错误恢复
        if (PlayController.instance != null)
        {
            PlayController.instance.gameObject.SetActive(true);
            PlayController.instance.EnableControl();
            PlayController.instance.currentHp = PlayController.instance.maxHp;
            PlayController.instance.HpChange();
            
            // 设置位置
            PlayController.instance.transform.position = defaultSpawnPoint;
            
            // 更新相机位置
            if (CameraController.Instance != null)
            {
                CameraController.Instance.CameraStartResetPosition(defaultSpawnPoint);
            }
        }
    }

    private void SetLoadState(GameLoadState newState)
    {
        if (currentLoadState != newState)
        {
            currentLoadState = newState;
            OnLoadStateChanged?.Invoke(currentLoadState);
        }
    }

    private void UpdateLoadProgress(float progress, string operation)
    {
        OnLoadProgressChanged?.Invoke(new LoadingProgress(progress, operation));
    }

    public bool DoesSaveExist(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= MaxSaveSlots)
        {
            Debug.LogError("无效的存档索引");
            return false;
        }

        string savePath = GetSavePath(slotIndex, "playerData.json");
        return File.Exists(savePath);
    }

    public static string GetSavePath(int slotIndex, string fileName)
    {
        return Path.Combine(Application.persistentDataPath, $"SaveSlot_{slotIndex}", fileName);
    }

    public void GetRespawnPosition(Vector2 respawnPosition)
    {
        _respawnPosition = respawnPosition;
    }

    public Vector2 GetCurrentRespawnPosition()
    {
        return _respawnPosition;
    }

    private void SetRespawnState(RespawnState newState)
    {
        if (currentRespawnState != newState)
        {
            currentRespawnState = newState;
            OnRespawnStateChanged?.Invoke(currentRespawnState);
        }
    }

    public async Task HandlePlayerRespawn()
    {
        try
        {
            Debug.Log("[SaveManager] Starting respawn process");
            SetRespawnState(RespawnState.PreparingRespawn);
            
            // 1. 准备重生，禁用玩家控制和显示
            if (PlayController.instance != null)
            {
                PlayController.instance.DisableControl();
                PlayController.instance.gameObject.SetActive(false);
            }

            // 2. 加载存档数据并确定重生位置
            SetRespawnState(RespawnState.LoadingData);
            string saveFilePath = GetSavePath(CurrentSlotIndex, "playerData.json");
            Vector2 respawnPosition = defaultSpawnPoint;

            if (File.Exists(saveFilePath))
            {
                try
                {
                    string jsonData = File.ReadAllText(saveFilePath);
                    var saveData = JsonUtility.FromJson<PlayerSaveData>(jsonData);
                    
                    if (saveData != null)
                    {
                        respawnPosition = new Vector2(saveData.respawnPointX, saveData.respawnPointY);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SaveManager] Error loading save data: {ex.Message}");
                }
            }

            // 3. 加载区块
            SetRespawnState(RespawnState.WaitingForChunks);
            Debug.Log($"[SaveManager] Loading chunks for position: {respawnPosition}");
            await WaitForChunksLoad(respawnPosition);

            // 4. 设置玩家位置和状态
            if (PlayController.instance != null)
            {
                Debug.Log($"[SaveManager] Setting player position to: {respawnPosition}");
                PlayController.instance.transform.position = respawnPosition;
                PlayController.instance.currentHp = PlayController.instance.maxHp;
                PlayController.instance.HpChange();
                
                // 确保相机位置正确
                if (CameraController.Instance != null)
                {
                    CameraController.Instance.CameraStartResetPosition(respawnPosition);
                }
                
                // 激活玩家
                PlayController.instance.gameObject.SetActive(true);
                PlayController.instance.EnableControl();
            }

            SetRespawnState(RespawnState.Complete);
            Debug.Log("[SaveManager] Respawn completed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Error during respawn: {ex.Message}");
            SetRespawnState(RespawnState.Failed);
            HandleRespawnError();
        }
    }

    private async Task WaitForChunksLoad(Vector2 position)
    {
        if (ChunkManager.Instance == null) return;

        var loadingComplete = new TaskCompletionSource<bool>();
        void OnChunksLoaded() => loadingComplete.TrySetResult(true);

        try
        {
            ChunkManager.OnChunkLoadedEvent += OnChunksLoaded;
            ChunkManager.Instance.InitializeChunks(position);

            // 使用 Task.WhenAny 实现时
            using (var cts = new CancellationTokenSource())
            {
                var timeoutTask = Task.Delay(5000, cts.Token); // 5秒超时
                var completedTask = await Task.WhenAny(loadingComplete.Task, timeoutTask);
                
                // 取消另一个未完成的任务
                cts.Cancel();
                
                if (completedTask == timeoutTask)
                {
                    Debug.LogWarning("[SaveManager] Chunk loading timed out");
                    throw new TimeoutException("Chunk loading timed out");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Error waiting for chunks: {ex.Message}");
            throw;
        }
        finally
        {
            ChunkManager.OnChunkLoadedEvent -= OnChunksLoaded;
        }
    }

    private void SetPlayerLoadState(PlayerLoadState newState)
    {
        if (playerLoadState != newState)
        {
            playerLoadState = newState;
            OnPlayerLoadStateChanged?.Invoke(playerLoadState);
        }
    }

    private void ResetPlayerState()
    {
        if (PlayController.instance == null) return;

        try
        {
            PlayController.instance.currentHp = PlayController.instance.maxHp;
            PlayController.instance.HpChange();
            
            // 确保相机位置正确
            if (CameraController.Instance != null)
            {
                CameraController.Instance.CameraStartResetPosition(PlayController.instance.transform.position);
            }
        }
        catch (Exception)
        {
            HandleRespawnError();
        }
    }

    private void HandleRespawnTimeout()
    {
        Debug.LogWarning("[SaveManager] Handling respawn timeout");
        SetPlayerLoadState(PlayerLoadState.Failed);
        ResetToDefaultState();
        EnableAllControls();
    }

    private void HandleRespawnError()
    {
        Debug.LogError("[SaveManager] Handling respawn error");
        
        if (PlayController.instance != null)
        {
            // 确保玩家对象被激活
            PlayController.instance.gameObject.SetActive(true);
            
            // 重置到默认位置
            PlayController.instance.transform.position = defaultSpawnPoint;
            
            // 重置状态
            PlayController.instance.currentHp = PlayController.instance.maxHp;
            PlayController.instance.HpChange();
            PlayController.instance.EnableControl();
            
            // 更新相机位置
            if (CameraController.Instance != null)
            {
                CameraController.Instance.CameraStartResetPosition(defaultSpawnPoint);
            }
        }
    }

    private void ResetToDefaultState()
    {
        if (PlayController.instance != null)
        {
            try
            {
                // 重置到默认状态
                PlayController.instance.transform.position = defaultSpawnPoint;
                PlayController.instance.currentHp = PlayController.instance.maxHp;
                PlayController.instance.HpChange();
                
                if (CameraController.Instance != null)
                {
                    CameraController.Instance.CameraStartResetPosition(defaultSpawnPoint);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Error during reset to default state: {ex.Message}");
            }
        }
    }

    private async Task LoadSaveDataAsync(int slotIndex)
    {
        try
        {
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Error loading save data: {ex.Message}");
            throw;
        }
    }

    private async Task LoadPlayerStateAsync()
    {
        try
        {
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Error loading player state: {ex.Message}");
            throw;
        }
    }

    private async Task LoadChunksAsync()
    {
        try
        {
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] Error loading chunks: {ex.Message}");
            throw;
        }
    }

    private async Task WaitWithTimeout(Task task, int milliseconds)
    {
        using var cts = new CancellationTokenSource(milliseconds);
        try
        {
            await Task.WhenAny(task, Task.Delay(milliseconds, cts.Token));
            if (!task.IsCompleted)
            {
                throw new TimeoutException();
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException();
        }
    }
}

