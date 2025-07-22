using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System;
using Windows.UI.ApplicationSettings;
//using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AnitabiStitcher
{
    public sealed partial class AniStiPage : Page
    {
        #region variables
        private List<BitmapImage> loadedImages = new();
        private List<string> loadedImagePaths = new();
        private int totalWidth = 0;
        private int edgeMargin = 0;
        private int space = 0;
        private string discription = string.Empty;
        //private string outputPath = string.Empty;
        //private string fontPath = string.Empty;
        //逻辑：先尝试加载json文件，若空，则使用推荐的默认值
        #endregion

        public AniStiPage()
        {
            InitializeComponent();
            loadedImages.Clear();
            loadedImagePaths.Clear();
            //loadPreferences();

        }

        #region UIHandle

        private async void BrowseImages_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App._window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();
            if (files == null || files.Count == 0)
                return;

            if (files.Count + loadedImages.Count> 2)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "最多只能选择两张图片",
                    Content = "请选择不超过两张图像。",
                    CloseButtonText = "确定",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            foreach (var file in files)
            {
                // 加载图片到 BitmapImage
                using var stream = await file.OpenAsync(FileAccessMode.Read);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                loadedImages.Add(bitmap);
                loadedImagePaths.Add(file.Path);
            }

            LoadUIImageContainer();
        }

        private void DeleteImages_Click(object sender, RoutedEventArgs e)
        {
            loadedImages.Clear();
            loadedImagePaths.Clear();
            LoadUIImageContainer();
        }

        private async void StartStitching_Click(object sender, RoutedEventArgs e)
        {
            if (await ImageEnough())
            {
                HandleDataInAllTextbox();
                //System.Diagnostics.Debug.WriteLine(totalWidth);
                //foreach(var path in loadedImagePaths)
                //{
                //    System.Diagnostics.Debug.WriteLine(path);
                //}
                byte[] pngData = await Stitcher.StitchImagesAsync(loadedImagePaths, totalWidth, edgeMargin, space, discription);
                string filePathPrefix = "C:\\Users\\9EI\\Desktop\\test\\";
                string filePathSuffix = GenerateUniqueFileName(pngData);
                string finalPath = filePathPrefix + filePathSuffix + ".png";
                await File.WriteAllBytesAsync(finalPath, pngData);
            }
            else return;
        }

        private void NumberBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            // 如果输入中包含非数字字符，就阻止这次输入
            if (!Regex.IsMatch(args.NewText, @"^\d*$")) // 允许空文本或纯数字
            {
                args.Cancel = true;
            }
        }

        private void IntegerInput_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)//这里是怎么取到的？
            {
                // 判断当前是哪一个控件
                if (tb == TotalWidth)
                    ValidateIntegerInRange(tb, 256, 16384);
                else if (tb == EdgeMargin)
                    ValidateIntegerInRange(tb, 0, 1000);
                else if (tb == Space)
                    ValidateIntegerInRange(tb, 0, 1000);
            }
        }

        private async void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            var result = await SettingsDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 应用设置
                double fontSize = FontSizeSlider.Value;
                string fontPath = FontPathTextBox.Text;

                // 你可以在这里更新UI或保存配置
            }
            else
            {
                // 用户取消，无需处理
            }
        }



        #endregion

        #region helpfuncs

        private void LoadUIImageContainer()
        {
            ImageContainer.Children.Clear();

            foreach (var bitmap in loadedImages)
            {
                var image = new Image
                {
                    Source = bitmap,
                    Width = 180,
                    Height = 180,
                    Stretch = Stretch.Uniform,
                    Margin = new Thickness(4),
                };

                ImageContainer.Children.Add(image);
            }
        }

        private async Task<bool> ImageEnough()
        {
            if (loadedImages.Count != 2)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = "选择图片不足",
                    Content = "请选择两张图片",
                    CloseButtonText = "确定",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
                //System.Diagnostics.Debug.WriteLine("image not enough");
                return false;
            }else return true; 
        }

        private void ValidateIntegerInRange(TextBox textBox, int min, int max)
        {
            if (int.TryParse(textBox.Text, out int value))
            {
                if (value < min)
                    textBox.Text = min.ToString();
                else if (value > max)
                    textBox.Text = max.ToString();
                // 合法范围内，不改动
            }
            else
            {
                textBox.Text = string.Empty; // 非法输入
            }
        }

        private void HandleDataInAllTextbox()
        {
            var textboxes = new[] {TotalWidth, EdgeMargin, Space, Discription};

            foreach (var textbox in textboxes)
            {
                HandleDataInTextbox(textbox);
            }
        }


        private void HandleDataInTextbox(TextBox textBox)
        {
            TextBox tb = textBox;
            if (tb == TotalWidth)
            {
                //如果空则置2160
                //如果不空则赋值
                //if（tb.Text）
                GiveValue(tb, ref totalWidth, 2160);
            }
            //ValidateIntegerInRange(tb, 256, 16384);
            else if (tb == EdgeMargin)
            {
                GiveValue(tb, ref edgeMargin, 90);
            }
            //ValidateIntegerInRange(tb, 0, 1000);
            else if (tb == Space)
            {
                GiveValue(tb, ref space, 90);
            }
            else if (tb == Discription) discription = Discription.Text;
                //ValidateIntegerInRange(tb, 0, 1000);
        }

        private void GiveValue(TextBox textBox, ref int var, int fillValue)
        {
            if (int.TryParse(textBox.Text, out int value))//有值
            {
                var = value;
            }
            else var = fillValue;
        }

        private async void loadPreferences()
        {
            //load preferences from root content

        }

        private async void savePreferences()
        {
            //when click confirm button, save preference
        }

        private string GenerateUniqueFileName(byte[] data)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(data);
                // 将字节数组转换为十六进制字符串
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                    sb.Append(b.ToString("x2"));

                return sb.ToString(); // 可以加扩展名，比如 + ".bin"
            }
        }

    }

    #endregion
}
