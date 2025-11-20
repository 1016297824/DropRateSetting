# DropRateSetting Mod 文档

## 概述

DropRateSetting 是一个为游戏《Escape from Duckov》设计的Mod，用于增加敌人被击败后掉落物品的概率和数量。通过这个Mod，玩家可以获得更好的游戏体验，更容易收集到所需的物品。

## 功能特性

1. **增加掉落概率**：提高敌人掉落物品的基础概率
2. **增加掉落数量**：增加每次掉落的物品数量
3. **可调节设置**：通过ModConfig系统调节掉落率倍数
4. **本地配置持久化**：配置会保存到本地文件，即使ModConfig不可用也能加载
5. **Mod激活事件监听**：支持ModConfig动态加载
6. **无侵入性**：使用Harmony补丁技术，不修改游戏原始文件

## 核心组件

### 1. DropRateModifier 类
负责实现掉落率修改的核心逻辑，使用Harmony补丁技术修改游戏原有方法。

主要修改的方法：
- `LootSpawner.Start`：增加基础掉落概率
- `LootSpawner.Setup`：增加掉落物品数量

### 2. ModConfigDropRateManager 类
提供通过ModConfig系统管理的配置界面，允许玩家自定义掉落率倍数。

特性：
- 与ModConfig系统集成，提供图形化配置界面
- 提供1.0到100.0范围的掉落率调节滑块，默认值为10.0
- 实时更新功能，无需重启游戏即可看到效果
- 本地配置文件持久化，保存在 `StreamingAssets/DropRateSettingConfig.txt`
- Mod激活事件监听，支持ModConfig动态加载

### 3. ModBehaviour 类
Mod的主入口点，负责初始化和管理整个Mod的生命周期。

### 4. ModConfigAPI 类
提供与ModConfig系统交互的安全接口封装，通过反射调用ModConfig的方法，确保即使ModConfig未加载也不会导致异常。

## 使用方法

### 安装
1. 构建项目生成DLL文件
2. 确保安装了 [ModConfig](https://github.com/FrozenFish259/duckov_mod_config) 系统
3. 将DLL文件放入游戏的Mods文件夹中
4. 启动游戏

### 调节掉落率
1. 在游戏主菜单中找到"Mod设置"或类似选项
2. 找到"DropRateSetting"配置项
3. 调整"掉落率倍数"参数：
   - 1.0 = 默认掉落率
   - 10.0 = 十倍掉落率
   - 50.0 = 五十倍掉落率
   - 100.0 = 一百倍掉落率

## 技术实现

### Harmony补丁技术
使用Harmony库的以下特性：
- `Prefix`补丁：在原方法执行前修改参数
- `Postfix`补丁：在原方法执行后进行清理
- `Traverse`类：访问私有字段和方法

### ModConfig集成
使用ModConfigAPI与通用Mod配置系统集成：
- `SafeAddInputWithSlider`：添加带滑块的输入框
- `SafeLoad`：加载配置值
- `SafeSave`：保存配置值（内部使用）
- 事件委托：监听配置变更和Mod激活事件

### 代码安全措施
1. 使用`__state`参数保存和恢复原始值
2. 限制数值范围防止异常
3. 兼容性检查确保与ModConfig系统协同工作
4. 本地配置文件持久化，提高容错性
5. Mod激活事件监听，支持动态加载

## API文档

### ModConfigDropRateManager

#### 属性
- `Instance`: 获取ModConfigDropRateManager单例实例
- `CurrentDropRateMultiplier`: 当前掉落率倍数

#### 方法
- `GetVersionInfo()`: 获取当前版本信息

### ModConfigAPI

#### 属性
- `ModConfigName`: ModConfig模块的名称
- `isInitialized`: API是否已初始化标志

#### 方法
- `SafeAddOnOptionsChangedDelegate(Action<string> action)`: 安全地添加选项变更事件委托
- `SafeRemoveOnOptionsChangedDelegate(Action<string> action)`: 安全地移除选项变更事件委托
- `SafeAddDropdownList(...)`: 安全地添加下拉列表配置项
- `SafeAddInputWithSlider(...)`: 安全地添加带滑条的输入框配置项
- `SafeAddBoolDropdownList(...)`: 安全地添加布尔下拉列表配置项
- `SafeLoad<T>(...)`: 安全地加载配置值
- `SafeSave<T>(...)`: 安全地保存配置值
- `IsAvailable()`: 检查ModConfig是否可用
- `GetVersionInfo()`: 获取ModConfig版本信息
- `IsVersionCompatible()`: 检查版本兼容性

## 注意事项

1. 该Mod需要ModConfig系统才能使用配置界面
2. 该Mod仅修改掉落概率和数量，不改变掉落物品的种类
3. 过高的掉落率倍数可能影响游戏平衡性
4. Mod不会修改游戏原始文件，卸载简单

## 故障排除

### 常见问题
1. **掉落率未改变**：检查ModConfig是否正确安装
2. **Mod未生效**：确认DLL文件已正确放置在Mods文件夹中
3. **游戏崩溃**：尝试降低掉落率倍数或卸载Mod

### 日志查看
可通过游戏日志查看Mod是否正常加载和运行。