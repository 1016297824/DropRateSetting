using System;
using HarmonyLib;
using Duckov.Utilities;
using UnityEngine;
using System.IO;
using System.Reflection;

namespace DropRateSetting
{
    /// <summary>
    /// 战利品掉落率修改器类
    /// 用于增加敌人被击败后掉落物品的概率和数量
    /// </summary>
    public class DropRateModifier
    {
        /// <summary>
        /// 全局爆率倍数
        /// 1为默认爆率，10为十倍爆率，以此类推
        /// </summary>
        public static float SpawnChanceMultiplier = 1f;
        
        /// <summary>
        /// 全局爆出个数倍数
        /// 1为默认个数，5为五倍个数，以此类推
        /// </summary>
        public static int RandomCountMultiplier = 1;
        
        /// <summary>
        /// 日志文件路径
        /// </summary>
        private static string logPath => Path.Combine(Path.GetDirectoryName(typeof(DropRateModifier).Assembly.Location), "DropRateSetting.log");
        
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