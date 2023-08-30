﻿using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

namespace NotToday.Converters
{
    public sealed class DrawColorToUIColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            System.Drawing.Color color = (System.Drawing.Color)value;
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            Color color = (Color)value;
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
        }
    }
}
