# CCcut

CCcut 是一个基于 .NET / WPF 的桌面视频切片工具：使用 FlyleafLib 播放视频，在可视化时间轴上把视频划分为若干「段落」（时间段），再通过 Xabe.FFmpeg 调用 FFmpeg 批量把选中的段落导出为独立视频文件。

## 功能特性

### 视频播放与定位
- 打开本地视频文件（菜单：文件 → 选择本地文件），或从剪贴板打开
- 播放 / 暂停，进度条点击或拖动定位
- 鼠标悬停显示位置虚影，红线实时指示当前播放位置

### 段落（时间段）编辑
- 新增段落：以当前游标为起点，默认时长 10 秒
- 多种删除方式：勾选后批量删除、在列表项内逐个删除
- 在列表中直接编辑段落标题
- 开始 / 结束时间一键对齐当前游标（保留最短 5 秒规则）

### 时间轴可视化
- 每个段落按时间比例渲染成彩色条
- 拖拽中间整体平移；拖拽左右边缘调整起止时间
- 自动边界钳制，且始终保持最短 5 秒
- 当前选中段落自动置顶高亮

### 拆分任务队列
- 勾选段落批量加入拆分队列
- 后台队列逐个处理，实时显示进度与日志
- 支持取消任务、打开结果文件、打开所在目录、删除任务记录
- 输出文件已存在时自动跳过
- 菜单「文件 → 打开切片目录」可一键打开当前视频的切片输出目录

### 设置
- FFmpeg 可执行文件目录、FFmpeg 动态库目录、插件目录
- 录制、截图、切片输出目录
- 配置持久化到程序目录下的 `config.json`

## 快捷键

| 快捷键 | 功能 |
| --- | --- |
| `Insert` / `Ctrl+N` | 在当前游标处新增段落 |
| `Delete` | 删除所有勾选的段落 |
| `Shift+Delete` | 删除当前选中的单个段落 |
| `Ctrl+Enter` | 拆分所有勾选的段落 |
| `Space` | 播放 / 暂停 |
| `[` | 开始时间对齐当前游标 |
| `]` | 结束时间对齐当前游标 |
| `←` / `→` | 整体平移当前段落 1 秒 |
| `Shift + ←/→` | 调整当前段落开始时间 1 秒 |
| `Ctrl + ←/→` | 调整当前段落结束时间 1 秒 |
| `Ctrl+Shift + ←/→` | 整体平移当前段落 0.1 秒（精细微调） |

> 编辑段落标题等文本框时不会触发以上快捷键；菜单「帮助 → 快捷键说明」中也有完整列表。

## 使用说明

### 环境要求
- Windows
- .NET 10（运行时 / SDK）
- FFmpeg 可执行文件（`ffmpeg.exe`、`ffprobe.exe`）
- Flyleaf 播放引擎所需的 FFmpeg 动态库与插件

### 首次运行
程序启动时会校验 FFmpeg 路径；若未配置或路径无效，会弹出配置向导：

1. 在设置窗口中选择 **FFmpeg 可执行文件目录**（包含 `ffmpeg.exe`）
2. 选择 **FFmpeg DLL 文件目录**（Flyleaf 引擎使用）
3. 如有需要，选择 **插件目录**，并按需修改录制 / 截图 / 切片输出目录
4. 保存后按提示重启以应用配置

### 基本流程
1. **文件 → 选择本地文件**（或「从剪贴板打开」）载入视频
2. 播放或拖动进度条，定位到目标位置
3. 点击 **添加段落**，随后拖动时间轴或用方向键微调起止时间
4. 勾选要导出的段落，点击 **拆分段落**
5. 在 **拆分队列** 窗口中查看进度、取消任务、打开结果文件或所在目录

## 输出说明
- 切片保存在 `<切片目录>/<视频文件名>/` 下
- 文件命名规则：`<标题(最多5字)>-<开始 dd.hhmmss_fff>-<结束 dd.hhmmss_fff><扩展名>`
- 来源与区间相同的任务会自动去重

## 配置文件

程序目录下的 `config.json`（由设置窗口维护）：

| 字段 | 说明 | 默认值 |
| --- | --- | --- |
| `FFmpegBinPath` | FFmpeg 可执行文件目录 | `./FFmpegBin` |
| `FFmpegDllPath` | FFmpeg 动态库目录 | `./FFmpegDll` |
| `PluginsPath` | 插件目录 | 空 |
| `FolderRecordings` | 录制输出目录 | `./Recordings` |
| `FolderSnapshots` | 截图输出目录 | `./Snapshots` |
| `FolderParagraphs` | 切片输出目录 | `./Paragraphs` |
| `SnapshotFormat` | 截图格式 | `png` |

## 项目结构

| 文件 | 说明 |
| --- | --- |
| `App.xaml(.cs)` | 应用启动、播放引擎初始化、配置校验与向导 |
| `MainWindow.xaml(.cs)` | 主窗口：播放器、时间轴、段落列表、全局键盘事件 |
| `MainViewModel.cs` | 主逻辑：段落增删改、对齐游标、入队拆分、按键微调 |
| `VideoParagraphModel.cs` | 段落数据模型 |
| `SplitQueueService.cs` | 拆分队列服务（单例、后台处理循环、去重与取消） |
| `SplitTask.cs` | 拆分任务模型与状态枚举 |
| `TaskManagerWindow.xaml(.cs)` | 拆分队列窗口 |
| `SettingsWindow.xaml(.cs)` / `SettingsViewModel.cs` | 设置窗口与逻辑 |
| `ShortcutsWindow.xaml(.cs)` | 快捷键说明窗口 |
| `SettingManager.cs` | `config.json` 读写与 `SettingsDto` 定义 |
| `WindowsFileDialogService.cs` | 文件 / 文件夹选择服务（`IFileDialogService`） |
| `*Converter.cs` | 时间字符串、像素、宽度、状态等值转换器 |

## 技术栈
- .NET 10 / WPF（`net10.0-windows`）
- CommunityToolkit.Mvvm — MVVM 框架（源生成器）
- FlyleafLib — 视频播放
- Xabe.FFmpeg — FFmpeg 封装
- HandyControl — UI 控件与样式
