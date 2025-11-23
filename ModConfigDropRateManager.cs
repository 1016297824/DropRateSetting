using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using Duckov.Modding;
using System.Reflection;

namespace DropRateSetting
{
    /// <summary>
    /// 掉落率配置管理器
    /// 通过ModConfig系统管理游戏的掉落率和物品数量配置
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
        
        // 配置项键名常量
        /// <summary>
        /// 爆率配置项的键名
        /// </summary>
        private const string SPAWN_CHANCE_KEY = "SpawnChanceMultiplier";
        /// <summary>
        /// 爆出个数配置项的键名
        /// </summary>
        private const string RANDOM_COUNT_KEY = "RandomCountMultiplier";
        /// <summary>
        /// 启用Mod功能的配置项键名
        /// </summary>
        private const string ENABLE_MOD_KEY = "EnableMod";
        /// <summary>
        /// 即时刷新按钮的配置项键名
        /// </summary>
        private const string REFRESH_LOOT_KEY = "RefreshLoot";
        
        // 预拼接的配置键名，避免重复字符串拼接
        private static readonly string FULL_SPAWN_CHANCE_KEY = $"{MOD_NAME}_{SPAWN_CHANCE_KEY}";
        private static readonly string FULL_RANDOM_COUNT_KEY = $"{MOD_NAME}_{RANDOM_COUNT_KEY}";
        private static readonly string FULL_ENABLE_MOD_KEY = $"{MOD_NAME}_{ENABLE_MOD_KEY}";
        private static readonly string FULL_REFRESH_LOOT_KEY = $"{MOD_NAME}_{REFRESH_LOOT_KEY}";
        
        // 配置项描述信息常量
        /// <summary>
        /// 爆率配置项的描述信息
        /// </summary>
        private const string SPAWN_CHANCE_DESC = "爆率 (控制高品质物品掉落概率)";
        /// <summary>
        /// 爆出个数配置项的描述信息
        /// </summary>
        private const string RANDOM_COUNT_DESC = "爆出个数 (控制战利品箱物品数量)";
        /// <summary>
        /// 启用Mod功能的配置项描述信息
        /// </summary>
        private const string ENABLE_MOD_DESC = "是否启用本mod (防止与其它修改掉率mod冲突)";
        
        /// <summary>
        /// 即时刷新按钮的配置项描述信息
        /// </summary>
        private const string REFRESH_LOOT_DESC = "爆率修改后即时刷新战利品箱";
        
        /// <summary>
        /// 本地配置文件路径
        /// </summary>
        private static string persistentConfigPath => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DropRateSettingConfig.json");
        
        /// <summary>
        /// 当前爆率倍数
        /// </summary>
        public static float DropRateMultiplier { get; private set; } = 1.0f;
        
        /// <summary>
        /// 当前爆出个数倍数
        /// </summary>
        public static float RandomCountMultiplier { get; private set; } = 1.0f;
        
        /// <summary>
        /// 是否启用Mod功能
        /// </summary>
        public static bool IsModEnabled { get; private set; } = false;
        
        /// <summary>
        /// 即时刷新按钮状态
        /// </summary>
        public static bool RefreshLoot { get; private set; } = false;
        
        // 保存上一次的值用于比较
        private static float previousDropRateMultiplier = 1.0f;
        private static float previousRandomCountMultiplier = 1.0f;
        private static bool previousIsModEnabled = false;
        private static bool previousRefreshLoot = false;
        
        // 添加ConfigData结构体定义
        [Serializable]
        private struct ConfigData
        {
            public float spawnChanceMultiplier;
            public float randomCountMultiplier;
            public bool isModEnabled;
            public bool refreshLoot;
        }
        
        /// <summary>
        /// 标记是否已经初始化过配置项
        /// </summary>
        private static bool isConfigInitialized = false;
        
        /// <summary>
        /// 当组件被唤醒时调用
        /// 确保此类为单例模式并初始化配置
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
                InitializeConfig();
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
                // 如果ModConfig不可用，从本地配置加载
                LoadLocalConfig();
                
