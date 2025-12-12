using DataStruct;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStruct;
using HalconDotNet;
using System.Net.NetworkInformation;

namespace UltrapreciseBonding.Ransac
{
    /// <summary>
    /// Ransac提取直线
    /// </summary>
    public static class RansacLine
    {
        /// <summary>
        /// 直线提取
        /// </summary>
        /// <param name="points">边缘点</param>
        /// <param name="ransacParam">Ransac参数</param>
        /// <param name="pointsOut">输出的边缘点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype LineExtraction(List<Point> points, RansacParam ransacParam, out List<Point> pointsOut)
        {
            pointsOut = new List<Point>(points);

            if (points is null || points.Count == 0)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            int iterations = 0;
            int bestInlineCount = 0;
            HOperatorSet.SetSystem("seed_rand", ransacParam.RandSeed);

            List<double> inlinePx = new List<double>();
            List<double> inlinePy = new List<double>();
            while (iterations < ransacParam.MaxIterator)
            {
                //随机初始点
                List<int> samples = new List<int>() { 0, 0 };
                for (int i = 0; i < samples.Count; ++i)
                {
                    HOperatorSet.TupleRand(2, out HTuple rand);
                    HOperatorSet.TupleRound(rand[0] * (points.Count - 1), out HTuple round);
                    HOperatorSet.TupleFloor(rand[0] * (points.Count - 1), out HTuple floor);
                    samples[i] = i == 0 ? (int)round.D : (int)floor.D + 1;
                }

                // 计算内点数
                int inlineCounts = 0;
                List<double> inlinePxTmp = new List<double>();
                List<double> inlinePyTmp = new List<double>();
                Errortype ret = CountWithInDistance(samples, points, ransacParam.DistanceThreshold, out inlinePxTmp, out inlinePyTmp, out inlineCounts);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                if (inlineCounts >= bestInlineCount && inlineCounts >= (ransacParam.MinScore * points.Count))
                {
                    bestInlineCount = inlineCounts;
                    inlinePx = new List<double>(inlinePxTmp);
                    inlinePy = new List<double>(inlinePyTmp);
                }
                else if ((inlineCounts < bestInlineCount && inlineCounts >= (ransacParam.MinScore * points.Count)) || (iterations > ransacParam.MaxIterator))
                {
                    break;
                }

                iterations++;
            }

            pointsOut = new List<Point>();
            for (int i = 0; i < inlinePx.Count; i++)
            {
                pointsOut.Add(new Point(inlinePx[i], inlinePy[i]));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 直线提取
        /// </summary>
        /// <param name="px">输入X</param>
        /// <param name="py">输入Y</param>
        /// <param name="ransacParam">Ransac参数</param>
        /// <param name="pxOut">输出X</param>
        /// <param name="pyOut">输出Y</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype LineExtraction(List<double> px, List<double> py, RansacParam ransacParam, out List<double> pxOut, out List<double> pyOut)
        {
            pxOut = new List<double>(px);
            pyOut = new List<double>(py);
            if (px is null || py is null)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            List<Point> points = new List<Point>();
            for (int i = 0; i < px.Count; i++)
            {
                points.Add(new Point(px[i], py[i]));
            }

            Errortype ret = LineExtraction(points, ransacParam, out List<Point> pointsOut);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            pxOut = new List<double>();
            pyOut = new List<double>();
            for (int i = 0; i < pointsOut.Count; i++)
            {
                pxOut.Add(pointsOut[i].X);
                pyOut.Add(pointsOut[i].Y);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算内点
        /// </summary>
        /// <param name="samples">采样点索引</param>
        /// <param name="edgePoints">边缘点</param>
        /// <param name="distanceThreshold">距离阈值</param>
        /// <param name="inlinePx">内点x</param>
        /// <param name="inlinePy">内点y</param>
        /// <param name="inlineCounts">内点数量</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal static Errortype CountWithInDistance(List<int> samples, List<Point> edgePoints, double distanceThreshold, out List<double> inlinePx, out List<double> inlinePy, out int inlineCounts)
        {
            inlineCounts = 0;
            inlinePx = new List<double>();
            inlinePy = new List<double>();

            if (samples.Count != 2)
            {
                return Errortype.RANSAC_SAMPLE_COUNT_ERROR;
            }

            if (samples[0] >= edgePoints.Count || samples[1] >= edgePoints.Count)
            {
                return Errortype.RANSAC_SAMPLE_INDEX_ERROR;
            }

            double mc1x = edgePoints[samples[0]].X;
            double mc1y = edgePoints[samples[0]].Y;
            double mc2x = edgePoints[samples[1]].X;
            double mc2y = edgePoints[samples[1]].Y;

            for (int i = 0; i < edgePoints.Count; ++i)
            {
                double px = edgePoints[i].X;
                double py = edgePoints[i].Y;

                double a, b, c, p, s;
                a = Math.Sqrt((mc1x - px) * (mc1x - px) + (mc1y - py) * (mc1y - py));
                b = Math.Sqrt((mc2x - px) * (mc2x - px) + (mc2y - py) * (mc2y - py));
                c = Math.Sqrt((mc1x - mc2x) * (mc1x - mc2x) + (mc1y - mc2y) * (mc1y - mc2y));
                p = (a + b + c) / 2.0;
                s = Math.Sqrt(p * (p - a) * (p - b) * (p - c));
                double pDistance = 2 * s / c;

                if (pDistance <= distanceThreshold)
                {
                    inlinePx.Add(px);
                    inlinePy.Add(py);
                    inlineCounts++;
                }
            }

            return Errortype.OK;
        }
    }
}
