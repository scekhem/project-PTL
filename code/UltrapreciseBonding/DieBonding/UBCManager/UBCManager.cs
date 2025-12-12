using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataStruct;
using HalconDotNet;
using IniFileHelper;
using OpenCvSharp.Flann;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.DieBonding;
using UltrapreciseBonding.UBAManager;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.UBCManager
{
    /// <summary>
    /// 机构名称
    /// </summary>
    public class CalibParam
    {
        private string _topSensorName;
        private string _bottomSensorName;
        private string _stageName;

        /// <summary>
        /// Gets or sets同轴上相机名称
        /// </summary>
        public string TopSensorName
        {
            get => _topSensorName;
            set => _topSensorName = value;
        }

        /// <summary>
        /// Gets or sets同轴下相机名称
        /// </summary>
        public string BottomSensorName
        {
            get => _bottomSensorName;
            set => _bottomSensorName = value;
        }

        /// <summary>
        /// Gets or sets Stage轴名称
        /// </summary>
        public string StageName
        {
            get => _stageName;
            set => _stageName = value;
        }
    }

    /// <summary>
    /// mark识别参数
    /// </summary>
    public class MarkSetParam
    {
        /// <summary>
        /// Gets or sets the user's Mark背景类型    ("light" or "dark")
        /// </summary>
        public string LightDark { get; set; } = "light";

        /// <summary>
        /// Gets or sets the user's 阈值提取次数
        /// </summary>
        public int BinaryTimes { get; set; } = 2;

        /// <summary>
        /// Gets or sets the user's 最大mark面积
        /// </summary>
        public double MaxMarkArea { get; set; } = 3000;

        /// <summary>
        /// Gets or sets the user's 最小mark面积
        /// </summary>
        public double MinMarkArea { get; set; } = 800;

        /// <summary>
        /// Gets or sets the user's mark圆度阈值
        /// </summary>
        public double Circularity { get; set; } = 0.9;

        /// <summary>
        /// Gets or sets the user's 最小得分
        /// </summary>
        public double MinScore { get; set; } = 0.8;

        /// <summary>
        /// Gets or sets the user's 卡尺提取灰阶阈值
        /// </summary>
        public double MeasureThreshold { get; set; } = 25;

        /// <summary>
        /// Gets or sets the user's Mark是区域还是轮廓     （"region" or "xld"）
        /// </summary>
        public string RegionOrXLD { get; set; } = "region";
    }

    /// <summary>
    /// 轴参数
    /// </summary>
    public class AxisParam
    {
        /// <summary>
        /// Gets or sets the user's stage轴X方向
        /// </summary>
        public int StageAxisX { get; set; } = 1;

        /// <summary>
        /// Gets or sets the user's Stage轴Y方向
        /// </summary>
        public int StageAxisY { get; set; } = 1;

        /// <summary>
        /// Gets or sets the user's stage轴旋转方向
        /// </summary>
        public int StageTheta { get; set; } = 1;
    }

    /// <summary>
    /// 旋转角度标定类
    /// </summary>
    public class CalibRotateAngle : Singleton<CalibRotateAngle>
    {
        private static List<double> _anglesMotor = new List<double>();  // 电机旋转角度
        private static List<double> _anglesTrue = new List<double>();   // 旋转角度真值

        //private static bool _isCalibed = false;                      // 是否完成标定 默认false
        private static string _fileName;   // 标定文件保存名称

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="filePath">标定文件路径</param>
        /// <returns>OK：成功，其他失败</returns>
        public Errortype Init(string filePath)
        {
            _fileName = filePath + "_rotateAngle.ini";
            return Errortype.OK;
        }

        /// <summary>
        /// 计算电机移动角度-角度真值键值对
        /// </summary>
        /// <param name="baseLine"> 旋转前算法识别2Mark的连线</param>
        /// <param name="rotateLine">旋转后算法识别2Mark的连线</param>
        /// <param name="moveAngle">电机旋转角度</param>
        /// <returns>OK：成功，其他失败</returns>
        ///
        public Errortype CalcRotateAngle(LineSeg baseLine, LineSeg rotateLine, double moveAngle)
        {
            var ret = ComAlgo.CalcAngleLL(baseLine, rotateLine, out double angle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _anglesMotor.Add(moveAngle);
            _anglesTrue.Add(angle);
            return Errortype.OK;
        }

        /// <summary>
        /// 根据角度计算电机的旋转量
        /// </summary>
        /// <param name="currentMotorAngle">当前电机角度</param>
        /// <param name="rotateAngle">旋转角度</param>
        /// <param name="motorDist">电机旋转后的角度</param>
        /// <returns>OK：成功，其他失败</returns>
        public Errortype GetMotorDistByAngle(double currentMotorAngle, double rotateAngle, out double motorDist)
        {
            motorDist = 0;
            if (_anglesMotor.Count == 0 || _anglesTrue.Count == 0 || _anglesMotor.Count != _anglesTrue.Count)
            {
                return Errortype.UBC_ANGLEDATA_IS_ERROR;
            }

            var ret = GetValueByKey(_anglesMotor, _anglesTrue, currentMotorAngle, out double value);
            value += rotateAngle;
            ret = GetValueByKey(_anglesTrue, _anglesMotor, value, out motorDist);

            return ret;
        }

        /// <summary>
        /// 通过插值计算键值对应的值
        /// </summary>
        /// <param name="keyList">键值List</param>
        /// <param name="valueList">值List</param>
        /// <param name="key">需要查找的键值</param>
        /// <param name="value">插值后键值对应的值</param>
        /// <returns>OK：成功，其他失败</returns>
        private Errortype GetValueByKey(List<double> keyList, List<double> valueList, double key, out double value)
        {
            value = 0;

            //  找到距离key最近的左右点
            HOperatorSet.TupleSub(keyList.ToArray(), key, out HTuple subKeyList);
            HOperatorSet.TupleAbs(subKeyList, out HTuple absKeyList);
            HOperatorSet.TupleSortIndex(absKeyList, out HTuple indicesList);

            //  插值计算
            var ret = ComAlgo.LinearInterpolation(
                new KeyValuePair<double, double>(keyList[indicesList[0]], valueList[indicesList[0]]),
                new KeyValuePair<double, double>(keyList[indicesList[1]], valueList[indicesList[1]]), key, out value);

            subKeyList.UnPinTuple();
            absKeyList.UnPinTuple();
            indicesList.UnPinTuple();
            return ret;
        }

        /// <summary>
        /// 释放
        /// </summary>
        /// <returns>ok:释放成功</returns>
        public Errortype Release()
        {
            _anglesMotor = new List<double>();
            _anglesTrue = new List<double>();
            return Errortype.OK;
        }

        /// <summary>
        /// 保存标定文件
        /// </summary>
        /// <param name="savePath">保存路径</param>
        /// <returns>OK：成功，其他失败</returns>
        public Errortype Save(string savePath)
        {
            if (!System.IO.Directory.Exists(savePath))
            {
                return Errortype.UBC_CALIBROTATEANGLE_INFO_FILE_NOT_EXIST;
            }

            string fullFileName = Path.Combine(savePath, _fileName);
            if (File.Exists(fullFileName))
            {
                File.Delete(fullFileName);
            }

            List<string> keys = new List<string>();
            List<string> values = new List<string>();
            for (int i = 0; i < _anglesMotor.Count; ++i)
            {
                keys.Add(_anglesMotor[i].ToString());
                values.Add(_anglesTrue[i].ToString());
            }

            IniHelper.AddSectionWithKeyValues("Info", keys, values, fullFileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 加载标定文件
        /// </summary>
        /// <param name="loadPath">加载文件路径</param>
        /// <returns>OK：成功，其他失败</returns>
        public Errortype Load(string loadPath)
        {
            if (!Directory.Exists(loadPath))
            {
                return Errortype.PARAMS_FILE_LOAD_ERROR;
            }

            if (System.IO.Directory.GetFiles(loadPath, "*.ini").Length == 0)
            {
                return Errortype.UBC_CALIBROTATEANGLE_INFO_FILE_NOT_EXIST;
            }

            System.IO.DirectoryInfo dirInfo = new System.IO.DirectoryInfo(loadPath);
            var allFiles = dirInfo.GetFiles();
            foreach (var file in allFiles)
            {
                string name = file.Name;

                // 判断是否存在所需文件
                if (name.Contains(_fileName))
                {
                    IniHelper.GetAllKeyValues("Info", out string[] keys, out string[] values, file.FullName);
                    _anglesMotor.Clear();
                    _anglesTrue.Clear();
                    for (int i = 0; i < keys.Length; i++)
                    {
                        _anglesMotor.Add(double.Parse(keys[i]));
                        _anglesTrue.Add(double.Parse(values[i]));
                    }
                }
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 数据记录类
    /// </summary>
    internal class RecordData
    {
        // Teach Points
        private List<Point> _headICPatterns = new List<Point>();     // head_ic上pattern中心坐标
        private List<Point> _headICEyePoints = new List<Point>();    // head_ic上eyepoints坐标
        private List<Point> _subICPatterns = new List<Point>();      // sub_ic上pattern中心坐标
        private List<Point> _subICEyePoints = new List<Point>();     // sub_ic上eyepoints坐标

        // Working head、sub Pattern Location
        private List<Point> _headPatternList = new List<Point>();    // aligment headPattern
        private List<Point> _subPatternList = new List<Point>();     // aligment subPattern
        private Point _currentStagePos = new Point();
        private double _offsetX = 0;
        private double _offsetY = 0;
        private double _offsetTheta = 0;
        private Point _subTargetPoint = new Point();

        // 精度补偿
        private Point _precisionCompensate = new Point();
        private double _rotateCompensate = 0;

        // 对位精度
        private Point _aligmentPrecisionXY = new Point();
        private double _aligmentPrecisionT = 0;

        /// <summary>
        /// Gets or sets mark image
        /// </summary>
        public HObject MarkImage { get; set; } = new HObject();

        // path
        private string _initPath = "D:\\Alg\\recordLog.ini";
        private string _savePath = "D:\\Alg\\";

        #region  // 公共变量

        /// <summary>
        /// Gets or sets the user's offsetX
        /// </summary>
        public double OffsetX
        {
            get { return _offsetX; }
            set { _offsetX = value; }
        }

        /// <summary>
        /// Gets or sets the user's offsetY
        /// </summary>
        public double OffsetY
        {
            get { return _offsetY; }
            set { _offsetY = value; }
        }

        /// <summary>
        /// Gets or sets the user's offsetTheta
        /// </summary>
        public double OffsetTheta
        {
            get { return _offsetTheta; }
            set { _offsetTheta = value; }
        }

        /// <summary>
        /// Gets or sets the user's subTargetPoint
        /// </summary>
        public Point SubTargetPoint
        {
            get { return _subTargetPoint; }
            set { _subTargetPoint = value; }
        }

        /// <summary>
        /// Gets or sets the user's headICPatterns
        /// </summary>
        public List<Point> HeadICPatterns
        {
            get { return _headICPatterns; }
            set { _headICPatterns = value; }
        }

        /// <summary>
        /// Gets or sets the user's headICEyePoints
        /// </summary>
        public List<Point> HeadICEyePoints
        {
            get { return _headICEyePoints; }
            set { _headICEyePoints = value; }
        }

        /// <summary>
        /// Gets or sets the user's subICPatterns
        /// </summary>
        public List<Point> SubICPatterns
        {
            get { return _subICPatterns; }
            set { _subICPatterns = value; }
        }

        /// <summary>
        /// Gets or sets the user's subICEyePoints
        /// </summary>
        public List<Point> SubICEyePoints
        {
            get { return _subICEyePoints; }
            set { _subICEyePoints = value; }
        }

        /// <summary>
        /// Gets or sets the user's headPatternList
        /// </summary>
        public List<Point> HeadPatternList
        {
            get { return _headPatternList; }
            set { _headPatternList = value; }
        }

        /// <summary>
        /// Gets or sets the user's subPatternList
        /// </summary>
        public List<Point> SubPatternList
        {
            get { return _subPatternList; }
            set { _subPatternList = value; }
        }

        /// <summary>
        ///  Gets or sets the user's 
        /// </summary>
        public Point CurrentStagePos
        {
            get { return _currentStagePos; }
            set { _currentStagePos = value; }
        }

        /// <summary>
        ///  Gets or sets the user's 
        /// </summary>
        public Point PrecisionCompensate
        {
            get { return _precisionCompensate; }
            set { _precisionCompensate = value; }
        }

        /// <summary>
        ///  Gets or sets the user's 
        /// </summary>
        public double RotateCompensate
        {
            get { return _rotateCompensate; }
            set { _rotateCompensate = value; }
        }

        /// <summary>
        ///  Gets or sets the user's 
        /// </summary>
        public Point AligmentPrecisionXY
        {
            get { return _aligmentPrecisionXY; }
            set { _aligmentPrecisionXY = value; }
        }

        /// <summary>
        ///  Gets or sets the user's 
        /// </summary>
        public double AligmentPrecisionT
        {
            get { return _aligmentPrecisionT; }
            set { _aligmentPrecisionT = value; }
        }

        /// <summary>
        /// Gets or sets the user's 同轴度变化量
        /// </summary>
        public Point CoaxiaVariation { get; set; }

        #endregion

        /// <summary>
        /// 对位数据保存
        /// </summary>
        /// <returns>OK：成功，其他失败</returns>
        public Errortype SaveAligmentData()
        {
            if (!File.Exists(_initPath))
            {
                return Errortype.UBC_RECORDLOG_IS_NULL;
            }

            IniHelper.GetAllKeyValues("TeachData", out string[] _, out string[] teachData, _initPath);
            IniHelper.GetAllKeyValues("Aligment", out string[] keys, out string[] aligment, _initPath);
            string fullName = Path.Combine(_savePath, "AligmentData.txt");
            if (!File.Exists(fullName))
            {
                File.Create(fullName).Close();
                StreamWriter sw = new StreamWriter(fullName, true, Encoding.Default);
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                sw.WriteLine(time + " " +
                             "HeadPatternList1X" + " " +
                             "HeadPatternList1Y" + " " +
                             "HeadPatternList2X" + " " +
                             "HeadPatternList2Y" + " " +
                             "SubPatternList1X" + " " +
                             "SubPatternList1Y" + " " +
                             "SubPatternList2X" + " " +
                             "SubPatternList2Y" + " " +
                             "CurrentStagePosX" + " " +
                             "CurrentStagePosY" + " " +
                             "offsetX" + " " +
                             "offsetY" + " " +
                             "offsetAngle" + " " +
                             "compensateX" + " " +
                             "compensateY" + " " +
                             "compensateAngle" + " " +
                             "AligmentPrecisionX" + " " +
                             "AligmentPrecisionY" + " " +
                             "AligmentPrecisionT" + " " +
                             "CoaxiaVariationX" + " " +
                             "CoaxiaVariationY");
                sw.Close();
            }

            // 示教数据 只需保存一次
            if (teachData[0] == "true")
            {
                Save(fullName, "headICPattern", _headICPatterns);
                Save(fullName, "subICPattern", _subICPatterns);
                Save(fullName, "headICEyePoints", _headICEyePoints);
                Save(fullName, "subICEyePoints", _subICEyePoints);
            }

            //  对位数据 每次都需要保存
            if (aligment[0] == "true")
            {
                //Save(fullName, "headPatternList", _headPatternList);
                //Save(fullName, "subPatternList", _subPatternList);
                //Save(fullName, "offset", new List<Point>() { new Point(_offsetX, _offsetY), new Point(_offsetTheta, _offsetTheta) });
                ////Save(fullName, "subTargetPoint", new List<Point>() { _subTargetPoint });
                //Save(fullName, "compensate", new List<Point>() { _precisionCompensate, new Point(_rotateCompensate, _rotateCompensate) });
                Save(fullName);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存图片
        /// </summary>
        /// <returns>OK：成功，其他失败</returns>
        public Errortype SaveImage()
        {
            if (!File.Exists(_initPath))
            {
                return Errortype.UBC_RECORDLOG_IS_NULL;
            }

            IniHelper.GetAllKeyValues("MarkImage", out string[] _, out string[] isSave, _initPath);

            if (isSave[0] == "true")
            {
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                HOperatorSet.WriteImage(MarkImage, "bmp", 0, _savePath + time);
            }

            return Errortype.OK;
        }

        private void Save(string path, string dataName, List<Point> data)
        {
            //  以append的方式进行保存
            StreamWriter sw = new StreamWriter(path, true, Encoding.Default);
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            sw.WriteLine("******" + dataName + "******" + time);
            for (int i = 0; i < data.Count; ++i)
            {
                sw.Write(Math.Round(data[i].X, 4) + "\t" + Math.Round(data[i].Y, 4) + "\n");
            }

            sw.Close();
        }

        private void Save(string path)
        {
            //  以append的方式进行保存
            StreamWriter sw = new StreamWriter(path, true, Encoding.Default);
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            sw.WriteLine(time + " " +
                         Math.Round(_headPatternList[0].X, 6) + " " +
                         Math.Round(_headPatternList[0].Y, 6) + " " +
                         Math.Round(_headPatternList[1].X, 6) + " " +
                         Math.Round(_headPatternList[1].Y, 6) + " " +
                         Math.Round(_subPatternList[0].X, 6) + " " +
                         Math.Round(_subPatternList[0].Y, 6) + " " +
                         Math.Round(_subPatternList[1].X, 6) + " " +
                         Math.Round(_subPatternList[1].Y, 6) + " " +
                         Math.Round(_currentStagePos.X, 6) + " " +
                         Math.Round(_currentStagePos.Y, 6) + " " +
                         Math.Round(_offsetX, 6) + " " +
                         Math.Round(_offsetY, 6) + " " +
                         Math.Round(_offsetTheta, 6) + " " +
                         Math.Round(_precisionCompensate.X, 6) + " " +
                         Math.Round(_precisionCompensate.Y, 6) + " " +
                         Math.Round(_rotateCompensate, 6) + " " +
                         Math.Round(_aligmentPrecisionXY.X, 6) + " " +
                         Math.Round(_aligmentPrecisionXY.Y, 6) + " " +
                         Math.Round(_aligmentPrecisionT, 6) + " " +
                         Math.Round(CoaxiaVariation.X, 6) + " " +
                         Math.Round(CoaxiaVariation.Y, 6));
            sw.Close();
        }
    }

    /// <summary>
    /// UBCManager
    /// </summary>
    public static class UBCManager
    {
        // 数据
        private static RecordData _recordData = new RecordData();

        // 相机、轴机构名称
        private static CalibParam _calibParam = new CalibParam();
        private static AxisParam _axisParam = new AxisParam();
        private static string _algParamFile;

        // 误差补偿
        private static Point _coaxiaError = new Point(0.184397632, 0.012788459);  // 同轴偏差 上相机->下相机   下相机->上相机需要加负号 动Stage轴
        private static Point _errorXYCompensate = new Point(0.0074, -0.074);   // 误差补偿
        private static double _errorTCompensate = -0.0045;             // 角度误差

        // 对齐视野偏差 -- 修改 TODO
        private static Point _viewError = new Point(-0.18, -0.013);     //  产品   与_coaxiaError存在负号的差别  动相机

        /// <summary>
        /// Gets or sets 偏移误差补偿
        /// </summary>
        public static Point ErrorXYCompensate { get => _errorXYCompensate; set => _errorXYCompensate = value; }

        /// <summary>
        /// Gets or sets 角度误差补偿
        /// </summary>
        public static double ErrorTCompensate { get => _errorTCompensate; set => _errorTCompensate = value; }

        /// <summary>
        /// Gets or sets 上下相机识别视野偏差
        /// </summary>
        public static Point ViewError { get => _viewError; set => _viewError = value; }

        /// <summary>
        /// Gets or sets 上下相机同轴度误差
        /// </summary>
        public static Point CoaxiaError { get => _coaxiaError; set => _coaxiaError = value; }

        /// <summary>
        /// Gets or sets 同轴度变化量
        /// </summary>
        public static Point CoaxiaVariation { get; set; } = new Point(0, 0);

        #region 基本方法（初始化、保存、加载、查询、释放）

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="calibParam">标定目录(标定类型，标定名称)</param>
        ///  <param name="algParamFile">算法文件读取路径</param>
        /// <returns>OK：成功，其他失败</returns>
        public static Errortype Init(CalibParam calibParam, string algParamFile = null)
        {
            _calibParam = calibParam;
            _algParamFile = algParamFile;

            LoadAxisParam(algParamFile);
            return Errortype.OK;
        }
        #endregion

        #region  工艺流程

        /// <summary>
        /// 添加示教点位
        /// </summary>
        /// <param name="headPatterns">head左上、右下pattern中心光栅坐标</param>
        /// <param name="headEyePoints">head左上、右下eyepoints点光栅坐标</param>
        /// <param name="subPatterns">sub左上、右下pattern中心光栅坐标</param>
        /// <param name="subEyePoints">sub左上、右下eyepoints点光栅坐标</param>
        /// <returns>OK：成功，其他失败</returns>
        public static Errortype AddTeachPoints(List<Point> headPatterns, List<Point> headEyePoints, List<Point> subPatterns, List<Point> subEyePoints)
        {
            if (headPatterns.Count < 2 || headEyePoints.Count < 2 ||
                subPatterns.Count < 2 || subEyePoints.Count < 2)
            {
                return Errortype.UBC_INPUT_COUNT_IS_LESS_TWO;
            }

            // pattern 转真值
            List<Point> headICPatterns = new List<Point>();
            List<Point> subICPatterns = new List<Point>();
            for (int i = 0; i < headPatterns.Count; ++i)
            {
                var ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, headPatterns[i], out Point headPatternTrue);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, subPatterns[i], out Point subPatternTrue);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                headICPatterns.Add(headPatternTrue);
                subICPatterns.Add(subPatternTrue);
            }

            // eyepoints转真值
            List<Point> headICEyePoints = new List<Point>();
            List<Point> subICEyePoints = new List<Point>();
            for (int j = 0; j < headEyePoints.Count; ++j)
            {
                var ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, headEyePoints[j], out Point headEyeTrue);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, subEyePoints[j], out Point subEyeTrue);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                headICEyePoints.Add(headEyeTrue);
                subICEyePoints.Add(subEyeTrue);
            }

            _recordData.HeadICPatterns = new List<Point>(headICPatterns);
            _recordData.HeadICEyePoints = new List<Point>(headICEyePoints);
            _recordData.SubICPatterns = new List<Point>(subICPatterns);
            _recordData.SubICEyePoints = new List<Point>(subICEyePoints);

            return Errortype.OK;
        }

        /// <summary>
        /// 根据eyepoints计算X、Y、θ偏移量
        /// </summary>
        /// <param name="headLeftRightRuler">head左上、右下pattern光栅坐标</param>
        /// <param name="subLeftRightRuler">Sub左上、右下pattern光栅坐标</param>
        /// <param name="currentStageRuler">当前Stage轴光栅坐标</param>
        /// <param name="offsetXY">XY偏移量</param>
        /// <param name="offsetTheta">θ偏移量</param>
        /// <returns>OK：成功，其他失败</returns>
        public static Errortype GetXYThetaOffset(List<Point> headLeftRightRuler, List<Point> subLeftRightRuler, Point currentStageRuler,
            out Point offsetXY, out double offsetTheta)
        {
            offsetXY = new Point();
            offsetTheta = 0;
            var result = Errortype.OK;

            if (headLeftRightRuler.Count < 2 || subLeftRightRuler.Count < 2)
            {
                return Errortype.UBC_INPUT_COUNT_IS_LESS_TWO;
            }

            //  光栅转真值
            List<Point> headPatternList = new List<Point>();
            List<Point> subPatternList = new List<Point>();
            for (int i = 0; i < headLeftRightRuler.Count; ++i)
            {
                result = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, headLeftRightRuler[i], out Point headTrue);
                if (result != Errortype.OK)
                {
                    return result;
                }

                result = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, subLeftRightRuler[i], out Point subTrue);
                if (result != Errortype.OK)
                {
                    return result;
                }

                headPatternList.Add(headTrue);
                subPatternList.Add(subTrue);
            }

            _recordData.CurrentStagePos = currentStageRuler;
            _recordData.HeadPatternList = new List<Point>(headPatternList);
            _recordData.SubPatternList = new List<Point>(subPatternList);

            // 计算示教pattern位置和生产时pattern位置计算刚体变换关系
            result = ComAlgo.VectorToRigid(_recordData.HeadICPatterns, _recordData.HeadPatternList, out double[] matHead, out List<Point> errorHead);
            if (result != Errortype.OK)
            {
                return result;
            }

            result = ComAlgo.VectorToRigid(_recordData.SubICPatterns, _recordData.SubPatternList, out double[] matSub, out List<Point> errorSub);
            if (result != Errortype.OK)
            {
                return result;
            }

            // 转换eyePoints点
            List<Point> headEqList = new List<Point>();
            List<Point> subEqList = new List<Point>();
            result = ComAlgo.AffineTransPoint2d(_recordData.HeadICEyePoints, matHead, out headEqList);
            result = ComAlgo.AffineTransPoint2d(_recordData.SubICEyePoints, matSub, out subEqList);
            if (result != Errortype.OK)
            {
                return result;
            }

            result = CalibXYT.GetInstance().GetRotateCenter(_calibParam.BottomSensorName, true, currentStageRuler, out Point rotateCenter1, _axisParam.StageAxisX, _axisParam.StageAxisY);
            if (result != Errortype.OK)
            {
                return result;
            }

            // 区分左上角、右下角eyePoints
            //if (headEqList.Count % 2 != 0)
            //{
            //    return Errortype.UBC_INPUT_POINT_COUNT_ERROR;
            //}
            //int indexs = headEqList.Count / 2;
            //double t = 0;
            //Point xy = new Point();

            //for (int i = 0; i < indexs; ++i)
            //{
            //    result = ComAlgo.CalcLinesRT(headEqList[i], headEqList[i + indexs], subEqList[i], subEqList[i + indexs], rotateCenter1,
            //        out double rotateAngle, out Point translation);
            //    if (result != Errortype.OK)
            //    {
            //        return result;
            //    }

            //    t += -rotateAngle;
            //    xy += new Point(translation.X, translation.Y);
            //}

            //xy = new Point(xy.X * _axisParam.StageAxisX, xy.Y * _axisParam.StageAxisY);
            //offsetXY = xy / indexs + _errorXYCompensate - _coaxiaError;
            //t *= _axisParam.StageTheta;
            //offsetTheta = t / indexs + _errorTCompensate;
            var ret = ComAlgo.CalcPointsRT(subEqList, headEqList, rotateCenter1, out double tranT, out Point tranXY,
                out double errorT, out double errorXY);
            offsetXY += new Point(CoaxiaVariation.X * -1, CoaxiaVariation.Y);
            offsetTheta = -tranT * _axisParam.StageTheta + _errorTCompensate;
            offsetXY = new Point(tranXY.X * _axisParam.StageAxisX, tranXY.Y * _axisParam.StageAxisY) + _errorXYCompensate - _coaxiaError;

            // 保存数据
            _recordData.OffsetX = offsetXY.X;
            _recordData.OffsetY = offsetXY.Y;
            _recordData.OffsetTheta = offsetTheta;
            _recordData.PrecisionCompensate = _errorXYCompensate;
            _recordData.RotateCompensate = _errorTCompensate;
            _recordData.CoaxiaVariation = CoaxiaVariation;
            _recordData.SaveAligmentData();

            return Errortype.OK;
        }

        /// <summary>
        /// 根据Mark计算对位的旋转平移量
        /// </summary>
        /// <param name="headLeftRightP">head左上角、右下角Mark中心点</param>
        /// <param name="stageLeftRightP">Stage左上角、右下角Mark中心点</param>
        /// <param name="currentStagePos">当前轴光栅坐标</param>
        /// <param name="offsetXY">偏移量XY</param>
        /// <param name="rotateAngle">转角</param>
        /// <returns>OK:成功，其他:失败</returns>
        public static Errortype GetMarkXYTheta(List<Point> headLeftRightP, List<Point> stageLeftRightP, Point currentStagePos, out Point offsetXY, out double rotateAngle)
        {
            offsetXY = new Point();
            rotateAngle = 0;
            var ret = Errortype.OK;

            //nextStagePoint = new Point();
            if (headLeftRightP.Count < 2 || stageLeftRightP.Count < 2)
            {
                return Errortype.UBC_INPUT_COUNT_IS_LESS_TWO;
            }

            // 光栅转真值
            List<Point> headReal = new List<Point>();
            List<Point> stageReal = new List<Point>();
            for (int i = 0; i < headLeftRightP.Count; ++i)
            {
                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, headLeftRightP[i], out Point hReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, stageLeftRightP[i], out Point sReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                headReal.Add(hReal);
                stageReal.Add(sReal);
            }

            _recordData.CurrentStagePos = currentStagePos;
            _recordData.HeadPatternList = new List<Point>(headReal);
            _recordData.SubPatternList = new List<Point>(stageReal);

            ret = CalibXYT.GetInstance().GetRotateCenter(_calibParam.BottomSensorName, true, currentStagePos, out Point rotateCenter, _axisParam.StageAxisX, _axisParam.StageAxisY);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //ret = ComAlgo.CalcLinesRT(headReal[0], headReal[1], stageReal[0], stageReal[1], rotateCenter,
            //    out rotateAngle, out offsetXY);
            ret = ComAlgo.CalcPointsRT(stageReal, headReal, rotateCenter, out rotateAngle, out offsetXY,
                out double errorT, out double errorXY);

            if (ret != Errortype.OK)
            {
                return ret;
            }

            // 借用同轴标定接口，将下相机光栅转到上相机光栅，此处不是光栅值
            //offsetXY *= -1;
            offsetXY += new Point(CoaxiaVariation.X * -1, CoaxiaVariation.Y);
            offsetXY = new Point(offsetXY.X * _axisParam.StageAxisX, offsetXY.Y * _axisParam.StageAxisY);
            offsetXY += _errorXYCompensate - _coaxiaError;
            rotateAngle *= -1 * _axisParam.StageTheta;
            rotateAngle += _errorTCompensate;

            // 保存数据
            _recordData.OffsetX = offsetXY.X;
            _recordData.OffsetY = offsetXY.Y;
            _recordData.OffsetTheta = rotateAngle;
            _recordData.PrecisionCompensate = _errorXYCompensate;
            _recordData.RotateCompensate = _errorTCompensate;
            _recordData.CoaxiaVariation = CoaxiaVariation;

            //_recordData.SubTargetPoint = nextStagePoint;
            _recordData.SaveAligmentData();

            return ret;
        }

        /// <summary>
        /// 计算ICMark圆圆心像素坐标
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="markParam">mark识别参数</param>
        /// <param name="pix">圆心像素坐标</param>
        /// <param name="markScore">圆圆度分数</param>
        /// <returns>OK：成功，其他失败</returns>
        public static Errortype GetICMarkCenter(Camera image, MarkSetParam markParam, out Point pix, out double markScore)
        {
            pix = new Point();
            markScore = 0;
            var ret = Errortype.OK;
            if (image == null)
            {
                return Errortype.UBC_INPUT_IMAGE_NULL;
            }

            if (ComAlgo.SaveFlg("GetICMarkCenter", out int days))
            {
                string path = @"D:\Alg\GetICMarkCenterImg";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                ComAlgo.FileManage(path, 7, out path);

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                string name = path + "\\" + time + ".bmp";
                image.Save(name);
            }

            // 阈值提取圆Mark区域
            HOperatorSet.GenEmptyObj(out HObject img);
            img = image.GenHObject();
            _recordData.MarkImage = img;

            HTuple rows = new HTuple();
            HTuple cols = new HTuple();
            HTuple radius = new HTuple();
            HTuple circularity = new HTuple();
            if (markParam.RegionOrXLD == "xld")
            {
                //gxc算子
                GlassIcMarkParams markParams = new GlassIcMarkParams();
                markParams.IsFilter = "false";
                ret = DieBondComAlgo.CalcCircleRingMarkCenter(image, out List<Point> centerExtract, out List<double> markRadius, out List<bool> isCircle, markParams);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                pix = centerExtract[0];
                return ret;

                //HOperatorSet.EdgesSubPix(img, out HObject edges, "canny", 1, 10, 20);
                //HOperatorSet.SelectShapeXld(edges, out HObject circularXLD, "circularity", "and", markParam.Circularity, 1);
                //HOperatorSet.CountObj(circularXLD, out HTuple count);
                //if (count < 1)
                //{
                //    return Errortype.UBC_XLD_CIRCULARITY_SELECT_LESS_ONE;
                //}

                //HOperatorSet.SelectShapeXld(circularXLD, out HObject lengthXLD, "contlength", "and", markParam.MinMarkArea, markParam.MaxMarkArea);
                //HOperatorSet.SmallestCircleXld(lengthXLD, out HTuple rowsXLD, out HTuple colsXLD, out HTuple radiusXLD);
                //if (rowsXLD.Length < 1)
                //{
                //    return Errortype.UBC_XLD_AREA_SELECT_NOT_EQUAL_ONE;
                //}

                //HOperatorSet.CircularityXld(lengthXLD, out circularity);
                //rows = rowsXLD[0];
                //cols = colsXLD[0];
                //radius = radiusXLD[0];
                //edges.Dispose();
                //circularXLD.Dispose();
                //lengthXLD.Dispose();
            }
            else
            {
                int count = markParam.BinaryTimes;
                while (count > 0)
                {
                    ret = Errortype.OK;
                    count--;
                    HOperatorSet.GenEmptyObj(out HObject region);
                    region.Dispose();
                    HOperatorSet.BinaryThreshold(img, out region, "max_separability", markParam.LightDark, out HTuple usedTh);
                    HObject expTmpOut;
                    HOperatorSet.ReduceDomain(img, region, out expTmpOut);
                    img.Dispose();
                    img = expTmpOut;

                    // 删除包含目标区域,由于玻璃IC有分裂，故不能先connection,和也不能先fillup冲突
                    HOperatorSet.Connection(region, out HObject connectedRegions);
                    HOperatorSet.FillUp(connectedRegions, out HObject fillRegions);
                    HOperatorSet.AreaCenter(fillRegions, out HTuple area, out HTuple centerRow, out HTuple centerCol);
                    HOperatorSet.TupleGenConst(area.Length, markParam.MaxMarkArea, out HTuple areaHTuple);
                    HOperatorSet.TupleGreaterEqualElem(area, areaHTuple, out HTuple greatereq);
                    HOperatorSet.TupleFind(greatereq, 1, out HTuple indices);

                    if (indices != -1)
                    {
                        for (int i = 0; i < indices.Length; ++i)
                        {
                            HOperatorSet.SelectObj(connectedRegions, out HObject objectSelected, indices[i] + 1);
                            HOperatorSet.Difference(region, objectSelected, out HObject regionDifference);
                            region = regionDifference.Clone();
                            objectSelected.Dispose();
                            regionDifference.Dispose();
                        }
                    }

                    HOperatorSet.FillUp(region, out HObject regionFillup);
                    HOperatorSet.OpeningCircle(regionFillup, out HObject regionOpening, 3.5);
                    HOperatorSet.Connection(regionOpening, out HObject regionConnection);
                    HOperatorSet.SelectShape(regionConnection, out HObject selectedRegions, "circularity", "and", markParam.Circularity, 1);
                    HOperatorSet.CountObj(selectedRegions, out HTuple number);
                    if (number < 1)
                    {
                        ret = Errortype.UBC_CIRCULARITY_SELECT_LESS_ONE;
                    }

                    HOperatorSet.SelectShape(selectedRegions, out HObject selectedRegionArea, "area", "and", markParam.MinMarkArea, markParam.MaxMarkArea);
                    HOperatorSet.SmallestCircle(selectedRegionArea, out HTuple rowsRegion, out HTuple colsRegion, out HTuple radiusRegion);
                    if (rowsRegion.Length != 1)
                    {
                        ret = Errortype.UBC_AREA_SELECT_NOT_EQUAL_ONE;
                    }

                    HOperatorSet.Circularity(selectedRegionArea, out circularity);

                    fillRegions.Dispose();
                    rows = rowsRegion;
                    cols = colsRegion;
                    radius = radiusRegion;
                    regionFillup.Dispose();
                    region.Dispose();
                    regionConnection.Dispose();
                    selectedRegions.Dispose();
                    selectedRegionArea.Dispose();
                    regionOpening.Dispose();
                    connectedRegions.Dispose();

                    if (ret == Errortype.OK && rowsRegion.Length == 1)
                    {
                        break;
                    }
                }
            }

            if (ret != Errortype.OK)
            {
                return ret;
            }

            markScore = circularity.D;
            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = markParam.MinScore;
            calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
            calipParam.MeasureLength1 = 10;
            calipParam.MeasureLength2 = 4;
            calipParam.NumMeasures = 120;
            calipParam.MeasureThreshold = markParam.MeasureThreshold;
            calipParam.MeasureSelect = "first";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "uniform";

            ret = CaliperCircle.CircleExtraction(image, new Point(cols, rows), radius, calipParam, out pix, out double raiudOut);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _recordData.SaveImage();

            return Errortype.OK;
        }

        /// <summary>
        /// 上料时，根据左上、右上、右下计算旋转角度和IC中心位置
        /// </summary>
        /// <param name="cornerPoints">左上、右上、右下角点光栅位置</param>
        /// <param name="currentStagePos">当前Stage轴位置</param>
        /// <param name="sensorInitPos">同轴相机工作状态初始位置</param>
        /// <param name="offsetPos">偏移量</param>
        /// <param name="angle">旋转角度</param>
        /// <returns>OK：成功，其他失败</returns>
        public static Errortype GetFeedingPos(List<Point> cornerPoints, Point currentStagePos, Point sensorInitPos, out Point offsetPos, out double angle)
        {
            offsetPos = new Point();
            angle = 0;
            var ret = Errortype.OK;

            if (cornerPoints.Count != 3)
            {
                return Errortype.UBC_INPUT_COUNT_NOT_EQUAAL_THREE;
            }

            // 光栅转真值
            List<Point> cornerPointsReal = new List<Point>();
            for (int i = 0; i < cornerPoints.Count; ++i)
            {
                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, cornerPoints[i], out Point real);
                cornerPointsReal.Add(real);
            }

            // 计算与x轴夹角
            HOperatorSet.AngleLx(cornerPointsReal[0].Y, cornerPointsReal[0].X, cornerPointsReal[1].Y, cornerPointsReal[1].X, out HTuple rad);
            HOperatorSet.TupleDeg(rad, out HTuple deg);
            ret = CalibXYT.GetInstance().GetRotateCenter(_calibParam.BottomSensorName, true, currentStagePos, out Point rotateCenter, _axisParam.StageAxisX, _axisParam.StageAxisY);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            // 计算绕基板中心旋转角度后的真值坐标
            HOperatorSet.HomMat2dIdentity(out HTuple homMat2d);
            HOperatorSet.HomMat2dRotate(homMat2d, rad, rotateCenter.X, rotateCenter.Y, out HTuple homMat2DRotate);
            ret = ComAlgo.AffineTransPoint2d(cornerPointsReal, homMat2DRotate.ToDArr(), out List<Point> cornerPointsRotate);
            ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, sensorInitPos, out Point sensorInitPosReal);

            angle = deg * _axisParam.StageTheta;
            offsetPos = (cornerPointsRotate[0] + cornerPointsRotate[2]) / 2 - sensorInitPosReal;
            offsetPos = new Point(-offsetPos.X * _axisParam.StageAxisX, -offsetPos.Y * _axisParam.StageAxisY);
            return Errortype.OK;
        }
        #endregion

        /// <summary>
        /// 玻璃IC键合打样精度计算
        /// </summary>
        /// <param name="img">键合后拍摄的左上、右下Mark两幅图像</param>
        /// <param name="headRuler">Head相机在左上、右下采图时对应的轴坐标</param>
        /// <param name="currentStagePos">当前stage轴坐标</param>
        /// <param name="markParam">mark识别参数</param>
        /// <param name="angleError">键合角度误差</param>
        /// <param name="translateError">键合精度</param>
        /// <returns>ok:成功，其他：失败</returns>
        public static Errortype CalcGlassBondAccuracy(List<Camera> img, List<Point> headRuler, Point currentStagePos, MarkSetParam markParam,
            out double angleError, out Point translateError)
        {
            angleError = 0;
            translateError = new Point();
            if (img.Count != 2 || headRuler.Count != 2)
            {
                return Errortype.UBC_INPUT_COUNT_NOT_EQUAAL_TWO;
            }

            List<Point> icMarkRulers = new List<Point>();
            List<Point> subMarkRulers = new List<Point>();
            List<Point> icMarkReals = new List<Point>();
            List<Point> subMarkReals = new List<Point>();

            Errortype ret = Errortype.OK;
            CaliperParams calipParam = new CaliperParams();

            //calipParam.Load(_algParamFile + "\\CaliperParamsBondCheck.ini", "CaliperParams");
            calipParam.NumInstances = 1;
            calipParam.MinScore = markParam.MinScore;
            calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
            calipParam.MeasureLength1 = 10;
            calipParam.MeasureLength2 = 4;
            calipParam.NumMeasures = 120;
            calipParam.MeasureThreshold = markParam.MeasureThreshold;
            calipParam.MeasureSelect = "last";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "uniform";

            if (ret != Errortype.OK)
            {
                return ret;
            }

            for (int i = 0; i < img.Count; ++i)
            {
                ret = ComAlgo.CalcGlassIcBondPix(img[i], calipParam, out Point innerPix, out Point outerPix,
                     out Point errorXYLeft);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                // pix -> ruler
                ret = CalibPixScaleXY.GetInstance().GetRulerByPix(_calibParam.BottomSensorName, innerPix, headRuler[i], out Point innerRuler);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibPixScaleXY.GetInstance().GetRulerByPix(_calibParam.BottomSensorName, outerPix, headRuler[i], out Point outRuler);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                // ruler -> real
                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, innerRuler, out Point innerReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.BottomSensorName, outRuler, out Point outReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                icMarkRulers.Add(innerRuler);
                subMarkRulers.Add(outRuler);
                icMarkReals.Add(innerReal);
                subMarkReals.Add(outReal);
            }

            //ret = CalibXYT.GetInstance().GetRotateCenter(_calibParam.BottomSensorName, true, currentStagePos, out Point rotateCenter1, _axisParam.StageAxisX, _axisParam.StageAxisY);
            //if (ret != Errortype.OK)
            //{
            //    return ret;
            //}
            Point rotateCenter = (icMarkReals[0] + icMarkReals[1]) / 2;

            ret = ComAlgo.CalcLinesRT(icMarkReals[0], icMarkReals[1], subMarkReals[0], subMarkReals[1],
                rotateCenter, out angleError, out translateError);

            angleError *= _axisParam.StageTheta * -1;
            translateError = new Point(translateError.X * _axisParam.StageAxisX, translateError.Y * _axisParam.StageAxisY);
            _recordData.AligmentPrecisionXY = translateError;
            _recordData.AligmentPrecisionT = angleError;

            if (ComAlgo.SaveFlg("CalcGlassBondAccuracy", out int days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                ComAlgo.SaveData(
                    path + "\\CalcGlassBondAccuracy.txt",
                    headRuler[0].ToString(sep), headRuler[1].ToString(sep),
                    icMarkRulers[0].ToString(sep), icMarkRulers[1].ToString(sep),
                    subMarkRulers[0].ToString(sep), subMarkRulers[1].ToString(sep),
                    translateError.ToString(sep), angleError.ToString("f6"));
            }

            if (ComAlgo.SaveFlg("CalcGlassBondAccuracyImg", out days))
            {
                string path = @"D:\Alg\CalcGlassBondAccuracyImg";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                ComAlgo.FileManage(path, 7, out path);

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                for (int i = 0; i < img.Count; ++i)
                {
                    string name = path + "\\" + time + "_" + translateError.X.ToString("f6") + "_" + translateError.Y.ToString("f6") + "_" + i.ToString() + ".bmp";
                    img[i].Save(name);
                }
            }

            return ret;
        }

        /// <summary>
        /// 计算产品die键合打样精度计算
        /// </summary>
        /// <param name="img">键合后拍摄的左上、右下Mark两幅图像</param>
        /// <param name="region">区域</param>
        /// <param name="cameraAxis">相机轴坐标</param>
        /// <param name="currentStagePos">Stage轴坐标</param>
        /// <param name="markParam">mark识别参数</param>
        /// <param name="offsetXY">键合偏移误差</param>
        /// <param name="offsetT">键合角度误差</param>
        /// <returns>ok:成功，其他：失败</returns>
        public static Errortype CalcDieAccuracy(List<Camera> img, List<Region> region, List<Point> cameraAxis, Point currentStagePos, MarkSetParam markParam, out Point offsetXY, out double offsetT)
        {
            offsetXY = new Point();
            offsetT = 0;

            if (img.Count < 2 || region.Count < 2 || cameraAxis.Count < 2)
            {
                return Errortype.UBC_INPUT_COUNT_IS_LESS_TWO;
            }

            List<Point> innerPoints = new List<Point>();
            List<Point> outerPoints = new List<Point>();
            Point rotateCenter = new Point();
            var ret = Errortype.OK;

            if (ComAlgo.SaveFlg("CalcDieAccuracyImg", out int days))
            {
                string path = @"D:\Alg\CalcDieAccuracyImg";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                ComAlgo.FileManage(path, 7, out path);

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                for (int i = 0; i < img.Count; ++i)
                {
                    string name = path + "\\" + time + "_" + i.ToString() + ".bmp";
                    img[i].Save(name);
                }
            }

            int minArea = (int)markParam.MinMarkArea;
            int maxArea = (int)markParam.MaxMarkArea;

            for (int i = 0; i < img.Count; ++i)
            {
                HObject image = img[i].GenHObject();
                HOperatorSet.GenEmptyObj(out HObject regionObject);
                HOperatorSet.GenRectangle1(out HObject rectangle, 0, 0, img[i].Width, img[i].Height);
                regionObject = region[i].Rectangle1 == null ? rectangle : region[i].GenRegion();

                HOperatorSet.ReduceDomain(image, regionObject, out HObject imageReduced);

                //筛选出圆
                HOperatorSet.EdgesSubPix(imageReduced, out HObject edge, "canny", 1, 20, 40);
                HOperatorSet.SegmentContoursXld(edge, out HObject contoursSplit, "lines_circles", 5, 4, 2);
                HOperatorSet.SelectShapeXld(contoursSplit, out HObject selectedXld, "contlength", "and", 50, 99999);

                List<double> rows = new List<double>();
                List<double> cols = new List<double>();
                List<double> radius = new List<double>();

                HOperatorSet.CountObj(selectedXld, out HTuple numbers);
                for (int j = 1; j <= numbers; ++j)
                {
                    HOperatorSet.SelectObj(selectedXld, out HObject objectSelected, j);
                    HOperatorSet.FitCircleContourXld(objectSelected, "algebraic", -1, 0, 0, 3, 2, out HTuple r,
                        out HTuple c, out HTuple radiu, out HTuple startPhi, out HTuple endPhi, out HTuple pointOrder);

                    //HOperatorSet.DistancePc(objectSelected, r, c, out HTuple distanceMin, out HTuple distanceMax);
                    double area = radiu * radiu * Math.PI;
                    if (area > minArea && area < maxArea)
                    {
                        rows.Add(r);
                        cols.Add(c);
                        radius.Add(radiu);
                    }
                }

                if (rows.Count < 2)
                {
                    return Errortype.UBC_AREA_SELECT_NOT_REASONABLE;
                }

                HOperatorSet.TupleSortIndex(radius.ToArray(), out HTuple indices);
                double[] indexDoubles = new double[] { indices[0], indices[indices.Length - 1] };
                HOperatorSet.TupleSelect(radius.ToArray(), indexDoubles.ToArray(), out HTuple radiusSort);
                HOperatorSet.TupleSelect(rows.ToArray(), indexDoubles.ToArray(), out HTuple rowsSort);
                HOperatorSet.TupleSelect(cols.ToArray(), indexDoubles.ToArray(), out HTuple colsSort);

                List<string> transition = new List<string>() { "positive", "negative" };
                List<Point> centerList = new List<Point>();
                for (int j = 0; j < radiusSort.Length; ++j)
                {
                    // 卡尺提取
                    CaliperParams calipParam = new CaliperParams();
                    calipParam.NumInstances = 1;
                    calipParam.MinScore = markParam.MinScore;
                    calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
                    calipParam.MeasureLength1 = 5;
                    calipParam.MeasureLength2 = 3;
                    calipParam.NumMeasures = 120;
                    calipParam.MeasureSelect = "all";
                    calipParam.MeasureThreshold = markParam.MeasureThreshold;
                    calipParam.MeasureInterpolation = "nearest_neighbor";
                    calipParam.MeasureTransition = transition[j];
                    ret = CaliperCircle.CircleExtraction(img[i], new Point(colsSort[j].D, rowsSort[j].D), radiusSort[j].D, calipParam, out Point centerOut, out double rad);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    ret = CalibPixScaleXY.GetInstance().GetRulerByPix(_calibParam.BottomSensorName, centerOut, cameraAxis[i], out Point targetRuler);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    centerList.Add(targetRuler);
                }

                innerPoints.Add(centerList[0]);
                outerPoints.Add(centerList[1]);
                rotateCenter += centerList[1];
            }

            rotateCenter = rotateCenter / outerPoints.Count;

            // 产品键合时圆环在head，实心圆在sub，颠倒
            //ret = ComAlgo.CalcLinesRT(outerPoints[0], outerPoints[1], innerPoints[0], innerPoints[1],
            //    rotateCenter, out offsetT, out offsetXY);
            ret = ComAlgo.CalcPointsRT(innerPoints, outerPoints, rotateCenter, out offsetT, out offsetXY,
                out double errorT, out double errorXY);

            // 计算绕基板中心旋转角度后的真值坐标 计算误差
            HOperatorSet.TupleRad(offsetT, out HTuple offsetRad);
            HOperatorSet.HomMat2dIdentity(out HTuple homMat2d);
            HOperatorSet.HomMat2dRotate(homMat2d, -offsetRad, rotateCenter.X, rotateCenter.Y, out HTuple homMat2DRotate);
            ret = ComAlgo.AffineTransPoint2d(innerPoints, homMat2DRotate.ToDArr(), out List<Point> cornerPointsRotate);
            List<Point> error = new List<Point>();
            for (int k = 0; k < outerPoints.Count; ++k)
            {
                Point p = cornerPointsRotate[k] + offsetXY - outerPoints[k];
                error.Add(p);
            }

            offsetXY = new Point(offsetXY.X * -1, offsetXY.Y * -1);

            if (ComAlgo.SaveFlg("CalcDieAccuracy", out days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                ComAlgo.SaveData(
                    path + "\\CalcDieAccuracy.txt",
                    cameraAxis[0].ToString(sep), cameraAxis[1].ToString(sep),
                    currentStagePos.ToString(sep),
                    offsetXY.ToString(sep), offsetT.ToString("f6"));
            }

            return ret;
        }

        /// <summary>
        /// 计算产品die键合打样精度计算
        /// </summary>
        /// <param name="img">键合后拍摄的左上、右下Mark两幅图像</param>
        /// <param name="region">区域</param>
        /// <param name="cameraAxis">相机轴坐标</param>
        /// <param name="currentStagePos">Stage轴坐标</param>
        /// <param name="offsetXY">键合偏移误差</param>
        /// <param name="offsetT">键合角度误差</param>
        /// <returns>ok:成功，其他：失败</returns>
        public static Errortype CalcDieAccuracy(List<Camera> img, List<Region> region, List<Point> cameraAxis, Point currentStagePos, out Point offsetXY, out double offsetT)
        {
            offsetXY = new Point();
            offsetT = 0;

            if (img.Count < 2 || region.Count < 2 || cameraAxis.Count < 2)
            {
                return Errortype.UBC_INPUT_COUNT_IS_LESS_TWO;
            }

            List<Point> innerPoints = new List<Point>();
            List<Point> outerPoints = new List<Point>();
            Point rotateCenter = new Point();
            var ret = Errortype.OK;

            if (ComAlgo.SaveFlg("CalcDieAccuracyImg", out int days))
            {
                string path = @"D:\Alg\CalcDieAccuracyImg";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                ComAlgo.FileManage(path, 7, out path);

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                for (int i = 0; i < img.Count; ++i)
                {
                    string name = path + "\\" + time + "_" + i.ToString() + ".bmp";
                    img[i].Save(name);
                }
            }

            int minArea = 9000;
            int maxArea = 50000;

            for (int i = 0; i < img.Count; ++i)
            {
                HObject image = img[i].GenHObject();
                HOperatorSet.GenEmptyObj(out HObject regionObject);
                HOperatorSet.GenRectangle1(out HObject rectangle, 0, 0, img[i].Width, img[i].Height);
                regionObject = region[i].Rectangle1 == null ? rectangle : region[i].GenRegion();

                HOperatorSet.ReduceDomain(image, regionObject, out HObject imageReduced);

                //筛选出圆
                HOperatorSet.EdgesSubPix(imageReduced, out HObject edge, "canny", 1, 10, 20);
                HOperatorSet.SegmentContoursXld(edge, out HObject contoursSplit, "lines_circles", 5, 4, 2);
                HOperatorSet.SelectShapeXld(contoursSplit, out HObject selectedXld, "contlength", "and", 50, 99999);

                List<double> rows = new List<double>();
                List<double> cols = new List<double>();
                List<double> radius = new List<double>();

                HOperatorSet.CountObj(selectedXld, out HTuple numbers);
                for (int j = 1; j <= numbers; ++j)
                {
                    HOperatorSet.SelectObj(selectedXld, out HObject objectSelected, j);
                    HOperatorSet.FitCircleContourXld(objectSelected, "algebraic", -1, 0, 0, 3, 2, out HTuple r,
                        out HTuple c, out HTuple radiu, out HTuple startPhi, out HTuple endPhi, out HTuple pointOrder);

                    //HOperatorSet.DistancePc(objectSelected, r, c, out HTuple distanceMin, out HTuple distanceMax);
                    double area = radiu * radiu * Math.PI;
                    if (area > minArea && area < maxArea)
                    {
                        rows.Add(r);
                        cols.Add(c);
                        radius.Add(radiu);
                    }
                }

                if (rows.Count < 2)
                {
                    return Errortype.UBC_AREA_SELECT_NOT_REASONABLE;
                }

                HOperatorSet.TupleSortIndex(radius.ToArray(), out HTuple indices);
                double[] indexDoubles = new double[] { indices[0], indices[indices.Length - 1] };
                HOperatorSet.TupleSelect(radius.ToArray(), indexDoubles.ToArray(), out HTuple radiusSort);
                HOperatorSet.TupleSelect(rows.ToArray(), indexDoubles.ToArray(), out HTuple rowsSort);
                HOperatorSet.TupleSelect(cols.ToArray(), indexDoubles.ToArray(), out HTuple colsSort);

                List<string> transition = new List<string>() { "negative", "positive" };
                List<Point> centerList = new List<Point>();
                for (int j = 0; j < radiusSort.Length; ++j)
                {
                    // 卡尺提取
                    CaliperParams calipParam = new CaliperParams();
                    calipParam.NumInstances = 1;
                    calipParam.MinScore = 0.7;
                    calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
                    calipParam.MeasureLength1 = 10;
                    calipParam.MeasureLength2 = 3;
                    calipParam.NumMeasures = 120;
                    calipParam.MeasureSelect = "all";
                    calipParam.MeasureThreshold = 10;
                    calipParam.MeasureInterpolation = "nearest_neighbor";
                    calipParam.MeasureTransition = transition[j];
                    ret = CaliperCircle.CircleExtraction(img[i], new Point(colsSort[j].D, rowsSort[j].D), radiusSort[j].D, calipParam, out Point centerOut, out double rad);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    ret = CalibPixScaleXY.GetInstance().GetRulerByPix(_calibParam.BottomSensorName, centerOut, cameraAxis[i], out Point targetRuler);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    centerList.Add(targetRuler);
                }

                innerPoints.Add(centerList[0]);
                outerPoints.Add(centerList[1]);
                rotateCenter += centerList[1];
            }

            rotateCenter = rotateCenter / outerPoints.Count;

            // 产品键合时圆环在head，实心圆在sub，颠倒
            //ret = ComAlgo.CalcLinesRT(outerPoints[0], outerPoints[1], innerPoints[0], innerPoints[1],
            //    rotateCenter, out offsetT, out offsetXY);
            ret = ComAlgo.CalcPointsRT(innerPoints, outerPoints, rotateCenter, out offsetT, out offsetXY,
                out double errorT, out double errorXY);

            // 计算绕基板中心旋转角度后的真值坐标 计算误差
            HOperatorSet.TupleRad(offsetT, out HTuple offsetRad);
            HOperatorSet.HomMat2dIdentity(out HTuple homMat2d);
            HOperatorSet.HomMat2dRotate(homMat2d, -offsetRad, rotateCenter.X, rotateCenter.Y, out HTuple homMat2DRotate);
            ret = ComAlgo.AffineTransPoint2d(innerPoints, homMat2DRotate.ToDArr(), out List<Point> cornerPointsRotate);
            List<Point> error = new List<Point>();
            for (int k = 0; k < outerPoints.Count; ++k)
            {
                Point p = cornerPointsRotate[k] + offsetXY - outerPoints[k];
                error.Add(p);
            }

            offsetXY = new Point(offsetXY.X * -1, offsetXY.Y * -1);

            if (ComAlgo.SaveFlg("CalcDieAccuracy", out days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                ComAlgo.SaveData(
                    path + "\\CalcDieAccuracy.txt",
                    cameraAxis[0].ToString(sep), cameraAxis[1].ToString(sep),
                    currentStagePos.ToString(sep),
                    offsetXY.ToString(sep), offsetT.ToString("f6"));
            }

            return ret;
        }

        /// <summary>
        /// 计算debond同轴度及垂直度误差
        /// </summary>
        /// <param name="img">相机拍摄图片(下相机、上相机)</param>
        /// <param name="regions">识别区域(下相机、上相机)</param>
        /// <param name="currentRuler">取图时相机当前光栅(下相机、上相机)，预防同轴度较差</param>
        /// <param name="coaxiaError">同轴度及垂直度误差</param>
        /// <returns>ok:成功，其他：失败</returns>
        public static Errortype CalibCoaxiaError(List<Camera> img, List<Region> regions, List<Point> currentRuler, out Point coaxiaError)
        {
            coaxiaError = new Point();
            Errortype ret = Errortype.OK;
            if (img.Count != regions.Count || img.Count != 2)
            {
                return Errortype.UBC_INPUT_IMAGE_NULL;
            }

            List<List<Point>> points = new List<List<Point>>();
            for (int i = 0; i < img.Count; ++i)
            {
                ret = ComAlgo.GetMarkCenterPix(img[i], out List<Point> p, regions[i]);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                points.Add(p);
            }

            Point offset = new Point();
            for (int i = 0; i < points[0].Count; ++i)
            {
                ret = CalibPixScaleXY.GetInstance()
                    .GetRulerByPix(_calibParam.BottomSensorName, points[0][i], currentRuler[0], out Point bottomP);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibPixScaleXY.GetInstance()
                    .GetRulerByPix(_calibParam.TopSensorName, points[1][i], currentRuler[1], out Point topP);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                offset += bottomP - topP;
            }

            coaxiaError = offset / points[0].Count;
            coaxiaError = new Point(coaxiaError.X * _axisParam.StageAxisX, coaxiaError.Y * _axisParam.StageAxisY);
            return ret;
        }

        /// <summary>
        /// 加载机台轴参数
        /// </summary>
        /// <param name="path">加载路径</param>
        /// <returns>OK：成功；其他：失败</returns>
        public static Errortype LoadAxisParam(string path)
        {
            path = path + "\\algorithmParam.ini";
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                StringBuilder sb = new StringBuilder();
                int stageAxisX = 1;
                int stageAxisY = 1;
                int stageAxisT = 1;

                sb.AppendLine("[StageAxis]");
                sb.AppendLine("StageAxisX=" + stageAxisX.ToString());
                sb.AppendLine("StageAxisY=" + stageAxisY.ToString());
                sb.AppendLine("StageAxisT=" + stageAxisT.ToString());
                File.WriteAllText(path, sb.ToString());
            }

            string[] keys = null;
            string[] values = null;

            IniHelper.GetAllKeyValues("StageAxis", out keys, out values, path);
            _axisParam.StageAxisX = int.Parse(values[0]);
            _axisParam.StageAxisY = int.Parse(values[1]);
            _axisParam.StageTheta = int.Parse(values[2]);

            return Errortype.OK;
        }
    }
}