                // 即使ModConfig不可用，也要确保生成初始配置文件
                EnsureInitialConfigFile();
                return;
            }
            
            // 避免重复初始化配置项
            if (isConfigInitialized)
            {
                return;
            }
            
            // 先从本地配置加载，获取当前设置
            LoadLocalConfig();
            
            // 保存加载后的本地配置值
            float localDropRate = DropRateMultiplier;
            float localRandomCount = RandomCountMultiplier;
            bool localIsModEnabled = IsModEnabled;
            bool localRefreshLoot = RefreshLoot;
            
            // 注册配置项变更事件
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnConfigChanged);
            
            // 添加即时刷新按钮
            ModConfigAPI.SafeAddBoolDropdownList(
                MOD_NAME,
                REFRESH_LOOT_KEY,
                REFRESH_LOOT_DESC,
                false // 默认值为false
            );
            
            // 添加爆出个数滑条输入框配置项（在下面）
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                RANDOM_COUNT_KEY,
                RANDOM_COUNT_DESC,
                typeof(float),
                RandomCountMultiplier, // 使用当前值而不是硬编码默认值
                new Vector2(0.1f, 10f) // 滑条范围 0.1f~10f
            );
            
            // 添加爆率滑条输入框配置项（在上面）
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                SPAWN_CHANCE_KEY,
                SPAWN_CHANCE_DESC,
                typeof(float),
                DropRateMultiplier, // 使用当前值而不是硬编码默认值
                new Vector2(1f, 200f) // 滑条范围 1f~200f
            );
            
            // 添加启用Mod功能的布尔下拉列表配置项（放在最上面）
            ModConfigAPI.SafeAddBoolDropdownList(
                MOD_NAME,
                ENABLE_MOD_KEY,
                ENABLE_MOD_DESC,
                IsModEnabled // 使用当前值而不是硬编码默认值
            );
            
            isConfigInitialized = true;
            
            // 从ModConfig加载最新配置
            LoadConfig();
            
            // 检查ModConfig中的配置是否与本地配置不同
            bool configChanged = Math.Abs(DropRateMultiplier - localDropRate) > 0.001f || 
                               Math.Abs(RandomCountMultiplier - localRandomCount) > 0.001f || 
                               IsModEnabled != localIsModEnabled ||
                               RefreshLoot != localRefreshLoot;
            
            // 如果配置不同，将ModConfig的配置同步到本地配置文件
            if (configChanged)
            {
                SaveLocalConfig();
            }
            
            // 设置初始值用于比较
            previousDropRateMultiplier = DropRateMultiplier;
            previousRandomCountMultiplier = RandomCountMultiplier;
            previousIsModEnabled = IsModEnabled;
            previousRefreshLoot = RefreshLoot;
        }
        
        /// <summary>
        /// 确保生成初始配置文件
        /// </summary>
        private void EnsureInitialConfigFile()
        {
            // 如果配置文件不存在，则创建初始配置文件
            if (!File.Exists(persistentConfigPath))
            {
                SaveLocalConfig();
            }
        }
        
        /// <summary>
        /// 配置变更时的回调函数
        /// 当用户在ModConfig界面中更改设置时调用
        /// </summary>
        /// <param name="key">变更的配置项键名</param>
        private void OnConfigChanged(string key)
        {
            if (key == FULL_SPAWN_CHANCE_KEY || key == FULL_RANDOM_COUNT_KEY || key == FULL_ENABLE_MOD_KEY || key == FULL_REFRESH_LOOT_KEY)
            {

                
                LoadConfig();
                
                // 只有在值真正发生变化时才保存到本地文件
                if (Math.Abs(DropRateMultiplier - previousDropRateMultiplier) > 0.001f || 
                    Math.Abs(RandomCountMultiplier - previousRandomCountMultiplier) > 0.001f || 
                    IsModEnabled != previousIsModEnabled ||
                    RefreshLoot != previousRefreshLoot)
                {
                    SaveLocalConfig();
                    previousDropRateMultiplier = DropRateMultiplier;
                    previousRandomCountMultiplier = RandomCountMultiplier;
                    previousIsModEnabled = IsModEnabled;
                    previousRefreshLoot = RefreshLoot;
                }
            }
        }
        
        /// <summary>
        /// 从ModConfig加载配置
        /// </summary>
        private void LoadConfig()
        {
            if (ModConfigAPI.IsAvailable())
            {
                // 保存当前值用于比较
                float previousSpawnChance = DropRateMultiplier;
                float previousRandomCount = RandomCountMultiplier;
                bool previousEnabled = IsModEnabled;
                
                // 从ModConfig加载新值
                DropRateMultiplier = ModConfigAPI.SafeLoad<float>(MOD_NAME, SPAWN_CHANCE_KEY, DropRateMultiplier);
                RandomCountMultiplier = ModConfigAPI.SafeLoad<float>(MOD_NAME, RANDOM_COUNT_KEY, RandomCountMultiplier);
                IsModEnabled = ModConfigAPI.SafeLoad<bool>(MOD_NAME, ENABLE_MOD_KEY, IsModEnabled);
                RefreshLoot = ModConfigAPI.SafeLoad<bool>(MOD_NAME, REFRESH_LOOT_KEY, RefreshLoot);
                
                // 如果值发生了变化，保存到本地配置
                if (Math.Abs(DropRateMultiplier - previousSpawnChance) > 0.001f || 
                    Math.Abs(RandomCountMultiplier - previousRandomCount) > 0.001f || 
                    IsModEnabled != previousEnabled ||
                    RefreshLoot != previousRefreshLoot)
                {
                    SaveLocalConfig();
                    previousDropRateMultiplier = DropRateMultiplier;
                    previousRandomCountMultiplier = RandomCountMultiplier;
                    previousIsModEnabled = IsModEnabled;
                    previousRefreshLoot = RefreshLoot;
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
                // 确保目录存在
                string directory = Path.GetDirectoryName(persistentConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                // 创建格式化后的JSON字符串，确保浮点数保留一位小数
                var configData = new ConfigData { 
                    spawnChanceMultiplier = DropRateMultiplier,
                    randomCountMultiplier = RandomCountMultiplier,
                    isModEnabled = IsModEnabled,
                    refreshLoot = RefreshLoot
                };
                
                // 手动构建JSON字符串以控制浮点数格式
                string json = "{\n" +
                    $"  \"spawnChanceMultiplier\": {configData.spawnChanceMultiplier:F1},\n" +
                    $"  \"randomCountMultiplier\": {configData.randomCountMultiplier:F1},\n" +
                    $"  \"isModEnabled\": {configData.isModEnabled.ToString().ToLower()},\n" +
                    $"  \"refreshLoot\": {configData.refreshLoot.ToString().ToLower()}\n" +
                    "}";
                
                File.WriteAllText(persistentConfigPath, json);
            }
            catch (Exception)
            {

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
                    IsModEnabled = configData.isModEnabled;
                    RefreshLoot = configData.refreshLoot;
                }
                else
                {
                    // 如果配置文件不存在，确保生成初始配置文件
                    EnsureInitialConfigFile();
                }
            }
            catch (Exception)
            {
                // 如果加载失败，确保生成初始配置文件
                EnsureInitialConfigFile();
            }
        }
        
        /// <summary>
        /// 获取当前版本信息
        /// </summary>
        /// <returns>ModConfig版本信息字符串</returns>
        public static string GetVersionInfo()
        {
            // 缓存ModConfig可用性检查结果
            bool isModConfigAvailable = ModConfigAPI.IsAvailable();
            if (isModConfigAvailable)
            {
                return $"ModConfig版本: {ModConfigAPI.GetVersionInfo()}";
            }
            return "ModConfig不可用";
        }
        
    }
}