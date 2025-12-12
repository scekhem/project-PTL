using System.Collections.Generic;
using System.IO;
using System;
using DataStruct;
using IniFileHelper;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using UltrapreciseBonding.Calib;
using HalconDotNet;
using System.Linq;
using System.Drawing.Design;
using System.Data;
using System.ComponentModel.Design;
using OpenCvSharp.Features2D;

namespace UltrapreciseBonding.FusionCollections
{
    #region 标定管理 旧版 弃用
    /*
    /// <summary>
    /// 熔融键合管理类
    /// </summary>
    public static class FusionManager
    {
        private static CalibCoaxiality _sensorConcentricCali = new CalibCoaxiality();   // 同轴同心标定
        private static CalibPix _sensorExtrinsicCali = new CalibPix();     // 相机外参(各轴图像转角&像素比)
        private static CalibXY _sensorMotionCali = new CalibXY();     // 光栅标定板坐标系全局标定
        private static MacroStageCalib _stageMacroCali = new MacroStageCalib();     // 宏动平台(转角控制)
        private static NanoStageCalib _stageNanoCali = new NanoStageCalib();          // 微动平台
        private static ChuckStageCalib _stageChuckCali = new ChuckStageCalib();          // Chuck标定

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="calibDictionary">标定类型</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Init(Dictionary<CalibType, List<string>> calibDictionary)
        {
            foreach (CalibType inputType in calibDictionary.Keys)
            {
                if (calibDictionary[inputType] is null)
                {
                }
                else
                {
                    switch (inputType)
                    {
                        case CalibType.OPTIC_CONCENTERIC:
                            _sensorConcentricCali.Init(calibDictionary[CalibType.OPTIC_CONCENTERIC]);
                            break;
                        case CalibType.OPTIC_EXTERNAL:
                            _sensorExtrinsicCali.Init(calibDictionary[CalibType.OPTIC_EXTERNAL]);
                            break;
                        case CalibType.OPTIC_UNION:
                            _sensorMotionCali.Init(calibDictionary[CalibType.OPTIC_UNION]);
                            break;
                        case CalibType.STAGE_MACRO:
                            _stageMacroCali.Init(calibDictionary[CalibType.STAGE_MACRO]);
                            break;
                        case CalibType.STAGE_NANO:
                            _stageNanoCali.Init(calibDictionary[CalibType.STAGE_NANO]);
                            break;
                        case CalibType.STAGE_CHUCK:
                            _stageChuckCali.Init(calibDictionary[CalibType.STAGE_CHUCK]);
                            break;
                    }
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 内存释放
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            _sensorConcentricCali.Release();
            _sensorExtrinsicCali.Release();
            _sensorMotionCali.Release();
            _stageMacroCali.Release();
            _stageNanoCali.Release();
            _stageChuckCali.Release();
            return Errortype.OK;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="dataDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SaveData(string dataDir)
        {
            // calibration settings
            string calibRecipyName = "CalibData";
            List<string> calibSettingKey = new List<string> { "current_calib_dir_name" };
            List<string> calibSettingValue = new List<string> { calibRecipyName };
            IniHelper.AddSectionWithKeyValues("CalibSettings", calibSettingKey, calibSettingValue, dataDir + "/SetUp.ini");
            if (!Directory.Exists(dataDir + "/" + calibRecipyName + "/"))
            {
                Directory.CreateDirectory(dataDir + "/" + calibRecipyName + "/");
            }

            _sensorConcentricCali.Save(dataDir + calibRecipyName + "/", out Dictionary<string, Errortype> saveConcentricResult);
            _sensorExtrinsicCali.Save(dataDir + calibRecipyName + "/", out Dictionary<string, Errortype> saveExtrinsicResult);
            _sensorMotionCali.Save(dataDir + calibRecipyName + "/", out Dictionary<string, Errortype> saveMotionResult);
            _stageMacroCali.Save(dataDir + calibRecipyName + "/");
            _stageNanoCali.Save(dataDir + calibRecipyName + "/");
            _stageChuckCali.Save(dataDir + calibRecipyName + "/", out Dictionary<string, Errortype> saveChuckResult);

            return Errortype.OK;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="dataDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype LoadData(string dataDir)
        {
            string calibRecipyName = "CalibData";
            if (!Directory.Exists(dataDir))
            {
                return Errortype.PARAMS_FILE_LOAD_ERROR;
            }

            if (!File.Exists(dataDir + "SetUp.ini"))
            {
                return Errortype.PARAMS_FILE_LOAD_ERROR;
            }
            else
            {
                List<string> calibSettingKey = new List<string> { "current_calib_recipy_name" };
                List<string> calibSettingValue = new List<string> { calibRecipyName };
                IniHelper.AddSectionWithKeyValues("CalibSettings", calibSettingKey, calibSettingValue, dataDir + "/SetUp.ini");
            }

            _sensorConcentricCali.Load(dataDir + calibRecipyName + "/", out Dictionary<string, Errortype> loadConcentricResult);
            _sensorExtrinsicCali.Load(dataDir + calibRecipyName + "/", out Dictionary<string, Errortype> loadExtrinsicResult);
            _sensorMotionCali.Load(dataDir + calibRecipyName + "/", out Dictionary<string, Errortype> loadMotionResult);
            _stageMacroCali.Load(dataDir + calibRecipyName + "/");
            _stageNanoCali.Load(dataDir + calibRecipyName + "/");
            _stageChuckCali.Load(dataDir + calibRecipyName + "/", out Dictionary<string, Errortype> loadChuckResult);

            return Errortype.OK;
        }

        /// <summary>
        /// 查询标定完成状态
        /// </summary>
        /// <param name="calibType">标定类型</param>
        /// <param name="itemName">标定名称</param>
        /// <param name="calibComplete">标定是否完成</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetCalibStatus(CalibType calibType, string itemName, out bool calibComplete)
        {
            calibComplete = false;
            switch (calibType)
            {
                case CalibType.OPTIC_CONCENTERIC:
                    _sensorConcentricCali.GetCalibStatus(itemName, out calibComplete);
                    break;
                case CalibType.OPTIC_EXTERNAL:
                    _sensorExtrinsicCali.GetCalibStatus(itemName, out calibComplete);
                    break;
                case CalibType.OPTIC_UNION:
                    _sensorMotionCali.GetCalibStatus(itemName, out calibComplete);
                    break;
                case CalibType.STAGE_MACRO:
                    _stageMacroCali.GetCalibStatus(itemName, out calibComplete);
                    break;
                case CalibType.STAGE_NANO:
                    _stageNanoCali.GetCalibStatus(itemName, out calibComplete);
                    break;
                case CalibType.STAGE_CHUCK:
                    _stageChuckCali.GetCalibStatus(itemName, out calibComplete);
                    break;
            }

            return Errortype.OK;
        }

        #region 镜组标定

        /// <summary>
        /// 上下镜头同心度标定
        /// </summary>
        /// <param name="opticName">标定名称</param>
        /// <param name="topMarkCenter">同轴上相机Mark中心</param>
        /// <param name="bottomMarkCenter">同轴下相机Mark中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibConcentric(string opticName, List<Point> topMarkCenter, List<Point> bottomMarkCenter) // Optic，标定后用于 将上下镜头视野坐标系统一
        {
            return _sensorConcentricCali.Calib(opticName, topMarkCenter, bottomMarkCenter);
        }

        /// <summary>
        /// 设定相机中心
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="opticCenterPix">外参中心像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibExtrinsicsCenter(string opticName, Point opticCenterPix)
        {
            return _sensorExtrinsicCali.SetPixCenter(opticName, opticCenterPix);
        }

        /// <summary>
        /// 各水平轴向和相机夹角标定，注意相机图像坐标原点在左上方，得到夹角结果可能为负值
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="pixMarkCenterX1">Mark1中心像素坐标</param>
        /// <param name="pixMarkCenterX2">Mark2中心像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibExtrinsicsAngle(string opticName, Point pixMarkCenterX1, Point pixMarkCenterX2) // pix-Optic，PECpix-MacroStage，pix-MacroStage，标定后用于控制各轴 getMarkCenter 逼近Mark中心
        {
            //res = scale_x , angle (pix <-> ruler / pix <-> macro_XX / pix <-> nano_xx)
            return _sensorExtrinsicCali.CalibPixAngle(opticName, pixMarkCenterX1, pixMarkCenterX2, out double pixAngle);
        }

        /// <summary>
        /// 获取相机和水平轴向夹角，注意相机图像坐标原点在左上方，得到夹角结果可能为负值
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="angleOut">夹角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetExtrinsicsAngle(string opticName, out double angleOut)
        {
            return _sensorExtrinsicCali.GetPixAngle(opticName, out angleOut);
        }

        /// <summary>
        /// 各X轴单位和相机横向像素比标定
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="pixMarkCenterX1">Mark1中心X像素坐标</param>
        /// <param name="pixMarkCenterX2">Mark2中心X像素坐标</param>
        /// <param name="ruler1">轴位置1</param>
        /// <param name="ruler2">轴位置2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibExtrinsicsScaleX(string opticName, Point pixMarkCenterX1, Point pixMarkCenterX2, Point ruler1, Point ruler2) // pix-Optic，PECpix-MacroStage，pix-MacroStage，标定后用于控制各轴 getMarkCenter 逼近Mark中心
        {
            //res = scale_x , angle (pix <-> ruler / pix <-> macro_XX / pix <-> nano_xx)
            return _sensorExtrinsicCali.CalibPixScaleX(opticName, pixMarkCenterX1, pixMarkCenterX2, ruler1, ruler2, out double pixScale);
        }

        /// <summary>
        /// 获得X方向像素比
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="scaleOut">x方向像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetExtrinsicsScaleX(string opticName, out double scaleOut)
        {
            return _sensorExtrinsicCali.GetPixScaleX(opticName, out scaleOut);
        }

        /// <summary>
        /// 各Y轴单位和相机纵向像素比标定
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="pixMarkCenterY1">Mark1中心Y像素坐标</param>
        /// <param name="pixMarkCenterY2">Mark2中心Y像素坐标</param>
        /// <param name="ruler1">轴位置1</param>
        /// <param name="ruler2">轴位置2</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibExtrinsicsScaleY(string opticName, Point pixMarkCenterY1, Point pixMarkCenterY2, Point ruler1, Point ruler2) // pix-Optic，PECpix-MacroStage，pix-MacroStage，标定后用于控制各轴 getMarkCenter 逼近Mark中心
        {
            return _sensorExtrinsicCali.CalibPixScaleY(opticName, pixMarkCenterY1, pixMarkCenterY2, ruler1, ruler2, out double pixScale);
        }

        /// <summary>
        /// 获得Y方向像素比
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="scaleOut">Y方向像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetExtrinsicsScaleY(string opticName, out double scaleOut)
        {
            return _sensorExtrinsicCali.GetPixScaleY(opticName, out scaleOut);
        }
        #endregion

        #region 光栅真值坐标标定

        /// <summary>
        /// optic光栅标定Mark图像单幅识别
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="currentRulerPose">逼近当前mark后optic光栅坐标</param>
        /// <param name="currentRealPose">当前mark的标定板坐标，通过行列间距计算得出</param>
        /// 2023.05.15 升级成meshmap处理方式
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AddOpticRulerRealPoint(string opticName, Point currentRulerPose, Point currentRealPose)
        {
            return _sensorMotionCali.AddPoint(opticName, currentRulerPose, currentRealPose);
        }

        /// <summary>
        /// 光栅真值坐标标定
        /// </summary>
        /// <param name="opticName">绑定optic名称</param>
        /// <param name="rowNum">已抓取的行数(含异常点)</param>
        /// <param name="colNum">已抓取的列数(含异常点)</param>
        /// <param name="rowInterval">行间距</param>
        /// <param name="colInterval">列间距</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibOpticRulerReal(string opticName, int rowNum, int colNum, double rowInterval, double colInterval) // affine_ruler_real，标定后用于绑定光栅轴和标定板坐标系映射关系，后续用于宏动、微动平台平移运动、旋转中心标定（旋转中心校正）
        {
            return _sensorMotionCali.CalibRulerToReal(opticName); //默认变换算法为基于KDTree的仿射变换，可添加TransType参数改变
        }
        #endregion

        /// <summary>
        /// 标定验证
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype VerifyCalibRulerReal(string opticName)
        {
            return _sensorMotionCali.CalCalibVerify(opticName);
        }

        #region 平台标定

        /// <summary>
        /// 一次性标定宏动平台每个旋转角对应的X1X2轴运动量
        /// </summary>
        /// <param name="stageName">平台名称</param>
        /// <param name="markCenterAtRealL">单侧相机逼近后的mark标定板坐标</param>
        /// <param name="markCenterAtRealR">另一侧相机逼近后的mark标定板坐标</param>
        /// <param name="axisX">当前X1轴移动量(X2始终为X1的负值)</param>
        /// <param name="angleList">旋转角列表</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibMacroTheta(string stageName, List<Point> markCenterAtRealL, List<Point> markCenterAtRealR, List<double> axisX, out List<double> angleList)
        {
            // res = table_macro_theta( macro_theta <-> XXY)
            angleList = new List<double>();
            if (markCenterAtRealL is null || markCenterAtRealR is null || axisX is null)
            {
                return Errortype.MACROSTAGE_CALIB_POINTS_NUM_ERROR;
            }

            if ((markCenterAtRealL.Count != markCenterAtRealR.Count) || (markCenterAtRealR.Count != axisX.Count))
            {
                return Errortype.MACROSTAGE_CALIB_POINTS_NUM_ERROR;
            }

            double base_angle = Math.Atan2(markCenterAtRealR[0].Y - markCenterAtRealL[0].Y, markCenterAtRealR[0].X - markCenterAtRealL[0].X);
            double base_AxisX = axisX[0];
            _stageMacroCali.CalibMacroRotateCenter(stageName, markCenterAtRealL[0]);
            _stageMacroCali.CalibMacroZeroPoint(stageName, new Point(base_AxisX, 0));
            List<double> angle_list = new List<double> { 0.0 };
            for (int index = 1; index < markCenterAtRealL.Count; index++)
            {
                if (markCenterAtRealL[index] is null || markCenterAtRealR[index] is null)
                {
                    return Errortype.MACROSTAGE_CALIB_POINTS_NUM_ERROR;
                }

                double new_angle = Math.Atan2(markCenterAtRealR[index].Y - markCenterAtRealL[index].Y, markCenterAtRealR[index].X - markCenterAtRealL[index].X);
                double angle_deg = (new_angle - base_angle) / Math.PI * 180;
                angle_list.Add(angle_deg);
                axisX[index] = axisX[index] - axisX[0];
            }

            axisX[0] = 0.0;
            angleList = angle_list;
            return _stageMacroCali.AddThetaDictionary(axisX, angle_list, stageName);
        }

        /// <summary>
        /// 宏动旋转中心纠正标定
        /// （当角度过大时，可能存在mark飞出相机视野的情况，此时可以根据上一次角度旋转计算出的平移量进行一个初动，再进行逼近）
        /// </summary>
        /// <param name="stageName">平台名称</param>
        /// <param name="currentAngle">当前角度</param>
        /// <param name="centerOffset">中心偏移</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibMacroRotateCenterRectification(string stageName, List<double> currentAngle, List<Point> centerOffset) // 宏动平台每个旋转角对应的旋转中心XXY纠正量标定
        {
            return _stageMacroCali.AddRotateCenterOffsetDictionary(currentAngle, centerOffset, stageName);
        }

        /// <summary>
        /// 微动平台零点状态的标定板坐标转换标定，需要 GetRealByPix 配合计算像素中心真实坐标
        /// (考虑使用微动-像素比逼近方法对准)
        /// <para>1.宏动平台处于零位，微动平台处于零位，相机逼近mark。</para>
        /// <para>2.移动微动平台x轴，使mark在相机视野内左右移动，记录不同x位置下mark的标定板坐标系坐标。</para>
        /// <para>3.根据多个点可求出微动平台xy轴在标定板坐标系下的方向向量和缩放比例。</para>
        /// </summary>
        /// <param name="stageName">平台名称</param>
        /// <param name="axisPointX1">X向左侧点微动轴坐标</param>
        /// <param name="axisPointX2">X向右侧点微动轴坐标</param>
        /// <param name="axisPointY1">Y向上侧点微动轴坐标</param>
        /// <param name="axisPointY2">Y向下侧点微动轴坐标</param>
        /// <param name="realPoint1">X向左侧点标定板坐标</param>
        /// <param name="realPoint2">X向右侧点标定板坐标</param>
        /// <param name="realPoint3">Y向上侧点标定板坐标</param>
        /// <param name="realPoint4">Y向下侧点标定板坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibNanoRulerToReal(string stageName, Point axisPointX1, Point axisPointX2,
                                                                        Point axisPointY1, Point axisPointY2,
                                                                        Point realPoint1, Point realPoint2,
                                                                        Point realPoint3, Point realPoint4) //  微动台 nano_axisXY <-> real 标定
        {
            List<Point> stage_points = new List<Point> { axisPointX1, axisPointX2, axisPointY1, axisPointY2 };
            List<Point> real_points = new List<Point> { realPoint1, realPoint2, realPoint3, realPoint4 };
            _stageNanoCali.CalcAffineToNanoStage(stage_points, real_points, stageName);
            return Errortype.OK;
        }

        /// <summary>
        /// 微动平台零点状态的旋转中心位置标定
        /// (考虑使用微动-像素比逼近方法对准，此标定可不用)
        /// <para>1.宏动平台处于零位，微动平台处于零位，左右相机逼近mark中心</para>
        /// <para>2.在旋转的范围内均分角度进行旋转，相机获取mark的像素坐标，转换为标定板坐标系坐标</para>
        /// <para>3.根据多组标定板坐标系的线段，求出微动平台的旋转中心在标定板坐标系下的坐标</para>
        /// </summary>
        /// <param name="stageName">平台名称</param>
        /// <param name="realMarkPointL">线段左mark真值坐标集</param>
        /// <param name="realMarkPointR">线段右Mark真值坐标集</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibNanoRotateCenter(string stageName, List<Point> realMarkPointL, List<Point> realMarkPointR) //  微动旋转中心标定
        {
            if (realMarkPointL.Count != realMarkPointR.Count)
            {
                return Errortype.MACROSTAGE_CALIB_POINTS_NUM_ERROR;
            }

            var algo_res = ComAlgo.CalcRotateCenter(realMarkPointL, realMarkPointR, out Point rotate_center, out _);
            if (algo_res != Errortype.OK)
            {
                return algo_res;
            }

            _stageNanoCali.CalibRotateCenterAtReal(stageName, rotate_center);
            return Errortype.OK;
        }

        #endregion

        #region 标定验证和工具

        /// <summary>
        /// 用上相机对应像素坐标获取下相机对应像素的坐标(同心度验证)
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="topPoint">上相机坐标</param>
        /// <param name="bottomPoint">下相机坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetBottomPix(string opticName, Point topPoint, out Point bottomPoint)
        {
            return _sensorConcentricCali.GetBottomPixel(opticName, topPoint, out bottomPoint);
        }

        /// <summary>
        /// 用下相机对应像素坐标获取上相机对应像素的坐标(同心度验证)
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="bottomPoint">下相机坐标</param>
        /// <param name="topPoint">上相机坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetTopPix(string opticName, Point bottomPoint, out Point topPoint)
        {
            return _sensorConcentricCali.GetTopPixel(opticName, bottomPoint, out topPoint);
        }

        /// <summary>
        /// 像素点逼近
        /// 宏动平台像素点逼近(仅平移，无旋转)
        /// 微动平台像素点逼近(仅平移，无旋转)
        /// ，标定依赖：
        /// <para>【<seealso cref="CalibExtrinsicsScaleX"/>】</para>【<seealso cref="CalibExtrinsicsScaleY"/>】
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="currentImageCenterPix">当前图像中心坐标</param>
        /// <param name="targetMarkCenterPix">目标逼近像素点(mark中心像素)</param>
        /// <param name="motionXY">输出XY轴运动量</param>
        /// <param name="baseAngle">附加倾角(逆时针正向)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMotionXYByPix(string opticName, Point currentImageCenterPix, Point targetMarkCenterPix, out Point motionXY, double baseAngle = 0) // Optic， PEC
        {
            // scale_x, sacle_y (pix_to_ruler/ pix_to_macro/ pix_to_nano)
            Point axisPoint = new Point(0, 0);
            var cac_res = _sensorExtrinsicCali.GetRulerByPix(opticName, targetMarkCenterPix, axisPoint, out motionXY);
            if (cac_res != Errortype.OK)
            {
                return cac_res;
            }

            Point r_center = new Point(0, 0);
            ComAlgo.CalcRotatePoint(motionXY, baseAngle, r_center, out motionXY);
            return Errortype.OK;
        }

        /// <summary>
        /// 像素点转轴坐标
        /// <para>【<seealso cref="CalibExtrinsicsScaleX"/>】</para>【<seealso cref="CalibExtrinsicsScaleY"/>】
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="currentRulerPose">当前轴坐标</param>
        /// <param name="currentImageCenterPix">当前图像中心坐标</param>
        /// <param name="targetMarkCenterPix">待转换像素点(mark中心像素)</param>
        /// <param name="outPutXY">输出XY轴运动量</param>
        /// <param name="baseAngle">附加倾角(逆时针正向)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRulerByPix(string opticName, Point currentRulerPose, Point currentImageCenterPix, Point targetMarkCenterPix, out Point outPutXY, double baseAngle = 0) // Optic， PEC
        {
            // scale_x, sacle_y (pix_to_ruler/ pix_to_macro/ pix_to_nano)
            Point axisPoint = new Point(0, 0);

            // 此处先不传入当前轴位置，在输出相对距离之后加入坐标系旋转角，最终将叠加了旋转量的距离加上当前坐标即为目标位置
            var cac_res = _sensorExtrinsicCali.GetRulerByPix(opticName, targetMarkCenterPix, axisPoint, out outPutXY);
            if (cac_res != Errortype.OK)
            {
                return cac_res;
            }

            Point r_center = new Point(0, 0);
            ComAlgo.CalcRotatePoint(outPutXY, baseAngle, r_center, out outPutXY);
            outPutXY += currentRulerPose;
            return Errortype.OK;
        }

        /// <summary>
        /// 像素点+轴坐标转真实坐标
        /// <para>【<seealso cref="CalibExtrinsicsScaleX"/>】</para>【<seealso cref="CalibExtrinsicsScaleY"/>】
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="currentAxisPose">当前轴位置</param>
        /// <param name="currentImageCenterPix">当前图像中心坐标</param>
        /// <param name="targetMarkCenterPix">待转换像素点(mark中心像素)</param>
        /// <param name="realXY">输出XY轴运动量</param>
        /// <param name="baseAngle">附加倾角(逆时针正向)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRealByPix(string opticName, Point currentAxisPose, Point currentImageCenterPix, Point targetMarkCenterPix, out Point realXY, double baseAngle = 0) // Optic， PEC
        {
            // scale_x, sacle_y (pix_to_ruler/ pix_to_macro/ pix_to_nano)
            realXY = new Point(0, 0);
            var calc_res = _sensorExtrinsicCali.GetRulerByPix(opticName, targetMarkCenterPix, currentAxisPose, out Point targetRuler);
            if (calc_res != Errortype.OK)
            {
                return calc_res;
            }

            Point r_center = new Point(0, 0);
            ComAlgo.CalcRotatePoint(targetRuler, baseAngle, r_center, out targetRuler);
            calc_res = _sensorMotionCali.GetRealByRuler(opticName, targetRuler, out realXY);
            return Errortype.OK;
        }

        /// <summary>
        /// 轴坐标转像素点
        /// <para>【<seealso cref="CalibExtrinsicsScaleX"/>】</para>【<seealso cref="CalibExtrinsicsScaleY"/>】
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="currentImageCenterPix">当前图像中心坐标</param>
        /// <param name="currentAxisPose">当前轴坐标位置</param>
        /// <param name="targetAxisPose">目标轴坐标位置</param>
        /// <param name="outPutPix">输出轴像素坐标</param>
        /// <param name="baseAngle">附加倾角(逆时针正向)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetPixByAxis(string opticName, Point currentImageCenterPix, Point currentAxisPose, Point targetAxisPose, out Point outPutPix, double baseAngle = 0) // Optic， PEC
        {
            // scale_x, sacle_y (pix_to_ruler/ pix_to_macro/ pix_to_nano)
            //Point axisPoint = new Point(0, 0);
            var cac_res = _sensorExtrinsicCali.GetPixByRuler(opticName, currentAxisPose, targetAxisPose, out outPutPix);
            if (cac_res != Errortype.OK)
            {
                return cac_res;
            }

            Point r_center = new Point(0, 0);
            ComAlgo.CalcRotatePoint(outPutPix, -baseAngle, r_center, out outPutPix);
            return Errortype.OK;
        }

        /// <summary>
        /// 根据左右mark位置计算当前产品倾角
        /// </summary>
        /// <param name="opticLName">左标定项名称</param>
        /// <param name="opticRName">右标定项名称</param>
        /// <param name="imageCenterPixL">左图像中心像素坐标</param>
        /// <param name="imageCenterPixR">右图像中心像素坐标</param>
        /// <param name="markCenterPixL">左mark中心像素坐标</param>
        /// <param name="markCenterPixR">右Mark中心像素坐标</param>
        /// <param name="opticAxisL">当前左optic光栅位置</param>
        /// <param name="opticAxisR">当前右侧optic光栅位置</param>
        /// <param name="dipAngle">倾角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRealAngleByMarkCenterPixLR(
            string opticLName,
            string opticRName,
            Point imageCenterPixL,
            Point imageCenterPixR,
            Point markCenterPixL,
            Point markCenterPixR,
            Point opticAxisL,
            Point opticAxisR,
            out double dipAngle)
        {
            if (imageCenterPixL is null || imageCenterPixR is null || markCenterPixL is null || markCenterPixR is null || opticAxisL is null || opticAxisR is null)
            {
                dipAngle = 0;
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            _sensorExtrinsicCali.GetRulerByPix(opticLName, markCenterPixL, opticAxisL, out Point target_ruler_left);
            GetRealPoseByOpticRuler(opticLName, target_ruler_left, out Point target_real_left);
            _sensorExtrinsicCali.GetRulerByPix(opticRName, markCenterPixR, opticAxisR, out Point target_ruler_right);
            GetRealPoseByOpticRuler(opticLName, target_ruler_right, out Point target_real_right);
            dipAngle = Math.Atan2(target_real_right.Y - target_real_left.Y, target_real_right.X - target_real_left.X) / Math.PI * 180;
            return Errortype.OK;
        }

        /// <summary>
        /// 输入标定板坐标点，输出对应optic光栅坐标（光栅真实坐标系全局标定验证）
        /// 标定依赖 【<seealso cref="CalibOpticRulerReal"/>】
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="targetRealPoint">标定板坐标系上的目标</param>
        /// <param name="opticPose">输出光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetOpticPoseByReal(string opticName, Point targetRealPoint, out Point opticPose)
        {
            return _sensorMotionCali.GetRulerByReal(opticName, targetRealPoint, out opticPose);
        }

        /// <summary>
        /// 输入optic光栅坐标点，输出对应标定板坐标（光栅真实坐标系全局标定验证）
        /// 标定依赖 【<seealso cref="CalibOpticRulerReal"/>】
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="targetRulerPoint">目标光栅坐标</param>
        /// <param name="realPose">输出真值坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRealPoseByOpticRuler(string opticName, Point targetRulerPoint, out Point realPose)
        {
            return _sensorMotionCali.GetRealByRuler(opticName, targetRulerPoint, out realPose);
        }

        /// <summary>
        /// 宏动平台默认旋转中心获取(由最近一次标定的左侧mark中心位置决定)
        /// </summary>
        /// <param name="stageName">平台名称</param>
        /// <param name="rotateCenter">旋转中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMacroRotateCenter(string stageName, out Point rotateCenter)
        {
            return _stageMacroCali.GetMacroRotateCenter(stageName, out rotateCenter);
        }

        /// <summary>
        /// 宏动平台：输入角度信息(逆时针为正方向)，输出宏动平台各轴运动量，默认使用标定旋转中心旋转
        /// </summary>
        /// <param name="stageName">平台名称</param>
        /// <param name="currentMacroAngle">当前mark(产品)倾角</param>
        /// <param name="targetMacroAngle">目标mark(产品)倾角</param>
        /// <param name="motionXX">宏动X1，X2对向运动距离(一正一负)，实现旋转</param>
        /// <param name="motionXY">旋转后的XXY平移，实现旋转中心的纠正</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMacroThetaAndOffsetByAngle(string stageName, double currentMacroAngle, double targetMacroAngle, out double motionXX, out Point motionXY)
        {
            // search table_macro_theta & thetaoffset
            motionXX = 0;
            motionXY = new Point(0, 0);
            var res_status = _stageMacroCali.GetThetaOffset(currentMacroAngle, stageName, out double currentXX, out Point currentXY);
            if (res_status != Errortype.OK)
            {
                return res_status;
            }

            res_status = _stageMacroCali.GetThetaOffset(targetMacroAngle, stageName, out double targetXX, out Point targetXY);
            if (res_status != Errortype.OK)
            {
                return res_status;
            }

            motionXX = targetXX - currentXX;
            motionXY = targetXY - targetXY;
            return Errortype.OK;
        }

        /// <summary>
        /// 宏动平台：输入旋转角度，输出宏动平台各轴运动量(无固定旋转中心)，mark搜索时使用，需配合宏动像素逼近实现绕定mark中心旋转
        /// </summary>
        /// <param name="stageName">平台名称</param>
        /// <param name="currentMacroAngle">当前宏动角度</param>
        /// <param name="targetMacroAngle">目标宏动角度</param>
        /// <param name="motionXX">宏动X1，X2对向运动距离(一正一负)，实现旋转</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMacroThetaByRotation(string stageName, double currentMacroAngle, double targetMacroAngle, out double motionXX)
        {
            // search table_macro_theta
            motionXX = 0;
            var res_status = _stageMacroCali.GetThetaOffset(currentMacroAngle, stageName, out double currentXX, out Point currentXY);
            if (res_status != Errortype.OK)
            {
                return res_status;
            }

            res_status = _stageMacroCali.GetThetaOffset(targetMacroAngle, stageName, out double targetXX, out Point targetXY);
            if (res_status != Errortype.OK)
            {
                return res_status;
            }

            motionXX = targetXX - currentXX;
            return Errortype.OK;
        }

        /// <summary>
        /// 微动旋转变换计算
        /// </summary>
        /// <param name="nanoStageName">微平台名称</param>
        /// <param name="nanoRotateAngle">微平台旋转角度</param>
        /// <param name="currentRealPoint">当前真值坐标</param>
        /// <param name="targetRealPoint">目标真值坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetNanoPointRotateByReal(
            string nanoStageName, double nanoRotateAngle,
            Point currentRealPoint, out Point targetRealPoint)
        {
            return _stageNanoCali.CacNanoRotateTransform(nanoStageName, nanoRotateAngle, currentRealPoint, out targetRealPoint);
        }

        /// <summary>
        /// 微动平台旋转中心获取
        /// </summary>
        /// <param name="nanoStageName">微平台名称</param>
        /// <param name="targetRotatePoint">目标旋转位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetNanoRotateCenter(string nanoStageName, out Point targetRotatePoint)
        {
            return _stageNanoCali.GetNanoRotateCenter(nanoStageName, out targetRotatePoint);
        }

        /// <summary>
        /// AVM模块获取chuck坐标
        /// </summary>
        /// <param name="chuckName">chuck名称</param>
        /// <param name="rulerPoint">光栅坐标</param>
        /// <param name="chuckPoint">chuck坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetChuckByRuler(string chuckName, Point rulerPoint, out Point chuckPoint)
        {
            return _stageChuckCali.GetChuckByRuler(chuckName, rulerPoint, out chuckPoint);
        }

        /// <summary>
        /// AVM模块获取轴光栅坐标
        /// </summary>
        /// <param name="chuckName">chuck名称</param>
        /// <param name="chuckPoint">chuck坐标</param>
        /// <param name="rulerPoint">光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRulerByChuck(string chuckName, Point chuckPoint, out Point rulerPoint)
        {
            return _stageChuckCali.GetRulerByChuck(chuckName, chuckPoint, out rulerPoint);
        }

        #endregion

    }
    */
    #endregion

