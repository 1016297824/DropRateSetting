using System;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement; // 添加SceneManager引用

namespace DropRateSetting
{
    /// <summary>
    /// 掉落率设置Mod主类
    /// 负责初始化Harmony补丁系统和配置管理器
    /// </summary>
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        /// <summary>
        /// Harmony实例，用于应用代码补丁
        /// </summary>
        public Harmony harmony = new Harmony("DropRateSetting");
        
        /// <summary>
        /// Mod配置管理器实例
        /// </summary>
        private ModConfigDropRateManager? configManager;
        
        // 缓存反射字段以提高性能
        private static FieldInfo? lootBoxHighQualityChanceMultiplierField;
        private static FieldInfo? lootboxItemCountMultiplierField;
        private bool fieldsCached = false;
        
        // 保存上一次的配置值用于比较
        private float lastDropRateMultiplier = 1.0f;
        private float lastRandomCountMultiplier = 1.0f;
        private bool lastIsModEnabled = false;
        private bool lastRefreshLoot = false;
        
        // 添加延迟刷新相关变量
        private bool pendingRespawn = false;
        private float respawnDelay = 0.1f; // 0.1秒延迟
        private float respawnTimer = 0f;
        
        // 添加场景切换相关变量
        private bool isSwitchingScene = false;

        /// <summary>
        /// 当组件启用时调用
        /// 加载Harmony库
        /// </summary>
        private void OnEnable()
        {
            HarmonyLoad.HarmonyLoad.Load0Harmony();
            // 注册场景切换事件
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// 当游戏对象第一次被激活时调用
        /// 应用所有Harmony补丁并初始化配置系统
        /// </summary>
        private void Start()
        {
            // 应用所有Harmony补丁
            try
            {
                harmony.PatchAll();
            }
            catch
            {
                // 静默处理错误
            }
            
            // 初始化配置管理器
            GameObject configManagerObject = new GameObject("ModConfigDropRateManager");
            configManager = configManagerObject.AddComponent<ModConfigDropRateManager>();
        }

        /// <summary>
        /// 场景加载完成时调用
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 场景加载完成后，重置场景切换标志
            isSwitchingScene = false;
        }

        /// <summary>
        /// 当场景开始卸载时调用
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            // 标记场景正在切换
            isSwitchingScene = true;
        }

        /// <summary>
        /// 每帧更新时调用
        /// 可用于处理实时逻辑
        /// </summary>
        private void Update()
        {
            // 检查是否启用了Mod功能
            if (!ModConfigDropRateManager.IsModEnabled)
                return;
                
            // 检查是否正在切换场景
            if (isSwitchingScene)
                return;
                
            // 检查是否正在进行重新生成操作
            if (LootSpawnerPatch.IsRespawning())
                return;
                
            // 检查是否正在切换场景（通过LootSpawnerPatch检查）
            if (LootSpawnerPatch.IsSceneChanging())
                return;
                
            // 检查LevelConfig.Instance是否可用
            if (LevelConfig.Instance != null)
            {
                // 缓存反射字段以提高性能
                if (!fieldsCached)
                {
                    CacheReflectionFields();
                }
                
                // 只有当字段不为null时才设置值，避免不必要的操作
                // 设置高品质物品掉落概率
                if (lootBoxHighQualityChanceMultiplierField != null)
                {
                    lootBoxHighQualityChanceMultiplierField.SetValue(LevelConfig.Instance, ModConfigDropRateManager.DropRateMultiplier);
                }

                // 设置战利品箱物品数量
                if (lootboxItemCountMultiplierField != null)
                {
                    lootboxItemCountMultiplierField.SetValue(LevelConfig.Instance, ModConfigDropRateManager.RandomCountMultiplier);
                }
            }
            
            // 检查配置是否发生变化
            CheckForConfigChanges();
            
            // 处理延迟刷新
            HandlePendingRespawn();
        }
        
        /// <summary>
        /// 检查配置是否有变化
        /// </summary>
        private void CheckForConfigChanges()
        {
            // 检查是否需要更新设置
            bool shouldUpdate = 
                Math.Abs(lastDropRateMultiplier - ModConfigDropRateManager.DropRateMultiplier) > 0.001f ||
                Math.Abs(lastRandomCountMultiplier - ModConfigDropRateManager.RandomCountMultiplier) > 0.001f ||
                lastIsModEnabled != ModConfigDropRateManager.IsModEnabled ||
                lastRefreshLoot != ModConfigDropRateManager.RefreshLoot;
                
            if (shouldUpdate)
            {
                // 设置延迟刷新标志
                pendingRespawn = true;
                respawnTimer = 0f;
                
                // 更新跟踪变量
                lastDropRateMultiplier = ModConfigDropRateManager.DropRateMultiplier;
                lastRandomCountMultiplier = ModConfigDropRateManager.RandomCountMultiplier;
                lastIsModEnabled = ModConfigDropRateManager.IsModEnabled;
                lastRefreshLoot = ModConfigDropRateManager.RefreshLoot;
            }
        }
        
        /// <summary>
        /// 处理延迟刷新
        /// </summary>
        private void HandlePendingRespawn()
        {
            // 只有当即时刷新按钮为true时才处理延迟刷新
            if (ModConfigDropRateManager.RefreshLoot && pendingRespawn && !isSwitchingScene && !LootSpawnerPatch.IsRespawning() && !LootSpawnerPatch.IsSceneChanging())
            {
                respawnTimer += Time.deltaTime;
                if (respawnTimer >= respawnDelay)
                {
                    // 检查是否还有待处理的重新生成操作
                    if (!LootSpawnerPatch.HasPendingRespawns())
                    {
                        // 重新生成战利品箱子
                        LootSpawnerPatch.RespawnLoot();
                        pendingRespawn = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// 缓存反射字段以提高性能
        /// </summary>
        private void CacheReflectionFields()
        {
            // 获取并缓存高品质物品掉落概率字段
            lootBoxHighQualityChanceMultiplierField = typeof(LevelConfig)
                .GetField("lootBoxHighQualityChanceMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
            
            // 获取并缓存战利品箱物品数量字段
            lootboxItemCountMultiplierField = typeof(LevelConfig)
                .GetField("lootboxItemCountMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
                
            // 即使字段为null也标记为已缓存，避免重复尝试获取
            fieldsCached = true;
        }

        /// <summary>
        /// 当组件被禁用时调用
        /// 移除所有Harmony补丁
        /// </summary>
        private void OnDisable()
        {
            try
            {
                // 取消注册场景切换事件
                SceneManager.sceneLoaded -= OnSceneLoaded;
                SceneManager.sceneUnloaded -= OnSceneUnloaded;
                harmony?.UnpatchAll("DropRateSetting");
            }
            catch
            {
                // 静默处理错误
            }
        }
        
        /// <summary>
        /// 当Mod被销毁时调用
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            // 取消注册场景切换事件
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            
            if (configManager != null)
            {
                Destroy(configManager.gameObject);
            }
        }
    }
}