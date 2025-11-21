# DropRateSetting Mod 技术文档

## 项目架构

### 整体设计
DropRateSetting Mod采用模块化设计，主要由以下几个核心组件构成：

1. **ModBehaviour** - Mod主入口点
2. **ModConfigDropRateManager** - 配置管理核心
3. **ModConfigAPI** - 第三方配置系统接口
4. **HarmonyLoad** - Harmony库加载器

### 数据流图
```
用户配置 ↔ ModConfigDropRateManager ↔ ModConfigAPI ↔ ModConfig系统
              ↕
        本地JSON文件存储
              ↕
         游戏运行时修改
```

## 核心组件详解

### ModBehaviour.cs

#### 职责
- 初始化Harmony补丁系统
- 管理Mod的生命周期
- 每帧更新游戏中的掉落率设置

#### 关键方法
- `Start()` - 初始化Harmony补丁和配置管理器
- `Update()` - 每帧检查并应用掉落率设置
- `OnDisable()` - 清理Harmony补丁

#### 性能优化
- 使用字段缓存避免每帧反射操作
- 添加功能开关减少不必要的处理

### ModConfigDropRateManager.cs

#### 职责
- 管理所有配置项
- 处理配置的加载和保存
- 提供配置变更通知机制

#### 配置项
1. **EnableMod** - 功能开关
2. **SpawnChanceMultiplier** - 掉落率倍数
3. **RandomCountMultiplier** - 物品数量倍数

#### 存储机制
- **优先级**：本地文件 > ModConfig系统
- **双备份**：同时保存在ModConfig系统和本地JSON文件
- **格式**：JSON格式，包含所有配置项

#### 关键方法
- `InitializeConfig()` - 初始化配置系统
- `LoadConfig()` - 从ModConfig加载配置
- `LoadLocalConfig()` - 从本地文件加载配置
- `SaveLocalConfig()` - 保存到本地文件

### ModConfigApi.cs
第三方提供的安全接口封装，通过反射调用ModConfig系统功能。

### HarmonyLoad.cs
负责加载嵌入的Harmony库，避免部署时的依赖问题。

## 配置管理机制

### 初始化流程
1. 尝试从本地JSON文件加载配置
2. 检查ModConfig系统是否可用
3. 如果可用，注册配置项并同步当前值
4. 确保初始配置文件存在

### 配置变更处理
1. 监听ModConfig配置变更事件
2. 检测值是否真正发生变化
3. 同步更新本地文件和内存中的值
4. 通知相关组件配置已更新

### 持久化策略
- **文件位置**：DLL同目录下的`DropRateSettingConfig.json`
- **写入时机**：配置变更时立即保存
- **读取时机**：Mod初始化时加载
- **容错机制**：文件损坏时使用默认值

## 性能优化措施

### 反射优化
- 缓存反射获取的字段信息
- 避免每帧重复执行反射操作
- 在首次使用时进行反射查询

### 字符串操作优化
- 预拼接常用字符串避免重复操作
- 使用常量存储频繁访问的键名

### 配置保存优化
- 仅在值真正变化时才执行保存
- 合并多个配置变更到一次保存操作
- 异常静默处理避免影响游戏运行

## 错误处理机制

### 静默处理原则
- 所有异常都进行静默处理
- 不影响游戏正常运行
- 记录错误信息供调试使用

### 容错设计
- 配置文件损坏时自动重建
- 缺失配置项时使用默认值
- 系统不可用时优雅降级

## 扩展性设计

### 新增配置项
1. 在`ModConfigDropRateManager`中添加常量定义
2. 在`InitializeConfig`方法中注册新配置项
3. 在`LoadConfig`和`LoadLocalConfig`中添加加载逻辑
4. 在`SaveLocalConfig`中添加保存逻辑

### 新增功能模块
1. 创建新的管理器类
2. 在`ModBehaviour`中实例化
3. 通过配置系统控制功能开关

## 试玩和调试

### 调试日志
- 通过Unity的Debug系统输出关键信息
- 包含配置加载、保存、变更等关键节点日志

### 常见问题排查
1. **配置不生效**：检查功能开关是否启用
2. **配置丢失**：确认JSON文件是否存在且格式正确
3. **Mod不加载**：检查DLL是否正确放置，是否有依赖问题

## 版本兼容性

### 向后兼容
- 配置文件格式保持稳定
- 默认值设置合理
- 旧版本配置自动适配

### ModConfig系统兼容
- 使用安全接口避免版本不匹配
- 提供版本检查机制
- 无法连接时使用本地配置

## 部署说明

### 编译要求
- .NET Standard 2.1
- Unity引擎环境
- Harmony库依赖（已嵌入）

### 部署步骤
1. 编译项目生成DLL文件
2. 将DLL文件放入游戏Mods目录
3. 启动游戏验证功能