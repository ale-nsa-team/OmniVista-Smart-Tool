using System.Reflection;
using System.Windows;
using static PoEWizard.Data.Constants;
using HtmlAgilityPack;
using System;
using System.Windows.Media.Imaging;
using System.IO;
using PoEWizard.Data;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for HelpViewer.xaml
    /// </summary>
    public partial class HelpViewer : Window
    {
        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public HelpViewer(string hlpFile)
        {
            InitializeComponent();

            if (MainWindow.theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }

            double w = MainWindow.Instance.ActualWidth;
            double h = MainWindow.Instance.ActualHeight;

            this.Width = 0.85 * w;
            this.Height = 0.75 * h;

            Assembly assembly = Assembly.GetEntryAssembly();
            Stream stream = assembly.GetManifestResourceStream($"PoEWizard.Resources.Help.{hlpFile}");
            Stream modified = new MemoryStream();
            StreamWriter writer = new StreamWriter(modified);
            // load images from embedded resources
            HtmlDocument doc = new HtmlDocument();
            doc.Load(stream);
            // find img elements
            foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//img"))
            {
                //change image ref to embedded string
                string src = node.GetAttributeValue("src", "");
                src = ConvertToEmbeddedString(src);
                if (! string.IsNullOrWhiteSpace(src))
                {
                    node.SetAttributeValue("src", src);
                }
            }
            doc.Save(writer);
            writer.Flush();
            modified.Position = 0;

            _hlpBrowser.NavigateToStream(modified);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            SetTitleColor();
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private string ConvertToEmbeddedString(string source)
        {
            try
            {
                Uri uri = new Uri($"pack://application:,,,/Resources/Help/Img/{source}");
                BitmapImage bmi = new BitmapImage(uri);
                if (bmi == null) return null;
                string result = "data:image/png;base64, ";
                return result += Convert.ToBase64String(Encode(bmi));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return null;
        }


        private byte[] Encode(BitmapImage bmi)
        {
            byte[] data = null;
            BitmapEncoder encoder = new PngBitmapEncoder();
            var bmf = BitmapFrame.Create(bmi);
            if (encoder != null)
            {
                encoder.Frames.Add(bmf);
                using (var ms = new MemoryStream())
                {
                    encoder.Save(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    data = ms.ToArray();
                }
            }
            return data;
        }

        private void SetTitleColor()
        {
            IntPtr handle = new WindowInteropHelper(this).Handle;
            int bckgndColor = MainWindow.theme == ThemeType.Dark ? 0x333333 : 0xFFFFFF;
            int textColor = MainWindow.theme == ThemeType.Dark ? 0xFFFFFF : 0x000000;
            DwmSetWindowAttribute(handle, 35, ref bckgndColor, Marshal.SizeOf(bckgndColor));
            DwmSetWindowAttribute(handle, 36, ref textColor, Marshal.SizeOf(textColor));
        }
    }
}
