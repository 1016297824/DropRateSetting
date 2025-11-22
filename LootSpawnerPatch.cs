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

namespace DropRateSetting
{
    /// <summary>
    /// Harmony补丁类，用于修改LootSpawner行为
    /// </summary>
    [HarmonyPatch(typeof(LootSpawner))]
    public class LootSpawnerPatch
    {
        /// <summary>
        /// 重新生成所有场景中的战利品
        /// </summary>
        public static void RespawnLoot()
        {
            try
            {
                // 记录当前爆率设置
                int dropRateMultiplier = ModConfigDropRateManager.DropRateMultiplier;
                int randomCountMultiplier = ModConfigDropRateManager.RandomCountMultiplier;
                bool isModEnabled = ModConfigDropRateManager.IsModEnabled;
                
                // 检查是否在基地场景，避免在基地刷新物品
                Scene currentScene = SceneManager.GetActiveScene();
                // 假设基地场景名称包含"Base"或类似的标识
                if (currentScene.name.Contains("Base") || currentScene.name.Contains("Hub") || currentScene.name.Contains("Menu"))
                {
                    WriteDetailedLog($"[DropRateSetting] 检测到基地场景 {currentScene.name}，跳过箱子刷新以避免物品掉落");
                    return;
                }
                
                // 查找场景中所有的LootSpawner实例
                // LootSpawner[] spawners = GameObject.FindObjectsOfType<LootSpawner>();
                // 使用Resources.FindObjectsOfTypeAll查找所有LootSpawner实例，包括未激活的
                LootSpawner[] spawners = Resources.FindObjectsOfTypeAll<LootSpawner>();
                
                // 过滤掉不在当前场景中的对象
                List<LootSpawner> spawnersInScene = new List<LootSpawner>();
                foreach (LootSpawner spawner in spawners)
                {
                    // 只处理当前场景中激活的对象
                    if (spawner.gameObject.scene == currentScene && spawner.gameObject.activeInHierarchy)
                    {
                        spawnersInScene.Add(spawner);
                    }
                }
                spawners = spawnersInScene.ToArray();
                
                // 同时查找所有InteractableLootbox实例（直接生成的箱子）
                InteractableLootbox[] lootBoxes = Resources.FindObjectsOfTypeAll<InteractableLootbox>();
                List<InteractableLootbox> lootBoxesInScene = new List<InteractableLootbox>();
                foreach (InteractableLootbox lootBox in lootBoxes)
                {
                    // 只处理当前场景中激活的对象
                    if (lootBox.gameObject.scene == currentScene && lootBox.gameObject.activeInHierarchy)
                    {
                        lootBoxesInScene.Add(lootBox);
                    }
                }
                WriteDetailedLog($"[DropRateSetting] 找到 {spawners.Length} 个LootSpawner 和 {lootBoxesInScene.Count} 个InteractableLootbox");
                
                // 限制处理的箱子数量，避免性能问题（减少到50个）
                const int maxBoxesToProcess = 50;
                int totalBoxCount = spawners.Length + lootBoxesInScene.Count;
                if (totalBoxCount > maxBoxesToProcess)
                {
                    WriteDetailedLog($"[DropRateSetting] 箱子数量过多 ({totalBoxCount})，限制处理数量为 {maxBoxesToProcess}");
                    // 优先处理LootSpawner，然后是InteractableLootbox
                    if (spawners.Length > maxBoxesToProcess)
                    {
                        // 如果LootSpawner就超过限制，只处理部分LootSpawner
                        spawners = spawners.Take(maxBoxesToProcess).ToArray();
                        lootBoxesInScene.Clear();
                    }
                    else
                    {
                        // 处理所有LootSpawner，然后补充部分InteractableLootbox
                        int remainingSlots = maxBoxesToProcess - spawners.Length;
                        if (lootBoxesInScene.Count > remainingSlots)
                        {
                            lootBoxesInScene = lootBoxesInScene.Take(remainingSlots).ToList();
                        }
                    }
                }
                
                // 记录日志信息
                int totalSpawners = spawners.Length + lootBoxesInScene.Count;
                int spawnedCount = 0;        // 已生成箱子数（箱子已生成）
                int unspawnedCount = 0;      // 未生成箱子数（箱子未生成）
                int modifiedCount = 0;       // 实际被修改箱子数
                
                // 添加到文件日志
                WriteDetailedLog($"[DropRateSetting] 当前爆率设置 - Mod启用: {isModEnabled}, 爆率倍数: {dropRateMultiplier}, 数量倍数: {randomCountMultiplier}");
                WriteDetailedLog($"[DropRateSetting] 开始重新生成战利品，总箱子数: {totalSpawners}");
                
                // 如果没有找到箱子，记录警告信息
                if (totalSpawners == 0)
                {
                    WriteDetailedLog("[DropRateSetting] 未找到任何LootSpawner实例");
                }
                
                // 重新生成每个LootSpawner的战利品
                foreach (LootSpawner spawner in spawners)
                {
                    // 处理LootSpawner
                    if (ProcessLootSpawner(spawner, ref spawnedCount, ref unspawnedCount, ref modifiedCount))
                    {
                        // modifiedCount++ 已在ProcessLootSpawner中增加
                    }
                }
                
                // 重新生成每个InteractableLootbox的战利品
                foreach (InteractableLootbox lootBox in lootBoxesInScene)
                {
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
            }
            catch (Exception ex)
            {
                WriteDetailedLog($"[DropRateSetting] 重新生成战利品时出错: {ex.Message}");
                WriteDetailedLog($"[DropRateSetting] StackTrace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// 处理LootSpawner对象
        /// </summary>
        /// <param name="spawner">LootSpawner对象</param>
        /// <param name="spawnedCount">已生成计数</param>
        /// <param name="unspawnedCount">未生成计数</param>
        /// <param name="modifiedCount">修改计数</param>
        /// <returns>是否成功处理</returns>
        private static bool ProcessLootSpawner(LootSpawner spawner, ref int spawnedCount, ref int unspawnedCount, ref int modifiedCount)
        {
            try
            {
                // 检查这个战利品点是否已经生成过物品
                var getKeyMethod = spawner.GetType().GetMethod("GetKey", BindingFlags.NonPublic | BindingFlags.Instance);
                if (getKeyMethod == null) 
                {
                    WriteDetailedLog("[DropRateSetting] 无法获取GetKey方法");
                    return false;
                }
                
                int key = (int)getKeyMethod.Invoke(spawner, null);
                
                // 检查MultiSceneCore.Instance是否可用
                if (MultiSceneCore.Instance == null) 
                {
                    WriteDetailedLog("[DropRateSetting] MultiSceneCore.Instance不可用");
                    return false;
                }
                
                // 检查该位置是否已经生成过箱子
                // inLevelData[key] 为 true 表示箱子已生成
                bool isSpawned = MultiSceneCore.Instance.inLevelData.ContainsKey(key);
                bool isGenerated = false;
                
                // 检查箱子是否已生成
                object generatedFlag;
                if (MultiSceneCore.Instance.inLevelData.TryGetValue(key, out generatedFlag) && generatedFlag is bool)
                {
                    isGenerated = (bool)generatedFlag;
                }
                
                // 统计箱子状态
                if (isSpawned && isGenerated)
                {
                    spawnedCount++; // 箱子已生成
                }
                else
                {
                    unspawnedCount++; // 箱子未生成
                }
                
                // 添加详细的箱子状态日志
                WriteDetailedLog($"[DropRateSetting] 箱子状态 - Key: {key}, 已存在: {isSpawned}, 已生成: {isGenerated}");
                
                // 检查是否有关联的InteractableLootbox并且已被打开
                bool isLootBoxOpened = false;
                Points points = spawner.GetComponent<Points>();
                if (points != null)
                {
                    // 查找是否有InteractableLootbox使用相同的Points
                    InteractableLootbox[] lootBoxes = Resources.FindObjectsOfTypeAll<InteractableLootbox>();
                    Scene currentScene = SceneManager.GetActiveScene();
                    foreach (InteractableLootbox lootBox in lootBoxes)
                    {
                        if (lootBox.gameObject.scene == currentScene && lootBox.GetComponent<Points>() == points)
                        {
                            // 检查这个箱子是否已被打开过
                            Inventory inventory = lootBox.Inventory;
                            if (inventory != null && LootView.HasInventoryEverBeenLooted(inventory))
                            {
                                isLootBoxOpened = true;
                                WriteDetailedLog($"[DropRateSetting] 箱子 {key} 已被打开，跳过重新生成");
                                break;
                            }
                        }
                    }
                }
                
                // 如果箱子已经打开过，就不要重新生成
                if (isLootBoxOpened) 
                {
                    return false;
                }
                
                // 清除已存在的物品
                if (points != null)
                {
                    // 记录清除前的子对象数量
                    int childCount = points.transform.childCount;
                    WriteDetailedLog($"[DropRateSetting] 清除箱子 {key} 中的物品，原有物品数: {childCount}");
                    
                    // 移除所有子对象（已生成的物品）
                    for (int i = points.transform.childCount - 1; i >= 0; i--)
                    {
                        GameObject.Destroy(points.transform.GetChild(i).gameObject);
                    }
                }
                
                // 重新生成物品（使用当前的掉率设置）
                // var setupMethod = spawner.GetType().GetMethod("Setup", BindingFlags.Public | BindingFlags.Instance);
                // if (setupMethod != null)
                // {
                //     WriteDetailedLog($"[DropRateSetting] 调用箱子 {key} 的Setup方法重新生成物品");
                //     setupMethod.Invoke(spawner, null);
                // }
                // else
                // {
                //     WriteDetailedLog($"[DropRateSetting] 无法获取Setup方法");
                // }
                
                // 尝试直接调用Setup方法
                try
                {
                    WriteDetailedLog($"[DropRateSetting] 调用箱子 {key} 的Setup方法重新生成物品");
                    UniTask task = spawner.Setup();
                    // 不等待异步任务完成，直接继续
                }
                catch (Exception setupEx)
                {
                    WriteDetailedLog($"[DropRateSetting] 调用Setup方法时出错: {setupEx.Message}");
                    
                    // 如果Setup方法调用失败，尝试其他方式重新生成
                    // 例如：查找关联的LootBoxLoader并调用其Setup方法
                    try
                    {
                        LootBoxLoader loader = spawner.GetComponent<LootBoxLoader>();
                        if (loader != null)
                        {
                            WriteDetailedLog($"[DropRateSetting] 通过LootBoxLoader重新生成箱子 {key}");
                            loader.StartSetup();
                        }
                        else
                        {
                            // 如果没有LootBoxLoader，尝试禁用/启用组件方式
                            spawner.enabled = false;
                            spawner.enabled = true;
                            WriteDetailedLog($"[DropRateSetting] 通过禁用/启用组件方式重新生成箱子 {key}");
                        }
                    }
                    catch (Exception loaderEx)
                    {
                        WriteDetailedLog($"[DropRateSetting] 通过LootBoxLoader重新生成箱子时出错: {loaderEx.Message}");
                        
                        // 最后的备选方案
                        try
                        {
                            spawner.enabled = false;
                            spawner.enabled = true;
                            WriteDetailedLog($"[DropRateSetting] 通过组件启用方式重新生成箱子 {key}");
                        }
                        catch (Exception enableEx)
                        {
                            WriteDetailedLog($"[DropRateSetting] 通过组件启用方式重新生成箱子时出错: {enableEx.Message}");
                        }
                    }
                }
                
                // 标记该箱子已生成
                if (MultiSceneCore.Instance != null)
                {
                    MultiSceneCore.Instance.inLevelData[key] = true;
                    WriteDetailedLog($"[DropRateSetting] 标记箱子 {key} 为已生成");
                }
                else
                {
                    WriteDetailedLog($"[DropRateSetting] 无法标记箱子 {key} 为已生成，MultiSceneCore.Instance为null");
                }
                
                WriteDetailedLog($"[DropRateSetting] 箱子 {key} 重新生成完成");
                modifiedCount++; // 增加修改计数
                return true;
            }
            catch (Exception ex)
            {
                WriteDetailedLog($"[DropRateSetting] 处理LootSpawner时出错: {ex.Message}");
                return false;
            }
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
                // 检查箱子是否已被打开
                if (lootBox.Looted)
                {
                    WriteDetailedLog($"[DropRateSetting] InteractableLootbox 已被打开，跳过重新生成");
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
                    return false;
                }
                
                // 检查MultiSceneCore.Instance是否可用
                if (MultiSceneCore.Instance == null) 
                {
                    WriteDetailedLog("[DropRateSetting] MultiSceneCore.Instance不可用");
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
                
                // 统计箱子状态
                if (isSpawned && isGenerated)
                {
                    spawnedCount++; // 箱子已生成
                }
                else
                {
                    unspawnedCount++; // 箱子未生成
                }
                
                // 添加详细的箱子状态日志
                WriteDetailedLog($"[DropRateSetting] InteractableLootbox状态 - Key: {key}, 已存在: {isSpawned}, 已生成: {isGenerated}");
                
                // 清除箱子中的物品
                Inventory inventory = lootBox.Inventory;
                if (inventory != null)
                {
                    // 记录清除前的物品数量
                    int itemCount = inventory.Content.Count;
                    WriteDetailedLog($"[DropRateSetting] 清除InteractableLootbox {key} 中的物品，原有物品数: {itemCount}");
                    
                    // 清除所有物品
                    inventory.Content.Clear();
                }
                
                // 重新生成物品
                try
                {
                    // 查找关联的LootBoxLoader并重新生成
                    LootBoxLoader loader = lootBox.GetComponent<LootBoxLoader>();
                    if (loader != null)
                    {
                        WriteDetailedLog($"[DropRateSetting] 通过LootBoxLoader重新生成InteractableLootbox {key}");
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
                return true;
            }
            catch (Exception ex)
            {
                WriteDetailedLog($"[DropRateSetting] 处理InteractableLootbox时出错: {ex.Message}");
                return false;
            }
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