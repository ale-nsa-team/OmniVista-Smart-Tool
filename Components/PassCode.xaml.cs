using PoEWizard.Data;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for PassCode.xaml
    /// </summary>
    public partial class PassCode : Window
    {
        public string SavedPassword;
        public string Password { get; set; }
        
        public PassCode(Window owner)
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
            DataContext = this;
            this.Owner = owner;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SavedPassword = GetPassword();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _pwd.Focus();
        }

        private void Pwd_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) BtnOk_Click(sender, e);
            if (e.Key == Key.Escape) BtnCancel_Click(sender, e);
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ChgPwd_Click(object sender, RoutedEventArgs e)
        {
            ChangePwd cp = new ChangePwd(this.Owner, SavedPassword);
            if (cp.ShowDialog() == true && cp.NewPwd != SavedPassword) SavePassword(cp.NewPwd);
        }

        private string GetPassword()
        {
            string filepath = GetFilePath();
            if (File.Exists(filepath))
            {
                string encPwd = File.ReadAllText(filepath);
                if (!string.IsNullOrEmpty(encPwd)) return DecryptString(encPwd);
            }
            return DEFAULT_PASS_CODE;
        }

        private void SavePassword(string newpwd)
        {
            try
            {
                if (newpwd != null)
                {
                    string filepath = GetFilePath();
                    string np = EncryptString(newpwd);
                    File.WriteAllText(filepath, np);
                    SavedPassword = newpwd;
                    this.DataContext = null;
                    Password = newpwd;
                    this.DataContext = this;
                }
            }
            catch (Exception ex)
            {
                CustomMsgBox cm = new CustomMsgBox(this.Owner)
                {
                    Title = "Change Password",
                    Message = $"Could not change password: {ex.Message}"
                };
                cm.ShowDialog();
            }
        }

        private string GetFilePath()
        {
            string filepath = string.Empty;
            try
            {
                string folder = Path.Combine(MainWindow.DataPath, PASSCODE_FOLDER);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                folder = Path.Combine(folder, PASSCODE_SUB_FOLDER);
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                filepath = Path.Combine(folder, PASSCODE_FILENAME);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return filepath;
        }
    }
}
