# 图片压缩工具 🦞

一个简单易用的桌面图片压缩工具，支持拖放操作，精确控制压缩大小，基于 .NET 10.0 + WPF 构建。

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
dotnet run

# 4. 发布生产版本
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishReadyToRun=true /p:TrimMode=link
```

## 功能特点

- 🎯 **精确控制**：可设置目标文件大小（如100KB、200KB等）
- 🖱️ **拖放操作**：直接拖放图片到窗口即可
- 📊 **智能压缩**：自动调整压缩质量，找到最佳平衡点
- 🎨 **简约界面**：现代化UI设计，操作直观
- ⚡ **批量处理**：支持同时压缩多张图片
- 💾 **自动保存**：压缩后的文件保存在你选择的输出目录
- 🔧 **ReadyToRun优化**：使用R2R编译，启动速度更快

## 技术栈

- **C# .NET 10.0**
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
dotnet run --framework net8.0-windows
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

- **.NET 10.0 SDK** 或更高版本
- **Windows 10/11** (64位系统)

### 快速编译（调试模式）

```bash
# 进入项目目录
cd ImageCompressor

# 还原依赖
dotnet restore

# 编译运行
dotnet run
```

### 发布为可执行文件（推荐）

#### 1. **开发/测试发布**（无需安装 .NET）

打包包含 .NET 运行时的自包含应用：

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

- ✅ **无需安装 .NET**
- ✅ 直接运行生成的 `ImageCompressor.exe`
- 📦 包含完整 .NET 运行时（约 50-80 MB）

#### 2. **ReadyToRun (R2R) 优化** ⚡

使用 R2R 编译，显著提升启动速度：

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishReadyToRun=true
```

- ⚡ **启动速度更快**（比普通发布快 2-3 倍）
- ✅ **仍然需要 .NET 运行时**
- 📦 较小的文件体积（相比普通发布）

#### 3. **R2R + 资源裁剪** 🚀

组合使用 R2R 和裁剪功能，最佳性能：

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishReadyToRun=true /p:TrimMode=link
```

- ⚡ **最快的启动速度**
- 📦 **最小的文件体积**
- ✅ **不需要 .NET 运行时**
- 🔧 **推荐用于生产环境**

### 发布选项说明

| 选项 | 文件大小 | 启动速度 | 需要运行时 | 推荐场景 |
|------|---------|---------|-----------|---------|
| 普通 | 10-20 MB | ⚡⚡ | ✅ 是 | 开发测试 |
| Self-Contained | 50-80 MB | ⚡⚡ | ❌ 否 | 无需安装 |
| ReadyToRun | 50-80 MB | ⚡⚡⚡ | ✅ 是 | 测试性能 |
| **R2R + 裁剪** | **40-60 MB** | **⚡⚡⚡** | **❌ 否** | **生产部署** |

### 发布文件位置

编译成功后，可执行文件位于：

```
bin/Release/net10.0/win-x64/publish/
├── ImageCompressor.exe  ← 主程序
├── Microsoft.*.dll       ← 依赖库
└── ...
```

直接运行 `ImageCompressor.exe` 即可使用！

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
- WPF 不支持 Native AOT 编译，推荐使用 ReadyToRun (R2R) 方案

## 许可证

MIT License
