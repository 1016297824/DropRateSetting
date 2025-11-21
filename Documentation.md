# DropRateSetting Mod 文档

## 简介

DropRateSetting 是一个用于修改游戏掉落率的Mod。通过这个Mod，玩家可以调整敌人掉落物品的概率和数量。

## 功能特性

1. 可调节的爆率倍数（控制高品质物品掉落概率）
2. 可调节的爆出个数倍数（控制战利品箱物品数量）
3. 通过ModConfig界面进行实时配置
4. 配置自动保存和加载

## 配置说明

### 爆率 (Spawn Chance)
- 控制高品质物品掉落概率
- 范围: 1 - 10 (默认值: 1)
- 类型: 整数
- 倍数越高，高品质物品掉落概率越大

### 爆出个数 (Random Count)
- 控制战利品箱物品数量
- 范围: 1 - 5 (默认值: 1)
- 类型: 整数
- 倍数越高，战利品箱物品数量越多

## 使用方法

1. 安装Mod后启动游戏
2. 在主菜单中找到"Mod设置"选项
3. 在设置界面中找到"DropRateSetting"配置项
4. 调整"爆率"和"爆出个数"滑块到所需值
5. 设置会自动保存并在游戏中实时生效

## 技术实现

### 核心组件

1. **ModConfigDropRateManager.cs** - 负责配置管理
   - 与ModConfig系统集成
   - 管理配置项的加载和保存
   - 提供配置值给其他组件使用

2. **DropRateModifier.cs** - 负责游戏逻辑修改
   - 使用Harmony库进行代码补丁
   - 修改LevelConfig组件的属性获取方法
   - 应用配置的倍数到游戏逻辑中

3. **ModBehaviour.cs** - Mod主入口
   - 初始化Harmony补丁
   - 创建配置管理器实例
   - 管理Mod的生命周期

### 工作原理

1. Mod启动时初始化配置管理器和Harmony补丁
2. 配置管理器注册两个滑块配置项：爆率和爆出个数
3. 当玩家调整配置时，配置管理器会更新内部值并通知其他组件
4. DropRateModifier通过Harmony补丁拦截LevelConfig的属性获取方法
5. 在获取lootBoxHighQualityChanceMultiplier和LootboxItemCountMultiplier属性值时，应用配置的倍数

## 故障排除

### 配置不生效
1. 确认ModConfig系统正常工作
2. 检查日志文件查看是否有错误信息
3. 确认配置值是否正确加载

### 掉落率没有变化
1. 确认敌人类型是否支持掉落物品
2. 检查游戏原生的掉落逻辑是否被其他Mod影响
3. 查看日志确认补丁是否正确应用

## 版本历史

### v1.0.0
- 初始版本
- 添加爆率和爆出个数配置项
- 实现基本的掉落率修改功能

### v1.1.0
- 优化配置更新逻辑
- 改进日志记录
- 修复数值类型问题

### v1.2.0
- 修改目标属性，使用真正影响爆率的LevelConfig属性
- lootBoxHighQualityChanceMultiplier 对应爆率设置
- LootboxItemCountMultiplier 对应爆出个数设置

## 已知问题

1. 在某些情况下，配置更新可能不会立即反映在已经生成的敌人上
2. 与其他修改掉落逻辑的Mod可能存在兼容性问题

## 技术支持

如遇到问题，请查看日志文件或联系开发者。
日志文件位置: `<游戏目录>/DropRateSetting.log`
