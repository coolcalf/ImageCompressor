using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageCompressor
{
    public sealed class ImageCompressionResult
    {
        public ImageCompressionResult(byte[] data, string outputExtension)
        {
            Data = data;
            OutputExtension = outputExtension;
        }

        public byte[] Data { get; }

        public string OutputExtension { get; }
    }

    public class ImageCompressionService
    {
        public ImageCompressionResult CompressToTargetBytes(string sourcePath, int targetKb, bool useBestCompression)
        {
            using var sourceImage = (Bitmap)Bitmap.FromFile(sourcePath);
            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();

            if (extension != ".jpg" && extension != ".jpeg")
            {
                using var jpegBitmap = CreateJpegCompatibleBitmap(sourceImage);
                var jpegBytes = CompressBitmapAsJpeg(jpegBitmap, targetKb, useBestCompression);
                return new ImageCompressionResult(jpegBytes, ".jpg");
            }

            var imageCodecInfo = GetImageCodecInfo(extension);
            if (imageCodecInfo == null)
            {
                throw new InvalidOperationException("无法识别图片格式");
            }

            if (!SupportsQualityAdjustment(extension))
            {
                return new ImageCompressionResult(SaveImage(sourceImage, imageCodecInfo, quality: null), extension);
            }

            const int initialQuality = 70;
            var compressedData = SaveImage(sourceImage, imageCodecInfo, initialQuality);
            if (compressedData.Length <= targetKb * 1024)
            {
                return new ImageCompressionResult(compressedData, extension);
            }

            compressedData = useBestCompression
                ? CompressWithBinarySearch(sourcePath, targetKb, initialQuality)
                : CompressWithQualityBySteps(sourcePath, targetKb, initialQuality);

            return new ImageCompressionResult(compressedData, extension);
        }
        private static Bitmap CreateJpegCompatibleBitmap(Bitmap sourceImage)
        {
            var bitmap = new Bitmap(sourceImage.Width, sourceImage.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            using var graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.White);
            graphics.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);

            return bitmap;
        }

        private byte[] CompressWithBinarySearch(string sourcePath, int targetKb, int currentQuality)
        {
            int minQuality = 1;
            int maxQuality = currentQuality;
            byte[]? bestResult = null;
            byte[]? smallestResult = null;
            var imageCodecInfo = GetImageCodecInfo(Path.GetExtension(sourcePath).ToLowerInvariant());
            if (imageCodecInfo == null)
            {
                throw new InvalidOperationException("无法识别图片格式");
            }

            while (minQuality <= maxQuality)
            {
                int midQuality = (minQuality + maxQuality) / 2;

                using var image = Bitmap.FromFile(sourcePath);
                var result = SaveImage(image, imageCodecInfo, midQuality);

                if (smallestResult == null || result.Length < smallestResult.Length)
                {
                    smallestResult = result;
                }

                if (result.Length <= targetKb * 1024)
                {
                    bestResult = result;
                    minQuality = midQuality + 1;
                }
                else
                {
                    maxQuality = midQuality - 1;
                }
            }

            return bestResult ?? smallestResult ?? Array.Empty<byte>();
        }

        private byte[] CompressWithQualityBySteps(string sourcePath, int targetKb, int currentQuality)
        {
            byte[]? result = null;
            byte[]? smallestResult = null;
            int quality = currentQuality;
            var imageCodecInfo = GetImageCodecInfo(Path.GetExtension(sourcePath).ToLowerInvariant());
            if (imageCodecInfo == null)
            {
                throw new InvalidOperationException("无法识别图片格式");
            }

            while (quality >= 1)
            {
                using var image = Bitmap.FromFile(sourcePath);
                result = SaveImage(image, imageCodecInfo, quality);

                if (smallestResult == null || result.Length < smallestResult.Length)
                {
                    smallestResult = result;
                }

                if (result.Length <= targetKb * 1024)
                {
                    break;
                }

                quality -= 5;
            }

            return result != null && result.Length <= targetKb * 1024
                ? result
                : smallestResult ?? Array.Empty<byte>();
        }

        private byte[] CompressBitmapAsJpeg(Bitmap sourceImage, int targetKb, bool useBestCompression)
        {
            var jpegCodec = GetImageCodecInfo(".jpg");
            if (jpegCodec == null)
            {
                throw new InvalidOperationException("无法识别 JPEG 编码器");
            }

            const int initialQuality = 70;
            var initial = SaveImage(sourceImage, jpegCodec, initialQuality);
            if (initial.Length <= targetKb * 1024)
            {
                return initial;
            }

            return useBestCompression
                ? CompressBitmapJpegWithBinarySearch(sourceImage, targetKb, initialQuality, jpegCodec)
                : CompressBitmapJpegWithQualityBySteps(sourceImage, targetKb, initialQuality, jpegCodec);
        }

        private static byte[] CompressBitmapJpegWithBinarySearch(Bitmap sourceImage, int targetKb, int currentQuality, ImageCodecInfo jpegCodec)
        {
            int minQuality = 1;
            int maxQuality = currentQuality;
            byte[]? bestResult = null;
            byte[]? smallestResult = null;

            while (minQuality <= maxQuality)
            {
                int midQuality = (minQuality + maxQuality) / 2;
                var result = SaveImage(sourceImage, jpegCodec, midQuality);

                if (smallestResult == null || result.Length < smallestResult.Length)
                {
                    smallestResult = result;
                }

                if (result.Length <= targetKb * 1024)
                {
                    bestResult = result;
                    minQuality = midQuality + 1;
                }
                else
                {
                    maxQuality = midQuality - 1;
                }
            }

            return bestResult ?? smallestResult ?? Array.Empty<byte>();
        }

        private static byte[] CompressBitmapJpegWithQualityBySteps(Bitmap sourceImage, int targetKb, int currentQuality, ImageCodecInfo jpegCodec)
        {
            byte[]? result = null;
            byte[]? smallestResult = null;
            int quality = currentQuality;

            while (quality >= 1)
            {
                result = SaveImage(sourceImage, jpegCodec, quality);

                if (smallestResult == null || result.Length < smallestResult.Length)
                {
                    smallestResult = result;
                }

                if (result.Length <= targetKb * 1024)
                {
                    break;
                }

                quality -= 5;
            }

            return result != null && result.Length <= targetKb * 1024
                ? result
                : smallestResult ?? Array.Empty<byte>();
        }

        private static byte[] SaveImage(Image image, ImageCodecInfo codec, int? quality)
        {
            using var stream = new MemoryStream();

            if (quality.HasValue)
            {
                using var encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality.Value);
                image.Save(stream, codec, encoderParams);
            }
            else
            {
                image.Save(stream, codec, null);
            }

            return stream.ToArray();
        }

        private static bool SupportsQualityAdjustment(string extension)
        {
            return extension == ".jpg" || extension == ".jpeg";
        }

        private static ImageCodecInfo? GetImageCodecInfo(string extension)
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (!codec.MimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var mimeParts = codec.MimeType.Split('/');
                var format = mimeParts[1].Replace("jpeg", "jpg");

                if (extension == $".{format}" || (extension == ".jpg" && format == "jpeg"))
                {
                    return codec;
                }
            }

            return ImageCodecInfo.GetImageEncoders().FirstOrDefault(c => c.MimeType == "image/jpeg");
        }
    }
}
