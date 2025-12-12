using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniFileHelper;
using System.IO;

namespace DataStruct
{
    /// <summary>
    /// 正矩形
    /// </summary>
    public class Rectangle1 : DataStructBase
    {
        private double _start_x; //起点X
        private double _start_y; //起点Y
        private double _end_x; //终点X
        private double _end_y; //终点Y

        /// <summary>
        /// Gets or sets the user's _start_x
        /// </summary>
        public double Start_X
        {
            get { return _start_x; }
            set { _start_x = value; }
        }

        /// <summary>
        /// Gets or sets the user's _start_y
        /// </summary>
        public double Start_Y
        {
            get { return _start_y; }
            set { _start_y = value; }
        }

        /// <summary>
        /// Gets or sets the user's _end_x
        /// </summary>
        public double End_X
        {
            get { return _end_x; }
            set { _end_x = value; }
        }

        /// <summary>
        /// Gets or sets the user's _end_y
        /// </summary>
        public double End_Y
        {
            get { return _end_y; }
            set { _end_y = value; }
        }

        /// <summary>
        /// Rectangle1默认构造函数
        /// </summary>
        public Rectangle1()
        {
        }

        /// <summary>
        /// Rectangle1有参构造函数
        /// </summary>
        /// <param name="start_x">起始点坐标x</param>
        /// <param name="start_y">起始点坐标y</param>
        /// <param name="end_x">终点坐标x</param>
        /// <param name="end_y">终点坐标y</param>
        public Rectangle1(double start_x, double start_y, double end_x, double end_y)
        {
            Start_X = start_x;
            Start_Y = start_y;
            End_X = end_x;
            End_Y = end_y;
        }

        /// <summary>
        /// 获取四个角点坐标集合
        /// </summary>
        /// <returns>角点坐标</returns>
        public List<Point> GetCornerPoints()
        {
            var cornerPoints = new List<Point>();
            double rowLeftUp = Start_Y;
            double colLeftUp = Start_X;
            double colRighttdown = End_X;
            double rowRighttdown = End_Y;
            var colRightUP = colRighttdown;
            var rowRightUP = rowLeftUp;
            var colLeftDown = colLeftUp;
            var rowLeftDown = rowRighttdown;
            cornerPoints.Add(new Point(colLeftUp, rowLeftUp));
            cornerPoints.Add(new Point(colLeftDown, rowLeftDown));
            cornerPoints.Add(new Point(colRighttdown, rowRighttdown));
            cornerPoints.Add(new Point(colRightUP, rowRightUP));
            return cornerPoints;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>拷贝对象</returns>
        public Rectangle1 Clone()
        {
            Rectangle1 obj = new Rectangle1(Start_X, Start_Y, End_X, End_Y);
            return obj;
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
            list.Add(_start_x.ToString());
            list.Add(_start_y.ToString());
            list.Add(_end_x.ToString());
            list.Add(_end_y.ToString());

            //1.判断文件夹是否存在，若不存在，则创建
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
                return Errortype.READ_RECTANGLE1_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE1_VALUE_ERROR;
            }

            Start_X = double.Parse(values[index]);

            index = Array.IndexOf(keys, "_start_y");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_RECTANGLE1_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE1_VALUE_ERROR;
            }

            Start_Y = double.Parse(values[index]);

            index = Array.IndexOf(keys, "_end_x");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_RECTANGLE1_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE1_VALUE_ERROR;
            }

            End_X = double.Parse(values[index]);

            index = Array.IndexOf(keys, "_end_y");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_RECTANGLE1_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_RECTANGLE1_VALUE_ERROR;
            }

            End_Y = double.Parse(values[index]);

            return Errortype.OK;
        }
    }
}
