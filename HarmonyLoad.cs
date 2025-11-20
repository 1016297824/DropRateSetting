using System.IO;
using System.Reflection;

namespace HarmonyLoad
{
    /// <summary>
    /// Harmony库加载器类
    /// 负责在运行时加载嵌入的0Harmony.dll程序集
    /// </summary>
    public class HarmonyLoad
    {
        /// <summary>
        /// 加载嵌入的0Harmony.dll程序集
        /// 通过将0Harmony.dll作为嵌入资源包含在项目中，可以避免部署时的依赖问题
        /// </summary>
        /// <returns>加载的Assembly对象</returns>
        public static Assembly Load0Harmony()
        {
            // 在项目属性中设置 0Harmony.dll 的 "生成操作" 为 "嵌入的资源"
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            // 获取当前类的命名空间（假设当前类在 "DeadNoDrop" 命名空间下）
            string currentNamespace = typeof(HarmonyLoad).Namespace;
            using (Stream stream = executingAssembly.GetManifestResourceStream($"{currentNamespace}.0Harmony.dll"))
            using (MemoryStream ms = new MemoryStream())
            {
                stream?.CopyTo(ms);
                Assembly assembly = Assembly.Load(ms.ToArray());

                return assembly;
            }
        }
    }
}