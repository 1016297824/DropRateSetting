using UnityEngine;

namespace DropRateSetting
{
    /// <summary>
    /// 掉落率配置类（已弃用，保留以防止兼容性问题）
    /// 现在使用ModConfig系统进行配置管理
    /// 
    /// 此类已弃用，仅保留以确保向后兼容性。
    /// 新的配置管理通过ModConfigDropRateManager类实现。
    /// </summary>
    public class DropRateConfig : MonoBehaviour
    {
        /// <summary>
        /// 获取实例（已弃用，仅用于向后兼容）
        /// 
        /// 该属性始终返回null，指示调用方应使用新的ModConfig系统。
        /// </summary>
        public static DropRateConfig? Instance
        {
            get
            {
                // 返回null以指示应使用新的ModConfig系统
                return null;
            }
        }
        
        /// <summary>
        /// 掉落率倍数（已弃用，仅用于向后兼容）
        /// 
        /// 此字段不再使用，新的掉落率倍数存储在ModConfigDropRateManager.SpawnChanceMultiplier和ModConfigDropRateManager.RandomCountMultiplier中。
        /// </summary>
        public float dropRateMultiplier = 2.0f;
        
        /// <summary>
        /// 当组件被唤醒时调用
        /// 
        /// 销毁此组件，因为我们现在使用ModConfig系统进行配置管理。
        /// 输出警告信息，提示开发者使用新的配置系统。
        /// </summary>
        private void Awake()
        {
            // 销毁此组件，因为我们现在使用ModConfig系统
            Debug.LogWarning("[DropRateSetting] DropRateConfig已弃用，请使用ModConfig系统");
            Destroy(this);
        }
    }
}