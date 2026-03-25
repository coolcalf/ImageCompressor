using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Serialization;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Point = System.Windows.Point;

namespace ImageCompressor
{
    public partial class MainWindow : Window
    {
        private ImageManager _imageManager;
        private readonly ImageCompressionService _imageCompressionService = new ImageCompressionService();
        private bool _isProcessing = false;
        private int _targetKb = 200;
        private string _customSizeText = "";
        private bool _useBestCompression = true;
        private string _outputPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        private double _zoomLevel = 1.0;
        private Point _panOffset = new Point(0, 0);
        private Point _lastMousePosition;
        private bool _isDragging = false;
        private ImageItem? _draggedItem = null;
        private ImageItem? _currentPreviewItem = null;

    public MainWindow()
    {
        InitializeComponent();
        
        using var dummyBitmap = new Bitmap(1, 1);
        using var dummyGraphics = Graphics.FromImage(dummyBitmap);
        
        _imageManager = new ImageManager();
        DataContext = _imageManager;
        
        InitializeUI();
        LoadConfiguration();
        
        if (string.IsNullOrEmpty(OutputPathTextBox.Text))
        {
            OutputPathTextBox.Text = _outputPath;
        }
        
        ThumbnailListBox.SelectionChanged += ThumbnailListBox_SelectionChanged;
        ThumbnailListBox.AllowDrop = true;
        ThumbnailListBox.DragOver += ThumbnailListBox_DragOver;
        ThumbnailListBox.Drop += ThumbnailListBox_Drop;
        ThumbnailListBox.PreviewMouseMove += ThumbnailListBox_PreviewMouseMove;
        ThumbnailListBox.PreviewMouseLeftButtonDown += ThumbnailListBox_PreviewMouseLeftButtonDown;
        ThumbnailListBox.PreviewMouseLeftButtonUp += ThumbnailListBox_PreviewMouseLeftButtonUp;
        
        ThumbnailGrid.DragOver += ThumbnailGrid_DragOver;
        ThumbnailGrid.Drop += ThumbnailGrid_Drop;
        
        ZoomInButton.Click += ZoomInButton_Click;
        ZoomOutButton.Click += ZoomOutButton_Click;
        ResetZoomButton.Click += ResetZoomButton_Click;
        PreviewImage.MouseWheel += PreviewImage_MouseWheel;
        PreviewImage.MouseLeftButtonDown += PreviewImage_MouseLeftButtonDown;
        PreviewImage.MouseLeftButtonUp += PreviewImage_MouseLeftButtonUp;
        PreviewImage.MouseMove += PreviewImage_MouseMove;
        
        Closed += MainWindow_Closed;
    }

    private void InitializeUI()
    {
        PreviewPlaceholder.Visibility = Visibility.Visible;
        PreviewImage.Visibility = Visibility.Collapsed;
        CompressButton.IsEnabled = false;
        ExportPdfButton.IsEnabled = false;
    }

    private void MainWindow_Closed(object sender, EventArgs e)
    {
        SaveConfiguration();
    }

