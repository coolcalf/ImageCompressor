using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace ImageCompressor
{
    public class ImageManager : INotifyPropertyChanged
    {
        private readonly ObservableCollection<ImageItem> _images = new();
        private ImageItem? _selectedImage;
        private int _loadingCount;
        private int _totalToLoad;

        public ObservableCollection<ImageItem> Images => _images;

        public ImageItem? SelectedImage
        {
            get => _selectedImage;
            set
            {
                if (_selectedImage != value)
                {
                    // 取消之前图片的PropertyChanged监听
                    if (_selectedImage != null)
                    {
                        _selectedImage.PropertyChanged -= OnSelectedImagePropertyChanged;
                    }

                    _selectedImage = value;

                    // 监听新选中图片的PropertyChanged事件
                    if (_selectedImage != null)
                    {
                        _selectedImage.PropertyChanged += OnSelectedImagePropertyChanged;
                    }

                    OnPropertyChanged(nameof(SelectedImage));
                    LoadFullResolutionImageAsync(value);
                }
            }
        }

        private void OnSelectedImagePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当OriginalImage属性改变时，通知SelectedImage更新
            if (e.PropertyName == nameof(ImageItem.OriginalImage))
            {
                OnPropertyChanged(nameof(SelectedImage));
            }
        }

        public int ImageCount => _images.Count;

        public int CompressedCount => _images.Count(img => img.Status == ImageStatus.Compressed);

        public bool HasCompressedImages => CompressedCount > 0;

        public bool HasNoImages => _images.Count == 0;

        public bool IsLoading => _loadingCount > 0;

        public bool IsNotLoading => !IsLoading;

        public string LoadingStatus => _totalToLoad > 0 
            ? $"正在加载 ({_totalToLoad - _loadingCount}/{_totalToLoad})" 
            : "";

        public ImageManager()
        {
            _images.CollectionChanged += OnImagesCollectionChanged;
        }

        private void OnImagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(ImageCount));
            OnPropertyChanged(nameof(CompressedCount));
            OnPropertyChanged(nameof(HasCompressedImages));
            OnPropertyChanged(nameof(HasNoImages));
            
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems != null && e.NewItems.Count > 0)
            {
                var lastAdded = e.NewItems[e.NewItems.Count - 1] as ImageItem;
                if (lastAdded != null)
                {
                    SelectedImage = lastAdded;
                }
            }
        }

        public void AddImage(string filePath)
        {
            AddImages(new[] { filePath });
        }

        public void AddImages(IEnumerable<string> filePaths)
        {
            var fileList = filePaths.ToList();
            _totalToLoad = fileList.Count;
            _loadingCount = fileList.Count;
            UpdateLoadingStatus();

            foreach (var filePath in fileList)
            {
                AddImageInternal(filePath);
            }
        }

        private void AddImageInternal(string filePath)
        {
            if (!File.Exists(filePath))
            {
                DecrementLoadingCount();
                return;
            }

            if (_images.Any(img => img.FilePath == filePath))
            {
                DecrementLoadingCount();
                return;
            }

            var fileInfo = new FileInfo(filePath);
            var imageItem = new ImageItem
            {
                FilePath = filePath,
                FileName = fileInfo.Name,
                FileSize = fileInfo.Length,
                Status = ImageStatus.Pending
            };

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _images.Add(imageItem);
            });

            Task.Run(() => LoadImageMetadata(imageItem));
            GenerateThumbnailAsync(imageItem);
        }

        private void DecrementLoadingCount()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                _loadingCount--;
                if (_loadingCount <= 0)
                {
                    _loadingCount = 0;
                    _totalToLoad = 0;
                }
                UpdateLoadingStatus();
            });
        }

        private void UpdateLoadingStatus()
        {
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(IsNotLoading));
            OnPropertyChanged(nameof(LoadingStatus));
        }

        public void RemoveImage(ImageItem image)
        {
            _images.Remove(image);
            if (SelectedImage == image)
            {
                SelectedImage = null;
            }
        }

        public void ClearImages()
        {
            _images.Clear();
            SelectedImage = null;
        }

        public void MoveImage(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= _images.Count || newIndex < 0 || newIndex >= _images.Count)
                return;

            var item = _images[oldIndex];
            _images.RemoveAt(oldIndex);
            _images.Insert(newIndex, item);
        }

        private void LoadImageMetadata(ImageItem imageItem)
        {
            try
            {
                using var stream = new FileStream(imageItem.FilePath, FileMode.Open, FileAccess.Read);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnDemand;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                bitmap.Freeze();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    imageItem.Width = bitmap.PixelWidth;
                    imageItem.Height = bitmap.PixelHeight;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading metadata for {imageItem.FileName}: {ex.Message}");
            }
        }

        private async void GenerateThumbnailAsync(ImageItem imageItem)
        {
            await Task.Run(() =>
            {
                try
                {
                    var thumbnail = new BitmapImage();
                    thumbnail.BeginInit();
                    thumbnail.CacheOption = BitmapCacheOption.OnLoad;
                    thumbnail.UriSource = new Uri(imageItem.FilePath);
                    thumbnail.DecodePixelWidth = 100;
                    thumbnail.EndInit();
                    thumbnail.Freeze();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        imageItem.Thumbnail = thumbnail;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error generating thumbnail for {imageItem.FileName}: {ex.Message}");
                }
                finally
                {
                    DecrementLoadingCount();
                }
            });
        }

        private async void LoadFullResolutionImageAsync(ImageItem? imageItem)
        {
            if (imageItem == null || imageItem.OriginalImage != null)
                return;

            await Task.Run(() =>
            {
                try
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(imageItem.FilePath);
                    bitmap.EndInit();
                    bitmap.Freeze();

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        imageItem.OriginalImage = bitmap;
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading full image for {imageItem.FileName}: {ex.Message}");
                }
            });
        }

        public void UpdateCompressedStatus()
        {
            OnPropertyChanged(nameof(CompressedCount));
            OnPropertyChanged(nameof(HasCompressedImages));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
