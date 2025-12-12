using DataStruct;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.FusionCollections;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using Point = DataStruct.Point;

namespace UltrapreciseBonding.UBAManager
{
    /// <summary>
    /// stage 坐标转换管理类
    /// </summary>
    public static class StageXyyManager
    {
        /// <summary>
        /// 初始化参数
        /// </summary>
        public class InitParam
        {
            /// <summary>
            /// Gets or Sets 两个Y轴的距离
            /// </summary>
            public double Distyy { get; set; }

            /// <summary>
            /// Gets or Sets X轴正极限
            /// </summary>
            public double LimitPositiveX { get; set; }

            /// <summary>
            /// Gets or Sets X轴负极限
            /// </summary>
            public double LimitNegativeX { get; set; }

            /// <summary>
            /// Gets or Sets 左侧Y轴正极限
            /// </summary>
            public double LimitPositiveYLeft { get; set; }

            /// <summary>
            /// Gets or Sets 左侧Y轴负极限
            /// </summary>
            public double LimitNegativeYLeft { get; set; }

            /// <summary>
            /// Gets or Sets 右侧Y轴正极限
            /// </summary>
            public double LimitPositiveYRight { get; set; }

            /// <summary>
            /// Gets or Sets 右侧Y轴负极限
            /// </summary>
            public double LimitNegativeYRight { get; set; }

            /// <summary>
            /// Gets or Sets X轴到晶圆中心的距离
            /// </summary>
            public double DistXRc { get; set; }

            /// <summary>
            /// Gets or Sets y轴到晶圆中心的距离
            /// </summary>
            public double DistYRc { get; set; }

            /// <summary>
            /// clone
            /// </summary>
            /// <returns>OK:成功,其他:失败</returns>
            public InitParam Clone()
            {
                InitParam param = new InitParam();
                param.Distyy = Distyy;
                param.LimitPositiveX = LimitPositiveX;
                param.LimitNegativeX = LimitNegativeX;
                param.LimitPositiveYLeft = LimitPositiveYLeft;
                param.LimitNegativeYLeft = LimitNegativeYLeft;
                param.LimitPositiveYRight = LimitPositiveYRight;
                param.LimitNegativeYRight = LimitNegativeYRight;
                param.DistXRc = DistXRc;
                param.DistYRc = DistYRc;
                return param;
            }
        }

        private static InitParam _InitParam;
        private static double _x;
        private static double _yLeft;
        private static double _yRight;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="initParam">初始化参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Init(InitParam initParam)
        {
            _InitParam = initParam.Clone();
            return Errortype.OK;
        }

