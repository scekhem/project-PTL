using System;
using System.Collections.Generic;

namespace Supercluster.KDTree.Utilities
{
    using System.Linq;

    using Supercluster.KDTree;

    /// <summary>
    /// 公用程序
    /// </summary>
    public static class Utilities
    {
        #region Metrics
        private static Func<float[], float[], double> _l2Norm_Squared_Float = (x, y) =>
        {
            float dist = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }

            return dist;
        };

        private static Func<double[], double[], double> _l2Norm_Squared_Double = (x, y) =>
        {
            double dist = 0f;
            for (int i = 0; i < x.Length; i++)
            {
                dist += (x[i] - y[i]) * (x[i] - y[i]);
            }

            return dist;
        };

        /// <summary>
        /// Gets _l2Norm_Squared_Float
        /// </summary>
        public static Func<float[], float[], double> L2Norm_Squared_Float
        {
            get
            {
                return _l2Norm_Squared_Float;
            }
        }

        /// <summary>
        /// Gets _l2Norm_Squared_Double
        /// </summary>
        public static Func<double[], double[], double> L2Norm_Squared_Double
        {
            get
            {
                return _l2Norm_Squared_Double;
            }
        }
        #endregion

        #region Data Generation

        /// <summary>
        /// 生成二维double数组
        /// </summary>
        /// <param name="points">一维长度</param>
        /// <param name="range">范围</param>
        /// <param name="dimensions">二维长度</param>
        /// <returns>二维double数组</returns>
        public static double[][] GenerateDoubles(int points, double range, int dimensions)
        {
            var data = new List<double[]>();
            var random = new Random();

            for (var i = 0; i < points; i++)
            {
                var array = new double[dimensions];
                for (var j = 0; j < dimensions; j++)
                {
                    array[j] = random.NextDouble() * range;
                }

                data.Add(array);
            }

            return data.ToArray();
        }

        /// <summary>
        /// 生成二维double数组
        /// </summary>
        /// <param name="points">一维长度</param>
        /// <param name="range">范围</param>
        /// <returns>二维double数组</returns>
        public static double[][] GenerateDoubles(int points, double range)
        {
            var data = new List<double[]>();
            var random = new Random();

            for (int i = 0; i < points; i++)
            {
                data.Add(new double[] { (random.NextDouble() * range), (random.NextDouble() * range) });
            }

            return data.ToArray();
        }

        /// <summary>
        /// 生成二维float数组
        /// </summary>
        /// <param name="points">一维长度</param>
        /// <param name="range">范围</param>
        /// <returns>二维float数组</returns>
        public static float[][] GenerateFloats(int points, double range)
        {
            var data = new List<float[]>();
            var random = new Random();

            for (int i = 0; i < points; i++)
            {
                data.Add(new float[] { (float)(random.NextDouble() * range), (float)(random.NextDouble() * range) });
            }

            return data.ToArray();
        }

        /// <summary>
        /// 生成二维float数组
        /// </summary>
        /// <param name="points">一维长度</param>
        /// <param name="range">范围</param>
        /// <param name="dimensions">二维长度</param>
        /// <returns>二维float数组</returns>
        public static float[][] GenerateFloats(int points, double range, int dimensions)
        {
            var data = new List<float[]>();
            var random = new Random();

            for (var i = 0; i < points; i++)
            {
                var array = new float[dimensions];
                for (var j = 0; j < dimensions; j++)
                {
                    array[j] = (float)(random.NextDouble() * range);
                }

                data.Add(array);
            }

            return data.ToArray();
        }
        #endregion

        #region Searches

        /// <summary>
        /// Performs a linear search on a given points set to find a nodes that is closest to the given nodes
        /// </summary>
        /// <typeparam name="T">泛型参数</typeparam>
        /// <param name="data">nodes集</param>
        /// <param name="point">关键nodes</param>
        /// <param name="metric">委托函数</param>
        /// <returns>float nodes</returns>
        public static T[] LinearSearch<T>(T[][] data, T[] point, Func<T[], T[], float> metric)
        {
            var bestDist = Double.PositiveInfinity;
            T[] bestPoint = null;

            for (int i = 0; i < data.Length; i++)
            {
                var currentDist = metric(point, data[i]);
                if (bestDist > currentDist)
                {
                    bestDist = currentDist;
                    bestPoint = data[i];
                }
            }

            return bestPoint;
        }

