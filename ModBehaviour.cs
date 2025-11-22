using System;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement; // 添加SceneManager引用

namespace DropRateSetting
{
    /// <summary>
    /// Mod主类
    /// 负责初始化Harmony补丁和配置系统
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
        private int lastDropRateMultiplier = 1;
        private int lastRandomCountMultiplier = 1;
        private bool lastIsModEnabled = false;
        
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
            WriteDebugLog($"[DropRateSetting] 场景加载完成: {scene.name}, 模式: {mode}");
        }

        /// <summary>
        /// 当场景开始卸载时调用
        /// </summary>
        private void OnSceneUnloaded(Scene scene)
        {
            // 标记场景正在切换
            isSwitchingScene = true;
            WriteDebugLog($"[DropRateSetting] 场景开始卸载: {scene.name}");
        }

        /// <summary>
        /// 每帧更新时调用
        /// 可用于处理实时逻辑
        /// </summary>
        private void Update()
        {
            // 检查是否正在切换场景
            if (isSwitchingScene)
                return;
                
            // 检查是否正在进行重新生成操作
            if (LootSpawnerPatch.IsRespawning())
                return;
                
            // 检查是否正在切换场景（通过LootSpawnerPatch检查）
            if (LootSpawnerPatch.IsSceneChanging())
                return;
                
            // 检查是否启用了Mod功能
            if (!ModConfigDropRateManager.IsModEnabled)
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
                    lootBoxHighQualityChanceMultiplierField.SetValue(LevelConfig.Instance, (float)ModConfigDropRateManager.DropRateMultiplier);
                }

                // 设置战利品箱物品数量
                if (lootboxItemCountMultiplierField != null)
                {
                    lootboxItemCountMultiplierField.SetValue(LevelConfig.Instance, (float)ModConfigDropRateManager.RandomCountMultiplier);
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
                lastDropRateMultiplier != ModConfigDropRateManager.DropRateMultiplier ||
                lastRandomCountMultiplier != ModConfigDropRateManager.RandomCountMultiplier ||
                lastIsModEnabled != ModConfigDropRateManager.IsModEnabled;
                
            if (shouldUpdate)
            {
                WriteDebugLog($"[DropRateSetting] 检测到配置变化 - " +
                         $"爆率: {lastDropRateMultiplier} -> {ModConfigDropRateManager.DropRateMultiplier}, " +
                         $"数量: {lastRandomCountMultiplier} -> {ModConfigDropRateManager.RandomCountMultiplier}, " +
                         $"启用: {lastIsModEnabled} -> {ModConfigDropRateManager.IsModEnabled}");
                
                // 设置延迟刷新标志
                pendingRespawn = true;
                respawnTimer = 0f;
                
                // 更新跟踪变量
                lastDropRateMultiplier = ModConfigDropRateManager.DropRateMultiplier;
                lastRandomCountMultiplier = ModConfigDropRateManager.RandomCountMultiplier;
                lastIsModEnabled = ModConfigDropRateManager.IsModEnabled;
            }
        }
        
        /// <summary>
        /// 处理延迟刷新
        /// </summary>
        private void HandlePendingRespawn()
        {
            if (pendingRespawn && !isSwitchingScene && !LootSpawnerPatch.IsRespawning() && !LootSpawnerPatch.IsSceneChanging())
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
        /// 将调试日志写入文件
        /// </summary>
        /// <param name="message">日志消息</param>
        private void WriteDebugLog(string message)
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
        
        /// <summary>
        /// 将爆率更新写入日志文件
        /// </summary>
        /// <param name="dropRate">高品质掉落率</param>
        /// <param name="itemCount">物品数量</param>
        private void WriteDropRateLog(float dropRate, float itemCount)
        {
            try
            {
                // 获取DLL所在目录
                string dllPath = Assembly.GetExecutingAssembly().Location;
                string logDirectory = Path.GetDirectoryName(dllPath);
                // 统一使用统一日志文件
                string logFilePath = Path.Combine(logDirectory, "DropRateSetting.log");
                
                // 创建日志内容
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                                $"爆率更新 - 高品质掉落率: {dropRate}, 物品数量: {itemCount}" + 
                                Environment.NewLine;
                
                // 写入日志文件
                File.AppendAllText(logFilePath, logEntry);
            }
            catch (Exception ex)
            {
                WriteDebugLog($"[DropRateSetting] 写入爆率日志文件时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 缓存反射字段以提高性能
        /// </summary>
        private void CacheReflectionFields()
        {
            WriteDebugLog($"[DropRateSetting] 开始缓存反射字段");
            
            // 获取并缓存高品质物品掉落概率字段
            lootBoxHighQualityChanceMultiplierField = typeof(LevelConfig)
                .GetField("lootBoxHighQualityChanceMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
            
            if (lootBoxHighQualityChanceMultiplierField != null)
            {
                WriteDebugLog($"[DropRateSetting] 成功获取lootBoxHighQualityChanceMultiplierField");
            }
            else
            {
                WriteDebugLog($"[DropRateSetting] 无法获取lootBoxHighQualityChanceMultiplierField");
            }

            // 获取并缓存战利品箱物品数量字段
            lootboxItemCountMultiplierField = typeof(LevelConfig)
                .GetField("lootboxItemCountMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
                
            if (lootboxItemCountMultiplierField != null)
            {
                WriteDebugLog($"[DropRateSetting] 成功获取lootboxItemCountMultiplierField");
            }
            else
            {
                WriteDebugLog($"[DropRateSetting] 无法获取lootboxItemCountMultiplierField");
            }
                
            // 即使字段为null也标记为已缓存，避免重复尝试获取
            fieldsCached = true;
            WriteDebugLog($"[DropRateSetting] 反射字段缓存完成");
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