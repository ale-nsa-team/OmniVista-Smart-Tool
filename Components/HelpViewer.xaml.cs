using HtmlAgilityPack;
using PoEWizard.Data;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for HelpViewer.xaml
    /// </summary>
    public partial class HelpViewer : Window
    {
        public HelpViewer(string hlpFile)
        {
            InitializeComponent();

            if (MainWindow.Theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            Resources.MergedDictionaries.Add(MainWindow.Strings);

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
            Utils.SetTitleColor(this);
        }

        private void OnNavigating(object sender, NavigatingCancelEventArgs e)
        {
            if (e.Uri != null && !e.Uri.ToString().Contains("about:blank"))
            {
                e.Cancel = true;
                Process.Start(e.Uri.ToString());
            }
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
    }
}
