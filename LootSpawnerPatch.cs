using System;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using Duckov.Scenes;
using Duckov.Utilities;
using System.IO;
using System.Collections.Generic;
using Duckov.UI;  // 添加UI命名空间引用
using ItemStatsSystem;  // 添加ItemStatsSystem命名空间引用
using UnityEngine.SceneManagement;  // 添加SceneManager引用
using Cysharp.Threading.Tasks; // 添加UniTask支持
using System.Linq; // 添加LINQ支持
using System.Threading.Tasks; // 添加Task支持

namespace DropRateSetting
{
    /// <summary>
    /// Harmony补丁类，用于修改LootSpawner行为
    /// </summary>
    [HarmonyPatch(typeof(LootSpawner))]
    public class LootSpawnerPatch
    {
        // 添加一个静态变量来跟踪是否正在进行重新生成操作
        private static bool isRespawning = false;
        private static int pendingRespawns = 0;
        private static bool isSceneChanging = false;
        
        /// <summary>
        /// 重新生成所有场景中的战利品
        /// </summary>
        public static void RespawnLoot()
        {
            try
            {
                // 检查是否正在切换场景
                if (isSceneChanging)
                {
                    WriteDetailedLog($"[DropRateSetting] 正在切换场景，跳过重新生成战利品");
                    return;
                }
                
                // 设置重新生成标志
                isRespawning = true;
                pendingRespawns = 0;
                
                // 记录当前爆率设置
                int dropRateMultiplier = ModConfigDropRateManager.DropRateMultiplier;
                int randomCountMultiplier = ModConfigDropRateManager.RandomCountMultiplier;
                bool isModEnabled = ModConfigDropRateManager.IsModEnabled;
                
                // 检查是否在基地场景，避免在基地刷新物品
                if (LevelManager.Instance != null && LevelManager.Instance.IsBaseLevel)
                {
                    WriteDetailedLog($"[DropRateSetting] 检测到基地场景，跳过箱子刷新以避免物品掉落");
                    isRespawning = false;
                    return;
                }
                
                // 检查MultiSceneCore.Instance是否可用
                if (MultiSceneCore.Instance == null)
                {
                    WriteDetailedLog($"[DropRateSetting] MultiSceneCore.Instance不可用，跳过箱子刷新");
                    isRespawning = false;
                    return;
                }
                
                // 同时查找所有InteractableLootbox实例（直接生成的箱子）
                InteractableLootbox[] lootBoxes = Resources.FindObjectsOfTypeAll<InteractableLootbox>();
                List<InteractableLootbox> lootBoxesInScene = new List<InteractableLootbox>();
                Scene currentScene = SceneManager.GetActiveScene();
                foreach (InteractableLootbox lootBox in lootBoxes)
                {
                    // 只处理当前场景中激活的对象
                    if (lootBox.gameObject.scene == currentScene && lootBox.gameObject.activeInHierarchy)
                    {
                        lootBoxesInScene.Add(lootBox);
                    }
                }
                
                WriteDetailedLog($"[DropRateSetting] 找到 {lootBoxesInScene.Count} 个InteractableLootbox");
                
                // 记录日志信息
                int totalSpawners = lootBoxesInScene.Count;
                int spawnedCount = 0;        // 已生成箱子数（箱子已生成）
                int unspawnedCount = 0;      // 未生成箱子数（箱子未生成）
                int modifiedCount = 0;       // 实际被修改箱子数
                
                // 添加到文件日志
                WriteDetailedLog($"[DropRateSetting] 当前爆率设置 - Mod启用: {isModEnabled}, 爆率倍数: {dropRateMultiplier}, 数量倍数: {randomCountMultiplier}");
                WriteDetailedLog($"[DropRateSetting] 开始重新生成战利品，总箱子数: {totalSpawners}");
                
                // 如果没有找到箱子，记录警告信息
                if (totalSpawners == 0)
                {
                    WriteDetailedLog("[DropRateSetting] 未找到任何InteractableLootbox实例");
                    isRespawning = false;
                    return;
                }
                
                // 设置待处理的重新生成数量
                pendingRespawns = lootBoxesInScene.Count;
                
                // 重新生成每个InteractableLootbox的战利品
                foreach (InteractableLootbox lootBox in lootBoxesInScene)
                {
                    // 检查是否正在切换场景
                    if (isSceneChanging)
                    {
                        WriteDetailedLog($"[DropRateSetting] 场景切换中，中断重新生成操作");
                        pendingRespawns = 0;
                        isRespawning = false;
                        return;
                    }
                    
                    // 处理InteractableLootbox
                    if (ProcessInteractableLootbox(lootBox, ref spawnedCount, ref unspawnedCount, ref modifiedCount))
                    {
                        // modifiedCount++ 已在ProcessInteractableLootbox中增加
                    }
                }
                
                // 记录最终日志
                WriteDetailedLog($"[DropRateSetting] 战利品重新生成完成");
                WriteDetailedLog($"  总箱子数: {totalSpawners}");
                WriteDetailedLog($"  已生成箱子数: {spawnedCount}");
                WriteDetailedLog($"  未生成箱子数: {unspawnedCount}");
                WriteDetailedLog($"  实际被修改箱子数: {modifiedCount}");
                
                // 使用统一的日志函数记录统计信息
                WriteDetailedLog($"[DropRateSetting] 统计信息 - Mod启用: {isModEnabled}, 爆率倍数: {dropRateMultiplier}, 数量倍数: {randomCountMultiplier}, " +
                               $"总箱子数: {totalSpawners}, 已生成箱子数: {spawnedCount}, 未生成箱子数: {unspawnedCount}, 实际被修改箱子数: {modifiedCount}");
                
                // 重置重新生成标志
                isRespawning = false;
            }
            catch (Exception ex)
            {
                WriteDetailedLog($"[DropRateSetting] 重新生成战利品时出错: {ex.Message}");
                WriteDetailedLog($"[DropRateSetting] StackTrace: {ex.StackTrace}");
                isRespawning = false;
            }
        }
        
