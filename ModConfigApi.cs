using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

//替换为你的mod命名空间, 防止多个同名ModConfigAPI冲突
namespace DropRateSetting { 
/// <summary>
/// ModConfig 安全接口封装类 - 提供不抛异常的静态接口
/// ModConfig Safe API Wrapper Class - Provides non-throwing static interfaces
/// 
/// 该类通过反射方式调用ModConfig系统的方法，提供安全的API接口。
/// 所有方法都包含错误处理，确保即使ModConfig未加载也不会导致异常。
/// </summary>
public static class ModConfigAPI
{
    /// <summary>
    /// ModConfig模块的名称，用于识别ModConfig系统
    /// </summary>
    public static string ModConfigName = "ModConfig";

    //Ensure this match the number of ModConfig.ModBehaviour.VERSION
    //这里确保版本号与ModConfig.ModBehaviour.VERSION匹配
    /// <summary>
    /// ModConfig API版本号，必须与ModConfig.ModBehaviour.VERSION保持一致
    /// 用于确保API兼容性
    /// </summary>
    private const int ModConfigVersion = 1;

    /// <summary>
    /// 日志标签前缀，包含版本信息
    /// </summary>
    private static string TAG = $"ModConfig_v{ModConfigVersion}";

    /// <summary>
    /// ModConfig.ModBehaviour类型的反射引用
    /// </summary>
    private static Type modBehaviourType;
    
    /// <summary>
    /// ModConfig.OptionsManager_Mod类型的反射引用
    /// </summary>
    private static Type optionsManagerType;
    
    /// <summary>
    /// API是否已初始化标志
    /// </summary>
    public static bool isInitialized = false;
    
    /// <summary>
    /// 版本兼容性检查是否已完成标志
    /// </summary>
    private static bool versionChecked = false;
    
    /// <summary>
    /// ModConfig版本是否兼容标志
    /// </summary>
    private static bool isVersionCompatible = false;

