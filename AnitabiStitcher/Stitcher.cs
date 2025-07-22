using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace AnitabiStitcher
{
    public static class Stitcher
    {
        public static async Task<byte[]> StitchImagesAsync(List<string> imagePaths, int width, int margin, int space, string? description = null)
        {
            if (imagePaths == null || imagePaths.Count != 2)
                throw new ArgumentException("you need to give two pathes of images");

            // 加载图片为 SKBitmap 并缩放到统一宽度
            List<SKBitmap> scaledBitmaps = new();
            foreach (var path in imagePaths)
            {
                using var stream = File.OpenRead(path);
                using var codec = SKCodec.Create(stream);
                var original = SKBitmap.Decode(codec);

                float scale = (float)(width - 2 * margin) / original.Width;//计算缩放系数
                int newHeight = (int)(original.Height * scale);
                var scaled = original.Resize(new SKImageInfo(width - 2 * margin, newHeight), SKFilterQuality.High);
                if (scaled == null)
                    throw new Exception("failed");
                scaledBitmaps.Add(scaled);
            }

            int totalImageHeight = scaledBitmaps[0].Height + scaledBitmaps[1].Height + space;
            int descriptionHeight = 0;
            int descriptionMargin = 0;

            SKPaint? textPaint = null;
            if (!string.IsNullOrWhiteSpace(description))
            {
                textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = 64,
                    IsAntialias = true,
                    Typeface = SKTypeface.FromFamilyName("方正兰亭中黑_GBK"),
                    //Typeface = SKTypeface.Default,
                    TextAlign = SKTextAlign.Left
                };

                var textLines = WrapText(description, textPaint, width - 2 * margin);
                descriptionHeight = textLines.Count * (int)(textPaint.TextSize + 10);
                descriptionMargin = 0;
            }

            int canvasHeight = margin + totalImageHeight + margin + descriptionMargin + descriptionHeight;

            if (description != "")
            {
                canvasHeight += margin;
            }

            var info = new SKImageInfo(width, canvasHeight);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);

            // 画第一张图
            int y = margin;
            canvas.DrawBitmap(scaledBitmaps[0], new SKPoint(margin, y));

            // 画第二张图
            y += scaledBitmaps[0].Height + space;
            canvas.DrawBitmap(scaledBitmaps[1], new SKPoint(margin, y));

            // 画描述文字
            if (!string.IsNullOrWhiteSpace(description) && textPaint != null)
            {
                y += scaledBitmaps[1].Height + margin;
                var lines = WrapText(description, textPaint, width - 2 * margin);
                foreach (var line in lines)
                {
                    canvas.DrawText(line, margin, y + textPaint.TextSize, textPaint);
                    y += (int)(textPaint.TextSize + 10);
                }
            }

            // 导出为PNG
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }

        // 将长文本按行拆分为不超过最大宽度的多行
        private static List<string> WrapText(string text, SKPaint paint, int maxWidth)
        {
            var result = new List<string>();

            // 首先按用户手动输入的换行符分段
            var paragraphs = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            foreach (var paragraph in paragraphs)
            {
                var words = paragraph.Split(' ');
                var sb = new StringBuilder();

                foreach (var word in words)
                {
                    var test = sb.Length == 0 ? word : sb + " " + word;

                    if (paint.MeasureText(test) > maxWidth)
                    {
                        if (sb.Length > 0)
                        {
                            result.Add(sb.ToString());
                            sb.Clear();
                        }
                        // 特别处理：这个词本身太长也无法显示一行（可能是英文长单词或无空格的文本）
                        if (paint.MeasureText(word) > maxWidth)
                        {
                            var chars = word.ToCharArray();
                            var temp = new StringBuilder();
                            foreach (var c in chars)
                            {
                                temp.Append(c);
                                if (paint.MeasureText(temp.ToString()) > maxWidth+64)//64 is text size
                                {
                                    temp.Remove(temp.Length - 1, 1);
                                    result.Add(temp.ToString());
                                    temp.Clear();
                                    temp.Append(c);
                                }
                            }
                            if (temp.Length > 0)
                                sb.Append(temp.ToString());
                        }
                        else
                        {
                            sb.Append(word);
                        }
                    }
                    else
                    {
                        if (sb.Length > 0)
                            sb.Append(" ");
                        sb.Append(word);
                    }
                }
                if (sb.Length > 0)
                    result.Add(sb.ToString());
            }

            return result;
        }

        //logics:首先按照输入的将其分为多段。

        //private static List<string> WrapTextFixed(string text, SKPaint paint)

    }
}
