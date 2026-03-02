using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace ImageCompressor
{
    public enum ImageStatus
    {
        Pending,
        Compressed,
        Error
    }

    public class ImageItem : INotifyPropertyChanged
    {
        private string _filePath = string.Empty;
        private string _fileName = string.Empty;
        private long _fileSize;
        private BitmapImage? _thumbnail;
        private BitmapSource? _originalImage;
        private BitmapSource? _compressedImage;
        private string _compressedFilePath = string.Empty;
        private long _compressedSize;
        private ImageStatus _status = ImageStatus.Pending;
        private int _width;
        private int _height;

        public string FilePath
        {
            get => _filePath;
            set
            {
                _filePath = value;
                OnPropertyChanged(nameof(FilePath));
            }
        }

        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        public long FileSize
        {
            get => _fileSize;
            set
            {
                _fileSize = value;
                OnPropertyChanged(nameof(FileSize));
                OnPropertyChanged(nameof(FileSizeDisplay));
            }
        }

        public string FileSizeDisplay => FormatFileSize(FileSize);

        public BitmapImage? Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged(nameof(Thumbnail));
            }
        }

        public BitmapSource? OriginalImage
        {
            get => _originalImage;
            set
            {
                _originalImage = value;
                OnPropertyChanged(nameof(OriginalImage));
            }
        }

        public BitmapSource? CompressedImage
        {
            get => _compressedImage;
            set
            {
                _compressedImage = value;
                OnPropertyChanged(nameof(CompressedImage));
                OnPropertyChanged(nameof(HasCompressedVersion));
            }
        }

        public string CompressedFilePath
        {
            get => _compressedFilePath;
            set
            {
                _compressedFilePath = value;
                OnPropertyChanged(nameof(CompressedFilePath));
            }
        }

        public long CompressedSize
        {
            get => _compressedSize;
            set
            {
                _compressedSize = value;
                OnPropertyChanged(nameof(CompressedSize));
                OnPropertyChanged(nameof(CompressedSizeDisplay));
                OnPropertyChanged(nameof(CompressionRatio));
            }
        }

        public string CompressedSizeDisplay => FormatFileSize(CompressedSize);

        public double CompressionRatio => FileSize > 0 ? (1.0 - (double)CompressedSize / FileSize) * 100 : 0;

        public bool HasCompressedVersion => CompressedImage != null;

        public ImageStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        public string StatusText => Status switch
        {
            ImageStatus.Pending => "待压缩",
            ImageStatus.Compressed => "已压缩",
            ImageStatus.Error => "错误",
            _ => "未知"
        };

        public Brush StatusColor => Status switch
        {
            ImageStatus.Pending => new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)),
            ImageStatus.Compressed => new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
            ImageStatus.Error => new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
            _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(148, 163, 184))
        };

        public int Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged(nameof(Width));
                OnPropertyChanged(nameof(Dimensions));
            }
        }

        public int Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged(nameof(Height));
                OnPropertyChanged(nameof(Dimensions));
            }
        }

        public string Dimensions => $"{Width} × {Height}";

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private static string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            return $"{number:n1} {suffixes[counter]}";
        }
    }
}
