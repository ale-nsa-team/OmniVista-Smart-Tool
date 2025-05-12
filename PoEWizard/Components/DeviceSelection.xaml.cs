using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using static PoEWizard.Data.Constants;
using static PoEWizard.Data.Utils;

namespace PoEWizard.Components
{
    /// <summary>
    /// Interaction logic for DeviceSelection.xaml
    /// </summary>
    public partial class DeviceSelection : Window
    {   
        public List<string> Devices { get;  set; }
        public string Device { get; set; }
        public DeviceType DeviceType { get; set; }

        public DeviceSelection(string port)
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

            _header.Text = $"{Translate("i18n_devSel")} {port}";
            Devices = Enum.GetValues(typeof(DeviceType)).OfType<DeviceType>().ToList().Select(d => GetDescription(d)).ToList();
        }

        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Device = GetDescription(DeviceType);
            DataContext = this;
        }

        public void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            DeviceType = GetValue(Device);
            Close();
        }

        public void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult= false;
            Close();
        }

        private string GetDescription(DeviceType value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            if (fi.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }

        private DeviceType GetValue(string description)
        {
            FieldInfo[] fields = typeof(DeviceType).GetFields();
            var field = fields.SelectMany(f => f.GetCustomAttributes(typeof(DescriptionAttribute), false),
                (f, a) => new { Field = f, Att = a }).Where(a => ((DescriptionAttribute)a.Att)
                .Description == description).SingleOrDefault();
            return field == null ? DeviceType.Other : (DeviceType)field.Field.GetRawConstantValue();
        }
    }
}
