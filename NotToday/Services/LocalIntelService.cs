using NotToday.Helpers;
using NotToday.Models;
using NotToday.Notifications;
using NotToday.Wins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace NotToday.Services
{
    /// <summary>
    /// 用于计算声望变化和发出通知
    /// 每个LocalIntelPage对应一个service
    /// </summary>
    internal class LocalIntelService
    {
        class StandingChange
        {
            public LocalIntelStandingSetting Setting;
            public int Change;
            public int Remain;
        }
        private readonly Dictionary<string, List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>>> _lastStandingsDic = new Dictionary<string, List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>>>();
        private readonly Dictionary<string, long[]> _lastPointRGBDic = new Dictionary<string, long[]>();
        private static LocalIntelService current;
        public static LocalIntelService Current
        {
            get
            {
                current ??= new LocalIntelService();
                return current;
            }
        }
        private LocalIntelService()
        {
        }
        public void Add(LocalIntelProcSetting item)
        {
            _lastStandingsDic.Add(item.Name, new List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>>());
            item.OnScreenshotChanged += Item_OnScreenshotChanged;
        }
        public void Remove(LocalIntelProcSetting item)
        {
            _lastStandingsDic.Remove(item.Name);
            _lastPointRGBDic.Remove(item.Name);
            item.OnScreenshotChanged -= Item_OnScreenshotChanged;
            RemoveMediaPlayer(item.HWnd);
            if (!_lastStandingsDic.Any())
            {
                _window?.ClearMsg();
                _window?.Hide();
            }
        }
        public void Dispose()
        {
            foreach(var player in MediaPlayers.Values)
            {
                player.Pause();
                player.Source = null;
                player.Dispose();
            }
            MediaPlayers.Clear();
            foreach (var m in MediaSourceDic.Values)
            {
                m.Dispose();
            }
            MediaSourceDic.Clear();
            defaultMediaSource?.Dispose();
            _window?.Dispose();
        }
        private void Item_OnScreenshotChanged(LocalIntelProcSetting sender, System.Drawing.Bitmap img)
        {
            if(sender.LocalIntelMode == LocalIntelMode.PointRGB)
            {
                if(sender.Delay > 0)
                {
                    Analyse1_2(sender, img);
                }
                else
                {
                    Analyse1_1(sender, img);
                }
            }
            else
            {
                Analyse2(sender, img);
            }
        }
        private long[] Analyse1_FindCurSums(LocalIntelProcSetting sender, System.Drawing.Bitmap img)
        {
            long[] curSums = new long[sender.StandingSettings.Count];
            var sourceMat = IntelImageHelper.BitmapToMat(img);
            for (int i = 0; i < sender.StandingSettings.Count; i++)
            {
                var setting = sender.StandingSettings[i];
                var points = IntelImageHelper.GetMatchPoint(sourceMat, setting.Color.R, setting.Color.G, setting.Color.B, sender.AlgorithmParameter.ColorMatchThreshold);
                long sum = 0;//使用每个点xy总和来记录，可能存在小概率误报
                sum += points.Count;
                curSums[i] = sum;
            }
            sourceMat.Dispose();
            return curSums;
        }
        /// <summary>
        /// PointRGB模式，不启用延迟
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="img"></param>
        /// <exception cref="Exception"></exception>
        private void Analyse1_1(LocalIntelProcSetting sender, System.Drawing.Bitmap img)
        {
            long[] lastSums;
            if (_lastPointRGBDic.TryGetValue(sender.Name, out var value))
            {
                lastSums = value;
            }
            else
            {
                lastSums = new long[sender.StandingSettings.Count];
            }
            var curSums = Analyse1_FindCurSums(sender, img);
            StringBuilder stringBuilder = new StringBuilder();
            for(int i = 0;i< lastSums.Length; i++)
            {
                if (Math.Abs(curSums[i] - lastSums[i]) >= sender.AlgorithmParameter.MinMatchPixel)
                {
                    stringBuilder.Append(sender.StandingSettings[i].Name);
                    stringBuilder.Append("++");
                    stringBuilder.Append("  ");
                }
            }
            _lastPointRGBDic.Remove(sender.Name);
            _lastPointRGBDic.Add(sender.Name, curSums);
            if(stringBuilder.Length != 0)
            {
                SendNotify(sender, stringBuilder.ToString(), string.Empty);
            }
        }
        private Timer _pointRGBTimer;
        /// <summary>
        /// PointRGB模式，启用延迟
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="img"></param>
        private void Analyse1_2(LocalIntelProcSetting sender, System.Drawing.Bitmap img)
        {
            long[] lastSums;
            if (_lastPointRGBDic.TryGetValue(sender.Name, out var value))
            {
                lastSums = value;
            }
            else
            {
                lastSums = new long[sender.StandingSettings.Count];
            }
            var curSums = Analyse1_FindCurSums(sender, img);
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < lastSums.Length; i++)
            {
                if (Math.Abs(curSums[i] - lastSums[i]) >= sender.AlgorithmParameter.MinMatchPixel)
                {
                    stringBuilder.Append(sender.StandingSettings[i].Name);
                    stringBuilder.Append("++");
                    stringBuilder.Append("  ");
                }
            }
            _lastPointRGBDic.Remove(sender.Name);
            _lastPointRGBDic.Add(sender.Name, curSums);
            if (stringBuilder.Length != 0)
            {
                SendNotify(sender, stringBuilder.ToString(), string.Empty);
            }
        }

        /// <summary>
        /// RectRGB模式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="img"></param>
        /// <exception cref="Exception"></exception>
        private void Analyse2(LocalIntelProcSetting sender, System.Drawing.Bitmap img)
        {
            var sourceMat = IntelImageHelper.BitmapToMat(img);
            var sourceMat2 = IntelImageHelper.ChangeRedTo(sourceMat, 100, 100, 100);
            var grayMat = IntelImageHelper.GetGray(sourceMat2);
            var edgeMat = IntelImageHelper.GetEdge(grayMat, sender.AlgorithmParameter.BlurSizeW,
                sender.AlgorithmParameter.BlurSizeH, sender.AlgorithmParameter.CannyThreshold1, sender.AlgorithmParameter.CannyThreshold2);

            var rects = IntelImageHelper.CalStandingRects(edgeMat, sender.AlgorithmParameter.FillThresholdV,
                sender.AlgorithmParameter.FillThresholdH, sender.AlgorithmParameter.SpanLineV,
                sender.AlgorithmParameter.MinHeight, sender.AlgorithmParameter.MinWidth);

            sender.ChangeGrayImg(grayMat);
            sender.ChangeEdgeImg(edgeMat);
            sender.ChangeStandingRects(sourceMat, rects);
            if (rects != null && rects.Any())
            {
                if (_lastStandingsDic.TryGetValue(sender.Name, out var lastStandings))
                {
                    List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>> matchList = new List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>>();
                    OpenCvSharp.Vec3b[] vec3bs = new OpenCvSharp.Vec3b[rects.Count];
                    for (int i = 0; i < rects.Count; i++)
                    {
                        vec3bs[i] = IntelImageHelper.GetMainColor(rects[i], sourceMat, sender.AlgorithmParameter.MainColorSpan);
                    }
                    for (int i = 0; i < vec3bs.Length; i++)
                    {
                        foreach (var refColors in sender.StandingSettings)
                        {
                            if (IsMatch(refColors.Color, vec3bs[i], sender.AlgorithmParameter.ColorMatchThreshold))
                            {
                                matchList.Add(new Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>(rects[i], refColors));
                                break;
                            }
                        }
                    }
                    sender.ChangeMatchRects(sourceMat, matchList);
                    if (lastStandings.Count == matchList.Count)//总匹配的声望和上一回一样，需要排除位置不同
                    {
                        for (int i = 0; i < lastStandings.Count; i++)
                        {
                            var centerY1 = lastStandings[i].Item1.Top + lastStandings[i].Item1.Height / 2;
                            var centerY2 = matchList[i].Item1.Top + matchList[i].Item1.Height / 2;
                            if (Math.Abs(centerY1 - centerY2) < lastStandings[i].Item1.Height * 0.2)//位置误差在高度的20%算相同位置
                            {
                                if (lastStandings[i].Item2.Equals(matchList[i].Item2))//位置相同颜色也相同
                                {
                                    continue;
                                }
                                else//只要出现位置相同颜色不同就是有变化，需要预警
                                {
                                    //找出变化
                                    List<StandingChange> standingChanges = new List<StandingChange>();
                                    var lastGroup = lastStandings.Skip(i).GroupBy(p => p.Item2);
                                    foreach (var group in lastGroup)
                                    {
                                        var sameMath = matchList.Where(p => p.Item2.Color == group.Key.Color).ToList();
                                        standingChanges.Add(new StandingChange()
                                        {
                                            Setting = group.Key,
                                            Change = sameMath.Count - group.Count(),
                                            Remain = sameMath.Count
                                        });
                                    }
                                    SendNotify(sender, standingChanges);
                                    break;
                                }
                            }
                            else
                            {
                                //位置发生了变化，先检查声望有没有变化，有则提示声望变化，没则提示注意预警
                                //找出变化
                                List<StandingChange> standingChanges = new List<StandingChange>();
                                var lastGroup = lastStandings.GroupBy(p => p.Item2);
                                foreach (var group in lastGroup)
                                {
                                    var sameMath = matchList.Where(p => p.Item2.Color == group.Key.Color).ToList();
                                    standingChanges.Add(new StandingChange()
                                    {
                                        Setting = group.Key,
                                        Change = sameMath.Count - group.Count(),
                                        Remain = sameMath.Count
                                    });
                                }
                                if (standingChanges.FirstOrDefault(p => p.Change != 0) != null)
                                {
                                    //有声望变化，提示声望变化
                                    SendNotify(sender, standingChanges);
                                }
                                else
                                {
                                    //无声望变化，提示注意预警
                                    SendNotify(sender, "声望区域定位波动，请注意", string.Empty);
                                }
                                break;
                            }
                        }
                    }
                    else//总匹配的声望和上回不一样，提示变化
                    {
                        if (!(lastStandings.Count > matchList.Count && !sender.NotifyDecrease))
                        {
                            List<StandingChange> standingChanges = new List<StandingChange>();
                            if (lastStandings.Count == 0)
                            {
                                var groups = matchList.GroupBy(p => p.Item2).ToList();
                                foreach (var group in groups)
                                {
                                    standingChanges.Add(new StandingChange()
                                    {
                                        Setting = group.Key,
                                        Change = group.Count(),
                                        Remain = group.Count()
                                    });
                                }
                            }
                            else
                            {
                                var lastGroup = lastStandings.GroupBy(p => p.Item2);
                                foreach (var group in lastGroup)
                                {
                                    var sameMath = matchList.Where(p => p.Item2.Color == group.Key.Color).ToList();
                                    standingChanges.Add(new StandingChange()
                                    {
                                        Setting = group.Key,
                                        Change = sameMath.Count - group.Count(),
                                        Remain = sameMath.Count
                                    });
                                }
                            }
                            SendNotify(sender, standingChanges);
                        }
                    }
                    lastStandings.Clear();
                    lastStandings.AddRange(matchList);
                }
                else
                {
                    throw new Exception($"LocalIntelService：未找到{sender.Name}的上一次声望记录");
                }
            }
            sourceMat.Dispose();
            sourceMat2.Dispose();
            grayMat.Dispose();
            edgeMat.Dispose();
        }

        private bool IsMatch(System.Drawing.Color refColor, OpenCvSharp.Vec3b targetColor, double threshold = 0.2f)
        {
            ///Vec3b为BGR
            if ((double)Math.Abs(targetColor.Item0 - refColor.B) / 255 < threshold)
            {
                if ((double)Math.Abs(targetColor.Item1 - refColor.G) / 255 < threshold)
                {
                    if ((double)Math.Abs(targetColor.Item2 - refColor.R) / 255 < threshold)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        #region 通知
        private void SendNotify(LocalIntelProcSetting setting, List<StandingChange> standingChanges)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var change in standingChanges)
            {
                if (change.Change != 0)
                {
                    stringBuilder.Append(change.Setting.Name);
                    if (change.Change > 0)
                    {
                        stringBuilder.Append('+');
                    }
                    stringBuilder.Append(change.Change);
                    stringBuilder.Append("  ");
                }
            }
            var changedMsg = stringBuilder.ToString();
            stringBuilder.Clear();
            foreach (var change in standingChanges)
            {
                stringBuilder.Append(change.Setting.Name);
                stringBuilder.Append(" : ");
                stringBuilder.Append(change.Remain);
                stringBuilder.Append("  ");
            }
            stringBuilder.Remove(stringBuilder.Length - 1, 1);
            var remainMsg = stringBuilder.ToString();
            SendNotify(setting, changedMsg, remainMsg);
        }
        private void SendNotify(LocalIntelProcSetting setting, string changedMsg, string remainMsg)
        {
            string name = setting.Name;
            if (name.Contains('-'))
            {
                var index = name.IndexOf('-');
                name = name.Substring(index + 1, name.Length - index - 1);
            }
            if (setting.WindowNotify)
                SendWindowNotify(setting.HWnd, name, changedMsg, remainMsg);
            if (setting.ToastNotify)
                SendToastNotify(setting.HWnd, name, changedMsg, remainMsg);
            if (setting.SoundNotify)
                SendSoundNotify(setting.HWnd, setting.Loop,setting.Volume, setting.SoundFile);
        }
        private LocalIntelNotifyWindow _window;
        private void SendWindowNotify(IntPtr hwnd, string name, string changedMsg, string remainMsg)
        {
            if(_window == null)
            {
                Helpers.WindowHelper.MainWindow.DispatcherQueue.TryEnqueue(() =>
                {
                    _window = new LocalIntelNotifyWindow();
                    _window.Add(new LocalIntelNotify(hwnd, name, changedMsg, remainMsg));
                });
            }
            else
            {
                _window.Add(new LocalIntelNotify(hwnd, name, changedMsg, remainMsg));
            }
        }

        private async void SendToastNotify(IntPtr hwnd, string title, string changedMsg, string remainMsg)
        {
            await LocalIntelToast.SendToast(hwnd, title, changedMsg, remainMsg);
        }

        private Dictionary<string, MediaSource> MediaSourceDic = new Dictionary<string, MediaSource>();
        private Dictionary<IntPtr, MediaPlayer> MediaPlayers = new Dictionary<IntPtr, MediaPlayer>();
        private MediaSource defaultMediaSource;
        private MediaSource DefaultMediaSource
        {
            get
            {
                if (defaultMediaSource == null)
                {
                    defaultMediaSource = MediaSource.CreateFromUri(new Uri(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "default.mp3")));
                }
                return defaultMediaSource;
            }
        }
        private void SendSoundNotify(IntPtr hwnd, bool loop, int volume, string filepath)
        {
            MediaPlayer mediaPlayer;
            if (!MediaPlayers.TryGetValue(hwnd, out mediaPlayer))
            {
                filepath = string.IsNullOrEmpty(filepath) ?
                System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "default.mp3") :
                filepath;
                MediaSource mediaSource;
                if (!MediaSourceDic.TryGetValue(filepath, out mediaSource))
                {
                    mediaSource = MediaSource.CreateFromUri(new Uri(filepath));
                    if (mediaSource != null)
                    {
                        MediaSourceDic.Add(filepath, mediaSource);
                    }
                    else
                    {
                        Log.Error($"Create mediaSource from {filepath} return null");
                    }
                }
                mediaPlayer = new MediaPlayer()
                {
                    IsLoopingEnabled = loop,
                    Source = mediaSource,
                    Volume = volume / 100.0
                };
                MediaPlayers.Add(hwnd, mediaPlayer);
            }
            
            mediaPlayer.Pause();
            mediaPlayer.Position = TimeSpan.Zero;
            mediaPlayer.Play();
        }

        public void StopSoundNotify(IntPtr hwnd)
        {
            if (MediaPlayers.TryGetValue(hwnd, out var mediaPlayer))
            {
                mediaPlayer.Pause();
                mediaPlayer.Position = TimeSpan.Zero;
            }
        }
        public void StopSoundNotify()
        {
            foreach(var mediaPlayer in MediaPlayers.Values)
            {
                mediaPlayer.Pause();
                mediaPlayer.Position = TimeSpan.Zero;
            }
        }
        private void RemoveMediaPlayer(IntPtr hwnd)
        {
            if (MediaPlayers.TryGetValue(hwnd, out var mediaPlayer))
            {
                mediaPlayer.Pause();
                mediaPlayer.Position = TimeSpan.Zero;
                mediaPlayer.Source = null;
                mediaPlayer.Dispose();
                MediaPlayers.Remove(hwnd);
            }
        }
        #endregion
    }
}
