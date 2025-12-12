using DataStruct;
using HalconDotNet;
using IniFileHelper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.Ransac;
using UltrapreciseBonding.TemplateMatch;
using UltrapreciseBonding.UBCManager;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.DieBonding
{
    /// <summary>
    /// diebond 通用算法
    /// </summary>
    public static class DieBondComAlgo
    {
        /// <summary>   TODO 测试完成  暂时没用
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
                return Errortype.INPUT_NULL;
            }

            //double pixWidth = cropSize.X / pisSize;
            //double pixHeight = cropSize.Y / pisSize;
            int newWidth = (int)(Math.Floor(cropSize.X / 4) * 4); //保证裁剪后的图像为4的倍数
            int newHeight = (int)(Math.Floor(cropSize.Y / 4) * 4);
            double stepX = newWidth / 2;
            double stepY = newHeight / 2;
            Point pixCenter = new Point(img.Width / 2, img.Height / 2);

            HOperatorSet.GenEmptyObj(out HObject emptyObject);
            emptyObject = img.GenHObject();
            HOperatorSet.Rgb1ToGray(emptyObject, out HObject grayImage);
            HOperatorSet.GenRectangle1(out HObject rectangle, pixCenter.Y - stepY + 1, pixCenter.X - stepX + 1, pixCenter.Y + stepY, pixCenter.X + stepX);
            HOperatorSet.ReduceDomain(grayImage, rectangle, out HObject reduceImg);
            HOperatorSet.CropDomain(reduceImg, out HObject cropImagePart);

            //HOperatorSet.WriteImage(cropImagePart,"bmp",0, @"D:/Maxwell/TASK/UBH/waferLensData/wafeLens2.0/reduceImge.bmp");
            cropImg = new Camera(cropImagePart);

            grayImage.Dispose();
            rectangle.Dispose();
            reduceImg.Dispose();
            cropImagePart.Dispose();
            emptyObject.Dispose();

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
        private static Errortype GetCutPathEdge(Camera image, Rectangle1 maskRegion, out LineSeg minLine, out LineSeg maxLine, string horOrVer = "hor")
        {
            minLine = null;
            maxLine = null;
            if (image.Width < 1)
            {
                return Errortype.INPUT_IMAGE_NULL;
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
            ret = ComAlgo.FitLine(minPoints, out minLine, out double[] straightnessErrorMin);
            if (ret != Errortype.OK)
            {
                img.Dispose();
                return ret;
            }

            ret = RansacLine.LineExtraction(usedMaxP, ransacParam, out List<Point> maxPoints);
            ret = ComAlgo.FitLine(maxPoints, out maxLine, out double[] straightnessErrorMax);
            if (ret != Errortype.OK)
            {
                img.Dispose();
                return ret;
            }

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
        private static Errortype RemoveNoise(List<Point> edgePoints, double gap, out List<Point> usedPoints)
        {
            usedPoints = new List<Point>();
            if (edgePoints.Count < 2)
            {
                return Errortype.UBH_INPUT_POINT_NULL;
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
        /// 识别切割道角点
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="centerPoint">切割道十字中心像素坐标</param>
        /// <param name="selectRegion">识别区域</param>
        /// <param name="leftTOrRightB">区分左上角(leftTop)和右下角(rightBottom)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcCutPathCorner(Camera img, out Point centerPoint, Region selectRegion = null, string leftTOrRightB = "leftTop")
        {
            centerPoint = new Point();
            if (img is null)
            {
                return Errortype.DIEBONDCOMMALGO_CALCCUTPATHCORNER_INPUT_IMAGE_NULL;
            }

            if (img.Width < 1)
            {
                return Errortype.DIEBONDCOMMALGO_CALCCUTPATHCORNER_INPUT_IMAGE_ERROR;
            }

            Rectangle1 rectTmp = new Rectangle1(0, 0, img.Width, img.Height);
            Rectangle1 rect = selectRegion == null ? rectTmp : selectRegion.Rectangle1;
            var ret = GetCutPathEdge(img, rect, out LineSeg minLineH, out LineSeg maxLineH, "hor");
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = GetCutPathEdge(img, rect, out LineSeg minLineV, out LineSeg maxLineV, "ver");
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

            List<Point> centerPoints = new List<Point>();
            centerPoints.Add(centerPoint);

            /*
            if (ComAlgo.SaveFlg("CalcCutPathCorner"))
            {
                LabelImg(img, centerPoints, out Camera imgLabel);
                ret = ComAlgo.SaveCameraImage(imgLabel);
                imgLabel.Dispose();
            }
            */
            return Errortype.OK;
        }

        /// <summary>
        /// 计算wafer偏角（不限制方向以及X、Y轴）
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
                return Errortype.DIEBONDCOMMALGO_CALCWAFERANGLE_INPUT_POINTS_NULL;
            }

            if (startPoint == endPoint)
            {
                return Errortype.DIEBONDCOMMALGO_CALCWAFERANGLE_INPUT_POINTS_EQUAL;
            }

            // 计算与X轴夹角
            HOperatorSet.AngleLx(startPoint.Y, startPoint.X, endPoint.Y, endPoint.X, out HTuple angleRad);
            HOperatorSet.TupleDeg(angleRad, out HTuple angleDeg);

            // 计算水平或竖直方向的最小倾角 (夹角和最小90°倍数差)
            HOperatorSet.TupleRound(angleDeg / 90.0, out HTuple roundRate);
            var resDeg = angleDeg - (90.0 * roundRate);
            waferRotateOffset = resDeg.D;

            return Errortype.OK;
        }

        /// <summary>
        /// 点集坐标刚体转换
        /// </summary>
        /// <param name="src">用于计算刚体变换矩阵的原始数据</param>
        /// <param name="dst">用于计算刚体变换矩阵的目标数据</param>
        /// <param name="toTransPoint">待转换的坐标点集</param>
        /// <param name="transPoint">转换后的坐标点集</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype RigidTransPoint2d(List<Point> src, List<Point> dst, List<Point> toTransPoint, out List<Point> transPoint)
        {
            transPoint = new List<Point>();
            if (toTransPoint == null)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            Errortype ret = ComAlgo.VectorToRigid(src, dst, out double[] homMat2d, out List<Point> error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            for (int i = 0; i < toTransPoint.Count; i++)
            {
                ret = ComAlgo.AffineTransPoint2d(toTransPoint[i], homMat2d, out Point targetPoint);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                transPoint.Add(targetPoint);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算玻璃IC mark中心，包括圆环
        /// </summary>
        /// <param name="markImage">mark图像</param>
        /// <param name="centerExtract">输出各个mark定位中心坐标</param>
        /// <param name="markRadius">输出各个mark半径，用于可视化</param>
        /// <param name="isCircle">输出对应的各中心是否是圆环</param>
        /// <param name="markParams">mark抓取配置参数</param>
        /// <returns>ok：计算完成</returns>
        public static Errortype CalcCircleRingMarkCenter(Camera markImage, out List<Point> centerExtract, out List<double> markRadius, out List<bool> isCircle, GlassIcMarkParams markParams = null)
        {
            var ret = Errortype.UNKNOW_ERROR;
            centerExtract = new List<Point>();
            markRadius = new List<double>();
            isCircle = new List<bool>();

            markParams = new GlassIcMarkParams();
            string path = @"D:\Alg\";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = path + "\\RingMarkParam.ini";
            if (!File.Exists(path))
            {
                markParams.Save(path);
            }

            markParams.Load(path);

            if (markImage is null)
            {
                return Errortype.DIEBONDCOMMALGO_CALCCIRCLERINGMARKCENTER_IMAGE_NULL;
            }

            var hImage = markImage.GenHObject();
            HOperatorSet.Rgb1ToGray(hImage, out HObject imgGray);
            hImage.Dispose();

            if (markParams.IsFilter == "true")
            {
                HOperatorSet.MedianImage(imgGray, out HObject imageMedian, "circle", 8, "mirrored");
                imgGray = imageMedian.Clone();
                imageMedian.Dispose();
            }

            HOperatorSet.EdgesImage(imgGray, out HObject imgAmp, out _, "canny", 1.5, "nms", markParams.EdgeThreshLow, markParams.EdgeThreshHigh);
            HOperatorSet.Threshold(imgAmp, out HObject regionEdge, 0, 255);

            //imageMedian.Dispose();
            // 筛选并去除脏污的小段边缘
            HOperatorSet.Connection(regionEdge, out HObject connectedEdge);
            HOperatorSet.SelectShape(connectedEdge, out HObject selectedEdge, "area", "and", 500, markImage.Width * 100);
            HOperatorSet.DilationCircle(selectedEdge, out HObject edgeDilation, 2);

            HOperatorSet.Difference(imgGray, edgeDilation, out HObject regionClipEdge);
            HOperatorSet.Connection(regionClipEdge, out HObject connectedRegions);
            HOperatorSet.FillUp(connectedRegions, out HObject regionFill);

            // 少选出面积小于视野一半并大于下限的region
            HOperatorSet.AreaCenter(regionFill, out HTuple fillArea, out _, out _);
            HOperatorSet.TupleLessElem(fillArea, markImage.Width * markImage.Height * 0.5, out HTuple lessMask);
            HOperatorSet.TupleGreaterElem(fillArea, markParams.AreaMin, out HTuple moreMask);
            HTuple validAreaMask = moreMask * lessMask;
            HOperatorSet.TupleFind(validAreaMask, 1, out HTuple validAreaIndex);

            if (validAreaIndex[0] > -1)
            {
                // 在有效面积区域内筛选面积和外接圆相似度大于阈值的填充区域
                HOperatorSet.SelectObj(regionFill, out HObject regionFillSelected, validAreaIndex + 1);

                HOperatorSet.AreaCenter(regionFillSelected, out HTuple fillAreaSelected, out _, out _);

                //HOperatorSet.SmallestCircle(regionFillSelected, out _, out _, out HTuple fillRadius);
                HOperatorSet.GenContourRegionXld(regionFillSelected, out HObject regionFillSelectedContour, "border");

                HOperatorSet.FitCircleContourXld(regionFillSelectedContour, "geotukey", -1, 3, 0, 3, 2,
                    out _, out _, out HTuple fillRadius, out _, out _, out _);
                HTuple circleRate = fillAreaSelected / (Math.PI * fillRadius * fillRadius) * 1.0; // 用填充区域面积与外接圆面积比较，约接近1.0和外接圆越近似
                regionFillSelectedContour.Dispose();

                HOperatorSet.TupleGreaterElem(circleRate, markParams.CircleRate, out HTuple circleMoreMask);
                HOperatorSet.TupleFind(circleMoreMask, 1, out HTuple circleRateValidIndex);
                if (circleRateValidIndex[0] > -1)
                {
                    // 将近似外接圆的区域和其对应填充区取出
                    HOperatorSet.SelectObj(connectedRegions, out HObject areaValidRegions, validAreaIndex + 1);
                    HOperatorSet.SelectObj(areaValidRegions, out HObject circleRateValidRegions, circleRateValidIndex + 1);
                    HOperatorSet.SelectObj(regionFillSelected, out HObject circleRateValidFillUps, circleRateValidIndex + 1);

                    HOperatorSet.AreaCenter(circleRateValidRegions, out HTuple areaCircleHole, out _, out _);
                    HOperatorSet.AreaCenter(circleRateValidFillUps, out HTuple areaCircleFill, out _, out _);

                    HTuple holeAreaRate = (areaCircleFill - areaCircleHole) * 1.0 / areaCircleFill;   // 计算区域空洞占整个填充区的面积比，留下空洞面积比低于阈值的圆和圆环
                    HOperatorSet.TupleLessElem(holeAreaRate, markParams.MaxHoleAreaRate, out HTuple validMask);
                    HOperatorSet.TupleFind(validMask, 1, out HTuple validHoleRateIndex);
                    HOperatorSet.SelectObj(circleRateValidRegions, out HObject validCircleRings, validHoleRateIndex + 1);
                    HOperatorSet.TupleSelect(holeAreaRate, validHoleRateIndex, out HTuple areaRateRes);

                    //HOperatorSet.SmallestCircle(validCircleRings, out HTuple circleRingRow, out HTuple circleRingCol, out HTuple circleRingRadius);
                    HOperatorSet.GenContourRegionXld(validCircleRings, out HObject validCircleRingContours, "border");
                    HOperatorSet.FitCircleContourXld(validCircleRingContours, "geotukey", -1, 3, 0, 3, 2,
                        out HTuple circleRingRow, out HTuple circleRingCol, out HTuple circleRingRadius, out _, out _,
                        out _);
                    HOperatorSet.TupleLessElem(areaRateRes, 0.1, out HTuple regionIsCircle);

                    validCircleRingContours.Dispose();
                    validCircleRings.Dispose();
                    circleRateValidFillUps.Dispose();
                    circleRateValidRegions.Dispose();
                    areaValidRegions.Dispose();

                    // todo:circleExtration
                    CaliperParams calipParam = new CaliperParams();
                    calipParam.NumInstances = 1;
                    calipParam.MinScore = markParams.CaliperScore;
                    calipParam.MeasureSigma = 2.0;
                    calipParam.MeasureLength1 = 35;
                    calipParam.MeasureLength2 = 6;
                    calipParam.NumMeasures = 240;
                    calipParam.MeasureThreshold = markParams.CaliperThresh;
                    calipParam.MeasureSelect = "first";
                    calipParam.MeasureTransition = "auto";
                    if (markParams.MarkColor == "light")
                    {
                        calipParam.MeasureTransition = "negative";
                    }
                    else if (markParams.MarkColor == "dark")
                    {
                        calipParam.MeasureTransition = "positive";
                    }

                    calipParam.MeasureInterpolation = "nearest_neighbor";

                    for (int index = 0; index < circleRingRadius.Length; index++)
                    {
                        Point circleCenter = new Point(circleRingCol[index].D, circleRingRow[index].D);
                        ret = CaliperCircle.CircleExtraction(markImage, circleCenter, circleRingRadius[index].D, calipParam, out Point centerOut, out double radius);
                        if (ret == Errortype.OK)
                        {
                            // if (ComAlgo.SaveFlg("CalcCircleRingMarkCenterError"))
                            // {
                            //     string path = @"D:\Alg\CalcCircleRingMarkCenterErrorImg";
                            //     if (!Directory.Exists(path))
                            //     {
                            //         Directory.CreateDirectory(path);
                            //     }
                            //
                            //     ComAlgo.FileManage(path, 7, out path);
                            //     ComAlgo.SaveImage(path, markImage);
                            // }
                            // return ret;
                            //continue;
                            centerExtract.Add(new Point(centerOut.X, centerOut.Y));
                            markRadius.Add(radius);
                            if (regionIsCircle[index] > 0)
                            {
                                isCircle.Add(true);
                            }
                            else
                            {
                                isCircle.Add(false);
                            }
                        }
                    }
                }

                regionFillSelected.Dispose();
            }
            else
            {
                ret = Errortype.CALIPER_CIRCLE_NULL;
            }

            connectedEdge.Dispose();
            selectedEdge.Dispose();
            regionFill.Dispose();
            regionClipEdge.Dispose();
            edgeDilation.Dispose();
            regionEdge.Dispose();
            imgAmp.Dispose();
            imgGray.Dispose();

            if (centerExtract.Count > 0)
            {
                ret = Errortype.OK;
            }

            return ret;
        }

        /// <summary>
        /// 标注图片
        /// </summary>
        /// <param name="img">原始图</param>
        /// <param name="featurePoints">标注点</param>
        /// <param name="imgLabel">标注后的图像</param>
        public static void LabelImg(Camera img, List<Point> featurePoints, out Camera imgLabel)
        {
            HObject himg = img.GenHObject();
            HOperatorSet.GetImageSize(himg, out HTuple composeWidth, out HTuple composeHeight);
            HOperatorSet.OpenWindow(0, 0, composeWidth, composeHeight, 0, "invisible", "", out HTuple windowHandle);
            HOperatorSet.SetColored(windowHandle, 12);
            HOperatorSet.SetLineWidth(windowHandle, 2);

            HOperatorSet.SetPart(windowHandle, 0, 0, composeHeight - 1, composeWidth - 1);
            HOperatorSet.DispObj(himg, windowHandle);
            List<double> labelRow = new List<double>();
            List<double> labelCol = new List<double>();
            foreach (var point in featurePoints)
            {
                labelRow.Add(point.Y);
                labelCol.Add(point.X);
            }

            HOperatorSet.GenCrossContourXld(out HObject cross, labelRow.ToArray(), labelCol.ToArray(), 10, 0.785398);

            HOperatorSet.DispObj(cross, windowHandle);

            HOperatorSet.DumpWindowImage(out HObject indexImage, windowHandle);
            imgLabel = new Camera(indexImage);
            cross.Dispose();
            indexImage.Dispose();
            himg.Dispose();
        }
    }

    /// <summary>
    /// 玻璃IC mark 参数类
    /// </summary>
    public class GlassIcMarkParams
    {
        private string _markColor = "auto";
        private string _isFilter = "true";
        private double _circleRate = 0.8;
        private double _maxHoleAreaRate = 0.4;
        private double _caliperScore = 0.5;
        private int _edgeThreshLow = 8;
        private int _edgeThreshHigh = 15;
        private int _caliperThresh = 10;
        private int _areaMin = 500;
        private int _markInCompleted = 0;

        /// <summary>
        /// Gets or sets mark颜色
        /// </summary>
        public string MarkColor
        {
            get => _markColor;
            set => _markColor = value;
        }

        /// <summary>
        /// Gets or sets mark颜色
        /// </summary>
        public string IsFilter
        {
            get => _isFilter;
            set => _isFilter = value;
        }

        /// <summary>
        /// Gets or sets mark圆形完美度
        /// </summary>
        public double CircleRate
        {
            get => _circleRate;
            set => _circleRate = value;
        }

        /// <summary>
        /// Gets or sets mark环形孔洞面积比例最小值
        /// </summary>
        public double MaxHoleAreaRate
        {
            get => _maxHoleAreaRate;
            set => _maxHoleAreaRate = value;
        }

        /// <summary>
        /// Gets or sets 卡尺圆分数下限
        /// </summary>
        public double CaliperScore
        {
            get => _caliperScore;
            set => _caliperScore = value;
        }

        /// <summary>
        /// Gets or sets 边缘提取第一阈值
        /// </summary>
        public int EdgeThreshLow
        {
            get => _edgeThreshLow;
            set => _edgeThreshLow = value;
        }

        /// <summary>
        /// Gets or sets 边缘提取第二阈值
        /// </summary>
        public int EdgeThreshHigh
        {
            get => _edgeThreshHigh;
            set => _edgeThreshHigh = value;
        }

        /// <summary>
        /// Gets or sets 卡尺圆阈值
        /// </summary>
        public int CaliperThresh
        {
            get => _caliperThresh;
            set => _caliperThresh = value;
        }

        /// <summary>
        /// Gets or sets mark 面积下限
        /// </summary>
        public int AreaMin
        {
            get => _areaMin;
            set => _areaMin = value;
        }

        /// <summary>
        /// Gets or sets mark 是否识别残缺玻璃IC圆和小网格板圆环
        /// </summary>
        public int MarkInCompleted
        {
            get => _markInCompleted;
            set => _markInCompleted = value;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="fromFile">从文件载入</param>
        public GlassIcMarkParams(string fromFile = null)
        {
            if (fromFile != null)
            {
                Load(fromFile);
            }
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <param name="toFile">参数文件名</param>
        /// <returns>ok：保存成功</returns>
        public Errortype Save(string toFile)
        {
            DirectoryInfo fileInfo = new DirectoryInfo(toFile);

            if (!fileInfo.Parent.Exists)
            {
                Directory.CreateDirectory(fileInfo.Parent.FullName);
            }

            List<string> keys = new List<string>();
            List<string> value = new List<string>();
            keys.Clear();
            value.Clear();
            keys.Add("MarkColor");
            keys.Add("CircleRate");
            keys.Add("MaxHoleRate");
            keys.Add("ExtractScore");
            keys.Add("ExtractThresh");
            keys.Add("AreaMin");
            keys.Add("IsFilter");
            keys.Add("MarkInCompleted");

            value.Add(_markColor);
            value.Add(_circleRate.ToString());
            value.Add(_maxHoleAreaRate.ToString());
            value.Add(_caliperScore.ToString());
            value.Add(_caliperThresh.ToString());
            value.Add(_areaMin.ToString());
            value.Add(_isFilter.ToString());
            value.Add(_markInCompleted.ToString());

            IniHelper.AddSectionWithKeyValues("icMarkInfo", keys, value, toFile);

            return Errortype.OK;
        }

        /// <summary>
        /// 载入
        /// </summary>
        /// <param name="fileIni">文件目录</param>
        /// <returns>返回保存结果</returns>
        public Errortype Load(string fileIni)
        {
            if (!File.Exists(fileIni))
            {
                var ret = Save(fileIni);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }

            if (!IniHelper.ExistSection("icMarkInfo", fileIni))
            {
                return Errortype.UBD_IC_MARK_PARAM_FILE_ERROR;
            }

            string[] keys = null;
            string[] values = null;
            IniHelper.GetAllKeyValues("icMarkInfo", out keys, out values, fileIni);
            if (values.Length != 8)
            {
                return Errortype.UBD_IC_MARK_PARAM_FILE_ERROR;
            }

            _markColor = values[0];
            _circleRate = Convert.ToDouble(values[1]);
            _maxHoleAreaRate = Convert.ToDouble(values[2]);
            _caliperScore = Convert.ToDouble(values[3]);
            _caliperThresh = Convert.ToInt32(values[4]);
            _areaMin = Convert.ToInt32(values[5]);
            _isFilter = values[6];
            _markInCompleted = Convert.ToInt32(values[7]);

            return Errortype.OK;
        }
    }

    /// <summary>
    /// head旋转角度标定
    /// </summary>
    public class CalibRotateAngle : Singleton<CalibRotateAngle>
    {
        private static List<KeyValuePair<double, double>> _angleDists = new List<KeyValuePair<double, double>>();

        /// <summary>
        /// 获取旋转角度
        /// </summary>
        /// <param name="rotateCenter">旋转中心</param>
        /// <param name="mark1">旋转前mark坐标</param>
        /// <param name="mark2">旋转后mark坐标</param>
        /// <param name="angle">旋转角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcRotateAngle(Point rotateCenter, Point mark1, Point mark2, out double angle)
        {
            HOperatorSet.AngleLl(rotateCenter.Y, rotateCenter.X, mark1.Y, mark1.X, rotateCenter.Y, rotateCenter.X, mark2.Y, mark2.X, out HTuple hAngle);
            HOperatorSet.TupleDeg(hAngle, out HTuple deg);
            angle = deg.D;
            if (angle < 0)
            {
                angle = angle + 360;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 添加角度-电机移动距离键值对
        /// </summary>
        /// <param name="moveInterval">电机移动步距</param>
        /// <param name="angles">旋转角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AddAngleDistPairs(double moveInterval, List<double> angles)
        {
            if (angles == null)
            {
                return Errortype.UBD_INPUT_ANGLE_NULL;
            }

            _angleDists.Add(new KeyValuePair<double, double>(0, 0)); //先添加行程为0,角度为0的键值对
            for (int i = 0; i < angles.Count; i++)
            {
                KeyValuePair<double, double> angleDist = new KeyValuePair<double, double>(angles[i], i * moveInterval + moveInterval);
                _angleDists.Add(angleDist);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 输入任意角度获取电机行程
        /// </summary>
        /// <param name="angle">旋转角度</param>
        /// <param name="motorDist">电机行程</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMotorDistByAngle(double angle, out double motorDist)
        {
            motorDist = 0;
            if (_angleDists.Count == 0)
            {
                return Errortype.UBD_ADD_ANGLE_NOT_COMPLETE;
            }

            //需要先完成添加角度行程键值对
            for (int i = 0; i < _angleDists.Count; i++)
            {
                if (_angleDists[0].Key > angle || _angleDists[_angleDists.Count - 1].Key < angle)
                {
                    return Errortype.UBD_HEAD_ANGLE_OUT_RANGE;
                }

                if (_angleDists[0].Key == angle)
                {
                    motorDist = _angleDists[0].Value;
                    break;
                }
                else if (_angleDists[_angleDists.Count - 1].Key == angle)
                {
                    motorDist = _angleDists[_angleDists.Count - 1].Value;
                    break;
                }
                else if (_angleDists[i].Key <= angle && _angleDists[i + 1].Key >= angle)
                {
                    KeyValuePair<double, double> firstP = _angleDists[i];
                    KeyValuePair<double, double> secondP = _angleDists[i + 1];
                    var ret = ComAlgo.LinearInterpolation(firstP, secondP, angle, out motorDist);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 释放
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _angleDists = new List<KeyValuePair<double, double>>();
            return Errortype.OK;
        }
    }

    /// <summary>
    /// 模板Mark对位时模板文件名称
    /// </summary>
    public enum AlignTemplateName
    {
        ICLeftTop,
        ICRightBottom,
        SubLeftTop,
        SubRightBottom,
    }
}
