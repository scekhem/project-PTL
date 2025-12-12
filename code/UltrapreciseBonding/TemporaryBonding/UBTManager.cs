using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;

using DataStruct;
using IniFileHelper;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.TemplateMatch;
using OpenCvSharp.Flann;

namespace UltrapreciseBonding.TemporaryBonding
{
    /// <summary>
    /// UBT接口管理类
    /// </summary>
    public static class UBTManager
    {
        private static string _dataDir = "./CalibData";
        private static CalibTableInfo _calibTableInfo = new CalibTableInfo();
        private static GrabEdgeInfo _grabEdgeInfo = new GrabEdgeInfo();     // 二代抓边预设信息（宽卡尺圆的弧边位置）
        private static List<CalibCoord> _calibList = new List<CalibCoord>();

        private static List<string> _cameraNames = new List<string>();
        private static string _axisName = String.Empty;
        private static Point _chuckRotateCenter = null;

        private static Dictionary<string, List<Point>> _grabEdgeAtPix = new Dictionary<string, List<Point>>();  // 抓取到的各边像素坐标（用于手动抓边和标定校验）
        private static Point _grabNotchAtReal = null;   // 抓取到的notch真值坐标，用于手动定位

        private static List<Point> _motionRulerPoints = new List<Point>();
        private static List<Point> _motionRealPoints = new List<Point>();

        private static int _axisDirectX = 1;
        private static int _axisDirectY = 1;
        private static int _axisDirectT = 1;

        /// <summary>
        /// Gets the user's _chuckRotateCenter
        /// </summary>
        public static Point RotateCenter { get => _chuckRotateCenter; }

        /// <summary>
        /// Gets wafer notch 模板名称
        /// </summary>
        public static string WaferTemplateName { get => _grabEdgeInfo.WaferNotchTemplateName; }

