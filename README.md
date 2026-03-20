# 图片压缩工具 🦞

一个简单易用的桌面图片压缩工具，支持拖放操作，精确控制压缩大小，基于 .NET Framework 4.8 + WPF 构建，可在 Windows 10 上运行。

## 项目结构

```
ImageCompressor/
├── ImageCompressor.sln          # 解决方案文件
├── ImageCompressor.csproj        # 项目文件
├── README.md                     # 项目说明（本文件）
├── .gitignore                    # Git 忽略文件
├── MainWindow.xaml               # 主窗口界面
├── MainWindow.xaml.cs            # 主窗口逻辑
├── App.xaml                      # 应用程序定义
├── App.xaml.cs                   # 应用程序入口
└── Program.cs                    # 程序入口
```

## 快速开始

### 最快方式：直接运行编译好的程序

1. **下载**：从发布文件中获取 `ImageCompressor.exe`
2. **运行**：直接双击运行（无需安装）
3. **开始使用**：拖放图片，选择大小，开始压缩

### 开发方式：从源码编译

```bash
# 1. 克隆仓库
git clone <repository-url>
cd ImageCompressor

# 2. 还原依赖
dotnet restore

# 3. 编译运行
dotnet run -p:Platform=x64

# 4. 发布 Windows 10 版本
dotnet publish -c Release -p:Platform=x64

# 5. 同时发布 32 位和 64 位版本
发布32位和64位版本.bat
```

## 功能特点

- 🎯 **精确控制**：可设置目标文件大小（如100KB、200KB等）
- 🖱️ **拖放操作**：直接拖放图片到窗口即可
- 📊 **智能压缩**：自动调整压缩质量，找到最佳平衡点
- 🎨 **简约界面**：现代化UI设计，操作直观
- ⚡ **批量处理**：支持同时压缩多张图片
- 💾 **自动保存**：压缩后的文件保存在你选择的输出目录
- 🪟 **Windows 10 兼容**：基于 .NET Framework 4.8 发布

## 技术栈

- **C# / .NET Framework 4.8**
- **WPF** (Windows Presentation Foundation)
- **System.Drawing** (图像处理)

## 如何运行

### 方式1：使用Visual Studio 2022

1. 打开 Visual Studio 2022
2. 选择"创建新项目"
3. 搜索"WPF Application"并创建
4. 将以下文件复制到项目中：
   - `MainWindow.xaml`
   - `MainWindow.xaml.cs`
   - `Program.cs`
   - `App.xaml`
   - `App.xaml.cs`
5. 在解决方案资源管理器中右键点击项目 → "管理NuGet程序包"
6. 安装 `SharpCompress` 包
7. 按 F5 运行

### 方式2：使用命令行

```bash
# 进入项目目录
cd ImageCompressor

# 还原依赖
dotnet restore

# 运行
dotnet run --framework net48 -p:Platform=x64
```

## 使用方法

1. **拖放图片**：将图片拖放到虚线框区域
2. **设置目标大小**：从下拉菜单选择预设大小（100KB、200KB、500KB、1MB），或选择"自定义"输入自定义大小
3. **调整压缩质量**：拖动滑块设置压缩质量范围（10%-80%）
4. **开始压缩**：点击"开始压缩"按钮
5. **查看结果**：压缩完成后，点击"打开文件"查看压缩后的图片

## 输出说明

- 压缩后的图片保存在：`原文件夹/压缩后/`
- 文件名格式：`原图名_compressed.扩展名`
- 压缩后文件会自动添加"_compressed"后缀

## 示例

```
原图：photo.jpg (2.5 MB)
压缩到：200 KB
结果：photo_compressed.jpg (198 KB)
```

## 编译和部署

### 环境要求

- **.NET SDK 8/9/10 任一可用版本**（用于编译 SDK-style `net48` 项目）
- **Windows 10/11**
- **目标机器需安装或启用 .NET Framework 4.8**

### 快速编译（调试模式）

```bash
# 进入项目目录
cd ImageCompressor

# 还原依赖
dotnet restore

# 编译运行
dotnet run -p:Platform=x64
```

### 发布为可执行文件（推荐）

`.NET Framework 4.8` 不是现代 `.NET` 的 self-contained 模式，因此推荐直接分发 `publish` 目录。

```bash
dotnet publish -c Release -p:Platform=x64
dotnet publish -c Release -p:Platform=x86
```

- ✅ 生成适合 Windows 10 的发布目录
- ✅ 直接运行生成的 `ImageCompressor.exe`
- ✅ 适合打包为 zip/rar 后分发
- 注意：目标机器需要启用 `.NET Framework 4.8`
- 注意：`QuestPDF` 依赖原生库，必须按架构发布，不能使用 `Any CPU`

也可以直接双击仓库根目录下的 `发布Windows10版本.bat` 或 `发布32位和64位版本.bat`。

### 发布文件位置

编译成功后，可执行文件位于：

```text
bin/x64/Release/net48/win-x64/publish/
├── ImageCompressor.exe  <- 64-bit main program
├── QuestPDF.dll
├── qpdf.dll
└── ...

bin/x86/Release/net48/win-x86/publish/
├── ImageCompressor.exe  <- 32-bit main program
├── QuestPDF.dll
├── qpdf.dll
└── ...
```

把对应架构的整个 `publish` 目录一起拷贝到目标机器，直接运行 `ImageCompressor.exe` 即可使用。

## 使用方法

1. **拖放图片**：将图片拖放到虚线框区域
2. **设置输出目录**：点击"选择输出目录"选择保存位置，或使用默认的桌面"\压缩后"
3. **设置目标大小**：从下拉菜单选择预设大小（100KB、200KB、500KB、1MB），或选择"自定义"输入自定义大小
4. **选择压缩模式**：
   - "尽可能压缩"：以最小质量为代价，严格控制在目标大小内
   - "保持较好质量"：在质量损失可控的情况下接近目标大小
5. **开始压缩**：点击"开始压缩"按钮，按钮会被禁用直到完成
6. **查看结果**：压缩完成后查看统计信息和压缩效果

## 输出说明

- 压缩后的图片保存在：`你选择的输出目录`
- 默认输出目录：`桌面\压缩后`
- 文件名格式：`原图名_compressed.扩展名`
- 压缩后文件会自动添加"_compressed"后缀

## 示例

```
原图：photo.jpg (2.5 MB)
目标：200 KB
模式：尽可能压缩
结果：photo_compressed.jpg (198 KB)
```

## 注意事项

- 支持的格式：JPG、PNG、GIF、BMP
- 压缩后的图片质量会比原图有所降低
- 对于非常大的图片，可能需要一些时间处理
- 压缩质量范围设置为10%-80%，避免过度压缩导致图片严重失真
- `QuestPDF` 使用原生依赖，若出现 `BadImageFormatException`，优先检查是否选错了 x86/x64 版本

## 许可证

MIT License
