using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Duckov.Modding;

namespace DropRateSetting
{
    /// <summary>
    /// 通过ModConfig系统管理的掉落率配置
    /// </summary>
    public class ModConfigDropRateManager : MonoBehaviour
    {
        private static ModConfigDropRateManager? _instance;
        /// <summary>
        /// 获取ModConfigDropRateManager单例实例
        /// </summary>
        public static ModConfigDropRateManager? Instance => _instance;
        
        /// <summary>
        /// Mod名称，用于在ModConfig中标识此Mod
        /// </summary>
        private const string MOD_NAME = "DropRateSetting";
        
        /// <summary>
        /// 掉落率倍数配置项的键名
        /// </summary>
        // private const string DROP_RATE_KEY = "DropRateMultiplier"; // 已弃用
        
        /// <summary>
        /// 爆率配置项的键名
        /// </summary>
        private const string SPAWN_CHANCE_KEY = "SpawnChanceMultiplier";
        
        /// <summary>
        /// 爆出个数配置项的键名
        /// </summary>
        private const string RANDOM_COUNT_KEY = "RandomCountMultiplier";
        
        /// <summary>
        /// 掉落率倍数配置项的描述信息
        /// </summary>
        private const string DROP_RATE_DESC = "掉落率倍数 (1.0 = 默认, 2.0 = 2倍掉落率)";
        
        /// <summary>
        /// 爆率配置项的描述信息
        /// </summary>
        private const string SPAWN_CHANCE_DESC = "爆率 (控制高品质物品掉落概率)";
        
        /// <summary>
        /// 爆出个数配置项的描述信息
        /// </summary>
        private const string RANDOM_COUNT_DESC = "爆出个数 (控制战利品箱物品数量)";
        
        /// <summary>
        /// 本地配置文件路径
        /// </summary>
        private static string persistentConfigPath => Path.Combine(Application.streamingAssetsPath, "DropRateSettingConfig.txt");
        
        /// <summary>
        /// 日志文件路径
        /// </summary>
        private static string logPath => Path.Combine(Path.GetDirectoryName(typeof(ModConfigDropRateManager).Assembly.Location), "DropRateSetting.log");
        
        /// <summary>
        /// 当前掉落率倍数
        /// </summary>
        // public static float CurrentDropRateMultiplier { get; private set; } = 2.0f; // 已弃用
        
        /// <summary>
        /// 当前爆率倍数
        /// </summary>
        public static int DropRateMultiplier { get; private set; } = 1;
        
        /// <summary>
        /// 当前爆出个数倍数
        /// </summary>
        public static int RandomCountMultiplier { get; private set; } = 1;
        
        // 添加ConfigData结构体定义
        [Serializable]
        private struct ConfigData
        {
            public int spawnChanceMultiplier;
            public int randomCountMultiplier;
        }
        
        /// <summary>
        /// 标记是否已经初始化过配置项
        /// </summary>
        private static bool isConfigInitialized = false;
        
        /// <summary>
        /// 当组件被唤醒时调用
        /// 确保此类为单例模式
        /// </summary>
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeConfig();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 当组件启用时调用
        /// </summary>
        private void OnEnable()
        {
            // 添加Mod激活事件监听
            ModManager.OnModActivated += OnModActivated;
        }
        
        /// <summary>
        /// 当组件被禁用时调用
        /// </summary>
        private void OnDisable()
        {
            // 移除Mod激活事件监听
            ModManager.OnModActivated -= OnModActivated;
            
            // 移除配置变更事件监听
            ModConfigAPI.SafeRemoveOnOptionsChangedDelegate(OnConfigChanged);
        }
        
        /// <summary>
        /// Mod激活事件处理
        /// </summary>
        private void OnModActivated(Duckov.Modding.ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if (info.name == ModConfigAPI.ModConfigName)
            {
                LogMessage("[DropRateSetting] ModConfig已激活!");
                InitializeConfig();
                LoadConfig();
            }
        }
        
        /// <summary>
        /// 初始化ModConfig配置
        /// 注册配置项并加载当前设置
        /// </summary>
        private void InitializeConfig()
        {
            // 检查ModConfig是否可用
            if (!ModConfigAPI.IsAvailable())
            {
                LogMessage("[DropRateSetting] ModConfig不可用，使用默认设置");
                LoadLocalConfig();
                DropRateModifier.SpawnChanceMultiplier = DropRateMultiplier;
                DropRateModifier.RandomCountMultiplier = RandomCountMultiplier;
                return;
            }
            
            // 避免重复初始化配置项
            if (isConfigInitialized)
            {
                LoadConfig();
                return;
            }
            
            // 注册配置项变更事件
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnConfigChanged);
            
