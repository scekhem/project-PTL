using DataStruct;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using IniFileHelper;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using System.Diagnostics;
using UltrapreciseBonding.FusionCollections;
using System.Xml.Linq;
using UltrapreciseBonding.DieBonding;
using System.Linq.Expressions;

namespace UltrapreciseBonding.TemplateMatch
{
    /// <summary>
    /// 匹配区域管理
    /// </summary>
    public static class MatchRegionManager
    {
        private static Dictionary<string, Rectangle1> _matchRegionManager = new Dictionary<string, Rectangle1>();

        /// <summary>
        /// 保存模板匹配区域
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="dir">保存路径</param>
        /// <returns>OK：成功；其他：失败</returns>
        public static Errortype SaveMatchRegion(string regionName, Rectangle1 matchRegion, string dir)
        {
            if (regionName == null || matchRegion == null || dir == null)
            {
                return Errortype.SAVE_MATCH_REGION_ERROR;
            }

            //string path = dir + "\\" + regionName + "\\";
            string path = dir + "\\" + "MatchRegion" + "\\";
            string filename = path + "\\" + regionName + "_matchRegion.ini";
            string section = regionName + "_matchRegion";

            if (!Directory.Exists(path))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                directoryInfo.Create();
            }

            Errortype ret = matchRegion.Save(filename, section);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载单个模板匹配区域
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="dir">加载路径</param>
        /// <returns>OK：成功；其他：失败</returns>
        public static Errortype LoadMatchRegion(string regionName, string dir)
        {
            //string path = dir + "\\" + regionName + "\\";
            string path = dir + "\\" + "MatchRegion" + "\\";
            string filename = path + "\\" + regionName + "_matchRegion.ini";
            string section = regionName + "_matchRegion";
            if (!File.Exists(filename))
            {
                return Errortype.READ_TEMPLATEREGION_ERROR;
            }

            Rectangle1 matchRegion = new Rectangle1();
            Errortype ret = matchRegion.Load(filename, section);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (_matchRegionManager.ContainsKey(regionName))
            {
                _matchRegionManager[regionName] = matchRegion;
            }
            else
            {
                _matchRegionManager.Add(regionName, matchRegion);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载所有匹配区域
        /// </summary>
        /// <param name="regionNames">匹配区域名集合</param>
        /// <param name="dir">加载路径</param>
        /// <returns>OK：成功；其他：失败</returns>
        public static Errortype LoadMatchRegion(List<string> regionNames, string dir)
        {
            foreach (var regionName in regionNames)
            {
                Errortype ret = LoadMatchRegion(regionName, dir);
                if (ret != Errortype.OK)
                {
                    continue;
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取模板匹配区域
        /// </summary>
        /// <param name="regionName">区域名称</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <returns>OK：成功；其他：失败</returns>
        public static Errortype GetMatchRegion(string regionName, out Rectangle1 matchRegion)
        {
            matchRegion = null;
            if (regionName == null)
            {
                return Errortype.GET_MATCH_REGION_ERROR;
            }

            if (_matchRegionManager.ContainsKey(regionName))
            {
                matchRegion = _matchRegionManager[regionName];
            }
            else
            {
                return Errortype.GET_MATCH_REGION_ERROR;
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 模板类接口管理
    /// </summary>
    public static class TemplateManager
    {
        private static List<Template> _templateList;

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <typeparam name="TCreate">泛型参数-模板创建参数</typeparam>
        /// <typeparam name="TMatch">泛型参数-模板匹配参数</typeparam>
        /// <param name="templateName">模板名称</param>
        /// <param name="img">图像</param>
        /// <param name="templateCreateParams">模板创建参数</param>
        /// <param name="templateMatchParams">模板匹配参数</param>
        /// <param name="templateRegions">模板区域</param>
        /// <param name="templateMasks">掩膜区域</param>
        /// <param name="type">模板类型(NCC/shape)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Create<TCreate, TMatch>(string templateName, Camera img, TCreate templateCreateParams, TMatch templateMatchParams, Region templateRegions, Region templateMasks, TemplateType type)
        {
            if (_templateList == null)
            {
                _templateList = new List<Template>();
            }

            if (img == null)
            {
                return Errortype.TEMPLATEMANAGER_IMAGE_NULL;
            }

            if (templateRegions is null)
            {
                return Errortype.TEMPLATEMANAGER_TEMPLATEREGION_NULL;
            }

            switch (templateCreateParams)
            {
                case NccTemplateParams nccParams:
                    if (!(templateMatchParams is NccMatchParams))
                    {
                        return Errortype.TEMPLATEMANAGER_CREATE_PARAMS_ERROR;
                    }

                    if (type != TemplateType.NCC)
                    {
                        return Errortype.TEMPLATEMANAGER_CREATE_PARAMS_ERROR;
                    }

                    break;
                case ShapeTemplateParams shapeParams:
                    if (!(templateMatchParams is ShapeMatchParams))
                    {
                        return Errortype.TEMPLATEMANAGER_CREATE_PARAMS_ERROR;
                    }

                    if (type != TemplateType.SHAPE)
                    {
                        return Errortype.TEMPLATEMANAGER_CREATE_PARAMS_ERROR;
                    }

                    break;
                default:
                    return Errortype.TEMPLATEMANAGER_CREATE_PARAMS_ERROR;
            }

            Template template = _templateList.Find(e => e.Name == templateName);
            HObject hImg = img.GenHObject();

            HObject templateMaskHObject = templateMasks is null ? null : templateMasks.GenRegion();
            HObject templateRegion = templateRegions.GenRegion();
            Errortype ret = Errortype.OK;
            if (template != null)
            {
                template.Release();
                template.Type = type;
                ret = template.Create(hImg, templateCreateParams, templateMatchParams, templateRegion, templateMaskHObject);
            }
            else
            {
                template = new Template(templateName, type);
                ret = template.Create(hImg, templateCreateParams, templateMatchParams, templateRegion, templateMaskHObject);
                _templateList.Add(template);
            }

            templateRegion.Dispose();
            templateMaskHObject?.Dispose();

            if (ret != Errortype.OK)
            {
                int index = _templateList.FindIndex(e => e.Name == templateName);
                _templateList.RemoveAt(index);
            }

            return ret;
        }

        /// <summary>
        ///  创建模板
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="img">图像</param>
        /// <param name="lines">线段</param>
        /// <param name="shapeTemplateParams">模板创建参数</param>
        /// <param name="shapeMatchParams">模板匹配参数</param>
        /// <param name="type">模板类型</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Create(string templateName, Camera img, List<LineSeg> lines, ShapeTemplateParams shapeTemplateParams, ShapeMatchParams shapeMatchParams, TemplateType type)
        {
            if (type != TemplateType.SHAPEXLD)
            {
                return Errortype.TEMPLATE_SHAPEXLD_TYPE_ERROR;
            }

            if (_templateList == null)
            {
                _templateList = new List<Template>();
            }

            if (img == null)
            {
                return Errortype.TEMPLATEMANAGER_IMAGE_NULL;
            }

            Template template = _templateList.Find(e => e.Name == templateName);
            HObject hImg = img.GenHObject();

            Errortype ret = Errortype.OK;
            if (template != null)
            {
                template.Release();
                template.Type = TemplateType.SHAPEXLD;
                ret = template.Create(hImg, lines, shapeTemplateParams, shapeMatchParams);
            }
            else
            {
                template = new Template(templateName, TemplateType.SHAPEXLD);
                ret = template.Create(hImg, lines, shapeTemplateParams, shapeMatchParams);
                _templateList.Add(template);
            }

            return Errortype.OK;
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
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Match(string templateName, Camera img, Region matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores)
        {
            rows = null;
            cols = null;
            angles = null;
            scales = null;
            scores = null;
            if (_templateList == null)
            {
                return Errortype.TEMPLATEMANAGER_OBJECT_NULL;
            }

            if (img == null)
            {
                return Errortype.TEMPLATEMANAGER_IMAGE_NULL;
            }

            Template template = _templateList.Find(e => e.Name == templateName);
            if (template == null)
            {
                return Errortype.TEMPLATEMANAGER_OBJECT_NAME_NULL;
            }

            HObject hImg = img.GenHObject();

            HObject templateMatchHObject = matchRegion is null ? null : matchRegion.GenRegion();

            Errortype ret = template.Match(hImg, templateMatchHObject, out rows, out cols, out angles, out scales, out scores);

            if (ret != Errortype.OK)
            {
                if (ComAlgo.SaveFlg("TemplateMatchError", out int days))
                {
                    string path = @"D:\Alg\TemplateMatchErrorImg\";
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    //ComAlgo.FileManage(path, 7, out path);
                    Camera cameraImg = new Camera(hImg);
                    ComAlgo.SaveImage(path, cameraImg, null, days);
                    cameraImg.Dispose();
                }

                return ret;
            }

            hImg.Dispose();
            templateMatchHObject?.Dispose();
            return ret;
        }

        /// <summary>
        /// 获取模板的显示图像
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="img">显示图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetTemplateImg(string templateName, out Camera img)
        {
            img = null;
            if (_templateList == null)
            {
                return Errortype.TEMPLATEMANAGER_OBJECT_NULL;
            }

            Template template = _templateList.Find(e => e.Name == templateName);
            if (template == null)
            {
                return Errortype.TEMPLATEMANAGER_OBJECT_NAME_NULL;
            }

            HOperatorSet.GenEmptyObj(out HObject hImg);
            Errortype ret = template.GetImg(out hImg);
            if (ret != Errortype.OK)
            {
                hImg.Dispose();
                return ret;
            }

            img = new Camera(hImg);
            return Errortype.OK;
        }

        /// <summary>
        /// 更改模板名
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="newName">更改的新名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype ChangeTemplateName(string templateName, string newName)
        {
            if (_templateList == null)
            {
                return Errortype.TEMPLATEMANAGER_OBJECT_NULL;
            }

            Template template = _templateList.Find(e => e.Name == templateName);
            if (template == null)
            {
                return Errortype.TEMPLATEMANAGER_OBJECT_NAME_NULL;
            }

            return template.ChangeName(newName);
        }

        /// <summary>
        /// 保存单个模版信息
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <param name="templateName">模板名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Save(string dir, string templateName)
        {
            if (_templateList == null)
            {
                return Errortype.TEMPLATEMANAGER_SAVE_OBJECT_NULL;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Template template = _templateList.Find(e => e.Name == templateName);
            if (template != null)
            {
                Errortype ret = template.Save(dir);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }
            else
            {
                return Errortype.TEMPLATEMANAGER_OBJECT_NAME_NULL;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存所有模版信息
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <param name="saveReturn">保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Save(string dir, out Dictionary<string, Errortype> saveReturn)
        {
            saveReturn = new Dictionary<string, Errortype>();

            if (_templateList == null)
            {
                return Errortype.TEMPLATEMANAGER_SAVE_OBJECT_NULL;
            }

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            for (int i = 0; i < _templateList.Count; i++)
            {
                Errortype ret = _templateList[i].Save(dir);
                saveReturn.Add(_templateList[i].Name, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载单个模板信息
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <param name="templateName">模板名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Load(string dir, string templateName)
        {
            if (_templateList == null)
            {
                _templateList = new List<Template>();
            }

            if (!Directory.Exists(dir))
            {
                return Errortype.TEMPLATEMANAGER_LOAD_DIR_NOT_EXIST;
            }

            string infoFile = dir + "\\" + templateName + "\\" + templateName + ".ini";
            if (!File.Exists(infoFile))
            {
                return Errortype.TEMPLATEMANAGER_LOAD_INFO_NOT_EXIST;
            }

            IniHelper.GetAllKeyValues("Info", out string[] keys, out string[] values, infoFile);
            TemplateType type = (TemplateType)Enum.Parse(typeof(TemplateType), values[1]);
            string readName = values[0];
            if (readName != templateName)
            {
                return Errortype.TEMPLATEMANAGER_LOAD_NAME_NOT_EQUAL;
            }

            Template template = _templateList.Find(e => e.Name == templateName);
            if (template != null)
            {
                Errortype ret = template.Load(dir);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }
            else
            {
                template = new Template(templateName, type);
                Errortype ret = template.Load(dir);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                _templateList.Add(template);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载所有模板信息
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <param name="templateName">模板名称集合</param>
        /// <param name="loadReturn">模板加载结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Load(string dir, List<string> templateName, out Dictionary<string, Errortype> loadReturn)
        {
            loadReturn = new Dictionary<string, Errortype>();

            Errortype ret;
            foreach (var name in templateName)
            {
                ret = Load(dir, name);
                if (ret != Errortype.OK)
                {
                    loadReturn.Add(name, ret);
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        /// <param name="dir">删除路径</param>
        /// <param name="templateName">模板名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Delete(string dir, string templateName)
        {
            if (_templateList == null)
            {
                _templateList = new List<Template>();
            }

            if (!Directory.Exists(dir))
            {
                return Errortype.TEMPLATEMANAGER_DELETE_DIR_NOT_EXIST;
            }

            Template template = _templateList.Find(e => e.Name == templateName);
            if (template != null)
            {
                string templatePath = dir + "\\" + templateName;
                if (Directory.Exists(templatePath))
                {
                    Directory.Delete(templatePath, true);
                }

                _templateList.Remove(template);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            if (_templateList != null)
            {
                for (int i = 0; i < _templateList.Count; i++)
                {
                    Errortype ret = _templateList[i].Release();
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }
                }

                _templateList.Clear();
                _templateList = null;
            }

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 模板基础类
    /// </summary>
    public abstract class TemplateBase
    {
        private string _name = null;
        private TemplateType _type = TemplateType.NCC;
        private HTuple _hvModelID;
        private Point _centerPoint;
        private HObject _img;

        /// <summary>
        /// Gets or sets the user's _type
        /// </summary>
        public TemplateType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        /// <summary>
        /// Gets or sets the user's _name
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        /// <summary>
        /// Gets or sets the user's _hvModelID
        /// </summary>
        public HTuple HvModelID
        {
            get { return _hvModelID; }
            set { _hvModelID = value; }
        }

        /// <summary>
        /// Gets or sets the user's _img
        /// </summary>
        public HObject Img
        {
            get { return _img; }
            set { _img = value; }
        }

        /// <summary>
        /// Gets or sets the user's _centerPoint
        /// </summary>
        public Point CenterPoint
        {
            get { return _centerPoint; }
            set { _centerPoint = value; }
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public abstract Errortype Load(string dir);

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public abstract Errortype Save(string dir);
    }

    /// <summary>
    /// 模板匹配
    /// </summary>
    public class Template : TemplateBase
    {
        private HObject _templateRegion;    //模板区域
        private HObject _maskRegion;        //掩膜区域

        private ShapeMatchParams _shapeMatchParams;     //模板创建参数
        private NccMatchParams _nccMatchParams;         //模板匹配参数

        /// <summary>
        /// Gets the user's _maskRegion
        /// </summary>
        public HObject MaskRegion
        {
            get { return _maskRegion; }
        }

        /// <summary>
        /// Gets the user's _templateRegion
        /// </summary>
        public HObject TemplateRegion
        {
            get { return _templateRegion; }
        }

        /// <summary>
        /// Gets the user's _shapeMatchParams
        /// </summary>
        public ShapeMatchParams ShapeMatchParams
        {
            get { return _shapeMatchParams; }
        }

        /// <summary>
        /// Gets the user's _nccMatchParams
        /// </summary>
        public NccMatchParams NccMatchParams
        {
            get { return _nccMatchParams; }
        }

        /// <summary>
        /// 有参构造  
        /// </summary>
        /// <param name="name">名称</param>
        /// <param name="type">模板类型</param>
        public Template(string name, TemplateType type)
        {
            this.Name = name;
            this.Type = type;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~Template()
        {
            Release();
        }

        /// <summary>
        /// 创建NCC模板
        /// </summary>
        /// <typeparam name="TCreate">泛型参数</typeparam>
        /// <param name="imageReduced">输入图像</param>
        /// <param name="templateCreateParams">模板创建参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype CreateNccTemplate<TCreate>(HObject imageReduced, TCreate templateCreateParams)
        {
            if (templateCreateParams is ShapeTemplateParams)
            {
                return Errortype.TEMPLATE_CREATE_PARAMTYPE_ERROR;
            }

            NccTemplateParams nccTemplateParams = templateCreateParams as NccTemplateParams;

            HTuple numLevels = nccTemplateParams.Pyramid == -1 ? new HTuple("auto") : new HTuple(nccTemplateParams.Pyramid);
            HTuple angleStep = string.Equals(nccTemplateParams.AngleStep, "auto") ? new HTuple("auto") : new HTuple(double.Parse(nccTemplateParams.AngleStep));

            HOperatorSet.CreateNccModel(imageReduced, numLevels, new HTuple(nccTemplateParams.AngleStart), new HTuple(nccTemplateParams.AngleExtent),
                angleStep, new HTuple(nccTemplateParams.Metric), out HTuple hvModelID);

            HvModelID = hvModelID;
            return Errortype.OK;
        }

        /// <summary>
        /// 创建Shape模板
        /// </summary>
        /// <typeparam name="TCreate">泛型参数</typeparam>
        /// <param name="imageReduced">输入图像</param>
        /// <param name="templateCreateParams">模板创建参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype CreateShapeTemplate<TCreate>(HObject imageReduced, TCreate templateCreateParams)
        {
            if (templateCreateParams is NccTemplateParams)
            {
                return Errortype.TEMPLATE_CREATE_PARAMTYPE_ERROR;
            }

            ShapeTemplateParams shapeTemplateParams = templateCreateParams as ShapeTemplateParams;

            HTuple numLevels = shapeTemplateParams.Pyramid == -1 ? new HTuple("auto") : new HTuple(shapeTemplateParams.Pyramid);
            HTuple angleStep = string.Equals(shapeTemplateParams.AngleStep, "auto") ? new HTuple("auto") : new HTuple(double.Parse(shapeTemplateParams.AngleStep));
            HTuple scaleStep = string.Equals(shapeTemplateParams.ScaleStep, "auto") ? new HTuple("auto") : new HTuple(double.Parse(shapeTemplateParams.ScaleStep));

            HOperatorSet.CreateScaledShapeModel(imageReduced, numLevels, new HTuple(shapeTemplateParams.AngleStart), new HTuple(shapeTemplateParams.AngleExtent),
                angleStep, shapeTemplateParams.ScaleMin, shapeTemplateParams.ScaleMax, scaleStep, shapeTemplateParams.Optimization,
                shapeTemplateParams.Metric, shapeTemplateParams.Contrast, shapeTemplateParams.MinContrast, out HTuple hvModelID);
            HvModelID = hvModelID;
            return Errortype.OK;
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <typeparam name="TCreate">创建模板参数泛型</typeparam>
        /// <typeparam name="TMatch">模板匹配参数泛型</typeparam>
        /// <param name="img">图像</param>
        /// <param name="templateCreateParams">模板创建参数</param>
        /// <param name="templateMatchParams">模板匹配参数</param>
        /// <param name="templateRegions">模板区域</param>
        /// <param name="templateMasks">掩膜区域</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Create<TCreate, TMatch>(HObject img, TCreate templateCreateParams, TMatch templateMatchParams, HObject templateRegions, HObject templateMasks)
        {
            Errortype ret = Errortype.OK;
            HTuple area = new HTuple();
            HTuple rowCenter = new HTuple();
            HTuple colCenter = new HTuple();

            HOperatorSet.GenEmptyObj(out HObject templateRegionsUnion);
            HOperatorSet.GenEmptyObj(out HObject templateMasksUnion);
            HOperatorSet.GenEmptyObj(out HObject templateRegionResult);
            HOperatorSet.GenEmptyObj(out HObject ho_imageReduced);

            HOperatorSet.GetImageSize(img, out HTuple width, out HTuple height);

            if (templateRegions != null && templateRegions.IsInitialized())
            {
                HOperatorSet.TestEqualObj(templateRegionsUnion, templateRegions, out HTuple isEqual);
                if (isEqual)
                {
                    return Errortype.TEMPLATE_CREATE_REGION_EMPTY;
                }

                templateRegionsUnion = templateRegions.Clone();
            }
            else
            {
                HOperatorSet.GenRectangle1(out templateRegionsUnion, 0, 0, height, width);
            }

            if (templateMasks != null)
            {
                templateMasksUnion = templateMasks.Clone();
            }

            HOperatorSet.Difference(templateRegionsUnion, templateMasksUnion, out templateRegionResult);

            HOperatorSet.AreaCenter(templateRegionResult, out area, out rowCenter, out colCenter);
            CenterPoint = new Point() { X = colCenter, Y = rowCenter };

            HOperatorSet.ReduceDomain(img, templateRegionResult, out ho_imageReduced);

            switch (Type)
            {
                case TemplateType.NCC:
                    ret = CreateNccTemplate(ho_imageReduced, templateCreateParams);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    break;
                case TemplateType.SHAPE:
                    ret = CreateShapeTemplate(ho_imageReduced, templateCreateParams);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    break;
                default:
                    break;
            }

            if (HvModelID.TupleSelect(0).I < 0)
            {
                ret = Errortype.CREATE_TEMPLATE_ERROR;
            }

            if (_templateRegion != null && _templateRegion.IsInitialized())
            {
                _templateRegion.Dispose();
            }

            if (_maskRegion != null && _maskRegion.IsInitialized())
            {
                _maskRegion.Dispose();
            }

            _templateRegion = templateRegionsUnion.Clone();
            _maskRegion = templateMasksUnion.Clone();

            if (templateMatchParams is NccMatchParams)
            {
                _nccMatchParams = templateMatchParams as NccMatchParams;
            }

            if (templateMatchParams is ShapeMatchParams)
            {
                _shapeMatchParams = templateMatchParams as ShapeMatchParams;
            }

            //create showImg
            HOperatorSet.GenRectangle1(out HObject imageRegion, 0, 0, height, width);
            HOperatorSet.Difference(imageRegion, templateRegionResult, out HObject paintRegion);
            HOperatorSet.PaintRegion(paintRegion, img, out HObject imgPainted, 0, "fill");
            Img = imgPainted.Clone();

            templateRegionsUnion.Dispose();
            templateMasksUnion.Dispose();
            templateRegionResult.Dispose();
            ho_imageReduced.Dispose();
            imageRegion.Dispose();
            paintRegion.Dispose();
            imgPainted.Dispose();
            return ret;
        }

        /// <summary>
        /// 创建模板  根据输入线段创建shape xld 模板
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="lines">输入线段</param>
        /// <param name="shapeTemplateParams">模板参数</param>
        /// <param name="shapeMatchParams">匹配参数</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Create(HObject img, List<LineSeg> lines, ShapeTemplateParams shapeTemplateParams, ShapeMatchParams shapeMatchParams)
        {
            if (lines is null)
            {
                return Errortype.TEMPLATE_CREATE_EDGELINE_NULL;
            }

            if (lines.Count == 0)
            {
                return Errortype.TEMPLATE_CREATE_EDGELINE_COUNT_ERROR;
            }

            HTuple numLevels = shapeTemplateParams.Pyramid == -1 ? new HTuple("auto") : new HTuple(shapeTemplateParams.Pyramid);
            HTuple angleStep = string.Equals(shapeTemplateParams.AngleStep, "auto") ? new HTuple("auto") : new HTuple(double.Parse(shapeTemplateParams.AngleStep));
            HTuple scaleStep = string.Equals(shapeTemplateParams.ScaleStep, "auto") ? new HTuple("auto") : new HTuple(double.Parse(shapeTemplateParams.ScaleStep));
            HTuple minConstrast = string.Equals(shapeTemplateParams.MinContrast, "auto") ? new HTuple(5) : new HTuple(double.Parse(shapeTemplateParams.MinContrast));
            HTuple metric = new HTuple("ignore_local_polarity"); // shape xld 只支持ignore_local_polarity
            HOperatorSet.GenEmptyObj(out HObject contour);
            for (int i = 0; i < lines.Count; i++)
            {
                HTuple row = new HTuple(lines[i].Start_Y, lines[i].End_Y);
                HTuple col = new HTuple(lines[i].Start_X, lines[i].End_X);
                HOperatorSet.GenContourPolygonXld(out HObject contourT, row, col);
                HOperatorSet.ConcatObj(contour, contourT, out contour);
                contourT.Dispose();
                row.UnPinTuple();
                col.UnPinTuple();
            }

            HOperatorSet.CreateScaledShapeModelXld(contour, numLevels, new HTuple(shapeTemplateParams.AngleStart), new HTuple(shapeTemplateParams.AngleExtent),
                angleStep, shapeTemplateParams.ScaleMin, shapeTemplateParams.ScaleMax, scaleStep, shapeTemplateParams.Optimization, metric,
                minConstrast, out HTuple hvModelId);

            HvModelID = hvModelId;

            _shapeMatchParams = shapeMatchParams.Clone();

            HOperatorSet.GenRegionContourXld(contour, out HObject contourRegion, "margin");
            HOperatorSet.Union1(contourRegion, out HObject contourUnionRegion);
            HOperatorSet.AreaCenter(contourUnionRegion, out HTuple area, out HTuple rowCenter, out HTuple colCenter);
            CenterPoint = new Point(colCenter.D, rowCenter.D);

            Img = img.Clone();

            if (_templateRegion != null && _templateRegion.IsInitialized())
            {
                _templateRegion.Dispose();
            }

            if (_maskRegion != null && _maskRegion.IsInitialized())
            {
                _maskRegion.Dispose();
            }

            HOperatorSet.GenEmptyObj(out _templateRegion);
            HOperatorSet.GenEmptyObj(out _maskRegion);

            area.UnPinTuple();
            rowCenter.UnPinTuple();
            colCenter.UnPinTuple();
            contourRegion.Dispose();
            contourUnionRegion.Dispose();
            contour.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// NCC匹配
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scales">匹配结果比例集合</param>
        /// <param name="scores">匹配结果分数集合</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype FindNccTemplate(HObject img, out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;
            scales = null;
            HTuple hv_Row = new HTuple();
            HTuple hv_Column = new HTuple();
            HTuple hv_Angle = new HTuple();
            HTuple hv_Score = new HTuple();

            NccMatchParams nccMatchParams = _nccMatchParams;
            Errortype ret = Errortype.OK;

            HOperatorSet.FindNccModel(img, HvModelID, nccMatchParams.AngleStart, nccMatchParams.AngleExtent, nccMatchParams.MinScore / 2, nccMatchParams.NumMatches,
                nccMatchParams.MaxOverlap, nccMatchParams.SubPixel.ToString().ToLower(), nccMatchParams.Pyramid,
                out hv_Row, out hv_Column, out hv_Angle, out hv_Score);

            LogMatchScore(hv_Score, TemplateType.NCC);

            if (hv_Row.Length == 0)
            {
                ret = Errortype.FIND_TEMPLATE_ZERO;
            }
            else
            {
                List<double> rowsList = new List<double>();
                List<double> colsList = new List<double>();
                List<double> anglesList = new List<double>();
                List<double> scoresList = new List<double>();
                List<double> scalesList = new List<double>();
                for (int i = 0; i < hv_Row.TupleLength(); i++)
                {
                    if (hv_Score[i].D < nccMatchParams.MinScore)
                    {
                        continue;
                    }

                    rowsList.Add(hv_Row[i].D);
                    colsList.Add(hv_Column[i].D);
                    anglesList.Add(hv_Angle[i].D);
                    scoresList.Add(hv_Score[i].D);
                    scalesList.Add(1);
                }

                rows = rowsList.ToArray();
                cols = colsList.ToArray();
                angles = anglesList.ToArray();
                scores = scoresList.ToArray();
                scales = scalesList.ToArray();
                if (rowsList.Count == 0)
                {
                    ret = Errortype.FIND_TEMPLATE_ZERO;
                }
            }

            return ret;
        }

        /// <summary>
        /// Shape匹配
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scales">匹配结果比例集合</param>
        /// <param name="scores">匹配结果分数集合</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype FindShapeTemplate(HObject img, out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;
            scales = null;
            HTuple hv_Row = new HTuple();
            HTuple hv_Column = new HTuple();
            HTuple hv_Angle = new HTuple();
            HTuple hv_Scale = new HTuple();
            HTuple hv_Score = new HTuple();

            ShapeMatchParams shapeMatchParams = _shapeMatchParams;
            Errortype ret = Errortype.OK;
            HOperatorSet.FindScaledShapeModel(img, HvModelID, shapeMatchParams.AngleStart, shapeMatchParams.AngleExtent, shapeMatchParams.ScaleMin, shapeMatchParams.ScaleMax,
                shapeMatchParams.MinScore / 2, shapeMatchParams.NumMatches, shapeMatchParams.MaxOverlap, shapeMatchParams.SubPixel, shapeMatchParams.Pyramid, shapeMatchParams.Greediness,
                out hv_Row, out hv_Column, out hv_Angle, out hv_Scale, out hv_Score);

            LogMatchScore(hv_Score, TemplateType.SHAPE);

            if (hv_Row.Length == 0)
            {
                ret = Errortype.FIND_TEMPLATE_ZERO;
            }
            else
            {
                List<double> rowsList = new List<double>();
                List<double> colsList = new List<double>();
                List<double> anglesList = new List<double>();
                List<double> scoresList = new List<double>();
                List<double> scalesList = new List<double>();
                for (int i = 0; i < hv_Row.TupleLength(); i++)
                {
                    if (hv_Score[i].D < shapeMatchParams.MinScore)
                    {
                        continue;
                    }

                    rowsList.Add(hv_Row[i].D);
                    colsList.Add(hv_Column[i].D);
                    anglesList.Add(hv_Angle[i].D);
                    scoresList.Add(hv_Score[i].D);
                    scalesList.Add(hv_Scale[i].D);
                }

                rows = rowsList.ToArray();
                cols = colsList.ToArray();
                angles = anglesList.ToArray();
                scores = scoresList.ToArray();
                scales = scalesList.ToArray();
                if (rowsList.Count == 0)
                {
                    ret = Errortype.FIND_TEMPLATE_ZERO;
                }
            }

            return ret;
        }

        /// <summary>
        /// 匹配模板
        /// </summary>
        /// <param name="img">图像</param>
        /// <param name="matchRegion">匹配区域</param>
        /// <param name="rows">匹配结果行集合</param>
        /// <param name="cols">匹配结果列集合</param>
        /// <param name="angles">匹配结果角度集合</param>
        /// <param name="scales">匹配结果比例集合</param>
        /// <param name="scores">匹配结果得分集合</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Match(HObject img, HObject matchRegion, out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores)
        {
            rows = null;
            cols = null;
            angles = null;
            scores = null;
            scales = null;
            Errortype ret = Errortype.OK;

            HOperatorSet.GenEmptyObj(out HObject ho_region);
            HOperatorSet.GenEmptyObj(out HObject ho_imageReduced);
            HOperatorSet.GetImageSize(img, out HTuple width, out HTuple height);
            if (matchRegion != null && matchRegion.IsInitialized())
            {
                HOperatorSet.TestEqualObj(ho_region, matchRegion, out HTuple isEqual);
                if (isEqual)
                {
                    HOperatorSet.GenRectangle1(out ho_region, 0, 0, height, width);
                }
                else
                {
                    ho_region = matchRegion;
                }
            }
            else
            {
                HOperatorSet.GenRectangle1(out ho_region, 0, 0, height, width);
            }

            HOperatorSet.ReduceDomain(img, ho_region, out ho_imageReduced);

            switch (Type)
            {
                case TemplateType.NCC:
                    ret = FindNccTemplate(ho_imageReduced, out rows, out cols, out angles, out scales, out scores);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    break;
                case TemplateType.SHAPE:
                    ret = FindShapeTemplate(ho_imageReduced, out rows, out cols, out angles, out scales, out scores);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    break;

                case TemplateType.SHAPEXLD:
                    ret = FindShapeTemplate(ho_imageReduced, out rows, out cols, out angles, out scales, out scores);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    break;
                default:
                    break;
            }

            ho_region.Dispose();
            ho_imageReduced.Dispose();
            return ret;
        }

        /// <summary>
        /// 获取模板的显示图像
        /// </summary>
        /// <param name="img">显示图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetImg(out HObject img)
        {
            img = Img.Clone();
            return Errortype.OK;
        }

        /// <summary>
        /// 更改模板名字
        /// </summary>
        /// <param name="name">模板新名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype ChangeName(string name)
        {
            Name = name;
            return Errortype.OK;
        }

        /// <summary>
        /// 保存模板信息
        /// </summary>
        /// <param name="dir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Save(string dir)
        {
            string path = dir + "\\" + Name + "\\";
            string fileName = path + Name;
            if (!Directory.Exists(path))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                directoryInfo.Create();
            }

            string templateRegionFilename = path + "\\" + Name + "_TemplateRegion.hobj";
            HOperatorSet.WriteRegion(_templateRegion, templateRegionFilename);

            string maskRegionFilename = path + "\\" + Name + "_maskRegion.hobj";
            HOperatorSet.WriteRegion(_maskRegion, maskRegionFilename);

            string basePointFile = path + "\\" + Name + "_BaseCenterPoint.ini";
            CenterPoint.Save(basePointFile, "BaseCenterPoint");

            string showImgFile = path + "\\" + Name + "_ShowImg.bmp";
            HOperatorSet.WriteImage(Img, "bmp", 0, showImgFile);

            string typeName = path + "\\" + Name + ".ini";
            List<string> keys = new List<string>() { "TemplateName", "TemplateType" };
            List<string> values = new List<string>() { Name, Type.ToString() };
            IniHelper.AddSectionWithKeyValues("Info", keys, values, typeName);

            string shapeMatchParamFile = path + "\\" + Name + "_MatchParam.ini";
            switch (this.Type)
            {
                case TemplateType.NCC:
                    NccMatchParams nccMatchParams = _nccMatchParams;
                    nccMatchParams.Save(shapeMatchParamFile, "MatchParam");
                    HOperatorSet.WriteNccModel(HvModelID, fileName);
                    break;
                case TemplateType.SHAPE:
                    _shapeMatchParams.Save(shapeMatchParamFile, "MatchParam");
                    HOperatorSet.WriteShapeModel(HvModelID, fileName);
                    break;
                case TemplateType.SHAPEXLD:
                    _shapeMatchParams.Save(shapeMatchParamFile, "MatchParam");
                    HOperatorSet.WriteShapeModel(HvModelID, fileName);
                    break;
                default:
                    break;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载模板信息
        /// </summary>
        /// <param name="dir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Load(string dir)
        {
            string path = dir + "\\" + Name + "\\";

            string templateRegionFilename = path + "\\" + Name + "_TemplateRegion.hobj";
            if (!File.Exists(templateRegionFilename))
            {
                return Errortype.READ_TEMPLATEREGION_ERROR;
            }

            HOperatorSet.ReadRegion(out _templateRegion, templateRegionFilename);
            string maskRegionFilename = path + "\\" + Name + "_maskRegion.hobj";
            if (!File.Exists(maskRegionFilename))
            {
                return Errortype.READ_TEMPLATEMASKREGION_ERROR;
            }

            HOperatorSet.ReadRegion(out _maskRegion, maskRegionFilename);

            string baseCenterPointFile = path + "\\" + Name + "_BaseCenterPoint.ini";
            CenterPoint = new Point();
            CenterPoint.Load(baseCenterPointFile, "BaseCenterPoint");

            string showImgFile = path + "\\" + Name + "_ShowImg.bmp";
            HOperatorSet.ReadImage(out HObject showImg, showImgFile);
            Img = showImg.Clone();
            showImg.Dispose();

            string fileName = path + "\\" + Name;
            if (!File.Exists(fileName))
            {
                return Errortype.READ_TEMPLATE_ERROR;
            }

            string matchParamFile = path + "\\" + Name + "_MatchParam.ini";
            if (!File.Exists(matchParamFile))
            {
                return Errortype.READ_TEMPLATEMATCHPARAM_ERROR;
            }

            HTuple hvModelID = null;
            switch (this.Type)
            {
                case TemplateType.NCC:
                    _nccMatchParams = new NccMatchParams();
                    NccMatchParams nccMatchParams = _nccMatchParams;
                    nccMatchParams.Load(matchParamFile, "MatchParam");
                    HOperatorSet.ReadNccModel(fileName, out hvModelID);
                    break;
                case TemplateType.SHAPE:
                    _shapeMatchParams = new ShapeMatchParams();
                    _shapeMatchParams.Load(matchParamFile, "MatchParam");
                    HOperatorSet.ReadShapeModel(fileName, out hvModelID);
                    break;
                case TemplateType.SHAPEXLD:
                    _shapeMatchParams = new ShapeMatchParams();
                    _shapeMatchParams.Load(matchParamFile, "MatchParam");
                    HOperatorSet.ReadShapeModel(fileName, out hvModelID);
                    break;
                default:
                    break;
            }

            HvModelID = hvModelID;

            return Errortype.OK;
        }

        /// <summary>
        /// 拷贝
        /// </summary>
        /// <param name="name">拷贝后的名字</param>
        /// <returns>拷贝后的对象</returns>
        public Template Clone(string name)
        {
            Template templateClone = new Template(name, this.Type);
            if (this.HvModelID != null)
            {
                templateClone.HvModelID = this.HvModelID.Clone();
            }

            if (this.CenterPoint != null)
            {
                templateClone.CenterPoint = this.CenterPoint.Clone();
            }

            if (this.Img != null && this.Img.IsInitialized())
            {
                templateClone.Img = this.Img.Clone();
            }

            if (this.TemplateRegion != null && this.TemplateRegion.IsInitialized())
            {
                templateClone._templateRegion = this.TemplateRegion.Clone();
            }

            if (this.MaskRegion != null && this.MaskRegion.IsInitialized())
            {
                templateClone._maskRegion = this.MaskRegion.Clone();
            }

            if (this._shapeMatchParams != null)
            {
                templateClone._shapeMatchParams = this._shapeMatchParams.Clone();
            }

            if (this._nccMatchParams != null)
            {
                templateClone._nccMatchParams = this._nccMatchParams.Clone();
            }

            return templateClone;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            if (_templateRegion != null && _templateRegion.IsInitialized())
            {
                _templateRegion.Dispose();
            }

            if (_maskRegion != null && _maskRegion.IsInitialized())
            {
                _maskRegion.Dispose();
            }

            if (Img != null && Img.IsInitialized())
            {
                Img.Dispose();
            }

            if (HvModelID != null)
            {
                switch (this.Type)
                {
                    case TemplateType.NCC:
                        HOperatorSet.ClearNccModel(HvModelID);
                        break;
                    case TemplateType.SHAPE:
                        HOperatorSet.ClearShapeModel(HvModelID);
                        break;
                    default:
                        break;
                }

                HvModelID = null;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 记录score分数
        /// </summary>
        /// <param name="score">分数</param>
        /// <param name="templateType">模板类型</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype LogMatchScore(HTuple score, TemplateType templateType)
        {
            if (ComAlgo.SaveFlg("TemplateMatchScore", out int days))
            {
                string path = @"D:\Alg\TemplateMatchScore";
                string fileName = "TemplateMatchScore.txt";
                string sep = " ";
                string time = ComAlgo.GetDateTime();
                string txt = time + sep + templateType.ToString() + sep;

                for (int i = 0; i < score.TupleLength(); i++)
                {
                    txt += score[i].D.ToString() + sep;
                }

                ComAlgo.LogText(txt, path, fileName, days);
            }

            return Errortype.OK;
        }
    }
}


