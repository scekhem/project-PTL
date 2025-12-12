using DataStruct;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using IniFileHelper;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using UltrapreciseBonding.TemplateMatch;
using Supercluster.KDTree;
using Supercluster.KDTree.Utilities;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Diagnostics;
using OpenCvSharp.Flann;

namespace UltrapreciseBonding.MarkLocation
{
    /// <summary>
    /// 模板匹配使用到的一些通用方法，后续可考虑移植到common中
    /// </summary>
    public static class Common
    {
        /// <summary>
        /// 寻找四条线段的中心
        /// </summary>
        /// <param name="lines">线段集合，大小为4</param>
        /// <param name="center">中心点坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Calc4LineCenter(List<LineSeg> lines, out Point center)
        {
            center = new Point();
            if (lines is null)
            {
                return Errortype.MARKLOCATIONCOMMON_LINE_NULL;
            }

            if (lines.Count != 4)
            {
                return Errortype.MARKLOCATIONCOMMON_LINE_NUM_ERROR;
            }

            List<Point> inters = new List<Point>();
            double intersXSum = 0;
            double intersYSum = 0;
            Point point = new Point();

            for (int i = 0; i < lines.Count - 1; ++i)
            {
                ComAlgo.IntersectionLines(lines[i], lines[i + 1], out point);
                inters.Add(point);
                intersXSum += point.X;
                intersYSum += point.Y;
            }

            ComAlgo.IntersectionLines(lines[0], lines[lines.Count - 1], out point);
            inters.Add(point);
            intersXSum += point.X;
            intersYSum += point.Y;

            center.X = intersXSum / lines.Count;
            center.Y = intersYSum / lines.Count;
            return Errortype.OK;
        }

