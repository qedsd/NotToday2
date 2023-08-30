﻿using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotToday.Helpers
{
    internal static class BitmapConveters
    {
        public static BitmapSource ConvertToBitMapSource(Bitmap bitmap)
        {
            BitmapImage bitmapImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                bitmapImage.SetSource(stream.AsRandomAccessStream());
            }
            return bitmapImage;
        }

        public static MemoryStream ConvertToMemoryStream(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            return ms;
        }
        public static Bitmap ConvertFromMemoryStream(MemoryStream ms)
        {
            return new Bitmap(ms);
        }
    }
}
