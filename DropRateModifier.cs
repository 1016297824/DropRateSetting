using System;
using HarmonyLib;
using Duckov.Utilities;
using UnityEngine;
using System.IO;

namespace DropRateSetting
{
    /// <summary>
    /// 战利品掉落率修改器类
    /// 用于增加敌人被击败后掉落物品的概率和数量
    /// </summary>
    public class DropRateModifier
    {
        /// <summary>
        /// 全局掉落率倍数
        /// 1.0为默认掉落率，10.0为十倍掉落率，以此类推
        /// </summary>
        public static float DropRateMultiplier = 10.0f;
        
        /// <summary>
        /// 日志文件路径
        /// </summary>
        private static string logPath => Path.Combine(Path.GetDirectoryName(typeof(DropRateModifier).Assembly.Location), "DropRateSetting.log");
        
        /// <summary>
        /// 修改LootSpawner.Start方法来增加基础掉落概率
        /// </summary>
        [HarmonyPatch(typeof(LootSpawner), "Start")]
        public class LootSpawnerStartPatch
        {
            /// <summary>
            /// 在LootSpawner.Start方法执行前调用
            /// 增加基础生成概率
            /// </summary>
            /// <param name="__instance">LootSpawner实例</param>
            public static void Prefix(LootSpawner __instance)
            {
                try
                {
                    float originalSpawnChance = __instance.spawnChance;
                    // 增加基础生成概率，但不超过1.0（100%）
                    __instance.spawnChance = Mathf.Min(1.0f, __instance.spawnChance * DropRateMultiplier);
                    LogMessage($"[DropRateSetting] 修改了spawnChance: {__instance.spawnChance:F2} (原始: {originalSpawnChance:F2}), 倍数: {DropRateMultiplier:F2}");
                }
                catch (Exception ex)
                {
                    LogMessage($"[DropRateSetting] 修改spawnChance时出错: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 修改LootSpawner.Setup方法来增加掉落物品数量
        /// </summary>
        [HarmonyPatch(typeof(LootSpawner), "Setup")]
        public class LootSpawnerSetupPatch
        {
            /// <summary>
            /// 在LootSpawner.Setup方法执行前调用
            /// 增加生成物品的数量范围
            /// </summary>
            /// <param name="__instance">LootSpawner实例</param>
            /// <param name="__state">用于保存和恢复原始值的状态参数</param>
            public static void Prefix(LootSpawner __instance, ref Vector2Int __state)
            {
                try
                {
                    // 通过Traverse访问私有字段randomCount
                    Vector2Int randomCount = Traverse.Create(__instance).Field("randomCount").GetValue<Vector2Int>();
                    // 保存原始值以便后续恢复
                    __state = randomCount;
                    
                    // 增加生成数量范围
                    if (__instance.randomGenrate)
                    {
                        Vector2Int newRandomCount = new Vector2Int(
                            Mathf.Max(1, Mathf.RoundToInt(randomCount.x * DropRateMultiplier)),
                            Mathf.Max(randomCount.x, Mathf.RoundToInt(randomCount.y * DropRateMultiplier))
                        );
                        
                        // 设置新的随机数量范围
                        Traverse.Create(__instance).Field("randomCount").SetValue(newRandomCount);
                        LogMessage($"[DropRateSetting] 修改了randomCount: {newRandomCount} (原始: {randomCount}), 倍数: {DropRateMultiplier:F2}");
                    }
                    else
                    {
                        LogMessage($"[DropRateSetting] LootSpawner不使用随机生成: {__instance.name}");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"[DropRateSetting] 修改randomCount时出错: {ex.Message}");
                }
            }
            
            /// <summary>
            /// 在LootSpawner.Setup方法执行后调用
            /// 恢复原始的随机数量范围值
            /// </summary>
            /// <param name="__instance">LootSpawner实例</param>
            /// <param name="__state">之前保存的原始值</param>
            public static void Postfix(LootSpawner __instance, Vector2Int __state)
            {
                // 不再恢复原始值，因为这会影响掉落效果
                // 注释掉恢复代码，让修改后的值保持生效
                /*
                try
                {
                    // 恢复原始值，确保不影响游戏其他部分的逻辑
                    Traverse.Create(__instance).Field("randomCount").SetValue(__state);
                    LogMessage($"[DropRateSetting] 恢复了randomCount: {__state}");
                }
                catch (Exception ex)
                {
                    LogMessage($"[DropRateSetting] 恢复randomCount时出错: {ex.Message}");
                }
                */
                LogMessage($"[DropRateSetting] 保持修改后的randomCount值");
            }
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