using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IniFileHelper;
using System.IO;
using System.Diagnostics.Contracts;
using System.Drawing.Drawing2D;
using OpenCvSharp.LineDescriptor;

namespace DataStruct
{
    /// <summary>
    /// ncc模板建立的参数
    /// </summary>
    public class NccTemplateParams : DataStructBase
    {
        private int _pyramid; //金字塔层数  -1代表默认auto
        private double _angleStart; //起始角度
        private double _angleExtent; //终止角度
        private string _angleStep; //角度间隔 包含 'auto'
        private string _metric; //是否使用极性 List of values: 'ignore_global_polarity', 'use_polarity'

        //filter
        private bool _smoothImg; //预处理是否平滑图像
        private double _filterSize; //预处理平滑图像的窗口大小

        /// <summary>
        /// Gets or sets the user's _pyramid
        /// </summary>
        public int Pyramid
        {
            get { return _pyramid; }
            set { _pyramid = value; }
        }

        /// <summary>
        /// Gets or sets the user's _angleStart
        /// </summary>
        public double AngleStart
        {
            get { return _angleStart; }
            set { _angleStart = value; }
        }

        /// <summary>
        /// Gets or sets the user's _angleExtent
        /// </summary>
        public double AngleExtent
        {
            get { return _angleExtent; }
            set { _angleExtent = value; }
        }

        /// <summary>
        /// Gets or sets the user's _angleStep
        /// </summary>
        public string AngleStep
        {
            get { return _angleStep; }
            set { _angleStep = value; }
        }

        /// <summary>
        /// Gets or sets the user's _metric
        /// </summary>
        public string Metric
        {
            get { return _metric; }
            set { _metric = value; }
        }

        /// <summary>
        /// 获取或设置一个值，该值指示是否光滑图像。
        /// </summary>
        public bool SmoothImg
        {
            get { return _smoothImg; }
            set { _smoothImg = value; }
        }

        /// <summary>
        /// Gets or sets the user's _filterSize
        /// </summary>
        public double FilterSize
        {
            get { return _filterSize; }
            set { _filterSize = value; }
        }

        /// <summary>
        /// 默认构造
        /// </summary>
        public NccTemplateParams()
        {
            _pyramid = -1;
            _angleStart = -0.39;
            _angleExtent = 0.79;
            _angleStep = "auto";
            _metric = "use_polarity";
            _smoothImg = false;
            _filterSize = 5;
        }

        /// <summary>
        /// ncc模板创建参数
        /// </summary>
        /// <param name="pyramid">金字塔层数,默认-1,List of values: -1,0,1,2,3,4,5</param>
        /// <param name="angleStart">起始角度,默认-0.39</param>
        /// <param name="angleExtent">终止角度,默认0.79</param>
        /// <param name="angleStep">角度步进,默认"auto"</param>
        /// <param name="metric">是否使用极性,默认“use_polarity” List of values: 'ignore_global_polarity', 'use_polarity'</param>
        /// <param name="smoothImg">是否平滑图像,默认false</param>
        /// <param name="filterSize">平滑图像窗口大小,默认5</param>
        public NccTemplateParams(int pyramid = -1, double angleStart = -0.39, double angleExtent = 0.79, string angleStep = "auto", string metric = "use_polarity", bool smoothImg = false, double filterSize = 5)
        {
            _pyramid = pyramid;
            _angleStart = angleStart;
            _angleExtent = angleExtent;
            _angleStep = angleStep;
            _metric = metric;
            _smoothImg = smoothImg;
            _filterSize = filterSize;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>模板参数</returns>
        public NccTemplateParams Clone()
        {
            NccTemplateParams obj = new NccTemplateParams(Pyramid, AngleStart, AngleExtent, AngleStep, Metric, SmoothImg, FilterSize);
            return obj;
        }

        /// <summary>
        /// 保存ncc模板建立的参数
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

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            List<string> keyList = new List<string>();
            keyList.Add("AngleStart");
            keyList.Add("AngleExtent");
            keyList.Add("AngleStep");
            keyList.Add("Metric");
            keyList.Add("Pyramid");
            keyList.Add("SmoothImg");
            keyList.Add("FilterSize");
            List<string> list = new List<string>();
            list.Add(AngleStart.ToString());
            list.Add(AngleExtent.ToString());
            list.Add(AngleStep.ToString());
            list.Add(Metric.ToString());
            list.Add(Pyramid.ToString());
            list.Add(SmoothImg.ToString());
            list.Add(FilterSize.ToString());

            //2.将数据写入到文件中
            IniHelper.AddSectionWithKeyValues(section, keyList, list, fileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 加载ncc模板建立的参数
        /// </summary>
        /// <param name="filename">加载路径</param>
        /// <param name="section">节名</param>
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

            int index = Array.IndexOf(keys, "AngleStart");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_VALUE_ERROR;
            }

            AngleStart = double.Parse(values[index]);

            index = Array.IndexOf(keys, "AngleExtent");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_VALUE_ERROR;
            }

