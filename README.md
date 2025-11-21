# DropRateSetting Mod

## 项目简介

DropRateSetting 是一个为游戏开发的Mod，允许玩家自定义游戏中的掉落率设置。该Mod通过ModConfig系统提供用户友好的配置界面，让玩家可以调整高品质物品的掉落概率和战利品箱的物品数量。

## 功能特性

- **自定义掉落率**：调整高品质物品的掉落概率
- **自定义物品数量**：控制战利品箱中的物品数量
- **功能开关**：可选择启用或禁用Mod功能，避免与其他修改相同值的Mod冲突
- **配置持久化**：设置会自动保存并在游戏重启后恢复
- **双存储机制**：同时支持ModConfig系统和本地JSON文件存储配置

## 安装说明

1. 将编译后的DLL文件放入游戏的Mods文件夹
2. 确保游戏支持ModConfig系统（如未支持，Mod将使用本地配置文件）
3. 启动游戏，在ModConfig界面中找到"DropRateSetting"配置项进行设置

## 配置说明

### 主要配置项

- **是否启用本mod**：控制整个Mod功能的启用状态，默认为关闭
- **爆率**：控制高品质物品掉落概率的倍数，默认值为1（不改变原始掉率）
- **爆出个数**：控制战利品箱物品数量的倍数，默认值为1（不改变原始数量）

### 配置存储

配置会同时保存在两个地方：
1. ModConfig系统（如果可用）
2. 本地JSON文件（位于DLL同目录下的`DropRateSettingConfig.json`）

## 技术实现

### 核心组件

- `ModBehaviour.cs`：Mod主类，负责初始化Harmony补丁和配置系统
- `ModConfigDropRateManager.cs`：配置管理器，处理所有配置相关的逻辑
- `ModConfigApi.cs`：ModConfig系统安全接口封装（第三方提供）
- `HarmonyLoad.cs`：Harmony库加载器

### 性能优化

- 反射字段缓存：避免每帧重复执行反射操作
- 配置变更检测：仅在值真正变化时才执行保存操作
- 字符串拼接优化：预拼接常用字符串避免重复操作

## 使用注意事项

1. 默认情况下Mod功能是关闭的，需要手动在配置界面中启用
2. 所有数值默认值均为1，表示不改变游戏原始设置
3. 如果同时使用多个修改掉落率的Mod，建议使用功能开关避免冲突
4. 配置文件会自动创建并保存在DLL同目录下

## 版本兼容性

该Mod设计为与ModConfig系统兼容，同时也支持在没有ModConfig系统的情况下运行。

## 故障排除

如果遇到问题，请检查：
1. 确认DLL文件已正确放置在Mods文件夹
2. 检查游戏日志中是否有相关错误信息
3. 确认配置文件`DropRateSettingConfig.json`是否正确生成

## 开发信息

### 项目结构

```
DropRateSetting/
├── ModBehaviour.cs              # Mod主类
├── ModConfigDropRateManager.cs  # 配置管理器
├── ModConfigApi.cs             # ModConfig接口封装
├── HarmonyLoad.cs              # Harmony库加载器
├── 0Harmony.dll                # Harmony库（嵌入资源）
├── DropRateSetting.csproj      # 项目文件
├── README.md                   # 项目说明
└── Documentation.md            # 技术文档
```

### 编译要求

- .NET Standard 2.1
- Unity引擎环境
- Harmony库依赖（已嵌入）

### 部署步骤

1. 编译项目生成DLL文件
2. 将DLL文件放入游戏Mods目录
3. 启动游戏验证功能