using DataStruct;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Threading;
using System.IO;
using HalconDotNet;
using OpenCvSharp;
using Point = DataStruct.Point;
using UltrapreciseBonding.DieBonding;

public class Test
{
    public static void TestUBHMark()
    {

        string path = @"D:\数据\UBH\左\圆\";
        double pixRate = 6.15; // um
        for (int i = 1; i < 101; i++)
        {
            Camera img = new Camera(path+"c ("+i.ToString()+").bmp");
            List<Point> cPoints = new List<Point>();
            var ret = DieBondComAlgo.CalcCircleRingMarkCenter(img, out cPoints, out List<double> radius, out _, null);
            var dist = cPoints[0].DistanceTo(cPoints[1]);
            Console.WriteLine("dist: " + dist.ToString());
        }
        return;
    }
    public static void Main()
    {
        TestUBHMark();
        return;

        Camera img = new Camera(@"..\..\UTData\UBD\CuttingPathImg\no_ic1.bmp");
        Point preSetCenter = new Point(1550.07, 1562.32);

        Stopwatch stopwatch = Stopwatch.StartNew();

        var ret = DieBondComAlgo.CalcCutPathCorner(img, out Point centerPoint, null, "leftTop");
        stopwatch.Stop();
        Console.WriteLine($"运行时间: {stopwatch.ElapsedMilliseconds} 毫秒");

        return;
    }
}



