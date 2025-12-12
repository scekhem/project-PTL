using System;
using System.Collections.Generic;
using UltrapreciseBonding.Calib;
using DataStruct;
using HalconDotNet;
using Microsoft.SqlServer.Server;
using System.Runtime.Remoting.Messaging;
using System.IO;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using System.Linq;

namespace UltrapreciseBonding.AutoFocus
{
    /// <summary>
    /// 自动对焦评分策略
    /// </summary>
    public enum FocusType
    {
        AbsDifference, // 绝对对比度评分
        AutoCorrelation, // 相对对比度评分
        BandPass, // 带通评分
        GradientEnergy, // 梯度能量评分
        BrennerDeviation, // Brenner评分
        GradientDeviation, // 梯度方差评分
    }

    /// <summary>
    /// 相机对焦类
    /// </summary>
    public static class CameraFocus
    {
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取焦点轴位置
        /// </summary>
        /// <param name="axisPoses">图像集对应的轴位置</param>
        /// <param name="imageList">图像集</param>
        /// <param name="focusZPos">焦点Z坐标</param>
        /// <param name="focusScore">焦点分数</param>
        /// <param name="focusType">对焦评分类型</param>
        /// <param name="roiArea">ROI区域</param>
        /// <param name="gaussSigma">高斯因子</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetAxisFocusPose(
            double[] axisPoses,
            Camera[] imageList,
            ref double focusZPos,
            out double[] focusScore,
            FocusType focusType = FocusType.BrennerDeviation,
            Rectangle1 roiArea = null,
            double gaussSigma = 1.0)
        {
            lock (_lock)
            {
                CameraFocusBase focusBase = new CameraFocusBase();
                focusScore = new double[1];
                var ret = focusBase.SetFocusCalibrateParams(axisPoses, roiArea, gaussSigma);
                if (ret != Errortype.OK)
                {
                    SaveLog(ret.ToString(), focusScore, axisPoses, 0);
                    return ret;
                }

                ret = focusBase.CalcAxisFocusPoint(imageList, out focusZPos, focusType);

                focusScore = focusBase.ImageScores;

                SaveLog(ret.ToString(), focusScore, axisPoses, focusZPos);
                if (ret != Errortype.OK)
                {
                    SaveImageError(imageList);
                }
                else
                {
                    SaveImage(imageList);
                }

                focusBase.Release();
                return ret;
            }
        }