    /// <summary>
    /// SVA参数
    /// </summary>
    public class SVAParamInfo
    {
        private string _topLeftItemName;

        /// <summary>
        /// Gets or Sets 左上相机标定名称
        /// </summary>
        public string TopLeftItemName
        {
            get => _topLeftItemName;
            set => _topLeftItemName = value;
        }

        private string _bottomLeftItemName;

        /// <summary>
        /// Gets or Sets 左下相机标定名称
        /// </summary>
        public string BottomLeftItemName
        {
            get => _bottomLeftItemName;
            set => _bottomLeftItemName = value;
        }

        private string _topRightItemName;

        /// <summary>
        /// Gets or Sets 右上相机标定名称
        /// </summary>
        public string TopRightItemName
        {
            get => _topRightItemName;
            set => _topRightItemName = value;
        }

        private string _bottomRightItemName; //右下相机标定名称

        /// <summary>
        /// Gets or Sets 右下相机标定名称
        /// </summary>
        public string BottomRightItemName
        {
            get => _bottomRightItemName;
            set => _bottomRightItemName = value;
        }

        private string _leftCoaxialityItemName;

        /// <summary>
        /// Gets or Sets 左侧镜组同轴度标定名称
        /// </summary>
        public string LeftCoaxialityItemName
        {
            get => _leftCoaxialityItemName;
            set => _leftCoaxialityItemName = value;
        }

        private string _rightCoaxialityItemName;

        /// <summary>
        /// Gets or Sets 右侧镜组同轴度标定名称
        /// </summary>
        public string RightCoaxialityItemName
        {
            get => _rightCoaxialityItemName;
            set => _rightCoaxialityItemName = value;
        }

        private string _pecLeftItemName; //pec左侧相机标定名称

        /// <summary>
        /// Gets or Sets pec左侧相机标定名称
        /// </summary>
        public string PecLeftItemName
        {
            get => _pecLeftItemName;
            set => _pecLeftItemName = value;
        }

        private string _pecRightItemName; //pec右侧相机标定名称

        /// <summary>
        /// Gets or Sets pec右侧相机标定名称
        /// </summary>
        public string PecRightItemName
        {
            get => _pecRightItemName;
            set => _pecRightItemName = value;
        }

        private Point _imgCenter; //图像中心像素

        /// <summary>
        /// Gets or Sets 图像中心像素
        /// </summary>
        public Point ImgCenter
        {
            get => _imgCenter;
            set => _imgCenter = value;
        }

        private Point _pecLeftRuler; //pec左侧光栅坐标

        /// <summary>
        /// Gets or Sets pec左侧光栅坐标
        /// </summary>
        public Point PecLeftRuler
        {
            get => _pecLeftRuler;
            set => _pecLeftRuler = value;
        }

        private Point _pecRightRuler; //pec右侧光栅坐标

        /// <summary>
        /// Gets or Sets pec右侧光栅坐标
        /// </summary>
        public Point PecRightRuler
        {
            get => _pecRightRuler;
            set => _pecRightRuler = value;
        }

        private double _topLeftScale; //左上相机像素比

        /// <summary>
        /// Gets or Sets 左上相机像素比
        /// </summary>
        public double TopLeftScale
        {
            get => _topLeftScale;
            set => _topLeftScale = value;
        }

        private double _bottomLeftScale; //左下相机像素比

        /// <summary>
        /// Gets or Sets 左下相机像素比
        /// </summary>
        public double BottomLeftScale
        {
            get => _bottomLeftScale;
            set => _bottomLeftScale = value;
        }

        private double _topRightScale; //右上相机像素比

        /// <summary>
        /// Gets or Sets 右上相机像素比
        /// </summary>
        public double TopRightScale
        {
            get => _topRightScale;
            set => _topRightScale = value;
        }

        private double _bottomRightScale; //右下相机像素比

        /// <summary>
        /// Gets or Sets 
        /// </summary>
        public double BottomRightScale
        {
            get => _bottomRightScale;
            set => _bottomRightScale = value;
        }

        private double _pecLeftScale; //pec左侧相机像素比

        /// <summary>
        /// Gets or Sets pec左侧相机像素比
        /// </summary>
        public double PecLeftScale
        {
            get => _pecLeftScale;
            set => _pecLeftScale = value;
        }

        private double _pecRightScale; //pec右侧相机像素比

