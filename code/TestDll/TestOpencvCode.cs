using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;

namespace TestDll
{
    public static class TestOpencvCode
    {
        public static void TestSaveColorMap()
        {
            var colorMapType = ColormapTypes.Cividis;
            HOperatorSet.TupleGenSequence(0,255,1,out HTuple values);

            //byte[] baseValue = new byte[256];
            var baseValue = values.ToIArr();
            //Mat baseGray = new Mat(1, 256, MatType.CV_8UC1);
            Mat baseGray = new Mat(1, 256, MatType.CV_32SC1,baseValue,0);
            baseGray.ConvertTo(baseGray,MatType.CV_8UC1);
            //for (int i = 0; i < 256; i++)
            //{
            //    baseGray.Set<byte>(0, i, (byte)i);
            //}

            foreach (ColormapTypes id in Enum.GetValues(typeof(ColormapTypes)))
            {
                colorMapType = id;
                Mat colorValue = new Mat();
                Cv2.ApplyColorMap(baseGray, colorValue, colorMapType);
                Cv2.NamedWindow("test", WindowFlags.FreeRatio);
                Cv2.ImShow("test", colorValue);
                List<Vec3b> valueRGB = new List<Vec3b>();
                for (int colId = 0; colId < 256; colId++)
                {
                    var values3b = colorValue.Get<Vec3b>(0, colId);
                    valueRGB.Add(values3b);
                    int valueR = values3b.Item0;
                    int valueG = values3b.Item1;
                    int valueB = values3b.Item2;
                    Console.WriteLine(colId + " RGB:" + valueR + " " + valueG + " " + valueB);
                }
                Cv2.WaitKey(0);
                string fileDir = "./colormap/COLOR_";
                SaveRGB(fileDir + colorMapType.ToString() + ".xml", valueRGB);
            }
        }

        internal static void ReadPointTxt(string fullFileName, out List<Point> pointsOut)
        {
            pointsOut = new List<Point>();
            if (!File.Exists(fullFileName)) return;
            StreamReader sr = new StreamReader(fullFileName, Encoding.Default);
            string line = string.Empty;
            line = sr.ReadLine();
            string[] _fristLine = line.Split(' ');
            string[] s = line.Split(' ');
            pointsOut.Add(new Point(double.Parse(s[0]), double.Parse(s[1])));
            while ((line = sr.ReadLine()) != null)
            {
                s = line.Split(' ');
                pointsOut.Add(new Point(double.Parse(s[0]), double.Parse(s[1])));
            }
            sr.Close();
        }

        internal static void SaveRGB(string fullFileName, List<Vec3b> valueRGB)
        {
            if (File.Exists(fullFileName)) File.Delete(fullFileName);
            StreamWriter sw = new StreamWriter(fullFileName, true);
            foreach (var rgb in valueRGB)
            {
                string line = rgb.Item0.ToString()+ ' ' + rgb.Item1.ToString()+ ' ' + rgb.Item2.ToString();
                sw.WriteLine(line);
            }
            sw.Close();
        }
    }
}
