﻿using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotToday.Models
{
    public class LocalIntelSetting : ObservableObject
    {
        private string processName = "exefile";
        public string ProcessName
        {
            get => processName; set => SetProperty(ref processName, value);
        }
        private int refreshSpan = 100;
        /// <summary>
        /// 更新间隔
        /// 单位毫秒
        /// </summary>
        public int RefreshSpan
        {
            get => refreshSpan; set => SetProperty(ref refreshSpan, value);
        }
        public List<LocalIntelProcSetting> ProcSettings { get; set; } = new List<LocalIntelProcSetting>();
    }

    public class LocalIntelProcSetting : ObservableObject
    {
        [JsonIgnore]
        public IntPtr HWnd { get; set; }
        private string name;
        public string Name
        {
            get => name; set => SetProperty(ref name, value);
        }

        private int x;
        public int X
        {
            get => x; set => SetProperty(ref x, value);
        }
        private int y;
        public int Y
        {
            get => y; set => SetProperty(ref y, value);
        }
        private int width;
        public int Width
        {
            get => width; set => SetProperty(ref width, value);
        }
        private int height;
        public int Height
        {
            get => height; set => SetProperty(ref height, value);
        }

        private string soundFile;
        public string SoundFile
        {
            get => soundFile;
            set => SetProperty(ref soundFile, value);
        }
        private bool windowNotify = true;
        public bool WindowNotify
        {
            get => windowNotify;
            set => SetProperty(ref windowNotify, value);
        }
        private bool toastNotify = true;
        public bool ToastNotify
        {
            get => toastNotify;
            set => SetProperty(ref toastNotify, value);
        }
        private bool soundNotify = true;
        public bool SoundNotify
        {
            get => soundNotify;
            set => SetProperty(ref soundNotify, value);
        }

        private bool notifyDecrease = true;
        /// <summary>
        /// 是否提示减少的情况
        /// </summary>
        public bool NotifyDecrease
        {
            get => notifyDecrease;
            set => SetProperty(ref notifyDecrease, value);
        }

        private bool loop = true;
        public bool Loop
        {
            get => loop;
            set => SetProperty(ref loop, value);
        }

        private int volume = 100;
        public int Volume
        {
            get => volume;
            set => SetProperty(ref volume, value);
        }

        public LocalIntelMode LocalIntelMode { get; set; }

        private LocalIntelAlgorithmParameter algorithmParameter = new LocalIntelAlgorithmParameter();
        public LocalIntelAlgorithmParameter AlgorithmParameter
        {
            get => algorithmParameter;
            set => SetProperty(ref algorithmParameter, value);
        }

        public ObservableCollection<LocalIntelStandingSetting> StandingSettings { get; set; } = new ObservableCollection<LocalIntelStandingSetting>();
        public void ChangeScreenshot(Bitmap img)
        {
            OnScreenshotChanged?.Invoke(this, img);
            img.Dispose();
        }
        public void ChangeEdgeImg(OpenCvSharp.Mat img)
        {
            OnEdgeImgChanged?.Invoke(this, img);
        }
        public void ChangeGrayImg(OpenCvSharp.Mat img)
        {
            OnGrayImgChanged?.Invoke(this, img);
        }
        public void ChangeStandingRects(OpenCvSharp.Mat img, List<OpenCvSharp.Rect> rects)
        {
            OnStandingRectsChanged?.Invoke(this, img, rects);
        }
        public void ChangeMatchRects(OpenCvSharp.Mat img, List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>> matchList)
        {
            OnMatchRectsChanged?.Invoke(this, img, matchList);
        }
        public delegate void ScreenshotChangedDelegate(LocalIntelProcSetting sender, Bitmap img);
        public event ScreenshotChangedDelegate OnScreenshotChanged;

        public delegate void GrayImgChangedDelegate(LocalIntelProcSetting sender, OpenCvSharp.Mat img);
        public event GrayImgChangedDelegate OnGrayImgChanged;

        public delegate void EdgeImgChangedDelegate(LocalIntelProcSetting sender, OpenCvSharp.Mat img);
        public event EdgeImgChangedDelegate OnEdgeImgChanged;

        public delegate void StandingRectsChangedDelegate(LocalIntelProcSetting sender, OpenCvSharp.Mat img, List<OpenCvSharp.Rect> rects);
        public event StandingRectsChangedDelegate OnStandingRectsChanged;

        public delegate void MatchRectsChangedDelegate(LocalIntelProcSetting sender, OpenCvSharp.Mat img, List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>> matchList);
        public event MatchRectsChangedDelegate OnMatchRectsChanged;
    }

    public class LocalIntelStandingSetting : ObservableObject
    {
        private Color color = Color.Red;
        public Color Color
        {
            get => color;
            set => SetProperty(ref color, value);
        }

        private string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }
    }
    /// <summary>
    /// 声望区域识别相关算法参数
    /// </summary>
    public class LocalIntelAlgorithmParameter : ObservableObject
    {
        private int blurSizeW = 3;
        /// <summary>
        /// 高斯模糊Size参数Width
        /// </summary>
        public int BlurSizeW
        {
            get => blurSizeW;
            set => SetProperty(ref blurSizeW, value);
        }

        private int blurSizeH = 3;
        /// <summary>
        /// 高斯模糊Size参数Height
        /// </summary>
        public int BlurSizeH
        {
            get => blurSizeH;
            set => SetProperty(ref blurSizeH, value);
        }

        private int cannyThreshold1 = 100;
        /// <summary>
        /// Canny算子阈值1
        /// </summary>
        public int CannyThreshold1
        {
            get => cannyThreshold1;
            set => SetProperty(ref cannyThreshold1, value);
        }

        private int cannyThreshold2 = 100;
        /// <summary>
        /// Canny算子阈值2
        /// </summary>
        public int CannyThreshold2
        {
            get => cannyThreshold2;
            set => SetProperty(ref cannyThreshold2, value);
        }

        private double fillThresholdV = 0.1f;
        /// <summary>
        /// 垂直方向直线最小有效像素百分比
        /// </summary>
        public double FillThresholdV
        {
            get => fillThresholdV;
            set => SetProperty(ref fillThresholdV, value);
        }

        private double fillThresholdH = 0.1f;
        /// <summary>
        /// 水平方向直线最小有效像素百分比
        /// </summary>
        public double FillThresholdH
        {
            get => fillThresholdH;
            set => SetProperty(ref fillThresholdH, value);
        }

        private int spanLineV = 3;
        /// <summary>
        /// 垂直方向最小有效空白像素行数
        /// </summary>
        public int SpanLineV
        {
            get => spanLineV;
            set => SetProperty(ref spanLineV, value);
        }

        private int minHeight = 5;
        /// <summary>
        /// 声望矩形最小高度
        /// </summary>
        public int MinHeight
        {
            get => minHeight;
            set => SetProperty(ref minHeight, value);
        }

        private int minWidth = 5;
        /// <summary>
        /// 声望矩形最小宽度
        /// </summary>
        public int MinWidth
        {
            get => minWidth;
            set => SetProperty(ref minWidth, value);
        }

        private int mainColorSpan = 2;
        /// <summary>
        /// 声望主颜色识别区域距边缘像素
        /// </summary>
        public int MainColorSpan
        {
            get => mainColorSpan;
            set => SetProperty(ref mainColorSpan, value);
        }

        private double colorMatchThreshold = 0.15;
        /// <summary>
        /// 声望RGB识别阈值百分比
        /// </summary>
        public double ColorMatchThreshold
        {
            get => colorMatchThreshold;
            set => SetProperty(ref colorMatchThreshold, value);
        }

        private int minMatchPixel = 10;
        /// <summary>
        /// 最小匹配像素个数
        /// PointRGB模式专用
        /// </summary>
        public int MinMatchPixel
        {
            get => minMatchPixel;
            set => SetProperty(ref minMatchPixel, value);
        }

        private bool isDelay = false;
        public bool IsDelay
        {
            get => isDelay;
            set => SetProperty(ref isDelay, value);
        }

        private int delay = 500;
        public int Delay
        {
            get => delay;
            set => SetProperty(ref delay, value);
        }
    }

    public class LocalIntelNotify
    {
        public LocalIntelNotify(IntPtr hwnd, string name, string changedMsg, string remainMsg)
        {
            HWnd = hwnd;
            Name = name;
            ChangedMsg = changedMsg;
            RemainMsg = remainMsg;
            Time = DateTime.Now;
        }
        public IntPtr HWnd { get; set; }
        public string Name { get; set; }
        public string ChangedMsg { get; set; }
        public string RemainMsg { get; set; }
        public DateTime Time { get; set; }
    }

    public enum LocalIntelMode
    {
        PointRGB = 0,
        RectRGB = 1
    }
}
