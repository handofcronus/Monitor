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
    var res = 'b';
    while(res!='n' || res!='y')
    {
        Console.WriteLine("morning? (y/n)");
        res = Console.ReadKey().KeyChar;
        if (res == 'y')
        {
            mon.SetBrightness(50);
            mon.SetContrast(50);
            InbuiltMonitorController.Set(100);
            return;
        }
        if (res == 'n')
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

}


