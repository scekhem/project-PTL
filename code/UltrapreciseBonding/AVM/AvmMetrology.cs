using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using DataStruct;
using HalconDotNet;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using OpenCvSharp;
using Point = DataStruct.Point;

namespace UltrapreciseBonding.FusionCollections.AVM
{
    /// <summary>
    /// 生成的AVM量测点的排序方向
    /// </summary>
    public enum AVMMeasureArrayOrder
    {
        LeftUP,     // 从左到右，上到下
        LeftDown,   // 从左到右，下到上
        RightUP,    // 从右到左，上到下
        RightDown,   // 从右到左，下到上
    }

    /// <summary>
    /// AVM单片量测计算
    /// </summary>
    public static class AvmMetrology
    {
        /// <summary>
        /// 生成相机绕圈搜索坐标
        /// </summary>
        /// <param name="searchRound">搜索圈数</param>
        /// <param name="stepRate">步长倍率</param>
        /// <param name="currentAxis">当前相机轴坐标</param>
        /// <param name="stepXY">步长XY</param>
        /// <param name="searchPoints">输出搜索点位坐标</param>
        /// <returns>AVM_SEARCH_MARK_ROUND_NUM_ERROR：绕圈数大于1/AVM_SEARCH_MARK_ROUND_NUM_ERROR：绕圈步长大于0</returns>
        public static Errortype GenSearchArray(int searchRound, double stepRate, Point currentAxis, Point stepXY, out List<Point> searchPoints)
        {
            searchPoints = new List<Point>();
            if (searchRound < 1)
            {
                return Errortype.AVM_SEARCH_MARK_ROUND_NUM_ERROR;
            }

            if (stepRate < 0)
            {
                return Errortype.AVM_SEARCH_MARK_ROUND_NUM_ERROR;
            }

            // 生成偏移倍率的乘数矩阵
            HOperatorSet.TupleGenSequence(-searchRound, searchRound, 1, out HTuple stepsRow);
            HOperatorSet.TupleGenSequence(-searchRound, searchRound, 1, out HTuple stepsCol);
            HOperatorSet.CreateMatrix(stepsRow.Length, 1, stepsRow, out HTuple matrixRow);
            HOperatorSet.RepeatMatrix(matrixRow, 1, stepsCol.Length, out matrixRow);
            HOperatorSet.CreateMatrix(1, stepsCol.Length, stepsCol, out HTuple matrixCol);
            HOperatorSet.RepeatMatrix(matrixCol, stepsRow.Length, 1, out matrixCol);
            HOperatorSet.GetFullMatrix(matrixRow, out stepsRow);
            HOperatorSet.GetFullMatrix(matrixCol, out stepsCol);

            // 生成绕圈序列
            List<int> sortIndex = new List<int>();
            int[] rowDirect = new int[] { 0, 1, 0, -1 };
            int[] colDirect = new int[] { 1, 0, -1, 0 };
            int turns = 0;
            int currentId = 0;
            int rowGap = searchRound * 2 + 1;
            int directStep = rowGap;
            while (sortIndex.Count < stepsRow.Length)
            {
                int directId = turns % 4;
                for (int i = 0; i < directStep; i++)
                {
                    currentId += (rowDirect[directId] * rowGap) + (colDirect[directId] * 1);
                    sortIndex.Add(currentId - 1);
                }

                if (turns % 2 == 0)
                {
                    directStep -= 1;
                }

                turns += 1;
            }

            HOperatorSet.TupleInverse(sortIndex.ToArray(), out HTuple sortedId);
            HOperatorSet.TupleSelect(stepsRow, sortedId, out HTuple sortedRow);
            HOperatorSet.TupleSelect(stepsCol, sortedId, out HTuple sortedCol);

            // 将所有点偏移量加上中心相对位置
            for (int i = 1; i < sortedRow.Length; i++)
            {
                // 单点偏移量=偏移步长*偏移乘数*偏移倍率
                Point onePoint = new Point(sortedCol[i] * stepXY.X * stepRate, sortedRow[i] * stepXY.Y * stepRate);
                searchPoints.Add(currentAxis + onePoint);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 检查当前图像里有没有图案
        /// </summary>
        /// <param name="imageToCheck">需要检查的图像</param>
        /// <param name="searchPix">输出检查到的图案中心</param>
        /// <param name="needToSearch">输出是否有图案</param>
        /// <param name="dynamicRate">动态分割阈值，使用默认值</param>
        /// <returns>OK：计算成功</returns>
        public static Errortype CheckSearchImag(Camera imageToCheck, out Point searchPix, out bool needToSearch, double dynamicRate = 0.05)
        {
            needToSearch = false;
            if (imageToCheck is null || imageToCheck.Height < 10)
            {
                searchPix = null;
                return Errortype.CAMERA_DATA_NULL_ERROR;
            }

            searchPix = new Point(imageToCheck.Height / 2.0, imageToCheck.Width / 2.0);

            HObject himage = imageToCheck.GenHObject();
            HOperatorSet.MeanImage(himage, out HObject imageFiltered, 7, 7);  // 滤波去除相机增益产生的高亮白点，影响下面的灰度Range统计
            HOperatorSet.MinMaxGray(imageFiltered, imageFiltered, 0, out _, out _, out HTuple grayRange);

            if (grayRange < 15)
            {
                return Errortype.OK;
            }

            HOperatorSet.MeanImage(himage, out HObject imgMean, imageToCheck.Height / 4, imageToCheck.Height / 4);
            HOperatorSet.DynThreshold(himage, imgMean, out HObject dynRegion, grayRange * dynamicRate, "not_equal");
            HOperatorSet.Connection(dynRegion, out HObject connetRegions);
            HOperatorSet.CountObj(connetRegions, out HTuple regioNumber);
            if (regioNumber < 1)
            {
                return Errortype.OK;
            }

            HOperatorSet.AreaCenter(connetRegions, out HTuple areaDyn, out HTuple dynRow, out HTuple dynCol);
            HOperatorSet.TupleDeviation(areaDyn, out HTuple areaDeviation);
            if (regioNumber > 1 && areaDeviation < 100)
            {
                return Errortype.OK;
            }

            HOperatorSet.TupleMax(areaDyn, out HTuple maxArea);
            HOperatorSet.TupleFindFirst(areaDyn, maxArea, out HTuple maxId);
            searchPix = new Point(dynCol[maxId], dynRow[maxId]);
            needToSearch = true;

            himage.Dispose();
            imageFiltered.Dispose();
            imgMean.Dispose();
            dynRegion.Dispose();
            connetRegions.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 计算Map上的测量点真实坐标
        /// </summary>
        /// <param name="pointsInMap">点在预设spot map上的坐标</param>
        /// <param name="pointsFindOut">点实际搜索到的chuck坐标</param>
        /// <param name="pointsToLocate">需要更新位置的spot map点</param>
        /// <param name="pointsPredict">输出更新后的点在chuck上的坐标</param>
        /// <returns>AVM_CHECKPOINTS_NUM_ERROR：输入点为空</returns>
        public static Errortype CalcMapPoints(List<Point> pointsInMap, List<Point> pointsFindOut, List<Point> pointsToLocate, out List<Point> pointsPredict)
        {
            pointsPredict = new List<Point>();
            if ((pointsInMap.Count < 2) || (pointsFindOut.Count < 2))
            {
                return Errortype.AVM_CHECKPOINTS_NUM_ERROR;
            }

            if (pointsInMap.Count != pointsFindOut.Count)
            {
                return Errortype.AVM_CHECKPOINTS_NUM_ERROR;
            }

            double[] mapX = new double[pointsInMap.Count];
            double[] mapY = new double[pointsInMap.Count];
            double[] chuckX = new double[pointsInMap.Count];
            double[] chuckY = new double[pointsInMap.Count];
            for (int i = 0; i < pointsInMap.Count; i++)
            {
                if (pointsInMap[i] is null || pointsInMap[i] is null)
                {
                    continue;
                }

                mapX[i] = pointsInMap[i].X;
                mapY[i] = pointsInMap[i].Y;
                chuckX[i] = pointsFindOut[i].X;
                chuckY[i] = pointsFindOut[i].Y;
            }

            try
            {
                HOperatorSet.VectorToRigid(mapX, mapY, chuckX, chuckY, out HTuple curRulerToRealMatrix);
                for (int j = 0; j < pointsToLocate.Count; j++)
                {
                    HOperatorSet.AffineTransPoint2d(curRulerToRealMatrix, pointsToLocate[j].X, pointsToLocate[j].Y, out HTuple meshX, out HTuple meshY);
                    pointsPredict.Add(new Point(meshX.D, meshY.D));
                }
            }
            catch (HOperatorException e)
            {
                pointsPredict = pointsToLocate;
                return Errortype.AVM_SEARCHPOINTS_MISSMATCH_TO_TEACHPOINTS;
            }

            if (ComAlgo.SaveFlg("CalcMapPoints", out int days))
            {
                string pointsInMapStr = "pointsInMap ";
                string pointsFindOutStr = "pointsFindOut ";
                string pointsToLocateStr = "pointsToLocate ";
                string pointsPredictStr = "pointsPredict ";
                string sep = " ";
                for (int i = 0; i < pointsInMap.Count; i++)
                {
                    pointsInMapStr += pointsInMap[i].ToString(sep) + sep;
                    pointsFindOutStr += pointsFindOut[i].ToString(sep) + sep;
                }

                for (int i = 0; i < pointsToLocate.Count; i++)
                {
                    pointsToLocateStr += pointsToLocate[i].ToString(sep) + sep;
                    pointsPredictStr += pointsPredict[i].ToString(sep) + sep;
                }

                string text = pointsInMapStr + "\n" + pointsFindOutStr + "\n" + pointsToLocateStr + "\n" + pointsPredictStr;
                string path = @"D:\Alg\CalcMapPoints";
                string fileName = "CalcMapPoints.txt";

                ComAlgo.LogText(text, path, fileName, days);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// AVM Overlay测量
        /// </summary>
        /// <param name="markPositions">所有测量点的chuck坐标</param>
        /// <param name="offsetVectors">所有测量点的偏移量</param>
        /// <param name="outPutModelParam">输出ovelay结果</param>
        /// <param name="residual">各点的overlay残差</param>
        /// <returns>AVM_INPUT_POINT_NULL：输入的点数为空</returns>
        public static Errortype CalcOverlayModel(List<Point> markPositions, List<Point> offsetVectors, out AVMOverlayModelParam outPutModelParam, out List<Point> residual)
        {
            outPutModelParam = new AVMOverlayModelParam();
            residual = new List<Point>();
            List<Point> modelResidual = new List<Point>();

            if (markPositions == null || offsetVectors == null)
            {
                return Errortype.AVM_INPUT_POINT_NULL;
            }

            if (markPositions.Count < 3 || offsetVectors.Count < 3)
            {
                if (markPositions.Count == 2)
                {
                    Point basePoint1 = markPositions[0];
                    Point basePoint2 = markPositions[1];
                    Point curPoint1 = markPositions[0] + offsetVectors[0];
                    Point curPoint2 = markPositions[1] + offsetVectors[1];
                    Point baseCenterRuler = (basePoint1 + basePoint2) / 2;
                    Point curCenterRuler = (curPoint1 + curPoint2) / 2;

                    LineSeg line1 = new LineSeg(basePoint1, basePoint2);
                    LineSeg line2 = new LineSeg(curPoint1, curPoint2);
                    Errortype ret = ComAlgo.CalcAngleLL(line1, line2, out double angleRad);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    HOperatorSet.TupleDeg(angleRad, out HTuple angleDeg);
                    ret = ComAlgo.CalcRotatePoint(curCenterRuler, -angleDeg, new Point(0, 0), out Point pointRotated);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    double runOutRate = Math.Abs(basePoint1.DistanceTo(basePoint2) - curPoint1.DistanceTo(curPoint2)) /
                        basePoint1.DistanceTo(basePoint2) * 1000000;

                    Point translation = baseCenterRuler - pointRotated;
                    outPutModelParam.TransX = translation.X;
                    outPutModelParam.TransY = translation.Y;
                    outPutModelParam.RotationUrad = angleRad * 1000000;
                    outPutModelParam.GoodnessOfFit = 100;
                    outPutModelParam.RunOut = runOutRate;
                    outPutModelParam.ModelError = 0.0;
                    residual = new List<Point>() { new Point(0, 0), new Point(0, 0) };
                    return Errortype.OK;
                }

                return Errortype.AVM_INPUT_POINT_COUNT_ERROR;
            }

            int pointsCount = markPositions.Count;

            // 测量点坐标
            HTuple px = new HTuple();
            HTuple py = new HTuple();

            // 自变量矩阵
            HTuple pColA = new HTuple();    // X   Y
            HTuple pColB = new HTuple();    //-Y   X
            HTuple pColC = new HTuple();    // 1   0
            HTuple pColD = new HTuple();    // 0  -1

            // 偏移量矩阵(因变量)
            HTuple qRes = new HTuple();

            // 准备数据列
            for (int index = 0; index < pointsCount; index++)
            {
                if (markPositions[index] == null || offsetVectors[index] == null)
                {
                    return Errortype.AVM_INPUT_POINT_NULL;
                }

                pColA.Append(markPositions[index].X);
                pColA.Append(markPositions[index].Y);
                pColB.Append(-markPositions[index].Y);
                pColB.Append(markPositions[index].X);
                pColC.Append(1.0);
                pColC.Append(0.0);
                pColD.Append(0.0);
                pColD.Append(1.0);
                qRes.Append(offsetVectors[index].X);
                qRes.Append(offsetVectors[index].Y);
            }

            //   A   B  C  D
            // | X, -Y, 1, 0 |
            // | Y,  X, 0, 1 |
            HOperatorSet.CreateMatrix(pointsCount * 2, 4, 1.0, out HTuple matrixRawABCD);
            HOperatorSet.CreateMatrix(pointsCount * 2, 1, pColA, out HTuple matrixColA);
            HOperatorSet.CreateMatrix(pointsCount * 2, 1, pColB, out HTuple matrixColB);
            HOperatorSet.CreateMatrix(pointsCount * 2, 1, pColC, out HTuple matrixColC);
            HOperatorSet.CreateMatrix(pointsCount * 2, 1, pColD, out HTuple matrixColD);
            HOperatorSet.SetSubMatrix(matrixRawABCD, matrixColA, 0, 0);
            HOperatorSet.SetSubMatrix(matrixRawABCD, matrixColB, 0, 1);
            HOperatorSet.SetSubMatrix(matrixRawABCD, matrixColC, 0, 2);
            HOperatorSet.SetSubMatrix(matrixRawABCD, matrixColD, 0, 3);

            //  qRes
            // | ΔX |
            // | ΔY |
            HOperatorSet.CreateMatrix(pointsCount * 2, 1, qRes, out HTuple matrixOffset);

            // 矩阵求解:
            //                   | Magni |
            // | X, -Y, 1, 0 | * |  Sinθ | = | ΔX |
            // | Y,  X, 0, 1 |   |   Tx  |   | ΔY |
            //                   |   Ty  |
            HOperatorSet.SolveMatrix(matrixRawABCD, "general", 0, matrixOffset, out HTuple matrixRes);

            // 得到未知系数矩阵 matrixRes：
            // | Magni |
            // |  Sinθ |
            // |   Tx  |
            // |   Ty  |
            HOperatorSet.GetValueMatrix(matrixRes, 0, 0, out HTuple hMagnificationXY);  // Magni （放大倍率）
            HOperatorSet.GetValueMatrix(matrixRes, 1, 0, out HTuple hCoffTheta);        // Sinθ
            HOperatorSet.TupleAsin(hCoffTheta, out HTuple theta);                       // θ(rad)
            HOperatorSet.GetValueMatrix(matrixRes, 2, 0, out HTuple hTx);
            HOperatorSet.GetValueMatrix(matrixRes, 3, 0, out HTuple hTy);
            double tansX = hTx.D;
            double tansY = hTy.D;
            double rotateRad = theta.D;
            double runOut = hMagnificationXY.D;
            outPutModelParam.TransX = hTx.D;
            outPutModelParam.TransY = hTy.D;
            outPutModelParam.RotationUrad = rotateRad * 1000000;    // (rad->urad)
            outPutModelParam.RunOut = runOut * 1000000;         // (1->ppm)

            double avg = 0.0;
            double sse = 0.0;
            double ssr = 0.0;
            double sst = 0.0;
            double rSpuare = 0.0;   // R² = SSE/SST
            List<Point> predictOffset = new List<Point>();
            for (int index = 0; index < pointsCount; index++)
            {
                // 统计观测偏移量均值
                avg += offsetVectors[index].X;
                avg += offsetVectors[index].Y;

                // 将每个点带入公式计算残差
                double deltaX = tansX + (runOut * markPositions[index].X) - (Math.Sin(rotateRad) * markPositions[index].Y);
                double deltaY = tansY + (runOut * markPositions[index].Y) + (Math.Sin(rotateRad) * markPositions[index].X);
                predictOffset.Add(new Point(deltaX, deltaY));       // 模型预测值
                double residualX = offsetVectors[index].X - deltaX; //  残差=观测值-模型预测值
                double residualY = offsetVectors[index].Y - deltaY; //  残差=观测值-模型预测值

                modelResidual.Add(new Point(residualX, residualY));
            }

            avg = avg / pointsCount / 2;

            for (int index = 0; index < pointsCount; index++)
            {
                // Sum Square of Residual 模型残差
                ssr += Math.Pow(modelResidual[index].X, 2) + Math.Pow(modelResidual[index].Y, 2);

                // Sum Squares of model 模型预测偏离值
                sse += Math.Pow(predictOffset[index].X - avg, 2) + Math.Pow(predictOffset[index].Y - avg, 2);

                // Sum Squares of True 观测偏离值
                sst += Math.Pow(offsetVectors[index].X - avg, 2) + Math.Pow(offsetVectors[index].Y - avg, 2);
            }

            rSpuare = sse / sst;    // 模型预测偏离值 / 观测偏离值 (值越接近 1.0 模型越符合数据分布)
            outPutModelParam.GoodnessOfFit = rSpuare * 100; // 输出结果为百分数
            outPutModelParam.ModelError = ssr;
            residual = modelResidual;
            return Errortype.OK;
        }

        /// <summary>
        /// AVM 数据常规统计
        /// </summary>
        /// <param name="values">需要计算的数据</param>
        /// <param name="analysisValue">输出数据的算数统计量</param>
        /// <returns>COMMONALGO_POINTSET_NUM_ERROR：输入的数据为空</returns>
        public static Errortype CalcCommonSummary(List<double> values, out DataStatisticParam analysisValue)
        {
            return ComAlgo.CalcDataSummary(values, out analysisValue);
        }

        /// <summary>
        /// AVM检测阵列坐标生成
        /// </summary>
        /// <param name="waferRadius">wafer半径</param>
        /// <param name="exclusive">外边距</param>
        /// <param name="intervalXY">行列间距</param>
        /// <param name="shiftXY">平移量XY</param>
        /// <param name="outPutPositions">输出阵列点集</param>
        /// <param name="refill">是否填充平移空白部分</param>
        /// <param name="outPutOrder">输出阵列顺序</param>
        /// <returns>AVM_SOPTMAP_PARAM_ERROR：输入参数错误</returns>
        public static Errortype GenMeasureArray(double waferRadius, double exclusive, Point intervalXY, Point shiftXY, out List<Point> outPutPositions, bool refill = true,
            AVMMeasureArrayOrder outPutOrder = AVMMeasureArrayOrder.LeftUP)
        {
            outPutPositions = new List<Point>();
            HTuple shiftX = new HTuple(shiftXY.X);
            HTuple shiftY = new HTuple(shiftXY.Y);
            HTuple distX = new HTuple();
            HTuple distY = new HTuple();
            HTuple powX = new HTuple();
            HTuple powY = new HTuple();
            HTuple distSquare = new HTuple();
            HTuple areaR = new HTuple(waferRadius - exclusive); // 去除边距后的半径
            if (areaR < 1)
            {
                return Errortype.AVM_SOPTMAP_PARAM_ERROR;
            }

            HObject imageX, imageY, domain;
            HOperatorSet.GenEmptyObj(out imageX);
            HOperatorSet.GenEmptyObj(out imageY);
            HOperatorSet.GenEmptyObj(out domain);

            // 计算行列格数(以wafer中心为原点，上下左右划分)
            int sectionNumXRight = (int)(waferRadius / intervalXY.X);
            int sectionNumYUp = (int)(waferRadius / intervalXY.Y);

            // 填充平移空白
            if (refill)
            {
                sectionNumXRight = (int)((waferRadius + shiftXY.X) / intervalXY.X);
                sectionNumYUp = (int)((waferRadius + shiftXY.Y) / intervalXY.Y);
            }

            int sectionNumXLeft = -sectionNumXRight;
            int sectionNumYDown = -sectionNumYUp;
            if (intervalXY.DistanceTo(new Point(0.0, 0.0)) == 0)
            {
                return Errortype.AVM_SOPTMAP_PARAM_ERROR;
            }

            // 计算阵列绘制点坐标范围
            double startX = sectionNumXLeft * intervalXY.X;
            double endX = sectionNumXRight * intervalXY.X;
            double startY = sectionNumYUp * intervalXY.Y;
            double endY = sectionNumYDown * intervalXY.Y;

            // 生成行坐标列坐标
            HOperatorSet.TupleGenSequence(startX + shiftXY.X, endX + shiftXY.X, intervalXY.X, out HTuple pointX);
            HOperatorSet.TupleGenSequence(startY + shiftXY.Y, endY + shiftXY.Y, -intervalXY.Y, out HTuple pointY);
            HOperatorSet.CreateMatrix(1, pointX.Length, pointX, out HTuple matrixSingleX);
            HOperatorSet.CreateMatrix(pointY.Length, 1, pointY, out HTuple matrixSingleY);
            HOperatorSet.RepeatMatrix(matrixSingleX, pointY.Length, 1, out HTuple matrixX);
            HOperatorSet.RepeatMatrix(matrixSingleY, 1, pointX.Length, out HTuple matrixY);
            HOperatorSet.GetFullMatrix(matrixX, out HTuple valuesX);
            HOperatorSet.GetFullMatrix(matrixY, out HTuple valuesY);

            // *****根据 outPutOrder 整理顺序*****
            // 将矩阵转为图像
            HOperatorSet.GetSizeMatrix(matrixX, out HTuple matrixHeight, out HTuple matrixWidth);
            HOperatorSet.GenImageConst(out imageX, "real", matrixWidth, matrixHeight);
            HOperatorSet.GenImageConst(out imageY, "real", matrixWidth, matrixHeight);
            HOperatorSet.GetDomain(imageX, out domain);
            HOperatorSet.GetRegionPoints(domain, out HTuple pixRows, out HTuple pixCols);
            HOperatorSet.SetGrayval(imageX, pixRows, pixCols, valuesX);
            HOperatorSet.SetGrayval(imageY, pixRows, pixCols, valuesY);

            // 镜像翻转，改变顺序
            if (outPutOrder == AVMMeasureArrayOrder.RightDown || outPutOrder == AVMMeasureArrayOrder.RightUP)
            {
                HOperatorSet.MirrorImage(imageX, out imageX, "column");
                HOperatorSet.MirrorImage(imageY, out imageY, "column");
            }

            if (outPutOrder == AVMMeasureArrayOrder.RightDown || outPutOrder == AVMMeasureArrayOrder.LeftDown)
            {
                HOperatorSet.MirrorImage(imageX, out imageX, "row");
                HOperatorSet.MirrorImage(imageY, out imageY, "row");
            }

            // 获取行列坐标
            HOperatorSet.GetGrayval(imageX, pixRows, pixCols, out valuesX);
            HOperatorSet.GetGrayval(imageY, pixRows, pixCols, out valuesY);

            // 判断坐标是否在wafer上
            HTuple preSelectedX = valuesX;
            HTuple preSelectedY = valuesY;

            // 不填充平移空白时需要单独建立一个圆形点阵区域
            if (!refill)
            {
                distX = valuesX - shiftX;
                distY = valuesY - shiftY;
                HOperatorSet.TuplePow(distX, 2, out powX);
                HOperatorSet.TuplePow(distY, 2, out powY);
                distSquare = powX + powY;
                HTuple preSelectMask = distSquare / areaR / areaR;
                HOperatorSet.TupleInt(preSelectMask, out preSelectMask);
                HOperatorSet.TupleNot(preSelectMask, out preSelectMask);
                HOperatorSet.TupleSelectMask(valuesX, preSelectMask, out preSelectedX);
                HOperatorSet.TupleSelectMask(valuesY, preSelectMask, out preSelectedY);
            }

            // 剔除wafer之外的点
            distX = preSelectedX;
            distY = preSelectedY;
            HOperatorSet.TuplePow(distX, 2, out powX);
            HOperatorSet.TuplePow(distY, 2, out powY);
            distSquare = powX + powY;
            HTuple selectMask = distSquare / areaR / areaR;
            HOperatorSet.TupleInt(selectMask, out selectMask);
            HOperatorSet.TupleNot(selectMask, out selectMask);
            HOperatorSet.TupleSelectMask(preSelectedX, selectMask, out HTuple selectedX);
            HOperatorSet.TupleSelectMask(preSelectedY, selectMask, out HTuple selectedY);

            // 整理结果
            List<Point> selectedPoint = new List<Point>();
            for (int index = 0; index < selectedX.Length; index++)
            {
                selectedPoint.Add(new Point(selectedX[index].D, selectedY[index].D));
            }

            outPutPositions = selectedPoint;
            imageX.Dispose();
            imageY.Dispose();
            domain.Dispose();
            return Errortype.OK;
        }
    }
}
