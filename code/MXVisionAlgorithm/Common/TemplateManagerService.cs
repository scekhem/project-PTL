using DataStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using UltrapreciseBonding.TemplateMatch;

namespace MXVisionAlgorithm.Common
{
    public class TemplateManagerService : SingletonTemplate<TemplateManagerService>
    {
        private Errortype ret = Errortype.OK;

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="img">图像</param>
        /// <param name="templateCreateParams">模板创建参数</param>
        /// <param name="templateMatchParams">模板匹配参数</param>
        /// <param name="templateRegions">模板区域</param>
        /// <param name="templateMasks">掩膜区域</param>
        /// <param name="type">模板类型（NCC/Shape）</param>
        /// <returns></returns>
        public Errortype Create<TCreate, TMatch>(string templateName, Camera img, TCreate templateCreateParams, TMatch templateMatchParams, Region templateRegions, Region templateMasks, TemplateType type)
        {
            ret = TemplateManager.Create<TCreate, TMatch>(templateName, img, templateCreateParams, templateMatchParams, templateRegions, templateMasks, type);
            return ret;
        }

        /// <summary>
        /// 模版匹配
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="img">图像</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scales">匹配结果比例集合</param>
        /// <param name="scores">匹配结果分数集合</param>
        /// <returns></returns>
        public Errortype Match(string templateName, Camera img, Region matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores)
        {
            ret = TemplateManager.Match(templateName, img, matchRegion, out rows, out cols, out angles, out scales, out scores);
            return ret;

        }

        /// <summary>
        /// 获取模板的显示图像
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="img">显示图像</param>
        /// <returns></returns>
        public Errortype GetTemplateImg(string templateName, out Camera img)
        {
            ret = TemplateManager.GetTemplateImg(templateName, out img);
            return ret;
        }

        /// <summary>
        /// 更改模板名
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="newName">更改的新名称</param>
        /// <returns></returns>
        public Errortype ChangeTemplateName(string templateName, string newName)
        {
            ret = TemplateManager.ChangeTemplateName(templateName, newName);
            return ret;
        }

        /// <summary>
        /// 保存单个模版信息
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <param name="templateName">模板名称</param>
        /// <returns></returns>
        public Errortype Save(string dir, string templateName)
        {
            ret = TemplateManager.Save(dir, templateName);
            return ret;

        }

        /// <summary>
        /// 保存所有模版信息
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <returns></returns>
        public Errortype Save(string dir)
        {
            ret = TemplateManager.Save(dir, out Dictionary<string, Errortype> saveReturn);
            return ret;

        }

        /// <summary>
        /// 加载单个模板信息
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <param name="templateName">模板名称</param>
        /// <returns></returns>
        public Errortype Load(string dir, string templateName)
        {
            ret = TemplateManager.Load(dir, templateName);
            return ret;
        }

        /// <summary>
        /// 加载所有模板信息
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <param name="templateName">模板名称集合</param>
        /// <returns></returns>
        public Errortype Load(string dir, List<string> templateName)
        {
            ret = TemplateManager.Load(dir, templateName, out Dictionary<string, Errortype> loadReturn);
            return ret;
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        /// <param name="dir">删除路径</param>
        /// <param name="templateName">模板名称</param>
        /// <returns></returns>
        public Errortype Delete(string dir, string templateName)
        {
            ret = TemplateManager.Delete(dir, templateName);
            return ret;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns></returns>
        public Errortype Release()
        {
            ret = TemplateManager.Release();
            return ret;
        }
    }
}
