using DataStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaxwellControl.Controls;
using UltrapreciseBonding.MarkLocation;
using UltrapreciseBonding.PatternManager;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace MwTrainTemplate.Common
{
    public class MarkLocationManagerService : SingletonTemplate<MarkLocationManagerService>
    {
        private Errortype ret = Errortype.OK;

        private DataStruct.CaliperParams _caliperParams = new DataStruct.CaliperParams();

        private NccTemplateParams _nccTemplateParams = new NccTemplateParams();

        private NccMatchParams _nccMatchParams = new NccMatchParams();

        private ShapeTemplateParams _shapeTemplateParams = new ShapeTemplateParams();

        private ShapeMatchParams _shapeMatchParams = new ShapeMatchParams();

        private TemplateType _templateType = TemplateType.SHAPE;

        private int _minLength = 10;

        //public List<string> AlignmentTopMarkModelPath { get; set; } = new List<string>();
        //public List<string> AlignmentBottomMarkModelPath { get; set; } = new List<string>();
        //public List<string> AVMTopMarkModelPath { get; set; } = new List<string>();
        //public List<string> AVMBottomMarkModelPath { get; set; } = new List<string>();
        //public List<string> PECMarkModelPath { get; set; } = new List<string>();
        //public List<string> LoadModelPath { get; set; } = new List<string>();
        public bool UseLinePolarity { get; set; } = true;

        public PatternMetric Metric { get; set; } = PatternMetric.Use;

        /// <summary>
        /// 设置pattern参数
        /// </summary>
        /// <param name="minLength">最小线长</param>
        /// <param name="edgeThreshold">边缘提取阈值</param>
        /// <param name="scoreThreshold">得分阈值</param>
        /// <returns></returns>
        public Errortype SetParams(int minLength, int edgeThreshold, double scoreThreshold, TemplateType templateType = TemplateType.SHAPE, PatternMetric metric = PatternMetric.Use, bool isUseLinePolarity = true)
        {
            _minLength = minLength;
            _caliperParams.MeasureThreshold = edgeThreshold;
            _nccMatchParams.MinScore = scoreThreshold;
            _shapeMatchParams.MinScore = scoreThreshold;
            this.UseLinePolarity = isUseLinePolarity;
            this.Metric = metric;
            this._templateType = templateType;
            switch (Metric)
            {
                case PatternMetric.Use:
                    this._nccTemplateParams.Metric = "use_polarity";
                    this._shapeTemplateParams.Metric = "use_polarity";
                    break;
                case PatternMetric.IgnoreGlobal:
                    this._nccTemplateParams.Metric = "ignore_global_polarity";
                    this._shapeTemplateParams.Metric = "ignore_global_polarity";
                    break;
                case PatternMetric.IgnoreLocal:
                    this._nccTemplateParams.Metric = "ignore_global_polarity";
                    this._shapeTemplateParams.Metric = "ignore_local_polarity";
                    break;
                default:
                    this._nccTemplateParams.Metric = "use_polarity";
                    this._shapeTemplateParams.Metric = "use_polarity";
                    break;
            }
            return Errortype.OK;
        }

        /// <summary>
        /// 设置矩形区域
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="region"></param>
        /// <param name="regionImg"></param>
        /// <returns></returns>
        public Errortype SetRegionRectangle(Camera camera, Rectangle1 region, out Camera regionImg)
        {
            try
            {
                regionImg = null;
                Errortype ret = PatterManager.SetImgSource(camera);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
                var regionT = ChangeRect(region);
                ret = PatterManager.SetRegion(regionT);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
                ret = PatterManager.GetRegionImg(_caliperParams, out regionImg, _minLength);
                return ret;
            }
            catch (System.Exception)
            {
                regionImg = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 设置Train care Region
        /// </summary>
        /// <param name="p"></param>
        /// <param name="regionImg"></param>
        /// <returns></returns>
        public Errortype SetTrainCareRegion(Dictionary<List<DataStruct.Point>, bool> p, out Camera regionImg)
        {
            try
            {
                regionImg = null;
                Errortype ret = PatterManager.SetTrainCareRegion(p);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = PatterManager.GetTrainDontCareImg(_caliperParams, out regionImg, _minLength);
                return ret;
            }
            catch (System.Exception)
            {
                regionImg = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 设置TrainDontCare
        /// </summary>
        /// <param name="p"></param>
        /// <param name="regionImg"></param>
        /// <returns></returns>
        public Errortype SetTrainDontCareRegion(Dictionary<List<DataStruct.Point>, bool> p, out Camera regionImg)
        {
            try
            {
                regionImg = null;
                Errortype ret = PatterManager.SetTrainDontCareRegion(p);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = PatterManager.GetTrainDontCareImg(_caliperParams, out regionImg, _minLength);
                return ret;
            }
            catch (System.Exception)
            {
                regionImg = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 清除Train dont care Region
        /// </summary>
        /// <param name="regionImg"></param>
        /// <returns></returns>
        public Errortype ClearTrainDontCareRegion(out Camera regionImg)
        {
            try
            {
                regionImg = null;
                Errortype ret = PatterManager.ClearTrainDontCareRegion();
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = PatterManager.GetTrainDontCareImg(_caliperParams, out regionImg, _minLength);
                return ret;
            }
            catch (System.Exception)
            {
                regionImg = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 设置DetailDontCare
        /// </summary>
        /// <param name="p"></param>
        /// <param name="regionImg"></param>
        /// <returns></returns>
        public Errortype SetDetailDontCareRegion(Dictionary<List<DataStruct.Point>, bool> p, out Camera regionImg)
        {
            try
            {
                regionImg = null;
                Errortype ret = PatterManager.SetDetailDontCareRegion(p);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = PatterManager.GetDetailImg(_caliperParams, out regionImg, _minLength);
                return ret;
            }
            catch (System.Exception)
            {
                regionImg = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 设置Detail
        /// </summary>
        /// <param name="p"></param>
        /// <param name="regionImg"></param>
        /// <returns></returns>
        public Errortype SetDetailRegion(Dictionary<List<DataStruct.Point>, bool> p, out Camera regionImg)
        {
            try
            {
                regionImg = null;
                Errortype ret = PatterManager.SetDetailRegion(p);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = PatterManager.GetDetailImg(_caliperParams, out regionImg, _minLength);
                return ret;
            }
            catch (System.Exception)
            {
                regionImg = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        public Errortype ClearDetailDontCareRegion(out Camera regionImg)
        {
            try
            {
                regionImg = null;
                Errortype ret = PatterManager.ClearDetailDontCareRegion();
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = PatterManager.GetDetailImg(_caliperParams, out regionImg, _minLength);
                return ret;
            }
            catch (System.Exception)
            {
                regionImg = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 获取Origin十字坐标
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public Errortype GetOrigin(out DataStruct.Point origin)
        {
            try
            {
                Errortype ret = PatterManager.GetOrigin(out origin, _caliperParams);
                return ret;
            }
            catch (System.Exception)
            {
                origin = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 获取模板显示图像
        /// </summary>
        /// <param name="markName"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public Errortype GetTemplateShowImg(out Camera img)
        {
            try
            {
                //ret = PatterManager.GetRegionImg(out img);
                Errortype ret = PatterManager.GetTrainDontCareImg(_caliperParams, out img, _minLength);
                return ret;
            }
            catch (System.Exception)
            {
                img = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 生成模板
        /// </summary>
        /// <param name="patternName"></param>
        /// <returns></returns>
        public Errortype CreatePattern(string patternName)
        {
            try
            {
                switch (_templateType)
                {
                    case TemplateType.NCC:
                        ret = PatterManager.CreatePattern<NccTemplateParams, NccMatchParams>(patternName, _nccTemplateParams, _nccMatchParams, _caliperParams, _templateType, UseLinePolarity);
                        break;
                    case TemplateType.SHAPE:
                        ret = PatterManager.CreatePattern<ShapeTemplateParams, ShapeMatchParams>(patternName, _shapeTemplateParams, _shapeMatchParams, _caliperParams, _templateType, UseLinePolarity);
                        break;
                    case TemplateType.SHAPEXLD:
                        ret = PatterManager.CreatePattern<ShapeTemplateParams, ShapeMatchParams>(patternName, _shapeTemplateParams, _shapeMatchParams, _caliperParams, _templateType, UseLinePolarity);
                        break;
                }

                return ret;
            }
            catch (System.Exception ex)
            {
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 保存模板
        /// </summary>
        /// <param name="patternPath"></param>
        /// <returns></returns>
        public Errortype SavePattern(string patternPath)
        {
            try
            {
                Errortype ret = PatterManager.Save(patternPath);
                return ret;
            }
            catch (System.Exception ex)
            {
                return Errortype.UNKNOW_ERROR;

            }
        }

        /// <summary>
        /// 加载模板
        /// </summary>
        /// <param name="dirPath"></param>
        /// <param name="patternName"></param>
        /// <returns></returns>
        public Errortype Load(string dirPath, string patternName)
        {
            try
            {
                Errortype ret = PatterManager.Load(dirPath, patternName);
                return ret;
            }
            catch (System.Exception)
            {
                return Errortype.AVM_CHECKPOINTS_NUM_ERROR;
            }
        }

        #region 调用
        /// <summary>
        /// 初始化模板(加载模板)
        /// </summary>
        /// <param name="DirPath"></param>
        /// <param name="patternName"></param>
        /// <param name="markInfo"></param>
        /// <returns></returns>
        public Errortype InitMarkAutoCenter(string dirPath, List<string> patternName)
        {
            try
            {
                Errortype ret = MarkAutoCenterLocationManager.Init(dirPath, patternName, out Dictionary<string, Errortype> initReturn);
                return ret;
            }
            catch (System.Exception)
            {
                return Errortype.UNKNOW_ERROR;

            }
        }

        /// <summary>
        /// 获取本地所有模板信息和图片
        /// </summary>
        /// <param name="DirPath"></param>
        /// <param name="markInfo"></param>
        /// <returns></returns>
        public Errortype GetAllPattern(string DirPath, out Dictionary<string, Camera> markInfo)
        {
            try
            {
                Errortype ret = MarkAutoCenterLocationManager.GetNameAndImg(DirPath, out markInfo);
                return ret;
            }
            catch (System.Exception)
            {
                markInfo = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 释放Manager
        /// </summary>
        /// <returns></returns>
        public Errortype Release()
        {
            try
            {
                Errortype ret = MarkAutoCenterLocationManager.Release();
                return ret;
            }
            catch (System.Exception)
            {
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 保存所有Mark信息
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public Errortype Save(string dir)
        {
            try
            {
                Errortype ret = MarkAutoCenterLocationManager.Save(dir, out Dictionary<string, Errortype> saveReturn);
                return ret;
            }
            catch (System.Exception)
            {
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 获取Mark中心点（匹配模板）
        /// </summary>
        /// <param name="markName"></param>
        /// <param name="img"></param>
        /// <param name="matchRegion"></param>      
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <param name="angles"></param>
        /// <param name="scores"></param>
        /// <param name="straightnessErrorList"></param>
        /// <param name="useMask"></param>
        /// <returns></returns>
        public Errortype GetMarkCenter(string markName, Camera img, Rectangle1 matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scores, /*out List<DataStruct.Point> inters,*/ out List<List<double[]>> straightnessErrorList, bool useMask = true)
        {
            try
            {
                //ret = MarkAutoCenterLocationManager.GetNccMarkCenter(markName, img, matchRegion, out rows, out cols, out angles, out scores, out straightnessErrorList);
                Errortype ret = MarkAutoCenterLocationManager.GetMarkCenter(markName, img, matchRegion, out rows, out cols, out angles, out scores, out straightnessErrorList);
                return ret;
            }
            catch (System.Exception ex)
            {
                rows = null; cols = null; angles = null; scores = null;/* inters = null;*/ straightnessErrorList = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        public Errortype GetDataSummary(Dictionary<string, List<double>> paramValue, out Dictionary<string, DataStatisticParam> dataStatisticParam)
        {
            dataStatisticParam = new Dictionary<string, DataStatisticParam>();
            foreach (var item in paramValue)
            {
                ret = ComAlgo.CalcDataSummary(item.Value, out DataStatisticParam analysisValue);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                dataStatisticParam.Add(item.Key, analysisValue);
            }

            return ret;
        }

        /// <summary>
        /// 根据模板名称获取模板显示图像
        /// </summary>
        /// <param name="markName"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public Errortype GetTemplateShowImg(string markName, out Camera img)
        {
            try
            {
                Errortype ret = MarkAutoCenterLocationManager.GetTemplateShowImg(markName, out img);
                return ret;
            }
            catch (System.Exception)
            {
                img = null;
                return Errortype.UNKNOW_ERROR;
            }
        }

        /// <summary>
        /// 修改模板名称
        /// </summary>
        /// <param name="markName"></param>
        /// <param name="markNameNew"></param>
        /// <returns></returns>
        public Errortype ChangeTemplateName(string markName, string markNameNew)
        {
            try
            {
                Errortype ret = MarkAutoCenterLocationManager.ChangeTemplateName(markName, markNameNew);
                return ret;
            }
            catch (System.Exception)
            {
                return Errortype.UNKNOW_ERROR;
            }
        }
        #endregion

        /// <summary>
        /// 获取4的倍数矩形
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        private static Rectangle1 ChangeRect(Rectangle1 t)
        {
            Rectangle1 ret = new Rectangle1();
            ret.Start_X = (int)t.Start_X;
            ret.Start_Y = (int)t.Start_Y;
            ret.End_X = (int)t.End_X;
            ret.End_Y = (int)t.End_Y;
            if ((ret.End_X - ret.Start_X + 1) % 4 != 0)
            {
                ret.End_X += 4 - (ret.End_X - ret.Start_X + 1) % 4;
            }
            return ret;
        }
    }
}
