using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using DataStruct;
using HalconDotNet;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.FusionCollections.AVM
{
    /// <summary>
    /// 滑片检测功能类
    /// </summary>
    public class SlideCheck
    {
        /// <summary>
        /// 计算滑片偏离
        /// (使用 minrectangle2，仅支持2片相同尺寸wafer滑片检)
        /// </summary>
        /// <param name="img">相机图像</param>
        /// <param name="waferPixCenter">两片wafer的圆心像素坐标</param>
        /// <param name="waferPixRadius">两片wafer的像素半径</param>
        /// <param name="slidePoint">两片wafer偏离最大处的线段</param>
        /// <returns>SLIDE_CHECK_IAMGE_NULL:输入图像为空/SLIDE_CHECK_WAFER_GRAB_ERROR:边缘抓取失败</returns>
        public Errortype CalcSlideOff(Camera img, out List<Point> waferPixCenter, out List<double> waferPixRadius, out List<Point> slidePoint)
        {
            waferPixCenter = new List<Point>();
            waferPixRadius = new List<double>();
            slidePoint = new List<Point>();
            if ((img == null) || (img.Width == 0) || (img.Height == 0))
            {
                return Errortype.SLIDE_CHECK_IAMGE_NULL;
            }

            HObject hImg = img.GenHObject();

            HOperatorSet.Rgb1ToGray(hImg, out HObject grayImg);
            HOperatorSet.GenEmptyObj(out HObject darkRegion);
            HOperatorSet.GenEmptyObj(out HObject connectedRegions);
            HOperatorSet.GenEmptyObj(out HObject selectedRegions);
            HOperatorSet.GenEmptyObj(out HObject contourXLD);

            HOperatorSet.GetImageSize(grayImg, out HTuple imgWidth, out HTuple imgHeight);
            HOperatorSet.BinaryThreshold(grayImg, out darkRegion, "smooth_histo", "dark", out HTuple usedThresh);
            HOperatorSet.Connection(darkRegion, out connectedRegions);
            HOperatorSet.SelectShape(connectedRegions, out selectedRegions, "circularity", "and", 0.8, 1);
            HTuple minArea = imgHeight * imgHeight * 0.3 * 0.3;
            HTuple maxArea = imgHeight * imgHeight * 0.9 * 0.9;
            HOperatorSet.SelectShape(selectedRegions, out selectedRegions, "area", "and", minArea, maxArea);

            HOperatorSet.CountObj(selectedRegions, out HTuple selectedRegionNum);
            if (selectedRegionNum != 1)
            {
                return Errortype.SLIDE_CHECK_WAFER_GRAB_ERROR;
            }

            HOperatorSet.GenContourRegionXld(selectedRegions, out contourXLD, "border");
            HOperatorSet.SmoothContoursXld(contourXLD, out contourXLD, 5);
            HOperatorSet.SmallestRectangle2Xld(contourXLD, out HTuple rectCenterRow, out HTuple rectCenterCol, out HTuple rectPhi, out HTuple rectLenW, out HTuple rectLenH);

            double lenDiff = Math.Abs(rectLenW.D - rectLenH.D);
            Point rectCenter = new Point(rectCenterCol.D, rectCenterRow.D);
            Point circleCenter1 = new Point(rectCenter.X - lenDiff * Math.Cos(rectPhi.D), rectCenter.Y + lenDiff * Math.Sin(rectPhi.D));
            Point circleCenter2 = new Point(rectCenter.X + lenDiff * Math.Cos(rectPhi.D), rectCenter.Y - lenDiff * Math.Sin(rectPhi.D));
            Point slidePt1 = new Point(rectCenter.X + rectLenW * Math.Cos(rectPhi.D), rectCenter.Y - rectLenW * Math.Sin(rectPhi.D));
            Point slidePt2 = new Point(slidePt1.X - 2 * lenDiff * Math.Cos(rectPhi.D), slidePt1.Y + 2 * lenDiff * Math.Sin(rectPhi.D));

            waferPixCenter = new List<Point> { circleCenter1, circleCenter2 };
            waferPixRadius = new List<double> { rectLenH.D, rectLenH.D };
            slidePoint = new List<Point> { slidePt1, slidePt2 };

            hImg.Dispose();
            grayImg.Dispose();
            darkRegion.Dispose();
            connectedRegions.Dispose();
            selectedRegions.Dispose();
            contourXLD.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 计算滑片偏离
        /// (使用 环形区域检测方法)
        /// </summary>
        /// <param name="img">相机图像</param>
        /// <param name="minCenter">限制区域的小圆中心坐标</param>
        /// <param name="maxCenter">限制区域的大圆中心坐标</param>
        /// <param name="minRadius">限制区域最小半径</param>
        /// <param name="maxRadius">限制区域最大半径</param>
        /// <param name="alarm">输出是否异常</param>
        /// <param name="slidePoint">输出超出范围的点</param>
        /// <param name="fitPointsNum">输出拟合点数量</param>
        /// <param name="score">输出得分</param>
        /// <param name="imgShow">显示图像</param>
        /// <param name="circleScore">有效边缘的最低分数</param>
        /// <param name="measureThresh">卡尺阈值</param>
        /// <param name="distThresh">拟合圆距离阈值</param>
        /// <returns>SLIDE_CHECK_IAMGE_NULL:输入图像为空/SLIDE_CHECK_WAFER_GRAB_ERROR:边缘抓取失败</returns>
        public Errortype CalcSlideOffGen2(Camera img, Point minCenter, Point maxCenter, double minRadius, double maxRadius, out bool alarm,
            out List<Point> slidePoint, out int fitPointsNum, out double score, out Camera imgShow, double circleScore = 0.98, int measureThresh = 15, int distThresh = 10)
        {
            // 该方法为现场调用函数，直接更新使用新方法
            return CalcSlideOffCaliper(img, minCenter, maxCenter, minRadius, maxRadius, out alarm, out slidePoint, out fitPointsNum, out score, out imgShow, circleScore, measureThresh, distThresh);
        }

        /// <summary>
        /// 计算滑片偏离
        /// (使用 环形区域检测方法)
        /// </summary>
        /// <param name="img">相机图像</param>
        /// <param name="minCenter">限制区域的小圆中心坐标</param>
        /// <param name="maxCenter">限制区域的大圆中心坐标</param>
        /// <param name="minRadius">限制区域最小半径</param>
        /// <param name="maxRadius">限制区域最大半径</param>
        /// <param name="alarm">输出是否异常</param>
        /// <param name="slidePoint">输出超出范围的点</param>
        /// <param name="fitPointsNum">参与拟合的点的数量</param>
        /// <param name="score">拟合结果分数</param>
        /// <param name="imgShow">显示图像</param>
        /// <param name="circleThresh">有效边缘点的最低百分比</param>
        /// <param name="measureThresh">卡尺阈值</param>
        /// <param name="distThresh">拟合圆距离阈值</param>
        /// <returns>SLIDE_CHECK_IAMGE_NULL:输入图像为空/SLIDE_CHECK_WAFER_GRAB_ERROR:边缘抓取失败</returns>
        public Errortype CalcSlideOffCaliper(Camera img, Point minCenter, Point maxCenter, double minRadius, double maxRadius, out bool alarm, out List<Point> slidePoint,
            out int fitPointsNum, out double score, out Camera imgShow, double circleThresh = 0.98, int measureThresh = 15, int distThresh = 10)
        {
            alarm = true;
            slidePoint = new List<Point>();
            fitPointsNum = 0;
            score = 0;
            int days = 0;
            imgShow = null;
            if ((img == null) || (img.Width == 0) || (img.Height == 0))
            {
                return Errortype.SLIDE_CHECK_IAMGE_NULL;
            }

            imgShow = img.Clone();
            HObject hImg = img.GenHObject();

            double measureSigma = 1;
            int numOfMeasures = 360;

            // 建立测量模板
            HOperatorSet.CreateMetrologyModel(out HTuple hv_MetrologyHandle);
            HOperatorSet.SetMetrologyModelImageSize(hv_MetrologyHandle, img.Width, img.Height);

            HOperatorSet.AddMetrologyObjectCircleMeasure(hv_MetrologyHandle, minCenter.Y, minCenter.X, (maxRadius + minRadius) * 0.5,
                (maxRadius - minRadius) * 0.5, 4,
                measureSigma, measureThresh, new HTuple(), new HTuple(), out HTuple hv_MetrologyCircleIndices);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "num_measures", numOfMeasures);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "distance_threshold", distThresh);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "min_score", 0.4);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "num_instances", 1);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_select", "last");
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_transition", "uniform");

            // 执行测量
            HOperatorSet.ApplyMetrologyModel(hImg, hv_MetrologyHandle);

            HOperatorSet.GetMetrologyObjectMeasures(out _, hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", out HTuple measureRow, out HTuple measureCol);
            if (measureRow.Length < 100)
            {
                GenSlideOffImage(img, measureRow, measureCol, new HTuple(), new HTuple(), new HTuple(), new HTuple(), out Camera imgShowZero);
                if (ComAlgo.SaveFlg("SlideCheckAlarm", out days))
                {
                    string path = @"D:\Alg\SlideCheck";
                    string name = "SlideCheckAlarm_" + measureRow.Length + "_" + "ZeroFit" + "_" + "ZeroRate";
                    ComAlgo.LogImage(imgShowZero, path, name, days);
                }

                imgShowZero.Dispose();
                return Errortype.OK;
            }

            //HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "used_edges", "row", out HTuple edgePointsRow);
            //HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "used_edges", "column", out HTuple edgePointsCol);
            HTuple angleArray = new HTuple();
            for (int i = 0; i < measureRow.Length; i++)
            {
                HOperatorSet.AngleLx(measureRow[i], measureCol[i], minCenter.Y, minCenter.X, out HTuple angle);
                angleArray = angleArray.TupleConcat(angle);
            }

            HOperatorSet.ClearMetrologyModel(hv_MetrologyHandle);

            HOperatorSet.TupleSortIndex(angleArray, out HTuple sortIndices);
            HOperatorSet.TupleSelect(measureRow, sortIndices, out HTuple measureRowSorted);
            HOperatorSet.TupleSelect(measureCol, sortIndices, out HTuple measureColSorted);
            HOperatorSet.GenContourPolygonXld(out HObject measureContour, measureRowSorted, measureColSorted);
            HOperatorSet.FitCircleContourXld(measureContour, "geometric", -1, 0, 0, 3, 2, out HTuple centerY, out HTuple centerX, out HTuple r, out HTuple startPhi, out HTuple endPhi, out HTuple pointOrder);
            hImg.Dispose();
            measureContour.Dispose();

            HTuple circleRow = new HTuple();
            HTuple circleCol = new HTuple();
            for (int i = 0; i < measureRowSorted.Length; i++)
            {
                HOperatorSet.DistancePp(measureRowSorted[i], measureColSorted[i], centerY, centerX, out HTuple distance);
                distance = Math.Abs(distance.D - r.D);
                if (distance < distThresh)
                {
                    circleRow = circleRow.TupleConcat(measureRowSorted[i]);
                    circleCol = circleCol.TupleConcat(measureColSorted[i]);
                }
            }

            fitPointsNum = circleRow.Length;
            score = circleRow.Length * 1.0 / numOfMeasures;
            if (score > circleThresh)
            {
                alarm = false;
            }

            for (int i = 0; i < circleRow.Length; i++)
            {
                slidePoint.Add(new Point(circleCol[i].D, circleRow[i].D));
            }

            CalcMeasureFailPoints(measureColSorted, measureRowSorted, centerX, centerY, r, out HTuple measureFailPointsX, out HTuple measureFailPointsY);

            imgShow.Dispose();
            GenSlideOffImage(img, measureRow, measureCol, circleRow, circleCol, measureFailPointsY, measureFailPointsX, out imgShow);

            if (alarm == true && ComAlgo.SaveFlg("SlideCheckAlarm", out days))
            {
                string path = @"D:\Alg\SlideCheck";
                string name = "SlideCheckAlarm_" + measureRow.Length + "_" + circleRow.Length + "_" + score.ToString("f3");
                ComAlgo.LogImage(imgShow, path, name, days);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 生成滑片检测图像
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="measureRows">测量点行</param>
        /// <param name="measureCols">测量点列</param>
        /// <param name="circleRows">拟合点行</param>
        /// <param name="circleCols">拟合点列</param>
        /// <param name="measureFailRows">测量失败点行</param>
        /// <param name="measureFailCols">测量失败点列</param>
        /// <param name="imgOut">输出图像</param>
        /// <returns>OK：成功，其他：失败</returns>
        private Errortype GenSlideOffImage(Camera img, HTuple measureRows, HTuple measureCols, HTuple circleRows, HTuple circleCols,
            HTuple measureFailRows, HTuple measureFailCols, out Camera imgOut)
        {
            HObject imageH = img.GenHObject();
            HObject imageR = imageH.Clone();
            HObject imageG = imageH.Clone();
            HObject imageB = imageH.Clone();

            HOperatorSet.GenRegionPoints(out HObject measureRegionSrc, measureRows, measureCols);
            HOperatorSet.GenRegionPoints(out HObject circleRegionSrc, circleRows, circleCols);
            HOperatorSet.GenRegionPoints(out HObject measureFailRegionSrc, measureFailRows, measureFailCols);

            HOperatorSet.DilationCircle(measureRegionSrc, out HObject measureRegion, 1);
            HOperatorSet.DilationCircle(circleRegionSrc, out HObject circleRegion, 1);
            HOperatorSet.DilationCircle(measureFailRegionSrc, out HObject measureFailRegion, 1);

            HOperatorSet.PaintRegion(measureFailRegion, imageR, out HObject imageAftermeasureFailR, 255, "fill");
            HOperatorSet.PaintRegion(measureFailRegion, imageG, out HObject imageAftermeasureFailG, 0, "fill");
            HOperatorSet.PaintRegion(measureFailRegion, imageB, out HObject imageAftermeasureFailB, 0, "fill");

            HOperatorSet.PaintRegion(measureRegion, imageAftermeasureFailR, out HObject imageAftermeasureR, 255, "fill");
            HOperatorSet.PaintRegion(measureRegion, imageAftermeasureFailG, out HObject imageAfterMeasureG, 255, "fill");
            HOperatorSet.PaintRegion(measureRegion, imageAftermeasureFailB, out HObject imageAfterMeasureB, 0, "fill");

            HOperatorSet.PaintRegion(circleRegion, imageAftermeasureR, out HObject imageAfterCircleR, 0, "fill");
            HOperatorSet.PaintRegion(circleRegion, imageAfterMeasureG, out HObject imageAfterCircleG, 255, "fill");
            HOperatorSet.PaintRegion(circleRegion, imageAfterMeasureB, out HObject imageAfterCircleB, 0, "fill");

            HOperatorSet.Compose3(imageAfterCircleR, imageAfterCircleG, imageAfterCircleB, out HObject imageRgb);
            imgOut = new Camera(imageRgb);

            imageH.Dispose();
            imageRgb.Dispose();
            measureRegion.Dispose();
            circleRegion.Dispose();
            measureFailRegion.Dispose();
            imageAftermeasureFailR.Dispose();
            imageAftermeasureFailG.Dispose();
            imageAftermeasureFailB.Dispose();
            imageAftermeasureR.Dispose();
            imageAfterMeasureG.Dispose();
            imageAfterMeasureB.Dispose();
            imageAfterCircleR.Dispose();
            imageAfterCircleG.Dispose();
            imageAfterCircleB.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 计算量测失败的点，通过圆的一圈理论点和实际点的区别
        /// </summary>
        /// <param name="actualPointsX">实际点X</param>
        /// <param name="actualPointsY">实际点Y</param>
        /// <param name="centerX">中心X</param>
        /// <param name="centerY">中心Y</param>
        /// <param name="r">半径</param>
        /// <param name="measureFailPointsX">返回量测失败的点X</param>
        /// <param name="measureFailPointsY">返回量测失败的点Y</param>
        /// <returns>OK：成功，其他：失败</returns>
        private Errortype CalcMeasureFailPoints(HTuple actualPointsX, HTuple actualPointsY, HTuple centerX, HTuple centerY, HTuple r,
            out HTuple measureFailPointsX, out HTuple measureFailPointsY)
        {
            measureFailPointsX = new HTuple();
            measureFailPointsY = new HTuple();

            if (actualPointsX.Length < 3)
            {
                return Errortype.OK;
            }

            HTuple angleArray = new HTuple();
            for (int i = 0; i < actualPointsX.Length; i++)
            {
                double angle = Math.Atan2(actualPointsY[i] - centerY, actualPointsX[i] - centerX);
                angleArray = angleArray.TupleConcat(angle);
            }

            HOperatorSet.TupleSort(angleArray, out HTuple angleSorted);

            double radInterval = 1.0 / 180.0 * Math.PI;
            for (int i = -179; i < 180; i++)
            {
                double rad = i / 180.0 * Math.PI;
                if (angleSorted[0] - rad > radInterval * 1.1)
                {
                    double x = centerX.D + r * Math.Cos(rad);
                    double y = centerY.D + r * Math.Sin(rad);
                    measureFailPointsX = measureFailPointsX.TupleConcat(x);
                    measureFailPointsY = measureFailPointsY.TupleConcat(y);
                }
                else
                {
                    break;
                }
            }

            for (int i = 180; i > -179; i--)
            {
                double rad = i / 180.0 * Math.PI;
                if (rad - angleSorted[angleSorted.Length - 1] > radInterval * 1.1)
                {
                    double x = centerX.D + r * Math.Cos(rad);
                    double y = centerY.D + r * Math.Sin(rad);
                    measureFailPointsX = measureFailPointsX.TupleConcat(x);
                    measureFailPointsY = measureFailPointsY.TupleConcat(y);
                }
                else
                {
                    break;
                }
            }

            for (int i = 0; i < angleSorted.Length - 1; i++)
            {
                if (angleSorted[i + 1] - angleSorted[i] > radInterval * 1.1)
                {
                    for (double j = angleSorted[i] + radInterval; j < angleSorted[i + 1] - radInterval; j += radInterval)
                    {
                        double rad = j;
                        double x = centerX.D + r * Math.Cos(rad);
                        double y = centerY.D + r * Math.Sin(rad);
                        measureFailPointsX = measureFailPointsX.TupleConcat(x);
                        measureFailPointsY = measureFailPointsY.TupleConcat(y);
                    }
                }
            }

            return Errortype.OK;
        }
    }
}