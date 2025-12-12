using DataStruct;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Drawing.Design;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.IRDetection
{
    /// <summary>
    /// IR检测管理类
    /// </summary>
    public static class IRDetectionManager
    {
        private static CalibCoord _calibCoord; // 标定示例
        private static string _calibName = "IRDetectionCalib"; //标定名称

        /// <summary>
        /// 标定初始化
        /// </summary>
        /// <returns>OK为成功，其他为失败</returns>
        public static Errortype Init()
        {
            _calibCoord = new CalibCoord(_calibName);
            return Errortype.OK;
        }

        /// <summary>
        /// 标定释放
        /// </summary>
        /// <returns>OK为成功，其他为失败</returns>
        public static Errortype Release()
        {
            _calibCoord = null;
            return Errortype.OK;
        }

        private static Errortype GetCalibImgCircle(Camera img, out List<Point> pixel)
        {
            pixel = new List<Point>();

            HObject hImg = img.GenHObject();
            HOperatorSet.MeanImage(hImg, out HObject imageMean, 50, 50);
            HOperatorSet.SubImage(hImg, imageMean, out HObject imageSub, 50, 0);

            HOperatorSet.BinaryThreshold(imageSub, out HObject region, "max_separability", "dark", out HTuple usedThreshold);
            HOperatorSet.OpeningCircle(region, out HObject regionOpening, 3.5);
            HOperatorSet.Connection(regionOpening, out HObject connectedRegions);
            HOperatorSet.SelectShape(connectedRegions, out HObject selectedRegions, "circularity", "and", 0.8, 1);

            HOperatorSet.AreaCenter(selectedRegions, out HTuple areas, out HTuple rows, out HTuple cols);
            HOperatorSet.TupleMedian(areas, out HTuple medianArea);
            double r = Math.Sqrt(medianArea.D / Math.PI * 2);
            r = r - 5;

            List<Point> centerList = new List<Point>();
            List<double> rList = new List<double>();

            for (int i = 0; i < rows.DArr.Length; i++)
            {
                centerList.Add(new Point(cols.DArr[i], rows.DArr[i]));
                rList.Add(r);
            }

            CaliperParams caliperParams = new CaliperParams();
            caliperParams.MeasureThreshold = 10;
            caliperParams.MeasureLength1 = 10;
            caliperParams.MeasureLength2 = 1;
            caliperParams.MinScore = 0.8;
            Errortype ret = Caliper.CaliperCircle.CircleExtraction(img, centerList, rList, caliperParams, out pixel, out List<double> rListOut);

            imageMean.Dispose();
            imageSub.Dispose();
            region.Dispose();
            regionOpening.Dispose();
            connectedRegions.Dispose();
            selectedRegions.Dispose();
            usedThreshold.UnPinTuple();

            return ret;
        }

        /// <summary>
        /// 标定
        /// </summary>
        /// <param name="img">输入标定板图像</param>
        /// <param name="markIntervalDist">标定板圆间隔mm</param>
        /// <param name="pixel">标定板圆像素坐标</param>
        /// <param name="real">标定板圆真值坐标</param>
        /// <returns>OK为成功，其他为失败</returns>
        public static Errortype Calib(Camera img, double markIntervalDist, out List<Point> pixel, out List<Point> real)
        {
            pixel = new List<Point>();
            real = new List<Point>();
            if (img is null || img.Height < 1 || img.Width < 1)
            {
                return Errortype.IRDETECTION_CALIB_IMG_NULL;
            }

            if (_calibCoord is null)
            {
                return Errortype.IRDETECTION_CALIB_NULL;
            }

            Errortype ret = GetCalibImgCircle(img, out List<Point> centerPixel);

            List<double> xList = new List<double>();
            List<double> yList = new List<double>();
            for (int i = 0; i < centerPixel.Count; i++)
            {
                xList.Add(centerPixel[i].X);
                yList.Add(centerPixel[i].Y);
            }

            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.SortRowColumn(centerPixel, out pixel, out long[] sortOrder, out long[] rowIdAfterSort, out long[] colIdAfterSort, 10);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            // 根据输入真值和行列间隔计算所有点真值
            int selectedMarkId = 0;
            Point selectedMarkAtReal = new Point(0, 0);
            double markRowInterval = markIntervalDist;
            double markColInterval = markIntervalDist;

            double selectedRealY = pixel[selectedMarkId].Y;
            double selectedRealX = pixel[selectedMarkId].X;
            double rowBias = selectedMarkAtReal.Y - (rowIdAfterSort[selectedMarkId] * markRowInterval);
            double colBias = selectedMarkAtReal.X - (colIdAfterSort[selectedMarkId] * markColInterval);

            for (int index = 0; index < pixel.Count; index++)
            {
                double realValueX = colIdAfterSort[index] * markColInterval + colBias;
                double realValueY = rowIdAfterSort[index] * markRowInterval + rowBias;
                real.Add(new Point(realValueX, realValueY));
            }

            // 标定
            ret = _calibCoord.CalibDo(pixel, real, TransType.AffineKDTrans);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// 保存标定文件
        /// </summary>
        /// <param name="path">标定文件路径</param>
        /// <returns>OK为成功，其他为失败</returns>
        public static Errortype Save(string path)
        {
            if (_calibCoord is null)
            {
                return Errortype.IRDETECTION_CALIB_NULL;
            }

            Errortype ret = _calibCoord.Save(path);
            return ret;
        }

        /// <summary>
        /// 加载标定文件
        /// </summary>
        /// <param name="path">标定文件路径</param>
        /// <returns>OK为成功，其他为失败</returns>
        public static Errortype Load(string path)
        {
            if (_calibCoord is null)
            {
                return Errortype.IRDETECTION_CALIB_NULL;
            }

            Errortype ret = _calibCoord.Load(path);
            return ret;
        }

        /// <summary>
        /// 像素坐标转真值坐标
        /// </summary>
        /// <param name="pixel">像素坐标</param>
        /// <param name="real">真值坐标</param>
        /// <returns>OK为成功，其他为失败</returns>
        public static Errortype GetRealByPixel(Point pixel, out Point real)
        {
            real = new Point(0, 0);
            if (_calibCoord is null)
            {
                return Errortype.IRDETECTION_CALIB_NULL;
            }

            Errortype ret = _calibCoord.Src2Dst(pixel, out real, out List<Point> error);
            return ret;
        }

        /// <summary>
        /// 计算切线
        /// </summary>
        /// <param name="startLine">起始线段</param>
        /// <param name="hImg">原始图像</param>
        /// <param name="endLine">切线线段</param>
        /// <returns>OK为成功，其他为失败</returns>
        private static Errortype CalcTangentLine(LineSeg startLine, HObject hImg, out LineSeg endLine)
        {
            endLine = new LineSeg();

            HOperatorSet.EdgesImage(hImg, out HObject imaAmp, out HObject imaDir, "canny", 1, "nms", 10, 20);
            HOperatorSet.Threshold(imaAmp, out HObject edgeRegion, 0, 255);

            Point startLineCenter = (startLine.Start + startLine.End) / 2;

            HOperatorSet.GetImageSize(hImg, out HTuple width, out HTuple height);
            Point imgCenter = new Point(width / 2, height / 2);
            ComAlgo.CalcRotatePoint(startLine.Start, 90, startLineCenter, out Point startRotated);
            double startDist = ComAlgo.Dist(startRotated, imgCenter);
            ComAlgo.CalcRotatePoint(startLine.End, 90, startLineCenter, out Point endRotated);
            double endDist = ComAlgo.Dist(endRotated, imgCenter);

            Point offsetEnd = startDist < endDist ? startRotated : endRotated;

            Point offset = (offsetEnd - startLineCenter) / 2;

            HOperatorSet.GenRegionLine(out HObject endLineRegion, startLine.Start_Y, startLine.Start_X, startLine.End_Y, startLine.End_X);
            bool flg = true;
            double ratio = 1;
            double ratioStep = 0.1;
            double maxRatio = 4;
            double minRatio = 1;
            bool getMinRatio = false;
            while (flg)
            {
                HOperatorSet.MoveRegion(endLineRegion, out HObject regionMoved, offset.Y * ratio, offset.X * ratio);
                HOperatorSet.Intersection(regionMoved, edgeRegion, out HObject regionIntersection);
                HOperatorSet.AreaCenter(regionIntersection, out HTuple intersectionArea, out HTuple intersectionRow, out HTuple intersectionCol);

                regionMoved.Dispose();
                regionIntersection.Dispose();
                intersectionRow.UnPinTuple();
                intersectionCol.UnPinTuple();
                double areaDouble = intersectionArea.D;
                intersectionArea.UnPinTuple();

                if (areaDouble > 2)
                {
                    minRatio = ratio;
                    ratio += ratioStep;
                    getMinRatio = true;
                    continue;
                }

                if (areaDouble == 0 && getMinRatio)
                {
                    maxRatio = ratio;
                    break;
                }

                ratio += ratioStep;
            }

            while (flg)
            {
                HOperatorSet.MoveRegion(endLineRegion, out HObject regionMoved, offset.Y * ratio, offset.X * ratio);
                HOperatorSet.Intersection(regionMoved, edgeRegion, out HObject regionIntersection);
                HOperatorSet.AreaCenter(regionIntersection, out HTuple intersectionArea, out HTuple intersectionRow, out HTuple intersectionCol);

                regionMoved.Dispose();
                regionIntersection.Dispose();
                intersectionRow.UnPinTuple();
                intersectionCol.UnPinTuple();
                double areaDouble = intersectionArea.D;
                intersectionArea.UnPinTuple();

                if (areaDouble == 0)
                {
                    maxRatio = ratio;
                    ratio = (minRatio + maxRatio) / 2;
                    if (maxRatio - minRatio < 1e-6)
                    {
                        break;
                    }

                    continue;
                }

                if (areaDouble > 2)
                {
                    minRatio = ratio;
                    ratio = (minRatio + maxRatio) / 2;
                    if (maxRatio - minRatio < 1e-6)
                    {
                        break;
                    }

                    continue;
                }

                break;
            }

            endLineRegion.Dispose();

            endLine.Start_X = startLine.Start_X + offset.X * ratio;
            endLine.Start_Y = startLine.Start_Y + offset.Y * ratio;

            endLine.End_X = startLine.End_X + offset.X * ratio;
            endLine.End_Y = startLine.End_Y + offset.Y * ratio;

            return Errortype.OK;
        }

        /// <summary>
        /// 计算切线距离
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="dist">返回距离</param>
        /// <param name="bladeLine">返回刀片线段</param>
        /// <param name="bubbleLine">返回气泡线段</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype CalcTangentLineDist(Camera img, out double dist, out LineSeg bladeLine, out LineSeg bubbleLine)
        {
            dist = 0;
            bladeLine = new LineSeg();
            bubbleLine = new LineSeg();
            if (img is null || img.Height < 1 || img.Width < 1)
            {
                return Errortype.IRDETECTION_CALCTANGENTLINEDIST_IMG_NULL;
            }

            HObject hImg = img.GenHObject();
            HOperatorSet.BinaryThreshold(hImg, out HObject reduceRegion, "max_separability", "light", out HTuple usedThreshold);
            usedThreshold.UnPinTuple();

            //HOperatorSet.FillUp(reduceRegion, out HObject regionFillUp);
            HOperatorSet.ErosionCircle(reduceRegion, out HObject regionErosion, 3.5);
            HOperatorSet.Connection(regionErosion, out HObject connectedRegions);
            HOperatorSet.SelectShape(connectedRegions, out HObject selectedRegions, "area", "and", 4000, 10000);

            if (selectedRegions.CountObj() == 0)
            {
                return Errortype.IRDETECTION_SELECTEDREGION_NULL;
            }

            HObject selectedRegionsRect = selectedRegions.Clone();
            if (selectedRegions.CountObj() > 1)
            {
                selectedRegionsRect.Dispose();
                HOperatorSet.Rectangularity(selectedRegions, out HTuple rectangularity);
                HOperatorSet.TupleMin(rectangularity, out HTuple min);
                HOperatorSet.TupleFind(rectangularity, min, out HTuple indices);
                HOperatorSet.SelectObj(selectedRegions, out selectedRegionsRect, indices + 1);
            }

            HOperatorSet.SmallestRectangle2(selectedRegionsRect, out HTuple row, out HTuple col, out HTuple phi, out HTuple length1, out HTuple length2);

            //regionFillUp.Dispose();
            regionErosion.Dispose();
            connectedRegions.Dispose();
            selectedRegions.Dispose();
            selectedRegionsRect.Dispose();

            Rectangle2 smallestRectangle2 = new Rectangle2(col, row, length1 * 2, length2 * 7, phi);

            List<Point> corners = smallestRectangle2.GetCornerPoints();

            double min1 = double.MaxValue;
            double min2 = double.MaxValue;
            int min1Index = 0;
            int min2Index = 0;
            for (int i = 0; i < corners.Count; i++)
            {
                double distCorner = ComAlgo.Dist(new Point(img.Width / 2, img.Height / 2), corners[i]);

                if (distCorner < min1)
                {
                    min2 = min1;
                    min2Index = min1Index;
                    min1 = distCorner;
                    min1Index = i;
                    continue;
                }

                if (distCorner < min2)
                {
                    min2 = distCorner;
                    min2Index = i;
                    continue;
                }
            }

            CaliperParams caliperParams = new CaliperParams();
            caliperParams.MeasureThreshold = 10;
            caliperParams.MeasureLength1 = 15;
            Errortype ret = Caliper.CaliperLine.LineExtraction(img, new LineSeg(corners[min1Index], corners[min2Index]), caliperParams, out bladeLine, out double[] error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalcTangentLine(bladeLine, hImg, out bubbleLine);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = GetRealByPixel((bladeLine.Start + bladeLine.End) / 2, out Point bladeLineCenterReal);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = GetRealByPixel((bubbleLine.Start + bubbleLine.End) / 2, out Point bubbleLineCenterReal);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            dist = ComAlgo.Dist(bladeLineCenterReal, bubbleLineCenterReal);

            return Errortype.OK;
        }
    }
}
