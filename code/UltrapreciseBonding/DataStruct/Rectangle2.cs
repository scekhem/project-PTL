using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniFileHelper;
using System.IO;
using OpenCvSharp.Flann;

namespace DataStruct
{
    /// <summary>
    /// 中心矩形
    /// </summary>
    public class Rectangle2 : DataStructBase
    {
        private double _xCenter;
        private double _yCenter;
        private double _width;
        private double _height;
        private double _phi;

        /// <summary>
        /// Gets or sets the user's _xCenter
        /// </summary>
        public double XCenter
        {
            get { return _xCenter; }
            set { _xCenter = value; }
        }

        /// <summary>
        /// Gets or sets the user's _yCenter
        /// </summary>
        public double YCenter
        {
            get { return _yCenter; }
            set { _yCenter = value; }
        }

        /// <summary>
        /// Gets or sets the user's _height
        /// </summary>
        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        /// Gets or sets the user's _width
        /// </summary>
        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// Gets or sets the user's 倾斜角(弧度)
        /// </summary>
        public double Phi
        {
            get { return _phi; }
            set { _phi = value; }
        }

        /// <summary>
        /// 默认构造
        /// </summary>
        public Rectangle2()
        {
        }

        /// <summary>
        /// 有参构造-无旋转角度
        /// </summary>
        /// <param name="x">矩形中心x坐标</param>
        /// <param name="y">矩形中心y坐标</param>
        /// <param name="w">矩形宽度</param>
        /// <param name="h">矩形高度</param>
        public Rectangle2(double x, double y, double w, double h)
        {
            XCenter = x;
            YCenter = y;
            Width = w;
            Height = h;
            Phi = 0.0;
        }

        /// <summary>
        /// 有参构造-有旋转角度
        /// </summary>
        /// <param name="x">矩形旋转中心x坐标</param>
        /// <param name="y">矩形旋转中心y坐标</param>
        /// <param name="w">矩形宽度</param>
        /// <param name="h">矩形高度</param>
        /// <param name="a">矩形旋转角</param>
        public Rectangle2(double x, double y, double w, double h, double a)
        {
            XCenter = x;
            YCenter = y;
            Width = w;
            Height = h;
            Phi = a;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>中心矩形</returns>
        public Rectangle2 Clone()
        {
            Rectangle2 obj = new Rectangle2(XCenter, YCenter, Width, Height, Phi);
            return obj;
        }

        /// <summary>
        /// 获取四个角点坐标集合
        /// </summary>
        /// <returns>矩形角点</returns>
        public List<Point> GetCornerPoints()
        {
            var cornerPoints = new List<Point>();

            double hv_RowT = 0;
            double hv_ColT = 0;
            double hv_centerX = _xCenter;
            double hv_centerY = _yCenter;
            double hv_len1 = _width / 2;
            double hv_len2 = _height / 2;
            double hv_Cos = Math.Cos(_phi);
            double hv_Sin = Math.Sin(_phi);

            hv_ColT = (hv_centerX + (hv_len1 * hv_Cos)) - (hv_len2 * hv_Sin);
            hv_RowT = hv_centerY - ((hv_len1 * hv_Sin) + (hv_len2 * hv_Cos));
            cornerPoints.Add(new Point(hv_ColT, hv_RowT));

            //左下
            hv_ColT = (hv_centerX - (hv_len1 * hv_Cos)) - (hv_len2 * hv_Sin);
            hv_RowT = hv_centerY - (((-hv_len1) * hv_Sin) + (hv_len2 * hv_Cos));
            cornerPoints.Add(new Point(hv_ColT, hv_RowT));

            //右下
            hv_ColT = (hv_centerX - (hv_len1 * hv_Cos)) + (hv_len2 * hv_Sin);
            hv_RowT = hv_centerY - (((-hv_len1) * hv_Sin) - (hv_len2 * hv_Cos));
            cornerPoints.Add(new Point(hv_ColT, hv_RowT));

            //右上
            hv_ColT = (hv_centerX + (hv_len1 * hv_Cos)) + (hv_len2 * hv_Sin);
            hv_RowT = hv_centerY - ((hv_len1 * hv_Sin) - (hv_len2 * hv_Cos));
            cornerPoints.Add(new Point(hv_ColT, hv_RowT));

            return cornerPoints;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="fileName">保存路径</param>
        /// <param name="section">节点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Save(string fileName, string section)
        {
            if (fileName is null || section is null)
            {
                return Errortype.SAVE_INPUT_NULL;
            }

            string path = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            List<string> keyList = new List<string>();
            keyList.Add("xStart");
            keyList.Add("yStart");
            keyList.Add("width");
            keyList.Add("height");
            keyList.Add("phi");

            List<string> list = new List<string>();
            list.Add(XCenter.ToString());
            list.Add(YCenter.ToString());
            list.Add(Width.ToString());
            list.Add(Height.ToString());
            list.Add(Phi.ToString());

            //1.判断文件夹是否存在，若不存在，则创建
            //2.将数据写入到文件中
            IniHelper.AddSectionWithKeyValues(section, keyList, list, fileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="filename">加载路径</param>
        /// <param name="section">节点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Load(string filename, string section)
        {
            if (!File.Exists(filename))
            {
                return Errortype.FILE_NOT_EXIST;
            }

            string[] keys = null;
            string[] values = null;

            IniHelper.GetAllKeyValues(section, out keys, out values, filename);

            int index = Array.IndexOf(keys, "xStart");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_RECTANGLE2_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE2_VALUE_ERROR;
            }

            XCenter = double.Parse(values[index]);

            index = Array.IndexOf(keys, "yStart");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_RECTANGLE2_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE2_VALUE_ERROR;
            }

            YCenter = double.Parse(values[index]);

            index = Array.IndexOf(keys, "width");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_RECTANGLE2_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE2_VALUE_ERROR;
            }

            Width = double.Parse(values[index]);

            index = Array.IndexOf(keys, "height");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_RECTANGLE2_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE2_VALUE_ERROR;
            }

            Height = double.Parse(values[index]);

            index = Array.IndexOf(keys, "phi");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_RECTANGLE2_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE2_VALUE_ERROR;
            }

            Phi = double.Parse(values[index]);

            return Errortype.OK;
        }
    }
}
