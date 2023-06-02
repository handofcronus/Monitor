using Monitor;

var mon = new PhysicalMonitorBrightnessController();


if(args.Length==2)
{
    var brightness = Convert.ToUInt32(int.Parse(args[0]));
    var contrast = Convert.ToUInt32(int.Parse(args[1]));
    mon.SetBrightness(brightness);
    mon.SetContrast(contrast);
}
else
{
    Console.WriteLine("morning? (y/n)");
    var res = Console.ReadKey();
    if(res.KeyChar=='y')
    {
        mon.SetBrightness(50);
        mon.SetContrast(50);
        InbuiltMonitorController.Set(100);
        return;
    }
    if(res.KeyChar=='n')
    {
        mon.SetBrightness(0);
        mon.SetContrast(0);
        InbuiltMonitorController.Set(0);
        return;
    }
    else
    {
        Console.WriteLine("wrong input.");
    }

}