            // 添加爆率滑条输入框配置项（在上面）
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                SPAWN_CHANCE_KEY,
                SPAWN_CHANCE_DESC,
                typeof(int),
                1, // 默认值
                new Vector2(1, 100) // 滑条范围
            );
            
            // 添加爆出个数滑条输入框配置项（在下面）
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                RANDOM_COUNT_KEY,
                RANDOM_COUNT_DESC,
                typeof(int),
                1, // 默认值
                new Vector2(1, 5) // 滑条范围
            );
            
            isConfigInitialized = true;
            
            // 加载当前配置值
            LoadConfig();
        }
        
        /// <summary>
        /// 配置变更时的回调函数
        /// 当用户在ModConfig界面中更改设置时调用
        /// </summary>
        /// <param name="key">变更的配置项键名</param>
        private void OnConfigChanged(string key)
        {
            if (key == $"{MOD_NAME}_{SPAWN_CHANCE_KEY}" || key == $"{MOD_NAME}_{RANDOM_COUNT_KEY}")
            {
                LoadConfig();
                SaveLocalConfig();
            }
        }
        
        /// <summary>
        /// 从ModConfig加载配置
        /// </summary>
        private void LoadConfig()
        {
            if (ModConfigAPI.IsAvailable())
            {
                float previousSpawnChance = DropRateMultiplier;
                int previousRandomCount = RandomCountMultiplier;
                
                DropRateMultiplier = ModConfigAPI.SafeLoad<int>(MOD_NAME, SPAWN_CHANCE_KEY, 10);
                RandomCountMultiplier = ModConfigAPI.SafeLoad<int>(MOD_NAME, RANDOM_COUNT_KEY, 1);
                
                DropRateModifier.SpawnChanceMultiplier = DropRateMultiplier;
                DropRateModifier.RandomCountMultiplier = RandomCountMultiplier;
                
                // 只有当值真正改变时才记录日志
                if (previousSpawnChance != DropRateMultiplier)
                {
                    LogMessage($"[DropRateSetting] 爆率已更新为: {DropRateMultiplier}");
                }
                
                if (previousRandomCount != RandomCountMultiplier)
                {
                    LogMessage($"[DropRateSetting] 爆出个数已更新为: {RandomCountMultiplier}");
                }
            }
        }
        
        /// <summary>
        /// 保存配置到本地文件
        /// </summary>
        private void SaveLocalConfig()
        {
            try
            {
                string json = JsonUtility.ToJson(new ConfigData { 
                    spawnChanceMultiplier = DropRateMultiplier,
                    randomCountMultiplier = RandomCountMultiplier
                }, true);
                File.WriteAllText(persistentConfigPath, json);
                // 配置已保存到本地文件，不显示冒泡框
            }
            catch
            {
                // 静默处理错误，不显示冒泡框
            }
        }
        
        /// <summary>
        /// 从本地文件加载配置
        /// </summary>
        private void LoadLocalConfig()
        {
            try
            {
                if (File.Exists(persistentConfigPath))
                {
                    string json = File.ReadAllText(persistentConfigPath);
                    ConfigData configData = JsonUtility.FromJson<ConfigData>(json);
                    DropRateMultiplier = configData.spawnChanceMultiplier;
                    RandomCountMultiplier = configData.randomCountMultiplier;
                    DropRateModifier.SpawnChanceMultiplier = DropRateMultiplier;
                    DropRateModifier.RandomCountMultiplier = RandomCountMultiplier;
                    LogMessage($"[DropRateSetting] 从本地文件加载配置: 爆率={DropRateMultiplier}, 爆出个数={RandomCountMultiplier}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"[DropRateSetting] 加载本地配置失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取当前版本信息
        /// </summary>
        /// <returns>ModConfig版本信息字符串</returns>
        public static string GetVersionInfo()
        {
            if (ModConfigAPI.IsAvailable())
            {
                return $"ModConfig版本: {ModConfigAPI.GetVersionInfo()}";
            }
            return "ModConfig不可用";
        }
        
        /// <summary>
        /// 记录日志到本地文件
        /// </summary>
        /// <param name="message">要记录的消息</param>
        private static void LogMessage(string message)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                File.AppendAllText(logPath, logEntry);
            }
            catch
            {
                // 静默处理日志记录错误
            }
        }
    }
}