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
    /// 圆类型
    /// </summary>
    public class Circle : DataStructBase
    {
        private double _centerX;
        private double _centerY;
        private double _radius;
        private double _startAngleRad;
        private double _endAngleRad;

        /// <summary>
        /// Gets or sets 圆心X坐标
        /// </summary>
        public double CenterX
        {
            get { return _centerX; }
            set { _centerX = value; }
        }

        /// <summary>
        /// Gets or sets 圆心Y坐标
        /// </summary>
        public double CenterY
        {
            get { return _centerY; }
            set { _centerY = value; }
        }

        /// <summary>
        /// Gets or sets 半径
        /// </summary>
        public double Radius
        {
            get { return _radius; }
            set { _radius = value; }
        }

        /// <summary>
        /// Gets or sets 圆弧起始角度
        /// </summary>
        public double StartAngleRad
        {
            get { return _startAngleRad; }
            set { _startAngleRad = value; }
        }

        /// <summary>
        /// Gets or sets 圆弧终止角度
        /// </summary>
        public double EndAngleRad
        {
            get { return _endAngleRad; }
            set { _endAngleRad = value; }
        }

        ///<summary>
        /// 圆默认构造参数
        /// </summary>
        public Circle()
        {
        }

        /// <summary>
        /// 圆构造函数
        /// </summary>
        /// <param name="centerX">圆心X</param>
        /// <param name="centerY">圆心Y</param>
        /// <param name="radius">半径</param>
        /// <param name="startAngleRadRad">弧线起始角度</param>
        /// <param name="endAngleRadRad">弧线结束角度</param>
        public Circle(double centerX, double centerY, double radius, double startAngleRadRad = 0.0, double endAngleRadRad = 2 * Math.PI)
        {
            CenterX = centerX;
            CenterY = centerY;
            Radius = radius;
            StartAngleRad = startAngleRadRad;
            EndAngleRad = endAngleRadRad;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>返回深拷贝对象</returns>
        public Circle Clone()
        {
            Circle obj = new Circle(CenterX, CenterY, Radius, StartAngleRad, EndAngleRad);
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
            keyList.Add("centerX");
            keyList.Add("centerY");
            keyList.Add("radius");
            keyList.Add("startAngleRad");
            keyList.Add("endAngleRad");
            List<string> list = new List<string>();
            list.Add(_centerX.ToString());
            list.Add(_centerY.ToString());
            list.Add(_radius.ToString());
            list.Add(_startAngleRad.ToString());
            list.Add(_endAngleRad.ToString());

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

            int index = Array.IndexOf(keys, "centerX");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CIRCLE_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CIRCLE_VALUE_ERROR;
            }

            CenterX = double.Parse(values[index]);

            index = Array.IndexOf(keys, "centerY");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CIRCLE_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CIRCLE_VALUE_ERROR;
            }

            CenterY = double.Parse(values[index]);

            index = Array.IndexOf(keys, "radius");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CIRCLE_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CIRCLE_VALUE_ERROR;
            }

            Radius = double.Parse(values[index]);

            index = Array.IndexOf(keys, "startAngleRad");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CIRCLE_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CIRCLE_VALUE_ERROR;
            }

            StartAngleRad = double.Parse(values[index]);

            index = Array.IndexOf(keys, "endAngleRad");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CIRCLE_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CIRCLE_VALUE_ERROR;
            }

            EndAngleRad = double.Parse(values[index]);

            return Errortype.OK;
        }
    }
}
