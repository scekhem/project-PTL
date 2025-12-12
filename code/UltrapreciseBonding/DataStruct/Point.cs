using IniFileHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace DataStruct
{
    /// <summary>
    /// 点
    /// </summary>
    public class Point : DataStructBase
    {
        private double _x;
        private double _y;

        /// <summary>
        /// Gets or sets the user's _x
        /// </summary>
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Gets or sets the user's _y
        /// </summary>
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// 默认构造
        /// </summary>
        public Point()
        {
        }

        /// <summary>
        /// 有参构造
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        public Point(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }

        /// <summary>
        /// 重载“+”
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Point operator +(Point p1, Point p2)
        {
            if (p1 is null && p2 is null)
            {
                return null;
            }
            else if (p1 is null)
            {
                return p2;
            }
            else if (p2 is null)
            {
                return p1;
            }

            return new Point(p1.X + p2.X, p1.Y + p2.Y);
        }

        /// <summary>
        /// 重载“-”
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Point operator -(Point p1, Point p2)
        {
            if (p1 is null && p2 is null)
            {
                return null;
            }
            else if (p1 is null)
            {
                return p2 * -1;
            }
            else if (p2 is null)
            {
                return p1;
            }

            return new Point(p1.X - p2.X, p1.Y - p2.Y);
        }

        /// <summary>
        /// 重载“*”
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="op">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Point operator *(Point p1, double op)
        {
            if (p1 is null)
            {
                return null;
            }

            return new Point(p1.X * op, p1.Y * op);
        }

        /// <summary>
        /// 重载“/”
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="op">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Point operator /(Point p1, double op)
        {
            if (p1 is null)
            {
                return null;
            }

            return new Point(p1.X / op, p1.Y / op);
        }

        /// <summary>
        /// 判断点相等
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool operator ==(Point p1, Point p2)
        {
            if (p1 is null)
            {
                if (p2 is null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (p2 is null)
            {
                return false;
            }

            if (Math.Abs(p1.X - p2.X) < 1e-3 && Math.Abs(p1.Y - p2.Y) < 1e-3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 获取hash代码
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        /// <summary>
        /// 是否相等
        /// </summary>
        /// <param name="obj">比较对象</param>
        /// <returns>对象是否相等</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            Point other = (Point)obj;
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// 判断不相等
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>是否不相等</returns>
        public static bool operator !=(Point p1, Point p2)
        {
            if (p1 == p2)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>拷贝对象</returns>
        public Point Clone()
        {
            Point obj = new Point(_x, _y);
            return obj;
        }

        /// <summary>
        /// 转为字符串
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <param name="decimalDigits">保留小数位数</param>
        /// <returns>转换后字符串</returns>
        public string ToString(string separator, int decimalDigits = 6)
        {
            if (decimalDigits < 0)
            {
                decimalDigits = 0;
            }

            string format = "f" + decimalDigits.ToString();
            string ret = X.ToString(format) + separator + Y.ToString(format);
            return ret;
        }

        /// <summary>
        /// 计算点到目标点的距离
        /// </summary>
        /// <param name="target">目标点</param>
        /// <returns>距离</returns>
        public double DistanceTo(Point target)
        {
            if (target == null)
            {
                return 0.0;
            }

            Point distXY = this - target;
            return Math.Sqrt(Math.Pow(distXY.X, 2) + Math.Pow(distXY.Y, 2));
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
            keyList.Add("X");
            keyList.Add("Y");
            List<string> list = new List<string>();
            list.Add(X.ToString());
            list.Add(Y.ToString());

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

            int index = Array.IndexOf(keys, "X");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_POINT_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_POINT_VALUE_ERROR;
            }

            X = double.Parse(values[index]);

            index = Array.IndexOf(keys, "Y");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_POINT_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_POINT_VALUE_ERROR;
            }

            Y = double.Parse(values[index]);

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 点 3D
    /// </summary>
    public class Point3D : DataStructBase
    {
        private double _x;
        private double _y;
        private double _z;

        /// <summary>
        /// Gets or sets the user's _x
        /// </summary>
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// Gets or sets the user's _y
        /// </summary>
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// Gets or sets the user's _z
        /// </summary>
        public double Z
        {
            get { return _z; }
            set { _z = value; }
        }

        /// <summary>
        /// Point3D默认构造函数
        /// </summary>
        public Point3D()
        {
        }

        /// <summary>
        /// Point3D有参构造
        /// </summary>
        /// <param name="x">坐标x</param>
        /// <param name="y">坐标y</param>
        /// <param name="z">坐标z</param>
        public Point3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        /// <summary>
        /// 重载“+”
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Point3D operator +(Point3D p1, Point3D p2)
        {
            if (p1 is null && p2 is null)
            {
                return null;
            }
            else if (p1 is null)
            {
                return p2;
            }
            else if (p2 is null)
            {
                return p1;
            }

            return new Point3D(p1.X + p2.X, p1.Y + p2.Y, p1.Z + p2.Z);
        }

        /// <summary>
        /// 重载“-”
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Point3D operator -(Point3D p1, Point3D p2)
        {
            if (p1 is null && p2 is null)
            {
                return null;
            }
            else if (p1 is null)
            {
                return p2 * -1;
            }
            else if (p2 is null)
            {
                return p1;
            }

            return new Point3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);
        }

        /// <summary>
        /// 重载“*”
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="op">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Point3D operator *(Point3D p1, double op)
        {
            if (p1 is null)
            {
                return null;
            }

            return new Point3D(p1.X * op, p1.Y * op, p1.Z * op);
        }

        /// <summary>
        /// 重载“/”
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="op">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Point3D operator /(Point3D p1, double op)
        {
            if (p1 is null)
            {
                return null;
            }

            return new Point3D(p1.X / op, p1.Y / op, p1.Z / op);
        }

        /// <summary>
        /// 判断点相等
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool operator ==(Point3D p1, Point3D p2)
        {
            if (p1 is null)
            {
                if (p2 is null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (p2 is null)
            {
                return false;
            }

            if (Math.Abs(p1.X - p2.X) < 1e-3 && Math.Abs(p1.Y - p2.Y) < 1e-3 && Math.Abs(p1.Z - p2.Z) < 1e-3)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 判断不相等
        /// </summary>
        /// <param name="p1">点1</param>
        /// <param name="p2">点2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool operator !=(Point3D p1, Point3D p2)
        {
            if (p1 == p2)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>拷贝对象</returns>
        public Point3D Clone()
        {
            Point3D obj = new Point3D(X, Y, Z);
            return obj;
        }

        /// <summary>
        /// 转为字符串
        /// </summary>
        /// <param name="separator">分隔符</param>
        /// <param name="decimalDigits">保留小数位数</param>
        /// <returns>转换后字符串</returns>
        public string ToString(string separator, int decimalDigits = 6)
        {
            if (decimalDigits < 0)
            {
                decimalDigits = 0;
            }

            string format = "f" + decimalDigits.ToString();
            string ret = X.ToString(format) + separator + Y.ToString(format) + separator + Z.ToString(format);
            return ret;
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
            keyList.Add("X");
            keyList.Add("Y");
            keyList.Add("Z");
            List<string> list = new List<string>();
            list.Add(X.ToString());
            list.Add(Y.ToString());
            list.Add(Z.ToString());

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

            int index = Array.IndexOf(keys, "X");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_POINT_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_POINT_VALUE_ERROR;
            }

            X = double.Parse(values[index]);

            index = Array.IndexOf(keys, "Y");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_POINT_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_POINT_VALUE_ERROR;
            }

            Y = double.Parse(values[index]);

            index = Array.IndexOf(keys, "Z");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_POINT_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_POINT_VALUE_ERROR;
            }

            Z = double.Parse(values[index]);

            return Errortype.OK;
        }
    }
}
