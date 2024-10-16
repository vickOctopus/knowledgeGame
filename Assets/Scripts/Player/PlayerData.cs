using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "ScriptableObjects/PlayerData", order = 1)]
public class PlayerData : ScriptableObject
{
    [Tooltip("currentHp的初始值也就是玩家开始游戏时的生命初始值")]
    public int currentHp;
    public int maxHp;
    public Vector2 respawnPoint;
}
