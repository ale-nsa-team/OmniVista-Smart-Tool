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
        public MsgBoxResult Result { get; private set; }

        public CustomMsgBox(Window owner) : this(owner, MsgBoxButtons.Ok) { }

        public CustomMsgBox(Window owner, MsgBoxButtons buttons)
        {
            InitializeComponent();
            light = Resources.MergedDictionaries[0];
            dark = Resources.MergedDictionaries[1];
            if (MainWindow.Theme == ThemeType.Dark)
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
            Buttons = buttons;
            msgIcon.Source = null;
            msgYesBtn.Visibility = Visibility.Collapsed;
            msgNoBtn.Visibility = Visibility.Collapsed;
            msgCancelBtn.Visibility = Visibility.Collapsed;
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            msgHeader.Text = Header ?? "";
            msgBody.Text = Message ?? "";
            switch (Buttons)
            {
                case MsgBoxButtons.Ok:
                    SetButtons(true, false, false, false);
                    break;
                case MsgBoxButtons.Cancel:
                    SetButtons(false, true, false, false);
                    break;
                case MsgBoxButtons.OkCancel:
                    SetButtons(true, true, false, false);
                    break;
                case MsgBoxButtons.YesNo:
                    SetButtons(false, false, true, true);
                    break;
                case MsgBoxButtons.YesNoCancel:
                    SetButtons(false, true, true, true);
                    break;
                case MsgBoxButtons.None:
                    Task.Delay(TimeSpan.FromSeconds(3)).ContinueWith(o => Dispatcher.Invoke(new Action(() => this.Close())));
                    break;
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
        private void MsgYesBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MsgBoxResult.Yes;
            this.Close();
        }

        private void MsgNoBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Result= MsgBoxResult.No;
            this.Close();
        }

        private void MsgCancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Result = MsgBoxResult.Cancel;
            this.Close();
        }

        private void SetButtons(bool ok, bool cancel, bool yes, bool no)
        {
            msgYesBtn.Content = ok ? "OK" : "Yes";
            msgYesBtn.Visibility = ok || yes ? Visibility.Visible : Visibility.Collapsed;
            msgNoBtn.Visibility = no ? Visibility.Visible : Visibility.Collapsed;
            msgCancelBtn.Visibility = cancel ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
