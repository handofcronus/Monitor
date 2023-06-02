using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Versioning;

namespace Monitor;

[SupportedOSPlatform("windows")]
public static class InbuiltMonitorController
{
    public static int Get()
    {
        using var mclass = new ManagementClass("WmiMonitorBrightness")
        {
            Scope = new ManagementScope(@"\\.\root\wmi")
        };
        using var instances = mclass.GetInstances();
        foreach (var instance in instances)
        {
            return (byte)instance.GetPropertyValue("CurrentBrightness");
        }
        return 0;
    }

    public static void Set(int brightness)
    {
        using var mclass = new ManagementClass("WmiMonitorBrightnessMethods")
        {
            Scope = new ManagementScope(@"\\.\root\wmi")
        };
        using var instances = mclass.GetInstances();
        var args = new object[] { 1, brightness };
        foreach (ManagementObject instance in instances)
        {
            instance.InvokeMethod("WmiSetBrightness", args);
        }
    }
}
