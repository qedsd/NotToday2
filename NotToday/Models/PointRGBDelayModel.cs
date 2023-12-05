using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NotToday.Models
{
    internal class PointRGBDelayModel
    {
        public PointRGBDelayModel(IntPtr hwnd, LocalIntelProcSetting setting)
        {
            Hwnd = hwnd;
            Setting = setting;
        }
        public LocalIntelProcSetting Setting { get; set; }
        public IntPtr Hwnd { get; set; }
        public long[] LastSums { get; set; }
        public long[] CurSums { get; set; }
        private System.Timers.Timer _timer { get; set; }
        private System.Timers.Timer GetTimer()
        {
            if(_timer == null)
            {
                _timer = new System.Timers.Timer()
                {
                    Interval = Setting.AlgorithmParameter.Delay,
                    AutoReset = false
                };
                _timer.Elapsed += Timer_Elapsed;
            }
            return _timer;
        }

        /// <summary>
        /// 延迟处理通知
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < LastSums.Length; i++)
            {
                if (Math.Abs(CurSums[i] - LastSums[i]) >= Setting.AlgorithmParameter.MinMatchPixel)
                {
                    stringBuilder.Append(Setting.StandingSettings[i].Name);
                    stringBuilder.Append("++");
                    stringBuilder.Append("  ");
                }
            }
            LastSums = CurSums;
            if (stringBuilder.Length != 0)
            {
                Services.LocalIntelService.Current.SendNotify(Setting, stringBuilder.ToString(), string.Empty);
            }
        }

        public void UpdateCurSums(long[] curSums)
        {
            for (int i = 0; i < CurSums.Length; i++)
            {
                //只要有大于阈值的变化，就重新开始计时
                if (Math.Abs(curSums[i] - CurSums[i]) >= Setting.AlgorithmParameter.MinMatchPixel)
                {
                    var timer = GetTimer();
                    timer.Stop();
                    CurSums = curSums;
                    timer.Start();
                    break;
                }
            }
        }

        public void Dispose()
        {
            if(_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }
    }
}
