using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverlayTimers;
public class TimerSettings
{
    public string Name { get; set; } = "Timer";
    public string HotkeyStr { get; set; } = "R";
    public uint VirtualKey { get; set; }
    public double DurationSeconds { get; set; } = 10.0;
    public double X { get; set; } = 100;
    public double Y { get; set; } = 100;
}