        /// <summary>
        /// Gets or Sets pec右侧相机像素比
        /// </summary>
        public double PecRightScale
        {
            get => _pecRightScale;
            set => _pecRightScale = value;
        }

        private Dir _topLeftDir; //左上相机像素比方向

        /// <summary>
        /// Gets or Sets 左上相机像素比方向
        /// </summary>
        public Dir TopLeftDir
        {
            get => _topLeftDir;
            set => _topLeftDir = value;
        }

        private Dir _bottomLeftDir; //左下相机像素比方向

        /// <summary>
        /// Gets or Sets 左下相机像素比方向
        /// </summary>
        public Dir BottomLeftDir
        {
            get => _bottomLeftDir;
            set => _bottomLeftDir = value;
        }

        private Dir _topRightDir; //右上相机像素比方向

        /// <summary>
        /// Gets or Sets 右上相机像素比方向
        /// </summary>
        public Dir TopRightDir
        {
            get => _topRightDir;
            set => _topRightDir = value;
        }

        private Dir _bottomRightDir; //右下相机像素比方向

        /// <summary>
        /// Gets or Sets 右下相机像素比方向
        /// </summary>
        public Dir BottomRightDir
        {
            get => _bottomRightDir;
            set => _bottomRightDir = value;
        }

        private Dir _pecLeftDir; //pec左侧相机像素比方向

        /// <summary>
        /// Gets or Sets pec左侧相机像素比方向
        /// </summary>
        public Dir PecLeftDir
        {
            get => _pecLeftDir;
            set => _pecLeftDir = value;
        }

        private Dir _pecRightDir; //pec右侧相机像素比方向

        /// <summary>
        /// Gets or Sets pec右侧相机像素比方向
        /// </summary>
        public Dir PecRightDir
        {
            get => _pecRightDir;
            set => _pecRightDir = value;
        }

        private double _topLeftAngle; //左上相机角度

        /// <summary>
        /// Gets or Sets 左上相机角度
        /// </summary>
        public double TopLeftAngle
        {
            get => _topLeftAngle;
            set => _topLeftAngle = value;
        }

        private double _bottomLeftAngle; //左下相机角度

        /// <summary>
        /// Gets or Sets 左下相机角度
        /// </summary>
        public double BottomLeftAngle
        {
            get => _bottomLeftAngle;
            set => _bottomLeftAngle = value;
        }

        private double _topRightAngle; //右上相机角度

        /// <summary>
        /// Gets or Sets 右上相机角度
        /// </summary>
        public double TopRightAngle
        {
            get => _topRightAngle;
            set => _topRightAngle = value;
        }

        private double _bottomRightAngle; //右下相机角度

        /// <summary>
        /// Gets or Sets 右下相机角度
        /// </summary>
        public double BottomRightAngle
        {
            get => _bottomRightAngle;
            set => _bottomRightAngle = value;
        }

        private double _pecLeftAngle; //pec左侧相机角度

        /// <summary>
        /// Gets or Sets pec左侧相机角度
        /// </summary>
        public double PecLeftAngle
        {
            get => _pecLeftAngle;
            set => _pecLeftAngle = value;
        }

        private double _pecRightAngle; //pec右侧相机角度

        /// <summary>
        /// Gets or Sets pec右侧相机角度
        /// </summary>
        public double PecRightAngle
        {
            get => _pecRightAngle;
            set => _pecRightAngle = value;
        }