        /// <summary>
        /// Performs a linear search on a given points set to find a nodes that is closest to the given nodes
        /// </summary>
        /// <typeparam name="T">泛型参数</typeparam>
        /// <param name="data">nodes集</param>
        /// <param name="point">关键nodes</param>
        /// <param name="metric">委托函数</param>
        /// <returns>double nodes</returns>
        public static T[] LinearSearch<T>(T[][] data, T[] point, Func<T[], T[], double> metric)
        {
            var bestDist = Double.PositiveInfinity;
            T[] bestPoint = null;

            for (int i = 0; i < data.Length; i++)
            {
                var currentDist = metric(point, data[i]);
                if (bestDist > currentDist)
                {
                    bestDist = currentDist;
                    bestPoint = data[i];
                }
            }

            return bestPoint;
        }

        /// <summary>
        /// Performs a linear search on a given TPoints set to find a TNodes that is closest to the given TNodes
        /// </summary>
        /// <typeparam name="TPoint">泛型参数名1</typeparam>
        /// <typeparam name="TNode">泛型参数名2</typeparam>
        /// <param name="points">TPoint集</param>
        /// <param name="nodes">TNode集</param>
        /// <param name="target">目标TPoint</param>
        /// <param name="metric">委托函数</param>
        /// <returns>Tuple<TPoint[], TNode></returns>
        public static Tuple<TPoint[], TNode> LinearSearch<TPoint, TNode>(TPoint[][] points, TNode[] nodes, TPoint[] target, Func<TPoint[], TPoint[], double> metric)
        {
            var bestIndex = 0;
            var bestDist = Double.MaxValue;

            for (int i = 0; i < points.Length; i++)
            {
                var currentDist = metric(points[i], target);
                if (bestDist > currentDist)
                {
                    bestDist = currentDist;
                    bestIndex = i;
                }
            }

            return new Tuple<TPoint[], TNode>(points[bestIndex], nodes[bestIndex]);
        }

        /// <summary>
        /// Performs a linear radial search on a given Points set to find a Nodes that is closes to the given Nodes
        /// </summary>
        /// <typeparam name="T">泛型参数</typeparam>
        /// <param name="data">泛型参数集</param>
        /// <param name="point">关键node</param>
        /// <param name="metric">委托函数</param>
        /// <param name="radius">半径</param>
        /// <returns>T[][]</returns>
        public static T[][] LinearRadialSearch<T>(T[][] data, T[] point, Func<T[], T[], double> metric, double radius)
        {
            var pointsInRadius = new BoundedPriorityList<T[], double>(data.Length, true);

            for (int i = 0; i < data.Length; i++)
            {
                var currentDist = metric(point, data[i]);
                if (radius >= currentDist)
                {
                    pointsInRadius.Add(data[i], currentDist);
                }
            }

            return pointsInRadius.ToArray();
        }

        /// <summary>
        /// Performs a linear radial search on a given TPoints set to find a TNodes that is closes to the given TNodes
        /// </summary>
        /// <typeparam name="TPoint">泛型参数名1</typeparam>
        /// <typeparam name="TNode">泛型参数名2</typeparam>
        /// <param name="points">TPoint集</param>
        /// <param name="nodes">关键TNode</param>
        /// <param name="target">目标TPoint</param>
        /// <param name="metric">委托函数</param>
        /// <param name="radius">半径</param>
        /// <returns>Tuple<TPoint[], TNode>[]</returns>
        public static Tuple<TPoint[], TNode>[] LinearRadialSearch<TPoint, TNode>(TPoint[][] points, TNode[] nodes, TPoint[] target, Func<TPoint[], TPoint[], double> metric, double radius)
        {
            var pointsInRadius = new BoundedPriorityList<int, double>(points.Length, true);

            for (int i = 0; i < points.Length; i++)
            {
                var currentDist = metric(target, points[i]);
                if (radius * radius >= currentDist)
                {
                    pointsInRadius.Add(i, currentDist);
                }
            }

            return pointsInRadius.Select(idx => new Tuple<TPoint[], TNode>(points[idx], nodes[idx])).ToArray();
        }

        #endregion
    }
}
