/// <summary>
/// 实现功能：旧版游戏结束检测脚本的兼容占位。胜负检测已合并到 GameFlowController。
/// </summary>
using System;
using UnityEngine;

[Obsolete("胜负检测已合并到 GameFlowController。请从场景中移除 GameEndController，改用 GameFlowController + GameEndUIController。")]
public class GameEndController : MonoBehaviour
{
    [Header("迁移提示")]
    [SerializeField] private bool logMigrationWarning = true;

    private void Start()
    {
        if (!logMigrationWarning)
            return;

        Debug.LogWarning($"[GameEndController] 该脚本已废弃，不再负责胜负检测。请移除它，并使用 GameFlowController 管理胜负规则 | object:{name}");
    }
}