            AngleExtent = double.Parse(values[index]);

            index = Array.IndexOf(keys, "AngleStep");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_VALUE_ERROR;
            }

            AngleStep = values[index];

            index = Array.IndexOf(keys, "Metric");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_VALUE_ERROR;
            }

            Metric = values[index];

            index = Array.IndexOf(keys, "Pyramid");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_VALUE_ERROR;
            }

            Pyramid = int.Parse(values[index]);

            index = Array.IndexOf(keys, "SmoothImg");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !bool.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_VALUE_ERROR;
            }

            SmoothImg = bool.Parse(values[index]);

            index = Array.IndexOf(keys, "FilterSize");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCTEMPLATEPARAMS_VALUE_ERROR;
            }

            FilterSize = double.Parse(values[index]);

            return Errortype.OK;
        }
    }

    /// <summary>
    /// ncc模板匹配参数
    /// </summary>
    public class NccMatchParams : DataStructBase
    {
        private double _angleStart; //起始角度
        private double _angleExtent; //终止角度
        private double _minScore; //最小得分
        private int _numMatches; //最大匹配数量
        private double _maxOverlap; //最大重合范围
        private bool _subPixel; //是否启用亚像素
        private int _pyramid; //金字塔层数

        //filter
        private bool _smoothImg; //是否平滑图像
        private double _filterSize; //平滑图像的窗口大小

        /// <summary>
        /// Gets or sets the user's _angleStart
        /// </summary>
        public double AngleStart
        {
            get { return _angleStart; }
            set { _angleStart = value; }
        }

        /// <summary>
        /// Gets or sets the user's _angleExtent
        /// </summary>
        public double AngleExtent
        {
            get { return _angleExtent; }
            set { _angleExtent = value; }
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
        /// Gets or sets the user's _numMatches
        /// </summary>
        public int NumMatches
        {
            get { return _numMatches; }
            set { _numMatches = value; }
        }

        /// <summary>
        /// Gets or sets the user's _maxOverlap
        /// </summary>
        public double MaxOverlap
        {
            get { return _maxOverlap; }
            set { _maxOverlap = value; }
        }

        /// <summary>
        /// 获取或设置一个值，该值指示是否求亚像素坐标
        /// </summary>
        public bool SubPixel
        {
            get { return _subPixel; }
            set { _subPixel = value; }
        }

        /// <summary>
        /// Gets or sets the user's _pyramid
        /// </summary>
        public int Pyramid
        {
            get { return _pyramid; }
            set { _pyramid = value; }
        }

        /// <summary>
        /// 获取或设置一个值，该值指示是否平滑图像
        /// </summary>
        public bool SmoothImg
        {
            get { return _smoothImg; }
            set { _smoothImg = value; }
        }

        /// <summary>
        /// Gets or sets the user's _filterSize
        /// </summary>
        public double FilterSize
        {
            get { return _filterSize; }
            set { _filterSize = value; }
        }

        /// <summary>
        /// ncc默认构造
        /// </summary>
        public NccMatchParams()
        {
            _angleStart = -0.39;
            _angleExtent = 0.79;
            _minScore = 0.8;
            _numMatches = 1;
            _maxOverlap = 0.5;
            _subPixel = true;
            _pyramid = 0;
            _smoothImg = false;
            _filterSize = 5;
        }

        /// <summary>
        /// ncc模板匹配参数
        /// </summary>
        /// <param name="angleStart">起始角度,默认-0.39</param>
        /// <param name="angleExtent">终止角度,默认0.79</param>
        /// <param name="minScore">最小得分,默认0.9</param>
        /// <param name="numMatches">目标匹配数量,默认0,代表识别图像中所有对象</param>
        /// <param name="maxOverlap">相邻目标最大重合度,默认0.5</param>
        /// <param name="subPixel">是否使用亚像素</param>
        /// <param name="pyramid">金字塔层数,默认0,List of values: 0,1,2,3,4,5</param>
        /// <param name="smoothImg">是否平滑图像,默认false</param>
        /// <param name="filterSize">平滑图像窗口大小,默认5</param>
        public NccMatchParams(double angleStart = -0.39, double angleExtent = 0.79, double minScore = 0.9, int numMatches = 0, double maxOverlap = 0.5, bool subPixel = true, int pyramid = 0, bool smoothImg = false, double filterSize = 5)
        {
            _angleStart = angleStart;
            _angleExtent = angleExtent;
            _minScore = minScore;
            _numMatches = numMatches;
            _maxOverlap = maxOverlap;
            _subPixel = subPixel;
            _pyramid = pyramid;
            _smoothImg = smoothImg;
            _filterSize = filterSize;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>ncc模板匹配参数</returns>
        public NccMatchParams Clone()
        {
            NccMatchParams obj = new NccMatchParams(AngleStart, AngleExtent, MinScore, NumMatches, MaxOverlap, SubPixel, Pyramid, SmoothImg, FilterSize);
            return obj;
        }

        /// <summary>
        /// 保存ncc匹配参数
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

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            List<string> keyList = new List<string>();
            keyList.Add("AngleStart");
            keyList.Add("AngleExtent");
            keyList.Add("MinScore");
            keyList.Add("NumMatches");
            keyList.Add("MaxOverlap");
            keyList.Add("SubPixel");
            keyList.Add("Pyramid");
            keyList.Add("SmoothImg");
            keyList.Add("FilterSize");

            List<string> list = new List<string>();
            list.Add(AngleStart.ToString());
            list.Add(AngleExtent.ToString());
            list.Add(MinScore.ToString());
            list.Add(NumMatches.ToString());
            list.Add(MaxOverlap.ToString());
            list.Add(SubPixel.ToString());
            list.Add(Pyramid.ToString());
            list.Add(SmoothImg.ToString());
            list.Add(FilterSize.ToString());

            //2.将数据写入到文件中
            IniHelper.AddSectionWithKeyValues(section, keyList, list, fileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 加载ncc匹配参数
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

            int index = Array.IndexOf(keys, "AngleStart");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            AngleStart = double.Parse(values[index]);

            index = Array.IndexOf(keys, "AngleExtent");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            AngleExtent = double.Parse(values[index]);

            index = Array.IndexOf(keys, "MinScore");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            MinScore = double.Parse(values[index]);

            index = Array.IndexOf(keys, "NumMatches");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            NumMatches = int.Parse(values[index]);

            index = Array.IndexOf(keys, "MaxOverlap");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            MaxOverlap = double.Parse(values[index]);

            index = Array.IndexOf(keys, "SubPixel");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !bool.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            SubPixel = bool.Parse(values[index]);

            index = Array.IndexOf(keys, "Pyramid");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            Pyramid = int.Parse(values[index]);

            index = Array.IndexOf(keys, "SmoothImg");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !bool.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            SmoothImg = bool.Parse(values[index]);

            index = Array.IndexOf(keys, "FilterSize");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_NCCMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_NCCMATCHPARAMS_VALUE_ERROR;
            }

            FilterSize = double.Parse(values[index]);

            return Errortype.OK;
        }
    }

    /// <summary>
    /// shape模板建立参数
    /// </summary>
    public class ShapeTemplateParams : DataStructBase
    {
        private int _pyramid; //金字塔层数  -1代表默认auto
        private double _angleStart; //起始角度
        private double _angleExtent; //终止角度
        private string _angleStep; //角度间隔, 包含 'auto'
        private double _scaleMin; //起始缩放比
        private double _scaleMax; //终止缩放比
        private string _scaleStep; //缩放比间隔, 包含 'auto'
        private string _optimization; //模型优化选择  包含 "auto", "no_pregeneration", "none", "point_reduction_high", "point_reduction_low", "point_reduction_medium", "pregeneration"
        private string _metric; //是否使用极性 List of values: 'ignore_global_polarity', 'use_polarity'
        private string _contrast; //背景和shape对比度阈值 "auto", "auto_contrast", "auto_contrast_hyst", "auto_min_size", 10, 20, 30, 40
        private string _minContrast; //背景和shape对比度阈值 "auto", 1, 2, 3, 5, 7, 10, 20, 30, 40

        //filter
        private bool _smoothImg; //预处理是否平滑图像
        private double _filterSize; //预处理平滑图像的窗口大小

        /// <summary>
        /// Gets or sets the user's _pyramid
        /// </summary>
        public int Pyramid { get => _pyramid; set => _pyramid = value; }

        /// <summary>
        /// Gets or sets the user's _angleStart
        /// </summary>
        public double AngleStart { get => _angleStart; set => _angleStart = value; }

        /// <summary>
        /// Gets or sets the user's _angleExtent
        /// </summary>
        public double AngleExtent { get => _angleExtent; set => _angleExtent = value; }

        /// <summary>
        /// Gets or sets the user's _angleStep
        /// </summary>
        public string AngleStep { get => _angleStep; set => _angleStep = value; }

        /// <summary>
        /// Gets or sets the user's _scaleMin
        /// </summary>
        public double ScaleMin { get => _scaleMin; set => _scaleMin = value; }

        /// <summary>
        /// Gets or sets the user's _scaleMax
        /// </summary>
        public double ScaleMax { get => _scaleMax; set => _scaleMax = value; }

        /// <summary>
        /// Gets or sets the user's _scaleStep
        /// </summary>
        public string ScaleStep { get => _scaleStep; set => _scaleStep = value; }

        /// <summary>
        /// Gets or sets the user's _optimization
        /// </summary>
        public string Optimization { get => _optimization; set => _optimization = value; }

        /// <summary>
        /// Gets or sets the user's _metric
        /// </summary>
        public string Metric { get => _metric; set => _metric = value; }

        /// <summary>
        /// Gets or sets the user's _contrast
        /// </summary>
        public string Contrast { get => _contrast; set => _contrast = value; }

        /// <summary>
        /// Gets or sets the user's _minContrast
        /// </summary>
        public string MinContrast { get => _minContrast; set => _minContrast = value; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否平滑图像。
        /// </summary>
        public bool SmoothImg { get => _smoothImg; set => _smoothImg = value; }

        /// <summary>
        /// Gets or sets the user's _filterSize
        /// </summary>
        public double FilterSize { get => _filterSize; set => _filterSize = value; }

        /// <summary>
        /// shape模板参数
        /// </summary>
        public ShapeTemplateParams()
        {
            _pyramid = -1;
            _angleStart = -0.39;
            _angleExtent = 0.79;
            _angleStep = "auto";
            _scaleMin = 0.9;
            _scaleMax = 1.1;
            _scaleStep = "auto";
            _optimization = "auto";
            _metric = "ignore_global_polarity";
            _contrast = "auto";
            _minContrast = "auto";
            _smoothImg = false;
            _filterSize = 5;
        }

        /// <summary>
        /// shape模板创建参数
        /// </summary>
        /// <param name="pyramid">金字塔层数,默认-1,List of values: -1,0,1,2,3,4,5</param>
        /// <param name="angleStart">起始角度,默认-0.39</param>
        /// <param name="angleExtent">终止角度,默认0.79</param>
        /// <param name="angleStep">角度步进,默认"auto"</param>
        /// <param name="scaleMin">最小缩放比例,默认0.9</param>
        /// <param name="scaleMax">最大缩放比例,默认1.1</param>
        /// <param name="scaleStep">缩放步进,默认"auto"</param>
        /// <param name="optimization">模型优化方式,默认"auto",List of values: 'auto', 'no_pregeneration', 'none', 'point_reduction_high', 'point_reduction_low', 'point_reduction_medium', 'pregeneration'</param>
        /// <param name="metric">是否使用极性,默认“use_polarity” List of values: 'ignore_global_polarity', 'use_polarity'</param>
        /// <param name="contrast">模板图像中对象的对比度阈值或滞后阈值，以及可选的对象部件的最小尺寸,默认"auto"</param>
        /// <param name="minContrast">搜索图像中对象的最小对比度,默认"auto"</param>
        /// <param name="smoothImg">是否平滑图像,默认false</param>
        /// <param name="filterSize">平滑图像窗口大小,默认5</param>
        public ShapeTemplateParams(int pyramid = -1, double angleStart = -0.39, double angleExtent = 0.79, string angleStep = "auto",
            double scaleMin = 0.9, double scaleMax = 1.1, string scaleStep = "auto", string optimization = "auto", string metric = "use_polarity",
            string contrast = "auto", string minContrast = "auto", bool smoothImg = false, double filterSize = 5)
        {
            _pyramid = pyramid;
            _angleStart = angleStart;
            _angleExtent = angleExtent;
            _angleStep = angleStep;
            _scaleMin = scaleMin;
            _scaleMax = scaleMax;
            _scaleStep = scaleStep;
            _optimization = optimization;
            _metric = metric;
            _contrast = contrast;
            _minContrast = minContrast;
            _smoothImg = smoothImg;
            _filterSize = filterSize;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>shape模板参数</returns>
        public ShapeTemplateParams Clone()
        {
            ShapeTemplateParams obj = new ShapeTemplateParams(Pyramid, AngleStart, AngleExtent, AngleStep, ScaleMin, ScaleMax, ScaleStep, Optimization,
                Metric, Contrast, MinContrast, SmoothImg, FilterSize);
            return obj;
        }

        /// <summary>
        /// 保存参数
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

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            List<string> keyList = new List<string>();
            keyList.Add("Pyramid");
            keyList.Add("AngleStart");
            keyList.Add("AngleExtent");
            keyList.Add("AngleStep");
            keyList.Add("ScaleMin");
            keyList.Add("ScaleMax");
            keyList.Add("ScaleStep");
            keyList.Add("Optimization");
            keyList.Add("Metric");
            keyList.Add("Contrast");
            keyList.Add("MinContrast");
            keyList.Add("SmoothImg");
            keyList.Add("FilterSize");

            List<string> list = new List<string>();
            list.Add(Pyramid.ToString());
            list.Add(AngleStart.ToString());
            list.Add(AngleExtent.ToString());
            list.Add(AngleStep.ToString());
            list.Add(ScaleMin.ToString());
            list.Add(ScaleMax.ToString());
            list.Add(ScaleStep.ToString());
            list.Add(Optimization.ToString());
            list.Add(Metric.ToString());
            list.Add(Contrast.ToString());
            list.Add(MinContrast.ToString());
            list.Add(SmoothImg.ToString());
            list.Add(FilterSize.ToString());

            //1.判断文件夹是否存在，若不存在，则创建
            //2.将数据写入到文件中
            IniHelper.AddSectionWithKeyValues(section, keyList, list, fileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
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

            int index = Array.IndexOf(keys, "Pyramid");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            Pyramid = int.Parse(values[index]);

            index = Array.IndexOf(keys, "AngleStart");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            AngleStart = double.Parse(values[index]);

            index = Array.IndexOf(keys, "AngleExtent");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            AngleExtent = double.Parse(values[index]);

            index = Array.IndexOf(keys, "AngleStep");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            AngleStep = values[index];

            index = Array.IndexOf(keys, "ScaleMin");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            ScaleMin = double.Parse(values[index]);

            index = Array.IndexOf(keys, "ScaleMax");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            ScaleMax = double.Parse(values[index]);

            index = Array.IndexOf(keys, "ScaleStep");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            ScaleStep = values[index];

            index = Array.IndexOf(keys, "Optimization");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            Optimization = values[index];

            index = Array.IndexOf(keys, "Metric");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            Metric = values[index];

            index = Array.IndexOf(keys, "Contrast");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            Contrast = values[index];

            index = Array.IndexOf(keys, "MinContrast");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            MinContrast = values[index];

            index = Array.IndexOf(keys, "SmoothImg");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !bool.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            SmoothImg = bool.Parse(values[index]);

            index = Array.IndexOf(keys, "FilterSize");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPETEMPLATEPARAMS_VALUE_ERROR;
            }

            FilterSize = double.Parse(values[index]);

            return Errortype.OK;
        }
    }

    /// <summary>
    /// shape模板匹配参数
    /// </summary>
    public class ShapeMatchParams : DataStructBase
    {
        private double _angleStart; //起始角度
        private double _angleExtent; //终止角度
        private double _scaleMin; //起始缩放比
        private double _scaleMax; //终止缩放比
        private double _minScore; //最小得分
        private int _numMatches; //最大匹配数量
        private double _maxOverlap; //最大重合范围
        private string _subPixel; //亚像素  'none', 'interpolation', 'least_squares', 'least_squares_high', 'least_squares_very_high'
        private int _pyramid; //金字塔层数 包含‘auto’
        private double _greediness; // 搜索贪婪度  0 ≤ greediness ≤ 1（0: safe but slow; 1: fast but matches may be missed）

        //filter
        private bool _smoothImg; //是否平滑图像
        private double _filterSize; //平滑图像的窗口大小

        /// <summary>
        /// Gets or sets the user's _angleStart
        /// </summary>
        public double AngleStart { get => _angleStart; set => _angleStart = value; }

        /// <summary>
        /// Gets or sets the user's _angleExtent
        /// </summary>
        public double AngleExtent { get => _angleExtent; set => _angleExtent = value; }

        /// <summary>
        /// Gets or sets the user's _scaleMin
        /// </summary>
        public double ScaleMin { get => _scaleMin; set => _scaleMin = value; }

        /// <summary>
        /// Gets or sets the user's _scaleMax
        /// </summary>
        public double ScaleMax { get => _scaleMax; set => _scaleMax = value; }

        /// <summary>
        /// Gets or sets the user's _minScore
        /// </summary>
        public double MinScore { get => _minScore; set => _minScore = value; }

        /// <summary>
        /// Gets or sets the user's _numMatches
        /// </summary>
        public int NumMatches { get => _numMatches; set => _numMatches = value; }

        /// <summary>
        /// Gets or sets the user's _maxOverlap
        /// </summary>
        public double MaxOverlap { get => _maxOverlap; set => _maxOverlap = value; }

        /// <summary>
        /// Gets or sets the user's _subPixel
        /// </summary>
        public string SubPixel { get => _subPixel; set => _subPixel = value; }

        /// <summary>
        /// Gets or sets the user's _pyramid
        /// </summary>
        public int Pyramid { get => _pyramid; set => _pyramid = value; }

        /// <summary>
        /// Gets or sets the user's _greediness
        /// </summary>
        public double Greediness { get => _greediness; set => _greediness = value; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否平滑图像。
        /// </summary>
        public bool SmoothImg { get => _smoothImg; set => _smoothImg = value; }

        /// <summary>
        /// Gets or sets the user's _filterSize
        /// </summary>
        public double FilterSize { get => _filterSize; set => _filterSize = value; }

        /// <summary>
        /// 保存参数
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

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            List<string> keyList = new List<string>();
            keyList.Add("AngleStart");
            keyList.Add("AngleExtent");
            keyList.Add("ScaleMin");
            keyList.Add("ScaleMax");
            keyList.Add("MinScore");
            keyList.Add("NumMatches");
            keyList.Add("MaxOverlap");
            keyList.Add("SubPixel");
            keyList.Add("Pyramid");
            keyList.Add("Greediness");
            keyList.Add("SmoothImg");
            keyList.Add("FilterSize");

            List<string> list = new List<string>();
            list.Add(AngleStart.ToString());
            list.Add(AngleExtent.ToString());
            list.Add(ScaleMin.ToString());
            list.Add(ScaleMax.ToString());
            list.Add(MinScore.ToString());
            list.Add(NumMatches.ToString());
            list.Add(MaxOverlap.ToString());
            list.Add(SubPixel.ToString());
            list.Add(Pyramid.ToString());
            list.Add(Greediness.ToString());
            list.Add(SmoothImg.ToString());
            list.Add(FilterSize.ToString());

            //2.将数据写入到文件中
            IniHelper.AddSectionWithKeyValues(section, keyList, list, fileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
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

            int index = Array.IndexOf(keys, "AngleStart");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            AngleStart = double.Parse(values[index]);

            index = Array.IndexOf(keys, "AngleExtent");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            AngleExtent = double.Parse(values[index]);

            index = Array.IndexOf(keys, "ScaleMin");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            ScaleMin = double.Parse(values[index]);

            index = Array.IndexOf(keys, "ScaleMax");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            ScaleMax = double.Parse(values[index]);

            index = Array.IndexOf(keys, "MinScore");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            MinScore = double.Parse(values[index]);

            index = Array.IndexOf(keys, "NumMatches");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            NumMatches = int.Parse(values[index]);

            index = Array.IndexOf(keys, "MaxOverlap");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            MaxOverlap = double.Parse(values[index]);

            index = Array.IndexOf(keys, "SubPixel");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            SubPixel = values[index];

            index = Array.IndexOf(keys, "Pyramid");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !int.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            Pyramid = int.Parse(values[index]);

            index = Array.IndexOf(keys, "Greediness");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            Greediness = double.Parse(values[index]);

            index = Array.IndexOf(keys, "SmoothImg");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !bool.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            SmoothImg = bool.Parse(values[index]);

            index = Array.IndexOf(keys, "FilterSize");
            if (index < 0 || index >= values.Length)
            {
                return Errortype.READ_SHAPEMATCHPARAMS_INDEX_ERROR;
            }

            if (values[index] == string.Empty || !double.TryParse(values[index], out _))
            {
                return Errortype.READ_SHAPEMATCHPARAMS_VALUE_ERROR;
            }

            FilterSize = double.Parse(values[index]);

            return Errortype.OK;
        }

        /// <summary>
        /// shape匹配参数默认构造 
        /// </summary>
        public ShapeMatchParams()
        {
            AngleStart = -0.39;
            AngleExtent = 0.79;
            ScaleMin = 0.9;
            ScaleMax = 1.1;
            MinScore = 0.9;
            NumMatches = 1;
            MaxOverlap = 0.5;
            SubPixel = "least_squares";
            Pyramid = 0;
            SmoothImg = false;
            FilterSize = 5;
        }

        /// <summary>
        /// shape模板匹配参数
        /// </summary>
        /// <param name="angleStart">起始角度,默认-0.39</param>
        /// <param name="angleExtent">终止角度,默认0.79</param>
        /// <param name="scaleMin">最小缩放比例,默认0.9</param>
        /// <param name="scaleMax">最大缩放比例,默认1.1</param>
        /// <param name="minScore">最小得分,默认0.9</param>
        /// <param name="numMatches">目标匹配数量,默认0,代表识别图像中所有对象</param>
        /// <param name="maxOverlap">相邻目标最大重合度,默认0.5</param>
        /// <param name="subPixel">亚像素精度,默认"least_squares",List of values:'none', 'interpolation', 'least_squares', 'least_squares_high', 'least_squares_very_high'</param>
        /// <param name="pyramid">金字塔层数,默认0,List of values: 0,1,2,3,4,5</param>
        /// <param name="smoothImg">是否平滑图像,默认false</param>
        /// <param name="filterSize">平滑图像窗口大小,默认5</param>
        public ShapeMatchParams(double angleStart = -0.39, double angleExtent = 0.79, double scaleMin = 0.9, double scaleMax = 1.1, double minScore = 0.9,
            int numMatches = 0, double maxOverlap = 0.5, string subPixel = "least_squares", int pyramid = 0, bool smoothImg = false, double filterSize = 5)
        {
            AngleStart = angleStart;
            AngleExtent = angleExtent;
            ScaleMin = scaleMin;
            ScaleMax = scaleMax;
            MinScore = minScore;
            NumMatches = numMatches;
            MaxOverlap = maxOverlap;
            SubPixel = subPixel;
            Pyramid = pyramid;
            SmoothImg = smoothImg;
            FilterSize = filterSize;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>shape匹配参数</returns>
        public ShapeMatchParams Clone()
        {
            ShapeMatchParams obj = new ShapeMatchParams(AngleStart, AngleExtent, ScaleMin, ScaleMax, MinScore, NumMatches, MaxOverlap, SubPixel, Pyramid, SmoothImg, FilterSize);
            return obj;
        }
    }

    /// <summary>
    /// 模板类型
    /// </summary>
    public enum TemplateType
    {
        NCC,
        SHAPE,
        SHAPEXLD,
    }
}
