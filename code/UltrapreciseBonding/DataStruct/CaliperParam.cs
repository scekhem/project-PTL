using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniFileHelper;
using System.IO;
using UltrapreciseBonding.Ransac;

namespace DataStruct
{
    /// <summary>
    /// 卡尺极性 当为auto时会进行自动判断
    /// </summary>
    public enum Polarity
    {
        Auto,
        Positive,
        Negative,
    }

    /// <summary>
    /// 卡尺参数
    /// </summary>
    public class CaliperParams : DataStructBase
    {
        private int _numMeasures; //卡尺数量
        private int _numInstances; //每个度量对象成功拟合实例的最大数量
        private double _measureSigma; //高斯平滑参数
        private double _measureLength1; //垂直于测量线的长度的一半
        private double _measureLength2; //平行于测量线的长度的一半
        private double _measureThreshold; //测量阈值
        private double _minScore;        //最小得分
        private string _measureSelect; //是否选择端点
        private string _measureTransition; //测量极性 使用暗/亮或亮/暗边。positive negative
        private string _measureInterpolation; //插值类型

        private double _circleStartPhi = 0.0;
        private double _circleEndPhi = 2 * Math.PI;

        private RansacParam _ransacParam; //ransac参数

        /// <summary>
        /// Gets or sets the user's _numMeasures
        /// </summary>
        public int NumMeasures
        {
            get { return _numMeasures; }
            set { _numMeasures = value; }
        }

        /// <summary>
        /// Gets or sets the user's _numInstances
        /// </summary>
        public int NumInstances
        {
            get { return _numInstances; }
            set { _numInstances = value; }
        }

        /// <summary>
        /// Gets or sets the user's _measureSigma
        /// </summary>
        public double MeasureSigma
        {
            get { return _measureSigma; }
            set { _measureSigma = value; }
        }

        /// <summary>
        /// Gets or sets the user's _measureLength1
        /// </summary>
        public double MeasureLength1
        {
            get { return _measureLength1; }
            set { _measureLength1 = value; }
        }

        /// <summary>
        /// Gets or sets the user's _measureLength2
        /// </summary>
        public double MeasureLength2
        {
            get { return _measureLength2; }
            set { _measureLength2 = value; }
        }

        /// <summary>
        /// Gets or sets the user's _measureThreshold
        /// </summary>
        public double MeasureThreshold
        {
            get { return _measureThreshold; }
            set { _measureThreshold = value; }
        }

        /// <summary>
        /// Gets or sets the user's _minScore
        /// </summary>
        public double MinScore
        {
            get { return _minScore; }
            set { _minScore = value; }
        }

        /// <summary>
        /// Gets or sets the user's _measureTransition
        /// </summary>
        public string MeasureTransition
        {
            get { return _measureTransition; }
            set { _measureTransition = value; }
        }

        /// <summary>
        /// Gets or sets the user's _measureSelect
        /// </summary>
        public string MeasureSelect
        {
            get { return _measureSelect; }
            set { _measureSelect = value; }
        }

        /// <summary>
        /// Gets or sets the user's _measureInterpolation
        /// </summary>
        public string MeasureInterpolation
        {
            get { return _measureInterpolation; }
            set { _measureInterpolation = value; }
        }

        /// <summary>
        /// Gets or sets the user's _circleStartPhi
        /// </summary>
        public double CircleStartPhi
        {
            get => _circleStartPhi;
            set => _circleStartPhi = value;
        }

        /// <summary>
        /// Gets or sets the user's _circleEndPhi
        /// </summary>
        public double CircleEndPhi
        {
            get => _circleEndPhi;
            set => _circleEndPhi = value;
        }

