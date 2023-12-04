using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using NotToday.Helpers;
using NotToday.Models;
using NotToday.Services;
using NotToday.Wins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NotToday.ViewModels
{
    public class LocalIntelViewModel : BaseViewModel
    {
        private ObservableCollection<ProcessInfo> processes = new ObservableCollection<ProcessInfo>();
        public ObservableCollection<ProcessInfo> Processes
        {
            get => processes;
            set => SetProperty(ref processes, value);
        }

        private ProcessInfo selectedProcess;
        public ProcessInfo SelectedProcess
        {
            get => selectedProcess;
            set
            {
                if (SetProperty(ref selectedProcess, value))
                {
                    ProcSetting = GetProcess(value);
                }
            }
        }

        public LocalIntelSetting Setting { get; set; }

        private LocalIntelProcSetting procSetting;
        public LocalIntelProcSetting ProcSetting
        {
            get => procSetting;
            set
            {
                ClearImage();
                if (procSetting != null)
                {
                    procSetting.OnScreenshotChanged -= ProcSetting_OnScreenshotChanged;
                    procSetting.OnEdgeImgChanged -= ProcSetting_OnEdgeImgChanged;
                    procSetting.OnStandingRectsChanged -= ProcSetting_OnStandingRectsChanged;
                    value.OnGrayImgChanged -= Value_OnGrayImgChanged;
                    value.OnMatchRectsChanged -= Value_OnMatchRectsChanged;
                }
                if (SetProperty(ref procSetting, value))
                {
                    if (value != null)
                    {
                        value.OnScreenshotChanged += ProcSetting_OnScreenshotChanged;
                        value.OnEdgeImgChanged += ProcSetting_OnEdgeImgChanged;
                        value.OnStandingRectsChanged += ProcSetting_OnStandingRectsChanged;
                        value.OnGrayImgChanged += Value_OnGrayImgChanged;
                        value.OnMatchRectsChanged += Value_OnMatchRectsChanged;
                        ModeIndex = (int)ProcSetting.LocalIntelMode;
                    }
                }
            }
        }

        private bool isPointRGBMode;
        public bool IsPointRGBMode
        {
            get => isPointRGBMode;
            set => SetProperty(ref isPointRGBMode, value);
        }

        private int modeIndex;
        public int ModeIndex
        {
            get => modeIndex;
            set
            {
                if(SetProperty(ref modeIndex, value))
                {
                    ProcSetting.LocalIntelMode = (LocalIntelMode)value;
                }
                IsPointRGBMode = value == 0;
            }
        }

        private bool running;
        public bool Running
        {
            get => running;
            set => SetProperty(ref running, value);
        }

        private Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap imageSource_Source;
        /// <summary>
        /// 声望区域原图
        /// </summary>
        public Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap ImageSource_Source
        {
            get => imageSource_Source;
            set => SetProperty(ref imageSource_Source, value);
        }

        private Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap imageSource_Edge;
        /// <summary>
        /// 声望区域轮廓图
        /// </summary>
        public Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap ImageSource_Edge
        {
            get => imageSource_Edge;
            set => SetProperty(ref imageSource_Edge, value);
        }

        private Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap imageSource_DrawRect;
        /// <summary>
        /// 声望区域矩形识别图
        /// </summary>
        public Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap ImageSource_DrawRect
        {
            get => imageSource_DrawRect;
            set => SetProperty(ref imageSource_DrawRect, value);
        }

        private Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap imageSource_DrawMainColorPos;
        /// <summary>
        /// 声望区域取色位置图
        /// </summary>
        public Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap ImageSource_DrawMainColorPos
        {
            get => imageSource_DrawMainColorPos;
            set => SetProperty(ref imageSource_DrawMainColorPos, value);
        }

        private Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap imageSource_Gray;
        /// <summary>
        /// 灰度图
        /// </summary>
        public Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap ImageSource_Gray
        {
            get => imageSource_Gray;
            set => SetProperty(ref imageSource_Gray, value);
        }

        private Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap imageSource_MatchRect;
        /// <summary>
        /// 声望匹配区域
        /// </summary>
        public Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap ImageSource_MatchRect
        {
            get => imageSource_MatchRect;
            set => SetProperty(ref imageSource_MatchRect, value);
        }

        private int selectedThemeIndex = (int)ThemeSelectorService.Theme;
        public int SelectedThemeIndex
        {
            get => selectedThemeIndex;
            set
            {
                if(SetProperty(ref selectedThemeIndex, value))
                {
                    _ = ThemeSelectorService.SetThemeAsync((ElementTheme)value);
                }
            }
        }


        private readonly Dictionary<string, LocalIntelProcSetting> _runningDic = new Dictionary<string, LocalIntelProcSetting>();
        private static readonly string SettingFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "LocalIntelSetting.json");
        public LocalIntelViewModel()
        {
            Init();
        }
        private void Init()
        {
            if (System.IO.File.Exists(SettingFilePath))
            {
                string json = System.IO.File.ReadAllText(SettingFilePath);
                if (string.IsNullOrEmpty(json))
                {
                    Setting = new LocalIntelSetting();
                }
                else
                {
                    Setting = JsonConvert.DeserializeObject<LocalIntelSetting>(json);
                }
            }
            else
            {
                Setting = new LocalIntelSetting();
            }
            GetProcesses();
        }
        private async void GetProcesses()
        {
            var allProcesses = Process.GetProcesses();
            if (allProcesses != null && allProcesses.Any())
            {
                List<ProcessInfo> targetProcesses = new List<ProcessInfo>();
                //获取所有目标进程
                await Task.Run(() =>
                {
                    foreach (var process in allProcesses.Where(p => p.ProcessName == Setting.ProcessName).ToList())
                    {
                        if (process.MainWindowHandle != IntPtr.Zero)
                        {
                            ProcessInfo processInfo = new ProcessInfo()
                            {
                                MainWindowHandle = process.MainWindowHandle,
                                ProcessName = process.ProcessName,
                                WindowTitle = process.MainWindowTitle,
                                Process = process
                            };
                            targetProcesses.Add(processInfo);
                        }
                    }
                });
                if (targetProcesses != null && targetProcesses.Any())
                {
                    List<ProcessInfo> targetProcessesForShow;//最终要显示的目标进程
                    if (Processes != null && Processes.Any())//当前列表不为空，需要保留运行中的进程
                    {
                        targetProcessesForShow = new List<ProcessInfo>();
                        var runnings = Processes.Where(p => p.Running).ToList();
                        if (runnings != null && runnings.Any())//存在运行中，
                        {
                            //将运行中从新增里排除
                            foreach (var running in runnings)
                            {
                                var item = targetProcesses.FirstOrDefault(p => p.MainWindowHandle == running.MainWindowHandle);
                                if (item != null)
                                {
                                    targetProcesses.Remove(item);
                                }
                                targetProcessesForShow.Add(running);
                            }
                        }
                        if (targetProcesses.Any())//添加未运行中的
                        {
                            foreach (var item in targetProcesses)
                            {
                                targetProcessesForShow.Add(item);
                            }
                        }
                    }
                    else//当前列表为空，保存显示全部
                    {
                        targetProcessesForShow = targetProcesses;
                    }
                    Processes.Clear();
                    foreach (var item in targetProcessesForShow)
                    {
                        Processes.Add(item);
                    }
                }
            }
            else
            {
                Processes.Clear();
            }
        }
        private LocalIntelProcSetting GetProcess(ProcessInfo processInfo)
        {
            if (processInfo == null)
            {
                return null;
            }
            var target = Setting.ProcSettings.FirstOrDefault(p => p.Name == processInfo.WindowTitle);
            if (target == null)
            {
                target = new LocalIntelProcSetting()
                {
                    Name = processInfo.WindowTitle,
                    StandingSettings = new ObservableCollection<LocalIntelStandingSetting>()
                    {
                        new LocalIntelStandingSetting()
                        {
                            Name = "红",
                            Color = Color.FromArgb(111, 4, 4),
                        },
                        new LocalIntelStandingSetting()
                        {
                            Name = "白",
                            Color = Color.FromArgb(110,110,110),
                        },
                    }
                };
                Setting.ProcSettings.Add(target);
            }
            target.HWnd = processInfo.MainWindowHandle;
            return target;
        }


        public ICommand StartCommand => new RelayCommand(() =>
        {
            if (Start(ProcSetting))
            {
                Save();
                Window?.ShowSuccess("已开始监控");
            }
            else
            {
                Window?.ShowError("请检查选择区域是否规范、是否已启动监控同名进程");
            }
        });

        public ICommand StopCommand => new RelayCommand(() =>
        {
            Stop(ProcSetting);
        });

        public ICommand StartAllCommand => new RelayCommand(() =>
        {
            foreach (var p in Processes.Where(p => !p.Running).ToList())
            {
                Start(GetProcess(p));
            }
            Save();
        });

        public ICommand StopAllCommand => new RelayCommand(() =>
        {
            foreach (var p in _runningDic.Select(p => p.Value).ToList())
            {
                Stop(p);
            }
        });
        public ICommand RefreshProcessListCommand => new RelayCommand(() =>
        {
            GetProcesses();
        });
        public ICommand AddStandingCommand => new RelayCommand(() =>
        {
            ProcSetting.StandingSettings.Add(new LocalIntelStandingSetting()
            {
                Name = "红",
                Color = Color.FromArgb(111, 4, 4)
            });
        });
        public ICommand PickSoundFileCommand => new RelayCommand(async () =>
        {
            var file = await Helpers.PickHelper.PickFileAsync(Window);
            if (file != null)
            {
                ProcSetting.SoundFile = file.Path;
            }
        });
        private SelecteCaptureAreaWindow _currentSelecteCaptureAreaWindow;
        public ICommand SelectRegionCommand => new RelayCommand(() =>
        {
            if (_currentSelecteCaptureAreaWindow == null)
            {
                _currentSelecteCaptureAreaWindow = new SelecteCaptureAreaWindow(SelectedProcess.MainWindowHandle, new Windows.Foundation.Rect(ProcSetting.X, ProcSetting.Y, ProcSetting.Width, ProcSetting.Height));
                _currentSelecteCaptureAreaWindow.Activate();
                _currentSelecteCaptureAreaWindow.CroppedRegionChanged += SelecteCaptureAreaWindow_CroppedRegionChanged;
                _currentSelecteCaptureAreaWindow.Closed += ((s, e) =>
                {
                    _currentSelecteCaptureAreaWindow = null;
                });
            }
        });
        public ICommand CheckLogCommand => new RelayCommand(() =>
        {
            System.Diagnostics.Process.Start("explorer.exe", Log.GetLogPath());
        });
        public ICommand CheckConfigCommand => new RelayCommand(() =>
        {
            System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs"));
        });
        public ICommand CheckGithubCommand => new RelayCommand(() =>
        {
            System.Diagnostics.Process.Start("explorer.exe", @"https://github.com/qedsd/NotToday2");
        });
        public void RemoveStanding(LocalIntelStandingSetting standingSetting)
        {
            if (standingSetting != null)
            {
                ProcSetting.StandingSettings.Remove(standingSetting);
            }
        }

        private void SelecteCaptureAreaWindow_CroppedRegionChanged(Windows.Foundation.Rect rect)
        {
            Debug.WriteLine($"裁剪区域 {(int)rect.X} {(int)rect.Y} {(int)rect.Width} {(int)rect.Height}");
            ProcSetting.X = (int)rect.X;
            ProcSetting.Y = (int)rect.Y;
            ProcSetting.Width = (int)rect.Width;
            ProcSetting.Height = (int)rect.Height;
        }
        private bool IsValidRegion(LocalIntelProcSetting setting)
        {
            bool result = (setting.X + setting.Width) > 0 && (setting.Y + setting.Height) > 0;
            var targetProcess = Processes.FirstOrDefault(p => p.WindowTitle == setting.Name);
            Rectangle clientRect = new Rectangle();
            Win32.GetClientRect(targetProcess.MainWindowHandle, ref clientRect);
            Point point = new Point();
            Win32.ClientToScreen(targetProcess.MainWindowHandle, ref point);
            result &= setting.Width <= clientRect.Width;
            result &= setting.Height <= clientRect.Height;
            return result;
        }

        private LocalIntelWindow _localIntelWindow;
        private bool Start(LocalIntelProcSetting setting)
        {
            if (IsValidRegion(setting))
            {
                if (!_runningDic.ContainsKey(setting.Name))
                {
                    var target = Processes.FirstOrDefault(p => p.WindowTitle == setting.Name);
                    if (target != null)
                    {
                        if (_localIntelWindow == null)
                        {
                            _localIntelWindow = new LocalIntelWindow(Setting.RefreshSpan);
                            _localIntelWindow.Closed += _localIntelWindow_Closed;
                            _localIntelWindow.Activate();
                        }
                        if (_localIntelWindow.Add(setting))
                        {
                            _runningDic.Add(setting.Name, setting);
                            LocalIntelService.Current.Add(setting);
                            target.Running = true;
                            Running = true;
                            return true;
                        }
                        else
                        {
                            Log.Error($"添加{setting.Name}到监控窗口失败");
                            Window?.ShowError($"添加{setting.Name}到监控窗口失败");
                            return false;
                        }
                    }
                    else
                    {
                        Log.Error($"未找到目标进程:{setting.Name}");
                        return false;
                    }
                }
                else
                {
                    Log.Error($"已启动同名进程监控:{setting.Name}");
                    return false;
                }
            }
            else
            {
                Log.Error($"不规范区域:{setting.Name}");
                return false;
            }
        }

        private void _localIntelWindow_Closed(object sender, Microsoft.UI.Xaml.WindowEventArgs args)
        {
            _localIntelWindow = null;
        }

        private void Stop(LocalIntelProcSetting setting)
        {
            if (_localIntelWindow.Remve(setting))
            {
                _runningDic.Remove(setting.Name);
                LocalIntelService.Current.Remove(setting);
                var target = Processes.FirstOrDefault(p => p.WindowTitle == setting.Name);
                if (target != null)
                {
                    target.Running = false;
                }
            }
            else
            {
                Window.ShowError("停止失败");
            }
            Running = _runningDic.Count > 0;
        }

        private void Save()
        {
            string dir = System.IO.Path.GetDirectoryName(SettingFilePath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }
            string json = JsonConvert.SerializeObject(Setting);
            System.IO.File.WriteAllText(SettingFilePath, json);
        }

        private void ProcSetting_OnScreenshotChanged(LocalIntelProcSetting sender, Bitmap img)
        {
            var m = img.Clone() as Bitmap;
            Window?.DispatcherQueue.TryEnqueue(() =>
            {
                if (ImageSource_Source == null)
                {
                    ImageSource_Source = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(m.Width, m.Height);
                }
                else if (ImageSource_Source.PixelWidth != m.Width || ImageSource_Source.PixelHeight != m.Height)
                {
                    ImageSource_Source = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(m.Width, m.Height);
                }
                ImageHelper.BitmapWriteToWriteableBitmap(ImageSource_Source, m);
                m.Dispose();
            });
        }
        private void ProcSetting_OnStandingRectsChanged(LocalIntelProcSetting sender, OpenCvSharp.Mat img, List<OpenCvSharp.Rect> rects)
        {
            var m = img.Clone();
            var afterDrawRectMat = IntelImageHelper.DrawRects(m, rects);
            var afterDrawMainColorPos = IntelImageHelper.DrawMainColorPos(m, rects, sender.AlgorithmParameter.MainColorSpan);
            Window?.DispatcherQueue.TryEnqueue(() =>
            {
                if (ImageSource_DrawRect == null)
                {
                    ImageSource_DrawRect = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(afterDrawRectMat.Width, afterDrawRectMat.Height);
                }
                else if (ImageSource_DrawRect.PixelWidth != afterDrawRectMat.Width || ImageSource_DrawRect.PixelHeight != afterDrawRectMat.Height)
                {
                    ImageSource_DrawRect = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(afterDrawRectMat.Width, afterDrawRectMat.Height);
                }
                ImageSource_DrawRect.SetSource(afterDrawRectMat.ToMemoryStream().AsRandomAccessStream());

                if (ImageSource_DrawMainColorPos == null)
                {
                    ImageSource_DrawMainColorPos = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(afterDrawMainColorPos.Width, afterDrawMainColorPos.Height);
                }
                else if (ImageSource_DrawMainColorPos.PixelWidth != afterDrawMainColorPos.Width || ImageSource_DrawMainColorPos.PixelHeight != afterDrawMainColorPos.Height)
                {
                    ImageSource_DrawMainColorPos = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(afterDrawMainColorPos.Width, afterDrawMainColorPos.Height);
                }
                ImageSource_DrawMainColorPos.SetSource(afterDrawMainColorPos.ToMemoryStream().AsRandomAccessStream());
            });
        }
        private void Value_OnGrayImgChanged(LocalIntelProcSetting sender, OpenCvSharp.Mat input)
        {
            var img = input.Clone();
            Window?.DispatcherQueue.TryEnqueue(() =>
            {
                if (ImageSource_Gray == null)
                {
                    ImageSource_Gray = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(img.Width, img.Height);
                }
                else if (ImageSource_Gray.PixelWidth != img.Width || ImageSource_Gray.PixelHeight != img.Height)
                {
                    ImageSource_Gray = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(img.Width, img.Height);
                }
                ImageSource_Gray.SetSource(img.ToMemoryStream().AsRandomAccessStream());
            });
        }
        private void ProcSetting_OnEdgeImgChanged(LocalIntelProcSetting sender, OpenCvSharp.Mat input)
        {
            var img = input.Clone();
            Window?.DispatcherQueue.TryEnqueue(() =>
            {
                if (ImageSource_Edge == null)
                {
                    ImageSource_Edge = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(img.Width, img.Height);
                }
                else if (ImageSource_Edge.PixelWidth != img.Width || ImageSource_Edge.PixelHeight != img.Height)
                {
                    ImageSource_Edge = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(img.Width, img.Height);
                }
                ImageSource_Edge.SetSource(img.ToMemoryStream().AsRandomAccessStream());
            });
        }
        private void Value_OnMatchRectsChanged(LocalIntelProcSetting sender, OpenCvSharp.Mat input, List<Tuple<OpenCvSharp.Rect, LocalIntelStandingSetting>> matchList)
        {
            var img2 = input.Clone();
            var img = IntelImageHelper.DrawRects(img2, matchList);
            img2.Dispose();
            Window?.DispatcherQueue.TryEnqueue(() =>
            {
                if (ImageSource_MatchRect == null)
                {
                    ImageSource_MatchRect = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(img.Width, img.Height);
                }
                else if (ImageSource_MatchRect.PixelWidth != img.Width || ImageSource_MatchRect.PixelHeight != img.Height)
                {
                    ImageSource_MatchRect = new Microsoft.UI.Xaml.Media.Imaging.WriteableBitmap(img.Width, img.Height);
                }
                ImageSource_MatchRect.SetSource(img.ToMemoryStream().AsRandomAccessStream());
                img.Dispose();
            });
        }
        private void ClearImage()
        {
        }

        public void Dispose()
        {
            _localIntelWindow?.Close();
            LocalIntelService.Current.Dispose();
        }
    }
}
