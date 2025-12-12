using DataStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;
using OpenCvSharp;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using Point = DataStruct.Point;
using System.Drawing.Imaging;
using UltrapreciseBonding.FusionCollections.AVM;
using System.Security.Cryptography;

namespace UltrapreciseBonding.TemporaryBonding
{
    /// <summary>
    /// UBT测量
    /// </summary>
    public static class UBTMeasure
    {
        private static int _mapSize = 0;
        private static double _pixScale = 1.0;

        private static double _colorAxisMax = -1.0;
        private static double _colorAxisMin = 1.0;
        private static double _colorVisMax = -1.0;
        private static double _colorVisMin = 1.0;

        private static string _colorType = "";

        private static int[] _colorTableR = new int[256];
        private static int[] _colorTableG = new int[256];
        private static int[] _colorTableB = new int[256];

        private static int[] _areaMask = null;
        private static double[] _heightMap = null;

        //private static Camera _heightImg = null;

        /// <summary>
        /// 初始化配置参数
        /// </summary>
        /// <param name="visParams">配置参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetParams(MeasureVisParams visParams)
        {
            _mapSize = visParams.MeasureSizeOfMap;
            _pixScale = visParams.MeasureSizeAtReal / visParams.MeasureSizeOfMap;

            _colorAxisMax = visParams.ColorAxisMax;
            _colorAxisMin = visParams.ColorAxisMin;
            _colorVisMax = visParams.ColorVisMax;
            _colorVisMin = visParams.ColorVisMin;

            //if (_colorType.Equals(visParams.ColorType))
            //{
            //    return Errortype.OK;
            //}
            if (visParams.ColorType is null)
            {
                return Errortype.UBT_MEASURE_COLOR_SET_ERROR;
            }

            _colorType = visParams.ColorType;
            return UpdateColorTable(_colorType);
        }

