using DataStruct;
using HalconDotNet;
using IniFileHelper;
using System;
using System.Collections.Generic;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using Point = DataStruct.Point;
using UltrapreciseBonding.DieBonding;
using DataStruct;
using System.IO;
using UltrapreciseBonding.UBAManager;
using UltrapreciseBonding.UBCManager;
using System.Text;
using System.Net;
using static UltrapreciseBonding.UBDManager.UBDRecord;
using System.Linq;
using UltrapreciseBonding.AutoFocus;
using UltrapreciseBonding.TemplateMatch;

namespace UltrapreciseBonding.UBDManager
{
    /// <summary>
    /// UBDManager
    /// </summary>
    public static class UBDManager
    {
        private static UBDCalibParam _calibParam = new UBDCalibParam();     // 标定项列表
        private static UBDTeachParam _teachParam = new UBDTeachParam();     // 示教项列表
        private static UBDAxisParam _axisParam = new UBDAxisParam();
        private static string _paramFileDir = "./CalibData";

        //private static KeyValuePair<Point, Point> _fuStagePose = new KeyValuePair<Point, Point>();

        // 误差补偿
        private static Point _coaxiaError = new Point(0.184397632, 0.012788459);  // 同轴偏差 上相机->下相机   下相机->上相机需要加负号 动Stage轴
        private static Point _errorXYCompensate = new Point(0.0074, -0.074);    // 误差补偿
        private static double _errorTCompensate = -0.0045;                      // 角度误差

        ///// <summary>
        ///// Gets or sets 上下相机同轴度误差
        ///// </summary>
        //public static Point CoaxiaError { get => _coaxiaError; set => _coaxiaError = value; }

        ///// <summary>
        ///// Gets or sets 偏移误差补偿
        ///// </summary>
        //public static Point ErrorXYCompensate { get => _errorXYCompensate; set => _errorXYCompensate = value; }

        ///// <summary>
        ///// Gets or sets 角度误差补偿
        ///// </summary>
        //public static double ErrorTCompensate { get => _errorTCompensate; set => _errorTCompensate = value; }
        #region 初始化加载和释放

        /// <summary>
        /// 初始化标定项目
        /// </summary>
        /// <param name="calibParam">标定项信息</param>
        /// <param name="algParamFile">轴信息文件夹</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Init(UBDCalibParam calibParam, string algParamFile = "./CalibData/")
        {
            _calibParam = calibParam;

            // 判断标定项齐全
            if (_calibParam.WaferCamFineName == null || _calibParam.IcDownCamName == null ||
                _calibParam.IcUpCamName == null || _calibParam.BondHeadName == null ||
                _calibParam.WaferStageName == null || _calibParam.SubStageName == null ||
                _calibParam.WaferCamCoarseName == null || _calibParam.FuStageName == null)
            {
                return Errortype.UBD_CALIB_NAME_NULL;
            }

            // 判断相机属性设置完成
            if (_calibParam.ICCamCenter == new Point(0, 0) || _calibParam.WaferCamCenter == new Point(0, 0) || _calibParam.ICCamCenter == null || _calibParam.WaferCamCenter == null)
            {
                return Errortype.UBD_CAMCENTER_NULL;
            }

            List<string> cameraList = new List<string>()
            {
                _calibParam.WaferCamFineName,
                _calibParam.WaferCamCoarseName,
                _calibParam.IcUpCamName,
                _calibParam.IcDownCamName,
            };

            List<string> axisXYList = new List<string>()
            {
                _calibParam.WaferStageName,
                _calibParam.BondHeadName,
                _calibParam.SubStageName,
                _calibParam.IcUpCamName,
                _calibParam.FuStageName,
            };

            List<string> stageCenterList = new List<string>()
            {
                _calibParam.WaferStageName,
                _calibParam.BondHeadName,
                _calibParam.SubStageName,
            };
            List<string> conCenterOpticList = new List<string>()
            {
                _calibParam.IcUpCamName,
            };
            _paramFileDir = algParamFile;
            var ret = LoadAxisParam(algParamFile);
            return ret;
        }

        /// <summary>
        /// 释放
        /// </summary>
        /// <returns>ok:释放成功</returns>
        public static Errortype Release()
        {
            _calibParam = new UBDCalibParam();
            _teachParam = new UBDTeachParam();

            //_fuStagePose = new KeyValuePair<Point, Point>();
            return Errortype.OK;
        }

        #endregion
        
        #region 坐标转换

        /// <summary>
        /// 像素坐标转轴坐标（相机运动）
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="currentRuler">当前轴坐标</param>
        /// <param name="targetMarkPix">目标Mark像素坐标</param>
        /// <param name="targetMarkRuler">目标Mark轴坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRulerByPix(string cameraName, Point currentRuler, Point targetMarkPix, out Point targetMarkRuler)
        {
            return CalibPixScaleXY.GetInstance().GetRulerByPix(cameraName, targetMarkPix, currentRuler, out targetMarkRuler);
        }

        /// <summary>
        /// 像素坐标转head轴坐标（用于head真值标定时把mark move Center）
        /// </summary>
        /// <param name="currentRuler">当前轴坐标</param>
        /// <param name="targetMarkPix">目标Mark像素坐标</param>
        /// <param name="targetMarkRuler">目标Mark轴坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetHeadRulerByPix(Point currentRuler, Point targetMarkPix, out Point targetMarkRuler)
        {
            var headName = _calibParam.BondHeadName;
            return CalibPixScaleXY.GetInstance().GetRulerByPix(headName, targetMarkPix, currentRuler, out targetMarkRuler);
        }

        /// <summary>
        /// FU相机像素转FU轴坐标
        /// </summary>
        /// <param name="currentRuler">当前FU轴坐标</param>
        /// <param name="targetMarkPix">当前像素坐标XY</param>
        /// <param name="targetMarkRuler">输出当前目标的FU轴坐标</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype GetFuRulerByPix(Point currentRuler, Point targetMarkPix, out Point targetMarkRuler)
        {
            var stageName = _calibParam.FuStageName;
            return CalibPixScaleXY.GetInstance().GetRulerByPix(stageName, targetMarkPix, currentRuler, out targetMarkRuler);
        }

        /// <summary>
        /// 同轴上相机像素坐标转下相机像素坐标
        /// </summary>
        /// <param name="topPix">上相机采集的像素坐标</param>
        /// <param name="bottomPix">转换到下相机像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetBottomByTop(Point topPix, out Point bottomPix)
        {
            var opticName = _calibParam.IcUpCamName;
            return CalibCoaxiality.GetInstance().GetBottomPixel(opticName, topPix, out bottomPix);
        }

        #endregion

        #region 流程接口

        /// <summary>
        /// 加载需要使用的模版
        /// </summary>
        /// <param name="templateDir">模版目录文件夹</param>
        /// <param name="templateNames">模版名称</param>
        /// <param name="loadFinish">返回模版加载状态</param>
        /// <returns>OK：加载结束</returns>
        public static Errortype LoadTemplate(string templateDir, List<string> templateNames, out Dictionary<string, Errortype> loadFinish)
        {
            MatchRegionManager.LoadMatchRegion(templateNames, templateDir);
            return TemplateManager.Load(templateDir, templateNames, out loadFinish);
        }

        /// <summary>
        /// 释放模版数据
        /// </summary>
        /// <returns>OK:释放结束</returns>
        public static Errortype ClearTemplate()
        {
            return TemplateManager.Release();
        }

        /// <summary>
        /// die拉直全流程接口
        /// </summary>
        /// <param name="dieMatchData">die 定位图像和匹配参数</param>
        /// <param name="currentRuler">拍摄图像时的wafer stage光栅坐标</param>
        /// <param name="dieSize">die的长宽</param>
        /// <param name="waferAngle">输出拉直旋转角度</param>
        /// <param name="dieCenterRuler">输出先转后移到die中心的光栅坐标</param>
        /// <returns>ok:计算成功，其他：计算失败</returns>
        public static Errortype CalcWaferAngleDie(List<KeyValuePair<Camera, string>> dieMatchData, List<Point> currentRuler, Point dieSize, 
            out double waferAngle, out Point dieCenterRuler)
        {
            waferAngle = 0.0;
            dieCenterRuler = currentRuler[0];
            if (dieMatchData.Count != 2)
            {
                return Errortype.UBD_DIEISEXIST_INPUT_IMAGE_NULL;
            }

            if (dieMatchData.Count != currentRuler.Count)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            var ret = Errortype.UNKNOW_ERROR;
            List<Point> dieCornerPoint = new List<Point>();
            bool useCutPath = true;
            for (int i = 0; i < dieMatchData.Count; i++)
            {
                string templateName = dieMatchData[i].Value;
                Camera dieImage = dieMatchData[i].Key;
                if ((templateName == null) || (templateName == string.Empty))
                {
                    return Errortype.UBD_DIEISEXIST_INPUT_IMAGE_NULL;
                }

                if ((templateName == "leftTop") || (templateName == "RightBottom"))
                {
                    ret = CalcCutPathCorner(dieImage, out Point dieCornerPix, out double grayValue, null,
                        templateName);
                    if (ret != Errortype.OK)
                    {
                        // 记录图像
                        if (ComAlgo.SaveFlg("TemplateMatchError", out int days))
                        {
                            string path = @"D:\Alg\CutPathErrorImg\";
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }

                            ComAlgo.SaveImage(path, dieImage, null, 7);
                        }

                        return ret;
                    }

                    ret = GetRulerByPix(_calibParam.WaferCamFineName, currentRuler[i], dieCornerPix,
                        out Point dieCornerRuler);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    dieCornerPoint.Add(dieCornerRuler);
                }
                else
                {
                    useCutPath = false;
                    MatchRegionManager.GetMatchRegion(templateName, out Rectangle1 matchRectangle1);
                    Region matchRegion = new Region();
                    matchRegion.Rectangle1List.Add(matchRectangle1);
                    ret = TemplateManager.Match(templateName, dieImage, matchRegion, out double[] rows, out double[] cols,
                        out double[] angles, out _, out double[] scores);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    if (rows.Length != 1)
                    {
                        // 记录图像
                        if (ComAlgo.SaveFlg("TemplateMatchError", out int days))
                        {
                            string path = @"D:\Alg\TemplateMatchErrorImg\";
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }

                            ComAlgo.SaveImage(path, dieImage, null, 7);
                        }

                        return Errortype.UBD_CALCWAFERANGLE_DIE_NONE;
                    }

                    Point dieCornerPix = new Point(cols[0], rows[0]);
                    ret = GetRulerByPix(_calibParam.WaferCamFineName, currentRuler[i], dieCornerPix,
                        out Point dieCornerRuler);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    dieCornerPoint.Add(dieCornerRuler);
                }
            }

            if (dieCornerPoint.Count != 2)
            {
                return Errortype.UBD_CALCWAFERANGLE_DIE_NONE;
            }

            ret = CalcWaferAngleDie(dieCornerPoint[0], dieCornerPoint[1], dieSize, out waferAngle, out dieCenterRuler,
                useCutPath);

