using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;
using PoEWizard.Data;
using System.Windows.Media;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for TextViewer.xaml
    /// </summary>
    public partial class TextViewer : Window
    {
        private readonly string title;
        private readonly string content;

        public string Filename { get; set; }
        public string SaveFilename { get; set; }
        public string CsvData { get; set; }

        public TextViewer(string title, string content = "", bool canClear = false)
        {
            InitializeComponent();

            if (MainWindow.Theme == Constants.ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[0]);
            }
            else
            {
                Resources.MergedDictionaries.Remove(Resources.MergedDictionaries[1]);
            }
            this.title = title;
            this.content = content;
            this.CsvData = null;
            Title = title;
            _content.FontFamily = new FontFamily("Courier New");
            _content.FontSize = 14;
            _btnClear.Visibility = canClear ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnWindow_Loaded(object sender, RoutedEventArgs e)
        {
            SetTitleColor(this);

            double h = Owner.ActualHeight;
            double w = Owner.ActualWidth;
            Width = w - 20;
            Height = h - 100;
            Top = Owner.Top + 50;
            Left = Owner.Left + 10;

            if (Filename != null && File.Exists(Filename))
            {
                _content.Document.Blocks.Add(new Paragraph(new Run("Loading file, please wait...")));
                Task.Run(() => {
                    Thread.Sleep(200);
                    Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() =>
                    {
                        FileStream fs = new FileStream(Filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        TextRange range = new TextRange(_content.Document.ContentStart, _content.Document.ContentEnd);
                        range.Load(fs, DataFormats.Text);
                        fs.Close();
                    }));
                });
            }
            else
            {
                _content.Document.Blocks.Add(new Paragraph(new Run(content)));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string file = SaveFilename ?? (Filename != null ? Path.GetFileName(Filename) : "");
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Text File|*.txt",
                Title = "Save " + title,
                InitialDirectory = Filename != null ? Path.GetDirectoryName(Filename) : Environment.SpecialFolder.MyDocuments.ToString(),
                FileName = file
            };
            if (sfd.ShowDialog() == true)
            {
                string txt = new TextRange(_content.Document.ContentStart, _content.Document.ContentEnd).Text;
                File.WriteAllText(sfd.FileName, txt);
                if (!string.IsNullOrEmpty(this.CsvData))
                {
                    string csvFileName = Path.Combine(Path.GetDirectoryName(sfd.FileName), $"{Path.GetFileNameWithoutExtension(sfd.FileName)}.csv");
                    File.WriteAllText(csvFileName, this.CsvData);
                }
            }
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            if (Filename == Logger.LogPath) Logger.Clear();
            else if (Filename == Activity.FilePath) Activity.Clear(); 
            _content.Document.Blocks.Clear();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
