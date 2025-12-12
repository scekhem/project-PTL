using System;
using System.Collections.Generic;
using IniFileHelper;
using DataStruct;
using HalconDotNet;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using System.IO;

namespace UltrapreciseBonding.Calib
{
    /// <summary>
    /// CalibXYTBase
    /// </summary>
    internal class CalibXYTBase : CalibItem
    {
        private Point _rotateCenter = new Point();
        private double _thetaBase;
        private Point _xyRulerBase;
        private CalibCoord _calibXY;
        private string _calibXYName;

        /// <summary>
        /// Gets or sets the  _rotateCenter
        /// </summary>
        public Point RotateCenter
        {
            get { return _rotateCenter; }
            set { _rotateCenter = value; }
        }

        /// <summary>
        /// Gets or sets the _xyRulerBase
        /// </summary>
        public Point XYRulerBase
        {
            get { return _xyRulerBase; }
            set { _xyRulerBase = value; }
        }

        /// <summary>
        /// Gets or sets the _thetaBase
        /// </summary>
        public double ThetaBase
        {
            get { return _thetaBase; }
            set { _thetaBase = value; }
        }

        /// <summary>
        /// Gets or sets the _calibXY
        /// </summary>
        public CalibCoord CalibXY
        {
            get { return _calibXY; }
            set { _calibXY = value; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">标定项名称</param>
        public CalibXYTBase(string name)
        {
            ItemName = name;
            IsCalibed = false;
        }

        /// <summary>
        /// 初始化旋转中心标定参数
        /// </summary>
        /// <param name="xyRulerBase">X\Y轴初始位姿</param>
        /// <param name="thetaBase">旋转轴初始位姿</param>
        /// <param name="calibXY">XY轴运动系标定状态</param>
        /// <param name="defaultCenter">默认的旋转中心坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype InitCalibRotateCenter(Point xyRulerBase, double thetaBase, CalibCoord calibXY, Point defaultCenter)
        {
            if (defaultCenter is null)
            {
                return Errortype.XYT_CALCCENTER_POINTS_NULL;
            }

            if (calibXY is null)
            {
                return Errortype.CalibXYT_INITCALIBROTATECENTER_CALIBCOORD_NULL;
            }

            RotateCenter = defaultCenter;
            XYRulerBase = xyRulerBase;
            ThetaBase = thetaBase;
            CalibXY = calibXY;

            IsCalibed = true;
            _calibXYName = calibXY.ItemName;

            return Errortype.OK;
        }

        /// <summary>
        /// 标定旋转中心
        /// </summary>
        /// <param name="realPoints">用于拟合圆的点集（真值坐标）</param>
        /// <param name="xyRulerBase">标定旋转中心时XY轴初始位置</param>
        /// <param name="thetaBase">标定XY运动系时，Theta轴初始角度</param>
        /// <param name="calibXY">用于判断XY运动系标定</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibRotateCenter(List<Point> realPoints, Point xyRulerBase, double thetaBase, CalibCoord calibXY)
        {
            if (realPoints is null)
            {
                return Errortype.XYT_CALCCENTER_POINTS_NULL;
            }

            if (realPoints.Count < 3)
            {
                return Errortype.XYT_CALCCENTER_POINTS_NOT_ENOUGH;
            }

            if (calibXY is null)
            {
                return Errortype.XYT_XYCALIB_NULL;
            }

            if (!calibXY.IsCalibed)
            {
                return Errortype.XYT_CALIBXY_ISCALIB_FALSE;
            }

            ComAlgo.FitCircle(realPoints, out Point center, out double radius, out List<double> error);
            RotateCenter = center;

            XYRulerBase = xyRulerBase;
            ThetaBase = thetaBase;
            CalibXY = calibXY;

            IsCalibed = true;
            _calibXYName = calibXY.ItemName;

            return Errortype.OK;
        }

        /// <summary>
        /// 标定旋转中心
        /// </summary>
        /// <param name="realPoints">旋转前的点集（真值坐标）</param>
        /// <param name="rotetedRealPoints">旋转后的点集（顺序与旋转前对应）</param>
        /// <param name="xyRulerBase">标定旋转中心时XY轴初始位置</param>
        /// <param name="thetaBase">标定XY运动系时，Theta轴初始角度</param>
        /// <param name="calibXY">用于判断XY运动系标定</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibRotateCenter(List<Point> realPoints, List<Point> rotetedRealPoints, Point xyRulerBase, double thetaBase, CalibCoord calibXY)
        {
            if (realPoints is null)
            {
                return Errortype.XYT_CALCCENTER_POINTS_NULL;
            }

            if ((realPoints.Count < 3) || (rotetedRealPoints.Count != realPoints.Count))
            {
                return Errortype.XYT_CALCCENTER_POINTS_NOT_ENOUGH;
            }

            if (calibXY is null)
            {
                return Errortype.XYT_XYCALIB_NULL;
            }

            if (!calibXY.IsCalibed)
            {
                return Errortype.XYT_CALIBXY_ISCALIB_FALSE;
            }

            ComAlgo.CalcRotateCenter(realPoints, rotetedRealPoints, out Point center, out List<double> error);
            RotateCenter = center;

            XYRulerBase = xyRulerBase;
            ThetaBase = thetaBase;
            CalibXY = calibXY;

            IsCalibed = true;
            _calibXYName = calibXY.ItemName;

            return Errortype.OK;
        }

        /// <summary>
        /// 获取机构当前旋转中心（标定板坐标）
        /// </summary>
        /// <param name="changeCenter">设定旋转中心是否变化（true为变化，false为不变）</param>
        /// <param name="currentRuler">机构当前轴坐标</param>
        /// <param name="currentRotateCenter">机构当前旋转中心真值坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRotateCenter(bool changeCenter, Point currentRuler, Point axis, out Point currentRotateCenter)
        {
            currentRotateCenter = new Point();
            if (!IsCalibed)
            {
                return Errortype.XYT_ISCALIB_FALSE;
            }

            Errortype ret = _calibXY.Src2Dst(XYRulerBase, out Point xyRealBase, out List<Point> error1);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = _calibXY.Src2Dst(currentRuler, out Point currentReal, out List<Point> error2);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            Point offsetReal = currentReal - xyRealBase;
            offsetReal = new Point(offsetReal.X * axis.X, offsetReal.Y * axis.Y);

            if (changeCenter == true)
            {
                currentRotateCenter = RotateCenter + offsetReal;
            }
            else
            {
                currentRotateCenter = RotateCenter;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 光栅转真值
        /// </summary>
        /// <param name="ruler">待转换的光栅坐标</param>
        /// <param name="theta">当前Theta轴角度</param>
        /// <param name="real">转换后的真值坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRealByRuler(Point ruler, double theta, out Point real)
        {
            real = new Point();

            if (!IsCalibed)
            {
                return Errortype.XYT_ISCALIB_FALSE;
            }

            CalibXY.Src2Dst(XYRulerBase, out Point realBase, out List<Point> error);
            CalibXY.Src2Dst(ruler, out Point realPoint, out error);
            Point offset = realPoint - realBase;
            double angle = theta - ThetaBase;
            ComAlgo.CalcRotatePoint(realPoint, angle, RotateCenter, out Point pointRotated);
            real = pointRotated + offset;

            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Load(string fileDir)
        {
            Errortype ret = Errortype.OK;

            if (fileDir is null)
            {
                return Errortype.CALIBXYT_LOAD_FILEPATH_NULL;
            }

            if (fileDir.Length < 1)
            {
                return Errortype.CALIBXYT_lOAD_FILEPATH_EMPTY;
            }

            if (!Directory.Exists(fileDir))
            {
                return Errortype.XYT_LOAD_DIR_NOT_EXIST;
            }

            string filename = fileDir + "\\" + ItemName + "_CalibXYT.ini";
            if (!File.Exists(filename))
            {
                return Errortype.XYT_LOAD_FILE_NOT_EXIST;
            }

            string[] keys = null;
            string[] values = null;
            IniHelper.GetAllKeyValues("Info", out keys, out values, filename);

            if (keys.Length != 7 || values.Length != 7)
            {
                return Errortype.XYT_LOAD_KEY_LENGTH_ERROR;
            }

            RotateCenter = new Point();

            ItemName = values[0];
            RotateCenter.X = Convert.ToDouble(values[1]);
            RotateCenter.Y = Convert.ToDouble(values[2]);
            XYRulerBase = new Point();
            XYRulerBase.X = Convert.ToDouble(values[3]);
            XYRulerBase.Y = Convert.ToDouble(values[4]);
            ThetaBase = Convert.ToDouble(values[5]);
            _calibXYName = values[6];

            _calibXY = new CalibCoord(_calibXYName);

            ret = _calibXY.Load(fileDir);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            IsCalibed = true;

            return ret;
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Save(string fileDir)
        {
            Errortype ret = Errortype.OK;

            if (!IsCalibed)
            {
                return Errortype.CALIBXYT_ISNOT_COMPLET_ERROR;
            }

            if (fileDir is null)
            {
                return Errortype.CALIBXYT_SAVE_FILEPATH_NULL;
            }

            if (fileDir.Length < 1)
            {
                return Errortype.CALIBXYT_SAVE_FILEPATH_EMPTY;
            }

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            if (!Directory.Exists(fileDir))
            {
                return Errortype.CALIBXYT_SAVE_FILE_DIR_NOT_EXIST_ERROR;
            }

            ret = _calibXY.Save(fileDir);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            string filename = fileDir + "\\" + ItemName + "_CalibXYT.ini";
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            List<string> keys = new List<string> { "item_name" };
            List<string> values = new List<string> { ItemName };
            IniHelper.AddSectionWithKeyValues("Info", keys, values, filename);

            keys.Clear();
            values.Clear();
            keys.Add("RotateCenterX");
            values.Add(RotateCenter.X.ToString());
            keys.Add("RotateCenterY");
            values.Add(RotateCenter.Y.ToString());
            keys.Add("XYRulerBaseX");
            values.Add(XYRulerBase.X.ToString());
            keys.Add("XYRulerBaseY");
            values.Add(XYRulerBase.Y.ToString());
            keys.Add("ThetaBase");
            values.Add(ThetaBase.ToString());
            keys.Add("CalibXYName");
            values.Add(_calibXYName);

            IniHelper.AddSectionWithKeyValues("info", keys, values, filename);

            return ret;
        }
    }

    /// <summary>
    /// CalibXYT
    /// </summary>
    public class CalibXYT : Singleton<CalibXYT>
    {
        private List<CalibXYTBase> _xytCalibList = new List<CalibXYTBase>();
        private string _calibTypeName = "XYT_";

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Init(List<string> itemName)
        {
            if (itemName is null)
            {
                return Errortype.CALIBXYT_INIT_ITEMNAME_NULL;
            }

            foreach (var name in itemName)
            {
                string fullName = _calibTypeName + name;
                CalibXYTBase xYTCalibbase = _xytCalibList.Find(e => e.ItemName == fullName);
                if (xYTCalibbase != null)
                {
                    xYTCalibbase = new CalibXYTBase(fullName);
                }
                else
                {
                    _xytCalibList.Add(new CalibXYTBase(fullName));
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 初始化旋转中心标定参数
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="xyRulerBase">XY轴初始位姿</param>
        /// <param name="thetaBase">旋转轴初始位姿</param>
        /// <param name="calibXY">XY轴运动系标定状态</param>
        /// <param name="defaultCenter">默认的旋转中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype InitCalibCenter(string itemName, Point xyRulerBase, double thetaBase, CalibCoord calibXY, Point defaultCenter)
        {
            CalibXYTBase xYTCalibbase = _xytCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (xYTCalibbase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return xYTCalibbase.InitCalibRotateCenter(xyRulerBase, thetaBase, calibXY, defaultCenter);
        }

        /// <summary>
        /// 标定旋转中心
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="realPoints">用于拟合圆的坐标点集（真值坐标）</param>
        /// <param name="xyRulerBase">标定旋转中心时XY轴的初始位置</param>
        /// <param name="thetaBase">标定XY运动系时Theta轴初始角度</param>
        /// <param name="calibXY">用于判断是否完成XY运动系标定</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibCenter(string itemName, List<Point> realPoints, Point xyRulerBase, double thetaBase, CalibCoord calibXY)
        {
            CalibXYTBase xYTCalibbase = _xytCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (xYTCalibbase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return xYTCalibbase.CalibRotateCenter(realPoints, xyRulerBase, thetaBase, calibXY);
        }

        /// <summary>
        /// 标定旋转中心
        /// </summary>
        /// <param name="realPoints">旋转前的点集（真值坐标）</param>
        /// <param name="rotetedRealPoints">旋转后的点集（顺序与旋转前对应）</param>
        /// <param name="xyRulerBase">标定旋转中心时XY轴初始位置</param>
        /// <param name="thetaBase">标定XY运动系时，Theta轴初始角度</param>
        /// <param name="calibXY">用于判断XY运动系标定</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibCenter(string itemName, List<Point> realPoints, List<Point> rotetedRealPoints, Point xyRulerBase, double thetaBase, CalibCoord calibXY)
        {
            CalibXYTBase xYTCalibbase = _xytCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (xYTCalibbase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return xYTCalibbase.CalibRotateCenter(realPoints, rotetedRealPoints, xyRulerBase, thetaBase, calibXY);
        }

        /// <summary>
        /// 获取当前旋转中心
        /// </summary>
        /// <param name="itemName">旋转机构名称</param>
        /// <param name="changeCenter">设定旋转中心是否变化（true为变化，false为不变）</param>
        /// <param name="currentRuler">机构当前轴坐标</param>
        /// <param name="currentRotateCenter">机构当前旋转中心真值坐标</param>
        /// <param name="axisX">补偿X方向正反</param>
        /// <param name="axisY">补偿Y方向正反</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRotateCenter(string itemName, bool changeCenter, Point currentRuler, out Point currentRotateCenter, int axisX = 1, int axisY = 1)
        {
            CalibXYTBase xytCalibbase = _xytCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (xytCalibbase == null)
            {
                currentRotateCenter = new Point(0, 0);
                return Errortype.OPT_NAME_NULL;
            }

            return xytCalibbase.GetRotateCenter(changeCenter, currentRuler, new Point(axisX, axisY), out currentRotateCenter);
        }

        /// <summary>
        /// 验证旋转中心标定（确保旋转顺时针为正）
        /// </summary>
        /// <param name="stageName">标定机构名称（与旋转中心标定时保持一致）</param>
        /// <param name="changeCenter">设定旋转中心是否变化（true为变化，false为不变）</param>
        /// <param name="currentRuler">机构当前轴坐标</param>
        /// <param name="markReal">选取的mark真值坐标</param>
        /// <param name="angle">旋转角度</param>
        /// <param name="rotatedMarkReal">旋转后的mark真值坐标（通过获取mark像素坐标转换为真值坐标）</param>
        /// <param name="rotateCenterError">旋转中心标定误差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype VerifyRotateCenter(string stageName, bool changeCenter, Point currentRuler, Point markReal, double angle, Point rotatedMarkReal, out Point rotateCenterError)
        {
            rotateCenterError = new Point();
            if (stageName == null)
            {
                return Errortype.INPUT_NULL;
            }

            if (markReal == null || currentRuler == null || rotatedMarkReal == null)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            Errortype ret = GetRotateCenter(stageName, changeCenter, currentRuler, out Point rotateCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.CalcRotatePoint(markReal, -angle, rotateCenter, out Point calcMarkReal);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            rotateCenterError = calcMarkReal - rotatedMarkReal;

            return Errortype.OK;
        }

        /// <summary>
        /// 光栅转真值
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="ruler">待转换的光栅坐标</param>
        /// <param name="theta">当前Theta角度</param>
        /// <param name="real">转换后的真值坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalcRealByRuler(string itemName, Point ruler, double theta, out Point real)
        {
            CalibXYTBase xYTCalibbase = _xytCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (xYTCalibbase == null)
            {
                real = new Point(0, 0);
                return Errortype.OPT_NAME_NULL;
            }

            return xYTCalibbase.GetRealByRuler(ruler, theta, out real);
        }

        /// <summary>
        /// 查询标定状态
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="calibStatus">标定状态</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCalibStatus(string itemName, out bool calibStatus)
        {
            calibStatus = false;
            CalibXYTBase xYTCalibbase = _xytCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (xYTCalibbase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            calibStatus = xYTCalibbase.IsCalibed;
            return Errortype.OK;
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <param name="fileDir">文件路径</param>
        /// <param name="saveReturn">文件保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string fileDir, out Dictionary<string, Errortype> saveReturn)
        {
            saveReturn = new Dictionary<string, Errortype>();
            for (int index = 0; index < _xytCalibList.Count; index++)
            {
                Errortype ret = _xytCalibList[index].Save(fileDir);
                saveReturn.Add(_xytCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="fileDir">文件路径</param>
        /// <param name="loadReturn">文件加载结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Load(string fileDir, out Dictionary<string, Errortype> loadReturn)
        {
            loadReturn = new Dictionary<string, Errortype>();
            for (int index = 0; index < _xytCalibList.Count; index++)
            {
                Errortype ret = _xytCalibList[index].Load(fileDir);
                loadReturn.Add(_xytCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _xytCalibList.Clear();
            _xytCalibList = new List<CalibXYTBase>();
            return Errortype.OK;
        }
    }
}