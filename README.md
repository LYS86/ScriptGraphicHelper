# Script Graphic Helper

**一款简单好用的 AutoX/Auto.js 图色助手, 快速生成多种脚本开发工具的图色格式代码**

## 功能

- ⭐ ADB连接模式: 通过 adb 进行截图 (usb/wifi)
- ⭐ 支持生成各种工具的格式代码 与 自定义的格式代码 (在 Assets/diyFormat.json 的 FindStrFormat 定义)
- 模拟器模式: 调用模拟器命令行进行截图, 无需手动连接 adb (适用于雷电、夜神、逍遥)
- AJ连接模式: 调用 aj 的 tcp 调试端口进行截图 (需要安装 autojs.pro 8, 并开启调试服务和悬浮窗)
- 多分辨率适配的测试和代码生成 (锚点格式)

## 说明

软件在 Release 中下载

## 源码开发

### 环境要求

| 依赖 | 说明 |
|------|------|
| **.NET 6.0 SDK** 或更高版本 | 编译工具链（.NET 10 SDK 同样兼容） |
| **Visual Studio 2022** | IDE（推荐，非必需） |
| **Windows x64** | 运行/编译平台 |

### 编译

```powershell
# 还原依赖
dotnet restore

# 编译（Debug 模式）
dotnet build

# 编译（Release 模式）
dotnet build -c Release
```

### 发布（生成可分发 EXE）

**框架依赖发布（体积小，需目标机器安装 .NET 6 Runtime）：**

```powershell
dotnet publish -c Release -r win-x64 --self-contained false -o ./build
```

输出目录：`./build/`，入口：`ScriptGraphicHelper.exe`。

**自包含单文件发布（无需安装 .NET Runtime，体积较大）：**

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./build
```

### Native 组件

`Assets/mouse.dll` 为预编译的 C++ 鼠标控制库。如需重新编译，使用 Visual Studio 打开 `Native/Winodws/mouse/mouse.sln`（需要 v142 工具集），编译后将输出的 `mouse.dll` 覆盖到 `Assets/` 目录。

## 原作者

[yiszza](https://gitee.com/yiszza/ScriptGraphicHelper)

(已经删库了)