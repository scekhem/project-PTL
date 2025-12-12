using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using DataStruct;
using MaxwellFramework.Core.Interfaces;
using MwFramework.Device;
using MwTrainTemplate.Component.TemplateMatch.View;
using MwTrainTemplate.Common;
using Stylet;
using System.Windows.Media.Imaging;
using MessageBox = MaxwellControl.Controls.MessageBox;
using MwFramework.Device.Model;
using MwFramework.ManagerService;
using System.IO;
using UltrapreciseBonding.TemplateMatch;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using System.Threading;
using Point = DataStruct.Point;
using StyletIoC;
using Microsoft.Win32;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DialogResult = System.Windows.Forms.DialogResult;


namespace MwTrainTemplate.Component.TemplateMatch.ViewModel
{
    public class TrainTemplateViewModel : Screen, IPage
    {
        public string Name = "TrainTemplateView";

        public string TemplateName { get; set; }
        public string TemplatePath { get; set; }

        public string MatchRegionName { get; set; }
        public string MatchRegionPath { get; set; }

        private TemplateType TemplateType { get; set; } = TemplateType.NCC;

        /// <summary>
        /// 相机流
        /// </summary>
        private MwCamera _slectedCamera;
        public MwCamera SelectedCamera
        {
            get => _slectedCamera;
            set
            {
                if (_slectedCamera != null && _slectedCamera.IsGrabbing)
                {
                    _slectedCamera.StopGrabbing();
                }
                if (value != null && !value.IsGrabbing)
                {
                    value.StartGrabbing();
                }
                SetAndNotify(ref _slectedCamera, value);
                _camera = _slectedCamera;
            }
        }

        /// <summary>
        /// 获取哪个相机的相机流
        /// </summary>
        public ObservableCollection<MwCamera> CameraList { get; set; } = new ObservableCollection<MwCamera>();

        public List<string> TemplateList { get; set; } = new List<string>();

        private NccTemplateParams NccTemplateParams { get; set; } = new NccTemplateParams();

        private NccMatchParams NccMatchParams { get; set; } = new NccMatchParams();

        private ShapeTemplateParams ShapeTemplateParams { get; set; } = new ShapeTemplateParams();

        private ShapeMatchParams ShapeMatchParams { get; set; } = new ShapeMatchParams();

        private TrainNccTemplateSetupParameter _trainNccTemplateSetupParameter;
        private TrainShapeTemplateSetupParameter _trainShapeTemplateSetupParameter;

        private MwCamera _camera;

        /// <summary>
        /// interface
        /// </summary>
        [Inject]
        public IDeviceManager DeviceManager { get; set; }

        public TrainTemplateViewModel()
        { }

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="templateName">模板名称</param>
        /// <param name="templateType">模板类型</param>
        /// <param name="camera">相机流</param>
        /// <param name="writeableBitmap">图片流</param>
        public TrainTemplateViewModel(string templateName, TemplateType templateType = TemplateType.NCC, MwCamera camera = null, WriteableBitmap writeableBitmap = null)
        {
            if (CameraList != null && CameraList.Count > 0)
            {
                SelectedCamera = CameraList[0];
            }

            TemplateName = templateName;
            TemplateType = templateType;
            
            Image = writeableBitmap;
            _trainNccTemplateSetupParameter = new TrainNccTemplateSetupParameter();
            _trainShapeTemplateSetupParameter = new TrainShapeTemplateSetupParameter();

            string templatePath = AppDomain.CurrentDomain.BaseDirectory + "TemplateModel";
            TemplateManager.Load(templatePath, TemplateName);
        }

        /// <summary>
        /// UBD2有参构造函数
        /// </summary>
        /// <param name="cameraList">相机列表</param>
        /// <param name="templateDir">模版文件目录</param>
        /// <param name="templateType">模版类型，默认NCC</param>
        public TrainTemplateViewModel(List<MwCamera> cameraList, string templateDir, TemplateType templateType = TemplateType.NCC)
        {
            foreach (var ca in cameraList)
            {
                CameraList.Add(ca);
            }

            if (CameraList != null && CameraList.Count > 0)
            {
                SelectedCamera = CameraList[0];
            }

            string[] dirs = Directory.GetDirectories(templateDir); // 获取所有子目录的完整路径

            foreach (string dir in dirs)
            {
                TemplateList.Add(Path.GetFileNameWithoutExtension(dir));
            }

            TemplateName = TemplateList[0];
            TemplateType = templateType;
            TemplatePath = templateDir;

            _trainNccTemplateSetupParameter = new TrainNccTemplateSetupParameter();
            _trainShapeTemplateSetupParameter = new TrainShapeTemplateSetupParameter();

            TemplateManager.Load(TemplatePath, TemplateName);

        }

        private bool _setNccModel = true;
        public bool SetNccModel
        {
            get { return _setNccModel; }
            set
            {
                _setNccModel = value;
                OnPropertyChanged(nameof(SetNccModel));
            }
        }

        private bool _setShapeModel = false;
        public bool SetShapeModel
        {
            get { return _setShapeModel; }
            set
            {
                _setShapeModel = value;
                OnPropertyChanged(nameof(SetShapeModel));
            }
        }