        /// <summary>
        /// 检查必要参数合理性
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Check()
        {
            if (TopLeftItemName is null ||
                BottomLeftItemName is null ||
                TopRightItemName is null ||
                BottomRightItemName is null ||
                LeftCoaxialityItemName is null ||
                RightCoaxialityItemName is null ||
                PecLeftItemName is null ||
                PecRightItemName is null)
            {
                return Errortype.FUSIONMANAGER_SVAPARAMINFO_ITEMNAME_NULL;
            }

            if (TopLeftItemName == string.Empty ||
                BottomLeftItemName == string.Empty ||
                TopRightItemName == string.Empty ||
                BottomRightItemName == string.Empty ||
                LeftCoaxialityItemName == string.Empty ||
                RightCoaxialityItemName == string.Empty ||
                PecLeftItemName == string.Empty ||
                PecRightItemName == string.Empty)
            {
                return Errortype.FUSIONMANAGER_SVAPARAMINFO_ITEMNAME_EMPTY;
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// Dir
    /// </summary>
    public enum Dir
    {
        Dir_XPositive_YPositive,
        Dir_XPositive_YNegative,
        Dir_XNegative_YPositive,
        Dir_XNegative_YNegative,
    }

    /// <summary>
    /// 简化标定内容
    /// </summary>
    public static class FusionManagerSimplifyCalib
    {
        //简化标定内容，缩短标定时间，精度主要通过逼近的方式来控制
        //private static CalibCoaxiality _calibCoaxiality = new CalibCoaxiality(); //同轴标定  left right
        private static Dictionary<string, Point> _coaxiality;
        private static CalibPix _calibPix = new CalibPix(); //转角像素比 leftTop rightTop leftBottom rightBottom leftPec rightPec
        private static SVAParamInfo _svaParamInfo; //sva参数信息
        private static Point _offsetXY = new Point(0, 0); //xy固定偏移 指在sva坐标系下上晶圆相对于下晶圆的偏差
        private static double _offsetTheta = 0; //角度固定偏移 指在sva坐标系下上晶圆相对于下晶圆的角度偏差

        #region 初始化

        /// <summary>
        /// 设置参数信息 同时执行初始化操作
        /// </summary>
        /// <param name="svaParamInfo">参数信息</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetParamInfo(SVAParamInfo svaParamInfo)
        {
            HOperatorSet.SetSystem("temporary_mem_cache", "false");
            HOperatorSet.SetSystem("temporary_mem_reservoir", "false");
            HOperatorSet.SetSystem("tsp_temporary_mem_cache", "false");
            HOperatorSet.SetSystem("tsp_temporary_mem_reservoir", "false");

            if (svaParamInfo is null)
            {
                return Errortype.FUSIONMANAGER_SETPARAMINFO_INPUT_NULL;
            }

            Errortype ret = svaParamInfo.Check();
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _svaParamInfo = svaParamInfo;

            _coaxiality = new Dictionary<string, Point>();
            _coaxiality.Add(_svaParamInfo.LeftCoaxialityItemName, new Point(0, 0));
            _coaxiality.Add(_svaParamInfo.RightCoaxialityItemName, new Point(0, 0));

            _calibPix = new CalibPix();
            List<string> calibPixName = new List<string>()
            {
                _svaParamInfo.TopLeftItemName,
                _svaParamInfo.BottomLeftItemName,
                _svaParamInfo.TopRightItemName,
                _svaParamInfo.BottomRightItemName,
                _svaParamInfo.PecLeftItemName,
                _svaParamInfo.PecRightItemName,
            };
            _calibPix.Init(calibPixName);

            _svaParamInfo.ImgCenter = new Point(2448 / 2, 2048 / 2);

            ret = CalibPixAngle(_svaParamInfo.TopLeftItemName, _svaParamInfo.TopLeftScale / 1e6, _svaParamInfo.TopLeftAngle, _svaParamInfo.TopLeftDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.BottomLeftItemName, _svaParamInfo.BottomLeftScale / 1e6, _svaParamInfo.BottomLeftAngle, _svaParamInfo.BottomLeftDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.TopRightItemName, _svaParamInfo.TopRightScale / 1e6, _svaParamInfo.TopRightAngle, _svaParamInfo.TopRightDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.BottomRightItemName, _svaParamInfo.BottomRightScale / 1e6, _svaParamInfo.BottomRightAngle, _svaParamInfo.BottomRightDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.PecLeftItemName, _svaParamInfo.PecLeftScale / 1e6, _svaParamInfo.PecLeftAngle, _svaParamInfo.PecLeftDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.PecRightItemName, _svaParamInfo.PecRightScale / 1e6, _svaParamInfo.PecRightAngle, _svaParamInfo.PecRightDir, _svaParamInfo.ImgCenter);

            return ret;
        }

        /// <summary>
        /// 释放所有内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            //_calibCoaxiality.Release();
            _coaxiality = null;
            _calibPix.Release();
            return Errortype.OK;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Save(string dir)
        {
            //TODO : 
            return Errortype.OK;
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Load(string dir)
        {
            //TODO : 
            return Errortype.OK;
        }

        /// <summary>
        /// 查询标定完成状态
        /// </summary>
        /// <param name="calibType">标定类型</param>
        /// <param name="itemName">标定项名称</param>
        /// <param name="calibComplete">输出标定是否完成</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetCalibStatus(CalibType calibType, string itemName, out bool calibComplete)
        {
            calibComplete = false;
            if (itemName is null)
            {
                return Errortype.FUSIONMANAGER_GETCALIBSTATUS_ITEMNAME_NULL;
            }

            Errortype ret = Errortype.OK;
            switch (calibType)
            {
                case CalibType.OPTIC_CONCENTERIC:
                    if (_coaxiality != null)
                    {
                        if (_coaxiality.ContainsKey(itemName))
                        {
                            if (_coaxiality[itemName] != null)
                            {
                                calibComplete = true;
                                ret = Errortype.OK;
                                break;
                            }
                            else
                            {
                                calibComplete = false;
                                ret = Errortype.OK;
                                break;
                            }
                        }
                    }

                    ret = Errortype.FUSIONMANAGER_COAXIALITY_ITEMNAME_ERROR;
                    break;
                case CalibType.OPTIC_EXTERNAL:
                    ret = _calibPix.GetCalibStatus(itemName, out calibComplete);
                    break;
                default:
                    return ret;
            }

            return ret;
        }

        #endregion

        #region 根据图像计算相机像素比

        /// <summary>
        /// 寻找最左边和最右边的线段（用于像素比计算）
        /// </summary>
        /// <param name="lineSegs">线段集</param>
        /// <param name="leftLine">最左边线段</param>
        /// <param name="rightLine">最右边线段</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype FindLineInLeftRight(List<List<LineSeg>> lineSegs, out LineSeg leftLine, out LineSeg rightLine)
        {
            leftLine = new LineSeg();
            rightLine = new LineSeg();
            HObject singleLineContour;
            HOperatorSet.GenEmptyObj(out singleLineContour);
            List<double> minDistanceLc = new List<double>();
            List<double> maxDistanceLc = new List<double>();
            List<LineSeg> allLines = new List<LineSeg>();
            if (lineSegs == null)
            {
                return Errortype.FUSIONMANAGER_FINDLINEINLEFTRIGHT_LINESEG_NULL;
            }

            List<LineSeg> lineSegsT = new List<LineSeg>();
            for (int i = 0; i < lineSegs.Count; i++)
            {
                lineSegsT.AddRange(lineSegs[i]);
            }

            if (lineSegsT == null || lineSegsT.Count < 2)
            {
                return Errortype.FUSIONMANAGER_FINDLINEINLEFTRIGHT_LINESEG_EMPTY;
            }

            foreach (LineSeg singleLine in lineSegsT)
            {
                if (singleLine == null)
                {
                    return Errortype.COMMONALGO_LINESEG_NUM_ERROR;
                }

                if (singleLine.CalculateLineLength() == 0.0)
                {
                    return Errortype.COMMONALGO_LINESEG_NUM_ERROR;
                }

                double deg = singleLine.CalculateLineDeg();

                // 选取尽量垂直或水平范围的线段
                if (Math.Abs(Math.Abs(deg) - 90) < 20)
                {
                    allLines.Add(new LineSeg(singleLine.Start_X, singleLine.Start_Y, singleLine.End_X, singleLine.End_Y));
                    singleLine.Shorten(0.15);
                    HOperatorSet.GenRegionLine(out HObject singleRegion, singleLine.Start_Y, singleLine.Start_X, singleLine.End_Y, singleLine.End_X);
                    HOperatorSet.GenContourRegionXld(singleRegion, out singleLineContour, "border");
                    HOperatorSet.DistanceLc(singleLineContour, 0, 0, 1, 0, out HTuple distanceMin, out HTuple distanceMax);

                    minDistanceLc.Add(distanceMin.D);
                    maxDistanceLc.Add(distanceMax.D);
                    singleRegion.Dispose();
                    singleLineContour.Dispose();
                }
            }

            if (maxDistanceLc.Count < 2)
            {
                return Errortype.FUSIONMANAGER_CALIBPIX_BAD_MRAK;
            }

            HOperatorSet.TupleMax(maxDistanceLc.ToArray(), out HTuple maxDist);
            HOperatorSet.TupleFind(maxDistanceLc.ToArray(), maxDist, out HTuple maxId);
            HOperatorSet.TupleMin(minDistanceLc.ToArray(), out HTuple minDist);
            HOperatorSet.TupleFind(minDistanceLc.ToArray(), minDist, out HTuple minId);
            leftLine = allLines[minId.I];
            rightLine = allLines[maxId.I];
            return Errortype.OK;
        }

        /// <summary>
        /// 计算毫米像素比
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="size">左右侧线段间距</param>
        /// <param name="mmppx">毫米像素比</param>
        /// <param name="lineSegs">边缘线段集合</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcMmppx(Camera img, double size, out double mmppx, out List<LineSeg> lineSegs)
        {
            mmppx = 0;
            lineSegs = new List<LineSeg>();
            if (img == null)
            {
                return Errortype.FUSIONMANAGER_CALCMMPPX_IMG_NULL;
            }

            Errortype ret = Errortype.OK;
            HObject hImg = img.GenHObject();
            CaliperParams caliperParams = new CaliperParams();
            ret = MarkLocation.Common.GetMarkEdgeLine(hImg, caliperParams, out HObject imgOut, out List<List<LineSeg>> lineSegsT);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = FindLineInLeftRight(lineSegsT, out LineSeg leftLine, out LineSeg rightLine);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //找到后需要增加卡尺的操作，不然不够准确,参数使用自动参数
            ret = Caliper.CaliperLine.LineExtraction(img, leftLine, caliperParams, out LineSeg leftCaliper, out double[] error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = Caliper.CaliperLine.LineExtraction(img, rightLine, caliperParams, out LineSeg rightCaliper, out error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            lineSegs.Add(leftCaliper);
            lineSegs.Add(rightCaliper);
            double dist = ComAlgo.Dist(leftCaliper, rightCaliper);
            mmppx = size / dist;

            hImg.Dispose();
            imgOut.Dispose();

            return Errortype.OK;
        }

        #endregion

        #region 内外参

        //相机内外参标定
        //这里标定所有相机的内外参，主要用于逼近时使用，建议itemName为LeftTop，LeftBottom，RightTop，RightBottom，PecRight，PecLeft
        //calibType均为OPTIC_EXTERNAL
        //使用时可以输入单个像素点和当前光栅值，输出目标点的光栅值

        /// <summary>
        /// 相机内参标定 xy像素比值设定，转角为0
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="scale">像素比</param>
        /// <param name="dir">图像与轴方向</param>
        /// <param name="imgCenter">相机图像中心像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibPix(string itemName, double scale, Dir dir, Point imgCenter)
        {
            int dirX = 1;
            int dirY = 1;
            switch (dir)
            {
                case Dir.Dir_XPositive_YPositive:
                    break;
                case Dir.Dir_XPositive_YNegative:
                    dirY = -1;
                    break;
                case Dir.Dir_XNegative_YPositive:
                    dirX = -1;
                    break;
                case Dir.Dir_XNegative_YNegative:
                    dirX = -1;
                    dirY = -1;
                    break;
                default:
                    break;
            }

            _calibPix.SetPixCenter(itemName, imgCenter);
            return _calibPix.SetScale(itemName, scale * dirX / 1e6, scale * dirY / 1e6);
        }

        private static Errortype CalibPixAngle(string itemName, double scale, double angle, Dir dir, Point imgCenter)
        {
            int dirX = 1;
            int dirY = 1;
            switch (dir)
            {
                case Dir.Dir_XPositive_YPositive:
                    break;
                case Dir.Dir_XPositive_YNegative:
                    dirY = -1;
                    break;
                case Dir.Dir_XNegative_YPositive:
                    dirX = -1;
                    break;
                case Dir.Dir_XNegative_YNegative:
                    dirX = -1;
                    dirY = -1;
                    break;
                default:
                    break;
            }

            _calibPix.SetPixCenter(itemName, imgCenter);
            return _calibPix.SetScaleAngle(itemName, scale * dirX, scale * dirY, angle);
        }

        /// <summary>
        /// 标定转角
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pixelStart">像素起始坐标</param>
        /// <param name="pixelEnd">像素终点坐标</param>
        /// <param name="angleDeg">返回角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibPixAngleX(string itemName, Point pixelStart, Point pixelEnd, out double angleDeg)
        {
            return _calibPix.CalibPixAngle(itemName, pixelStart, pixelEnd, out angleDeg);
        }

        /// <summary>
        /// 像素点转轴坐标
        /// <para>【<seealso cref="CalibExtrinsicsScaleX"/>】</para>【<seealso cref="CalibExtrinsicsScaleY"/>】
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="currentRuler">当前的光栅值</param>
        /// <param name="targetMarkCenterPix">待转换像素点(mark中心像素)</param>
        /// <param name="targetMarkCenterRuler">目标点的光栅位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRulerByPix(string itemName, Point currentRuler, Point targetMarkCenterPix, out Point targetMarkCenterRuler)
        {
            var ret = _calibPix.GetRulerByPix(itemName, targetMarkCenterPix, currentRuler, out targetMarkCenterRuler);
            return ret;
        }

        /// <summary>
        /// 轴坐标转像素点
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="currentRuler">当前光栅值</param>
        /// <param name="targetMarkCenterRuler">目标mark中心光栅值</param>
        /// <param name="targetMarkCenterPixel">目标mark中心像素值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetPixByRuler(string itemName, Point currentRuler, Point targetMarkCenterRuler, out Point targetMarkCenterPixel)
        {
            var ret = _calibPix.GetPixByRuler(itemName, currentRuler, targetMarkCenterRuler, out targetMarkCenterPixel);
            return ret;
        }

        #endregion

        #region 同心度

        //同心度标定
        //这里标定左右侧相机组的同心度，建议itemName分别为left和right，calibType均为OPTIC_CONCENTERIC
        //标定时输入为两张图像
        //使用时可以输入一个相机的像素点，输出另一个相机的像素点

        /// <summary>
        /// 标定同心度误差
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="upOpticItemName">顶部相机标定项名称</param>
        /// <param name="downOpticItemName">底部相机标定项名称</param>
        /// <param name="upMarkCenterPixel">顶部mark中心像素坐标</param>
        /// <param name="downMarkCenterPixel">底部mark中心像素坐标</param>
        /// <param name="offset">输出顶部到底部的偏移值（nm）</param>
        /// <param name="upMarkCenterRuler">输出顶部mark的中心光栅坐标</param>
        /// <param name="downMarkCenterRuler">输出底部mark的中心光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibCoaxiality(string itemName, string upOpticItemName, string downOpticItemName, Point upMarkCenterPixel, Point downMarkCenterPixel,
            out Point offset, out Point upMarkCenterRuler, out Point downMarkCenterRuler)
        {
            offset = new Point();
            upMarkCenterRuler = new Point();
            downMarkCenterRuler = new Point();

            if (upMarkCenterPixel is null || downMarkCenterPixel is null)
            {
                return Errortype.FUSIONMANAGER_COAXIALITY_POINTIN_NULL;
            }

            if (_coaxiality == null || !_coaxiality.ContainsKey(itemName))
            {
                return Errortype.FUSIONMANAGER_COAXIALITY_ITEMNAME_ERROR;
            }

            //点转光栅，用于导出
            Point ruler = new Point(0, 0);
            Errortype ret = GetRulerByPix(upOpticItemName, ruler, upMarkCenterPixel, out upMarkCenterRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = GetRulerByPix(downOpticItemName, ruler, downMarkCenterPixel, out downMarkCenterRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //offset = upMarkCenterRuler - downMarkCenterRuler;
            offset = downMarkCenterRuler - upMarkCenterRuler;

            //_coaxiality[itemName] = upMarkCenterPixel - downMarkCenterPixel;
            _coaxiality[itemName] = offset.Clone();

            if (ComAlgo.SaveFlg("CalibCoaxiality", out int days))
            {
                string path = @"D:\Alg\CalibCoaxiality";

                string fileName = "CalibCoaxiality.txt";

                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string text = time + " CalibCoaxiality " + upOpticItemName + sep + downOpticItemName + sep +
                              upMarkCenterPixel.ToString(sep) + sep + downMarkCenterPixel.ToString(sep) + sep +
                              upMarkCenterRuler.ToString(sep) + sep + downMarkCenterRuler.ToString(sep) + sep +
                              offset.ToString(sep);

                ComAlgo.LogText(text, path, fileName, days);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 重置同心度误差
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype ReleaseCalibCoaxiality()
        {
            if (_coaxiality is null)
            {
                return Errortype.OK;
            }

            _coaxiality.Clear();
            _coaxiality.Add(_svaParamInfo.LeftCoaxialityItemName, new Point(0, 0));
            _coaxiality.Add(_svaParamInfo.RightCoaxialityItemName, new Point(0, 0));
            return Errortype.OK;
        }

        /// <summary>
        /// 根据底部像素点获取顶部像素点
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="bottomPoint">底部像素坐标</param>
        /// <param name="topPoint">顶部像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetTopPosition(string itemName, Point bottomPoint, out Point topPoint)
        {
            topPoint = new Point();

            //return _calibCoaxiality.GetTopPixel(itemName, bottomPoint, out topPoint);
            if (!_coaxiality.ContainsKey(itemName))
            {
                return Errortype.FUSIONMANAGER_COAXIALITY_ITEMNAME_ERROR;
            }

            topPoint = bottomPoint + _coaxiality[itemName];
            return Errortype.OK;
        }

        /// <summary>
        /// 根据顶部像素点获取底部像素点
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="topPoint">顶部像素坐标</param>
        /// <param name="bottomPoint">底部像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetBottomPosition(string itemName, Point topPoint, out Point bottomPoint)
        {
            bottomPoint = new Point();

            //return _calibCoaxiality.GetBottomPixel(itemName, topPoint, out bottomPoint);
            if (!_coaxiality.ContainsKey(itemName))
            {
                return Errortype.FUSIONMANAGER_COAXIALITY_ITEMNAME_ERROR;
            }

            bottomPoint = topPoint - _coaxiality[itemName];
            return Errortype.OK;
        }
        #endregion

        #region 标定微动平台的增益

        //XYT 需要软件自己求一下增益，算法求出前后两次之间的角度和距离
        private static Point _firstGainLeftRuler; //第一次左侧mark光栅
        private static Point _firstGainRightRuler; //第一次右侧mark光栅
        private static Point _leftGainRuler; //左侧相机轴光栅
        private static Point _rightGainRuler; //右侧相机轴光栅
        private static string _leftItemName; //左侧相机标定项名称
        private static string _rightItemName; //右侧相机标定项名称

        /// <summary>
        /// 计算两条线段之间的角度
        /// </summary>
        /// <param name="line1_Start">线段1起点</param>
        /// <param name="line1_End">线段1终点</param>
        /// <param name="line2_Start">线段2起点</param>
        /// <param name="line2_End">线段2终点</param>
        /// <param name="angle">输出角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype CalcAngleLL(Point line1_Start, Point line1_End, Point line2_Start, Point line2_End, out double angle)
        {
            angle = 0;
            LineSeg line1 = new LineSeg(line1_Start, line1_End);
            LineSeg line2 = new LineSeg(line2_Start, line2_End);
            Errortype ret = ComAlgo.CalcAngleLL(line1, line2, out angle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HOperatorSet.TupleDeg(angle, out HTuple deg);
            angle = deg.D;
            return ret;
        }

        /// <summary>
        /// 设置增益标定起始位置信息
        /// </summary>
        /// <param name="leftPixel">左侧像素坐标</param>
        /// <param name="rightPixel">右侧像素坐标</param>
        /// <param name="leftItemName">左侧相机标定名称</param>
        /// <param name="rightItemName">右侧相机标定名称</param>
        /// <param name="leftRuler">左侧光栅</param>
        /// <param name="rightRuler">右侧光栅</param>
        /// <param name="firstGainLeftRuler">输出左侧mark点光栅</param>
        /// <param name="firstGainRightRuler">输出右侧mark点光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetFirstGainInfo(Point leftPixel, Point rightPixel, string leftItemName, string rightItemName,
            Point leftRuler, Point rightRuler, out Point firstGainLeftRuler, out Point firstGainRightRuler)
        {
            firstGainLeftRuler = new Point();
            firstGainRightRuler = new Point();

            if (leftPixel is null || rightPixel is null || leftItemName is null || rightItemName is null || leftRuler is null || rightRuler is null)
            {
                return Errortype.FUSIONMANAGER_INPUT_NULL;
            }

            Errortype ret = Errortype.OK;

            //坐标转换
            ret = GetRulerByPix(leftItemName, leftRuler, leftPixel, out _firstGainLeftRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            firstGainLeftRuler = new Point(_firstGainLeftRuler.X, _firstGainLeftRuler.Y);

            ret = GetRulerByPix(rightItemName, rightRuler, rightPixel, out _firstGainRightRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            firstGainRightRuler = new Point(_firstGainRightRuler.X, _firstGainRightRuler.Y);

            _leftGainRuler = leftRuler;
            _rightGainRuler = rightRuler;
            _leftItemName = leftItemName;
            _rightItemName = rightItemName;

            return Errortype.OK;
        }

        /// <summary>
        /// 设置增益标定终止位置信息
        /// </summary>
        /// <param name="leftPixel">左侧mark像素坐标</param>
        /// <param name="rightPixel">右侧mark像素坐标</param>
        /// <param name="secondGainLeftRuler">输出左侧mark光栅</param>
        /// <param name="secondGainRightRuler">输出右侧mark光栅</param>
        /// <param name="dist">输出增益标定前后的移动距离</param>
        /// <param name="angle">输出增益标定前后的旋转角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetSecondGainInfo(Point leftPixel, Point rightPixel, out Point secondGainLeftRuler, out Point secondGainRightRuler, out double dist, out double angle)
        {
            dist = 0;
            angle = 0;
            secondGainLeftRuler = new Point();
            secondGainRightRuler = new Point();

            if (leftPixel is null || rightPixel is null)
            {
                return Errortype.FUSIONMANAGER_INPUT_NULL;
            }

            Errortype ret = Errortype.OK;

            //坐标转换
            ret = GetRulerByPix(_leftItemName, _leftGainRuler, leftPixel, out secondGainLeftRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = GetRulerByPix(_rightItemName, _rightGainRuler, rightPixel, out secondGainRightRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalcAngleLL(_firstGainLeftRuler, _firstGainRightRuler, secondGainLeftRuler, secondGainRightRuler, out angle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            Point centerFirst = (_firstGainLeftRuler + _firstGainRightRuler) / 2;
            Point centerSecond = (secondGainLeftRuler + secondGainRightRuler) / 2;
            dist = ComAlgo.Dist(centerFirst, centerSecond);

            return Errortype.OK;
        }

        #endregion

        #region 对准流程方法

        private static Point _leftRuler; //相机左侧光栅
        private static Point _rightRuler; //相机右侧光栅
        private static Point _bottomLeftPixel; //底部左侧mark像素
        private static Point _bottomRightPixel; //底部右侧mark像素
        private static Point _bottomLeftMarkRuler; //底部左侧mark光栅
        private static Point _bottomRightMarkRuler; //底部右侧mark光栅

        private static Point _pecLeftBasePixel; //pec左侧基准像素
        private static Point _pecRightBasePixel; //pec右侧基准像素
        private static Point _pecLeftBaseRuler; //pec左侧基准光栅
        private static Point _pecRightBaseRuler; //pec右侧基准光栅

        /// <summary>
        /// 计算线段与x轴的旋转平移量
        /// </summary>
        /// <param name="pointStart">线段起点</param>
        /// <param name="pointEnd">线段终点</param>
        /// <param name="translation">平移量</param>
        /// <param name="rotate">旋转量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcLXTR(Point pointStart, Point pointEnd, out Point translation, out double rotate)
        {
            Point basePoint1 = new Point(-1, 0);
            Point basePoint2 = new Point(1, 0);
            Errortype ret = CalcTR(basePoint1, basePoint2, pointStart, pointEnd, out translation, out rotate);
            return ret;
        }

        /// <summary>
        /// 计算当前点对到基准点对的旋转平移，默认旋转中心在0，0
        /// </summary>
        /// <param name="basePoint1">基准点1</param>
        /// <param name="basePoint2">基准点2</param>
        /// <param name="curPoint1">当前点1</param>
        /// <param name="curPoint2">当前点2</param>
        /// <param name="translation">平移量</param>
        /// <param name="rotate">旋转量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcTR(Point basePoint1, Point basePoint2, Point curPoint1, Point curPoint2, out Point translation, out double rotate)
        {
            translation = new Point();
            rotate = 0;

            if (basePoint1 is null || basePoint2 is null || curPoint1 is null || curPoint2 is null)
            {
                return Errortype.FUSIONMANAGER_INPUT_NULL;
            }

            Point baseCenterRuler = (basePoint1 + basePoint2) / 2;
            Point curCenterRuler = (curPoint1 + curPoint2) / 2;

            Errortype ret = CalcAngleLL(curPoint1, curPoint2, basePoint1, basePoint2, out rotate);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            // 角度取反 因为calcAngleLL的结果为反向
            //ret = ComAlgo.CalcRotatePoint(curCenterRuler, -rotate, new Point(0, 0), out Point pointRotated);
            //if (ret != Errortype.OK)
            //{
            //    return ret;
            //}
            //translation = baseCenterRuler - pointRotated;
            translation = baseCenterRuler - curCenterRuler;
            return Errortype.OK;
        }

        /// <summary>
        /// 设置偏差补偿值
        /// </summary>
        /// <param name="offsetXY">xy补偿，单位um</param>
        /// <param name="offsetTheta">角度补偿，单位urad</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetOffset(Point offsetXY, double offsetTheta)
        {
            _offsetTheta = offsetTheta / Math.PI * 180 / 1e6;
            _offsetXY = offsetXY / 1e3;

            if (ComAlgo.SaveFlg("SetOffset", out int days))
            {
                string path = @"D:\Alg\SetOffset";

                string fileName = "SetOffset.txt";

                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string text = time + " SetOffset " +
                              offsetXY.ToString(sep) + sep + offsetTheta.ToString() + sep +
                              _offsetXY.ToString(sep) + sep + _offsetTheta.ToString();
                ComAlgo.LogText(text, path, fileName, days);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取偏差补偿值
        /// </summary>
        /// <param name="offsetXy">xy补偿，单位um</param>
        /// <param name="offsetTheta">角度补偿，单位urad</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetOffset(out Point offsetXy, out double offsetTheta)
        {
            offsetXy = _offsetXY * 1e3;
            offsetTheta = _offsetTheta * 1e6 / 180 * Math.PI;
            return Errortype.OK;
        }

        /// <summary>
        /// 设置底部wafer信息
        /// </summary>
        /// <param name="bottomLeftPixel">底部左侧mark像素坐标</param>
        /// <param name="bottomRightPixel">底部右侧mark像素坐标</param>
        /// <param name="leftRuler">左侧光栅坐标</param>
        /// <param name="rightRuler">右侧光栅坐标</param>
        /// <param name="bottomLeftRuler">底部左侧mark光栅坐标</param>
        /// <param name="bottomRightRuler">底部右侧mark光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetBottomWaferInfo(Point bottomLeftPixel, Point bottomRightPixel, Point leftRuler, Point rightRuler, out Point bottomLeftRuler, out Point bottomRightRuler)
        {
            bottomLeftRuler = new Point();
            bottomRightRuler = new Point();

            Errortype ret = Errortype.OK;

            _bottomLeftPixel = bottomLeftPixel;
            _bottomRightPixel = bottomRightPixel;

            _leftRuler = leftRuler;
            _rightRuler = rightRuler;

            ret = GetRulerByPix(_svaParamInfo.TopLeftItemName, leftRuler, _bottomLeftPixel, out _bottomLeftMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            bottomLeftRuler = _bottomLeftMarkRuler;

            ret = GetRulerByPix(_svaParamInfo.TopRightItemName, rightRuler, _bottomRightPixel, out _bottomRightMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            bottomRightRuler = _bottomRightMarkRuler;

            return Errortype.OK;
        }

        /// <summary>
        /// 设置初始wafe信息
        /// </summary>
        /// <param name="bottomLeftPixel">左下像素坐标</param>
        /// <param name="bottomRightPixel">右下像素坐标</param>
        /// <param name="leftRuler">左侧光栅坐标</param>
        /// <param name="rightRuler">右侧光栅坐标</param>
        /// <param name="leftOpticName">左Name</param>
        /// <param name="rightOpticName">右Name</param>
        /// <param name="bottomLeftRuler">左下光栅</param>
        /// <param name="bottomRightRuler">右下光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetBaseWaferInfo(Point bottomLeftPixel, Point bottomRightPixel, Point leftRuler, Point rightRuler, string leftOpticName, string rightOpticName, out Point bottomLeftRuler, out Point bottomRightRuler)
        {
            bottomLeftRuler = new Point();
            bottomRightRuler = new Point();

            Errortype ret = Errortype.OK;

            _bottomLeftPixel = bottomLeftPixel;
            _bottomRightPixel = bottomRightPixel;

            _leftRuler = leftRuler;
            _rightRuler = rightRuler;

            ret = GetRulerByPix(leftOpticName, leftRuler, _bottomLeftPixel, out _bottomLeftMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            bottomLeftRuler = _bottomLeftMarkRuler;

            ret = GetRulerByPix(rightOpticName, rightRuler, _bottomRightPixel, out _bottomRightMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            bottomRightRuler = _bottomRightMarkRuler;

            if (ComAlgo.SaveFlg("TopWaferTR", out int days))
            {
                string path = @"D:\Alg\CalcBaseCurTR";
                string fileName = "CalcBaseCurTR.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string txt = time + sep + leftOpticName + sep + rightOpticName + sep +
                             _bottomLeftPixel.ToString(sep) + sep + _bottomRightPixel.ToString(sep) + sep +
                             leftRuler.ToString(sep) + sep + rightRuler.ToString(sep) + sep +
                             _bottomLeftMarkRuler.ToString(sep) + sep + _bottomRightMarkRuler.ToString(sep) + sep;
                ComAlgo.LogText(txt, path, fileName, days);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 设置顶部wafer信息 弃用
        /// </summary>
        /// <param name="topLeftPixel">顶部左侧mark像素坐标</param>
        /// <param name="topRightPixel">顶部右侧mark像素坐标</param>
        /// <param name="translation">输出对齐需要的平移量</param>
        /// <param name="rotate">输出对齐需要的旋转量</param>
        /// <param name="topLeftRuler">顶部左侧mark光栅坐标</param>
        /// <param name="topRightRuler">顶部右侧mark光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetTopWaferInfo(Point topLeftPixel, Point topRightPixel, out Point translation, out double rotate, out Point topLeftRuler, out Point topRightRuler)
        {
            translation = new Point();
            rotate = 0;
            topLeftRuler = new Point();
            topRightRuler = new Point();

            Errortype ret = Errortype.OK;

            //计算光栅坐标
            ret = GetRulerByPix(_svaParamInfo.BottomLeftItemName, _leftRuler, topLeftPixel, out Point topLeftMarkRuler);

            // 单相机art测试使用
            //ret = GetRulerByPix(_svaParamInfo._topLeftItemName, _leftRuler, topLeftPixel, out Point topLeftMarkRuler);
            //ret = GetRulerByPix(_svaParamInfo._topLeftItemName, new Point(0, 0), topLeftPixel, out Point topLeftMarkRulerRelative);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            topLeftRuler = topLeftMarkRuler;

            ret = GetRulerByPix(_svaParamInfo.BottomRightItemName, _rightRuler, topRightPixel, out Point topRightMarkRuler);

            // 单相机art测试使用
            //ret = GetRulerByPix(_svaParamInfo._topRightItemName, _rightRuler, topRightPixel, out Point topRightMarkRuler);
            //ret = GetRulerByPix(_svaParamInfo._topRightItemName, new Point(0, 0), topRightPixel, out Point topRightMarkRulerRelateive);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            topRightRuler = topRightMarkRuler;

            //将顶部相机的像素投影到底部相机上
            //ret = GetBottomPosition(_svaParamInfo._leftCoaxialityItemName, topLeftMarkRuler, out Point top_bottomLeftRuler);
            ret = GetTopPosition(_svaParamInfo.LeftCoaxialityItemName, topLeftMarkRuler, out Point top_bottomLeftRuler); //顶部mark是底部相机看到的 转到顶部相机看到的 用GetTopPosition
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //ret = GetBottomPosition(_svaParamInfo._rightCoaxialityItemName, topRightMarkRuler, out Point top_bottomRightRuler);
            ret = GetTopPosition(_svaParamInfo.RightCoaxialityItemName, topRightMarkRuler, out Point top_bottomRightRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //计算两组mark之间的平移旋转
            ret = CalcTR(_bottomLeftMarkRuler, _bottomRightMarkRuler, top_bottomLeftRuler, top_bottomRightRuler, out translation, out rotate);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //new alignment
            //Point deltaLeft = top_bottomLeftPixel - _bottomLeftPixel;
            //Point deltaRight = top_bottomRightPixel - _bottomRightPixel;
            //ret = GetRulerByPix(_svaParamInfo._bottomLeftItemName, _leftRuler, deltaLeft + _svaParamInfo._imgCenter, out Point deltaLeftRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaLeftRuler = deltaLeftRuler - _leftRuler;
            //ret = GetRulerByPix(_svaParamInfo._bottomRightItemName, _rightRuler, deltaRight + _svaParamInfo._imgCenter, out Point deltaRightRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaRightRuler = deltaRightRuler - _rightRuler;

            //translation = (deltaLeftRuler + deltaRightRuler) / -2;
            //rotate = Math.Atan((deltaRightRuler.Y - deltaLeftRuler.Y) / (_rightRuler.X - _leftRuler.X)) / Math.PI * 180;

            // 用mark相对坐标计算轴坐标,单相机art测试使用
            //Point currentRulerLeft = topLeftMarkRuler - topLeftMarkRulerRelative;
            //Point currentRulerRight = topRightMarkRuler - topRightMarkRulerRelateive;
            if (ComAlgo.SaveFlg("TopWaferTR", out int days))
            {
                string path = @"D:\Alg\CalcTR";
                string fileName = "CalcTR.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string txt = time + sep +
                    _bottomLeftPixel.ToString(sep) + sep + _bottomRightPixel.ToString(sep) + sep +
                    topLeftPixel.ToString(sep) + sep + topRightPixel.ToString(sep) + sep +
                    _bottomLeftMarkRuler.ToString(sep) + sep + _bottomRightMarkRuler.ToString(sep) + sep +
                    topLeftMarkRuler.ToString(sep) + sep + topRightMarkRuler.ToString(sep) + sep +
                    top_bottomLeftRuler.ToString(sep) + sep + top_bottomRightRuler.ToString(sep) + sep +

                    // optic轴坐标,单相机art测试使用
                    //sep + currentRulerLeft.ToString(sep) + sep + currentRulerRight.ToString(sep) + sep + sep +
                    translation.ToString(sep) + sep + rotate.ToString();
                ComAlgo.LogText(txt, path, fileName, days);
            }

            return ret;
        }

        /// <summary>
        /// 设置当前wafer信息
        /// </summary>
        /// <param name="topLeftPixel">左上像素坐标</param>
        /// <param name="topRightPixel">右上像素坐标</param>
        /// <param name="leftOpticName">左相机标定名称</param>
        /// <param name="rightOpticName">右相机标定名称</param>
        /// <param name="translation">平移量</param>
        /// <param name="rotate">转角</param>
        /// <param name="topLeftRuler">左上光栅坐标</param>
        /// <param name="topRightRuler">右上光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetCurrentWaferInfo(Point topLeftPixel, Point topRightPixel, string leftOpticName, string rightOpticName, out Point translation, out double rotate, out Point topLeftRuler, out Point topRightRuler)
        {
            translation = new Point();
            rotate = 0;
            topLeftRuler = new Point();
            topRightRuler = new Point();

            Errortype ret = Errortype.OK;

            //计算光栅坐标
            ret = GetRulerByPix(leftOpticName, _leftRuler, topLeftPixel, out Point topLeftMarkRuler);

            // 单相机art测试使用
            //ret = GetRulerByPix(_svaParamInfo._topLeftItemName, _leftRuler, topLeftPixel, out Point topLeftMarkRuler);
            //ret = GetRulerByPix(_svaParamInfo._topLeftItemName, new Point(0, 0), topLeftPixel, out Point topLeftMarkRulerRelative);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            topLeftRuler = topLeftMarkRuler;

            ret = GetRulerByPix(rightOpticName, _rightRuler, topRightPixel, out Point topRightMarkRuler);

            // 单相机art测试使用
            //ret = GetRulerByPix(_svaParamInfo._topRightItemName, _rightRuler, topRightPixel, out Point topRightMarkRuler);
            //ret = GetRulerByPix(_svaParamInfo._topRightItemName, new Point(0, 0), topRightPixel, out Point topRightMarkRulerRelateive);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            topRightRuler = topRightMarkRuler;

            //将顶部相机的像素投影到底部相机上
            //ret = GetBottomPosition(_svaParamInfo._leftCoaxialityItemName, topLeftMarkRuler, out Point top_bottomLeftRuler);
            ret = GetTopPosition(_svaParamInfo.LeftCoaxialityItemName, topLeftMarkRuler, out Point top_bottomLeftRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //ret = GetBottomPosition(_svaParamInfo._rightCoaxialityItemName, topRightMarkRuler, out Point top_bottomRightRuler);
            ret = GetTopPosition(_svaParamInfo.RightCoaxialityItemName, topRightMarkRuler, out Point top_bottomRightRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //计算两组mark之间的平移旋转
            //Point bottomLeftMarkRulerTranslation = _bottomLeftMarkRuler + _offsetXY;
            //Point bottomRightMarkRulerTranslation = _bottomRightMarkRuler + _offsetXY;
            ret = CalcTR(_bottomLeftMarkRuler, _bottomRightMarkRuler, top_bottomLeftRuler, top_bottomRightRuler, out translation, out rotate);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            translation += _offsetXY;
            rotate += _offsetTheta;

            //new alignment
            //Point deltaLeft = top_bottomLeftPixel - _bottomLeftPixel;
            //Point deltaRight = top_bottomRightPixel - _bottomRightPixel;
            //ret = GetRulerByPix(_svaParamInfo._bottomLeftItemName, _leftRuler, deltaLeft + _svaParamInfo._imgCenter, out Point deltaLeftRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaLeftRuler = deltaLeftRuler - _leftRuler;
            //ret = GetRulerByPix(_svaParamInfo._bottomRightItemName, _rightRuler, deltaRight + _svaParamInfo._imgCenter, out Point deltaRightRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaRightRuler = deltaRightRuler - _rightRuler;

            //translation = (deltaLeftRuler + deltaRightRuler) / -2;
            //rotate = Math.Atan((deltaRightRuler.Y - deltaLeftRuler.Y) / (_rightRuler.X - _leftRuler.X)) / Math.PI * 180;

            // 用mark相对坐标计算轴坐标,单相机art测试使用
            //Point currentRulerLeft = topLeftMarkRuler - topLeftMarkRulerRelative;
            //Point currentRulerRight = topRightMarkRuler - topRightMarkRulerRelateive;
            if (ComAlgo.SaveFlg("TopWaferTR", out int days))
            {
                string path = @"D:\Alg\CalcBaseCurTR";
                string fileName = "CalcBaseCurTR.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string txt = time + sep + leftOpticName + sep + rightOpticName + sep +
                    _bottomLeftPixel.ToString(sep) + sep + _bottomRightPixel.ToString(sep) + sep +
                    topLeftPixel.ToString(sep) + sep + topRightPixel.ToString(sep) + sep +
                    _bottomLeftMarkRuler.ToString(sep) + sep + _bottomRightMarkRuler.ToString(sep) + sep +
                    topLeftMarkRuler.ToString(sep) + sep + topRightMarkRuler.ToString(sep) + sep +
                    top_bottomLeftRuler.ToString(sep) + sep + top_bottomRightRuler.ToString(sep) + sep +

                    // optic轴坐标,单相机art测试使用
                    // sep + currentRulerLeft.ToString(sep) + sep + currentRulerRight.ToString(sep) + sep + sep +
                    translation.ToString(sep) + sep + rotate.ToString() + sep +
                    _offsetXY.ToString(sep) + sep + _offsetTheta.ToString();

                ComAlgo.LogText(txt, path, fileName, days);
            }

            return ret;
        }

        /// <summary>
        /// 设置第一次pec信息
        /// </summary>
        /// <param name="leftPixel">左侧mark像素坐标</param>
        /// <param name="rightPixel">右侧mark像素坐标</param>
        /// <param name="leftRuler">左侧光栅像素坐标</param>
        /// <param name="rightRuler">右侧光栅像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetBasePecInfo(Point leftPixel, Point rightPixel, out Point leftRuler, out Point rightRuler)
        {
            leftRuler = new Point();
            rightRuler = new Point();

            Errortype ret = Errortype.OK;

            _pecLeftBasePixel = leftPixel;
            ret = GetRulerByPix(_svaParamInfo.PecLeftItemName, _svaParamInfo.PecLeftRuler, _pecLeftBasePixel, out _pecLeftBaseRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            leftRuler = _pecLeftBaseRuler;

            _pecRightBasePixel = rightPixel;
            ret = GetRulerByPix(_svaParamInfo.PecRightItemName, _svaParamInfo.PecRightRuler, _pecRightBasePixel, out _pecRightBaseRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            rightRuler = _pecRightBaseRuler;

            if (ComAlgo.SaveFlg("PECInfo", out int days))
            {
                string path = @"D:\Alg\PecInfo";
                string fileName = "PecInfo.txt";
                string time = ComAlgo.GetDateTime();
                string text = time + " SetBasePecInfo " + leftPixel.ToString(" ") + " " + rightPixel.ToString(" ") + " " + _pecLeftBaseRuler.ToString(" ") + " " + _pecRightBaseRuler.ToString(" ");
                ComAlgo.LogText(text, path, fileName, days);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Errortype.OK;
        }

        /// <summary>
        /// 设置第二次pec信息
        /// </summary>
        /// <param name="leftPixel">左侧mark像素坐标</param>
        /// <param name="rightPixel">右侧mark像素坐标</param>
        /// <param name="translation">输出第二次到第一次的平移量</param>
        /// <param name="rotate">输出第二次到第一次的旋转量</param>
        /// <param name="leftRuler">左侧mark光栅坐标</param>
        /// <param name="rightRuler">右侧mark光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetCurPecInfo(Point leftPixel, Point rightPixel, out Point translation, out double rotate, out Point leftRuler, out Point rightRuler)
        {
            translation = new Point();
            rotate = 0;
            leftRuler = new Point();
            rightRuler = new Point();

            Errortype ret = Errortype.OK;

            ret = GetRulerByPix(_svaParamInfo.PecLeftItemName, _svaParamInfo.PecLeftRuler, leftPixel, out Point pecLeftCurRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            leftRuler = pecLeftCurRuler;

            ret = GetRulerByPix(_svaParamInfo.PecRightItemName, _svaParamInfo.PecRightRuler, rightPixel, out Point pecRightCurRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            rightRuler = pecRightCurRuler;

            //计算两组mark之间的平移旋转
            ret = CalcTR(_pecLeftBaseRuler, _pecRightBaseRuler, pecLeftCurRuler, pecRightCurRuler, out translation, out rotate);

            ////new alignment
            //Point deltaLeft = leftPixel - _pecLeftBasePixel;
            //Point deltaRight = rightPixel - _pecRightBasePixel;
            //ret = GetRulerByPix(_svaParamInfo._pecLeftItemName, _svaParamInfo._pecLeftRuler, deltaLeft + _svaParamInfo._imgCenter, out Point deltaLeftRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaLeftRuler = deltaLeftRuler - _svaParamInfo._pecLeftRuler;
            //ret = GetRulerByPix(_svaParamInfo._pecRightItemName, _svaParamInfo._pecRightRuler, deltaRight + _svaParamInfo._imgCenter, out Point deltaRightRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaRightRuler = deltaRightRuler - _svaParamInfo._pecRightRuler;

            //translation = (deltaLeftRuler + deltaRightRuler) / -2;
            //rotate = Math.Atan((deltaRightRuler.Y - deltaLeftRuler.Y) / (_svaParamInfo._pecRightRuler.X - _svaParamInfo._pecLeftRuler.X)) / Math.PI * 180;
            if (ComAlgo.SaveFlg("PECInfo", out int days))
            {
                string path = @"D:\Alg\PecInfo";
                string fileName = "PecInfo.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string text = time + " SetCurPecInfo " +
                              _pecLeftBasePixel.ToString(sep) + sep + _pecRightBasePixel.ToString(sep) + sep +
                              leftPixel.ToString(sep) + sep + rightPixel.ToString(sep) + sep +
                              _pecLeftBaseRuler.ToString(sep) + sep + _pecRightBaseRuler.ToString(sep) + sep +
                              pecLeftCurRuler.ToString(sep) + sep + pecRightCurRuler.ToString(sep) + sep +
                              translation.ToString(sep) + sep + rotate.ToString();
                ComAlgo.LogText(text, path, fileName, days);
            }

            return ret;
        }

        /// <summary>
        /// calc art error
        /// </summary>
        /// <param name="data">art data</param>
        /// <param name="errorXy">error xy</param>
        /// <param name="errorT">error t</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcArtError(AlignmentRepeatabilityTestData data, out Point errorXy, out double errorT)
        {
            Errortype ret = Errortype.OK;
            Point bottomWaferInspectXy = (data.BottomWaferLeftMarkInspectXy + data.BottomWaferRightMarkInspectXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.BottomWaferLeftMarkInspectXy, data.BottomWaferRightMarkInspectXy, out Point bottomWaferInspectTranslation, out double bottomWaferInspectT);
            bottomWaferInspectT = bottomWaferInspectT / 180 * Math.PI;

            Point topWaferInspectXy = (data.TopWaferLeftMarkInspectXy + data.TopWaferRightMarkInspectXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.TopWaferLeftMarkInspectXy, data.TopWaferRightMarkInspectXy, out Point topWaferInspectTranslation, out double topWaferInspectT);
            topWaferInspectT = topWaferInspectT / 180 * Math.PI;

            errorXy = (topWaferInspectXy - bottomWaferInspectXy) * 1e3; //mm to um
            errorT = (topWaferInspectT - bottomWaferInspectT) * 1e6; //rad to urad

            errorXy *= -1; //XY结果取反用于误差直观显示
            return Errortype.OK;
        }
        #endregion
    }

    /// <summary>
    /// 键合数据记录
    /// </summary>
    public static class FusionRecord
    {
        private static string _sep = " ";
        private static string _format = "f6";

        /// <summary>
        /// 获取文件中当前插入第几行
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static int GetLineID(string filename)
        {
            if (!File.Exists(filename))
            {
                return 0;
            }

            var text = File.ReadAllLines(filename);
            return text.Length;
        }

        /// <summary>
        /// 记录同轴标定数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="data">数据源</param>
        /// <param name="calibrationType">标定类型</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibrateTopToBottomOpticsRecord(string fileName, CalibrateTopToBottomOpticsData data, string calibrationType)
        {
            if (fileName is null)
            {
                return Errortype.FUSION_RECORD_FILENAME_NULL;
            }

            if (fileName == string.Empty)
            {
                return Errortype.FUSION_RECORD_FILENAME_EMPTY;
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            if (calibrationType is null)
            {
                calibrationType = string.Empty;
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID Type OpticLeftTopSearchX OpticLeftTopSearchY OpticLeftBottomSearchX OpticLeftBottomSearchY " +
                               "OpticRightTopSearchX OpticRightTopSearchY OpticRightBottomSearchX OpticRightBottomSearchY " +
                               "OpticLeftX OpticLeftY OpticRightX OpticRightY";
                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            Point opticLeftXy = data.LeftTopXy - data.LeftBottomXy;
            Point opticRightXy = data.RightTopXy - data.RightBottomXy;

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep +
                          id.ToString() + _sep +
                          calibrationType + _sep +
                          (data.LeftTopXy * 1e6).ToString(_sep) + _sep +
                          (data.LeftBottomXy * 1e6).ToString(_sep) + _sep +
                          (data.RightTopXy * 1e6).ToString(_sep) + _sep +
                          (data.RightBottomXy * 1e6).ToString(_sep) + _sep +
                          (opticLeftXy * 1e6).ToString(_sep) + _sep +
                          (opticRightXy * 1e6).ToString(_sep);
            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 记录微动平台增益标定数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="dataFirst">第一次数据</param>
        /// <param name="dataSecond">第二次数据</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibrateNanoStageRecord(string fileName, CalibrateNanoStageData dataFirst, CalibrateNanoStageData dataSecond)
        {
            if (fileName is null)
            {
                return Errortype.FUSION_RECORD_FILENAME_NULL;
            }

            if (fileName == string.Empty)
            {
                return Errortype.FUSION_RECORD_FILENAME_EMPTY;
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID OpticLeftSearchX1st OpticLeftSearchY1st OpticRightSearchX1st OpticRightSearchY1st GaugeX1st GaugeY1st GaugeT1st " +
                    "OpticLeftSearchX2st OpticLeftSearchY2st OpticRightSearchX2st OpticRightSearchY2st GaugeX2st GaugeY2st GaugeT2st " +
                    "OpticT1st OpticT2nd OpticXDistance OpticYDistance OpticTDistance GaugeXDistance GaugeYDistance GaugeTDistance GainX GainY GainT";
                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            Errortype ret = FusionManagerSimplifyCalib.CalcLXTR(dataFirst.LeftMarkXy, dataFirst.RightMarkXy, out Point translationFirst, out double opticTFirst);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = FusionManagerSimplifyCalib.CalcLXTR(dataSecond.LeftMarkXy, dataSecond.RightMarkXy, out Point translationSecond, out double opticTSecond);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            Point opticXYDistance = translationSecond - translationFirst;
            double opticTDistance = opticTSecond - opticTFirst;
            Point gaugeXYDistance = dataSecond.GaugeXy - dataFirst.GaugeXy;
            double gaugeTDistance = dataSecond.GaugeT - dataFirst.GaugeT;
            double gainX = opticXYDistance.X / gaugeXYDistance.X;
            double gainY = opticXYDistance.Y / gaugeXYDistance.Y;
            double gainT = gaugeTDistance / opticTDistance;

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep +
                          id.ToString() + _sep +
                          (dataFirst.LeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.RightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.GaugeXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.GaugeT * 1e6).ToString(_format) + _sep +
                          (dataSecond.LeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.RightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.GaugeXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.GaugeT * 1e6).ToString(_format) + _sep +
                          (opticTFirst * 1e6).ToString(_format) + _sep +
                          (opticTSecond * 1e6).ToString(_format) + _sep +
                          (opticXYDistance * 1e6).ToString(_sep) + _sep +
                          (opticTDistance * 1e6).ToString(_format) + _sep +
                          (gaugeXYDistance * 1e6).ToString(_sep) + _sep +
                          (gaugeTDistance * 1e6).ToString(_format) + _sep +
                          gainX.ToString(_format) + _sep +
                          gainY.ToString(_format) + _sep +
                          gainT.ToString(_format);
            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 记录bottom table 重复性测试数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="dataFirst">第一次数据</param>
        /// <param name="dataSecond">第二次数据</param>
        /// <param name="dataPost">dataPost</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype BottomTableRepeatabilityTestRecord(string fileName, BottomTableData dataFirst, BottomTableData dataSecond, BottomTableData dataPost)
        {
            if (fileName is null)
            {
                return Errortype.FUSION_RECORD_FILENAME_NULL;
            }

            if (fileName == string.Empty)
            {
                return Errortype.FUSION_RECORD_FILENAME_EMPTY;
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID LeftPecX1st LeftPecY1st RightPecX1st RightPecY1st " +
                               "OpticLeftX1st OpticLeftY1st OpticRightX1st OpticRightY1st " +
                               "BottomTableY01st BottomTableY11st BottomTableX01st BottomTableX11st " +
                               "LeftPecX2nd LeftPecY2nd RightPecX2nd RightPecY2nd " +
                               "OpticLeftX2nd OpticLeftY2nd OpticRightX2nd OpticRightY2nd " +
                               "BottomTableY02nd BottomTableY12nd BottomTableX02nd BottomTableX12nd " +
                               "LeftPecXPost LeftPecYPost RightPecXPost RightPecYPost " +
                               "OpticLeftXPost OpticLeftYPost OpticRightXPost OpticRightYPost " +
                               "BottomTableY0Post BottomTableY1Post BottomTableX0Post BottomTableX1Post " +
                               "PecX1st PecY1st PecT1st PecXPost PecYPost PecTPost PecXError PecYError PecTError " +
                               "LeftPecXError LeftPecYError RightPecXError RightPecYError " +
                               "OpticX1st OpticY1st OpticT1st OpticXPost OpticYPost OpticTPost OpticXError OpticYError OpticTError " +
                               "OpticLeftXError OpticLeftYError OpticRightXError OpticRightYError " +
                               "BottomTableY0Error BottomTableY1Error BottomTableX0Error BottomTableX1Error " +
                               "OpticVSPecLeftXError OpticVSPecLeftYError OpticVSPecRightXError OpticVSPecRightYError " +
                               "OpticVSPecXError OpticVSPecYError OpticVSPecTError";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            Point pecXyFirst = (dataFirst.PecLeftMarkXy + dataFirst.PecRightMarkXy) / 2;
            Errortype ret = FusionManagerSimplifyCalib.CalcLXTR(dataFirst.PecLeftMarkXy, dataFirst.PecRightMarkXy, out Point pecTranslationFirst, out double pecTFirst);
            pecTFirst = pecTFirst / 180 * Math.PI;

            Point pecXyPost = (dataPost.PecLeftMarkXy + dataPost.PecRightMarkXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(dataPost.PecLeftMarkXy, dataPost.PecRightMarkXy, out Point pecTranslationPost, out double pecTPost);
            pecTPost = pecTPost / 180 * Math.PI;

            Point pecXyError = pecXyPost - pecXyFirst;
            double pecTError = pecTPost - pecTFirst;
            Point pecLeftError = dataPost.PecLeftMarkXy - dataFirst.PecLeftMarkXy;
            Point pecRightError = dataPost.PecRightMarkXy - dataFirst.PecRightMarkXy;

            Point opticXyFirst = (dataFirst.OpticLeftMarkXy + dataFirst.OpticRightMarkXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(dataFirst.OpticLeftMarkXy, dataFirst.OpticRightMarkXy, out Point opticTranslationFirst, out double opticTFirst);
            opticTFirst = opticTFirst / 180 * Math.PI;

            Point opticXyPost = (dataPost.OpticLeftMarkXy + dataPost.OpticRightMarkXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(dataPost.OpticLeftMarkXy, dataPost.OpticRightMarkXy, out Point opticTranslationPost, out double opticTPost);
            opticTPost = opticTPost / 180 * Math.PI;

            Point opticXyError = opticXyPost - opticXyFirst;
            double opticTError = opticTPost - opticTFirst;
            Point opticLeftError = dataPost.OpticLeftMarkXy - dataFirst.OpticLeftMarkXy;
            Point opticRightError = dataPost.OpticRightMarkXy - dataFirst.OpticRightMarkXy;

            double bottomTableX0Error = dataPost.BottomTableX0 - dataFirst.BottomTableX0;
            double bottomTableX1Error = dataPost.BottomTableX1 - dataFirst.BottomTableX1;
            double bottomTableY0Error = dataPost.BottomTableY0 - dataFirst.BottomTableY0;
            double bottomTableY1Error = dataPost.BottomTableY1 - dataFirst.BottomTableY1;

            Point opticVsPecLeftError = opticLeftError - pecLeftError;
            Point opticVsPecRightError = opticRightError - pecRightError;
            Point opticVsPecXyError = opticXyError - pecXyError;
            double opticVsPecTError = opticTError - pecTError;

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep +
                          id.ToString() + _sep +
                          (dataFirst.PecLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.PecRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.OpticLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.OpticRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.BottomTableY0 * 1e6).ToString(_format) + _sep +
                          (dataFirst.BottomTableY1 * 1e6).ToString(_format) + _sep +
                          (dataFirst.BottomTableX0 * 1e6).ToString(_format) + _sep +
                          (dataFirst.BottomTableX1 * 1e6).ToString(_format) + _sep +
                          (dataSecond.PecLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.PecRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.OpticLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.OpticRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.BottomTableY0 * 1e6).ToString(_format) + _sep +
                          (dataSecond.BottomTableY1 * 1e6).ToString(_format) + _sep +
                          (dataSecond.BottomTableX0 * 1e6).ToString(_format) + _sep +
                          (dataSecond.BottomTableX1 * 1e6).ToString(_format) + _sep +
                          (dataPost.PecLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataPost.PecRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataPost.OpticLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataPost.OpticRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataPost.BottomTableY0 * 1e6).ToString(_format) + _sep +
                          (dataPost.BottomTableY1 * 1e6).ToString(_format) + _sep +
                          (dataPost.BottomTableX0 * 1e6).ToString(_format) + _sep +
                          (dataPost.BottomTableX1 * 1e6).ToString(_format) + _sep +
                          (pecXyFirst * 1e6).ToString(_sep) + _sep +
                          (pecTFirst * 1e6).ToString(_format) + _sep +
                          (pecXyPost * 1e6).ToString(_sep) + _sep +
                          (pecTPost * 1e6).ToString(_format) + _sep +
                          (pecXyError * 1e6).ToString(_sep) + _sep +
                          (pecTError * 1e6).ToString(_format) + _sep +
                          (pecLeftError * 1e6).ToString(_sep) + _sep +
                          (pecRightError * 1e6).ToString(_sep) + _sep +
                          (opticXyFirst * 1e6).ToString(_sep) + _sep +
                          (opticTFirst * 1e6).ToString(_format) + _sep +
                          (opticXyPost * 1e6).ToString(_sep) + _sep +
                          (opticTPost * 1e6).ToString(_format) + _sep +
                          (opticXyError * 1e6).ToString(_sep) + _sep +
                          (opticTError * 1e6).ToString(_format) + _sep +
                          (opticLeftError * 1e6).ToString(_sep) + _sep +
                          (opticRightError * 1e6).ToString(_sep) + _sep +
                          (bottomTableY0Error * 1e6).ToString(_format) + _sep +
                          (bottomTableY1Error * 1e6).ToString(_format) + _sep +
                          (bottomTableX0Error * 1e6).ToString(_format) + _sep +
                          (bottomTableX1Error * 1e6).ToString(_format) + _sep +
                          (opticVsPecLeftError * 1e6).ToString(_sep) + _sep +
                          (opticVsPecRightError * 1e6).ToString(_sep) + _sep +
                          (opticVsPecXyError * 1e6).ToString(_sep) + _sep +
                          (opticVsPecTError * 1e6).ToString(_format);

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 记录对准重复性测试数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="data">数据</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AlignmentRepeatabilityTestRecord(string fileName, AlignmentRepeatabilityTestData data)
        {
            if (fileName is null)
            {
                return Errortype.FUSION_RECORD_FILENAME_NULL;
            }

            if (fileName == string.Empty)
            {
                return Errortype.FUSION_RECORD_FILENAME_EMPTY;
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID OpticLeftTopSearchX(calibrationMark) OpticLeftTopSearchY(calibrationMark) OpticLeftBottomSearchX(calibrationMark) OpticLeftBottomSearchY(calibrationMark) " +
                               "OpticRightTopSearchX(calibrationMark) OpticRightTopSearchY(calibrationMark) OpticRightBottomSearchX(calibrationMark) OpticRightBottomSearchY(calibrationMark) " +
                               "OpticLeftTopSearchX(calibrationMarkSecond) OpticLeftTopSearchY(calibrationMarkSecond) OpticLeftBottomSearchX(calibrationMarkSecond) OpticLeftBottomSearchY(calibrationMarkSecond) " +
                               "OpticRightTopSearchX(calibrationMarkSecond) OpticRightTopSearchY(calibrationMarkSecond) OpticRightBottomSearchX(calibrationMarkSecond) OpticRightBottomSearchY(calibrationMarkSecond) " +
                               "BottomTableX01st BottomTableX11st BottomTableY01st BottomTableY11st " +
                               "BottomWaferLeftMarkX BottomWaferLeftMarkY BottomWaferRightMarkX BottomWaferRightMarkY " +
                               "LeftOpticX LeftOpticY LeftOpticZ " +
                               "RightOpticX RightOpticY RightOpticZ " +
                               "LeftPecX1st LeftPecY1st RightPecX1st RightPecY1st " +
                               "LeftPecZ RightPecZ " +
                               "TopTableX0 TopTableX1 TopTableY0 TopTableY1 " +
                               "TopNanoStageX TopNanoStageY TopNanoStageT " +
                               "TopWaferLeftMarkX TopWaferLeftMarkY TopWaferRightMarkX TopWaferRightMarkY " +
                               "LeftOpticAfterTopAlignmentX LeftOpticAfterTopAlignmentY LeftOpticAfterTopAlignmentZ " +
                               "RightOpticAfterTopAlignmentX RightOpticAfterTopAlignmentY RightOpticAfterTopAlignmentZ " +
                               "BottomTableX02nd BottomTableX12nd BottomTableY02nd BottomTableY12nd " +
                               "LeftPecX2nd LeftPecY2nd RightPecX2nd RightPecY2nd " +
                               "LeftPecXPost LeftPecYPost RightPecXPost RightPecYPost " +
                               "BottomNanoStageX BottomNanoStageY BottomNanoStageT " +
                               "BottomWaferLeftMarkXInspect BottomWaferLeftMarkYInspect BottomWaferRightMarkXInspect BottomWaferRightMarkYInspect " +
                               "LeftOpticAfterBottomWaferInspectX LeftOpticAfterBottomWaferInspectY LeftOpticAfterBottomWaferInspectZ " +
                               "RightOpticAfterBottomWaferInspectX RightOpticAfterBottomWaferInspectY RightOpticAfterBottomWaferInspectZ " +
                               "TopWaferLeftMarkXInspect TopWaferLeftMarkYInspect TopWaferRightMarkXInspect TopWaferRightMarkYInspect " +
                               "BottomWaferLeftMarkNoGlassXInspect BottomWaferLeftMarkNoGlassYInspect BottomWaferRightMarkNoGlassXInspect BottomWaferRightMarkNoGlassYInspect " +
                               "LeftOpticAfterTopWaferInspectX LeftOpticAfterTopWaferInspectY LeftOpticAfterTopWaferInspectZ " +
                               "RightOpticAfterTopWaferInspectX RightOpticAfterTopWaferInspectY RightOpticAfterTopWaferInspectZ " +
                               "OffsetX OffsetY OffsetT " +
                               "LeftOpticXError(Concentricity) LeftOpticYError(Concentricity) RightOpticXError(Concentricity) RightOpticYError(Concentricity) " +
                               "LeftOpticXError(ConcentricitySecond) LeftOpticYError(ConcentricitySecond) RightOpticXError(ConcentricitySecond) RightOpticYError(ConcentricitySecond) " +
                               "BottomTableX BottomTableY BottomTableT " +
                               "LeftBottomWaferPixelPosX LeftBottomWaferPixelPosY RightBottomWaferPixelPosX RightBottomWaferPixelPosY " +
                               "BottomWaferX BottomWaferY BottomWaferT " +
                               "BottomPecX BottomPecY BottomPecT " +
                               "TopTableX TopTableY TopTableT " +
                               "LeftTopWaferPixelPosX LeftTopWaferPixelPosY RightTopWaferPixelPosX RightTopWaferPixelPosY " +
                               "TopWaferX TopWaferY TopWaferT " +
                               "BottomTableX2nd BottomTableY2nd BottomTableT2nd " +
                               "BottomPecX2nd BottomPecY2nd BottomPecT2nd " +
                               "BottomPecXPost BottomPecYPost BottomPecTPost " +
                               "BottomWaferXInspect BottomWaferYInspect BottomWaferTInspect " +
                               "TopWaferXInspect TopWaferYInspect TopWaferTInspect " +
                               "BottomWaferNoGlassXInspect BottomWaferNoGlassYInspect BottomWaferNoGlassTInspect " +
                               "XInspect YInspect TInspect " +
                               "XNoGlassInspect YNoGlassInspect TNoGlassInspect";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            Point leftOpticConcentricityErrorXy = data.OpticLeftTopCalibrationMarkXy - data.OpticLeftBottomCalibrationMarkXy;
            Point rightOpticConcentricityErrorXy = data.OpticRightTopCalibrationMarkXy - data.OpticRightBottomCalibrationMarkXy;

            Point leftOpticConcentricityErrorXySecond = data.OpticLeftTopCalibrationMarkXySecond - data.OpticLeftBottomCalibrationMarkXySecond;
            Point rightOpticConcentricityErrorXySecond = data.OpticRightTopCalibrationMarkXySecond - data.OpticRightBottomCalibrationMarkXySecond;

            double bottomTableXFirst = (data.BottomTableDataFirst.BottomTableX0 + data.BottomTableDataFirst.BottomTableX1) / 2;
            double bottomTableYFirst = (data.BottomTableDataFirst.BottomTableY0 + data.BottomTableDataFirst.BottomTableY1) / 2;
            double bottomTableTFirst = Math.Atan((data.BottomTableDataFirst.BottomTableX0 - data.BottomTableDataFirst.BottomTableX1) / 456);

            Point bottomWaferLeftPixelPos = data.BottomTableDataFirst.OpticLeftMarkXy - new Point(data.OpticLeftXyz.X, data.OpticLeftXyz.Y);
            Point bottomWaferRightPixelPos = data.BottomTableDataFirst.OpticRightMarkXy - new Point(data.OpticRightXyz.X, data.OpticRightXyz.Y);

            Point bottomWaferXy = (data.BottomTableDataFirst.OpticLeftMarkXy + data.BottomTableDataFirst.OpticRightMarkXy) / 2;
            Errortype ret = FusionManagerSimplifyCalib.CalcLXTR(data.BottomTableDataFirst.OpticLeftMarkXy, data.BottomTableDataFirst.OpticRightMarkXy, out Point bottomWaferTranslation, out double bottomWaferT);
            bottomWaferT = bottomWaferT / 180 * Math.PI;

            Point bottomPecXyFirst = (data.BottomTableDataFirst.PecLeftMarkXy + data.BottomTableDataFirst.PecRightMarkXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.BottomTableDataFirst.PecLeftMarkXy, data.BottomTableDataFirst.PecRightMarkXy, out Point bottomPecTranslationFirst, out double bottomPecTFirst);
            bottomPecTFirst = bottomPecTFirst / 180 * Math.PI;

            Point topWaferLeftPixelPos = data.TopWaferLeftMarkXy - new Point(data.OpticLeftXyzAfterAlignment.X, data.OpticLeftXyzAfterAlignment.Y);
            Point topWaferRightPixelPos = data.TopWaferRightMarkXy - new Point(data.OpticRightXyzAfterAlignment.X, data.OpticRightXyzAfterAlignment.Y);

            double topTableX = (data.TopTableX0 + data.TopTableX1) / 2;
            double topTableY = (data.TopTableY0 + data.TopTableY1) / 2;
            double topTableT = Math.Atan((data.TopTableX0 - data.TopTableX1) / 508);

            Point topWaferXy = (data.TopWaferLeftMarkXy + data.TopWaferRightMarkXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.TopWaferLeftMarkXy, data.TopWaferRightMarkXy, out Point topWaferTranslation, out double topWaferT);
            topWaferT = topWaferT / 180 * Math.PI;

            double bottomTableXSecond = (data.BottomTableDataSecond.BottomTableX0 + data.BottomTableDataSecond.BottomTableX1) / 2;
            double bottomTableYSecond = (data.BottomTableDataSecond.BottomTableY0 + data.BottomTableDataSecond.BottomTableY1) / 2;
            double bottomTableTSecond = Math.Atan((data.BottomTableDataSecond.BottomTableX0 - data.BottomTableDataSecond.BottomTableX1) / 456);

            Point bottomPecXySecond = (data.BottomTableDataSecond.PecLeftMarkXy + data.BottomTableDataSecond.PecRightMarkXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.BottomTableDataSecond.PecLeftMarkXy, data.BottomTableDataSecond.PecRightMarkXy, out Point bottomPecTranslationSecond, out double bottomPecTSecond);
            bottomPecTSecond = bottomPecTSecond / 180 * Math.PI;

            Point pecPostXy = (data.BottomTableDataPost.PecLeftMarkXy + data.BottomTableDataPost.PecRightMarkXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.BottomTableDataPost.PecLeftMarkXy, data.BottomTableDataPost.PecRightMarkXy, out Point pecPostTranslation, out double pecPostT);
            pecPostT = pecPostT / 180 * Math.PI;

            Point bottomWaferInspectXy = (data.BottomWaferLeftMarkInspectXy + data.BottomWaferRightMarkInspectXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.BottomWaferLeftMarkInspectXy, data.BottomWaferRightMarkInspectXy, out Point bottomWaferInspectTranslation, out double bottomWaferInspectT);
            bottomWaferInspectT = bottomWaferInspectT / 180 * Math.PI;

            Point bottomWaferNoGlassInspectXy = (data.BottomWaferLeftMarkInspectNoGlassXy + data.BottomWaferRightMarkInspectNoGlassXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.BottomWaferLeftMarkInspectNoGlassXy, data.BottomWaferRightMarkInspectNoGlassXy, out Point bottomWaferNoGlassInspectTranslation, out double bottomWaferNoGlassInspectT);
            bottomWaferNoGlassInspectT = bottomWaferNoGlassInspectT / 180 * Math.PI;

            Point topWaferInspectXy = (data.TopWaferLeftMarkInspectXy + data.TopWaferRightMarkInspectXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.TopWaferLeftMarkInspectXy, data.TopWaferRightMarkInspectXy, out Point topWaferInspectTranslation, out double topWaferInspectT);
            topWaferInspectT = topWaferInspectT / 180 * Math.PI;

            Point inspectXy = topWaferInspectXy - bottomWaferInspectXy;
            double inspectT = topWaferInspectT - bottomWaferInspectT;

            inspectXy *= -1; //XY结果取反用于误差直观显示

            Point inspectNoGlassXy = topWaferInspectXy - bottomWaferNoGlassInspectXy;
            double inspectNoGlassT = topWaferInspectT - bottomWaferNoGlassInspectT;

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep +
                          id.ToString() + _sep +
                          (data.OpticLeftTopCalibrationMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.OpticLeftBottomCalibrationMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.OpticRightTopCalibrationMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.OpticRightBottomCalibrationMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.OpticLeftTopCalibrationMarkXySecond * 1e6).ToString(_sep) + _sep +
                          (data.OpticLeftBottomCalibrationMarkXySecond * 1e6).ToString(_sep) + _sep +
                          (data.OpticRightTopCalibrationMarkXySecond * 1e6).ToString(_sep) + _sep +
                          (data.OpticRightBottomCalibrationMarkXySecond * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataFirst.BottomTableX0 * 1e6).ToString(_format) + _sep +
                          (data.BottomTableDataFirst.BottomTableX1 * 1e6).ToString(_format) + _sep +
                          (data.BottomTableDataFirst.BottomTableY0 * 1e6).ToString(_format) + _sep +
                          (data.BottomTableDataFirst.BottomTableY1 * 1e6).ToString(_format) + _sep +
                          (data.BottomTableDataFirst.OpticLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataFirst.OpticRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.OpticLeftXyz * 1e6).ToString(_sep) + _sep +
                          (data.OpticRightXyz * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataFirst.PecLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataFirst.PecRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataFirst.PecLeftZ * 1e6).ToString(_format) + _sep + (data.BottomTableDataFirst.PecRightZ * 1e6).ToString(_format) + _sep +
                          (data.TopTableX0 * 1e6).ToString(_format) + _sep +
                          (data.TopTableX1 * 1e6).ToString(_format) + _sep +
                          (data.TopTableY0 * 1e6).ToString(_format) + _sep +
                          (data.TopTableY1 * 1e6).ToString(_format) + _sep +
                          (data.TopNanoStageXy * 1e6).ToString(_sep) + _sep +
                          (data.TopNanoStageT * 1e6).ToString(_format) + _sep +
                          (data.TopWaferLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.TopWaferRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.OpticLeftXyzAfterAlignment * 1e6).ToString(_sep) + _sep + (data.OpticRightXyzAfterAlignment * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataSecond.BottomTableX0 * 1e6).ToString(_format) + _sep +
                          (data.BottomTableDataSecond.BottomTableX1 * 1e6).ToString(_format) + _sep +
                          (data.BottomTableDataSecond.BottomTableY0 * 1e6).ToString(_format) + _sep +
                          (data.BottomTableDataSecond.BottomTableY1 * 1e6).ToString(_format) + _sep +
                          (data.BottomTableDataSecond.PecLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataSecond.PecRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataPost.PecLeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomTableDataPost.PecRightMarkXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomNanoStageXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomNanoStageT * 1e6).ToString(_format) + _sep +
                          (data.BottomWaferLeftMarkInspectXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomWaferRightMarkInspectXy * 1e6).ToString(_sep) + _sep +
                          (data.OpticLeftXyzAfterBottomWaferInspect * 1e6).ToString(_sep) + _sep + (data.OpticRightXyzAfterBottomWaferInspect * 1e6).ToString(_sep) + _sep +
                          (data.TopWaferLeftMarkInspectXy * 1e6).ToString(_sep) + _sep +
                          (data.TopWaferRightMarkInspectXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomWaferLeftMarkInspectNoGlassXy * 1e6).ToString(_sep) + _sep +
                          (data.BottomWaferRightMarkInspectNoGlassXy * 1e6).ToString(_sep) + _sep +
                          (data.OpticLeftXyzAfterTopWaferInspect * 1e6).ToString(_sep) + _sep + (data.OpticRightXyzAfterTopWaferInspect * 1e6).ToString(_sep) + _sep +
                          (data.OffsetXy * 1e3).ToString(_sep) + _sep +
                          data.OffsetT.ToString(_format) + _sep +
                          (leftOpticConcentricityErrorXy * 1e6).ToString(_sep) + _sep +
                          (rightOpticConcentricityErrorXy * 1e6).ToString(_sep) + _sep +
                          (leftOpticConcentricityErrorXySecond * 1e6).ToString(_sep) + _sep +
                          (rightOpticConcentricityErrorXySecond * 1e6).ToString(_sep) + _sep +
                          (bottomTableXFirst * 1e6).ToString(_format) + _sep +
                          (bottomTableYFirst * 1e6).ToString(_format) + _sep +
                          (bottomTableTFirst * 1e6).ToString(_format) + _sep +
                          (bottomWaferLeftPixelPos * 1e6).ToString(_sep) + _sep + (bottomWaferRightPixelPos * 1e6).ToString(_sep) + _sep +
                          (bottomWaferXy * 1e6).ToString(_sep) + _sep +
                          (bottomWaferT * 1e6).ToString(_format) + _sep +
                          (bottomPecXyFirst * 1e6).ToString(_sep) + _sep +
                          (bottomPecTFirst * 1e6).ToString(_format) + _sep +
                          (topTableX * 1e6).ToString(_format) + _sep +
                          (topTableY * 1e6).ToString(_format) + _sep +
                          (topTableT * 1e6).ToString(_format) + _sep +
                          (topWaferLeftPixelPos * 1e6).ToString(_sep) + _sep + (topWaferRightPixelPos * 1e6).ToString(_sep) + _sep +
                          (topWaferXy * 1e6).ToString(_sep) + _sep +
                          (topWaferT * 1e6).ToString(_format) + _sep +
                          (bottomTableXSecond * 1e6).ToString(_format) + _sep +
                          (bottomTableYSecond * 1e6).ToString(_format) + _sep +
                          (bottomTableTSecond * 1e6).ToString(_format) + _sep +
                          (bottomPecXySecond * 1e6).ToString(_sep) + _sep +
                          (bottomPecTSecond * 1e6).ToString(_format) + _sep +
                          (pecPostXy * 1e6).ToString(_sep) + _sep +
                          (pecPostT * 1e6).ToString(_format) + _sep +
                          (bottomWaferInspectXy * 1e6).ToString(_sep) + _sep +
                          (bottomWaferInspectT * 1e6).ToString(_format) + _sep +
                          (topWaferInspectXy * 1e6).ToString(_sep) + _sep +
                          (topWaferInspectT * 1e6).ToString(_format) + _sep +
                          (bottomWaferNoGlassInspectXy * 1e6).ToString(_sep) + _sep +
                          (bottomWaferNoGlassInspectT * 1e6).ToString(_format) + _sep +
                          (inspectXy * 1e6).ToString(_sep) + _sep +
                          (inspectT * 1e6).ToString(_format) + _sep +
                          (inspectNoGlassXy * 1e6).ToString(_sep) + _sep +
                          (inspectNoGlassT * 1e6).ToString(_format);

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 记录微动平台精度测试数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="dataFirst">第一次数据</param>
        /// <param name="dataSecond">第二次数据</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype NanoStageAccuracyTestRecord(string fileName, NanoStageAccuracyTestData dataFirst, NanoStageAccuracyTestData dataSecond)
        {
            if (fileName is null)
            {
                return Errortype.FUSION_RECORD_FILENAME_NULL;
            }

            if (fileName == string.Empty)
            {
                return Errortype.FUSION_RECORD_FILENAME_EMPTY;
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID WaferMarkLeftX1st WaferMarkLeftY1st WaferMarkRightX1st WaferMarkRightY1st " +
                    "NanoStageX1st NanoStageY1st NanoStageT1st " +
                    "WaferMarkLeftX2nd WaferMarkLeftY2nd WaferMarkRightX2nd WaferMarkRightY2nd " +
                    "NanoStageX2nd NanoStageY2nd NanoStageT2nd " +
                    "WaferX1st WaferY1st WaferT1st " +
                    "WaferX2nd WaferY2nd WaferT2nd " +
                    "WaferXDistance WaferYDistance WaferTDistance " +
                    "NanoStageXDistance NanoStageYDistance NanoStageTDistance " +
                    "ErrorX ErrorY ErrorT";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            Point waferXyFirst = (dataFirst.LeftWaferMarkXy + dataFirst.RightWaferMarkXy) / 2;
            Errortype ret = FusionManagerSimplifyCalib.CalcLXTR(dataFirst.LeftWaferMarkXy, dataFirst.RightWaferMarkXy, out Point waferTranslationFirst, out double waferTFirst);
            waferTFirst = waferTFirst / 180 * Math.PI;

            Point waferXySecond = (dataSecond.LeftWaferMarkXy + dataSecond.RightWaferMarkXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(dataSecond.LeftWaferMarkXy, dataSecond.RightWaferMarkXy, out Point waferTranslationSecond, out double waferTSecond);
            waferTSecond = waferTSecond / 180 * Math.PI;

            Point waferDistanceXy = waferXySecond - waferXyFirst;
            double waferDistanceT = waferTSecond - waferTFirst;

            Point nanoStageDistanceXy = dataSecond.NanoStageXy - dataFirst.NanoStageXy;
            double nanoStageDistanceT = dataSecond.NanoStageT - dataFirst.NanoStageT;

            Point errorXy = waferDistanceXy - nanoStageDistanceXy;
            double errorT = waferDistanceT - nanoStageDistanceT;

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep +
                          id.ToString() + _sep +
                          (dataFirst.LeftWaferMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.RightWaferMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.NanoStageXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.NanoStageT * 1e6).ToString(_format) + _sep +
                          (dataSecond.LeftWaferMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.RightWaferMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.NanoStageXy * 1e6).ToString(_sep) + _sep +
                          (dataSecond.NanoStageT * 1e6).ToString(_format) + _sep +
                          (waferXyFirst * 1e6).ToString(_sep) + _sep +
                          (waferTFirst * 1e6).ToString(_format) + _sep +
                          (waferXySecond * 1e6).ToString(_sep) + _sep +
                          (waferTSecond * 1e6).ToString(_format) + _sep +
                          (waferDistanceXy * 1e6).ToString(_sep) + _sep +
                          (waferDistanceT * 1e6).ToString(_format) + _sep +
                          (nanoStageDistanceXy * 1e6).ToString(_sep) + _sep +
                          (nanoStageDistanceT * 1e6).ToString(_format) + _sep +
                          (errorXy * 1e6).ToString(_sep) + _sep +
                          (errorT * 1e6).ToString(_format);

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 记录相机重复性数据
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="leftPixelPoints">左侧相机点集</param>
        /// <param name="rightPixelPoints">右侧相机点集</param>
        /// <param name="leftRulerPoints">左侧光栅点集</param>
        /// <param name="rightRulerPoints">右侧光栅点集</param>
        /// <param name="leftOpticXyz">左相机三维坐标</param>
        /// <param name="rightOpticXyz">右相机三维坐标</param>
        /// <param name="temperature">温度</param>
        /// <param name="bottomStageZ">下平台z坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CameraRepeatabilityTestRecord(string fileName, List<Point> leftPixelPoints, List<Point> rightPixelPoints, List<Point> leftRulerPoints, List<Point> rightRulerPoints, Point3D leftOpticXyz, Point3D rightOpticXyz, List<double> temperature, List<double> bottomStageZ)
        {
            if (fileName is null)
            {
                return Errortype.FUSION_RECORD_FILENAME_NULL;
            }

            if (fileName == string.Empty)
            {
                return Errortype.FUSION_RECORD_FILENAME_EMPTY;
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID ";

                title += "LeftRulerX LeftRulerY ";
                for (int i = 0; i < leftRulerPoints.Count; i++)
                {
                    title += "LeftRulerX" + i.ToString() + _sep + "LeftRulerY" + i.ToString() + _sep;
                }

                title += "LeftRulerSigmaX LeftRulerSigmaY ";
                title += "LeftOpticX LeftOpticY LeftOpticZ ";

                title += "RightRulerX RightRulerY ";
                for (int i = 0; i < rightRulerPoints.Count; i++)
                {
                    title += "RightRulerX" + i.ToString() + _sep + "RightRulerY" + i.ToString() + _sep;
                }

                title += "RightRulerSigmaX RightRulerSigmaY ";
                title += "RightOpticX RightOpticY RightOpticZ ";

                title += "LeftPixelX LeftPixelY ";
                for (int i = 0; i < leftPixelPoints.Count; i++)
                {
                    title += "LeftPixelX" + i.ToString() + _sep + "LeftPixelY" + i.ToString() + _sep;
                }

                title += "LeftPixelSigmaX LeftPixelSigmaY ";

                title += "RightPixelX RightPixelY ";
                for (int i = 0; i < rightPixelPoints.Count; i++)
                {
                    title += "RightPixelX" + i.ToString() + _sep + "RightPixelY" + i.ToString() + _sep;
                }

                title += "RightPixelSigmaX RightPixelSigmaY ";

                for (int i = 0; i < temperature.Count; i++)
                {
                    title += "temperature" + i.ToString() + _sep;
                }

                title += "BottomStageZ0 BottomStageZ1 BottomStageZ2" + _sep;

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            ComAlgo.CalcDataSummary(leftRulerPoints, out DataStatisticParam analysisLeftRulerX, out DataStatisticParam analysisLeftRulerY);
            ComAlgo.CalcDataSummary(rightRulerPoints, out DataStatisticParam analysisRightRulerX, out DataStatisticParam analysisRightRulerY);

            ComAlgo.CalcDataSummary(leftPixelPoints, out DataStatisticParam analysisLeftPixelX, out DataStatisticParam analysisLeftPixelY);
            ComAlgo.CalcDataSummary(rightPixelPoints, out DataStatisticParam analysisRightPixelX, out DataStatisticParam analysisRightPixelY);

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep + id.ToString() + _sep;

            text += (analysisLeftRulerX.Mean * 1e6).ToString("f3") + _sep + (analysisLeftRulerY.Mean * 1e6).ToString("f3") + _sep;
            for (int i = 0; i < leftRulerPoints.Count; i++)
            {
                text += (leftRulerPoints[i] * 1e6).ToString(_sep, 3) + _sep;
            }

            text += (analysisLeftRulerX.Sigma3 * 1e6).ToString("f3") + _sep + (analysisLeftRulerY.Sigma3 * 1e6).ToString("f3") + _sep;
            text += (leftOpticXyz * 1e6).ToString(_sep) + _sep;

            text += (analysisRightRulerX.Mean * 1e6).ToString("f3") + _sep + (analysisRightRulerY.Mean * 1e6).ToString("f3") + _sep;
            for (int i = 0; i < rightRulerPoints.Count; i++)
            {
                text += (rightRulerPoints[i] * 1e6).ToString(_sep, 3) + _sep;
            }

            text += (analysisRightRulerX.Sigma3 * 1e6).ToString("f3") + _sep + (analysisRightRulerY.Sigma3 * 1e6).ToString("f3") + _sep;
            text += (rightOpticXyz * 1e6).ToString(_sep) + _sep;

            text += (analysisLeftPixelX.Mean * 1e6).ToString("f3") + _sep + (analysisLeftPixelY.Mean * 1e6).ToString("f3") + _sep;
            for (int i = 0; i < leftPixelPoints.Count; i++)
            {
                text += (leftPixelPoints[i] * 1e6).ToString(_sep, 3) + _sep;
            }

            text += (analysisLeftPixelX.Sigma3 * 1e6).ToString("f3") + _sep + (analysisLeftPixelY.Sigma3 * 1e6).ToString("f3") + _sep;

            text += (analysisRightPixelX.Mean * 1e6).ToString("f3") + _sep + (analysisRightPixelY.Mean * 1e6).ToString("f3") + _sep;
            for (int i = 0; i < rightPixelPoints.Count; i++)
            {
                text += (rightPixelPoints[i] * 1e6).ToString(_sep, 3) + _sep;
            }

            text += (analysisRightPixelX.Sigma3 * 1e6).ToString("f3") + _sep + (analysisRightPixelY.Sigma3 * 1e6).ToString("f3") + _sep;

            for (int i = 0; i < temperature.Count; i++)
            {
                text += temperature[i].ToString("f3") + _sep;
            }

            for (int i = 0; i < bottomStageZ.Count; i++)
            {
                text += (bottomStageZ[i] * 1e6).ToString("f3") + _sep;
            }

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 微动台线性测试数据记录
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="dataFirstIn">第一次数据</param>
        /// <param name="dataCurrentIn">当前组数据</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype NanoStageLinearTestRecord(string fileName, NanoStageLinearTestData dataFirstIn, NanoStageLinearTestData dataCurrentIn)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID " +
                               "LeftOpticFirstX LeftOpticFirstY LeftOpticFirstZ " +
                               "LeftMarkFirstX LeftMarkFirstY " +
                               "RightOpticFirstX RightOpticFirstY RightOpticFirstZ " +
                               "RightMarkFirstX RightMarkFirstY " +
                               "NanoStageTheoryFirstX NanoStageTheoryFirstY NanoStageTheoryFirstT " +
                               "NanoStageActualFirstX NanoStageActualFirstY NanoStageActualFirstT " +
                               "LeftOpticCurrentX LeftOpticCurrentY LeftOpticCurrentZ " +
                               "LeftMarkCurrentX LeftMarkCurrentY " +
                               "RightOpticCurrentX RightOpticCurrentY RightOpticCurrentZ " +
                               "RightMarkCurrentX RightMarkCurrentY " +
                               "NanoStageTheoryCurrentX NanoStageTheoryCurrentY NanoStageTheoryCurrentT " +
                               "NanoStageActualCurrentX NanoStageActualCurrentY NanoStageActualCurrentT " +
                               "WaferFirstX WaferFirstY WaferFirstT " +
                               "WaferCurrentX WaferCurrentY WaferCurrentT " +
                               "WaferDistX WaferDistY WaferDistXY WaferDistT " +
                               "NanoStageTheoryDistX NanoStageTheoryDistY NanoStageTheoryDistXY NanoStageTheoryDistT " +
                               "ErrorX ErrorY ErrorXY ErrorT ";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            // nano stage T 传入的单位是毫弧度 要转到弧度 
            NanoStageLinearTestData dataFirst = dataFirstIn.Clone();
            NanoStageLinearTestData dataCurrent = dataCurrentIn.Clone();

            dataFirst.NanoTheoryT /= 1000;
            dataFirst.NanoActualT /= 1000;
            dataCurrent.NanoTheoryT /= 1000;
            dataCurrent.NanoActualT /= 1000;

            Errortype ret = FusionManagerSimplifyCalib.CalcLXTR(dataFirst.LeftMarkXy, dataFirst.RightMarkXy, out Point markFirstTranslation, out double waferFirstT);
            waferFirstT = waferFirstT / 180 * Math.PI;

            ret = FusionManagerSimplifyCalib.CalcLXTR(dataCurrent.LeftMarkXy, dataCurrent.RightMarkXy, out Point markCurrentTranslation, out double waferCurrentT);
            waferCurrentT = waferCurrentT / 180 * Math.PI;

            Point waferCenterFirst = (dataFirst.LeftMarkXy + dataFirst.RightMarkXy) / 2;
            Point waferCenterCurrent = (dataCurrent.LeftMarkXy + dataCurrent.RightMarkXy) / 2;

            Point waferDist = waferCenterCurrent - waferCenterFirst;
            double waferDistXy = ComAlgo.Dist(waferCenterCurrent, waferCenterFirst);
            double waferDistT = waferCurrentT - waferFirstT;

            Point nanoStageDist = dataCurrent.NanoTheoryXy - dataFirst.NanoTheoryXy;
            double nanoStageDistXy = ComAlgo.Dist(dataFirst.NanoTheoryXy, dataCurrent.NanoTheoryXy);
            double nanoStageDistT = dataCurrent.NanoTheoryT - dataFirst.NanoTheoryT;

            Point error = waferDist - nanoStageDist;
            double errorXy = waferDistXy - nanoStageDistXy;
            double errorT = waferDistT - nanoStageDistT;

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep +
                          id.ToString() + _sep +
                          (dataFirst.LeftOpticXyz * 1e6).ToString(_sep) + _sep +
                          (dataFirst.LeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.RightOpticXyz * 1e6).ToString(_sep) + _sep +
                          (dataFirst.RightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.NanoTheoryXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.NanoTheoryT * 1e3).ToString(_format) + _sep +
                          (dataFirst.NanoActualXy * 1e6).ToString(_sep) + _sep +
                          (dataFirst.NanoActualT * 1e6).ToString(_format) + _sep +
                          (dataCurrent.LeftOpticXyz * 1e6).ToString(_sep) + _sep +
                          (dataCurrent.LeftMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataCurrent.RightOpticXyz * 1e6).ToString(_sep) + _sep +
                          (dataCurrent.RightMarkXy * 1e6).ToString(_sep) + _sep +
                          (dataCurrent.NanoTheoryXy * 1e6).ToString(_sep) + _sep +
                          (dataCurrent.NanoTheoryT * 1e6).ToString(_format) + _sep +
                          (dataCurrent.NanoActualXy * 1e6).ToString(_sep) + _sep +
                          (dataCurrent.NanoActualT * 1e6).ToString(_format) + _sep +
                          (waferCenterFirst * 1e6).ToString(_sep) + _sep +
                          (waferFirstT * 1e6).ToString(_format) + _sep +
                          (waferCenterCurrent * 1e6).ToString(_sep) + _sep +
                          (waferCurrentT * 1e6).ToString(_format) + _sep +
                          (waferDist * 1e6).ToString(_sep) + _sep +
                          (waferDistXy * 1e6).ToString(_format) + _sep +
                          (waferDistT * 1e6).ToString(_format) + _sep +
                          (nanoStageDist * 1e6).ToString(_sep) + _sep +
                          (nanoStageDistXy * 1e6).ToString(_format) + _sep +
                          (nanoStageDistT * 1e6).ToString(_format) + _sep +
                          (error * 1e6).ToString(_sep) + _sep +
                          (errorXy * 1e6).ToString(_format) + _sep +
                          (errorT * 1e6).ToString(_format) + _sep;

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 计算bow值
        /// </summary>
        /// <param name="laserDist">激光位移数据</param>
        /// <param name="bow">bow值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcBow(List<double> laserDist, out double bow)
        {
            bow = 0;

            double[] xLine = new double[2] { 0, laserDist.Count - 1 };
            double[] yLine = new double[2] { laserDist[0], laserDist[laserDist.Count - 1] };

            HOperatorSet.GenContourPolygonXld(out HObject contour, yLine, xLine);
            HOperatorSet.FitLineContourXld(contour, "regression", -1, 0, 5, 2,
                out HTuple rowBegin, out HTuple colBegin, out HTuple rowEnd, out HTuple colEnd, out HTuple nr, out HTuple nc, out HTuple dist);

            LineSeg line = new LineSeg();
            line.Start_X = colBegin;
            line.Start_Y = rowBegin;
            line.End_X = colEnd;
            line.End_Y = rowEnd;
            double[] straightnessError = null;

            if (line.End_X != line.Start_X)
            {
                // ax+by+c = 0   dist = |ax+by+c| / sqrt(a^2+b^2)
                double a = (line.End_Y - line.Start_Y) / (line.End_X - line.Start_X);
                double b = -1;
                double c = line.Start_Y - a * line.Start_X;

                straightnessError = new double[laserDist.Count];
                for (int i = 0; i < laserDist.Count; i++)
                {
                    straightnessError[i] = Math.Abs(a * i + b * laserDist[i] + c) / Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
                }
            }
            else
            {
                straightnessError = new double[laserDist.Count];
                for (int i = 0; i < laserDist.Count; i++)
                {
                    straightnessError[i] = Math.Abs(i - line.Start_X);
                }
            }

            bow = straightnessError.Max();

            return Errortype.OK;
        }

        /// <summary>
        /// 标定bow和气压的关系测试数据记录
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="pressure">输入气压</param>
        /// <param name="laserDist">位移传感器数据</param>
        /// <param name="minFirst">第一组数据的最小点值 单位um</param>
        /// <param name="bow">返回bow值 单位um</param>
        /// <param name="min">返回当前最小值 单位um</param>
        /// <param name="lift">返回当前lift值 单位um</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibrateBowMeasurementTestRecord(string fileName, double pressure, List<double> laserDist, double minFirst, out double bow, out double min, out double lift)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID " +
                               "MinFirst MinCurrent Pressure Bow Lift LaserDist";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            ComAlgo.CalcDataSummary(laserDist, out DataStatisticParam analysisValue);
            min = analysisValue.Min;

            CalcBow(laserDist, out bow);
            lift = analysisValue.Min - minFirst;

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep +
                          id.ToString() + _sep +
                          minFirst.ToString(_format) + _sep +
                          min.ToString(_format) + _sep +
                          pressure.ToString(_format) + _sep +
                          bow.ToString(_format) + _sep +
                          lift.ToString(_format) + _sep;
            for (int i = 0; i < laserDist.Count; i++)
            {
                text += laserDist[i].ToString(_format) + _sep;
            }

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// sdb线性测试和精度测试数据记录
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="sdbForce">sdb force 数值</param>
        /// <param name="forceSensor">force sensor 数值</param>
        /// <param name="positionSensor">position sensor 数值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SdbTestRecord(string fileName, double sdbForce, double forceSensor, double positionSensor)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID " +
                               "SDBForce ForceSensor PositionSensor";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            int id = GetLineID(fileName);
            string time = ComAlgo.GetDateTime();
            string text = time + _sep +
                          id.ToString() + _sep +
                          sdbForce.ToString(_format) + _sep +
                          forceSensor.ToString(_format) + _sep +
                          positionSensor.ToString(_format) + _sep;

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }
    }

    #region Record

    /// <summary>
    /// 左上、左下、右上、右下相机坐标
    /// </summary>
    public class CalibrateTopToBottomOpticsData
    {
        /// <summary>
        /// Gets or Sets 左上相机Mark中心光栅坐标
        /// </summary>
        public Point LeftTopXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左下相机Mark中心光栅坐标
        /// </summary>
        public Point LeftBottomXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右上相机Mark中心光栅坐标
        /// </summary>
        public Point RightTopXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右下相机Mark中心光栅坐标
        /// </summary>
        public Point RightBottomXy { get; set; } = new Point();
    }

    /// <summary>
    /// nano平台标定信息
    /// </summary>
    public class CalibrateNanoStageData
    {
        /// <summary>
        /// Gets or Sets 左侧mark第一次光栅坐标
        /// </summary>
        public Point LeftMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右侧mark第一次光栅坐标
        /// </summary>
        public Point RightMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 微动平台XY第一次坐标度数
        /// </summary>
        public Point GaugeXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 微动平台T第一次坐标度数
        /// </summary>
        public double GaugeT { get; set; }
    }

    /// <summary>
    /// 下表数据
    /// </summary>
    public class BottomTableData
    {
        /// <summary>
        /// Gets or Sets bottom table的x0轴读数
        /// </summary>
        public double BottomTableX0 { get; set; }

        /// <summary>
        /// Gets or Sets bottom table的y0轴读数
        /// </summary>
        public double BottomTableY0 { get; set; }

        /// <summary>
        /// Gets or Sets bottom table的x1轴读数
        /// </summary>
        public double BottomTableX1 { get; set; }

        /// <summary>
        /// Gets or Sets bottom table的y1轴读数
        /// </summary>
        public double BottomTableY1 { get; set; }

        /// <summary>
        /// Gets or Sets 左侧pec的mark光栅坐标
        /// </summary>
        public Point PecLeftMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右侧pec的mark光栅坐标
        /// </summary>
        public Point PecRightMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左侧相机mark的光栅坐标
        /// </summary>
        public Point OpticLeftMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右侧相机mark的光栅坐标
        /// </summary>
        public Point OpticRightMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左侧pec相机z轴
        /// </summary>
        public double PecLeftZ { get; set; }

        /// <summary>
        /// Gets or Sets 右侧pec相机z轴
        /// </summary>
        public double PecRightZ { get; set; }
    }

    /// <summary>
    /// 对位重复性测试
    /// </summary>
    public class AlignmentRepeatabilityTestData
    {
        /// <summary>
        /// Gets or Sets 左上相机看CalibrationMark的坐标
        /// </summary>
        public Point OpticLeftTopCalibrationMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左下相机看CalibrationMark的坐标
        /// </summary>
        public Point OpticLeftBottomCalibrationMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右上相机看CalibrationMark的坐标
        /// </summary>
        public Point OpticRightTopCalibrationMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右下相机看CalibrationMark的坐标
        /// </summary>
        public Point OpticRightBottomCalibrationMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左侧相机轴坐标
        /// </summary>
        public Point3D OpticLeftXyz { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets 右侧相机轴坐标
        /// </summary>
        public Point3D OpticRightXyz { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets bottom table 第一次数据 包含x0 x1 y0 y1 左右侧pec mark数据
        /// </summary>
        public BottomTableData BottomTableDataFirst { get; set; } = new BottomTableData();

        /// <summary>
        /// Gets or Sets top table的x0轴读数
        /// </summary>
        public double TopTableX0 { get; set; }

        /// <summary>
        /// Gets or Sets top table的x1轴读数
        /// </summary>
        public double TopTableX1 { get; set; }

        /// <summary>
        /// Gets or Sets top table的y0轴读数
        /// </summary>
        public double TopTableY0 { get; set; }

        /// <summary>
        /// Gets or Sets top table的y1轴读数
        /// </summary>
        public double TopTableY1 { get; set; }

        /// <summary>
        /// Gets or Sets top 微动平台坐标Xy
        /// </summary>
        public Point TopNanoStageXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets top 微动平台旋转量T
        /// </summary>
        public double TopNanoStageT { get; set; }

        /// <summary>
        /// Gets or Sets top wafer左侧mark坐标
        /// </summary>
        public Point TopWaferLeftMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets top wafer右侧mark坐标
        /// </summary>
        public Point TopWaferRightMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左侧相机轴坐标 在top对准后的坐标
        /// </summary>
        public Point3D OpticLeftXyzAfterAlignment { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets 右侧相机轴坐标  在top对准后的坐标
        /// </summary>
        public Point3D OpticRightXyzAfterAlignment { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets bottom table 第二次数据 对准前 包含x0 x1 y0 y1 左右侧pec mark数据
        /// </summary>
        public BottomTableData BottomTableDataSecond { get; set; } = new BottomTableData();

        /// <summary>
        /// Gets or Sets bottom table 第三次数据 对准后 包含x0 x1 y0 y1 左右侧pec mark数据
        /// </summary>
        public BottomTableData BottomTableDataPost { get; set; } = new BottomTableData();

        /// <summary>
        /// Gets or Sets bottom 微动平台坐标Xy
        /// </summary>
        public Point BottomNanoStageXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets bottom 微动平台旋转坐标T
        /// </summary>
        public double BottomNanoStageT { get; set; }

        /// <summary>
        /// Gets or Sets 校准后bottom wafer 左侧mark坐标
        /// </summary>
        public Point BottomWaferLeftMarkInspectXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 校准后bottom wafer 右侧mark坐标
        /// </summary>
        public Point BottomWaferRightMarkInspectXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 校准后bottom wafer 左侧mark坐标
        /// </summary>
        public Point BottomWaferLeftMarkInspectNoGlassXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 校准后bottom wafer 右侧mark坐标
        /// </summary>
        public Point BottomWaferRightMarkInspectNoGlassXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左侧相机轴坐标 在Bottom Wafer Inspect后
        /// </summary>
        public Point3D OpticLeftXyzAfterBottomWaferInspect { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets 右侧相机轴坐标 在Bottom Wafer Inspect后
        /// </summary>
        public Point3D OpticRightXyzAfterBottomWaferInspect { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets 校准后top wafer 左侧mark坐标
        /// </summary>
        public Point TopWaferLeftMarkInspectXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 校准后top wafer 右侧mark坐标
        /// </summary>
        public Point TopWaferRightMarkInspectXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左侧相机轴坐标 在Top Wafer Inspect后
        /// </summary>
        public Point3D OpticLeftXyzAfterTopWaferInspect { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets 右侧相机轴坐标  在Top Wafer Inspect后
        /// </summary>
        public Point3D OpticRightXyzAfterTopWaferInspect { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets 左上相机看CalibrationMark的坐标
        /// </summary>
        public Point OpticLeftTopCalibrationMarkXySecond { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左下相机看CalibrationMark的坐标
        /// </summary>
        public Point OpticLeftBottomCalibrationMarkXySecond { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右上相机看CalibrationMark的坐标
        /// </summary>
        public Point OpticRightTopCalibrationMarkXySecond { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右下相机看CalibrationMark的坐标
        /// </summary>
        public Point OpticRightBottomCalibrationMarkXySecond { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 补偿值Xy
        /// </summary>
        public Point OffsetXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 补偿值Xy
        /// </summary>
        public double OffsetT { get; set; } = 0;
    }

    /// <summary>
    /// nano平台重复性测试
    /// </summary>
    public class NanoStageAccuracyTestData
    {
        /// <summary>
        /// Gets or Sets wafer左侧mark坐标
        /// </summary>
        public Point LeftWaferMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets wafer右侧mark坐标
        /// </summary>
        public Point RightWaferMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 微动平台xy坐标
        /// </summary>
        public Point NanoStageXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 微动平台t坐标
        /// </summary>
        public double NanoStageT { get; set; }
    }

    /// <summary>
    /// nano stage linear test
    /// </summary>
    public class NanoStageLinearTestData
    {
        /// <summary>
        /// Gets or Sets 镜头左侧XYZ
        /// </summary>
        public Point3D LeftOpticXyz { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets 镜头右侧XYZ
        /// </summary>
        public Point3D RightOpticXyz { get; set; } = new Point3D();

        /// <summary>
        /// Gets or Sets 左侧mark XY
        /// </summary>
        public Point LeftMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右侧mark XY
        /// </summary>
        public Point RightMarkXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets NanoStage xy
        /// </summary>
        public Point NanoTheoryXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets nano stage T
        /// </summary>
        public double NanoTheoryT { get; set; }

        /// <summary>
        /// Gets or Sets NanoStage xy
        /// </summary>
        public Point NanoActualXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets nano stage T
        /// </summary>
        public double NanoActualT { get; set; }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>深拷贝对象</returns>
        public NanoStageLinearTestData Clone()
        {
            NanoStageLinearTestData copyObject = new NanoStageLinearTestData();
            copyObject.LeftOpticXyz = this.LeftOpticXyz.Clone();
            copyObject.RightOpticXyz = this.RightOpticXyz.Clone();
            copyObject.LeftMarkXy = this.LeftMarkXy.Clone();
            copyObject.RightMarkXy = this.RightMarkXy.Clone();
            copyObject.NanoTheoryXy = this.NanoTheoryXy.Clone();
            copyObject.NanoTheoryT = this.NanoTheoryT;
            copyObject.NanoActualXy = this.NanoActualXy.Clone();
            copyObject.NanoActualT = this.NanoActualT;
            return copyObject;
        }

        #endregion
    }
}

