using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using DataStruct;
using HalconDotNet;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.FusionCollections.AVM;
using UltrapreciseBonding.FusionCollections.TSP;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.FusionCollections
{
    /// <summary>
    /// 滑片及AVM检测功能部分
    /// </summary>
    public static class FusionAlignVerify
    {
        private static ChuckStageCalib _stageChuckCali = new ChuckStageCalib();
        private static CalibPix _calibPix = new CalibPix();
        private static AVMCalibParam _calibParam = new AVMCalibParam();

        /// <summary>
        /// AVM检测阵列坐标生成（spot map）
        /// </summary>
        /// <param name="waferRadius">wafer半径</param>
        /// <param name="exclusive">外边距</param>
        /// <param name="intervalX">列间距</param>
        /// <param name="intervalY">行间距</param>
        /// <param name="shiftX">平移量X</param>
        /// <param name="shiftY">平移量Y</param>
        /// <param name="outPutPositions">输出阵列点集</param>
        /// <param name="refill">是否填充平移空白部分</param>
        /// <param name="outPutOrder">输出阵列顺序</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GenMeasureArray(double waferRadius, double exclusive, double intervalX, double intervalY, double shiftX, double shiftY, out List<Point> outPutPositions,
            bool refill = true, AVMMeasureArrayOrder outPutOrder = AVMMeasureArrayOrder.LeftUP)
        {
            outPutPositions = new List<Point>();
            Point intervalXY = new Point(intervalX, intervalY);
            Point shiftXY = new Point(shiftX, shiftY);
            return AvmMetrology.GenMeasureArray(waferRadius, exclusive, intervalXY, shiftXY, out outPutPositions, refill, outPutOrder);
        }

        /// <summary>
        /// 排序方法
        /// </summary>
        public enum SortMethod
        {
            ShortPath,
            ShortPathUseFarPairPoints,
            UseFarPairPoints,
        }

        /// <summary>
        /// 生成多点检测路径
        /// </summary>
        /// <param name="pointsX">输入点X(chuck坐标)</param>
        /// <param name="pointsY">输入点Y(chuck坐标)</param>
        /// <param name="order">输出排序后的序号</param>
        /// <param name="sortMethod">排序方法</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SortChuckPoints(List<double> pointsX, List<double> pointsY, out List<int> order, SortMethod sortMethod = SortMethod.ShortPathUseFarPairPoints)
        {
            List<Point> pointsIn = new List<Point>();
            order = new List<int>();
            if (pointsX == null || pointsY == null)
            {
                return Errortype.AVM_INPUT_POINT_NULL;
            }

            if (pointsX.Count != pointsY.Count)
            {
                return Errortype.AVM_INPUT_POINT_NUM_ERROR;
            }

            for (int index = 0; index < pointsX.Count; index++)
            {
                pointsIn.Add(new Point(pointsX[index], pointsY[index]));
            }

            switch (sortMethod)
            {
                case SortMethod.ShortPath:
                    return TspTool.CalcTSP(pointsIn, out order);
                case SortMethod.ShortPathUseFarPairPoints:
                    return TspTool.CalcTSPUseFarPairPoints(pointsIn, out order);
                case SortMethod.UseFarPairPoints:
                    {
                        double maxDist = 0;
                        int maxDistIndex = 0;
                        for (int i = 1; i < pointsIn.Count; i++)
                        {
                            double dist = ComAlgo.Dist(pointsIn[0], pointsIn[i]);
                            if (dist > maxDist)
                            {
                                maxDist = dist;
                                maxDistIndex = i;
                            }
                        }

                        order.Add(0);
                        order.Add(maxDistIndex);
                        for (int i = 1; i < pointsIn.Count; i++)
                        {
                            if (i == maxDistIndex)
                            {
                                continue;
                            }

                            order.Add(i);
                        }
                    }

                    break;
                default:
                    break;
            }

            return Errortype.OK;
        }

        #region 像素比及chuck坐标转换

        /// <summary>
        ///  初始化
        /// </summary>
        /// <param name="calibParam">标定项</param>
        /// <returns>ok:成功</returns>
        public static Errortype Init(AVMCalibParam calibParam)
        {
            _calibParam = calibParam;
            Errortype ret = Errortype.OK;
            if (!(calibParam.ChuckStageName is null))
            {
                List<string> chuckNames = new List<string> { calibParam.ChuckStageName };
                ret = _stageChuckCali.Init(chuckNames);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }

            if (!(calibParam.AVMCamName is null))
            {
                List<string> cameraNames = new List<string> { calibParam.AVMCamName };
                _calibPix.Init(cameraNames);
                if (!(calibParam.AVMCamCenter is null))
                {
                    ret = _calibPix.SetPixCenter(calibParam.AVMCamName, calibParam.AVMCamCenter);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    ret = SetCalibPixAndAngle();
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            _stageChuckCali.Release();
            _calibPix.Release();
            return Errortype.OK;
        }

        /// <summary>
        /// SetCalibPixAndAngle
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype SetCalibPixAndAngle()
        {
            int dirX = 1;
            int dirY = 1;
            switch (_calibParam.AVMCamDirection)
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

            return _calibPix.SetScaleAngle(_calibParam.AVMCamName, _calibParam.AVMCamScale * dirX, _calibParam.AVMCamScale * dirY, _calibParam.AVMCamAngleDeg);
        }

        /// <summary>
        /// 配置像素比标定
        /// </summary>
        /// <param name="itemName">相机名称</param>
        /// <param name="scale">像素比</param>
        /// <returns>ok：设定成功</returns>
        public static Errortype SetCalibPix(string itemName, double scale)
        {
            _calibParam.AVMCamScale = scale;

            return SetCalibPixAndAngle();
        }

        /// <summary>
        /// 标定像素角度
        /// </summary>
        /// <param name="itemName">相机名称</param>
        /// <param name="pix1">起始mark像素点</param>
        /// <param name="pix2">mark X正方向平移后像素点</param>
        /// <param name="pixDeg">得到的像素角度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibPixAngle(string itemName, Point pix1, Point pix2, out double pixDeg)
        {
            _calibPix.CalibPixAngle(itemName, pix1, pix2, out pixDeg);
            _calibParam.AVMCamAngleDeg = pixDeg;
            return SetCalibPixAndAngle();
        }

        /// <summary>
        /// 像素点转轴坐标
        /// </summary>
        /// <param name="itemName">标定时绑定的相机名称</param>
        /// <param name="currentRuler">当前轴的光栅值</param>
        /// <param name="targetMarkCenterPix">目标像素点(mark中心像素)</param>
        /// <param name="targetMarkCenterRuler">输出轴目标点位</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRulerByPix(string itemName, Point currentRuler, Point targetMarkCenterPix, out Point targetMarkCenterRuler)
        {
            var ret = _calibPix.GetRulerByPix(itemName, targetMarkCenterPix, currentRuler, out targetMarkCenterRuler);
            return ret;
        }

        /// <summary>
        /// 配置chuck标定
        /// </summary>
        /// <param name="chuckName">chuck名称</param>
        /// <param name="rulerPoints">chuck上两个对称的mark所在的轴坐标位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibChuck(string chuckName, List<Point> rulerPoints)
        {
            return _stageChuckCali.CalibChuck(chuckName, rulerPoints);
        }

        /// <summary>
        /// 获取输入轴位置的chuck坐标
        /// </summary>
        /// <param name="chuckName">标定时绑定的chuck名称</param>
        /// <param name="rulerPoint">输入轴位置</param>
        /// <param name="chuckPoint">输出对应位置的chuck坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetChuckByRuler(string chuckName, Point rulerPoint, out Point chuckPoint)
        {
            return _stageChuckCali.GetChuckByRuler(chuckName, rulerPoint, out chuckPoint);
        }

        /// <summary>
        /// 获取chuck坐标对应轴光栅坐标
        /// </summary>
        /// <param name="chuckName">标定时绑定的chuck名称</param>
        /// <param name="chuckPoint">输入chuck坐标</param>
        /// <param name="rulerPoint">输出对应的轴坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRulerByChuck(string chuckName, Point chuckPoint, out Point rulerPoint)
        {
            return _stageChuckCali.GetRulerByChuck(chuckName, chuckPoint, out rulerPoint);
        }

        #endregion

        /// <summary>
        /// 生成相机绕圈search的轴点位
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="currentAxis">开始绕圈的轴绝对位置</param>
        /// <param name="searchList">输出search点位</param>
        /// <param name="searchRound">搜索圈数，默认1圈</param>
        /// <param name="stepRate">搜索步长倍率，默认一个视野步长</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GenSearchPoint(string cameraName, Point currentAxis, out List<Point> searchList, int searchRound = 1, double stepRate = 1.0)
        {
            searchList = new List<Point>();
            _calibPix.GetRulerByPix(cameraName, new Point(0, 0), new Point(0, 0), out Point cornerDist);
            Point imageDist = cornerDist * -2;
            return AvmMetrology.GenSearchArray(searchRound, stepRate, currentAxis, imageDist, out searchList);
        }

        /// <summary>
        /// 相机绕圈搜索时检查图像中是否有图案，有图案移到中心，无图案跳过
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="imageToCheck">当前图像</param>
        /// <param name="currentAxis">当前轴</param>
        /// <param name="searchAxis">输出移动目标轴位置</param>
        /// <param name="needToSearch">是否有图案</param>
        /// <param name="dynamicRate">动态比例</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CheckSearchImage(string cameraName, Camera imageToCheck, Point currentAxis, out Point searchAxis,
            out bool needToSearch, double dynamicRate = 0.05)
        {
            needToSearch = false;
            searchAxis = currentAxis;
            var ret = AvmMetrology.CheckSearchImag(imageToCheck, out Point searchPix, out needToSearch, dynamicRate);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _calibPix.GetRulerByPix(cameraName, searchPix, currentAxis, out searchAxis);
            return Errortype.OK;
        }

        /// <summary>
        /// 相机绕圈搜索时检查图像中是否有图案，有图案移到中心，无图案跳过
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="imageToCheck">当前图像</param>
        /// <param name="currentAxis">当前轴</param>
        /// <param name="templateNames">模板名称</param>
        /// <param name="searchAxis">输出移动目标轴位置</param>
        /// <param name="needToSearch">是否有图案</param>
        /// <param name="scoreRate">分数比例</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CheckSearchImageByMark(string cameraName, Camera imageToCheck, Point currentAxis, List<string> templateNames,
            out Point searchAxis, out bool needToSearch, double scoreRate = 0.8)
        {
            searchAxis = currentAxis.Clone();
            needToSearch = false;
            Errortype ret = Errortype.OK;
            double[] rows, cols, angles, scores;
            Point markCenterPix = new Point();
            bool searchMark = false;
            for (int i = 0; i < templateNames.Count; i++)
            {
                ret = MarkLocation.MarkAutoCenterLocationManager.GetMarkCenterHalfScore(templateNames[i], imageToCheck, null, out rows, out cols, out angles, out scores, true, scoreRate);
                if (ret != Errortype.OK || rows.Length != 1)
                {
                    continue;
                }

                markCenterPix = new Point(cols[0], rows[0]);
                searchMark = true;
                needToSearch = true;
                break;
            }

            if (searchMark == true)
            {
                _calibPix.GetRulerByPix(cameraName, markCenterPix, currentAxis, out searchAxis);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 使用前三点搜索实际坐标
        /// (上料偏移较大时，使用搜索到的前三个点计算剩下的点实际位置)
        /// </summary>
        /// <param name="pointsInMap">示教设定的检测点坐标</param>
        /// <param name="pointsFindOut">实际找到的坐标</param>
        /// <param name="pointsToLocate">需要重新定位的所有点</param>
        /// <param name="pointsPredict">所有点的实际坐标预估值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcMapPointsInReal(List<Point> pointsInMap, List<Point> pointsFindOut, List<Point> pointsToLocate, out List<Point> pointsPredict)
        {
            pointsPredict = new List<Point>();
            return AvmMetrology.CalcMapPoints(pointsInMap, pointsFindOut, pointsToLocate, out pointsPredict);
        }

        /// <summary>
        /// 校正已有点的位置
        /// </summary>
        /// <param name="pointsBefore">校正前的参考点坐标</param>
        /// <param name="pointsTrained">校正后的参考点坐标</param>
        /// <param name="pointsAll">需要更新的坐标</param>
        /// <param name="pointNew">输出更新后的所有坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype UpdateSpotMap(List<Point> pointsBefore, List<Point> pointsTrained,
            List<Point> pointsAll, out List<Point> pointNew)
        {
            pointNew = new List<Point>();
            return AvmMetrology.CalcMapPoints(pointsBefore, pointsTrained, pointsAll, out pointNew);
        }

        /// <summary>
        /// 整体键合质量评估及偏移量输出
        /// </summary>
        /// <param name="pointsFindOut">输入点(chuck坐标)</param>
        /// <param name="markOffsets">每个mark偏量</param>
        /// <param name="resModelParams">输出模型参数(偏移um、旋转urad、放大量ppm)</param>
        /// <param name="residuals">输入点带入模型后的残差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcFusionOverlayModel(List<Point> pointsFindOut, List<Point> markOffsets, out AVMOverlayModelParam resModelParams, out List<Point> residuals)
        {
            return AvmMetrology.CalcOverlayModel(pointsFindOut, markOffsets, out resModelParams, out residuals);
        }

        /// <summary>
        /// 键合检测数据统计
        /// </summary>
        /// <param name="values">输入数值</param>
        /// <param name="analysisValues">输出数值的均值极差、3σ等信息</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcFusionAnalysis(List<double> values, out DataStatisticParam analysisValues)
        {
            return AvmMetrology.CalcCommonSummary(values, out analysisValues);
        }

        /// <summary>
        /// 获取AVM补偿的统计权重值
        /// </summary>
        /// <param name="windowSize">监控窗口大小</param>
        /// <param name="wRate">权重变化率</param>
        /// <param name="confidenceValueExample">输出样例权重值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetAvmFeedBackConfidenceRate(int windowSize, double wRate, out List<double> confidenceValueExample)
        {
            return AvmFeedBack.GenConfidenceValues(windowSize, wRate, out confidenceValueExample);
        }

        /// <summary>
        /// 获取AVM补偿的倍率数值
        /// </summary>
        /// <param name="erfAmp">补偿倍率的幅值</param>
        /// <param name="erfPos">补偿倍率的变化区域</param>
        /// <param name="erfWidth">补偿倍率的变化区宽度</param>
        /// <param name="influenceValueExample">输出样例倍率数据(0-200nm范围)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetAvmFeedBackInfluenceRate(double erfAmp, double erfPos, double erfWidth, out List<double> influenceValueExample)
        {
            return AvmFeedBack.GenInfluenceValues(erfAmp, erfPos, erfWidth, out influenceValueExample);
        }

        /// <summary>
        /// 计算AVM补偿量
        /// </summary>
        /// <param name="windowSize">监控窗口大小</param>
        /// <param name="wRate">权重变化率</param>
        /// <param name="erfAmp">补偿倍率的幅值</param>
        /// <param name="erfPos">补偿倍率的变化区域</param>
        /// <param name="erfWidth">补偿倍率的变化区宽度</param>
        /// <param name="overlayList">窗口内的统计数据</param>
        /// <param name="feedBackOverlay">输出补偿量</param>
        /// <param name="waferSize">wafer尺寸</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcAvmFeedBack(int windowSize, double wRate, double erfAmp, double erfPos, double erfWidth, List<AVMOverlayModelParam> overlayList,
            out AVMOverlayModelParam feedBackOverlay, int waferSize = 8)
        {
            return AvmFeedBack.CalcFeedBack(windowSize, wRate, erfAmp, erfPos, erfWidth, overlayList, out feedBackOverlay, waferSize);
        }
    }

    /// <summary>
    /// 图像预处理
    /// </summary>
    public static class ImagePreprocess
    {
        /// <summary>
        /// 图像增强，使用幂函数
        /// </summary>
        /// <param name="imgIn">输入图像</param>
        /// <param name="imgOut">输出图像</param>
        /// <param name="gaussFilter">高斯平滑参数</param>
        /// <param name="multNum">幂函数指数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype ImageEmphasize(Camera imgIn, out Camera imgOut, int gaussFilter = 5, int multNum = 2)
        {
            imgOut = new Camera();
            if (imgIn is null || imgIn.Width < 1 || imgIn.Height < 1 || imgIn.Channel < 1)
            {
                return Errortype.IMAGE_PREPROCESS_INPUT_NULL;
            }

            if (gaussFilter < 1 || gaussFilter > 11 || gaussFilter % 2 == 0)
            {
                imgOut = imgIn.Clone();
                return Errortype.IMAGEEMPHASIZE_GAUSSSIZE_ERROR;
            }

            HObject hImg = imgIn.GenHObject();

            HOperatorSet.GenEmptyObj(out HObject imgGauss);
            if (gaussFilter == 1)
            {
                imgGauss = hImg.Clone();
            }
            else
            {
                HOperatorSet.GaussFilter(hImg, out imgGauss, gaussFilter);
            }

            HOperatorSet.PowImage(imgGauss, out HObject powImage, multNum);
            HOperatorSet.ScaleImageMax(powImage, out HObject imgScaleMax);
            imgOut = new Camera(imgScaleMax);

            hImg.Dispose();
            powImage.Dispose();
            imgScaleMax.Dispose();
            imgGauss.Dispose();
            return Errortype.OK;
        }
    }

    /// <summary>
    /// AVM标定参数类
    /// </summary>
    public class AVMCalibParam
    {
        /// <summary>
        /// Gets or sets 相机名称
        /// </summary>
        public string AVMCamName { get; set; }

        /// <summary>
        /// Gets or sets 相机中心坐标
        /// </summary>
        public Point AVMCamCenter { get; set; } = null;

        /// <summary>
        /// Gets or sets 相机像素比
        /// </summary>
        public double AVMCamScale { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets 相机像素夹角
        /// </summary>
        public double AVMCamAngleDeg { get; set; } = 0.0;

        /// <summary>
        /// Gets or sets 相机图像和轴方向
        /// </summary>
        public Dir AVMCamDirection { get; set; } = Dir.Dir_XPositive_YPositive;

        /// <summary>
        /// Gets or sets chuck名称
        /// </summary>
        public string ChuckStageName { get; set; }
    }
}