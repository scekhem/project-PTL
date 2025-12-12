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

namespace UltrapreciseBonding.PatternManager
{
    /// <summary>
    /// 训练模板管理
    /// </summary>
    public static class PatterManager
    {
        private static HObject _imgSource; //原始图像
        private static HObject _region; //原始图像的感兴趣区域
        private static HObject _imgPart; //原始图像中裁剪出来的感兴趣区域局部图像
        private static HObject _trainDontCareRegion; //取中心不感兴趣区域

        //private static HObject _detailsRegion; //辅助模板匹配区域
        private static HObject _detailsDontCareRegion; //辅助模板匹配不感兴趣区域

        private static List<List<LineSeg>> _lineSegs; //卡尺的线段

        private static Point _origin; //取中心的结果

        private static string _patternName; //mark name

        /// <summary>
        /// 对图像的局部区域进行亮度变化
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="region">局部区域</param>
        /// <param name="scale">变化比例，0-1为降低亮度  1-∞为增加亮度</param>
        /// <param name="imgOut">输出图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        private static Errortype ScaleImgPart(HObject img, HObject region, double scale, out HObject imgOut)
        {
            imgOut = img.Clone();
            if (img == null)
            {
                return Errortype.INPUT_NULL;
            }

            if (region == null)
            {
                return Errortype.OK;
            }

            if (!region.IsInitialized())
            {
                return Errortype.OK;
            }

            HOperatorSet.CountObj(region, out HTuple regionNum);
            if (regionNum == 0)
            {
                return Errortype.OK;
            }

            HOperatorSet.CountChannels(img, out HTuple channels);

            HOperatorSet.GetImageSize(img, out HTuple width, out HTuple height);
            HOperatorSet.GenImageConst(out HObject imgScale, "byte", width, height);
            HOperatorSet.ScaleImage(imgScale, out imgScale, 1, 2);
            HOperatorSet.PaintRegion(region, imgScale, out imgScale, scale / 0.5, "fill");
            if (channels == 3)
            {
                HOperatorSet.Compose3(imgScale, imgScale, imgScale, out imgScale);
            }

            HOperatorSet.MultImage(img, imgScale, out imgOut, 0.5, 0);

            imgScale.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 设置原始图像
        /// </summary>
        /// <param name="imgSource">原始图像数据</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetImgSource(Camera imgSource)
        {
            if (imgSource == null)
            {
                return Errortype.PATTERN_INPUT_NULL;
            }

            Release();
            if (_imgSource != null)
            {
                _imgSource.Dispose();
            }

            HObject img = imgSource.GenHObject();

            // 灰度化
            HOperatorSet.Rgb1ToGray(img, out _imgSource);
            img.Dispose();

            if (_imgPart != null)
            {
                _imgPart.Dispose();
            }

            _imgPart = _imgSource.Clone();

            return Errortype.OK;
        }

        /// <summary>
        /// 设置感兴趣区域
        /// </summary>
        /// <param name="rectangle">感兴趣区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetRegion(Rectangle1 rectangle)
        {
            if (_imgSource == null)
            {
                return Errortype.PATTERN_SOURCEIMG_NULL;
            }

            if (_region != null)
            {
                _region.Dispose();
            }

            if (rectangle == null)
            {
                _region = null;
                return Errortype.OK;
            }

            HOperatorSet.GenRectangle1(out _region, rectangle.Start_Y, rectangle.Start_X, rectangle.End_Y, rectangle.End_X);
            HOperatorSet.ReduceDomain(_imgSource, _region, out HObject regionHImg);
            _imgPart.Dispose();
            HOperatorSet.CropDomain(regionHImg, out _imgPart);

            regionHImg.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 获取感兴趣区域图像
        /// </summary>
        /// <param name="caliperParam">卡尺参数</param>
        /// <param name="regionImg">感兴趣区域的图像</param>
        /// <param name="minLength">线段最小长度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRegionImg(CaliperParams caliperParam, out Camera regionImg, int minLength = 10)
        {
            regionImg = null;
            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            Errortype ret = MarkLocation.Common.GetMarkEdgeLine(_imgPart, caliperParam, out HObject outImg, out List<List<LineSeg>> lineSegs, null, null, minLength);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _lineSegs = lineSegs;

            regionImg = new Camera(outImg);
            outImg.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 设置取中心感兴趣区域
        /// </summary>
        /// <param name="polygon">感兴趣区域的轮廓点，true为内部数据，false为外部区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetTrainCareRegion(Dictionary<List<Point>, bool> polygon)
        {
            if (polygon == null)
            {
                return Errortype.PATTERN_INPUT_NULL;
            }

            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            if (_trainDontCareRegion == null || !_trainDontCareRegion.IsInitialized())
            {
                HOperatorSet.GenEmptyObj(out _trainDontCareRegion);
            }

            Errortype ret;
            HOperatorSet.GetImageSize(_imgPart, out HTuple width, out HTuple height);
            foreach (var item in polygon)
            {
                ret = ComAlgo.GetPolygonRegion(item.Key, out HObject region, width, height, item.Value);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                HOperatorSet.Difference(_trainDontCareRegion, region, out _trainDontCareRegion);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 设置取中心不感兴趣区域
        /// </summary>
        /// <param name="polygon">不感兴趣区域的轮廓点，true为内部数据，false为外部区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetTrainDontCareRegion(Dictionary<List<Point>, bool> polygon)
        {
            if (polygon == null)
            {
                return Errortype.PATTERN_INPUT_NULL;
            }

            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            if (_trainDontCareRegion == null || !_trainDontCareRegion.IsInitialized())
            {
                HOperatorSet.GenEmptyObj(out _trainDontCareRegion);
            }

            HOperatorSet.GetImageSize(_imgPart, out HTuple width, out HTuple height);
            foreach (var item in polygon)
            {
                ComAlgo.GetPolygonRegion(item.Key, out HObject region, width, height, item.Value);
                HOperatorSet.Union2(_trainDontCareRegion, region, out _trainDontCareRegion);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 清除所有取中心不感兴趣区域
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype ClearTrainDontCareRegion()
        {
            if (_trainDontCareRegion == null || !_trainDontCareRegion.IsInitialized())
            {
                HOperatorSet.GenEmptyObj(out _trainDontCareRegion);
            }
            else
            {
                _trainDontCareRegion.Dispose();
                HOperatorSet.GenEmptyObj(out _trainDontCareRegion);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取不感兴趣区域设置后的图像，设置区域会低亮
        /// </summary>
        /// <param name="caliperParam">卡尺参数</param>
        /// <param name="edgeImg">输出图像</param>
        /// <param name="minLength">线段最小长度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetTrainDontCareImg(CaliperParams caliperParam, out Camera edgeImg, int minLength = 10)
        {
            edgeImg = null;

            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            Errortype ret = MarkLocation.Common.GetMarkEdgeLine(_imgPart, caliperParam, out HObject edgeHImg, out List<List<LineSeg>> lineSegs, null, _trainDontCareRegion, minLength);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _lineSegs = lineSegs;

            ret = ScaleImgPart(edgeHImg, _trainDontCareRegion, 0.5, out HObject imgOut);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            edgeImg = new Camera(imgOut);
            return Errortype.OK;
        }

        /// <summary>
        /// 获取中心
        /// </summary>
        /// <param name="origin">输出的中心</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetOrigin(out Point origin, CaliperParams caliperParams)
        {
            origin = new Point();
            if (caliperParams == null)
            {
                return Errortype.PATTERN_INPUT_NULL;
            }

            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            Errortype ret;

            ret = MarkLocation.Common.GetMarkCenter(_imgPart, _lineSegs, caliperParams, out origin, out List<double[]> straightnessErrorList, _trainDontCareRegion);

            HOperatorSet.GetImageSize(_imgPart, out HTuple imgWidth, out HTuple imgHeight);
            if (origin.X < 0 || origin.Y < 0 || origin.X > imgWidth || origin.Y > imgHeight)
            {
                origin = new Point() { X = 0, Y = 0 };
                return Errortype.PATTERN_ORIGIN_OUTOFRANGE;
            }

            return ret;
        }

        /// <summary>
        /// 设置辅助匹配区域
        /// </summary>
        /// <param name="polygon">辅助匹配区域的轮廓点，true为内部数据，false为外部区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetDetailRegion(Dictionary<List<Point>, bool> polygon)
        {
            if (polygon == null)
            {
                return Errortype.PATTERN_INPUT_NULL;
            }

            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            if (_detailsDontCareRegion is null || !_detailsDontCareRegion.IsInitialized())
            {
                HOperatorSet.GenEmptyObj(out _detailsDontCareRegion);
            }

            HOperatorSet.GetImageSize(_imgPart, out HTuple width, out HTuple height);
            foreach (var item in polygon)
            {
                ComAlgo.GetPolygonRegion(item.Key, out HObject region, width, height, item.Value);
                HOperatorSet.Difference(_detailsDontCareRegion, region, out _detailsDontCareRegion);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 设置辅助匹配不感兴趣区域
        /// </summary>
        /// <param name="polygon">辅助匹配不感兴趣区域的轮廓点，true为内部数据，false为外部区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SetDetailDontCareRegion(Dictionary<List<Point>, bool> polygon)
        {
            if (polygon == null)
            {
                return Errortype.PATTERN_INPUT_NULL;
            }

            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            if (_detailsDontCareRegion is null || !_detailsDontCareRegion.IsInitialized())
            {
                HOperatorSet.GenEmptyObj(out _detailsDontCareRegion);
            }

            HOperatorSet.GetImageSize(_imgPart, out HTuple width, out HTuple height);
            foreach (var item in polygon)
            {
                ComAlgo.GetPolygonRegion(item.Key, out HObject region, width, height, item.Value);
                HOperatorSet.Union2(_detailsDontCareRegion, region, out _detailsDontCareRegion);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 清除所有模板不感兴趣区域
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype ClearDetailDontCareRegion()
        {
            if (_detailsDontCareRegion == null || !_detailsDontCareRegion.IsInitialized())
            {
                HOperatorSet.GenEmptyObj(out _detailsDontCareRegion);
            }
            else
            {
                _detailsDontCareRegion.Dispose();
                HOperatorSet.GenEmptyObj(out _detailsDontCareRegion);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取设置辅助匹配区域后的图像
        /// </summary>
        /// <param name="caliperParam">卡尺参数</param>
        /// <param name="detailImg">返回图像</param>
        /// <param name="minLength">线段最小长度</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetDetailImg(CaliperParams caliperParam, out Camera detailImg, int minLength = 10)
        {
            detailImg = null;

            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            Errortype ret = MarkLocation.Common.GetMarkEdgeLine(_imgPart, caliperParam, out HObject edgeHImg, out List<List<LineSeg>> lineSegs, null, _trainDontCareRegion, minLength);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _lineSegs = lineSegs;

            ret = ScaleImgPart(edgeHImg, _detailsDontCareRegion, 0.5, out HObject imgOut);
            if (ret != Errortype.OK)
            {
                imgOut.Dispose();
                return ret;
            }

            detailImg = new Camera(imgOut);

            imgOut.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 创建pattern
        /// </summary>
        /// <typeparam name="TCreate">模板创建参数，泛型</typeparam>
        /// <typeparam name="TMatch">模板匹配参数，泛型</typeparam>
        /// <param name="patternName">模板名称</param>
        /// <param name="templateParams">模板创建参数</param>
        /// <param name="matchParams">模板匹配参数</param>
        /// <param name="caliperParams">卡尺参数</param>
        /// <param name="templateType">模板类型</param>
        /// <param name="useLinePolarity">是否使用线段极性</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CreatePattern<TCreate, TMatch>(string patternName, TCreate templateParams, TMatch matchParams, CaliperParams caliperParams, TemplateType templateType, bool useLinePolarity = true)
        {
            if (_imgPart == null)
            {
                return Errortype.PATTERN_PARTIMG_NULL;
            }

            if (!_imgPart.IsInitialized())
            {
                return Errortype.PATTERN_PARTIMG_EMPTY;
            }

            if (_lineSegs == null)
            {
                return Errortype.PATTERN_LINE_NULL;
            }

            HOperatorSet.GetImageSize(_imgPart, out HTuple width, out HTuple height);
            HOperatorSet.GenRectangle1(out HObject imgRegion, 0, 0, height, width);
            HOperatorSet.GenEmptyObj(out HObject templateRegion);
            templateRegion = imgRegion;
            if (_detailsDontCareRegion != null && _detailsDontCareRegion.IsInitialized())
            {
                HOperatorSet.Difference(imgRegion, _detailsDontCareRegion, out templateRegion);
            }

            // no use line polarity  change lines polarity
            if (!useLinePolarity)
            {
                for (int i = 0; i < _lineSegs.Count; i++)
                {
                    for (int j = 0; j < _lineSegs[i].Count; j++)
                    {
                        _lineSegs[i][j].ProbInfo = "auto";
                    }
                }
            }

            Camera imgCamera = new Camera(_imgPart);
            Errortype ret = MarkLocation.MarkAutoCenterLocationManager.CreateTemplate<TCreate, TMatch>(patternName, imgCamera, templateParams, matchParams, templateRegion, null, _trainDontCareRegion, _lineSegs, caliperParams, templateType);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _patternName = patternName;
            return Errortype.OK;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="dir">模板保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Save(string dir)
        {
            return MarkLocation.MarkAutoCenterLocationManager.Save(dir, _patternName);
        }

        /// <summary>
        /// 加载patterntrain信息
        /// </summary>
        /// <param name="dir">路径</param>
        /// <param name="markName">Mark名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Load(string dir, string markName)
        {
            Errortype ret = Errortype.OK;
            ret = MarkLocation.MarkAutoCenterLocationManager.Init(dir, new List<string>() { markName }, out Dictionary<string, Errortype> initReturn);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (initReturn[markName] != Errortype.OK)
            {
                return ret;
            }

            ret = MarkLocation.MarkAutoCenterLocationManager.GetPatternTrainInfo(markName, out _imgPart, out HObject templateRegion, out HObject templateMask, out _trainDontCareRegion, out _lineSegs);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HOperatorSet.GetDomain(_imgPart, out HObject imgRegion);
            HOperatorSet.Difference(imgRegion, templateRegion, out _detailsDontCareRegion);

            return ret;
        }

        /// <summary>
        /// 释放，清空内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            //MarkLocation.MarkAutoCenterLocationManager.Release();
            if (_imgSource != null)
            {
                _imgSource.Dispose();
            }

            if (_imgPart != null)
            {
                _imgPart.Dispose();
            }

            if (_trainDontCareRegion != null)
            {
                _trainDontCareRegion.Dispose();
            }

            if (_region != null)
            {
                _region.Dispose();
            }

            if (_detailsDontCareRegion != null)
            {
                _detailsDontCareRegion.Dispose();
            }

            if (_lineSegs != null)
            {
                _lineSegs.Clear();
            }

            return Errortype.OK;
        }
    }
}