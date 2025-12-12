using DataStruct;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltrapreciseBonding.MarkLocation;
using UltrapreciseBonding.PatternManager;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace MXVisionAlgorithm.Common
{
    /// <summary>
    /// 模板匹配管理
    /// </summary>
    public class MarkLocationManagerService : SingletonTemplate<MarkLocationManagerService>
    {
        private Errortype ret = Errortype.OK;

        public DataStruct.CaliperParams CaliperParams { get; set; } = new DataStruct.CaliperParams();
        public NccTemplateParams NccTemplateParams { get; set; } = new NccTemplateParams();

        public NccMatchParams NccMatchParams { get; set; } = new NccMatchParams();

        public ShapeTemplateParams ShapeTemplateParams { get; set; } = new ShapeTemplateParams();

        public ShapeMatchParams ShapeMatchParams { get; set; } = new ShapeMatchParams();

        public int MinLength { get; set; } = 10;

        public TemplateType TemplateType { get; set; } = TemplateType.NCC;

        public bool UseLinePolarity { get; set; } = true;

        public List<string> AlignmentTopMarkModelPath { get; set; } = new List<string>();
        public List<string> AlignmentBottomMarkModelPath { get; set; } = new List<string>();
        public List<string> AVMTopMarkModelPath { get; set; } = new List<string>();
        public List<string> AVMBottomMarkModelPath { get; set; } = new List<string>();
        public List<string> PECMarkModelPath { get; set; } = new List<string>();
        public List<string> LoadModelPath { get; set; } = new List<string>();
        /// <summary>
        /// 设置矩形区域
        /// </summary>
        /// <param name="camera"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "<挂起>")]
        public Errortype SetRegionRectangle(Camera camera, Rectangle1 region, out Camera regionImg)
        {
            try
            {
                ret = PatterManager.SetImgSource(camera);
                var regionT = ChangeRect(region);
                ret = PatterManager.SetRegion(regionT);
                ret = PatterManager.GetRegionImg(CaliperParams, out regionImg, MinLength);
            }
            catch (System.Exception)
            {
                regionImg = null;
                return ret;
            }

            return ret;
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
                ret = PatterManager.SetTrainCareRegion(p);
                ret = PatterManager.GetTrainDontCareImg(CaliperParams, out regionImg, MinLength);
            }
            catch (System.Exception)
            {
                regionImg = null;
                return ret;
            }
            return ret;
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
                ret = PatterManager.SetTrainDontCareRegion(p);
                ret = PatterManager.GetTrainDontCareImg(CaliperParams, out regionImg, MinLength);
            }
            catch (System.Exception)
            {
                regionImg = null;
                return ret;
            }

            return ret;
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
                ret = PatterManager.ClearTrainDontCareRegion();
                ret = PatterManager.GetTrainDontCareImg(CaliperParams, out regionImg, MinLength);
            }
            catch (System.Exception)
            {
                regionImg = null;
                return ret;
            }
            return ret;
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
                ret = PatterManager.SetDetailDontCareRegion(p);
                ret = PatterManager.GetDetailImg(CaliperParams, out regionImg, MinLength);
            }
            catch (System.Exception)
            {
                regionImg = null;
                return ret;
            }

            return ret;
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
                ret = PatterManager.SetDetailRegion(p);
                ret = PatterManager.GetDetailImg(CaliperParams, out regionImg, MinLength);
            }
            catch (System.Exception)
            {
                regionImg = null;
                return ret;
            }

            return ret;
        }

        public Errortype ClearDetailDontCareRegion(out Camera regionImg)
        {
            try
            {
                ret = PatterManager.ClearDetailDontCareRegion();
                ret = PatterManager.GetDetailImg(CaliperParams, out regionImg, MinLength);
            }
            catch (System.Exception)
            {
                regionImg = null;
                return ret;
            }

            return ret;
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
                ret = PatterManager.GetOrigin(out origin, CaliperParams);
            }
            catch (System.Exception)
            {
                origin = null;
                if (ret == Errortype.OK) ret = Errortype.CALIPER_LINE_POLARITY_POINT_OUT_OF_IMAGE;
                return ret;
            }
            return ret;
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
                ret = PatterManager.GetTrainDontCareImg(CaliperParams, out img, MinLength);
                return ret;
            }
            catch (System.Exception)
            {
                img = null;
                return ret;
            }
        }
        /// <summary>
        /// 生成模板
        /// </summary>
        /// <param name="patternName"></param>
        /// <returns></returns>
        public Errortype CreateNccPattern(string patternName)
        {
            try
            {
                switch (TemplateType)
                {
                    case TemplateType.NCC:
                        ret = PatterManager.CreatePattern<NccTemplateParams, NccMatchParams>(patternName, NccTemplateParams, NccMatchParams, CaliperParams, TemplateType, UseLinePolarity);
                        break;
                    case TemplateType.SHAPE:
                        ret = PatterManager.CreatePattern<ShapeTemplateParams, ShapeMatchParams>(patternName, ShapeTemplateParams, ShapeMatchParams, CaliperParams, TemplateType, UseLinePolarity);
                        break;
                    case TemplateType.SHAPEXLD:
                        ret = PatterManager.CreatePattern<ShapeTemplateParams, ShapeMatchParams>(patternName, ShapeTemplateParams, ShapeMatchParams, CaliperParams, TemplateType, UseLinePolarity);
                        break;
                }

                return ret;
            }
            catch (System.Exception)
            {

                return ret;
            }
        }
        /// <summary>
        /// 保存模板
        /// </summary>
        /// <param name="patternPath"></param>
        /// <returns></returns>
        public Errortype SaveNccPattern(string patternPath)
        {
            try
            {
                ret = PatterManager.Save(patternPath);
                return ret;
            }
            catch (System.Exception)
            {

                return ret;
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
                ret = PatterManager.Load(dirPath, patternName);
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
                ret = MarkAutoCenterLocationManager.Init(dirPath, patternName, out Dictionary<string, Errortype> initReturn);
                return ret;
            }
            catch (System.Exception)
            {
                return ret;
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
                ret = MarkAutoCenterLocationManager.GetNameAndImg(DirPath, out markInfo);
                return ret;
            }
            catch (System.Exception)
            {
                markInfo = null;
                return ret;
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
                ret = MarkAutoCenterLocationManager.Release();
                return ret;
            }
            catch (System.Exception)
            {
                return ret;
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
                ret = MarkAutoCenterLocationManager.Save(dir, out Dictionary<string, Errortype> saveReturn);
                return ret;
            }
            catch (System.Exception)
            {
                return ret;
            }
        }

        /// <summary>
        /// 保存单个mark信息
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public Errortype Save(string dir, string name)
        {
            try
            {
                ret = MarkAutoCenterLocationManager.Save(dir, name);
                return ret;
            }
            catch (System.Exception)
            {
                return ret;
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
        public Errortype GetMarkCenter(string markName, Camera img, Rectangle1 matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scores, out List<List<double[]>> straightnessErrorList, bool useMask = true)
        {
            try
            {
                ret = MarkAutoCenterLocationManager.GetMarkCenter(markName, img, matchRegion, out rows, out cols, out angles, out scores, out straightnessErrorList);
                return ret;
            }
            catch (System.Exception)
            {
                rows = null; cols = null; angles = null; scores = null;/* inters = null;*/ straightnessErrorList = null;
                return ret;
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
        public Errortype GetNccTemplateShowImg(string markName, out Camera img)
        {
            try
            {
                ret = MarkAutoCenterLocationManager.GetTemplateShowImg(markName, out img);
                return ret;
            }
            catch (System.Exception)
            {
                img = null;
                return ret;
            }
        }

        /// <summary>
        /// 修改模板名称
        /// </summary>
        /// <param name="markName"></param>
        /// <param name="markNameNew"></param>
        /// <returns></returns>
        public Errortype ChangeNccTemplateName(string markName, string markNameNew)
        {
            try
            {
                ret = MarkAutoCenterLocationManager.ChangeTemplateName(markName, markNameNew);
                return ret;
            }
            catch (System.Exception)
            {
                return ret;
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
