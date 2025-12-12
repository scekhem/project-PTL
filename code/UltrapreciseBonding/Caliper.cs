using DataStruct;
using System;
using HalconDotNet;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using IniFileHelper;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UltrapreciseBonding.MarkLocation;

namespace UltrapreciseBonding.Caliper
{
    /// <summary>
    /// 卡尺提取直线
    /// </summary>
    public static class CaliperLine
    {
        /// <summary>
        /// 判断线段使用卡尺时候的极性
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="line">线段</param>
        /// <param name="polarityValue">极性值</param>
        /// <param name="judgementWidth">判断的宽度，建议使用卡尺提取的宽度</param>
        /// <param name="regionIgnore">忽略区域</param>
        /// <param name="sampleNum">采样的数量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcLinePolarityOld(Camera img, LineSeg line, out double polarityValue, double judgementWidth = 15, HObject regionIgnore = null, int sampleNum = 10)
        {
            double noiseLevel = 0.3;
            int toleranceWidth = 10;
            polarityValue = 0.0;
            int sample_num = sampleNum;
            Point sample_step = new Point(0, 0);
            Point sample_vector = new Point(0, 0);

            if (regionIgnore == null || !regionIgnore.IsInitialized())
            {
                HOperatorSet.GenEmptyRegion(out regionIgnore);
            }

            HObject hImage = img.GenHObject();

            //HOperatorSet.BilateralFilter(hImage, hImage, out HObject filterImg,3,20,new HTuple(),new HTuple());
            //hImage.Dispose();
            //hImage = filterImg;

            // 生成采样线段
            sample_step.X = (line.End_X - line.Start_X) / (sample_num + 1);
            sample_step.Y = (line.End_Y - line.Start_Y) / (sample_num + 1);
            double sample_step_vector_scale = judgementWidth / Math.Sqrt(Math.Pow(sample_step.X, 2) + Math.Pow(sample_step.Y, 2));
            sample_vector.X = -sample_step.Y * sample_step_vector_scale * 0.5;
            sample_vector.Y = sample_step.X * sample_step_vector_scale * 0.5;
            double vectorLenX = Math.Abs(sample_vector.X);
            double vectorLenY = Math.Abs(sample_vector.Y);
            HTuple indices = new HTuple();
            HTuple derivateSumValues = new HTuple();
            for (int sample_index = 1; sample_index <= sample_num; sample_index++)
            {
                Point sample_middle = new Point(line.Start_X, line.Start_Y) + (sample_step * sample_index);
                Point samplePointStart = sample_middle - sample_vector;
                Point samplePointEnd = sample_middle + sample_vector;
                HOperatorSet.GenRegionLine(out HObject sampleRegionLine, samplePointStart.Y, samplePointStart.X, samplePointEnd.Y, samplePointEnd.X);
                HOperatorSet.Difference(sampleRegionLine, regionIgnore, out sampleRegionLine);
                HOperatorSet.GetDomain(hImage, out HObject imageDomain);
                HOperatorSet.Intersection(sampleRegionLine, imageDomain, out HObject sampleRegionLineInImg);
                HOperatorSet.GetRegionPoints(sampleRegionLineInImg, out HTuple pointRows, out HTuple pointCols);
                HOperatorSet.GetGrayval(hImage, pointRows, pointCols, out HTuple grayValue);
                sampleRegionLine.Dispose();
                sampleRegionLineInImg.Dispose();
                if (grayValue.Length < 1)
                {
                    return Errortype.CALIPER_LINE_POLARITY_POINT_OUT_OF_IMAGE;
                }

                // 判断一下采样是X方向为主还是Y方向为主
                if (vectorLenX < vectorLenY)
                {
                    // 按采样方向排序
                    if (sample_vector.Y > 0)
                    {
                        HOperatorSet.TupleSortIndex(pointRows, out indices);
                    }
                    else
                    {
                        HOperatorSet.TupleSortIndex(-pointRows, out indices);
                    }
                }
                else
                {
                    if (sample_vector.X > 0)
                    {
                        HOperatorSet.TupleSortIndex(pointCols, out indices);
                    }
                    else
                    {
                        HOperatorSet.TupleSortIndex(-pointCols, out indices);
                    }
                }

                HOperatorSet.TupleSelect(grayValue, indices, out grayValue);    // 整理排序后的灰度值

                // 将灰度值归一化
                HOperatorSet.TupleReal(grayValue, out grayValue);
                HOperatorSet.TupleMin(grayValue, out HTuple grayMin);
                HOperatorSet.TupleMax(grayValue, out HTuple grayMax);
                grayValue = grayValue - grayMin;
                if (grayMax - grayMin > 0)
                {
                    grayValue = grayValue / (grayMax - grayMin);
                }

                HOperatorSet.CreateFunct1dArray(grayValue, out HTuple functGray);
                HOperatorSet.DerivateFunct1d(functGray, "first", out HTuple functdDerivate);
                HOperatorSet.Funct1dToPairs(functdDerivate, out HTuple xValues, out HTuple derivateValues);

                // HOperatorSet.TupleSum(derivateValues, out HTuple derivateSum);  // 计算该采样处的梯度和
                // HOperatorSet.TupleConcat(derivateSumValues, derivateSum, out derivateSumValues); // 整理整条线上各采样点位置的的梯度和
                HOperatorSet.TupleRound(derivateValues / noiseLevel, out HTuple roundValues);   // 归一化，去除噪声的梯度
                roundValues = roundValues * noiseLevel;
                HOperatorSet.TupleSum(roundValues, out HTuple derivateSum);
                HOperatorSet.TupleAbs(derivateSum, out HTuple absSumValue);

                // 梯度和小于0.5时，即同时出现灰度上升和下降沿，进一步判断上升下降沿是否满足最小宽度
                if (absSumValue < 0.5)
                {
                    HOperatorSet.TupleMin(roundValues, out HTuple minValue);        // 寻找下降沿位置
                    HOperatorSet.TupleFind(roundValues, minValue, out HTuple minId);
                    HOperatorSet.TupleMax(roundValues, out HTuple maxValue);        // 寻找上升沿位置
                    HOperatorSet.TupleFind(roundValues, maxValue, out HTuple maxId);

                    //if (minId.Length > 1) roundValues[minId[1].I] = 0;
                    //if (maxId.Length > 1) roundValues[maxId[1].I] = 0;
                    minId = minId[0];
                    maxId = maxId[0];

                    // 统计从靠近采样中心的变化沿到最小宽度位置的梯度和，如果在最小宽度之内又出现另一个梯度变化，则梯度和将小于0.5
                    HOperatorSet.TupleConcat(minId - roundValues.Length / 2, maxId - roundValues.Length / 2, out HTuple distToJudgeCenter);
                    HOperatorSet.TupleAbs(distToJudgeCenter, out distToJudgeCenter);
                    HOperatorSet.TupleSortIndex(distToJudgeCenter, out HTuple minDistId);
                    HTuple subSampleCenter = new HTuple(minId, maxId)[minDistId[0].I];

                    HOperatorSet.TupleMax2(0, subSampleCenter - toleranceWidth, out HTuple subSampleBegin);
                    HOperatorSet.TupleMin2(roundValues.Length - 1, subSampleCenter + toleranceWidth, out HTuple subSampleEnd);
                    HOperatorSet.TupleInt(subSampleBegin, out subSampleBegin);
                    HOperatorSet.TupleInt(subSampleEnd, out subSampleEnd);

                    // 采样线段超出图像边缘会导致采样end位置超出roundValues长度
                    if (subSampleEnd < roundValues.Length)
                    {
                        HOperatorSet.TupleSelectRange(roundValues, subSampleBegin, subSampleEnd, out HTuple subSampleValues);
                        HOperatorSet.TupleSum(subSampleValues, out derivateSum);
                        HOperatorSet.TupleConcat(derivateSumValues, derivateSum, out derivateSumValues);
                    }
                }
                else
                {
                    HOperatorSet.TupleConcat(derivateSumValues, derivateSum, out derivateSumValues);
                }
            }

            HOperatorSet.TupleMean(derivateSumValues, out HTuple derivateSumMean);
            polarityValue = derivateSumMean.D;

            //filterImg.Dispose();
            hImage.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 判断线段使用卡尺时候的极性
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="line">线段</param>
        /// <param name="polarityValue">极性值</param>
        /// <param name="judgementWidth">判断的宽度，建议使用卡尺提取的宽度</param>
        /// <param name="regionIgnore">忽略区域</param>
        /// <param name="sampleNum">采样的数量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcLinePolarityOld2(Camera img, LineSeg line, out double polarityValue, double judgementWidth = 15, HObject regionIgnore = null, int sampleNum = 10)
        {
            double noiseLevel = 0.15;
            int toleranceWidth = 10;
            polarityValue = 0.0;
            int sample_num = sampleNum;
            Point sample_step = new Point(0, 0);
            Point sample_vector = new Point(0, 0);

            if (regionIgnore == null || !regionIgnore.IsInitialized())
            {
                HOperatorSet.GenEmptyRegion(out regionIgnore);
            }

            HObject hImage = img.GenHObject();

            // 生成采样线段
            sample_step.X = (line.End_X - line.Start_X) / (sample_num + 1);
            sample_step.Y = (line.End_Y - line.Start_Y) / (sample_num + 1);
            double sample_step_vector_scale = judgementWidth / Math.Sqrt(Math.Pow(sample_step.X, 2) + Math.Pow(sample_step.Y, 2));
            sample_vector.X = -sample_step.Y * sample_step_vector_scale * 0.5;
            sample_vector.Y = sample_step.X * sample_step_vector_scale * 0.5;
            double vectorLenX = Math.Abs(sample_vector.X);
            double vectorLenY = Math.Abs(sample_vector.Y);
            HTuple indices = new HTuple();
            HTuple derivateSumValues = new HTuple();
            for (int sample_index = 1; sample_index <= sample_num; sample_index++)
            {
                Point sample_middle = new Point(line.Start_X, line.Start_Y) + (sample_step * sample_index);
                Point samplePointStart = sample_middle - sample_vector;
                Point samplePointEnd = sample_middle + sample_vector;
                HOperatorSet.GenRegionLine(out HObject sampleRegionLine, samplePointStart.Y, samplePointStart.X, samplePointEnd.Y, samplePointEnd.X);
                HOperatorSet.Difference(sampleRegionLine, regionIgnore, out sampleRegionLine);
                HOperatorSet.GetDomain(hImage, out HObject imageDomain);
                HOperatorSet.Intersection(sampleRegionLine, imageDomain, out HObject sampleRegionLineInImg);
                HOperatorSet.GetRegionPoints(sampleRegionLineInImg, out HTuple pointRows, out HTuple pointCols);
                HOperatorSet.GetGrayval(hImage, pointRows, pointCols, out HTuple grayValue);
                sampleRegionLine.Dispose();
                sampleRegionLineInImg.Dispose();
                if (grayValue.Length < 1)
                {
                    return Errortype.CALIPER_LINE_POLARITY_POINT_OUT_OF_IMAGE;
                }

                // 判断一下采样是X方向为主还是Y方向为主
                if (vectorLenX < vectorLenY)
                {
                    // 按采样方向排序
                    if (sample_vector.Y > 0)
                    {
                        HOperatorSet.TupleSortIndex(pointRows, out indices);
                    }
                    else
                    {
                        HOperatorSet.TupleSortIndex(-pointRows, out indices);
                    }
                }
                else
                {
                    if (sample_vector.X > 0)
                    {
                        HOperatorSet.TupleSortIndex(pointCols, out indices);
                    }
                    else
                    {
                        HOperatorSet.TupleSortIndex(-pointCols, out indices);
                    }
                }

                HOperatorSet.TupleSelect(grayValue, indices, out grayValue);    // 整理排序后的灰度值

                // 将灰度值归一化
                HOperatorSet.TupleReal(grayValue, out grayValue);
                HOperatorSet.TupleMin(grayValue, out HTuple grayMin);
                HOperatorSet.TupleMax(grayValue, out HTuple grayMax);
                grayValue = grayValue - grayMin;
                if (grayMax - grayMin > 0)
                {
                    grayValue = grayValue / (grayMax - grayMin);
                }

                HOperatorSet.CreateFunct1dArray(grayValue, out HTuple functGray);
                HOperatorSet.DerivateFunct1d(functGray, "first", out HTuple functdDerivate);
                HOperatorSet.Funct1dToPairs(functdDerivate, out HTuple xValues, out HTuple derivateValues);

                //HOperatorSet.TupleSum(derivateValues, out HTuple derivateSum);  // 计算该采样处的梯度和
                //HOperatorSet.TupleConcat(derivateSumValues, derivateSum, out derivateSumValues); // 整理整条线上各采样点位置的的梯度和
                HOperatorSet.TupleRound(derivateValues / noiseLevel, out HTuple roundValues);   // 归一化，去除噪声的梯度
                roundValues = roundValues * noiseLevel;
                HOperatorSet.TupleNotEqualElem(roundValues, 0.0, out HTuple nequal);
                HOperatorSet.TupleSelectMask(grayValue, nequal, out HTuple selected);
                HOperatorSet.TupleInverse(selected, out HTuple inverted);
                HTuple grayDiff = selected - inverted;
                HTuple grayDiffAbs = grayDiff.TupleAbs();
                HTuple grayDiffAbsMean = grayDiffAbs.TupleMean();

                //HOperatorSet.TupleSum(roundValues, out HTuple derivateSum);
                //HOperatorSet.TupleAbs(derivateSum, out HTuple absSumValue);
                // 梯度和小于0.5时，即同时出现灰度上升和下降沿，进一步判断上升下降沿是否满足最小宽度
                if (grayDiffAbsMean < noiseLevel)
                {
                    HOperatorSet.TupleMin(roundValues, out HTuple minValue);        // 寻找下降沿位置
                    HOperatorSet.TupleFind(roundValues, minValue, out HTuple minId);
                    HOperatorSet.TupleMax(roundValues, out HTuple maxValue);        // 寻找上升沿位置
                    HOperatorSet.TupleFind(roundValues, maxValue, out HTuple maxId);

                    //if (minId.Length > 1) roundValues[minId[1].I] = 0;
                    //if (maxId.Length > 1) roundValues[maxId[1].I] = 0;
                    minId = minId[0];
                    maxId = maxId[0];

                    // 统计从靠近采样中心的变化沿到最小宽度位置的梯度和，如果在最小宽度之内又出现另一个梯度变化，则梯度和将小于0.5
                    HOperatorSet.TupleConcat(minId - roundValues.Length / 2, maxId - roundValues.Length / 2, out HTuple distToJudgeCenter);
                    HOperatorSet.TupleAbs(distToJudgeCenter, out distToJudgeCenter);
                    HOperatorSet.TupleSortIndex(distToJudgeCenter, out HTuple minDistId);
                    HTuple subSampleCenter = new HTuple(minId, maxId)[minDistId[0].I];

                    HOperatorSet.TupleMax2(0, subSampleCenter - toleranceWidth, out HTuple subSampleBegin);
                    HOperatorSet.TupleMin2(roundValues.Length - 1, subSampleCenter + toleranceWidth, out HTuple subSampleEnd);
                    HOperatorSet.TupleInt(subSampleBegin, out subSampleBegin);
                    HOperatorSet.TupleInt(subSampleEnd, out subSampleEnd);

                    // 采样线段超出图像边缘会导致采样end位置超出roundValues长度
                    if (subSampleEnd < roundValues.Length)
                    {
                        HOperatorSet.TupleSelectRange(roundValues, subSampleBegin, subSampleEnd, out HTuple subSampleValues);
                        HOperatorSet.TupleSum(subSampleValues, out grayDiffAbsMean);
                        HOperatorSet.TupleConcat(derivateSumValues, grayDiffAbsMean, out derivateSumValues);
                    }
                }
                else if (grayDiff[0] > 0)
                {
                    HOperatorSet.TupleConcat(derivateSumValues, -1.0, out derivateSumValues);
                }
                else
                {
                    HOperatorSet.TupleConcat(derivateSumValues, 1.0, out derivateSumValues);
                }
            }

            HOperatorSet.TupleMean(derivateSumValues, out HTuple derivateSumMean);
            polarityValue = derivateSumMean.D;
            return Errortype.OK;
        }

        /// <summary>
        /// 判断线段使用卡尺时候的极性
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="line">线段</param>
        /// <param name="polarityValue">极性值</param>
        /// <param name="judgementWidth">判断的宽度，建议使用卡尺提取的宽度</param>
        /// <param name="regionIgnore">忽略区域</param>
        /// <param name="sampleNum">采样的数量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcLinePolarity(Camera img, LineSeg line, out double polarityValue, double judgementWidth = 15, HObject regionIgnore = null, int sampleNum = 10)
        {
            polarityValue = 0.0;
            int sample_num = sampleNum;
            Point sample_step = new Point(0, 0);
            Point sample_vector = new Point(0, 0);

            if (regionIgnore == null || !regionIgnore.IsInitialized())
            {
                HOperatorSet.GenEmptyRegion(out regionIgnore);
            }

            HObject hImage = img.GenHObject();

            // 生成采样线段
            sample_step.X = (line.End_X - line.Start_X) / (sample_num + 1);
            sample_step.Y = (line.End_Y - line.Start_Y) / (sample_num + 1);
            double sampleStepVectorScale = 2.0 / Math.Sqrt(Math.Pow(sample_step.X, 2) + Math.Pow(sample_step.Y, 2));
            sample_vector.X = -sample_step.Y * sampleStepVectorScale * 0.5;
            sample_vector.Y = sample_step.X * sampleStepVectorScale * 0.5;
            double vectorLenX = Math.Abs(sample_vector.X);
            double vectorLenY = Math.Abs(sample_vector.Y);

            //生成采样点矩阵
            HOperatorSet.TupleGenSequence(1.0, sampleNum, 1.0, out HTuple sampleStepId);
            HOperatorSet.TupleGenSequence(-judgementWidth / 2, judgementWidth / 2, 1.0, out HTuple judgeStepId);

            HTuple sampleBaseRow = sampleStepId * sample_step.Y + line.Start_Y;
            HTuple sampleBaseCol = sampleStepId * sample_step.X + line.Start_X;
            HOperatorSet.CreateMatrix(sampleNum, 1, sampleBaseRow, out HTuple matrixSampleBaseRow);
            HOperatorSet.CreateMatrix(sampleNum, 1, sampleBaseCol, out HTuple matrixSampleBaseCol);
            HOperatorSet.RepeatMatrix(matrixSampleBaseRow, 1, judgeStepId.Length, out matrixSampleBaseRow);
            HOperatorSet.RepeatMatrix(matrixSampleBaseCol, 1, judgeStepId.Length, out matrixSampleBaseCol);

            HTuple sampleVectorRow = judgeStepId * sample_vector.Y;
            HTuple sampleVectorCol = judgeStepId * sample_vector.X;
            HOperatorSet.CreateMatrix(1, judgeStepId.Length, sampleVectorRow, out HTuple matrixSampleVectorRow);
            HOperatorSet.CreateMatrix(1, judgeStepId.Length, sampleVectorCol, out HTuple matrixSampleVectorCol);
            HOperatorSet.RepeatMatrix(matrixSampleVectorRow, sampleNum, 1, out matrixSampleVectorRow);
            HOperatorSet.RepeatMatrix(matrixSampleVectorCol, sampleNum, 1, out matrixSampleVectorCol);
            HOperatorSet.AddMatrix(matrixSampleBaseRow, matrixSampleVectorRow, out HTuple matrixSampleRow);
            HOperatorSet.AddMatrix(matrixSampleBaseCol, matrixSampleVectorCol, out HTuple matrixSampleCol);

            // 获得采样点位
            HOperatorSet.GetFullMatrix(matrixSampleRow, out HTuple sampleRowValues);
            HOperatorSet.GetFullMatrix(matrixSampleCol, out HTuple sampleColValues);

            // 处理mask和越界采样点
            HOperatorSet.GenEmptyObj(out HObject hImageMasked);
            HOperatorSet.ConvertImageType(hImage, out HObject intImage, "int2");
            HOperatorSet.AreaCenter(regionIgnore, out HTuple area, out _, out _);
            if (area.Length > 0)
            {
                HOperatorSet.PaintRegion(regionIgnore, intImage, out hImageMasked, -1, "fill");
            }
            else
            {
                hImageMasked = intImage.Clone();
            }

            //ComAlgo.ShowHimageGray(intImage);
            HOperatorSet.GetRegionPoints(hImageMasked, out HTuple hImageRows, out HTuple hImageCols);
            HOperatorSet.GetGrayval(hImageMasked, hImageRows, hImageCols, out HTuple maskedImgValue);
            HOperatorSet.GenImageConst(out HObject imgBlack, "int2", img.Width + judgementWidth + judgementWidth, img.Height + judgementWidth + judgementWidth);
            HOperatorSet.PaintRegion(imgBlack, imgBlack, out HObject imgMaskedExtend, -1, "fill");
            HOperatorSet.GenRectangle1(out HObject paintArea, 0 + judgementWidth, 0 + judgementWidth,
                img.Height - 1 + judgementWidth, img.Width - 1 + judgementWidth);
            HOperatorSet.GetRegionPoints(paintArea, out HTuple paintRows, out HTuple paintCols);
            HOperatorSet.SetGrayval(imgMaskedExtend, paintRows, paintCols, maskedImgValue);

            //灰度采样
            sampleRowValues = sampleRowValues + judgementWidth;
            sampleColValues = sampleColValues + judgementWidth;
            HOperatorSet.GetGrayvalInterpolated(imgMaskedExtend, sampleRowValues, sampleColValues, "bilinear", out HTuple sampleGrayval);

            //HOperatorSet.GetGrayval(imgMaskedExtend, sampleRowValues, sampleColValues, out HTuple sampleValid);
            //HOperatorSet.TupleGreaterElem(sampleValid, 0, out HTuple sampleMask);
            //sampleGrayval = sampleGrayval * sampleMask;

            // 生成采样图
            HOperatorSet.GenImageConst(out HObject sampleImg, "int2", sampleGrayval.Length / sampleNum, sampleNum);
            HOperatorSet.GetRegionPoints(sampleImg, out HTuple rows, out HTuple cols);
            HOperatorSet.SetGrayval(sampleImg, rows, cols, sampleGrayval);

            //ComAlgo.ShowHimageGray(sampleImg);
            // 缩放统计
            HOperatorSet.Threshold(sampleImg, out HObject validSampleRegion, 0, 255);              // 处理空值
            HOperatorSet.ReduceDomain(sampleImg, validSampleRegion, out HObject sampleImageReduce);       // 处理空值
            HOperatorSet.CropDomain(sampleImageReduce, out HObject sampleImageCrop);                        // 处理空值
            HOperatorSet.MinMaxGray(validSampleRegion, sampleImageCrop, 0, out _, out _, out HTuple grayRange);
            HTuple threshValue = grayRange * 0.25;

            HOperatorSet.ZoomImageSize(sampleImageCrop, out HObject sampleZoom, 3, sampleNum, "bicubic");
            HOperatorSet.GrayProjections(sampleZoom, sampleZoom, "simple", out _, out HTuple veProj);

            HTuple leftRightDiff = veProj[0] - veProj[2];

            if (leftRightDiff > threshValue)
            {
                polarityValue = -1.0;
            }
            else if (leftRightDiff < -threshValue)
            {
                polarityValue = 1.0;
            }
            else
            {
                // 镂空
                HTuple leftCenterDiff = veProj[0] - veProj[1];
                if (leftCenterDiff > threshValue)
                {
                    polarityValue = -0.5;
                }
                else if (leftCenterDiff < -threshValue)
                {
                    polarityValue = 0.5;
                }
                else
                {
                    // 镂空采样对比度较小时判断一下方差
                    HOperatorSet.GrayProjections(sampleZoom, sampleZoom, "simple", out _, out HTuple sampleProj);
                    HOperatorSet.TupleDeviation(sampleProj, out HTuple deviation);
                    if (deviation > 1)
                    {
                        polarityValue = 0.5;
                    }
                }
            }

            sampleImageReduce.Dispose();
            validSampleRegion.Dispose();
            hImageMasked.Dispose();
            imgBlack.Dispose();
            paintArea.Dispose();
            imgMaskedExtend.Dispose();
            hImage.Dispose();
            intImage.Dispose();
            sampleImg.Dispose();
            imgMaskedExtend.Dispose();
            sampleImageCrop.Dispose();
            sampleZoom.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 判断线段使用卡尺时候的极性
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="line">线段</param>
        /// <param name="polarityValue">极性值</param>
        /// <param name="grayThresh">灰度阈值</param>
        /// <param name="judgementWidth">判断的宽度，建议使用卡尺提取的宽度</param>
        /// <param name="regionIgnore">忽略区域</param>
        /// <param name="sampleNum">采样的数量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcLinePolarity(Camera img, LineSeg line, out double polarityValue, out double grayThresh, double judgementWidth = 15, HObject regionIgnore = null, int sampleNum = 10)
        {
            double noiseLevel = 0.15;
            int toleranceWidth = 10;
            polarityValue = 0.0;
            grayThresh = 0.0;
            int sample_num = sampleNum;
            Point sample_step = new Point(0, 0);
            Point sample_vector = new Point(0, 0);

            if (regionIgnore == null || !regionIgnore.IsInitialized())
            {
                HOperatorSet.GenEmptyRegion(out regionIgnore);
            }

            HObject hImage = img.GenHObject();

            // 生成采样线段
            sample_step.X = (line.End_X - line.Start_X) / (sample_num + 1);
            sample_step.Y = (line.End_Y - line.Start_Y) / (sample_num + 1);
            double sample_step_vector_scale = judgementWidth / Math.Sqrt(Math.Pow(sample_step.X, 2) + Math.Pow(sample_step.Y, 2));
            sample_vector.X = -sample_step.Y * sample_step_vector_scale * 0.5;
            sample_vector.Y = sample_step.X * sample_step_vector_scale * 0.5;
            double vectorLenX = Math.Abs(sample_vector.X);
            double vectorLenY = Math.Abs(sample_vector.Y);
            HTuple indices = new HTuple();
            HTuple derivateSumValues = new HTuple();
            HTuple derivateRangeValues = new HTuple();
            for (int sample_index = 1; sample_index <= sample_num; sample_index++)
            {
                Point sample_middle = new Point(line.Start_X, line.Start_Y) + (sample_step * sample_index);
                Point samplePointStart = sample_middle - sample_vector;
                Point samplePointEnd = sample_middle + sample_vector;
                HOperatorSet.GenRegionLine(out HObject sampleRegionLine, samplePointStart.Y, samplePointStart.X, samplePointEnd.Y, samplePointEnd.X);
                HOperatorSet.Difference(sampleRegionLine, regionIgnore, out sampleRegionLine);
                HOperatorSet.GetDomain(hImage, out HObject imageDomain);
                HOperatorSet.Intersection(sampleRegionLine, imageDomain, out HObject sampleRegionLineInImg);
                HOperatorSet.GetRegionPoints(sampleRegionLineInImg, out HTuple pointRows, out HTuple pointCols);
                HOperatorSet.GetGrayval(hImage, pointRows, pointCols, out HTuple grayValue);
                sampleRegionLine.Dispose();
                sampleRegionLineInImg.Dispose();
                if (grayValue.Length < 1)
                {
                    return Errortype.CALIPER_LINE_POLARITY_POINT_OUT_OF_IMAGE;
                }

                // 判断一下采样是X方向为主还是Y方向为主
                if (vectorLenX < vectorLenY)
                {
                    // 按采样方向排序
                    if (sample_vector.Y > 0)
                    {
                        HOperatorSet.TupleSortIndex(pointRows, out indices);
                    }
                    else
                    {
                        HOperatorSet.TupleSortIndex(-pointRows, out indices);
                    }
                }
                else
                {
                    if (sample_vector.X > 0)
                    {
                        HOperatorSet.TupleSortIndex(pointCols, out indices);
                    }
                    else
                    {
                        HOperatorSet.TupleSortIndex(-pointCols, out indices);
                    }
                }

                HOperatorSet.TupleSelect(grayValue, indices, out grayValue);    // 整理排序后的灰度值

                // 将灰度值归一化
                HOperatorSet.TupleReal(grayValue, out grayValue);
                HOperatorSet.TupleMin(grayValue, out HTuple grayMin);
                HOperatorSet.TupleMax(grayValue, out HTuple grayMax);
                grayValue = grayValue - grayMin;
                if (grayMax - grayMin > 0)
                {
                    grayValue = grayValue / (grayMax - grayMin);
                }

                HOperatorSet.CreateFunct1dArray(grayValue, out HTuple functGray);
                HOperatorSet.DerivateFunct1d(functGray, "first", out HTuple functdDerivate);
                HOperatorSet.Funct1dToPairs(functdDerivate, out HTuple xValues, out HTuple derivateValues);

                //HOperatorSet.TupleSum(derivateValues, out HTuple derivateSum);  // 计算该采样处的梯度和
                //HOperatorSet.TupleConcat(derivateSumValues, derivateSum, out derivateSumValues); // 整理整条线上各采样点位置的的梯度和
                HOperatorSet.TupleRound(derivateValues / noiseLevel, out HTuple roundValues);   // 归一化，去除噪声的梯度
                roundValues = roundValues * noiseLevel;

                HOperatorSet.TupleSum(roundValues, out HTuple derivateSum);
                HOperatorSet.TupleAbs(derivateSum, out HTuple absSumValue);

                HTuple roundDerivate = roundValues * (grayMax - grayMin);
                HOperatorSet.TupleSelectRange(roundDerivate, 1, roundDerivate.Length - 2, out roundDerivate);
                HOperatorSet.TupleAbs(roundDerivate, out roundDerivate);
                HOperatorSet.TupleMax(roundDerivate, out HTuple maxD1);

                // 梯度和小于0.5时，即同时出现灰度上升和下降沿，进一步判断上升下降沿是否满足最小宽度
                if (absSumValue < 0.5)
                {
                    HOperatorSet.TupleMin(roundValues, out HTuple minValue);        // 寻找下降沿位置
                    HOperatorSet.TupleFind(roundValues, minValue, out HTuple minId);
                    HOperatorSet.TupleMax(roundValues, out HTuple maxValue);        // 寻找上升沿位置
                    HOperatorSet.TupleFind(roundValues, maxValue, out HTuple maxId);

                    //if (minId.Length > 1) roundValues[minId[1].I] = 0;
                    //if (maxId.Length > 1) roundValues[maxId[1].I] = 0;
                    minId = minId[0];
                    maxId = maxId[0];

                    // 统计从靠近采样中心的变化沿到最小宽度位置的梯度和，如果在最小宽度之内又出现另一个梯度变化，则梯度和将小于0.5
                    HOperatorSet.TupleConcat(minId - roundValues.Length / 2, maxId - roundValues.Length / 2, out HTuple distToJudgeCenter);
                    HOperatorSet.TupleAbs(distToJudgeCenter, out distToJudgeCenter);
                    HOperatorSet.TupleSortIndex(distToJudgeCenter, out HTuple minDistId);
                    HTuple subSampleCenter = new HTuple(minId, maxId)[minDistId[0].I];

                    HOperatorSet.TupleMax2(0, subSampleCenter - toleranceWidth, out HTuple subSampleBegin);
                    HOperatorSet.TupleMin2(roundValues.Length - 1, subSampleCenter + toleranceWidth, out HTuple subSampleEnd);
                    HOperatorSet.TupleInt(subSampleBegin, out subSampleBegin);
                    HOperatorSet.TupleInt(subSampleEnd, out subSampleEnd);

                    // 采样线段超出图像边缘会导致采样end位置超出roundValues长度
                    if (subSampleEnd < roundValues.Length)
                    {
                        HOperatorSet.TupleSelectRange(roundValues, subSampleBegin, subSampleEnd, out HTuple subSampleValues);
                        HOperatorSet.TupleSum(subSampleValues, out derivateSum);
                        HOperatorSet.TupleConcat(derivateSumValues, derivateSum, out derivateSumValues);
                        HOperatorSet.TupleConcat(derivateRangeValues, maxD1, out derivateRangeValues);
                    }
                }
                else
                {
                    HOperatorSet.TupleConcat(derivateSumValues, derivateSum, out derivateSumValues);
                    HOperatorSet.TupleConcat(derivateRangeValues, maxD1, out derivateRangeValues);
                }
            }

            HOperatorSet.TupleMean(derivateSumValues, out HTuple derivateSumMean);
            HOperatorSet.TupleMean(derivateRangeValues, out HTuple derivateRangeMean);
            HOperatorSet.TupleRound(derivateRangeMean / 5, out derivateRangeMean);
            hImage.Dispose();
            polarityValue = derivateSumMean.D;
            grayThresh = derivateRangeMean.D * 5.0;
            return Errortype.OK;
        }

        /// <summary>
        /// 计算线段使用卡尺的极性
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="line">输入线段</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="polarityValue">输出极性结果</param>
        /// <param name="mask">掩模</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcLinePolarity(Camera img, LineSeg line, CaliperParams caliperParams, out string polarityValue, HObject mask = null)
        {
            polarityValue = "all";

            LineSeg lineClone = line.Clone();
            lineClone.ProbInfo = "positive";
            Errortype retPositive = LineExtraction(img, lineClone, caliperParams, out LineSeg lineOutPositive, out double[] straightnessPositive, mask);

            lineClone.ProbInfo = "negative";
            Errortype retNegative = LineExtraction(img, lineClone, caliperParams, out LineSeg lineOutNegative, out double[] straightnessNegative, mask);

            if (retPositive == Errortype.OK)
            {
                if (retNegative != Errortype.OK)
                {
                    polarityValue = "positive";
                }
                else
                {
                    // 都是ok的情况 去判断误差谁小用谁
                    //double sumPowPositive = 0;
                    //for (int i = 0; i < straightnessPositive.Length; i++)
                    //{
                    //    sumPowPositive += Math.Pow(straightnessPositive[i], 2);
                    //}

                    //sumPowPositive = sumPowPositive / straightnessPositive.Length;

                    //double sumPowNegative = 0;
                    //for (int i = 0; i < straightnessNegative.Length; i++)
                    //{
                    //    sumPowNegative += Math.Pow(straightnessNegative[i], 2);
                    //}

                    //sumPowNegative = sumPowNegative / straightnessNegative.Length;

                    //if (sumPowPositive < sumPowNegative)
                    //{
                    //    polarityValue = "positive";
                    //}
                    //else
                    //{
                    //    polarityValue = "negative";
                    //}
                    polarityValue = "all";
                }
            }
            else
            {
                if (retNegative == Errortype.OK)
                {
                    polarityValue = "negative";
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 卡尺提取边缘点
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="line">卡尺提取的线段</param>
        /// <param name="caliperParams">卡尺的参数</param>
        /// <param name="rows">卡尺提取结果的行集合</param>
        /// <param name="cols">卡尺提取结果的列集合</param>
        /// <param name="mask">掩膜区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype CaliperPoints(Camera img, LineSeg line, CaliperParams caliperParams, out double[] rows, out double[] cols, HObject mask = null)
        {
            rows = null;
            cols = null;

            HObject himg = img.GenHObject();
            HOperatorSet.GenEmptyObj(out HObject maskRegion);
            if (mask != null && mask.IsInitialized())
            {
                maskRegion = mask.Clone();
            }

            HOperatorSet.CreateMetrologyModel(out HTuple metrologyHandle);
            HOperatorSet.SetMetrologyModelImageSize(metrologyHandle, img.Width, img.Height);

            //判断line的长度是否是measureLength2的4倍，小于4倍报错
            HOperatorSet.DistancePp(line.Start_Y, line.Start_X, line.End_Y, line.End_X, out HTuple lineLength);
            double measureLength2 = caliperParams.MeasureLength2;
            if (lineLength.D <= (caliperParams.MeasureLength2 * 4))
            {
                measureLength2 = Math.Floor(lineLength.D / 4.0);
            }

            if (measureLength2 <= 0)
            {
                return Errortype.CALIPER_LINE_SHORT;
            }

            HOperatorSet.AddMetrologyObjectLineMeasure(metrologyHandle, line.Start_Y, line.Start_X, line.End_Y, line.End_X, caliperParams.MeasureLength1,
                measureLength2, caliperParams.MeasureSigma, caliperParams.MeasureThreshold, new HTuple(), new HTuple(), out HTuple index);

            string[] paramNameArray = new string[] { "num_measures", "num_instances", "measure_transition", "measure_interpolation", "min_score", "measure_select" };
            HTuple paramName = new HTuple(paramNameArray);
            HTuple paramValue = new HTuple();
            HOperatorSet.TupleConcat(paramValue, caliperParams.NumMeasures, out paramValue);
            HOperatorSet.TupleConcat(paramValue, caliperParams.NumInstances, out paramValue);
            HOperatorSet.TupleConcat(paramValue, caliperParams.MeasureTransition, out paramValue);
            HOperatorSet.TupleConcat(paramValue, caliperParams.MeasureInterpolation, out paramValue);
            HOperatorSet.TupleConcat(paramValue, caliperParams.MinScore, out paramValue);
            HOperatorSet.TupleConcat(paramValue, caliperParams.MeasureSelect, out paramValue);

            HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, paramName, paramValue);

            //HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "num_measures", caliperParams.NumMeasures);
            //HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "num_instances", caliperParams.NumInstances);
            //HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_transition", caliperParams.MeasureTransition);
            //HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_interpolation", caliperParams.MeasureInterpolation);
            //HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "min_score", caliperParams.MinScore);
            //HOperatorSet.SetMetrologyObjectParam(metrologyHandle, index, "measure_select", caliperParams.MeasureSelect);
            HOperatorSet.ApplyMetrologyModel(himg, metrologyHandle);
            HOperatorSet.GetMetrologyObjectMeasures(out HObject contours, metrologyHandle, index, "all", out HTuple hRow, out HTuple hCol);
            HOperatorSet.ClearMetrologyModel(metrologyHandle);

            if (hRow.Length == 0 || hCol.Length == 0)
            {
                return Errortype.CALIPER_LINE_NULL;
            }

            double[] rowsArray = hRow.DArr;
            double[] colsArray = hCol.DArr;
            List<double> rowList = new List<double>();
            List<double> colList = new List<double>();

            for (int i = 0; i < rowsArray.Length; i++)
            {
                double row = rowsArray[i];
                double col = colsArray[i];
                HOperatorSet.TestRegionPoint(maskRegion, row, col, out HTuple isInside);
                if (isInside.Length < 1)
                {
                    rowList.Add(row);
                    colList.Add(col);
                    continue;
                }

                if (isInside.I == 0)
                {
                    rowList.Add(row);
                    colList.Add(col);
                }
            }

            if (rowList.Count < 1)
            {
                return Errortype.CALIPER_LINE_NULL;
            }

            rows = rowList.ToArray();
            cols = colList.ToArray();

            contours.Dispose();
            maskRegion?.Dispose();
            himg.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 提取直线
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="line">线段</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="lineOut">提取到的线段</param>
        /// <param name="straightnessError">直线度误差</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype LineExtraction(Camera img, LineSeg line, CaliperParams caliperParams, out LineSeg lineOut, out double[] straightnessError, HObject mask = null)
        {
            lineOut = new LineSeg();
            straightnessError = null;
            CaliperParams caliperParamsNew = caliperParams.Clone();
            Errortype ret = Errortype.OK;

            if (line.ProbInfo == "positive")
            {
                caliperParamsNew.MeasureTransition = Polarity.Positive.ToString().ToLower();
            }
            else if (line.ProbInfo == "negative")
            {
                caliperParamsNew.MeasureTransition = Polarity.Negative.ToString().ToLower();
            }
            else
            {
                if (caliperParamsNew.MeasureTransition == "auto")
                {
                    //CalcLinePolarity(img, line, out double polarityValue, caliperParams.MeasureLength1 * 2, mask);
                    ret = CalcLinePolarity(img, line, caliperParams, out string polarity, mask);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    if (polarity == "all")
                    {
                        caliperParamsNew.MeasureTransition = "positive";
                    }
                    else
                    {
                        caliperParamsNew.MeasureTransition = polarity;
                    }
                }
            }

            ret = CaliperPoints(img, line, caliperParamsNew, out double[] rows, out double[] cols, mask);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = Ransac.RansacLine.LineExtraction(cols.ToList(), rows.ToList(), caliperParams.RansacParam, out List<double> colsRansac, out List<double> rowsRansac);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if ((double)colsRansac.Count / (double)cols.Length < caliperParams.MinScore)
            {
                return Errortype.CALIPER_NUM_MINSOCRE;
            }

            ret = ComAlgo.FitLine(rowsRansac.ToArray(), colsRansac.ToArray(), out lineOut, out straightnessError);

            lineOut.ProbInfo = caliperParamsNew.MeasureTransition;
            return ret;
        }

        /// <summary>
        /// 提取直线
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="line">线段</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="lineOut">提取到的线段</param>
        /// <param name="straightnessError">直线度误差</param>
        /// <param name="lineScore">直线度分数</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype LineExtraction(Camera img, LineSeg line, CaliperParams caliperParams, out LineSeg lineOut, out double[] straightnessError, out double lineScore, HObject mask = null)
        {
            lineOut = new LineSeg();
            lineScore = 0;
            straightnessError = null;
            CaliperParams caliperParamsNew = caliperParams.Clone();
            Errortype ret = Errortype.OK;

            if (line.ProbInfo == "positive")
            {
                caliperParamsNew.MeasureTransition = Polarity.Positive.ToString().ToLower();
            }
            else if (line.ProbInfo == "negative")
            {
                caliperParamsNew.MeasureTransition = Polarity.Negative.ToString().ToLower();
            }
            else
            {
                if (caliperParamsNew.MeasureTransition == "auto")
                {
                    //CalcLinePolarity(img, line, out double polarityValue, caliperParams.MeasureLength1 * 2, mask);
                    ret = CalcLinePolarity(img, line, caliperParams, out string polarity, mask);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    if (polarity == "all")
                    {
                        caliperParamsNew.MeasureTransition = "positive";
                    }
                    else
                    {
                        caliperParamsNew.MeasureTransition = polarity;
                    }
                }
            }

            ret = CaliperPoints(img, line, caliperParamsNew, out double[] rows, out double[] cols, mask);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = Ransac.RansacLine.LineExtraction(cols.ToList(), rows.ToList(), caliperParams.RansacParam, out List<double> colsRansac, out List<double> rowsRansac);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            lineScore = (double)colsRansac.Count / (double)cols.Length;
            if (lineScore < caliperParams.MinScore)
            {
                return Errortype.CALIPER_NUM_MINSOCRE;
            }

            ret = ComAlgo.FitLine(rowsRansac.ToArray(), colsRansac.ToArray(), out lineOut, out _);

            lineOut.ProbInfo = caliperParamsNew.MeasureTransition;
            return ret;
        }
    }

    /// <summary>
    /// 卡尺提取圆
    /// </summary>
    public static class CaliperCircle
    {
        /// <summary>
        /// 卡尺提取圆
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="circleCenter">初始圆心</param>
        /// <param name="circleRadius">初始圆半径</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="circleCenterOut">提取圆心</param>
        /// <param name="radiusOut">提取圆半径</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CircleExtraction(Camera img, Point circleCenter, double circleRadius, CaliperParams caliperParams, out Point circleCenterOut, out double radiusOut, HObject mask = null)
        {
            circleCenterOut = new Point();
            radiusOut = 0.0;
            List<Point> circleCenterList = new List<Point>() { circleCenter };
            List<double> circleRadiusList = new List<double>() { circleRadius };
            var ret = CircleExtraction(img, circleCenterList, circleRadiusList, caliperParams, out List<Point> extractCenterOut,
                out List<double> extractRadiusOut, mask);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            circleCenterOut = extractCenterOut[0];
            radiusOut = extractRadiusOut[0];
            return Errortype.OK;
        }

        /// <summary>
        /// 卡尺提取圆
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="circleCenter">初始圆圆心</param>
        /// <param name="circleRadius">初始圆半径</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="circleCenterOut">提取圆圆心</param>
        /// <param name="radiusOut">提取圆半径</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CircleExtraction(Camera img, List<Point> circleCenter, List<double> circleRadius, CaliperParams caliperParams, out List<Point> circleCenterOut, out List<double> radiusOut, HObject mask = null)
        {
            circleCenterOut = new List<Point>();
            radiusOut = new List<double>();
            if (img is null)
            {
                return Errortype.OPT_CALIBRATE_MARK_ERROR;
            }

            if ((img.Height < 1) || (img.Width < 1))
            {
                return Errortype.OPT_CALIBRATE_MARK_ERROR;
            }

            int circleNum = circleCenter.Count;

            double[] circleCenterArrayX = new double[circleNum];
            double[] circleCenterArrayY = new double[circleNum];
            for (int circleId = 0; circleId < circleNum; circleId++)
            {
                circleCenterArrayX[circleId] = circleCenter[circleId].X;
                circleCenterArrayY[circleId] = circleCenter[circleId].Y;
            }

            HObject himg = img.GenHObject();
            CaliperParams caliperParamsNew = caliperParams.Clone();
            if (caliperParamsNew.MeasureTransition == "auto")
            {
                caliperParamsNew.MeasureTransition = "uniform";
            }

            // 去除mask区域(保留原图像尺寸)
            if (mask != null)
            {
                HOperatorSet.GetDomain(himg, out HObject domain);
                HOperatorSet.GenEmptyObj(out HObject maskRegion);
                maskRegion = mask;
                HOperatorSet.Difference(domain, maskRegion, out HObject regionIgnore);
                HOperatorSet.GetRegionPoints(regionIgnore, out HTuple diffRegionRow, out HTuple diffRegionCol);
                HOperatorSet.TupleGenConst(diffRegionCol.Length, 0, out HTuple zero);
                HOperatorSet.SetGrayval(himg, diffRegionRow, diffRegionCol, zero);
                regionIgnore.Dispose();
                domain.Dispose();
                maskRegion.Dispose();
            }

            // 建立测量模板
            HOperatorSet.CreateMetrologyModel(out HTuple hv_MetrologyHandle);
            HOperatorSet.SetMetrologyModelImageSize(hv_MetrologyHandle, img.Width, img.Height);

            HOperatorSet.AddMetrologyObjectCircleMeasure(hv_MetrologyHandle, circleCenterArrayY, circleCenterArrayX, circleRadius.ToArray(), caliperParamsNew.MeasureLength1, caliperParamsNew.MeasureLength2,
                caliperParamsNew.MeasureSigma, caliperParamsNew.MeasureThreshold, new HTuple(), new HTuple(), out HTuple hv_MetrologyCircleIndices);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "min_score", caliperParamsNew.MinScore);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "num_measures", caliperParamsNew.NumMeasures);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "num_instances", caliperParamsNew.NumInstances);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_transition", caliperParamsNew.MeasureTransition);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_interpolation", caliperParamsNew.MeasureInterpolation);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_select", caliperParamsNew.MeasureSelect);

            // 执行测量
            HOperatorSet.ApplyMetrologyModel(himg, hv_MetrologyHandle);

            // 获取测量结果
            HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "result_type", "all_param", out HTuple hv_CircleParameter);
            if (hv_CircleParameter.Length < 1)
            {
                return Errortype.CALIPER_CIRCLE_NULL;
            }

            HTuple hv_Sequence = HTuple.TupleGenSequence(0, new HTuple(hv_CircleParameter.TupleLength()) - 1, 3);
            HTuple circleCenterY = hv_CircleParameter.TupleSelect(hv_Sequence);
            HTuple circleCenterX = hv_CircleParameter.TupleSelect(hv_Sequence + 1);
            HTuple circleR = hv_CircleParameter.TupleSelect(hv_Sequence + 2);
            radiusOut = circleR.DArr.ToList<double>();
            circleCenterOut = new List<Point>();
            for (int resId = 0; resId < radiusOut.Count; resId++)
            {
                circleCenterOut.Add(new Point(circleCenterX[resId], circleCenterY[resId]));
            }

            //Console.WriteLine("Locate circle center precise: " + circleCenterX.D.ToString() + ", " + circleCenterY.D.ToString());
            himg.Dispose();
            HOperatorSet.ClearMetrologyModel(hv_MetrologyHandle);

            return Errortype.OK;
        }