        /// <summary>
        /// 转换线段位置，根据原始模板的中心和当前模板中心以及角度对线段做刚体变换
        /// </summary>
        /// <param name="linesIn">输入线段</param>
        /// <param name="baseCenter">原始的模板中心位置</param>
        /// <param name="curCenter">当前模板中心位置</param>
        /// <param name="angle">模板匹配的角度结果</param>
        /// <param name="linesOut">输出的线段</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype TransAffineLine(List<LineSeg> linesIn, Point baseCenter, Point curCenter, double angle, out List<LineSeg> linesOut)
        {
            linesOut = new List<LineSeg>();
            if (linesIn is null || baseCenter is null || curCenter is null)
            {
                return Errortype.MARKLOCATIONCOMMON_INPUT_NULL;
            }

            HOperatorSet.VectorAngleToRigid(baseCenter.Y, baseCenter.X, 0, curCenter.Y, curCenter.X, angle, out HTuple homMat2D);
            HTuple rowsStart = new HTuple();
            HTuple colsStart = new HTuple();
            HTuple rowsEnd = new HTuple();
            HTuple colsEnd = new HTuple();

            for (int i = 0; i < linesIn.Count; i++)
            {
                rowsStart = rowsStart.TupleConcat(linesIn[i].Start_Y);
                colsStart = colsStart.TupleConcat(linesIn[i].Start_X);
                rowsEnd = rowsEnd.TupleConcat(linesIn[i].End_Y);
                colsEnd = colsEnd.TupleConcat(linesIn[i].End_X);
            }

            HOperatorSet.AffineTransPoint2d(homMat2D, rowsStart, colsStart, out HTuple rowsStartNew, out HTuple colsStartNew);
            HOperatorSet.AffineTransPoint2d(homMat2D, rowsEnd, colsEnd, out HTuple rowsEndNew, out HTuple colsEndNew);
            linesOut = new List<LineSeg>();
            for (int i = 0; i < linesIn.Count; i++)
            {
                LineSeg t = new LineSeg() { Start_X = colsStartNew[i].D, Start_Y = rowsStartNew[i].D, End_X = colsEndNew[i].D, End_Y = rowsEndNew[i].D };
                t.ProbInfo = linesIn[i].ProbInfo;       // 复制属性信息
                linesOut.Add(t);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 转换线段位置，根据原始模板的中心和当前模板中心以及角度对线段做刚体变换
        /// </summary>
        /// <param name="linesIn">输入线段</param>
        /// <param name="baseCenter">原始的模板中心位置</param>
        /// <param name="curCenter">当前模板中心位置</param>
        /// <param name="angle">模板匹配的角度结果</param>
        /// <param name="linesOut">输出的线段</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype TransAffineLine(List<List<LineSeg>> linesIn, Point baseCenter, Point curCenter, double angle, out List<List<LineSeg>> linesOut)
        {
            Errortype ret;
            linesOut = new List<List<LineSeg>>();
            for (int i = 0; i < linesIn.Count; i++)
            {
                ret = TransAffineLine(linesIn[i], baseCenter, curCenter, angle, out List<LineSeg> linesOutT);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                linesOut.Add(linesOutT);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 转化区域位置，根据原始模板的中心和当前模板中心以及角度对区域做刚体变换
        /// </summary>
        /// <param name="regionIn">仿射变换前区域位置</param>
        /// <param name="baseCenter">变换前圆心</param>
        /// <param name="curCenter">变换后圆心</param>
        /// <param name="angle">角度</param>
        /// <param name="regionOut">仿射变换后区域位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype TransAffineRegion(HObject regionIn, Point baseCenter, Point curCenter, double angle, out HObject regionOut)
        {
            HOperatorSet.GenEmptyObj(out regionOut);
            if (regionIn is null || !regionIn.IsInitialized() || baseCenter is null || curCenter is null)
            {
                return Errortype.MARKLOCATIONCOMMON_INPUT_NULL;
            }

            HOperatorSet.VectorAngleToRigid(baseCenter.Y, baseCenter.X, 0, curCenter.Y, curCenter.X, angle, out HTuple homMat2D);
            HOperatorSet.AffineTransRegion(regionIn, out regionOut, homMat2D, "nearest_neighbor");
            return Errortype.OK;
        }

        /// <summary>
        /// 计算点到线段的垂足
        /// </summary>
        /// <param name="p">输入点</param>
        /// <param name="line">输入线段</param>
        /// <param name="fp">输出垂足</param>
        /// <param name="flg">垂线角度是否在0-45°之间</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcFootPoint(Point p, LineSeg line, out Point fp, out bool flg)
        {
            fp = new Point();
            flg = true;

            if (p is null || line is null)
            {
                return Errortype.MARKLOCATIONCOMMON_INPUT_NULL;
            }

            if (line.Start_X == line.End_X && line.Start_Y == line.End_Y)
            {
                fp.X = line.Start_X;
                fp.Y = line.Start_Y;
                return Errortype.OK;
            }

            double x1 = line.Start_X;
            double x2 = line.End_X;
            double y1 = line.Start_Y;
            double y2 = line.End_Y;
            double x0 = p.X;
            double y0 = p.Y;
            double k = -((x1 - x0) * (x2 - x1) + (y1 - y0) * (y2 - y1)) / ((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2));
            fp.X = k * (x2 - x1) + x1;
            fp.Y = k * (y2 - y1) + y1;

            if (k < 0 || k > 1)
            {
                flg = false;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算平行线的中分线
        /// </summary>
        /// <param name="line1">输入线1</param>
        /// <param name="line2">输入线2</param>
        /// <param name="bisector">输出中分线</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcParallelLinesBisector(LineSeg line1, LineSeg line2, out LineSeg bisector)
        {
            bisector = new LineSeg();
            if (line1 is null || line2 is null)
            {
                return Errortype.MARKLOCATIONCOMMON_INPUT_NULL;
            }

            HOperatorSet.AngleLx(line1.Start_Y, line1.Start_X, line1.End_Y, line1.End_X, out HTuple hAngle1);
            double angle1 = hAngle1.D;
            HOperatorSet.AngleLx(line2.Start_Y, line2.Start_X, line2.End_Y, line2.End_X, out HTuple hAngle2);
            double angle2 = hAngle2.D;

            double deltaAngle = Math.Abs(angle1 - angle2);
            if (deltaAngle < Math.PI / 4 || deltaAngle > Math.PI * 7 / 4)
            {
                bisector.Start_X = (line1.Start_X + line2.Start_X) / 2;
                bisector.Start_Y = (line1.Start_Y + line2.Start_Y) / 2;
                bisector.End_X = (line1.End_X + line2.End_X) / 2;
                bisector.End_Y = (line1.End_Y + line2.End_Y) / 2;
            }
            else
            {
                bisector.Start_X = (line1.Start_X + line2.End_X) / 2;
                bisector.Start_Y = (line1.Start_Y + line2.End_Y) / 2;
                bisector.End_X = (line1.End_X + line2.Start_X) / 2;
                bisector.End_Y = (line1.End_Y + line2.Start_Y) / 2;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算线段的旋转中心（粗略）
        /// </summary>
        /// <param name="lineSegs">输入线段</param>
        /// <param name="center">输出旋转中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcLinesRotateCenter(List<LineSeg> lineSegs, out Point center)
        {
            center = new Point();
            if (lineSegs is null)
            {
                return Errortype.MARKLOCATIONCOMMON_INPUT_NULL;
            }

            HTuple lineRows = new HTuple();
            HTuple lineCols = new HTuple();
            for (int i = 0; i < lineSegs.Count; i++)
            {
                lineRows = lineRows.TupleConcat(lineSegs[i].Start_Y, lineSegs[i].End_Y);
                lineCols = lineCols.TupleConcat(lineSegs[i].Start_X, lineSegs[i].End_X);
            }

            HOperatorSet.GenRegionPoints(out HObject pointsRegion, lineRows, lineCols);
            HOperatorSet.SmallestCircle(pointsRegion, out HTuple circleRow, out HTuple circleCol, out HTuple radius);
            center.X = circleCol;
            center.Y = circleRow;
            return Errortype.OK;
        }

        /// <summary>
        /// 根据线段计算对称中心
        /// </summary>
        /// <param name="lineSegs">线段集</param>
        /// <param name="center">对称中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcCenterByLine(List<LineSeg> lineSegs, out Point center)
        {
            center = new Point();
            Errortype ret = Errortype.OK;
            if (lineSegs is null)
            {
                return Errortype.MARKLOCATIONCOMMON_INPUT_NULL;
            }

            if (lineSegs.Count < 4)
            {
                return Errortype.MARKLOCATIONCOMMON_LINE_NUM_ERROR;
            }

            ret = CalcLinesRotateCenter(lineSegs, out Point rotateCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HTuple rows = new HTuple();
            HTuple cols = new HTuple();
            List<double[]> rawData = new List<double[]>();
            List<LineSeg> notes = new List<LineSeg>();
            for (int i = 0; i < lineSegs.Count; i++)
            {
                Point lineCenter = lineSegs[i].Center();
                rows = rows.TupleConcat(lineCenter.Y);
                cols = cols.TupleConcat(lineCenter.X);
                rawData.Add(new double[] { lineCenter.X, lineCenter.Y });
                notes.Add(lineSegs[i]);
            }

            HOperatorSet.HomMat2dIdentity(out HTuple homMat2DIdentity);
            HOperatorSet.HomMat2dRotate(homMat2DIdentity, Math.PI, rotateCenter.X, rotateCenter.Y, out HTuple homMat2DRotate);

            HOperatorSet.AffineTransPoint2d(homMat2DRotate, cols, rows, out HTuple qx, out HTuple qy);

            //建立二叉树，找最近点，最近点距离小于阈值，认为是点对，保存所有存在点对的数据，平均就是图像中心
            KDTree<double, LineSeg> kDTree = new KDTree<double, LineSeg>(2, rawData.ToArray(), notes.ToArray(), Utilities.L2Norm_Squared_Double);

            List<LineSeg> lineBisectorArray = new List<LineSeg>();

            //string path = @"D:\Alg\";
            //if (Directory.Exists(path)) Directory.Delete(path, true);
            //Directory.CreateDirectory(path);
            for (int i = 0; i < qx.Length; i++)
            {
                int num = 1;
                Point pointRotated = new Point(qx[i].D, qy[i].D);
                var result = kDTree.NearestNeighbors(new double[] { qx[i].D, qy[i].D }, num); //这里找最近的三条线，判断距离哪个最近，就用哪个
                int index = 0;
                double minDist = double.MaxValue;
                for (int j = 0; j < num; j++)
                {
                    LineSeg lineT = result[j].Item2;
                    HOperatorSet.DistancePl(pointRotated.Y, pointRotated.X, lineT.Start_Y, lineT.Start_X, lineT.End_Y, lineT.End_X, out HTuple distancePL);
                    if (minDist > distancePL)
                    {
                        minDist = distancePL;
                        index = j;
                    }
                }

                LineSeg lineResult = result[index].Item2;

                //多判断一步  他们是否是平行的
                double angleLLAbs = Math.Abs(lineResult.CalculateLineDeg() - lineSegs[i].CalculateLineDeg());
                if (angleLLAbs > 10 && angleLLAbs < 170)
                {
                    continue;
                }

                CalcParallelLinesBisector(lineSegs[i], lineResult, out LineSeg bisector1);
                CalcParallelLinesBisector(lineResult, lineSegs[i], out LineSeg bisector2);
                lineBisectorArray.Add(bisector1);

                //lineBisectorArray.Add(bisector2);
                //LineSeg t1 = new LineSeg(lineSegs[i].Start_X, lineSegs[i].Start_Y, lineSegs[i].End_X, lineSegs[i].End_Y);
                //t1.Shorten(1);
                //LineSeg t2 = new LineSeg(lineResult.Start_X, lineResult.Start_Y, lineResult.End_X, lineResult.End_Y);
                //t2.Shorten(1);
                //LineSeg t3 = new LineSeg(bisector1.Start_X, bisector1.Start_Y, bisector1.End_X, bisector1.End_Y);
                //t3.Shorten(1);
                //LineSeg t4 = new LineSeg(bisector2.Start_X, bisector2.Start_Y, bisector2.End_X, bisector2.End_Y);
                //t4.Shorten(1);

                //HOperatorSet.GenContourPolygonXld(out HObject regionLine1, new HTuple(t1.Start_Y, t1.End_Y), new HTuple(t1.Start_X, t1.End_X));
                //HOperatorSet.GenContourPolygonXld(out HObject regionLine2, new HTuple(t2.Start_Y, t2.End_Y), new HTuple(t2.Start_X, t2.End_X));
                //HOperatorSet.GenContourPolygonXld(out HObject regionLine3, new HTuple(t3.Start_Y, t3.End_Y), new HTuple(t3.Start_X, t3.End_X));
                //HOperatorSet.GenContourPolygonXld(out HObject regionLine4, new HTuple(t4.Start_Y, t4.End_Y), new HTuple(t4.Start_X, t4.End_X));
                //HOperatorSet.WriteObject(regionLine1, path + "region1_" + i.ToString() + ".hobj");
                //HOperatorSet.WriteObject(regionLine2, path + "region2_" + i.ToString() + ".hobj");
                //HOperatorSet.WriteObject(regionLine3, path + "region3_" + i.ToString() + ".hobj");
                //HOperatorSet.WriteObject(regionLine4, path + "region4_" + i.ToString() + ".hobj");
            }

            Point centerSum = new Point();
            List<double> xArray = new List<double>();
            List<double> yArray = new List<double>();

            int count = 0;
            for (int i = 0; i < lineBisectorArray.Count; i++)
            {
                for (int j = i + 1; j < lineBisectorArray.Count; j++)
                {
                    //多判断一步  他们是否是平行的
                    double angleLLAbs = Math.Abs(lineBisectorArray[i].CalculateLineDeg() - lineBisectorArray[j].CalculateLineDeg());
                    if (angleLLAbs < 22.5 || angleLLAbs > 157.5)
                    {
                        continue;
                    }

                    ComAlgo.IntersectionLines(lineBisectorArray[i], lineBisectorArray[j], out Point p);
                    centerSum += p;
                    xArray.Add(p.X);
                    yArray.Add(p.Y);
                    count++;
                }
            }

            center = new Point(centerSum.X / count, centerSum.Y / count);

            if (double.IsNaN(center.X) || double.IsNaN(center.Y))
            {
                if (ComAlgo.SaveFlg("CalcCenterByLineNaN", out int days))
                {
                    string path = @"D:\Alg\CalcCenterByLineNaN\";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string time = ComAlgo.GetDateTime();
                    string saveFile = path + time + ".ini";
                    for (int i = 0; i < lineSegs.Count; i++)
                    {
                        string section = i.ToString();
                        lineSegs[i].Save(saveFile, section);
                    }
                }

                return Errortype.MARKLOCATIONCOMMON_RESULT_NAN;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取mark的中心
        /// </summary>
        /// <param name="imgIn">输入图像</param>
        /// <param name="lineSegs">输入线段</param>
        /// <param name="caliperParams">卡尺提取参数</param>
        /// <param name="center">输出mark中心</param>
        /// <param name="straightnessErrorList">输出直线度误差</param>
        /// <param name="mask">输入掩膜区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMarkCenter(HObject imgIn, List<List<LineSeg>> lineSegs, CaliperParams caliperParams, out Point center, out List<double[]> straightnessErrorList, HObject mask = null)
        {
            Camera imgCamera = new Camera(imgIn);
            center = new Point();
            straightnessErrorList = new List<double[]>();
            double xSum = 0;
            double ySum = 0;
            Errortype ret;

            List<LineSeg> lineAllList = new List<LineSeg>();

            for (int i = 0; i < lineSegs.Count; i++)
            {
                lineAllList.AddRange(lineSegs[i]);
            }

            LineSeg[] lineExtractionAll = new LineSeg[lineAllList.Count];

            //Task[] taskarray = new Task[lineAllList.Count];
            //for (int i = 0; i < taskarray.Length; i++)
            //{
            //    int index = i;
            //    taskarray[index] = Task.Factory.StartNew(() =>
            //    {
            //        ret = CaliperLine.LineExtraction(imgCamera, lineAllList[index], caliperParams, out LineSeg lineExtraction, out double[] straightnessError, mask);

            //        if (ret == Errortype.OK)
            //        {
            //            lineExtractionAll[index] = lineExtraction;
            //        }
            //    });
            //}

            //Task.WaitAll(taskarray);
            for (int i = 0; i < lineAllList.Count; i++)
            {
                int index = i;
                ret = CaliperLine.LineExtraction(imgCamera, lineAllList[index], caliperParams, out LineSeg lineExtraction, out double[] straightnessError, mask);

                if (ret == Errortype.OK)
                {
                    lineExtractionAll[index] = lineExtraction;
                }
            }

            List<LineSeg> lineExtractionAllNoNull = new List<LineSeg>();
            for (int i = 0; i < lineExtractionAll.Length; i++)
            {
                if (lineExtractionAll[i] is null)
                {
                    continue;
                }

                lineExtractionAllNoNull.Add(lineExtractionAll[i]);
            }

            ret = CalcCenterByLine(lineExtractionAllNoNull, out center);
            if (ret != Errortype.OK)
            {
                imgCamera.Dispose();
                return ret;
            }

            imgCamera.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 验证模板mark中心并写入线段极性属性，在建立模板时使用
        /// </summary>
        /// <param name="imgIn">输入图像</param>
        /// <param name="lineSegs">线段集</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="center"> 线段中心</param>
        /// <param name="lineExtractions">卡尺提取到的直线</param>
        /// <param name="straightnessErrorList">垂直度误差</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CheckMarkCenter(HObject imgIn, List<List<LineSeg>> lineSegs, CaliperParams caliperParams, out Point center, out List<List<LineSeg>> lineExtractions, out List<double[]> straightnessErrorList, HObject mask = null)
        {
            Camera imgCamera = new Camera(imgIn);
            center = new Point();
            straightnessErrorList = new List<double[]>();
            double xSum = 0;
            double ySum = 0;
            Errortype ret;

            lineExtractions = new List<List<LineSeg>>();
            List<LineSeg> lineExtractionAll = new List<LineSeg>();
            for (int edgeIndex = 0; edgeIndex < lineSegs.Count; edgeIndex++)
            {
                List<LineSeg> oneEdge = lineSegs[edgeIndex];
                for (int i = 0; i < oneEdge.Count; i++)
                {
                    oneEdge[i].ProbInfo = String.Empty;
                    ret = CaliperLine.LineExtraction(imgCamera, oneEdge[i], caliperParams, out LineSeg lineExtraction, out double[] straightnessError, mask);
                    straightnessErrorList.Add(straightnessError);
                    if (ret != Errortype.OK)
                    {
                        //imgCamera.Dispose();
                        //return ret;
                        continue;
                    }

                    lineExtractionAll.Add(lineExtraction);
                    oneEdge[i].ProbInfo = lineExtraction.ProbInfo;
                }

                lineExtractions.Add(oneEdge);
            }

            ret = CalcCenterByLine(lineExtractionAll, out center);
            if (ret != Errortype.OK)
            {
                imgCamera.Dispose();
                return ret;
            }

            imgCamera.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 对所有的线条进行排序，目标使其收尾相接
        /// </summary>
        /// <param name="lineSegsIn">输入的线条，第一层集合为不同的边缘，第二层集合为同一边缘的不同线段</param>
        /// <param name="lineSegsOut">输出的线条</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SortEdgeLine(List<List<LineSeg>> lineSegsIn, out List<List<LineSeg>> lineSegsOut)
        {
            lineSegsOut = new List<List<LineSeg>>();
            int indexOffset = 0;
            for (int i = 0; i < lineSegsIn.Count; i++)
            {
                if (lineSegsIn[i].Count <= 1)
                {
                    indexOffset++;
                    continue;
                }

                lineSegsOut.Add(new List<LineSeg>());

                LineSeg cur = lineSegsIn[i][0];
                lineSegsOut[i - indexOffset].Add(cur);

                List<LineSeg> restArray = new List<LineSeg>();
                for (int j = 1; j < lineSegsIn[i].Count; j++)
                {
                    restArray.Add(lineSegsIn[i][j]);
                }

                HTuple distArray = new HTuple();
                while (restArray.Count > 0)
                {
                    double dist = double.MaxValue;
                    int index = 0;
                    bool isInvert = false;

                    //找到最近的线段
                    for (int j = 0; j < restArray.Count; j++)
                    {
                        double d1 = Math.Sqrt(Math.Pow(cur.End_X - restArray[j].Start_X, 2) + Math.Pow(cur.End_Y - restArray[j].Start_Y, 2));
                        if (d1 < dist)
                        {
                            index = j;
                            dist = d1;
                            isInvert = false;
                        }

                        double d2 = Math.Sqrt(Math.Pow(cur.End_X - restArray[j].End_X, 2) + Math.Pow(cur.End_Y - restArray[j].End_Y, 2));
                        if (d2 < dist)
                        {
                            index = j;
                            dist = d2;
                            isInvert = true;
                        }
                    }

                    //if (distArray.TupleLength() > 1)
                    //{
                    //    if (dist > distArray.TupleMean().D * 10)
                    //    {
                    //        lineSegsOut.Add(new List<LineSeg>());
                    //        indexOffset--;
                    //        distArray = new HTuple();
                    //        cur = restArray[index];
                    //        lineSegsOut[i - indexOffset].Add(cur);
                    //        restArray.RemoveAt(index);
                    //        continue;
                    //    }
                    //}
                    distArray = distArray.TupleConcat(dist);
                    cur = restArray[index];

                    if (isInvert)
                    {
                        cur = new LineSeg(restArray[index].End_X, restArray[index].End_Y, restArray[index].Start_X, restArray[index].Start_Y);
                    }

                    lineSegsOut[i - indexOffset].Add(cur);
                    restArray.RemoveAt(index);
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 筛选线段
        /// </summary>
        /// <param name="lineSegsIn">输入线段</param>
        /// <param name="lineSegsOut">输出线段</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype FiltrationLine(List<List<LineSeg>> lineSegsIn, out List<List<LineSeg>> lineSegsOut)
        {
            int distThreshold = 5;
            int angleThreshold = 30;
            lineSegsOut = new List<List<LineSeg>>();
            if (lineSegsIn is null)
            {
                return Errortype.OK;
            }

            for (int i = 0; i < lineSegsIn.Count; i++)
            {
                if (lineSegsIn[i] is null)
                {
                    lineSegsOut.Add(new List<LineSeg>());
                    continue;
                }

                //少于4条线的不输出
                if (lineSegsIn[i].Count < 4)
                {
                    continue;
                }

                //少于6条线的 均全部输出
                if (lineSegsIn[i].Count <= 6)
                {
                    lineSegsOut.Add(lineSegsIn[i]);
                    continue;
                }

                //长度为12条线的 十字mark 直接输出
                if (lineSegsIn[i].Count == 12)
                {
                    lineSegsOut.Add(lineSegsIn[i]);
                    continue;
                }

                //对线段进行排序  如果最长的线段只有4个 则选择使用最长的4个线段 
                //如果所有线段的长度均接近(distThreshold个像素内均认为长度接近），则选择较为水平的4个
                //如果所有线段的长度不一致，则全部使用
                //判断留下的线段 相邻的角度接近90度 否则失败 保留原始线段
                List<LineSeg> result = new List<LineSeg>();

                //根据线段长度对所有线段分类
                HTuple distTuple = new HTuple();
                HTuple indexTuple = new HTuple();
                for (int j = 0; j < lineSegsIn[i].Count; j++)
                {
                    distTuple = distTuple.TupleConcat(lineSegsIn[i][j].CalculateLineLength());
                    indexTuple = indexTuple.TupleConcat(j);
                }

                ComAlgo.SortPairs(distTuple, indexTuple, -1, out HTuple distSorted, out HTuple indexSorted);

                //所有线段长度接近
                if (distSorted[0] - distSorted[distSorted.TupleLength() - 1] < distThreshold)
                {
                    //按照角度进行
                    HTuple angleTuple = new HTuple();
                    indexTuple = new HTuple();
                    for (int j = 0; j < lineSegsIn[i].Count; j++)
                    {
                        angleTuple = angleTuple.TupleConcat(lineSegsIn[i][j].CalculateLineDeg());
                        indexTuple = indexTuple.TupleConcat(j);
                    }

                    //找到第一条接近0度或者90度的线(角度与0或者90之间小于angleThreshold度就算）  然后每n/4选一个  该前提是mark比较接近水平  如果mark没有接近水平的  则选择第一条为第一个
                    int firstIndex = 0;
                    for (int j = 0; j < angleTuple.TupleLength(); j++)
                    {
                        if (Math.Abs(angleTuple[j].D) < angleThreshold || Math.Abs(Math.Abs(angleTuple[j].D) - 90) < angleThreshold)
                        {
                            firstIndex = j;
                            break;
                        }
                    }

                    for (int j = firstIndex; j < angleTuple.TupleLength(); j += angleTuple.TupleLength() / 4)
                    {
                        result.Add(lineSegsIn[i][j]);
                    }
                }
                else
                {
                    for (int j = 0; j < distSorted.TupleLength() - 2; j++)
                    {
                        if (distSorted[j] - distSorted[j + 1] > distThreshold)
                        {
                            //最长的线段只有4个
                            if (j == 3)
                            {
                                //这里线段的顺序会被打乱
                                HOperatorSet.TupleSelectRange(indexSorted, 0, 3, out HTuple indexSortedSelected);
                                HOperatorSet.TupleSort(indexSortedSelected, out HTuple indexSortedSelectedSorted);
                                result.Add(lineSegsIn[i][indexSortedSelectedSorted[0]]);
                                result.Add(lineSegsIn[i][indexSortedSelectedSorted[1]]);
                                result.Add(lineSegsIn[i][indexSortedSelectedSorted[2]]);
                                result.Add(lineSegsIn[i][indexSortedSelectedSorted[3]]);
                                break;
                            }

                            //线段长度不一致，且最长的不止是4个，全部使用
                            else
                            {
                                result = lineSegsIn[i];
                                break;
                            }
                        }
                    }

                    //这里可能存在都不满足上面的条件，则输出所有的
                    if (result.Count == 0)
                    {
                        result = lineSegsIn[i];
                    }
                }

                lineSegsOut.Add(result);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 绘图，在输入图像中绘制输入的线段
        /// </summary>
        /// <param name="imgIn">输入图像</param>
        /// <param name="lineSegs">线段</param>
        /// <param name="imgOut">输出图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype PaintImgLine(HObject imgIn, List<List<LineSeg>> lineSegs, out HObject imgOut)
        {
            HObject imgR = imgIn.Clone();
            HObject imgG = imgIn.Clone();
            HObject imgB = imgIn.Clone();

            HOperatorSet.GenEmptyObj(out HObject regionLine);

            for (int i = 0; i < lineSegs.Count; i++)
            {
                for (int j = 0; j < lineSegs[i].Count; j++)
                {
                    HOperatorSet.GenRegionLine(out regionLine, lineSegs[i][j].Start_Y, lineSegs[i][j].Start_X, lineSegs[i][j].End_Y, lineSegs[i][j].End_X);
                    HOperatorSet.DilationCircle(regionLine, out regionLine, 1); //线条显示上增大
                    HOperatorSet.PaintRegion(regionLine, imgR, out imgR, 0, "fill");
                    HOperatorSet.PaintRegion(regionLine, imgG, out imgG, 255, "fill");
                    HOperatorSet.PaintRegion(regionLine, imgB, out imgB, 0, "fill");
                    regionLine.Dispose();
                }
            }

            HOperatorSet.Compose3(imgR, imgG, imgB, out imgOut);
            return Errortype.OK;
        }

        /// <summary>
        /// 移除重复线段
        /// </summary>
        /// <param name="linesIn">输入线段</param>
        /// <param name="linesOut">输出线段</param>
        /// <param name="minLength">最小长度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype RemoveDuplicateLine(List<LineSeg> linesIn, out List<LineSeg> linesOut, int minLength = 10)
        {
            linesOut = new List<LineSeg>();
            List<LineSeg> linesT = new List<LineSeg>();
            for (int i = 0; i < linesIn.Count; i++)
            {
                linesT.Add(linesIn[i]);
            }

            for (int i = 0; i < linesT.Count; i++)
            {
                //List<LineSeg> colline = new List<LineSeg>();
                //List<double> lineDist = new List<double>();
                List<KeyValuePair<LineSeg, double>> colLine = new List<KeyValuePair<LineSeg, double>>();
                colLine.Add(new KeyValuePair<LineSeg, double>(linesT[i], linesT[i].CalculateLineLength()));

                //colline.Add(linesT[i]);
                //lineDist.Add(linesT[i].CalculateLineLength());
                for (int j = i + 1; j < linesT.Count; j++)
                {
                    LineSeg line1 = linesT[i];
                    LineSeg line2 = linesT[j];
                    double angleAbs = Math.Abs(line1.CalculateLineDeg() - line2.CalculateLineDeg());
                    if (angleAbs < 10 || angleAbs > 170)
                    {
                        double distanceMin = ComAlgo.Dist(line1, line2, false);
                        if (distanceMin < minLength)
                        {
                            //colline.Add(linesT[j]);
                            //lineDist.Add(linesT[j].CalculateLineLength());
                            colLine.Add(new KeyValuePair<LineSeg, double>(linesT[j], linesT[j].CalculateLineLength()));
                            linesT.RemoveAt(j);
                            j--;
                        }
                    }
                }

                colLine.Sort((line1, line2) => line2.Value.CompareTo(line1.Value));

                LineSeg lineBase = colLine[0].Key;
                List<LineSeg> lineOutT = new List<LineSeg>();
                lineOutT.Add(lineBase);
                for (int j = 1; j < colLine.Count; j++)
                {
                    if (ComAlgo.Dist(lineBase, colLine[j].Key) > minLength)
                    {
                        bool flg = true;
                        for (int k = 0; k < lineOutT.Count; k++)
                        {
                            if (ComAlgo.Dist(lineOutT[k], colLine[j].Key) < minLength)
                            {
                                flg = false;
                            }
                        }

                        if (flg)
                        {
                            lineOutT.Add(colLine[j].Key);
                        }
                    }
                }

                linesOut.AddRange(lineOutT);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 调整线段的方向，让成组的线段的方向相同
        /// </summary>
        /// <param name="lineSegsIn">调整前输入线段</param>
        /// <param name="lineSegsOut">调整后线段</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype AdjustLineDirection(List<LineSeg> lineSegsIn, out List<LineSeg> lineSegsOut)
        {
            lineSegsOut = new List<LineSeg>();

            Errortype ret = CalcLinesRotateCenter(lineSegsIn, out Point rotateCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HTuple rows = new HTuple();
            HTuple cols = new HTuple();
            List<double[]> rawData = new List<double[]>();
            Dictionary<int, LineSeg> notes = new Dictionary<int, LineSeg>();
            for (int i = 0; i < lineSegsIn.Count; i++)
            {
                Point lineCenter = lineSegsIn[i].Center();
                rows = rows.TupleConcat(lineCenter.Y);
                cols = cols.TupleConcat(lineCenter.X);
                rawData.Add(new double[] { lineCenter.X, lineCenter.Y });
                notes.Add(i, lineSegsIn[i]);
            }

            HOperatorSet.HomMat2dIdentity(out HTuple homMat2DIdentity);
            HOperatorSet.HomMat2dRotate(homMat2DIdentity, Math.PI, rotateCenter.X, rotateCenter.Y, out HTuple homMat2DRotate);
            HOperatorSet.AffineTransPoint2d(homMat2DRotate, cols, rows, out HTuple qx, out HTuple qy);

            //建立二叉树，找最近点，最近点距离小于阈值，认为是点对
            KDTree<double, KeyValuePair<int, LineSeg>> kDTree = new KDTree<double, KeyValuePair<int, LineSeg>>(2, rawData.ToArray(), notes.ToArray(), Utilities.L2Norm_Squared_Double);
            List<int> containIndex = new List<int>();

            for (int i = 0; i < qx.Length; i++)
            {
                if (containIndex.Contains(i))
                {
                    continue;
                }

                int num = 1;
                Point pointRotated = new Point(qx[i].D, qy[i].D);
                var result = kDTree.NearestNeighbors(new double[] { qx[i].D, qy[i].D }, num); //这里找最近的三条线，判断距离哪个最近，就用哪个
                int index = 0;
                double minDist = double.MaxValue;
                for (int j = 0; j < num; j++)
                {
                    LineSeg lineT = result[j].Item2.Value;
                    HOperatorSet.DistancePl(pointRotated.Y, pointRotated.X, lineT.Start_Y, lineT.Start_X, lineT.End_Y, lineT.End_X, out HTuple distancePL);
                    if (minDist > distancePL)
                    {
                        minDist = distancePL;
                        index = j;
                    }
                }

                LineSeg lineResult = result[index].Item2.Value;
                int lineResultIndex = result[index].Item2.Key;

                if (containIndex.Contains(lineResultIndex))
                {
                    continue;
                }

                //多判断一步  他们是否是平行的
                double angleLLAbs = Math.Abs(lineResult.CalculateLineDeg() - lineSegsIn[i].CalculateLineDeg());
                if (angleLLAbs > 10 && angleLLAbs < 170)
                {
                    continue;
                }

                LineSeg line1 = lineSegsIn[i].Clone();
                LineSeg line2 = lineResult.Clone();

                Point centerLl = (line1.Center() + line2.Center()) / 2;

                HOperatorSet.DistancePl(centerLl.Y, centerLl.X, line1.Start_Y, line1.Start_X, line1.End_Y, line1.End_X, out HTuple distToLine1);
                HOperatorSet.DistancePl(centerLl.Y, centerLl.X, line2.Start_Y, line2.Start_X, line2.End_Y, line2.End_X, out HTuple distToLine2);

                if ((distToLine1.D + distToLine2.D) < 10)
                {
                    //两条线非常平行的情况
                    //st与center的距离 与 ed与center的距离 对比结果 相同是对的
                    double distSt = ComAlgo.Dist(line1.Start, centerLl);
                    double distEd = ComAlgo.Dist(line1.End, centerLl);
                    bool lineSegsInBool = distSt < distEd;
                    distSt = ComAlgo.Dist(line2.Start, centerLl);
                    distEd = ComAlgo.Dist(line2.End, centerLl);
                    bool lineResultBool = distSt < distEd;
                    if (lineSegsInBool == lineResultBool)
                    {
                        lineSegsOut.Add(line1);
                        lineSegsOut.Add(line2);
                    }
                    else
                    {
                        lineSegsOut.Add(line1);
                        lineSegsOut.Add(new LineSeg(line2.End, line2.Start));
                    }
                }
                else
                {
                    //两条线不平行的情况
                    //st连线与ed连线的交点 靠近center的是对的
                    ComAlgo.IntersectionLines(new LineSeg(line1.Start, line2.Start), new LineSeg(line1.End, line2.End), out Point intersectionSs);
                    double distSs = ComAlgo.Dist(intersectionSs, centerLl);
                    ComAlgo.IntersectionLines(new LineSeg(line1.Start, line2.End), new LineSeg(line1.End, line2.Start), out Point intersectionSe);
                    double distSe = ComAlgo.Dist(intersectionSe, centerLl);
                    if (distSs < distSe)
                    {
                        lineSegsOut.Add(line1);
                        lineSegsOut.Add(line2);
                    }
                    else
                    {
                        lineSegsOut.Add(line1);
                        lineSegsOut.Add(new LineSeg(line2.End, line2.Start));
                    }
                }

                containIndex.Add(i);
                containIndex.Add(lineResultIndex);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算滞后阈值的自适应参数
        /// </summary>
        /// <param name="imgIn">输入图像</param>
        /// <param name="low">输出low阈值</param>
        /// <param name="high">输出high阈值</param>
        /// <param name="domainRects">输入操作区域</param>
        /// <param name="maskRects">输入掩膜区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcHysteresisThreshold(HObject imgIn, out double low, out double high, HObject domainRects = null, HObject maskRects = null)
        {
            low = 10;
            high = 20;

            HOperatorSet.GenEmptyObj(out HObject regionDomain);
            HOperatorSet.GenEmptyObj(out HObject regionMask);

            HOperatorSet.GetImageSize(imgIn, out HTuple width, out HTuple height);
            if (domainRects == null)
            {
                HOperatorSet.GenRectangle1(out regionDomain, 0, 0, height, width);
            }
            else
            {
                regionDomain = domainRects.Clone();
            }

            if (maskRects != null && maskRects.IsInitialized())
            {
                regionMask = maskRects.Clone();
            }

            HOperatorSet.Difference(regionDomain, regionMask, out regionDomain);
            HOperatorSet.ReduceDomain(imgIn, regionDomain, out HObject imageReduced);

            HOperatorSet.EdgesImage(imageReduced, out HObject imaAmp, out HObject imaDir, "canny", 1, "nms", 10, 20);
            HOperatorSet.Threshold(imaAmp, out HObject region, 0, 255);
            HOperatorSet.GrayHisto(region, imaAmp, out HTuple absoluteHisto, out HTuple relativeHisto);

            HOperatorSet.CreateFunct1dArray(absoluteHisto, out HTuple function);
            HOperatorSet.SmoothFunct1dGauss(function, 10, out HTuple smoothedFunction);
            HOperatorSet.DerivateFunct1d(smoothedFunction, "first", out HTuple derivative);
            HOperatorSet.ZeroCrossingsFunct1d(derivative, out HTuple zeroCrossings);
            HOperatorSet.GetYValueFunct1d(smoothedFunction, zeroCrossings, "constant", out HTuple yVal);
            HOperatorSet.TupleMax(yVal, out HTuple yValMax);
            HOperatorSet.TupleFind(yVal, yValMax, out HTuple yValIndex);

            for (double i = zeroCrossings[yValIndex].D; i > 0; i = i - 2)
            {
                HOperatorSet.GetYValueFunct1d(smoothedFunction, i, "constant", out HTuple yValI);
                if (yValI < yValMax / 10)
                {
                    low = i;
                    break;
                }
            }

            if (low < 10)
            {
                low = 10;
            }

            high = zeroCrossings[yValIndex].D;

            if (high < low)
            {
                high = low + 10;
            }

            regionDomain.Dispose();
            regionMask.Dispose();
            imageReduced.Dispose();
            imaAmp.Dispose();
            imaDir.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 使用梯度图的方式获取边缘线段
        /// </summary>
        /// <param name="imgIn">输入图像</param>
        /// <param name="lineSegs">边缘线段集合</param>
        /// <param name="domainRects">输入操作区域</param>
        /// <param name="maskRects">输入掩膜区域</param>
        /// <param name="minLength">线段最小长度（像素）</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetEdgeByCanny(HObject imgIn, out List<List<LineSeg>> lineSegs, HObject domainRects = null, HObject maskRects = null, int minLength = 10)
        {
            Errortype ret = Errortype.OK;
            CalcHysteresisThreshold(imgIn, out double cannyThresholdLow, out double cannyThresholdHeight, domainRects, maskRects);

            HOperatorSet.GenEmptyObj(out HObject imageReduced);
            HOperatorSet.GenEmptyObj(out HObject regionDomain);
            HOperatorSet.GenEmptyObj(out HObject regionMask);

            HOperatorSet.GetImageSize(imgIn, out HTuple width, out HTuple height);
            if (domainRects == null)
            {
                HOperatorSet.GenRectangle1(out regionDomain, 0, 0, height, width);
            }
            else
            {
                regionDomain = domainRects.Clone();
            }

            if (maskRects != null && maskRects.IsInitialized())
            {
                regionMask = maskRects.Clone();
            }

            HOperatorSet.Difference(regionDomain, regionMask, out regionDomain);
            HOperatorSet.ReduceDomain(imgIn, regionDomain, out imageReduced);
            HOperatorSet.EdgesImage(imageReduced, out HObject imaAmp, out HObject imaDir, "canny", 1, "nms", cannyThresholdLow, cannyThresholdHeight);
            HOperatorSet.Threshold(imaAmp, out HObject edgesRegions, 0, 255);
            HOperatorSet.DilationCircle(edgesRegions, out HObject edgesDilationRegions, 2);
            HOperatorSet.Union1(edgesDilationRegions, out HObject edgesDilationUnionRegions);
            HOperatorSet.Connection(edgesDilationUnionRegions, out HObject edgesConnections);

            lineSegs = new List<List<LineSeg>>(); //最终的结果，第一个list为轮廓的集合，第二个list为每个轮廓包含的线段
            for (int edgeIndex = 1; edgeIndex <= edgesConnections.CountObj(); edgeIndex++)
            {
                HOperatorSet.SelectObj(edgesConnections, out HObject edgeSelected, edgeIndex);
                HOperatorSet.ReduceDomain(imageReduced, edgeSelected, out HObject imgReduced);
                imaAmp.Dispose();
                imaDir.Dispose();
                HOperatorSet.EdgesImage(imgReduced, out imaAmp, out imaDir, "canny", 1, "nms", cannyThresholdLow, cannyThresholdHeight);

                HOperatorSet.Threshold(imaAmp, out HObject edgeRegion, 0, 255);
                HOperatorSet.AreaCenter(edgeRegion, out HTuple area, out HTuple row, out HTuple column);
                if (area.D < minLength * 4)
                {
                    continue;
                }

                HOperatorSet.GrayHisto(edgeRegion, imaDir, out HTuple absoluteHisto, out HTuple relativeHisto);
                HTuple relativeHistoPi = relativeHisto.TupleSelectRange(0, 179);
                HTuple relativeHistoPi3 = new HTuple();
                relativeHistoPi3 = relativeHistoPi3.TupleConcat(relativeHistoPi, relativeHistoPi, relativeHistoPi);

                HOperatorSet.CreateFunct1dArray(relativeHistoPi3, out HTuple function);
                HOperatorSet.SmoothFunct1dGauss(function, 4, out HTuple smoothedFunction);
                HOperatorSet.DerivateFunct1d(smoothedFunction, "first", out HTuple derivativeFirst);
                HOperatorSet.DerivateFunct1d(smoothedFunction, "second", out HTuple derivativeSecond);
                HOperatorSet.ZeroCrossingsFunct1d(derivativeFirst, out HTuple zeroCrossings);

                //scale derivativeSecond 解决部分图像在二阶导附近可能存在小于0的情况
                //放大后进行平滑，将异常值拉回
                HOperatorSet.ScaleYFunct1d(derivativeSecond, 10, 0, out HTuple derivativeSecondScaled);
                HOperatorSet.SmoothFunct1dGauss(derivativeSecondScaled, 2, out HTuple derivativeSecondScaledSmoothed);

                List<double> peakValue = new List<double>();
                for (int i = 0; i < zeroCrossings.Length - 1; i++)
                {
                    if (zeroCrossings[i].D < 180 || zeroCrossings[i].D > 360)
                    {
                        continue;
                    }

                    HOperatorSet.GetYValueFunct1d(derivativeSecondScaledSmoothed, zeroCrossings[i], "constant", out HTuple y);
                    if (y < 0)
                    {
                        continue;
                    }

                    peakValue.Add(zeroCrossings[i].D - 180);
                }

                HObject region = new HObject();
                List<LineSeg> lineSegsOneEdge = new List<LineSeg>();
                for (int peakValueIndex = 0; peakValueIndex < peakValue.Count; peakValueIndex++)
                {
                    region.Dispose();
                    if (peakValueIndex == 0)
                    {
                        HOperatorSet.Threshold(imaDir, out HObject regionT1, 0, peakValue[0]);
                        HOperatorSet.Threshold(imaDir, out HObject regionT2, peakValue[peakValue.Count - 1], 180);
                        HOperatorSet.Union2(regionT1, regionT2, out region);
                    }
                    else
                    {
                        HOperatorSet.Threshold(imaDir, out region, peakValue[peakValueIndex - 1], peakValue[peakValueIndex]);
                    }

                    HOperatorSet.ClosingCircle(region, out HObject regionClosing, 3);
                    HOperatorSet.Connection(regionClosing, out HObject connectedRegions);
                    int connectedRegionsNumber = connectedRegions.CountObj();

                    HOperatorSet.GenEmptyObj(out HObject contourArray);
                    for (int lineRegionIndex = 1; lineRegionIndex <= connectedRegionsNumber; lineRegionIndex++)
                    {
                        HOperatorSet.SelectObj(connectedRegions, out HObject regionSelected, lineRegionIndex);
                        HOperatorSet.GetRegionPoints(regionSelected, out HTuple rows, out HTuple columns);
                        if (rows.Length < minLength)
                        {
                            regionSelected.Dispose();
                            continue;
                        }

                        HOperatorSet.TupleMax(rows, out HTuple rowsMax);
                        HOperatorSet.TupleMax(columns, out HTuple columnsMax);
                        HOperatorSet.TupleMin(rows, out HTuple rowsMin);
                        HOperatorSet.TupleMin(columns, out HTuple columnsMin);
                        int sortBy = 1;
                        if (columnsMax - columnsMin > rowsMax - rowsMin)
                        {
                            sortBy = 2;
                        }

                        ComAlgo.SortPairs(rows, columns, sortBy, out HTuple rowsSorted, out HTuple columnsSorted);
                        HOperatorSet.GenContourPolygonXld(out HObject contour, rowsSorted, columnsSorted);

                        contourArray = contourArray.ConcatObj(contour);

                        contour.Dispose();
                        regionSelected.Dispose();
                    }

                    HOperatorSet.UnionCollinearContoursXld(contourArray, out HObject contourArrayUnion, 20, 0.3, 3, 0.08, "attr_keep");

                    for (int contourIndex = 1; contourIndex <= contourArrayUnion.CountObj(); contourIndex++)
                    {
                        HOperatorSet.SelectObj(contourArrayUnion, out HObject contourSelected, contourIndex);
                        HOperatorSet.FitLineContourXld(contourSelected, "tukey", -1, 0, 5, 2, out HTuple rowBegin, out HTuple colBegin, out HTuple rowEnd, out HTuple colEnd, out HTuple nr, out HTuple nc, out HTuple dist);
                        HOperatorSet.DistancePp(rowBegin, colBegin, rowEnd, colEnd, out HTuple distance);
                        if (distance > minLength)
                        {
                            lineSegsOneEdge.Add(new LineSeg(colBegin, rowBegin, colEnd, rowEnd));
                        }
                    }

                    region.Dispose();
                    regionClosing.Dispose();
                    connectedRegions.Dispose();
                    contourArray.Dispose();
                    contourArrayUnion.Dispose();
                }

                //对lineseg集合进行去重
                ret = RemoveDuplicateLine(lineSegsOneEdge, out List<LineSeg> lineSegsOneEdgeRemoveDup, minLength);
                if (ret != Errortype.OK)
                {
                    edgeSelected.Dispose();
                    imgReduced.Dispose();
                    imaAmp.Dispose();
                    imaDir.Dispose();
                    edgeRegion.Dispose();
                    edgesRegions.Dispose();
                    edgesDilationRegions.Dispose();
                    edgesDilationUnionRegions.Dispose();
                    edgesConnections.Dispose();
                    return ret;
                }

                if (lineSegsOneEdgeRemoveDup.Count > 0)
                {
                    lineSegs.Add(lineSegsOneEdgeRemoveDup);
                }

                edgeSelected.Dispose();
                imgReduced.Dispose();
                imaAmp.Dispose();
                imaDir.Dispose();
                edgeRegion.Dispose();
            }

            edgesRegions.Dispose();
            edgesDilationRegions.Dispose();
            edgesDilationUnionRegions.Dispose();
            edgesConnections.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 直线提取测试 若提取失败，则返回结果无该线
        /// </summary>
        /// <param name="imgIn">输入图像</param>
        /// <param name="lineSegsIn">输入线段</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="lineSegsOut">输出线段</param>
        /// <param name="maskRects">掩膜区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype TryLineExtraction(HObject imgIn, List<LineSeg> lineSegsIn, CaliperParams caliperParams, out List<LineSeg> lineSegsOut, HObject maskRects = null)
        {
            lineSegsOut = new List<LineSeg>();
            Errortype ret = Errortype.OK;
            Camera imgCamera = new Camera(imgIn);

            for (int i = 0; i < lineSegsIn.Count / 2; i++)
            {
                ret = CaliperLine.CalcLinePolarity(imgCamera, lineSegsIn[i * 2 + 0], caliperParams, out string polarityValue1, maskRects);
                if (ret != Errortype.OK)
                {
                    continue;
                }

                ret = CaliperLine.CalcLinePolarity(imgCamera, lineSegsIn[i * 2 + 1], caliperParams, out string polarityValue2, maskRects);
                if (ret != Errortype.OK)
                {
                    continue;
                }

                string polaritySelected = "positive";

                if (polarityValue1 == polarityValue2)
                {
                    if (polarityValue1 == "all")
                    {
                        polaritySelected = "positive";
                    }
                    else
                    {
                        polaritySelected = polarityValue1;
                    }
                }
                else
                {
                    if (polarityValue1 == "all")
                    {
                        polaritySelected = polarityValue2;
                    }
                    else if (polarityValue2 == "all")
                    {
                        polaritySelected = polarityValue1;
                    }
                    else
                    {
                        continue;
                    }
                }

                LineSeg line1 = lineSegsIn[i * 2 + 0].Clone();
                line1.ProbInfo = polaritySelected;
                LineSeg line2 = lineSegsIn[i * 2 + 1].Clone();
                line2.ProbInfo = polaritySelected;

                ret = CaliperLine.LineExtraction(imgCamera, line1, caliperParams, out LineSeg lineOut1, out _, maskRects);
                if (ret != Errortype.OK)
                {
                    continue;
                }

                ret = CaliperLine.LineExtraction(imgCamera, line2, caliperParams, out LineSeg lineOut2, out _, maskRects);
                if (ret != Errortype.OK)
                {
                    continue;
                }

                lineSegsOut.Add(lineOut1);
                lineSegsOut.Add(lineOut2);
            }

            imgCamera.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 使用梯度图的方式获取边缘线段
        /// </summary>
        /// <param name="imgIn">输入图像</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="imgOut">输出绘制边缘的图像</param>
        /// <param name="lineSegs">边缘线段集合</param>
        /// <param name="domainRects">输入操作区域</param>
        /// <param name="maskRects">输入掩膜区域</param>
        /// <param name="minLength">线段最小长度（像素）</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMarkEdgeLine(HObject imgIn, CaliperParams caliperParams, out HObject imgOut, out List<List<LineSeg>> lineSegs, HObject domainRects = null, HObject maskRects = null, int minLength = 10)
        {
            Errortype ret = Errortype.OK;
            imgOut = imgIn.Clone();
            lineSegs = new List<List<LineSeg>>();

            ret = GetEdgeByCanny(imgIn, out List<List<LineSeg>> edgeLines, domainRects, maskRects, minLength);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            List<LineSeg> lineSegsAll = edgeLines.SelectMany(innerList => innerList).ToList();
            if (lineSegsAll.Count == 0)
            {
                return Errortype.OK;

                //return Errortype.MARKLOCATIONCOMMON_EDGELINENUM_ZERO;
            }

            ret = RemoveDuplicateLine(lineSegsAll, out List<LineSeg> lineSegsOneEdgeRemoveDupAll, minLength);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (lineSegsOneEdgeRemoveDupAll.Count == 0)
            {
                return Errortype.OK;

                //return Errortype.MARKLOCATIONCOMMON_EDGEREMOVELINENUM_ZERO;
            }

            //校准线的方向
            ret = AdjustLineDirection(lineSegsOneEdgeRemoveDupAll, out List<LineSeg> lineSegsAdjustT);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (lineSegsAdjustT.Count == 0)
            {
                return Errortype.OK;

                //return Errortype.MARKLOCATIONCOMMON_EDGEADJUSTLINENUM_ZERO;
            }

            // 每条线进行提取测试
            ret = TryLineExtraction(imgIn, lineSegsAdjustT, caliperParams, out List<LineSeg> extractedLines, maskRects);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (extractedLines.Count == 0)
            {
                return Errortype.OK;

                //return Errortype.MARKLOCATIONCOMMON_EDGEEXTRACTEDLINENUM_ZERO;
            }

            //绘图
            lineSegs = new List<List<LineSeg>>() { extractedLines };
            imgOut.Dispose();
            ret = PaintImgLine(imgIn, lineSegs, out imgOut);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 四条线确定mark中心
    /// </summary>
    internal class MarkFourLinesLocation
    {
        private string _name;
        private TemplateType _type;
        private Template _template;
        private List<CaliperParams> _caliperParams;
        private List<LineSeg> _lines;

        /// <summary>
        /// Gets the param _type
        /// </summary>
        public TemplateType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the param _name
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="type">模板类型</param>
        public MarkFourLinesLocation(string name, TemplateType type)
        {
            _name = name;
            _type = type;
            _template = new Template(name, type);
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="nccTemplateParams">创建模板参数</param>
        /// <param name="nccMatchParams">模板匹配参数</param>
        /// <param name="templateRegions">模板区域</param>
        /// <param name="templateMasks">掩膜区域</param>
        /// <param name="lines">线段集合</param>
        /// <param name="caliperParams">卡尺提取的参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CreateNccTemplate(HObject img, NccTemplateParams nccTemplateParams, NccMatchParams nccMatchParams, HObject templateRegions, HObject templateMasks, List<LineSeg> lines, List<CaliperParams> caliperParams)
        {
            Errortype ret = Errortype.OK;
            ret = _template.Create(img, nccTemplateParams, nccMatchParams, templateRegions, templateMasks);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _lines = lines;
            _caliperParams = caliperParams;
            return Errortype.OK;
        }

        /// <summary>
        /// 卡尺获取mark中心
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="matchRow">模板中心行坐标</param>
        /// <param name="matchCol">模板中心列坐标</param>
        /// <param name="matchAngle">模板匹配角度</param>
        /// <param name="centerX">输出中心x坐标</param>
        /// <param name="centerY">输出中心y坐标</param>
        /// <param name="mask">掩膜区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype GetMarkCenter(Camera img, double matchRow, double matchCol, double matchAngle, out double centerX, out double centerY, out List<double[]> straightnessErrorList, HObject mask = null)
        {
            centerX = 0;
            centerY = 0;
            straightnessErrorList = new List<double[]>();
            Errortype ret;
            Point curPoint = new Point() { X = matchCol, Y = matchRow };
            ret = Common.TransAffineLine(_lines, _template.CenterPoint, curPoint, matchAngle, out List<LineSeg> linesTrans);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = Common.TransAffineRegion(mask, _template.CenterPoint, curPoint, matchAngle, out HObject maskAffine);
            if (ret != Errortype.OK)
            {
                maskAffine.Dispose();
                return ret;
            }

            List<LineSeg> lineExtractionAll = new List<LineSeg>();
            for (int i = 0; i < linesTrans.Count; i++)
            {
                ret = CaliperLine.LineExtraction(img, linesTrans[i], _caliperParams[i], out LineSeg lineExtraction, out double[] straightnessError, maskAffine);
                straightnessErrorList.Add(straightnessError);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                lineExtractionAll.Add(lineExtraction);
            }

            ret = Common.Calc4LineCenter(lineExtractionAll, out Point center);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            centerX = center.X;
            centerY = center.Y;
            maskAffine.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// ncc模板匹配并提取中心
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scores">匹配结果得分集合</param>
        /// <param name="straightnessErrorList">垂直度误差</param>
        /// <param name="useMask">是否使用掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype NccMatchAndGetMarkCenter(HObject img, HObject matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scores, out List<List<double[]>> straightnessErrorList, bool useMask = true)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;
            straightnessErrorList = new List<List<double[]>>();
            Errortype ret = Errortype.OK;

            ret = _template.Match(img, matchRegion, out double[] matchRows, out double[] matchCols, out double[] matchAngles, out double[] scales, out double[] matchScores);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //对所有找到的mark做中心提取
            int num = matchRows.Length;
            rows = new double[num];
            cols = new double[num];
            angles = matchAngles;
            scores = matchScores;

            Camera imgCamera = new Camera(img);
            for (int i = 0; i < num; i++)
            {
                ret = GetMarkCenter(imgCamera, matchRows[i], matchCols[i], matchAngles[i], out double centerX, out double centerY, out List<double[]> straightnessError, useMask ? _template.MaskRegion : null);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                straightnessErrorList.Add(straightnessError);
                rows[i] = centerY;
                cols[i] = centerX;
            }

            imgCamera.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Load(string dir)
        {
            string path = dir + "\\" + _name + "\\";
            string filename = path + "\\" + _name + ".ini";
            string lineCaliperFile = path + "\\" + _name + "_LineCaliper.ini";
            Errortype ret;
            if (!File.Exists(filename))
            {
                return Errortype.FILE_NOT_EXIST;
            }

            if (!File.Exists(lineCaliperFile))
            {
                return Errortype.FILE_NOT_EXIST;
            }

            string[] keys = null;
            string[] values = null;
            IniHelper.GetAllKeyValues("Info", out keys, out values, filename);
            string name = values[0];
            if (name != _name)
            {
                return Errortype.NAME_NOT_EQUAL;
            }

            _type = (TemplateType)Enum.Parse(typeof(TemplateType), values[1]);
            _template = new Template(name, _type);

            IniHelper.GetAllKeyValues("LineNum", out keys, out values, lineCaliperFile);
            int lineNum = Convert.ToInt32(values[0]);

            _lines = new List<LineSeg>();
            for (int i = 0; i < lineNum; i++)
            {
                LineSeg t = new LineSeg();
                ret = t.Load(lineCaliperFile, "Line_" + i.ToString());
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                _lines.Add(t);
            }

            IniHelper.GetAllKeyValues("CaliperParamsNum", out keys, out values, lineCaliperFile);
            int caliperParamsNum = Convert.ToInt32(values[0]);

            _caliperParams = new List<CaliperParams>();
            for (int i = 0; i < caliperParamsNum; i++)
            {
                CaliperParams t = new CaliperParams();
                ret = t.Load(lineCaliperFile, "CaliperParams_" + i.ToString());
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                _caliperParams.Add(t);
            }

            if (_lines.Count != _caliperParams.Count)
            {
                return Errortype.LINE_CALIPER_NUM_NOT_EQUAL;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string dir)
        {
            if (_template != null)
            {
                Errortype ret = _template.Save(dir);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }

            string path = dir + "\\" + _name + "\\";

            // save line and lineCaliperParam
            string lineCaliperFile = path + "\\" + _name + "_LineCaliper.ini";
            List<string> keys = new List<string>() { "LineNum" };
            List<string> value = new List<string>() { _lines.Count.ToString() };
            IniHelper.AddSectionWithKeyValues("LineNum", keys, value, lineCaliperFile);

            for (int i = 0; i < _lines.Count; i++)
            {
                _lines[i].Save(lineCaliperFile, "Line_" + i.ToString());
            }

            keys = new List<string>() { "CaliperParamsNum" };
            value = new List<string>() { _lines.Count.ToString() };
            IniHelper.AddSectionWithKeyValues("CaliperParamsNum", keys, value, lineCaliperFile);

            for (int i = 0; i < _caliperParams.Count; i++)
            {
                _caliperParams[i].Save(lineCaliperFile, "CaliperParams_" + i.ToString());
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 四条线确定mark中心的集合管理
    /// </summary>
    public static class MarkFourLinesLocationManager
    {
        private static List<MarkFourLinesLocation> _markLocation;

        /// <summary>
        /// 初始化manager
        /// </summary>
        /// <param name="markInfo">mark集合</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Init(Dictionary<KeyValuePair<string, string>, TemplateType> markInfo)
        {
            if (_markLocation == null)
            {
                _markLocation = new List<MarkFourLinesLocation>();
            }

            if (markInfo == null)
            {
                return Errortype.OK;
            }

            foreach (var info in markInfo)
            {
                TemplateType type = info.Value;
                string name = info.Key.Key;
                string dir = info.Key.Value;
                MarkFourLinesLocation markLocation = _markLocation.Find(e => e.Name == name);
                if (markLocation != null)
                {
                    return Errortype.MACROSTAGE_NAME_REPEAT;
                }

                MarkFourLinesLocation t = new MarkFourLinesLocation(name, type);
                Errortype ret = t.Load(dir);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                _markLocation.Add(t);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 释放manager
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            if (_markLocation != null)
            {
                _markLocation.Clear();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存所有mark信息
        /// </summary>
        /// <param name="dir">保存路径集合，一个mark信息存一个文件夹</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Save(string dir)
        {
            if (_markLocation == null)
            {
                return Errortype.SAVE_MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            for (int i = 0; i < _markLocation.Count; i++)
            {
                Errortype ret = _markLocation[i].Save(dir);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 创建ncc模板
        /// </summary>
        /// <param name="markName">mark名称</param>
        /// <param name="img">图像</param>
        /// <param name="nccTemplateParams">模板创建参数</param>
        /// <param name="nccMatchParams">模板匹配参数</param>
        /// <param name="templateRegions">模板区域</param>
        /// <param name="templateMasks">掩膜区域</param>
        /// <param name="lines">卡尺线段集合</param>
        /// <param name="caliperParams">卡尺提取参数集合</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CreateNccTemplate(string markName, Camera img, NccTemplateParams nccTemplateParams, NccMatchParams nccMatchParams, List<Rectangle1> templateRegions, List<Rectangle1> templateMasks,
            List<LineSeg> lines, List<CaliperParams> caliperParams)
        {
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkFourLinesLocation markLocation = _markLocation.Find(e => e.Name == markName);

            HObject hImg = img.GenHObject();
            ComAlgo.GenRegionUnion(templateRegions, out HObject tRegion);
            ComAlgo.GenRegionUnion(templateMasks, out HObject mRegion);

            if (markLocation != null)
            {
                Errortype ret = markLocation.CreateNccTemplate(hImg, nccTemplateParams, nccMatchParams, tRegion, mRegion, lines, caliperParams);
                return ret;
            }
            else
            {
                markLocation = new MarkFourLinesLocation(markName, TemplateType.NCC);
                Errortype ret = markLocation.CreateNccTemplate(hImg, nccTemplateParams, nccMatchParams, tRegion, mRegion, lines, caliperParams);
                _markLocation.Add(markLocation);
                return ret;
            }
        }

        /// <summary>
        /// 获取mark中心
        /// </summary>
        /// <param name="markName">mark名称</param>
        /// <param name="img">图像</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scores">匹配结果得分集合</param>
        /// <param name="straightnessErrorList">垂直度误差</param>
        /// <param name="useMask">是否使用掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetNccMarkCenter(string markName, Camera img, Rectangle1 matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scores, out List<List<double[]>> straightnessErrorList, bool useMask = true)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;
            straightnessErrorList = new List<List<double[]>>();
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkFourLinesLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_NAME_NULL;
            }

            HObject hImg = img.GenHObject();
            HOperatorSet.GenEmptyObj(out HObject mRegion);
            if (matchRegion != null)
            {
                ComAlgo.GenRegionUnion(new List<Rectangle1>() { matchRegion }, out mRegion);
            }
            else
            {
                mRegion.Dispose();
                mRegion = null;
            }

            Errortype ret = markLocation.NccMatchAndGetMarkCenter(hImg, mRegion, out rows, out cols, out angles, out scores, out straightnessErrorList, useMask);
            hImg.Dispose();
            if (mRegion != null)
            {
                mRegion.Dispose();
            }

            return ret;
        }
    }

    /// <summary>
    /// 自动提取mark中心
    /// </summary>
    internal class MarkAutoCenterLocation
    {
        private HObject _hEdge;
        private Template _template;

        private CaliperParams _caliperParams;
        private List<List<LineSeg>> _lines;
        private HObject _caliperMask;

        /// <summary>
        /// Gets the user's _type
        /// </summary>
        public TemplateType Type { get => _template.Type; }

        /// <summary>
        /// Gets or sets the user's _name
        /// </summary>
        public string Name { get => _template.Name; set => _template.Name = value; }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="type">模板类型</param>
        public MarkAutoCenterLocation(string name, TemplateType type)
        {
            _template = new Template(name, type);
            HOperatorSet.GenEmptyObj(out _caliperMask);
        }

        /// <summary>
        /// 计算mark中心
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="matchRow">模板匹配行</param>
        /// <param name="matchCol">模板匹配列</param>
        /// <param name="matchAngle">模板匹配角度</param>
        /// <param name="centerX">Mark中心X坐标</param>
        /// <param name="centerY">Mark中心Y坐标</param>
        /// <param name="straightnessErrorList">垂直度误差</param>
        /// <param name="mask">掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype GetMarkCenter(HObject img, double matchRow, double matchCol, double matchAngle, out double centerX, out double centerY, out List<double[]> straightnessErrorList, HObject mask = null)
        {
            centerX = 0;
            centerY = 0;
            straightnessErrorList = new List<double[]>();

            Errortype ret;
            Point curPoint = new Point() { X = matchCol, Y = matchRow };
            ret = Common.TransAffineLine(_lines, _template.CenterPoint, curPoint, matchAngle, out List<List<LineSeg>> linesTrans);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = Common.TransAffineRegion(mask, _template.CenterPoint, curPoint, matchAngle, out HObject maskAffine);
            if (ret != Errortype.OK)
            {
                maskAffine.Dispose();
                return ret;
            }

            //linesTrans = _lines;
            ret = Common.GetMarkCenter(img, linesTrans, _caliperParams, out Point center, out straightnessErrorList, maskAffine);
            if (ret != Errortype.OK)
            {
                maskAffine.Dispose();
                return ret;
            }

            centerX = center.X;
            centerY = center.Y;
            maskAffine.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <typeparam name="TCreate">模板创建参数 泛型</typeparam>
        /// <typeparam name="TMatch">模板匹配参数 泛型</typeparam>
        /// <param name="img">图像</param>
        /// <param name="templateParams">模板创建参数</param>
        /// <param name="matchParams">模板匹配参数</param>
        /// <param name="templateRegions">模板区域</param>
        /// <param name="templateMasks">掩膜区域</param>
        /// <param name="caliperMasks">卡尺掩膜区域</param>
        /// <param name="lineSegs">线段</param>
        /// <param name="caliperParams">卡尺提取参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CreateTemplate<TCreate, TMatch>(HObject img, TCreate templateParams, TMatch matchParams, HObject templateRegions, HObject templateMasks, HObject caliperMasks, List<List<LineSeg>> lineSegs, CaliperParams caliperParams)
        {
            Errortype ret = Errortype.OK;

            switch (Type)
            {
                case TemplateType.NCC:
                    ret = _template.Create(img, templateParams, matchParams, templateRegions, templateMasks);
                    break;
                case TemplateType.SHAPE:
                    ret = _template.Create(img, templateParams, matchParams, templateRegions, templateMasks);
                    break;
                case TemplateType.SHAPEXLD:
                    if (templateParams is NccTemplateParams || matchParams is NccMatchParams)
                    {
                        return Errortype.TEMPLATE_CREATE_PARAMTYPE_ERROR;
                    }

                    ShapeTemplateParams shapeTemplateParams = templateParams as ShapeTemplateParams;
                    ShapeMatchParams shapeMatchParams = matchParams as ShapeMatchParams;
                    List<LineSeg> lines = lineSegs.SelectMany(inner => inner).ToList();
                    ret = _template.Create(img, lines, shapeTemplateParams, shapeMatchParams);
                    break;
            }

            if (ret != Errortype.OK)
            {
                return ret;
            }

            _lines = new List<List<LineSeg>>();
            for (int i = 0; i < lineSegs.Count; i++)
            {
                List<LineSeg> t = new List<LineSeg>();
                for (int j = 0; j < lineSegs[i].Count; j++)
                {
                    t.Add(lineSegs[i][j].Clone());
                }

                _lines.Add(t);
            }

            _caliperParams = caliperParams.Clone();
            if (caliperMasks != null && caliperMasks.IsInitialized())
            {
                _caliperMask = caliperMasks.Clone();
            }
            else
            {
                _caliperMask.Dispose();
                HOperatorSet.GenEmptyObj(out _caliperMask);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 模板匹配并提取中心
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scores">匹配结果得分集合</param>
        /// <param name="straightnessErrorList">垂直度误差</param>
        /// <param name="useMask">是否使用掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype MatchAndGetMarkCenter(HObject img, HObject matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scores, out List<List<double[]>> straightnessErrorList, bool useMask = true)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;
            straightnessErrorList = new List<List<double[]>>();

            Errortype ret = Errortype.OK;
            ret = _template.Match(img, matchRegion, out double[] matchRows, out double[] matchCols, out double[] matchAngles, out double[] scales, out double[] matchScores);

            if (ret != Errortype.OK)
            {
                return ret;
            }

            int num = matchRows.Length;
            rows = matchRows;
            cols = matchCols;
            angles = matchAngles;
            scores = matchScores;

            //对所有找到的mark做中心提取
            for (int i = 0; i < num; i++)
            {
                ret = GetMarkCenter(img, matchRows[i], matchCols[i], matchAngles[i], out double centerX, out double centerY, out List<double[]> straightnessError, useMask ? _caliperMask : null);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                straightnessErrorList.Add(straightnessError);
                rows[i] = centerY;
                cols[i] = centerX;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 使用新分数进行模板匹配，只使用模板匹配，不进行卡尺
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="newScoreRate">新分数</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scores">匹配结果得分集合</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype MatchChangeScore(HObject img, HObject matchRegion, double newScoreRate, out double[] rows, out double[] cols, out double[] angles, out double[] scores)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;

            Errortype ret = Errortype.OK;
            double oriScore = 0;
            if (_template.Type == TemplateType.NCC)
            {
                oriScore = _template.NccMatchParams.MinScore;
                _template.NccMatchParams.MinScore = oriScore * newScoreRate;
            }
            else
            {
                oriScore = _template.ShapeMatchParams.MinScore;
                _template.ShapeMatchParams.MinScore = oriScore * newScoreRate;
            }

            ret = _template.Match(img, matchRegion, out double[] matchRows, out double[] matchCols, out double[] matchAngles, out double[] scales, out double[] matchScores);
            if (_template.Type == TemplateType.NCC)
            {
                _template.NccMatchParams.MinScore = oriScore;
            }
            else
            {
                _template.ShapeMatchParams.MinScore = oriScore;
            }

            if (ret != Errortype.OK)
            {
                return ret;
            }

            int num = matchRows.Length;
            rows = matchRows;
            cols = matchCols;
            angles = matchAngles;
            scores = matchScores;

            return Errortype.OK;
        }

        /// <summary>
        /// 获取ncc模板的显示图像
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetTemplateShowImg(out HObject img)
        {
            img = new HObject();

            Errortype ret = _template.GetImg(out HObject imgSrc);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = Common.PaintImgLine(imgSrc, _lines, out img);

            return ret;
        }

        /// <summary>
        /// 修改ncc模板名称
        /// </summary>
        /// <param name="name">模板名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype ChangeTemplateName(string name)
        {
            return _template.ChangeName(name);
        }

        /// <summary>
        /// 获取ncc模板的train过程中的数据
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="templateRegion">模板区域</param>
        /// <param name="templateMask">模板掩膜区域</param>
        /// <param name="caliperMask">卡尺掩膜区域</param>
        /// <param name="lineSegs">线段</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetPatternTrainInfo(out HObject img, out HObject templateRegion, out HObject templateMask, out HObject caliperMask, out List<List<LineSeg>> lineSegs)
        {
            img = null;
            templateRegion = null;
            templateMask = null;
            caliperMask = null;
            lineSegs = null;
            if (_template is null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            img = _template.Img.Clone();
            templateRegion = _template.TemplateRegion.Clone();
            templateMask = _template.MaskRegion.Clone();
            caliperMask = _caliperMask.Clone();
            lineSegs = _lines;
            return Errortype.OK;
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Load(string dir)
        {
            string[] keys = null;
            string[] values = null;
            string path = dir + "\\" + _template.Name + "\\";
            string filename = path + "\\" + _template.Name + ".ini";
            string lineCaliperFile = path + "\\" + _template.Name + "_LineCaliper.ini";
            Errortype ret;

            if (!File.Exists(filename))
            {
                return Errortype.FILE_NOT_EXIST;
            }

            if (!File.Exists(lineCaliperFile))
            {
                return Errortype.FILE_NOT_EXIST;
            }

            IniHelper.GetAllKeyValues("Info", out keys, out values, filename);
            string name = values[0];
            if (name != _template.Name)
            {
                return Errortype.NAME_NOT_EQUAL;
            }

            var loadType = (TemplateType)Enum.Parse(typeof(TemplateType), values[1]);
            _template = new Template(name, loadType);
            ret = _template.Load(dir);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            IniHelper.GetAllKeyValues("EdgeNum", out keys, out values, lineCaliperFile);
            int edgeNum = Convert.ToInt32(values[0]);

            _lines = new List<List<LineSeg>>();
            for (int i = 0; i < edgeNum; i++)
            {
                IniHelper.GetAllKeyValues("LineNum_" + i.ToString(), out keys, out values, lineCaliperFile);
                int lineNum = Convert.ToInt32(values[0]);

                List<LineSeg> oneEdge = new List<LineSeg>();
                for (int j = 0; j < lineNum; j++)
                {
                    LineSeg oneline = new LineSeg();
                    ret = oneline.Load(lineCaliperFile, "Line_" + i.ToString() + "_" + j.ToString());

                    // todo：极性load
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    oneEdge.Add(oneline);
                }

                _lines.Add(oneEdge);
            }

            _caliperParams = new CaliperParams();
            CaliperParams t = new CaliperParams();
            ret = t.Load(lineCaliperFile, "CaliperParams");
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _caliperParams = t;

            string caliperMaskFile = path + "\\" + _template.Name + "_CaliperMask.hobj";

            if (!(_caliperMask is null) && _caliperMask.IsInitialized())
            {
                _caliperMask.Dispose();
            }

            HOperatorSet.ReadRegion(out _caliperMask, caliperMaskFile);

            return Errortype.OK;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string path = dir + "\\" + _template.Name + "\\";
            string filename = path + "\\" + _template.Name + ".ini";

            // save  template
            Errortype ret = _template.Save(dir);

            // save line and lineCaliperParam
            string lineCaliperFile = path + "\\" + _template.Name + "_LineCaliper.ini";
            if (File.Exists(lineCaliperFile))
            {
                File.Delete(lineCaliperFile);
            }

            List<string> keys = new List<string>() { "EdgeNum" };
            List<string> value = new List<string>() { _lines.Count.ToString() };
            IniHelper.AddSectionWithKeyValues("EdgeNum", keys, value, lineCaliperFile);

            for (int i = 0; i < _lines.Count; i++)
            {
                List<LineSeg> lineSegT = _lines[i];
                keys = new List<string>() { "LineNum_" + i.ToString() };
                value = new List<string>() { _lines[i].Count.ToString() };
                IniHelper.AddSectionWithKeyValues("LineNum_" + i.ToString(), keys, value, lineCaliperFile);

                for (int j = 0; j < lineSegT.Count; j++)
                {
                    lineSegT[j].Save(lineCaliperFile, "Line_" + i.ToString() + "_" + j.ToString());

                    // todo：单条极性保存
                }
            }

            _caliperParams.Save(lineCaliperFile, "CaliperParams");

            string caliperMaskFile = path + "\\" + _template.Name + "_CaliperMask.hobj";
            if (File.Exists(caliperMaskFile))
            {
                File.Delete(caliperMaskFile);
            }

            HOperatorSet.WriteRegion(_caliperMask, caliperMaskFile);

            return Errortype.OK;
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <param name="name">拷贝后名字</param>
        /// <returns>拷贝后对象</returns>
        public MarkAutoCenterLocation Clone(string name)
        {
            MarkAutoCenterLocation markAutoCenterLocationClone = new MarkAutoCenterLocation(this.Name, this.Type);

            if (this._hEdge != null && this._hEdge.IsInitialized())
            {
                markAutoCenterLocationClone._hEdge = this._hEdge.Clone();
            }

            if (this._template != null)
            {
                markAutoCenterLocationClone._template = this._template.Clone(name);
            }

            if (this._caliperParams != null)
            {
                markAutoCenterLocationClone._caliperParams = this._caliperParams.Clone();
            }

            if (this._lines != null)
            {
                markAutoCenterLocationClone._lines = new List<List<LineSeg>>();
                for (int i = 0; i < _lines.Count; i++)
                {
                    if (_lines[i] == null)
                    {
                        continue;
                    }

                    List<LineSeg> lineSegs = new List<LineSeg>();
                    for (int j = 0; j < _lines[i].Count; j++)
                    {
                        lineSegs.Add(_lines[i][j].Clone());
                    }

                    markAutoCenterLocationClone._lines.Add(lineSegs);
                }
            }

            if (this._caliperMask != null && this._caliperMask.IsInitialized())
            {
                markAutoCenterLocationClone._caliperMask = this._caliperMask.Clone();
            }

            return markAutoCenterLocationClone;
        }
    }

    /// <summary>
    /// 自动提取mark中心集合管理
    /// </summary>
    public static class MarkAutoCenterLocationManager
    {
        private static List<MarkAutoCenterLocation> _markLocation;

        /// <summary>
        /// 初始化manager(仅初始化ncc模板)
        /// </summary>
        /// <param name="dir">mark名称集合</param>
        /// <param name="markNames">mark类型集合</param>
        /// <param name="initReturn">初始化结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Init(string dir, List<string> markNames, out Dictionary<string, Errortype> initReturn)
        {
            initReturn = new Dictionary<string, Errortype>();

            if (_markLocation == null)
            {
                _markLocation = new List<MarkAutoCenterLocation>();
            }

            if (!Directory.Exists(dir))
            {
                return Errortype.MARKLOCATIONMANAGER_DIR_NOT_EXIST;
            }

            foreach (var name in markNames)
            {
                string infoFile = dir + "\\" + name + "\\" + name + ".ini";
                if (!File.Exists(infoFile))
                {
                    initReturn.Add(name, Errortype.FILE_NOT_EXIST);
                    continue; //对于不存在的内容，不加载，但是不报错
                }

                string[] keys = null;
                string[] values = null;
                IniHelper.GetAllKeyValues("Info", out keys, out values, infoFile);
                TemplateType type = (TemplateType)Enum.Parse(typeof(TemplateType), values[1]);

                string nameRead = values[0];
                if (nameRead != name)
                {
                    initReturn.Add(name, Errortype.NAME_NOT_EQUAL);
                    continue;
                }

                MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == name);
                if (markLocation != null)
                {
                    //markLocation = new MarkAutoCenterLocation(name, type);
                    Errortype ret = markLocation.Load(dir);  //如果存在 直接load新的 替换原有的
                    initReturn.Add(name, ret);
                }
                else
                {
                    MarkAutoCenterLocation t = new MarkAutoCenterLocation(name, type);
                    Errortype ret = t.Load(dir);
                    initReturn.Add(name, ret);
                    if (ret != Errortype.OK)
                    {
                        continue;
                    }

                    _markLocation.Add(t);
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 查询模版是否加载
        /// </summary>
        /// <param name="markNames">mark名称</param>
        /// <param name="isLoaded">结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetIsLoaded(List<string> markNames, out List<bool> isLoaded)
        {
            isLoaded = new List<bool>();

            if (_markLocation == null)
            {
                _markLocation = new List<MarkAutoCenterLocation>();
                return Errortype.OK;
            }

            foreach (var name in markNames)
            {
                MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == name);
                if (markLocation != null)
                {
                    //如果存在
                    isLoaded.Add(true);
                }
                else
                {
                    //不存在，没加载
                    isLoaded.Add(false);
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取目录下所有mark信息
        /// </summary>
        /// <param name="dir">路径</param>
        /// <param name="markInfo">Mark信息</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetNameAndImg(string dir, out Dictionary<string, Camera> markInfo)
        {
            markInfo = new Dictionary<string, Camera>();

            if (!Directory.Exists(dir))
            {
                return Errortype.MARKLOCATIONMANAGER_DIR_NOT_EXIST;
            }

            DirectoryInfo root = new DirectoryInfo(dir);
            DirectoryInfo[] dirs = root.GetDirectories();

            for (int i = 0; i < dirs.Length; i++)
            {
                string markName = dirs[i].Name;
                string imgFile = dirs[i].FullName + "\\" + markName + "_ShowImg.bmp";

                if (!File.Exists(imgFile))
                {
                    //return Errortype.MARKLOCATIONMANAGER_SHOWIMGFILE_NOT_EXIST;
                    //如果不存在认为该文件夹不是mark文件夹，直接跳过
                    continue;
                }

                Camera img = new Camera(imgFile);
                markInfo.Add(markName, img);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 释放manager
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            if (_markLocation != null)
            {
                _markLocation.Clear();
                _markLocation = null;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存所有mark信息
        /// </summary>
        /// <param name="dir">保存路径集合，一个mark信息存一个文件夹</param>
        /// <param name="saveReturn">保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Save(string dir, out Dictionary<string, Errortype> saveReturn)
        {
            saveReturn = new Dictionary<string, Errortype>();

            if (_markLocation == null)
            {
                return Errortype.SAVE_MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            for (int i = 0; i < _markLocation.Count; i++)
            {
                Errortype ret = _markLocation[i].Save(dir);
                saveReturn.Add(_markLocation[i].Name, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存单个mark信息
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <param name="markName">mark名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Save(string dir, string markName)
        {
            if (_markLocation == null)
            {
                return Errortype.SAVE_MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation != null)
            {
                Errortype ret = markLocation.Save(dir);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }
            else
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 移除模板(功能待验证)
        /// </summary>
        /// <param name="markName">模块名称</param>
        /// <param name="dir">路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Delete(string markName, string dir)
        {
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation != null)
            {
                string markPath = dir + "\\" + markName;
                if (!Directory.Exists(markPath))
                {
                    return Errortype.OK;
                }
                else
                {
                    Directory.Delete(markPath, true);
                }

                _markLocation.Remove(markLocation);
                return Errortype.OK;
            }
            else
            {
                return Errortype.OK; //对于不存在的Mark，在删除的过程中不报错
            }
        }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <param name="markName">原mark名</param>
        /// <param name="markNameClone">拷贝后Mark名</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Clone(string markName, string markNameClone)
        {
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_NAME_NULL;
            }

            MarkAutoCenterLocation markLocationClone = markLocation.Clone(markNameClone);
            _markLocation.Add(markLocationClone);
            return Errortype.OK;
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <typeparam name="TCreate">模板创建参数，泛型</typeparam>
        /// <typeparam name="TMatch">模板匹配参数，泛型</typeparam>
        /// <param name="markName">模板名称</param>
        /// <param name="img">图像</param>
        /// <param name="templateParams">模板创建参数</param>
        /// <param name="matchParams">模板匹配参数</param>
        /// <param name="templateRegions">模板区域</param>
        /// <param name="templateMasks">掩膜区域</param>
        /// <param name="caliperMasks">卡尺掩膜区域</param>
        /// <param name="lineSegs">线段</param>
        /// <param name="caliperParams">卡尺提取参数</param>
        /// <param name="templateType">模板类型</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CreateTemplate<TCreate, TMatch>(string markName, Camera img, TCreate templateParams, TMatch matchParams, HObject templateRegions, HObject templateMasks, HObject caliperMasks, List<List<LineSeg>> lineSegs, CaliperParams caliperParams, TemplateType templateType)
        {
            if (_markLocation == null)
            {
                _markLocation = new List<MarkAutoCenterLocation>();
            }

            if (img == null)
            {
                return Errortype.INPUT_NULL;
            }

            HObject hImg = img.GenHObject();
            Errortype ret = Errortype.OK;

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation != null)
            {
                if (markLocation.Type == templateType)
                {
                    ret = markLocation.CreateTemplate(hImg, templateParams, matchParams, templateRegions, templateMasks, caliperMasks, lineSegs, caliperParams);
                    return ret;
                }
                else
                {
                    _markLocation.Remove(markLocation);
                }
            }

            markLocation = new MarkAutoCenterLocation(markName, templateType);
            ret = markLocation.CreateTemplate(hImg, templateParams, matchParams, templateRegions, templateMasks, caliperMasks, lineSegs, caliperParams);
            _markLocation.Add(markLocation);
            return ret;
        }

        /// <summary>
        /// 获取mark中心
        /// </summary>
        /// <param name="markName">mark名称</param>
        /// <param name="img">图像</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scores">匹配结果得分集合</param>
        /// <param name="straightnessErrorList">直线度误差集合</param>
        /// <param name="useMask">是否使用掩膜</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMarkCenter(string markName, Camera img, Rectangle1 matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scores, out List<List<double[]>> straightnessErrorList, bool useMask = true)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;
            straightnessErrorList = new List<List<double[]>>();
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_NAME_NULL;
            }

            if (img == null)
            {
                return Errortype.MARKLOCATIONMANAGER_IMAGE_NULL;
            }

            HObject hImg = img.GenHObject();
            HOperatorSet.GenEmptyObj(out HObject mRegion);
            if (matchRegion != null)
            {
                ComAlgo.GenRegionUnion(new List<Rectangle1>() { matchRegion }, out mRegion);
            }
            else
            {
                mRegion.Dispose();
                mRegion = null;
            }

            Errortype ret = markLocation.MatchAndGetMarkCenter(hImg, mRegion, out rows, out cols, out angles, out scores, out straightnessErrorList, useMask);
            hImg.Dispose();
            if (mRegion != null)
            {
                mRegion.Dispose();
            }

            if (ComAlgo.SaveFlg("MarkCenter", out int days))
            {
                string path = @"D:\Alg\MarkLocation";
                ComAlgo.LogImage(img, path, markName, days);
            }

            if (ret == Errortype.OK)
            {
                if (rows is null || cols is null)
                {
                    string path = @"D:\Alg\MarkNullError";
                    ComAlgo.LogImage(img, path, markName, days);
                    return Errortype.MARKLOCATIONCOMMON_RESULT_NULL;
                }
            }

            if (ret != Errortype.OK)
            {
                if (ComAlgo.SaveFlg("MarkMatchErrorSave", out days))
                {
                    string path = @"D:\Alg\MarkMatchError";
                    ComAlgo.LogImage(img, path, markName + ret.ToString(), days);
                    return ret;
                }
            }

            return ret;
        }

        /// <summary>
        /// 获取mark中心
        /// </summary>
        /// <param name="markName">mark名称</param>
        /// <param name="img">图像</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scores">匹配结果得分集合</param>
        /// <param name="useMask">是否使用掩膜</param>
        /// <param name="scoreRate">分数比例</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMarkCenterHalfScore(string markName, Camera img, Rectangle1 matchRegion, out double[] rows, out double[] cols,
            out double[] angles, out double[] scores, bool useMask = true, double scoreRate = 0.8)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_NAME_NULL;
            }

            if (img == null)
            {
                return Errortype.MARKLOCATIONMANAGER_IMAGE_NULL;
            }

            HObject hImg = img.GenHObject();
            HOperatorSet.GenEmptyObj(out HObject mRegion);
            if (matchRegion != null)
            {
                ComAlgo.GenRegionUnion(new List<Rectangle1>() { matchRegion }, out mRegion);
            }
            else
            {
                mRegion.Dispose();
                mRegion = null;
            }

            // 扩展图像进行匹配
            HOperatorSet.GetRegionPoints(hImg, out HTuple rowsImage, out HTuple colsImage);
            HOperatorSet.GetGrayval(hImg, rowsImage, colsImage, out HTuple grayVal);
            HOperatorSet.TupleMean(grayVal, out HTuple meanGray);
            HOperatorSet.GenImageConst(out HObject imageBg, "byte", img.Width * 2, img.Height * 2);
            HOperatorSet.PaintRegion(imageBg, imageBg, out HObject imageMeanGray, meanGray, "fill");
            HOperatorSet.SetGrayval(imageMeanGray, rowsImage + img.Height / 2, colsImage + img.Width / 2, grayVal);

            Errortype ret = markLocation.MatchChangeScore(imageMeanGray, mRegion, scoreRate, out double[] rowsT, out double[] colsT, out double[] anglesT, out double[] scoresT);
            hImg.Dispose();
            imageBg.Dispose();
            imageMeanGray.Dispose();
            if (mRegion != null)
            {
                mRegion.Dispose();
            }

            if (scoresT != null && scoresT.Length > 0)
            {
                rows = new double[] { rowsT[0] - img.Height / 2 };
                cols = new double[] { colsT[0] - img.Width / 2 };
                angles = new double[] { anglesT[0] };
                scores = new double[] { scoresT[0] };
            }

            if (ComAlgo.SaveFlg("MarkCenter", out int days))
            {
                string path = @"D:\Alg\MarkLocation";
                ComAlgo.LogImage(img, path, markName + "_HalfScore", days);
            }

            if (ret == Errortype.OK)
            {
                if (rows is null || cols is null)
                {
                    string path = @"D:\Alg\MarkNullError";
                    ComAlgo.LogImage(img, path, markName + "_HalfScore", days);
                    return Errortype.MARKLOCATIONCOMMON_RESULT_NULL;
                }
            }

            if (ret != Errortype.OK)
            {
                if (ComAlgo.SaveFlg("MarkMatchErrorSave", out days))
                {
                    string path = @"D:\Alg\MarkMatchError";
                    ComAlgo.LogImage(img, path, markName + "_HalfScore" + ret.ToString(), days);
                    return ret;
                }
            }

            return ret;
        }

        /// <summary>
        /// 获取模板的显示图像
        /// </summary>
        /// <param name="markName">Mark名称</param>
        /// <param name="img">图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetTemplateShowImg(string markName, out Camera img)
        {
            img = null;
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_NAME_NULL;
            }

            Errortype ret = Errortype.OK;
            HOperatorSet.GenEmptyObj(out HObject himg);
            ret = markLocation.GetTemplateShowImg(out himg);
            if (ret != Errortype.OK)
            {
                himg.Dispose();
                return ret;
            }

            img = new Camera(himg);
            himg.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 修改模板名字
        /// </summary>
        /// <param name="markName">模板名称</param>
        /// <param name="markNameNew">模板新名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype ChangeTemplateName(string markName, string markNameNew)
        {
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            if (markNameNew == null)
            {
                return Errortype.MARKLOCATIONMANAGER_INPUTNAME_NULL;
            }

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_NAME_NULL;
            }

            Errortype ret = Errortype.OK;
            ret = markLocation.ChangeTemplateName(markNameNew);
            return ret;
        }

        /// <summary>
        /// 获取模板信息
        /// </summary>
        /// <param name="markName">模板名称</param>
        /// <param name="img">图像</param>
        /// <param name="templateRegion">模板区域</param>
        /// <param name="templateMask">模板掩膜</param>
        /// <param name="caliperMask">卡尺区域</param>
        /// <param name="lineSegs">卡尺线段</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetPatternTrainInfo(string markName, out HObject img, out HObject templateRegion, out HObject templateMask, out HObject caliperMask, out List<List<LineSeg>> lineSegs)
        {
            img = null;
            templateRegion = null;
            templateMask = null;
            caliperMask = null;
            lineSegs = null;
            if (_markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_OBJECT_NULL;
            }

            MarkAutoCenterLocation markLocation = _markLocation.Find(e => e.Name == markName);
            if (markLocation == null)
            {
                return Errortype.MARKLOCATIONMANAGER_NAME_NULL;
            }

            Errortype ret = Errortype.OK;
            ret = markLocation.GetPatternTrainInfo(out img, out templateRegion, out templateMask, out caliperMask, out lineSegs);
            return ret;
        }
    }
}