        private ObservableCollection<TrainSetupParameterItem> _createParamsItemsList = new ObservableCollection<TrainSetupParameterItem>();
        public ObservableCollection<TrainSetupParameterItem> CreateParamsItemsList
        {
            get
            {
                return _createParamsItemsList;
            }
            set
            {
                _createParamsItemsList = value;
                OnPropertyChanged(nameof(CreateParamsItemsList));
            }
        }

        private ObservableCollection<TrainSetupEnumItem> _createEnumsItemsList = new ObservableCollection<TrainSetupEnumItem>();
        public ObservableCollection<TrainSetupEnumItem> CreateEnumsItemsList
        {
            get
            {
                return _createEnumsItemsList;
            }
            set
            {
                _createEnumsItemsList = value;
                OnPropertyChanged(nameof(CreateEnumsItemsList));
            }
        }

        private ObservableCollection<TrainSetupParameterItem> _matchParamsItemsList = new ObservableCollection<TrainSetupParameterItem>();
        public ObservableCollection<TrainSetupParameterItem> MatchParamsItemsList
        {
            get
            {
                return _matchParamsItemsList;
            }
            set
            {
                _matchParamsItemsList = value;
                OnPropertyChanged(nameof(MatchParamsItemsList));
            }
        }

        private ObservableCollection<TrainSetupEnumItem> _matchEnumsItemsList = new ObservableCollection<TrainSetupEnumItem>();
        public ObservableCollection<TrainSetupEnumItem> MatchEnumsItemsList
        {
            get
            {
                return _matchEnumsItemsList;
            }
            set
            {
                _matchEnumsItemsList = value;
                OnPropertyChanged(nameof(MatchEnumsItemsList));
            }
        }

        private ObservableCollection<MatchParamter> _matchParameterList = new ObservableCollection<MatchParamter>();
        public ObservableCollection<MatchParamter> MatchParameterList
        {
            get
            {
                return _matchParameterList;
            }
            set
            {
                _matchParameterList = value;
                OnPropertyChanged(nameof(MatchParameterList));
            }
        }

        private readonly ManualResetEvent _pausEvent = new ManualResetEvent(false);
        private CancellationTokenSource _cancellation;

        /// <summary>
        /// 单张图像的mark中心结果
        /// </summary>
        private string _singleImageCenterResult = string.Empty;
        public string SingleImageCenterResult
        {
            get { return _singleImageCenterResult; }
            set { _singleImageCenterResult = value; OnPropertyChanged(nameof(SingleImageCenterResult)); }
        }

        /// <summary>
        /// 窗口图像
        /// </summary>
        public WriteableBitmap _image;
        public WriteableBitmap Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        private TrainTemplateView _thisView;

        private List<Camera> _offLineImages = null;
        private bool _isLoad = false;
        private double _progressValue = 0;
        public double ProgressValue
        {
            get
            {
                return _progressValue;
            }
            set
            {
                _progressValue = value;
                OnPropertyChanged(nameof(ProgressValue));
            }
        }

        private string _parameterXUnit = "X[nm]";
        public string ParameterXUnit
        {
            get
            {
                return _parameterXUnit;
            }
            set
            {
                _parameterXUnit = value;
                OnPropertyChanged(nameof(ParameterXUnit));
            }
        }

        private string _parameterYUnit = "Y[nm]";
        public string ParameterYUnit
        {
            get
            {
                return _parameterYUnit;
            }
            set
            {
                _parameterYUnit = value;
                OnPropertyChanged(nameof(ParameterYUnit));
            }
        }

        private int _interVal;
        public int InterVal
        {
            get
            {
                return _interVal;
            }
            set
            {
                _interVal = value;
                OnPropertyChanged(nameof(InterVal));
            }
        }

        /// <summary>
        /// 调整准星旋转
        /// </summary>
        private double _rotation = 0;
        public double Rotation
        {
            get { return _rotation; }
            set
            {
                _rotation = value;
                OnPropertyChanged(nameof(Rotation));
            }
        }

        /// <summary>
        /// 线宽
        /// </summary>
        private double _lineWidth = 300;
        public double LineWidth
        {
            get { return _lineWidth; }
            set
            {
                _lineWidth = value;
                OnPropertyChanged(nameof(LineWidth));
            }
        }

        /// <summary>
        /// 调整准星X,Y大小
        /// </summary>
        private int _lineSize = 1;
        public int LineSize
        {
            get { return _lineSize; }
            set
            {
                _lineSize = value;
                OnPropertyChanged(nameof(LineSize));
            }
        }

        private int _repeatings;
        public int Repeatings
        {
            get
            {
                return _repeatings;
            }
            set
            {
                _repeatings = value;
                OnPropertyChanged(nameof(Repeatings));
            }
        }

        public delegate bool GetRulerByPixDelegate(string itemName, Point rulerCurrent, Point pixel, out Point ruler);
        private GetRulerByPixDelegate _getRulerByPixDelegate;

        /// <summary>
        /// 图像显示控件
        /// </summary>
        private DesignPanel _designPanel;

        protected override void OnViewLoaded()
        {
            _thisView = this.View as TrainTemplateView;
            if (TemplateType == TemplateType.NCC)
            {
                SetNccModel = true;
                SetNccModelClick();
            }
            else if (TemplateType == TemplateType.SHAPE)
            {
                SetShapeModel = true;
                SetShapeModelClick();
            }

            IsAcquistion = false;
            if (_camera != null)
            {
                Camera image = ImageHelper.GetCamera(_camera.SnapImage());
                Image = ImageHelper.Camera2WritableBitmap(image);
            }

            _designPanel = _thisView.cameraPanel;

            foreach (string name in TemplateList)
            {
                _thisView.templateComboBox.Items.Add(name);
            }
        }