    private void SaveConfiguration()
    {
        try
        {
            var config = new UserConfig
            {
                TargetSizeTag = GetSelectedTag(),
                CustomSize = _customSizeText,
                UseBestCompression = _useBestCompression,
                OutputPath = _outputPath
            };

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
            XmlSerializer serializer = new XmlSerializer(typeof(UserConfig));
            using (StreamWriter writer = new StreamWriter(configPath))
            {
                serializer.Serialize(writer, config);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }

    private void LoadConfiguration()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.xml");
            if (File.Exists(configPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(UserConfig));
                using (StreamReader reader = new StreamReader(configPath))
                {
                    var config = (UserConfig)serializer.Deserialize(reader);

                    if (config.TargetSizeTag != null)
                    {
                        foreach (ComboBoxItem item in TargetSizeComboBox.Items)
                        {
                            if (item.Tag?.ToString() == config.TargetSizeTag)
                            {
                                TargetSizeComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(config.CustomSize))
                    {
                        _customSizeText = config.CustomSize ?? string.Empty;
                        CustomSizeTextBox.Text = config.CustomSize;
                        CustomSizeTextBox.Visibility = Visibility.Visible;
                    }

                    if (config.UseBestCompression)
                    {
                        CompressionModeBest.IsChecked = true;
                        CompressionModeGood.IsChecked = false;
                    }
                    else
                    {
                        CompressionModeBest.IsChecked = false;
                        CompressionModeGood.IsChecked = true;
                    }

                    if (!string.IsNullOrEmpty(config.OutputPath) && Directory.Exists(config.OutputPath))
                    {
                        _outputPath = config.OutputPath ?? _outputPath;
                        OutputPathTextBox.Text = _outputPath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading configuration: {ex.Message}");
        }
    }

    private string? GetSelectedTag()
    {
        try
        {
            var selectedItem = TargetSizeComboBox.SelectedItem as ComboBoxItem;
            return selectedItem?.Tag?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private void ThumbnailListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedItem = ThumbnailListBox.SelectedItem as ImageItem;
        if (selectedItem != null)
        {
            _imageManager.SelectedImage = selectedItem;
            UpdatePreviewPanel(selectedItem);
            ThumbnailListBox.ScrollIntoView(selectedItem);
        }
        else
        {
            ClearPreviewPanel();
        }
    }

    private void UpdatePreviewPanel(ImageItem item)
    {
        if (_currentPreviewItem != null)
        {
            _currentPreviewItem.PropertyChanged -= OnPreviewItemPropertyChanged;
        }

        if (item == null)
        {
            _currentPreviewItem = null;
            ClearPreviewPanel();
            return;
        }

        _currentPreviewItem = item;
        _currentPreviewItem.PropertyChanged += OnPreviewItemPropertyChanged;

        PreviewPlaceholder.Visibility = Visibility.Collapsed;
        PreviewImage.Visibility = Visibility.Visible;

        PreviewImage.Source = item.OriginalImage;

        PreviewFileName.Text = item.FileName;
        PreviewFileSize.Text = item.HasCompressedVersion 
            ? $"原始: {item.FileSizeDisplay} → 压缩后: {item.CompressedSizeDisplay}" 
            : $"大小: {item.FileSizeDisplay}";
        PreviewDimensions.Text = item.Dimensions;
    }

    private void OnPreviewItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageItem.OriginalImage) && _currentPreviewItem != null)
        {
            PreviewImage.Source = _currentPreviewItem.OriginalImage;
        }
    }

    private void ClearPreviewPanel()
    {
        PreviewPlaceholder.Visibility = Visibility.Visible;
        PreviewImage.Visibility = Visibility.Collapsed;
        PreviewFileName.Text = string.Empty;
        PreviewFileSize.Text = string.Empty;
        PreviewDimensions.Text = string.Empty;
    }

    private void ThumbnailListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var listBoxItem = FindParentListBoxItem(e.OriginalSource as DependencyObject);
        if (listBoxItem?.DataContext is ImageItem imageItem)
        {
            _isDragging = true;
            _draggedItem = imageItem;
            _lastMousePosition = e.GetPosition(null);
        }
    }

    private void ThumbnailListBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        _draggedItem = null;
    }

    private void ThumbnailListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging && _draggedItem != null && e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPosition = e.GetPosition(null);
            Vector diff = _lastMousePosition - currentPosition;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                _isDragging = false;
                DragDrop.DoDragDrop(ThumbnailListBox, _draggedItem, DragDropEffects.Move);
            }
        }
    }

    private void ThumbnailListBox_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ImageItem)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.All;
        }
        e.Handled = true;
    }

    private void ThumbnailListBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(ImageItem)))
        {
            var droppedItem = e.Data.GetData(typeof(ImageItem)) as ImageItem;
            if (droppedItem == null) return;

            var targetItem = FindParentListBoxItem(e.OriginalSource as DependencyObject)?.DataContext as ImageItem;
            if (targetItem != null)
            {
                int oldIndex = _imageManager.Images.IndexOf(droppedItem);
                int newIndex = _imageManager.Images.IndexOf(targetItem);

                if (oldIndex >= 0 && newIndex >= 0 && oldIndex != newIndex)
                {
                    _imageManager.MoveImage(oldIndex, newIndex);
                    ThumbnailListBox.SelectedItem = droppedItem;
                }
            }
        }
        else if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files);
        }
    }

    private void ThumbnailGrid_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ThumbnailGrid_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            AddFiles(files);
        }
    }

    private static ListBoxItem? FindParentListBoxItem(DependencyObject? child)
    {
        while (child != null)
        {
            if (child is ListBoxItem item)
                return item;
            child = System.Windows.Media.VisualTreeHelper.GetParent(child);
        }
        return null;
    }

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = Math.Min(_zoomLevel * 1.2, 10.0);
        ApplyZoom();
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = Math.Max(_zoomLevel / 1.2, 0.1);
        ApplyZoom();
    }

    private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = 1.0;
        _panOffset = new Point(0, 0);
        ApplyZoom();
    }

    private void PreviewImage_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (PreviewImage.Source == null) return;
        
        double delta = e.Delta > 0 ? 1.1 : 0.9;
        _zoomLevel = Math.Max(0.1, Math.Min(10.0, _zoomLevel * delta));
        ApplyZoom();
        e.Handled = true;
    }

    private void PreviewImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_zoomLevel > 1.0 && PreviewImage.Source != null)
        {
            PreviewImage.CaptureMouse();
            _lastMousePosition = e.GetPosition(PreviewImage);
        }
    }

    private void PreviewImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        PreviewImage.ReleaseMouseCapture();
    }

    private void PreviewImage_MouseMove(object sender, MouseEventArgs e)
    {
        if (PreviewImage.IsMouseCaptured && e.LeftButton == MouseButtonState.Pressed)
        {
            Point currentPosition = e.GetPosition(PreviewImage);
            Vector delta = currentPosition - _lastMousePosition;
            _panOffset = new Point(_panOffset.X + delta.X, _panOffset.Y + delta.Y);
            _lastMousePosition = currentPosition;
            ApplyZoom();
        }
    }

    private void ApplyZoom()
    {
        if (PreviewImage.Source == null) return;
        
        var transformGroup = new TransformGroup();
        transformGroup.Children.Add(new ScaleTransform(_zoomLevel, _zoomLevel));
        transformGroup.Children.Add(new TranslateTransform(_panOffset.X, _panOffset.Y));
        PreviewImage.RenderTransform = transformGroup;
        PreviewImage.RenderTransformOrigin = new Point(0.5, 0.5);
    }

    private void RemoveImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ImageItem item)
        {
            _imageManager.RemoveImage(item);
            UpdateButtonStates();
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (_imageManager.ImageCount == 0 || _isProcessing) return;

        if (MessageBox.Show("确定要清空所有已添加的图片吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _imageManager.ClearImages();
            ClearPreviewPanel();
            UpdateButtonStates();
        }
    }

    private void TargetSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            var selectedItem = TargetSizeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem == null || selectedItem.Tag == null)
            {
                return;
            }

            var tag = selectedItem.Tag.ToString();

            if (tag == "custom")
            {
                CustomSizeTextBox.Visibility = Visibility.Visible;
                CustomSizeTextBox.Focus();
            }
            else
            {
                CustomSizeTextBox.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error in TargetSizeComboBox_SelectionChanged: {ex.Message}");
        }
    }

    private void CustomSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
    }

    private void CompressionModeBest_Click(object sender, RoutedEventArgs e)
    {
        _useBestCompression = true;
        CompressionModeBest.IsChecked = true;
        CompressionModeGood.IsChecked = false;
    }

    private void CompressionModeGood_Click(object sender, RoutedEventArgs e)
    {
        _useBestCompression = false;
        CompressionModeGood.IsChecked = true;
        CompressionModeBest.IsChecked = false;
    }

    private void SelectOutputPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择图片输出目录",
            InitialDirectory = _outputPath,
            ValidateNames = false,
            CheckFileExists = false,
            CheckPathExists = true,
            FileName = "选择文件夹"
        };

        if (dialog.ShowDialog() == true)
        {
            var selectedPath = Path.GetDirectoryName(dialog.FileName);
            if (!string.IsNullOrEmpty(selectedPath))
            {
                _outputPath = selectedPath;
                OutputPathTextBox.Text = _outputPath;
                SaveConfiguration();
            }
        }
    }

    private void OpenOutputPath_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_outputPath) && Directory.Exists(_outputPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _outputPath,
                UseShellExecute = true
            });
        }
    }

    private void OutputPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var newPath = ((TextBox)sender).Text;
        if (Directory.Exists(newPath))
        {
            _outputPath = newPath;
            SaveConfiguration();
        }
    }

    private void UpdateButtonStates()
    {
        CompressButton.IsEnabled = _imageManager.ImageCount > 0 && !_isProcessing;
        ExportPdfButton.IsEnabled = _imageManager.ImageCount > 0 && !_isProcessing;
    }

    private void CompressButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isProcessing) return;
        if (_imageManager.ImageCount == 0)
        {
            MessageBox.Show("请先添加图片！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrEmpty(_outputPath) || !Directory.Exists(_outputPath))
        {
            MessageBox.Show("请先选择输出目录！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectedItem = TargetSizeComboBox.SelectedItem as ComboBoxItem;
        if (selectedItem == null || selectedItem.Tag == null)
        {
            foreach (ComboBoxItem item in TargetSizeComboBox.Items)
            {
                if (item.Tag != null && item.Tag.ToString() != "custom")
                {
                    TargetSizeComboBox.SelectedItem = item;
                    selectedItem = item;
                    break;
                }
            }
            
            if (selectedItem == null || selectedItem.Tag == null)
            {
                MessageBox.Show("请选择目标大小！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        if (selectedItem.Tag?.ToString() == "custom")
        {
            if (!int.TryParse(CustomSizeTextBox.Text, out _targetKb) || _targetKb <= 0)
            {
                MessageBox.Show("请输入有效的数值！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            _customSizeText = CustomSizeTextBox.Text;
        }
        else
        {
            if (!int.TryParse(selectedItem.Tag?.ToString(), out _targetKb))
            {
                _targetKb = 200;
            }
            _customSizeText = "";
        }

        CompressImages(_targetKb);
    }

    private void CompressImages(int targetKb)
    {
        _isProcessing = true;
        UpdateButtonStates();

        var task = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                foreach (var imageItem in _imageManager.Images)
                {
                    if (!File.Exists(imageItem.FilePath))
                    {
                        Dispatcher.Invoke(() => imageItem.Status = ImageStatus.Error);
                        continue;
                    }

                    var compressedPath = CompressSingleImage(imageItem.FilePath, targetKb);
                    
                    if (!string.IsNullOrEmpty(compressedPath) && File.Exists(compressedPath))
                    {
                        var finalCompressedPath = compressedPath!;
                        Dispatcher.Invoke(() =>
                        {
                            imageItem.CompressedFilePath = finalCompressedPath;
                            imageItem.CompressedSize = new FileInfo(finalCompressedPath).Length;
                            
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(finalCompressedPath);
                            bitmap.EndInit();
                            bitmap.Freeze();
                            imageItem.CompressedImage = bitmap;
                            
                            imageItem.Status = ImageStatus.Compressed;
                        });
                    }
                    else
                    {
                        Dispatcher.Invoke(() => imageItem.Status = ImageStatus.Error);
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    _imageManager.UpdateCompressedStatus();
                    UpdateButtonStates();
                    MessageBox.Show($"压缩完成！成功压缩 {_imageManager.CompressedCount} 张图片", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"压缩出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    _isProcessing = false;
                    UpdateButtonStates();
                });
            }
        });
    }

    private string? CompressSingleImage(string sourcePath, int targetKb)
    {
        try
        {
            var compressionResult = _imageCompressionService.CompressToTargetBytes(sourcePath, targetKb, _useBestCompression);
            var outputExtension = compressionResult.OutputExtension;
            var compressedData = compressionResult.Data;
            string outputPath;

            if (compressedData.Length <= targetKb * 1024)
            {
                Directory.CreateDirectory(_outputPath);
                outputPath = Path.Combine(_outputPath, $"{Path.GetFileNameWithoutExtension(sourcePath)}_compressed{outputExtension}");
                File.WriteAllBytes(outputPath, compressedData);
                return outputPath;
            }

            Directory.CreateDirectory(_outputPath);
            outputPath = Path.Combine(_outputPath, $"{Path.GetFileNameWithoutExtension(sourcePath)}_compressed{outputExtension}");
            File.WriteAllBytes(outputPath, compressedData);

            return outputPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"压缩 {Path.GetFileName(sourcePath)} 失败: {ex.Message}");
            return null;
        }
    }

    private void AddFiles(string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
            return;

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        var validFiles = filePaths
            .Where(filePath => validExtensions.Contains(Path.GetExtension(filePath).ToLower()))
            .ToArray();

        if (validFiles.Length > 0)
        {
            _imageManager.AddImages(validFiles);
            UpdateButtonStates();
        }
    }

    private void BrowseFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.gif;*.bmp|所有文件|*.*",
            Multiselect = true,
            Title = "选择图片文件"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            AddFiles(openFileDialog.FileNames);
        }
    }

    private void OpenDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "选择包含图片的文件夹",
            ShowNewFolderButton = false
        };

        if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var selectedPath = folderBrowserDialog.SelectedPath;
            
            var imageFiles = Directory.GetFiles(selectedPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));

            AddFiles(imageFiles.ToArray());
        }
    }

    private void ExportPdfButton_Click(object sender, RoutedEventArgs e)
    {
        if (_imageManager.ImageCount == 0)
        {
            MessageBox.Show("请先添加图片！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            EnsureQuestPdfInitialized();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"PDF导出组件初始化失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var settingsDialog = new PdfSettingsDialog { Owner = this };
        if (settingsDialog.ShowDialog() != true)
            return;

        var saveDialog = new SaveFileDialog
        {
            Filter = "PDF文件|*.pdf",
            Title = "保存PDF文件",
            FileName = "图片_" + DateTime.Now.ToString("yyyyMMdd_HHmmss")
        };

        if (saveDialog.ShowDialog() == true)
        {
            ExportToPdf(saveDialog.FileName, settingsDialog.Settings);
        }
    }

    private void EnsureQuestPdfInitialized()
    {
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "当前环境无法加载 PDF 导出组件。请使用与程序一致架构的发布版本（x86 或 x64），并确认 QuestPDF 原生依赖文件已随程序一起发布。",
                ex);
        }
    }

    private void ExportToPdf(string outputPath, PdfExportSettings settings)
    {
        var progressWindow = new Window
        {
            Title = "导出PDF",
            Width = 440,
            Height = 220,
            MinHeight = 220,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 250, 252)),
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
            FontFamily = new System.Windows.Media.FontFamily("Segoe UI"),
            ShowInTaskbar = false
        };

        var progressBorder = new Border
        {
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(20),
            Margin = new Thickness(14)
        };

        var progressGrid = new System.Windows.Controls.Grid();
        progressGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        progressGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        progressGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });
        progressGrid.RowDefinitions.Add(new System.Windows.Controls.RowDefinition { Height = GridLength.Auto });

        var statusText = new System.Windows.Controls.TextBlock
        {
            Text = "正在生成PDF...",
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 6)
        };
        System.Windows.Controls.Grid.SetRow(statusText, 0);
        progressGrid.Children.Add(statusText);

        var detailText = new System.Windows.Controls.TextBlock
        {
            Text = "正在准备导出内容，请稍候...",
            FontSize = 12,
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 116, 139)),
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };
        System.Windows.Controls.Grid.SetRow(detailText, 1);
        progressGrid.Children.Add(detailText);

        var progressBar = new System.Windows.Controls.ProgressBar
        {
            Height = 18,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
            Margin = new Thickness(0, 0, 0, 14),
            Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(226, 232, 240))
        };
        System.Windows.Controls.Grid.SetRow(progressBar, 2);
        progressGrid.Children.Add(progressBar);

        var cancelButton = new System.Windows.Controls.Button
        {
            Content = "取消",
            Width = 92,
            Height = 34,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 0),
            Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 245, 249)),
            BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.Medium,
            Cursor = Cursors.Hand
        };
        System.Windows.Controls.Grid.SetRow(cancelButton, 3);
        progressGrid.Children.Add(cancelButton);

        progressBorder.Child = progressGrid;
        progressWindow.Content = progressBorder;

        bool isCancelled = false;
        cancelButton.Click += (s, e) => 
        { 
            isCancelled = true;
            progressWindow.Close();
        };

        var task = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var allImages = _imageManager.Images.ToList();

                int total = allImages.Count;
                int processed = 0;

                if (total == 0)
                {
                    Dispatcher.Invoke(() =>
                    {
                        progressWindow.Close();
                        MessageBox.Show("当前没有可导出的图片。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                    return;
                }

                var pageSize = settings.PageSize switch
                {
                    "A3" => PageSizes.A3,
                    "Letter" => PageSizes.Letter,
                    _ => PageSizes.A4
                };

                var document = Document.Create(container =>
                {
                    foreach (var imageItem in allImages)
                    {
                        if (isCancelled)
                            break;

                        var imagePath = !string.IsNullOrEmpty(imageItem.CompressedFilePath) && File.Exists(imageItem.CompressedFilePath)
                            ? imageItem.CompressedFilePath
                            : imageItem.FilePath;

                        if (File.Exists(imagePath))
                        {
                            processed++;
                            Dispatcher.Invoke(() =>
                            {
                                progressBar.Value = (double)processed / total * 100;
                                statusText.Text = $"正在导出第 {processed} / {total} 张";
                                detailText.Text = $"当前文件: {imageItem.FileName}";
                            });

                            // 每张图片创建单独的一页
                            container.Page(page =>
                            {
                                if (settings.IsLandscape)
                                {
                                    page.Size(pageSize.Landscape());
                                }
                                else
                                {
                                    page.Size(pageSize);
                                }
                                page.Margin(20, Unit.Millimetre);
                                
                                page.Content().Element(content =>
                                {
                                    content.Image(imagePath)
                                        .FitArea();
                                });
                            });
                        }
                    }
                });

                if (!isCancelled)
                {
                    document.GeneratePdf(outputPath);

                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = 100;
                        statusText.Text = "导出完成";
                        detailText.Text = "正在打开生成的 PDF 文件...";
                        progressWindow.Close();
                        
                        // 成功时直接打开PDF，不显示提示
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = outputPath,
                            UseShellExecute = true
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    progressWindow.Close();
                    MessageBox.Show($"PDF导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        });

        progressWindow.ShowDialog();
    }
}

}
