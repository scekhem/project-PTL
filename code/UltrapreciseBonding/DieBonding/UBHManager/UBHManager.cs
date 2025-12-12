using DataStruct;
using HalconDotNet;
using IniFileHelper;
using OpenCvSharp.Flann;
using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using UltrapreciseBonding.AutoFocus;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.DieBonding;
using UltrapreciseBonding.FusionCollections;
using UltrapreciseBonding.MarkLocation;
using UltrapreciseBonding.Ransac;
using UltrapreciseBonding.TemplateMatch;
using UltrapreciseBonding.UBAManager;
using UltrapreciseBonding.UBCManager;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.UBHManager
{
    /// <summary>
    /// UBHManager
    /// </summary>
    public static class UBHManager
    {
        private static SetCalibParam _calibParam = new SetCalibParam(); //标定项名称
        private static SetAlignParam _alignParamL = new SetAlignParam(); //示教对位坐标
        private static SetAlignParam _alignParamR = new SetAlignParam(); //示教对位坐标
        private static SetTeachParam _teachParam = new SetTeachParam(); //示教参数
        private static string _algParamFile;

        //offset
        private static Point _headToolToCamOffset = new Point();
        private static Point _bondAccuracyXYOffset = new Point(-0.0233, 0.01816);
        private static double _bondAccuracyAngleOffset = 0;

        // 左右龙门轴坐标映射矩阵
        private static double[] _gantrySre2DstMat = new double[] { 1, 0, 0, 0, 1, 0 };

        // 补偿值KdTree
        private static KDTree<double, Point> _kdTreeXY;
        private static KDTree<double, Point> _kdTreeAngle;

        /// <summary>
        /// 初始化标定项目
        /// </summary>
        /// <param name="setCalibParam">标定参数信息</param>
        /// <param name="algParamFile">算法参数文件路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype InitCalib(SetCalibParam setCalibParam, string algParamFile = null)
        {
            _calibParam = setCalibParam;
            _algParamFile = algParamFile;

            if (_calibParam.WaferCamNameFine == null || _calibParam.WaferCamNameCoarse == null ||
                _calibParam.LeftIcCamName == null || _calibParam.RightIcCamName == null ||
                _calibParam.LeftHeadCamName == null || _calibParam.RightHeadCamName == null ||
                _calibParam.LeftHeadItemName == null || _calibParam.RightHeadItemName == null ||
                _calibParam.WaferStageName == null)
            {
                return Errortype.UBH_CALIB_NAME_NULL;
            }

            //List<string> markNames = new List<string>();
            //foreach (var name in Enum.GetValues(typeof(AlignTemplateName)))
            //{
            //    markNames.Add(Enum.GetName(typeof(AlignTemplateName), name));
            //}

            //MarkAutoCenterLocationManager.Init(alignTemplatePath, markNames);
            return Errortype.OK;
        }

        #region 网格板识别和玻璃IC识别

        /// <summary>
        /// 计算玻璃IC mark中心（环光）
        /// </summary>
        /// <param name="headCamName">相机机构名称</param>
        /// <param name="img">图像</param>
        /// <param name="curGantryRuler">轴光栅</param>
        /// <param name="markPix">mark像素坐标</param>
        /// <param name="markRuler">mark轴坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcGlassICMarkCenter(string headCamName, Camera img, Point curGantryRuler, out Point markPix, out Point markRuler)
        {
            markRuler = new Point();
            markPix = new Point();
            if (img is null || img.Width < 1 || img.Height < 1)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
            }

            HObject hImage = img.GenHObject();

            //粗定位
            //HOperatorSet.GenRectangle1(out HObject rectangle, img.Height / 4, img.Width / 4, img.Height * 3 / 4, img.Width * 3 / 4);
            //HOperatorSet.ReduceDomain(hImage, rectangle, out HObject imgReduce);
            HOperatorSet.AutoThreshold(hImage, out HObject thresholdRegion, 2);
            HOperatorSet.ClosingCircle(thresholdRegion, out HObject closingCircleRegion, 7.5);
            HOperatorSet.Connection(closingCircleRegion, out HObject connectionRegion);
            HOperatorSet.FillUp(connectionRegion, out HObject regionFillUp);
            HOperatorSet.SelectShape(regionFillUp, out HObject selectedRegionsCir, "circularity", "and", 0.9, 1);
            HOperatorSet.SelectShapeStd(selectedRegionsCir, out HObject selectedRegions, "max_area", 70);

            HOperatorSet.SmallestCircle(selectedRegions, out HTuple maxRow, out HTuple maxCol, out HTuple maxRadius);
            thresholdRegion.Dispose();
            regionFillUp.Dispose();
            connectionRegion.Dispose();
            closingCircleRegion.Dispose();
            selectedRegions.Dispose();
            selectedRegionsCir.Dispose();
            hImage.Dispose();

            //精定位
            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = 0.8;
            calipParam.MeasureSigma = 1;
            calipParam.MeasureLength1 = 20;
            calipParam.MeasureLength2 = 4;
            calipParam.NumMeasures = 150;
            calipParam.MeasureThreshold = 10;
            calipParam.MeasureSelect = "first";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "uniform";

            Errortype ret = CaliperCircle.CircleExtraction(img, new Point(maxCol.D, maxRow.D), maxRadius.D, calipParam, out markPix, out double radius);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //像素转轴
            ret = CalibPixScaleXY.GetInstance().GetRulerByPix(headCamName, markPix, curGantryRuler, out markRuler);
            if (ret != Errortype.OK)
            {
                return Errortype.OK;
            }

            if (ComAlgo.SaveFlg("CalcGlassICMarkCenter", out int days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                ComAlgo.SaveData(
                    path + "\\CalcGlassICMarkCenter.txt",
                    curGantryRuler.ToString(sep), markPix.ToString(sep), markRuler.ToString(sep));
            }

            return Errortype.OK;
        }

        /// <summary> 
        /// UBH计算玻璃IC键合补偿
        /// </summary>
        /// <param name="itemName">IC相机、基板相机</param>
        /// <param name="subMarkRuler">基板mark光栅</param>
        /// <param name="icMarkRuler">icmark光栅</param>
        /// <param name="angleOffset">角度偏差</param>
        /// <param name="translateOffset">平移偏差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcGlassIcTR(List<string> itemName, List<Point> subMarkRuler, List<Point> icMarkRuler,
            out double angleOffset, out Point translateOffset)
        {
            angleOffset = 0;
            translateOffset = new Point();
            if (subMarkRuler.Count != 2 || icMarkRuler.Count != 2)
            {
                return Errortype.UBH_INPUT_POINT_COUNT_NOT_EQUAAL_TWO;
            }

            if (itemName == null)
            {
                return Errortype.UBH_INPUT_PARAM_NULL;
            }

            List<Point> icMarkReals = new List<Point>();
            List<Point> subMarkReals = new List<Point>();
            Errortype ret;
            for (int i = 0; i < subMarkRuler.Count; i++)
            {
                ret = CalibXY.GetInstance().GetRealByRuler(itemName[1], icMarkRuler[i], out Point icMarkReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                icMarkReals.Add(icMarkReal);
                ret = CalibXY.GetInstance().GetRealByRuler(itemName[1], subMarkRuler[i], out Point subMarkReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                subMarkReals.Add(subMarkReal);
            }

            Point currentRuler = (icMarkRuler[0] + icMarkRuler[1]) / 2;
            ret = CalibXYT.GetInstance().GetRotateCenter(itemName[0], false, currentRuler, out Point currentRotateCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //ic左上右下坐标颠倒，放置角度计算方向反向
            ret = CalcRT(subMarkReals[0], subMarkReals[1], icMarkReals[1], icMarkReals[0],
                currentRotateCenter, out double angle, out Point translateDist);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //angleOffset = -angle+ _bondAccuracyAngleOffset;
            //translateOffset = new Point(-translateDist.X, -translateDist.Y) + _bondAccuracyXYOffset;
            angleOffset = -angle;
            translateOffset = new Point(-translateDist.X, -translateDist.Y);

            if (ComAlgo.SaveFlg("CalcGlassIcTR", out int days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                ComAlgo.SaveData(
                    path + "\\CalcGlassIcTR.txt",
                    subMarkRuler[0].ToString(sep), subMarkRuler[1].ToString(sep),
                    icMarkRuler[0].ToString(sep), icMarkRuler[1].ToString(sep),
                    subMarkReals[0].ToString(sep), subMarkReals[1].ToString(sep),
                    icMarkReals[0].ToString(sep), icMarkReals[1].ToString(sep),
                    currentRuler.ToString(sep), currentRotateCenter.ToString(sep),
                    translateDist.ToString(sep), angleOffset.ToString("f6"),
                    translateOffset.ToString(sep));
            }

            return Errortype.OK;
        }

        /// <summary>  
        /// UBH专用玻璃IC键合打样精度计算
        /// </summary>
        /// <param name="itemName">IC、基板相机</param>
        /// <param name="img">键合后拍摄的左上、右下Mark两幅图像</param>
        /// <param name="headRuler">Head相机在左上、右下采图时对应的轴坐标</param>
        /// <param name="angleError">键合角度误差</param>
        /// <param name="translateError">键合精度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcGlassBondAccuracy(List<string> itemName, List<Camera> img, List<Point> headRuler,
            out double angleError, out Point translateError)
        {
            angleError = 0;
            translateError = new Point();

            if (img.Count != 2)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
            }

            if (headRuler.Count != 2)
            {
                return Errortype.UBH_INPUT_POINT_COUNT_NOT_EQUAAL_TWO;
            }

            if (itemName == null)
            {
                return Errortype.UBH_INPUT_PARAM_NULL;
            }

            List<Point> icMarkReals = new List<Point>();
            List<Point> subMarkReals = new List<Point>();
            List<double> subMarkRulersX = new List<double>();
            List<double> subMarkRulersY = new List<double>();
            Errortype ret = Errortype.OK;
            CaliperParams calipParam = new CaliperParams();
            calipParam.Load(_algParamFile + "\\CaliperParamsBondCheck.ini", "CaliperParams");
            if (ret != Errortype.OK)
            {
                return ret;
            }

            for (int i = 0; i < img.Count; i++)
            {
                //ret = ComAlgo.CalcGlassIcBondPix(img[i], calipParam, out Point innerPix, out Point outerPix, out Point errorXYLeft);
                //if (ret != Errortype.OK)
                //{
                //    return ret;
                //}
                Point innerPix = new Point();
                Point outerPix = new Point();
                ret = DieBondComAlgo.CalcCircleRingMarkCenter(img[i], out List<Point> centerExtract, out List<double> radius, out List<bool> isCircle, null);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                if (centerExtract.Count != 2)
                {
                    return Errortype.UBD_CALCGLASSICMARKCENTER_GRAB_ERROR;
                }

                if (radius[0] > radius[1])
                {
                    innerPix = centerExtract[1];
                    outerPix = centerExtract[0];
                }
                else
                {
                    innerPix = centerExtract[0];
                    outerPix = centerExtract[1];
                }

                ret = CalibPixScaleXY.GetInstance().GetRulerByPix(itemName[1], innerPix, headRuler[i], out Point icMarkRuler);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibXY.GetInstance().GetRealByRuler(itemName[1], icMarkRuler, out Point icMarkReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                icMarkReals.Add(icMarkReal);
                ret = CalibPixScaleXY.GetInstance().GetRulerByPix(itemName[1], outerPix, headRuler[i], out Point subMarkRuler);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibXY.GetInstance().GetRealByRuler(itemName[1], subMarkRuler, out Point subMarkReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                subMarkRulersX.Add(subMarkRuler.X);
                subMarkRulersY.Add(subMarkRuler.Y);
                subMarkReals.Add(subMarkReal);
            }

            Point rotateCenter = (subMarkReals[0] + subMarkReals[1]) / 2;

            ret = ComAlgo.CalcLinesRT(subMarkReals[0], subMarkReals[1], icMarkReals[0], icMarkReals[1],
                rotateCenter, out angleError, out translateError);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (ComAlgo.SaveFlg("CalcGlassBondAccuracy", out int days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                //if (Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);
                //}
                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");

                ComAlgo.SaveData(
                    path + "\\CalcGlassBondAccuracy.txt",
                    subMarkReals[0].ToString(sep), subMarkReals[1].ToString(sep),
                    icMarkReals[0].ToString(sep), icMarkReals[1].ToString(sep),
                    translateError.ToString(sep), angleError.ToString("f6"));

                //保存键合图片
                HObject hImage = img[0].GenHObject();
                HOperatorSet.WriteImage(hImage, "bmp", 0, @"D:\Alg\Img\" + time + "errorImg0.bmp");
                hImage = img[1].GenHObject();
                HOperatorSet.WriteImage(hImage, "bmp", 0, @"D:\Alg\Img\" + time + "errorImg1.bmp");
                hImage.Dispose();
            }

            if (ComAlgo.SaveFlg("WholeWaferDebond", out days))
            {
                //save points
                //上面AddSectionWithKeyValues方法是进行逐个点写入，每次都是一个点调一次该方法，耗时82.731s
                //改用下面 StringBuilder方式一次性写入，耗时10ms
                StringBuilder contentBuilder = new StringBuilder();
                HOperatorSet.TupleMean(subMarkRulersX.ToArray(), out HTuple meanX);
                HOperatorSet.TupleMean(subMarkRulersY.ToArray(), out HTuple meanY);

                contentBuilder.AppendLine("[Point]");
                contentBuilder.AppendLine("src_x=" + meanX.D.ToString());
                contentBuilder.AppendLine("src_y=" + meanY.D.ToString());
                contentBuilder.AppendLine("dst_x=" + translateError.X.ToString());
                contentBuilder.AppendLine("dst_y=" + translateError.Y.ToString());
                contentBuilder.AppendLine("angle=" + angleError.ToString());

                File.WriteAllText(_algParamFile + "XY_CalibCoordPoints.ini", contentBuilder.ToString());
            }

            return Errortype.OK;
        }

        #endregion

        #region 工艺流程方法

        /// <summary>  
        /// UBH转用计算两组点连线的旋转平移量
        /// </summary>
        /// <param name="basePoint1">基准起始点</param>
        /// <param name="basePoint2">基准结束点</param>
        /// <param name="currentPoint1">当前起始点</param>
        /// <param name="currentPoint2">当前结束点</param>
        /// <param name="rotateCenter">当前旋转中心</param>
        /// <param name="angle">旋转量</param>
        /// <param name="translation">平移量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcRT(Point basePoint1, Point basePoint2, Point currentPoint1, Point currentPoint2,
            Point rotateCenter, out double angle, out Point translation)
        {
            angle = 0;
            translation = new Point();
            if (basePoint1 is null || basePoint2 is null || currentPoint1 is null || currentPoint2 is null)
            {
                return Errortype.UBH_INPUT_POINT_NULL;
            }

            Errortype ret = ComAlgo.CalcAngleLL(new LineSeg(currentPoint1, currentPoint2), new LineSeg(basePoint1, basePoint2), out double radian);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HOperatorSet.TupleDeg(new HTuple(radian), out HTuple deg);
            angle = deg.D;

            //平移
            Point currentCenter = (currentPoint1 + currentPoint2) / 2;
            ret = ComAlgo.CalcRotatePoint(currentCenter, -angle, rotateCenter, out Point rotateCurPoint);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            translation = rotateCenter - rotateCurPoint;

            return Errortype.OK;
        }

        /// <summary>   
        /// wafer手动拉直计算wafer偏角（不限制方向以及X、Y轴）调试完删除
        /// </summary>
        /// <param name="startPoint">轴坐标点1</param>
        /// <param name="endPoint">轴坐标点2</param>
        /// <param name="waferRotateOffset">wafer角度偏差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcWaferAngle(Point startPoint, Point endPoint, out double waferRotateOffset)
        {
            waferRotateOffset = 0;
            if (startPoint == null || endPoint == null)
            {
                return Errortype.UBH_INPUT_POINT_NULL;
            }

            if (startPoint == endPoint)
            {
                return Errortype.UBH_INPUT_POINT_EQUAL;
            }

            //计算与X轴夹角
            Point basePoint1 = new Point(-1, 0);
            Point basePoint2 = new Point(1, 0);
            Point rotateCenter = new Point(0, 0);
            Errortype ret = ComAlgo.CalcLinesRT(basePoint1, basePoint2, startPoint, endPoint, rotateCenter, out double rotate, out Point translation);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            double distX = Math.Abs(startPoint.X - endPoint.X);
            double distY = Math.Abs(startPoint.Y - endPoint.Y);
            double waferRotate = 0;

            if (distX >= distY)
            {
                waferRotate = rotate;
            }

            if (distX < distY)
            {
                waferRotate = rotate - 90;
            }

            waferRotateOffset = waferRotate;
            if (waferRotate > 90)
            {
                waferRotateOffset = waferRotate - 180;
            }

            if (waferRotate < -90)
            {
                waferRotateOffset = waferRotate + 180;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// wafer手动拉直计算wafer偏角(合并整体拉直和单个芯片拉直)
        /// </summary>
        /// <param name="startPoint">轴左边点</param>
        /// <param name="endPoint">轴右点边</param>
        /// <param name="currentRuler">当前轴光栅坐标</param>
        /// <param name="waferRotateOffset">Wafer旋转角度</param>
        /// <param name="rotatePoint">Wafer单颗die中心位置</param>
        /// <param name="dieSize">Wafer单颗die尺寸-Width</param>
        /// <param name="isSingle">是单个芯片拉直还是整体拉直</param>
        /// <param name="isPattern">是否使用pattern方式进行拉直</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcWaferAngle(Point startPoint, Point endPoint, Point currentRuler, out double waferRotateOffset, out Point rotatePoint, Point dieSize = null, bool isSingle = false, bool isPattern = false)
        {
            Errortype ret = Errortype.OK;
            waferRotateOffset = 0;
            rotatePoint = new Point();
            if (startPoint == null || endPoint == null)
            {
                return Errortype.UBH_INPUT_POINT_NULL;
            }

            if (startPoint == endPoint)
            {
                return Errortype.UBH_INPUT_POINT_EQUAL;
            }

            if (isPattern)
            {
                List<Point> diePatternPoints = new List<Point> { startPoint, endPoint };
                var res = DieBondComAlgo.RigidTransPoint2d(_teachParam.WaferDiePatternRuler, diePatternPoints, _teachParam.WaferDieCornerRuler, out List<Point> dieCornerPoints);
                if (res != Errortype.OK)
                {
                    return res;
                }

                startPoint = dieCornerPoints[0];
                endPoint = dieCornerPoints[1];
            }

            // 起点和终点随机选择都能进行拉直，故按X轴方向对两点进行排序;
            if (startPoint.X > endPoint.X)
            {
                Point temp = startPoint.Clone();
                startPoint = endPoint;
                endPoint = temp;
            }

            HOperatorSet.AngleLx(startPoint.Y, startPoint.X, endPoint.Y, endPoint.X, out HTuple angle);
            HOperatorSet.TupleDeg(-angle, out HTuple deg);
            ret = CalibXYT.GetInstance().GetRotateCenter(_calibParam.WaferCamNameFine, false, currentRuler,
                out Point currentRotateCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            // 如果是求单个die的角度，由于找的两点是对角线的点，故有对角线与坐标轴的夹角。
            // 拒绝自动判断，设置阈值，角度过大时会误判。
            if (isSingle)
            {
                HOperatorSet.TupleAtan2(dieSize.Y, dieSize.X, out HTuple asin);
                HOperatorSet.TupleDeg(-asin, out HTuple asinDeg);
                deg = deg - asinDeg;
            }
            else
            {
                // 如果Y向距离比X方向距离大，则认为是在进行Y向拉直;
                if (Math.Abs(startPoint.X - endPoint.X) < Math.Abs(startPoint.Y - endPoint.Y))
                {
                    deg = deg > 0 ? 90 - deg : -90 - deg;
                }
            }

            ret = ComAlgo.CalcRotatePoint((startPoint + endPoint) / 2, -deg, currentRotateCenter,
                out rotatePoint);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            waferRotateOffset = deg;

            if (ComAlgo.SaveFlg("CalcWaferAngle", out int days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                ComAlgo.SaveData(
                    path + "\\CalcWaferAngle.txt", startPoint.ToString(sep), endPoint.ToString(sep),
                    currentRuler.ToString(sep), rotatePoint.ToString(sep), waferRotateOffset.ToString("f6"));
            }

            return ret;
        }

        /// <summary>  
        /// 添加示教对准坐标点集
        /// </summary>
        /// <param name="station">区分左右工位</param>
        /// <param name="icPatternRuler">IC左上右下Pattern对应轴坐标</param>
        /// <param name="subPatternRuler">基板左上右下Pattern对应轴坐标</param>
        /// <param name="icBumpRuler">IC 4个Bump点对应轴坐标</param>
        /// <param name="subBumpRuler">4个Bump点对应轴坐标（保证与ICBump点一一对应）</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AddTeachPoint(WorkStation station, List<Point> icPatternRuler, List<Point> subPatternRuler,
            List<Point> icBumpRuler, List<Point> subBumpRuler)
        {
            if (icPatternRuler.Count != 2 || subPatternRuler.Count != 2)
            {
                return Errortype.UBH_INPUT_POINT_COUNT_NOT_EQUAAL_TWO;
            }

            if (icBumpRuler.Count < 1 || subBumpRuler.Count < 1)
            {
                return Errortype.UBH_INPUT_POINT_COUNT_NOT_EQUAAL_TWO;
            }

            switch (station)
            {
                case WorkStation.Left_Station:
                    _alignParamL.IcPatternRuler = icPatternRuler;
                    _alignParamL.SubPatternRuler = subPatternRuler;
                    _alignParamL.IcBumpRuler = icBumpRuler;
                    _alignParamL.SubBumpRuler = subBumpRuler;
                    break;
                case WorkStation.Right_Station:
                    _alignParamR.IcPatternRuler = icPatternRuler;
                    _alignParamR.SubPatternRuler = subPatternRuler;
                    _alignParamR.IcBumpRuler = icBumpRuler;
                    _alignParamR.SubBumpRuler = subBumpRuler;
                    break;
                default:
                    break;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 添加wafer die 拉直示教坐标点集
        /// </summary>
        /// <param name="waferDiePatternRuler">die 左上右下Pattern对应轴坐标</param>
        /// <param name="waferDieCornerRuler">die 左上右下角点对应轴坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AddDieTeachPoints(List<Point> waferDiePatternRuler, List<Point> waferDieCornerRuler)
        {
            if (waferDiePatternRuler.Count != 2 || waferDieCornerRuler.Count != 2)
            {
                return Errortype.UBH_INPUT_POINT_LENGTH_ERROR;
            }

            _teachParam.WaferDiePatternRuler = waferDiePatternRuler;
            _teachParam.WaferDieCornerRuler = waferDieCornerRuler;

            return Errortype.OK;
        }

        /// <summary> 
        /// 计算IC和基板Bump点的平均旋转平移量
        /// </summary>
        /// <param name="station">区分左右工位</param>
        /// <param name="icPatternRulers">当前IC左上右下Pattern光栅坐标</param>
        /// <param name="subPatternRulers">当前基板左上右下Pattern光栅坐标</param>
        /// <param name="angleOffset">平均旋转量补偿</param>
        /// <param name="translateOffset">平均平移量补偿</param>
        /// <param name="camCenterRuler">haed相机中心对位时光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcAlignOffset(WorkStation station, List<Point> icPatternRulers, List<Point> subPatternRulers, out double angleOffset, out Point translateOffset, out Point camCenterRuler)
        {
            angleOffset = 0;
            translateOffset = new Point();
            camCenterRuler = new Point();

            if (icPatternRulers.Count != subPatternRulers.Count)
            {
                return Errortype.UBH_INPUT_POINT_COUNT_NOT_EQUAAL_TWO;
            }

            SetAlignParam alignParam = new SetAlignParam();
            string headItemName = null;
            string icCamName = null;
            string headCamName = null;
            switch (station)
            {
                case WorkStation.Left_Station:
                    headItemName = _calibParam.LeftHeadItemName;
                    alignParam = _alignParamL;
                    icCamName = _calibParam.LeftIcCamName;
                    break;
                case WorkStation.Right_Station:
                    headItemName = _calibParam.RightHeadItemName;
                    alignParam = _alignParamR;
                    icCamName = _calibParam.RightIcCamName;
                    break;
                default:
                    break;
            }

            List<Point> icBumpReals = new List<Point>();
            List<Point> subBumpReals = new List<Point>();
            List<Point> srcIcPatternReals = new List<Point>();
            List<Point> dstIcPatternReals = new List<Point>();
            List<Point> srcSubPatternReals = new List<Point>();
            List<Point> dstSubPatternReals = new List<Point>();
            List<Point> icGantryReals = new List<Point>();
            List<Point> subGantryReals = new List<Point>();

            Errortype ret;
            for (int i = 0; i < icPatternRulers.Count; i++)
            {
                //IC Pattern轴转真值
                ret = CalibXY.GetInstance().GetRealByRuler(headItemName, alignParam.IcPatternRuler[i], out Point srcIcPatternReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                srcIcPatternReals.Add(srcIcPatternReal);
                ret = CalibXY.GetInstance().GetRealByRuler(headItemName, icPatternRulers[i], out Point dstIcPatternReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                dstIcPatternReals.Add(dstIcPatternReal);

                //基板Pattern轴转真值 
                ret = CalibXY.GetInstance().GetRealByRuler(headItemName, alignParam.SubPatternRuler[i], out Point srcSubPatternReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                srcSubPatternReals.Add(srcSubPatternReal);
                ret = CalibXY.GetInstance().GetRealByRuler(headItemName, subPatternRulers[i], out Point dstSubPatternReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                dstSubPatternReals.Add(dstSubPatternReal);
            }

            //示教Bump轴转真值
            for (int i = 0; i < alignParam.IcBumpRuler.Count; i++)
            {
                ret = CalibXY.GetInstance().GetRealByRuler(headItemName, alignParam.IcBumpRuler[i], out Point icBumpReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                icBumpReals.Add(icBumpReal);

                ret = CalibXY.GetInstance().GetRealByRuler(headItemName, alignParam.SubBumpRuler[i], out Point subBumpReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                subBumpReals.Add(subBumpReal);
            }

            // 计算示教pattern位置和生产时pattern位置计算刚体变换关系
            ret = ComAlgo.VectorToRigid(srcIcPatternReals, dstIcPatternReals, out double[] homMat2dIC, out List<Point> errorIC);
            ret = ComAlgo.VectorToRigid(srcSubPatternReals, dstSubPatternReals, out double[] homMat2dSub, out List<Point> errorSub);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            // 转换Bump点
            ret = ComAlgo.AffineTransPoint2d(icBumpReals, homMat2dIC, out List<Point> curIcBumpReals);
            ret = ComAlgo.AffineTransPoint2d(subBumpReals, homMat2dSub, out List<Point> curSubBumpReals);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //获取当前旋转中心   currentRuler 不重要 因为ChangeCenter为false就只是标定计算中心
            ret = CalibXYT.GetInstance().GetRotateCenter(icCamName, false, subPatternRulers[0], out Point curRotateCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //计算真值下的旋转平移量
            int counts = (int)curIcBumpReals.Count / 2;
            Point camCenterTrue = new Point();
            for (int i = 0; i < counts; ++i)
            {
                ret = CalcRT(curSubBumpReals[i], curSubBumpReals[i + counts], curIcBumpReals[i + counts], curIcBumpReals[i], curRotateCenter, out double angle, out Point translation);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                camCenterTrue += (curSubBumpReals[i] + curSubBumpReals[i + counts]) / 2;
                angleOffset += -angle;
                translateOffset += new Point(-translation.X, -translation.Y);

                if (ComAlgo.SaveFlg("CalcAlignOffset", out int days))
                {
                    string path = @"D:\Alg\";
                    string sep = " ";

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    ComAlgo.SaveData(
                        path + "\\CalcAlignOffset.txt",
                        alignParam.SubBumpRuler[i].ToString(sep), alignParam.SubBumpRuler[i + counts].ToString(sep),
                        alignParam.IcBumpRuler[i].ToString(sep), alignParam.IcBumpRuler[i + counts].ToString(sep),
                        alignParam.SubPatternRuler[i].ToString(sep), alignParam.SubPatternRuler[i + counts].ToString(sep),
                        alignParam.IcPatternRuler[i].ToString(sep), alignParam.IcPatternRuler[i + counts].ToString(sep),
                        subPatternRulers[i].ToString(sep), subPatternRulers[i + counts].ToString(sep),
                        icPatternRulers[i].ToString(sep), icPatternRulers[i + counts].ToString(sep),
                        curSubBumpReals[i].ToString(sep), curSubBumpReals[i + counts].ToString(sep),
                        curIcBumpReals[i].ToString(sep), curIcBumpReals[i + counts].ToString(sep),
                        curRotateCenter.ToString(sep),
                        translation.ToString(sep), angleOffset.ToString("f6"),
                        translateOffset.ToString(sep));
                }
            }

            ret = CalibXY.GetInstance().GetRulerByReal(headItemName, camCenterTrue / counts, out camCenterRuler);
            angleOffset = angleOffset / counts;
            translateOffset = translateOffset / counts;
            return Errortype.OK;
        }

        /// <summary>   
        /// 识别切割道角点
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="centerPoint">切割道十字中心像素坐标</param>
        /// <param name="selectRegion">识别区域</param>
        /// <param name="leftTOrRightB">区分左上角(leftTop)和右下角(rightBottom)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcCutPathCorner(Camera img, out Point centerPoint, out double score, Region selectRegion = null, string leftTOrRightB = "leftTop")
        {
            centerPoint = new Point();
            score = 0;
            if (img.Width < 1)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
            }

            Rectangle1 rectTmp = new Rectangle1(0, 0, img.Width, img.Height);
            Rectangle1 rect = selectRegion == null ? rectTmp : selectRegion.Rectangle1;

            Errortype ret = GetCutPathEdge(img, rect, out LineSeg minLineH, out LineSeg maxLineH, out double[] scoreHor, "hor");
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = GetCutPathEdge(img, rect, out LineSeg minLineV, out LineSeg maxLineV, out double[] scoreVer, "ver");
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = leftTOrRightB == "leftTop"
                ? ComAlgo.IntersectionLines(maxLineH, maxLineV, out centerPoint)
                : ComAlgo.IntersectionLines(minLineH, minLineV, out centerPoint);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            double[] scoreList = leftTOrRightB == "leftTop" ? new double[] { scoreHor[1], scoreVer[1] } : new double[] { scoreHor[0], scoreVer[0] };

            score = scoreList.Min();
            return Errortype.OK;
        }

        /// <summary>
        /// 计算切割道的边缘
        /// </summary>
        /// <param name="image">输入图片</param>
        /// <param name="maskRegion">掩膜区域</param>
        /// <param name="minLine">边缘卡尺极小值点集</param>
        /// <param name="maxLine">边缘卡尺极大值点集</param>
        /// <param name="horOrVer">卡尺边缘水平还是垂直（"hor"、"ver"）</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal static Errortype GetCutPathEdge(Camera image, Rectangle1 maskRegion, out LineSeg minLine, out LineSeg maxLine, out double[] score, string horOrVer = "hor")
        {
            minLine = null;
            maxLine = null;
            score = new double[2];
            if (image.Width < 1)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
            }

            double row1 = maskRegion.Start_Y;
            double column1 = maskRegion.Start_X;
            double row2 = maskRegion.End_Y;
            double column2 = maskRegion.End_X;

            HObject img = image.GenHObject();
            double count = 80.0;
            double length = horOrVer == "hor" ? (row2 - row1) / 2 : (column2 - column1) / 2;
            double gap = horOrVer == "hor" ? (column2 - column1) / count : (row2 - row1) / count;
            double angle = horOrVer == "hor" ? -Math.PI / 2 : 0;

            List<Point> minP = new List<Point>();
            List<Point> maxP = new List<Point>();
            for (int i = 0; i < count; ++i)
            {
                double row = horOrVer == "hor" ? (row2 + row1) / 2 : i * gap + row1;
                double col = horOrVer == "hor" ? i * gap + column1 : (column2 + column1) / 2;
                HOperatorSet.GenMeasureRectangle2(row, col, angle, length, 10, image.Width, image.Height, "nearest_neighbor", out HTuple measureHandle);
                HOperatorSet.MeasureProjection(img, measureHandle, out HTuple grayValues);
                HOperatorSet.CreateFunct1dArray(grayValues, out HTuple function);
                HOperatorSet.SmoothFunct1dGauss(function, 5, out HTuple smoothedFunction);
                HOperatorSet.DerivateFunct1d(smoothedFunction, "first", out HTuple derivative);

                HOperatorSet.LocalMinMaxFunct1d(derivative, "strict_min_max", "true", out HTuple min, out HTuple max);
                HOperatorSet.GetYValueFunct1d(derivative, min, "constant", out HTuple minY);
                HOperatorSet.TupleMin(minY, out HTuple minValueY);
                HOperatorSet.TupleFind(minY, minValueY, out HTuple minIndices);
                HOperatorSet.GetYValueFunct1d(derivative, max, "constant", out HTuple maxY);
                HOperatorSet.TupleMax(maxY, out HTuple maxValueY);
                HOperatorSet.TupleFind(maxY, maxValueY, out HTuple maxIndices);

                Point minPoint = horOrVer == "hor" ? new Point(i * gap + column1, min[minIndices] + row1) : new Point(min[minIndices] + column1, i * gap + row1);
                Point maxPoint = horOrVer == "hor" ? new Point(i * gap + column1, max[maxIndices] + row1) : new Point(max[maxIndices] + column1, i * gap + row1);

                minP.Add(minPoint);
                maxP.Add(maxPoint);
            }

            // 去除异常点
            RemoveNoise(minP, gap + 10, out List<Point> usedMinP);
            RemoveNoise(maxP, gap + 10, out List<Point> usedMaxP);

            RansacParam ransacParam = new RansacParam();
            ransacParam.DistanceThreshold = 10;
            var ret = RansacLine.LineExtraction(usedMinP, ransacParam, out List<Point> minPoints);
            score[0] = (double)minPoints.Count / (double)usedMinP.Count;
            ret = ComAlgo.FitLine(minPoints, out minLine, out double[] straightnessErrorMin);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = RansacLine.LineExtraction(usedMaxP, ransacParam, out List<Point> maxPoints);
            score[1] = (double)maxPoints.Count / (double)usedMaxP.Count;
            ret = ComAlgo.FitLine(maxPoints, out maxLine, out double[] straightnessErrorMax);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            img.Dispose();
            return ret;
        }

        /// <summary>
        /// 计算head IC角点坐标
        /// </summary>ss
        /// <param name="image">图像</param>
        /// <param name="cornerPix">角点光栅</param>
        /// <param name="leftTOrRightB">左上角点还是右下角点</param>
        /// <param name="region">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetHeadICCorner(Camera image, out Point cornerPix, out double score, CaliperParams param = null, string leftTOrRightB = "leftTop", Rectangle1 region = null)
        {
            cornerPix = new Point();
            score = 0;
            Errortype ret = Errortype.OK;
            if (image == null || image.Width < 1)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
            }

            if (param == null)
            {
                param = new CaliperParams();
            }

            Rectangle1 rectTmp = new Rectangle1(0, 0, image.Width, image.Height);
            Rectangle1 rect = region == null ? rectTmp : region;
            double yStart = rect.Start_Y;
            double xStart = rect.Start_X;
            double yEnd = rect.End_Y;
            double xEnd = rect.End_X;

            HOperatorSet.GenEmptyObj(out HObject emptyObject);
            emptyObject = image.GenHObject();
            HOperatorSet.GetImageSize(emptyObject, out HTuple width, out HTuple height);
            double[] rowBegin = new double[2] { yStart + (yEnd - yStart) / 2, yEnd };
            double[] colBegin = new double[2] { xStart + (xEnd - xStart) / 2, xStart + (xEnd - xStart) / 2 };
            double[] rowEnd = new double[2] { yStart + (yEnd - yStart) / 2, yStart + (yEnd - yStart) / 2 };
            double[] colEnd = new double[2] { xEnd, xStart + (xEnd - xStart) / 2 };
            string measureSelect = "last";
            string measureTransition = "positive";

            if (leftTOrRightB != "leftTop")
            {
                rowBegin = new double[2] { yStart + (yEnd - yStart) / 2, yStart + (yEnd - yStart) / 2 };
                colBegin = new double[2] { xStart, xStart + (xEnd - xStart) / 2 };
                rowEnd = new double[2] { yStart + (yEnd - yStart) / 2, yStart };
                colEnd = new double[2] { xStart + (xEnd - xStart) / 2, xStart + (xEnd - xStart) / 2 };
                measureSelect = "first";
                measureTransition = "negative";
            }

            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = param.MinScore;
            calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
            calipParam.MeasureLength1 = (xEnd - xStart) > (yEnd - yStart) ? (yEnd - yStart) / 2 : (xEnd - xStart) / 2;
            calipParam.MeasureLength2 = 4;
            calipParam.NumMeasures = 240;
            calipParam.MeasureThreshold = param.MeasureThreshold;
            calipParam.MeasureSelect = measureSelect;
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = measureTransition;

            List<LineSeg> lines = new List<LineSeg>();
            double[] scoreList = new double[rowBegin.Length];
            for (int i = 0; i < rowBegin.Length; ++i)
            {
                ret = CaliperLine.LineExtraction(image, new LineSeg(colBegin[i], rowBegin[i], colEnd[i], rowEnd[i]),
                    calipParam, out LineSeg lineOut, out double[] straightness);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                double distance1 = ComAlgo.Dist(new Point(colBegin[i], rowBegin[i]), new Point(colEnd[i], rowEnd[i]));
                double distance2 = ComAlgo.Dist(lineOut.Start, lineOut.End);
                double count = distance2 * calipParam.NumMeasures / distance1;
                scoreList[i] = (straightness.Length / count) > 1 ? 1 : (straightness.Length / count);

                lines.Add(lineOut);
            }

            score = scoreList.Min();
            ret = ComAlgo.IntersectionLines(lines[0], lines[1], out cornerPix);

            if (ComAlgo.SaveFlg("GetHeadICCorner", out int days))
            {
                string path = @"D:\Alg\GetHeadICCorner";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                ComAlgo.FileManage(path, 7, out path);

                ComAlgo.SaveData("D:\\Alg\\GetHeadICCorner.txt", cornerPix.ToString(" "));

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                string name = path + "\\" + time + ".bmp";
                image.Save(name);
            }

            return ret;
        }

        /// <summary>
        /// 拟合圆并计算圆心和notch口连线与圆的交点
        /// </summary>
        /// <param name="circleEdges">圆边缘点</param>
        /// <param name="notchPoint">notch口位置</param>
        /// <param name="circleCenter">圆心</param>
        /// <param name="radius">圆半径</param>
        /// <param name="notchIntersection">交点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetCircleNotchIntersection(List<Point> circleEdges, Point notchPoint,
            out Point circleCenter, out double radius, out Point notchIntersection)
        {
            circleCenter = new Point();
            notchIntersection = new Point();
            radius = 0;
            var ret = Errortype.OK;

            if (circleEdges.Count < 3)
            {
                return Errortype.UBH_INPUT_POINT_COUNT_LESS_THREE;
            }

            ret = ComAlgo.FitCircle(circleEdges, out circleCenter, out radius, out List<double> error);

            HOperatorSet.IntersectionLineCircle(circleCenter.Y, circleCenter.X, notchPoint.Y, notchPoint.X, circleCenter.Y, circleCenter.X,
                radius, 0, 2 * Math.PI, "positive", out HTuple rows, out HTuple cols);

            if (rows.Length < 1)
            {
                return Errortype.UBH_INTERSECTION_LESS_ONE;
            }

            HOperatorSet.DistancePp(notchPoint.Y, notchPoint.X, rows[0], cols[0], out HTuple disFirst);
            HOperatorSet.DistancePp(notchPoint.Y, notchPoint.X, rows[1], cols[1], out HTuple disSecond);

            notchIntersection = disFirst > disSecond ? new Point(cols[1], rows[1]) : new Point(cols[0], rows[0]);

            return ret;
        }

        /// <summary>
        /// 获取notch口最高点像素坐标
        /// </summary>
        /// <param name="image">notch口图像</param>
        /// <param name="peakPoint">顶点坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetNotchPeakPoint(Camera image, out Point peakPoint)
        {
            var ret = Errortype.OK;
            peakPoint = new Point();

            if (image == null || image.Width < 1)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
            }

            HOperatorSet.GenEmptyObj(out HObject img);
            img = image.GenHObject();

            double row1 = 0.0;
            double column1 = 0;
            double row2 = image.Height;
            double column2 = image.Width;

            double count = 150;
            double length = (row2 - row1) / 2;
            double gap = (column2 - column1) / count;
            double angle = -Math.PI / 2;

            List<double> mincols = new List<double>();
            List<double> minrows = new List<double>();
            for (int i = 0; i < count; ++i)
            {
                double row = (row2 + row1) / 2;
                double col = i * gap + column1;
                HOperatorSet.GenMeasureRectangle2(row, col, angle, length, 10, image.Width, image.Height, "nearest_neighbor", out HTuple measureHandle);
                HOperatorSet.MeasureProjection(img, measureHandle, out HTuple grayValues);
                HOperatorSet.CreateFunct1dArray(grayValues, out HTuple function);
                HOperatorSet.SmoothFunct1dGauss(function, 5, out HTuple smoothedFunction);
                HOperatorSet.DerivateFunct1d(smoothedFunction, "first", out HTuple derivative);

                HOperatorSet.LocalMinMaxFunct1d(derivative, "strict_min_max", "true", out HTuple min, out HTuple max);
                HOperatorSet.GetYValueFunct1d(derivative, min, "constant", out HTuple minY);
                HOperatorSet.TupleMin(minY, out HTuple minValueY);
                HOperatorSet.TupleFind(minY, minValueY, out HTuple minIndices);

                mincols.Add(i * gap + column1);
                minrows.Add(min[minIndices] + row1);
            }

            HOperatorSet.CreateFunct1dArray(minrows.ToArray(), out HTuple functionY);
            HOperatorSet.SmoothFunct1dGauss(functionY, 1, out HTuple smoothedFunctionY);
            HOperatorSet.DerivateFunct1d(smoothedFunctionY, "first", out HTuple derivativeY);
            HOperatorSet.ZeroCrossingsFunct1d(derivativeY, out HTuple zeroCrossings);
            HOperatorSet.GetYValueFunct1d(functionY, zeroCrossings, "constant", out HTuple y);
            HOperatorSet.TupleGenConst(zeroCrossings.Length, gap, out HTuple newtuple);
            HOperatorSet.TupleMult(zeroCrossings, newtuple, out HTuple x);

            HOperatorSet.TupleMean(minrows.ToArray(), out HTuple meanY);
            HOperatorSet.TupleMean(mincols.ToArray(), out HTuple meanX);
            HOperatorSet.DistancePp(meanY, meanX, y, x, out HTuple distance);
            HOperatorSet.TupleSortIndex(distance, out HTuple indices);
            if (indices.Length < 1)
            {
                return Errortype.UBH_NOTCH_PEAK_IS_LESS_ONE;
            }

            int index = indices[0];

            peakPoint = new Point(x[index], y[index]);

            img.Dispose();
            return ret;
        }

        /// <summary>
        /// sub 基板Wafer边缘点
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="circleEdge">边缘点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetWaferCircleEdge(Camera image, out List<Point> circleEdge)
        {
            circleEdge = new List<Point>();
            var ret = Errortype.OK;
            if (image == null || image.Width < 1)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
            }

            HOperatorSet.GenEmptyObj(out HObject img);
            img = image.GenHObject();
            HOperatorSet.EdgesSubPix(img, out HObject edges, "canny", 1, 20, 40);
            HOperatorSet.SelectShapeXld(edges, out HObject selectedXld, "contlength", "and", 500, 99999);
            HOperatorSet.UnionAdjacentContoursXld(selectedXld, out HObject unionContours, 10, 1, "attr_keep");
            HOperatorSet.FitCircleContourXld(unionContours, "algebraic", -1, 0, 0, 3, 2, out HTuple centerRow,
                out HTuple centerCol, out HTuple radius, out HTuple startPhi, out HTuple endPhi, out HTuple pointOrder);

            if (centerRow.Length != 1)
            {
                return Errortype.UBH_WAFERCIRCLE_IS_NOT_ONE;
            }

            if (startPhi > endPhi)
            {
                HTuple temp = startPhi;
                startPhi = endPhi;
                endPhi = temp;
            }

            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = 0.7;
            calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
            calipParam.MeasureLength1 = 100;
            calipParam.MeasureLength2 = 4;
            calipParam.NumMeasures = 120;
            calipParam.MeasureThreshold = 20;
            calipParam.MeasureSelect = "last";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "uniform";
            calipParam.CircleStartPhi = startPhi[0].D;
            calipParam.CircleEndPhi = endPhi[0].D;

            CaliperCircle.CircleArcExtraction(image, new Point(centerCol.D, centerRow.D), radius.D, calipParam, out Point centerOut, out double radiusOut, out double[] arcPointsRow, out double[] arcPointsCol);

            for (int i = 0; i < arcPointsRow.Length; ++i)
            {
                circleEdge.Add(new Point(arcPointsCol[i], arcPointsRow[i]));
            }

            edges.Dispose();
            selectedXld.Dispose();
            unionContours.Dispose();
            img.Dispose();
            return ret;
        }

        /// <summary>
        /// 去除识别到的边缘点中的噪点，噪点多时，Ransac并不能很好地去除
        /// </summary>
        /// <param name="edgePoints">边缘点</param>
        /// <param name="gap">噪点阈值</param>
        /// <param name="usedPoints">非噪声边缘点</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal static Errortype RemoveNoise(List<Point> edgePoints, double gap, out List<Point> usedPoints)
        {
            usedPoints = new List<Point>();
            if (edgePoints.Count < 2)
            {
                return Errortype.UBH_INPUT_POINT_COUNT_LESS_TWO;
            }

            int indexStart = 0;
            int indexEnd = 1;
            List<int> length = new List<int>();
            List<int> startList = new List<int>();
            List<int> endList = new List<int>();
            for (int i = 0; i < edgePoints.Count - 1; i++)
            {
                double distance = Math.Sqrt(Math.Pow(edgePoints[i].X - edgePoints[i + 1].X, 2) +
                                            Math.Pow(edgePoints[i].Y - edgePoints[i + 1].Y, 2));
                if (distance < gap)
                {
                    indexEnd = i + 1;
                }
                else
                {
                    indexStart = i + 1;
                }

                length.Add(indexEnd - indexStart);
                startList.Add(indexStart);
                endList.Add(indexEnd);
            }

            // 找出最长的连续边缘
            HOperatorSet.TupleSortIndex(length.ToArray(), out HTuple indices);
            HOperatorSet.TupleInverse(indices, out HTuple inverted);

            usedPoints = edgePoints.GetRange(startList[inverted[0]], endList[inverted[0]] - startList[inverted[0]] + 1);

            return Errortype.OK;
        }

        /// <summary>
        /// 裁剪固定视野的图像
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="cropSize">裁剪尺寸（mm）</param>
        /// <param name="cropImg">裁剪后的图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CropImg(Camera img, Point cropSize, out Camera cropImg)
        {
            cropImg = null;
            if (img.Height < 1 || cropSize == null)
            {
                return Errortype.UBH_INPUT_PARAM_NULL;
            }

            int newWidth = (int)(Math.Floor(cropSize.X / 4) * 4); //保证裁剪后的图像为4的倍数
            int newHeight = (int)(Math.Floor(cropSize.Y / 4) * 4);
            double stepX = newWidth / 2;
            double stepY = newHeight / 2;
            Point pixCenter = new Point(img.Width / 2, img.Height / 2);
            HObject image = img.GenHObject();
            HOperatorSet.Rgb1ToGray(image, out HObject grayImage);
            HOperatorSet.GenRectangle1(out HObject rectangle, pixCenter.Y - stepY + 1, pixCenter.X - stepX + 1,
                pixCenter.Y + stepY, pixCenter.X + stepX);
            HOperatorSet.ReduceDomain(grayImage, rectangle, out HObject reduceImg);
            HOperatorSet.CropDomain(reduceImg, out HObject cropImagePart);

            cropImg = new Camera(cropImagePart);

            grayImage.Dispose();
            rectangle.Dispose();
            reduceImg.Dispose();
            cropImagePart.Dispose();
            image.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// wafer 模块芯片缺陷检测
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="setParam">输入参数</param>
        /// <param name="regionList">缺陷区域</param>
        /// <param name="region">芯片缺陷检测区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetAoiDefect(Camera image, SetAoiParam setParam, out List<Rectangle1> regionList, Rectangle1 region = null)
        {
            var ret = Errortype.OK;
            regionList = new List<Rectangle1>();

            Rectangle1 rectTmp = new Rectangle1(0, 0, image.Width, image.Height);
            Rectangle1 rect = region == null ? rectTmp : region;

            HOperatorSet.GenEmptyObj(out HObject emptyObject);
            emptyObject = image.GenHObject();

            //通过切割道将产品分割成不同区域
            HOperatorSet.BinaryThreshold(emptyObject, out HObject regionB, "max_separability", setParam.LightDark, out HTuple used);
            HOperatorSet.GetDomain(emptyObject, out HObject domain);
            HOperatorSet.Difference(domain, regionB, out HObject regionDifference);

            if (setParam.AllRegion == true)
            {
                regionDifference = domain;
            }

            // 选出最大的连通区域 相当于是需要处理的产品
            HOperatorSet.Connection(regionDifference, out HObject connectedRegions);
            HOperatorSet.SelectShapeStd(connectedRegions, out HObject selectedRegions, "max_area", 70);
            HOperatorSet.ReduceDomain(emptyObject, selectedRegions, out HObject imageReduced);

            //将图片进行梯度求取
            HOperatorSet.SobelAmp(imageReduced, out HObject edeAmplitude, "sum_abs", setParam.SobelSize);
            HOperatorSet.Threshold(edeAmplitude, out HObject regionThreshold, setParam.MinThresh, setParam.MaxThresh);
            HOperatorSet.SmallestRectangle2(selectedRegions, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
            HOperatorSet.GenRectangle2(out HObject rectangle, row, column, phi, length1 - setParam.SobelSize, length2 - setParam.SobelSize);
            HOperatorSet.Intersection(regionThreshold, rectangle, out HObject regionIntersection);
            HOperatorSet.FillUp(regionIntersection, out HObject regionFillUp);
            HOperatorSet.Connection(regionFillUp, out HObject connectedR);
            HOperatorSet.SelectShape(connectedR, out HObject selectR, "area", "and", setParam.MinAoiArea, 99999);

            HOperatorSet.SmallestRectangle1(selectR, out HTuple rowStart, out HTuple colStart, out HTuple rowEnd, out HTuple colEnd);
            for (int i = 0; i < rowStart.Length; ++i)
            {
                Rectangle1 rectTemp = new Rectangle1(colStart[i].D, rowStart[i].D, colEnd[i].D, rowEnd[i].D);
                regionList.Add(rectTemp);
            }

            selectR.Dispose();
            connectedR.Dispose();
            regionFillUp.Dispose();
            regionIntersection.Dispose();
            rectangle.Dispose();
            regionThreshold.Dispose();
            edeAmplitude.Dispose();
            imageReduced.Dispose();
            selectedRegions.Dispose();
            connectedRegions.Dispose();
            regionDifference.Dispose();
            domain.Dispose();
            regionB.Dispose();
            emptyObject.Dispose();
            return ret;
        }

        /// <summary>
        /// 计算当前视野内中心区域灰度值
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="grayValue">灰度值</param>
        /// <returns>ok：计算成功，其他：计算失败</returns>
        public static Errortype CalcImageGrayValue(Camera image, out double grayValue)
        {
            grayValue = 0.0;
            if (image == null)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
            }

            HObject hImg = image.GenHObject();
            HOperatorSet.GetImageSize(hImg, out HTuple width, out HTuple height);
            HOperatorSet.Rgb1ToGray(hImg, out HObject grayImage);
            HOperatorSet.GenRectangle1(out HObject rectangle, height / 4, width / 4, height * 2 / 3, width * 2 / 3);
            HOperatorSet.Intensity(rectangle, grayImage, out HTuple meanGrayValue, out HTuple deviation);
            grayValue = meanGrayValue;
            hImg.Dispose();
            grayImage.Dispose();
            return Errortype.OK;
        }

        #endregion

        #region  左右龙门坐标转换

        /// <summary>
        /// 计算左右龙门映射矩阵
        /// </summary>
        /// <param name="srcGantryItem">左龙门运动系结构名称</param>
        /// <param name="dstGantryItem">右龙门运动系结构名称</param>
        /// <param name="srcGantryP">左龙门光栅点集</param>
        /// <param name="dstGantryP">右龙门光栅点集</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcGantryFusionMat(String srcGantryItem, string dstGantryItem, List<Point> srcGantryP, List<Point> dstGantryP)
        {
            Errortype ret = Errortype.OK;
            if (srcGantryP.Count < 3 || dstGantryP.Count < 3)
            {
                return Errortype.UBH_INPUT_POINT_COUNT_LESS_THREE;
            }

            List<Point> srcGantryPTrue = new List<Point>();
            List<Point> dstGantryPTrue = new List<Point>();
            for (int i = 0; i < srcGantryP.Count; i++)
            {
                ret = CalibXY.GetInstance().GetRealByRuler(srcGantryItem, srcGantryP[i], out Point srcT);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibXY.GetInstance().GetRealByRuler(dstGantryItem, dstGantryP[i], out Point dstT);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                srcGantryPTrue.Add(srcT);
                dstGantryPTrue.Add(dstT);
            }

            ret = ComAlgo.VectorToRigid(srcGantryPTrue, dstGantryPTrue, out _gantrySre2DstMat,
                out List<Point> error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// 根据左侧光栅坐标计算右侧光栅坐标
        /// </summary>
        /// <param name="srcGantryItem">左龙门运动系结构名称</param>
        /// <param name="dstGantryItem">右龙门运动系结构名称</param>
        /// <param name="srcPoint">左龙门待转光栅</param>
        /// <param name="dstPoint">转换后右龙门光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetGantryDstBySrc(String srcGantryItem, string dstGantryItem, Point srcPoint, out Point dstPoint)
        {
            Errortype ret = Errortype.OK;
            dstPoint = new Point();

            ret = CalibXY.GetInstance().GetRealByRuler(srcGantryItem, srcPoint, out Point srcT);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.AffineTransPoint2d(srcT, _gantrySre2DstMat, out Point dstT);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibXY.GetInstance().GetRulerByReal(dstGantryItem, dstT, out dstPoint);
            return ret;
        }

        #endregion

        #region 精度补偿

        /// <summary>
        /// 加载精度补偿数据
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype LoadAccCompensate()
        {
            string fullFileName = "D:\\Alg\\XY_CalibCoordPoints.ini";
            var ret = Errortype.OK;
            if (!File.Exists(fullFileName))
            {
                return Errortype.UBH_FILE_PATH_IS_NOT_EXIST;
            }

            IniHelper.GetAllKeyValues("PointsNum", out string[] keys, out string[] values, fullFileName);
            int pointsNum = Convert.ToInt32(values[0]);

            List<Point> srcPoints = new List<Point>();
            List<Point> dstPoints = new List<Point>();
            List<Point> anglePoint = new List<Point>();
            using (StreamReader sr = new StreamReader(fullFileName))
            {
                string line = string.Empty;
                int index = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    string strValue = "[Point_" + index.ToString() + "]";
                    if (line == strValue)
                    {
                        double[] axisValue = new double[5];
                        for (int j = 0; j < 5; ++j)
                        {
                            string tempLine = sr.ReadLine();
                            string[] tempLines = tempLine.Split('=');
                            axisValue[j] = double.Parse(tempLines[1]);
                        }

                        srcPoints.Add(new Point(axisValue[0], axisValue[1]));
                        dstPoints.Add(new Point(axisValue[2], axisValue[3]));
                        anglePoint.Add(new Point(axisValue[4], axisValue[4]));
                        index++;
                    }
                }
            }

            List<double[]> srcArray = new List<double[]>();
            for (int i = 0; i < srcPoints.Count; i++)
            {
                srcArray.Add(new double[] { srcPoints[i].X, srcPoints[i].Y });
            }

            _kdTreeXY = new KDTree<double, Point>(2, srcArray.ToArray(), dstPoints.ToArray(), Supercluster.KDTree.Utilities.Utilities.L2Norm_Squared_Double);
            _kdTreeAngle = new KDTree<double, Point>(2, srcArray.ToArray(), anglePoint.ToArray(), Supercluster.KDTree.Utilities.Utilities.L2Norm_Squared_Double);

            return ret;
        }

        /// <summary>
        /// 整个基板计算精度补偿
        /// </summary>
        /// <param name="srcRuler">需要补偿的光栅位置</param>
        /// <param name="accXY">XY方向补偿</param>
        /// <param name="accT">角度补偿</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcAccCompensate(Point srcRuler, out Point accXY, out double accT)
        {
            accT = 0;
            accXY = new Point();
            var ret = Errortype.OK;

            var result = _kdTreeXY.NearestNeighbors(new double[] { srcRuler.X, srcRuler.Y }, 1);
            if (result.Length < 1)
            {
                return Errortype.UBH_RESULT_COUNT_IS_LESS_ONE;
            }

            accXY = result[0].Item2;

            result = _kdTreeAngle.NearestNeighbors(new double[] { srcRuler.X, srcRuler.Y }, 1);
            if (result.Length < 1)
            {
                return Errortype.UBH_RESULT_COUNT_IS_LESS_ONE;
            }

            accT = result[0].Item2.X;

            return ret;
        }

        #endregion

    }

    /// <summary>
    /// 设置标定参数
    /// </summary>
    public class SetCalibParam
    {
        /// <summary>
        /// Gets or sets wafer高倍率相机名称
        /// </summary>
        public string WaferCamNameFine { get; set; }

        /// <summary>
        /// Gets or sets wafer低倍率相机名称
        /// </summary>
        public string WaferCamNameCoarse { get; set; }

        /// <summary>
        /// Gets or sets waferStage机构名称
        /// </summary>
        public string WaferStageName { get; set; }

        /// <summary>
        /// Gets or sets 左工位IC相机名称
        /// </summary>
        public string LeftIcCamName { get; set; }

        /// <summary>
        /// Gets or sets 左工位head相机名称
        /// </summary>
        public string LeftHeadCamName { get; set; }

        /// <summary>
        /// Gets or sets 左工位head机构名称
        /// </summary>
        public string LeftHeadItemName { get; set; }

        /// <summary>
        /// Gets or sets 右工位IC相机名称
        /// </summary>
        public string RightIcCamName { get; set; }

        /// <summary>
        /// Gets or sets 右工位head相机名称
        /// </summary>
        public string RightHeadCamName { get; set; }

        /// <summary>
        /// Gets or sets 右工位head机构名称
        /// </summary>
        public string RightHeadItemName { get; set; }
    }

    /// <summary>
    /// 设置对准参数
    /// </summary>
    public class SetAlignParam
    {
        /// <summary>
        /// Gets or sets 示教时IC左上右下Pattern对应轴坐标
        /// </summary>
        public List<Point> IcPatternRuler { get; set; }

        /// <summary>
        /// Gets or sets 示教时IC 4个Bump点对应的轴坐标
        /// </summary>
        public List<Point> IcBumpRuler { get; set; }

        /// <summary>
        /// Gets or sets 示教时基板左上右下Pattern对应轴坐标
        /// </summary>
        public List<Point> SubPatternRuler { get; set; }

        /// <summary>
        /// Gets or sets 示教时基板 4个Bump点对应的轴坐标(保证与IC Bump点一一对应)
        /// </summary>
        public List<Point> SubBumpRuler { get; set; }
    }

    public class SetTeachParam
    {
        /// <summary>
        /// Gets or Sets 示教wafer die 的模版轴坐标
        /// </summary>
        public List<Point> WaferDiePatternRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教wafer die 的角点轴坐标
        /// </summary>
        public List<Point> WaferDieCornerRuler { get; set; }
    }

    /// <summary>
    /// Aoi检测识别参数
    /// </summary>
    public class SetAoiParam
    {
        /// <summary>
        /// Gets or sets the user's 切割道颜色
        /// </summary>
        public string LightDark { get; set; } = "dark";

        /// <summary>
        /// Gets or sets the user's sobel size
        /// </summary>
        public int SobelSize { get; set; } = 5;

        /// <summary>
        /// Gets or sets the user's 最大阈值
        /// </summary>
        public int MaxThresh { get; set; } = 255;

        /// <summary>
        /// Gets or sets the user's 最小阈值
        /// </summary>
        public int MinThresh { get; set; } = 20;

        /// <summary>
        /// Gets or sets the user's Aoi最小面积
        /// </summary>
        public int MinAoiArea { get; set; } = 1;

        /// <summary>
        /// Gets or sets the user's Aoi是否在全图区域进行检测
        /// </summary>
        public bool AllRegion { get; set; } = false; 
    }
}
