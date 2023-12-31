﻿using NotToday.Models;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotToday.Helpers
{
    internal static class IntelImageHelper
    {
        public static Mat ImageToMat(System.Drawing.Image img)
        {
            using (var memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                return Mat.FromStream(memory, ImreadModes.Unchanged);
            }
        }
        public static Mat BitmapToMat(System.Drawing.Bitmap img)
        {
            using (var memory = new MemoryStream())
            {
                img.Save(memory, ImageFormat.Png);
                memory.Position = 0;
                return Mat.FromStream(memory, ImreadModes.Unchanged);
            }
        }
        public static Mat GetGray(Mat input)
        {
            Mat gray = Mat.Zeros(input.Rows, input.Cols, MatType.CV_8UC1);
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }
        public static Mat GetEdge(Mat input, int blurSizeW = 3, int blurSizeH = 3, int cannyThreshold1 = 100, int cannyThreshold2 = 100)
        {
            Mat afterBlur = Mat.Zeros(input.Rows, input.Cols, input.Type());
            Cv2.GaussianBlur(input, afterBlur, new Size(blurSizeW, blurSizeH), 0);
            Mat afterCanny = Mat.Zeros(afterBlur.Rows, afterBlur.Cols, afterBlur.Type());
            Cv2.Canny(afterBlur, afterCanny, cannyThreshold1, cannyThreshold2);
            afterBlur.Dispose();
            return afterCanny;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="spanLine">连续空白多少行才结束一个矩形</param>
        /// <returns></returns>
        public static List<Rect> CalStandingRects(Mat input, double fillThresholdV = 0.1f, double fillThresholdH = 0.1f, int spanLineV = 5, int minHeight = 5, int minWidth = 5)
        {
            var rectsV = FindVerticalRect(input, fillThresholdH, spanLineV, minHeight);
            if (rectsV.Any())
            {
                var rectsH = FindHorizontalRect(input, rectsV, fillThresholdV, minWidth);
                return rectsH;
            }
            else
            {
                return null;
            }
        }
        private static List<Rect> FindVerticalRect(Mat input, double fillThreshold, int spanLine, int minHeight)
        {
            int lineThreshold = (int)(input.Cols * fillThreshold);//每一行颜色超过此阈值才判断为有颜色
            int[] rowFill = new int[input.Rows];//表示每一行是否有颜色
            for (int i = 0; i < input.Rows; i++)
            {
                int count = 0;
                for (int j = 0; j < input.Cols; j++)
                {
                    var p = input.At<byte>(i, j);
                    if (p != 0)
                    {
                        count++;
                        if (count >= lineThreshold)
                        {
                            rowFill[i] = 1;
                            break;
                        }
                    }
                }
            }
            bool foundStartLine = false;//是否已找到当前矩形开始行
            int startRow = -1;//当前矩形最上方所在行数
            int lastFillRow = -1;//最后一次找到有颜色一行的行数
            int emptyLines = 0;
            List<Rect> rects = new List<Rect>();
            List<int> rectHeights = new List<int>();
            for (int i = 0; i < rowFill.Length; i++)
            {
                if (rowFill[i] == 1)
                {
                    if (!foundStartLine)
                    {
                        startRow = i;
                        foundStartLine = true;
                    }
                    lastFillRow = i;
                    emptyLines = 0;
                }
                else
                {
                    emptyLines++;
                    if (foundStartLine && emptyLines > spanLine)
                    {
                        if (lastFillRow - startRow >= minHeight)
                        {
                            //只有矩形高在最小范围内才当作声望区域
                            //结束当前矩形
                            Rect rect = new Rect()
                            {
                                Top = startRow,
                                Height = lastFillRow - startRow,
                                Left = 0,
                                Width = input.Cols
                            };
                            rects.Add(rect);
                            rectHeights.Add(lastFillRow - startRow);
                        }
                        foundStartLine = false;
                        startRow = -1;
                        lastFillRow = -1;
                    }
                }
            }
            //最后一个声望刚好没完全截取的情况下
            if (foundStartLine)
            {
                if (lastFillRow - startRow >= minHeight)//只有矩形高在最小范围内才当作声望区域
                {
                    Rect rect = new Rect()
                    {
                        Top = startRow,
                        Height = lastFillRow - startRow,
                        Left = 0,
                        Width = input.Cols
                    };
                    rects.Add(rect);
                    rectHeights.Add(lastFillRow - startRow);
                }
            }
            return rects;
        }
        private static List<Rect> FindHorizontalRect(Mat input, List<Rect> vRects, double fillThreshold, int minWidth)
        {
            int lineThreshold = (int)(input.Cols * fillThreshold);//每一列颜色超过此阈值才判断为有颜色
            List<Rect> outputRects = new List<Rect>();
            foreach (var rect in vRects)
            {
                int left = 0;
                int right = 0;
                //找最左边第一列满足的
                for (int x = rect.Left; x <= rect.Right; x++)
                {
                    int count = 0;
                    int y = rect.Top;
                    for (; y <= rect.Bottom; y++)
                    {
                        var p = input.At<byte>(y, x);
                        if (p != 0)
                        {
                            count++;
                            if (count >= lineThreshold)
                            {
                                left = x;
                                break;
                            }
                        }
                    }
                    if (y <= rect.Bottom)
                    {
                        break;
                    }
                }
                //找最右边最后一列满足的
                for (int x = rect.Right; x >= rect.Left; x--)
                {
                    int count = 0;
                    int y = rect.Top;
                    for (; y <= rect.Bottom; y++)
                    {
                        var p = input.At<byte>(y, x);
                        if (p != 0)
                        {
                            count++;
                            if (count >= lineThreshold)
                            {
                                right = x;
                                break;
                            }
                        }
                    }
                    if (y <= rect.Bottom)
                    {
                        break;
                    }
                }
                if (right - left >= minWidth)
                {
                    outputRects.Add(new Rect(left, rect.Top, right - left, rect.Height));
                }
            }
            return outputRects;
        }

        public static Mat DrawRects(Mat input, List<Rect> rects, int span = 2)
        {
            Mat output = Mat.Zeros(input.Rows, input.Cols, MatType.CV_8UC3);
            if (rects != null && rects.Any())
            {
                foreach (var rect in rects)
                {
                    var sourceVev3b = GetMainColor(rect, input, span);
                    for (int i = rect.Top; i < rect.Bottom; i++)
                    {
                        for (int j = rect.Left; j < rect.Right; j++)
                        {
                            output.At<Vec3b>(i, j) = sourceVev3b;
                        }
                    }
                }
            }
            return output;
        }
        public static Mat DrawRects(Mat input, List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>> matchList)
        {
            Mat output = input.Clone();
            if (matchList != null && matchList.Any())
            {
                foreach (var match in matchList)
                {
                    var rect = match.Item1;
                    var sourceVev3b = new Vec3b(match.Item2.Color.B, match.Item2.Color.G, match.Item2.Color.R);
                    for (int i = rect.Top; i < rect.Bottom; i++)
                    {
                        for (int j = rect.Left; j < rect.Right; j++)
                        {
                            output.At<Vec3b>(i, j) = sourceVev3b;
                        }
                    }
                }
            }
            return output;
        }
        public static Mat DrawMainColorPos(Mat input, List<Rect> rects, int span = 2)
        {
            Mat output = input.Clone();
            if (rects != null && rects.Any())
            {
                foreach (var rect in rects)
                {
                    var poss = GetMainColorPos(rect, span);
                    foreach (var pos in poss)
                    {
                        output.At<Vec3b>(pos.Y, pos.X) = new Vec3b(0, 255, 0);
                    }
                }
            }
            return output;
        }
        public static Vec3b GetMainColor(Rect rect, Mat input, int span = 2)
        {
            var poss = GetMainColorPos(rect, span);
            if (poss.Count == 1)
            {
                return input.At<Vec3b>(poss[0].Y, poss[0].X);
            }
            else
            {
                int r = 0;
                int g = 0;
                int b = 0;
                byte maxR = byte.MinValue;
                byte maxG = byte.MinValue;
                byte maxB = byte.MinValue;
                byte minR = byte.MaxValue;
                byte minG = byte.MaxValue;
                byte minB = byte.MaxValue;
                foreach (var pos in poss)
                {
                    var rgb = input.At<Vec3b>(pos.Y, pos.X);
                    b += rgb.Item0;
                    g += rgb.Item1;
                    r += rgb.Item2;
                    if (maxB < rgb.Item0)
                    {
                        maxB = rgb.Item0;
                    }
                    if (maxG < rgb.Item1)
                    {
                        maxG = rgb.Item1;
                    }
                    if (maxR < rgb.Item2)
                    {
                        maxR = rgb.Item2;
                    }
                    if (minB > rgb.Item0)
                    {
                        minB = rgb.Item0;
                    }
                    if (minG > rgb.Item1)
                    {
                        minG = rgb.Item1;
                    }
                    if (minR > rgb.Item2)
                    {
                        minR = rgb.Item2;
                    }
                }
                //移除最大最小值
                r -= maxR;
                g -= maxG;
                b -= maxB;
                r -= minR;
                g -= minG;
                b -= minB;
                int actullyCount = poss.Count - 2;
                return new Vec3b((byte)(b / actullyCount), (byte)(g / actullyCount), (byte)(r / actullyCount));
            }
        }
        /// <summary>
        /// 获取左右上下各距离指定像素处的矩形框作为颜色判断区
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        private static List<Point> GetMainColorPos(Rect rect, int span = 2)
        {
            if (span * 2 >= rect.Width || span * 2 > rect.Height)
            {
                //范围太小只取一个点
                return new List<Point>()
                {
                    new Point(rect.Left + span, rect.Top + rect.Height / 2),
                };
            }
            else
            {
                List<Point> points = new List<Point>();
                //左边
                int leftX = rect.Left + span;
                int topY = rect.Top + span;
                int rightX = rect.Right - span;
                int bottomY = rect.Bottom - span;
                for (int y = topY; y <= bottomY; y++)
                {
                    points.Add(new Point(leftX, y));
                }
                //上边
                for (int x = leftX; x <= rightX; x++)
                {
                    points.Add(new Point(x, topY));
                }
                //右边
                for (int y = topY; y <= bottomY; y++)
                {
                    points.Add(new Point(rightX, y));
                }
                //下边
                for (int x = rightX; x >= leftX; x--)
                {
                    points.Add(new Point(x, bottomY));
                }
                return points;
            }
        }

        public static Mat AddBrightness(Mat input, byte r, byte g, byte b)
        {
            Mat output = Mat.Zeros(input.Rows, input.Cols, input.Type());
            for (int i = 0; i < input.Rows; i++)
            {
                for (int j = 0; j < input.Cols; j++)
                {
                    var p = input.At<Vec3b>(i, j);
                    int bb = p.Item0;
                    int gg = p.Item1;
                    int rr = p.Item2;
                    if (p.Item0 <= 255 - b)
                    {
                        bb = p.Item0 + b;
                    }
                    if (p.Item1 <= 255 - g)
                    {
                        gg = p.Item1 + g;
                    }
                    if (p.Item2 <= 255 - r)
                    {
                        rr = p.Item2 + r;
                    }
                    output.At<Vec3b>(i, j) = new Vec3b((byte)bb, (byte)gg, (byte)rr);
                }
            }
            return output;
        }

        public static Mat ChangeRedTo(Mat input, byte targetR, byte targetG, byte targetB)
        {
            Mat output = input.Clone();
            for (int i = 0; i < input.Rows; i++)
            {
                for (int j = 0; j < input.Cols; j++)
                {
                    var p = input.At<Vec3b>(i, j);
                    int bb = p.Item0;
                    int gg = p.Item1;
                    int rr = p.Item2;
                    if(Math.Abs(rr - gg) / (double)rr > 0.8 && Math.Abs(rr - bb) / (double)rr > 0.8)
                    {
                        output.At<Vec3b>(i, j) = new Vec3b(targetB, targetG, targetR);
                    }
                }
            }
            return output;
        }

        public static List<Point> GetMatchPoint(Mat input, byte r, byte g, byte b, double threshold)
        {
            List<Point> points = new List<Point>();
            for (int i = 0; i < input.Rows; i++)
            {
                for (int j = 0; j < input.Cols; j++)
                {
                    var targetColor = input.At<Vec3b>(i, j);
                    ///Vec3b为BGR
                    if ((double)Math.Abs(targetColor.Item0 - b) / 255 < threshold)
                    {
                        if ((double)Math.Abs(targetColor.Item1 - g) / 255 < threshold)
                        {
                            if ((double)Math.Abs(targetColor.Item2 - r) / 255 < threshold)
                            {
                                points.Add(new Point(j, i));
                            }
                        }
                    }
                }
            }
            return points;
        }
    }
}
