using System;
using HarmonyLib;
using UnityEngine;
using System.IO;
using System.Reflection;

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

        /// <summary>
        /// 当组件启用时调用
        /// 加载Harmony库
        /// </summary>
        private void OnEnable()
        {
            HarmonyLoad.HarmonyLoad.Load0Harmony();
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
        /// 每帧更新时调用
        /// 可用于处理实时逻辑
        /// </summary>
        private void Update()
        {
            // 检查LevelConfig.Instance是否可用
            if (LevelConfig.Instance != null)
            {
                // 获取并修改高品质物品掉落概率字段
                var lootBoxHighQualityChanceMultiplierField = typeof(LevelConfig)
                    .GetField("lootBoxHighQualityChanceMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
                if (lootBoxHighQualityChanceMultiplierField != null)
                {
                    lootBoxHighQualityChanceMultiplierField.SetValue(LevelConfig.Instance, (float)ModConfigDropRateManager.DropRateMultiplier);
                }

                // 获取并修改战利品箱物品数量字段
                var lootboxItemCountMultiplierField = typeof(LevelConfig)
                    .GetField("lootboxItemCountMultiplier", BindingFlags.Instance | BindingFlags.NonPublic);
                if (lootboxItemCountMultiplierField != null)
                {
                    lootboxItemCountMultiplierField.SetValue(LevelConfig.Instance, (float)ModConfigDropRateManager.RandomCountMultiplier);
                }
            }
        }

        /// <summary>
        /// 当组件被禁用时调用
        /// 移除所有Harmony补丁
        /// </summary>
        private void OnDisable()
        {
            try
            {
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
            if (configManager != null)
            {
                Destroy(configManager.gameObject);
            }
        }
    }
}