using System;
using System.Collections.Generic;
using System.IO;
using IniFileHelper;
using DataStruct;
using HalconDotNet;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using System.Linq;
using OpenCvSharp.Flann;

namespace UltrapreciseBonding.Calib
{
    #region 镜组真值坐标系标定 Ruler <-> Real

    /// <summary>
    /// 运动系标定
    /// </summary>
    public class CalibXY : Singleton<CalibXY>
    {
        private List<Point> _calibRulerPoints = new List<Point>();
        private List<Point> _calibRealPoints = new List<Point>();
        private string _calibTypeName = "XY_";
        private List<CalibCoord> _opticMotionVetCalibList = new List<CalibCoord>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Init(List<string> itemName)
        {
            if (itemName is null)
            {
                return Errortype.CALIBXY_INIT_ITEMNAME_NULL;
            }

            foreach (var name in itemName)
            {
                string fullName = _calibTypeName + name;
                CalibCoord opticUnionCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == fullName);
                if (opticUnionCalibBase != null)
                {
                    opticUnionCalibBase = new CalibCoord(fullName);
                }
                else
                {
                    _opticMotionVetCalibList.Add(new CalibCoord(fullName));
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 抓取真值标定板Mark中心(方或圆形mark)
        /// </summary>
        /// <param name="calibRealMark">标定板图像</param>
        /// <param name="centerPoint">标定板圆心坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRealMarkCenter(Camera calibRealMark, out Point centerPoint)
        {
            centerPoint = new Point(0, 0);
            if (calibRealMark is null)
            {
                return Errortype.OPT_CALIBRATE_MARK_ERROR;
            }

            if ((calibRealMark.Height < 1) || (calibRealMark.Width < 1))
            {
                return Errortype.OPT_CALIBRATE_MARK_ERROR;
            }

            Point imageCenter = new Point((calibRealMark.Width - 1) / 2, (calibRealMark.Height - 1) / 2);

            // 抓取方形中心
            var retRect = ComAlgo.GetRectMarkCenterPix(calibRealMark, out List<Point> rectCenter, out List<double> rectPhi, out List<double> rectWidth, out List<double> rectHeight);

            // 抓取圆形中心
            var retCircle = ComAlgo.GetCircleMarkCenterPix(calibRealMark, out List<Point> circleCenter, out List<double> circleRadius);
            if ((retRect != Errortype.OK) && (retCircle != Errortype.OK))
            {
                return retRect == Errortype.OK ? retCircle : retRect;
            }

            // 精准中心测量
            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = 0.9;
            calipParam.MeasureSigma = 0.8;
            calipParam.MeasureLength1 = 10;
            calipParam.MeasureLength2 = 5;
            calipParam.NumMeasures = 25;
            calipParam.MeasureSelect = "all";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "auto";

            // 如果出现多个符合条件的mark，则选用最靠近视野中心的mark
            if (rectCenter.Count >= 1)
            {
                HTuple centerDist = new HTuple();
                for (int index = 0; index < rectCenter.Count; index++)
                {
                    //CaliperRectangle.Rectangle2Extraction(calibRealMark, rectCenter[index], rectPhi[index], rectLen1[index], rectLen2[index], calipParam, out Point rectCenterPrecise, out double[] rectParamPrecise);       // 测试抓所有矩形
                    double dist = Math.Sqrt(Math.Pow(imageCenter.X - rectCenter[index].X, 2) + Math.Pow(imageCenter.Y - rectCenter[index].Y, 2));
                    HOperatorSet.TupleConcat(dist, centerDist, out centerDist);
                }

                HOperatorSet.TupleMax(centerDist, out HTuple maxDist);
                HOperatorSet.TupleFind(centerDist, maxDist, out HTuple maxIndices);
                CaliperRectangle.Rectangle2Extraction(calibRealMark, rectCenter[maxIndices], rectPhi[maxIndices], rectWidth[maxIndices], rectHeight[maxIndices], calipParam, out Point rectCenterPrecise, out double rectPhiPrecise, out double rectWidthPrecise, out double rectHeightPrecise);
                centerPoint = new Point(rectCenterPrecise.X, rectCenterPrecise.Y);
            }

            //else if (circleCenter.Count >= 1)   // 只保留一个图形时
            // 测试同时抓方和圆
            if (circleCenter.Count >= 1)
            {
                HTuple centerDist = new HTuple();
                for (int index = 0; index < circleCenter.Count; index++)
                {
                    //CaliperCircle.CircleExtraction(calibRealMark, circleCenter[index], circleRadius[index], calipParam, out Point circleCenterPrecise, out double radiusPrecise);     // 测试抓所有圆
                    double dist = Math.Sqrt(Math.Pow(imageCenter.X - circleCenter[index].X, 2) + Math.Pow(imageCenter.Y - circleCenter[index].Y, 2));
                    HOperatorSet.TupleConcat(dist, centerDist, out centerDist);
                }

                HOperatorSet.TupleMax(centerDist, out HTuple maxDist);
                HOperatorSet.TupleFind(centerDist, maxDist, out HTuple maxIndices);
                var ret = CaliperCircle.CircleExtraction(calibRealMark, circleCenter[maxIndices], circleRadius[maxIndices], calipParam, out Point circleCenterPrecise, out double radiusPrecise);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                centerPoint = new Point(circleCenterPrecise.X, circleCenterPrecise.Y);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 添加一对相应点
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="rulerPt">光栅坐标</param>
        /// <param name="realPt">真值坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype AddPoint(string itemName, Point rulerPt, Point realPt)
        {
            CalibCoord opticUnionCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticUnionCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            if (rulerPt is null || realPt is null)
            {
                // 处理mesh网格映射表, 当该点为异常点时使用序号的负值，并且只记录cellmap，不加入点对list中
                //opticUnionCalibBase.Mesh.CellMap.Add(-opticUnionCalibBase.Mesh.CellMap.Count);
            }
            else
            {
                _calibRulerPoints.Add(rulerPt);
                _calibRealPoints.Add(realPt);

                // 处理mesh网格映射表
                //opticUnionCalibBase.Mesh.CellMap.Add(opticUnionCalibBase.Ruler_points.Count - 1);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 初始化运动系标定参数
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype InitRulerToReal(string itemName)
        {
            CalibCoord opticUnionCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticUnionCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            var ret = opticUnionCalibBase.InitCalibDoXY();
            _calibRulerPoints = new List<Point>();
            _calibRealPoints = new List<Point>();

            return ret;
        }

        /// <summary>
        /// 标定光栅和真值坐标之间的关系
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="transType">变换算法（刚体变换、相似变换、仿射变换、基于KDTree仿射变换等）</param>
        /// <param name="neighbors">最近邻点数，仅用于KDTree</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibRulerToReal(string itemName, TransType transType = TransType.AffineKDTrans, int neighbors = 4)
        {
            CalibCoord opticUnionCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticUnionCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            var ret = opticUnionCalibBase.CalibDo(_calibRulerPoints, _calibRealPoints, transType, neighbors);
            _calibRulerPoints = new List<Point>();
            _calibRealPoints = new List<Point>();

            return ret;
        }

        /// <summary>
        /// 计算运动系标定的光栅坐标
        /// </summary>
        /// <param name="polygonPoints">多边形边界拐点</param>
        /// <param name="startPoints">起始点坐标</param>
        /// <param name="nextColPoints">起始点隔壁一列轴坐标</param>
        /// <param name="nextRowPoints">起始点隔壁一行轴坐标</param>
        /// <param name="calibRuler">所有标定位置光栅坐标</param>
        /// <param name="calibTrue">所有标定位置真值坐标</param>
        /// <param name="firstXOrY">排列顺序先先x后y或者先y后x，默认先x后y</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalcXYMotionRuler(List<Point> polygonPoints, Point startPoints, Point nextColPoints, Point nextRowPoints, out List<Point> calibRuler, out List<Point> calibTrue, bool firstXOrY = true)
        {
            calibRuler = new List<Point>();
            calibTrue = new List<Point>();

            if (polygonPoints.Count < 4)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            Point colNeighPoint = nextColPoints - startPoints;
            Point rowNeighPoint = nextRowPoints - startPoints;
            double colDist = Math.Round(Math.Sqrt(Math.Pow(nextColPoints.X - startPoints.X, 2) + Math.Pow(nextColPoints.Y - startPoints.Y, 2)));
            double rowDist = Math.Round(Math.Sqrt(Math.Pow(nextRowPoints.X - startPoints.X, 2) + Math.Pow(nextRowPoints.Y - startPoints.Y, 2)));

            var ret = ComAlgo.GenPolygonAreaAxisPoints(polygonPoints, startPoints, colNeighPoint, rowNeighPoint, out calibRuler, out List<Point> axisTrue, firstXOrY);

            foreach (var point in axisTrue)
            {
                calibTrue.Add(new Point(point.X * colDist, point.Y * rowDist));
            }

            return ret;
        }

        /// <summary>
        /// 标定验证
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalCalibVerify(string itemName)
        {
            CalibCoord opticUnionCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticUnionCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticUnionCalibBase.GetCalibError(out List<Point> errorSrc2Dst, out List<Point> errorDst2Src);
        }

        /// <summary>
        /// 验证运动系标定
        /// </summary>
        /// <param name="stageName">机构名称（与标定机构运动系时保持一致）</param>
        /// <param name="currentRulers">光栅坐标点集</param>
        /// <param name="expectedReals">预期的真值点集（若标定板移动，并非当前Mark标记的真值）</param>
        /// <param name="maxError">最大误差</param>
        /// <param name="meanError">平均误差</param>
        /// <param name="stdDev">标准差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype VerifyMotionXY(string stageName, List<Point> currentRulers, List<Point> expectedReals, out Point maxError, out Point meanError, out Point stdDev)
        {
            maxError = new Point();
            meanError = new Point();
            stdDev = new Point();
            if (stageName == null || currentRulers == null || expectedReals == null)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            if (currentRulers.Count != expectedReals.Count)
            {
                return Errortype.INPUT_SIZE_NOT_EQUAL;
            }

            CalibCoord opticUnionCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + stageName);
            if (opticUnionCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticUnionCalibBase.CalcMotionError(currentRulers, expectedReals, out maxError, out meanError, out stdDev);
        }

        /// <summary>
        /// 从轴光栅坐标映射至标定板坐标
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="rulerPoint">光栅点</param>
        /// <param name="targetRealPoint">目标真值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRealByRuler(string itemName, Point rulerPoint, out Point targetRealPoint)
        {
            CalibCoord opticUnionCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            targetRealPoint = null;
            if (opticUnionCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticUnionCalibBase.Src2Dst(rulerPoint, out targetRealPoint, out List<Point> error);
        }

        /// <summary>
        /// 从标定板坐标映射至轴光栅坐标
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="realPoint">真值点</param>
        /// <param name="targetRulerPoint">目标光栅值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRulerByReal(string itemName, Point realPoint, out Point targetRulerPoint)
        {
            CalibCoord opticMotionVectCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            targetRulerPoint = null;
            if (opticMotionVectCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticMotionVectCalibBase.Dst2Src(realPoint, out targetRulerPoint, out List<Point> error);
        }

        /// <summary>
        /// 查询标定状态
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="calibStatus">标定状态</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCalibStatus(string itemName, out bool calibStatus)
        {
            calibStatus = false;
            CalibCoord opticMotionVectCalibBase = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticMotionVectCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            calibStatus = opticMotionVectCalibBase.IsCalibed;
            return Errortype.OK;
        }

        /// <summary>
        /// 获取标定信息
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="calibObj">坐标系转换方式</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCalibObj(string itemName, out CalibCoord calibObj)
        {
            calibObj = _opticMotionVetCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (calibObj == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _opticMotionVetCalibList.Clear();
            _opticMotionVetCalibList = new List<CalibCoord>();
            return Errortype.OK;
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <param name="saveReturn">保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string fileDir, out Dictionary<string, Errortype> saveReturn)
        {
            saveReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticMotionVetCalibList.Count; index++)
            {
                Errortype ret = _opticMotionVetCalibList[index].Save(fileDir);
                saveReturn.Add(_opticMotionVetCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <param name="loadReturn">加载结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Load(string fileDir, out Dictionary<string, Errortype> loadReturn)
        {
            loadReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticMotionVetCalibList.Count; index++)
            {
                Errortype ret = _opticMotionVetCalibList[index].Load(fileDir);
                loadReturn.Add(_opticMotionVetCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }
    }
    #endregion
}