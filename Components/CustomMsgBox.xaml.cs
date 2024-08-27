using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static PoEWizard.Data.Constants;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for CustomMsgBox.xaml
    /// </summary>
    public partial class CustomMsgBox : Window
    {
        private readonly ResourceDictionary light;
        private readonly ResourceDictionary dark;
        private readonly ResourceDictionary currDict;

        public string Header { get; set; }
        public string Message { get; set; }
        public MsgBoxButtons Buttons { get; set; }
        public MsgBoxIcons Img { get; set; }

        public CustomMsgBox(Window owner, MsgBoxButtons boxButtons)
        {
            InitializeComponent();
            light = Resources.MergedDictionaries[0];
            dark = Resources.MergedDictionaries[1];
            if (MainWindow.theme == ThemeType.Dark)
            {
                Resources.MergedDictionaries.Remove(light);
                currDict = dark;
            }
            else
            {
                Resources.MergedDictionaries.Remove(dark);
                currDict = light;
            }
            this.Owner = owner;
            msgOkBtn.Visibility = Visibility.Collapsed;
            msgCancelBtn.Visibility = Visibility.Collapsed;
            if (boxButtons == MsgBoxButtons.YesNo)
            {
                _okBtnLabel.Content = "Yes";
                _cancelBtnLabel.Content = "No";
            }
            else if (boxButtons == MsgBoxButtons.OkCancel)
            {
                _okBtnLabel.Content = "OK";
                _cancelBtnLabel.Content = "Cancel";
            }
            Buttons = boxButtons;
            msgIcon.Source = null;
        }
        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            msgHeader.Text = Header ?? "";
            msgBody.Text = Message ?? "";
            if (Buttons == MsgBoxButtons.Ok || Buttons == MsgBoxButtons.OkCancel || Buttons == MsgBoxButtons.YesNo) msgOkBtn.Visibility = Visibility.Visible;
            if (Buttons == MsgBoxButtons.Cancel || Buttons == MsgBoxButtons.OkCancel || Buttons == MsgBoxButtons.YesNo) msgCancelBtn.Visibility = Visibility.Visible;
            if (Buttons == MsgBoxButtons.None)
            {
                Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(o => Dispatcher.Invoke(new Action(() => this.Close())));
            }
            switch (Img)
            {
                case MsgBoxIcons.Warning:
                    msgIcon.Source = (ImageSource)currDict["alert"];
                    break;
                case MsgBoxIcons.Error:
                    msgIcon.Source = (ImageSource)currDict["error"];
                    break;
                case MsgBoxIcons.Info:
                    msgIcon.Source = (ImageSource)currDict["info"];
                    break;
                case MsgBoxIcons.Question:
                    msgIcon.Source = (ImageSource)currDict["question"];
                    break;
                default:
                    msgIcon.Visibility = Visibility.Collapsed;
                    _colOne.Width = new GridLength(5);
                    _colTwo.Width = new GridLength(1, GridUnitType.Star);
                    break;
            }
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        private void MsgOkBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void MsgCancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