        /// <summary>
        /// Gets or sets the user's RansacParam
        /// </summary>
        public RansacParam RansacParam
        {
            get => _ransacParam;
            set => _ransacParam = value;
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CaliperParams()
        {
            NumMeasures = 120;
            NumInstances = 1;
            MeasureSigma = 1;
            MeasureLength1 = 15;
            MeasureLength2 = 4;
            MeasureThreshold = 30;
            MinScore = 0.9;
            MeasureSelect = "all";
            MeasureTransition = "auto";
            MeasureInterpolation = "nearest_neighbor";
            CircleStartPhi = 0.0;
            CircleEndPhi = 2 * Math.PI;
            RansacParam = new RansacParam();
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>卡尺参数</returns>
        public CaliperParams Clone()
        {
            CaliperParams obj = new CaliperParams(NumMeasures, NumInstances, MeasureSigma, MeasureLength1,
                MeasureLength2, MeasureThreshold, MinScore, MeasureSelect, MeasureTransition, MeasureInterpolation, RansacParam);
            return obj;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="num_Measures">卡尺数量</param>
        /// <param name="num_Instances">每个计量对象成功拟合实例的最大数量</param>
        /// <param name="measure_Sigma">高斯平滑参数</param>
        /// <param name="measure_Length1">卡尺长度的一半</param>
        /// <param name="measure_Length2">卡尺宽度的一半</param>
        /// <param name="measure_Threshold">卡尺阈值</param>
        /// <param name="min_Score">拟合最小得分=边缘数量/测量区域的最大数量</param>
        /// <param name="measure_Select">指定边缘的端点选择List of values: 'all', 'first', 'last'</param>
        /// <param name="measure_transition">边缘极性,List of values: 'all', 'negative'(从白到黑), 'positive'（从黑到白）</param>
        /// <param name="measure_interpolation">插值类型,Default value: 'nearest_neighbor',List of values: 'bicubic', 'bilinear', 'nearest_neighbor'</param>
        /// <param name="ransacParam">ransac参数</param>
        public CaliperParams(int num_Measures, int num_Instances, double measure_Sigma, double measure_Length1, double measure_Length2,
            double measure_Threshold, double min_Score, string measure_Select, string measure_transition, string measure_interpolation, RansacParam ransacParam)
        {
            NumMeasures = num_Measures;
            NumInstances = num_Instances;
            MeasureSigma = measure_Sigma;
            MeasureLength1 = measure_Length1;
            MeasureLength2 = measure_Length2;
            MeasureThreshold = measure_Threshold;
            MinScore = min_Score;
            MeasureSelect = measure_Select;
            MeasureTransition = measure_transition;
            MeasureInterpolation = measure_interpolation;
            RansacParam = ransacParam;
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
            keyList.Add("NumMeasures");
            keyList.Add("NumInstances");
            keyList.Add("MeasureSigma");
            keyList.Add("MeasureLength1");
            keyList.Add("MeasureLength2");
            keyList.Add("MeasureThreshold");
            keyList.Add("MinScore");
            keyList.Add("MeasureSelect");
            keyList.Add("MeasureTransition");
            keyList.Add("MeasureInterpolation");
            keyList.Add("CircleStartPhi");
            keyList.Add("CircleEndPhi");
            List<string> list = new List<string>();
            list.Add(NumMeasures.ToString());
            list.Add(NumInstances.ToString());
            list.Add(MeasureSigma.ToString());
            list.Add(MeasureLength1.ToString());
            list.Add(MeasureLength2.ToString());
            list.Add(MeasureThreshold.ToString());
            list.Add(MinScore.ToString());
            list.Add(MeasureSelect);
            list.Add(MeasureTransition);
            list.Add(MeasureInterpolation);
            list.Add(CircleStartPhi.ToString());
            list.Add(CircleEndPhi.ToString());

            //2.将数据写入到文件中
            IniHelper.AddSectionWithKeyValues(section, keyList, list, fileName);

            Errortype ret = RansacParam.Save(fileName, section);
            if (ret != Errortype.OK)
            {
                return ret;
            }

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

            int index = Array.IndexOf(keys, "NumMeasures");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            NumMeasures = int.Parse(values[index]);

            index = Array.IndexOf(keys, "NumInstances");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            NumInstances = int.Parse(values[index]);

            index = Array.IndexOf(keys, "MeasureSigma");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MeasureSigma = double.Parse(values[index]);

            index = Array.IndexOf(keys, "MeasureLength1");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MeasureLength1 = double.Parse(values[index]);

            index = Array.IndexOf(keys, "MeasureLength2");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MeasureLength2 = double.Parse(values[index]);

            index = Array.IndexOf(keys, "MeasureThreshold");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MeasureThreshold = double.Parse(values[index]);

            index = Array.IndexOf(keys, "MinScore");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MinScore = double.Parse(values[index]);

            index = Array.IndexOf(keys, "MeasureSelect");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MeasureSelect = values[index];

            index = Array.IndexOf(keys, "MeasureTransition");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MeasureTransition = values[index];

            index = Array.IndexOf(keys, "MeasureInterpolation");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MeasureInterpolation = values[index];

            index = Array.IndexOf(keys, "CircleStartPhi");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            CircleStartPhi = double.Parse(values[index]);

            index = Array.IndexOf(keys, "CircleEndPhi");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            CircleEndPhi = double.Parse(values[index]);

            Errortype ret = RansacParam.Load(filename, section);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// Ransac参数
    /// </summary>
    public class RansacParam : DataStructBase
    {
        /// <summary>
        /// Gets or sets 最大迭代次数
        /// </summary>
        public int MaxIterator { get; set; } = 10;

        /// <summary>
        /// Gets or sets 种子点
        /// </summary>
        public int RandSeed { get; set; } = 42;

        /// <summary>
        /// Gets or sets 最小分数
        /// </summary>
        public double MinScore { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets 距离阈值
        /// </summary>
        public double DistanceThreshold { get; set; } = 3.5;

        /// <summary>
        /// 默认构造
        /// </summary>
        public RansacParam()
        {
        }

        /// <summary>
        /// 有参构造
        /// </summary>
        /// <param name="maxIterator">最大迭代次数</param>
        /// <param name="randSeed">种子点</param>
        /// <param name="minScore">最小分数</param>
        /// <param name="distanceThreshold">距离阈值</param>
        public RansacParam(int maxIterator, int randSeed, double minScore, double distanceThreshold)
        {
            MaxIterator = maxIterator;
            RandSeed = randSeed;
            MinScore = minScore;
            DistanceThreshold = distanceThreshold;
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
            keyList.Add("RansacMaxIterator");
            keyList.Add("RansacRandSeed");
            keyList.Add("RansacMinScore");
            keyList.Add("RansacDistanceThreshold");
            List<string> list = new List<string>();
            list.Add(MaxIterator.ToString());
            list.Add(RandSeed.ToString());
            list.Add(MinScore.ToString());
            list.Add(DistanceThreshold.ToString());

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

            int index = Array.IndexOf(keys, "RansacMaxIterator");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MaxIterator = int.Parse(values[index]);

            index = Array.IndexOf(keys, "RansacRandSeed");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            RandSeed = int.Parse(values[index]);

            index = Array.IndexOf(keys, "RansacMinScore");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            MinScore = double.Parse(values[index]);

            index = Array.IndexOf(keys, "RansacDistanceThreshold");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_CALIPERPARAM_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_CALIPERPARAM_VALUE_ERROR;
            }

            DistanceThreshold = double.Parse(values[index]);

            return Errortype.OK;
        }
    }
}