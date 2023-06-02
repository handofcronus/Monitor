using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

public class PhysicalMonitorBrightnessController : IDisposable
{
    #region DllImport
    [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

    [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", EntryPoint = "GetMonitorBrightness")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorBrightness(IntPtr handle, ref uint minimumBrightness, ref uint currentBrightness, ref uint maxBrightness);
    [DllImport("dxva2.dll", EntryPoint = "GetMonitorContrast")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorContrast(IntPtr handle, ref uint minContrast, ref uint currentContrast, ref uint maxContrast);

    [DllImport("dxva2.dll", EntryPoint = "SetMonitorBrightness")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetMonitorBrightness(IntPtr handle, uint newBrightness);

    [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitor")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);

    [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", EntryPoint = "SetMonitorContrast")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetMonitorContrast(IntPtr hMonitor, uint dwNewContrast);

    [DllImport("user32.dll")]
    static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
    delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);
    #endregion

    private IReadOnlyCollection<MonitorInfo> Monitors { get; set; }

    public PhysicalMonitorBrightnessController()
    {
        UpdateMonitors();
    }

    #region Get & Set
    public void SetBrightness(uint brightness)
    {
        SMB(brightness, true);
    }
    public void SetContrast(uint contrast)
    {
        SMC(contrast, true);
    }

    private void SMB(uint brightness, bool refreshMonitorsIfNeeded)
    {
        bool isSomeFail = false;
        foreach (var monitor in Monitors)
        {
            var realNewValue = brightness;

            if(brightness<monitor.MinBrightnessValue)
            {
                realNewValue = monitor.MinBrightnessValue;
            }
            if(brightness>monitor.MaxBrightnessValue)
            {
                realNewValue = monitor.MaxBrightnessValue;
            }
            Console.WriteLine($"maxB:{monitor.MaxBrightnessValue} minB{monitor.MinBrightnessValue}");
            if (SetMonitorBrightness(monitor.Handle, realNewValue))
            {
                monitor.CurrentBrightnessValue = realNewValue;
            }
            else if (refreshMonitorsIfNeeded)
            {
                isSomeFail = true;
                break;
            }
        }

        if (refreshMonitorsIfNeeded && (isSomeFail || !Monitors.Any()))
        {
            UpdateMonitors();
            SMB(brightness, false);
            return;
        }
    }

    private void SMC(uint contrast, bool refreshMonitorsIfNeeded)
    {
        bool isSomeFail = false;
        foreach (var monitor in Monitors)
        {

            var realNewValue = contrast;

            if (contrast < monitor.MinBrightnessValue)
            {
                realNewValue = monitor.MinBrightnessValue;
            }
            if (contrast > monitor.MaxBrightnessValue)
            {
                realNewValue = monitor.MaxBrightnessValue;
            }
            Console.WriteLine($"maxC:{monitor.MaxContrastValue} minC{monitor.MinContrastValue}");
            if (SetMonitorContrast(monitor.Handle, realNewValue))
            {
                monitor.CurrentBrightnessValue = realNewValue;
            }
            else if (refreshMonitorsIfNeeded)
            {
                isSomeFail = true;
                break;
            }
        }

        if (refreshMonitorsIfNeeded && (isSomeFail || !Monitors.Any()))
        {
            UpdateMonitors();
            SMC(contrast, false);
            return;
        }
    }

    public int Get()
    {
        if (!Monitors.Any())
        {
            return -1;
        }
        return (int)Monitors.Average(d => d.CurrentBrightnessValue);
    }
    #endregion

    private void UpdateMonitors()
    {
        DisposeMonitors(this.Monitors);

        var monitors = new List<MonitorInfo>();
        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
        {
            uint physicalMonitorsCount = 0;
            if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref physicalMonitorsCount))
            {
                // Cannot get monitor count
                return true;
            }

            var physicalMonitors = new PHYSICAL_MONITOR[physicalMonitorsCount];
            if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorsCount, physicalMonitors))
            {
                // Cannot get physical monitor handle
                return true;
            }

            foreach (PHYSICAL_MONITOR physicalMonitor in physicalMonitors)
            {
                uint minBrightnessValue = 0, currentBrightnessValue = 0, maxBrightnessValue = 0;
                uint minContrastValue = 0, currentContrastValue = 0, maxContrastValue = 0;
                if (!GetMonitorBrightness(physicalMonitor.hPhysicalMonitor, ref minBrightnessValue, ref currentBrightnessValue, ref maxBrightnessValue))
                {
                    DestroyPhysicalMonitor(physicalMonitor.hPhysicalMonitor);
                    continue;
                }
                if(!GetMonitorContrast(physicalMonitor.hPhysicalMonitor,ref minContrastValue,ref currentContrastValue,ref maxContrastValue))
                {
                    DestroyPhysicalMonitor(physicalMonitor.hPhysicalMonitor);
                    continue;
                }

                var info = new MonitorInfo
                {
                    Handle = physicalMonitor.hPhysicalMonitor,
                    MinBrightnessValue = minBrightnessValue,
                    CurrentBrightnessValue = currentBrightnessValue,
                    MaxBrightnessValue = maxBrightnessValue,
                    MinContrastValue = minContrastValue,
                    CurrentContrastValue = currentContrastValue,
                    MaxContrastValue = maxContrastValue,
                };
                monitors.Add(info);
            }

            return true;
        }, IntPtr.Zero);

        this.Monitors = monitors;
    }

    public void Dispose()
    {
        DisposeMonitors(Monitors);
        GC.SuppressFinalize(this);
    }

    private static void DisposeMonitors(IEnumerable<MonitorInfo> monitors)
    {
        if (monitors?.Any() == true)
        {
            PHYSICAL_MONITOR[] monitorArray = monitors.Select(m => new PHYSICAL_MONITOR { hPhysicalMonitor = m.Handle }).ToArray();
            DestroyPhysicalMonitors((uint)monitorArray.Length, monitorArray);
        }
    }

    #region Classes
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    public class MonitorInfo
    {
        public uint MinBrightnessValue { get; set; }
        public uint MaxBrightnessValue { get; set; }
        public IntPtr Handle { get; set; }
        public uint CurrentBrightnessValue { get; set; }
        public uint MinContrastValue { get; set; }
        public uint MaxContrastValue { get; set; }
        public uint CurrentContrastValue { get; set; }
    }
    #endregion
}