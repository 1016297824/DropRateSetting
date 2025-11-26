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
    /// 战利品生成器补丁类
    /// 用于修改游戏默认的战利品生成行为，实现自定义掉率和数量控制
    /// </summary>
    [HarmonyPatch(typeof(LootSpawner))]
    public class LootSpawnerPatch
    {
        // 跟踪重新生成操作状态的静态变量
        private static bool isRespawning = false;
        private static int pendingRespawns = 0;
        
        /// <summary>
        /// 重新生成当前场景中的所有战利品箱
        /// 根据配置的掉率和数量设置，重新生成所有未被打开的战利品箱内容
        /// </summary>
        public static void RespawnLoot()
        {
            try
            {
                // 如果正在切换场景，则取消本次重新生成操作
                // 已由ModBehaviour中的isSwitchingScene处理
                
                // 设置重新生成操作正在进行的标志
                isRespawning = true;
                pendingRespawns = 0;
                
                // 获取当前的掉率和数量配置
                float dropRateMultiplier = ModConfigDropRateManager.DropRateMultiplier;
                float randomCountMultiplier = ModConfigDropRateManager.RandomCountMultiplier;
                bool isModEnabled = ModConfigDropRateManager.IsModEnabled;
                
                // 检查是否在基地场景，基地场景中不重新生成战利品
                if (LevelManager.Instance != null && LevelManager.Instance.IsBaseLevel)
                {
                    isRespawning = false;
                    return;
                }
                
                // 检查MultiSceneCore实例是否可用，这是生成战利品的必要条件
                if (MultiSceneCore.Instance == null)
                {
                    isRespawning = false;
                    return;
                }
                
                // 查找当前场景中所有可交互的战利品箱实例
                InteractableLootbox[] lootBoxes = Resources.FindObjectsOfTypeAll<InteractableLootbox>();
                List<InteractableLootbox> lootBoxesInScene = new List<InteractableLootbox>();
                Scene currentScene = SceneManager.GetActiveScene();
                foreach (InteractableLootbox lootBox in lootBoxes)
                {
                    // 只处理当前场景中激活且可见的战利品箱
                    if (lootBox.gameObject.scene == currentScene && lootBox.gameObject.activeInHierarchy)
                    {
                        lootBoxesInScene.Add(lootBox);
                    }
                }
                

                
                // 初始化统计计数器
                int totalSpawners = lootBoxesInScene.Count;
                int spawnedCount = 0;        // 已生成箱子数（箱子已生成）
                int unspawnedCount = 0;      // 未生成箱子数（箱子未生成）
                int modifiedCount = 0;       // 实际被修改箱子数
                

                
                // 如果当前场景中没有找到战利品箱，直接返回
                if (totalSpawners == 0)
                {
                    isRespawning = false;
                    return;
                }
                
                // 设置需要重新生成的战利品箱总数
                pendingRespawns = lootBoxesInScene.Count;
                
                // 遍历并重新生成每个战利品箱的内容
                foreach (InteractableLootbox lootBox in lootBoxesInScene)
                {
                    // 处理单个战利品箱的重新生成
                    if (ProcessInteractableLootbox(lootBox, ref spawnedCount, ref unspawnedCount, ref modifiedCount))
                    {
                        // modifiedCount++ 已在ProcessInteractableLootbox中增加
                    }
                }
                

                
                // 重新生成操作完成，重置标志位
                isRespawning = false;
            }
            catch (Exception)
            {
                isRespawning = false;
            }
        }
        
        /// <summary>
        /// 检查是否正在进行战利品重新生成操作
        /// </summary>
        /// <returns>正在进行重新生成返回true，否则返回false</returns>
        public static bool IsRespawning()
        {
            return isRespawning;
        }
        
        /// <summary>
        /// 处理单个战利品箱的重新生成
        /// </summary>
        /// <param name="lootBox">要处理的战利品箱对象</param>
        /// <param name="spawnedCount">已生成箱子计数引用</param>
        /// <param name="unspawnedCount">未生成箱子计数引用</param>
        /// <param name="modifiedCount">已修改箱子计数引用</param>
        /// <returns>处理成功返回true，否则返回false</returns>
        private static bool ProcessInteractableLootbox(InteractableLootbox lootBox, ref int spawnedCount, ref int unspawnedCount, ref int modifiedCount)
        {
            try
            {
                // 检查是否在处理过程中发生了场景切换
                // 已由ModBehaviour中的isSwitchingScene处理
                
                // 检查是否在基地场景，基地中不处理战利品箱
                if (LevelManager.Instance != null && LevelManager.Instance.IsBaseLevel)
                {
                    pendingRespawns--;
                    return false;
                }
                
                // 检查MultiSceneCore实例是否仍然可用
                if (MultiSceneCore.Instance == null)
                {
                    pendingRespawns--;
                    return false;
                }
                
                // 检查战利品箱是否已被玩家打开
                if (lootBox.Looted)
                {
                    pendingRespawns--;
                    return false;
                }
                
                // 获取战利品箱的唯一标识key（通过反射调用私有方法）
                int key = 0;
                var getKeyMethod = typeof(InteractableLootbox).GetMethod("GetKey", BindingFlags.NonPublic | BindingFlags.Instance);
                if (getKeyMethod != null)
                {
                    key = (int)getKeyMethod.Invoke(lootBox, null);
                }
                else
                {
                    pendingRespawns--;
                    return false;
                }
                
                // 再次检查MultiSceneCore实例是否仍然可用
                if (MultiSceneCore.Instance == null) 
                {
                    pendingRespawns--;
                    return false;
                }
                
                // 检查该位置是否已经有战利品箱生成记录
                bool isSpawned = MultiSceneCore.Instance.inLevelData.ContainsKey(key);
                bool isGenerated = false;
                
                // 检查战利品箱是否已生成内容
                object generatedFlag;
                if (MultiSceneCore.Instance.inLevelData.TryGetValue(key, out generatedFlag) && generatedFlag is bool)
                {
                    isGenerated = (bool)generatedFlag;
                }
                
                // 统计战利品箱状态 - 修复逻辑：只要箱子存在就认为是已生成
                if (isSpawned)
                {
                    spawnedCount++; // 箱子已生成
                }
                else
                {
                    unspawnedCount++; // 箱子未生成
                }
                

                
                // 检查战利品箱的物品容器是否存在
                Inventory inventory = lootBox.Inventory;
                if (inventory != null)
                {
                    // 记录重新生成前的物品数量
                    int itemCount = inventory.Content.Count;
                    
                    // 只有当箱子中有物品时才进行清理，避免对空箱子执行不必要的操作
                    if (itemCount > 0)
                    {
                        // 使用反射调用更安全的清理方法，避免触发可能的问题
                        // 尝试通过反射调用Inventory的DestroyAllContent方法（如果存在）
                        var destroyMethod = typeof(Inventory).GetMethod("DestroyAllContent", BindingFlags.Public | BindingFlags.Instance);
                        if (destroyMethod != null)
                        {
                            try
                            {
                                destroyMethod.Invoke(inventory, null);
                            }
                            catch (Exception)
                            {
                                // 如果DestroyAllContent调用失败，回退到Clear方法
                                inventory.Content.Clear();
                            }
                        }
                        else
                        {
                            // 如果没有DestroyAllContent方法，直接使用Clear方法
                            inventory.Content.Clear();
                        }
                    }
                }
                 
                // 重新生成战利品箱内的物品
                try
                {
                    // 在调用StartSetup之前再次检查MultiSceneCore实例是否仍然可用
                    if (MultiSceneCore.Instance == null) 
                    {
                        pendingRespawns--;
                        return false;
                    }
                    
                    // 查找关联的战利品箱加载器并重新生成内容
                    LootBoxLoader loader = lootBox.GetComponent<LootBoxLoader>();
                    if (loader != null)
                    {
                        // 重置加载器状态并重新生成战利品
                        loader.StartSetup();
                    }
                    else
                    {

                    }
                }
                catch (Exception)
                {

                }
                
                // 标记该战利品箱已生成
                if (MultiSceneCore.Instance != null)
                {
                    MultiSceneCore.Instance.inLevelData[key] = true;
                }
                else
                {

                }
                
                modifiedCount++; // 增加修改计数
                pendingRespawns--;
                return true;
            }
            catch (Exception)
            {
                pendingRespawns--;
                return false;
            }
        }
        
        /// <summary>
        /// 检查是否还有待处理的战利品箱重新生成操作
        /// </summary>
        /// <returns>还有待处理的重新生成操作返回true，否则返回false</returns>
        public static bool HasPendingRespawns()
        {
            return pendingRespawns > 0;
        }


    }
}