        /// <summary>
        /// 生成map图
        /// </summary>
        /// <param name="sampleP">采样点径长</param>
        /// <param name="sampleThetaDeg">采样点极角</param>
        /// <param name="sampleZ">采样点高度值</param>
        /// <param name="mapImage">输出map图</param>
        /// <param name="minPointInMap">输出map图上最小值坐标</param>
        /// <param name="maxPointInMap">输出map图上最大值坐标</param>
        /// <param name="minAtAxisRow">输出最小值在色轴上的高度</param>
        /// <param name="maxAtAxisRow">输出最大值在色轴上的高度</param>
        /// <param name="showIndexLabel">是否在map图像上标注点位序号</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GenMapImage(List<double> sampleP, List<double> sampleThetaDeg, List<double> sampleZ, out Camera mapImage,
            out Point3D minPointInMap, out Point3D maxPointInMap, out double minAtAxisRow, out double maxAtAxisRow, bool showIndexLabel = true)
        {
            showIndexLabel = true;
            mapImage = new Camera();
            List<Point3D> samplePointsInMap = new List<Point3D>();
            List<double> labelXValues = new List<double>();
            List<double> labelYValues = new List<double>();
            minPointInMap = null;
            maxPointInMap = null;
            minAtAxisRow = 0.0;
            maxAtAxisRow = 0.0;
            int margin = 0;

            if ((sampleP is null) || (sampleZ is null) || (sampleThetaDeg is null))
            {
                return Errortype.UBT_MEASURE_INPUT_POINTS_NULL;
            }

            if ((sampleP.Count != sampleThetaDeg.Count) || (sampleZ.Count != sampleThetaDeg.Count))
            {
                return Errortype.UBT_MEASURE_INPUT_POINTS_NULL;
            }

            for (int i = 0; i < sampleP.Count; i++)
            {
                PolarToMapXY(sampleP[i], sampleThetaDeg[i], out Point samplePointXY);
                samplePointsInMap.Add(new Point3D(samplePointXY.X, samplePointXY.Y, sampleZ[i]));
                labelXValues.Add(samplePointXY.X + margin);
                labelYValues.Add(samplePointXY.Y + margin);
            }

            ComAlgo.FitCurvedSurface(samplePointsInMap, _mapSize, out _heightMap);
            HTuple heightTuple = new HTuple(_heightMap);
            HTuple mask = new HTuple(_areaMask);

            HOperatorSet.GenImageConst(out HObject domain, "byte", _mapSize, _mapSize);
            HOperatorSet.GetRegionPoints(domain, out HTuple pixRows, out HTuple pixCols);
            domain.Dispose();

            // 统计深度范围和最值位置
            HOperatorSet.TupleSelectMask(heightTuple, mask, out HTuple heightInRegion);
            HOperatorSet.TupleMax(heightInRegion, out HTuple maxHeight);
            HOperatorSet.TupleMin(heightInRegion, out HTuple minHeight);
            HOperatorSet.TupleFindFirst(heightTuple, maxHeight, out HTuple maxId);
            HOperatorSet.TupleFindFirst(heightTuple, minHeight, out HTuple minId);
            HOperatorSet.TupleMod(maxId, _mapSize, out HTuple maxCol);
            HOperatorSet.TupleMod(minId, _mapSize, out HTuple minCol);
            HOperatorSet.TupleRound(maxId / _mapSize, out HTuple maxRow);
            HOperatorSet.TupleRound(minId / _mapSize, out HTuple minRow);
            maxPointInMap = new Point3D(maxCol.I, maxCol.I, maxHeight.D);
            minPointInMap = new Point3D(minCol.I, minCol.I, minHeight.D);

            // 调整可视化上下限参数
            if (_colorVisMax <= _colorVisMin)
            {
                _colorVisMax = maxHeight.D;
                _colorVisMin = minHeight.D;
            }

            if (_colorAxisMax <= _colorAxisMin)
            {
                _colorAxisMax = maxHeight.D;
                _colorAxisMin = minHeight.D;
            }

            // 计算高度-灰度映射系数
            var colorScale = 255.0 / (maxHeight - minHeight);

            // 统计着色蒙板
            HOperatorSet.TupleLessEqualElem(heightTuple, _colorVisMax, out HTuple visLess);
            HOperatorSet.TupleGreaterEqualElem(heightTuple, _colorVisMin, out HTuple visGreat);
            HOperatorSet.TupleLessEqualElem(heightTuple, _colorAxisMax, out HTuple axisLess);
            HOperatorSet.TupleGreaterEqualElem(heightTuple, _colorAxisMin, out HTuple axisGreat);
            mask = mask * axisGreat * axisLess * visGreat * visLess;

            // 按灰度系数和蒙板计算灰度值
            heightTuple = (heightTuple - minHeight) * colorScale;
            HOperatorSet.TupleRound(heightTuple, out HTuple grayValue);

            // 将灰度映射到RGB
            HOperatorSet.TupleSelect(_colorTableR, grayValue * mask, out HTuple pixsR);
            HOperatorSet.TupleSelect(_colorTableG, grayValue * mask, out HTuple pixsG);
            HOperatorSet.TupleSelect(_colorTableB, grayValue * mask, out HTuple pixsB);

            // 生成RGB通道，右侧留出20像素空白画色彩轴
            HOperatorSet.GenImageConst(out HObject imgR, "byte", _mapSize + 20 + (margin * 2), _mapSize + (margin * 2));
            HOperatorSet.GenImageConst(out HObject imgG, "byte", _mapSize + 20 + (margin * 2), _mapSize + (margin * 2));
            HOperatorSet.GenImageConst(out HObject imgB, "byte", _mapSize + 20 + (margin * 2), _mapSize + (margin * 2));

            // 填色
            HOperatorSet.SetGrayval(imgR, pixRows + margin, pixCols + margin, pixsR * mask);
            HOperatorSet.SetGrayval(imgG, pixRows + margin, pixCols + margin, pixsG * mask);
            HOperatorSet.SetGrayval(imgB, pixRows + margin, pixCols + margin, pixsB * mask);

            // 处理色彩轴
            HOperatorSet.GenRectangle1(out HObject axisRect, margin, _mapSize + margin + 10, _mapSize + margin - 1,
                _mapSize + 19 + margin);
            HOperatorSet.GetRegionPoints(axisRect, out HTuple axisRows, out HTuple axisCols);
            HOperatorSet.TupleRound(255 - (axisRows * (255.0 / (_mapSize + margin))), out HTuple axisGray);
            HOperatorSet.TupleSelect(_colorTableR, axisGray, out HTuple axisR);
            HOperatorSet.TupleSelect(_colorTableG, axisGray, out HTuple axisG);
            HOperatorSet.TupleSelect(_colorTableB, axisGray, out HTuple axisB);
            HOperatorSet.SetGrayval(imgR, axisRows, axisCols, axisR);
            HOperatorSet.SetGrayval(imgG, axisRows, axisCols, axisG);
            HOperatorSet.SetGrayval(imgB, axisRows, axisCols, axisB);
            
            // 合并通道
            HOperatorSet.Compose3(imgR, imgG, imgB, out HObject resImg);

            if (showIndexLabel)
            {
                HOperatorSet.GetImageSize(resImg, out HTuple composeWidth, out HTuple composeHeight);
                HOperatorSet.OpenWindow(0, 0, composeWidth, composeHeight, 0, "invisible", "", out HTuple windowHandle);

                HOperatorSet.SetPart(windowHandle, 0, 0, composeHeight - 1, composeWidth - 1);
                HOperatorSet.DispObj(resImg, windowHandle);

                HOperatorSet.TupleGenSequence(1, labelXValues.Count, 1, out HTuple indexLabel);
                HOperatorSet.DispText(windowHandle, indexLabel, "image", labelYValues.ToArray(), labelXValues.ToArray(),
                    "black", new HTuple(), new HTuple());

                HOperatorSet.DumpWindowImage(out HObject indexImage, windowHandle);
                mapImage = new Camera(indexImage);
                indexImage.Dispose();
            }
            else
            {
                mapImage = new Camera(resImg);
            }

            resImg.Dispose();
            axisRect.Dispose();
            imgR.Dispose();
            imgG.Dispose();
            imgB.Dispose();

            // 计算最值在色彩轴的高度
            var minAtAxis = _mapSize - ((minHeight - _colorAxisMin) * colorScale / 255.0 * _mapSize);
            var maxAtAxis = _mapSize - ((maxHeight - _colorAxisMin) * colorScale / 255.0 * _mapSize);
            minAtAxisRow = minAtAxis.D;
            maxAtAxisRow = maxAtAxis.D;
            return Errortype.OK;
        }