        /// <summary>
        /// Gets glass notch 模板名称
        /// </summary>
        public static string GlassTemplateName { get => _grabEdgeInfo.GlassNotchTemplateName; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="calibItemNames">标定项名称</param>
        /// <param name="waferNotchTemplateName">wafer notch模板名称</param>
        /// <param name="glassNotchTemplateName">glass notch模板名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Init(List<string> calibItemNames, string waferNotchTemplateName = null, string glassNotchTemplateName = null)
        {
            _motionRealPoints.Clear();
            _motionRulerPoints.Clear();
            foreach (var itemName in calibItemNames)
            {
                CalibCoord newCalib = _calibList.Find(e => e.ItemName == itemName);
                if (newCalib != null)
                {
                    newCalib = new CalibCoord(itemName);
                }
                else
                {
                    _calibList.Add(new CalibCoord(itemName));
                }

                if (_grabEdgeAtPix.ContainsKey(itemName))
                {
                    _grabEdgeAtPix[itemName].Clear();
                }
                else if (itemName.Contains("Camera"))
                {
                    List<Point> emptyEdge = new List<Point>();
                    _grabEdgeAtPix.Add(itemName, emptyEdge);
                }

                if (!_cameraNames.Contains(itemName))
                {
                    if (itemName.Contains("Camera"))
                    {
                        _cameraNames.Add(itemName);
                    }
                    else
                    {
                        _axisName = itemName;
                    }
                }
            }

            _calibTableInfo.SetCameraNames(_cameraNames);
            _grabEdgeInfo.SetCameraNames(_cameraNames);
            if (_grabEdgeInfo.EnableMotionClib > 0)
            {
                _calibList.Add(new CalibCoord("XYMotion"));
            }

            if (waferNotchTemplateName != null)
            {
                _grabEdgeInfo.WaferNotchTemplateName = waferNotchTemplateName;
            }

            if (glassNotchTemplateName != null)
            {
                _grabEdgeInfo.GlassNotchTemplateName = glassNotchTemplateName;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 更新标定板信息
        /// </summary>
        /// <param name="info">标定板信息类</param>
        /// <returns>OK：更新完成</returns>
        public static Errortype UpdateCalibTableInfo(CalibTableInfo info)
        {
            _calibTableInfo = info;
            return _calibTableInfo.Save(_dataDir);
        }

        /// <summary>
        /// 更新抓边预设信息
        /// </summary>
        /// <param name="info">抓边信息类</param>
        /// <returns>OK：更新完成</returns>
        public static Errortype UpdateGrabEdgeInfo(GrabEdgeInfo info)
        {
            _grabEdgeInfo = info;
            return _grabEdgeInfo.Save(_dataDir);
        }

        /// <summary>
        /// 更新抓边预设信息
        /// </summary>
        /// <param name="grabEdgeBlack">抓边是否是黑边</param>
        /// <returns>OK：更新完成</returns>
        public static Errortype UpdateGLassEdgeColor(bool grabEdgeBlack)
        {
            if (grabEdgeBlack)
            {
                _grabEdgeInfo.GlassGrabWidth = Math.Abs(_grabEdgeInfo.GlassGrabWidth) * -1;
            }
            else
            {
                _grabEdgeInfo.GlassGrabWidth = Math.Abs(_grabEdgeInfo.GlassGrabWidth);
            }

            return _grabEdgeInfo.Save(_dataDir);
        }

        /// <summary>
        /// 更新抓边预设信息
        /// </summary>
        /// <param name="grabEdgeBlack">抓边是否是黑边</param>
        /// <returns>OK：更新完成</returns>
        public static Errortype UpdateWaferEdgeColor(bool grabEdgeBlack)
        {
            if (grabEdgeBlack)
            {
                _grabEdgeInfo.WaferGrabWidth = Math.Abs(_grabEdgeInfo.WaferGrabWidth) * -1;
            }
            else
            {
                _grabEdgeInfo.WaferGrabWidth = Math.Abs(_grabEdgeInfo.WaferGrabWidth);
            }

            return _grabEdgeInfo.Save(_dataDir);
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype Release()
        {
            _grabEdgeAtPix.Clear();
            _axisName = String.Empty;
            _cameraNames.Clear();
            _grabNotchAtReal = null;
            _motionRealPoints.Clear();
            _motionRulerPoints.Clear();
            _chuckRotateCenter = null;
            _calibList.Clear();
            _calibList = new List<CalibCoord>();
            TemplateManager.Release();
            return Errortype.OK;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="dataDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype SaveData(string dataDir)
        {
            foreach (var calibitem in _calibList)
            {
                var ret = calibitem.Save(dataDir + "/");
                if (ret != Errortype.OK)
                {
                    continue;
                }
            }

            if (_chuckRotateCenter is null)
            {
                return Errortype.UBT_ROTATE_CENTER_CALIB_INCOMPLETE;
            }

            string fullFileName = dataDir + "/_RotateCenter.ini";
            if (File.Exists(fullFileName))
            {
                File.Delete(fullFileName);
            }

            List<string> keys = new List<string> { "RotateCenterX", "RotateCenterY" };
            List<string> value = new List<string> { _chuckRotateCenter.X.ToString(), _chuckRotateCenter.Y.ToString() };
            IniHelper.AddSectionWithKeyValues("Info", keys, value, fullFileName);
            return Errortype.OK;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="dataDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype LoadData(string dataDir)
        {
            _dataDir = dataDir;
            LoadAxisParam(_dataDir);
            int loadErrorCount = 0;
            var ret = _calibTableInfo.Load(dataDir);  // 载入标定板信息
            if (ret != Errortype.OK)
            {
                loadErrorCount += 1;
            }

            ret = _grabEdgeInfo.Load(dataDir);
            if (ret != Errortype.OK)
            {
                loadErrorCount += 1;
            }

            for (int itemId = 0; itemId < _calibList.Count(); itemId++)
            {
                ret = _calibList[itemId].Load(dataDir);
                if (ret != Errortype.OK)
                {
                    loadErrorCount += 1;
                }
            }

            string fullFileName = dataDir + "_RotateCenter.ini";
            if (!File.Exists(fullFileName))
            {
                ret = Errortype.UBT_ROTATE_CENTER_FILE_NOT_EXIST;
            }
            else
            {
                if (!IniHelper.ExistSection("Info", fullFileName))
                {
                    ret = Errortype.UBT_ROTATE_CENTER_FILE_NOT_EXIST;
                }
                else
                {
                    string[] keys = null;
                    string[] values = null;
                    IniHelper.GetAllKeyValues("Info", out keys, out values, fullFileName);
                    if (values.Length != 2)
                    {
                        ret = Errortype.UBT_ROTATE_CENTER_FILE_ERROR;
                    }
                    else
                    {
                        _chuckRotateCenter = new Point();
                        _chuckRotateCenter.X = Convert.ToDouble(values[0]);
                        _chuckRotateCenter.Y = Convert.ToDouble(values[1]);
                    }
                }
            }

            if (ret != Errortype.OK)
            {
                loadErrorCount += 1;
            }

            string modelFile = dataDir + "/NotchModel.bmp";
            if (System.IO.File.Exists(modelFile))
            {
                Camera modelImage = new Camera(modelFile);
                CreateNotchTemplate(_grabEdgeInfo.WaferNotchTemplateName, modelImage);
                CreateNotchTemplate(_grabEdgeInfo.GlassNotchTemplateName, modelImage);
            }

            modelFile = dataDir + "/calibDie.bmp";
            if (System.IO.File.Exists(modelFile))
            {
                Camera modelImage = new Camera(modelFile);
                CreateCalibWaferDieTemplate(_calibTableInfo.CalibDieTempName, modelImage);
            }
            else
            {
                loadErrorCount += 1;
            }

            ret = TemplateManager.Load(_dataDir, _grabEdgeInfo.WaferNotchTemplateName);
            if (ret != Errortype.OK)
            {
                return Errortype.UBT_WAFER_NOTCH_TEMPLATE_LOAD_ERROR;
            }

            ret = TemplateManager.Load(_dataDir, _grabEdgeInfo.GlassNotchTemplateName);
            if (ret != Errortype.OK)
            {
                return Errortype.UBT_GLASS_NOTCH_TEMPLATE_LOAD_ERROR;
            }

            if (loadErrorCount > 0)
            {
                return Errortype.UBT_CALIB_DATA_NUM_ERROR;
            }

            return ret;
        }

        private static Errortype CreateCalibWaferDieTemplate(string templateName, Camera templateImage)
        {
            NccTemplateParams templateParams = new NccTemplateParams();
            NccTemplateParams nccTemplateParams = new NccTemplateParams();
            nccTemplateParams.AngleStart = -Math.PI;
            nccTemplateParams.AngleExtent = Math.PI * 2;

            NccMatchParams nccMatchParams = new NccMatchParams();
            nccTemplateParams.AngleStart = -Math.PI;
            nccTemplateParams.AngleExtent = Math.PI * 2;
            nccMatchParams.MinScore = 0.8;
            nccMatchParams.NumMatches = 50;
            nccMatchParams.Pyramid = 0;
            nccMatchParams.MaxOverlap = 0.5;

            var templateRegion = new Region()
            { Rectangle1 = new Rectangle1(0, 0, templateImage.Width - 1, templateImage.Height - 1) };

            var ret = TemplateManager.Create(templateName, templateImage, nccTemplateParams, nccMatchParams, templateRegion, null,
                TemplateType.NCC);

            if (ret == Errortype.OK)
            {
                ret = TemplateManager.Save(_dataDir, templateName);
            }

            return ret;
        }

        /// <summary>
        /// 创建notch模板接口
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="templateImage">模板图像</param>
        /// <param name="templateRegion">模板的区域，默认null使用整张templateImage</param>
        /// <param name="minScore">模板的匹配最低得分，范围0.2-1.0，默认0.7分</param>
        /// <param name="useNcc">是否创建Ncc模板</param>
        /// <param name="isWaferTemplate">是否时wafer模板，false：glass模板</param>
        /// <returns>ok：创建完成</returns>
        public static Errortype CreateNotchTemplate(string templateName, Camera templateImage, Rectangle1 templateRegion = null, double minScore = 0.9, bool useNcc = false, bool isWaferTemplate = true)
        {
            if ((templateName == null) || (templateName == string.Empty))
            {
                return Errortype.UBT_TEMPLATE_NAME_NULL;
            }

            if ((templateImage == null) || templateImage.Height < 1)
            {
                return Errortype.INPUT_IMAGE_NULL;
            }

            if (isWaferTemplate)
            {
                _grabEdgeInfo.WaferNotchTemplateName = templateName;
                _grabEdgeInfo.GlassNotchTemplateName = templateName;    // 若不创建glass模板，则默认使用 wafer 模板定位 glass notch
            }
            else
            {
                _grabEdgeInfo.GlassNotchTemplateName = templateName;
            }

            _grabEdgeInfo.Save(_dataDir);

            Region tempRegion = new Region();
            if (templateRegion == null)
            {
                tempRegion = new Region()
                { Rectangle1 = new Rectangle1(0, 0, templateImage.Width - 1, templateImage.Height - 1) };
            }
            else
            {
                tempRegion = new Region()
                { Rectangle1 = new Rectangle1(templateRegion.Start_X, templateRegion.Start_Y, templateRegion.End_X, templateRegion.End_Y) };
            }

            Camera camImage = templateImage.Clone();
            HObject hImage = camImage.GenHObject();
            HObject rectRegion = tempRegion.GenRegion();
            HOperatorSet.ReduceDomain(hImage, rectRegion, out HObject tempInRegion);
            HOperatorSet.CropDomain(tempInRegion, out HObject cropImage);
            rectRegion.Dispose();
            tempInRegion.Dispose();
            hImage.Dispose();

            HOperatorSet.GetImageSize(cropImage, out HTuple imgWidth, out HTuple imgHeight);
            tempRegion = new Region()
            { Rectangle1 = new Rectangle1(0, 0, imgWidth - 1, imgHeight - 1) };

            var ret = Errortype.UNKNOW_ERROR;
            if (useNcc)
            {
                NccTemplateParams nccTemplateParams = new NccTemplateParams();
                nccTemplateParams.AngleStart = -0.39;
                nccTemplateParams.AngleExtent = 0.79;

                NccMatchParams nccMatchParams = new NccMatchParams();
                nccMatchParams.AngleStart = -0.02;
                nccMatchParams.AngleExtent = 0.04;
                nccMatchParams.MinScore = minScore;
                nccMatchParams.NumMatches = 1;
                nccMatchParams.Pyramid = 0;
                nccMatchParams.MaxOverlap = 0.5;

                templateImage.Dispose();
                templateImage = new Camera(cropImage);
                ret = TemplateManager.Create(templateName, templateImage, nccTemplateParams, nccMatchParams, tempRegion, null,
                    TemplateType.NCC);
            }
            else
            {
                ShapeTemplateParams shapeTemplateParams = new ShapeTemplateParams();
                shapeTemplateParams.AngleStart = -0.39;
                shapeTemplateParams.AngleExtent = 0.79;

                ShapeMatchParams shapeMatchParams = new ShapeMatchParams();
                shapeMatchParams.AngleStart = -0.02;
                shapeMatchParams.AngleExtent = 0.04;
                shapeMatchParams.Greediness = 0.9;
                shapeMatchParams.MinScore = minScore;
                shapeMatchParams.NumMatches = 1;
                shapeMatchParams.Pyramid = 0;
                shapeMatchParams.MaxOverlap = 0.5;

                //HOperatorSet.BinaryThreshold(cropImage, out HObject lightRegion, "max_separability", "light", out _);
                //HOperatorSet.Connection(lightRegion, out HObject connectedRegions);
                //HOperatorSet.SelectShape(connectedRegions, out HObject selectedRegions, "convexity", "and", 0.2, 0.6);
                //HOperatorSet.DilationCircle(selectedRegions, out HObject regionDilation, 5);

                //HOperatorSet.CountObj(regionDilation, out HTuple validRegionNum);
                //if (validRegionNum < 1)
                //{
                //    regionDilation.Dispose();
                //    selectedRegions.Dispose();
                //    connectedRegions.Dispose();
                //    lightRegion.Dispose();
                //    cropImage.Dispose();
                //    return Errortype.UBT_NOTCH_MODLE_AREA_ERROR;
                //}

                //HOperatorSet.PaintRegion(cropImage, cropImage, out HObject imagePaint, 0, "fill");
                //HOperatorSet.PaintRegion(regionDilation, imagePaint, out HObject imageBinary, 255, "fill");

                //imagePaint.Dispose();
                //regionDilation.Dispose();
                //selectedRegions.Dispose();
                //connectedRegions.Dispose();
                //lightRegion.Dispose();

                //templateImage.Dispose();
                //templateImage = new Camera(imageBinary);
                //imageBinary.Dispose();
                templateImage.Dispose();
                templateImage = new Camera(cropImage);
                ret = TemplateManager.Create(templateName, templateImage, shapeTemplateParams, shapeMatchParams, tempRegion, null,
                    TemplateType.SHAPE);
            }

            cropImage.Dispose();
            if (ret == Errortype.OK)
            {
                ret = TemplateManager.Save(_dataDir, templateName);
            }

            return ret;
        }

        /// <summary>
        /// 切换wafer的notch模板
        /// </summary>
        /// <param name="templateName">切换的模板名称</param>
        /// <returns>ok：切换成功，其他：模板文件不存在</returns>
        public static Errortype ChangeWaferNotchTemplate(string templateName)
        {
            _grabEdgeInfo.WaferNotchTemplateName = templateName;
            return Errortype.OK;
        }

        /// <summary>
        /// 切换glass的notch模板
        /// </summary>
        /// <param name="templateName">切换的模板名称</param>
        /// <returns>ok：切换成功，其他：模板文件不存在</returns>
        public static Errortype ChangeGlassNotchTemplate(string templateName)
        {
            _grabEdgeInfo.GlassNotchTemplateName = templateName;
            return Errortype.OK;
        }

        /// <summary>
        /// 获取当前的模板名称
        /// </summary>
        /// <param name="waferTemplateName">wafer 模板名称</param>
        /// <param name="glassTemplateName">glass 模板名称</param>
        /// <returns>ok：获取成功</returns>
        public static Errortype GetCurrentNotchTemplateName(out string waferTemplateName, out string glassTemplateName)
        {
            waferTemplateName = _grabEdgeInfo.WaferNotchTemplateName;
            glassTemplateName = _grabEdgeInfo.GlassNotchTemplateName;
            return Errortype.OK;
        }

        /// <summary>
        /// wafer notch模板匹配度测试
        /// </summary>
        /// <param name="checkImage">测试图片</param>
        /// <param name="matchScore">匹配分数</param>
        /// <param name="resPix">匹配结果图像</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype CheckWaferNotchTemplate(Camera checkImage, out double matchScore, out Point resPix)
        {
            matchScore = 0.0;
            resPix = null;
            string templateName = _grabEdgeInfo.WaferNotchTemplateName;

            Camera camImage = checkImage.Clone();
            HObject himage = camImage.GenHObject();
            HOperatorSet.MeanImage(himage, out HObject imageToMatch, 7, 7);

            Camera cameraToMatch = new Camera(imageToMatch);
            var ret = TemplateManager.Match(templateName, cameraToMatch, null, out double[] notchRows, out double[] notchCols, out double[] angles, out double[] scales, out double[] scores);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            matchScore = scores[0];
            imageToMatch.Dispose();
            cameraToMatch.Dispose();

            if (scores.Length >= 1)
            {
                // 选取得分最高的结果
                HOperatorSet.TupleSortIndex(scores, out HTuple scoreIndex);
                HOperatorSet.TupleInverse(scoreIndex, out HTuple indexMaxToMin);
                HOperatorSet.TupleSelect(notchCols, indexMaxToMin[0], out HTuple selectedNotchCol);
                HOperatorSet.TupleSelect(notchRows, indexMaxToMin[0], out HTuple selectedNotchRow);
                resPix = new Point(selectedNotchCol.D, selectedNotchRow.D);

                himage.Dispose();
                return Errortype.OK;
            }

            himage.Dispose();
            return Errortype.UBT_NOTCH_SEARCH_FAILED;
        }

        /// <summary>
        /// glass notch模板匹配度测试
        /// </summary>
        /// <param name="checkImage">测试图片</param>
        /// <param name="matchScore">匹配分数</param>
        /// <param name="resPix">匹配结果图像</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype CheckGlassNotchTemplate(Camera checkImage, out double matchScore, out Point resPix)
        {
            matchScore = 0.0;
            resPix = null;
            string templateName = _grabEdgeInfo.GlassNotchTemplateName;

            Camera camImage = checkImage.Clone();
            HObject himage = camImage.GenHObject();

            HOperatorSet.MeanImage(himage, out HObject imageToMatch, 7, 7);

            Camera cameraToMatch = new Camera(imageToMatch);
            var ret = TemplateManager.Match(templateName, cameraToMatch, null, out double[] notchRows, out double[] notchCols, out double[] angles, out double[] scales, out double[] scores);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            matchScore = scores[0];
            imageToMatch.Dispose();
            cameraToMatch.Dispose();

            if (scores.Length >= 1)
            {
                // 选取得分最高的结果
                HOperatorSet.TupleSortIndex(scores, out HTuple scoreIndex);
                HOperatorSet.TupleInverse(scoreIndex, out HTuple indexMaxToMin);
                HOperatorSet.TupleSelect(notchCols, indexMaxToMin[0], out HTuple selectedNotchCol);
                HOperatorSet.TupleSelect(notchRows, indexMaxToMin[0], out HTuple selectedNotchRow);
                resPix = new Point(selectedNotchCol.D, selectedNotchRow.D);

                himage.Dispose();
                return Errortype.OK;
            }

            himage.Dispose();
            return Errortype.UBT_NOTCH_SEARCH_FAILED;
        }

        /// <summary>
        /// 获取notch模板图
        /// </summary>
        /// <param name="templateName">输入需要查看的notch模板名称</param>
        /// <param name="templateImage">输出wafer notch模板图片</param>
        /// <returns>ok：获取成功，其他失败</returns>
        public static Errortype ViewNotchTemplate(string templateName, out Camera templateImage)
        {
            templateImage = new Camera();
            var ret = TemplateManager.Load(_dataDir, templateName);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = TemplateManager.GetTemplateImg(templateName, out templateImage);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (templateImage.Channel > 1)
            {
                var himage = templateImage.GenHObject();
                HOperatorSet.Rgb1ToGray(himage, out HObject himgGray);
                templateImage.Dispose();
                templateImage = new Camera(himgGray);
                himage.Dispose();
                himgGray.Dispose();
            }

            return ret;
        }

        /// <summary>
        /// 生成轴运动系标定点阵
        /// </summary>
        /// <param name="axisPose">输出标定的轴坐标</param>
        /// <param name="origionX">设定标定的起点位置X轴坐标</param>
        /// <param name="origionY">设定标定的起点位置Y轴坐标</param>
        /// <param name="moveRage">设定标定运动区域范围</param>
        /// <param name="step">设定标定运动步长</param>
        /// <returns>ok：生成成功</returns>
        public static Errortype GenCalibMotionAxis(out List<Point> axisPose, double origionX = 0.0, double origionY = 0.0, double moveRage = 1.0, double step = 0.2)
        {
            return UBTAlogo.GenCalibRulerPoints(out axisPose, origionX, origionY, moveRage, step);
        }

        #region 自动标定

        /// <summary>
        /// 真值标定，wafer假片方法
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="tableImage">标定wafer图像</param>
        /// <param name="labelRealMark">标注像素</param>
        /// <returns>ok:标定完成</returns>
        public static Errortype CalibSensorRealAuto(string cameraName, Camera tableImage, out List<Point> labelRealMark)
        {
            GrabEdgeInfo info = _grabEdgeInfo;
            info.WaferGrabWidth = Math.Abs(info.WaferGrabWidth);
            labelRealMark = new List<Point>();
            CalibCoord sensorCalib = _calibList.Find(e => e.ItemName == cameraName);
            if (sensorCalib == null)
            {
                return Errortype.CALIB_ITEM_NAME_NULL;
            }

            var ret = UBTAlogo.CalcEdgePixGen2(info, cameraName, tableImage, out List<Point> edgeAtPix, null, 0, 0.1, false);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _grabEdgeAtPix[cameraName] = edgeAtPix;
            var res = ComAlgo.FitCircle(edgeAtPix, out Point waferCenterXY, out double waferRadius, out List<double> residual);
            HOperatorSet.TupleDeviation(residual.ToArray(), out HTuple dev);
            if (res != Errortype.OK)
            {
                return res;
            }

            //_grabEdgeInfo.SetEdgeRingCenter(cameraName, waferCenterXY);
            //_grabEdgeInfo.SetEdgeRingRadius(cameraName, waferRadius); 暂时不更新，保持使用chuck边界设定
            ret = UBTAlogo.CalcCalibWaferDie(_calibTableInfo, cameraName, tableImage, waferCenterXY, out List<Point> dieCenterPix, out List<Point> dieRealId, out Point featurePix);
            if (res != Errortype.OK)
            {
                return res;
            }

            labelRealMark.Add(featurePix);
            ret = sensorCalib.CalibDo(dieCenterPix, dieRealId, TransType.AffineTrans);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (ComAlgo.SaveFlg("saveCalibData", out int days))
            {
                string path = @"D:\Alg\";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string text = string.Empty;
                string sep = " ";
                foreach (var pix in labelRealMark)
                {
                    text += cameraName + sep + pix.X.ToString() + sep + pix.Y.ToString() + sep;
                }

                string fileName = path + "\\calibRecord.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();

                fileName = cameraName;
                labelRealMark.InsertRange(0, edgeAtPix);
                UBTAlogo.SaveImg(tableImage, labelRealMark, fileName);
            }

            sensorCalib.IsCalibed = true;
            return Errortype.OK;
        }

        /// <summary>
        /// XY轴自动标定，wafer假片方法
        /// </summary>
        /// <param name="tableImagesOrigion">移动前标定板在各相机图像</param>
        /// <param name="tableImagesX">X轴移动后标定板在各相机的图像</param>
        /// <param name="tableImagesY">Y轴移动后标定板在各相机的图像</param>
        /// <param name="axisOrigin">移动前轴坐标</param>
        /// <param name="axisX">X轴移动后轴坐标</param>
        /// <param name="axisY">Y轴移动后轴坐标</param>
        /// <param name="angleDegX">输出轴真值X夹角</param>
        /// <param name="angleDegY">输出轴真值Y夹角</param>
        /// <param name="scaleRulerRealX">输出轴真值X比例</param>
        /// <param name="scaleRulerRealY">输出轴真值Y比例</param>
        /// <returns>ok：标定完成</returns>
        public static Errortype CalibXYAuto(List<Camera> tableImagesOrigion, List<Camera> tableImagesX,
            List<Camera> tableImagesY, Point axisOrigin, Point axisX, Point axisY, out double angleDegX,
            out double angleDegY, out double scaleRulerRealX, out double scaleRulerRealY)
        {
            GrabEdgeInfo info = _grabEdgeInfo;
            info.WaferGrabWidth = Math.Abs(info.WaferGrabWidth);
            scaleRulerRealX = 0.0;
            scaleRulerRealY = 0.0;
            angleDegX = 0.0;
            angleDegY = 0.0;
            if (tableImagesX.Count != tableImagesOrigion.Count)
            {
                return Errortype.UBT_CALIB_DATA_NUM_ERROR;
            }

            if (tableImagesX.Count != _cameraNames.Count)
            {
                return Errortype.UBT_CALIB_DATA_NUM_ERROR;
            }

            CalibCoord axisCalib = _calibList.Find(e => e.ItemName == _axisName);
            if (axisCalib == null)
            {
                return Errortype.CALIB_ITEM_NAME_NULL;
            }

            Dictionary<string, List<Point>> origionEdgeAtPix = new Dictionary<string, List<Point>>();
            Dictionary<string, List<Point>> poseXEdgeAtPix = new Dictionary<string, List<Point>>();
            Dictionary<string, List<Point>> poseYEdgeAtPix = new Dictionary<string, List<Point>>();

            List<Point> origionMark = new List<Point>();
            List<Point> poseXMark = new List<Point>();
            List<Point> poseYMark = new List<Point>();

            var ret = Errortype.OK;
            for (int i = 0; i < _cameraNames.Count; i++)
            {
                CalibCoord sensorCalib = _calibList.Find(e => e.ItemName == _cameraNames[i]);
                if (sensorCalib == null)
                {
                    return Errortype.CALIB_ITEM_NAME_NULL;
                }

                if (!sensorCalib.IsCalibed)
                {
                    return Errortype.UBT_CAMERA_REAL_CALIB_INCOMPLETE;
                }

                #region 移动前-------------------------------------------
                ret = UBTAlogo.CalcEdgePixGen2(info, _cameraNames[i], tableImagesOrigion[i], out List<Point> edgeAtPix, null, 0, 0.1, false);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = ComAlgo.FitCircle(edgeAtPix, out Point waferCenterOrigion, out _, out List<double> residual);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = UBTAlogo.CalcCalibWaferDie(_calibTableInfo, _cameraNames[i], tableImagesOrigion[i], waferCenterOrigion, out _, out _, out Point featurePix);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = GetRealByPix(_cameraNames[i], featurePix, out Point featureAtReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                origionMark.Add(featureAtReal);
                origionEdgeAtPix.Add(_cameraNames[i], edgeAtPix);

                UBTAlogo.SaveImg(tableImagesOrigion[i], new List<Point> { featurePix }, "/" + _cameraNames[i] + ".bmp");
                #endregion

                #region X移动后------------------------------------------
                ret = UBTAlogo.CalcEdgePixGen2(info, _cameraNames[i], tableImagesX[i], out List<Point> edgeAtPixPosX, null, 0, 0.1, false, 320);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                //string fileName = "AlignWafer/" + _cameraNames[i];
                //UBTAlogo.SaveImg(tableImagesX[i], edgeAtPixPosX, fileName);
                ret = ComAlgo.FitCircle(edgeAtPixPosX, out Point waferCenterPoseX, out _, out residual);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = UBTAlogo.CalcCalibWaferDie(_calibTableInfo, _cameraNames[i], tableImagesX[i], waferCenterPoseX, out _, out _, out Point featurePixPoseX);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = GetRealByPix(_cameraNames[i], featurePixPoseX, out Point featurePoseXAtReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                poseXEdgeAtPix.Add(_cameraNames[i], edgeAtPixPosX);
                poseXMark.Add(featurePoseXAtReal);

                UBTAlogo.SaveImg(tableImagesX[i], new List<Point> { featurePixPoseX }, "/" + _cameraNames[i] + "_x.bmp");
                #endregion

                #region Y移动后------------------------------------------
                ret = UBTAlogo.CalcEdgePixGen2(info, _cameraNames[i], tableImagesY[i], out List<Point> edgeAtPixPosY, null, 0, 0.1, false, 320);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = ComAlgo.FitCircle(edgeAtPixPosY, out Point waferCenterPoseY, out _, out residual);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = UBTAlogo.CalcCalibWaferDie(_calibTableInfo, _cameraNames[i], tableImagesY[i], waferCenterPoseY, out _, out _, out Point featurePixPoseY);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = GetRealByPix(_cameraNames[i], featurePixPoseY, out Point featurePoseYAtReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                poseYEdgeAtPix.Add(_cameraNames[i], edgeAtPixPosY);
                poseYMark.Add(featurePoseYAtReal);

                UBTAlogo.SaveImg(tableImagesY[i], new List<Point> { featurePixPoseY }, "/" + _cameraNames[i] + "_y.bmp");
                #endregion
            }

            // 检查相机图像真值正确性
            _grabEdgeAtPix = origionEdgeAtPix;
            ret = CalibSensorCheck(out double residualA, out Point centerOrigion);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _grabEdgeAtPix = poseXEdgeAtPix;
            ret = CalibSensorCheck(out double residualB, out Point centerXPose);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _grabEdgeAtPix = poseYEdgeAtPix;
            ret = CalibSensorCheck(out double residualC, out Point centerYPose);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            #region 标定计算

            HTuple origionXCol = new HTuple(centerOrigion.X);
            HTuple origionXRow = new HTuple(centerOrigion.Y);
            HTuple origionYCol = new HTuple(centerOrigion.X);
            HTuple origionYRow = new HTuple(centerOrigion.Y);
            HTuple xCol = new HTuple(centerXPose.X);
            HTuple xRow = new HTuple(centerXPose.Y);
            HTuple yCol = new HTuple(centerYPose.X);
            HTuple yRow = new HTuple(centerYPose.Y);
            HOperatorSet.LineOrientation(origionXRow, origionXCol, xRow, xCol, out HTuple xPhi);    // 一三象限为正

            HOperatorSet.AngleLx(origionYRow, origionYCol, yRow, yCol, out HTuple yPhi);    // anglelx 输出逆时针为正
            double xAxisRad = xPhi.D;

            double yAxisRad = yPhi.D - xPhi.D + (Math.PI / 2); // 注意T标定板坐标中心在左上角，真值Y方向朝下
            HOperatorSet.TupleDeg(xAxisRad, out HTuple xDeg);
            HOperatorSet.TupleDeg(yAxisRad, out HTuple yDeg);

            double axisDistanceX = axisOrigin.DistanceTo(axisX);
            double axisDistanceY = axisOrigin.DistanceTo(axisY);
            double realDistanceX = new Point(origionXCol.D, origionXRow.D).DistanceTo(new Point(xCol.D, xRow.D));
            double realDistanceY = new Point(origionYCol.D, origionYRow.D).DistanceTo(new Point(yCol.D, yRow.D));

            //scaleRulerRealX = axisDistanceX / realDistanceX;
            //scaleRulerRealY = axisDistanceY / realDistanceY;
            //angleDegX = xDeg.D;
            //angleDegY = yDeg.D;
            scaleRulerRealX = 1.0;
            scaleRulerRealY = 1.0;
            angleDegX = 1.0;
            angleDegY = 0.0;
            HOperatorSet.TupleRad(angleDegX, out HTuple xRad);
            HOperatorSet.TupleRad(angleDegY, out HTuple yRad);
            xAxisRad = xRad.D;
            yAxisRad = yRad.D;

            ret = axisCalib.CalibDo(new Point(0, 0), scaleRulerRealX, scaleRulerRealY, xAxisRad, yAxisRad);
            if (ComAlgo.SaveFlg("saveCalibData", out int days))
            {
                string path = @"D:\Alg\";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string text = string.Empty;
                string sep = " ";

                text += "a " + centerOrigion.X.ToString() + sep + centerOrigion.Y.ToString() + " error:" + residualA.ToString() + sep;
                text += "b " + centerXPose.X.ToString() + sep + centerXPose.Y.ToString() + " error:" + residualB.ToString() + sep;
                text += "c " + centerYPose.X.ToString() + sep + centerYPose.Y.ToString() + " error:" + residualC.ToString() + sep;
                text += "calibValues " + scaleRulerRealX.ToString() + sep + angleDegX.ToString() + sep + scaleRulerRealY.ToString() + sep + angleDegY.ToString() + sep;

                string fileName = path + "\\calibXYRecord.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
            }

            axisCalib.IsCalibed = true;
            #endregion
            return Errortype.OK;
        }

        /// <summary>
        /// 标定旋转中心，wafer假片方法
        /// </summary>
        /// <param name="tableImagesOrigion">旋转前各相机图像</param>
        /// <param name="tableImagesRotate">旋转后各相机图像</param>
        /// <param name="rotateCenterX">输出标定的旋转中心真值X坐标</param>
        /// <param name="rotateCenterY">输出标定的旋转中心真值Y坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalibRotateAuto(List<Camera> tableImagesOrigion, List<Camera> tableImagesRotate, out double rotateCenterX, out double rotateCenterY)
        {
            GrabEdgeInfo info = _grabEdgeInfo;
            info.WaferGrabWidth = Math.Abs(info.WaferGrabWidth);
            rotateCenterX = 0.0;
            rotateCenterY = 0.0;
            if (tableImagesRotate.Count != tableImagesOrigion.Count)
            {
                return Errortype.UBT_CALIB_DATA_NUM_ERROR;
            }

            if (tableImagesRotate.Count != _cameraNames.Count)
            {
                return Errortype.UBT_CALIB_DATA_NUM_ERROR;
            }

            Dictionary<string, List<Point>> origionEdgeAtPix = _grabEdgeAtPix;
            Dictionary<string, List<Point>> rotateEdgeAtPix = _grabEdgeAtPix;

            List<Point> origionMark = new List<Point>();
            List<Point> rotateMark = new List<Point>();
            List<Point> origionMarkId = new List<Point>();
            List<Point> rotateMarkId = new List<Point>();

            var ret = Errortype.OK;
            for (int i = 0; i < _cameraNames.Count; i++)
            {
                CalibCoord sensorCalib = _calibList.Find(e => e.ItemName == _cameraNames[i]);
                if (sensorCalib == null)
                {
                    return Errortype.CALIB_ITEM_NAME_NULL;
                }

                if (!sensorCalib.IsCalibed)
                {
                    return Errortype.UBT_CAMERA_REAL_CALIB_INCOMPLETE;
                }

                #region 旋转前-------------------------------------------

                //UBTAlogo.CalcCalibTableRefMark(tableImagesOrigion[i], _calibTableInfo, out List<Point> refMarkPixOrigion, out List<Point> origionId, out List<Point> ringPixOrigion);
                ret = UBTAlogo.CalcEdgePixGen2(info, _cameraNames[i], tableImagesOrigion[i], out List<Point> edgeAtPix, null, 0, 0.1, false);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = ComAlgo.FitCircle(edgeAtPix, out Point waferCenterOrigion, out _, out List<double> residual);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = UBTAlogo.CalcCalibWaferDie(_calibTableInfo, _cameraNames[i], tableImagesOrigion[i], waferCenterOrigion, out _, out _, out Point featurePix);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                //UBTAlogo.SaveImg(tableImagesOrigion[i], refMarkPixOrigion, "./" + _cameraNames[i] + ".bmp");
                origionEdgeAtPix[_cameraNames[i]] = edgeAtPix;
                ret = GetRealByPix(_cameraNames[i], featurePix, out Point featureAtReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                origionMark.Add(featureAtReal);
                #endregion

                #region 旋转后------------------------------------------

                //UBTAlogo.CalcCalibTableRefMark(tableImagesRotate[i], _calibTableInfo, out List<Point> refMarkPixRotate, out List<Point> rotateId, out List<Point> ringPixRotate);
                ret = UBTAlogo.CalcEdgePixGen2(info, _cameraNames[i], tableImagesRotate[i], out List<Point> edgeAtPixRotate, null, 0, 0.1, false);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = ComAlgo.FitCircle(edgeAtPixRotate, out Point waferCenterRotate, out _, out residual);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                ret = UBTAlogo.CalcCalibWaferDie(_calibTableInfo, _cameraNames[i], tableImagesRotate[i], waferCenterRotate, out _, out _, out Point featurePixRotate);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                rotateEdgeAtPix[_cameraNames[i]] = edgeAtPixRotate;

                //UBTAlogo.SaveImg(tableImagesRotate[i], featurePixRotate, "./" + _cameraNames[i] + ".bmp");
                ret = GetRealByPix(_cameraNames[i], featurePixRotate, out Point featureRotateAtReal);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                rotateMark.Add(featureRotateAtReal);
                #endregion
            }

            // 检查相机图像真值正确性
            _grabEdgeAtPix = origionEdgeAtPix;
            ret = CalibSensorCheck(out _, out _);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _grabEdgeAtPix = rotateEdgeAtPix;
            ret = CalibSensorCheck(out _, out _);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (origionMark.Count != rotateMark.Count)
            {
                return Errortype.UBT_CALIBXY_OUT_OF_RANGE;
            }

            List<Point> refOrigionMark = new List<Point>();
            List<Point> refRotateMark = new List<Point>();

            for (int index = 0; index < origionMark.Count; index++)
            {
                refOrigionMark.Add(origionMark[index]);
                refRotateMark.Add(rotateMark[index]);
            }

            ret = ComAlgo.CalcRotateCenter(refOrigionMark, refRotateMark, out Point rotateCenter, out _);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _chuckRotateCenter = rotateCenter;
            rotateCenterX = rotateCenter.X;
            rotateCenterY = rotateCenter.Y;

            if (ComAlgo.SaveFlg("saveCalibData", out int days))
            {
                string path = @"D:\Alg\";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string text = string.Empty;
                string sep = " ";
                for (int i = 0; i < origionMark.Count; i++)
                {
                    text += _cameraNames[i] + sep;
                    text += "a " + refOrigionMark[i].X.ToString() + sep + refOrigionMark[i].Y.ToString() + sep;
                    text += "b " + refRotateMark[i].X.ToString() + sep + refRotateMark[i].Y.ToString() + sep;
                }

                text += "calibValues " + rotateCenterX.ToString() + sep + rotateCenterY.ToString() + sep;
                string fileName = path + "\\calibRotateRecord.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 校验相机真值
        /// </summary>
        /// <param name="residualDeviation">标定板圆环残差</param>
        /// <param name="tableCenterReal">估算的标定板圆环中心真值坐标</param>
        /// <param name="fitThresh">拟合误差阈值</param>
        /// <returns>ok：真值标定结果准确</returns>
        public static Errortype CalibSensorCheck(out double residualDeviation, out Point tableCenterReal, double fitThresh = 0.1)
        {
            residualDeviation = 0.0;
            tableCenterReal = new Point(0, 0);
            List<Point> edgeRealXY = new List<Point>();
            foreach (var pair in _grabEdgeAtPix)
            {
                if (pair.Value.Count < 10)
                {
                    return Errortype.UBT_CAMERA_REAL_CALIB_INCOMPLETE;
                }

                // 将标定板边缘像素转真值坐标
                foreach (var pix in pair.Value)
                {
                    var ret = GetRealByPix(pair.Key, pix, out Point pointAtReal);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }

                    edgeRealXY.Add(pointAtReal);
                }
            }

            // 用边缘真值坐标拟合圆(tableCenterRealXY 应该无限接近标定板中心坐标，若不是则标定板发生了位移)
            var res = ComAlgo.FitCircle(edgeRealXY, out tableCenterReal, out double waferRadius, out List<double> residual);
            if (res != Errortype.OK)
            {
                return res;
            }

            HOperatorSet.TupleDeviation(residual.ToArray(), out HTuple errorDeviation);
            residualDeviation = errorDeviation.D;
            if (errorDeviation > fitThresh)
            {
                return Errortype.UBT_CAMERA_REAL_CALIB_BAD;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算标定
        /// </summary>
        /// <returns>ok：标定完成</returns>
        public static Errortype CalibMotion()
        {
            CalibCoord motionCalib = _calibList.Find(e => e.ItemName == "XYmotion");
            if (motionCalib == null)
            {
                _motionRulerPoints.Clear();
                _motionRealPoints.Clear();
                return Errortype.CALIB_ITEM_NAME_NULL;
            }

            var ret = motionCalib.CalibDo(_motionRulerPoints, _motionRealPoints, TransType.AffineKDTrans);
            if (ret != Errortype.OK)
            {
                _motionRulerPoints.Clear();
                _motionRealPoints.Clear();
                motionCalib.IsCalibed = false;
                return ret;
            }

            _motionRulerPoints.Clear();
            _motionRealPoints.Clear();
            motionCalib.IsCalibed = true;
            motionCalib.Save(_dataDir + "/");
            return Errortype.OK;
        }

        /// <summary>
        /// 运动系标定后单点误差计算
        /// </summary>
        /// <param name="imgCameras">验证位置的各相机图像</param>
        /// <param name="rulerPoint">验证位置的轴坐标</param>
        /// <param name="errors">验证位置的标定残差</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype CheckCalibMotion(List<Camera> imgCameras, Point rulerPoint, out Point errors)
        {
            errors = new Point(0, 0);
            var ret = GetRealByRuler("XYMotion", new Point(0, 0), rulerPoint, new Point(0, 0), out Point realPoint);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            List<bool> searchNotch = new List<bool> { false, true, false, false };
            ret = CalcWaferCenter(_cameraNames, imgCameras, searchNotch, out Point centerReal, out _, out _, out _, out _);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            errors = centerReal - realPoint;

            if (ComAlgo.SaveFlg("saveCalibData", out int days))
            {
                string path = @"D:\Alg\";
                if (Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string text = string.Empty;
                string sep = " ";

                text += "ruler " + rulerPoint.X.ToString() + sep + rulerPoint.Y.ToString() + sep;
                text += "real " + realPoint.X.ToString() + sep + realPoint.Y.ToString() + sep;
                text += "center " + centerReal.X.ToString() + sep + centerReal.Y.ToString() + sep;
                text += "error " + errors.X.ToString() + sep + errors.Y.ToString() + sep;

                string fileName = path + "\\calibMotionRecord.txt";
                FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(text);
                sw.Close();
                fs.Close();
            }

            return Errortype.OK;
        }

        #endregion

        #region 工具

        /// <summary>
        /// 像素转真值
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="pixPoint">像素坐标</param>
        /// <param name="realPoint">真值坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal static Errortype GetRealByPix(string cameraName, Point pixPoint, out Point realPoint)
        {
            realPoint = new Point();
            CalibCoord sensorCalib = _calibList.Find(e => e.ItemName == cameraName);
            if (sensorCalib == null)
            {
                return Errortype.CALIB_ITEM_NAME_NULL;
            }

            if (!sensorCalib.IsCalibed)
            {
                return Errortype.UBT_CAMERA_REAL_CALIB_INCOMPLETE;
            }

            return sensorCalib.Src2Dst(pixPoint, out realPoint, out List<Point> residuals);
        }

        /// <summary>
        /// 真值转轴距离
        /// </summary>
        /// <param name="axisName">轴名称</param>
        /// <param name="targetRealPoint">目标真值</param>
        /// <param name="baseRealPoint">真值初始值</param>
        /// <param name="baseAxis">轴初始位置</param>
        /// <param name="targetAxis">目标轴位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype GetRulerByReal(string axisName, Point targetRealPoint, Point baseRealPoint, Point baseAxis, out Point targetAxis)
        {
            targetAxis = new Point();
            if (_grabEdgeInfo.EnableMotionClib > 0)
            {
                axisName = "XYmotion";
            }

            CalibCoord axisCalib = _calibList.Find(e => e.ItemName == axisName);
            if (axisCalib == null)
            {
                return Errortype.CALIB_ITEM_NAME_NULL;
            }

            if (!axisCalib.IsCalibed)
            {
                return Errortype.UBT_AXIS_CALIB_INCOMPLETE;
            }

            // 此处需要注意 计算运动量时baseAxis默认在拍照位置即标定起始点，baseReal为(0,0)，最终运动的真值坐标distReal = targetreal - basereal，
            // 若不在默认拍照位置 distReal 需要加上baseReal；
            Point distReal = targetRealPoint - baseRealPoint;
            axisCalib.Dst2Src(distReal, out Point motionAxis, out _);
            targetAxis = baseAxis + motionAxis;

            if (_grabEdgeInfo.EnableMotionClib > 0)
            {
                axisCalib.Src2Dst(baseAxis, out Point baseReal, out _);
                Point targetReal = baseReal + distReal;
                axisCalib.Dst2Src(targetReal, out targetAxis, out _);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 轴距离转真值距离
        /// </summary>
        /// <param name="axisName">轴名称</param>
        /// <param name="baseAxisPoint">轴初始值</param>
        /// <param name="targetAxisPoint">目标轴坐标</param>
        /// <param name="baseReal">真值初始位置</param>
        /// <param name="targetReal">目标真值位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal static Errortype GetRealByRuler(string axisName, Point baseAxisPoint, Point targetAxisPoint, Point baseReal, out Point targetReal)
        {
            targetReal = new Point();
            if (_grabEdgeInfo.EnableMotionClib > 0)
            {
                axisName = "XYmotion";
            }

            CalibCoord axisCalib = _calibList.Find(e => e.ItemName == axisName);
            if (axisCalib == null)
            {
                return Errortype.CALIB_ITEM_NAME_NULL;
            }

            if (!axisCalib.IsCalibed)
            {
                return Errortype.UBT_AXIS_CALIB_INCOMPLETE;
            }

            axisCalib.Src2Dst(baseAxisPoint, out Point baseAtReal, out _);
            axisCalib.Src2Dst(targetAxisPoint, out Point targetAtReal, out _);
            Point motionReal = targetAtReal - baseAtReal;
            targetReal = baseReal + motionReal;
            return Errortype.OK;
        }

        /// <summary>
        /// 加载机台轴参数
        /// </summary>
        /// <param name="path">加载路径</param>
        /// <returns>OK：成功；其他：失败</returns>
        internal static Errortype LoadAxisParam(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = path + "\\algorithmParam.ini";
            if (!File.Exists(path))
            {
                File.Create(path).Close();
                StringBuilder sb = new StringBuilder();
                int stageAxisX = 1;
                int stageAxisY = 1;
                int stageAxisT = 1;
                double waferAngleDie = 0.1;

                sb.AppendLine("[StageAxis]");
                sb.AppendLine("HeadAxisX=" + stageAxisX.ToString());
                sb.AppendLine("HeadAxisY=" + stageAxisY.ToString());
                sb.AppendLine("HeadAxisT=" + stageAxisT.ToString());
                File.WriteAllText(path, sb.ToString());
            }

            string[] keys = null;
            string[] values = null;

            IniHelper.GetAllKeyValues("StageAxis", out keys, out values, path);
            _axisDirectX = int.Parse(values[0]);
            _axisDirectY = int.Parse(values[1]);
            _axisDirectT = int.Parse(values[2]);

            return Errortype.OK;
        }

        #endregion

        #region 手动抓边

        /// <summary>
        /// 手动抓取单相机视野里的产品边缘
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="imageData">相机图像</param>
        /// <param name="selectedEdge">选中的边缘像素</param>
        /// <param name="cameraGrabLabel">输出边缘点标注图像</param>
        /// <param name="notch">框选的notch中心</param>
        /// <param name="searchGlass">是否搜索玻璃</param>
        /// <returns>ok：抓取成功</returns>
        public static Errortype GrabEdge(string cameraName, Camera imageData, List<Point> selectedEdge, out Camera cameraGrabLabel, Rectangle1 notch = null, bool searchGlass = false)
        {
            cameraGrabLabel = new Camera();

            // 单图自动抓取
            if (selectedEdge is null)
            {
                List<Point> featurePix = new List<Point>();
                List<Point> edgeAtPix = new List<Point>();
                if (_grabEdgeInfo.GetEdgeHasNotch(cameraName))
                {
                    // 先搜索notch并转为真值坐标
                    var ret = UBTAlogo.CalcNotchPixTemplate(_grabEdgeInfo, imageData, out Point notchCenterAtPix, out double notchSize,  searchGlass, _dataDir);

                    if (ret != Errortype.OK)
                    {
                        notchCenterAtPix = null;
                        notchSize = 0;
                        _grabNotchAtReal = null;
                    }
                    else
                    {
                        featurePix.Add(notchCenterAtPix);
                        ret = GetRealByPix(cameraName, notchCenterAtPix, out _grabNotchAtReal);
                        if (ret != Errortype.OK)
                        {
                            return ret;
                        }
                    }

                    //ret = UBTAlogo.CalcEdgePix(imageData, out edgeAtPix, notchCenterAtPix, notchSize, 0.1, searchGlass);
                    ret = UBTAlogo.CalcEdgePixGen2(_grabEdgeInfo, cameraName, imageData, out edgeAtPix, notchCenterAtPix, notchSize, 0.1, searchGlass);
                    if (ret != Errortype.OK)
                    {
                        cameraGrabLabel = imageData.Clone();
                        return Errortype.UBT_EDGE_SEARCH_FAILED;
                    }
                }
                else
                {
                    //var ret = UBTAlogo.CalcEdgePix(imageData, out edgeAtPix, null, 0, 0.1, searchGlass);
                    var ret = UBTAlogo.CalcEdgePixGen2(_grabEdgeInfo, cameraName, imageData, out edgeAtPix, null, 0, 0.1, searchGlass);
                    if (ret != Errortype.OK)
                    {
                        return ret;
                    }
                }

                // 将wafer边缘像素转真值坐标
                foreach (var pix in edgeAtPix)
                {
                    featurePix.Add(pix);
                }

                _grabEdgeAtPix[cameraName] = edgeAtPix;
                UBTAlogo.LabelImg(imageData, featurePix, out cameraGrabLabel);
                return Errortype.OK;
            }
            else
            {
                // 如果是手动框选的notch
                if (notch != null)
                {
                    Point notchPix = new Point((notch.Start_X + notch.End_X) / 2, (notch.Start_Y + notch.End_Y) / 2);
                    double notchSize = Math.Abs(notch.End_Y - notch.Start_Y);
                    var ret = UBTAlogo.GrabEdgPix(imageData, selectedEdge, out List<Point> caliperEdgePix, out cameraGrabLabel,
                        notchPix, notchSize);
                    if (ret == Errortype.OK)
                    {
                        ret = GetRealByPix(cameraName, notchPix, out Point notchAtReal);
                        _grabNotchAtReal = notchAtReal;
                        _grabEdgeAtPix[cameraName] = caliperEdgePix;
                    }
                }
                else
                {
                    var ret = UBTAlogo.GrabEdgPix(imageData, selectedEdge, out List<Point> caliperEdgePix, out cameraGrabLabel);
                    if (ret == Errortype.OK)
                    {
                        _grabEdgeAtPix[cameraName] = caliperEdgePix;
                    }
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 清除/初始化 手动边缘抓取数据
        /// </summary>
        /// <returns>ok：初始化成功</returns>
        public static Errortype GrabEdgeClear()
        {
            for (int i = 0; i < _cameraNames.Count; i++)
            {
                //_grabEdgeAtPix[_cameraNames[i]].Clear();
            }

            //_grabNotchAtReal = null;
            return Errortype.OK;
        }

        /// <summary>
        /// 计算手动抓取的产品中心坐标
        /// </summary>
        /// <param name="centerPoint">输出产品中心</param>
        /// <param name="notchDeg">输出notch角度</param>
        /// <param name="roundErrors">输出拟合的圆度值</param>
        /// <param name="isOk">输出各相机是否抓边正常</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype GrabEdgeCalcCenter(out Point centerPoint, out double notchDeg, out double roundErrors, out Dictionary<string, bool> isOk)
        {
            centerPoint = new Point();
            notchDeg = 0.0;
            List<Point> edgeRealXY = new List<Point>();
            roundErrors = -1.0;
            isOk = new Dictionary<string, bool>();
            bool allEdgeIsOk = true;

            foreach (KeyValuePair<string, List<Point>> keyValuePair in _grabEdgeAtPix)
            {
                if (keyValuePair.Value.Count < 10)
                {
                    isOk.Add(keyValuePair.Key, false);
                    allEdgeIsOk = false;
                }
                else
                {
                    isOk.Add(keyValuePair.Key, true);
                    foreach (Point pix in keyValuePair.Value)
                    {
                        var ret = GetRealByPix(keyValuePair.Key, pix, out Point pointAtReal);
                        if (ret != Errortype.OK)
                        {
                            return ret;
                        }

                        edgeRealXY.Add(pointAtReal);
                    }
                }
            }

            if (allEdgeIsOk)
            {
                // 用边缘真值坐标拟合圆
                var res = ComAlgo.FitCircle(edgeRealXY, out centerPoint, out double waferRadius,
                    out List<double> residual);
                if (res != Errortype.OK)
                {
                    return res;
                }

                HOperatorSet.TupleDeviation(residual.ToArray(), out HTuple errorDeviation);
                roundErrors = errorDeviation.D;
                if (errorDeviation > _grabEdgeInfo.CirleDevThresh)
                {
                    return Errortype.UBT_CAMERA_REAL_CALIB_BAD;
                }

                // 计算notch夹角
                if (_grabNotchAtReal != null)
                {
                    Point centerToNotch = _grabNotchAtReal - centerPoint;
                    HOperatorSet.TupleAtan2(centerToNotch.Y, centerToNotch.X, out HTuple notchRad);
                    HOperatorSet.TupleDeg(notchRad, out HTuple notchCenterDeg);
                    notchDeg = notchCenterDeg.D;
                }

                return Errortype.OK;
            }

            return Errortype.UBT_EDGE_SEARCH_FAILED;
        }

        /// <summary>
        /// 将手动抓边结果计算对准运动量
        /// </summary>
        /// <param name="topCenter">抓到的glass中心</param>
        /// <param name="topNotchDeg">抓到的glass Notch角度</param>
        /// <param name="bottomCenter">抓到的wafer中心</param>
        /// <param name="bottomNotchDeg">抓到的wafer Notch角度</param>
        /// <param name="rotateDeg">输出旋转角度</param>
        /// <param name="motionRulerXY">输出平移量</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype GrabEdgeAlign(Point topCenter, double topNotchDeg, Point bottomCenter, double bottomNotchDeg, out double rotateDeg, out Point motionRulerXY)
        {
            motionRulerXY = null;
            rotateDeg = 0.0;

            if (RotateCenter == null)
            {
                return Errortype.UBT_CALIB_DATA_NUM_ERROR;
            }

            rotateDeg = (topNotchDeg - bottomNotchDeg) * _axisDirectT;   // 计算bottom需要的旋转量
            UBTAlogo.GetT2thetaFixByAngle(_dataDir, rotateDeg, out double rotateDegFix);
            rotateDeg = rotateDegFix;

            var ret = ComAlgo.CalcRotatePoint(bottomCenter, rotateDeg, RotateCenter, out Point bottomCenterRotedRealXY); // 计算旋转后bottom中心坐标
            if (ret != Errortype.OK)
            {
                return ret;
            }

            Point curRuler = new Point(0, 0);
            if (_grabEdgeInfo.EnableMotionClib < 1)
            {
                curRuler = new Point(0, 0);
            }

            ret = GetRulerByReal(_axisName, topCenter, bottomCenterRotedRealXY, curRuler, out motionRulerXY);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            motionRulerXY = new Point(motionRulerXY.X * _axisDirectX, motionRulerXY.Y * _axisDirectY);
            UBTAlogo.GetT2RulerFixByRulerXY(_dataDir, motionRulerXY, out Point fixRulerXY);
            motionRulerXY = fixRulerXY;

            return Errortype.OK;
        }

        #endregion

        #region 对中心计算

        /// <summary>
        /// 计算第一次上料的下片wafer中心和notch角度
        /// </summary>
        /// <param name="cameraNames">图像顺序对应的相机名称</param>
        /// <param name="cameraData">图像数据</param>
        /// <param name="searchNotch">各图像是否搜索notch</param>
        /// <param name="waferCenterRealXY">输出wafer实际中心坐标</param>
        /// <param name="waferNotchDeg">输出wafer的notch角</param>
        /// <param name="isOk">输出各边抓取是否正常</param>
        /// <param name="labelPix">输出抓边结果像素点</param>
        /// <param name="circleResidual">拟合圆度结果</param>
        /// <param name="searchGlass">是否搜索玻璃，默认false</param>
        /// <param name="curRuler">记录当前轴坐标，用于手动标定运动系</param>
        /// <returns>ok:计算成功</returns>
        public static Errortype CalcWaferCenter(List<string> cameraNames, List<Camera> cameraData, List<bool> searchNotch, out Point waferCenterRealXY, out double waferNotchDeg, out Dictionary<string, bool> isOk, out Dictionary<string, List<Point>> labelPix, out double circleResidual, bool searchGlass = false, Point curRuler = null)
        {
            waferCenterRealXY = new Point();
            Point notchCenterRealXY = null;
            waferNotchDeg = 0.0;
            isOk = new Dictionary<string, bool>() { };
            labelPix = new Dictionary<string, List<Point>>() { };
            circleResidual = -1;
            Errortype ret = Errortype.OK;
            List<Point> dieAtPix = new List<Point>();
            List<Point> dieAtRuler = new List<Point>();

            if (cameraNames == null || cameraData == null)
            {
                return Errortype.UBT_CAMERA_DATA_NULL;
            }

            if (!((cameraNames.Count() == cameraData.Count()) && (cameraNames.Count() == searchNotch.Count())))
            {
                return Errortype.UBT_WAFER_EDGE_DATA_SIZE_MISMATCH;
            }

            // 清空一下抓边数据
            for (int i = 0; i < cameraNames.Count; i++)
            {
                isOk.Add(cameraNames[i], false);
                labelPix.Add(cameraNames[i], new List<Point>());
                _grabEdgeAtPix[cameraNames[i]] = new List<Point>();
                _grabNotchAtReal = null;
            }

            // 处理各相机图像
            for (int index = 0; index < cameraData.Count; index++)
            {
                if (cameraData[index].Height < 1)
                {
                    return Errortype.UBT_CAMERA_DATA_NULL;
                }

                List<Point> edgeAtPix = new List<Point>();          // 抓到边的像素点，用于转换成真实值计算wafer中心
                List<Point> featurePix = new List<Point>();         // 抓到边和notch的像素点，用于生成结果图片

                // 搜索notch+边缘
                if (searchNotch[index])
                {
                    if ((searchGlass && (_grabEdgeInfo.UseGlassDieAngle > 0)) ||
                        ((!searchGlass) && (_grabEdgeInfo.UseWaferDieAngle > 0)))
                    {
                        ret = UBTAlogo.CalcNotchDieTemplate(_grabEdgeInfo, cameraData[index], out dieAtPix, searchGlass);
                        if (ret != Errortype.OK)
                        {
                            _grabNotchAtReal = null;
                            notchCenterRealXY = null;

                            return Errortype.UBT_NOTCH_SEARCH_FAILED;
                        }

                        ret = GetRealByPix(cameraNames[index], dieAtPix[0], out Point pointAtReal);
                        if (ret != Errortype.OK)
                        {
                            return ret;
                        }

                        dieAtRuler.Add(pointAtReal);
                        ret = GetRealByPix(cameraNames[index], dieAtPix[dieAtPix.Count - 1], out pointAtReal);
                        if (ret != Errortype.OK)
                        {
                            return ret;
                        }

                        dieAtRuler.Add(pointAtReal);

                        ret = UBTAlogo.CalcEdgePixGen2(_grabEdgeInfo, cameraNames[index], cameraData[index], out edgeAtPix, null, 0, 0.1, searchGlass);
                        if (ret != Errortype.OK)
                        {
                            _grabEdgeAtPix[cameraNames[index]] = new List<Point>();
                        }
                    }
                    else
                    {
                        // 先搜索notch并转为真值坐标
                        ret = UBTAlogo.CalcNotchPixTemplate(_grabEdgeInfo, cameraData[index], out Point notchCenterAtPix, out double notchSize, searchGlass, _dataDir);
                        if (ret != Errortype.OK)
                        {
                            notchCenterAtPix = null;
                            notchSize = 0;
                            notchCenterRealXY = null;

                            return Errortype.UBT_NOTCH_SEARCH_FAILED;
                        }
                        else
                        {
                            featurePix.Add(notchCenterAtPix);
                            ret = GetRealByPix(cameraNames[index], notchCenterAtPix, out notchCenterRealXY);
                            if (ret != Errortype.OK)
                            {
                                return ret;
                            }

                            _grabNotchAtReal = notchCenterRealXY;
                        }

                        // 再搜索屏蔽notch区域后的wafer边缘像素
                        ret = UBTAlogo.CalcEdgePixGen2(_grabEdgeInfo, cameraNames[index], cameraData[index], out edgeAtPix, notchCenterAtPix, notchSize, 0.1, searchGlass);

                        if (ret != Errortype.OK)
                        {
                            _grabEdgeAtPix[cameraNames[index]] = new List<Point>();
                        }
                    }
                }
                else
                {
                    ret = UBTAlogo.CalcEdgePixGen2(_grabEdgeInfo, cameraNames[index], cameraData[index], out edgeAtPix, null, 0, 0.1, searchGlass);
                    if (ret != Errortype.OK)
                    {
                        _grabEdgeAtPix[cameraNames[index]] = new List<Point>();
                    }
                }

                _grabEdgeAtPix[cameraNames[index]] = edgeAtPix;

                // 将wafer边缘像素转真值坐标
                foreach (var pix in edgeAtPix)
                {
                    featurePix.Add(pix);
                }

                labelPix[cameraNames[index]] = featurePix;
                string fileName = "AlignWafer/" + cameraNames[index];
                if (searchGlass)
                {
                    fileName = "AlignGlass/" + cameraNames[index];
                }

                UBTAlogo.SaveImg(cameraData[index], featurePix, fileName);
            }

            // 处理各边数据
            List<Point> edgeRealXY = new List<Point>();
            bool allEdgeIsOk = true;
            foreach (var cameraRes in _grabEdgeAtPix)
            {
                // 判断抓边数据正常
                if (cameraRes.Value.Count > 10)
                {
                    // 将wafer边缘像素转真值坐标
                    foreach (var pix in cameraRes.Value)
                    {
                        ret = GetRealByPix(cameraRes.Key, pix, out Point pointAtReal);
                        if (ret != Errortype.OK)
                        {
                            return ret;
                        }

                        edgeRealXY.Add(pointAtReal);
                    }

                    isOk[cameraRes.Key] = true;
                }
                else
                {
                    allEdgeIsOk = false;
                }
            }

            if (allEdgeIsOk)
            {
                // 用边缘真值坐标拟合圆
                var res = ComAlgo.FitCircle(edgeRealXY, out waferCenterRealXY, out double waferRadius, out List<double> residual);
                if (res != Errortype.OK)
                {
                    return res;
                }

                // 计算圆度（中心距标准差）
                HOperatorSet.TupleDeviation(residual.ToArray(), out HTuple errorDeviation);
                circleResidual = errorDeviation;
                if (errorDeviation > _grabEdgeInfo.CirleDevThresh)
                {
                    for (int i = 0; i < cameraNames.Count; i++)
                    {
                        isOk[cameraNames[i]] = false;
                    }

                    return Errortype.UBT_CAMERA_REAL_CALIB_BAD;
                }

                // 计算notch夹角
                if (_grabNotchAtReal != null)
                {
                    Point centerToNotch = notchCenterRealXY - waferCenterRealXY;
                    HOperatorSet.TupleAtan2(centerToNotch.Y, centerToNotch.X, out HTuple notchRad);
                    HOperatorSet.TupleDeg(notchRad, out HTuple notchDeg);
                    waferNotchDeg = notchDeg.D;
                }
                else if (dieAtRuler.Count == 2)
                {
                    // todo: 增加计算die行角度模式
                    Point centerToNotch = dieAtRuler[0] - dieAtRuler[1];
                    HOperatorSet.TupleAtan2(centerToNotch.Y, centerToNotch.X, out HTuple notchRad);
                    HOperatorSet.TupleDeg(notchRad, out HTuple notchDeg);
                    waferNotchDeg = notchDeg.D;
                }

                if (ComAlgo.SaveFlg("saveCalcCenter", out int days))
                {
                    string path = @"D:\Alg\";
                    if (Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    DateTime now = DateTime.Now;
                    int milliseconds = now.Millisecond;
                    string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");

                    string text = string.Empty;
                    string sep = " ";

                    string product = "wafer";
                    if (searchGlass)
                    {
                        product = "glass";
                    }

                    if (curRuler != null)
                    {
                        text += curRuler.X.ToString() + sep;
                        text += curRuler.Y.ToString() + sep;
                        text += waferCenterRealXY.X.ToString() + sep;
                        text += waferCenterRealXY.Y.ToString() + sep;
                        text += waferNotchDeg.ToString() + sep;
                    }
                    else
                    {
                        text += time + sep + product + sep;
                        text += "X: " + waferCenterRealXY.X.ToString() + sep;
                        text += "Y: " + waferCenterRealXY.Y.ToString() + sep;
                        text += "deg: " + waferNotchDeg.ToString() + sep;
                        text += "radius: " + waferRadius.ToString() + sep;
                        text += "errors: " + errorDeviation.ToString() + sep;
                    }

                    string fileName = path + "\\calcCenter.txt";
                    FileStream fs = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                    StreamWriter sw = new StreamWriter(fs);
                    sw.WriteLine(text);
                    sw.Close();
                    fs.Close();
                }

                return Errortype.OK;
            }

            return Errortype.UBT_EDGE_SEARCH_FAILED;
        }

        /// <summary>
        /// 二次上料后根据上侧产品计算各轴旋转量和运动量，
        /// 先旋转后平移
        /// </summary>
        /// <param name="axisName">轴名称</param>
        /// <param name="cameraNames">图像顺序对应的相机名称</param>
        /// <param name="cameraData">图像数据</param>
        /// <param name="searchNotch">各图像是否搜索notch</param>
        /// <param name="bottomCenterRealXY">输入前一次计算的底部wafer中心</param>
        /// <param name="bottomNotchDeg">输入前一次计算的底部wafer的notch角</param>
        /// <param name="rotateDeg">输出旋转角</param>
        /// <param name="motionRulerXY">输出平移量</param>
        /// <param name="isOk">输出各边抓取是否正常</param>
        /// <param name="labelPix">输出各边抓取结果像素</param>
        /// <param name="circleResidual">拟合圆度误差</param>
        /// <param name="curRuler">当前轴坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcBottomAlignTop(string axisName, List<string> cameraNames, List<Camera> cameraData, List<bool> searchNotch, Point bottomCenterRealXY, double bottomNotchDeg,
            out double rotateDeg, out Point motionRulerXY, out Dictionary<string, bool> isOk, out Dictionary<string, List<Point>> labelPix, out double circleResidual, Point curRuler = null)
        {
            motionRulerXY = null;
            rotateDeg = 0.0;
            bool searchGlass = true;
            _grabEdgeInfo.Load(_dataDir);
            LoadAxisParam(_dataDir);
            var ret = CalcWaferCenter(cameraNames, cameraData, searchNotch, out Point topCenterRealXY, out double topNotchDeg, out isOk, out labelPix, out circleResidual, searchGlass);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            if (RotateCenter == null)
            {
                return Errortype.UBT_CALIB_DATA_NUM_ERROR;
            }

            rotateDeg = (topNotchDeg - bottomNotchDeg) * _axisDirectT;   // 计算bottom需要的旋转量
            double round = Math.Round(rotateDeg / 90.0);                 // 控制旋转角度在±90°以内
            rotateDeg = rotateDeg - (90 * round);
            UBTAlogo.GetT2thetaFixByAngle(_dataDir, rotateDeg, out double rotateDegFix);
            rotateDeg = rotateDegFix;

            ret = ComAlgo.CalcRotatePoint(bottomCenterRealXY, rotateDeg, RotateCenter, out Point bottomCenterRotedRealXY); // 计算旋转后bottom中心坐标
            if (ret != Errortype.OK)
            {
                return ret;
            }

            //motionRulerXY = topCenterRealXY - bottomCenterRotedRealXY;  // 计算bottom平移量
            if (_grabEdgeInfo.EnableMotionClib < 1)
            {
                curRuler = new Point(0, 0);
            }

            ret = GetRulerByReal(_axisName, topCenterRealXY, bottomCenterRotedRealXY, curRuler, out motionRulerXY);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            motionRulerXY = new Point(motionRulerXY.X * _axisDirectX, motionRulerXY.Y * _axisDirectY);
            UBTAlogo.GetT2RulerFixByRulerXY(_dataDir, motionRulerXY, out Point fixRulerXY);
            motionRulerXY = fixRulerXY;
            return Errortype.OK;
        }

        /// <summary>
        /// 检查对中心结果
        /// </summary>
        /// <param name="cameraNames">各相机名称</param>
        /// <param name="cameraData">各相机图像</param>
        /// <param name="info">检查结果，若无异常，则为Empty</param>
        /// <returns>OK 无异常/UBT_ALIGN_CHECK_ERROR 有异常</returns>
        public static Errortype CheckAlign(List<string> cameraNames, List<Camera> cameraData, out string info)
        {
            info = String.Empty;
            for (int i = 0; i < cameraData.Count; i++)
            {
                var ret = UBTAlogo.CheckAlignRes(cameraData[i], out int searchedNum);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                if (searchedNum != 1)
                {
                    info = cameraNames[i] + " serach out area:" + searchedNum;
                    return Errortype.UBT_ALIGN_CHECK_ERROR;
                }
            }

            return Errortype.OK;
        }
        #endregion

    }

    /// <summary>
    /// 算法识别边缘
    /// </summary>
    internal static class UBTAlogo
    {
        /// <summary>
        /// 计算标定wafer的die像素及真值
        /// </summary>
        /// <param name="tableInfo">标定wafer信息</param>
        /// <param name="cameraName">相机名称</param>
        /// <param name="tableImage">标定wafer图像</param>
        /// <param name="waferCenter">标定wafer中心像素坐标</param>
        /// <param name="dieCenterPix">die中心像素坐标</param>
        /// <param name="dieRealId">die真值坐标</param>
        /// <param name="featureDiePix">直径行列上左外侧die像素，用于标注</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype CalcCalibWaferDie(CalibTableInfo tableInfo, string cameraName, Camera tableImage, Point waferCenter, out List<Point> dieCenterPix, out List<Point> dieRealId, out Point featureDiePix)
        {
            dieCenterPix = new List<Point>();
            dieRealId = new List<Point>();
            featureDiePix = null;
            double featureDieAngle = tableInfo.GetFeatureAngle(cameraName);
            Point featureDieRowCol = tableInfo.GetFeaturePoint(cameraName);

            HObject hImage = tableImage.GenHObject();
            HOperatorSet.Rgb1ToGray(hImage, out HObject grayImage);
            Camera tableGray = new Camera(grayImage);

            TemplateManager.Match(tableInfo.CalibDieTempName, tableGray, null, out double[] rectCenterY,
                out double[] rectCenterX, out double[] angles, out double[] scales, out double[] scores);

            List<Point> rectPix = new List<Point>();
            if (scores.Length < 5)
            {
                return Errortype.UBT_CALIB_IMAGE_FAILED;
            }

            for (int i = 0; i < rectCenterY.Length; i++)
            {
                rectPix.Add(new Point(rectCenterX[i], rectCenterY[i]));
            }

            HOperatorSet.TupleMean(angles, out HTuple meanAngleRad);

            //SortRowColumn(rectPix, meanAngleRad.D, out double[] sortedX, out double[] sortedY, out long[] sortedId, out long[] sortedRowId, out long[] sortedColId, 18);
            ComAlgo.SortRowGroups(rectPix, out long[] indices, out double[] rowId, 18, meanAngleRad.D, true);
            ComAlgo.SortRowGroups(rectPix, out _, out double[] colId, 18, meanAngleRad.D * -1, false);
            HOperatorSet.TupleSelect(rectCenterX, indices, out HTuple sortedX);
            HOperatorSet.TupleSelect(rectCenterY, indices, out HTuple sortedY);
            HOperatorSet.TupleSelect(rowId, indices, out HTuple sortedRowId);
            HOperatorSet.TupleSelect(colId, indices, out HTuple sortedColId);

            HOperatorSet.TupleGenConst(rowId.Length, waferCenter.Y, out HTuple tupleCenterRow);
            HOperatorSet.TupleGenConst(rowId.Length, waferCenter.X, out HTuple tupleCenterCol);
            HOperatorSet.AngleLx(tupleCenterRow, tupleCenterCol, sortedY, sortedX, out HTuple angleCenterToDieRad);
            HTuple angleCenterDieToRowRad = angleCenterToDieRad - meanAngleRad;
            HOperatorSet.DistancePp(tupleCenterRow, tupleCenterCol, sortedY, sortedX, out HTuple distanceCenterToDie);

            // 找预设点（限定和圆心连线角度中的die中最外侧的一个）
            HOperatorSet.TupleDeg(angleCenterDieToRowRad, out HTuple angleCenterDieToRowDeg);
            HOperatorSet.TupleAbs(angleCenterDieToRowDeg - featureDieAngle, out HTuple absDeg);
            HOperatorSet.TupleLessElem(absDeg, 0.45, out HTuple angleSelectMask);
            HOperatorSet.TupleMax(distanceCenterToDie * angleSelectMask, out HTuple maxSelectDist);
            HOperatorSet.TupleFindFirst(distanceCenterToDie, maxSelectDist, out HTuple featureIndex);

            HTuple rowOffset = featureDieRowCol.Y - sortedRowId[featureIndex];
            HTuple colOffset = featureDieRowCol.X - sortedColId[featureIndex];
            HTuple finalRow = sortedRowId + rowOffset;
            HTuple finalCol = sortedColId + colOffset;
            featureDiePix = new Point(sortedX[featureIndex], sortedY[featureIndex]);
            for (int i = 0; i < rowId.Length; i++)
            {
                dieCenterPix.Add(new Point(sortedX[i], sortedY[i]));
                dieRealId.Add(new Point(finalCol[i] * tableInfo.MarkColInterval, finalRow[i] * tableInfo.MarkRowInterval));
            }

            //LabelImg(tableImage, dieCenterPix, out Camera labelDie);
            hImage.Dispose();
            tableGray.Dispose();
            grayImage.Dispose();

            //validRegions.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 计算notch中心像素坐标(模版匹配)
        /// </summary>
        /// <param name="info">抓边信息</param>
        /// <param name="notchImage">notch图像</param>
        /// <param name="notchAtPix">notch中心像素</param>
        /// <param name="notchSize">notch大小</param>
        /// <param name="searchGlass">是否寻找玻璃（内边）</param>
        /// <param name="dataDir">标定文件夹目录(存放模板)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcNotchPixTemplate(GrabEdgeInfo info, Camera notchImage, out Point notchAtPix, out double notchSize, bool searchGlass = true, string dataDir = "./CalibData")
        {
            notchAtPix = null;
            notchSize = 0.0;
            string templateName = info.WaferNotchTemplateName;
            if (searchGlass)
            {
                templateName = info.GlassNotchTemplateName;
            }

            HObject himage = notchImage.GenHObject();
            HOperatorSet.MeanImage(himage, out HObject imageToMatch, 7, 7);
            Camera cameraToMatch = new Camera(imageToMatch);

            TemplateManager.Match(templateName, cameraToMatch, null, out double[] notchRows, out double[] notchCols, out double[] angles, out double[] scales, out double[] scores);

            imageToMatch.Dispose();
            cameraToMatch.Dispose();
            himage.Dispose();

            // todo: add select code
            if ((scores != null) && (scores.Length > 0))
            {
                // 选取得分最高的结果
                HOperatorSet.TupleSortIndex(scores, out HTuple scoreIndex);
                HOperatorSet.TupleInverse(scoreIndex, out HTuple indexMaxToMin);
                if (scores[indexMaxToMin[0]] > 0.9)
                {
                    HOperatorSet.TupleSelect(notchCols, indexMaxToMin[0], out HTuple selectedNotchCol);
                    HOperatorSet.TupleSelect(notchRows, indexMaxToMin[0], out HTuple selectedNotchRow);
                    HOperatorSet.TupleSelect(angles, indexMaxToMin[0], out HTuple selectedNotchRad);
                    notchAtPix = new Point(selectedNotchCol.D, selectedNotchRow.D);
                    notchSize = 120;

                    return Errortype.OK;
                }
            }

            return Errortype.UBT_NOTCH_SEARCH_FAILED;
        }

        /// <summary>
        /// 计算die行列作为notch角度(模版匹配)
        /// </summary>
        /// <param name="info">抓边信息</param>
        /// <param name="notchImage">notch图像</param>
        /// <param name="notchAtPix">特征die行的像素坐标</param>
        /// <param name="searchGlass">使用wafer模版还是glass模版</param>
        /// <param name="dataDir">标定文件夹目录(存放模板)</param>
        /// <returns>OK:成功,其他:失败</returns>
        public static Errortype CalcNotchDieTemplate(GrabEdgeInfo info, Camera notchImage, out List<Point> notchAtPix, bool searchGlass = false, string dataDir = "./CalibData")
        {
            notchAtPix = new List<Point>();
            string templateName = info.WaferNotchTemplateName;
            if (searchGlass)
            {
                templateName = info.GlassNotchTemplateName;
            }

            HObject himage = notchImage.GenHObject();
            HOperatorSet.MeanImage(himage, out HObject imageToMatch, 7, 7);
            Camera cameraToMatch = new Camera(imageToMatch);

            var ret = TemplateManager.Match(templateName, cameraToMatch, null, out double[] notchRows, out double[] notchCols, out double[] angles, out double[] scales, out double[] scores);

            imageToMatch.Dispose();
            cameraToMatch.Dispose();
            himage.Dispose();
            if (ret != Errortype.OK)
            {
                return ret;
            }

            List<Point> diePix = new List<Point>();
            if (scores.Length > 2)
            {
                for (int i = 0; i < notchCols.Length; i++)
                {
                    diePix.Add(new Point(notchCols[i], notchRows[i]));
                }

                HOperatorSet.TupleMean(angles, out HTuple meanAngleRad);
                ComAlgo.SortRowGroups(diePix, out long[] indices, out double[] rowId, 18, meanAngleRad.D, true);
                HOperatorSet.TupleMax(rowId, out HTuple maxRow);
                
                // 找到die最多的一行
                HTuple maxRowItemNum = 0;
                HTuple maxRowMask = 0;
                for (int i = 0; i < maxRow.D; i++)
                {
                    HOperatorSet.TupleEqualElem(rowId, i, out HTuple rowMask);
                    HOperatorSet.TupleSum(rowMask, out HTuple rowItemCount);
                    if (rowItemCount > maxRowItemNum)
                    {
                        maxRowItemNum = rowItemCount;
                        maxRowMask = rowMask;
                    }
                }

                // 统计die最多的一行
                for (int i = 0; i < maxRowMask; i++)
                {
                    if (maxRowMask[i] > 0)
                    {
                        notchAtPix.Add(diePix[i]);
                    }
                }

                if (notchAtPix.Count > 1)
                {
                    return Errortype.OK;
                }
            }

            return Errortype.UBT_NOTCH_SEARCH_FAILED;
        }

        /// <summary>
        /// 宽卡尺抓边
        /// </summary>
        /// <param name="edgeInfo">抓边预设信息</param>
        /// <param name="cameraName">相机名称</param>
        /// <param name="waferImage">原始图像</param>
        /// <param name="edgePix">输出边缘像素</param>
        /// <param name="notchCenterPix">传入notch屏蔽坐标</param>
        /// <param name="notchSize">传入notch屏蔽区半径</param>
        /// <param name="cropRate">边缘裁剪比例</param>
        /// <param name="searchGlass">是否是玻璃（玻璃搜索内边）</param>
        /// <param name="caliperWdith">卡尺默认宽度</param>
        /// <returns>ok：抓边成功</returns>
        public static Errortype CalcEdgePixGen2(GrabEdgeInfo edgeInfo, string cameraName, Camera waferImage, out List<Point> edgePix, Point notchCenterPix = null,
            double notchSize = 0.0, double cropRate = 0.1, bool searchGlass = true, int caliperWdith = 320)
        {
            edgePix = new List<Point>();
            bool blackEdge = false;
            if (waferImage == null)
            {
                return Errortype.UBT_EDGE_SEARCH_FAILED;
            }

            if (searchGlass)
            {
                if (edgeInfo.GlassGrabWidth < 0)
                {
                    caliperWdith = -edgeInfo.GlassGrabWidth;
                    blackEdge = true;
                }
                else
                {
                    caliperWdith = edgeInfo.GlassGrabWidth;
                }
            }
            else
            {
                if (edgeInfo.WaferGrabWidth < 0)
                {
                    caliperWdith = -edgeInfo.WaferGrabWidth;
                    blackEdge = true;
                }
                else
                {
                    caliperWdith = edgeInfo.WaferGrabWidth;
                }
            }

            HObject hImage = waferImage.GenHObject();

            Point ringCenter = edgeInfo.GetEdgeRingCenter(cameraName);
            double ringStartRad = edgeInfo.GetEdgeRingStartPhi(cameraName);
            double ringEndRad = edgeInfo.GetEdgeRingEndPhi(cameraName);
            double ringRadius = edgeInfo.GetEdgeRingRadius(cameraName);

            HOperatorSet.Rgb1ToGray(hImage, out HObject grayImage);
            if (notchCenterPix != null)
            {
                HTuple notchRow = new HTuple(notchCenterPix.Y, notchCenterPix.Y);
                HTuple notchCol = new HTuple(notchCenterPix.X + 50, notchCenterPix.X + 250);
                HTuple notchRaidus = new HTuple(150, 150);
                HOperatorSet.GenCircle(out HObject circleMask, notchRow, notchCol, notchRaidus);
                HOperatorSet.PaintRegion(circleMask, grayImage, out HObject maskImage, 255, "fill");
                grayImage.Dispose();
                grayImage = maskImage.Clone();
                circleMask.Dispose();
                maskImage.Dispose();
            }

            // 建立测量模板
            HOperatorSet.CreateMetrologyModel(out HTuple hv_MetrologyHandle);
            HOperatorSet.SetMetrologyModelImageSize(hv_MetrologyHandle, waferImage.Width, waferImage.Height);

            // caliper circle arc 抓圆弧上点
            CaliperParams calipParam = new CaliperParams();
            calipParam.MinScore = 0.5;
            calipParam.MeasureSigma = 0.6;
            calipParam.NumInstances = 1;

            calipParam.MeasureLength1 = caliperWdith;

            calipParam.MeasureLength2 = 10;
            calipParam.NumMeasures = 240;
            calipParam.MeasureThreshold = 25;
            calipParam.MeasureInterpolation = "nearest_neighbor";
            if (!blackEdge)
            {
                calipParam.MeasureSelect = "last";
                calipParam.MeasureTransition = "negative";
            }
            else
            {
                calipParam.MeasureSelect = "first";
                calipParam.MeasureTransition = "positive";
            }

            calipParam.CircleStartPhi = ringStartRad;
            calipParam.CircleEndPhi = ringEndRad;

            HOperatorSet.AddMetrologyObjectCircleMeasure(hv_MetrologyHandle, ringCenter.Y, ringCenter.X, ringRadius, calipParam.MeasureLength1, calipParam.MeasureLength2,
                calipParam.MeasureSigma, calipParam.MeasureThreshold, new HTuple("start_phi", "end_phi", "point_order"), new HTuple(calipParam.CircleStartPhi, calipParam.CircleEndPhi, "positive"), out HTuple hv_MetrologyCircleIndices);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "min_score", calipParam.MinScore);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "num_measures", calipParam.NumMeasures);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "num_instances", calipParam.NumInstances);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_select", calipParam.MeasureSelect);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_transition", calipParam.MeasureTransition);
            HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "distance_threshold", 2);

            // 执行测量
            HOperatorSet.ApplyMetrologyModel(grayImage, hv_MetrologyHandle);

            // 获取测量结果
            HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "result_type", "all_param", out HTuple hv_CircleParameter);
            if (hv_CircleParameter.Length < 1)
            {
                if (blackEdge)
                {
                    calipParam.MeasureSelect = "last";
                    calipParam.MeasureTransition = "negative";
                }
                else
                {
                    calipParam.MeasureSelect = "first";
                    calipParam.MeasureTransition = "positive";
                }

                HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "min_score", calipParam.MinScore);
                HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_select", calipParam.MeasureSelect);
                HOperatorSet.SetMetrologyObjectParam(hv_MetrologyHandle, hv_MetrologyCircleIndices, "measure_transition", calipParam.MeasureTransition);

                // 执行测量
                HOperatorSet.ApplyMetrologyModel(grayImage, hv_MetrologyHandle);

                // 获取测量结果
                HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "result_type", "all_param", out hv_CircleParameter);
                if (hv_CircleParameter.Length < 1)
                {
                    return Errortype.CALIPER_CIRCLE_NULL;
                }
            }

            HTuple hv_Sequence = HTuple.TupleGenSequence(0, new HTuple(hv_CircleParameter.TupleLength()) - 1, 3);
            HTuple circleCenterY = hv_CircleParameter.TupleSelect(hv_Sequence);
            HTuple circleCenterX = hv_CircleParameter.TupleSelect(hv_Sequence + 1);
            HTuple circleR = hv_CircleParameter.TupleSelect(hv_Sequence + 2);

            double[] arcPointsRow = null;
            double[] arcPointsCol = null;

            HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "used_edges", "row", out HTuple edgePointsRow);
            HOperatorSet.GetMetrologyObjectResult(hv_MetrologyHandle, hv_MetrologyCircleIndices, "all", "used_edges", "column", out HTuple edgePointsCol);
            arcPointsRow = edgePointsRow.DArr;
            arcPointsCol = edgePointsCol.DArr;

            HOperatorSet.ClearMetrologyModel(hv_MetrologyHandle);

            for (int pindex = 0; pindex < arcPointsRow.Length; pindex++)
            {
                edgePix.Add(new Point(arcPointsCol[pindex], arcPointsRow[pindex]));
            }

            //caliperImage.Dispose();
            grayImage.Dispose();
            hImage.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 卡尺提取边缘
        /// </summary>
        /// <param name="waferImage">图像</param>
        /// <param name="usedThresh">边缘阈值</param>
        /// <param name="circleCenter">圆心</param>
        /// <param name="circleRadius">圆半径</param>
        /// <param name="order">negative or positive</param>
        /// <param name="startPhi">起始角度</param>
        /// <param name="endPhi">终止角度</param>
        /// <param name="centerOut">卡尺提取圆心</param>
        /// <param name="radiusOut">卡尺半径</param>
        /// <param name="arcPointsRow">卡尺边缘坐标行</param>
        /// <param name="arcPointsCol">卡尺边缘坐标列</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal static Errortype CaliperEdgeArc(Camera waferImage, int usedThresh, Point circleCenter, double circleRadius, HTuple order, double startPhi, double endPhi, out Point centerOut, out double radiusOut, out double[] arcPointsRow, out double[] arcPointsCol)
        {
            // caliper circle arc 抓圆弧上点
            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = 0.1;
            calipParam.MeasureSigma = 5;
            calipParam.MeasureLength1 = 16;
            calipParam.MeasureLength2 = 8;
            calipParam.NumMeasures = 240;
            calipParam.MeasureThreshold = 40;
            calipParam.MeasureSelect = "first";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "negative";
            if (order.S == "negative")
            {
                calipParam.CircleEndPhi = startPhi;
                calipParam.CircleStartPhi = endPhi;
            }
            else
            {
                calipParam.CircleStartPhi = startPhi;
                calipParam.CircleEndPhi = endPhi;
            }

            return CaliperCircle.CircleArcExtraction(waferImage, circleCenter, circleRadius, calipParam, out centerOut, out radiusOut, out arcPointsRow, out arcPointsCol);
        }

        /// <summary>
        /// 检查对中运动后是否滑片
        /// </summary>
        /// <param name="waferImage">wafer图像</param>
        /// <param name="searchedRegionNum">wafer和glass叠加出的区域数量，正确数量1</param>
        /// <param name="grayThresh">wafer和glass区域亮度阈值，默认245</param>
        /// <param name="cropRate">边缘裁剪比例，默认裁剪十分之一，适应边缘过暗</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype CheckAlignRes(Camera waferImage, out int searchedRegionNum, int grayThresh = 245, double cropRate = 0.1)
        {
            searchedRegionNum = 0;
            if ((waferImage is null) || (waferImage.Height < 1))
            {
                return Errortype.UBT_CAMERA_DATA_NULL;
            }

            HObject himg = waferImage.GenHObject();
            HOperatorSet.GetImageSize(himg, out HTuple width, out HTuple height);
            HOperatorSet.GetDomain(himg, out HObject domain);
            HOperatorSet.GenRectangle1(out HObject cropRect, height * cropRate, width * cropRate, height * (1 - cropRate), width * (1 - cropRate));
            HOperatorSet.Difference(domain, cropRect, out HObject regionIgnore);
            HOperatorSet.GetRegionPoints(regionIgnore, out HTuple diffRegionRow, out HTuple diffRegionCol);
            HOperatorSet.TupleGenConst(diffRegionCol.Length, 0, out HTuple zero);
            HOperatorSet.SetGrayval(himg, diffRegionRow, diffRegionCol, zero);
            domain.Dispose();
            regionIgnore.Dispose();
            cropRect.Dispose();

            HOperatorSet.GenEmptyObj(out HObject regionBinary);
            HOperatorSet.BinaryThreshold(himg, out regionBinary, "max_separability", "light", out _);
            HOperatorSet.OpeningCircle(regionBinary, out HObject regionOpening, 4.0);
            HOperatorSet.Connection(regionBinary, out HObject regionConnect);
            HOperatorSet.AreaCenter(regionConnect, out HTuple areas, out _, out _);
            HOperatorSet.TupleMax(areas, out HTuple maxArea);
            HOperatorSet.SelectShape(regionConnect, out HObject regionSelect, "area", "and", maxArea * 0.005, maxArea);
            HOperatorSet.Intensity(regionSelect, himg, out HTuple grayMean, out _);
            HOperatorSet.TupleGreaterElem(grayMean, grayThresh, out HTuple greaterMask);
            HOperatorSet.TupleSum(greaterMask, out HTuple greaterCount);

            regionSelect.Dispose();
            regionConnect.Dispose();
            regionOpening.Dispose();
            regionBinary.Dispose();
            himg.Dispose();

            searchedRegionNum = (int)greaterCount.L;

            return Errortype.OK;
        }

        /// <summary>
        /// 保存边缘抓取的结果图
        /// </summary>
        /// <param name="img">wafer原图像</param>
        /// <param name="featurePoints">抓取到的点集</param>
        /// <param name="fileName">保存的文件名</param>
        public static void SaveImg(Camera img, List<Point> featurePoints, string fileName)
        {
            //if (ComAlgo.SaveFlg("SaveUBTImage"))
            if (true)
            {
                //string time = System.DateTime.Now.ToString("yyyyMMddHHmmss") + System.DateTime.Now.Millisecond.ToString("D3");
                string path = @"D:\Alg\AlignData\" + fileName;
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    path = @"D:\Alg\AlignData\" + fileName;
                    if (!Directory.Exists(Path.GetDirectoryName(path)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path));
                    }
                }

                LabelImg(img, featurePoints, out Camera labelImage);

                //labelImage.Save(path + time + ".bmp");
                ComAlgo.SaveImage(path, img, null, 7);
                labelImage.Dispose();
            }
        }

        /// <summary>
        /// 标注图片
        /// </summary>
        /// <param name="img">原始图</param>
        /// <param name="featurePoints">标注点</param>
        /// <param name="imgLabel">标注后的图像</param>
        public static void LabelImg(Camera img, List<Point> featurePoints, out Camera imgLabel)
        {
            HObject himg = img.GenHObject();
            HOperatorSet.GetImageSize(himg, out HTuple composeWidth, out HTuple composeHeight);
            HOperatorSet.OpenWindow(0, 0, composeWidth, composeHeight, 0, "invisible", "", out HTuple windowHandle);
            HOperatorSet.SetColored(windowHandle, 12);
            HOperatorSet.SetLineWidth(windowHandle, 2);

            HOperatorSet.SetPart(windowHandle, 0, 0, composeHeight - 1, composeWidth - 1);
            HOperatorSet.DispObj(himg, windowHandle);
            List<double> labelRow = new List<double>();
            List<double> labelCol = new List<double>();
            foreach (var point in featurePoints)
            {
                labelRow.Add(point.Y);
                labelCol.Add(point.X);
            }

            HOperatorSet.GenCrossContourXld(out HObject cross, labelRow.ToArray(), labelCol.ToArray(), 10, 0.785398);
            
            HOperatorSet.TupleGenSequence(1, labelRow.Count, 1, out HTuple id);
            HOperatorSet.DispText(windowHandle, id, "image", labelRow.ToArray(), labelCol.ToArray(), "black", new HTuple(), new HTuple());
            HOperatorSet.DispObj(cross, windowHandle);

            HOperatorSet.DumpWindowImage(out HObject indexImage, windowHandle);
            HOperatorSet.CloseWindow(windowHandle);
            imgLabel = new Camera(indexImage);
            cross.Dispose();
            indexImage.Dispose();
            himg.Dispose();
        }

        /*
        /// <summary>
        /// 行列排序(第一个点为基准点)
        /// </summary>
        /// <param name="inputPoints">输入点(第一个为基准点不参与计算)</param>
        /// <param name="rowAngleRad">行角弧度（和X轴夹角）</param>
        /// <param name="sortedX">输出排序后点的X坐标</param>
        /// <param name="sortedY">输出排序后点的Y坐标</param>
        /// <param name="sortedId">输出点的序号</param>
        /// <param name="rowIdAfterSort">排序后的点集行号</param>
        /// <param name="colIdAfterSort">排序后的点集列号</param>
        /// <param name="inLineThresh">同行Y方向波动的阈值，不要超过行间距即可</param>
        /// <returns>OK:成功；其他:失败</returns>
        public static Errortype SortRowColumn(List<Point> inputPoints, double rowAngleRad, out double[] sortedX, out double[] sortedY, out long[] sortedId, out long[] rowIdAfterSort, out long[] colIdAfterSort,
            double inLineThresh = 5.0)
        {
            sortedX = new double[inputPoints.Count];
            sortedY = new double[inputPoints.Count];
            sortedId = null;
            sortedId = null;
            rowIdAfterSort = new long[] { };
            colIdAfterSort = new long[] { };

            if (inputPoints == null)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            int lenth = inputPoints.Count;
            if (lenth < 2)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            sortedId = new long[lenth];

            double[] pointsX = new double[lenth];
            double[] pointsY = new double[lenth];

            for (int index = 0; index < lenth; index++)
            {
                pointsX[index] = inputPoints[index].X;
                pointsY[index] = inputPoints[index].Y;
            }

            HTuple rectRad = new HTuple(rowAngleRad);

            // 转正后排序
            HOperatorSet.VectorAngleToRigid(0, 0, 0, 0, 0, -rectRad, out HTuple rigidMatRotate);
            HOperatorSet.AffineTransPoint2d(rigidMatRotate, pointsY, pointsX, out HTuple rigidY, out HTuple rigidX);
            HTuple transY = new HTuple(rigidY);
            HTuple transX = new HTuple(rigidX);

            // 将中心点也转正
            //HOperatorSet.AffineTransPoint2d(rigidMatRotate, corePoint.Y, corePoint.X, out HTuple coreY, out HTuple coreX);
            HOperatorSet.TupleGenSequence(0, rigidY.Length - 1, 1, out HTuple ids);

            HTuple groupStartPos = new HTuple();
            HTuple groupEndPos = new HTuple();
            HTuple halfSortedIds = new HTuple();
            HTuple groupRowValue = new HTuple();
            while (ids.Length > 0)
            {
                //HOperatorSet.DistancePl(rigidY, rigidX, rigidY[0], rigidX[0], rigidY[0], rigidX[0] + 100, out HTuple elemDistance);
                HOperatorSet.TupleAbs(rigidY - rigidY[0], out HTuple elemDistance);
                HOperatorSet.TupleLessElem(elemDistance, inLineThresh, out HTuple inlineElemMask);
                HOperatorSet.TupleSelectMask(rigidX, inlineElemMask, out HTuple inlinePointsX);
                HOperatorSet.TupleSelectMask(ids, inlineElemMask, out HTuple inlinePointsIds);

                HOperatorSet.TupleSortIndex(inlinePointsX, out HTuple singleLineXIndex);

                HOperatorSet.TupleSelect(inlinePointsIds, singleLineXIndex, out inlinePointsIds);

                groupStartPos = groupStartPos.TupleConcat(halfSortedIds.Length);
                halfSortedIds = halfSortedIds.TupleConcat(inlinePointsIds);
                groupEndPos = groupEndPos.TupleConcat(halfSortedIds.Length - 1);

                groupRowValue = groupRowValue.TupleConcat(rigidY[0]);   // 考虑精度的话此处可用inlinePointsY的均值

                HOperatorSet.TupleNot(inlineElemMask, out HTuple restElemMask);
                HOperatorSet.TupleSelectMask(rigidX, restElemMask, out rigidX);
                HOperatorSet.TupleSelectMask(rigidY, restElemMask, out rigidY);
                HOperatorSet.TupleSelectMask(ids, restElemMask, out ids);
            }

            HTuple finalOrder = new HTuple();
            HOperatorSet.TupleSortIndex(groupRowValue, out HTuple rowOrder);

            for (int index = 0; index < rowOrder.Length; index++)
            {
                var startPos = groupStartPos[rowOrder[index].L];
                var endPos = groupEndPos[rowOrder[index].L];
                HOperatorSet.TupleSelectRange(halfSortedIds, startPos, endPos, out HTuple selectedGroupElemtsId);
                finalOrder = finalOrder.TupleConcat(selectedGroupElemtsId);
            }

            HOperatorSet.TupleSelect(pointsX, finalOrder, out HTuple finalPointsX);
            HOperatorSet.TupleSelect(pointsY, finalOrder, out HTuple finalPointsY);
            sortedId = finalOrder.LArr;

            HTuple meanRowIntrval = new HTuple(0.0);
            HTuple meanColIntrval = new HTuple(0.0);

            // 估算行间隔
            HOperatorSet.TupleSelect(groupRowValue, rowOrder, out groupRowValue);

            HOperatorSet.CreateFunct1dArray(groupRowValue, out HTuple functionRowValues);
            HOperatorSet.DerivateFunct1d(functionRowValues, "first", out HTuple rowInterval);
            HOperatorSet.Funct1dToPairs(rowInterval, out _, out HTuple rowInterValues);

            while (rowInterValues.Length > 1)
            {
                HOperatorSet.TupleMin(rowInterValues, out HTuple minIntervalues);
                HOperatorSet.TupleRound(rowInterValues / minIntervalues, out HTuple intervalRound);
                HOperatorSet.TupleEqualElem(intervalRound, 1, out HTuple eqElemMask);
                HOperatorSet.TupleSum(eqElemMask, out HTuple eqCount);
                if (eqCount > 2)
                {
                    HOperatorSet.TupleSelectMask(rowInterValues, eqElemMask, out HTuple selectedInterval);
                    HOperatorSet.TupleMean(selectedInterval, out meanRowIntrval);
                    break;
                }
                else
                {
                    HOperatorSet.TupleNot(eqElemMask, out HTuple neElemMask);
                    HOperatorSet.TupleSelectMask(rowInterValues, neElemMask, out rowInterValues);
                }
            }

            // 估算列间隔(选取列最多的一行)
            HOperatorSet.TupleMax(groupEndPos - groupStartPos, out HTuple maxLenGroupValue);
            HOperatorSet.TupleFindFirst(groupEndPos - groupStartPos, maxLenGroupValue, out HTuple maxLenGroupId);
            HOperatorSet.TupleSelectRange(halfSortedIds, groupStartPos[maxLenGroupId], groupEndPos[maxLenGroupId], out HTuple maxLenGroupElemId);
            HOperatorSet.TupleSelect(pointsX, maxLenGroupElemId, out HTuple groupColValue);

            HOperatorSet.CreateFunct1dArray(groupColValue, out HTuple functionColValues);
            HOperatorSet.DerivateFunct1d(functionColValues, "first", out HTuple colInterval);
            HOperatorSet.Funct1dToPairs(colInterval, out _, out HTuple colInterValues);

            while (colInterValues.Length > 1)
            {
                HOperatorSet.TupleMin(colInterValues, out HTuple minIntervalues);
                HOperatorSet.TupleRound(colInterValues / minIntervalues, out HTuple intervalRound);
                HOperatorSet.TupleEqualElem(intervalRound, 1, out HTuple eqElemMask);
                HOperatorSet.TupleSum(eqElemMask, out HTuple eqCount);
                if (eqCount > 2)
                {
                    HOperatorSet.TupleSelectMask(colInterValues, eqElemMask, out HTuple selectedInterval);
                    HOperatorSet.TupleMean(selectedInterval, out meanColIntrval);
                    break;
                }
                else
                {
                    HOperatorSet.TupleNot(eqElemMask, out HTuple neElemMask);
                    HOperatorSet.TupleSelectMask(colInterValues, neElemMask, out colInterValues);
                }
            }

            // 可能出现行数或列数较少导致行或列间隔无法计算的情况
            if (meanRowIntrval.D == 0 || meanColIntrval.D == 0)
            {
                HOperatorSet.TupleMax2(meanRowIntrval, meanColIntrval, out HTuple maxInterval);
                meanRowIntrval = maxInterval;
                meanColIntrval = maxInterval;
            }

            HOperatorSet.TupleSelect(transX, finalOrder, out HTuple transPointsX);
            HOperatorSet.TupleSelect(transY, finalOrder, out HTuple transPointsY);
            var rowDoubleId = (transPointsY - transPointsY[0]) / meanRowIntrval;
            var colDoubleId = (transPointsX - transPointsX[0]) / meanColIntrval;
            HOperatorSet.TupleRound(rowDoubleId, out HTuple rowId);
            HOperatorSet.TupleRound(colDoubleId, out HTuple colId);
            rowIdAfterSort = rowId.LArr;
            colIdAfterSort = colId.LArr;

            sortedX = finalPointsX.DArr;
            sortedY = finalPointsY.DArr;

            return Errortype.OK;
        }

        */

        /// <summary>
        /// 手动抓边计算逻辑
        /// </summary>
        /// <param name="waferImage">原始图像数据</param>
        /// <param name="edgeSelectPix">手选的边缘点</param>
        /// <param name="caliperEdgePix">定位出的边缘点</param>
        /// <param name="grabLabel">输出定位后的标注</param>
        /// <param name="notchCenterPix">手选的notch中心</param>
        /// <param name="notchSize">手选的notch大小</param>
        /// <returns>OK：计算成功</returns>
        public static Errortype GrabEdgPix(Camera waferImage, List<Point> edgeSelectPix, out List<Point> caliperEdgePix, out Camera grabLabel, Point notchCenterPix = null, double notchSize = 0.0)
        {
            grabLabel = waferImage.Clone();
            caliperEdgePix = edgeSelectPix;
            double[] edgeRowsSort = new double[edgeSelectPix.Count];
            double[] edgeColsSort = new double[edgeSelectPix.Count];
            List<Point> labelPix = new List<Point>();
            for (int i = 0; i < edgeSelectPix.Count; i++)
            {
                edgeRowsSort[i] = edgeSelectPix[i].Y;
                edgeColsSort[i] = edgeSelectPix[i].X;
            }

            HOperatorSet.GenContourPolygonXld(out HObject edgeContour, edgeRowsSort, edgeColsSort);
            HOperatorSet.FitCircleContourXld(edgeContour, "algebraic", -1, 0, 0, 3, 2, out HTuple circleCenterRow, out HTuple circleCenterCol, out HTuple circleRadius,
                out HTuple startPhi, out HTuple endPhi, out HTuple order);

            // caliper circle arc 抓圆弧上点
            var ret = CaliperEdgeArc(waferImage, 40, new Point(circleCenterCol.D, circleCenterRow.D), circleRadius, order, startPhi.D, endPhi.D,
                out Point centerOut, out double radiusOut, out double[] arcPointsRow, out double[] arcPointsCol);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            HTuple arcRows = new HTuple(arcPointsRow);
            HTuple arcCols = new HTuple(arcPointsCol);

            // 去除靠近notch的边缘
            if (notchCenterPix != null)
            {
                labelPix.Add(notchCenterPix);
                HTuple arcToNotchCenterDist = (arcRows - notchCenterPix.Y) * (arcRows - notchCenterPix.Y) + (arcCols - notchCenterPix.X) * (arcCols - notchCenterPix.X);
                HOperatorSet.TupleSqrt(arcToNotchCenterDist, out arcToNotchCenterDist);
                HOperatorSet.TupleGreaterElem(arcToNotchCenterDist, notchSize + 10, out HTuple outerMask);
                HOperatorSet.TupleSelectMask(arcRows, outerMask, out arcRows);
                HOperatorSet.TupleSelectMask(arcCols, outerMask, out arcCols);
            }

            for (int pindex = 0; pindex < arcRows.Length; pindex++)
            {
                caliperEdgePix.Add(new Point(arcCols[pindex], arcRows[pindex]));
                labelPix.Add(new Point(arcCols[pindex], arcRows[pindex]));
            }

            // 标注图片
            grabLabel.Dispose();
            LabelImg(waferImage, labelPix, out grabLabel);

            edgeContour.Dispose();

            return Errortype.OK;
        }

        /// <summary>
        /// 生成轴运动系标定点阵
        /// </summary>
        /// <param name="axisPose">输出标定的轴坐标</param>
        /// <param name="origionX">设定标定的起点位置X轴坐标</param>
        /// <param name="origionY">设定标定的起点位置Y轴坐标</param>
        /// <param name="moveRage">设定标定运动区域范围</param>
        /// <param name="step">设定标定运动步长</param>
        /// <returns>ok：生成成功</returns>
        public static Errortype GenCalibRulerPoints(out List<Point> axisPose, double origionX = 0.0, double origionY = 0.0, double moveRage = 3.0, double step = 0.5)
        {
            axisPose = new List<Point>();
            HOperatorSet.TupleGenSequence(origionX - moveRage, origionX + moveRage, step, out HTuple poseBase);
            HOperatorSet.CreateMatrix(1, poseBase.Length, poseBase, out HTuple matrixSingleHo);
            HOperatorSet.CreateMatrix(poseBase.Length, 1, poseBase, out HTuple matrixSingleVe);
            HOperatorSet.RepeatMatrix(matrixSingleHo, poseBase.Length, 1, out HTuple matrixX);
            HOperatorSet.RepeatMatrix(matrixSingleVe, 1, poseBase.Length, out HTuple matrixY);

            HOperatorSet.GetFullMatrix(matrixX, out HTuple valuesX);
            HOperatorSet.GetFullMatrix(matrixY, out HTuple valuesY);

            for (int i = 0; i < valuesX.Length; i++)
            {
                axisPose.Add(new Point(valuesX[i].D, valuesY[i].D));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// UBT2伺杆误差校正
        /// </summary>
        /// <param name="dataDir">输入补偿文件路径</param>
        /// <param name="thetaInput">输入轴运动量</param>
        /// <param name="thetaFix">输出校正后的轴运动量</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype GetT2thetaFixByAngle(string dataDir, double thetaInput, out double thetaFix)
        {
            thetaFix = thetaInput;
            string fullFileName = dataDir + "/thetaFix.xml";
            if (!File.Exists(fullFileName))
            {
                return Errortype.OK;
            }

            List<double> rulerT = new List<double>();
            List<double> realT = new List<double>();

            StreamReader sr = new StreamReader(fullFileName, Encoding.Default);
            string line = string.Empty;
            line = sr.ReadLine();
            string[] fristLine = line.Split('\t');
            string[] s = line.Split('\t');
            rulerT.Add(double.Parse(s[0]));
            realT.Add(double.Parse(s[1]));

            while ((line = sr.ReadLine()) != null)
            {
                s = line.Split('\t');
                rulerT.Add(double.Parse(s[0]));
                realT.Add(double.Parse(s[1]));
            }

            HOperatorSet.CreateFunct1dPairs(realT.ToArray(), rulerT.ToArray(), out HTuple functionT);
            HOperatorSet.GetYValueFunct1d(functionT, thetaInput, "constant", out HTuple fixT);
            thetaFix = fixT.D;
            if (Math.Abs(thetaFix - thetaInput) > 0.1)
            {
                thetaFix = thetaInput;
            }

            sr.Close();

            return Errortype.OK;
        }

        /// <summary>
        /// UBT2伺杆误差校正
        /// </summary>
        /// <param name="dataDir">输入补偿文件路径</param>
        /// <param name="rulerXY">输入轴运动量</param>
        /// <param name="rulerXYFix">输出校正后的轴运动量</param>
        /// <returns>ok：计算成功</returns>
        public static Errortype GetT2RulerFixByRulerXY(string dataDir, Point rulerXY, out Point rulerXYFix)
        {
            rulerXYFix = rulerXY;
            string fullFileName = dataDir + "/axisFix.xml";
            if (!File.Exists(fullFileName))
            {
                return Errortype.OK;
            }

            List<double> rulerX = new List<double>();
            List<double> rulerY = new List<double>();
            List<double> realX = new List<double>();
            List<double> realY = new List<double>();

            StreamReader sr = new StreamReader(fullFileName, Encoding.Default);
            string line = string.Empty;
            line = sr.ReadLine();
            string[] fristLine = line.Split('\t');
            string[] s = line.Split('\t');
            rulerX.Add(double.Parse(s[0]));
            rulerY.Add(double.Parse(s[1]));
            realX.Add(double.Parse(s[2]));
            realY.Add(double.Parse(s[3]));

            while ((line = sr.ReadLine()) != null)
            {
                s = line.Split('\t');
                rulerX.Add(double.Parse(s[0]));
                rulerY.Add(double.Parse(s[1]));
                realX.Add(double.Parse(s[2]));
                realY.Add(double.Parse(s[3]));
            }

            HOperatorSet.CreateFunct1dPairs(realX.ToArray(), rulerX.ToArray(), out HTuple functionX);
            HOperatorSet.CreateFunct1dPairs(realY.ToArray(), rulerY.ToArray(), out HTuple functionY);
            HOperatorSet.GetYValueFunct1d(functionX, rulerXY.X, "constant", out HTuple fixX);
            HOperatorSet.GetYValueFunct1d(functionY, rulerXY.Y, "constant", out HTuple fixY);
            rulerXYFix = new Point(fixX.D, fixY.D);
            if (rulerXYFix.DistanceTo(rulerXY) > 0.01)
            {
                rulerXYFix = rulerXY;
            }

            sr.Close();

            return Errortype.OK;
        }
    }

    /// <summary>
    /// 标定板信息类
    /// </summary>
    public class CalibTableInfo : Singleton<CalibTableInfo>
    {
        private static string _calibDieTemplateName = "CalibDie";
        private static List<string> _cameraNames = new List<string>() { "CameraLeft", "CameraRight", "CameraUp", "CameraDown" };
        private static double _markRowInterval = 3.519961;
        private static double _markColInterval = 3.519961;
        private static Point _tableCenterAtReal = new Point(160.0, 160.0);
        private static Dictionary<string, Point> _featurePointsAtReal = new Dictionary<string, Point>
        {
            { _cameraNames[0], new Point(-41, 0.0) },
            { _cameraNames[1], new Point(42, 1.0) },
            { _cameraNames[2], new Point(0.0, -41) },
            { _cameraNames[3], new Point(0.0, 42) },
        };

        private static Dictionary<string, double> _featurePointsAngle = new Dictionary<string, double>
        {
            { _cameraNames[0], -180.4 },
            { _cameraNames[1], -0.5 },
            { _cameraNames[2], 90.9 },
            { _cameraNames[3], -90.2 },
        };

        private static double _markAreaMin = 35000;
        private static double _markAreaMax = 50000;
        private static double _angleRate = 1.0;

        /// <summary>
        /// 配置相机名称
        /// </summary>
        /// <param name="names">所有相机的名称</param>
        /// <returns>ok：配置成功</returns>
        public Errortype SetCameraNames(List<string> names)
        {
            _cameraNames = names;

            //_featurePointsAtReal = new Dictionary<string, Point>();
            //_featurePointsAngle = new Dictionary<string, double>();
            foreach (var camera in names)
            {
                if (!_featurePointsAtReal.ContainsKey(camera))
                {
                    _featurePointsAtReal.Add(camera, null);
                    _featurePointsAngle.Add(camera, 0);
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// Gets or sets 标定板Die模板名称
        /// </summary>
        public string CalibDieTempName
        {
            get => _calibDieTemplateName;
            set => _calibDieTemplateName = value;
        }

        /// <summary>
        /// Gets or sets 标定板行间距
        /// </summary>
        public double MarkRowInterval
        {
            get => _markRowInterval;
            set => _markRowInterval = value;
        }

        /// <summary>
        /// Gets or sets 标定板列间距
        /// </summary>
        public double MarkColInterval
        {
            get => _markColInterval;
            set => _markColInterval = value;
        }

        /// <summary>
        /// Gets or sets 标定板中心真值
        /// </summary>
        public Point TableCenterAtReal
        {
            get => _tableCenterAtReal;
            set => _tableCenterAtReal = value;
        }

        /// <summary>
        /// Gets or sets 标定wafer的die面积下限
        /// </summary>
        public double MarkAreaMin
        {
            get => _markAreaMin;
            set => _markAreaMin = value;
        }

        /// <summary>
        /// Gets or sets 标定wafer的die面积上限
        /// </summary>
        public double MarkAreaMax
        {
            get => _markAreaMax;
            set => _markAreaMax = value;
        }

        /// <summary>
        /// Gets or sets 标定 angleRate
        /// </summary>
        public double AngelRate
        {
            get => _angleRate;
            set => _angleRate = value;
        }

        ///<summary>
        ///保存标定板信息
        /// </summary>
        /// <param name="dataDir">信息文件保存目录</param>
        /// <returns>ok:保存成功</returns>
        public Errortype Save(string dataDir)
        {
            if (!System.IO.Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            string fullFileName = dataDir + "/" + "CalibTable.ini";
            if (File.Exists(fullFileName))
            {
                File.Delete(fullFileName);
            }

            List<string> keys = new List<string>();
            List<string> value = new List<string>();

            keys.Clear();
            value.Clear();
            keys.Add("CameraNums");
            value.Add(_cameraNames.Count.ToString());
            IniHelper.AddSectionWithKeyValues("cameraInfo", keys, value, fullFileName);

            keys.Clear();
            value.Clear();
            foreach (var pair in _featurePointsAtReal)
            {
                keys.Add(pair.Key + "_x");
                value.Add(pair.Value.X.ToString());
                keys.Add(pair.Key + "_y");
                value.Add(pair.Value.Y.ToString());
            }

            IniHelper.AddSectionWithKeyValues("featurePointsAtReal", keys, value, fullFileName);

            keys.Clear();
            value.Clear();
            foreach (var pair in _featurePointsAngle)
            {
                keys.Add(pair.Key + "_deg");
                value.Add(pair.Value.ToString());
            }

            IniHelper.AddSectionWithKeyValues("featurePointsDeg", keys, value, fullFileName);
            keys.Clear();
            value.Clear();
            keys.Add("TableCenterAtReal_x");
            keys.Add("TableCenterAtReal_y");
            value.Add(_tableCenterAtReal.X.ToString());
            value.Add(_tableCenterAtReal.Y.ToString());
            IniHelper.AddSectionWithKeyValues("tableCenterAtReal", keys, value, fullFileName);
            keys.Clear();
            value.Clear();
            keys.Add("MarkRowInterval");
            keys.Add("MarkColInterval");
            value.Add(_markRowInterval.ToString());
            value.Add(_markColInterval.ToString());
            IniHelper.AddSectionWithKeyValues("markInterval", keys, value, fullFileName);
            keys.Clear();
            value.Clear();
            keys.Add("MarkAreaMin");
            keys.Add("MarkAreaMax");

            value.Add(_markAreaMin.ToString());
            value.Add(_markAreaMax.ToString());
            IniHelper.AddSectionWithKeyValues("markAreaMinMax", keys, value, fullFileName);

            keys.Clear();
            value.Clear();
            keys.Add("AngleRate");
            value.Add(_angleRate.ToString());
            IniHelper.AddSectionWithKeyValues("angleInfo", keys, value, fullFileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 载入标定板信息文件
        /// </summary>
        /// <param name="dataDir">信息文件所在目录</param>
        /// <returns>ok：载入成功</returns>
        public Errortype Load(string dataDir)
        {
            string fullFileName = dataDir + "/" + "CalibTable.ini";
            if (!File.Exists(fullFileName))
            {
                Save(dataDir);
                return Errortype.OK;
            }

            if ((!IniHelper.ExistSection("featurePointsAtReal", fullFileName)) || (!IniHelper.ExistSection("tableCenterAtReal", fullFileName)) || (!IniHelper.ExistSection("markInterval", fullFileName)) || (!IniHelper.ExistSection("markAreaMinMax", fullFileName)))
            {
                return Errortype.UBT_CALIB_TABLEINFO_FILE_ERROR;
            }

            Console.WriteLine("load CalibTableInfo:" + fullFileName);
            string[] keys = null;
            string[] values = null;

            IniHelper.GetAllKeyValues("cameraInfo", out keys, out values, fullFileName);
            if (values.Length != 1)
            {
                return Errortype.UBT_GRAB_EDGEINFO_FILE_ERROR;
            }

            int cameraNums = Convert.ToInt32(values[0]);

            IniHelper.GetAllKeyValues("featurePointsAtReal", out keys, out values, fullFileName);
            if (values.Length < (_cameraNames.Count * 2))
            {
                return Errortype.UBT_CALIB_TABLEINFO_FILE_ERROR;
            }

            for (int i = 0; i < _cameraNames.Count; i++)
            {
                _featurePointsAtReal[_cameraNames[i]] = new Point(Convert.ToDouble(values[i * 2]), Convert.ToDouble(values[i * 2 + 1]));
            }

            IniHelper.GetAllKeyValues("featurePointsDeg", out keys, out values, fullFileName);
            if (values.Length < _cameraNames.Count)
            {
                return Errortype.UBT_CALIB_TABLEINFO_FILE_ERROR;
            }

            for (int i = 0; i < _cameraNames.Count; i++)
            {
                _featurePointsAngle[_cameraNames[i]] = Convert.ToDouble(values[i]);
            }

            IniHelper.GetAllKeyValues("tableCenterAtReal", out keys, out values, fullFileName);
            if (values.Length != 2)
            {
                return Errortype.UBT_CALIB_TABLEINFO_FILE_ERROR;
            }

            _tableCenterAtReal.X = Convert.ToDouble(values[0]);
            _tableCenterAtReal.Y = Convert.ToDouble(values[1]);
            IniHelper.GetAllKeyValues("markInterval", out keys, out values, fullFileName);
            if (values.Length != 2)
            {
                return Errortype.UBT_CALIB_TABLEINFO_FILE_ERROR;
            }

            _markRowInterval = Convert.ToDouble(values[0]);
            _markColInterval = Convert.ToDouble(values[1]);
            IniHelper.GetAllKeyValues("markAreaMinMax", out keys, out values, fullFileName);
            if (values.Length != 2)
            {
                return Errortype.UBT_CALIB_TABLEINFO_FILE_ERROR;
            }

            _markAreaMin = Convert.ToDouble(values[0]);
            _markAreaMax = Convert.ToDouble(values[1]);
            IniHelper.GetAllKeyValues("angleInfo", out keys, out values, fullFileName);
            if (values.Length != 1)
            {
                List<string> key = new List<string>();
                List<string> value = new List<string>();
                key.Add("AngleRate");
                value.Add(_angleRate.ToString());
                IniHelper.AddSectionWithKeyValues("angleInfo", key, value, fullFileName);
            }
            else
            {
                _angleRate = Convert.ToDouble(values[0]);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取特征点真值
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <returns>返回特征点真值</returns>
        public Point GetFeaturePoint(string cameraName)
        {
            if (_featurePointsAtReal.ContainsKey(cameraName))
            {
                return _featurePointsAtReal[cameraName];
            }

            return null;
        }

        /// <summary>
        /// 获取特征点到圆心和行的夹角
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <returns>返回特征点真值</returns>
        public double GetFeatureAngle(string cameraName)
        {
            if (_featurePointsAngle.ContainsKey(cameraName))
            {
                return _featurePointsAngle[cameraName];
            }

            return -999;
        }

        /// <summary>
        /// 设置单个视野标定板特征点
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="featurePoint">特征点真值坐标</param>
        /// <returns>UBT_CAMERA_DATA_NULL：没有该名称相机</returns>
        public Errortype SetFeaturePoint(string cameraName, Point featurePoint)
        {
            if (_featurePointsAtReal.ContainsKey(cameraName))
            {
                _featurePointsAtReal[cameraName] = featurePoint;
                return Errortype.OK;
            }

            return Errortype.UBT_CAMERA_DATA_NULL;
        }

        /// <summary>
        /// 设置单个视野标定板特征点圆心角
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="featureAngleDeg">特征点圆心线行夹角</param>
        /// <returns>UBT_CAMERA_DATA_NULL：没有该名称相机</returns>
        public Errortype SetFeatureAngle(string cameraName, double featureAngleDeg)
        {
            if (_featurePointsAngle.ContainsKey(cameraName))
            {
                _featurePointsAngle[cameraName] = featureAngleDeg;
                return Errortype.OK;
            }

            return Errortype.UBT_CAMERA_DATA_NULL;
        }
    }

    /// <summary>
    /// 宽卡尺抓边预设弧信息
    /// </summary>
    public class GrabEdgeInfo : Singleton<GrabEdgeInfo>
    {
        private static List<string> _cameraNames = new List<string>() { "CameraLeft", "CameraRight", "CameraUp", "CameraDown" };

        private static Dictionary<string, Point> _edgeRingCenter = new Dictionary<string, Point>
        {
            { _cameraNames[0], new Point(12814.3, 1495.84) },
            { _cameraNames[1], new Point(-9422.14, 1550.28) },
            { _cameraNames[2], new Point(1829.14, 12522.7) },
            { _cameraNames[3], new Point(1748.84, -9491.1) },
        };

        private static Dictionary<string, double> _edgeStartPhi = new Dictionary<string, double>
        {
            { _cameraNames[0], 3.03961 },
            { _cameraNames[1],  6.159 },
            { _cameraNames[2],  1.50097 },
            { _cameraNames[3],  4.57125 },
        };

        private static Dictionary<string, double> _edgeEndPhi = new Dictionary<string, double>
        {
            { _cameraNames[0], 3.24699 },
            { _cameraNames[1],  0.12 },
            { _cameraNames[2],  1.71946 },
            { _cameraNames[3],   4.79051 },
        };

        private static Dictionary<string, double> _edgeRingRadius = new Dictionary<string, double>
        {
            { _cameraNames[0], 11000.0 },
            { _cameraNames[1],  10900.0 },
            { _cameraNames[2],  10800.0 },
            { _cameraNames[3],   11000.0 },
        };

        private static Dictionary<string, bool> _edgeWithNotch = new Dictionary<string, bool>
        {
            { _cameraNames[0], false },
            { _cameraNames[1], true },
            { _cameraNames[2], false },
            { _cameraNames[3], false },
        };

        private int _useMotionCalib = 0;
        private int _notchPixSize = 120;
        private int _glassGrabWidth = 380;
        private int _waferGrabWidth = 380;
        private double _circleDevThresh = 0.06;
        private string _waferNotchTemplateName = "defalut_notch";
        private string _glassNotchTemplateName = "defalut_notch";
        private int _useWaferDieAngle = 0;
        private int _useGlassDieAngle = 0;

        /// <summary>
        /// 配置相机名称
        /// </summary>
        /// <param name="names">所有相机的名称</param>
        /// <returns>ok：配置成功</returns>
        public Errortype SetCameraNames(List<string> names)
        {
            _cameraNames = names;

            //_edgeRingCenter = new Dictionary<string, Point>();
            //_edgeStartPhi = new Dictionary<string, double>();
            //_edgeEndPhi = new Dictionary<string, double>();
            //_edgeRingRadius = new Dictionary<string, double>();
            //_edgeWithNotch = new Dictionary<string, bool>();
            foreach (var camera in names)
            {
                if (!_edgeRingCenter.ContainsKey(camera))
                {
                    _edgeRingCenter.Add(camera, null);
                }

                if (!_edgeStartPhi.ContainsKey(camera))
                {
                    _edgeStartPhi.Add(camera, 0);
                }

                if (!_edgeEndPhi.ContainsKey(camera))
                {
                    _edgeEndPhi.Add(camera, 0);
                }

                if (!_edgeRingRadius.ContainsKey(camera))
                {
                    _edgeRingRadius.Add(camera, 0);
                }

                if (!_edgeWithNotch.ContainsKey(camera))
                {
                    _edgeWithNotch.Add(camera, false);
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// Gets or sets 是否使用运动系标定
        /// </summary>
        public int EnableMotionClib
        {
            get => _useMotionCalib;
            set => _useMotionCalib = value;
        }

        /// <summary>
        /// Gets or sets 玻璃是在内侧边
        /// </summary>
        public int NotchPixSize
        {
            get => _notchPixSize;
            set => _notchPixSize = value;
        }

        /// <summary>
        /// Gets or sets 玻璃抓边宽度
        /// </summary>
        public int GlassGrabWidth
        {
            get => _glassGrabWidth;
            set => _glassGrabWidth = value;
        }

        /// <summary>
        /// Gets or sets wafer抓边宽度
        /// </summary>
        public int WaferGrabWidth
        {
            get => _waferGrabWidth;
            set => _waferGrabWidth = value;
        }

        /// <summary>
        /// Gets or sets wafer抓边宽度
        /// </summary>
        public double CirleDevThresh
        {
            get => _circleDevThresh;
            set => _circleDevThresh = value;
        }

        /// <summary>
        /// Gets or sets wafer notch模板名称
        /// </summary>
        public string WaferNotchTemplateName
        {
            get => _waferNotchTemplateName;
            set => _waferNotchTemplateName = value;
        }

        /// <summary>
        /// Gets or sets glass notch模板名称
        /// </summary>
        public string GlassNotchTemplateName
        {
            get => _glassNotchTemplateName;
            set => _glassNotchTemplateName = value;
        }

        /// <summary>
        /// Gets or sets 是否使用die行列角度作为wafer角度
        /// </summary>
        public int UseWaferDieAngle
        {
            get => _useWaferDieAngle;
            set => _useWaferDieAngle = value;
        }

        /// <summary>
        /// Gets or sets 是否使用die行列角度作为wafer角度
        /// </summary>
        public int UseGlassDieAngle
        {
            get => _useGlassDieAngle;
            set => _useGlassDieAngle = value;
        }

        /// <summary>
        /// 获取对应相机抓边弧的圆心
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <returns>返回抓边弧的圆心</returns>
        public Point GetEdgeRingCenter(string cameraName)
        {
            if (_edgeRingCenter.ContainsKey(cameraName))
            {
                return _edgeRingCenter[cameraName];
            }

            return null;
        }

        /// <summary>
        /// 获取抓边弧起始点弧度
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <returns>返回起始点弧度</returns>
        public double GetEdgeRingStartPhi(string cameraName)
        {
            if (_edgeStartPhi.ContainsKey(cameraName))
            {
                return _edgeStartPhi[cameraName];
            }

            return 0.0;
        }

        /// <summary>
        /// 获取抓边弧起始点弧度
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <returns>返回结束点弧度</returns>
        public double GetEdgeRingEndPhi(string cameraName)
        {
            if (_edgeEndPhi.ContainsKey(cameraName))
            {
                return _edgeEndPhi[cameraName];
            }

            return 0.0;
        }

        /// <summary>
        /// 获取抓边弧半径
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <returns>返回弧半径</returns>
        public double GetEdgeRingRadius(string cameraName)
        {
            if (_edgeRingRadius.ContainsKey(cameraName))
            {
                return _edgeRingRadius[cameraName];
            }

            return 0.0;
        }

        /// <summary>
        /// 获取各视野是否有notch
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <returns>返回是否有notch</returns>
        public bool GetEdgeHasNotch(string cameraName)
        {
            if (_edgeRingRadius.ContainsKey(cameraName))
            {
                return _edgeWithNotch[cameraName];
            }

            return false;
        }

        /// <summary>
        /// 设置单个视野抓边圆弧预设中心
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="edgeRingCenter">特征点真值坐标</param>
        /// <returns>UBT_CAMERA_DATA_NULL：没有该名称相机</returns>
        public Errortype SetEdgeRingCenter(string cameraName, Point edgeRingCenter)
        {
            if (_edgeRingCenter.ContainsKey(cameraName))
            {
                _edgeRingCenter[cameraName] = edgeRingCenter;
                return Errortype.OK;
            }

            return Errortype.UBT_CAMERA_DATA_NULL;
        }

        /// <summary>
        /// 设置单个视野抓边圆弧起始角
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="edgeRingStartPhi">圆弧起始角</param>
        /// <returns>UBT_CAMERA_DATA_NULL：没有该名称相机</returns>
        public Errortype SetEdgeRingStartPhi(string cameraName, double edgeRingStartPhi)
        {
            if (_edgeStartPhi.ContainsKey(cameraName))
            {
                _edgeStartPhi[cameraName] = edgeRingStartPhi;
                return Errortype.OK;
            }

            return Errortype.UBT_CAMERA_DATA_NULL;
        }

        /// <summary>
        /// 设置单个视野抓边圆弧结束角
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="edgeRingEndPhi">圆弧结束角</param>
        /// <returns>UBT_CAMERA_DATA_NULL：没有该名称相机</returns>
        public Errortype SetEdgeRingEndPhi(string cameraName, double edgeRingEndPhi)
        {
            if (_edgeEndPhi.ContainsKey(cameraName))
            {
                _edgeEndPhi[cameraName] = edgeRingEndPhi;
                return Errortype.OK;
            }

            return Errortype.UBT_CAMERA_DATA_NULL;
        }

        /// <summary>
        /// 设置单个视野抓边圆弧半径
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="edgeRingRadius">圆弧半径</param>
        /// <returns>UBT_CAMERA_DATA_NULL：没有该名称相机</returns>
        public Errortype SetEdgeRingRadius(string cameraName, double edgeRingRadius)
        {
            if (_edgeRingRadius.ContainsKey(cameraName))
            {
                _edgeRingRadius[cameraName] = edgeRingRadius;
                return Errortype.OK;
            }

            return Errortype.UBT_CAMERA_DATA_NULL;
        }

        /// <summary>
        /// 设置单个视野是否抓notch
        /// </summary>
        /// <param name="cameraName">相机名称</param>
        /// <param name="hasNotch">视野内是否有notch</param>
        /// <returns>UBT_CAMERA_DATA_NULL：没有该名称相机</returns>
        public Errortype SetEdgeGrabNotch(string cameraName, bool hasNotch)
        {
            if (_edgeWithNotch.ContainsKey(cameraName))
            {
                _edgeWithNotch[cameraName] = hasNotch;
                return Errortype.OK;
            }

            return Errortype.UBT_CAMERA_DATA_NULL;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="fileDir">设定保存目录</param>
        /// <returns>返回保存结果</returns>
        public Errortype Save(string fileDir)
        {
            if (!System.IO.Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            string fullFileName = fileDir + "/" + "GrabEdgeInfo.ini";
            if (File.Exists(fullFileName))
            {
                File.Delete(fullFileName);
            }

            List<string> keys = new List<string>();
            List<string> value = new List<string>();
            keys.Clear();
            value.Clear();
            keys.Add("CameraNums");
            value.Add(_cameraNames.Count.ToString());
            IniHelper.AddSectionWithKeyValues("cameraInfo", keys, value, fullFileName);

            keys.Clear();
            value.Clear();

            foreach (var cameraName in _cameraNames)
            {
                keys.Add(cameraName + "_x");
                value.Add(_edgeRingCenter[cameraName].X.ToString());
                keys.Add(cameraName + "_y");
                value.Add(_edgeRingCenter[cameraName].Y.ToString());
            }

            IniHelper.AddSectionWithKeyValues("edgeRingCenter", keys, value, fullFileName);

            keys.Clear();
            value.Clear();
            foreach (var cameraName in _cameraNames)
            {
                keys.Add(cameraName);
                value.Add(_edgeStartPhi[cameraName].ToString());
            }

            IniHelper.AddSectionWithKeyValues("edgeStartPhi", keys, value, fullFileName);

            keys.Clear();
            value.Clear();
            foreach (var cameraName in _cameraNames)
            {
                keys.Add(cameraName);
                value.Add(_edgeEndPhi[cameraName].ToString());
            }

            IniHelper.AddSectionWithKeyValues("edgeEndPhi", keys, value, fullFileName);

            keys.Clear();
            value.Clear();
            foreach (var cameraName in _cameraNames)
            {
                keys.Add(cameraName);
                value.Add(_edgeWithNotch[cameraName].ToString());
            }

            IniHelper.AddSectionWithKeyValues("edgeHasNotch", keys, value, fullFileName);

            keys.Clear();
            value.Clear();
            foreach (var cameraName in _cameraNames)
            {
                keys.Add(cameraName);
                value.Add(_edgeRingRadius[cameraName].ToString());
            }

            keys.Add("EnableMotioncalib");
            keys.Add("NotchPixSize");
            keys.Add("GlassGrabWidth");
            keys.Add("waferGrabWidth");
            keys.Add("circleDevThresh");

            value.Add(_useMotionCalib.ToString());
            value.Add(_notchPixSize.ToString());
            value.Add(_glassGrabWidth.ToString());
            value.Add(_waferGrabWidth.ToString());
            value.Add(_circleDevThresh.ToString());
            IniHelper.AddSectionWithKeyValues("edgeRingRadius", keys, value, fullFileName);

            keys.Clear();
            value.Clear();

            keys.Add("WaferNotchTemplateName");
            keys.Add("GlassNotchTemplateName");

            value.Add(_waferNotchTemplateName);
            value.Add(_glassNotchTemplateName);
            IniHelper.AddSectionWithKeyValues("notchTemplates", keys, value, fullFileName);

            return Errortype.OK;
        }

        /// <summary>
        /// 载入
        /// </summary>
        /// <param name="fileDir">文件目录</param>
        /// <returns>返回保存结果</returns>
        public Errortype Load(string fileDir)
        {
            string fullFileName = fileDir + "/" + "GrabEdgeInfo.ini";

            if (!File.Exists(fullFileName))
            {
                var ret = Save(fileDir);
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }

            if ((!IniHelper.ExistSection("edgeRingCenter", fullFileName)) || (!IniHelper.ExistSection("edgeStartPhi", fullFileName)) || (!IniHelper.ExistSection("edgeEndPhi", fullFileName)) || (!IniHelper.ExistSection("edgeRingRadius", fullFileName)))
            {
                return Errortype.UBT_CALIB_TABLEINFO_FILE_ERROR;
            }

            Console.WriteLine("load GrabEdgeInfo:" + fullFileName);
            string[] keys = null;
            string[] values = null;

            IniHelper.GetAllKeyValues("cameraInfo", out keys, out values, fullFileName);
            if (values.Length != 1)
            {
                return Errortype.UBT_GRAB_EDGEINFO_FILE_ERROR;
            }

            int cameraNums = Convert.ToInt32(values[0]);

            IniHelper.GetAllKeyValues("edgeRingCenter", out keys, out values, fullFileName);
            if (values.Length != (cameraNums * 2))
            {
                return Errortype.UBT_GRAB_EDGEINFO_FILE_ERROR;
            }

            for (int i = 0; i < cameraNums; i++)
            {
                _edgeRingCenter[_cameraNames[i]] = new Point(Convert.ToDouble(values[i * 2]), Convert.ToDouble(values[i * 2 + 1]));
            }

            IniHelper.GetAllKeyValues("edgeStartPhi", out keys, out values, fullFileName);
            if (values.Length != _cameraNames.Count)
            {
                return Errortype.UBT_GRAB_EDGEINFO_FILE_ERROR;
            }

            for (int i = 0; i < cameraNums; i++)
            {
                _edgeStartPhi[_cameraNames[i]] = Convert.ToDouble(values[i]);
            }

            IniHelper.GetAllKeyValues("edgeEndPhi", out keys, out values, fullFileName);
            if (values.Length != _cameraNames.Count)
            {
                return Errortype.UBT_GRAB_EDGEINFO_FILE_ERROR;
            }

            for (int i = 0; i < cameraNums; i++)
            {
                _edgeEndPhi[_cameraNames[i]] = Convert.ToDouble(values[i]);
            }

            IniHelper.GetAllKeyValues("edgeHasNotch", out keys, out values, fullFileName);
            if (values.Length != _cameraNames.Count)
            {
                return Errortype.UBT_GRAB_EDGEINFO_FILE_ERROR;
            }

            for (int i = 0; i < cameraNums; i++)
            {
                _edgeWithNotch[_cameraNames[i]] = Convert.ToBoolean(values[i]);
            }

            IniHelper.GetAllKeyValues("edgeRingRadius", out keys, out values, fullFileName);
            if (values.Length != (_cameraNames.Count + 5))
            {
                return Errortype.UBT_GRAB_EDGEINFO_FILE_ERROR;
            }

            for (int i = 0; i < cameraNums; i++)
            {
                _edgeRingRadius[_cameraNames[i]] = Convert.ToDouble(values[i]);
            }

            // 其他信息
            _useMotionCalib = Convert.ToInt32(values[cameraNums + 0]);
            _notchPixSize = Convert.ToInt32(values[cameraNums + 1]);
            _glassGrabWidth = Convert.ToInt32(values[cameraNums + 2]);
            _waferGrabWidth = Convert.ToInt32(values[cameraNums + 3]);
            _circleDevThresh = Convert.ToDouble(values[cameraNums + 4]);

            // notch模板信息
            IniHelper.GetAllKeyValues("notchTemplates", out keys, out values, fullFileName);
            if (values.Length != 2)
            {
                return Errortype.UBT_GRAB_EDGEINFO_FILE_ERROR;
            }

            _waferNotchTemplateName = values[0];
            _glassNotchTemplateName = values[1];

            return Errortype.OK;
        }
    }
}
