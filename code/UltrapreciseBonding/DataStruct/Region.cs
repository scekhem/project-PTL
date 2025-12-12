using DataStruct;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace DataStruct
{
    /// <summary>
    /// 区域
    /// </summary>
    public class Region
    {
        /// <summary>
        /// Gets or sets the user's 正矩形区域
        /// </summary>
        public Rectangle1 Rectangle1 { get; set; }

        /// <summary>
        /// Gets or sets the user's 圆形区域
        /// </summary>
        public Circle Circle { get; set; }

        /// <summary>
        /// Gets or sets the user's 多段线区域
        /// </summary>
        public List<Point> PolygonPoints { get; set; }

        /// <summary>
        /// Gets or sets the user's 正矩形集合区域
        /// </summary>
        public List<Rectangle1> Rectangle1List { get; set; }

        /// <summary>
        /// Gets or sets the user's 多段线集合区域
        /// </summary>
        public List<List<Point>> PolygonPointsList { get; set; }

        /// <summary>
        /// 生成区域HObject
        /// </summary>
        /// <returns>区域</returns>
        public HObject GenRegion()
        {
            HObject regionOut;
            HOperatorSet.GenEmptyObj(out regionOut);
            HOperatorSet.GenEmptyObj(out HObject regionUnion);

            if (Rectangle1 != null)
            {
                HOperatorSet.GenRectangle1(out HObject rectangle1, Rectangle1.Start_Y, Rectangle1.Start_X, Rectangle1.End_Y, Rectangle1.End_X);
                HOperatorSet.Union2(regionOut, rectangle1, out regionUnion);
                regionOut.Dispose();
                regionOut = regionUnion.Clone();
                regionUnion.Dispose();
            }

            if (Circle != null)
            {
                HOperatorSet.GenCircle(out HObject circle, Circle.CenterY, Circle.CenterX, Circle.Radius);
                HOperatorSet.Union2(regionOut, circle, out regionUnion);
                regionOut.Dispose();
                regionOut = regionUnion.Clone();
                regionUnion.Dispose();
            }

            if (PolygonPoints != null)
            {
                ComAlgo.GetPolygonRegion(PolygonPoints, out HObject polygonRegion, 65535, 65535); //这里默认图像最大65535
                HOperatorSet.Union2(regionOut, polygonRegion, out regionUnion);
                regionOut.Dispose();
                regionOut = regionUnion.Clone();
                regionUnion.Dispose();
            }

            if (Rectangle1List != null)
            {
                for (int i = 0; i < Rectangle1List.Count; i++)
                {
                    HOperatorSet.GenRectangle1(out HObject rectangle1Temp, Rectangle1List[i].Start_Y, Rectangle1List[i].Start_X, Rectangle1List[i].End_Y, Rectangle1List[i].End_X);
                    HOperatorSet.Union2(regionOut, rectangle1Temp, out regionUnion);
                    regionOut.Dispose();
                    regionOut = regionUnion.Clone();
                    regionUnion.Dispose();
                }
            }

            if (PolygonPointsList != null)
            {
                for (int i = 0; i < PolygonPointsList.Count; i++)
                {
                    ComAlgo.GetPolygonRegion(PolygonPointsList[i], out HObject polygonRegionTemp, 65535, 65535); //这里默认图像最大65535
                    HOperatorSet.Union2(regionOut, polygonRegionTemp, out regionUnion);
                    regionOut.Dispose();
                    regionOut = regionUnion.Clone();
                    regionUnion.Dispose();
                }
            }

            return regionOut;
        }
    }
}