    /// <summary>
    /// 检查版本兼容性
    /// Check version compatibility
    /// 
    /// 通过反射获取ModConfig的VERSION字段并与本地版本号进行比较，
    /// 确保API与ModConfig系统版本兼容。
    /// </summary>
    /// <returns>如果版本兼容返回true，否则返回false</returns>
    private static bool CheckVersionCompatibility()
    {
        if (versionChecked)
            return isVersionCompatible;

        try
        {
            // 尝试获取 ModConfig 的版本号
            // Try to get ModConfig version number
            FieldInfo versionField = modBehaviourType.GetField("VERSION", BindingFlags.Public | BindingFlags.Static);
            if (versionField != null && versionField.FieldType == typeof(int))
            {
                int modConfigVersion = (int)versionField.GetValue(null);
                isVersionCompatible = (modConfigVersion == ModConfigVersion);

                if (!isVersionCompatible)
                {
                    Debug.LogError($"[{TAG}] 版本不匹配！API版本: {ModConfigVersion}, ModConfig版本: {modConfigVersion}");
                    return false;
                }

                Debug.Log($"[{TAG}] 版本检查通过: {ModConfigVersion}");
                versionChecked = true;
                return true;
            }
            else
            {
                // 如果找不到版本字段，发出警告但继续运行（向后兼容）
                // If version field not found, warn but continue (backward compatibility)
                Debug.LogWarning($"[{TAG}] 未找到版本信息字段，跳过版本检查");
                isVersionCompatible = true;
                versionChecked = true;
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 版本检查失败: {ex.Message}");
            isVersionCompatible = false;
            versionChecked = true;
            return false;
        }
    }

    /// <summary>
    /// 初始化 ModConfigAPI，检查必要的函数是否存在
    /// Initialize ModConfigAPI, check if necessary functions exist
    /// 
    /// 通过反射查找ModConfig系统中的类型和方法，确保API可以正常工作。
    /// 包括版本检查和必要方法存在性验证。
    /// </summary>
    /// <returns>如果初始化成功返回true，否则返回false</returns>
    public static bool Initialize()
    {
        try
        {
            if (isInitialized)
                return true;

            // 获取 ModBehaviour 类型
            // Get ModBehaviour type
            modBehaviourType = FindTypeInAssemblies("ModConfig.ModBehaviour");
            if (modBehaviourType == null)
            {
                Debug.LogWarning($"[{TAG}] ModConfig.ModBehaviour 类型未找到，ModConfig 可能未加载");
                return false;
            }

            // 获取 OptionsManager_Mod 类型
            // Get OptionsManager_Mod type
            optionsManagerType = FindTypeInAssemblies("ModConfig.OptionsManager_Mod");
            if (optionsManagerType == null)
            {
                Debug.LogWarning($"[{TAG}] ModConfig.OptionsManager_Mod 类型未找到");
                return false;
            }

            // 检查版本兼容性
            // Check version compatibility
            if (!CheckVersionCompatibility())
            {
                Debug.LogWarning($"[{TAG}] ModConfig version mismatch!!!");
                return false;
            }

            // 检查必要的静态方法是否存在
            // Check if necessary static methods exist
            string[] requiredMethods = {
                "AddDropdownList",
                "AddInputWithSlider",
                "AddBoolDropdownList",
                "AddOnOptionsChangedDelegate",
                "RemoveOnOptionsChangedDelegate",
            };

            foreach (string methodName in requiredMethods)
            {
                MethodInfo method = modBehaviourType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    Debug.LogError($"[{TAG}] 必要方法 {methodName} 未找到");
                    return false;
                }
            }

            isInitialized = true;
            Debug.Log($"[{TAG}] ModConfigAPI 初始化成功");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 初始化失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 在所有已加载的程序集中查找类型
    /// 
    /// 遍历当前AppDomain中所有已加载的程序集，查找指定名称的类型。
    /// 主要用于查找ModConfig系统中的类型。
    /// </summary>
    /// <param name="typeName">要查找的类型全名</param>
    /// <returns>找到的类型，如果未找到则返回null</returns>
    private static Type FindTypeInAssemblies(string typeName)
    {
        try
        {
            // 获取当前域中的所有程序集
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    // 检查程序集名称是否包含 ModConfig
                    if (assembly.FullName.Contains("ModConfig"))
                    {
                        Debug.Log($"[{TAG}] 找到 ModConfig 相关程序集: {assembly.FullName}");
                    }

                    // 尝试在该程序集中查找类型
                    Type type = assembly.GetType(typeName);
                    if (type != null)
                    {
                        Debug.Log($"[{TAG}] 在程序集 {assembly.FullName} 中找到类型 {typeName}");
                        return type;
                    }
                }
                catch (Exception)
                {
                    // 忽略单个程序集的查找错误
                    continue;
                }
            }

            // 记录所有已加载的程序集用于调试
            Debug.LogWarning($"[{TAG}] 在所有程序集中未找到类型 {typeName}，已加载程序集数量: {assemblies.Length}");
            foreach (var assembly in assemblies.Where(a => a.FullName.Contains("ModConfig")))
            {
                Debug.Log($"[{TAG}] ModConfig 相关程序集: {assembly.FullName}");
            }

            return null;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 程序集扫描失败: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 安全地添加选项变更事件委托
    /// Safely add options changed event delegate
    /// 
    /// 注册一个配置变更事件处理函数，当用户在ModConfig界面中修改配置时会触发此事件。
    /// </summary>
    /// <param name="action">事件处理委托，参数为变更的选项键名</param>
    /// <returns>是否成功添加</returns>
    public static bool SafeAddOnOptionsChangedDelegate(Action<string> action)
    {
        if (!Initialize())
            return false;

        if (action == null)
        {
            Debug.LogWarning($"[{TAG}] 不能添加空的事件委托");
            return false;
        }

        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddOnOptionsChangedDelegate", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { action });

            Debug.Log($"[{TAG}] 成功添加选项变更事件委托");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 添加选项变更事件委托失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 安全地移除选项变更事件委托
    /// Safely remove options changed event delegate
    /// 
    /// 移除之前注册的配置变更事件处理函数。
    /// </summary>
    /// <param name="action">要移除的事件处理委托</param>
    /// <returns>是否成功移除</returns>
    public static bool SafeRemoveOnOptionsChangedDelegate(Action<string> action)
    {
        if (!Initialize())
            return false;

        if (action == null)
        {
            Debug.LogWarning($"[{TAG}] 不能移除空的事件委托");
            return false;
        }

        try
        {
            MethodInfo method = modBehaviourType.GetMethod("RemoveOnOptionsChangedDelegate", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { action });

            Debug.Log($"[{TAG}] 成功移除选项变更事件委托");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 移除选项变更事件委托失败: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 安全地添加下拉列表配置项
    /// Safely add dropdown list configuration item
    /// 
    /// 在ModConfig界面中添加一个下拉列表配置项，用户可以通过下拉菜单选择预定义的选项。
    /// </summary>
    /// <param name="modName">Mod名称，用于标识配置项归属</param>
    /// <param name="key">配置项键名，用于唯一标识配置项</param>
    /// <param name="description">配置项描述，显示在界面中的说明文字</param>
    /// <param name="options">选项字典，键为显示文本，值为对应的选项值</param>
    /// <param name="valueType">值的类型</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>是否成功添加</returns>
    public static bool SafeAddDropdownList(string modName, string key, string description, System.Collections.Generic.SortedDictionary<string, object> options, Type valueType, object defaultValue)
    {
        key = $"{modName}_{key}";

        if (!Initialize())
            return false;

        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddDropdownList", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { modName, key, description, options, valueType, defaultValue });

            Debug.Log($"[{TAG}] 成功添加下拉列表: {modName}.{key}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 添加下拉列表失败 {modName}.{key}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 安全地添加带滑条的输入框配置项
    /// Safely add input box with slider configuration item
    /// 
    /// 在ModConfig界面中添加一个带滑条的输入框配置项，用户可以通过输入框或滑条调整数值。
    /// </summary>
    /// <param name="modName">Mod名称，用于标识配置项归属</param>
    /// <param name="key">配置项键名，用于唯一标识配置项</param>
    /// <param name="description">配置项描述，显示在界面中的说明文字</param>
    /// <param name="valueType">值的类型</param>
    /// <param name="defaultValue">默认值</param>
    /// <param name="sliderRange">滑条范围，如果为null则不显示滑条</param>
    /// <returns>是否成功添加</returns>
    public static bool SafeAddInputWithSlider(string modName, string key, string description, Type valueType, object defaultValue, UnityEngine.Vector2? sliderRange = null)
    {
        key = $"{modName}_{key}";

        if (!Initialize())
            return false;

        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddInputWithSlider", BindingFlags.Public | BindingFlags.Static);

            // 处理可空参数
            // Handle nullable parameters
            object[] parameters = sliderRange.HasValue ?
                new object[] { modName, key, description, valueType, defaultValue, sliderRange.Value } :
                new object[] { modName, key, description, valueType, defaultValue, null };

            method.Invoke(null, parameters);

            Debug.Log($"[{TAG}] 成功添加滑条输入框: {modName}.{key}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 添加滑条输入框失败 {modName}.{key}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 安全地添加布尔下拉列表配置项
    /// Safely add boolean dropdown list configuration item
    /// 
    /// 在ModConfig界面中添加一个布尔值下拉列表配置项，用户可以在True/False之间选择。
    /// </summary>
    /// <param name="modName">Mod名称，用于标识配置项归属</param>
    /// <param name="key">配置项键名，用于唯一标识配置项</param>
    /// <param name="description">配置项描述，显示在界面中的说明文字</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>是否成功添加</returns>
    public static bool SafeAddBoolDropdownList(string modName, string key, string description, bool defaultValue)
    {
        key = $"{modName}_{key}";

        if (!Initialize())
            return false;

        try
        {
            MethodInfo method = modBehaviourType.GetMethod("AddBoolDropdownList", BindingFlags.Public | BindingFlags.Static);
            method.Invoke(null, new object[] { modName, key, description, defaultValue });

            Debug.Log($"[{TAG}] 成功添加布尔下拉列表: {modName}.{key}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 添加布尔下拉列表失败 {modName}.{key}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 安全地加载配置值
    /// Safely load configuration value
    /// 
    /// 从ModConfig系统中加载指定键名的配置值。
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="mod_name">Mod名称</param>
    /// <param name="key">配置项键名</param>
    /// <param name="defaultValue">默认值，如果配置项不存在则返回此值</param>
    /// <returns>加载的值或默认值</returns>
    public static T SafeLoad<T>(string mod_name, string key, T defaultValue = default(T))
    {
        key = $"{mod_name}_{key}";

        if (!Initialize())
            return defaultValue;

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning($"[{TAG}] 配置键不能为空");
            return defaultValue;
        }

        try
        {
            MethodInfo loadMethod = optionsManagerType.GetMethod("Load", BindingFlags.Public | BindingFlags.Static);
            if (loadMethod == null)
            {
                Debug.LogError($"[{TAG}] 未找到 OptionsManager_Mod.Load 方法");
                return defaultValue;
            }

            // 获取泛型方法
            MethodInfo genericLoadMethod = loadMethod.MakeGenericMethod(typeof(T));
            object result = genericLoadMethod.Invoke(null, new object[] { key, defaultValue! });

            Debug.Log($"[{TAG}] 成功加载配置: {key} = {result}");
            return (T)result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 加载配置失败 {key}: {ex.Message}");
            return defaultValue;
        }
    }

    /// <summary>
    /// 安全地保存配置值
    /// Safely save configuration value
    /// 
    /// 将配置值保存到ModConfig系统中。
    /// </summary>
    /// <typeparam name="T">值的类型</typeparam>
    /// <param name="mod_name">Mod名称</param>
    /// <param name="key">配置项键名</param>
    /// <param name="value">要保存的值</param>
    /// <returns>是否保存成功</returns>
    public static bool SafeSave<T>(string mod_name, string key, T value)
    {
        key = $"{mod_name}_{key}";

        if (!Initialize())
            return false;

        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning($"[{TAG}] 配置键不能为空");
            return false;
        }

        try
        {
            MethodInfo saveMethod = optionsManagerType.GetMethod("Save", BindingFlags.Public | BindingFlags.Static);
            if (saveMethod == null)
            {
                Debug.LogError($"[{TAG}] 未找到 OptionsManager_Mod.Save 方法");
                return false;
            }

            // 获取泛型方法
            MethodInfo genericSaveMethod = saveMethod.MakeGenericMethod(typeof(T));
            genericSaveMethod.Invoke(null, new object[] { key, value });

            Debug.Log($"[{TAG}] 成功保存配置: {key} = {value}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[{TAG}] 保存配置失败 {key}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 检查 ModConfig 是否可用
    /// Check if ModConfig is available
    /// 
    /// 检查ModConfig系统是否已加载并可用。
    /// </summary>
    /// <returns>如果ModConfig可用返回true，否则返回false</returns>
    public static bool IsAvailable()
    {
        return Initialize();
    }

    /// <summary>
    /// 获取 ModConfig 版本信息（如果存在）
    /// Get ModConfig version information (if exists)
    /// 
    /// 获取ModConfig系统的版本信息，用于调试和兼容性检查。
    /// </summary>
    /// <returns>ModConfig版本信息字符串</returns>
    public static string GetVersionInfo()
    {
        if (!Initialize())
            return "ModConfig 未加载 | ModConfig not loaded";

        try
        {
            // 尝试获取版本信息（如果 ModBehaviour 有相关字段或属性）
            // Try to get version information (if ModBehaviour has related fields or properties)
            FieldInfo versionField = modBehaviourType.GetField("VERSION", BindingFlags.Public | BindingFlags.Static);
            if (versionField != null && versionField.FieldType == typeof(int))
            {
                int modConfigVersion = (int)versionField.GetValue(null);
                string compatibility = (modConfigVersion == ModConfigVersion) ? "兼容" : "不兼容";
                return $"ModConfig v{modConfigVersion} (API v{ModConfigVersion}, {compatibility})";
            }

            PropertyInfo versionProperty = modBehaviourType.GetProperty("VERSION", BindingFlags.Public | BindingFlags.Static);
            if (versionProperty != null)
            {
                object versionValue = versionProperty.GetValue(null);
                return versionValue?.ToString() ?? "未知版本 | Unknown version";
            }

            return "ModConfig 已加载（版本信息不可用） | ModConfig loaded (version info unavailable)";
        }
        catch
        {
            return "ModConfig 已加载（版本检查失败） | ModConfig loaded (version check failed)";
        }
    }

    /// <summary>
    /// 检查版本兼容性
    /// Check version compatibility
    /// 
    /// 检查当前API与ModConfig系统的版本是否兼容。
    /// </summary>
    /// <returns>如果版本兼容返回true，否则返回false</returns>
    public static bool IsVersionCompatible()
    {
        if (!Initialize())
            return false;
        return isVersionCompatible;
    }
}
}