            return Errortype.OK;
        }

        #endregion

        /// <summary>
        /// IC计算中心到head中心距离，Head吸取IC中心矫正
        /// </summary>
        /// <param name="headPoints">Head角点对应轴坐标</param>
        /// <param name="icPoints">IC角点对应轴坐标</param>
        /// <param name="xyError">平移偏差(偏差XY方向与输入轴坐标同向)</param>
        /// <param name="angleError">旋转偏差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype HeadICCenterCorrect(List<Point> headPoints, List<Point> icPoints, out Point xyError, out double angleError)
        {
            xyError = new Point();
            angleError = 0;
            if (headPoints == null || icPoints == null)
            {
                return Errortype.UBD_INPUT_POINT_NULL;
            }

            if (headPoints.Count != icPoints.Count)
            {
                return Errortype.UBD_INPUT_POINT_NOT_EQUAL;
            }

            if (headPoints.Count < 3)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            double[] headX = new double[headPoints.Count];
            double[] headY = new double[headPoints.Count];
            double[] icX = new double[icPoints.Count];
            double[] icY = new double[icPoints.Count];
            for (int i = 0; i < headPoints.Count; i++)
            {
                headX[i] = headPoints[i].X;
                headY[i] = headPoints[i].Y;
                icX[i] = icPoints[i].X;
                icY[i] = icPoints[i].Y;
            }

            HOperatorSet.GenContourPolygonXld(out HObject headContour, headY, headX);
            HOperatorSet.GenContourPolygonXld(out HObject icContour, icY, icX);

            //HOperatorSet.SmallestRectangle2Xld(headContour, out HTuple headCenterRow, out HTuple headCenterColumn, out HTuple headPhi, out HTuple headL1, out HTuple headL2);
            //HOperatorSet.SmallestRectangle2Xld(icContour, out HTuple icCenterRow, out HTuple icCenterColumn, out HTuple icPhi, out HTuple icL1, out HTuple icL2);
            HOperatorSet.SmallestCircleXld(headContour, out HTuple headCenterRow, out HTuple headCenterColumn, out HTuple headRadius);
            HOperatorSet.SmallestCircleXld(icContour, out HTuple icCenterRow, out HTuple icCenterColumn, out HTuple icRadius);

            //HOperatorSet.TupleDeg(headPhi, out HTuple headDeg);
            //HOperatorSet.TupleDeg(icPhi, out HTuple icDeg);
            headContour.Dispose();
            icContour.Dispose();
            xyError.X = headCenterColumn - icCenterColumn;
            xyError.Y = headCenterRow - icCenterRow;

            //HTuple angleDeg = headDeg - icDeg;
            //angleError = headDeg - icDeg
            //// 计算水平或竖直方向的最小倾角 (夹角和最小90°倍数差)
            //HOperatorSet.TupleRound(angleDeg / 90.0, out HTuple roundRate);
            //var resDeg = angleDeg - (90.0 * roundRate);
            angleError = 0.0;

            return Errortype.OK;
        }

        /// <summary>
        /// 计算ICsize及PitchSize
        /// </summary>
        /// <param name="leftTopReal">左上角点</param>
        /// <param name="rightBottomReal">右下角点</param>
        /// <param name="anotherLeftTop">另一颗Ic左上角点</param>
        /// <param name="icSize">IC尺寸</param>
        /// <param name="pitchSize">Pitch尺寸</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetICPitchSize(Point leftTopReal, Point rightBottomReal, Point anotherLeftTop, out Point icSize, out Point pitchSize)
        {
            icSize = new Point();
            pitchSize = new Point();

            if (leftTopReal == null || rightBottomReal == null || anotherLeftTop == null)
            {
                return Errortype.UBD_INPUT_POINT_NULL;
            }

            if (leftTopReal == rightBottomReal || leftTopReal == anotherLeftTop)
            {
                return Errortype.UBD_INPUT_POINT_EQUAL;
            }

            icSize.X = Math.Abs(leftTopReal.X - rightBottomReal.X);
            icSize.Y = Math.Abs(leftTopReal.Y - rightBottomReal.Y);
            pitchSize.X = Math.Abs(leftTopReal.X - anotherLeftTop.X);
            pitchSize.Y = Math.Abs(leftTopReal.Y - anotherLeftTop.Y);
            return Errortype.OK;
        }

        /// <summary>
        /// 识别切割道角点
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="centerPoint">切割道十字中心像素坐标</param>
        /// /// <param name="grayValue">输出中心区域像素值</param>
        /// <param name="selectRegion">识别区域</param>
        /// <param name="leftTOrRightB">区分左上角(leftTopReal)和右下角(rightBottomReal)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcCutPathCorner(Camera img, out Point centerPoint, out double grayValue, Region selectRegion = null, string leftTOrRightB = "leftTop")
        {
            grayValue = 0.0;
            centerPoint = new Point();
            Errortype ret = CalcImageGrayValue(img, out double value);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (leftTOrRightB == "leftTop")
            {
                selectRegion = new Region();
                selectRegion.Rectangle1 = new Rectangle1(0, 0, img.Width / 2, img.Height / 2);
            }
            else
            {
                selectRegion = new Region();
                selectRegion.Rectangle1 = new Rectangle1(img.Width / 2, img.Height / 2, img.Width - 1, img.Height - 1);
            }

            grayValue = value;
            return DieBondComAlgo.CalcCutPathCorner(img, out centerPoint, selectRegion, leftTOrRightB);
        }

        /// <summary>
        /// 计算head IC角点坐标
        /// </summary>ss
        /// <param name="image">图像</param>
        /// <param name="cornerPix">角点光栅</param>
        /// <param name="lineScore">角点得分</param>
        /// <param name="leftTOrRightB">左上角点还是右下角点</param>
        /// <param name="region">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetHeadICCorner(Camera image, out Point cornerPix, out double lineScore, string leftTOrRightB = "leftTop", Rectangle1 region = null)
        {
            cornerPix = new Point();
            Errortype ret = Errortype.OK;
            lineScore = 0;

            if (image == null || image.Width < 1)
            {
                return Errortype.UBH_INPUT_IMG_NULL;
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
            calipParam.MinScore = 0.3;      // 可能出现比较短线段，分数会低
            calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
            calipParam.MeasureLength1 = (xEnd - xStart) > (yEnd - yStart) ? (yEnd - yStart) / 2 : (xEnd - xStart) / 2;
            calipParam.MeasureLength2 = 4;
            calipParam.NumMeasures = 360;
            calipParam.MeasureThreshold = 20;
            calipParam.MeasureSelect = measureSelect;
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = measureTransition;

            List<LineSeg> lines = new List<LineSeg>();
            double errorsum = 1.0;
            for (int i = 0; i < rowBegin.Length; ++i)
            {
                ret = CaliperLine.LineExtraction(image, new LineSeg(colBegin[i], rowBegin[i], colEnd[i], rowEnd[i]),
                    calipParam, out LineSeg lineOut, out _, out double score);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                lines.Add(lineOut);

                if (score < errorsum)
                {
                    errorsum = score;
                }
            }

            lineScore = errorsum;
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
        /// 计算wafer stage旋转后的点坐标
        /// </summary>
        /// <param name="originPoint">旋转前的点轴坐标</param>
        /// <param name="angleDeg">旋转角度</param>
        /// <param name="rotatePoint">旋转后的点坐标</param>
        /// <returns>ok：计算完成</returns>
        public static Errortype CalcWaferStageRotatePoint(Point originPoint, double angleDeg, out Point rotatePoint)
        {
            rotatePoint = null;

            // 绕旋转中心旋转
            Errortype ret = CalibXYT.GetInstance().GetRotateCenter(_calibParam.WaferStageName, false, originPoint, out Point currentRotateCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.CalcRotatePoint(originPoint, angleDeg, currentRotateCenter, out rotatePoint);
            return ret;
        }

        /// <summary>
        /// wafer自动拉直接口(默认使用小视野相机)
        /// </summary>
        /// <param name="startPoint">轴坐标点1</param>
        /// <param name="endPoint">轴坐标点2</param>
        /// <param name="waferAngle">wafer上料夹角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcWaferAngle(Point startPoint, Point endPoint, out double waferAngle)
        {
            waferAngle = 0;
            if (startPoint == null || endPoint == null)
            {
                return Errortype.UBD_CALCWAFERANGLE_INPUT_POINT_NULL;
            }

            if (endPoint == startPoint)
            {
                return Errortype.UBD_CALCWAFERANGLE_INPUT_POINT_EQUAL;
            }

            //计算角度
            var ret = DieBondComAlgo.CalcWaferAngle(startPoint, endPoint, out waferAngle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// wafer单颗die拉直接口
        /// </summary>
        /// <param name="startPoint">die左上角点轴坐标</param>
        /// <param name="endPoint">die右下角点轴坐标</param>
        /// <param name="dieSize">单颗die的size</param>
        /// <param name="waferAngle">wafer上料夹角</param>
        /// <param name="rotatedCenter">旋转后的die中心</param>
        /// <param name="isCutPath">是否为切割道角点</param>
        /// <returns>OK:成功；其他：失败</returns>
        public static Errortype CalcWaferAngleDie(Point startPoint, Point endPoint, Point dieSize, out double waferAngle, out Point rotatedCenter, bool isCutPath = true)
        {
            waferAngle = 0;
            rotatedCenter = new Point();
            List<Point> featurePoints = new List<Point> { startPoint, endPoint };
            if (startPoint == null || endPoint == null || dieSize is null)
            {
                return Errortype.UBD_CALCWAFERANGLE_INPUT_POINT_NULL;
            }

            if (startPoint == endPoint)
            {
                return Errortype.UBD_CALCWAFERANGLE_INPUT_POINT_EQUAL;
            }

            if (!isCutPath)
            {
                List<Point> diePatternPoints = new List<Point> { startPoint, endPoint };
                var res = DieBondComAlgo.RigidTransPoint2d(_teachParam.WaferDiePatternAtRuler, diePatternPoints, _teachParam.WaferDieCornerAtRuler, out List<Point> dieCornerPoints);
                if (res != Errortype.OK)
                {
                    return res;
                }

                startPoint = dieCornerPoints[0];
                endPoint = dieCornerPoints[1];
            }

            // 判断左上右下角尺寸是否和预设接近，尺寸超过10%判定为当前die错误，需要软件调到下一颗
            double dieSizeRate = endPoint.DistanceTo(startPoint) / dieSize.DistanceTo(new Point(0, 0));
            if (Math.Abs(dieSizeRate - 1.0) > 0.1)
            {
                return Errortype.UBD_CALCWAFERANGLE_DIE_NONE;
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

            HOperatorSet.TupleAtan2(dieSize.Y, dieSize.X, out HTuple asin);
            HOperatorSet.TupleDeg(-asin, out HTuple asinDeg);

            //deg = deg - asinDeg;
            deg = -deg + asinDeg;
            waferAngle = deg.D;
            double dieAngle = deg.D;

            LoadAxisParam(_paramFileDir);
            if (Math.Abs(waferAngle) < _axisParam.WaferAngleDie)
            {
                waferAngle = 0.0;
            }

            // 绕旋转中心旋转
            Errortype ret = CalibXYT.GetInstance().GetRotateCenter(_calibParam.WaferStageName, false, endPoint, out Point currentRotateCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.CalcRotatePoint((startPoint + endPoint) / 2.0, waferAngle, currentRotateCenter, out rotatedCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (ComAlgo.SaveFlg("CalcWaferAngleDie", out int days))
            {
                //存储软件传给的角点坐标和算法计算出的旋转后的单颗die中心点坐标
                string path = @"D:\Alg\";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = path + "\\CalcWaferAngleDie.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                string text = string.Empty;
                string sep = " ";

                text = string.Empty;
                text += time + sep;
                text += " startPoint: " + featurePoints[0].X.ToString() + sep + featurePoints[0].Y.ToString() +
                        " endPoint: " + featurePoints[1].X.ToString() + sep + featurePoints[1].Y.ToString() +
                        " dieAngle: " + dieAngle + " currentRotateCenter: " + currentRotateCenter.X + sep + currentRotateCenter.Y +
                        " rotateAngle: " + waferAngle + " rotatedCenter: " + rotatedCenter.X.ToString() + sep + rotatedCenter.Y.ToString() +
                        " dieSizeRate: " + dieSizeRate.ToString();
                sw.WriteLine(text);

                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 添加示教对准坐标点集
        /// </summary>
        /// <param name="icPatternRuler">IC左上右下Pattern对应轴坐标</param>
        /// <param name="subPatternRuler">基板左上右下Pattern对应轴坐标</param>
        /// <param name="icBumpRuler">IC 4个Bump点（eye point）对应轴坐标</param>
        /// <param name="subBumpRuler">基板4个Bump点对应轴坐标（保证与ICBump点一一对应）</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AddTeachPoints(List<Point> icPatternRuler, List<Point> subPatternRuler, List<Point> icBumpRuler, List<Point> subBumpRuler)
        {
            if (icPatternRuler.Count != 2 || subPatternRuler.Count != 2)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            if (icBumpRuler.Count < 2 || subBumpRuler.Count < 2)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            if (icBumpRuler.Count != subBumpRuler.Count)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            // 记录IC pattern和bump轴坐标
            _teachParam.IcPatternAtRuler = icPatternRuler;
            _teachParam.IcBumpAtRuler = icBumpRuler;

            // 记录IC pattern和bump真值坐标
            _teachParam.IcBumpAtReal = new List<Point>();
            _teachParam.IcPatternAtReal = new List<Point>();

            foreach (var icBump in _teachParam.IcBumpAtRuler)
            {
                CalibXY.GetInstance().GetRealByRuler(_calibParam.IcUpCamName, icBump, out Point bumpAtReal);
                _teachParam.IcBumpAtReal.Add(bumpAtReal);
            }

            foreach (var icPattern in _teachParam.IcPatternAtRuler)
            {
                CalibXY.GetInstance().GetRealByRuler(_calibParam.IcUpCamName, icPattern, out Point patternAtReal);
                _teachParam.IcPatternAtReal.Add(patternAtReal);
            }

            // 记录sub pattern和bump在top的轴坐标
            //GetTopByBottom(_calibParam.ICCamCenter, out Point subCenterToTopPix);
            _teachParam.SubPatternAtRuler = new List<Point>();
            _teachParam.SubBumpAtRuler = new List<Point>();

            foreach (var rulerPose in subPatternRuler)
            {
                //GetRulerByPix(_calibParam.IcUpCamName, rulerPose, subCenterToTopPix, out Point subPatternAtTop);
                //GetRulerByPix(_calibParam.IcDownCamName, rulerPose, _calibParam.ICCamCenter, out Point subPatternAtTop);
                _teachParam.SubPatternAtRuler.Add(rulerPose);
            }

            foreach (var rulerPose in subBumpRuler)
            {
                //GetRulerByPix(_calibParam.IcUpCamName, rulerPose, subCenterToTopPix, out Point subBumpAtTop);
                //GetRulerByPix(_calibParam.IcDownCamName, rulerPose, _calibParam.ICCamCenter, out Point subBumpAtTop);
                _teachParam.SubBumpAtRuler.Add(rulerPose);
            }

            // 记录sub pattern和bump真值坐标
            _teachParam.SubBumpAtReal = new List<Point>();
            _teachParam.SubPatternAtReal = new List<Point>();

            foreach (var subBump in _teachParam.SubBumpAtRuler)
            {
                CalibXY.GetInstance().GetRealByRuler(_calibParam.IcUpCamName, subBump, out Point bumpAtReal);
                _teachParam.SubBumpAtReal.Add(bumpAtReal);
            }

            foreach (var subPattern in _teachParam.SubPatternAtRuler)
            {
                CalibXY.GetInstance().GetRealByRuler(_calibParam.IcUpCamName, subPattern, out Point patternAtReal);
                _teachParam.SubPatternAtReal.Add(patternAtReal);
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
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            // 记录die pattern和 角点 轴坐标
            _teachParam.WaferDiePatternAtRuler = waferDiePatternRuler;
            _teachParam.WaferDieCornerAtRuler = waferDieCornerRuler;

            return Errortype.OK;
        }

        /// <summary>
        /// 添加FuStage die 拉直示教坐标点集
        /// </summary>
        /// <param name="fuDiePatternRuler">die 左上右下Pattern对应轴坐标</param>
        /// <param name="fuDieCornerRuler">die 左上右下角点对应轴坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AddFuTeachPoints(List<Point> fuDiePatternRuler, List<Point> fuDieCornerRuler)
        {
            if (fuDiePatternRuler.Count != 2 || fuDieCornerRuler.Count != 2)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            // 记录die pattern和 角点 轴坐标
            _teachParam.FuDiePatternAtRuler = fuDiePatternRuler;
            _teachParam.FuDieCornerAtRuler = fuDieCornerRuler;

            return Errortype.OK;
        }

        /// <summary>
        /// 计算 FuStage Die 中心补偿量
        /// </summary>
        /// <param name="dieMatchData">die 定位图像和匹配参数</param>
        /// <param name="currentRuler">拍摄图像时的wafer stage光栅坐标</param>
        /// <param name="dieSize">die的长宽</param>
        /// <param name="dieCenterOffSet">输出先转后移到die中心的光栅坐标</param>
        /// <returns>ok:计算成功，其他：计算失败</returns>
        public static Errortype GetFUSendRuler(List<KeyValuePair<Camera, string>> dieMatchData, List<Point> currentRuler, Point dieSize, out Point dieCenterOffSet)
        {
            dieCenterOffSet = currentRuler[0];
            if (dieMatchData.Count != 2)
            {
                return Errortype.UBD_DIEISEXIST_INPUT_IMAGE_NULL;
            }

            if (dieMatchData.Count != currentRuler.Count)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            var ret = Errortype.UNKNOW_ERROR;
            List<Point> dieCornerPoint = new List<Point>();
            for (int i = 0; i < dieMatchData.Count; i++)
            {
                string templateName = dieMatchData[i].Value;
                Camera dieImage = dieMatchData[i].Key;
                if ((templateName == null) || (templateName == string.Empty))
                {
                    return Errortype.UBD_DIEISEXIST_INPUT_IMAGE_NULL;
                }

                MatchRegionManager.GetMatchRegion(templateName, out Rectangle1 matchRectangle1);
                Region matchRegion = new Region();
                matchRegion.Rectangle1List.Add(matchRectangle1);
                ret = TemplateManager.Match(templateName, dieImage, matchRegion, out double[] rows, out double[] cols,
                    out double[] angles, out _, out double[] scores);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                if (rows.Length != 1)
                {
                    // 记录图像
                    if (ComAlgo.SaveFlg("TemplateMatchError", out int days))
                    {
                        string path = @"D:\Alg\TemplateMatchErrorImg\";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        ComAlgo.SaveImage(path, dieImage, null, 7);
                    }

                    return Errortype.UBD_CALCWAFERANGLE_DIE_NONE;
                }

                Point dieCornerPix = new Point(cols[0], rows[0]);
                ret = GetRulerByPix(_calibParam.FuStageName, currentRuler[i], dieCornerPix,
                    out Point dieCornerRuler);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                dieCornerPoint.Add(dieCornerRuler);
            }

            if (dieCornerPoint.Count != 2)
            {
                return Errortype.UBD_CALCWAFERANGLE_DIE_NONE;
            }

            dieCenterOffSet = ((dieCornerPoint[0] + dieCornerPoint[1]) * 0.5) - ((currentRuler[0] + currentRuler[1]) * 0.5);
            return Errortype.OK;
        }

        /// <summary>
        /// FaceUp相机定位IC吸取位置
        /// </summary>
        /// <param name="icTeachCornerRuler">示教的IC角点轴坐标</param>
        /// <param name="icTeachPatternRuler">示教的模版点轴坐标</param>
        /// <param name="icPatternRuler">当前IC左上右下轴坐标</param>
        /// <param name="headPickUpOffset">更新后Head拾取的轴坐标</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype GetFUSendRuler(List<Point> icTeachCornerRuler, List<Point> icTeachPatternRuler, List<Point> icPatternRuler, out Point headPickUpOffset)
        {
            headPickUpOffset = new Point(0, 0);

            //todo：1.计算当前IC中心轴坐标；2.计算当前IC中心和示教IC中心距离D；3.将D补偿到示教的stage传送终点坐标中
            // 这里数量若改为单个模板识别，也不用做=2的判断，需要先确认FU视野里可以看到一个芯片
            if (icPatternRuler.Count != 2)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            if (icTeachPatternRuler.Count != 2)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            Point teachSum = icTeachCornerRuler[0] + icTeachCornerRuler[1];
            Point teachCenter = teachSum / 2.0; //由单个模板定位后，这里传入示教的模板中心坐标

            var res = DieBondComAlgo.RigidTransPoint2d(icTeachPatternRuler, icPatternRuler, icTeachCornerRuler, out List<Point> currentDieCornerPoints);
            if (res != Errortype.OK)
            {
                return res;
            }

            Point curDieCenterSum = currentDieCornerPoints[0] + currentDieCornerPoints[1];
            Point icCenter = curDieCenterSum / 2.0; //这里是识别到的模板中心坐标，剩下的不变
            Point distOffset = icCenter - teachCenter;
            headPickUpOffset = distOffset;

            // record
            if (ComAlgo.SaveFlg("CalcFUOffset", out int days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");

                string text = string.Empty;
                text += time + sep;

                // real
                text += "headPickUpOffsetX: " + headPickUpOffset.X.ToString() + sep + "headPickUpOffsetY: " + headPickUpOffset.Y.ToString() + sep;

                string fileName = path + "\\CalcFUOffset.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算head下压IC所需旋转平移量
        /// </summary>
        /// <param name="icPatternRuler">当前IC左上右下Pattern在相机轴中的坐标</param>
        /// <param name="subPatternRuler">当前基板左上右下Pattern在相机轴中的坐标</param>
        /// <param name="currentHeadRuler">识别时head的轴坐标</param>
        /// <param name="angleOffset">旋转量补偿，角度，顺时针为正</param>
        /// <param name="translateOffset">平移量补偿</param>
        /// <param name="offsetConcenter">传入同轴度补偿值补偿</param>
        /// <param name="offsetXY">传入XY误差补偿</param>
        /// <param name="offsetT">传入旋转误差补偿</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetXYThetaOffset(List<Point> icPatternRuler, List<Point> subPatternRuler, Point currentHeadRuler, out double angleOffset, out Point translateOffset, Point offsetConcenter = null, Point offsetXY = null, double offsetT = 0.0)
        {
            angleOffset = 0;
            translateOffset = new Point();

            if (_calibParam.BondHeadName == null || _calibParam.SubStageName == null)
            {
                return Errortype.UBD_CALIB_NAME_NULL;
            }

            if (_calibParam.IcUpCamName == null || _calibParam.IcDownCamName == null)
            {
                return Errortype.UBD_CALIB_NAME_NULL;
            }

            if (_teachParam.SubPatternAtReal == null || _teachParam.IcPatternAtReal == null || _teachParam.IcBumpAtReal == null || _teachParam.SubBumpAtReal == null)
            {
                return Errortype.UBD_GETXYTHETAOFFSET_TEACHPARAMPOINT_NULL;
            }

            if (icPatternRuler.Count != 2 || subPatternRuler.Count != 2)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            if (_teachParam.SubPatternAtReal.Count != _teachParam.IcPatternAtReal.Count || _teachParam.IcBumpAtReal.Count != _teachParam.SubBumpAtReal.Count)
            {
                return Errortype.UBH_INPUT_POINT_LENGTH_ERROR;
            }

            List<Point> currentIcPatternReals = new List<Point>();
            List<Point> currentSubPatternReals = new List<Point>();

            List<Point> icGantryReals = new List<Point>();
            List<Point> subGantryReals = new List<Point>();

            Errortype ret;
            for (int i = 0; i < icPatternRuler.Count; i++)
            {
                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.IcUpCamName, icPatternRuler[i], out Point dstIcPatternReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                currentIcPatternReals.Add(dstIcPatternReal);

                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.IcUpCamName, subPatternRuler[i], out Point dstSubPatternReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                currentSubPatternReals.Add(dstSubPatternReal);
            }

            // 根据current pattern估算当前Bump真值
            ret = DieBondComAlgo.RigidTransPoint2d(_teachParam.IcPatternAtReal, currentIcPatternReals, _teachParam.IcBumpAtReal, out List<Point> curIcBumpReals);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = DieBondComAlgo.RigidTransPoint2d(_teachParam.SubPatternAtReal, currentSubPatternReals, _teachParam.SubBumpAtReal, out List<Point> curSubBumpReals);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //获取当前旋转中心
            ret = CalibXYT.GetInstance().GetRotateCenter(_calibParam.BondHeadName, true, currentHeadRuler,
                out Point curRotateCenter, 1, -1);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //计算真值下的旋转平移量, rotateResidual点集旋转残差，xyError点集平移残差后期可加入分析
            ret = ComAlgo.CalcPointsRT(curIcBumpReals, curSubBumpReals, curRotateCenter, out double angleDeg, out Point xyOffset, out double rotateResidual, out double xyError);
            if (offsetConcenter == null)
            {
                _coaxiaError = new Point(0.0, 0.0);
                offsetConcenter = _coaxiaError;
            }
            else
            {
                _coaxiaError = offsetConcenter;
            }

            if (offsetXY == null)
            {
                _errorXYCompensate = new Point(0.0, 0.0);
                offsetXY = _errorXYCompensate;
            }
            else
            {
                _errorXYCompensate = offsetXY;
            }

            _errorTCompensate = offsetT;

            if (ret != Errortype.OK)
            {
                return ret;
            }

            Point rulerOffset = xyOffset + _coaxiaError;

            //如果标定了head和camera轴关系则使用改标定转换出head运动量
            CalibCoord calibHead = new CalibCoord(_calibParam.BondHeadName);
            ret = calibHead.Load(_paramFileDir);
            if (ret == Errortype.OK)
            {
                calibHead.Dst2Src(xyOffset + _coaxiaError, out rulerOffset, out _);
            }

            //更新轴方向参数
            LoadAxisParam(_paramFileDir);
            translateOffset = new Point(rulerOffset.X * _axisParam.HeadAxisX, rulerOffset.Y * _axisParam.HeadAxisY) + _errorXYCompensate;
            angleOffset = angleDeg * _axisParam.HeadTheta + _errorTCompensate;

            // 记录对齐bump点位数据
            if (ComAlgo.SaveFlg("CalcAlignOffset", out int days))
            {
                string path = @"D:\Alg\";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = path + "\\CalcBondOffset.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                string text = string.Empty;
                string sep = " ";

                int counts = (int)curIcBumpReals.Count / 2;

                for (int i = 0; i < counts; ++i)
                {
                    text = string.Empty;
                    text += time + sep;
                    text += icPatternRuler[i].ToString(sep) + sep + icPatternRuler[i + counts].ToString(sep) + sep +
                            subPatternRuler[i].ToString(sep) + sep + subPatternRuler[i + counts].ToString(sep);
                    text += curIcBumpReals[i].ToString(sep) + sep + curIcBumpReals[i + counts].ToString(sep) + sep +
                            curSubBumpReals[i].ToString(sep) + sep + curSubBumpReals[i + counts].ToString(sep);
                    text += _teachParam.IcPatternAtReal[0].ToString(sep) + sep + _teachParam.IcPatternAtReal[1].ToString(sep) + sep +
                            _teachParam.SubPatternAtReal[0].ToString(sep) + sep + _teachParam.SubPatternAtReal[1].ToString(sep);

                    sw.WriteLine(text);
                }

                text = string.Empty;
                if ((Math.Abs(angleOffset) > _axisParam.BondICAngle) || (Math.Abs(translateOffset.X) > _axisParam.BondICOffsetX) || (Math.Abs(translateOffset.Y) > _axisParam.BondICOffsetY))
                {
                    text += '0'.ToString() + sep + time + sep;
                }
                else
                {
                    text += '1'.ToString() + sep + time + sep;
                }

                text += currentIcPatternReals[0].ToString(sep) + sep + currentIcPatternReals[1].ToString(sep) + sep +
                        currentSubPatternReals[0].ToString(sep) + sep + currentSubPatternReals[1].ToString(sep) + sep +
                        offsetConcenter.ToString(sep) + sep + offsetXY.ToString(sep) + sep + offsetT.ToString() + sep +

                        //_teachParam.IcPatternAtReal[0].ToString(sep) + sep + _teachParam.IcPatternAtReal[1].ToString(sep) + sep +
                        //_teachParam.SubPatternAtReal[0].ToString(sep) + sep + _teachParam.SubPatternAtReal[1].ToString(sep) + sep +
                        curRotateCenter.ToString(sep) + sep +
                        angleOffset.ToString("f6") + sep + translateOffset.ToString(sep);
                sw.WriteLine(text);

                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 玻璃IC下压精度计算
        /// </summary>
        /// <param name="img">键合后拍摄的左上、右下Mark两幅图像</param>
        /// <param name="cameraRuler">相机在左上、右下采图时对应的轴坐标</param>
        /// <param name="currentHead">当前head轴坐标</param>
        /// <param name="angleError">键合角度误差</param>
        /// <param name="translateError">键合精度</param>
        /// <param name="markParams"> mark 抓取配置参数</param>
        /// <param name="rowId"> 当前check 的行号 </param>
        /// <param name="colId"> 当前check 的列号 </param>
        /// <returns>ok:成功，其他：失败</returns>
        public static Errortype CalcGlassBondAccuracy(List<Camera> img, List<Point> cameraRuler, Point currentHead, out double angleError, out Point translateError, GlassIcMarkParams markParams = null, string rowId = null, string colId = null)
        {
            angleError = 0;
            translateError = new Point();
            List<Point> icMarkReals = new List<Point>();
            List<Point> subMarkReals = new List<Point>();
            List<Point> icMarkRulers = new List<Point>();
            List<Point> subMarkRulers = new List<Point>();
            Errortype ret = Errortype.OK;
            int days = 0;
            if (img == null || cameraRuler == null || currentHead == null)
            {
                return Errortype.UBD_CALCGLASSBONDACCURACY_PARAM_NULL;
            }

            if (img.Count < 1)
            {
                return Errortype.UBD_CALCGLASSBONDACCURACY_IMG_NUM_ERROR;
            }

            if (cameraRuler.Count < 1)
            {
                return Errortype.UBD_CALCGLASSBONDACCURACY_RULER_POINTS_NUM_ERROR;
            }

            for (int i = 0; i < img.Count; ++i)
            {
                // 需要保证mark比背景黑，需要验证D上是否能准确抓取
                //ret = ComAlgo.CalcGlassIcBondPix(img[i], out Point innerPix, out Point outerPix,
                //    out Point errorXYLeft);
                Point innerPix = new Point();
                Point outerPix = new Point();
                ret = DieBondComAlgo.CalcCircleRingMarkCenter(img[i], out List<Point> centerExtract, out List<double> radius, out List<bool> isCircle, markParams);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                if (centerExtract.Count != 2)
                {
                    if (ComAlgo.SaveFlg("CalcCircleRingMarkCenterError", out days))
                    {
                        string filePath = @"D:\Alg\BondCheckErrorImg";
                        if (!Directory.Exists(filePath))
                        {
                            Directory.CreateDirectory(filePath);
                        }

                        string imgName = null;
                        if ((rowId != null) && (colId != null))
                        {
                            imgName = rowId + "_" + colId;
                        }

                        ComAlgo.SaveImage(filePath, img[i], imgName);
                    }

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

                // bottom -> top  使用同轴下像素比转轴 替代 下转上再转轴
                //ret = GetTopByBottom(innerPix, out Point innerPixTop);
                //ret = GetTopByBottom(outerPix, out Point outerPixTop);

                // pix -> ruler
                //ret = CalibPixScaleXY.GetInstance().GetRulerByPix(_calibParam.IcUpCamName, innerPixTop, cameraRuler[i], out Point innerRuler);
                ret = CalibPixScaleXY.GetInstance().GetRulerByPix(_calibParam.IcDownCamName, innerPix, cameraRuler[i], out Point innerRuler);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                //ret = CalibPixScaleXY.GetInstance().GetRulerByPix(_calibParam.IcUpCamName, outerPixTop, cameraRuler[i], out Point outRuler);
                ret = CalibPixScaleXY.GetInstance().GetRulerByPix(_calibParam.IcDownCamName, outerPix, cameraRuler[i], out Point outRuler);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                // ruler -> real
                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.IcUpCamName, innerRuler, out Point innerReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = CalibXY.GetInstance().GetRealByRuler(_calibParam.IcUpCamName, outRuler, out Point outReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                icMarkRulers.Add(innerRuler);
                subMarkRulers.Add(outRuler);
                icMarkReals.Add(innerReal);
                subMarkReals.Add(outReal);
            }

            //ret = CalibXYT.GetInstance().GetRotateCenter(_calibParam.BondHeadName, true, currentHead, out Point rotateCenter);
            //if (ret != Errortype.OK)
            //{
            //    return ret;
            //}
            Point rotateCenter = (icMarkReals[0] + icMarkReals[1]) * 0.5;

            ret = ComAlgo.CalcLinesRT(icMarkReals[0], icMarkReals[1], subMarkReals[0], subMarkReals[1], rotateCenter, out angleError, out translateError);

            // record
            if (ComAlgo.SaveFlg("CalcGlassICBondError", out days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");

                string text = string.Empty;
                text += time + sep;

                //// ruler
                //text += icMarkRulers[0].X.ToString() + sep + icMarkRulers[0].Y.ToString() + sep;
                //text += icMarkRulers[1].X.ToString() + sep + icMarkRulers[1].Y.ToString() + sep;
                //text += subMarkRulers[0].X.ToString() + sep + subMarkRulers[0].Y.ToString() + sep;
                //text += subMarkRulers[1].X.ToString() + sep + subMarkRulers[1].Y.ToString() + sep;

                // real
                text += icMarkReals[0].X.ToString() + sep + icMarkReals[0].Y.ToString() + sep;
                text += icMarkReals[1].X.ToString() + sep + icMarkReals[1].Y.ToString() + sep;
                text += subMarkReals[0].X.ToString() + sep + subMarkReals[0].Y.ToString() + sep;
                text += subMarkReals[1].X.ToString() + sep + subMarkReals[1].Y.ToString() + sep;

                // errors
                text += rotateCenter.ToString(sep) + sep + translateError.X.ToString() + sep + translateError.Y.ToString() + sep + angleError.ToString();
                string fileName = path + "\\CalcGlassICBondError.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
                if (translateError.DistanceTo(new Point(0, 0)) > _axisParam.BondCheckErrorThresh)
                {
                    path = @"D:\Alg\BondCheckBad";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string imgName = null;
                    if ((rowId != null) && (colId != null))
                    {
                        imgName = rowId + "_" + colId;
                    }

                    ComAlgo.SaveImage(path, img[0], imgName + "_0");
                    ComAlgo.SaveImage(path, img[1], imgName + "_1");
                }
            }

            return ret;
        }

        /// <summary>
        /// 计算玻璃IC圆 或 网格板环 mark中心（需要调试适配D相机光源）
        /// </summary>
        /// <param name="cameraName">相机机构名称</param>
        /// <param name="img">图像</param>
        /// <param name="curCamRuler">当前相机光栅</param>
        /// <param name="markPix">mark像素坐标</param>
        /// <param name="markRuler">mark轴坐标(upCam)</param>
        /// <param name="markParams">mark抓取配置参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcGlassICMarkCenter(string cameraName, Camera img, Point curCamRuler, out Point markPix, out Point markRuler, GlassIcMarkParams markParams = null)
        {
            markRuler = null;
            markPix = null;
            if (img is null || img.Width < 1 || img.Height < 1)
            {
                return Errortype.UBD_CALCGLASSICMARKCENTER_INPUT_IMG_NULL;
            }

            var ret = DieBondComAlgo.CalcCircleRingMarkCenter(img, out List<Point> centerExtract, out _, out List<bool> isCircle, markParams);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            int days = 0;
            if (centerExtract.Count != 1)
            {
                if (ComAlgo.SaveFlg("CalcGlassICMarkCenterError", out days))
                {
                    string path = @"D:\Alg\CalcGlassICMarkCenterImg\";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    //ComAlgo.FileManage(path, 7, out path);
                    ComAlgo.SaveImage(path, img);
                }

                return Errortype.UBD_CALCGLASSICMARKCENTER_GRAB_ERROR;
            }

            markPix = centerExtract[0];

            if (markPix == new Point(0, 0))
            {
                if (ComAlgo.SaveFlg("CalcGlassICMarkCenterError", out days))
                {
                    string path = @"D:\Alg\CalcGlassICMarkCenterErrorImg";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    //ComAlgo.FileManage(path, 7, out path);
                    ComAlgo.SaveImage(path, img);
                }

                return Errortype.UBD_CALCGLASSICMARKCENTER_GRAB_ERROR;
            }

            //HObject hImage = img.GenHObject();

            ////粗定位
            //HOperatorSet.AutoThreshold(hImage, out HObject thresholdRegion, 2);
            //HOperatorSet.ClosingCircle(thresholdRegion, out HObject closingCircleRegion, 7.5);
            //HOperatorSet.FillUp(closingCircleRegion, out HObject regionFillUp);
            //HOperatorSet.Connection(regionFillUp, out HObject connectionRegion);
            //HOperatorSet.SelectShape(connectionRegion, out HObject selectedRegionsCir, "circularity", "and", 0.9, 1);
            //HOperatorSet.SelectShapeStd(selectedRegionsCir, out HObject selectedRegions, "max_area", 70);
            //HOperatorSet.SmallestCircle(selectedRegions, out HTuple maxRow, out HTuple maxCol, out HTuple maxRadius);
            //thresholdRegion.Dispose();
            //regionFillUp.Dispose();
            //connectionRegion.Dispose();
            //closingCircleRegion.Dispose();
            //selectedRegions.Dispose();
            //selectedRegionsCir.Dispose();
            //hImage.Dispose();

            ////精定位
            //CaliperParams calipParam = new CaliperParams();
            //calipParam.NumInstances = 1;
            //calipParam.MinScore = 0.8;
            //calipParam.MeasureSigma = 1;
            //calipParam.MeasureLength1 = 10;
            //calipParam.MeasureLength2 = 1;
            //calipParam.NumMeasures = 150;
            //calipParam.MeasureThreshold = 10;
            //calipParam.MeasureSelect = "all";
            //calipParam.MeasureInterpolation = "nearest_neighbor";
            //calipParam.MeasureTransition = "positive";
            //Errortype ret = CaliperCircle.CircleExtraction(img, new Point(maxCol.D, maxRow.D), maxRadius.D, calipParam, out markPix, out double radius);
            //if (ret != Errortype.OK)
            //{
            //    return ret;
            //}
            if (cameraName == _calibParam.IcUpCamName || cameraName == _calibParam.IcDownCamName)
            {
                //像素转轴
                ret = CalibPixScaleXY.GetInstance().GetRulerByPix(cameraName, markPix, curCamRuler, out markRuler);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }

            if (ComAlgo.SaveFlg("CalcGlassICMarkCenter", out days))
            {
                string path = @"D:\Alg\";
                string sep = " ";

                //if (Directory.Exists(path))
                //{
                //    Directory.CreateDirectory(path);
                //}

                //DateTime now = DateTime.Now;
                //int milliseconds = now.Millisecond;
                //string time = now.ToString().Replace(" ", "").Replace("/", "").Replace(":", "") + milliseconds.ToString("D3");

                //string text = string.Empty;
                //text += time + sep;
                //text += curHeadRuler.ToString(sep) + sep + markPix.ToString(sep) + sep +
                //        markRuler.ToString(sep);
                //string fileName = path + "\\CalcGlassICMarkCenter.txt";
                //FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                //StreamWriter sw = new StreamWriter(fs);
                //sw.WriteLine(text);
                //sw.Close();
                //fs.Close();
                ComAlgo.SaveData(
                    path + "\\CalcGlassICMarkCenter.txt",
                    curCamRuler.ToString(sep), markPix.ToString(sep), markRuler.ToString(sep));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载机台轴参数
        /// </summary>
        /// <param name="path">加载路径</param>
        /// <returns>OK：成功；其他：失败</returns>
        public static Errortype LoadAxisParam(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = path + "\\algorithmParam.ini";
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                StringBuilder sb = new StringBuilder();
                int stageAxisX = 1;
                int stageAxisY = 1;
                int stageAxisT = 1;
                double waferAngleDie = 0.1;
                double bondICAngle = 0.002;
                double bondICOffsetX = 0.0002;
                double bondICOffsetY = 0.0002;
                double bondCheckErrorThresh = 0.002;

                sb.AppendLine("[StageAxis]");
                sb.AppendLine("HeadAxisX=" + stageAxisX.ToString());
                sb.AppendLine("HeadAxisY=" + stageAxisY.ToString());
                sb.AppendLine("HeadAxisT=" + stageAxisT.ToString());
                sb.AppendLine("WaferAngleDie=" + waferAngleDie.ToString());
                sb.AppendLine("BondICAngle=" + bondICAngle.ToString());
                sb.AppendLine("BondICOffsetX=" + bondICOffsetX.ToString());
                sb.AppendLine("BondICOffsetY=" + bondICOffsetY.ToString());
                sb.AppendLine("BondCheckErrorThresh=" + bondCheckErrorThresh.ToString());
                File.WriteAllText(path, sb.ToString());
            }

            string[] keys = null;
            string[] values = null;

            IniHelper.GetAllKeyValues("StageAxis", out keys, out values, path);
            _axisParam.HeadAxisX = int.Parse(values[0]);
            _axisParam.HeadAxisY = int.Parse(values[1]);
            _axisParam.HeadTheta = int.Parse(values[2]);
            _axisParam.WaferAngleDie = double.Parse(values[3]);
            _axisParam.BondICAngle = double.Parse(values[4]);
            _axisParam.BondICOffsetX = double.Parse(values[5]);
            _axisParam.BondICOffsetY = double.Parse(values[6]);
            if (values.Length > 7)
            {
                _axisParam.BondCheckErrorThresh = double.Parse(values[7]);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算wafer stage避障矩形区域
        /// </summary>
        /// <param name="obstRect">障碍区域rectangle1</param>
        /// <param name="startPoint">直线路径起点</param>
        /// <param name="endPoint">直线路径终点</param>
        /// <param name="pathPoint">避障点</param>
        /// <returns>ok:计算成功，UBD_WAFER_STAGE_PATH_ERROR：无法避障</returns>
        public static Errortype CheckWaferStagePath(List<Point> obstRect, Point startPoint, Point endPoint, out Point pathPoint)
        {
            pathPoint = null;
            if (obstRect.Count != 2)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            HOperatorSet.GenRectangle1(out HObject obstRegion, obstRect[0].Y, obstRect[0].X, obstRect[1].Y, obstRect[1].X);

            double[] lineRow = { startPoint.Y, endPoint.Y };
            double[] lineCol = { startPoint.X, endPoint.X };
            HOperatorSet.GenRegionPolygon(out HObject lineRegion, lineRow, lineCol);
            HOperatorSet.Intersection(obstRegion, lineRegion, out HObject intersectRegion);
            HOperatorSet.AreaCenter(intersectRegion, out HTuple interArea, out _, out _);
            lineRegion.Dispose();
            intersectRegion.Dispose();
            if (interArea > 0)
            {
                double[] corner1PathRow = { lineRow[0], lineRow[0], lineRow[1] };
                double[] corner1PathCol = { lineCol[0], lineCol[1], lineCol[1] };
                HOperatorSet.GenRegionPolygon(out HObject corner1Region, corner1PathRow, corner1PathCol);
                HOperatorSet.Intersection(obstRegion, corner1Region, out intersectRegion);
                HOperatorSet.AreaCenter(intersectRegion, out HTuple interCorner1Area, out _, out _);
                corner1Region.Dispose();
                intersectRegion.Dispose();
                if (interCorner1Area > 0)
                {
                    double[] corner2PathRow = { lineRow[0], lineRow[1], lineRow[1] };
                    double[] corner2PathCol = { lineCol[0], lineCol[0], lineCol[1] };
                    HOperatorSet.GenRegionPolygon(out HObject corner2Region, corner2PathRow, corner2PathCol);
                    HOperatorSet.Intersection(obstRegion, corner2Region, out intersectRegion);
                    HOperatorSet.AreaCenter(intersectRegion, out HTuple interCorner2Area, out _, out _);
                    intersectRegion.Dispose();
                    corner2Region.Dispose();

                    // 如果另外一个拐点仍然经过障碍区域则返回错误
                    if (interCorner1Area > 0)
                    {
                        obstRegion.Dispose();
                        return Errortype.UBD_WAFER_STAGE_PATH_ERROR;
                    }
                    else
                    {
                        pathPoint = new Point(lineCol[0], lineRow[1]);
                    }
                }
                else
                {
                    pathPoint = new Point(lineCol[1], lineRow[0]);
                }
            }

            obstRegion.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// head轴和相机轴夹角缩放标定，先X后Y
        /// </summary>
        /// <param name="headX">X方向标定点head轴坐标head轴坐标</param>
        /// <param name="headY">Y方向标定点head轴坐标</param>
        /// <param name="cameraRulerX">X方向标定mark的camera轴坐标</param>
        /// <param name="cameraRulerY">Y方向标定mark的camera轴坐标</param>
        /// <param name="scaleX">标定出的X轴比例</param>
        /// <param name="scaleY">标定出的Y轴比例</param>
        /// <param name="angleX">标定出的X轴夹角</param>
        /// <param name="angleY">标定出的Y轴夹角</param>
        /// <returns>ok：标定成功</returns>
        public static Errortype CalibHeadCameraAxisAngle(List<Point> headX, List<Point> headY, List<Point> cameraRulerX, List<Point> cameraRulerY,
            out double scaleX, out double scaleY, out double angleX, out double angleY)
        {
            CalibCoord calibHead = new CalibCoord(_calibParam.BondHeadName);
            scaleX = 0.0;
            scaleY = 0.0;
            angleX = 0.0;
            angleY = 0.0;
            if (headX.Count != headY.Count || cameraRulerX.Count != cameraRulerY.Count ||
                headX.Count != 2 || cameraRulerX.Count != 2)
            {
                return Errortype.UBD_INPUT_POINT_LENGTH_ERROR;
            }

            Point headRulerX1 = headX[0];
            Point headRulerX2 = headX[1];
            Point headRulerY1 = headY[0];
            Point headRulerY2 = headY[1];

            Point cameraRulerX1 = cameraRulerX[0];
            Point cameraRulerX2 = cameraRulerX[1];
            Point cameraRulerY1 = cameraRulerY[0];
            Point cameraRulerY2 = cameraRulerY[1];

            double headDistanceX = headRulerX2.DistanceTo(headRulerX1);
            double headDistanceY = headRulerY2.DistanceTo(headRulerY1);

            HOperatorSet.LineOrientation(cameraRulerX1.Y, cameraRulerX1.X, cameraRulerX2.Y, cameraRulerX2.X,
                out HTuple xPhi); // 一三象限为正

            HOperatorSet.AngleLx(cameraRulerY1.Y, cameraRulerY1.X, cameraRulerY2.Y, cameraRulerY2.X,
                out HTuple yPhi); // anglelx 输出逆时针为正
            double xAxisRad = xPhi.D;

            double yAxisRad = yPhi.D - xPhi.D - (Math.PI / 2); // 注意相机轴Y方向，可能是 + 90°
            HOperatorSet.TupleDeg(xAxisRad, out HTuple xDeg);
            HOperatorSet.TupleDeg(yAxisRad, out HTuple yDeg);

            double camDistanceX = cameraRulerX1.DistanceTo(cameraRulerX2);
            double camDistanceY = cameraRulerY1.DistanceTo(cameraRulerY2);
            double scaleHeadCamX = headDistanceX / camDistanceX;
            double scaleHeadCamY = headDistanceY / camDistanceY;

            angleX = xDeg.D;
            angleY = yDeg.D;

            //scaleX = scaleHeadCamX;
            //scaleY = scaleHeadCamY;
            scaleX = 1.0;
            scaleY = 1.0;

            var ret = calibHead.CalibDo(new Point(0, 0), scaleHeadCamX, scaleHeadCamY, xAxisRad, yAxisRad);
            calibHead.IsCalibed = true;

            calibHead.Save(_paramFileDir);

            return ret;
        }

        /// <summary>
        /// 验证head轴和相机轴夹角缩放标定接口，运动完成后调用mark中心抓取，计算和视野中心的偏差
        /// </summary>
        /// <param name="cameraAxisDistance">mark移动到相机视野中心需要的相机轴距离</param>
        /// <param name="headAxisMotion">输出head实际需要运动的距离</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype CheckHeadCameraAxisCalib(Point cameraAxisDistance, out Point headAxisMotion)
        {
            Point rulerOffset = new Point(0, 0);
            headAxisMotion = new Point(0, 0);
            CalibCoord calibHead = new CalibCoord(_calibParam.BondHeadName);
            var ret = calibHead.Load(_paramFileDir);
            if (ret == Errortype.OK)
            {
                calibHead.Dst2Src(cameraAxisDistance, out rulerOffset, out _);
            }
            else
            {
                return ret;
            }

            LoadAxisParam(_paramFileDir);
            headAxisMotion = new Point(rulerOffset.X * _axisParam.HeadAxisX, rulerOffset.Y * _axisParam.HeadAxisY);
            return ret;
        }

        /// <summary>
        /// Map映射，示教sub轴点和map点矩阵
        /// </summary>
        /// <param name="teachPoints">Map点坐标</param>
        /// <param name="rulerPoints">sub轴坐标</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype TeachMapPoint(List<Point> teachPoints, List<Point> rulerPoints)
        {
            string path = _paramFileDir + "\\MapHomMat2d.ini";
            double[] homMat2D = new double[0];
            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            StringBuilder sb = new StringBuilder();
            var ret = ComAlgo.VectorToSimilarity(teachPoints, rulerPoints, out homMat2D, out List<Point> error);

            if (ret != Errortype.OK)
            {
                return ret;
            }

            List<string> keys = new List<string> { };
            List<string> value = new List<string> { };

            keys.Clear();
            value.Clear();
            keys.Clear();
            keys.Add("m01");
            keys.Add("m02");
            keys.Add("m03");
            keys.Add("m04");
            keys.Add("m05");
            keys.Add("m06");
            value.Add(homMat2D[0].ToString());
            value.Add(homMat2D[1].ToString());
            value.Add(homMat2D[2].ToString());
            value.Add(homMat2D[3].ToString());
            value.Add(homMat2D[4].ToString());
            value.Add(homMat2D[5].ToString());
            IniHelper.AddSectionWithKeyValues("Matrix_Src2Dst", keys, value, path);

            return Errortype.OK;
        }

        /// <summary>
        /// 计算Map点位的实际坐标
        /// </summary>
        /// <param name="mapSrc">转换的Map点坐标</param>
        /// <param name="mapRulerDst">转换后的轴点坐标</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype CalcMapPoint(Point mapSrc, out Point mapRulerDst)
        {
            mapRulerDst = new Point();

            string path = _paramFileDir + "\\MapHomMat2d.ini";
            double[] homMat2D = new double[0];
            if (!File.Exists(path))
            {
                return Errortype.UBD_LOAD_MAP_ERROR;
            }
            else
            {
                IniHelper.GetAllKeyValues("Matrix_Src2Dst", out string[] keys, out string[] values, path);
                if (values.Length != 6)
                {
                    return Errortype.UBD_MAP_LENGTH_ERROR;
                }

                homMat2D = new double[6];
                for (int i = 0; i < 6; i++)
                {
                    homMat2D[i] = Convert.ToDouble(values[i]);
                }
            }

            var result = ComAlgo.AffineTransPoint2d(mapSrc, homMat2D, out mapRulerDst);
            if (result != Errortype.OK)
            {
                return result;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 生成 sub stage 平坦度检测点
        /// </summary>
        /// <param name="centerPoint">检测点集的中心</param>
        /// <param name="checkPoints">输出 sub stage </param>
        /// <param name="checkRadius">检查区域的半径，默认100mm</param>
        /// <param name="circleNum">检查的圈数，默认两圈</param>
        /// <returns>ok：计算成功，其他：计算失败</returns>
        public static Errortype GetSubStageFlatCheckPoint(Point centerPoint, out List<Point> checkPoints,
            double checkRadius = 100, int circleNum = 2)
        {
            checkPoints = new List<Point>();
            if (checkRadius < 0)
            {
                return Errortype.UBD_SUB_POINTS_RADIUS_ERROR;
            }

            if (centerPoint == null)
            {
                centerPoint = new Point(0, 0);
            }

            // 圆形点阵分布
            int circleCore = 4; // 种子数量
            double radiusStep = checkRadius / circleNum;
            HTuple thetaDeg = new HTuple();
            HTuple radius = new HTuple();
            for (int i = 0; i < circleNum; i++)
            {
                int numOnCircle = circleCore + (circleCore * i);
                double thetaStep = 360.0 / numOnCircle;
                HOperatorSet.TupleGenSequence(0, 360.0 - thetaStep, thetaStep, out HTuple thetaOnCircle);
                HOperatorSet.TupleGenConst(thetaOnCircle.Length, radiusStep * (i + 1), out HTuple radiusOnCircle);
                HOperatorSet.TupleConcat(thetaDeg, thetaOnCircle, out thetaDeg);
                HOperatorSet.TupleConcat(radius, radiusOnCircle, out radius);
            }

            HOperatorSet.TupleRad(thetaDeg, out HTuple thetaRad);
            HOperatorSet.TupleSin(thetaRad, out HTuple sinValues);
            HOperatorSet.TupleCos(thetaRad, out HTuple cosValues);

            var xValues = radius * cosValues + centerPoint.X;
            var yValues = radius * sinValues + centerPoint.Y;

            for (int i = 0; i < xValues.Length; i++)
            {
                checkPoints.Add(new Point(xValues[i], yValues[i]));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算sub stage调平量
        /// </summary>
        /// <param name="checkPoints">sub高度测量点</param>
        /// <param name="axisPoint">sub调平轴点</param>
        /// <param name="baseZ">高度基准点</param>
        /// <param name="heightValues">轴点调平高度</param>
        /// <returns>ok：计算成功，其他：计算失败</returns>
        public static Errortype CalcSubStageFlatAdjust(List<Point3D> checkPoints, List<Point3D> axisPoint, double baseZ, out List<double> heightValues)
        {
            heightValues = new List<double>();
            if ((checkPoints == null) || checkPoints.Count < 3)
            {
                return Errortype.UBD_SUB_POINTS_INPUT_ERROR;
            }

            if ((axisPoint == null) || axisPoint.Count < 3)
            {
                return Errortype.UBD_SUB_POINTS_INPUT_ERROR;
            }

            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            List<double> zList = new List<double>();
            for (int i = 0; i < checkPoints.Count; i++)
            {
                xList.Add(checkPoints[i].X);
                yList.Add(checkPoints[i].Y);
                zList.Add(checkPoints[i].Z);
            }

            HOperatorSet.CreateMatrix(xList.Count, 1, xList.ToArray(), out HTuple matrixX);
            HOperatorSet.CreateMatrix(xList.Count, 1, yList.ToArray(), out HTuple matrixY);
            HOperatorSet.CreateMatrix(xList.Count, 1, zList.ToArray(), out HTuple matrixZ);

            HOperatorSet.CreateMatrix(xList.Count, 3, 1.0, out HTuple matrixXY1);
            HOperatorSet.SetSubMatrix(matrixXY1, matrixX, 0, 0);
            HOperatorSet.SetSubMatrix(matrixXY1, matrixY, 0, 1);

            HOperatorSet.SolveMatrix(matrixXY1, "general", 0, matrixZ, out HTuple matrixRes);
            HOperatorSet.GetValueMatrix(matrixRes, 0, 0, out HTuple valueA);
            HOperatorSet.GetValueMatrix(matrixRes, 1, 0, out HTuple valueB);
            HOperatorSet.GetValueMatrix(matrixRes, 2, 0, out HTuple valueC);

            for (int i = 0; i < axisPoint.Count; i++)
            {
                var valueZ = (valueA * axisPoint[i].X) + (valueB * axisPoint[i].Y) + valueC;
                heightValues.Add(valueZ.D - baseZ);
            }

            //if (ComAlgo.SaveFlg("CalcWaferAngleDie"))
            if (true)
            {
                //存储软件传给的角点坐标和算法计算出的旋转后的单颗die中心点坐标
                string path = @"D:\Alg\";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = path + "\\CalcFaltness.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmm") + System.DateTime.Now.Millisecond.ToString("D3");
                string text = string.Empty;
                string sep = " ";

                for (int i = 0; i < checkPoints.Count; i++)
                {
                    string textP;
                    textP = time + sep + "measure_" + i.ToString() + sep + checkPoints[i].X.ToString() + sep + checkPoints[i].Y + sep + checkPoints[i].Z + sep;
                    sw.WriteLine(textP);
                }

                text = string.Empty;
                text += time + sep;
                text += "resZ: ";
                for (int i = 0; i < heightValues.Count; i++)
                {
                    text += heightValues[i].ToString() + sep;
                }

                sw.WriteLine(text);

                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
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
                return Errortype.UBD_DIEISEXIST_INPUT_IMAGE_NULL;
            }

            HObject hImg = image.GenHObject();
            HOperatorSet.GetImageSize(hImg, out HTuple width, out HTuple height);
            HOperatorSet.Rgb1ToGray(hImg, out HObject grayImage);
            HOperatorSet.GenRectangle1(out HObject rectangle, height / 2 - height / 4, width / 2 - width / 4, height / 2 + height / 6, width / 2 + width / 6);
            HOperatorSet.Intensity(rectangle, grayImage, out HTuple meanGrayValue, out HTuple deviation);
            grayValue = meanGrayValue;
            hImg.Dispose();
            grayImage.Dispose();
            return Errortype.OK;
        }
    }

    /// <summary>
    /// 测试数据记录
    /// </summary>
    public static class UBDRecord
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
        /// 静态重复性测试
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="ruler">光栅值</param>
        /// <param name="markPixel">mark像素坐标</param>
        /// <param name="markRuler">mark光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype StaticRepeatTestRecord(string fileName, Point ruler, Point markPixel, Point markRuler)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID RulerX RulerY MarkPixelX MarkPixelY MarkRulerX MarkRulerY";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            int id = GetLineID(fileName);
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            string text = time + _sep +
                          id.ToString() + _sep +
                          ruler.ToString(_sep) + _sep +
                          markPixel.ToString(_sep) + _sep +
                          markRuler.ToString(_sep);

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 动态重复性测试
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="axisDynamicRepeatTestData">输入数据</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AxisDynamicRepeatTestRecord(string fileName, AxisDynamicRepeatTestData axisDynamicRepeatTestData)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID " + _sep +
                               "PointARulerX PointARulerY PointAMarkPixelX PointAMarkPixelY PointAMarkRulerX PointAMarkRulerY " + _sep +
                               "PointB1RulerX PointB1RulerY PointB1MarkPixelX PointB1MarkPixelY PointB1MarkRulerX PointB1MarkRulerY " + _sep +
                               "PointCRulerX PointCRulerY PointCMarkPixelX PointCMarkPixelY PointCMarkRulerX PointCMarkRulerY " + _sep +
                               "PointB2RulerX PointB2RulerY PointB2MarkPixelX PointB2MarkPixelY PointB2MarkRulerX PointB2MarkRulerY";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            int id = GetLineID(fileName);
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            string text = time + _sep +
                          id.ToString() + _sep +
                          axisDynamicRepeatTestData.PointARuler.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointAMarkPixel.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointAMarkRuler.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointB1Ruler.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointB1MarkPixel.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointB1MarkRuler.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointCRuler.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointCMarkPixel.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointCMarkRuler.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointB2Ruler.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointB2MarkPixel.ToString(_sep) + _sep +
                          axisDynamicRepeatTestData.PointB2MarkRuler.ToString(_sep);

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 倒装拾取精度测试
        /// </summary>
        /// <param name="fileName">保存的文件名</param>
        /// <param name="dieRowCol">拾取的die行列</param>
        /// <param name="waferDieDatas">die拉直角点数据</param>
        /// <param name="alignDatas">同轴check数据</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype FlipChipPickRepeatTestRecord(string fileName, Point dieRowCol,
            RepeatTestDieRecordData waferDieDatas, RepeatTestDieRecordData alignDatas)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID" + _sep +
                               "DieRow DieColumn " + _sep +
                               "WaferStageLeftTopX WaferStageLeftTopY WaferStageRightBottomX WaferStageRightBottomY" + _sep +
                               "DieLeftTopPixX DieLeftTopPixY DieRightBottomPixX DieRightBottomPixY" + _sep +
                               "DieLeftTopRulerX DieLeftTopRulerY DieRightBottomRulerX DieRightBottomRulerY" + _sep +
                               "AlignTopLeftRulerX AlignTopLeftRulerY AlignRightBottomRulerX AlignRightBottomRulerY " + _sep +
                               "TopLeftPixRulerX TopLeftPixY RightBottomPixRulerX RightBottomPixY TopLeftMarkRulerX TopLeftMarkRulerY RightBottomMarkRulerX RightBottomMarkRulerY";
                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            int id = GetLineID(fileName);
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") +
                          System.DateTime.Now.Millisecond.ToString("D3");
            string text = time + _sep +
                          id.ToString() + _sep + dieRowCol.ToString(_sep) + _sep +
                          waferDieDatas.PointLeftTopRuler.ToString(_sep) + _sep +
                          waferDieDatas.PointRightBottomRuler.ToString(_sep) + _sep +
                          waferDieDatas.PointLeftTopPix.ToString(_sep) + _sep +
                          waferDieDatas.PointRightBottomPix.ToString(_sep) + _sep +
                          waferDieDatas.PointLeftTopPix2Ruler.ToString(_sep) + _sep +
                          waferDieDatas.PointRightBottomPix2Ruler.ToString(_sep) + _sep +

                          alignDatas.PointLeftTopRuler.ToString(_sep) + _sep +
                          alignDatas.PointRightBottomRuler.ToString(_sep) + _sep +
                          alignDatas.PointLeftTopPix.ToString(_sep) + _sep +
                          alignDatas.PointRightBottomPix.ToString(_sep) + _sep +
                          alignDatas.PointLeftTopPix2Ruler.ToString(_sep) + _sep +
                          alignDatas.PointRightBottomPix2Ruler.ToString(_sep);

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 同轴度重复性测试
        /// </summary>
        /// <param name="fileName">保存的文件名</param>
        /// <param name="cameraRuler">同轴相机轴坐标</param>
        /// <param name="topMarkPix">top mark 像素坐标</param>
        /// <param name="topMarkPix2Ruler">top mark像素转轴</param>
        /// <param name="bottomMarkPix">bottom mark 像素坐标</param>
        /// <param name="bottomMarkPix2Ruler">bottom mark像素转轴</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CameraConCenterRepeatTestRecord(string fileName, Point cameraRuler,
            Point topMarkPix, Point topMarkPix2Ruler, Point bottomMarkPix, Point bottomMarkPix2Ruler)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID" + _sep +
                               "CameraRulerX CameraRulerY TopMarkPixelX TopMarkPixelY BottomMarkPixelX BottomMarkPixelY" + _sep +
                               "TopMarkRulerX TopMarKRulerY BottomMarkRulerX BottomMarkRulerY CoaxiaErrorX CoaxiaErrorY";
                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            int id = GetLineID(fileName);
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") +
                          System.DateTime.Now.Millisecond.ToString("D3");
            string text = time + _sep +
                          id.ToString() + _sep + cameraRuler.ToString(_sep) + _sep +
                          topMarkPix.ToString(_sep) + _sep +
                          bottomMarkPix.ToString(_sep) + _sep +
                          topMarkPix2Ruler.ToString(_sep) + _sep +
                          bottomMarkPix2Ruler.ToString(_sep) + _sep +
                          (topMarkPix2Ruler - bottomMarkPix2Ruler).ToString(_sep);

            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// 玻璃IC Bond 测试数据记录
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="alignmentBondRepeatTestData">Glass IC Bond 测试数据</param>
        /// /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AlignmentBondRepeatTestRecord(string fileName, AlignmentBondRepeatTestData alignmentBondRepeatTestData)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "ID Time SubCol SubRow " +
                               "HeadRulerX HeadRulerY HeadRulerT " +
                               "TopCameraLeftTopRulerX TopCameraLeftTopRulerY TopCameraRightBottomRulerX TopCameraRightBottomRulerY " +
                               "SubCameraLeftTopRulerX SubCameraLeftTopRulerY SubCameraRightBottomRulerX SubCameraRightBottomRulerY " +
                               "TopICLeftTopMarkPixelX TopICLeftTopMarkPixelY TopICRightBottomMarkPixelX TopICRightBottomMarkPixelY " +
                               "SubICLeftTopMarkPixelX SubICLeftTopMarkPixelY SubICRightBottomMarkPixelX SubICRightBottomMarkPixelY " +
                               "TopICLeftTopMarkRulerX TopICLeftTopMarkRulerY TopICRightBottomMarkRulerX TopICRightBottomMarkRulerY " +
                               "SubICLeftTopMarkRulerX SubICLeftTopMarkRulerY SubICRightBottomMarkRulerX SubICRightBottomMarkRulerY " +
                               "ConCenterErrorX ConCenterErrorY TranslationX TranslationY TranslationT " +
                               "OffsetX OffsetY OffsetT " +
                               "BondCheckErrorX BondCheckErrorY BondCheckErrorT";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            int id = GetLineID(fileName);
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            string text = id.ToString() + _sep +
                          time + _sep +
                          alignmentBondRepeatTestData.SubID.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.HeadRulerXy.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.HeadRulerT.ToString(_format) + _sep +

                          // ruler
                          alignmentBondRepeatTestData.IcData.PointLeftTopRuler.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.IcData.PointRightBottomRuler.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.SubData.PointLeftTopRuler.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.SubData.PointRightBottomRuler.ToString(_sep) + _sep +

                          // pix
                          alignmentBondRepeatTestData.IcData.PointLeftTopPix.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.IcData.PointRightBottomPix.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.SubData.PointLeftTopPix.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.SubData.PointRightBottomPix.ToString(_sep) + _sep +

                          // pix2ruler
                          alignmentBondRepeatTestData.IcData.PointLeftTopPix2Ruler.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.IcData.PointRightBottomPix2Ruler.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.SubData.PointLeftTopPix2Ruler.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.SubData.PointRightBottomPix2Ruler.ToString(_sep) + _sep +

                          alignmentBondRepeatTestData.ConCenterError.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.TranslationXy.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.TranslationT.ToString(_format) + _sep +
                          alignmentBondRepeatTestData.CompensateOffSetXy.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.CompensateOffSetT.ToString(_format) + _sep +
                          alignmentBondRepeatTestData.BondCheckErrorXy.ToString(_sep) + _sep +
                          alignmentBondRepeatTestData.BondCheckErrorT.ToString(_format);
            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }

        /// <summary>
        /// T轴动态重复性测试
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="pointA">A点输入数据</param>
        /// <param name="pointB">B点输入数据</param>
        /// <param name="pointC">C点输入数据</param>
        /// <param name="pointB2">B2点输入数据</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype TAxisDynamicRepeatTestRecord(string fileName, TAxisDynamicRepeatTestData pointA, TAxisDynamicRepeatTestData pointB, TAxisDynamicRepeatTestData pointC, TAxisDynamicRepeatTestData pointB2)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            FileStream fs;
            StreamWriter sw;
            if (!File.Exists(fileName))
            {
                string title = "Time ID " + _sep +
                               "PointAHeadRulerX PointAHeadRulerY PointAHeadRulerT" + _sep +
                               "LeftTopPointARulerX LeftTopPointARulerY RightBottomPointARulerX RightBottomPointARulerY" + _sep +
                               "LeftTopPointAMarkPixelX LeftTopPointAMarkPixelY RightBottomPointAMarkPixelX RightBottomPointAMarkPixelY" + _sep +
                               "LeftTopPointAMarkRulerX LeftTopPointAMarkRulerY RightBottomPointAMarkRulerX RightBottomPointAMarkRulerY" + _sep +
                               "PointBHeadRulerX PointBHeadRulerY PointBHeadRulerT" + _sep +
                               "LeftTopPointBRulerX LeftTopPointBRulerY RightBottomPointBRulerX RightBottomPointBRulerY" + _sep +
                               "LeftTopPointBMarkPixelX LeftTopPointBMarkPixelY RightBottomPointBMarkPixelX RightBottomPointBMarkPixelY" + _sep +
                               "LeftTopPointBMarkRulerX LeftTopPointBMarkRulerY RightBottomPointBMarkRulerX RightBottomPointBMarkRulerY" + _sep +
                               "PointCHeadRulerX PointCHeadRulerY PointCHeadRulerT" + _sep +
                               "LeftTopPointCRulerX LeftTopPointCRulerY RightBottomPointCRulerX RightBottomPointCRulerY" + _sep +
                               "LeftTopPointCMarkPixelX LeftTopPointCMarkPixelY RightBottomPointCMarkPixelX RightBottomPointCMarkPixelY" + _sep +
                               "LeftTopPointCMarkRulerX LeftTopPointCMarkRulerY RightBottomPointCMarkRulerX RightBottomPointCMarkRulerY" + _sep +
                               "PointB2HeadRulerX PointB2HeadRulerY PointB2HeadRulerT" + _sep +
                               "LeftTopPointB2RulerX LeftTopPointB2RulerY RightBottomPointB2RulerX RightBottomPointB2RulerY" + _sep +
                               "LeftTopPointB2MarkPixelX LeftTopPointB2MarkPixelY RightBottomPointB2MarkPixelX RightBottomPointB2MarkPixelY" + _sep +
                               "LeftTopPointB2MarkRulerX LeftTopPointB2MarkRulerY RightBottomPointB2MarkRulerX RightBottomPointB2MarkRulerY" + _sep +
                               "AngleA AngleB AngleC AngleB2";

                fs = new FileStream(fileName, FileMode.Create);
                sw = new StreamWriter(fs);
                sw.WriteLine(title);
                sw.Close();
                fs.Close();
            }

            Point aDieSize = new Point(pointA.RightBottomMarkRuler.X - pointA.LeftTopMarkRuler.X, pointA.RightBottomMarkRuler.Y - pointA.LeftTopMarkRuler.Y);
            HOperatorSet.TupleAtan2(aDieSize.Y, aDieSize.X, out HTuple aAsin);
            HOperatorSet.TupleDeg(aAsin, out HTuple aAsinDeg);

            Point bDieSize = new Point(pointB.RightBottomMarkRuler.X - pointB.LeftTopMarkRuler.X, pointB.RightBottomMarkRuler.Y - pointB.LeftTopMarkRuler.Y);
            HOperatorSet.TupleAtan2(bDieSize.Y, bDieSize.X, out HTuple bAsin);
            HOperatorSet.TupleDeg(bAsin, out HTuple bAsinDeg);

            Point cDieSize = new Point(pointC.RightBottomMarkRuler.X - pointC.LeftTopMarkRuler.X, pointC.RightBottomMarkRuler.Y - pointC.LeftTopMarkRuler.Y);
            HOperatorSet.TupleAtan2(cDieSize.Y, cDieSize.X, out HTuple cAsin);
            HOperatorSet.TupleDeg(cAsin, out HTuple cAsinDeg);

            Point b2DieSize = new Point(pointB2.RightBottomMarkRuler.X - pointB2.LeftTopMarkRuler.X, pointB2.RightBottomMarkRuler.Y - pointB2.LeftTopMarkRuler.Y);
            HOperatorSet.TupleAtan2(b2DieSize.Y, b2DieSize.X, out HTuple b2Asin);
            HOperatorSet.TupleDeg(b2Asin, out HTuple b2AsinDeg);

            int id = GetLineID(fileName);
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            string text = time + _sep +
                          id.ToString() + _sep +

                          // A点
                          pointA.HeadRulerXy.ToString(_sep) + _sep +
                          pointA.HeadRulerT.ToString(_format) + _sep +
                          pointA.LeftTopRuler.ToString(_sep) + _sep + pointA.RightBottomRuler.ToString(_sep) + _sep +
                          pointA.LeftTopMarkPixel.ToString(_sep) + _sep + pointA.RightBottomMarkPixel.ToString(_sep) + _sep +
                          pointA.LeftTopMarkRuler.ToString(_sep) + _sep + pointA.RightBottomMarkRuler.ToString(_sep) + _sep +

                          // B点
                          pointB.HeadRulerXy.ToString(_sep) + _sep +
                          pointB.HeadRulerT.ToString(_format) + _sep +
                          pointB.LeftTopRuler.ToString(_sep) + _sep + pointB.RightBottomRuler.ToString(_sep) + _sep +
                          pointB.LeftTopMarkPixel.ToString(_sep) + _sep + pointB.RightBottomMarkPixel.ToString(_sep) + _sep +
                          pointB.LeftTopMarkRuler.ToString(_sep) + _sep + pointB.RightBottomMarkRuler.ToString(_sep) + _sep +

                          // C点
                          pointC.HeadRulerXy.ToString(_sep) + _sep +
                          pointC.HeadRulerT.ToString(_format) + _sep +
                          pointC.LeftTopRuler.ToString(_sep) + _sep + pointC.RightBottomRuler.ToString(_sep) + _sep +
                          pointC.LeftTopMarkPixel.ToString(_sep) + _sep + pointC.RightBottomMarkPixel.ToString(_sep) + _sep +
                          pointC.LeftTopMarkRuler.ToString(_sep) + _sep + pointC.RightBottomMarkRuler.ToString(_sep) + _sep +

                          // B2点
                          pointB2.HeadRulerXy.ToString(_sep) + _sep +
                          pointB2.HeadRulerT.ToString(_format) + _sep +
                          pointB2.LeftTopRuler.ToString(_sep) + _sep + pointB2.RightBottomRuler.ToString(_sep) + _sep +
                          pointB2.LeftTopMarkPixel.ToString(_sep) + _sep + pointB2.RightBottomMarkPixel.ToString(_sep) + _sep +
                          pointB2.LeftTopMarkRuler.ToString(_sep) + _sep + pointB2.RightBottomMarkRuler.ToString(_sep) + _sep +

                          // Angle A B C B2
                          aAsinDeg.D.ToString(_format) + _sep + bAsinDeg.D.ToString(_format) + _sep + cAsinDeg.D.ToString(_format) + _sep + b2AsinDeg.D.ToString(_format);
            fs = new FileStream(fileName, FileMode.Append);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
            return Errortype.OK;
        }
    }

    /// <summary>
    /// Axis Dynamic Repeat Test Record Data
    /// </summary>
    public class AxisDynamicRepeatTestData
    {
        /// <summary>
        /// Gets or Sets PointARuler
        /// </summary>
        public Point PointARuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointAMarkPixel
        /// </summary>
        public Point PointAMarkPixel { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointAMarkRuler
        /// </summary>
        public Point PointAMarkRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointB1Ruler
        /// </summary>
        public Point PointB1Ruler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointB1MarkPixel
        /// </summary>
        public Point PointB1MarkPixel { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointB1MarkRuler
        /// </summary>
        public Point PointB1MarkRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointCRuler
        /// </summary>
        public Point PointCRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointCMarkPixel
        /// </summary>
        public Point PointCMarkPixel { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointCMarkRuler
        /// </summary>
        public Point PointCMarkRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointB2Ruler
        /// </summary>
        public Point PointB2Ruler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointB2MarkPixel
        /// </summary>
        public Point PointB2MarkPixel { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets PointB2MarkRuler
        /// </summary>
        public Point PointB2MarkRuler { get; set; } = new Point();
    }

    /// <summary>
    /// T Axis Dynamic Repeat Test Record Data
    /// </summary>
    public class TAxisDynamicRepeatTestData
    {
        /// <summary>
        /// Gets or Sets head轴当前XY坐标
        /// </summary>
        public Point HeadRulerXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets head轴当前T
        /// </summary>
        public double HeadRulerT { get; set; }

        /// <summary>
        /// Gets or Sets LeftTopRuler
        /// </summary>
        public Point LeftTopRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets RightBottomRuler
        /// </summary>
        public Point RightBottomRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets LeftTopMarkPixel
        /// </summary>
        public Point LeftTopMarkPixel { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets RightBottomMarkPixel
        /// </summary>
        public Point RightBottomMarkPixel { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets LeftTopMarkRuler
        /// </summary>
        public Point LeftTopMarkRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets RightBottomMarkRuler
        /// </summary>
        public Point RightBottomMarkRuler { get; set; } = new Point();
    }

    /// <summary>
    /// 重复性测试点位数据类
    /// </summary>
    public class RepeatTestDieRecordData
    {
        /// <summary>
        /// Gets or Sets 左上角点
        /// </summary>
        public Point PointLeftTopRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右下角点
        /// </summary>
        public Point PointRightBottomRuler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左上角点
        /// </summary>
        public Point PointLeftTopPix { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右下角点
        /// </summary>
        public Point PointRightBottomPix { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 左上角点
        /// </summary>
        public Point PointLeftTopPix2Ruler { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets 右下角点
        /// </summary>
        public Point PointRightBottomPix2Ruler { get; set; } = new Point();
    }

    /// <summary>
    /// GlassIC bond测试
    /// </summary>
    public class AlignmentBondRepeatTestData
    {
        /// <summary>
        /// Gets or Sets SubID
        /// </summary>
        public Point SubID { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets head轴当前XY坐标
        /// </summary>
        public Point HeadRulerXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets head轴当前T
        /// </summary>
        public double HeadRulerT { get; set; } = 0.0;

        /// <summary>
        /// Gets or Sets IC 点坐标
        /// </summary>
        public RepeatTestDieRecordData IcData { get; set; } = new RepeatTestDieRecordData();

        /// <summary>
        /// Gets or Sets SUB 点坐标
        /// </summary>
        public RepeatTestDieRecordData SubData { get; set; } = new RepeatTestDieRecordData();

        /// <summary>
        /// Gets or Sets 同轴度偏差
        /// </summary>
        public Point ConCenterError { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets TranslationXy
        /// </summary>
        public Point TranslationXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets TranslationT
        /// </summary>
        public double TranslationT { get; set; } = 0.0;

        /// <summary>
        /// Gets or Sets OffSetXy
        /// </summary>
        public Point CompensateOffSetXy { get; set; } = new Point();

        /// <summary>
        /// Gets or Sets OffSetT
        /// </summary>
        public double CompensateOffSetT { get; set; } = 0.0;

        /// <summary>
        /// Gets or Sets BondCheckErrorXy
        /// </summary>
        public Point BondCheckErrorXy { get; set; } = new Point(0, 0);

        /// <summary>
        /// Gets or Sets OffSetT
        /// </summary>
        public double BondCheckErrorT { get; set; } = 0.0;
    }

    /// <summary>
    /// UBD标定参数
    /// </summary>
    public class UBDCalibParam
    {
        private string _WaferCamFineName;
        private string _WaferCamCoarseName;
        private string _WaferStageName;
        private string _IcUpCamName;
        private string _IcDownCamName;
        private string _BondHeadName;
        private string _SubStageName;
        private string _FuStageName;

        /// <summary>
        /// Gets or sets wafer相机名称(小视野)
        /// </summary>
        public string WaferCamFineName
        {
            get => _WaferCamFineName;
            set => _WaferCamFineName = value;
        }

        /// <summary>
        /// Gets or sets wafer相机名称(大视野)
        /// </summary>
        public string WaferCamCoarseName
        {
            get => _WaferCamCoarseName;
            set => _WaferCamCoarseName = value;
        }

        /// <summary>
        /// Gets or sets wafer相机中心坐标
        /// </summary>
        public Point WaferCamCenter { get; set; } = new Point(0, 0);

        /// <summary>
        /// Gets or sets waferStage机构名称
        /// </summary>
        public string WaferStageName
        {
            get => _WaferStageName;
            set => _WaferStageName = value;
        }

        /// <summary>
        /// Gets or sets 同轴上相机名称
        /// </summary>
        public string IcUpCamName
        {
            get => _IcUpCamName;
            set => _IcUpCamName = value;
        }

        /// <summary>
        /// Gets or sets 同轴下相机名称
        /// </summary>
        public string IcDownCamName
        {
            get => _IcDownCamName;
            set => _IcDownCamName = value;
        }

        /// <summary>
        /// Gets or sets 同轴相机图像中心坐标
        /// </summary>
        public Point ICCamCenter { get; set; } = new Point(0, 0);

        /// <summary>
        /// Gets or sets head机构名称
        /// </summary>
        public string BondHeadName
        {
            get => _BondHeadName;
            set => _BondHeadName = value;
        }

        /// <summary>
        /// Gets or sets sub stage名称
        /// </summary>
        public string SubStageName
        {
            get => _SubStageName;
            set => _SubStageName = value;
        }

        /// <summary>
        /// Gets or sets FU stage名称
        /// </summary>
        public string FuStageName
        {
            get => _FuStageName;
            set => _FuStageName = value;
        }
    }

    /// <summary>
    /// UBD示教参数
    /// </summary>
    internal class UBDTeachParam
    {
        // 此处示教的相机轴坐标统一转至上相机图像坐标系，再转至相机轴坐标
        // 初期不标定real真值，使用轴光栅=真值

        /// <summary>
        /// Gets or Sets 示教wafer die 的模版轴坐标
        /// </summary>
        public List<Point> WaferDiePatternAtRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教wafer die 的角点轴坐标
        /// </summary>
        public List<Point> WaferDieCornerAtRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教FuStage die 的模版轴坐标
        /// </summary>
        public List<Point> FuDiePatternAtRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教FuStage die 的角点轴坐标
        /// </summary>
        public List<Point> FuDieCornerAtRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教时IC左上右下Pattern中心对应相机轴坐标
        /// </summary>
        public List<Point> IcPatternAtRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教时IC 4个Bump点中心对应的相机轴坐标
        /// </summary>
        public List<Point> IcBumpAtRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教时基板左上右下Pattern中心对应的相机轴坐标
        /// </summary>
        public List<Point> SubPatternAtRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教时基板 4个Bump点对应的相机轴坐标(保证与IC Bump点一一对应)
        /// </summary>
        public List<Point> SubBumpAtRuler { get; set; }

        /// <summary>
        /// Gets or Sets 示教时IC左上右下Pattern中心
        /// </summary>
        public List<Point> IcPatternAtReal { get; set; }

        /// <summary>
        /// Gets or Sets 示教时IC 4个Bump点中心
        /// </summary>
        public List<Point> IcBumpAtReal { get; set; }

        /// <summary>
        /// Gets or Sets 示教时基板左上右下Pattern中心
        /// </summary>
        public List<Point> SubPatternAtReal { get; set; }

        /// <summary>
        /// Gets or Sets 示教时基板 4个Bump点对应的相机轴坐标(保证与IC Bump点一一对应)
        /// </summary>
        public List<Point> SubBumpAtReal { get; set; }
    }

    /// <summary>
    /// UBD head轴 方向参数
    /// </summary>
    public class UBDAxisParam
    {
        /// <summary>
        /// Gets or sets the user's head 轴X方向
        /// </summary>
        public int HeadAxisX { get; set; } = 1;

        /// <summary>
        /// Gets or sets the user's head轴Y方向
        /// </summary>
        public int HeadAxisY { get; set; } = 1;

        /// <summary>
        /// Gets or sets the user's head轴旋转方向
        /// </summary>
        public int HeadTheta { get; set; } = 1;

        /// <summary>
        /// Gets or sets the user's waferDie角度
        /// </summary>
        public double WaferAngleDie { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets the user's bond check error 异常范围
        /// </summary>
        public double BondCheckErrorThresh { get; set; } = 0.003;

        /// <summary>
        /// Gets or sets the user's 玻璃ic贴片角度阈值
        /// </summary>
        public double BondICAngle { get; set; } = 0.002;

        /// <summary>
        /// Gets or sets the user's 玻璃ic贴片偏移量X阈值
        /// </summary>
        public double BondICOffsetX { get; set; } = 0.0002;

        /// <summary>
        /// Gets or sets the user's 玻璃ic贴片偏移量Y阈值
        /// </summary>
        public double BondICOffsetY { get; set; } = 0.0002;
    }
}
