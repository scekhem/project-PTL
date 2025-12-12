using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniFileHelper;
using System.IO;
using System.Security.Cryptography;

namespace DataStruct
{
    /// <summary>
    /// 线段
    /// </summary>
    public class LineSeg : DataStructBase
    {
        private double _startX; //起点X
        private double _startY; //起点Y
        private double _endX; //终点X
        private double _endY; //终点Y
        private String _probInfo = String.Empty;    // 属性信息

        /// <summary>
        /// Gets or sets the user's _startX
        /// </summary>
        public double Start_X
        {
            get { return _startX; }
            set { _startX = value; }
        }

        /// <summary>
        /// Gets or sets the user's _startY
        /// </summary>
        public double Start_Y
        {
            get { return _startY; }
            set { _startY = value; }
        }

        /// <summary>
        /// Gets or sets the user's _endX
        /// </summary>
        public double End_X
        {
            get { return _endX; }
            set { _endX = value; }
        }

        /// <summary>
        /// Gets or sets the user's _endY
        /// </summary>
        public double End_Y
        {
            get { return _endY; }
            set { _endY = value; }
        }

        /// <summary>
        /// Gets or sets the user's _probInfo
        /// </summary>
        public String ProbInfo
        {
            get { return _probInfo; }
            set { _probInfo = value; }
        }

        /// <summary>
        /// Gets the user's new Point(Start_X, Start_Y)
        /// </summary>
        public Point Start
        {
            get { return new Point(Start_X, Start_Y); }
        }

        /// <summary>
        /// Gets the user's new Point(End_X, End_Y)
        /// </summary>
        public Point End
        {
            get { return new Point(End_X, End_Y); }
        }

        /// <summary>
        /// 默认构造
        /// </summary>
        public LineSeg()
        {
        }

        /// <summary>
        /// 有参构造 起始点
        /// </summary>
        /// <param name="start_x">起始点x坐标</param>
        /// <param name="startY">起始点y坐标</param>
        /// <param name="end_x">终点x坐标</param>
        /// <param name="end_y">终点y坐标</param>
        public LineSeg(double start_x, double startY, double end_x, double end_y)
        {
            Start_X = start_x;
            Start_Y = startY;
            End_X = end_x;
            End_Y = end_y;
        }

        /// <summary>
        /// 有参构造 线段起始点
        /// </summary>
        /// <param name="p1">线段起点</param>
        /// <param name="p2">线段终点</param>
        public LineSeg(Point p1, Point p2)
        {
            Start_X = p1.X;
            Start_Y = p1.Y;
            End_X = p2.X;
            End_Y = p2.Y;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>线段拷贝值</returns>
        public LineSeg Clone()
        {
            LineSeg obj = new LineSeg(Start, End);
            obj.ProbInfo = ProbInfo;
            return obj;
        }

        /// <summary>
        /// 计算线段与x轴的夹角
        /// </summary>
        /// <returns>线段角度</returns>
        public double CalculateLineDeg()
        {
            if (End_X - Start_X == 0)
            {
                if (End_Y - Start_Y == 0)
                {
                    return 0;
                }
                else
                {
                    return End_Y - Start_Y > 0 ? 90 : -90;
                }
            }
            else
            {
                double k = (End_Y - Start_Y) / (End_X - Start_X);
                double a = Math.Atan(k);
                return a * 180 / Math.PI;
            }
        }

        /// <summary>
        /// 计算线段的长度
        /// </summary>
        /// <returns>线段长度</returns>
        public double CalculateLineLength()
        {
            double dist = Math.Sqrt(Math.Pow(End_X - Start_X, 2) + Math.Pow(End_Y - Start_Y, 2));
            return dist;
        }

        /// <summary>
        /// 获取线段的中心
        /// </summary>
        /// <returns> 线段中心点</returns>
        public Point Center()
        {
            return new Point((Start_X + End_X) / 2, (Start_Y + End_Y) / 2);
        }

        /// <summary>
        /// 将线段进行等比例缩小
        /// </summary>
        /// <param name="scale">缩放比例</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Shorten(double scale)
        {
            double sx = (End_X - Start_X) * scale + Start_X;
            double sy = (End_Y - Start_Y) * scale + Start_Y;
            double ex = (End_X - Start_X) * (1 - scale) + Start_X;
            double ey = (End_Y - Start_Y) * (1 - scale) + Start_Y;

            Start_X = sx;
            Start_Y = sy;
            End_X = ex;
            End_Y = ey;

            return Errortype.OK;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="section">段落名</param>
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
            keyList.Add("_start_x");
            keyList.Add("_start_y");
            keyList.Add("_end_x");
            keyList.Add("_end_y");
            List<string> list = new List<string>();
            list.Add(Start_X.ToString());
            list.Add(Start_Y.ToString());
            list.Add(End_X.ToString());
            list.Add(End_Y.ToString());
            if (ProbInfo != String.Empty)
            {
                keyList.Add("_prob_info");
                list.Add(ProbInfo);
            }

            //2.将数据写入到文件中
            IniHelper.AddSectionWithKeyValues(section, keyList, list, fileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <param name="section">段落名</param>
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

            int index = Array.IndexOf(keys, "_start_x");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_LINESEG_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_LINESEG_VALUE_ERROR;
            }

            Start_X = double.Parse(values[index]);

            index = Array.IndexOf(keys, "_start_y");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_LINESEG_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_LINESEG_VALUE_ERROR;
            }

            Start_Y = double.Parse(values[index]);

            index = Array.IndexOf(keys, "_end_x");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_LINESEG_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_LINESEG_VALUE_ERROR;
            }

            End_X = double.Parse(values[index]);

            index = Array.IndexOf(keys, "_end_y");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_LINESEG_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_LINESEG_VALUE_ERROR;
            }

            End_Y = double.Parse(values[index]);

            index = Array.IndexOf(keys, "_prob_info");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.OK;
            }

            ProbInfo = values[index];

            return Errortype.OK;
        }
    }
}
