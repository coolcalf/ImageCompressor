using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace ImageCompressor;

public partial class MainWindow : Window
{
    private List<string> _imagePaths = new();
    private string _currentCompressedPath = "";
    private bool _isProcessing = false;
    private int _targetKb = 200;
    private string _customSizeText = "";
    private bool _useBestCompression = true;
    private string _outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "压缩后");

    public MainWindow()
    {
        InitializeComponent();
        DropZone.AllowDrop = true;
        LoadConfiguration();
        Closed += MainWindow_Closed;
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
            System.Diagnostics.Debug.WriteLine($"Error saving configuration: {ex.Message}");
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
                        _customSizeText = config.CustomSize;
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
                        _outputPath = config.OutputPath;
                        OutputPathTextBox.Text = _outputPath;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}");
        }
    }

    private string GetSelectedTag()
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

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.All;
        DropZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233));
        DropZone.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
    }

    private void DropZone_MouseEnter(object sender, MouseEventArgs e)
    {
        DropZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(232, 245, 233));
        DropZone.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));
    }

    private void DropZone_MouseLeave(object sender, MouseEventArgs e)
    {
        DropZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
        DropZone.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 204, 204));
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        DropZone.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 255, 255));
        DropZone.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94));

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            _imagePaths = new List<string>((string[])e.Data.GetData(DataFormats.FileDrop));
            UpdateAddedCountText();
            StatusText.Text = $"已添加 {_imagePaths.Count} 张图片，拖放更多图片可以批量压缩";
            UpdateButtonStates();
        }
    }

    private void UpdateAddedCountText()
    {
        if (_imagePaths.Count > 0)
        {
            AddedCountText.Text = $"已添加: {_imagePaths.Count} 张";
            AddedCountText.Visibility = Visibility.Visible;
        }
        else
        {
            AddedCountText.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateButtonStates()
    {
        if (_isProcessing)
        {
            CompressButton.IsEnabled = false;
            ClearButtonLink.IsEnabled = false;
            return;
        }

        CompressButton.IsEnabled = _imagePaths.Count > 0;
        ClearButtonLink.IsEnabled = _imagePaths.Count > 0;
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        if (_imagePaths.Count == 0 || _isProcessing) return;

        if (MessageBox.Show("确定要清空所有已添加的图片吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _imagePaths.Clear();
            UpdateAddedCountText();
            StatusText.Text = "已清空所有图片";
            ResultBorder.Visibility = Visibility.Collapsed;
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
            System.Diagnostics.Debug.WriteLine($"Error in TargetSizeComboBox_SelectionChanged: {ex.Message}");
        }
    }

    private void CustomSizeTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        // 可以在这里添加验证逻辑
    }

    private void CompressionModeBest_Click(object sender, RoutedEventArgs e)
    {
        _useBestCompression = true;
        CompressionModeBest.IsChecked = true;
        CompressionModeGood.IsChecked = false;
        StatusText.Text = "模式：尽可能压缩到目标大小（质量可能下降）";
    }

    private void CompressionModeGood_Click(object sender, RoutedEventArgs e)
    {
        _useBestCompression = false;
        CompressionModeGood.IsChecked = true;
        CompressionModeBest.IsChecked = false;
        StatusText.Text = "模式：保持较好质量，压缩到接近目标大小";
    }

    private void SelectOutputPath_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
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

    private void CompressButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isProcessing) return;
        if (_imagePaths.Count == 0)
        {
            MessageBox.Show("请先拖放图片到指定区域！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        UpdateButtonStates();

        if (string.IsNullOrEmpty(_outputPath) || !Directory.Exists(_outputPath))
        {
            MessageBox.Show("请先选择输出目录！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var selectedItem = TargetSizeComboBox.SelectedItem as ComboBoxItem;
        if (selectedItem?.Tag?.ToString() == "custom")
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
            _targetKb = int.Parse(selectedItem.Tag.ToString());
            _customSizeText = "";
        }

        CompressImages(_targetKb);
    }

    private void CompressImages(int targetKb)
    {
        try
        {
            _isProcessing = true;
            UpdateButtonStates();

            var watch = System.Diagnostics.Stopwatch.StartNew();
            int compressedCount = 0;

            for (int i = 0; i < _imagePaths.Count; i++)
            {
                var imagePath = _imagePaths[i];
                if (!File.Exists(imagePath))
                {
                    UpdateStatus($"跳过不存在的文件: {Path.GetFileName(imagePath)}");
                    continue;
                }

                UpdateStatus($"压缩进度: {i + 1}/{_imagePaths.Count} - {Path.GetFileName(imagePath)}");

                var compressedPath = CompressSingleImage(imagePath, targetKb);

                if (!string.IsNullOrEmpty(compressedPath) && File.Exists(compressedPath))
                {
                    compressedCount++;
                }

                System.Threading.Tasks.Task.Delay(5).Wait();
            }

            var elapsed = watch.Elapsed;
            UpdateStatus($"压缩完成！成功压缩 {compressedCount} 张图片");

            ShowResult(targetKb);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"压缩出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateStatus("压缩失败，请检查日志");
        }
        finally
        {
            _isProcessing = false;
            UpdateButtonStates();
        }
    }

    private string CompressSingleImage(string sourcePath, int targetKb)
    {
        try
        {
            using var sourceImage = Bitmap.FromFile(sourcePath);
            using var memoryStream = new MemoryStream();
            int quality = 70;

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            var imageCodecInfo = GetImageCodecInfo(sourcePath);
            if (imageCodecInfo == null)
            {
                throw new Exception("无法识别图片格式");
            }

            sourceImage.Save(memoryStream, imageCodecInfo, encoderParams);

            var compressedData = memoryStream.ToArray();
            string outputPath;

            if (compressedData.Length <= targetKb * 1024)
            {
                outputPath = Path.Combine(_outputPath, $"{Path.GetFileNameWithoutExtension(sourcePath)}_compressed{Path.GetExtension(sourcePath)}");
                File.WriteAllBytes(outputPath, compressedData);
                return outputPath;
            }

            if (CompressionModeBest.IsChecked == true)
            {
                compressedData = CompressWithBinarySearch(sourcePath, targetKb, quality);
            }
            else
            {
                compressedData = CompressWithQualityBySteps(sourcePath, targetKb, quality);
            }

            Directory.CreateDirectory(_outputPath);
            outputPath = Path.Combine(_outputPath, $"{Path.GetFileNameWithoutExtension(sourcePath)}_compressed{Path.GetExtension(sourcePath)}");
            File.WriteAllBytes(outputPath, compressedData);

            return outputPath;
        }
        catch (Exception ex)
        {
            UpdateStatus($"压缩 {Path.GetFileName(sourcePath)} 失败: {ex.Message}");
            return null;
        }
    }

    private byte[] CompressWithBinarySearch(string sourcePath, int targetKb, int currentQuality)
    {
        int minQuality = 1;
        int maxQuality = currentQuality;
        byte[] bestResult = null;
        int bestQuality = currentQuality;

        while (minQuality <= maxQuality)
        {
            int midQuality = (minQuality + maxQuality) / 2;

            using var image = Bitmap.FromFile(sourcePath);
            using var tempStream = new MemoryStream();

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, midQuality);

            var imageCodecInfo = GetImageCodecInfo(sourcePath);
            if (imageCodecInfo == null) throw new Exception("无法识别图片格式");

            image.Save(tempStream, imageCodecInfo, encoderParams);

            var result = tempStream.ToArray();

            if (result.Length <= targetKb * 1024)
            {
                bestResult = result;
                bestQuality = midQuality;
                minQuality = midQuality + 1;
            }
            else
            {
                maxQuality = midQuality - 1;
            }

            System.Threading.Tasks.Task.Delay(5).Wait();
        }

        return bestResult ?? new byte[0];
    }

    private byte[] CompressWithQualityBySteps(string sourcePath, int targetKb, int currentQuality)
    {
        byte[] result = null;
        int quality = currentQuality;

        while (quality >= 1)
        {
            using var image = Bitmap.FromFile(sourcePath);
            using var tempStream = new MemoryStream();

            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);

            var imageCodecInfo = GetImageCodecInfo(sourcePath);
            if (imageCodecInfo == null) throw new Exception("无法识别图片格式");

            image.Save(tempStream, imageCodecInfo, encoderParams);

            result = tempStream.ToArray();

            if (result.Length <= targetKb * 1024)
            {
                break;
            }

            quality -= 5;

            System.Threading.Tasks.Task.Delay(5).Wait();
        }

        return result ?? new byte[0];
    }

    private ImageCodecInfo GetImageCodecInfo(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        ImageCodecInfo codecInfo = null;

        foreach (var codec in ImageCodecInfo.GetImageEncoders())
        {
            if (codec.MimeType.StartsWith("image/"))
            {
                var mimeParts = codec.MimeType.Split('/');
                var format = mimeParts[1].Replace("jpeg", "jpg");

                if (extension == $".{format}" || (extension == ".jpg" && format == "jpeg") ||
                    (extension == ".png" && format == "png") ||
                    (extension == ".gif" && format == "gif") ||
                    (extension == ".bmp" && format == "bmp"))
                {
                    codecInfo = codec;
                    break;
                }
            }
        }

        return ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");
    }

    private void ShowResult(int targetKb)
    {
        ResultBorder.Visibility = Visibility.Visible;
        ResultBorder.Margin = new Thickness(0, 10, 10, 10);

        long totalOriginalSize = 0;
        long totalCompressedSize = 0;
        long compressedCount = 0;
        var details = new List<string>();

        foreach (var imagePath in _imagePaths)
        {
            if (File.Exists(imagePath))
            {
                var fileInfo = new FileInfo(imagePath);
                totalOriginalSize += fileInfo.Length;

                var outputFileName = $"{Path.GetFileNameWithoutExtension(imagePath)}_compressed{Path.GetExtension(imagePath)}";
                var outputFilePath = Path.Combine(_outputPath, outputFileName);

                if (File.Exists(outputFilePath))
                {
                    var compressedFileInfo = new FileInfo(outputFilePath);
                    totalCompressedSize += compressedFileInfo.Length;
                    compressedCount++;

                    if (details.Count < 3)
                    {
                        var rate = 1 - (double)compressedFileInfo.Length / fileInfo.Length;
                        details.Add($"• {Path.GetFileName(imagePath)}: {FormatFileSize(compressedFileInfo.Length)} ({(rate * 100):F1}%)");
                    }
                }
            }
        }

        ImageCountText.Text = $"共压缩 {compressedCount}/{_imagePaths.Count} 张图片";

        if (compressedCount > 0)
        {
            var avgOriginal = (double)totalOriginalSize / compressedCount;
            var avgCompressed = (double)totalCompressedSize / compressedCount;
            var avgRate = 1 - avgCompressed / avgOriginal;

            AvgOriginalSizeText.Text = FormatFileSize((long)avgOriginal);
            AvgCompressedSizeText.Text = FormatFileSize((long)avgCompressed);
            AvgCompressionRateText.Text = $"{(avgRate * 100):F1}%";
        }
        else
        {
            AvgOriginalSizeText.Text = "-";
            AvgCompressedSizeText.Text = "-";
            AvgCompressionRateText.Text = "-";
        }

        var detailsText = string.Join("\n", details);
        if (details.Count > 0)
        {
            DetailsText.Text = "压缩结果:\n" + detailsText;
        }
        else
        {
            DetailsText.Text = "未找到压缩文件，请检查输出目录";
        }
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private void UpdateStatus(string message)
    {
        Dispatcher.Invoke(() =>
        {
            StatusText.Text = message;
        });
    }

    private void AddFiles(string[] filePaths)
    {
        if (filePaths == null || filePaths.Length == 0)
            return;

        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };

        foreach (var filePath in filePaths)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            if (validExtensions.Contains(extension))
            {
                _imagePaths.Add(filePath);
            }
        }

        UpdateAddedCountText();
        UpdateButtonStates();

        if (_imagePaths.Count > 0)
        {
            StatusText.Text = $"已添加 {_imagePaths.Count} 张图片，拖放更多图片可以批量压缩";
        }
    }

    private void BrowseFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
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
            _outputPath = folderBrowserDialog.SelectedPath;
            OutputPathTextBox.Text = _outputPath;
            SaveConfiguration();

            // Scan folder for images
            var imageFiles = Directory.GetFiles(_outputPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase));

            AddFiles(imageFiles.ToArray());
        }
    }
}