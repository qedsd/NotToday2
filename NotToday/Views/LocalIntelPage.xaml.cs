using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NotToday.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization.NumberFormatting;

namespace NotToday.Views
{
    public sealed partial class LocalIntelPage : Page
    {
        public LocalIntelPage()
        {
            this.InitializeComponent();
            Loaded += LocalIntelPage_Loaded;
        }

        private void LocalIntelPage_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= LocalIntelPage_Loaded;
            SetNumberBoxNumberFormatter();
            var window = Helpers.WindowHelper.GetWindowForElement(this) as BaseWindow;
            window.HideAppDisplayName();
            window.SetHeadText($"{Helpers.ResourcesHelper.GetString("AppDisplayName")} - {GetVersionDescription()}");
        }
        private static string GetVersionDescription()
        {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        private void SetNumberBoxNumberFormatter()
        {
            IncrementNumberRounder rounder = new IncrementNumberRounder();
            rounder.Increment = 0.01;
            rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

            DecimalFormatter formatter = new DecimalFormatter();
            formatter.IntegerDigits = 1;
            formatter.FractionDigits = 2;
            formatter.NumberRounder = rounder;
            NumberBox_FillThresholdV.NumberFormatter = formatter;
            NumberBox_FillThresholdH.NumberFormatter = formatter;
            NumberBox_ColorMatchThreshold.NumberFormatter = formatter;
        }

        private void Button_RemoveStanding_Click(object sender, RoutedEventArgs e)
        {
            VM.RemoveStanding((sender as Button).DataContext as LocalIntelStandingSetting);
        }

        public void Close()
        {
            VM.Dispose();
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