        /// <summary>
        /// 卡尺提取圆弧
        /// 'start_phi' and 'end_phi' in mathematically positive direction (counterclockwise)
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="circleCenter">初始圆弧圆心</param>
        /// <param name="circleRadius">初始圆弧半径</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="circleCenterOut">提取圆圆心</param>
        /// <param name="radiusOut">提取圆半径</param>
        /// <param name="arcPointsRow">提取圆边缘点行坐标</param>
        /// <param name="arcPointsCol">提取圆边缘点列坐标</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CircleArcExtraction(Camera img, Point circleCenter, double circleRadius, CaliperParams caliperParams, out Point circleCenterOut, out double radiusOut, out double[] arcPointsRow, out double[] arcPointsCol, HObject mask = null)
        {
            circleCenterOut = new Point();
            radiusOut = 0.0;
            arcPointsRow = null;
            arcPointsCol = null;
            if (img is null)
            {
                return Errortype.OPT_CALIBRATE_MARK_ERROR;
            }

            if ((img.Height < 1) || (img.Width < 1))
            {
                return Errortype.OPT_CALIBRATE_MARK_ERROR;
            }

            HObject himg = img.GenHObject();

            CaliperParams caliperParamsNew = caliperParams.Clone();
            caliperParamsNew.CircleStartPhi = caliperParams.CircleStartPhi;
            caliperParamsNew.CircleEndPhi = caliperParams.CircleEndPhi;

            if (caliperParamsNew.MeasureTransition == "auto")
            {
                caliperParamsNew.MeasureTransition = "uniform";
            }

            if (mask != null)
            {
                HOperatorSet.GetDomain(himg, out HObject domain);
                HOperatorSet.GenEmptyObj(out HObject maskRegion);
                maskRegion = mask;
                HOperatorSet.Difference(domain, maskRegion, out HObject regionIgnore);
                HOperatorSet.GetRegionPoints(regionIgnore, out HTuple diffRegionRow, out HTuple diffRegionCol);
                HOperatorSet.TupleGenConst(diffRegionCol.Length, 0, out HTuple zero);
                HOperatorSet.SetGrayval(himg, diffRegionRow, diffRegionCol, zero);
                regionIgnore.Dispose();
                domain.Dispose();
                maskRegion.Dispose();
            }

            //HOperatorSet.GenCircleContourXld(out HObject circleCont, circleCenter.Y, circleCenter.X, circleRadius, caliperParams.CircleStartPhi, caliperParams.CircleEndPhi, "positive", 1);

            // 建立测量模板
            HOperatorSet.CreateMetrologyModel(out HTuple hv_MetrologyHandle);
            HOperatorSet.SetMetrologyModelImageSize(hv_MetrologyHandle, img.Width, img.Height);

            HOperatorSet.AddMetrologyObjectCircleMeasure(hv_MetrologyHandle, circleCenter.Y, circleCenter.X, circleRadius, caliperParamsNew.MeasureLength1, caliperParamsNew.MeasureLength2,
                caliperParamsNew.MeasureSigma, caliperParamsNew.MeasureThreshold, new HTuple("start_phi", "end_phi", "point_order"), new HTuple(caliperParamsNew.CircleStartPhi, caliperParamsNew.CircleEndPhi, "positive"), out HTuple hv_MetrologyCircleIndices);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "min_score", caliperParamsNew.MinScore);

            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "num_measures", caliperParamsNew.NumMeasures);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "num_instances", caliperParamsNew.NumInstances);

            //HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_interpolation", caliperParamsNew.MeasureInterpolation);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_select", caliperParamsNew.MeasureSelect);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_transition", caliperParamsNew.MeasureTransition);

            // 执行测量
            HOperatorSet.ApplyMetrologyModel(himg, hv_MetrologyHandle);

            // 获取测量结果
            HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "result_type", "all_param", out HTuple hv_CircleParameter);
            if (hv_CircleParameter.Length < 1)
            {
                return Errortype.CALIPER_CIRCLE_NULL;
            }

            HTuple hv_Sequence = HTuple.TupleGenSequence(0, new HTuple(hv_CircleParameter.TupleLength()) - 1, 3);
            HTuple circleCenterY = hv_CircleParameter.TupleSelect(hv_Sequence);
            HTuple circleCenterX = hv_CircleParameter.TupleSelect(hv_Sequence + 1);
            HTuple circleR = hv_CircleParameter.TupleSelect(hv_Sequence + 2);
            radiusOut = circleR.D;
            circleCenterOut = new Point(circleCenterX.D, circleCenterY.D);

            HOperatorSet.GetMetrologyObjectMeasures(out _, hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", out _, out _);
            HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "used_edges", "row", out HTuple edgePointsRow);
            HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "used_edges", "column", out HTuple edgePointsCol);
            arcPointsRow = edgePointsRow.DArr;
            arcPointsCol = edgePointsCol.DArr;
            himg.Dispose();

            HOperatorSet.ClearMetrologyModel(hv_MetrologyHandle);

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 卡尺提取矩形
    /// </summary>
    public static class CaliperRectangle
    {
        /// <summary>
        /// 提取矩形2(带倾斜角矩形)
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="rectCenter">目标测量矩形上午中心</param>
        /// <param name="rectPhi">矩形倾角</param>
        /// <param name="rectWidth">矩形长轴</param>
        /// <param name="rectHeight">矩形短轴</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="rectCenterOut">提取出的矩形中心</param>
        /// <param name="phiOut">提取出矩形角度</param>
        /// <param name="widthOut">提取出矩形宽度</param>
        /// <param name="heightOut">提取出矩形高度</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Rectangle2Extraction(Camera img, Point rectCenter, double rectPhi, double rectWidth, double rectHeight, CaliperParams caliperParams, out Point rectCenterOut, out double phiOut, out double widthOut, out double heightOut, HObject mask = null)
        {
            rectCenterOut = new Point();
            phiOut = 0.0;
            widthOut = 0.0;
            heightOut = 0.0;
            List<Point> rectCenterList = new List<Point>() { rectCenter };
            List<double> rectPhiList = new List<double>() { rectPhi };
            List<double> rectWidthList = new List<double>() { rectWidth };
            List<double> rectHeightList = new List<double>() { rectHeight };
            var ret = Rectangle2Extraction(img, rectCenterList, rectPhiList, rectWidthList, rectHeightList, caliperParams, out List<Point> extractRectCenter, out List<double> extractPhi, out List<double> extractWidthOut, out List<double> extractHeightOut, null);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            rectCenterOut = extractRectCenter[0];
            phiOut = extractPhi[0];
            widthOut = extractWidthOut[0];
            heightOut = extractHeightOut[0];
            return Errortype.OK;
        }

        /// <summary>
        /// 卡尺提取方形mark
        /// </summary>
        /// <param name="img">输入图片</param>
        /// <param name="rectCenter">粗矩形中心</param>
        /// <param name="rectPhi">粗角度</param>
        /// <param name="rectWidth">粗宽度</param>
        /// <param name="rectHeight">粗高度</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="rectCenterOut">卡尺提取矩形中心</param>
        /// <param name="phiOut">卡尺提取角度</param>
        /// <param name="widthOut">卡尺提取宽</param>
        /// <param name="heightOut">卡尺提取高</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功；其他：失败</returns>
        public static Errortype Rectangle2Extraction(Camera img, List<Point> rectCenter, List<double> rectPhi, List<double> rectWidth, List<double> rectHeight, CaliperParams caliperParams, out List<Point> rectCenterOut, out List<double> phiOut, out List<double> widthOut, out List<double> heightOut, HObject mask = null)
        {
            rectCenterOut = new List<Point>();
            phiOut = new List<double>();
            widthOut = new List<double>();
            heightOut = new List<double>();
            if (img is null)
            {
                return Errortype.OPT_CALIBRATE_MARK_ERROR;
            }

            if ((img.Height < 1) || (img.Width < 1))
            {
                return Errortype.OPT_CALIBRATE_MARK_ERROR;
            }

            int rectNum = rectCenter.Count;

            double[] rectCenterArrayX = new double[rectNum];
            double[] rectCenterArrayY = new double[rectNum];
            double[] rectWidthArray = new double[rectNum];
            double[] rectHeightArray = new double[rectNum];
            for (int rectId = 0; rectId < rectNum; rectId++)
            {
                rectCenterArrayX[rectId] = rectCenter[rectId].X;
                rectCenterArrayY[rectId] = rectCenter[rectId].Y;
                rectWidthArray[rectId] = rectWidth[rectId] / 2;
                rectHeightArray[rectId] = rectHeight[rectId] / 2;
            }

            HObject himg = img.GenHObject();

            CaliperParams caliperParamsNew = caliperParams.Clone();

            if (caliperParamsNew.MeasureTransition == "auto")
            {
                caliperParamsNew.MeasureTransition = "uniform";
            }

            HOperatorSet.GetDomain(himg, out HObject domain);
            HOperatorSet.GenEmptyObj(out HObject maskRegion);
            if (mask != null)
            {
                maskRegion = mask;
            }

            HOperatorSet.Difference(domain, maskRegion, out HObject regionDifference);
            HOperatorSet.ReduceDomain(himg, regionDifference, out HObject himgReduced);
            HOperatorSet.CropDomain(himgReduced, out HObject himgPart);

            // 建立测量模板
            HOperatorSet.CreateMetrologyModel(out HTuple hv_MetrologyHandle);
            HOperatorSet.SetMetrologyModelImageSize(hv_MetrologyHandle, img.Width, img.Height);
            HOperatorSet.AddMetrologyObjectRectangle2Measure(hv_MetrologyHandle, rectCenterArrayY, rectCenterArrayX, rectPhi.ToArray(), rectWidthArray, rectHeightArray,
                caliperParamsNew.MeasureLength1, caliperParamsNew.MeasureLength2, caliperParamsNew.MeasureSigma, caliperParamsNew.MeasureThreshold, new HTuple(), new HTuple(), out HTuple hv_MetrologyRectIndices);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyRectIndices, "min_score", caliperParamsNew.MinScore);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyRectIndices, "num_measures", caliperParamsNew.NumMeasures);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyRectIndices, "num_instances", caliperParamsNew.NumInstances);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyRectIndices, "measure_transition", caliperParamsNew.MeasureTransition);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyRectIndices, "measure_interpolation", caliperParamsNew.MeasureInterpolation);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyRectIndices, "measure_select", caliperParamsNew.MeasureSelect);

            // 执行测量
            HOperatorSet.ApplyMetrologyModel(himg, hv_MetrologyHandle);

            // 获取测量结果
            HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyRectIndices, "all", "result_type", "all_param", out HTuple hv_RectangleParameter);
            if (hv_RectangleParameter.Length < 1)
            {
                return Errortype.CALIPER_CIRCLE_NULL;
            }

            HTuple sequence = HTuple.TupleGenSequence(0, new HTuple(hv_RectangleParameter.TupleLength()) - 1, 5);
            HTuple rectCenterY = hv_RectangleParameter.TupleSelect(sequence);
            HTuple rectCenterX = hv_RectangleParameter.TupleSelect(sequence + 1);
            HTuple rectPhiHv = hv_RectangleParameter.TupleSelect(sequence + 2);
            HTuple rectLen1 = hv_RectangleParameter.TupleSelect(sequence + 3);
            HTuple rectLen2 = hv_RectangleParameter.TupleSelect(sequence + 4);
            phiOut = rectPhiHv.DArr.ToList<double>();
            rectCenterOut = new List<Point>();
            for (int resId = 0; resId < phiOut.Count; resId++)
            {
                rectCenterOut.Add(new Point(rectCenterX[resId].D, rectCenterY[resId].D));
                widthOut.Add(rectLen1[resId].D * 2);
                heightOut.Add(rectLen2[resId].D * 2);
            }

            himg.Dispose();
            domain.Dispose();
            maskRegion.Dispose();
            regionDifference.Dispose();
            himgReduced.Dispose();
            himgPart.Dispose();
            HOperatorSet.ClearMetrologyModel(hv_MetrologyHandle);
            return Errortype.OK;
        }

        /// <summary>
        /// 提取水平矩形1(无倾斜角矩形)
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="rectCenter">初始矩形中心</param>
        /// <param name="rectWidth">初始矩形宽度</param>
        /// <param name="rectHeight">初始矩形高度</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="rectCenterOut">提取出矩形中心</param>
        /// <param name="widthOut">提取矩形宽度</param>
        /// <param name="heightOut">提取矩形高度</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Rectangle1Extraction(Camera img, Point rectCenter, double rectWidth, double rectHeight, CaliperParams caliperParams, out Point rectCenterOut, out double widthOut, out double heightOut, HObject mask = null)
        {
            rectCenterOut = new Point();
            widthOut = 0.0;
            heightOut = 0.0;
            return Rectangle2Extraction(img, rectCenter, 0.0, rectWidth, rectHeight, caliperParams, out rectCenterOut, out double phi, out widthOut, out heightOut, mask);
        }
    }
}