        /// <summary>
        /// 查询map图上单点高度值
        /// </summary>
        /// <param name="pixXY">map图XY坐标</param>
        /// <param name="heightValue">高度值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetHeightInMap(Point pixXY, out double heightValue)
        {
            heightValue = -1;
            if (_heightMap == null)
            {
                return Errortype.UBT_MEASURE_POINTS_MAP_NULL;
            }

            // 建立深度图
            HOperatorSet.GenImageConst(out HObject heightHImg, "real", _mapSize, _mapSize);
            HOperatorSet.GetRegionPoints(heightHImg, out HTuple imgRows, out HTuple imgCols);
            HOperatorSet.SetGrayval(heightHImg, imgRows, imgCols, _heightMap);

            // 查询亚像素位置的深度值
            HOperatorSet.GetGrayvalInterpolated(heightHImg, pixXY.Y, pixXY.X, "bicubic", out HTuple hValue);

            heightValue = hValue.D;
            heightHImg.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 查询实际坐标单点高度值
        /// </summary>
        /// <param name="ρValue">径长</param>
        /// <param name="thetaDeg">角度</param>
        /// <param name="heightValue">高度值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetHeightInReal(double ρValue, double thetaDeg, out double heightValue)
        {
            PolarToMapXY(ρValue, thetaDeg, out Point mapXY);
            return GetHeightInMap(mapXY, out heightValue);
        }

        /// <summary>
        /// 极坐标转像素XY坐标
        /// </summary>
        /// <param name="ρValue">径长</param>
        /// <param name="thetaDeg">极角</param>
        /// <param name="pointXY">XY坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype PolarToMapXY(double ρValue, double thetaDeg, out Point pointXY)
        {
            HOperatorSet.TupleRad(thetaDeg, out HTuple thetaRad);
            HOperatorSet.TupleCos(thetaRad, out HTuple cosValue);
            HOperatorSet.TupleSin(thetaRad, out HTuple sinValue);
            pointXY = new Point(cosValue.D * ρValue / _pixScale + (_mapSize / 2.0), -sinValue.D * ρValue / _pixScale + (_mapSize / 2.0));
            return Errortype.OK;
        }

        /// <summary>
        /// 像素坐标转极坐标
        /// </summary>
        /// <param name="pixXYInMap">XY坐标</param>
        /// <param name="ρValue">径长</param>
        /// <param name="thetaDeg">极角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype MapXYToPolar(Point pixXYInMap, out double ρValue, out double thetaDeg)
        {
            ρValue = 0.0;
            thetaDeg = 0.0;
            if (pixXYInMap is null)
            {
                return Errortype.UBT_MEASURE_INPUT_POINTS_NULL;
            }

            var realXY = new Point((pixXYInMap.X - (_mapSize / 2.0)) * _pixScale, (-pixXYInMap.Y + (_mapSize / 2.0)) * _pixScale);
            ρValue = realXY.DistanceTo(new Point(0, 0));

            // 点在Y正轴上时，theta为90°
            if ((realXY.X == 0.0) && (realXY.Y >= 0.0))
            {
                thetaDeg = 90.0;
            }
            else if ((realXY.X == 0.0) && (realXY.Y < 0.0))
            {
                // 点在Y负轴上时，theta为-90°
                thetaDeg = -90.0;
            }
            else
            {
                HOperatorSet.TupleAtan(realXY.Y / realXY.X, out HTuple thetaRad);
                HOperatorSet.TupleDeg(thetaRad, out HTuple degValue);
                thetaDeg = degValue.D;

                // 第三象限：-90°- -180°
                if ((realXY.X < 0.0) && (realXY.Y < 0.0))
                {
                    thetaDeg = -thetaDeg - 90.0;
                }
                else if ((realXY.X < 0.0) && (realXY.Y > 0.0))
                {
                    // 第二象限：90°- 180°
                    thetaDeg = -thetaDeg + 90.0;
                }
                else if ((realXY.X < 0.0) && (realXY.Y == 0.0))
                {
                    // 在X负轴上时，theta为180°
                    thetaDeg = 180.0;
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// XY轴坐标转极轴坐标
        /// </summary>
        /// <param name="axisXY">XY坐标</param>
        /// <param name="ρValue">径长</param>
        /// <param name="thetaDeg">极角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype XYAxisToPolarAxis(Point axisXY, out double ρValue, out double thetaDeg)
        {
            ρValue = 0.0;
            thetaDeg = 0.0;
            if (axisXY is null)
            {
                return Errortype.UBT_MEASURE_INPUT_POINTS_NULL;
            }

            var realXY = axisXY;
            ρValue = realXY.DistanceTo(new Point(0, 0));

            // 点在Y正轴上时，theta为90°
            if ((realXY.X == 0.0) && (realXY.Y >= 0.0))
            {
                thetaDeg = 90.0;
            }
            else if ((realXY.X == 0.0) && (realXY.Y < 0.0))
            {
                // 点在Y负轴上时，theta为-90°
                thetaDeg = -90.0;
            }
            else
            {
                HOperatorSet.TupleAtan(realXY.Y / realXY.X, out HTuple thetaRad);
                HOperatorSet.TupleDeg(thetaRad, out HTuple degValue);
                thetaDeg = degValue.D;
                if ((realXY.X < 0.0) && (realXY.Y < 0.0))
                {
                    // 第三象限：-90°- -180°
                    thetaDeg = -thetaDeg - 90.0;
                }
                else if ((realXY.X < 0.0) && (realXY.Y > 0.0))
                {
                    // 第二象限：90°- 180°
                    thetaDeg = -thetaDeg + 90.0;
                }
                else if ((realXY.X < 0.0) && (realXY.Y == 0.0))
                {
                    // 在X负轴上时，theta为180°
                    thetaDeg = 180.0;
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 极轴坐标转XY轴坐标（chuck坐标）
        /// </summary>
        /// <param name="ρValue">径长</param>
        /// <param name="thetaDeg">极角</param>
        /// <param name="axisXY">XY坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype PolarAxisToXYAxis(double ρValue, double thetaDeg, out Point axisXY)
        {
            HOperatorSet.TupleRad(thetaDeg, out HTuple thetaRad);
            HOperatorSet.TupleCos(thetaRad, out HTuple cos);
            HOperatorSet.TupleSin(thetaRad, out HTuple sin);
            double xValue = ρValue * cos.D;
            double yValue = ρValue * sin.D;
            axisXY = new Point(xValue, yValue);
            return Errortype.OK;
        }

        /// <summary>
        /// 生成测量点
        /// </summary>
        /// <param name="maxRadius">测量的半径</param>
        /// <param name="exclusive">边距</param>
        /// <param name="intervalXY">测量点XY间隔</param>
        /// <param name="shiftXY">测量点整体XY平移量</param>
        /// <param name="outρValue">输出测量点极径</param>
        /// <param name="outThetaDeg">输出测量点极角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GenMeasurePoints(double maxRadius, double exclusive, Point intervalXY, Point shiftXY, out List<double> outρValue, out List<double> outThetaDeg)
        {
            outρValue = new List<double>();
            outThetaDeg = new List<double>();
            var ret = AvmMetrology.GenMeasureArray(maxRadius, exclusive, intervalXY, shiftXY, out List<Point> outPutPositions);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            foreach (var point in outPutPositions)
            {
                var ρValue = point.DistanceTo(new Point(0, 0));
                double thetaDeg = 0.0;

                // 点在Y正轴上时，theta为90°
                if ((point.X == 0.0) && (point.Y >= 0.0))
                {
                    thetaDeg = 90.0;
                }
                else if ((point.X == 0.0) && (point.Y < 0.0))
                {
                    // 点在Y负轴上时，theta为-90°
                    thetaDeg = -90.0;
                }
                else
                {
                    HOperatorSet.TupleAtan(point.Y / point.X, out HTuple thetaRad);
                    HOperatorSet.TupleDeg(thetaRad, out HTuple degValue);
                    thetaDeg = degValue.D;
                    if ((point.X < 0.0) && (point.Y < 0.0))
                    {
                        // 第三象限：-90°- -180°
                        thetaDeg = -thetaDeg - 90.0;
                    }
                    else if ((point.X < 0.0) && (point.Y > 0.0))
                    {
                        // 第二象限：90°- 180°
                        thetaDeg = -thetaDeg + 90.0;
                    }
                    else if ((point.X < 0.0) && (point.Y == 0.0))
                    {
                        // 在X负轴上时，theta为180°
                        thetaDeg = 180.0;
                    }
                }

                outρValue.Add(ρValue);
                outThetaDeg.Add(thetaDeg);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 生成测量点简化版
        /// </summary>
        /// <param name="minRadius">测量中心屏蔽区半径，小于0则不屏蔽</param>
        /// <param name="maxRadius">测量区域半径</param>
        /// <param name="outPutNums">需要输出的点数或层数</param>
        /// <param name="outρValue">输出坐标极经长度</param>
        /// <param name="outThetaDeg">输出坐标极角</param>
        /// <param name="useCircleArrange">是否使用圆形排布，若使用则output输入层数，默认使用螺旋排布</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GenMeasurePoints(double minRadius, double maxRadius, int outPutNums, out List<double> outρValue, out List<double> outThetaDeg, bool useCircleArrange = true)
        {
            outρValue = new List<double>();
            outThetaDeg = new List<double>();
            if ((maxRadius <= 0) || (outPutNums <= 0))
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            HTuple radiusValue;
            HTuple degValue;

            // 向日葵点阵分布
            if (!useCircleArrange)
            {
                double alpha = 2.0;
                double numSqrt = Math.Sqrt(outPutNums);
                double round = Math.Round(numSqrt * alpha);
                double goldPhi = (Math.Sqrt(5) + 1) / 2;

                HOperatorSet.TupleGenSequence(1, outPutNums, 1, out HTuple id);
                HOperatorSet.TupleGreaterElem(id, outPutNums - round, out HTuple outerMask);
                HOperatorSet.TupleNot(outerMask, out HTuple innerMask);

                HOperatorSet.TupleSqrt(id - 1 / 2, out HTuple sqrt1);
                HOperatorSet.TupleSqrt(outPutNums - (round + 1) / 2, out HTuple sqrt2);
                var radiusRate = sqrt1 / sqrt2 * innerMask + outerMask;
                var thetas = 2 * Math.PI * id / (goldPhi * goldPhi);
                HOperatorSet.TupleDeg(thetas, out degValue);
                radiusValue = radiusRate * maxRadius;
            }
            else 
            {
                // 圆形点阵分布
                int circleCore = 6; // 种子数量
                double radiusStep = maxRadius / outPutNums;
                degValue = new HTuple(0);
                radiusValue = new HTuple(0);
                for (int circleId = 0; circleId < outPutNums; circleId++)
                {
                    int numOnCircle = circleCore + (circleId * circleCore);
                    double thetaStep = 360.0 / numOnCircle;
                    HOperatorSet.TupleGenSequence(0, 360.0 - thetaStep, thetaStep, out HTuple thetaOnCircle);
                    HOperatorSet.TupleGenConst(thetaOnCircle.Length, (circleId + 1) * radiusStep, out HTuple radiusOnCircle);
                    degValue = degValue.TupleConcat(thetaOnCircle + (360.0 * circleId));
                    radiusValue = radiusValue.TupleConcat(radiusOnCircle);
                }
            }

            // 筛选去除圆心附近测不到的点（机械轴极限）
            HOperatorSet.TupleGreaterElem(radiusValue, minRadius, out HTuple minMask);
            HOperatorSet.TupleSelectMask(degValue, minMask, out HTuple selectedDeg);
            HOperatorSet.TupleSelectMask(radiusValue, minMask, out HTuple selectedRadius);
            outThetaDeg = selectedDeg.DArr.ToList();
            outρValue = selectedRadius.DArr.ToList();
            selectedDeg.UnPinTuple();
            selectedRadius.UnPinTuple();

            return Errortype.OK;
        }

        /// <summary>
        /// 切换颜色映射表
        /// </summary>
        /// <param name="colorTypeName">颜色类型名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal static Errortype UpdateColorTable(string colorTypeName)
        {
            Mat baseGray = new Mat(1, 256, MatType.CV_8UC1);
            for (int i = 0; i < 256; i++)
            {
                baseGray.Set<byte>(0, i, (byte)i);
            }

            Mat colorValue = new Mat();

            ColormapTypes colorMapType = (ColormapTypes)Enum.Parse(typeof(ColormapTypes), colorTypeName);
            Cv2.ApplyColorMap(baseGray, colorValue, colorMapType);
            for (int colId = 0; colId < 256; colId++)
            {
                var values3b = colorValue.Get<Vec3b>(0, colId);
                _colorTableB[colId] = values3b.Item0;
                _colorTableG[colId] = values3b.Item1;
                _colorTableR[colId] = values3b.Item2;
            }

            HOperatorSet.GenImageConst(out HObject imgMask, "byte", _mapSize, _mapSize);
            HOperatorSet.GetDomain(imgMask, out HObject domain);
            HOperatorSet.GetRegionPoints(domain, out HTuple domainRows, out HTuple domainCols);
            HOperatorSet.GenCircle(out HObject circleMask, _mapSize / 2.0, _mapSize / 2.0, _mapSize / 2.0);
            HOperatorSet.PaintRegion(circleMask, imgMask, out HObject imgFill, 1, "fill");
            HOperatorSet.GetGrayval(imgFill, domainRows, domainCols, out HTuple grayval);
            _areaMask = grayval.ToIArr();
            imgMask.Dispose();
            imgFill.Dispose();
            circleMask.Dispose();
            domain.Dispose();
            return Errortype.OK;
        }
    }

    /// <summary>
    /// 测量区域可视化参数
    /// </summary>
    public class MeasureVisParams
    {
        private double _measureSizeAtReal;

        /// <summary>
        /// Gets or Sets 测量区域的实际大小
        /// </summary>
        public double MeasureSizeAtReal
        {
            get => _measureSizeAtReal;
            set => _measureSizeAtReal = value;
        }

        private int _measureSizeOfMap;

        /// <summary>
        /// Gets or Sets 测量区域的可视化图像大小
        /// </summary>
        public int MeasureSizeOfMap
        {
            get => _measureSizeOfMap;
            set => _measureSizeOfMap = value;
        }

        private double _colorAxisMax;

        /// <summary>
        /// Gets or Sets 色彩映射轴的最大高度，范围外的高度为黑色
        /// </summary>
        public double ColorAxisMax
        {
            get => _colorAxisMax;
            set => _colorAxisMax = value;
        }

        private double _colorAxisMin;

        /// <summary>
        /// Gets or Sets 色彩映射轴的最小高度，范围外的高度为黑色
        /// </summary>
        public double ColorAxisMin
        {
            get => _colorAxisMin;
            set => _colorAxisMin = value;
        }

        private double _colorVisMin;

        /// <summary>
        /// Gets or Sets 着色的最小值，范围外的高度不上色
        /// </summary>
        public double ColorVisMin
        {
            get => _colorVisMin;
            set => _colorVisMin = value;
        }

        private double _colorVisMax;

        /// <summary>
        /// Gets or Sets 着色的最大值，范围外的高度不上色
        /// </summary>
        public double ColorVisMax
        {
            get => _colorVisMax;
            set => _colorVisMax = value;
        }

        private string _colorType;

        /// <summary>
        /// Gets or Sets 配色类型
        /// </summary>
        public string ColorType
        {
            get => _colorType;
            set => _colorType = value;
        }

        /// <summary>
        /// 可视化参数
        /// </summary>
        public MeasureVisParams()
        {
            MeasureSizeAtReal = 500.0;
            MeasureSizeOfMap = 500;
            ColorAxisMax = -1.0;
            ColorVisMax = -1.0;
            ColorAxisMin = 1.0;
            ColorVisMin = 1.0;
            ColorType = GetColorTypes()[0];
        }

        /// <summary>
        /// 获取所有配色方案名
        /// </summary>
        /// <returns>List<String></returns>
        public static List<String> GetColorTypes()
        {
            List<string> colorType = new List<string>();
            foreach (ColormapTypes colorName in Enum.GetValues(typeof(ColormapTypes)))
            {
                colorType.Add(colorName.ToString());
            }

            return colorType;
        }
    }
}