        private bool _isAcquistion;
        public bool IsAcquistion
        {
            get { return _isAcquistion; }
            set { _isAcquistion = value; OnPropertyChanged(nameof(IsAcquistion)); }
        }

        /// <summary>
        /// 开启连续采集
        /// </summary>
        public void ContinueAcquistionCheck()
        {
            if (_camera == null)
            {
                IsAcquistion = false;
                MessageBox.Show("请先选择相机!");
                return;
            }

            _pausEvent.Set();
            _cancellation = new CancellationTokenSource();
            IsAcquistion = true;
            new Thread(() =>
            {
                while (!_cancellation.IsCancellationRequested)
                {
                    _pausEvent.WaitOne();
                    Camera image = ImageHelper.GetCamera(_camera.SnapImage());
                    if (image == null)
                    {
                        break;
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            Image = ImageHelper.Camera2WritableBitmap(image);
                        });
                        image.Dispose();
                    }
                }

            }).Start();
        }

        /// <summary>
        /// 释放线程
        /// </summary>
        protected override void OnDeactivate()
        {
            base.OnDeactivate();
            _cancellation?.Cancel();
        }

        /// <summary>
        /// 关闭连续采集
        /// </summary>
        public void ContinueAcquistionUnCheck()
        {
            IsAcquistion = false;
            OnDeactivate();
        }

        public void SetNccModelClick()
        {
            _trainNccTemplateSetupParameter.Read(System.IO.Path.Combine(GlobalPathHelper.G_ParameterConfigurePath, _trainNccTemplateSetupParameter.GetType().Name + TemplateName + ".xml"));
            _trainNccTemplateSetupParameter.Initialize();
            CreateParamsItemsList = _trainNccTemplateSetupParameter.CreateParamsItems;
            CreateEnumsItemsList = _trainNccTemplateSetupParameter.CreateEnumsItems;
            MatchParamsItemsList = _trainNccTemplateSetupParameter.MatchParamsItems;
            MatchEnumsItemsList = _trainNccTemplateSetupParameter.MatchEnumsItems;
            TemplateType = TemplateType.NCC;
        }

        public void SetShapeModelClick()
        {
            _trainShapeTemplateSetupParameter.Read(System.IO.Path.Combine(GlobalPathHelper.G_ParameterConfigurePath, _trainShapeTemplateSetupParameter.GetType().Name + TemplateName + ".xml"));
            _trainShapeTemplateSetupParameter.Initialize();
            CreateParamsItemsList = _trainShapeTemplateSetupParameter.CreateParamsItems;
            CreateEnumsItemsList = _trainShapeTemplateSetupParameter.CreateEnumsItems;
            MatchParamsItemsList = _trainShapeTemplateSetupParameter.MatchParamsItems;
            MatchEnumsItemsList = _trainShapeTemplateSetupParameter.MatchEnumsItems;
            TemplateType = TemplateType.SHAPE;
        }

        /// <summary>
        /// 参数容器，方便以Key-Value形式存取参数
        /// </summary>
        private Dictionary<string, string> _createParams = new Dictionary<string, string>();
        private Dictionary<string, string> _matchParams = new Dictionary<string, string>();

        /// <summary>
        /// 将界面参数更新进参数容器中
        /// </summary>
        private void SetParams()
        {
            _createParams = new Dictionary<string, string>();
            _matchParams = new Dictionary<string, string>();
            for (int i = 0; i < CreateParamsItemsList.Count; ++i)
            {
                _createParams.Add(CreateParamsItemsList[i].ItemName, CreateParamsItemsList[i].ItemValue);
            }

            for (int i = 0; i < CreateEnumsItemsList.Count; ++i)
            {
                int index = CreateEnumsItemsList[i].ItemIndex;
                _createParams.Add(CreateEnumsItemsList[i].ItemName, CreateEnumsItemsList[i].TemplateType[index]);
            }

            for (int i = 0; i < MatchParamsItemsList.Count; ++i)
            {
                _matchParams.Add(MatchParamsItemsList[i].ItemName, MatchParamsItemsList[i].ItemValue);
            }

            for (int i = 0; i < MatchEnumsItemsList.Count; ++i)
            {
                int index = MatchEnumsItemsList[i].ItemIndex;
                _matchParams.Add(MatchEnumsItemsList[i].ItemName, MatchEnumsItemsList[i].TemplateType[index]);
            }
        }

        /// <summary>
        /// 获取视窗内的矩形
        /// </summary>
        /// <param name="rectangle1"></param>
        /// <returns></returns>
        private bool GetViewRectangle(out Rectangle1 rectangle1)
        {
            rectangle1 = new Rectangle1();

            ImagePanelOperation.GetViewRectangle(_thisView.cameraPanel, out List<Rectangle1> regions);

            if (regions.Count < 1)
            {
                _thisView.cameraPanel.SelectedDrawingShapeList.Clear();
                _thisView.cameraPanel.Clear();
                MaxwellControl.Controls.MessageBox.Show("未识别到矩形框");
                return false;
            }

            if (regions.Count > 1)
            {
                _thisView.cameraPanel.SelectedDrawingShapeList.Clear();
                _thisView.cameraPanel.Clear();
                MaxwellControl.Controls.MessageBox.Show("识别到多个矩形框");
                return false;
            }

            rectangle1 = regions[0];
            return true;
        }

        /// <summary>
        /// 将界面NCC参数赋值给模板参数
        /// </summary>
        private void SetNccParam()
        {
            NccTemplateParams.Pyramid = int.Parse(_createParams["Pyramid"]);
            NccTemplateParams.AngleStart = double.Parse(_createParams["AngleStart"]);
            NccTemplateParams.AngleExtent = double.Parse(_createParams["AngleExtent"]);
            NccTemplateParams.AngleStep = _createParams["AngleStep"];
            NccTemplateParams.Metric = _createParams["Metric"];
            NccTemplateParams.SmoothImg = _createParams["SmoothImg"] == "true" ? true : false;
            NccTemplateParams.FilterSize = int.Parse(_createParams["FilterSize"]);

            NccMatchParams.AngleStart = double.Parse(_matchParams["AngleStart"]);
            NccMatchParams.AngleExtent = double.Parse(_matchParams["AngleExtent"]);
            NccMatchParams.MinScore = double.Parse(_matchParams["MinScore"]);
            NccMatchParams.NumMatches = int.Parse(_matchParams["NumMatches"]);
            NccMatchParams.MaxOverlap = double.Parse(_matchParams["MaxOverlap"]);
            NccMatchParams.SubPixel = _matchParams["SubPixel"] == "true" ? true : false;
            NccMatchParams.Pyramid = int.Parse(_matchParams["Pyramid"]);
            NccMatchParams.SmoothImg = _matchParams["SmoothImg"] == "true" ? true : false;
            NccMatchParams.FilterSize = int.Parse(_matchParams["FilterSize"]);
        }

        /// <summary>
        /// 将界面Shape参数赋值给模板参数
        /// </summary>
        private void SetShapeParam()
        {
            ShapeTemplateParams.Pyramid = int.Parse(_createParams["Pyramid"]);
            ShapeTemplateParams.AngleStart = double.Parse(_createParams["AngleStart"]);
            ShapeTemplateParams.AngleExtent = double.Parse(_createParams["AngleExtent"]);
            ShapeTemplateParams.AngleStep = _createParams["AngleStep"];
            ShapeTemplateParams.ScaleMin = double.Parse(_createParams["ScaleMin"]);
            ShapeTemplateParams.ScaleMax = double.Parse(_createParams["ScaleMax"]);
            ShapeTemplateParams.ScaleStep = _createParams["ScaleStep"];
            ShapeTemplateParams.Optimization = _createParams["Optimization"];
            ShapeTemplateParams.Metric = _createParams["Metric"];
            ShapeTemplateParams.Contrast = _createParams["Contrast"];
            ShapeTemplateParams.MinContrast = _createParams["MinContrast"];
            ShapeTemplateParams.SmoothImg = _createParams["SmoothImg"] == "true" ? true : false;
            ShapeTemplateParams.FilterSize = int.Parse(_createParams["FilterSize"]);

            ShapeMatchParams.AngleStart = double.Parse(_matchParams["AngleStart"]);
            ShapeMatchParams.AngleExtent = double.Parse(_matchParams["AngleExtent"]);
            ShapeMatchParams.ScaleMin = double.Parse(_matchParams["ScaleMin"]);
            ShapeMatchParams.ScaleMax = double.Parse(_matchParams["ScaleMax"]);
            ShapeMatchParams.MinScore = double.Parse(_matchParams["MinScore"]);
            ShapeMatchParams.NumMatches = int.Parse(_matchParams["NumMatches"]);
            ShapeMatchParams.MaxOverlap = double.Parse(_matchParams["MaxOverlap"]);
            ShapeMatchParams.SubPixel = _matchParams["SubPixel"];
            ShapeMatchParams.Pyramid = int.Parse(_matchParams["Pyramid"]);
            ShapeMatchParams.Greediness = double.Parse(_matchParams["Greediness"]);
            ShapeMatchParams.SmoothImg = _matchParams["SmoothImg"] == "true" ? true : false;
            ShapeMatchParams.FilterSize = int.Parse(_matchParams["FilterSize"]);
        }

        /// <summary>
        /// 创建ncc模板
        /// </summary>
        /// <param name="img"></param>
        /// <param name="region"></param>
        /// <param name="templateName"></param>
        /// <param name="nccTemplateParams"></param>
        /// <param name="nccMatchParams"></param>
        /// <returns></returns>
        private bool CreateNccTemplate(Camera img, Region region, string templateName, NccTemplateParams nccTemplateParams, NccMatchParams nccMatchParams)
        {
            Errortype ret = TemplateManagerService.GetInstance().Create<NccTemplateParams, NccMatchParams>(templateName, img,
                nccTemplateParams, nccMatchParams, region, null, TemplateType.NCC);
            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("CreateNccTemplateError: " + ret.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// 创建shape模板
        /// </summary>
        /// <param name="img"></param>
        /// <param name="region"></param>
        /// <param name="templateName"></param>
        /// <param name="shapeTemplateParams"></param>
        /// <param name="shapeMatchParams"></param>
        /// <returns></returns>
        private bool CreateShapeTemplate(Camera img, Region region, string templateName, ShapeTemplateParams shapeTemplateParams, ShapeMatchParams shapeMatchParams)
        {
            Errortype ret = TemplateManagerService.GetInstance().Create<ShapeTemplateParams, ShapeMatchParams>(templateName, img,
                shapeTemplateParams, shapeMatchParams, region, null, TemplateType.SHAPE);
            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("CreateShapeTemplateError: " + ret.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        private bool CreateTemplate(Region region)
        {
            bool ret = true;
            Camera img = ImageHelper.GetCamera(Image);
            if (img == null)
            {
                MaxwellControl.Controls.MessageBox.Show("Please Load Image!");
                return false;
            }

            switch (TemplateType)
            {
                case TemplateType.NCC:
                    SetNccParam();
                    _trainNccTemplateSetupParameter.Write(System.IO.Path.Combine(GlobalPathHelper.G_ParameterConfigurePath, _trainNccTemplateSetupParameter.GetType().Name + TemplateName + ".xml"));
                    ret = CreateNccTemplate(img, region, TemplateName, NccTemplateParams, NccMatchParams);
                    break;
                case TemplateType.SHAPE:
                    SetShapeParam();
                    _trainShapeTemplateSetupParameter.Write(System.IO.Path.Combine(GlobalPathHelper.G_ParameterConfigurePath, _trainShapeTemplateSetupParameter.GetType().Name + TemplateName + ".xml"));
                    ret = CreateShapeTemplate(img, region, TemplateName, ShapeTemplateParams, ShapeMatchParams);
                    break;
                default:
                    break;
            }

            img.Dispose();
            return ret;
        }

        /// <summary>
        /// 保存模板
        /// </summary>
        /// <returns></returns>
        private bool SaveTemplate()
        {
            
            string savePath = AppDomain.CurrentDomain.BaseDirectory + "TemplateModel";
            if ((TemplatePath != null) && TemplatePath != String.Empty)
            {
                savePath = TemplatePath;
            }
            Errortype ret = TemplateManagerService.GetInstance().Save(savePath, TemplateName);
            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("SaveTemplateError: " + ret.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// OK 按钮触发事件
        /// </summary>
        public void SetOk()
        {
            bool ret = true;
            ////= GetViewRectangle(out Rectangle1 viewRegion);

            ImagePanelOperation.GetViewRectangle(_thisView.cameraPanel, out List<Rectangle1> regions);
            if (!ret) return;

            SetParams();
            Region templateRegion = new Region();
            templateRegion.Rectangle1List = regions;
            ret = CreateTemplate(templateRegion);
            if (!ret) return;

            ret = SaveTemplate();
            if (!ret) return;


            MaxwellControl.Controls.MessageBox.Show("Create" + TemplateType.ToString() + "Template: OK" + ret.ToString());

            return;
        }

        /// <summary>
        /// Cancel 按钮触发事件
        /// </summary>
        public void SetCancel()
        {
            _thisView.Close();
        }

        /// <summary>
        /// 从相机中获取图像
        /// </summary>
        public void AcquiredImage()
        {
            ContinueAcquistionUnCheck();
            if (_camera is null)
            {
                MessageBox.Show("CameraDeviceNull");
                return;
            }

            CameraData cameraData = _camera.SnapImage();
            if (cameraData is null)
            {
                MessageBox.Show("CameraSnapImageNull");
            }

            Camera image = ImageHelper.GetCamera(cameraData);
            Image = ImageHelper.Camera2WritableBitmap(image);

            image.Dispose();
        }

        /// <summary>
        /// 从本地加载图像
        /// </summary>
        public void LoadImageFromFile()
        {
            ContinueAcquistionUnCheck();
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image File (*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif)|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif";
            if (openFileDialog.ShowDialog().Value)
            {
                try
                {
                    string importFile = openFileDialog.FileName;
                    ImportImage(importFile);
                }
                catch (Exception)
                {
                    MaxwellControl.Controls.MessageBox.Show("LoadFileError");
                }
            }
        }

        /// <summary>
        /// 从文件中获取bitmap
        /// </summary>
        /// <param name="filePath"></param>
        private void ImportImage(string filePath)
        {
            Camera img = new Camera(filePath);
            Image = ImageHelper.Camera2WritableBitmap(img);
            img.Dispose();
        }

        /// <summary>
        /// 匹配单张图像模板
        /// </summary>
        public async void Search()
        {
            //SingleImageCenterResult = string.Empty;

            //_thisView = this.View as TrainTemplateView;
            //ImagePanelOperation.GetViewRectangle(_thisView.cameraPanel, out List<Rectangle1> regions);
            //Region templateMatchRegion = new Region();
            //templateMatchRegion.Rectangle1List = regions;
            //Camera img = ImageHelper.GetCamera(Image);

            //Errortype ret = TemplateManagerService.GetInstance().Match(TemplateName, img, templateMatchRegion,
            //    out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores);

            //if (Errortype.OK != ret)
            //{
            //    MaxwellControl.Controls.MessageBox.Show("TemplateMatchError: " + ret.ToString());
            //    return;
            //}

            //for (int i = 0; i < rows.Length; i++)
            //{
            //    System.Windows.Point center = new System.Windows.Point(cols[i], rows[i]);
            //    ImagePanelOperation.DrawCenterCross(_thisView.cameraPanel, center);
            //    SingleImageCenterResult += new Point(cols[i], rows[i]).ToString(" ");
            //}

            if (_isLoad)
            {
                //await Task.Run(() =>
                //{
                //    SearchOffLine();
                //});
                SearchOffLine();
            }
            else
            {
                SearchOnLine();
            }
        }

        /// <summary>
        /// 读取模板
        /// </summary>
        public void LoadTemplateClick()
        {

            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择一个模版文件夹";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string folderPath = dialog.SelectedPath;
                string templatePath = Directory.GetParent(folderPath).FullName;
                TemplateName = Path.GetFileNameWithoutExtension(folderPath);
                TemplatePath = templatePath;
                var ret = TemplateManagerService.GetInstance().Load(TemplatePath, TemplateName);
                if (ret != Errortype.OK)
                {
                    MessageBox.Show("模版文件异常: " + ret);
                    return;
                }
                MessageBox.Show("载入模版: " + TemplateName);

                Directory.SetCurrentDirectory(Directory.GetParent(templatePath).FullName);
                MatchRegionPath = Directory.GetCurrentDirectory();
                MatchRegionName = TemplateName;

                ret = MatchRegionManager.LoadMatchRegion(MatchRegionName, MatchRegionPath);
                // 在这里处理选中的文件夹路径
                if (ret != Errortype.OK)
                {
                    MessageBox.Show("匹配区域文件异常: " + ret);
                    return;
                }

                MessageBox.Show("载入匹配区域: " + MatchRegionName);
            }
            else
            {
                MessageBox.Show("文件加载失败");
                return;
            }

        }

        public void ChangeTemplate()
        {
            TemplateName = _thisView.templateComboBox.SelectedItem.ToString();
            var ret = TemplateManagerService.GetInstance().Load(TemplatePath, TemplateName);
            if (ret != Errortype.OK)
            {
                MessageBox.Show("模版文件异常: " + ret);
                return;
            }
            MessageBox.Show("载入模版: " + TemplateName);

            Directory.SetCurrentDirectory(Directory.GetParent(TemplatePath).FullName);
            MatchRegionPath = Directory.GetCurrentDirectory();
            MatchRegionName = TemplateName;

            ret = MatchRegionManager.LoadMatchRegion(MatchRegionName, MatchRegionPath);
            // 在这里处理选中的文件夹路径
            if (ret != Errortype.OK)
            {
                MessageBox.Show("匹配区域文件异常: " + ret);
                return;
            }

            MessageBox.Show("载入匹配区域: " + MatchRegionName);
        }


        /// <summary>
        /// 本地加载图像
        /// </summary>
        public void Acquire()
        {
            IsAcquistion = false;
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image File (*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif)|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog().Value)
            {
                try
                {
                    List<string> importFiles = openFileDialog.FileNames.ToList();
                    if (importFiles.Count == 0 || importFiles == null)
                    {
                        MaxwellControl.Controls.MessageBox.Show("ImageError");
                        return;
                    }

                    _offLineImages?.ForEach(camera => camera.Dispose());

                    _offLineImages = new List<Camera>();
                    importFiles.ForEach(f =>
                    {
                        DataStruct.Camera img = new DataStruct.Camera(f);
                        _offLineImages.Add(img);
                        ImportImage(f);
                    });

                    MaxwellControl.Controls.MessageBox.Show("Image Load Complete");
                    _isLoad = true;
                }

                catch (Exception)
                {
                    MaxwellControl.Controls.MessageBox.Show("Image Load Catch Error");
                }
            }

            ProgressValue = 0;
        }

        /// <summary>
        /// 离线Search Mark
        /// </summary>
        private void SearchOffLine()
        {
            try
            {
                if (_offLineImages is null || _offLineImages.Count == 0)
                {
                    MaxwellControl.Controls.MessageBox.Show("Image Zero");
                    return;
                }

                ProgressValue = 0;
                _designPanel?.SelectedDrawingShapeList?.Clear();
                ParameterXUnit = "X[Pix]";
                ParameterYUnit = "Y[Pix]";

                List<double> rows = new List<double>();
                List<double> cols = new List<double>();
                List<double> angle = new List<double>();
                List<double> score = new List<double>();
                
                Errortype ret = Errortype.OK;

                for (int i = 0; i < _offLineImages.Count; i++)
                {
                    List<Tuple<double, double, double, double>> matchResult = new List<Tuple<double, double, double, double>>();
                    ProgressValue = (Convert.ToDouble(i) / _offLineImages.Count) * 80;

                    Thread.Sleep(InterVal);
                    if (MatchRegionName != null)
                    {
                        ret = MatchRegionManager.GetMatchRegion(MatchRegionName, out Rectangle1 rectangle);
                        Region matchRegion = new Region();
                        matchRegion.Rectangle1 = rectangle;
                        ret = MatchImg(_offLineImages[i], TemplateName, matchRegion, out matchResult);
                    }
                    else
                    {
                        ret = MatchImg(_offLineImages[i], TemplateName, null, out matchResult);
                    }
                    
                    if (ret != Errortype.OK)
                    {
                        MaxwellControl.Controls.MessageBox.Show(ret.ToString());
                        return;
                    }

                    if (matchResult.Count > 1 || matchResult.Count == 0)
                    {
                        Image = ImageHelper.Camera2WritableBitmap(_offLineImages[i]);
                        for (int j = 0; j < matchResult.Count; j++)
                        {
                            DrawRectangleCenterCross(new System.Windows.Point(matchResult[j].Item1, matchResult[j].Item2));
                        }

                        MaxwellControl.Controls.MessageBox.Show("Match Num Error");
                        return;
                    }

                    cols.Add(matchResult[0].Item1);
                    rows.Add(matchResult[0].Item2);

                    angle.Add(matchResult[0].Item3);
                    score.Add(matchResult[0].Item4 * 100);
                }

                Dictionary<string, List<double>> keyValuePairs = new Dictionary<string, List<double>>();
                keyValuePairs.Add("x", cols);
                keyValuePairs.Add("y", rows);
                keyValuePairs.Add("angle", angle);
                keyValuePairs.Add("score", score);

                ret = TemplateManagerService.GetInstance().GetDataSummary(keyValuePairs, out Dictionary<string, DataStatisticParam> summaryResult);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show(ret.ToString());
                    return;
                }


                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    if (MatchParameterList.Count >= 0)
                    {
                        MatchParameterList.Clear();
                        MatchParameterList.Add(new MatchParamter("Average", summaryResult["x"].Mean, summaryResult["y"].Mean, summaryResult["angle"].Mean, summaryResult["score"].Mean));
                        MatchParameterList.Add(new MatchParamter("Range", summaryResult["x"].Range, summaryResult["y"].Range, summaryResult["angle"].Range, summaryResult["score"].Range));
                        MatchParameterList.Add(new MatchParamter("Sigma3", summaryResult["x"].Sigma3, summaryResult["y"].Sigma3, summaryResult["angle"].Sigma3, summaryResult["score"].Sigma3));
                        MatchParameterList.Add(new MatchParamter("Min", summaryResult["x"].Min, summaryResult["y"].Min, summaryResult["angle"].Min, summaryResult["score"].Min));
                        MatchParameterList.Add(new MatchParamter("Max", summaryResult["x"].Max, summaryResult["y"].Max, summaryResult["angle"].Max, summaryResult["score"].Max));
                    }
                }));

                ProgressValue = 100;
            }

            catch (Exception ex)
            {
                MaxwellControl.Controls.MessageBox.Show(ex.Message);
            }

            finally
            {
                ProgressValue = 100;
                _isLoad = false;
            }
        }

        /// <summary>
        /// 在线Search Mark
        /// </summary>
        private void SearchOnLine()
        {
            try
            {
                if (Repeatings == 0)
                {
                    MaxwellControl.Controls.MessageBox.Show("Repeatings Zero");
                    return;
                }

                if (InterVal == 0)
                {
                    MaxwellControl.Controls.MessageBox.Show("InterVal Zero");
                    return;
                }

                if (_camera is null)
                {
                    MaxwellControl.Controls.MessageBox.Show("Camera Link Error");
                    return;
                }

                ProgressValue = 0;
                _designPanel?.SelectedDrawingShapeList?.Clear();
                ParameterXUnit = _getRulerByPixDelegate is null ? "X[Pix]" : "X[nm]";
                ParameterYUnit = _getRulerByPixDelegate is null ? "Y[Pix]" : "Y[nm]";

                List<double> rows = new List<double>();
                List<double> cols = new List<double>();
                List<double> angle = new List<double>();
                List<double> score = new List<double>();

                Errortype ret = Errortype.OK;

                for (int i = 0; i < Repeatings; i++)
                {
                    List<Tuple<double, double, double, double>> matchResult = new List<Tuple<double, double, double, double>>();
                    ProgressValue = (Convert.ToDouble(i) / Convert.ToDouble(Repeatings)) * 80;

                    Camera camera = ImageHelper.GetCamera(_camera.SnapImage());
                    Thread.Sleep(InterVal);

                    if (MatchRegionName != null)
                    {
                        ret = MatchRegionManager.GetMatchRegion(MatchRegionName, out Rectangle1 rectangle);
                        Region matchRegion = new Region();
                        matchRegion.Rectangle1 = rectangle;
                        ret = MatchImg(camera, TemplateName, matchRegion, out matchResult);
                    }
                    else
                    {
                        ret = MatchImg(camera, TemplateName, null, out matchResult);
                    }

                    if (ret != Errortype.OK)
                    {
                        MaxwellControl.Controls.MessageBox.Show(ret.ToString());
                        return;
                    }

                    if (matchResult.Count > 1 || matchResult.Count == 0)
                    {
                        MaxwellControl.Controls.MessageBox.Show("Match Num Error");
                        for (int j = 0; j < matchResult.Count; j++)
                        {
                            DrawRectangleCenterCross(new System.Windows.Point(matchResult[j].Item1, matchResult[j].Item2));
                        }

                        return;
                    }

                    if (_getRulerByPixDelegate != null)
                    {
                        this._getRulerByPixDelegate(_camera.PO.CategoryName, new Point(0, 0), new Point(matchResult[0].Item1, matchResult[0].Item2), out Point ruler);
                        cols.Add(ruler.X * 1e6);
                        rows.Add(ruler.Y * 1e6);
                    }
                    else
                    {
                        cols.Add(matchResult[0].Item1);
                        rows.Add(matchResult[0].Item2);
                    }

                    angle.Add(matchResult[0].Item3);
                    score.Add(matchResult[0].Item4 * 100);
                    camera.Dispose();
                }

                Dictionary<string, List<double>> keyValuePairs = new Dictionary<string, List<double>>();
                keyValuePairs.Add("x", cols);
                keyValuePairs.Add("y", rows);
                keyValuePairs.Add("angle", angle);
                keyValuePairs.Add("score", score);

                ret = TemplateManagerService.GetInstance().GetDataSummary(keyValuePairs, out Dictionary<string, DataStatisticParam> summaryResult);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show(ret.ToString());
                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    if (MatchParameterList.Count >= 0)
                    {
                        MatchParameterList.Clear();
                        MatchParameterList.Add(new MatchParamter("Average", summaryResult["x"].Mean, summaryResult["y"].Mean, summaryResult["angle"].Mean, summaryResult["score"].Mean));
                        MatchParameterList.Add(new MatchParamter("Range", summaryResult["x"].Range, summaryResult["y"].Range, summaryResult["angle"].Range, summaryResult["score"].Range));
                        MatchParameterList.Add(new MatchParamter("Sigma3", summaryResult["x"].Sigma3, summaryResult["y"].Sigma3, summaryResult["angle"].Sigma3, summaryResult["score"].Sigma3));
                        MatchParameterList.Add(new MatchParamter("Min", summaryResult["x"].Min, summaryResult["y"].Min, summaryResult["angle"].Min, summaryResult["score"].Min));
                        MatchParameterList.Add(new MatchParamter("Max", summaryResult["x"].Max, summaryResult["y"].Max, summaryResult["angle"].Max, summaryResult["score"].Max));
                    }
                }));

                ProgressValue = 100;
            }

            catch (Exception ex)
            {
                MaxwellControl.Controls.MessageBox.Show(ex.Message);
            }

            finally
            {
                ProgressValue = 100;
            }
        }

        /// <summary>
        /// 对单个模板图像进行匹配
        /// </summary>
        /// <param name="img"></param>
        /// <param name="markName"></param>
        /// <param name="matchRegion"></param>
        /// <param name="matchResult"></param>
        private Errortype MatchImg(Camera img, string markName, Region matchRegion, out List<Tuple<double, double, double, double>> matchResult)
        {
            matchResult = new List<Tuple<double, double, double, double>>();
            Errortype ret = TemplateManagerService.GetInstance().Match(markName, img, matchRegion,
                out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores);

            if (Errortype.OK != ret)
            {
                return ret;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                matchResult.Add(new Tuple<double, double, double, double>(cols[i], rows[i], angles[i], scores[i]));
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 绘制矩形十字
        /// </summary>
        /// <param name="centerPointT"></param>
        private void DrawRectangleCenterCross(System.Windows.Point centerPointT)
        {
            try
            {
                if (_designPanel.Shapes.Count > 0)
                    _designPanel.Shapes.Clear();

                //_designPanel.ShapeContent = new LineShapeModel();
                double rad = (double)Math.PI * Rotation / 180;
                DataStruct.Rectangle2 rectangleH = new DataStruct.Rectangle2(centerPointT.X, centerPointT.Y, LineSize, LineWidth, rad);
                List<DataStruct.Point> pointsH = rectangleH.GetCornerPoints();
                pointsH.Add(pointsH[0]);

                DataStruct.Rectangle2 rectangleV = new DataStruct.Rectangle2(centerPointT.X, centerPointT.Y, LineSize, LineWidth, rad + Math.PI / 2);
                List<DataStruct.Point> pointsV = rectangleV.GetCornerPoints();
                pointsV.Add(pointsV[0]);

                for (int i = 0; i < pointsH.Count - 1; i++)
                {
                    //var shapeline = new PrimShape();
                    //shapeline.Region = new LineRegion();

                    //System.Windows.Point startPoint = new System.Windows.Point(pointsH[i].X, pointsH[i].Y);
                    //System.Windows.Point endPoint = new System.Windows.Point(pointsH[i + 1].X, pointsH[i + 1].Y);
                    //_designPanel.ShapeContent.SetReportItem(_designPanel, shapeline.Region, startPoint, endPoint);
                    //_designPanel.Shapes.Add(shapeline);

                    //startPoint = new System.Windows.Point(pointsV[i].X, pointsV[i].Y);
                    //endPoint = new System.Windows.Point(pointsV[i + 1].X, pointsV[i + 1].Y);
                    //_designPanel.ShapeContent.SetReportItem(_designPanel, shapeline.Region, startPoint, endPoint);
                    //_designPanel.Shapes.Add(shapeline);
                    _designPanel.Items.Add(new LineShapeDrawing() { X = pointsH[i].X, Y = pointsH[i].Y, X1 = pointsH[i + 1].X, Y1 = pointsH[i + 1].Y, Range = 1000 });
                    _designPanel.Items.Add(new LineShapeDrawing() { X = pointsV[i].X, Y = pointsV[i + 1].Y, X1 = pointsV[i + 1].X, Y1 = pointsV[i + 1].Y });
                }
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
