using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStruct;

namespace UltrapreciseBonding.FusionCollections.AVM
{
    /// <summary>
    /// 计算AVM反馈给SVA的偏移量
    /// </summary>
    public static class AvmFeedBack
    {
        /// <summary>
        /// 计算窗口内批次的偏移量的权重系数(用于ui显示)
        /// </summary>
        /// <param name="windowSize">窗口大小</param>
        /// <param name="wRate">系数变化率</param>
        /// <param name="confidenceValues">输出权重系数</param>
        /// <returns>AVM_FEEDBACK_WINSIZE_ERROR：窗口数值错误/OK：计算成功</returns>
        public static Errortype GenConfidenceValues(int windowSize, double wRate, out List<double> confidenceValues)
        {
            confidenceValues = new List<double>();
            if (windowSize < 2)
            {
                return Errortype.AVM_FEEDBACK_WINSIZE_ERROR;
            }

            for (int id = 1; id < windowSize; id++)
            {
                double pow = Math.Pow((double)(windowSize - id) / (double)windowSize, 1.8 / wRate);
                confidenceValues.Add(1.0 - pow);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 根据偏移量计算调整倍率数值(用于ui显示)
        /// </summary>
        /// <param name="amp">调整的幅值</param>
        /// <param name="pos">启用调整的偏移大小</param>
        /// <param name="width">调整的区间宽度</param>
        /// <param name="influenceValues">输出200nm区间内的调整倍率数值样例</param>
        /// <returns>AVM_FEEDBACK_ERFPARAM_ERROR：输入参数错误/OK：计算成功</returns>
        public static Errortype GenInfluenceValues(double amp, double pos, double width, out List<double> influenceValues)
        {
            influenceValues = new List<double>();
            if (amp < 0 || width < 0)
            {
                return Errortype.AVM_FEEDBACK_ERFPARAM_ERROR;
            }

            for (double x = 0; x <= 0.2; x += 0.001)
            {
                double power = (double)(x - pos) / (double)width;
                double pow = Math.Pow(2.0, -power);
                influenceValues.Add(amp / (1 + pow));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算偏移补偿数值
        /// </summary>
        /// <param name="windowSize">窗口大小</param>
        /// <param name="wRate">权重变化率</param>
        /// <param name="amp">调整倍率的幅值</param>
        /// <param name="pos">调整倍率激活的偏移大小</param>
        /// <param name="width">调整倍率的区间宽度</param>
        /// <param name="overlayList">输入窗口内的测量数据</param>
        /// <param name="feedBackOverlay">输出偏移调整量</param>
        /// <param name="waferSize">wafer 尺寸</param>
        /// <returns>AVM_FEEDBACK_DATA_NUM_ERROR：输入测测量数据为空/OK：计算成功</returns>
        public static Errortype CalcFeedBack(int windowSize, double wRate, double amp, double pos, double width, List<AVMOverlayModelParam> overlayList,
            out AVMOverlayModelParam feedBackOverlay, int waferSize = 8)
        {
            feedBackOverlay = new AVMOverlayModelParam();
            if (overlayList == null)
            {
                return Errortype.AVM_FEEDBACK_DATA_NUM_ERROR;
            }

            if (overlayList.Count != windowSize)
            {
                return Errortype.AVM_FEEDBACK_DATA_NUM_ERROR;
            }

            AVMOverlayModelParam sum = new AVMOverlayModelParam();
            double weightSum = 0.0;

            for (int index = 0; index < windowSize; index++)
            {
                double confidenceWeight = 1 - Math.Pow((double)(windowSize - index) / (double)windowSize, 1.8 / wRate);
                sum.TransX += confidenceWeight * overlayList[index].TransX;
                sum.TransY += confidenceWeight * overlayList[index].TransY;
                sum.RotationUrad += confidenceWeight * overlayList[index].RotationUrad;
                sum.RunOut += confidenceWeight * overlayList[index].RunOut;
                weightSum += confidenceWeight;
            }

            if (windowSize == 1)
            {
                weightSum = 1.0;
                sum.TransX += overlayList[0].TransX;
                sum.TransY += overlayList[0].TransY;
                sum.RotationUrad += overlayList[0].RotationUrad;
                sum.RunOut += overlayList[0].RunOut;
            }

            sum.TransX = sum.TransX / weightSum;
            sum.TransY = sum.TransY / weightSum;
            sum.RotationUrad = sum.RotationUrad / weightSum;
            sum.RunOut = sum.RunOut / weightSum;

            // multip influence rate
            double displacement = Math.Sqrt(Math.Pow(sum.TransX, 2) + Math.Pow(sum.TransY, 2));
            double power = (displacement - pos) / width;
            double influenceRate = amp / (1.0 + Math.Pow(2.0, -power));
            sum.TransX *= influenceRate;
            sum.TransY *= influenceRate;

            double waferR = waferSize * 25.4 / 2;
            double displacementT = waferR * Math.Abs(sum.RotationUrad) / 1e3;
            double powerT = (displacementT - pos) / width;
            double influenceRateT = amp / (1.0 + Math.Pow(2.0, -powerT));
            sum.RotationUrad *= influenceRateT;

            double displacementR = waferR * Math.Abs(sum.RunOut) / 1e3;
            double powerR = (displacementR - pos) / width;
            double influenceRateR = amp / (1.0 + Math.Pow(2.0, -powerR));
            sum.RunOut *= influenceRateR;

            feedBackOverlay = sum;
            return Errortype.OK;
        }
    }
}
