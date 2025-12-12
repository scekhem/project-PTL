using DataStruct;
using HalconDotNet;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.FusionCollections;
using static System.Net.Mime.MediaTypeNames;
using Point = DataStruct.Point;
using Region = DataStruct.Region;

namespace UltrapreciseBonding.UltrapreciseAlgorithm
{
    /// <summary>
    /// 通用算法类
    /// </summary>
    public static class ComAlgo
    {
        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        /// <param name="a">输入点1</param>
        /// <param name="b">输出点2</param>
        /// <returns>两点之间距离</returns>
        public static double Dist(Point a, Point b)
        {
            if (a is null || b is null)
            {
                return 0;
            }

            return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        /// <summary>
        /// 计算两线之间的距离
        /// </summary>
        /// <param name="line1">直线1</param>
        /// <param name="line2">直线2</param>
        /// <param name="isLL">是否是直线到直线，若是线段到线段则为false</param>
        /// <returns>两线之间的距离</returns>
        public static double Dist(LineSeg line1, LineSeg line2, bool isLL = true)
        {
            if (line1 == null || line2 == null)
            {
                return 0;
            }

            double[] rowArray = new double[] { line1.Start_Y, line1.End_Y };
            double[] colArray = new double[] { line1.Start_X, line1.End_X };
            HOperatorSet.GenContourPolygonXld(out HObject contour1, rowArray, colArray);

            rowArray = new double[] { line2.Start_Y, line2.End_Y };
            colArray = new double[] { line2.Start_X, line2.End_X };
            HOperatorSet.GenContourPolygonXld(out HObject contour2, rowArray, colArray);

            HTuple distMin;
            HTuple distMax;
            if (isLL)
            {
                HOperatorSet.DistanceLc(contour1, line2.Start_Y, line2.Start_X, line2.End_Y, line2.End_X, out distMin, out distMax);
            }
            else
            {
                HOperatorSet.DistanceCc(contour1, contour2, "point_to_segment", out distMin, out distMax);
            }

            contour1.Dispose();
            contour2.Dispose();

            return distMin.D;
        }

        /// <summary>
        /// 数据常规统计
        /// </summary>
        /// <param name="values">输入要计算的数值列表</param>
        /// <param name="analysisValue">输出计算结果</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype CalcDataSummary(List<double> values, out DataStatisticParam analysisValue)
        {
            analysisValue = new DataStatisticParam();
            if (values == null)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            //加入列表没有元素的情况
            if (values.Count == 0)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            double[] inputValue = values.ToArray();
            HOperatorSet.TupleMin(inputValue, out HTuple minValue);
            analysisValue.Min = minValue.D;
            HOperatorSet.TupleMax(inputValue, out HTuple maxValue);
            analysisValue.Max = maxValue.D;
            HOperatorSet.TupleMean(inputValue, out HTuple meanValue);
            analysisValue.Mean = meanValue.D;
            HOperatorSet.TupleMedian(inputValue, out HTuple medianValue);
            analysisValue.Median = medianValue.D;

            //HOperatorSet.TupleDeviation(inputValue, out HTuple deviation);//标准差的计算出现问题，例如1、2、3、4、5的标准差为根号2（1.414），而这个算出的是1.58114
            HOperatorSet.TuplePow(inputValue - meanValue, 2, out HTuple powValue);
            HOperatorSet.TupleSum(powValue, out HTuple sumValue);
            HTuple deviation = Math.Sqrt(sumValue / values.Count);
            analysisValue.Sigma3 = 3 * deviation;
            analysisValue.MeanSigma3 = Math.Abs(analysisValue.Mean) + analysisValue.Sigma3;
            analysisValue.Range = analysisValue.Max - analysisValue.Min;

            return Errortype.OK;
        }

        /// <summary>
        /// 数据常规统计
        /// </summary>
        /// <param name="points">输入点坐标</param>
        /// <param name="analysisX">x方向统计结果</param>
        /// <param name="analysisY">y方向统计结果</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype CalcDataSummary(List<Point> points, out DataStatisticParam analysisX, out DataStatisticParam analysisY)
        {
            analysisX = new DataStatisticParam();
            analysisY = new DataStatisticParam();
            List<double> inputPtX = new List<double>();
            List<double> inputPtY = new List<double>();
            if (points == null)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            for (int index = 0; index < points.Count; index++)
            {
                inputPtX.Add(points[index].X);
                inputPtY.Add(points[index].Y);
            }

            CalcDataSummary(inputPtX, out analysisX);
            CalcDataSummary(inputPtY, out analysisY);

            return Errortype.OK;
        }

        /// <summary>
        /// 检查点集是否共线
        /// </summary>
        /// <param name="points">输入的点集</param>
        /// <returns>是否共线true：不共线；false:共线</returns>
        public static bool CheckUnColline(List<Point> points)
        {
            if (points is null)
            {
                return false;
            }

            int nonCollineNum = points.Count;
            if (points.Count < 2)
            {
                return false;
            }

            Point basePoint = points[0];
            List<double> angles = new List<double>();
            for (int index = 1; index < points.Count; index++)
            {
                Point vect = points[index] - basePoint;
                if (vect != new Point(0, 0))
                {
                    HOperatorSet.LineOrientation(basePoint.Y, basePoint.X, points[index].Y, points[index].X, out HTuple phi);

                    if (!angles.Contains(phi))
                    {
                        angles.Add(phi);
                    }
                }

                if (angles.Count > 1)
                {
                    return true;
                }
            }

            if (angles.Count < 2)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// 检查点集是否共线
        /// </summary>
        /// <param name="points">输入的点集</param>
        /// <returns>是否共线，共线：false,不共线：true</returns>
        public static bool CheckUnColLines(List<Point> points)
        {
            if (points is null)
            {
                return false;
            }

            int nonCollineNum = points.Count;
            if (points.Count < 2)
            {
                return false;
            }

            Point basePoint = points[0];
            List<double> angles = new List<double>();
            for (int index = 0; index < points.Count; index++)
            {
                for (int i = index + 1; i < points.Count; i++)
                {
                    Point vect = points[i] - points[index];
                    double angle = Math.Atan2(vect.Y, vect.X);
                }

                if (angles.Count > 1)
                {
                    return true;
                }
            }

            if (angles.Count < 2)
            {
                return false;
            }

            return false;
        }

        /// <summary>
        /// 计算仿射变换矩阵
        /// </summary>
        /// <param name="src">原始数据</param>
        /// <param name="dst">目标数据</param>
        /// <param name="homMat2d">变换矩阵</param>
        /// <param name="error">误差</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype VectorToHomMat2d(List<Point> src, List<Point> dst, out double[] homMat2d, out List<Point> error)
        {
            homMat2d = new double[0];
            error = new List<Point>();
            if (src == null || dst == null)
            {
                return Errortype.HOMMAT_AFFINE_INPUT_NULL;
            }

            if (src.Count != dst.Count)
            {
                return Errortype.HOMMAT_AFFINE_INPUT_SIZE_NOT_ERUQL;
            }

            if (src.Count <= 2)
            {
                return Errortype.HOMMAT_AFFINE_INPUT_SIZE_NOT_ENOUGH;
            }

            if (!CheckUnColline(src) || !CheckUnColline(dst))
            {
                return Errortype.HOMMAT_AFFINE_POINT_COLLINE;
            }

            double[] srcX = new double[src.Count];
            double[] srcY = new double[src.Count];
            double[] dstX = new double[src.Count];
            double[] dstY = new double[src.Count];
            for (int i = 0; i < src.Count; i++)
            {
                srcX[i] = src[i].X;
                srcY[i] = src[i].Y;
                dstX[i] = dst[i].X;
                dstY[i] = dst[i].Y;
            }

            HOperatorSet.VectorToHomMat2d(srcX, srcY, dstX, dstY, out HTuple hHomMat2d);
            homMat2d = hHomMat2d.ToDArr();

            for (int i = 0; i < src.Count; i++)
            {
                HOperatorSet.AffineTransPoint2d(hHomMat2d, src[i].X, src[i].Y, out HTuple x, out HTuple y);
                error.Add(new Point(x.D - dst[i].X, y.D - dst[i].Y));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算相似变换矩阵（旋转平移缩放）
        /// </summary>
        /// <param name="src">原始数据</param>
        /// <param name="dst">目标数据</param>
        /// <param name="homMat2d">变换矩阵</param>
        /// <param name="error">误差</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype VectorToSimilarity(List<Point> src, List<Point> dst, out double[] homMat2d, out List<Point> error)
        {
            homMat2d = new double[6] { 1, 0, 0, 0, 1, 0 };
            error = new List<Point>();
            if (src == null || dst == null)
            {
                return Errortype.HOMMAT_SIMILARITY_INPUT_NULL;
            }

            if (src.Count != dst.Count)
            {
                return Errortype.HOMMAT_SIMILARITY_INPUT_SIZE_NOT_ERUQL;
            }

            if (src.Count < 2)
            {
                return Errortype.HOMMAT_SIMILARITY_INPUT_SIZE_NOT_ENOUGH;
            }

            double[] srcX = new double[src.Count];
            double[] srcY = new double[src.Count];
            double[] dstX = new double[src.Count];
            double[] dstY = new double[src.Count];
            for (int i = 0; i < src.Count; i++)
            {
                srcX[i] = src[i].X;
                srcY[i] = src[i].Y;
                dstX[i] = dst[i].X;
                dstY[i] = dst[i].Y;
            }

            HOperatorSet.VectorToSimilarity(srcX, srcY, dstX, dstY, out HTuple hHomMat2d);
            homMat2d = hHomMat2d.ToDArr();

            for (int i = 0; i < src.Count; i++)
            {
                HOperatorSet.AffineTransPoint2d(hHomMat2d, src[i].X, src[i].Y, out HTuple x, out HTuple y);
                error.Add(new Point(x.D - dst[i].X, y.D - dst[i].Y));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算刚体变换（旋转平移）
        /// </summary>
        /// <param name="src">原始数据</param>
        /// <param name="dst">目标数据</param>
        /// <param name="homMat2d">变换矩阵</param>
        /// <param name="error">误差</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype VectorToRigid(List<Point> src, List<Point> dst, out double[] homMat2d, out List<Point> error)
        {
            homMat2d = new double[6] { 1, 0, 0, 0, 1, 0 };
            error = new List<Point>();
            if (src == null || dst == null)
            {
                return Errortype.HOMMAT_RIGID_INPUT_NULL;
            }

            if (src.Count != dst.Count)
            {
                return Errortype.HOMMAT_RIGID_INPUT_SIZE_NOT_ERUQL;
            }

            if (src.Count < 2)
            {
                return Errortype.HOMMAT_RIGID_INPUT_SIZE_NOT_ENOUGH;
            }

            HashSet<Point> uniqueSrc = new HashSet<Point>(src);
            if (uniqueSrc.Count < 2)
            {
                return Errortype.HOMMAT_RIGID_INPUT_SIZE_NOT_ENOUGH;
            }

            HashSet<Point> uniqueDst = new HashSet<Point>(dst);
            if (uniqueDst.Count < 2)
            {
                return Errortype.HOMMAT_RIGID_INPUT_SIZE_NOT_ENOUGH;
            }

            double[] srcX = new double[src.Count];
            double[] srcY = new double[src.Count];
            double[] dstX = new double[src.Count];
            double[] dstY = new double[src.Count];
            for (int i = 0; i < src.Count; i++)
            {
                srcX[i] = src[i].X;
                srcY[i] = src[i].Y;
                dstX[i] = dst[i].X;
                dstY[i] = dst[i].Y;
            }

            HOperatorSet.VectorToRigid(srcX, srcY, dstX, dstY, out HTuple hHomMat2d);
            homMat2d = hHomMat2d.ToDArr();

            for (int i = 0; i < src.Count; i++)
            {
                HOperatorSet.AffineTransPoint2d(hHomMat2d, src[i].X, src[i].Y, out HTuple x, out HTuple y);
                error.Add(new Point(x.D - dst[i].X, y.D - dst[i].Y));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 二维点坐标转换
        /// </summary>
        /// <param name="src">原始点</param>
        /// <param name="matrix">转换矩阵</param>
        /// <param name="dst">转换后的点</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype AffineTransPoint2d(Point src, double[] matrix, out Point dst)
        {
            dst = new Point();
            if (matrix == null)
            {
                return Errortype.COMMONALGO_MATRIX_INPUT_NULL;
            }

            if (src is null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            if (matrix.Length != 6)
            {
                return Errortype.COMMONALGO_MATRIX_LENGTH_ERROR;
            }

            HOperatorSet.AffineTransPoint2d(matrix, src.X, src.Y, out HTuple qx, out HTuple qy);
            dst.X = qx;
            dst.Y = qy;
            return Errortype.OK;
        }

        /// <summary>
        /// 二维点集坐标转换
        /// </summary>
        /// <param name="src">原始点集</param>
        /// <param name="matrix">转换矩阵</param>
        /// <param name="dst">转换后的点</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype AffineTransPoint2d(List<Point> src, double[] matrix, out List<Point> dst)
        {
            dst = new List<Point>();
            if (matrix == null)
            {
                return Errortype.COMMONALGO_MATRIX_INPUT_NULL;
            }

            if (src is null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            if (matrix.Length != 6)
            {
                return Errortype.COMMONALGO_MATRIX_LENGTH_ERROR;
            }

            for (int i = 0; i < src.Count; i++)
            {
                AffineTransPoint2d(src[i], matrix, out Point p);
                dst.Add(p);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算点集旋转中心（旋转矩阵求解，建议2对以上点,仅旋转)
        /// </summary>
        /// <param name="startPoints">旋转前点坐标</param>
        /// <param name="destPoints">旋转后点坐标</param>
        /// <param name="rotateCenter">输出旋转中心</param>
        /// <param name="error">输出计算残差</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype CalcRotateCenter(List<Point> startPoints, List<Point> destPoints, out Point rotateCenter, out List<double> error)
        {
            rotateCenter = new Point(0, 0);
            error = new List<double>();

            if (startPoints is null || destPoints is null)
            {
                rotateCenter = null;
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            if (startPoints.Count != destPoints.Count)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            if (startPoints.Count < 2)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            if (startPoints[0] is null || destPoints[0] is null)
            {
                rotateCenter = null;
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            double[] startX = new double[startPoints.Count];
            double[] startY = new double[startPoints.Count];
            double[] endX = new double[destPoints.Count];
            double[] endY = new double[destPoints.Count];
            for (int index = 0; index < startPoints.Count; index++)
            {
                if (startPoints[index] is null || destPoints[index] is null)
                {
                    rotateCenter = null;
                    return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
                }

                startX[index] = startPoints[index].X;
                startY[index] = startPoints[index].Y;
                endX[index] = destPoints[index].X;
                endY[index] = destPoints[index].Y;
            }

            // 先统计出旋转角
            HOperatorSet.TupleMean(startX, out HTuple baseCenterX);
            HOperatorSet.TupleMean(startY, out HTuple baseCenterY);
            HOperatorSet.TupleGenConst(startX.Length, baseCenterX, out HTuple baseXEnd);
            HOperatorSet.TupleGenConst(startX.Length, baseCenterY, out HTuple baseYEnd);
            HOperatorSet.TupleMean(endX, out HTuple targetCenterX);
            HOperatorSet.TupleMean(endY, out HTuple targetCenterY);
            HOperatorSet.TupleGenConst(startX.Length, targetCenterX, out HTuple targetXEnd);
            HOperatorSet.TupleGenConst(startX.Length, targetCenterY, out HTuple targetYEnd);

            HOperatorSet.AngleLl(startY, startX, baseYEnd, baseXEnd, endY, endX, targetYEnd, targetXEnd, out HTuple angleRadList);
            HOperatorSet.TupleNotEqualElem(angleRadList, 0, out HTuple notZero);
            HOperatorSet.TupleSelectMask(angleRadList, notZero, out angleRadList);
            HOperatorSet.TupleMean(-angleRadList, out HTuple angleRad);

            HOperatorSet.TupleSin(angleRad, out HTuple sin);
            HOperatorSet.TupleCos(angleRad, out HTuple cos);

            //// 绕点旋转矩阵求解旋转中心 Tx,Ty
            ////                   |       Cosθ        ,     Sinθ          , 0  |
            ////     | X, Y, 1 | * |      -Sinθ        ,     Cosθ          , 0  | = | X', Y', 1 |
            ////                   | (1-Cosθ)Tx+SinθTy , (1-Cosθ)Ty-SinθTx , 1  |
            ////
            ////    xCosθ - ySinθ + (1-Cosθ)Tx + SinθTy = x'
            ////    xSinθ + yCosθ + (1-Cosθ)Ty - SinθTx = y'
            //
            // 准备矩阵
            // a = xCosθ - ySinθ
            // b = xSinθ + yCosθ
            HTuple tuple_a = new HTuple((startX * cos) - (startY * sin));
            HTuple tuple_b = new HTuple((startX * sin) + (startY * cos));
            HOperatorSet.TupleGenConst(startX.Length, 1.0 - cos, out HTuple tuple1Cos);
            HOperatorSet.TupleGenConst(startX.Length, sin, out HTuple tupleSin);
            HOperatorSet.TupleConcat(tuple1Cos, -tupleSin, out HTuple tupleCol0);
            HOperatorSet.TupleConcat(tupleSin, tuple1Cos, out HTuple tupleCol1);

            HOperatorSet.CreateMatrix(tupleCol0.Length, 2, 1.0, out HTuple matrixXY);
            HOperatorSet.CreateMatrix(tupleCol0.Length, 1, tupleCol0, out HTuple matrixCol0);
            HOperatorSet.CreateMatrix(tupleCol0.Length, 1, tupleCol1, out HTuple matrixCol1);
            HOperatorSet.SetSubMatrix(matrixXY, matrixCol0, 0, 0);
            HOperatorSet.SetSubMatrix(matrixXY, matrixCol1, 0, 1);

            HOperatorSet.TupleConcat(endX - tuple_a, endY - tuple_b, out HTuple tupleResXY);
            HOperatorSet.CreateMatrix(tupleResXY.Length, 1, tupleResXY, out HTuple matrixXYTr);
            //// 转换为方程:
            ////     | (1-Cosθ),  Sinθ   | * | Tx| = | X'-a, Y'-b |
            ////     |  -Sinθ  , (1-Cosθ)|   | Ty|
            HOperatorSet.SolveMatrix(matrixXY, "general", 0, matrixXYTr, out HTuple matrixResult);

            HOperatorSet.GetValueMatrix(matrixResult, 0, 0, out HTuple centerX);
            HOperatorSet.GetValueMatrix(matrixResult, 1, 0, out HTuple centerY);
            rotateCenter = new Point(centerX.D, centerY.D);

            HOperatorSet.TupleDeg(angleRad, out HTuple angleDeg);
            for (int i = 0; i < startPoints.Count; i++)
            {
                CalcRotatePoint(startPoints[i], angleDeg.D, rotateCenter, out Point predictPoint);
                error.Add(destPoints[i].DistanceTo(predictPoint));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 线性插值方法查找中间值
        /// </summary>
        /// <param name="firstP">第一个点</param>
        /// <param name="secondP">第二个点</param>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype LinearInterpolation(KeyValuePair<double, double> firstP, KeyValuePair<double, double> secondP, double key, out double value)
        {
            if (secondP.Key == firstP.Key)
            {
                value = firstP.Value;
                return Errortype.OK;
            }

            value = (secondP.Value - firstP.Value) / (secondP.Key - firstP.Key) * (key - firstP.Key) + firstP.Value;
            return Errortype.OK;
        }

        /// <summary>
        /// 绕点旋转
        /// </summary>
        /// <param name="pointToRotate">待旋转的点</param>
        /// <param name="rotateDeg">旋转角度（角度制）</param>
        /// <param name="rotateCenter">旋转中心</param>
        /// <param name="pointRes">旋转后的点</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype CalcRotatePoint(Point pointToRotate, double rotateDeg, Point rotateCenter, out Point pointRes)
        {
            pointRes = new Point();
            if (pointToRotate is null || rotateCenter is null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            HTuple phi = Math.PI * rotateDeg / 180;

            //计算
            pointRes.X = (pointToRotate.X - rotateCenter.X) * Math.Cos(phi) - (pointToRotate.Y - rotateCenter.Y) * Math.Sin(phi) + rotateCenter.X;
            pointRes.Y = (pointToRotate.Y - rotateCenter.Y) * Math.Cos(phi) + (pointToRotate.X - rotateCenter.X) * Math.Sin(phi) + rotateCenter.Y;
            return Errortype.OK;
        }

        /// <summary>
        /// 直线拟合
        /// </summary>
        /// <param name="rows">点行值</param>
        /// <param name="cols">点列值</param>
        /// <param name="line">返回的线的值</param>
        /// <param name="straightnessError">直线度误差</param>
        /// <param name="iterations">迭代次数</param>
        /// <param name="clippingFactor">剔除异常因子</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype FitLine(double[] rows, double[] cols, out LineSeg line, out double[] straightnessError, int iterations = 5, int clippingFactor = 2)
        {
            line = new LineSeg();
            straightnessError = null;
            if (rows.Length != cols.Length)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_NOT_EQUAL;
            }

            if (rows.Length < 2)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            HOperatorSet.GenContourPolygonXld(out HObject contour, rows, cols);
            HOperatorSet.FitLineContourXld(contour, "tukey", -1, 0, iterations, clippingFactor, out HTuple rowBegin, out HTuple colBegin, out HTuple rowEnd, out HTuple colEnd, out HTuple nr, out HTuple nc, out HTuple dist);

            line.Start_X = colBegin;
            line.Start_Y = rowBegin;
            line.End_X = colEnd;
            line.End_Y = rowEnd;

            if (line.End_X != line.Start_X)
            {
                // ax+by+c = 0   dist = |ax+by+c| / sqrt(a^2+b^2)
                double a = (line.End_Y - line.Start_Y) / (line.End_X - line.Start_X);
                double b = -1;
                double c = line.Start_Y - a * line.Start_X;

                straightnessError = new double[rows.Length];
                for (int i = 0; i < rows.Length; i++)
                {
                    straightnessError[i] = (a * cols[i] + b * rows[i] + c) / Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
                }
            }
            else
            {
                straightnessError = new double[rows.Length];
                for (int i = 0; i < rows.Length; i++)
                {
                    straightnessError[i] = cols[i] - line.Start_X;
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 直线拟合
        /// </summary>
        /// <param name="points">输入点集</param>
        /// <param name="line">返回直线</param>
        /// <param name="straightnessError">直线度误差</param>
        /// <param name="iterations">迭代次数</param>
        /// <param name="clippingFactor">剔除异常值</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype FitLine(List<Point> points, out LineSeg line, out double[] straightnessError, int iterations = 5, int clippingFactor = 2)
        {
            line = new LineSeg();
            straightnessError = null;

            if (points == null)
            {
                return Errortype.COMMONALGO_FITLINE_POINTS_NULL;
            }

            if (points.Count < 2)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_NOT_EQUAL;
            }

            double[] rows = new double[points.Count];
            double[] cols = new double[points.Count];
            for (int i = 0; i < points.Count; ++i)
            {
                rows[i] = points[i].Y;
                cols[i] = points[i].X;
            }

            var ret = FitLine(rows, cols, out line, out straightnessError, iterations, clippingFactor);

            return ret;
        }

        /// <summary>
        /// 拟合直线 y = ax + b
        /// </summary>
        /// <param name="x">x值</param>
        /// <param name="y">y值</param>
        /// <param name="a">返回a值</param>
        /// <param name="b">返回b值</param>
        /// <param name="rSquare">返回rSquare值</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype FitLine(List<double> x, List<double> y, out double a, out double b, out double rSquare)
        {
            // 输入参数 点、a值、b值
            int pointsCount = x.Count;
            HOperatorSet.TupleGenConst(x.Count, 1.0, out HTuple matrix1Value);

            //   A   B 
            // | X,  1 |
            HOperatorSet.CreateMatrix(pointsCount, 2, 1.0, out HTuple matrixRaw);
            HOperatorSet.CreateMatrix(pointsCount, 1, x.ToArray(), out HTuple matrixColA);
            HOperatorSet.CreateMatrix(pointsCount, 1, matrix1Value, out HTuple matrixColB);
            HOperatorSet.SetSubMatrix(matrixRaw, matrixColA, 0, 0);
            HOperatorSet.SetSubMatrix(matrixRaw, matrixColB, 0, 1);

            //  qRes
            // | ΔY |
            HOperatorSet.CreateMatrix(pointsCount, 1, y.ToArray(), out HTuple matrixY);

            // 矩阵求解:
            // | X, 1 | * |  a  | = | Y |
            //            |  b  |
            HOperatorSet.SolveMatrix(matrixRaw, "general", 0, matrixY, out HTuple matrixRes);

            // 得到未知系数矩阵 matrixRes：
            // |   a   |
            // |   b   |
            HOperatorSet.GetValueMatrix(matrixRes, 0, 0, out HTuple aValue);
            HOperatorSet.GetValueMatrix(matrixRes, 1, 0, out HTuple bValue);

            //double a = 6.4377960;
            a = aValue.D;
            b = bValue.D;

            List<double> modelResidual = new List<double>();
            List<double> predictY = new List<double>();
            double rSpuare = 0.0;   // R² = SSE/SST
            double avgY = 0.0;
            double sse = 0.0;
            double ssr = 0.0;
            double sst = 0.0;

            for (int index = 0; index < x.Count; index++)
            {
                // 统计原始点Y均值
                avgY += y[index];

                // 将每个点带入直线公式计算残差
                double predictValue = a * x[index] + b;
                double residualY = y[index] - predictValue; //  残差=观测值-模型预测值

                predictY.Add(predictValue);
                modelResidual.Add(residualY);
            }

            avgY = avgY / x.Count;

            for (int index = 0; index < x.Count; index++)
            {
                // Sum Square of Residual 模型残差和
                ssr += Math.Pow(modelResidual[index], 2);

                // Sum Squares of model 模型预测偏离值
                sse += Math.Pow(predictY[index] - avgY, 2);

                // Sum Squares of True 观测偏离值
                sst += Math.Pow(y[index] - avgY, 2);
            }

            rSquare = sse / sst;    // 模型预测偏离值 / 观测偏离值 (值越接近 1.0 模型越符合数据分布)
            double sumSquare = (ssr + sse) / sst;

            return Errortype.OK;
        }

        /// <summary>
        /// 多点圆拟合
        /// </summary>
        /// <param name="points">输入点集</param>
        /// <param name="center">输出圆心坐标</param>
        /// <param name="radius">输出半径</param>
        /// <param name="error">输出误差</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype FitCircle(List<Point> points, out Point center, out double radius, out List<double> error)
        {
            center = new Point();
            radius = 0;
            error = new List<double>();

            if (points is null || points.Count < 2)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            bool colline = CheckUnColline(points);
            if (!colline)
            {
                return Errortype.FIT_CIRCLE_POINT_COLLINE;
            }

            double[] rows = new double[points.Count];
            double[] cols = new double[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                rows[i] = points[i].Y;
                cols[i] = points[i].X;
            }

            HOperatorSet.GenContourPolygonXld(out HObject contour, rows, cols);
            HOperatorSet.FitCircleContourXld(contour, "geometric", -1, 0, 0, 3, 2, out HTuple centerY, out HTuple centerX, out HTuple r, out HTuple startPhi, out HTuple endPhi, out HTuple pointOrder);
            center = new Point(centerX.D, centerY.D);
            radius = r.D;

            for (int i = 0; i < points.Count; i++)
            {
                double dist = Dist(points[i], center);
                error.Add(dist - radius);
            }

            bool savePoints = SaveFlg("SaveFitCircle", out int days);
            String path = @"D:\Alg\";
            string fileName = path + "FitCirclePoints.txt";

            string sep = " ";
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            string text = string.Empty;
            text += time + "\n ";

            if (savePoints)
            {
                if (!File.Exists(fileName))
                {
                    File.Create(fileName).Close();
                }

                for (int j = 0; j < rows.Length; ++j)
                {
                    text += "rows:" + " " + rows[j].ToString() + " " + "cols:" + " " + cols[j].ToString() + " " + "error:" + " " + error[j].ToString() + "\n ";
                }

                text += "\n pointsX:";
                for (int j = 0; j < rows.Length; ++j)
                {
                    text += cols[j].ToString() + ",";
                }

                text += "\n pointsY:";
                for (int j = 0; j < rows.Length; ++j)
                {
                    text += rows[j].ToString() + ",";
                }

                text += "\n";
            }

            FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();

            return Errortype.OK;
        }

        /// <summary>
        /// 多点拟合曲面
        /// </summary>
        /// <param name="points">输入采样点（XY为单位为像素）</param>
        /// <param name="mapSize">生成map图的直径</param>
        /// <param name="surfaceMap">输出map深度图</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype FitCurvedSurface(List<Point3D> points, int mapSize, out double[] surfaceMap)
        {
            surfaceMap = null;
            if (points is null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            if (points.Count == 0)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            double[] pointX = new double[points.Count];
            double[] pointY = new double[points.Count];
            double[] pointZ = new double[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                pointX[i] = points[i].X;
                pointY[i] = points[i].Y;
                pointZ[i] = points[i].Z;
            }

            HOperatorSet.InterpolateScatteredDataPointsToImage(out HObject surfaceImg, "thin_plate_splines", pointY, pointX, pointZ, mapSize, mapSize, new HTuple("alpha"), new HTuple(0));
            HOperatorSet.GetRegionPoints(surfaceImg, out HTuple rows, out HTuple cols);
            HOperatorSet.GetGrayval(surfaceImg, rows, cols, out HTuple heightValue);
            surfaceMap = heightValue.DArr;
            surfaceImg.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 求两直线交点
        /// </summary>
        /// <param name="line1">输入直线1</param>
        /// <param name="line2">输入直线2</param>
        /// <param name="point">输出交点</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype IntersectionLines(LineSeg line1, LineSeg line2, out Point point)
        {
            point = new Point();
            if (line1 == null || line2 == null)
            {
                return Errortype.COMMONALGO_INTERSECTIONLINES_NULL;
            }

            double angle1 = line1.CalculateLineDeg();
            double angle2 = line2.CalculateLineDeg();
            if (Math.Abs(angle1 - angle2) < 1e-6 || Math.Abs(Math.Abs(angle1 - angle2) - 180) < 1e-6)
            {
                return Errortype.COMMONALGO_INPUT_LINE_PARALLEL;
            }

            HOperatorSet.IntersectionLines(line1.Start_Y, line1.Start_X, line1.End_Y, line1.End_X, line2.Start_Y, line2.Start_X, line2.End_Y, line2.End_X, out HTuple row, out HTuple col, out HTuple isOverlapping);
            point.X = col.TupleSelect(0).D;
            point.Y = row.TupleSelect(0).D;
            return Errortype.OK;
        }

        /// <summary>
        /// 根据输入的rectangle获取合并的region
        /// </summary>
        /// <param name="inputRect">输入rect集合</param>
        /// <param name="regionUnion">合并的region</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GenRegionUnion(List<Rectangle1> inputRect, out HObject regionUnion)
        {
            HOperatorSet.GenEmptyRegion(out regionUnion);
            if (inputRect is null)
            {
                return Errortype.OK;
            }

            for (int index = 0; index < inputRect.Count; index++)
            {
                if (inputRect[index] == null)
                {
                    continue;
                }

                HOperatorSet.GenRectangle1(out HObject rectangle, inputRect[index].Start_Y, inputRect[index].Start_X, inputRect[index].End_Y, inputRect[index].End_X);
                HOperatorSet.Union2(regionUnion, rectangle, out HObject regionUnionT);
                regionUnion?.Dispose();
                regionUnion = regionUnionT.Clone();
                regionUnionT.Dispose();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 方形mark抓取(像素级)
        /// </summary>
        /// <param name="calibMark">图像</param>
        /// <param name="centerPoints">中心坐标</param>
        /// <param name="rectPhi">角度</param>
        /// <param name="rectWidth">方形宽度（全长）</param>
        /// <param name="rectHeight">方形高度（全长）</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GetRectMarkCenterPix(Camera calibMark, out List<Point> centerPoints, out List<double> rectPhi, out List<double> rectWidth, out List<double> rectHeight)
        {
            centerPoints = new List<Point>();
            rectPhi = new List<double>();
            rectWidth = new List<double>();
            rectHeight = new List<double>();
            if (calibMark is null)
            {
                return Errortype.COMMONALGO_GETRECTMARKCENTERPIX_IMG_NULL;
            }

            if ((calibMark.Height < 1) || (calibMark.Width < 1))
            {
                return Errortype.COMMONALGO_GETRECTMARKCENTERPIX_IMG_ERROR;
            }

            HObject hoImage = calibMark.GenHObject();
            HObject maxLarityRegion;
            HObject hoSelectedRect;
            HTuple numOfRectRegions;
            HTuple rectX;
            HTuple rectY;

            HOperatorSet.BinaryThreshold(hoImage, out HObject darkRegion, "smooth_histo", "dark", out HTuple usedthresh);
            HOperatorSet.Connection(darkRegion, out HObject connectedDarkRegion);
            HOperatorSet.OpeningCircle(connectedDarkRegion, out connectedDarkRegion, 10);     // 开运算radius目前暂定10

            // 筛选矩形区域
            HOperatorSet.SelectShape(connectedDarkRegion, out HObject selectedRegion, "rectangularity", "and", 0.75, 1);
            HOperatorSet.CountObj(selectedRegion, out HTuple numOfAlmostRegions);
            HTuple rectlarity = new HTuple();
            for (int regionIndex = 0; regionIndex < numOfAlmostRegions; regionIndex++)
            {
                HOperatorSet.SelectObj(selectedRegion, out HObject singleRegion, regionIndex + 1);
                HOperatorSet.Rectangularity(singleRegion, out HTuple singleLarity);
                HOperatorSet.TupleConcat(rectlarity, singleLarity, out rectlarity);
                singleRegion.Dispose();
            }

            if (numOfAlmostRegions < 1)
            {
                return Errortype.COMMONALGO_GETRECTMARKCENTERPIX_RECTREGION_ZERO;
            }

            HOperatorSet.TupleMax(rectlarity, out HTuple maxLarity);
            HOperatorSet.TupleFind(rectlarity, maxLarity, out HTuple indicesMax);
            HOperatorSet.SelectObj(selectedRegion, out maxLarityRegion, indicesMax[0] + 1);
            HOperatorSet.AreaCenter(maxLarityRegion, out HTuple areaThresh, out HTuple rectCenterAlmostRow, out HTuple rectCenterAlmostCol);
            HOperatorSet.SelectShape(selectedRegion, out hoSelectedRect, "area", "and", areaThresh * 0.85, areaThresh * 1.15);
            HOperatorSet.CountObj(hoSelectedRect, out numOfRectRegions);
            if (numOfRectRegions < 1)
            {
                return Errortype.COMMONALGO_GETRECTMARKCENTERPIX_RECTREGION_ZERO;
            }

            HOperatorSet.SmallestRectangle2(hoSelectedRect, out rectY, out rectX, out HTuple hoRectPhi, out HTuple rectLen1, out HTuple rectLen2);

            for (int index = 0; index < rectY.TupleLength(); index++)
            {
                centerPoints.Add(new Point(rectX[index].D, rectY[index].D));
                rectPhi.Add(hoRectPhi.D);
                rectWidth.Add(rectLen1.D * 2);
                rectHeight.Add(rectLen2.D * 2);
            }

            hoImage.Dispose();
            darkRegion.Dispose();
            connectedDarkRegion.Dispose();
            selectedRegion.Dispose();
            maxLarityRegion.Dispose();
            hoSelectedRect.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 圆形mark抓取(像素级)
        /// </summary>
        /// <param name="calibMark">图像</param>
        /// <param name="centerPoints">输出中心坐标</param>
        /// <param name="circleRadius">输出圆心半径</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GetCircleMarkCenterPix(Camera calibMark, out List<Point> centerPoints, out List<double> circleRadius)
        {
            centerPoints = new List<Point>();
            circleRadius = new List<double>();
            if (calibMark is null)
            {
                return Errortype.CALIPER_CIRCLE_NULL;
            }

            if ((calibMark.Height < 1) || (calibMark.Width < 1))
            {
                return Errortype.CALIPER_CIRCLE_NULL;
            }

            HObject ho_Image = calibMark.GenHObject();
            HOperatorSet.Rgb1ToGray(ho_Image, out ho_Image);
            HTuple hv_CircleX;
            HTuple hv_CircleY;

            HOperatorSet.BinaryThreshold(ho_Image, out HObject ho_darkRegion, "max_separability", "light", out HTuple hv_usedthresh);
            HOperatorSet.Connection(ho_darkRegion, out HObject ho_connectedLightRegion);
            HOperatorSet.OpeningCircle(ho_connectedLightRegion, out ho_connectedLightRegion, 5);     // 开运算radius目前暂定10

            // 筛选圆形区域
            HOperatorSet.SelectShape(ho_connectedLightRegion, out HObject ho_selectedRegion, "circularity", "and", 0.8, 1);
            HOperatorSet.SelectShape(ho_selectedRegion, out ho_selectedRegion, "area", "and", 150, calibMark.Width * calibMark.Height);
            HOperatorSet.CountObj(ho_selectedRegion, out HTuple numOfAlmostRegions);
            if (numOfAlmostRegions.I < 1)
            {
                return Errortype.COMMONALGO_MARK_SEARCH_FAIL;
            }

            HOperatorSet.SmallestRectangle2(ho_selectedRegion, out hv_CircleY, out hv_CircleX, out HTuple hv_CirlclePhi, out HTuple hv_CircleR1, out HTuple hv_CircleR2);

            for (int index = 0; index < hv_CircleY.TupleLength(); index++)
            {
                centerPoints.Add(new Point(hv_CircleX[index].D, hv_CircleY[index].D));
                circleRadius.Add(hv_CircleR1.D);
            }

            ho_Image.Dispose();
            ho_darkRegion.Dispose();
            ho_connectedLightRegion.Dispose();
            ho_selectedRegion.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 标定板Mark粗定位（已排序）
        /// </summary>
        /// <param name="calibMark">输入图像</param>
        /// <param name="centerPoints">输出中心坐标</param>
        /// <param name="circleRadius">输出圆心半径</param>
        /// <param name="region">框选区域</param>
        /// <param name="lightOrDark">亮暗</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GetCircleMarkCenter(Camera calibMark, out List<Point> centerPoints, out List<double> circleRadius, Region region = null, string lightOrDark = "light")
        {
            centerPoints = new List<Point>();
            circleRadius = new List<double>();
            if (calibMark is null)
            {
                return Errortype.COMMONALGO_GETCIRCLEMARKCENTER_INPUT_CIRCLE_NULL;
            }

            if ((calibMark.Height < 1) || (calibMark.Width < 1))
            {
                return Errortype.COMMONALGO_GETCIRCLEMARKCENTER_INPUT_CIRCLE_ERROR;
            }

            HOperatorSet.GenEmptyObj(out HObject regionObject);
            HOperatorSet.GenRectangle1(out HObject rectangle, 0, 0, calibMark.Height, calibMark.Width);
            regionObject = region == null ? rectangle : region.GenRegion();

            HObject ho_Image = calibMark.GenHObject();
            HOperatorSet.ReduceDomain(ho_Image, regionObject, out HObject reduceImage);
            HOperatorSet.Rgb1ToGray(reduceImage, out ho_Image);

            HTuple hv_CircleX;
            HTuple hv_CircleY;

            HOperatorSet.BinaryThreshold(ho_Image, out HObject ho_darkRegion, "max_separability", lightOrDark, out HTuple hv_usedthresh);
            HOperatorSet.FillUp(ho_darkRegion, out HObject fillUpRegion); //针对圆环填充
            HOperatorSet.Union1(fillUpRegion, out HObject unionRegion);
            HOperatorSet.Connection(unionRegion, out HObject ho_connectedLightRegion);
            HOperatorSet.OpeningCircle(ho_connectedLightRegion, out ho_connectedLightRegion, 7.5);

            // 筛选圆形区域
            HOperatorSet.SelectShape(ho_connectedLightRegion, out HObject ho_selectedRegion, "circularity", "and", 0.8, 1);
            HOperatorSet.SelectShape(ho_selectedRegion, out ho_selectedRegion, "area", "and", 150, calibMark.Height * calibMark.Width);
            HOperatorSet.CountObj(ho_selectedRegion, out HTuple numOfAlmostRegions);
            if (numOfAlmostRegions.I < 1)
            {
                return Errortype.COMMONALGO_MARK_SEARCH_FAIL;
            }

            HOperatorSet.SortRegion(ho_selectedRegion, out HObject ho_sortedRegions, "character", "true", "row"); //排序
            HOperatorSet.SmallestRectangle2(ho_sortedRegions, out hv_CircleY, out hv_CircleX, out HTuple hv_CirlclePhi, out HTuple hv_CircleR1, out HTuple hv_CircleR2);

            for (int index = 0; index < hv_CircleY.TupleLength(); index++)
            {
                centerPoints.Add(new Point(hv_CircleX[index].D, hv_CircleY[index].D));
                circleRadius.Add(hv_CircleR1[index].D);
            }

            ho_Image.Dispose();
            ho_darkRegion.Dispose();
            fillUpRegion.Dispose();
            unionRegion.Dispose();
            ho_connectedLightRegion.Dispose();
            ho_selectedRegion.Dispose();
            ho_sortedRegions.Dispose();
            reduceImage.Dispose();
            regionObject.Dispose();
            rectangle.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 获取圆形mark中心像素坐标（卡尺精定位）
        /// </summary>
        /// <param name="circleMarkImg">输入标定板图像</param>
        /// <param name="centerPixPoint">输出像素坐标</param>
        /// <param name="selectRegion">框选区域</param>
        /// <param name="lightOrDark">亮暗</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GetMarkCenterPix(Camera circleMarkImg, out List<Point> centerPixPoint, Region selectRegion = null, string lightOrDark = "light")
        {
            if (SaveFlg("GetMarkCenterPixImg", out int days))
            {
                string path = @"D:\Alg\GetMarkCenterPixImg";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = now.ToString("yyyyMMddHHmmss") + milliseconds.ToString("D3");
                string name = path + "\\" + time + ".bmp";
                circleMarkImg.Save(name);
            }

            centerPixPoint = new List<Point>();
            if (circleMarkImg.Width < 1 || circleMarkImg.Height < 1)
            {
                return Errortype.COMMONALGO_GETMARKCENTERPIX_INPUT_IMG_NULL;
            }

            var retCircle = GetCircleMarkCenter(circleMarkImg, out List<Point> circleCenter, out List<double> circleRadius, selectRegion, lightOrDark);
            if (retCircle != Errortype.OK)
            {
                return retCircle;
            }

            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = 0.8;
            calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
            calipParam.MeasureLength1 = 20;
            calipParam.MeasureLength2 = 4;
            calipParam.NumMeasures = 120;
            calipParam.MeasureThreshold = 20;
            calipParam.MeasureSelect = "all";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "uniform";

            for (int index = 0; index < circleCenter.Count; index++)
            {
                var ret = CaliperCircle.CircleExtraction(circleMarkImg, circleCenter[index], circleRadius[index], calipParam, out Point centerOut, out double radius);
                if (ret != Errortype.OK)
                {
                    continue;
                }

                centerPixPoint.Add(new Point(centerOut.X, centerOut.Y));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取视野中唯一的圆像素坐标(使用轮廓进行定位)
        /// </summary>
        /// <param name="circleMarkImg">输入圆</param>
        /// <param name="centerPixPoint">输出圆</param>
        /// <param name="selectRegion">识别区域</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GetMarkCenterByContour(Camera circleMarkImg, out Point centerPixPoint, Region selectRegion = null)
        {
            if (SaveFlg("GetMarkCenterByContour", out int days))
            {
                string path = @"D:\Alg\GetMarkCenterByContour";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = now.ToString("yyyyMMddHHmmss") + milliseconds.ToString("D3");
                string name = path + "\\" + time + ".bmp";
                circleMarkImg.Save(name);
            }

            centerPixPoint = new Point();
            if (circleMarkImg.Width < 1 || circleMarkImg.Height < 1)
            {
                return Errortype.COMMONALGO_GETMARKCENTERPIX_INPUT_IMG_NULL;
            }

            HOperatorSet.GenEmptyObj(out HObject regionObject);
            HOperatorSet.GenRectangle1(out HObject rectangle, 0, 0, circleMarkImg.Height, circleMarkImg.Width);
            regionObject = selectRegion == null ? rectangle : selectRegion.GenRegion();
            HObject hoImage = circleMarkImg.GenHObject();
            HOperatorSet.ReduceDomain(hoImage, regionObject, out HObject reduceImage);
            HOperatorSet.Rgb1ToGray(reduceImage, out hoImage);

            HOperatorSet.EdgesSubPix(hoImage, out HObject edgeImage, "canny", 1, 10, 20);
            HOperatorSet.SelectShapeXld(edgeImage, out HObject selectedXld, "area", "and", 6000, 9999999);
            HOperatorSet.CircularityXld(selectedXld, out HTuple circularity);
            HOperatorSet.TupleSortIndex(circularity, out HTuple sortedIndex);
            HOperatorSet.TupleInverse(sortedIndex, out HTuple inverseIndex);
            HOperatorSet.SmallestCircleXld(selectedXld, out HTuple centerY, out HTuple centerX, out HTuple radius);
            if (centerY.Length < 1)
            {
                return Errortype.COMMONALGO_MARK_SEARCH_FAIL;
            }

            int index = inverseIndex[0];
            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = 0.7;
            calipParam.MeasureSigma = 1; //注意此值对重复性定位精度有影响（设置为1效果较好）
            calipParam.MeasureLength1 = 20;
            calipParam.MeasureLength2 = 4;
            calipParam.NumMeasures = 120;
            calipParam.MeasureThreshold = 10;
            calipParam.MeasureSelect = "all";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "uniform";

            var ret = CaliperCircle.CircleExtraction(circleMarkImg, new Point(centerX[index], centerY[index]), radius[index], calipParam, out centerPixPoint, out double _);

            regionObject.Dispose();
            rectangle.Dispose();
            edgeImage.Dispose();
            selectedXld.Dispose();
            return ret;
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns>时间</returns>
        public static string GetTimeStamp()
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            var timeStamp = Convert.ToInt64(ts.TotalMilliseconds);
            return timeStamp.ToString();
        }

        /// <summary>
        /// 获取时间
        /// </summary>
        /// <param name="format">时间格式，默认"yyyy:MM:dd:HH:mm:ss:"</param>
        /// <returns>返回时间的字符串</returns>
        public static string GetDateTime(string format = "yyyy-MM-dd-HH:mm:ss:")
        {
            string time = System.DateTime.Now.ToString(format) + System.DateTime.Now.Millisecond.ToString("D3");
            return time;
        }

        /// <summary>
        /// 生成多边形标定区域点集及对应行列号
        /// </summary>
        /// <param name="cornerPoints">多边形边界拐角点</param>
        /// <param name="seedPointAxisValue">起始点的轴坐标</param>
        /// <param name="colNeighbourDist">起始点隔壁一列的轴距离(横向生长方向)</param>
        /// <param name="rowNeighbourDist">起始点隔壁一行的轴距离(竖向生长方向)</param>
        /// <param name="axisPoseToCalib">输出所有标定轴位置点</param>
        /// <param name="poseColRowId">输出所有轴位置点的行列序号（不是坐标，默认起始点为0行0列）</param>
        /// <param name="rowFirst">是否按行先排序</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GenPolygonAreaAxisPoints(List<Point> cornerPoints, Point seedPointAxisValue, Point colNeighbourDist, Point rowNeighbourDist, out List<Point> axisPoseToCalib, out List<Point> poseColRowId, bool rowFirst = true)
        {
            Region region = new Region() { PolygonPoints = cornerPoints };
            var ret = GenPolygonAreaAxisPoints(region, seedPointAxisValue, colNeighbourDist, rowNeighbourDist, out axisPoseToCalib, out poseColRowId, rowFirst);

            return ret;
        }

        /// <summary>
        /// 生成标定区域点集及对应行列号
        /// </summary>
        /// <param name="axisRegion">多边形区域（轴坐标系）</param>
        /// <param name="seedPointAxisValue">起始点的轴坐标</param>
        /// <param name="colNeighbourDist">起始点隔壁一列的轴距离(横向生长方向)</param>
        /// <param name="rowNeighbourDist">起始点隔壁一行的轴距离(竖向生长方向)</param>
        /// <param name="axisPoseToCalib">输出所有标定轴位置点</param>
        /// <param name="poseColRowId">输出所有轴位置点的行列序号（不是坐标，默认起始点为0行0列）</param>
        /// <param name="rowFirst">是否按行先排序</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GenPolygonAreaAxisPoints(Region axisRegion, Point seedPointAxisValue, Point colNeighbourDist, Point rowNeighbourDist, out List<Point> axisPoseToCalib, out List<Point> poseColRowId, bool rowFirst = true)
        {
            axisPoseToCalib = new List<Point>();
            poseColRowId = new List<Point>();
            if ((axisRegion.PolygonPoints is null) && (axisRegion.Circle is null))
            {
                return Errortype.COMMONALGO_POLYGONPOINT_NUM_ERROR;
            }

            if (seedPointAxisValue == null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            if (colNeighbourDist == null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            if (rowNeighbourDist == null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            if (colNeighbourDist.DistanceTo(new Point(0, 0)) == 0)
            {
                return Errortype.COMMONALGO_POLYGONPOINT_DIST_ERROR;
            }

            if (rowNeighbourDist.DistanceTo(new Point(0, 0)) == 0)
            {
                return Errortype.COMMONALGO_POLYGONPOINT_DIST_ERROR;
            }

            double[] polygonX = null;
            double[] polygonY = null;
            if (axisRegion.PolygonPoints != null)
            {
                polygonX = new double[axisRegion.PolygonPoints.Count];
                polygonY = new double[axisRegion.PolygonPoints.Count];
                for (int index = 0; index < axisRegion.PolygonPoints.Count; index++)
                {
                    polygonX[index] = axisRegion.PolygonPoints[index].X;
                    polygonY[index] = axisRegion.PolygonPoints[index].Y;
                }
            }
            else if (axisRegion.Circle != null)
            {
                HOperatorSet.GenCircleContourXld(out HObject axisCircle, axisRegion.Circle.CenterY, axisRegion.Circle.CenterX, axisRegion.Circle.Radius, axisRegion.Circle.StartAngleRad, axisRegion.Circle.EndAngleRad, "positive", 1.0);
                HOperatorSet.GetContourXld(axisCircle, out HTuple borderPointsY, out HTuple borderPointsX);
                polygonX = borderPointsX.DArr;
                polygonY = borderPointsY.DArr;
            }

            if (polygonX == null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            HOperatorSet.TupleMin(polygonY, out HTuple minCornerRow);
            HOperatorSet.TupleMax(polygonY, out HTuple maxCornerRow);
            HOperatorSet.TupleMin(polygonX, out HTuple minCornerCol);
            HOperatorSet.TupleMax(polygonX, out HTuple maxCornerCol);

            HOperatorSet.TupleFloor((minCornerRow - seedPointAxisValue.Y) / rowNeighbourDist.Y, out HTuple roundYMin);
            HOperatorSet.TupleFloor((maxCornerRow - seedPointAxisValue.Y) / rowNeighbourDist.Y, out HTuple roundYMax);
            HOperatorSet.TupleFloor((maxCornerCol - seedPointAxisValue.X) / colNeighbourDist.X, out HTuple roundXMax);
            HOperatorSet.TupleFloor((minCornerCol - seedPointAxisValue.X) / colNeighbourDist.X, out HTuple roundXMin);
            HOperatorSet.TupleAbs(new HTuple(roundYMin, roundYMax, roundXMax, roundXMin), out HTuple stepsAll);
            HOperatorSet.TupleMax(stepsAll + 5, out HTuple maxStep);
            HOperatorSet.TupleGenSequence(-maxStep, maxStep, 1, out HTuple stepBase);
            HTuple rowBias = stepBase * colNeighbourDist.Y;
            HTuple colBias = stepBase * rowNeighbourDist.X;

            HOperatorSet.CreateMatrix(1, stepBase.Length, stepBase * rowNeighbourDist.Y + seedPointAxisValue.Y, out HTuple rowBaseMatrix);
            HOperatorSet.RepeatMatrix(rowBaseMatrix, stepBase.Length, 1, out HTuple matrixRow);
            HOperatorSet.CreateMatrix(rowBias.Length, 1, rowBias, out HTuple rowBiasMatrix);
            HOperatorSet.RepeatMatrix(rowBiasMatrix, 1, stepBase.Length, out HTuple matrixRowBias);

            HOperatorSet.CreateMatrix(stepBase.Length, 1, stepBase * colNeighbourDist.X + seedPointAxisValue.X, out HTuple colBaseMatrix);
            HOperatorSet.RepeatMatrix(colBaseMatrix, 1, stepBase.Length, out HTuple matrixCol);
            HOperatorSet.CreateMatrix(1, colBias.Length, colBias, out HTuple colBiasMatrix);
            HOperatorSet.RepeatMatrix(colBiasMatrix, stepBase.Length, 1, out HTuple matrixColBias);

            HOperatorSet.AddMatrix(matrixCol, matrixColBias, out HTuple colMatrix);
            HOperatorSet.AddMatrix(matrixRow, matrixRowBias, out HTuple rowMatrix);

            HOperatorSet.GetFullMatrix(rowMatrix, out HTuple rowPose);
            HOperatorSet.GetFullMatrix(colMatrix, out HTuple colPose);

            //HTuple tableWindowHeight = maxCornerRow - minCornerRow;
            //HTuple tableWindowWidth = maxCornerCol - minCornerCol;
            HOperatorSet.TupleFloor(maxCornerRow - minCornerRow, out HTuple tableWindowHeight);
            HOperatorSet.TupleFloor(maxCornerCol - minCornerCol, out HTuple tableWindowWidth);

            HOperatorSet.GenImageConst(out HObject tabelWindow, "byte", tableWindowWidth, tableWindowHeight);
            HTuple cornerPointsX = polygonX - minCornerCol;
            HTuple cornerPointsY = polygonY - minCornerRow;
            HOperatorSet.GenRegionPolygonFilled(out HObject polyRegion, cornerPointsY, cornerPointsX);
            HOperatorSet.PaintRegion(polyRegion, tabelWindow, out HObject tableWindowPaint, 1, "fill");

            HTuple rowPoseToSelect = rowPose - minCornerRow;
            HTuple colPoseToSelect = colPose - minCornerCol;
            HOperatorSet.TupleGreaterElem(rowPoseToSelect, 0, out HTuple greaterRow);
            HOperatorSet.TupleGreaterElem(colPoseToSelect, 0, out HTuple greaterCol);
            HOperatorSet.TupleLessElem(rowPoseToSelect, tableWindowHeight, out HTuple lessRow);
            HOperatorSet.TupleLessElem(colPoseToSelect, tableWindowWidth, out HTuple lessCol);
            HTuple selectMask = greaterRow * greaterCol * lessRow * lessCol;
            HOperatorSet.TupleSelectMask(rowPoseToSelect, selectMask, out HTuple rowPoseInWindow);
            HOperatorSet.TupleSelectMask(colPoseToSelect, selectMask, out HTuple colPoseInWindow);
            HOperatorSet.GetGrayval(tableWindowPaint, rowPoseInWindow, colPoseInWindow, out HTuple isInRegion);

            HOperatorSet.TupleSelectMask(rowPoseInWindow, isInRegion, out HTuple rowPoseInRegion);
            HOperatorSet.TupleSelectMask(colPoseInWindow, isInRegion, out HTuple colPoseInRegion);
            tableWindowPaint.Dispose();
            tabelWindow.Dispose();
            polyRegion.Dispose();

            List<Point> poseInRegion = new List<Point>();
            List<Point> sortedPose = new List<Point>();
            long[] sortedRowId = null;
            long[] sortedColId = null;

            int directionX = (colNeighbourDist.X - seedPointAxisValue.X) >= 0 ? 1 : -1;
            int directionY = (rowNeighbourDist.Y - seedPointAxisValue.Y) >= 0 ? 1 : -1;

            if (rowFirst)
            {
                // 正常顺序
                for (int i = 0; i < rowPoseInRegion.Length; i++)
                {
                    poseInRegion.Add(new Point(colPoseInRegion[i].D, rowPoseInRegion[i].D));
                }

                // 其中inLineThresh 行列间隔的阈值，不要超过行列间距，即（rowNeighbourDist-seedPointAxisValue），以差值的10%为宜
                SortRowColumn(poseInRegion, out sortedPose, out _, out sortedRowId, out sortedColId, 0.2, colNeighbourDist, rowNeighbourDist);
                foreach (var axisPose in sortedPose)
                {
                    axisPoseToCalib.Add(axisPose + new Point(minCornerCol, minCornerRow));
                }

                for (int i = 0; i < sortedRowId.Length; i++)
                {
                    poseColRowId.Add(new Point(sortedColId[i] * directionX, sortedRowId[i] * directionY));
                }
            }
            else
            {
                // 先列后行（行列反转排序）
                for (int i = 0; i < rowPoseInRegion.Length; i++)
                {
                    poseInRegion.Add(new Point(rowPoseInRegion[i].D, colPoseInRegion[i].D));
                }

                Point colDist = new Point(rowNeighbourDist.Y, rowNeighbourDist.X);
                Point rowDist = new Point(colNeighbourDist.Y, colNeighbourDist.X);

                SortRowColumn(poseInRegion, out sortedPose, out _, out sortedRowId, out sortedColId, 0.2, colDist, rowDist);

                foreach (var axisPose in sortedPose)
                {
                    axisPoseToCalib.Add(new Point(minCornerCol + axisPose.Y, minCornerRow + axisPose.X));
                }

                //double[] x = new double[sortedRowId.Length];
                //double[] y = new double[sortedRowId.Length];
                for (int i = 0; i < sortedRowId.Length; i++)
                {
                    poseColRowId.Add(new Point(sortedRowId[i] * directionX, sortedColId[i] * directionY));

                    //x[i] = axisPoseToCalib[i].X;
                    //y[i] = axisPoseToCalib[i].Y;
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="hv_T1">第一维度信息</param>
        /// <param name="hv_T2">第二维度信息</param>
        /// <param name="hv_SortMode">排序方式，1按照维度1升序排序，2按照维度2升序排序，-1按照维度1降序排序，-2按照维度2降序排序</param>
        /// <param name="hv_Sorted1">排序后第一维度信息</param>
        /// <param name="hv_Sorted2">排序后第二维度信息</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SortPairs(HTuple hv_T1, HTuple hv_T2, int hv_SortMode, out HTuple hv_Sorted1, out HTuple hv_Sorted2)
        {
            HTuple hv_Indices1 = new HTuple(), hv_Indices2 = new HTuple();

            // Initialize local and output iconic variables 
            hv_Sorted1 = new HTuple();
            hv_Sorted2 = new HTuple();

            if (hv_T1 is null || hv_T2 is null)
            {
                return Errortype.COMMONALGO_SORTPAIR_INPUT_NULL;
            }

            if (hv_T1.Length != hv_T2.Length)
            {
                return Errortype.COMMONALGO_SORTPAIR_INPUTLENGTH_NOT_EQUAL;
            }

            if (hv_T1.Length == 0)
            {
                return Errortype.COMMONALGO_SORTPAIR_INPUTLENGTH_ZERO;
            }

            switch (hv_SortMode)
            {
                case 1:
                    HOperatorSet.TupleSortIndex(hv_T1, out hv_Indices1);
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_Sorted1 = hv_T1.TupleSelect(hv_Indices1);
                    }

                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_Sorted2 = hv_T2.TupleSelect(hv_Indices1);
                    }

                    return Errortype.OK;
                case -1:
                    HOperatorSet.TupleSortIndex(hv_T1, out hv_Indices1);
                    HOperatorSet.TupleInverse(hv_Indices1, out hv_Indices1);
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_Sorted1 = hv_T1.TupleSelect(hv_Indices1);
                    }

                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_Sorted2 = hv_T2.TupleSelect(hv_Indices1);
                    }

                    return Errortype.OK;
                case 2:
                    HOperatorSet.TupleSortIndex(hv_T2, out hv_Indices2);
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_Sorted1 = hv_T1.TupleSelect(hv_Indices2);
                    }

                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_Sorted2 = hv_T2.TupleSelect(hv_Indices2);
                    }

                    return Errortype.OK;
                case -2:
                    HOperatorSet.TupleSortIndex(hv_T2, out hv_Indices2);
                    HOperatorSet.TupleInverse(hv_Indices2, out hv_Indices2);
                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_Sorted1 = hv_T1.TupleSelect(hv_Indices2);
                    }

                    using (HDevDisposeHelper dh = new HDevDisposeHelper())
                    {
                        hv_Sorted2 = hv_T2.TupleSelect(hv_Indices2);
                    }

                    return Errortype.OK;
                default:
                    return Errortype.COMMONALGO_SORTMODE_ERROR;
            }
        }

        /// <summary>
        /// 行列排序
        /// </summary>
        /// <param name="inputPoints">输入点</param>
        /// <param name="sortedPoints">输出排序后的点</param>
        /// <param name="sortedId">输出点的序号</param>
        /// <param name="rowIdAfterSort">排序后的点集行号</param>
        /// <param name="colIdAfterSort">排序后的点集列号</param>
        /// <param name="inLineThresh">同行Y方向波动的阈值，不要超过行间距即可</param>
        /// <param name="xDirectVector">限定X生长方向，默认自动判断</param>
        /// <param name="yDirectVector">限定Y生长方向，默认自动判断</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SortRowColumn(List<Point> inputPoints, out List<Point> sortedPoints, out long[] sortedId, out long[] rowIdAfterSort, out long[] colIdAfterSort, double inLineThresh = 5.0, Point xDirectVector = null, Point yDirectVector = null)
        {
            sortedPoints = new List<Point>();
            sortedId = null;
            sortedId = null;
            rowIdAfterSort = new long[] { };
            colIdAfterSort = new long[] { };

            if (inputPoints == null)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            int lenth = inputPoints.Count;
            if (lenth < 2)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            sortedId = new long[lenth];

            double[] pointsX = new double[lenth];
            double[] pointsY = new double[lenth];
            for (int index = 0; index < lenth; index++)
            {
                pointsX[index] = inputPoints[index].X;
                pointsY[index] = inputPoints[index].Y;
            }

            HTuple rectRad = new HTuple(0.0);
            HTuple rectCenterRow = new HTuple();
            HTuple rectCenterCol = new HTuple();
            if (xDirectVector == null)
            {
                HOperatorSet.TupleMean(pointsY, out rectCenterRow);
                HOperatorSet.TupleMean(pointsX, out rectCenterCol);
                var distanceToCenter = ((new HTuple(pointsX) - rectCenterCol) * (new HTuple(pointsX) - rectCenterCol)) + ((new HTuple(pointsY) - rectCenterRow) * (new HTuple(pointsY) - rectCenterRow));
                HOperatorSet.TupleMin(distanceToCenter, out HTuple minDist);
                HOperatorSet.TupleFindFirst(distanceToCenter, minDist, out HTuple basePointId);
                rectCenterRow = new HTuple(pointsY[basePointId]);
                rectCenterCol = new HTuple(pointsX[basePointId]);
                distanceToCenter = ((new HTuple(pointsX) - rectCenterCol) * (new HTuple(pointsX) - rectCenterCol)) + ((new HTuple(pointsY) - rectCenterRow) * (new HTuple(pointsY) - rectCenterRow));
                HOperatorSet.TupleSortIndex(distanceToCenter, out HTuple distIndices);
                HOperatorSet.TupleSelectRange(distIndices, 0, 1, out HTuple selectedId);
                HOperatorSet.TupleSelect(pointsX, selectedId, out HTuple inRangeX);
                HOperatorSet.TupleSelect(pointsY, selectedId, out HTuple inRangeY);

                HOperatorSet.TupleAtan2(inRangeY[1] - inRangeY[0], inRangeX[1] - inRangeX[0], out HTuple rectPhi);

                HOperatorSet.TupleDeg(-rectPhi, out HTuple rectDeg);
                HOperatorSet.TupleRound(rectDeg / 90.0, out HTuple roundRate);
                var resDeg = rectDeg - roundRate * 90.0;
                HOperatorSet.TupleRad(resDeg, out rectRad);
            }
            else
            {
                HOperatorSet.TupleAtan2(-xDirectVector.Y, xDirectVector.X, out HTuple rectPhi);
                HOperatorSet.TupleDeg(rectPhi, out HTuple rectDeg);
                HOperatorSet.TupleRound(rectDeg / 90.0, out HTuple roundRate);
                var resDeg = rectDeg - roundRate * 90.0;
                HOperatorSet.TupleRad(resDeg, out rectRad);
                HOperatorSet.TupleMean(pointsY, out rectCenterRow);
                HOperatorSet.TupleMean(pointsX, out rectCenterCol);
            }

            HOperatorSet.VectorAngleToRigid(rectCenterRow, rectCenterCol, 0, rectCenterRow, rectCenterCol, -rectRad, out HTuple rigidMatRotate);
            HOperatorSet.AffineTransPoint2d(rigidMatRotate, pointsY, pointsX, out HTuple rigidY, out HTuple rigidX);
            HTuple transY = new HTuple(rigidY);
            HTuple transX = new HTuple(rigidX);

            HOperatorSet.TupleGenSequence(0, rigidY.Length - 1, 1, out HTuple ids);

            HTuple groupStartPos = new HTuple();
            HTuple groupEndPos = new HTuple();
            HTuple halfSortedIds = new HTuple();
            HTuple groupRowValue = new HTuple();
            while (ids.Length > 0)
            {
                //HOperatorSet.DistancePl(rigidY, rigidX, rigidY[0], rigidX[0], rigidY[0], rigidX[0] + 100, out HTuple elemDistance);
                HOperatorSet.TupleAbs(rigidY - rigidY[0], out HTuple elemDistance);
                HOperatorSet.TupleLessElem(elemDistance, inLineThresh, out HTuple inlineElemMask);
                HOperatorSet.TupleSelectMask(rigidX, inlineElemMask, out HTuple inlinePointsX);
                HOperatorSet.TupleSelectMask(ids, inlineElemMask, out HTuple inlinePointsIds);

                HOperatorSet.TupleSortIndex(inlinePointsX, out HTuple singleLineXIndex);
                if ((xDirectVector != null) && xDirectVector.X < 0)
                {
                    HOperatorSet.TupleInverse(singleLineXIndex, out singleLineXIndex);
                }

                HOperatorSet.TupleSelect(inlinePointsIds, singleLineXIndex, out inlinePointsIds);

                groupStartPos = groupStartPos.TupleConcat(halfSortedIds.Length);
                halfSortedIds = halfSortedIds.TupleConcat(inlinePointsIds);
                groupEndPos = groupEndPos.TupleConcat(halfSortedIds.Length);

                groupRowValue = groupRowValue.TupleConcat(rigidY[0]);   // 考虑精度的话此处可用inlinePointsY的均值

                HOperatorSet.TupleNot(inlineElemMask, out HTuple restElemMask);
                HOperatorSet.TupleSelectMask(rigidX, restElemMask, out rigidX);
                HOperatorSet.TupleSelectMask(rigidY, restElemMask, out rigidY);
                HOperatorSet.TupleSelectMask(ids, restElemMask, out ids);
            }

            HTuple finalOrder = new HTuple();
            HOperatorSet.TupleSortIndex(groupRowValue, out HTuple rowOrder);
            if ((yDirectVector != null) && yDirectVector.Y < 0)
            {
                HOperatorSet.TupleInverse(rowOrder, out rowOrder);
            }

            for (int index = 0; index < rowOrder.Length; index++)
            {
                var startPos = groupStartPos[rowOrder[index].L];
                var endPos = groupEndPos[rowOrder[index].L];
                HOperatorSet.TupleSelectRange(halfSortedIds, startPos, endPos - 1, out HTuple selectedGroupElemtsId);
                finalOrder = finalOrder.TupleConcat(selectedGroupElemtsId);
            }

            HOperatorSet.TupleSelect(pointsX, finalOrder, out HTuple finalPointsX);
            HOperatorSet.TupleSelect(pointsY, finalOrder, out HTuple finalPointsY);
            sortedId = finalOrder.LArr;

            HTuple meanRowIntrval = new HTuple(0.0);
            HTuple meanColIntrval = new HTuple(0.0);

            // 估算行间隔
            HOperatorSet.TupleSelect(groupRowValue, rowOrder, out groupRowValue);
            HOperatorSet.CreateFunct1dArray(groupRowValue, out HTuple functionRowValues);
            HOperatorSet.DerivateFunct1d(functionRowValues, "first", out HTuple rowInterval);
            HOperatorSet.Funct1dToPairs(rowInterval, out _, out HTuple rowInterValues);
            while (rowInterValues.Length > 1)
            {
                HOperatorSet.TupleNotEqualElem(rowInterValues, 0, out HTuple neMask);
                HOperatorSet.TupleSelectMask(rowInterValues, neMask, out HTuple rowInterValuesValid);
                HOperatorSet.TupleMin(rowInterValuesValid, out HTuple minIntervalues);
                HOperatorSet.TupleRound(rowInterValues / minIntervalues, out HTuple intervalRound);
                HOperatorSet.TupleEqualElem(intervalRound, 1, out HTuple eqElemMask);
                HOperatorSet.TupleSum(eqElemMask, out HTuple eqCount);
                if (eqCount >= 2)
                {
                    HOperatorSet.TupleSelectMask(rowInterValues, eqElemMask, out HTuple selectedInterval);
                    HOperatorSet.TupleMean(selectedInterval, out meanRowIntrval);
                    break;
                }
                else
                {
                    HOperatorSet.TupleNot(eqElemMask, out HTuple neElemMask);
                    HOperatorSet.TupleSelectMask(rowInterValues, neElemMask, out rowInterValues);
                }
            }

            // 估算列间隔(选取列最多的一行)
            HOperatorSet.TupleMax(groupEndPos - groupStartPos, out HTuple maxLenGroupValue);
            HOperatorSet.TupleFindFirst(groupEndPos - groupStartPos, maxLenGroupValue, out HTuple maxLenGroupId);
            HOperatorSet.TupleSelectRange(halfSortedIds, groupStartPos[maxLenGroupId], groupEndPos[maxLenGroupId] - 1, out HTuple maxLenGroupElemId);
            HOperatorSet.TupleSelect(pointsX, maxLenGroupElemId, out HTuple groupColValue);
            HOperatorSet.CreateFunct1dArray(groupColValue, out HTuple functionColValues);
            HOperatorSet.DerivateFunct1d(functionColValues, "first", out HTuple colInterval);
            HOperatorSet.Funct1dToPairs(colInterval, out _, out HTuple colInterValues);
            while (colInterValues.Length > 1)
            {
                HOperatorSet.TupleNotEqualElem(colInterValues, 0, out HTuple neMask);
                HOperatorSet.TupleSelectMask(colInterValues, neMask, out HTuple colInterValueValid);
                HOperatorSet.TupleMin(colInterValueValid, out HTuple minIntervalues);
                HOperatorSet.TupleRound(colInterValues / minIntervalues, out HTuple intervalRound);
                HOperatorSet.TupleEqualElem(intervalRound, 1, out HTuple eqElemMask);
                HOperatorSet.TupleSum(eqElemMask, out HTuple eqCount);
                if (eqCount >= 2)
                {
                    HOperatorSet.TupleSelectMask(colInterValues, eqElemMask, out HTuple selectedInterval);
                    HOperatorSet.TupleMean(selectedInterval, out meanColIntrval);
                    break;
                }
                else
                {
                    HOperatorSet.TupleNot(eqElemMask, out HTuple neElemMask);
                    HOperatorSet.TupleSelectMask(colInterValues, neElemMask, out colInterValues);
                }
            }

            HOperatorSet.TupleSelect(transX, finalOrder, out HTuple transPointsX);
            HOperatorSet.TupleSelect(transY, finalOrder, out HTuple transPointsY);
            if (meanRowIntrval == 0.0 || meanColIntrval == 0.0)
            {
                return Errortype.COMMONALGO_SORTROWCOLUMN_DISORDER;
            }

            HOperatorSet.TupleRound((transPointsY - transPointsY[0]) / meanRowIntrval, out HTuple rowId);
            HOperatorSet.TupleRound((transPointsX - transPointsX[0]) / meanColIntrval, out HTuple colId);
            rowIdAfterSort = rowId.LArr;
            colIdAfterSort = colId.LArr;

            for (int id = 0; id < lenth; id++)
            {
                sortedPoints.Add(new Point(finalPointsX[id].D, finalPointsY[id].D));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 行或列排序
        /// </summary>
        /// <param name="inputPoints">输入点</param>
        /// <param name="sortedId">输出点的序号</param>
        /// <param name="groupId">排序后的点集行号</param>
        /// <param name="inLineThresh">同行Y方向波动的阈值，不要超过行间距即可</param>
        /// <param name="lineAngleRad">限定点集角度</param>
        /// <param name="sortRow">排序行/列</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SortRowGroups(List<Point> inputPoints, out long[] sortedId, out double[] groupId, double inLineThresh = 5.0, double lineAngleRad = 0, bool sortRow = true)
        {
            sortedId = null;
            sortedId = null;
            groupId = new double[] { };

            if (inputPoints == null)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            int lenth = inputPoints.Count;
            if (lenth < 2)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            sortedId = new long[lenth];

            double[] pointsX = new double[lenth];
            double[] pointsY = new double[lenth];
            for (int index = 0; index < lenth; index++)
            {
                pointsX[index] = inputPoints[index].X;
                pointsY[index] = inputPoints[index].Y;
            }

            if (!sortRow)
            {
                (pointsX, pointsY) = (pointsY, pointsX);
            }

            HTuple rectRad = new HTuple(0.0);
            HTuple rectCenterRow = new HTuple();
            HTuple rectCenterCol = new HTuple();
            if (lineAngleRad == 0)
            {
                HOperatorSet.TupleMean(pointsY, out rectCenterRow);
                HOperatorSet.TupleMean(pointsX, out rectCenterCol);
                var distanceToCenter = ((new HTuple(pointsX) - rectCenterCol) * (new HTuple(pointsX) - rectCenterCol)) + ((new HTuple(pointsY) - rectCenterRow) * (new HTuple(pointsY) - rectCenterRow));
                HOperatorSet.TupleMin(distanceToCenter, out HTuple minDist);
                HOperatorSet.TupleFindFirst(distanceToCenter, minDist, out HTuple basePointId);
                rectCenterRow = new HTuple(pointsY[basePointId]);
                rectCenterCol = new HTuple(pointsX[basePointId]);
                distanceToCenter = ((new HTuple(pointsX) - rectCenterCol) * (new HTuple(pointsX) - rectCenterCol)) + ((new HTuple(pointsY) - rectCenterRow) * (new HTuple(pointsY) - rectCenterRow));
                HOperatorSet.TupleSortIndex(distanceToCenter, out HTuple distIndices);
                HOperatorSet.TupleSelectRange(distIndices, 0, 1, out HTuple selectedId);
                HOperatorSet.TupleSelect(pointsX, selectedId, out HTuple inRangeX);
                HOperatorSet.TupleSelect(pointsY, selectedId, out HTuple inRangeY);

                HOperatorSet.TupleAtan2(inRangeY[1] - inRangeY[0], inRangeX[1] - inRangeX[0], out HTuple rectPhi);

                HOperatorSet.TupleDeg(-rectPhi, out HTuple rectDeg);
                HOperatorSet.TupleRound(rectDeg / 90.0, out HTuple roundRate);
                var resDeg = rectDeg - roundRate * 90.0;
                HOperatorSet.TupleRad(resDeg, out rectRad);
            }
            else
            {
                HOperatorSet.TupleDeg(lineAngleRad, out HTuple rectDeg);
                HOperatorSet.TupleRound(rectDeg / 90.0, out HTuple roundRate);
                var resDeg = rectDeg - roundRate * 90.0;
                HOperatorSet.TupleRad(resDeg, out rectRad);
                HOperatorSet.TupleMean(pointsY, out rectCenterRow);
                HOperatorSet.TupleMean(pointsX, out rectCenterCol);
            }

            HOperatorSet.VectorAngleToRigid(rectCenterRow, rectCenterCol, 0, rectCenterRow, rectCenterCol, -rectRad, out HTuple rigidMatRotate);
            HOperatorSet.AffineTransPoint2d(rigidMatRotate, pointsY, pointsX, out HTuple rigidY, out HTuple rigidX);
            HTuple transY = new HTuple(rigidY);
            HTuple transX = new HTuple(rigidX);

            HOperatorSet.TupleGenSequence(0, rigidY.Length - 1, 1, out HTuple ids);

            HTuple groupStartPos = new HTuple();
            HTuple groupEndPos = new HTuple();
            HTuple halfSortedIds = new HTuple();
            HTuple groupRowValue = new HTuple();

            HOperatorSet.CreateMatrix(1, ids.Length, -1, out HTuple rowMatrix);

            while (ids.Length > 0)
            {
                HOperatorSet.TupleAbs(rigidY - rigidY[0], out HTuple elemDistance);
                HOperatorSet.TupleLessElem(elemDistance, inLineThresh, out HTuple inlineElemMask);
                HOperatorSet.TupleSelectMask(rigidX, inlineElemMask, out HTuple inlinePointsX);
                HOperatorSet.TupleSelectMask(ids, inlineElemMask, out HTuple inlinePointsIds);

                HOperatorSet.TupleSortIndex(inlinePointsX, out HTuple singleLineXIndex);

                HOperatorSet.TupleSelect(inlinePointsIds, singleLineXIndex, out inlinePointsIds);

                groupStartPos = groupStartPos.TupleConcat(halfSortedIds.Length);
                halfSortedIds = halfSortedIds.TupleConcat(inlinePointsIds);
                groupEndPos = groupEndPos.TupleConcat(halfSortedIds.Length);

                groupRowValue = groupRowValue.TupleConcat(rigidY[0]);   // 考虑精度的话此处可用inlinePointsY的均值

                HOperatorSet.TupleNot(inlineElemMask, out HTuple restElemMask);
                HOperatorSet.TupleSelectMask(rigidX, restElemMask, out rigidX);
                HOperatorSet.TupleSelectMask(rigidY, restElemMask, out rigidY);
                HOperatorSet.TupleSelectMask(ids, restElemMask, out ids);
            }

            HTuple finalOrder = new HTuple();
            HOperatorSet.TupleSortIndex(groupRowValue, out HTuple rowOrder);

            for (int index = 0; index < rowOrder.Length; index++)
            {
                var startPos = groupStartPos[rowOrder[index].L];
                var endPos = groupEndPos[rowOrder[index].L];
                HOperatorSet.TupleSelectRange(halfSortedIds, startPos, endPos - 1, out HTuple selectedGroupElemtsId);
                finalOrder = finalOrder.TupleConcat(selectedGroupElemtsId);

                HOperatorSet.TupleGenConst(selectedGroupElemtsId.Length, 0, out HTuple matRow);
                HOperatorSet.TupleGenConst(selectedGroupElemtsId.Length, index, out HTuple matCol);
                HOperatorSet.SetValueMatrix(rowMatrix, matRow, selectedGroupElemtsId, matCol);
            }

            HOperatorSet.TupleSelect(pointsX, finalOrder, out HTuple finalPointsX);
            HOperatorSet.TupleSelect(pointsY, finalOrder, out HTuple finalPointsY);
            sortedId = finalOrder.LArr;

            HOperatorSet.GetFullMatrix(rowMatrix, out HTuple rowIds);

            //HOperatorSet.TupleInt(rowIds, out HTuple idInt);
            groupId = rowIds.DArr;

            return Errortype.OK;
        }

        /// <summary>
        /// 计算两条直线的夹角
        /// </summary>
        /// <param name="line1">直线1</param>
        /// <param name="line2">直线2</param>
        /// <param name="angle">输出夹角弧度</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype CalcAngleLL(LineSeg line1, LineSeg line2, out double angle)
        {
            angle = 0;
            if (line1 is null || line2 is null)
            {
                return Errortype.COMMONALGO_CALCANGLELL_INPUT_NULL;
            }

            HOperatorSet.AngleLl(line1.Start_Y, line1.Start_X, line1.End_Y, line1.End_X, line2.Start_Y, line2.Start_X, line2.End_Y, line2.End_X, out HTuple hAngle);
            angle = hAngle.D;
            return Errortype.OK;
        }

        /// <summary>
        /// 根据轮廓点生成区域
        /// </summary>
        /// <param name="polygon">轮廓点</param>
        /// <param name="region">返回的区域</param>
        /// <param name="width">区域极限的宽</param>
        /// <param name="height">区域极限的高</param>
        /// <param name="inner">所求区域是否是在轮廓内</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GetPolygonRegion(List<Point> polygon, out HObject region, int width, int height, bool inner = true)
        {
            HOperatorSet.GenEmptyObj(out region);
            if (polygon == null)
            {
                return Errortype.OK;
            }

            HTuple rows = new HTuple();
            HTuple cols = new HTuple();
            for (int i = 0; i < polygon.Count; i++)
            {
                HOperatorSet.TupleConcat(rows, polygon[i].Y, out rows);
                HOperatorSet.TupleConcat(cols, polygon[i].X, out cols);
            }

            HOperatorSet.GenRegionPolygonFilled(out HObject regionPolygon, rows, cols);
            HOperatorSet.GenRectangle1(out HObject rectangle, 0, 0, height, width);
            if (inner)
            {
                region = regionPolygon.Clone();
            }
            else
            {
                HOperatorSet.Difference(rectangle, regionPolygon, out region);
            }

            rectangle.Dispose();
            regionPolygon.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 粗略提取内外圆(用于网格板/玻璃IC重叠成像)
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="circleCenter">圆形坐标</param>
        /// <param name="circleRadius">圆半径</param>
        /// <returns>OK：成功，其他：失败</returns>
        public static Errortype ExtractInnerOuterCircle(Camera img, out List<Point> circleCenter, out List<double> circleRadius)
        {
            circleCenter = new List<Point>();
            circleRadius = new List<double>();
            if (img is null || img.Width < 1 || img.Height < 1)
            {
                return Errortype.INPUT_IMAGE_NULL;
            }

            HObject hImage = img.GenHObject();
            HOperatorSet.BinaryThreshold(hImage, out HObject thresholdRegionMax, "max_separability", "dark", out HTuple usedThresh);
            HOperatorSet.ClosingCircle(thresholdRegionMax, out HObject regionClosing, 7.5);
            HOperatorSet.Connection(regionClosing, out HObject connectionRegion);

            // 获取内圆
            HOperatorSet.SelectShape(connectionRegion, out HObject mixSelectedRegion, "circularity", "and", 0.7, 1);
            HOperatorSet.SelectShape(mixSelectedRegion, out HObject minSelectedRegionArea, "area", "and", 10000, 9999999);

            //获取外圆
            HOperatorSet.Difference(regionClosing, minSelectedRegionArea, out HObject regionDifference);
            HOperatorSet.FillUp(regionDifference, out HObject regionFillUp);
            HOperatorSet.Connection(regionFillUp, out HObject connectionRegion1);
            HOperatorSet.SelectShape(connectionRegion1, out HObject selectedRegion, "circularity", "and", 0.7, 1);
            HOperatorSet.SelectShape(selectedRegion, out HObject maxSelectedRegion, "area", "and", 10000, 9999999);

            //获取圆心
            HOperatorSet.SmallestCircle(minSelectedRegionArea, out HTuple minRow, out HTuple minCol, out HTuple minRadius);
            HOperatorSet.SmallestCircle(maxSelectedRegion, out HTuple maxRow, out HTuple maxCol, out HTuple maxRadius);

            if (minRadius.Length != 1 || maxRadius.Length != 1)
            {
                return Errortype.CALIPER_CIRCLE_NULL;
            }

            circleCenter.Add(new Point(minCol.D, minRow.D));
            circleCenter.Add(new Point(maxCol.D, maxRow.D));
            circleRadius.Add(minRadius.D);
            circleRadius.Add(maxRadius.D);

            //thresholdRegionMin.Dispose();
            thresholdRegionMax.Dispose();

            //imageReduced.Dispose();
            connectionRegion.Dispose();
            regionClosing.Dispose();
            mixSelectedRegion.Dispose();
            minSelectedRegionArea.Dispose();
            maxSelectedRegion.Dispose();
            selectedRegion.Dispose();
            regionFillUp.Dispose();
            regionDifference.Dispose();
            connectionRegion1.Dispose();
            hImage.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 计算玻璃IC键合像素误差
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="calipParam">卡尺参数</param>
        /// <param name="innerCircle">键合后玻璃ICMark像素坐标</param>
        /// <param name="outerCircle">键合后棋盘格Mark像素坐标</param>
        /// <param name="errorXY">键合像素误差</param>
        /// <returns>OK：成功，其他：失败</returns>
        public static Errortype CalcGlassIcBondPix(Camera img, CaliperParams calipParam, out Point innerCircle, out Point outerCircle, out Point errorXY)
        {
            innerCircle = new Point();
            outerCircle = new Point();
            errorXY = new Point();
            Errortype ret = ExtractInnerOuterCircle(img, out List<Point> circleCenter, out List<double> circleRadius);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            List<Point> centerPixPoint = new List<Point>();

            for (int index = 0; index < circleCenter.Count; index++)
            {
                ret = CaliperCircle.CircleExtraction(img, circleCenter[index], circleRadius[index], calipParam, out Point centerOut, out double radius);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                centerPixPoint.Add(new Point(centerOut.X, centerOut.Y));
            }

            innerCircle = centerPixPoint[0];
            outerCircle = centerPixPoint[1];
            errorXY = outerCircle - innerCircle;
            return Errortype.OK;
        }

        /// <summary>
        /// 计算两组点连线的旋转平移量
        /// </summary>
        /// <param name="basePoint1">基准起始点</param>
        /// <param name="basePoint2">基准结束点</param>
        /// <param name="currentPoint1">当前起始点</param>
        /// <param name="currentPoint2">当前结束点</param>
        /// <param name="rotateCenter">当前旋转中心</param>
        /// <param name="angle">旋转量</param>
        /// <param name="translation">平移量</param>
        /// <returns>OK：成功，其他：失败</returns>
        public static Errortype CalcLinesRT(Point basePoint1, Point basePoint2, Point currentPoint1, Point currentPoint2, Point rotateCenter, out double angle, out Point translation)
        {
            angle = 0;
            translation = new Point();
            if (basePoint1 is null || basePoint2 is null || currentPoint1 is null || currentPoint2 is null)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            Errortype ret = CalcAngleLL(new LineSeg(currentPoint1, currentPoint2), new LineSeg(basePoint1, basePoint2), out double radian);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HOperatorSet.TupleDeg(new HTuple(radian), out HTuple deg);
            angle = deg.D;

            //平移
            Point baseCenter = (basePoint1 + basePoint2) / 2;
            Point currentCenter = (currentPoint1 + currentPoint2) / 2;
            ret = CalcRotatePoint(currentCenter, -angle, rotateCenter, out Point rotateCurPoint);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            translation = baseCenter - rotateCurPoint;

            return Errortype.OK;
        }

        /// <summary>
        /// 计算点集1到点集2的旋转平移
        /// </summary>
        /// <param name="pointsBase">点集1</param>
        /// <param name="pointsTarget">点集2，顺序和1对应</param>
        /// <param name="rotateCenter">点集1的旋转中心</param>
        /// <param name="rotateDeg">输出旋转角度(顺时针为正)</param>
        /// <param name="transXY">输出平移量</param>
        /// <param name="rotateErros">各点旋转角残差的标准差</param>
        /// <param name="xyErrors">xy误差</param>
        /// <returns>OK：成功，其他：失败</returns>
        public static Errortype CalcPointsRT(List<Point> pointsBase, List<Point> pointsTarget, Point rotateCenter, out double rotateDeg, out Point transXY, out double rotateErros, out double xyErrors)
        {
            rotateDeg = 0.0;
            transXY = new Point(0, 0);
            rotateErros = 0.0;
            xyErrors = 0.0;
            if (pointsBase is null || pointsTarget is null)
            {
                return Errortype.COMMONALGO_POINT_INPUT_NULL;
            }

            if (pointsBase.Count != pointsTarget.Count)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_NOT_EQUAL;
            }

            if (rotateCenter == null)
            {
                return Errortype.COMMONALGO_CALCPOINTSRT_ROTATECENTER_NULL;
            }

            if (pointsBase.Count < 1)
            {
                return Errortype.COMMONALGO_CALCPOINTSRT_POINT_EMPTY;
            }

            double[] baseX = new double[pointsBase.Count];
            double[] baseY = new double[pointsBase.Count];
            double[] targetX = new double[pointsBase.Count];
            double[] targetY = new double[pointsBase.Count];
            for (int i = 0; i < pointsBase.Count; i++)
            {
                baseX[i] = pointsBase[i].X;
                baseY[i] = pointsBase[i].Y;
                targetX[i] = pointsTarget[i].X;
                targetY[i] = pointsTarget[i].Y;
            }

            HOperatorSet.TupleMean(baseX, out HTuple baseCenterX);
            HOperatorSet.TupleMean(baseY, out HTuple baseCenterY);
            HOperatorSet.TupleGenConst(baseX.Length, baseCenterX, out HTuple baseXEnd);
            HOperatorSet.TupleGenConst(baseX.Length, baseCenterY, out HTuple baseYEnd);
            HOperatorSet.TupleMean(targetX, out HTuple targetCenterX);
            HOperatorSet.TupleMean(targetY, out HTuple targetCenterY);
            HOperatorSet.TupleGenConst(baseX.Length, targetCenterX, out HTuple targetXEnd);
            HOperatorSet.TupleGenConst(baseX.Length, targetCenterY, out HTuple targetYEnd);

            HOperatorSet.AngleLl(baseY, baseX, baseYEnd, baseXEnd, targetY, targetX, targetYEnd, targetXEnd, out HTuple angleRadList);
            HOperatorSet.TupleDeg(angleRadList, out HTuple angleDegList);
            HOperatorSet.TupleNotEqualElem(angleDegList, 0, out HTuple notZero);
            HOperatorSet.TupleSelectMask(angleDegList, notZero, out angleDegList);
            HOperatorSet.TupleMean(angleDegList, out HTuple angleDeg);

            //计算旋转后的点集1坐标
            HOperatorSet.TupleRad(-angleDeg, out HTuple phiRad);
            HTuple baseXRotate = (new HTuple(baseX) - rotateCenter.X) * Math.Cos(phiRad.D) - (new HTuple(baseY) - rotateCenter.Y) * Math.Sin(phiRad.D) + rotateCenter.X;
            HTuple baseYRotate = (new HTuple(baseY) - rotateCenter.Y) * Math.Cos(phiRad.D) + (new HTuple(baseX) - rotateCenter.X) * Math.Sin(phiRad.D) + rotateCenter.Y;
            HOperatorSet.TupleMean(baseXRotate, out HTuple baseCenterXRotate);
            HOperatorSet.TupleMean(baseYRotate, out HTuple baseCenterYRotate);

            //计算两个点集和重心连线夹角的波动性
            HOperatorSet.TupleDeviation(angleDegList - angleDeg, out HTuple angleResidual);

            // 计算两个点集和各自重心的距离的对比差异
            HOperatorSet.DistancePp(baseY, baseX, baseYEnd, baseXEnd, out HTuple baseDistance);
            HOperatorSet.DistancePp(targetY, targetX, targetYEnd, targetXEnd, out HTuple targetDistance);
            HOperatorSet.TupleSum(targetDistance - baseDistance, out HTuple distanceResidual);

            rotateDeg = angleDeg.D;
            transXY = new Point(targetCenterX - baseCenterXRotate, targetCenterY - baseCenterYRotate);

            rotateErros = angleResidual.D;
            xyErrors = distanceResidual.D;

            angleRadList.UnPinTuple();
            angleDegList.UnPinTuple();
            baseXEnd.UnPinTuple();
            baseYEnd.UnPinTuple();
            targetXEnd.UnPinTuple();
            baseDistance.UnPinTuple();
            targetDistance.UnPinTuple();

            return Errortype.OK;
        }

        /// <summary>
        /// 保存Flg
        /// </summary>
        /// <param name="saveItem">保存项</param>
        /// <param name="flgInt">保存项对应的数字</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static bool SaveFlg(string saveItem, out int flgInt)
        {
            string file = @"D:\Alg\Save.txt";
            flgInt = 0;
            if (!File.Exists(file))
            {
                return false;
            }

            FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            StreamReader sr = new StreamReader(fs);
            while (true)
            {
                string line = sr.ReadLine();
                if (line == null)
                {
                    break;
                }

                if (line.Contains(saveItem))
                {
                    string[] lineParts = line.Split();
                    if (lineParts.Length != 2)
                    {
                        return false;
                    }

                    if (!int.TryParse(lineParts[1], out int result))
                    {
                        sr.Close();
                        fs.Close();
                        return false;
                    }

                    flgInt = result;
                    sr.Close();
                    fs.Close();
                    return result == 0 ? false : true;
                }
            }

            sr.Close();
            fs.Close();
            return false;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="filePath">路径</param>
        /// <param name="data">需要保存的数据</param>
        public static void SaveData(string filePath, params object[] data)
        {
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();
            }

            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");

            string text = string.Empty;
            text += time + " ";
            for (int i = 0; i < data.Length; ++i)
            {
                text += data[i].ToString() + " ";
            }

            FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
        }

        /// <summary>
        /// 文件夹管理
        /// </summary>
        /// <param name="path">保存文件夹路径</param>
        /// <param name="dateTime">保留文件天数</param>
        /// <param name="filePath">文件保存路径</param>
        public static void FileManage(string path, int dateTime, out string filePath)
        {
            var ret = Errortype.OK;
            filePath = string.Empty;

            DateTime now = DateTime.Now;
            string time = now.ToString().Replace("/", "");
            string[] str = time.Split(' ');
            filePath = path + "\\" + str[0];
            if (!Directory.Exists(filePath))
            {
                return;
            }

            // 删除dateTime之前dateTime日期创建的文件夹
            string[] currentFile = Directory.GetDirectories(path);
            for (int i = 0; i < currentFile.Length; ++i)
            {
                int date = int.Parse(str[0]) - dateTime;
                string dateFile = path + "\\" + date.ToString();
                int compareResult = currentFile[i].CompareTo(dateFile);
                if (compareResult < 0)
                {
                    Directory.Delete(currentFile[i], true);
                }
            }
        }

        /// <summary>
        /// 保存识别不到的模板
        /// </summary>
        /// <param name="path">保存文件夹路径</param>
        /// <param name="img">img图像</param>
        /// <param name="imgName">限定文件名</param>
        /// <param name="dateTime">文件保留时长</param>
        /// <param name="saveSingleChannel">是否保存为单通道图</param>
        /// <returns>OK：成功</returns>
        public static Errortype SaveImage(string path, Camera img, string imgName = null, int dateTime = 30, bool saveSingleChannel = true)
        {
            FileManage(path, dateTime, out string filePath);
            string templateName = string.Empty;
            if ((imgName != null) && (imgName != string.Empty))
            {
                templateName = imgName + ".bmp";
            }
            else
            {
                DateTime now = DateTime.Now;
                int milliseconds = now.Millisecond;
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                templateName = "Image_" + time + ".bmp";
            }

            string saveName = Path.Combine(filePath, templateName);

            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            HObject hImg = img.GenHObject();
            if (saveSingleChannel)
            {
                HOperatorSet.Rgb1ToGray(hImg, out HObject grayImage);
                HOperatorSet.WriteImage(grayImage, "bmp", 0, saveName);
                grayImage.Dispose();
                hImg.Dispose();
                return Errortype.OK;
            }

            HOperatorSet.WriteImage(hImg, "bmp", 0, saveName);
            hImg.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 计算光源中心
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="start">起点</param>
        /// <param name="end">终点</param>
        /// <param name="center">中心</param>
        /// <param name="crossPoints">两端点</param>
        /// <param name="gray">灰度值</param>
        /// <param name="sigma">sigma值</param>
        /// <returns>OK：成功，其他：失败</returns>
        public static Errortype CalcLightCenter(Camera img, Point start, Point end, out Point center, out List<Point> crossPoints, out List<double> gray, double sigma = 5)
        {
            center = new Point();
            gray = new List<double>();
            crossPoints = new List<Point>();

            if (img is null)
            {
                return Errortype.INPUT_IMAGE_NULL;
            }

            HObject hImg = img.GenHObject();
            HOperatorSet.GenRegionLine(out HObject line, start.Y, start.X, end.Y, end.X);

            HOperatorSet.GetRegionPoints(line, out HTuple rows, out HTuple cols);

            HOperatorSet.GetGrayval(hImg, rows, cols, out HTuple grayVal);

            for (int i = 0; i < grayVal.Length; i++)
            {
                gray.Add(grayVal[i].D);
            }

            HOperatorSet.CreateFunct1dArray(grayVal, out HTuple func);
            HOperatorSet.SmoothFunct1dGauss(func, sigma, out HTuple smoothedFunc);
            HOperatorSet.DerivateFunct1d(smoothedFunc, "first", out HTuple der);
            HOperatorSet.ZeroCrossingsFunct1d(der, out HTuple zeroCorssings);

            if (zeroCorssings.Length < 2)
            {
                return Errortype.COMMONALGO_CALCLIGHECENTER_ZEORCROSSING_ERROR;
            }

            double firstIndex = zeroCorssings[0].D;
            double lastIndex = zeroCorssings[zeroCorssings.Length - 1].D;

            Point crossFirst = new Point()
            {
                X = start.X + (end.X - start.X) * (firstIndex / grayVal.Length),
                Y = start.Y + (end.Y - start.Y) * (firstIndex / grayVal.Length),
            };

            Point crossSecond = new Point()
            {
                X = start.X + (end.X - start.X) * (lastIndex / grayVal.Length),
                Y = start.Y + (end.Y - start.Y) * (lastIndex / grayVal.Length),
            };

            crossPoints.Add(crossFirst);
            crossPoints.Add(crossSecond);

            center = ((crossFirst + crossSecond) / 2).Clone();

            hImg.Dispose();
            line.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 计算光源中心
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="center">中心</param>
        /// <returns>OK：成功，其他：失败</returns>
        public static Errortype CalcLightCenter(Camera img, out Point center)
        {
            center = new Point();
            if (img is null)
            {
                return Errortype.INPUT_IMAGE_NULL;
            }

            HObject hImg = img.GenHObject();
            HOperatorSet.GetRegionPoints(hImg, out HTuple rows, out HTuple cols);
            HOperatorSet.GetGrayval(hImg, rows, cols, out HTuple grayVal);
            HOperatorSet.TupleSort(grayVal, out HTuple graySorted);

            int index = (int)Math.Round(graySorted.Length * 0.95);
            double grayThresh = graySorted[index];

            HOperatorSet.Threshold(hImg, out HObject region, grayThresh, 255);
            HOperatorSet.AreaCenter(region, out HTuple area, out HTuple centerRow, out HTuple centerCol);
            center = new Point(centerCol, centerRow);
            hImg.Dispose();
            region.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 删除超过7天的数据
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="days">间隔日期</param>
        public static void LogDelete(string path, int days = 7)
        {
            if (!Directory.Exists(path))
            {
                return;
            }

            var allFolders = Directory.GetDirectories(path);
            foreach (var folder in allFolders)
            {
                var folderInfo = new DirectoryInfo(folder);
                if (DateTime.Now.Date - folderInfo.CreationTime.Date > TimeSpan.FromDays(days))
                {
                    Directory.Delete(folder, true);
                }
            }
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="path">路径</param>
        /// <param name="name">名字</param>
        /// <param name="days">保存日期</param>
        public static void LogImage(Camera image, string path, string name, int days = 3)
        {
            if (image is null)
            {
                return;
            }

            LogDelete(path, days);
            var now = DateTime.Now;
            var dataPath = Path.Combine(path, now.ToString("yyyyMMdd"));

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            var imgPath = Path.Combine(dataPath, $"{name}-{now:HHmmssfff}.bmp");
            var res = image?.Save(imgPath);
        }

        /// <summary>
        /// 保存多张图像
        /// </summary>
        /// <param name="images">图像集合</param>
        /// <param name="path">路径</param>
        /// <param name="name">名字集合</param>
        /// <param name="days">保存日期</param>
        public static void LogImages(List<Camera> images, string path, string name, int days = 3)
        {
            for (int index = 0; index < images.Count; index++)
            {
                LogImage(images[index], path, name + "_" + index.ToString());
            }
        }

        /// <summary>
        /// 保存内容
        /// </summary>
        /// <param name="text">内容</param>
        /// <param name="path">路径</param>
        /// <param name="name">名字</param>
        /// <param name="days">保存日期</param>
        public static void LogText(string text, string path, string name, int days = 3)
        {
            LogDelete(path, days);
            var now = DateTime.Now;
            var dataPath = Path.Combine(path, now.ToString("yyyyMMdd"));

            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            var fileName = Path.Combine(dataPath, name);

            FileStream fs;
            StreamWriter sw;
            fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            sw = new StreamWriter(fs);
            sw.WriteLine(text);
            sw.Close();
            fs.Close();
        }
    }

    /// <summary>
    /// 重复性测试分析类
    /// </summary>
    public static class RepeatAnalyse
    {
        private static string _testName = string.Empty;
        private static List<string> _titles = new List<string>();
        private static List<string> _titlesSep = new List<string>();
        private static List<List<double>> _datas = new List<List<double>>();
        private static string _sep = ","; //csv 逗号分割 
        private static bool _sepTitleDone = false;
        private static string _startTime = string.Empty;
        private static List<string> _dataTime = new List<string>();
        private static string _analysisTitle = string.Empty;
        private static int _dataDimension = 0;

        /// <summary>
        /// 初始化，置空所有的变量
        /// </summary>
        /// <returns>OK:成功；其他:失败</returns>
        private static Errortype Init()
        {
            _testName = string.Empty;
            _titles.Clear();
            _titlesSep.Clear();
            _datas.Clear();
            _sepTitleDone = false;
            _startTime = string.Empty;
            _dataTime.Clear();
            _dataDimension = 0;
            _analysisTitle = "Min" + _sep + "Max" + _sep + "Mean" + _sep + "Median" + _sep + "Range" + _sep + "3Sigma";
            return Errortype.OK;
        }

        /// <summary>
        /// 设置测试名称和数据标题
        /// </summary>
        /// <param name="testName">测试名称</param>
        /// <param name="titles">数据标题</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SetTitle(string testName, List<string> titles)
        {
            Init();

            _startTime = System.DateTime.Now.ToString().Replace(" ", "/") + "/" + System.DateTime.Now.Millisecond;

            _testName = testName;
            foreach (var title in titles)
            {
                _titles.Add(title);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 分割title 主要对复杂结构体做拆分
        /// </summary>
        /// <param name="datas">数据</param>
        /// <returns>OK:成功；其他:失败</returns>
        private static Errortype SepTitle(params object[] datas)
        {
            if (_titles.Count != datas.Length)
            {
                return Errortype.REPEATANALYSIS_SEPTITLE_TITLE_DATA_LENGTH_NOT_EQUAL;
            }

            for (int i = 0; i < datas.Length; i++)
            {
                if (datas[i] is double || datas[i] is int || datas[i] is float)
                {
                    _titlesSep.Add(_titles[i]);
                }

                if (datas[i] is Point)
                {
                    _titlesSep.Add(_titles[i] + "_X");
                    _titlesSep.Add(_titles[i] + "_Y");
                }

                if (datas[i] is Point3D)
                {
                    _titlesSep.Add(_titles[i] + "_X");
                    _titlesSep.Add(_titles[i] + "_Y");
                    _titlesSep.Add(_titles[i] + "_Z");
                }
            }

            _dataDimension = _titlesSep.Count;
            _sepTitleDone = true;
            return Errortype.OK;
        }

        /// <summary>
        /// 添加重复性数据
        /// </summary>
        /// <param name="datas">数据</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype AddRepeatData(params object[] datas)
        {
            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            _dataTime.Add(time);

            if (!_sepTitleDone)
            {
                SepTitle(datas);
                for (int i = 0; i < _dataDimension; i++)
                {
                    _datas.Add(new List<double>());
                }
            }

            List<double> oneLineData = new List<double>();
            foreach (var data in datas)
            {
                if (data is Point)
                {
                    Point pointData = data as Point;
                    oneLineData.Add(pointData.X);
                    oneLineData.Add(pointData.Y);
                }

                if (data is Point3D)
                {
                    Point3D pointData = data as Point3D;
                    oneLineData.Add(pointData.X);
                    oneLineData.Add(pointData.Y);
                    oneLineData.Add(pointData.Z);
                }

                if (data is double || data is int || data is float)
                {
                    oneLineData.Add(Convert.ToDouble(data));
                }
            }

            if (oneLineData.Count != _dataDimension)
            {
                return Errortype.REPEATANALYSIS_GENREPEATREPORT_DATA_LENGTH_ERROR;
            }

            for (int i = 0; i < _dataDimension; i++)
            {
                _datas[i].Add(oneLineData[i]);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算重复性报告
        /// </summary>
        /// <returns>OK:成功；其他:失败</returns>
        private static Errortype GenRepeatReport(out StringBuilder reportContent)
        {
            Errortype ret = Errortype.OK;
            reportContent = new StringBuilder();

            List<DataStatisticParam> overRallResult = new List<DataStatisticParam>();
            List<DataStatisticParam> relativeResult = new List<DataStatisticParam>();

            for (int i = 0; i < _datas.Count; i++)
            {
                ret = ComAlgo.CalcDataSummary(_datas[i], out DataStatisticParam overRallValueResult);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                overRallResult.Add(overRallValueResult);
                List<double> relativeValue = new List<double>();
                for (int j = 1; j < _datas[i].Count; j++)
                {
                    relativeValue.Add(_datas[i][j] - _datas[i][j - 1]);
                }

                ret = ComAlgo.CalcDataSummary(relativeValue, out DataStatisticParam relativeValueResult);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                relativeResult.Add(relativeValueResult);
            }

            reportContent.AppendLine(_startTime);
            reportContent.AppendLine(_testName);
            reportContent.AppendLine();
            reportContent.AppendLine("OverRallAnalysisResult" + _sep + _analysisTitle + " " + _sep + "RelativeAnalysisResult" + _sep + _analysisTitle);

            for (int i = 0; i < _dataDimension; i++)
            {
                reportContent.AppendLine(_titlesSep[i] + _sep +
                     overRallResult[i].Min.ToString() + _sep +
                     overRallResult[i].Max.ToString() + _sep +
                     overRallResult[i].Mean.ToString() + _sep +
                     overRallResult[i].Median.ToString() + _sep +
                     overRallResult[i].Range.ToString() + _sep +
                     overRallResult[i].Sigma3.ToString() + _sep +
                     " " + _sep +
                     relativeResult[i].Min.ToString() + _sep +
                     relativeResult[i].Max.ToString() + _sep +
                     relativeResult[i].Mean.ToString() + _sep +
                     relativeResult[i].Median.ToString() + _sep +
                     relativeResult[i].Range.ToString() + _sep +
                     relativeResult[i].Sigma3.ToString() + _sep);
            }

            reportContent.Append("Time" + _sep);
            for (int i = 0; i < _dataDimension; i++)
            {
                reportContent.Append(_titlesSep[i] + _sep);
            }

            reportContent.Append('\n');

            if (_datas.Count < 1)
            {
                return Errortype.REPEATANALYSIS_GENREPEATREPORT_DATA_ZERO;
            }

            for (int i = 0; i < _datas[0].Count; i++)
            {
                reportContent.Append(_dataTime[i] + _sep);
                for (int j = 0; j < _datas.Count; j++)
                {
                    reportContent.Append(_datas[j][i] + _sep);
                }

                reportContent.Append('\n');
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存重复性分析报告
        /// </summary>
        /// <param name="dir">文件保存路径</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SaveRepeatReport(string dir)
        {
            Errortype ret = GenRepeatReport(out StringBuilder reportContent);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
            string reportName = Path.Combine(dir, _testName + time + ".csv");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            FileStream fs = new FileStream(reportName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(reportContent);
            sw.Close();
            fs.Close();

            return Errortype.OK;
        }
    }
}