        /// <summary>
        /// 获取焦点轴位置
        /// </summary>
        /// <param name="axisPoses">图像集对应的轴位置</param>
        /// <param name="imageList">图像集</param>
        /// <param name="focusZPos">焦点Z坐标</param>
        /// <param name="templateNames">模板名称</param>
        /// <param name="focusScore">焦点分数</param>
        /// <param name="focusType">对焦评分类型</param>
        /// <param name="roiArea">ROI区域</param>
        /// <param name="gaussSigma">高斯因子</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetAxisFocusPoseByTemplate(
             double[] axisPoses,
             Camera[] imageList,
             ref double focusZPos,
             List<string> templateNames,
             out double[] focusScore,
             FocusType focusType = FocusType.BrennerDeviation,
             Rectangle1 roiArea = null,
             double gaussSigma = 1.0)
        {
            lock (_lock)
            {
                CameraFocusBase focusBase = new CameraFocusBase();
                focusScore = new double[1];
                var ret = focusBase.SetFocusCalibrateParams(axisPoses, roiArea, gaussSigma);
                if (ret != Errortype.OK)
                {
                    SaveLog(ret.ToString(), focusScore, axisPoses, 0);
                    return ret;
                }

                ret = focusBase.CalcAxisFocusPoint(imageList, out focusZPos, focusType);

                focusScore = focusBase.ImageScores;

                double min = double.MaxValue;
                int indexSelect = 0;
                for (int i = 0; i < axisPoses.Length; i++)
                {
                    if (min > Math.Abs(axisPoses[i] - focusZPos))
                    {
                        min = Math.Abs(axisPoses[i] - focusZPos);
                        indexSelect = i;
                    }
                }

                int width = 0;
                int height = 0;
                Point center = new Point();
                Errortype retTemplate = Errortype.OK;
                if (templateNames != null)
                {
                    for (int i = 0; i < templateNames.Count; i++)
                    {
                        retTemplate = MarkLocation.MarkAutoCenterLocationManager.GetMarkCenter(templateNames[i], imageList[indexSelect], null, out double[] rows, out double[] cols, out _, out _, out _);

                        if (retTemplate == Errortype.OK && rows.Length == 1)
                        {
                            MarkLocation.MarkAutoCenterLocationManager.GetTemplateShowImg(templateNames[i], out Camera imgT);
                            width = imgT.Width;
                            height = imgT.Height;
                            center = new Point(cols[0], rows[0]);
                            imgT.Dispose();
                            break;
                        }
                    }
                }

                if (width != 0 && height != 0)
                {
                    Point st = new Point(center.X - width / 2, center.Y - height / 2);
                    Point ed = new Point(center.X + width / 2, center.Y + height / 2);
                    st.X = Math.Max(0, st.X);
                    st.Y = Math.Max(0, st.Y); //防止rect区域小于0 
                    Rectangle1 rect = new Rectangle1(st.X, st.Y, ed.X, ed.Y);

                    ret = focusBase.SetFocusCalibrateParams(axisPoses, rect, gaussSigma);
                    ret = focusBase.CalcAxisFocusPoint(imageList, out focusZPos, focusType);
                }

                focusScore = focusBase.ImageScores;

                SaveLog(ret.ToString(), focusScore, axisPoses, focusZPos);
                if (ret != Errortype.OK)
                {
                    SaveImageError(imageList);
                }
                else
                {
                    SaveImage(imageList);
                }

                focusBase.Release();
                return ret;
            }
        }

        /// <summary>
        /// 保存log
        /// </summary>
        /// <param name="info">保存信息</param>
        /// <param name="imageScore">图像分数</param>
        /// <param name="zPos">z坐标</param>
        /// <param name="focusZPos">聚焦分数</param>
        public static void SaveLog(string info, double[] imageScore, double[] zPos, double focusZPos)
        {
            if (ComAlgo.SaveFlg("AutoFocusScore", out int days))
            {
                if (imageScore == null)
                {
                    return;
                }

                string path = @"D:\Alg\AutoFoucus\";
                string fileName = "AutoFocusScore.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string text = "";
                text += " imageScore ";

                foreach (var value in imageScore)
                {
                    text = text + sep + value.ToString("##.000");
                }

                text += " zPos ";

                foreach (var value in zPos)
                {
                    text = text + sep + value.ToString("##.000");
                }

                string txt = time + sep + info.ToString() + sep + text + sep + focusZPos + "\n";

                ComAlgo.LogText(txt, path, fileName, days);
            }

            return;
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="imageList">图像</param>
        public static void SaveImage(Camera[] imageList)
        {
            if (ComAlgo.SaveFlg("SaveAutoFocusImage", out int days))
            {
                if (imageList == null)
                {
                    return;
                }

                string time = ComAlgo.GetDateTime("yyyyMMddHHmmss");
                string path = @"D:\Alg\AutoFoucus\";

                for (int i = 0; i < imageList.Length; i++)
                {
                    ComAlgo.LogImages(imageList.ToList(), path, time, days);
                }
            }

            return;
        }

        /// <summary>
        /// 保存错误图像
        /// </summary>
        /// <param name="imageList">图像</param>
        public static void SaveImageError(Camera[] imageList)
        {
            if (ComAlgo.SaveFlg("SaveAutoFocusErrorImage", out int days))
            {
                if (imageList == null)
                {
                    return;
                }

                string time = ComAlgo.GetDateTime("yyyyMMddHHmmss");
                string path = @"D:\Alg\AutoFoucusError";
                for (int i = 0; i < imageList.Length; i++)
                {
                    ComAlgo.LogImages(imageList.ToList(), path, time, days);
                }
            }

            return;
        }
    }

    /// <summary>
    /// 分布聚焦类
    /// </summary>
    public static class CameraStepFocus
    {
        private static List<CameraStepFocusData> _cameraStepFocusDataList = new List<CameraStepFocusData>();

        /// <summary>
        /// 获取聚焦轴坐标
        /// </summary>
        /// <param name="cameraFocusName">聚焦对象名称，用于区分不同的相机</param>
        /// <param name="axisPoses">输入当前轴坐标</param>
        /// <param name="img">输入当前相机图像</param>
        /// <param name="focusZPos">返回最佳轴坐标位置</param>
        /// <param name="focusScore">返回聚焦分数</param>
        /// <param name="focusType">聚焦类型</param>
        /// <param name="roiArea">Roi区域</param>
        /// <param name="gaussSigma">高斯滤波参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetAxisFocusPose(string cameraFocusName, double axisPoses, Camera img, ref double focusZPos, out double[] focusScore,
            FocusType focusType = FocusType.BrennerDeviation, Rectangle1 roiArea = null, double gaussSigma = 1.0)
        {
            if (_cameraStepFocusDataList is null)
            {
                _cameraStepFocusDataList = new List<CameraStepFocusData>();
            }

            CameraStepFocusData cameraStepFocusData = _cameraStepFocusDataList.Find(e => e.Name == cameraFocusName);

            if (cameraStepFocusData is null)
            {
                cameraStepFocusData = new CameraStepFocusData(cameraFocusName);
                _cameraStepFocusDataList.Add(cameraStepFocusData);
            }

            cameraStepFocusData.AxisPoses.Add(axisPoses);
            cameraStepFocusData.ImageList.Add(img);

            return CameraFocus.GetAxisFocusPose(cameraStepFocusData.AxisPoses.ToArray(), cameraStepFocusData.ImageList.ToArray(), ref focusZPos, out focusScore, focusType, roiArea, gaussSigma);
        }

        /// <summary>
        /// 释放对应聚焦对象的数据内容
        /// </summary>
        /// <param name="cameraFocusName">聚焦对象名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release(string cameraFocusName)
        {
            if (_cameraStepFocusDataList is null)
            {
                _cameraStepFocusDataList = new List<CameraStepFocusData>();
            }

            CameraStepFocusData cameraStepFocusData = _cameraStepFocusDataList.Find(e => e.Name == cameraFocusName);

            if (cameraStepFocusData != null)
            {
                cameraStepFocusData.Release();
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 聚焦评分方法
    /// </summary>
    internal static class FocusScoreFunc
    {
        /// <summary>
        /// Brenner对比度评分
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="roiArea">识别区域</param>
        /// <param name="score">分数</param>
        /// <param name="diffDistance">距离</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool CalcBrennerScore(HObject image, Rectangle1 roiArea, out double score, int diffDistance = 2)
        {
            HObject imagePart01, imagePart10, ho_ImageSub, ho_ImageResult;
            HOperatorSet.GenEmptyObj(out imagePart01);
            HOperatorSet.GenEmptyObj(out imagePart10);
            HOperatorSet.GenEmptyObj(out ho_ImageSub);
            HOperatorSet.GenEmptyObj(out ho_ImageResult);

            HOperatorSet.CropPart(image, out imagePart01, roiArea.Start_Y, roiArea.Start_X, roiArea.End_X - roiArea.Start_X, roiArea.End_Y - roiArea.Start_Y - diffDistance);
            {
                HObject expTmpOutVar01;
                HOperatorSet.ConvertImageType(imagePart01, out expTmpOutVar01, "int2");
                imagePart01.Dispose();
                imagePart01 = expTmpOutVar01.Clone();
                expTmpOutVar01.Dispose();
            }

            HOperatorSet.CropPart(image, out imagePart10, roiArea.Start_Y + diffDistance, roiArea.Start_X, roiArea.End_X - roiArea.Start_X, roiArea.End_Y - roiArea.Start_Y - diffDistance);
            {
                HObject expTmpOutVar10;
                HOperatorSet.ConvertImageType(imagePart10, out expTmpOutVar10, "int2");
                imagePart10.Dispose();
                imagePart10 = expTmpOutVar10.Clone();
                expTmpOutVar10.Dispose();
            }

            ho_ImageSub.Dispose();
            HOperatorSet.SubImage(imagePart10, imagePart01, out ho_ImageSub, 1, 0);
            ho_ImageResult.Dispose();
            HOperatorSet.MultImage(ho_ImageSub, ho_ImageSub, out ho_ImageResult, 1, 0);
            HOperatorSet.Intensity(ho_ImageResult, ho_ImageResult, out HTuple hv_Mean, out HTuple hv_Deviation);
            score = hv_Mean.D;
            imagePart01.Dispose();
            imagePart10.Dispose();
            ho_ImageSub.Dispose();
            ho_ImageResult.Dispose();
            return true;
        }

        /// <summary>
        /// 绝对对比度评分
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="roiArea">识别区域</param>
        /// <param name="score">分数</param>
        /// <param name="diffDistance">距离</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool CalcAbsDifferenceScore(HObject image, Rectangle1 roiArea, out double score, int diffDistance = 2)
        {
            HObject imagePart01, imagePart10, ho_ImageSub, ho_ImageResult;
            HOperatorSet.GenEmptyObj(out imagePart01);
            HOperatorSet.GenEmptyObj(out imagePart10);
            HOperatorSet.GenEmptyObj(out ho_ImageSub);
            HOperatorSet.GenEmptyObj(out ho_ImageResult);
            HOperatorSet.CropPart(image, out imagePart01, roiArea.Start_Y, roiArea.Start_X, roiArea.End_X - roiArea.Start_X, roiArea.End_Y - roiArea.Start_Y - diffDistance);
            {
                HObject expTmpOutVar0;
                HOperatorSet.ConvertImageType(imagePart01, out expTmpOutVar0, "int2");
                imagePart01.Dispose();
                imagePart01 = expTmpOutVar0;
            }

            HOperatorSet.CropPart(image, out imagePart10, roiArea.Start_Y + diffDistance, roiArea.Start_X, roiArea.End_X - roiArea.Start_X, roiArea.End_Y - roiArea.Start_Y - diffDistance);
            {
                HObject expTmpOutVar0;
                HOperatorSet.ConvertImageType(imagePart10, out expTmpOutVar0, "int2");
                imagePart10.Dispose();
                imagePart10 = expTmpOutVar0;
            }

            ho_ImageSub.Dispose();
            HOperatorSet.SubImage(imagePart10, imagePart01, out ho_ImageSub, 1, 0);
            ho_ImageResult.Dispose();
            HOperatorSet.AbsImage(ho_ImageSub, out ho_ImageResult);
            HOperatorSet.Intensity(ho_ImageResult, ho_ImageResult, out HTuple hv_Mean, out HTuple hv_Deviation);
            score = hv_Deviation.D;
            imagePart01.Dispose();
            imagePart10.Dispose();
            ho_ImageSub.Dispose();
            ho_ImageResult.Dispose();
            return true;
        }

        /// <summary>
        /// 梯度方差评分
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="roiArea">识别区域</param>
        /// <param name="score">分数</param>
        /// <param name="windowSize">窗口尺寸</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool CalcGradientDeviationScore(HObject image, Rectangle1 roiArea, out double score, int windowSize = 3)
        {
            HObject imagePart, ho_ImageAmp;
            HOperatorSet.GenEmptyObj(out imagePart);
            HOperatorSet.GenEmptyObj(out ho_ImageAmp);
            HOperatorSet.CropPart(image, out imagePart, roiArea.Start_Y, roiArea.Start_X, roiArea.End_X - roiArea.Start_X, roiArea.End_Y - roiArea.Start_Y);

            HOperatorSet.SobelAmp(imagePart, out ho_ImageAmp, "sum_abs", windowSize);
            HOperatorSet.Intensity(ho_ImageAmp, ho_ImageAmp, out HTuple hv_Mean, out HTuple hv_Deviation);
            score = hv_Deviation.D;
            imagePart.Dispose();
            ho_ImageAmp.Dispose();
            return true;
        }

        /// <summary>
        /// 梯度能量评分
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="roiArea">识别区域</param>
        /// <param name="score">分数</param>
        /// <param name="diffDistance">距离</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool CalcGradientEnergyScore(HObject image, Rectangle1 roiArea, out double score, int diffDistance = 3)
        {
            HObject imagePart00, imagePart01, imagePart10, ho_ImageSub01, ho_ImageResult01, ho_ImageSub10, ho_ImageResult10, ho_ImageResultFinal;
            HOperatorSet.GenEmptyObj(out imagePart00);
            HOperatorSet.GenEmptyObj(out imagePart01);
            HOperatorSet.GenEmptyObj(out imagePart10);
            HOperatorSet.GenEmptyObj(out ho_ImageSub01);
            HOperatorSet.GenEmptyObj(out ho_ImageResult01);
            HOperatorSet.GenEmptyObj(out ho_ImageSub10);
            HOperatorSet.GenEmptyObj(out ho_ImageResult10);
            HOperatorSet.GenEmptyObj(out ho_ImageResultFinal);
            HOperatorSet.CropPart(image, out imagePart00, roiArea.Start_Y, roiArea.Start_X, roiArea.End_X - roiArea.Start_X - diffDistance, roiArea.End_Y - roiArea.Start_Y - diffDistance);
            {
                HObject expTmpOutVar0;
                HOperatorSet.ConvertImageType(imagePart00, out expTmpOutVar0, "int2");
                imagePart00.Dispose();
                imagePart00 = expTmpOutVar0;
            }

            HOperatorSet.CropPart(image, out imagePart01, roiArea.Start_Y, roiArea.Start_X + diffDistance, roiArea.End_X - roiArea.Start_X - diffDistance, roiArea.End_Y - roiArea.Start_Y - diffDistance);
            {
                HObject expTmpOutVar0;
                HOperatorSet.ConvertImageType(imagePart01, out expTmpOutVar0, "int2");
                imagePart01.Dispose();
                imagePart01 = expTmpOutVar0;
            }

            HOperatorSet.CropPart(image, out imagePart10, roiArea.Start_Y + diffDistance, roiArea.Start_X, roiArea.End_X - roiArea.Start_X - diffDistance, roiArea.End_Y - roiArea.Start_Y - diffDistance);
            {
                HObject expTmpOutVar0;
                HOperatorSet.ConvertImageType(imagePart10, out expTmpOutVar0, "int2");
                imagePart10.Dispose();
                imagePart10 = expTmpOutVar0;
            }

            ho_ImageSub01.Dispose();
            HOperatorSet.SubImage(imagePart01, imagePart00, out ho_ImageSub01, 1, 0);
            ho_ImageResult01.Dispose();
            HOperatorSet.MultImage(ho_ImageSub01, ho_ImageSub01, out ho_ImageResult01, 1, 0);

            ho_ImageSub10.Dispose();
            HOperatorSet.SubImage(imagePart10, imagePart00, out ho_ImageSub10, 1, 0);
            ho_ImageResult10.Dispose();
            HOperatorSet.MultImage(ho_ImageSub10, ho_ImageSub10, out ho_ImageResult10, 1, 0);

            HOperatorSet.AddImage(ho_ImageResult01, ho_ImageResult10, out ho_ImageResultFinal, 0.1, 0);
            HOperatorSet.Intensity(ho_ImageResultFinal, ho_ImageResultFinal, out HTuple hv_Mean, out HTuple hv_Deviation);
            score = hv_Mean.D;

            imagePart00.Dispose();
            imagePart01.Dispose();
            imagePart10.Dispose();
            ho_ImageSub01.Dispose();
            ho_ImageResult01.Dispose();
            ho_ImageSub10.Dispose();
            ho_ImageResult10.Dispose();
            ho_ImageResultFinal.Dispose();

            return true;
        }

        /// <summary>
        /// 相对对比度评分
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="roiArea">识别区域</param>
        /// <param name="score">分数</param>
        /// <param name="subSampling">尺度因子</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool CalcAutoCorrelationScore(HObject image, Rectangle1 roiArea, out double score, int subSampling = 3)
        {
            double scale = 1.0 / subSampling;
            HObject imagePart, ho_ImageZoom, ho_ImageFFT, ho_ImageFFTINV, ho_ImageCorrelation;
            HOperatorSet.GenEmptyObj(out imagePart);
            HOperatorSet.GenEmptyObj(out ho_ImageZoom);
            HOperatorSet.GenEmptyObj(out ho_ImageFFT);
            HOperatorSet.GenEmptyObj(out ho_ImageFFTINV);
            HOperatorSet.GenEmptyObj(out ho_ImageCorrelation);
            HOperatorSet.CropPart(image, out imagePart, roiArea.Start_Y, roiArea.Start_X, roiArea.End_X - roiArea.Start_X, roiArea.End_Y - roiArea.Start_Y);

            HOperatorSet.ZoomImageFactor(imagePart, out ho_ImageZoom, scale, scale, "constant");
            HOperatorSet.GetImageSize(ho_ImageZoom, out HTuple width, out HTuple height);
            HOperatorSet.RftGeneric(ho_ImageZoom, out ho_ImageFFT, "to_freq", "none", "complex", width);
            HOperatorSet.CorrelationFft(ho_ImageFFT, ho_ImageFFT, out ho_ImageCorrelation);
            HOperatorSet.RftGeneric(ho_ImageCorrelation, out ho_ImageFFTINV, "from_freq", "n", "real", width);
            int[] rowId = new int[4] { 0, 1, height - 1, height - 2 };
            int[] colId = new int[4] { 1, 0, width - 2, width - 1 };
            HOperatorSet.GetGrayval(ho_ImageFFTINV, rowId, colId, out HTuple selectValue);
            HOperatorSet.Intensity(ho_ImageZoom, ho_ImageZoom, out HTuple hv_Mean, out HTuple hv_Deviation);
            HTuple blurness = (selectValue / (width * height) - hv_Mean * hv_Mean) / (hv_Deviation * hv_Deviation);
            HOperatorSet.TupleMin(blurness, out HTuple minBlurness);
            HTuple sharpness = (1000.0 - minBlurness * 1000) / 40.0 * 3;
            score = sharpness.D;
            imagePart.Dispose();
            ho_ImageZoom.Dispose();
            ho_ImageFFT.Dispose();
            ho_ImageFFTINV.Dispose();
            ho_ImageCorrelation.Dispose();
            return true;
        }

        /// <summary>
        /// 带通评分
        /// </summary>
        /// <param name="image">图像</param>
        /// <param name="roiArea">识别区域</param>
        /// <param name="score">分数</param>
        /// <param name="lowerBandpass">低带通因子</param>
        /// <param name="higherBandpass">高带通因子</param>
        /// <param name="subSampling">尺度因子</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static bool CalcBandPassScore(HObject image, Rectangle1 roiArea, out double score, double lowerBandpass = 0.1, double higherBandpass = 0.4, int subSampling = 3)
        {
            double scale = 1.0 / subSampling;
            HObject imagePart, ho_ImageZoom, ho_ImageFFT, imageResult;
            HObject innerBandpass, outerBandpass, ringBandpass;

            HOperatorSet.GenEmptyObj(out innerBandpass);
            HOperatorSet.GenEmptyObj(out outerBandpass);
            HOperatorSet.GenEmptyObj(out ringBandpass);

            HOperatorSet.GenEmptyObj(out imagePart);
            HOperatorSet.GenEmptyObj(out ho_ImageZoom);
            HOperatorSet.GenEmptyObj(out ho_ImageFFT);
            HOperatorSet.GenEmptyObj(out imageResult);
            HOperatorSet.CropPart(image, out imagePart, roiArea.Start_Y, roiArea.Start_X, roiArea.End_X - roiArea.Start_X, roiArea.End_Y - roiArea.Start_Y);
            HOperatorSet.GetImageSize(imagePart, out HTuple width, out HTuple height);
            HOperatorSet.TupleMin2(width, height, out HTuple minSide);
            HOperatorSet.GenCircle(out innerBandpass, 0, 0, scale * lowerBandpass * minSide);
            HOperatorSet.GenCircle(out outerBandpass, 0, 0, scale * higherBandpass * minSide);
            HOperatorSet.Difference(outerBandpass, innerBandpass, out ringBandpass);
            HOperatorSet.Union2(outerBandpass, innerBandpass, out ringBandpass);
            HOperatorSet.ZoomImageFactor(imagePart, out ho_ImageZoom, scale, scale, "constant");
            HOperatorSet.RftGeneric(ho_ImageZoom, out ho_ImageFFT, "to_freq", "sqrt", "complex", scale * width);
            HOperatorSet.PowerReal(ho_ImageFFT, out imageResult);
            HOperatorSet.Intensity(ringBandpass, imageResult, out HTuple hv_Mean, out HTuple hv_Deviation);
            score = hv_Mean.D;
            imagePart.Dispose();
            ho_ImageZoom.Dispose();
            ho_ImageFFT.Dispose();
            imageResult.Dispose();
            innerBandpass.Dispose();
            outerBandpass.Dispose();
            ringBandpass.Dispose();
            return true;
        }
    }

    /// <summary>
    /// 相机对焦过程数据
    /// </summary>
    public class CameraStepFocusData
    {
        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or Sets the axispose
        /// </summary>
        public List<double> AxisPoses { get; set; }

        /// <summary>
        /// gets or Sets the imageList
        /// </summary>
        public List<Camera> ImageList { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype Init()
        {
            Name = string.Empty;
            AxisPoses = new List<double>();
            ImageList = new List<Camera>();
            return Errortype.OK;
        }

        /// <summary>
        /// 有参构造
        /// </summary>
        /// <param name="name">名称</param>
        public CameraStepFocusData(string name)
        {
            Init();
            Name = name;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            return Init();
        }
    }

    /// <summary>
    /// 相机对焦基类
    /// </summary>
    internal class CameraFocusBase
    {
        private double _zStartPos; // 对焦起始点
        private double _zMotionStep; // 飞拍移动步长
        private double _gaussSigma = 1;
        private Rectangle1 _focusRoiArea = null; // 限定区域
        private HTuple _zPoses = new HTuple();
        private double[] _imageScores = null;

        /// <summary>
        /// Gets or Sets _zStartPos
        /// </summary>
        public double ZStartPos { get => _zStartPos; set => _zStartPos = value; }

        /// <summary>
        /// Gets or Sets _zMotionStep
        /// </summary>
        public double ZMotionStep { get => _zMotionStep; set => _zMotionStep = value; }

        /// <summary>
        /// Gets _gaussSigma
        /// </summary>
        public double GaussSigma { get => _gaussSigma; }

        /// <summary>
        /// Gets _imageScores
        /// </summary>
        public double[] ImageScores { get => _imageScores; }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CameraFocusBase()
        {
        }

        /// <summary>
        /// 设置自动对焦参数
        /// </summary>
        /// <param name="axisPoses">轴位置</param>
        /// <param name="roiArea">识别区域</param>
        /// <param name="gaussSigma">高斯因子</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetFocusCalibrateParams(double[] axisPoses, Rectangle1 roiArea, double gaussSigma = 1.0)
        {
            if ((axisPoses == null) || (axisPoses.Length < 2))
            {
                return Errortype.FOCUS_AXIS_POINTS_NULL;
            }

            _zStartPos = axisPoses[0];
            _zMotionStep = axisPoses[2] - axisPoses[1];
            if (Math.Abs(_zMotionStep) < 0.0001)
            {
                return Errortype.FOCUS_AXIS_POINTS_STEPERROR;
            }

            _zPoses = new HTuple();
            _gaussSigma = gaussSigma;
            for (int hv_index = 0; hv_index < axisPoses.Length; hv_index++)
            {
                using (HDevDisposeHelper dh = new HDevDisposeHelper())
                {
                    _zPoses = _zPoses.TupleConcat(axisPoses[hv_index]);
                }
            }

            _focusRoiArea = roiArea;
            return Errortype.OK;
        }

        /// <summary>
        /// 计算轴高度
        /// </summary>
        /// <param name="imageList">图像集</param>
        /// <param name="focusZPos">轴高度</param>
        /// <param name="focusType">对焦评分策略</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalcAxisFocusPoint(Camera[] imageList, out double focusZPos, FocusType focusType = FocusType.BrennerDeviation)
        {
            focusZPos = ZStartPos;
            if ((imageList == null) || (imageList.Length < 2))
            {
                return Errortype.FOCUS_IMAGES_ERROR;
            }

            // CropPart范围0-1024
            double maxLength = 1392;

            if (imageList[0] == null)
            {
                return Errortype.FOCUS_IMAGES_ERROR;
            }

            double shortSize = (imageList[0].Height < imageList[0].Width) ? imageList[0].Height : imageList[0].Width;   // 图像短边
            double maxCropSize = (maxLength < shortSize) ? maxLength : shortSize;                                       // 选取短边或最小长度

            // 最大方形区域计算梯度
            if (_focusRoiArea == null)
            {
                _focusRoiArea = new Rectangle1(
                    (imageList[0].Height - maxCropSize) / 2,
                    (imageList[0].Width - maxCropSize) / 2,
                    (imageList[0].Height + maxCropSize) / 2 - 1,
                    (imageList[0].Width + maxCropSize) / 2 - 1);
            }

            if (_zPoses.Length != imageList.Length)
            {
                return Errortype.FOCUS_AXIS_POINTS_ERROR;
            }

            // 执行焦点计算
            HTuple focus_zaxis_pos = new HTuple();
            HTuple focusScores = new HTuple();
            var resState = ExcuteFocus(imageList, focusType, out focus_zaxis_pos, out focusScores);
            if (resState != Errortype.FOCUS_IMAGES_ERROR)
            {
                focusZPos = focus_zaxis_pos.D;
                _imageScores = focusScores.DArr;
            }

            return resState;
        }

        /// <summary>
        /// 计算图像得分
        /// </summary>
        /// <param name="imageList">图像集</param>
        /// <param name="score">分数</param>
        /// <param name="roiArea">识别区域</param>
        /// <param name="focusType">评分策略</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalcImgSocre(Camera[] imageList, out double[] score, Rectangle1 roiArea, FocusType focusType)
        {
            score = new double[1];
            if (imageList == null)
            {
                return Errortype.FOCUS_IMAGES_ERROR;
            }

            // CropPart范围0-1024
            double maxLength = 1392;

            if (imageList[0] == null)
            {
                return Errortype.FOCUS_IMAGES_ERROR;
            }

            double shortSize = (imageList[0].Height < imageList[0].Width) ? imageList[0].Height : imageList[0].Width;   // 图像短边
            double maxCropSize = (maxLength < shortSize) ? maxLength : shortSize;                                       // 选取短边或最小长度

            // 最大方形区域计算梯度
            if (_focusRoiArea == null)
            {
                _focusRoiArea = new Rectangle1(
                    (imageList[0].Height - maxCropSize) / 2,
                    (imageList[0].Width - maxCropSize) / 2,
                    (imageList[0].Height + maxCropSize) / 2 - 1,
                    (imageList[0].Width + maxCropSize) / 2 - 1);
            }

            int imageNum = imageList.Length;
            HTuple scoreTuple = new HTuple();
            _focusRoiArea = roiArea;
            for (int index = 0; index < imageNum; index++)
            {
                bool ret = false;
                double focuScore = -1.0;
                if (imageList[index] != null)
                {
                    HObject img = imageList[index].GenHObject();
                    switch (focusType)
                    {
                        case FocusType.BrennerDeviation:
                            ret = FocusScoreFunc.CalcBrennerScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.AbsDifference:
                            ret = FocusScoreFunc.CalcAbsDifferenceScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.AutoCorrelation:
                            ret = FocusScoreFunc.CalcAutoCorrelationScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.BandPass:
                            ret = FocusScoreFunc.CalcBandPassScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.GradientDeviation:
                            ret = FocusScoreFunc.CalcGradientDeviationScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.GradientEnergy:
                            ret = FocusScoreFunc.CalcGradientEnergyScore(img, _focusRoiArea, out focuScore);
                            break;
                        default:
                            break;
                    }

                    img.Dispose();
                    scoreTuple = scoreTuple.TupleConcat(focuScore);
                }
            }

            score = scoreTuple.DArr;
            return Errortype.OK;
        }

        /// <summary>
        /// 执行算法自动对焦
        /// </summary>
        /// <param name="imageList">图像集</param>
        /// <param name="focusType">评分策略</param>
        /// <param name="focusPos">聚焦位置</param>
        /// <param name="focusScores">聚焦分数</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal Errortype ExcuteFocus(Camera[] imageList, FocusType focusType, out HTuple focusPos, out HTuple focusScores)
        {
            // 参数
            int imageNum = imageList.Length;
            HTuple hv_MaxPos, hv_Indices, palorValueTuple;
            HTuple hv_StartZpos = ZStartPos;
            HTuple maxPosIndex, secondMaxTuple, secondMaxValue;
            HTuple scoreTuple = new HTuple();
            HTuple hv_GradientFunction = null;
            HTuple hv_GradientSmoothedFunction = null;
            HTuple hv_GradientFirstDerivative = null;
            HTuple hv_GradientZeroCrossings = null;
            HTuple maxValue = new HTuple(0), maxZIndex = new HTuple(0), funValue = null;
            HTuple zPosIndex = null;
            HTuple gradientSmoothed = null;
            focusScores = new HTuple();

            // 图像数量不足
            if (imageNum < 1)
            {
                focusPos = hv_StartZpos;
                focusScores.TupleAdd(0);
                return Errortype.FOCUS_IMAGES_ERROR;
            }

            var validZPoses = new HTuple();
            for (int index = 0; index < imageNum; index++)
            {
                bool ret = false;
                double focuScore = -1.0;
                if (imageList[index] != null)
                {
                    HObject img = imageList[index].GenHObject();
                    switch (focusType)
                    {
                        case FocusType.BrennerDeviation:
                            ret = FocusScoreFunc.CalcBrennerScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.AbsDifference:
                            ret = FocusScoreFunc.CalcAbsDifferenceScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.AutoCorrelation:
                            ret = FocusScoreFunc.CalcAutoCorrelationScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.BandPass:
                            ret = FocusScoreFunc.CalcBandPassScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.GradientDeviation:
                            ret = FocusScoreFunc.CalcGradientDeviationScore(img, _focusRoiArea, out focuScore);
                            break;
                        case FocusType.GradientEnergy:
                            ret = FocusScoreFunc.CalcGradientEnergyScore(img, _focusRoiArea, out focuScore);
                            break;
                        default:
                            break;
                    }

                    img.Dispose();
                    scoreTuple = scoreTuple.TupleConcat(focuScore);
                    validZPoses = validZPoses.TupleConcat(_zPoses[index]);
                }
            }

            focusScores = scoreTuple;
            zPosIndex = HTuple.TupleGenSequence(0, imageNum - 1, 1);

            // 执行高斯平滑滤波
            HOperatorSet.CreateFunct1dArray(scoreTuple, out hv_GradientFunction);
            HOperatorSet.TupleLength(scoreTuple, out HTuple gradient_tupleLength);

            double gaussSigma = GaussSigma;
            if ((gradient_tupleLength.D - 2.0) / 7.8 < GaussSigma)
            {
                gaussSigma = (gradient_tupleLength.D - 2.0) / 7.8;
            }

            if (gaussSigma < 0.01)
            {
                gaussSigma = 0.01;
            }

            HOperatorSet.SmoothFunct1dGauss(hv_GradientFunction, gaussSigma, out hv_GradientSmoothedFunction);
            HOperatorSet.GetYValueFunct1d(hv_GradientSmoothedFunction, zPosIndex, "constant", out gradientSmoothed);

            // 保证最大值点在采图序列中段
            HOperatorSet.TupleMax(gradientSmoothed, out hv_MaxPos);
            HOperatorSet.TupleFind(gradientSmoothed, hv_MaxPos, out hv_Indices);
            if (hv_Indices.TupleLength() > 1)
            {
                if (hv_Indices.TupleLength() > 2 || (hv_Indices.TupleSelect(1) - hv_Indices.TupleSelect(0)) > 1)
                {
                    focusPos = validZPoses[(int)hv_Indices[0]];
                    return Errortype.FOCUS_IMAGES_ERROR;
                }
            }

            // 根据focus score max - min < 0.2 进行筛选
            HOperatorSet.TupleMax(focusScores, out HTuple scoreMax);
            HOperatorSet.TupleMin(focusScores, out HTuple scoreMin);
            if (scoreMax.D - scoreMin.D < 0.2)
            {
                focusPos = validZPoses[(int)hv_Indices[0]];
                return Errortype.FOCUS_SCORE_RANGE_MINERROR;
            }

            if (hv_Indices[0] < 1)
            {
                //若Z轴正向移动请降低起始点位置，若Z轴负向运动请增大起始点位置
                focusPos = validZPoses[hv_Indices];
                return Errortype.FOCUS_FRONT_DECREASE;
            }

            if (hv_Indices[hv_Indices.TupleLength() - 1] > (imageNum - 2))
            {
                //若Z轴正向移动请增大起始点位置，若Z轴负向运动请降低起始点位置
                focusPos = validZPoses[hv_Indices];
                return Errortype.FOCUS_FRONT_INCREASE;
            }

            // 计算最大值对应的位置
            HOperatorSet.DerivateFunct1d(hv_GradientSmoothedFunction, "first", out hv_GradientFirstDerivative);
            HOperatorSet.ZeroCrossingsFunct1d(hv_GradientFirstDerivative, out hv_GradientZeroCrossings);
            palorValueTuple = new HTuple(0);
            for (int index = 0; index < hv_GradientZeroCrossings.TupleLength(); index++)
            {
                HOperatorSet.GetYValueFunct1d(hv_GradientSmoothedFunction, hv_GradientZeroCrossings[index], "constant", out funValue);
                if (funValue > maxValue)
                {
                    maxValue = funValue;
                    maxZIndex = hv_GradientZeroCrossings[index];
                }

                palorValueTuple = palorValueTuple.TupleConcat(funValue);
            }

            // 检测最大与第二大极点
            HOperatorSet.TupleFind(palorValueTuple, maxValue, out maxPosIndex);
            HOperatorSet.TupleRemove(palorValueTuple, maxPosIndex, out secondMaxTuple);
            HOperatorSet.TupleMax(secondMaxTuple, out secondMaxValue);
            HOperatorSet.TupleFind(palorValueTuple, secondMaxValue, out HTuple secondMaxPosIndex);

            HTuple maxPosIndexSelected = maxPosIndex;

            // 图像质量判定
            if ((maxValue - secondMaxValue) < 0.2)
            {
                HOperatorSet.TupleMin(new HTuple(maxPosIndex, secondMaxPosIndex), out maxPosIndexSelected);
            }

            maxZIndex = hv_GradientZeroCrossings[maxPosIndexSelected - 1];

            // 插值
            double imageZpos = validZPoses[Math.Floor((double)maxZIndex)];
            focusPos = imageZpos + ((double)maxZIndex - Math.Floor((double)maxZIndex)) * _zMotionStep;
            return Errortype.OK;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _focusRoiArea = null;
            _zPoses = new HTuple();
            _imageScores = null;
            return Errortype.OK;
        }
    }
}