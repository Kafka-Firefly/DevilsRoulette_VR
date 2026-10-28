# DevilsRoulette_VR

一个面向 Meta Quest 2 的 Unity VR 原型，使用 Unity 6.6 和 XR Interaction Toolkit 3.6.1。

## 当前内容

- `Assets/Scenes/ThePit.unity`：深色房间、桌子、带靶心的目标、枪械和 XR Origin。
- `Assets/Scripts/ShotgunController.cs`：抓取后按手柄扳机激活；六发弹膛中随机一发实弹，其余为空仓；包含射线命中、枪口火花、振动反馈和无需外部素材的临时音效。
- `Assets/Scripts/TargetHitReceiver.cs`：目标命中闪红后恢复，使用材质属性块，避免为每个渲染器复制材质。
- `Assets/Editor/CreateThePitPrototype.cs` 与 `ThePitModelingPass.cs`：原型场景生成及低多边形模型增强工具。

## 版本与设备配置

- Unity Editor：`6000.6.7f1`
- XR Interaction Toolkit：`3.6.1`
- OpenXR：`1.18.0`
- Meta OpenXR：`2.6.1`
- Android 图形/脚本配置：Linear 色彩空间、ARM64、IL2CPP、API 34
- OpenXR Android 设置中 Meta Quest Support 与 Quest 2 目标已启用。

## 操作

使用右手手柄握把键抓枪，按扳机键射击。项目使用 XR Interaction Toolkit Starter Assets 的默认输入动作。

## 打开与构建

1. 使用 Unity Hub 添加本仓库目录，并用 Unity `6000.6.7f1` 打开。
2. 打开 `Assets/Scenes/ThePit.unity`。
3. 如需构建 Quest 2 APK，在 Unity Hub 的该编辑器安装中确认已安装 Android Build Support、Android SDK & NDK Tools 和 OpenJDK。
4. 在 Build Profiles 中选择 Android，检查场景列表含 `ThePit`，再构建 APK。

## 原型范围

枪械、房间和目标是可编辑的低多边形占位模型；当前使用程序合成的临时声音。自瞄玩家只会写入调试日志，不会造成伤害。尚未制作完整回合 UI、弹药装填交互，也未在 Quest 2 设备上验证运行表现。