        /// <summary>
        /// 检查是否正在进行重新生成操作
        /// </summary>
        /// <returns>是否正在进行重新生成</returns>
        public static bool IsRespawning()
        {
            return isRespawning;
        }
        
        /// <summary>
        /// 处理InteractableLootbox对象
        /// </summary>
        /// <param name="lootBox">InteractableLootbox对象</param>
        /// <param name="spawnedCount">已生成计数</param>
        /// <param name="unspawnedCount">未生成计数</param>
        /// <param name="modifiedCount">修改计数</param>
        /// <returns>是否成功处理</returns>
        private static bool ProcessInteractableLootbox(InteractableLootbox lootBox, ref int spawnedCount, ref int unspawnedCount, ref int modifiedCount)
        {
            try
            {
                // 检查是否正在切换场景
                if (isSceneChanging)
                {
                    pendingRespawns--;
                    return false;
                }
                
                // 检查是否在基地场景
                if (LevelManager.Instance != null && LevelManager.Instance.IsBaseLevel)
                {
                    WriteDetailedLog($"[DropRateSetting] 检测到基地场景，跳过InteractableLootbox处理");
                    pendingRespawns--;
                    return false;
                }
                
                // 检查MultiSceneCore.Instance是否可用
                if (MultiSceneCore.Instance == null)
                {
                    WriteDetailedLog($"[DropRateSetting] MultiSceneCore.Instance不可用，跳过InteractableLootbox处理");
                    pendingRespawns--;
                    return false;
                }
                
                // 检查箱子是否已被打开
                if (lootBox.Looted)
                {
                    WriteDetailedLog($"[DropRateSetting] InteractableLootbox 已被打开，跳过重新生成");
                    pendingRespawns--;
                    return false;
                }
                
                // 获取箱子的key（使用反射调用private方法）
                int key = 0;
                var getKeyMethod = typeof(InteractableLootbox).GetMethod("GetKey", BindingFlags.NonPublic | BindingFlags.Instance);
                if (getKeyMethod != null)
                {
                    key = (int)getKeyMethod.Invoke(lootBox, null);
                }
                else
                {
                    WriteDetailedLog("[DropRateSetting] 无法获取InteractableLootbox的GetKey方法");
                    pendingRespawns--;
                    return false;
                }
                
                // 检查MultiSceneCore.Instance是否仍然可用
                if (MultiSceneCore.Instance == null) 
                {
                    WriteDetailedLog("[DropRateSetting] MultiSceneCore.Instance不可用");
                    pendingRespawns--;
                    return false;
                }
                
                // 检查该位置是否已经生成过箱子
                bool isSpawned = MultiSceneCore.Instance.inLevelData.ContainsKey(key);
                bool isGenerated = false;
                
                // 检查箱子是否已生成
                object generatedFlag;
                if (MultiSceneCore.Instance.inLevelData.TryGetValue(key, out generatedFlag) && generatedFlag is bool)
                {
                    isGenerated = (bool)generatedFlag;
                }
                
                // 统计箱子状态 - 修复逻辑：只要箱子存在就认为是已生成
                if (isSpawned)
                {
                    spawnedCount++; // 箱子已生成
                }
                else
                {
                    unspawnedCount++; // 箱子未生成
                }
                
                // 添加详细的箱子状态日志
                WriteDetailedLog($"[DropRateSetting] InteractableLootbox状态 - Key: {key}, 已存在: {isSpawned}, 已生成: {isGenerated}");
                
                // 检查箱子的Inventory是否存在
                Inventory inventory = lootBox.Inventory;
                if (inventory != null)
                {
                    // 记录重新生成前的状态
                    int itemCount = inventory.Content.Count;
                    WriteDetailedLog($"[DropRateSetting] 准备重新生成InteractableLootbox {key} 中的物品，原有物品数: {itemCount}");
                    
                    // 只有当箱子中有物品时才进行清理，避免对空箱子进行不必要的操作
                    if (itemCount > 0)
                    {
                        // 使用更安全的方式清理物品，避免触发可能的问题
                        // 通过反射调用Inventory的DestroyAllContent方法（如果存在）
                        var destroyMethod = typeof(Inventory).GetMethod("DestroyAllContent", BindingFlags.Public | BindingFlags.Instance);
                        if (destroyMethod != null)
                        {
                            try
                            {
                                destroyMethod.Invoke(inventory, null);
                                WriteDetailedLog($"[DropRateSetting] 通过DestroyAllContent清理InteractableLootbox {key} 中的物品");
                            }
                            catch (Exception ex)
                            {
                                WriteDetailedLog($"[DropRateSetting] 调用DestroyAllContent失败: {ex.Message}");
                                // 如果DestroyAllContent失败，回退到Clear方法
                                inventory.Content.Clear();
                                WriteDetailedLog($"[DropRateSetting] 通过Content.Clear清理InteractableLootbox {key} 中的物品");
                            }
                        }
                        else
                        {
                            // 如果没有DestroyAllContent方法，使用Clear方法
                            inventory.Content.Clear();
                            WriteDetailedLog($"[DropRateSetting] 通过Content.Clear清理InteractableLootbox {key} 中的物品");
                        }
                    }
                }
                 
                // 重新生成物品
                try
                {
                    // 在调用StartSetup之前再次检查MultiSceneCore.Instance是否仍然可用
                    if (MultiSceneCore.Instance == null) 
                    {
                        WriteDetailedLog("[DropRateSetting] MultiSceneCore.Instance在调用StartSetup之前不可用");
                        pendingRespawns--;
                        return false;
                    }
                    
                    // 查找关联的LootBoxLoader并重新生成
                    LootBoxLoader loader = lootBox.GetComponent<LootBoxLoader>();
                    if (loader != null)
                    {
                        WriteDetailedLog($"[DropRateSetting] 通过LootBoxLoader重新生成InteractableLootbox {key}");
                        // 重置加载器状态并重新生成
                        loader.StartSetup();
                    }
                    else
                    {
                        WriteDetailedLog($"[DropRateSetting] InteractableLootbox {key} 没有关联的LootBoxLoader");
                    }
                }
                catch (Exception loaderEx)
                {
                    WriteDetailedLog($"[DropRateSetting] 通过LootBoxLoader重新生成InteractableLootbox时出错: {loaderEx.Message}");
                }
                
                // 标记该箱子已生成
                if (MultiSceneCore.Instance != null)
                {
                    MultiSceneCore.Instance.inLevelData[key] = true;
                    WriteDetailedLog($"[DropRateSetting] 标记InteractableLootbox {key} 为已生成");
                }
                else
                {
                    WriteDetailedLog($"[DropRateSetting] 无法标记InteractableLootbox {key} 为已生成，MultiSceneCore.Instance为null");
                }
                
                WriteDetailedLog($"[DropRateSetting] InteractableLootbox {key} 重新生成完成");
                modifiedCount++; // 增加修改计数
                pendingRespawns--;
                return true;
            }
            catch (Exception ex)
            {
                WriteDetailedLog($"[DropRateSetting] 处理InteractableLootbox时出错: {ex.Message}");
                pendingRespawns--;
                return false;
            }
        }
        
        /// <summary>
        /// 检查是否还有待处理的重新生成操作
        /// </summary>
        /// <returns>是否还有待处理的重新生成操作</returns>
        public static bool HasPendingRespawns()
        {
            return pendingRespawns > 0;
        }
        
        /// <summary>
        /// 检查是否正在切换场景
        /// </summary>
        /// <returns>是否正在切换场景</returns>
        public static bool IsSceneChanging()
        {
            return isSceneChanging;
        }
        
        /// <summary>
        /// 将详细日志写入文件
        /// </summary>
        /// <param name="message">日志消息</param>
        private static void WriteDetailedLog(string message)
        {
            try
            {
                // 获取DLL所在目录
                string dllPath = Assembly.GetExecutingAssembly().Location;
                string logDirectory = Path.GetDirectoryName(dllPath);
                string logFilePath = Path.Combine(logDirectory, "DropRateSetting.log");
                
                // 创建日志内容
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}" + Environment.NewLine;
                
                // 写入日志文件
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception)
            {
                // 静默处理错误，避免日志写入错误影响主逻辑
            }
        }
    }
}