        /// <summary>
        /// 判断是否超出极限
        /// </summary>
        /// <param name="xRuler">x光栅</param>
        /// <param name="yLeftRuler">y左侧光栅</param>
        /// <param name="yRightRuler">y右侧光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CheckLimit(double xRuler, double yLeftRuler, double yRightRuler)
        {
            if (xRuler > _InitParam.LimitPositiveX || xRuler < _InitParam.LimitNegativeX ||
                yLeftRuler > _InitParam.LimitPositiveYLeft || yLeftRuler < _InitParam.LimitNegativeYLeft ||
                yRightRuler > _InitParam.LimitPositiveYRight || yRightRuler < _InitParam.LimitNegativeYRight)
            {
                return Errortype.UBA_TRANSXYY_OUTOFLIMIT;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 绕右侧支点旋转
        /// </summary>
        /// <param name="xRuler">当前x轴光栅</param>
        /// <param name="yLeftRuler">当前y轴左侧光栅</param>
        /// <param name="yRightRuler">当前y轴右侧光栅</param>
        /// <param name="rotate">旋转角度</param>
        /// <param name="rotateCenter">目标定的旋转中心</param>
        /// <param name="xRulerOut">输出x轴光栅</param>
        /// <param name="yLeftRulerOut">输出y轴左侧光栅</param>
        /// <param name="yRightRulerOut">输出y轴右侧光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype RotateRightCenter(double xRuler, double yLeftRuler, double yRightRuler, double rotate, Point rotateCenter,
            out double xRulerOut, out double yLeftRulerOut, out double yRightRulerOut)
        {
            xRulerOut = xRuler;
            yLeftRulerOut = yLeftRuler;
            yRightRulerOut = yRightRuler;

            double rotateRad = Math.PI * rotate / 180;
            double transFromRotate = Math.Tan(rotateRad) * _InitParam.Distyy * -1; //角度逆时针为正
            yLeftRulerOut = yLeftRuler + transFromRotate;

            Point rotateCenterActual = new Point(_InitParam.DistYRc, -_InitParam.DistXRc);
            ComAlgo.CalcRotatePoint(rotateCenter, rotate, rotateCenterActual, out Point rotateCenterAfter);

            Point offset = rotateCenter - rotateCenterAfter;

            xRulerOut += offset.X;
            yLeftRulerOut += offset.Y;
            yRightRulerOut += offset.Y;

            Errortype ret = CheckLimit(xRulerOut, yLeftRulerOut, yRightRulerOut);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 绕左侧支点旋转
        /// </summary>
        /// <param name="xRuler">当前x轴光栅</param>
        /// <param name="yLeftRuler">当前y轴左侧光栅</param>
        /// <param name="yRightRuler">当前y轴右侧光栅</param>
        /// <param name="rotate">旋转角度</param>
        /// <param name="rotateCenter">目标定的旋转中心</param>
        /// <param name="xRulerOut">输出x轴光栅</param>
        /// <param name="yLeftRulerOut">输出y轴左侧光栅</param>
        /// <param name="yRightRulerOut">输出y轴右侧光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype RotateLeftCenter(double xRuler, double yLeftRuler, double yRightRuler, double rotate, Point rotateCenter,
            out double xRulerOut, out double yLeftRulerOut, out double yRightRulerOut)
        {
            xRulerOut = xRuler;
            yLeftRulerOut = yLeftRuler;
            yRightRulerOut = yRightRuler;

            double rotateRad = Math.PI * rotate / 180;
            double transFromRotate = Math.Tan(rotateRad) * _InitParam.Distyy * 1; //角度逆时针为正
            yRightRulerOut = yLeftRuler + transFromRotate;

            Point rotateCenterActual = new Point(-_InitParam.DistYRc, -_InitParam.DistXRc);
            ComAlgo.CalcRotatePoint(rotateCenter, rotate, rotateCenterActual, out Point rotateCenterAfter);

            Point offset = rotateCenter - rotateCenterAfter;

            xRulerOut += offset.X;
            yLeftRulerOut += offset.Y;
            yRightRulerOut += offset.Y;

            Errortype ret = CheckLimit(xRulerOut, yLeftRulerOut, yRightRulerOut);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// (绕左右侧支点旋转，优先右侧做支点，再左侧)->修改为不先以右侧为支点了
        /// </summary>
        /// <param name="xRuler">当前x轴光栅</param>
        /// <param name="yLeftRuler">当前y轴左侧光栅</param>
        /// <param name="yRightRuler">当前y轴右侧光栅</param>
        /// <param name="rotate">旋转角度</param>
        /// <param name="rotateCenter">目标定的旋转中心</param>
        /// <param name="xRulerOut">输出x轴光栅</param>
        /// <param name="yLeftRulerOut">输出y轴左侧光栅</param>
        /// <param name="yRightRulerOut">输出y轴右侧光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype RotateRightAndLeft(double xRuler, double yLeftRuler, double yRightRuler, double rotate, Point rotateCenter,
            out double xRulerOut, out double yLeftRulerOut, out double yRightRulerOut)
        {
            /* 绕左右两侧支点旋转，优先右侧做支点，再左侧
            xRulerOut = xRuler;
            yLeftRulerOut = yLeftRuler;
            yRightRulerOut = yRightRuler;

            // 先让左侧走到极限，看角度能满足多少
            double leftRulerYCanGo = rotate > 0 ? _InitParam.LimitNegativeYLeft - yLeftRuler : _InitParam.LimitPositiveYLeft - yLeftRuler;
            double rotateByRight = -Math.Atan(leftRulerYCanGo / _InitParam.Distyy);
            yLeftRulerOut += leftRulerYCanGo;

            Point rotateCenterActual = new Point(_InitParam.DistYRc, -_InitParam.DistXRc);
            ComAlgo.CalcRotatePoint(rotateCenter, rotateByRight, rotateCenterActual, out Point rotateCenterAfter);

            Point offset = rotateCenter - rotateCenterAfter;

            xRulerOut += offset.X;
            yLeftRulerOut += offset.Y;
            yRightRulerOut += offset.Y;

            double rotateResidue = rotate - rotateByRight;

            Errortype ret = RotateLeftCenter(xRulerOut, yLeftRulerOut, yRightRulerOut, rotateResidue, rotateCenter,
                out double xRulerOutT, out double yLeftRulerOutT, out double yRightRulerOutT);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            xRulerOut = xRulerOutT;
            yLeftRulerOut = yLeftRulerOutT;
            yRightRulerOut = yRightRulerOutT;

            ret = CheckLimit(xRulerOut, yLeftRulerOut, yRightRulerOut);
            if (ret != Errortype.OK)
            {
                return ret;
            }
            */

            xRulerOut = xRuler;
            yLeftRulerOut = yLeftRuler;
            yRightRulerOut = yRightRuler;

            double rotateRad = Math.PI * rotate / 180;
            double transFromRotate = Math.Tan(rotateRad) * _InitParam.Distyy;

            double yLeftRulerDist = rotate > 0 ? yLeftRuler + (transFromRotate / 2) * -1 : yLeftRuler + (transFromRotate / 2) * 1;
            double yRightRulerDist = rotate > 0 ? yRightRuler + (transFromRotate / 2) * 1 : yRightRuler + (transFromRotate / 2) * -1;

            double yDist = yLeftRulerDist - yRightRulerDist;
            double actualRotate = Math.Atan2(yDist, _InitParam.Distyy);

            Point rotateCenterActual = new Point(_InitParam.DistYRc, -_InitParam.DistXRc);

            ComAlgo.CalcRotatePoint(rotateCenter, actualRotate, rotateCenterActual, out Point rotateCenterAfter);

            Point offset = rotateCenter - rotateCenterAfter;
            xRulerOut += offset.X;
            yLeftRulerOut += offset.Y;   
            yRightRulerOut += offset.Y; 

            Errortype ret = CheckLimit(xRulerOut, yLeftRulerOut, yRightRulerOut);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 坐标转换
        /// </summary>
        /// <param name="xRuler">当前x光栅值</param>
        /// <param name="yLeftRuler">当前左侧y光栅值</param>
        /// <param name="yRightRuler">当前右侧y光栅值</param>
        /// <param name="transXy">需要的平移量xy</param>
        /// <param name="rotate">需要的旋转量t</param>
        /// <param name="xRulerOut">目标x轴光栅值</param>
        /// <param name="yLeftRulerOut">目标左侧y轴光栅值</param>
        /// <param name="yRightRulerOut">目标右侧y轴光栅值</param>
        /// <param name="rotateCenter">目标旋转中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Trans(double xRuler, double yLeftRuler, double yRightRuler, Point transXy, double rotate,
            out double xRulerOut, out double yLeftRulerOut, out double yRightRulerOut, Point rotateCenter = null)
        {
            xRulerOut = xRuler;
            yLeftRulerOut = yLeftRuler;
            yRightRulerOut = yRightRuler;

            double xAfterTrans = xRuler + transXy.X;
            double yLeftAfterTrans = yLeftRuler + transXy.Y;
            double yRightAfterTrans = yRightRuler + transXy.Y;

            Point rotateCenterT = rotateCenter is null ? new Point(0, 0) : rotateCenter;

            Errortype ret = CheckLimit(xAfterTrans, yLeftAfterTrans, yRightAfterTrans);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = RotateRightCenter(xRuler, yLeftRuler, yRightRuler, rotate, rotateCenter,
                out xRulerOut, out yLeftRulerOut, out yRightRulerOut);
            if (ret == Errortype.OK)
            {
                return ret;
            }

            ret = RotateLeftCenter(xRuler, yLeftRuler, yRightRuler, rotate, rotateCenter,
                out xRulerOut, out yLeftRulerOut, out yRightRulerOut);
            if (ret == Errortype.OK)
            {
                return ret;
            }

            ret = RotateRightAndLeft(xRuler, yLeftRuler, yRightRuler, rotate, rotateCenter,
                out xRulerOut, out yLeftRulerOut, out yRightRulerOut);
            if (ret == Errortype.OK)
            {
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// stage X X Y坐标转X Y T
        /// </summary>
        /// <param name="xRuler">当前x光栅值</param>
        /// <param name="yLeftRuler">当前左侧y光栅值</param>
        /// <param name="yRightRuler">当前右侧y光栅值</param>
        /// <param name="transXy">输出XY</param>
        /// <param name="theta">输出Theta(角度）</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype XyyTransXyt(double xRuler, double yLeftRuler, double yRightRuler, out Point transXy, out double theta)
        {
            transXy = new Point(0, 0);
            theta = 0;

            double yDist = yRightRuler - yLeftRuler;
            double thetaRad = Math.Atan2(yDist, _InitParam.Distyy);

            theta = thetaRad * 180.0 / Math.PI;
            transXy = new Point(xRuler, (yLeftRuler + yRightRuler) / 2);
            return Errortype.OK;
        }
    }

    /// <summary>
    /// 简化标定内容
    /// </summary>
    public static class FusionManagerSimplifyCalib
    {
        //简化标定内容，缩短标定时间，精度主要通过逼近的方式来控制
        //private static CalibCoaxiality _calibCoaxiality = new CalibCoaxiality(); //同轴标定  left right
        private static Dictionary<string, Point> _coaxiality;
        private static CalibPix _calibPix = new CalibPix(); //转角像素比 leftTop rightTop leftBottom rightBottom leftPec rightPec
        private static SVAParamInfo _svaParamInfo; //sva参数信息
        private static Point _offsetXY = new Point(0, 0); //xy固定偏移 指在sva坐标系下上晶圆相对于下晶圆的偏差
        private static double _offsetTheta = 0; //角度固定偏移 指在sva坐标系下上晶圆相对于下晶圆的角度偏差

        #region 初始化

        /// <summary>
        /// 设置参数信息 同时执行初始化操作
        /// </summary>
        /// <param name="svaParamInfo">参数信息</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetParamInfo(SVAParamInfo svaParamInfo)
        {
            HOperatorSet.SetSystem("temporary_mem_cache", "false");
            HOperatorSet.SetSystem("temporary_mem_reservoir", "false");
            HOperatorSet.SetSystem("tsp_temporary_mem_cache", "false");
            HOperatorSet.SetSystem("tsp_temporary_mem_reservoir", "false");

            if (svaParamInfo is null)
            {
                return Errortype.FUSIONMANAGER_SETPARAMINFO_INPUT_NULL;
            }

            Errortype ret = svaParamInfo.Check();
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _svaParamInfo = svaParamInfo;

            _coaxiality = new Dictionary<string, Point>();
            _coaxiality.Add(_svaParamInfo.LeftCoaxialityItemName, new Point(0, 0));
            _coaxiality.Add(_svaParamInfo.RightCoaxialityItemName, new Point(0, 0));

            _calibPix = new CalibPix();
            List<string> calibPixName = new List<string>()
            {
                _svaParamInfo.TopLeftItemName,
                _svaParamInfo.BottomLeftItemName,
                _svaParamInfo.TopRightItemName,
                _svaParamInfo.BottomRightItemName,
                _svaParamInfo.PecLeftItemName,
                _svaParamInfo.PecRightItemName,
            };
            _calibPix.Init(calibPixName);

            _svaParamInfo.ImgCenter = new Point(2448 / 2, 2048 / 2);

            ret = CalibPixAngle(_svaParamInfo.TopLeftItemName, _svaParamInfo.TopLeftScale / 1e6, _svaParamInfo.TopLeftAngle, _svaParamInfo.TopLeftDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.BottomLeftItemName, _svaParamInfo.BottomLeftScale / 1e6, _svaParamInfo.BottomLeftAngle, _svaParamInfo.BottomLeftDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.TopRightItemName, _svaParamInfo.TopRightScale / 1e6, _svaParamInfo.TopRightAngle, _svaParamInfo.TopRightDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.BottomRightItemName, _svaParamInfo.BottomRightScale / 1e6, _svaParamInfo.BottomRightAngle, _svaParamInfo.BottomRightDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.PecLeftItemName, _svaParamInfo.PecLeftScale / 1e6, _svaParamInfo.PecLeftAngle, _svaParamInfo.PecLeftDir, _svaParamInfo.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalibPixAngle(_svaParamInfo.PecRightItemName, _svaParamInfo.PecRightScale / 1e6, _svaParamInfo.PecRightAngle, _svaParamInfo.PecRightDir, _svaParamInfo.ImgCenter);

            return ret;
        }

        /// <summary>
        /// 释放所有内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            //_calibCoaxiality.Release();
            _coaxiality = null;
            _calibPix.Release();
            return Errortype.OK;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Save(string dir)
        {
            //TODO : 
            return Errortype.OK;
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Load(string dir)
        {
            //TODO : 
            return Errortype.OK;
        }

        /// <summary>
        /// 查询标定完成状态
        /// </summary>
        /// <param name="calibType">标定类型</param>
        /// <param name="itemName">标定项名称</param>
        /// <param name="calibComplete">输出标定是否完成</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetCalibStatus(CalibType calibType, string itemName, out bool calibComplete)
        {
            calibComplete = false;
            if (itemName is null)
            {
                return Errortype.FUSIONMANAGER_GETCALIBSTATUS_ITEMNAME_NULL;
            }

            Errortype ret = Errortype.OK;
            switch (calibType)
            {
                case CalibType.OPTIC_CONCENTERIC:
                    if (_coaxiality != null)
                    {
                        if (_coaxiality.ContainsKey(itemName))
                        {
                            if (_coaxiality[itemName] != null)
                            {
                                calibComplete = true;
                                ret = Errortype.OK;
                                break;
                            }
                            else
                            {
                                calibComplete = false;
                                ret = Errortype.OK;
                                break;
                            }
                        }
                    }

                    ret = Errortype.FUSIONMANAGER_COAXIALITY_ITEMNAME_ERROR;
                    break;
                case CalibType.OPTIC_EXTERNAL:
                    ret = _calibPix.GetCalibStatus(itemName, out calibComplete);
                    break;
                default:
                    return ret;
            }

            return ret;
        }

        #endregion

        #region 根据图像计算相机像素比

        /// <summary>
        /// 寻找最左边和最右边的线段（用于像素比计算）
        /// </summary>
        /// <param name="lineSegs">线段集</param>
        /// <param name="leftLine">最左边线段</param>
        /// <param name="rightLine">最右边线段</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype FindLineInLeftRight(List<List<LineSeg>> lineSegs, out LineSeg leftLine, out LineSeg rightLine)
        {
            leftLine = new LineSeg();
            rightLine = new LineSeg();
            HObject singleLineContour;
            HOperatorSet.GenEmptyObj(out singleLineContour);
            List<double> minDistanceLc = new List<double>();
            List<double> maxDistanceLc = new List<double>();
            List<LineSeg> allLines = new List<LineSeg>();
            if (lineSegs == null)
            {
                return Errortype.FUSIONMANAGER_FINDLINEINLEFTRIGHT_LINESEG_NULL;
            }

            List<LineSeg> lineSegsT = new List<LineSeg>();
            for (int i = 0; i < lineSegs.Count; i++)
            {
                lineSegsT.AddRange(lineSegs[i]);
            }

            if (lineSegsT == null || lineSegsT.Count < 2)
            {
                return Errortype.FUSIONMANAGER_FINDLINEINLEFTRIGHT_LINESEG_EMPTY;
            }

            foreach (LineSeg singleLine in lineSegsT)
            {
                if (singleLine == null)
                {
                    return Errortype.COMMONALGO_LINESEG_NUM_ERROR;
                }

                if (singleLine.CalculateLineLength() == 0.0)
                {
                    return Errortype.COMMONALGO_LINESEG_NUM_ERROR;
                }

                double deg = singleLine.CalculateLineDeg();

                // 选取尽量垂直或水平范围的线段
                if (Math.Abs(Math.Abs(deg) - 90) < 20)
                {
                    allLines.Add(new LineSeg(singleLine.Start_X, singleLine.Start_Y, singleLine.End_X, singleLine.End_Y));
                    singleLine.Shorten(0.15);
                    HOperatorSet.GenRegionLine(out HObject singleRegion, singleLine.Start_Y, singleLine.Start_X, singleLine.End_Y, singleLine.End_X);
                    HOperatorSet.GenContourRegionXld(singleRegion, out singleLineContour, "border");
                    HOperatorSet.DistanceLc(singleLineContour, 0, 0, 1, 0, out HTuple distanceMin, out HTuple distanceMax);

                    minDistanceLc.Add(distanceMin.D);
                    maxDistanceLc.Add(distanceMax.D);
                    singleRegion.Dispose();
                    singleLineContour.Dispose();
                }
            }

            if (maxDistanceLc.Count < 2)
            {
                return Errortype.FUSIONMANAGER_CALIBPIX_BAD_MRAK;
            }

            HOperatorSet.TupleMax(maxDistanceLc.ToArray(), out HTuple maxDist);
            HOperatorSet.TupleFind(maxDistanceLc.ToArray(), maxDist, out HTuple maxId);
            HOperatorSet.TupleMin(minDistanceLc.ToArray(), out HTuple minDist);
            HOperatorSet.TupleFind(minDistanceLc.ToArray(), minDist, out HTuple minId);
            leftLine = allLines[minId.I];
            rightLine = allLines[maxId.I];
            return Errortype.OK;
        }

        /// <summary>
        /// 计算毫米像素比
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="size">左右侧线段间距</param>
        /// <param name="mmppx">毫米像素比</param>
        /// <param name="lineSegs">边缘线段集合</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcMmppx(Camera img, double size, out double mmppx, out List<LineSeg> lineSegs)
        {
            mmppx = 0;
            lineSegs = new List<LineSeg>();
            if (img == null)
            {
                return Errortype.FUSIONMANAGER_CALCMMPPX_IMG_NULL;
            }

            Errortype ret = Errortype.OK;
            HObject hImg = img.GenHObject();
            CaliperParams caliperParams = new CaliperParams();
            ret = MarkLocation.Common.GetMarkEdgeLine(hImg, caliperParams, out HObject imgOut, out List<List<LineSeg>> lineSegsT);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = FindLineInLeftRight(lineSegsT, out LineSeg leftLine, out LineSeg rightLine);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //找到后需要增加卡尺的操作，不然不够准确,参数使用自动参数
            ret = Caliper.CaliperLine.LineExtraction(img, leftLine, caliperParams, out LineSeg leftCaliper, out double[] error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = Caliper.CaliperLine.LineExtraction(img, rightLine, caliperParams, out LineSeg rightCaliper, out error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            lineSegs.Add(leftCaliper);
            lineSegs.Add(rightCaliper);
            double dist = ComAlgo.Dist(leftCaliper, rightCaliper);
            mmppx = size / dist;

            hImg.Dispose();
            imgOut.Dispose();

            return Errortype.OK;
        }

        #endregion

        #region 内外参

        //相机内外参标定
        //这里标定所有相机的内外参，主要用于逼近时使用，建议itemName为LeftTop，LeftBottom，RightTop，RightBottom，PecRight，PecLeft
        //calibType均为OPTIC_EXTERNAL
        //使用时可以输入单个像素点和当前光栅值，输出目标点的光栅值

        /// <summary>
        /// 相机内参标定 xy像素比值设定，转角为0
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="scale">像素比</param>
        /// <param name="dir">图像与轴方向</param>
        /// <param name="imgCenter">相机图像中心像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibPix(string itemName, double scale, Dir dir, Point imgCenter)
        {
            int dirX = 1;
            int dirY = 1;
            switch (dir)
            {
                case Dir.Dir_XPositive_YPositive:
                    break;
                case Dir.Dir_XPositive_YNegative:
                    dirY = -1;
                    break;
                case Dir.Dir_XNegative_YPositive:
                    dirX = -1;
                    break;
                case Dir.Dir_XNegative_YNegative:
                    dirX = -1;
                    dirY = -1;
                    break;
                default:
                    break;
            }

            _calibPix.SetPixCenter(itemName, imgCenter);
            return _calibPix.SetScale(itemName, scale * dirX / 1e6, scale * dirY / 1e6);
        }

        private static Errortype CalibPixAngle(string itemName, double scale, double angle, Dir dir, Point imgCenter)
        {
            int dirX = 1;
            int dirY = 1;
            switch (dir)
            {
                case Dir.Dir_XPositive_YPositive:
                    break;
                case Dir.Dir_XPositive_YNegative:
                    dirY = -1;
                    break;
                case Dir.Dir_XNegative_YPositive:
                    dirX = -1;
                    break;
                case Dir.Dir_XNegative_YNegative:
                    dirX = -1;
                    dirY = -1;
                    break;
                default:
                    break;
            }

            _calibPix.SetPixCenter(itemName, imgCenter);
            return _calibPix.SetScaleAngle(itemName, scale * dirX, scale * dirY, angle);
        }

        /// <summary>
        /// 标定转角
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pixelStart">像素起始坐标</param>
        /// <param name="pixelEnd">像素终点坐标</param>
        /// <param name="angleDeg">返回角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibPixAngleX(string itemName, Point pixelStart, Point pixelEnd, out double angleDeg)
        {
            return _calibPix.CalibPixAngle(itemName, pixelStart, pixelEnd, out angleDeg);
        }

        /// <summary>
        /// 像素点转轴坐标
        /// <para>【<seealso cref="CalibExtrinsicsScaleX"/>】</para>【<seealso cref="CalibExtrinsicsScaleY"/>】
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="currentRuler">当前的光栅值</param>
        /// <param name="targetMarkCenterPix">待转换像素点(mark中心像素)</param>
        /// <param name="targetMarkCenterRuler">目标点的光栅位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRulerByPix(string itemName, Point currentRuler, Point targetMarkCenterPix, out Point targetMarkCenterRuler)
        {
            var ret = _calibPix.GetRulerByPix(itemName, targetMarkCenterPix, currentRuler, out targetMarkCenterRuler);
            return ret;
        }

        /// <summary>
        /// 轴坐标转像素点
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="currentRuler">当前光栅值</param>
        /// <param name="targetMarkCenterRuler">目标mark中心光栅值</param>
        /// <param name="targetMarkCenterPixel">目标mark中心像素值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetPixByRuler(string itemName, Point currentRuler, Point targetMarkCenterRuler, out Point targetMarkCenterPixel)
        {
            var ret = _calibPix.GetPixByRuler(itemName, currentRuler, targetMarkCenterRuler, out targetMarkCenterPixel);
            return ret;
        }

        #endregion

        #region 同心度

        //同心度标定
        //这里标定左右侧相机组的同心度，建议itemName分别为left和right，calibType均为OPTIC_CONCENTERIC
        //标定时输入为两张图像
        //使用时可以输入一个相机的像素点，输出另一个相机的像素点

        /// <summary>
        /// 标定同心度误差
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="upOpticItemName">顶部相机标定项名称</param>
        /// <param name="downOpticItemName">底部相机标定项名称</param>
        /// <param name="upMarkCenterPixel">顶部mark中心像素坐标</param>
        /// <param name="downMarkCenterPixel">底部mark中心像素坐标</param>
        /// <param name="offset">输出顶部到底部的偏移值（nm）</param>
        /// <param name="upMarkCenterRuler">输出顶部mark的中心光栅坐标</param>
        /// <param name="downMarkCenterRuler">输出底部mark的中心光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibCoaxiality(string itemName, string upOpticItemName, string downOpticItemName, Point upMarkCenterPixel, Point downMarkCenterPixel,
            out Point offset, out Point upMarkCenterRuler, out Point downMarkCenterRuler)
        {
            offset = new Point();
            upMarkCenterRuler = new Point();
            downMarkCenterRuler = new Point();

            if (upMarkCenterPixel is null || downMarkCenterPixel is null)
            {
                return Errortype.FUSIONMANAGER_COAXIALITY_POINTIN_NULL;
            }

            if (_coaxiality == null || !_coaxiality.ContainsKey(itemName))
            {
                return Errortype.FUSIONMANAGER_COAXIALITY_ITEMNAME_ERROR;
            }

            //点转光栅，用于导出
            Point ruler = new Point(0, 0);
            Errortype ret = GetRulerByPix(upOpticItemName, ruler, upMarkCenterPixel, out upMarkCenterRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = GetRulerByPix(downOpticItemName, ruler, downMarkCenterPixel, out downMarkCenterRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //offset = upMarkCenterRuler - downMarkCenterRuler;
            offset = downMarkCenterRuler - upMarkCenterRuler;

            //_coaxiality[itemName] = upMarkCenterPixel - downMarkCenterPixel;
            _coaxiality[itemName] = offset.Clone();

            if (ComAlgo.SaveFlg("CalibCoaxiality", out int days))
            {
                string path = @"D:\Alg\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = path + "CalibCoaxiality.txt";
                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);

                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string text = time + " CalibCoaxiality " + upOpticItemName + sep + downOpticItemName + sep +
                              upMarkCenterPixel.ToString(sep) + sep + downMarkCenterPixel.ToString(sep) + sep +
                              upMarkCenterRuler.ToString(sep) + sep + downMarkCenterRuler.ToString(sep) + sep +
                              offset.ToString(sep);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 重置同心度误差
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype ReleaseCalibCoaxiality()
        {
            if (_coaxiality is null)
            {
                return Errortype.OK;
            }

            _coaxiality.Clear();
            _coaxiality.Add(_svaParamInfo.LeftCoaxialityItemName, new Point(0, 0));
            _coaxiality.Add(_svaParamInfo.RightCoaxialityItemName, new Point(0, 0));
            return Errortype.OK;
        }

        /// <summary>
        /// 根据底部像素点获取顶部像素点
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="bottomPoint">底部像素坐标</param>
        /// <param name="topPoint">顶部像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetTopPosition(string itemName, Point bottomPoint, out Point topPoint)
        {
            topPoint = new Point();

            //return _calibCoaxiality.GetTopPixel(itemName, bottomPoint, out topPoint);
            if (!_coaxiality.ContainsKey(itemName))
            {
                return Errortype.FUSIONMANAGER_COAXIALITY_ITEMNAME_ERROR;
            }

            topPoint = bottomPoint + _coaxiality[itemName];
            return Errortype.OK;
        }

        /// <summary>
        /// 根据顶部像素点获取底部像素点
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="topPoint">顶部像素坐标</param>
        /// <param name="bottomPoint">底部像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetBottomPosition(string itemName, Point topPoint, out Point bottomPoint)
        {
            bottomPoint = new Point();

            //return _calibCoaxiality.GetBottomPixel(itemName, topPoint, out bottomPoint);
            if (!_coaxiality.ContainsKey(itemName))
            {
                return Errortype.FUSIONMANAGER_COAXIALITY_ITEMNAME_ERROR;
            }

            bottomPoint = topPoint - _coaxiality[itemName];
            return Errortype.OK;
        }
        #endregion

        #region 标定微动平台的增益

        //XYT 需要软件自己求一下增益，算法求出前后两次之间的角度和距离
        private static Point _firstGainLeftRuler; //第一次左侧mark光栅
        private static Point _firstGainRightRuler; //第一次右侧mark光栅
        private static Point _leftGainRuler; //左侧相机轴光栅
        private static Point _rightGainRuler; //右侧相机轴光栅
        private static string _leftItemName; //左侧相机标定项名称
        private static string _rightItemName; //右侧相机标定项名称

        /// <summary>
        /// 计算两条线段之间的角度
        /// </summary>
        /// <param name="line1_Start">线段1起点</param>
        /// <param name="line1_End">线段1终点</param>
        /// <param name="line2_Start">线段2起点</param>
        /// <param name="line2_End">线段2终点</param>
        /// <param name="angle">输出角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype CalcAngleLL(Point line1_Start, Point line1_End, Point line2_Start, Point line2_End, out double angle)
        {
            angle = 0;
            LineSeg line1 = new LineSeg(line1_Start, line1_End);
            LineSeg line2 = new LineSeg(line2_Start, line2_End);
            Errortype ret = ComAlgo.CalcAngleLL(line1, line2, out angle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HOperatorSet.TupleDeg(angle, out HTuple deg);
            angle = deg.D;
            return ret;
        }

        /// <summary>
        /// 设置增益标定起始位置信息
        /// </summary>
        /// <param name="leftPixel">左侧像素坐标</param>
        /// <param name="rightPixel">右侧像素坐标</param>
        /// <param name="leftItemName">左侧相机标定名称</param>
        /// <param name="rightItemName">右侧相机标定名称</param>
        /// <param name="leftRuler">左侧光栅</param>
        /// <param name="rightRuler">右侧光栅</param>
        /// <param name="firstGainLeftRuler">输出左侧mark点光栅</param>
        /// <param name="firstGainRightRuler">输出右侧mark点光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetFirstGainInfo(Point leftPixel, Point rightPixel, string leftItemName, string rightItemName,
            Point leftRuler, Point rightRuler, out Point firstGainLeftRuler, out Point firstGainRightRuler)
        {
            firstGainLeftRuler = new Point();
            firstGainRightRuler = new Point();

            if (leftPixel is null || rightPixel is null || leftItemName is null || rightItemName is null || leftRuler is null || rightRuler is null)
            {
                return Errortype.FUSIONMANAGER_INPUT_NULL;
            }

            Errortype ret = Errortype.OK;

            //坐标转换
            ret = GetRulerByPix(leftItemName, leftRuler, leftPixel, out _firstGainLeftRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            firstGainLeftRuler = new Point(_firstGainLeftRuler.X, _firstGainLeftRuler.Y);

            ret = GetRulerByPix(rightItemName, rightRuler, rightPixel, out _firstGainRightRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            firstGainRightRuler = new Point(_firstGainRightRuler.X, _firstGainRightRuler.Y);

            _leftGainRuler = leftRuler;
            _rightGainRuler = rightRuler;
            _leftItemName = leftItemName;
            _rightItemName = rightItemName;

            return Errortype.OK;
        }

        /// <summary>
        /// 设置增益标定终止位置信息
        /// </summary>
        /// <param name="leftPixel">左侧mark像素坐标</param>
        /// <param name="rightPixel">右侧mark像素坐标</param>
        /// <param name="secondGainLeftRuler">输出左侧mark光栅</param>
        /// <param name="secondGainRightRuler">输出右侧mark光栅</param>
        /// <param name="dist">输出增益标定前后的移动距离</param>
        /// <param name="angle">输出增益标定前后的旋转角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetSecondGainInfo(Point leftPixel, Point rightPixel, out Point secondGainLeftRuler, out Point secondGainRightRuler, out double dist, out double angle)
        {
            dist = 0;
            angle = 0;
            secondGainLeftRuler = new Point();
            secondGainRightRuler = new Point();

            if (leftPixel is null || rightPixel is null)
            {
                return Errortype.FUSIONMANAGER_INPUT_NULL;
            }

            Errortype ret = Errortype.OK;

            //坐标转换
            ret = GetRulerByPix(_leftItemName, _leftGainRuler, leftPixel, out secondGainLeftRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = GetRulerByPix(_rightItemName, _rightGainRuler, rightPixel, out secondGainRightRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = CalcAngleLL(_firstGainLeftRuler, _firstGainRightRuler, secondGainLeftRuler, secondGainRightRuler, out angle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            Point centerFirst = (_firstGainLeftRuler + _firstGainRightRuler) / 2;
            Point centerSecond = (secondGainLeftRuler + secondGainRightRuler) / 2;
            dist = ComAlgo.Dist(centerFirst, centerSecond);

            return Errortype.OK;
        }

        #endregion

        #region 对准流程方法

        private static Point _leftRuler; //相机左侧光栅
        private static Point _rightRuler; //相机右侧光栅
        private static Point _bottomLeftPixel; //底部左侧mark像素
        private static Point _bottomRightPixel; //底部右侧mark像素
        private static Point _bottomLeftMarkRuler; //底部左侧mark光栅
        private static Point _bottomRightMarkRuler; //底部右侧mark光栅

        private static Point _pecLeftBasePixel; //pec左侧基准像素
        private static Point _pecRightBasePixel; //pec右侧基准像素
        private static Point _pecLeftBaseRuler; //pec左侧基准光栅
        private static Point _pecRightBaseRuler; //pec右侧基准光栅

        /// <summary>
        /// 计算线段与x轴的旋转平移量
        /// </summary>
        /// <param name="pointStart">线段起点</param>
        /// <param name="pointEnd">线段终点</param>
        /// <param name="translation">平移量</param>
        /// <param name="rotate">旋转量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcLXTR(Point pointStart, Point pointEnd, out Point translation, out double rotate)
        {
            Point basePoint1 = new Point(-1, 0);
            Point basePoint2 = new Point(1, 0);
            Errortype ret = CalcTR(basePoint1, basePoint2, pointStart, pointEnd, out translation, out rotate);
            return ret;
        }

        /// <summary>
        /// 计算当前点对到基准点对的旋转平移，默认旋转中心在0，0
        /// </summary>
        /// <param name="basePoint1">基准点1</param>
        /// <param name="basePoint2">基准点2</param>
        /// <param name="curPoint1">当前点1</param>
        /// <param name="curPoint2">当前点2</param>
        /// <param name="translation">平移量</param>
        /// <param name="rotate">旋转量</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcTR(Point basePoint1, Point basePoint2, Point curPoint1, Point curPoint2, out Point translation, out double rotate)
        {
            translation = new Point();
            rotate = 0;

            if (basePoint1 is null || basePoint2 is null || curPoint1 is null || curPoint2 is null)
            {
                return Errortype.FUSIONMANAGER_INPUT_NULL;
            }

            Point baseCenterRuler = (basePoint1 + basePoint2) / 2;
            Point curCenterRuler = (curPoint1 + curPoint2) / 2;

            Errortype ret = CalcAngleLL(curPoint1, curPoint2, basePoint1, basePoint2, out rotate);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            // 角度取反 因为calcAngleLL的结果为反向
            //ret = ComAlgo.CalcRotatePoint(curCenterRuler, -rotate, new Point(0, 0), out Point pointRotated);
            //if (ret != Errortype.OK)
            //{
            //    return ret;
            //}
            //translation = baseCenterRuler - pointRotated;
            translation = baseCenterRuler - curCenterRuler;
            return Errortype.OK;
        }

        /// <summary>
        /// 设置偏差补偿值
        /// </summary>
        /// <param name="offsetXY">xy补偿，单位um</param>
        /// <param name="offsetTheta">角度补偿，单位urad</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetOffset(Point offsetXY, double offsetTheta)
        {
            _offsetTheta = offsetTheta / Math.PI * 180 / 1e6;
            _offsetXY = offsetXY / 1e3;

            if (ComAlgo.SaveFlg("SetOffset", out int days))
            {
                string path = @"D:\Alg\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = path + "SetOffset.txt";
                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);

                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string text = time + " SetOffset " +
                              offsetXY.ToString(sep) + sep + offsetTheta.ToString() + sep +
                              _offsetXY.ToString(sep) + sep + _offsetTheta.ToString();
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取偏差补偿值
        /// </summary>
        /// <param name="offsetXy">xy补偿，单位um</param>
        /// <param name="offsetTheta">角度补偿，单位urad</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetOffset(out Point offsetXy, out double offsetTheta)
        {
            offsetXy = _offsetXY * 1e3;
            offsetTheta = _offsetTheta * 1e6 / 180 * Math.PI;
            return Errortype.OK;
        }

        /// <summary>
        /// 设置底部wafer信息
        /// </summary>
        /// <param name="bottomLeftPixel">底部左侧mark像素坐标</param>
        /// <param name="bottomRightPixel">底部右侧mark像素坐标</param>
        /// <param name="leftRuler">左侧光栅坐标</param>
        /// <param name="rightRuler">右侧光栅坐标</param>
        /// <param name="bottomLeftRuler">底部左侧mark光栅坐标</param>
        /// <param name="bottomRightRuler">底部右侧mark光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetBottomWaferInfo(Point bottomLeftPixel, Point bottomRightPixel, Point leftRuler, Point rightRuler, out Point bottomLeftRuler, out Point bottomRightRuler)
        {
            bottomLeftRuler = new Point();
            bottomRightRuler = new Point();

            Errortype ret = Errortype.OK;

            _bottomLeftPixel = bottomLeftPixel;
            _bottomRightPixel = bottomRightPixel;

            _leftRuler = leftRuler;
            _rightRuler = rightRuler;

            ret = GetRulerByPix(_svaParamInfo.TopLeftItemName, leftRuler, _bottomLeftPixel, out _bottomLeftMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            bottomLeftRuler = _bottomLeftMarkRuler;

            ret = GetRulerByPix(_svaParamInfo.TopRightItemName, rightRuler, _bottomRightPixel, out _bottomRightMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            bottomRightRuler = _bottomRightMarkRuler;

            return Errortype.OK;
        }

        /// <summary>
        /// 设置初始wafe信息
        /// </summary>
        /// <param name="bottomLeftPixel">左下像素坐标</param>
        /// <param name="bottomRightPixel">右下像素坐标</param>
        /// <param name="leftRuler">左侧光栅坐标</param>
        /// <param name="rightRuler">右侧光栅坐标</param>
        /// <param name="leftOpticName">左Name</param>
        /// <param name="rightOpticName">右Name</param>
        /// <param name="bottomLeftRuler">左下光栅</param>
        /// <param name="bottomRightRuler">右下光栅</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetBaseWaferInfo(Point bottomLeftPixel, Point bottomRightPixel, Point leftRuler, Point rightRuler, string leftOpticName, string rightOpticName, out Point bottomLeftRuler, out Point bottomRightRuler)
        {
            bottomLeftRuler = new Point();
            bottomRightRuler = new Point();

            Errortype ret = Errortype.OK;

            _bottomLeftPixel = bottomLeftPixel;
            _bottomRightPixel = bottomRightPixel;

            _leftRuler = leftRuler;
            _rightRuler = rightRuler;

            ret = GetRulerByPix(leftOpticName, leftRuler, _bottomLeftPixel, out _bottomLeftMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            bottomLeftRuler = _bottomLeftMarkRuler;

            ret = GetRulerByPix(rightOpticName, rightRuler, _bottomRightPixel, out _bottomRightMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            bottomRightRuler = _bottomRightMarkRuler;

            if (ComAlgo.SaveFlg("TopWaferTR", out int days))
            {
                string path = @"D:\Alg\";
                string fileName = path + "CalcBaseCurTR.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string txt = time + sep + leftOpticName + sep + rightOpticName + sep +
                             _bottomLeftPixel.ToString(sep) + sep + _bottomRightPixel.ToString(sep) + sep +
                             leftRuler.ToString(sep) + sep + rightRuler.ToString(sep) + sep +
                             _bottomLeftMarkRuler.ToString(sep) + sep + _bottomRightMarkRuler.ToString(sep) + sep;

                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);
                sw.WriteLine(txt);
                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 设置顶部wafer信息 弃用
        /// </summary>
        /// <param name="topLeftPixel">顶部左侧mark像素坐标</param>
        /// <param name="topRightPixel">顶部右侧mark像素坐标</param>
        /// <param name="translation">输出对齐需要的平移量</param>
        /// <param name="rotate">输出对齐需要的旋转量</param>
        /// <param name="topLeftRuler">顶部左侧mark光栅坐标</param>
        /// <param name="topRightRuler">顶部右侧mark光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetTopWaferInfo(Point topLeftPixel, Point topRightPixel, out Point translation, out double rotate, out Point topLeftRuler, out Point topRightRuler)
        {
            translation = new Point();
            rotate = 0;
            topLeftRuler = new Point();
            topRightRuler = new Point();

            Errortype ret = Errortype.OK;

            //计算光栅坐标
            ret = GetRulerByPix(_svaParamInfo.BottomLeftItemName, _leftRuler, topLeftPixel, out Point topLeftMarkRuler);

            // 单相机art测试使用
            //ret = GetRulerByPix(_svaParamInfo._topLeftItemName, _leftRuler, topLeftPixel, out Point topLeftMarkRuler);
            //ret = GetRulerByPix(_svaParamInfo._topLeftItemName, new Point(0, 0), topLeftPixel, out Point topLeftMarkRulerRelative);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            topLeftRuler = topLeftMarkRuler;

            ret = GetRulerByPix(_svaParamInfo.BottomRightItemName, _rightRuler, topRightPixel, out Point topRightMarkRuler);

            // 单相机art测试使用
            //ret = GetRulerByPix(_svaParamInfo._topRightItemName, _rightRuler, topRightPixel, out Point topRightMarkRuler);
            //ret = GetRulerByPix(_svaParamInfo._topRightItemName, new Point(0, 0), topRightPixel, out Point topRightMarkRulerRelateive);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            topRightRuler = topRightMarkRuler;

            //将顶部相机的像素投影到底部相机上
            //ret = GetBottomPosition(_svaParamInfo._leftCoaxialityItemName, topLeftMarkRuler, out Point top_bottomLeftRuler);
            ret = GetTopPosition(_svaParamInfo.LeftCoaxialityItemName, topLeftMarkRuler, out Point top_bottomLeftRuler); //顶部mark是底部相机看到的 转到顶部相机看到的 用GetTopPosition
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //ret = GetBottomPosition(_svaParamInfo._rightCoaxialityItemName, topRightMarkRuler, out Point top_bottomRightRuler);
            ret = GetTopPosition(_svaParamInfo.RightCoaxialityItemName, topRightMarkRuler, out Point top_bottomRightRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //计算两组mark之间的平移旋转
            ret = CalcTR(_bottomLeftMarkRuler, _bottomRightMarkRuler, top_bottomLeftRuler, top_bottomRightRuler, out translation, out rotate);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //new alignment
            //Point deltaLeft = top_bottomLeftPixel - _bottomLeftPixel;
            //Point deltaRight = top_bottomRightPixel - _bottomRightPixel;
            //ret = GetRulerByPix(_svaParamInfo._bottomLeftItemName, _leftRuler, deltaLeft + _svaParamInfo._imgCenter, out Point deltaLeftRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaLeftRuler = deltaLeftRuler - _leftRuler;
            //ret = GetRulerByPix(_svaParamInfo._bottomRightItemName, _rightRuler, deltaRight + _svaParamInfo._imgCenter, out Point deltaRightRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaRightRuler = deltaRightRuler - _rightRuler;

            //translation = (deltaLeftRuler + deltaRightRuler) / -2;
            //rotate = Math.Atan((deltaRightRuler.Y - deltaLeftRuler.Y) / (_rightRuler.X - _leftRuler.X)) / Math.PI * 180;

            // 用mark相对坐标计算轴坐标,单相机art测试使用
            //Point currentRulerLeft = topLeftMarkRuler - topLeftMarkRulerRelative;
            //Point currentRulerRight = topRightMarkRuler - topRightMarkRulerRelateive;
            if (ComAlgo.SaveFlg("TopWaferTR", out int days))
            {
                string path = @"D:\Alg\";
                string fileName = path + "CalcTR.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string txt = time + sep +
                    _bottomLeftPixel.ToString(sep) + sep + _bottomRightPixel.ToString(sep) + sep +
                    topLeftPixel.ToString(sep) + sep + topRightPixel.ToString(sep) + sep +
                    _bottomLeftMarkRuler.ToString(sep) + sep + _bottomRightMarkRuler.ToString(sep) + sep +
                    topLeftMarkRuler.ToString(sep) + sep + topRightMarkRuler.ToString(sep) + sep +
                    top_bottomLeftRuler.ToString(sep) + sep + top_bottomRightRuler.ToString(sep) + sep +

                    // optic轴坐标,单相机art测试使用
                    //sep + currentRulerLeft.ToString(sep) + sep + currentRulerRight.ToString(sep) + sep + sep +
                    translation.ToString(sep) + sep + rotate.ToString();
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);
                sw.WriteLine(txt);
                sw.Close();
                fs.Close();
            }

            return ret;
        }

        /// <summary>
        /// 设置当前wafer信息
        /// </summary>
        /// <param name="topLeftPixel">左上像素坐标</param>
        /// <param name="topRightPixel">右上像素坐标</param>
        /// <param name="leftOpticName">左相机标定名称</param>
        /// <param name="rightOpticName">右相机标定名称</param>
        /// <param name="translation">平移量</param>
        /// <param name="rotate">转角</param>
        /// <param name="topLeftRuler">左上光栅坐标</param>
        /// <param name="topRightRuler">右上光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetCurrentWaferInfo(Point topLeftPixel, Point topRightPixel, string leftOpticName, string rightOpticName, out Point translation, out double rotate, out Point topLeftRuler, out Point topRightRuler)
        {
            translation = new Point();
            rotate = 0;
            topLeftRuler = new Point();
            topRightRuler = new Point();

            Errortype ret = Errortype.OK;

            //计算光栅坐标
            ret = GetRulerByPix(leftOpticName, _leftRuler, topLeftPixel, out Point topLeftMarkRuler);

            // 单相机art测试使用
            //ret = GetRulerByPix(_svaParamInfo._topLeftItemName, _leftRuler, topLeftPixel, out Point topLeftMarkRuler);
            //ret = GetRulerByPix(_svaParamInfo._topLeftItemName, new Point(0, 0), topLeftPixel, out Point topLeftMarkRulerRelative);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            topLeftRuler = topLeftMarkRuler;

            ret = GetRulerByPix(rightOpticName, _rightRuler, topRightPixel, out Point topRightMarkRuler);

            // 单相机art测试使用
            //ret = GetRulerByPix(_svaParamInfo._topRightItemName, _rightRuler, topRightPixel, out Point topRightMarkRuler);
            //ret = GetRulerByPix(_svaParamInfo._topRightItemName, new Point(0, 0), topRightPixel, out Point topRightMarkRulerRelateive);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            topRightRuler = topRightMarkRuler;

            //将顶部相机的像素投影到底部相机上
            //ret = GetBottomPosition(_svaParamInfo._leftCoaxialityItemName, topLeftMarkRuler, out Point top_bottomLeftRuler);
            ret = GetTopPosition(_svaParamInfo.LeftCoaxialityItemName, topLeftMarkRuler, out Point top_bottomLeftRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //ret = GetBottomPosition(_svaParamInfo._rightCoaxialityItemName, topRightMarkRuler, out Point top_bottomRightRuler);
            ret = GetTopPosition(_svaParamInfo.RightCoaxialityItemName, topRightMarkRuler, out Point top_bottomRightRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //计算两组mark之间的平移旋转
            //Point bottomLeftMarkRulerTranslation = _bottomLeftMarkRuler + _offsetXY;
            //Point bottomRightMarkRulerTranslation = _bottomRightMarkRuler + _offsetXY;
            ret = CalcTR(_bottomLeftMarkRuler, _bottomRightMarkRuler, top_bottomLeftRuler, top_bottomRightRuler, out translation, out rotate);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            translation += _offsetXY;
            rotate += _offsetTheta;

            //new alignment
            //Point deltaLeft = top_bottomLeftPixel - _bottomLeftPixel;
            //Point deltaRight = top_bottomRightPixel - _bottomRightPixel;
            //ret = GetRulerByPix(_svaParamInfo._bottomLeftItemName, _leftRuler, deltaLeft + _svaParamInfo._imgCenter, out Point deltaLeftRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaLeftRuler = deltaLeftRuler - _leftRuler;
            //ret = GetRulerByPix(_svaParamInfo._bottomRightItemName, _rightRuler, deltaRight + _svaParamInfo._imgCenter, out Point deltaRightRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaRightRuler = deltaRightRuler - _rightRuler;

            //translation = (deltaLeftRuler + deltaRightRuler) / -2;
            //rotate = Math.Atan((deltaRightRuler.Y - deltaLeftRuler.Y) / (_rightRuler.X - _leftRuler.X)) / Math.PI * 180;

            // 用mark相对坐标计算轴坐标,单相机art测试使用
            //Point currentRulerLeft = topLeftMarkRuler - topLeftMarkRulerRelative;
            //Point currentRulerRight = topRightMarkRuler - topRightMarkRulerRelateive;
            if (ComAlgo.SaveFlg("TopWaferTR", out int days))
            {
                string path = @"D:\Alg\";
                string fileName = path + "CalcBaseCurTR.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string txt = time + sep + leftOpticName + sep + rightOpticName + sep +
                    _bottomLeftPixel.ToString(sep) + sep + _bottomRightPixel.ToString(sep) + sep +
                    topLeftPixel.ToString(sep) + sep + topRightPixel.ToString(sep) + sep +
                    _bottomLeftMarkRuler.ToString(sep) + sep + _bottomRightMarkRuler.ToString(sep) + sep +
                    topLeftMarkRuler.ToString(sep) + sep + topRightMarkRuler.ToString(sep) + sep +
                    top_bottomLeftRuler.ToString(sep) + sep + top_bottomRightRuler.ToString(sep) + sep +

                    // optic轴坐标,单相机art测试使用
                    // sep + currentRulerLeft.ToString(sep) + sep + currentRulerRight.ToString(sep) + sep + sep +
                    translation.ToString(sep) + sep + rotate.ToString() + sep +
                    _offsetXY.ToString(sep) + sep + _offsetTheta.ToString();

                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);
                sw.WriteLine(txt);
                sw.Close();
                fs.Close();
            }

            return ret;
        }

        /// <summary>
        /// 设置第一次pec信息
        /// </summary>
        /// <param name="leftPixel">左侧mark像素坐标</param>
        /// <param name="rightPixel">右侧mark像素坐标</param>
        /// <param name="leftRuler">左侧光栅像素坐标</param>
        /// <param name="rightRuler">右侧光栅像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetBasePecInfo(Point leftPixel, Point rightPixel, out Point leftRuler, out Point rightRuler)
        {
            leftRuler = new Point();
            rightRuler = new Point();

            Errortype ret = Errortype.OK;

            _pecLeftBasePixel = leftPixel;
            ret = GetRulerByPix(_svaParamInfo.PecLeftItemName, _svaParamInfo.PecLeftRuler, _pecLeftBasePixel, out _pecLeftBaseRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            leftRuler = _pecLeftBaseRuler;

            _pecRightBasePixel = rightPixel;
            ret = GetRulerByPix(_svaParamInfo.PecRightItemName, _svaParamInfo.PecRightRuler, _pecRightBasePixel, out _pecRightBaseRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            rightRuler = _pecRightBaseRuler;

            if (ComAlgo.SaveFlg("PECInfo", out int days))
            {
                string path = @"D:\Alg\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = path + "PecInfo.txt";
                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);

                string time = ComAlgo.GetDateTime();
                string text = time + " SetBasePecInfo " + leftPixel.ToString(" ") + " " + rightPixel.ToString(" ") + " " + _pecLeftBaseRuler.ToString(" ") + " " + _pecRightBaseRuler.ToString(" ");
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();

            return Errortype.OK;
        }

        /// <summary>
        /// 设置第二次pec信息
        /// </summary>
        /// <param name="leftPixel">左侧mark像素坐标</param>
        /// <param name="rightPixel">右侧mark像素坐标</param>
        /// <param name="translation">输出第二次到第一次的平移量</param>
        /// <param name="rotate">输出第二次到第一次的旋转量</param>
        /// <param name="leftRuler">左侧mark光栅坐标</param>
        /// <param name="rightRuler">右侧mark光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetCurPecInfo(Point leftPixel, Point rightPixel, out Point translation, out double rotate, out Point leftRuler, out Point rightRuler)
        {
            translation = new Point();
            rotate = 0;
            leftRuler = new Point();
            rightRuler = new Point();

            Errortype ret = Errortype.OK;

            ret = GetRulerByPix(_svaParamInfo.PecLeftItemName, _svaParamInfo.PecLeftRuler, leftPixel, out Point pecLeftCurRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            leftRuler = pecLeftCurRuler;

            ret = GetRulerByPix(_svaParamInfo.PecRightItemName, _svaParamInfo.PecRightRuler, rightPixel, out Point pecRightCurRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            rightRuler = pecRightCurRuler;

            //计算两组mark之间的平移旋转
            ret = CalcTR(_pecLeftBaseRuler, _pecRightBaseRuler, pecLeftCurRuler, pecRightCurRuler, out translation, out rotate);

            ////new alignment
            //Point deltaLeft = leftPixel - _pecLeftBasePixel;
            //Point deltaRight = rightPixel - _pecRightBasePixel;
            //ret = GetRulerByPix(_svaParamInfo._pecLeftItemName, _svaParamInfo._pecLeftRuler, deltaLeft + _svaParamInfo._imgCenter, out Point deltaLeftRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaLeftRuler = deltaLeftRuler - _svaParamInfo._pecLeftRuler;
            //ret = GetRulerByPix(_svaParamInfo._pecRightItemName, _svaParamInfo._pecRightRuler, deltaRight + _svaParamInfo._imgCenter, out Point deltaRightRuler);
            //if (ret != Errortype.OK) return ret;
            //deltaRightRuler = deltaRightRuler - _svaParamInfo._pecRightRuler;

            //translation = (deltaLeftRuler + deltaRightRuler) / -2;
            //rotate = Math.Atan((deltaRightRuler.Y - deltaLeftRuler.Y) / (_svaParamInfo._pecRightRuler.X - _svaParamInfo._pecLeftRuler.X)) / Math.PI * 180;
            if (ComAlgo.SaveFlg("PECInfo", out int days))
            {
                string path = @"D:\Alg\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = path + "PecInfo.txt";
                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);

                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string text = time + " SetCurPecInfo " +
                              _pecLeftBasePixel.ToString(sep) + sep + _pecRightBasePixel.ToString(sep) + sep +
                              leftPixel.ToString(sep) + sep + rightPixel.ToString(sep) + sep +
                              _pecLeftBaseRuler.ToString(sep) + sep + _pecRightBaseRuler.ToString(sep) + sep +
                              pecLeftCurRuler.ToString(sep) + sep + pecRightCurRuler.ToString(sep) + sep +
                              translation.ToString(sep) + sep + rotate.ToString();
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
            }

            return ret;
        }

        /// <summary>
        /// calc art error
        /// </summary>
        /// <param name="data">art data</param>
        /// <param name="errorXy">error xy</param>
        /// <param name="errorT">error t</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcArtError(AlignmentRepeatabilityTestData data, out Point errorXy, out double errorT)
        {
            Errortype ret = Errortype.OK;
            Point bottomWaferInspectXy = (data.BottomWaferLeftMarkInspectXy + data.BottomWaferRightMarkInspectXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.BottomWaferLeftMarkInspectXy, data.BottomWaferRightMarkInspectXy, out Point bottomWaferInspectTranslation, out double bottomWaferInspectT);
            bottomWaferInspectT = bottomWaferInspectT / 180 * Math.PI;

            Point topWaferInspectXy = (data.TopWaferLeftMarkInspectXy + data.TopWaferRightMarkInspectXy) / 2;
            ret = FusionManagerSimplifyCalib.CalcLXTR(data.TopWaferLeftMarkInspectXy, data.TopWaferRightMarkInspectXy, out Point topWaferInspectTranslation, out double topWaferInspectT);
            topWaferInspectT = topWaferInspectT / 180 * Math.PI;

            errorXy = (topWaferInspectXy - bottomWaferInspectXy) * 1e3; //mm to um
            errorT = (topWaferInspectT - bottomWaferInspectT) * 1e6; //rad to urad

            errorXy *= -1; //XY结果取反用于误差直观显示
            return Errortype.OK;
        }
        #endregion
    }

    /// <summary>
    /// UBA流程管理
    /// </summary>
    public static class UBAManager
    {
        private static CalibParam _calibParam = null; //标定类管理

        private static CalibPix _calibPix = new CalibPix(); //像素比标定对象

        private static AlignParam _alignParam = new AlignParam(); //对准参数管理

        private static Point _leftOffsetXy = new Point(0, 0); //左侧Mark偏移量 单位mm

        private static Point _rightOffsetXy = new Point(0, 0); //右侧Mark偏移量 单位mm

        /// <summary>
        /// 设置标定参数
        /// </summary>
        /// <param name="calibParam">标定参数</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SetParamInfo(CalibParam calibParam)
        {
            if (calibParam is null)
            {
                return Errortype.UBA_CALIBPARAM_NULL;
            }

            _calibParam = calibParam;

            Errortype ret = _calibParam.Check();
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _calibPix = new CalibPix();
            ret = _calibPix.Init(new List<string>() { _calibParam.LeftCamName, _calibParam.RightCamName });
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = _calibPix.SetPixCenter(_calibParam.LeftCamName, _calibParam.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = _calibPix.SetScaleAngle(_calibParam.LeftCamName, _calibParam.LeftCamScaleX, _calibParam.LeftCamScaleY, _calibParam.LeftCamAngle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = _calibPix.SetPixCenter(_calibParam.RightCamName, _calibParam.ImgCenter);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = _calibPix.SetScaleAngle(_calibParam.RightCamName, _calibParam.RightCamScaleX, _calibParam.RightCamScaleY, _calibParam.RightCamAngle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype Release()
        {
            _calibParam = null;
            _calibPix.Release();
            _alignParam = new AlignParam();
            return Errortype.OK;
        }

        /// <summary>
        /// 标定x方向像素比和转角
        /// </summary>
        /// <param name="camName">相机名</param>
        /// <param name="pix1">第一个点像素坐标</param>
        /// <param name="pix2">第二个点像素坐标</param>
        /// <param name="ruler1">第一个点光栅坐标</param>
        /// <param name="ruler2">第二个点光栅坐标</param>
        /// <param name="scaleX">x方向像素比</param>
        /// <param name="angle">转角</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype CalibScaleXAndAngle(string camName, Point pix1, Point pix2, Point ruler1, Point ruler2, out double scaleX, out double angle)
        {
            scaleX = 0;
            angle = 0;
            Errortype ret = _calibPix.CalibPixAngle(camName, pix1, pix2, out angle);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            Point axisDist = ruler2 - ruler1;
            ret = _calibPix.CalibPixScaleX(camName, pix1, pix2, ruler1, ruler2, out scaleX);
            return ret;
        }

        /// <summary>
        /// 标定y方向像素比
        /// </summary>
        /// <param name="camName">相机名</param>
        /// <param name="pix1">第一个点像素坐标</param>
        /// <param name="pix2">第二个点像素坐标</param>
        /// <param name="ruler1">第一个点光栅坐标</param>
        /// <param name="ruler2">第二个点光栅坐标</param>
        /// <param name="scaleY">y方向像素比</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype CalibScaleY(string camName, Point pix1, Point pix2, Point ruler1, Point ruler2, out double scaleY)
        {
            Point axisDist = ruler2 - ruler1;
            Errortype ret = _calibPix.CalibPixScaleY(camName, pix1, pix2, ruler1, ruler2, out scaleY);
            return ret;
        }

        /// <summary>
        /// 像素点转轴坐标
        /// </summary>
        /// <param name="camName">相机名</param>
        /// <param name="currentRuler">当前的光栅值</param>
        /// <param name="targetMarkCenterPix">待转换像素点(mark中心像素)</param>
        /// <param name="targetMarkCenterRuler">目标点的光栅位置</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GetRulerByPix(string camName, Point currentRuler, Point targetMarkCenterPix, out Point targetMarkCenterRuler)
        {
            Errortype ret = _calibPix.GetRulerByPix(camName, targetMarkCenterPix, currentRuler, out targetMarkCenterRuler);
            return ret;
        }

        /// <summary>
        /// 像素点转轴坐标
        /// </summary>
        /// <param name="camName">相机名</param>
        /// <param name="currentRuler">当前的光栅值</param>
        /// <param name="targetMarkCenterRuler">目标点的光栅位置</param>
        /// <param name="targetMarkCenterPix">待转换像素点(mark中心像素)</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype GetPixByRuler(string camName, Point currentRuler, Point targetMarkCenterRuler, out Point targetMarkCenterPix)
        {
            Errortype ret = _calibPix.GetPixByRuler(camName, currentRuler, targetMarkCenterRuler, out targetMarkCenterPix);
            return ret;
        }

        /// <summary>
        /// 计算当前点对到基准点对的旋转平移，默认旋转中心在0，0
        /// </summary>
        /// <param name="basePoint1">基准点1</param>
        /// <param name="basePoint2">基准点2</param>
        /// <param name="curPoint1">当前点1</param>
        /// <param name="curPoint2">当前点2</param>
        /// <param name="translation">平移量</param>
        /// <param name="rotate">旋转量</param>
        /// <returns>OK:成功；其他:失败</returns>
        private static Errortype CalcTR(Point basePoint1, Point basePoint2, Point curPoint1, Point curPoint2, out Point translation, out double rotate)
        {
            translation = new Point();
            rotate = 0;

            if (basePoint1 is null || basePoint2 is null || curPoint1 is null || curPoint2 is null)
            {
                return Errortype.UBA_CALCTR_INPUT_NULL;
            }

            Point baseCenterRuler = (basePoint1 + basePoint2) / 2;
            Point curCenterRuler = (curPoint1 + curPoint2) / 2;

            LineSeg line1 = new LineSeg(basePoint1, basePoint2);
            LineSeg line2 = new LineSeg(curPoint1, curPoint2);
            Errortype ret = ComAlgo.CalcAngleLL(line1, line2, out double angleRad);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HOperatorSet.TupleDeg(angleRad, out HTuple deg);
            rotate = deg.D;

            //ret = ComAlgo.CalcRotatePoint(curCenterRuler, rotate, new Point(0, 0), out Point pointRotated);
            //if (ret != Errortype.OK)
            //{
            //    return ret;
            //}
            translation = baseCenterRuler - curCenterRuler;
            return Errortype.OK;
        }

        /// <summary>
        /// 设置wafer1对位信息
        /// </summary>
        /// <param name="leftMarkPixel">左侧Mark像素坐标</param>
        /// <param name="rightMarkPixel">右侧Mark像素坐标</param>
        /// <param name="leftCamRuler">左侧相机光栅</param>
        /// <param name="rightCamRuler">右侧相机光栅</param>
        /// <param name="leftMarkRuler">左侧Mark光栅坐标</param>
        /// <param name="rightMarkRuler">右侧Mark光栅坐标</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SetBaseWaferInfo(Point leftMarkPixel, Point rightMarkPixel, Point leftCamRuler, Point rightCamRuler, out Point leftMarkRuler, out Point rightMarkRuler)
        {
            leftMarkRuler = new Point();
            rightMarkRuler = new Point();

            if (_calibParam is null)
            {
                return Errortype.UBA_CALIBPARAM_NULL;
            }

            Errortype ret = Errortype.OK;
            _alignParam = new AlignParam();

            _alignParam.BaseWaferLeftMarkPixel = leftMarkPixel;
            _alignParam.BaseWaferRightMarkPixel = rightMarkPixel;

            leftCamRuler += new Point(120, 0);

            _alignParam.LeftCamRuler = leftCamRuler;
            _alignParam.RightCamRuler = rightCamRuler;

            ret = GetRulerByPix(_calibParam.LeftCamName, leftCamRuler, leftMarkPixel, out leftMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _alignParam.BaseWaferLeftMarkRuler = leftMarkRuler;

            ret = GetRulerByPix(_calibParam.RightCamName, rightCamRuler, rightMarkPixel, out rightMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _alignParam.BaseWaferRightMarkRuler = rightMarkRuler;

            // 加offset 转回pixel
            _alignParam.BaseWaferLeftMarkRulerAfterOffset = _alignParam.BaseWaferLeftMarkRuler - _leftOffsetXy;
            _alignParam.BaseWaferRightMarkRulerAfterOffset = _alignParam.BaseWaferRightMarkRuler - _rightOffsetXy;

            ret = GetPixByRuler(_calibParam.LeftCamName, leftCamRuler, _alignParam.BaseWaferLeftMarkRulerAfterOffset, out Point leftMarkPixelAfterOffset);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _alignParam.BaseWaferLeftMarkPixelAfterOffset = leftMarkPixelAfterOffset;

            ret = GetPixByRuler(_calibParam.RightCamName, rightCamRuler, _alignParam.BaseWaferRightMarkRulerAfterOffset, out Point rightMarkPixelAfterOffset);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _alignParam.BaseWaferRightMarkPixelAfterOffset = rightMarkPixelAfterOffset;

            if (ComAlgo.SaveFlg("CalcBaseCurTR", out int days))
            {
                string path = @"D:\Alg\";
                string fileName = path + "CalcBaseCurTR.txt";
                string sep = " ";
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                string txt = time + sep + _calibParam.LeftCamName + sep + _calibParam.RightCamName + sep +
                             leftCamRuler.ToString(sep) + sep + rightCamRuler.ToString(sep) + sep +
                             leftMarkPixel.ToString(sep) + sep + rightMarkPixel.ToString(sep) + sep +
                             leftMarkRuler.ToString(sep) + sep + rightMarkRuler.ToString(sep) + sep +
                             _alignParam.BaseWaferLeftMarkRulerAfterOffset.ToString(sep) + sep + _alignParam.BaseWaferRightMarkRulerAfterOffset.ToString(sep) + sep +
                             _alignParam.BaseWaferLeftMarkPixelAfterOffset.ToString(sep) + sep + _alignParam.BaseWaferRightMarkPixelAfterOffset.ToString(sep) + sep;

                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);
                sw.WriteLine(txt);
                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 设置wafer2对位信息，计算旋转平移量
        /// </summary>
        /// <param name="leftMarkPixel">左侧Mark像素坐标</param>
        /// <param name="rightMarkPixel">右侧Mark像素坐标</param>
        /// <param name="translation">平移量</param>
        /// <param name="rotate">旋转量（角度）</param>
        /// <param name="leftError">左侧Mark误差</param>
        /// <param name="rightError">右侧Mark误差</param>
        /// <param name="leftMarkRuler">左侧Mark光栅坐标</param>
        /// <param name="rightMarkRuler">右侧Mark光栅坐标</param>
        /// <param name="measureOrAlign">测量或对位，false为测量，不计算补偿值</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SetCurrentWaferInfo(Point leftMarkPixel, Point rightMarkPixel, out Point translation, out double rotate, out Point leftError, out Point rightError,
            out Point leftMarkRuler, out Point rightMarkRuler, bool measureOrAlign = false)
        {
            translation = new Point();
            rotate = 0;
            leftMarkRuler = new Point();
            rightMarkRuler = new Point();
            leftError = new Point();
            rightError = new Point();

            if (_calibParam is null)
            {
                return Errortype.UBA_CALIBPARAM_NULL;
            }

            Errortype ret = Errortype.OK;

            _alignParam.CurWaferLeftMarkPixel = leftMarkPixel;
            _alignParam.CurWaferRightMarkPixel = rightMarkPixel;

            //计算光栅坐标
            ret = GetRulerByPix(_calibParam.LeftCamName, _alignParam.LeftCamRuler, leftMarkPixel, out leftMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _alignParam.CurWaferLeftMarkRuler = leftMarkRuler;

            ret = GetRulerByPix(_calibParam.RightCamName, _alignParam.RightCamRuler, rightMarkPixel, out rightMarkRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _alignParam.CurWaferRightMarkRuler = rightMarkRuler;

            Point leftBaseMarkRulerForCalc = new Point(0, 0);
            Point rightBaseMarkRulerForCalc = new Point(0, 0);

            if (measureOrAlign)
            {
                leftBaseMarkRulerForCalc = _alignParam.BaseWaferLeftMarkRuler.Clone();
                rightBaseMarkRulerForCalc = _alignParam.BaseWaferRightMarkRuler.Clone();
            }
            else
            {
                leftBaseMarkRulerForCalc = _alignParam.BaseWaferLeftMarkRulerAfterOffset.Clone();
                rightBaseMarkRulerForCalc = _alignParam.BaseWaferRightMarkRulerAfterOffset.Clone();
            }

            //计算两组mark之间的平移旋转
            ret = CalcTR(leftBaseMarkRulerForCalc, rightBaseMarkRulerForCalc, _alignParam.CurWaferLeftMarkRuler, _alignParam.CurWaferRightMarkRuler, out translation, out rotate);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            leftError = _alignParam.CurWaferLeftMarkRuler - leftBaseMarkRulerForCalc;
            rightError = _alignParam.CurWaferRightMarkRuler - rightBaseMarkRulerForCalc;

            if (ComAlgo.SaveFlg("CalcBaseCurTR", out int days))
            {
                string path = @"D:\Alg\";
                string fileName = path + "CalcBaseCurTR.txt";
                string sep = " ";
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                string measureOrAlignStr = measureOrAlign ? "Measure" : "Alignment";

                string txt = time + sep + _calibParam.LeftCamName + sep + _calibParam.RightCamName + sep +
                    leftMarkPixel.ToString(sep) + sep + rightMarkPixel.ToString(sep) + sep +
                    _alignParam.LeftCamRuler.ToString(sep) + sep + _alignParam.RightCamRuler.ToString(sep) + sep +
                    leftMarkRuler.ToString(sep) + sep + rightMarkRuler.ToString(sep) + sep +
                    translation.ToString(sep) + sep + rotate.ToString("f6") + sep +
                    _leftOffsetXy.ToString(sep) + sep + _rightOffsetXy.ToString(sep) + sep +
                    leftError.ToString(sep) + sep + rightError.ToString(sep) + sep + measureOrAlignStr;

                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);
                sw.WriteLine(txt);
                sw.Close();
                fs.Close();
            }

            return ret;
        }

        /// <summary>
        /// 裁剪固定视野的图像
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="cropSize">裁剪尺寸（mm）</param>
        /// <param name="cropImg">裁剪后的图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CropImg(Camera img, Point cropSize, out Camera cropImg)
        {
            cropImg = null;
            if (img == null || img.Height < 1 || img.Width < 1)
            {
                return Errortype.UBA_CROPIMG_IMAGE_NULL;
            }

            if (cropSize == null)
            {
                return Errortype.UBA_CROPIMG_INPUT_POINT_NULL;
            }

            if (cropSize.X < 4 || cropSize.Y < 4)
            {
                return Errortype.UBA_CROPIMG_INPUT_POINT_LITTLE;
            }

            int newWidth = (int)(Math.Floor(cropSize.X / 4) * 4); //保证裁剪后的图像为4的倍数
            int newHeight = (int)(Math.Floor(cropSize.Y / 4) * 4);
            double stepX = newWidth / 2;
            double stepY = newHeight / 2;
            Point pixCenter = new Point(img.Width / 2, img.Height / 2);

            HOperatorSet.Rgb1ToGray(img.GenHObject(), out HObject grayImage);
            HOperatorSet.GenRectangle1(out HObject rectangle, pixCenter.Y - stepY + 1, pixCenter.X - stepX + 1,
                pixCenter.Y + stepY, pixCenter.X + stepX);
            HOperatorSet.ReduceDomain(grayImage, rectangle, out HObject reduceImg);
            HOperatorSet.CropDomain(reduceImg, out HObject cropImagePart);

            cropImg = new Camera(cropImagePart);

            grayImage.Dispose();
            rectangle.Dispose();
            reduceImg.Dispose();
            cropImagePart.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 获取top wafer mark像素坐标
        /// </summary>
        /// <param name="leftMarkPixel">左侧Mark像素</param>
        /// <param name="rightMarkPixel">右侧Mark像素</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetTopWaferMarkCenter(out Point leftMarkPixel, out Point rightMarkPixel)
        {
            leftMarkPixel = null;
            rightMarkPixel = null;
            if (_alignParam.BaseWaferLeftMarkPixelAfterOffset is null || _alignParam.BaseWaferRightMarkPixelAfterOffset is null)
            {
                return Errortype.UBA_BASEWAFER_INFO_NULL;
            }

            leftMarkPixel = _alignParam.BaseWaferLeftMarkPixelAfterOffset.Clone();
            rightMarkPixel = _alignParam.BaseWaferRightMarkPixelAfterOffset.Clone();
            return Errortype.OK;
        }

        /// <summary>
        /// 设置偏差补偿值
        /// </summary>
        /// <param name="leftOffsetXy">左侧Mark偏移补偿，单位mm</param>
        /// <param name="rightOffsetXy">右侧Mark偏移补偿，单位mm</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetOffset(Point leftOffsetXy, Point rightOffsetXy)
        {
            if (leftOffsetXy is null || rightOffsetXy is null)
            {
                return Errortype.UBA_SETOFFSET_INPUT_POINT_NULL;
            }

            _leftOffsetXy = leftOffsetXy.Clone();
            _rightOffsetXy = rightOffsetXy.Clone();

            if (ComAlgo.SaveFlg("SetOffset", out int days))
            {
                string path = @"D:\Alg\";
                string fileName = path + "SetOffset.txt";
                string sep = " ";
                string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");

                string txt = time + sep + leftOffsetXy.ToString(sep) + sep + rightOffsetXy.ToString(sep) + sep;

                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));
                }

                FileStream fs;
                StreamWriter sw;
                fs = new FileStream(fileName, FileMode.Append);
                sw = new StreamWriter(fs);
                sw.WriteLine(txt);
                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 输出对准参数
        /// </summary>
        /// <param name="alignParam">对准参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetAlignParam(out AlignParam alignParam)
        {
            alignParam = _alignParam.Clone();
            return Errortype.OK;
        }

        /// <summary>
        /// 获取补偿后的像素坐标
        /// </summary>
        /// <param name="camName">相机名称</param>
        /// <param name="curRuler">当前相机轴坐标</param>
        /// <param name="centerPixel">绘制的像素坐标</param>
        /// <param name="offset">补偿值（mm）</param>
        /// <param name="centerPixelAfterOffset">补偿后的像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetMarkCenterAfterOffset(string camName, Point curRuler, Point centerPixel, Point offset, out Point centerPixelAfterOffset)
        {
            centerPixelAfterOffset = centerPixel.Clone();
            Errortype ret = Errortype.OK;
            ret = GetRulerByPix(camName, curRuler, centerPixel, out Point centerRuler);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            Point centerRulerAfterOffset = centerRuler - offset;
            ret = GetPixByRuler(camName, curRuler, centerRulerAfterOffset, out centerPixelAfterOffset);
            return ret;
        }
    }

    /// <summary>
    /// 标定相关参数
    /// </summary>
    public class CalibParam
    {
        /// <summary>
        /// Gets or sets 左侧相机名称
        /// </summary>
        public string LeftCamName { get; set; }

        /// <summary>
        /// Gets or sets 右侧相机名称
        /// </summary>
        public string RightCamName { get; set; }

        /// <summary>
        /// Gets or sets 左侧相机x方向像素比（带正负）
        /// </summary>
        public double LeftCamScaleX { get; set; }

        /// <summary>
        /// Gets or sets 左侧相机y方向像素比（带正负）
        /// </summary>
        public double LeftCamScaleY { get; set; }

        /// <summary>
        /// Gets or sets 右侧相机x方向像素比（带正负）
        /// </summary>
        public double RightCamScaleX { get; set; }

        /// <summary>
        /// Gets or sets 右侧相机y方向像素比（带正负）
        /// </summary>
        public double RightCamScaleY { get; set; }

        /// <summary>
        /// Gets or sets 左侧相机夹角
        /// </summary>
        public double LeftCamAngle { get; set; }

        /// <summary>
        /// Gets or sets 右侧相机夹角
        /// </summary>
        public double RightCamAngle { get; set; }

        /// <summary>
        /// Gets or sets 相机的中心像素
        /// </summary>
        public Point ImgCenter { get; set; } = new Point(1280 / 2.0, 1024 / 2.0);

        /// <summary>
        /// Gets or sets 检查参数是否存在不合理
        /// </summary>
        /// <returns>OK:成功；其他:失败</returns>
        public Errortype Check()
        {
            if (LeftCamName is null || LeftCamName == string.Empty)
            {
                return Errortype.CALIBPARAM_LEFTCAMNAME_EMPYT;
            }

            if (RightCamName is null || RightCamName == string.Empty)
            {
                return Errortype.CALIBPARAM_RIGHTCAMNAME_EMPYT;
            }

            if (LeftCamScaleX == 0 || LeftCamScaleY == 0)
            {
                return Errortype.CALIBPARAM_LEFTSCALE_ZERO;
            }

            if (RightCamScaleX == 0 || RightCamScaleY == 0)
            {
                return Errortype.CALIBPARAM_RIGHTSCALE_ZERO;
            }

            if (ImgCenter is null)
            {
                return Errortype.CALIBPARAM_IMGCENTER_NULL;
            }

            if (ImgCenter == new Point(0, 0))
            {
                return Errortype.CALIBPARAM_IMGCENTER_ZERO;
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 对准参数
    /// </summary>
    public class AlignParam
    {
        /// <summary>
        /// Gets or sets 左侧相机光栅值
        /// </summary>
        public Point LeftCamRuler { get; set; }

        /// <summary>
        /// Gets or sets 右侧相机光栅值
        /// </summary>
        public Point RightCamRuler { get; set; }

        /// <summary>
        /// Gets or sets wafer1左侧Mark像素坐标
        /// </summary>
        public Point BaseWaferLeftMarkPixel { get; set; }

        /// <summary>
        /// Gets or sets wafer1左侧Mark光栅坐标
        /// </summary>
        public Point BaseWaferLeftMarkRuler { get; set; }

        /// <summary>
        /// Gets or sets wafer1右侧Mark像素坐标
        /// </summary>
        public Point BaseWaferRightMarkPixel { get; set; }

        /// <summary>
        /// Gets or sets wafer1右侧Mark光栅坐标
        /// </summary>
        public Point BaseWaferRightMarkRuler { get; set; }

        /// <summary>
        /// Gets or sets wafer1右侧Mark光栅坐标 补偿后
        /// </summary>
        public Point BaseWaferRightMarkRulerAfterOffset { get; set; }

        /// <summary>
        /// Gets or sets wafer1左侧Mark像素坐标 补偿后
        /// </summary>
        public Point BaseWaferLeftMarkPixelAfterOffset { get; set; }

        /// <summary>
        /// Gets or sets wafer1左侧Mark光栅坐标 补偿后
        /// </summary>
        public Point BaseWaferLeftMarkRulerAfterOffset { get; set; }

        /// <summary>
        /// Gets or sets wafer1右侧Mark像素坐标 补偿后
        /// </summary>
        public Point BaseWaferRightMarkPixelAfterOffset { get; set; }

        /// <summary>
        /// Gets or sets wafer2左侧Mark像素坐标
        /// </summary>
        public Point CurWaferLeftMarkPixel { get; set; }

        /// <summary>
        /// Gets or sets wafer2左侧Mark光栅坐标
        /// </summary>
        public Point CurWaferLeftMarkRuler { get; set; }

        /// <summary>
        /// Gets or sets wafer2右侧Mark像素坐标
        /// </summary>
        public Point CurWaferRightMarkPixel { get; set; }

        /// <summary>
        /// Gets or sets wafer2右侧Mark光栅坐标
        /// </summary>
        public Point CurWaferRightMarkRuler { get; set; }

        /// <summary>
        /// 深拷贝
        /// </summary>
        /// <returns>深拷贝对象</returns>
        public AlignParam Clone()
        {
            AlignParam alignParam = new AlignParam()
            {
                LeftCamRuler = this.LeftCamRuler,
                RightCamRuler = this.RightCamRuler,
                BaseWaferLeftMarkPixel = this.BaseWaferLeftMarkPixel,
                BaseWaferLeftMarkRuler = this.BaseWaferLeftMarkRuler,
                BaseWaferRightMarkPixel = this.BaseWaferRightMarkPixel,
                BaseWaferRightMarkRuler = this.BaseWaferRightMarkRuler,
                BaseWaferRightMarkRulerAfterOffset = this.BaseWaferRightMarkRulerAfterOffset,
                BaseWaferLeftMarkPixelAfterOffset = this.BaseWaferLeftMarkPixelAfterOffset,
                BaseWaferLeftMarkRulerAfterOffset = this.BaseWaferLeftMarkRulerAfterOffset,
                BaseWaferRightMarkPixelAfterOffset = this.BaseWaferRightMarkPixelAfterOffset,
                CurWaferLeftMarkPixel = this.CurWaferLeftMarkPixel,
                CurWaferLeftMarkRuler = this.CurWaferLeftMarkRuler,
                CurWaferRightMarkPixel = this.CurWaferRightMarkPixel,
                CurWaferRightMarkRuler = this.CurWaferRightMarkRuler,
            };
            return alignParam;
        }
    }
}
