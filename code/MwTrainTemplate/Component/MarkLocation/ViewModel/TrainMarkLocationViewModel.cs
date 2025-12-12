using DataStruct;
using MaxwellControl.Language;
using MaxwellFramework.Core.Common;
using MaxwellFramework.Core.Interfaces;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using MwFramework.Controls.ControlCanvas.Model;
using MwFramework.ManagerService;
using Stylet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using MwTrainTemplate.Common;
using Color = System.Windows.Media.Color;
using DelegateCommand = MaxwellFramework.Core.Common.Command.DelegateCommand;
using MessageBox = MaxwellControl.Controls.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Point = DataStruct.Point;
using Screen = Stylet.Screen;
using MwFramework.Device;
using System.Drawing.Imaging;
using static MwTrainTemplate.Component.MarkLocation.ViewModel.TrainMarkLocationViewModel;
using MaxwellControl.Tools;
using MwFramework.Device.Model;

namespace MwTrainTemplate.Component.MarkLocation.ViewModel
{
    public class TrainMarkLocationViewModel : Screen, IPage
    {
        public string Name = "TrainMarkLocationView";

        public delegate bool GetRulerByPixDelegate(string itemName, Point rulerCurrent, Point pixel, out Point ruler);
        private GetRulerByPixDelegate _getRulerByPixDelegate;

        #region Property

        /// <summary>
        /// 图像显示Source
        /// </summary>
        private WriteableBitmap _continuousImage;

        public WriteableBitmap ContinuousImage
        {
            get { return _continuousImage; }
            set
            {
                _continuousImage = value;
                OnPropertyChanged(nameof(ContinuousImage));
            }
        }

        /// <summary>
        /// 实时显示鼠标移动像素坐标
        /// </summary>
        private double _motorX;

        public double MotorX
        {
            get { return _motorX; }
            set
            {
                _motorX = value;
                OnPropertyChanged(nameof(MotorX));
            }
        }

        /// <summary>
        /// 实时显示鼠标移动像素坐标
        /// </summary>
        private double _motorY;

        public double MotorY
        {
            get { return _motorY; }
            set
            {
                _motorY = value;
                OnPropertyChanged(nameof(MotorY));
            }
        }

        #region Region

        /// <summary>
        /// 设置区域起点坐标X
        /// </summary>
        private string _setRegionX;

        public string SetRegionX
        {
            get { return _setRegionX; }
            set
            {
                _setRegionX = value;
                OnPropertyChanged(nameof(SetRegionX));
            }
        }

        /// <summary>
        /// 设置区域起点坐标Y
        /// </summary>
        private string _setRegionY;

        public string SetRegionY
        {
            get { return _setRegionY; }
            set
            {
                _setRegionY = value;
                OnPropertyChanged(nameof(SetRegionY));
            }
        }

        /// <summary>
        /// 设置区域矩形高
        /// </summary>
        private string _setRegionHeight;

        public string SetRegionHeight
        {
            get { return _setRegionHeight; }
            set
            {
                _setRegionHeight = value;
                OnPropertyChanged(nameof(SetRegionHeight));
            }
        }

        /// <summary>
        /// 设置区域矩形宽
        /// </summary>
        private string _setRegionWidth;

        public string SetRegionWidth
        {
            get { return _setRegionWidth; }
            set
            {
                _setRegionWidth = value;
                OnPropertyChanged(nameof(SetRegionWidth));
            }
        }

        /// <summary>
        /// 设置区域模式Move
        /// </summary>
        private bool _setRegionModeMove = true;

        public bool SetRegionModeMove
        {
            get { return _setRegionModeMove; }
            set
            {
                _setRegionModeMove = value;
                OnPropertyChanged(nameof(SetRegionModeMove));
            }
        }

        /// <summary>
        /// 设置区域模式Resize
        /// </summary>
        private bool _setRegionModeResize = false;

        public bool SetRegionModeResize
        {
            get { return _setRegionModeResize; }
            set
            {
                _setRegionModeResize = value;
                OnPropertyChanged(nameof(SetRegionModeResize));
            }
        }

        /// <summary>
        /// 选择区域步长宽度（像素值）
        /// </summary>
        private string _stepWidth = "0";

        public string StepWidth
        {
            get { return _stepWidth; }
            set
            {
                _stepWidth = value;
                OnPropertyChanged(nameof(StepWidth));
            }
        }

        /// <summary>
        /// 选择区域缩放比例（像素值）
        /// </summary>
        private string _scale = "0";

        public string Scale
        {
            get { return _scale; }
            set
            {
                _scale = value;
                OnPropertyChanged(nameof(Scale));
            }
        }

        #endregion Region

        #region Train don't care

        /// <summary>
        /// 设置Mask模式Inner
        /// </summary>
        private bool _setInnerMaskMode = true;

        public bool SetInnerMaskMode
        {
            get { return _setInnerMaskMode; }
            set
            {
                _setInnerMaskMode = value;
                OnPropertyChanged(nameof(SetInnerMaskMode));
            }
        }

        /// <summary>
        /// 设置Mask模式Outer
        /// </summary>
        private bool _setOuterMaskMode;

        public bool SetOuterMaskMode
        {
            get { return _setOuterMaskMode; }
            set
            {
                _setOuterMaskMode = value;
                OnPropertyChanged(nameof(SetOuterMaskMode));
            }
        }

        private string _navigation = "Rectangle Navigation";

        public string Navigation
        {
            get { return _navigation; }
            set
            {
                _navigation = value;
                OnPropertyChanged(nameof(Navigation));
            }
        }

        private SolidColorBrush _solideRectangle = new SolidColorBrush(Color.FromRgb(122, 122, 122));

        public SolidColorBrush SolideRectangle
        {
            get
            {
                return _solideRectangle;
            }
            set
            {
                _solideRectangle = value;
                OnPropertyChanged(nameof(SolideRectangle));
            }
        }

        private SolidColorBrush _solidePolygon = new SolidColorBrush(Color.FromRgb(122, 122, 122));

        public SolidColorBrush SolidePolygon
        {
            get
            {
                return _solidePolygon;
            }
            set
            {
                _solidePolygon = value;
                OnPropertyChanged(nameof(SolidePolygon));
            }
        }

        #endregion Train don't care

        #region Origin

        /// <summary>
        /// 自动显示图像起源坐标
        /// </summary>
        private string _autoOrginValue = string.Empty;

        public string AutoOrginValue
        {
            get { return _autoOrginValue; }
            set
            {
                _autoOrginValue = value;
                OnPropertyChanged(nameof(AutoOrginValue));
            }
        }

        /// <summary>
        /// 像素步长
        /// </summary>
        private double _pixelSteps = 0;

        public double PixelSteps
        {
            get { return _pixelSteps; }
            set
            {
                _pixelSteps = value;
                OnPropertyChanged(nameof(PixelSteps));
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
        /// 是否显示圆
        /// </summary>
        private bool _isDiplayCircle = false;

        public bool IsDiplayCircle
        {
            get { return _isDiplayCircle; }
            set
            {
                _isDiplayCircle = value;
                OnPropertyChanged(nameof(IsDiplayCircle));
            }
        }

        /// <summary>
        /// 圆半径
        /// </summary>
        private double _adius = 150;

        public double Radius
        {
            get { return _adius; }
            set
            {
                _adius = value;
                OnPropertyChanged(nameof(Radius));
            }
        }

        #endregion Origin

        #region Details

        /// <summary>
        /// 模板集合
        /// </summary>
        private ObservableCollection<string> _detailModels = new ObservableCollection<string>();

        public ObservableCollection<string> DetailModels
        {
            get { return _detailModels; }
            set
            {
                _detailModels = value;
                OnPropertyChanged(nameof(DetailModels));
            }
        }

        /// <summary>
        /// 单个模板
        /// </summary>
        private string _detailModel = string.Empty;

        public string DetailModel
        {
            get { return _detailModel; }
            set
            {
                _detailModel = value;
                OnPropertyChanged(nameof(DetailModel));
            }
        }

        /// <summary>
        /// 模板总数
        /// </summary>
        private int totalDetailModel = 0;

        public int TotalDetailModel
        {
            get { return totalDetailModel; }
            set
            {
                totalDetailModel = value;
                OnPropertyChanged(nameof(TotalDetailModel));
            }
        }

        /// <summary>
        /// 初始图像字典
        /// </summary>
        private ConcurrentDictionary<int, WriteableBitmap> keyValuePairs = new ConcurrentDictionary<int, WriteableBitmap>();

        /// <summary>
        /// 十字坐标
        /// </summary>
        private Point _autoOrigin = new Point(0, 0);

        #endregion Details

        #region Match
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

        #endregion Match

        #region Expander
        private bool _selectedExpander1;

        public bool SelectedExpander1
        {
            get
            {
                return _selectedExpander1;
            }
            set
            {
                _selectedExpander1 = value;
                if (_selectedExpander1)
                {
                    SelectedExpander2 = false;
                    SelectedExpander3 = false;
                    SelectedExpander4 = false;
                    SelectedExpander5 = false;
                    SelectedExpander6 = false;
                    SelectedExpander7 = false;
                    SelectedExpander8 = false;
                }
                OnPropertyChanged(nameof(SelectedExpander1));
            }
        }

        private bool _selectedExpander2;

        public bool SelectedExpander2
        {
            get
            {
                return _selectedExpander2;
            }
            set
            {
                _selectedExpander2 = value;
                if (_selectedExpander2)
                {
                    SelectedExpander1 = false;
                    SelectedExpander3 = false;
                    SelectedExpander4 = false;
                    SelectedExpander5 = false;
                    SelectedExpander6 = false;
                    SelectedExpander7 = false;
                    SelectedExpander8 = false;
                }

                OnPropertyChanged(nameof(SelectedExpander2));
            }
        }

        private bool _selectedExpander3;

        public bool SelectedExpander3
        {
            get
            {
                return _selectedExpander3;
            }
            set
            {
                _selectedExpander3 = value;
                if (_selectedExpander3)
                {
                    SelectedExpander1 = false;
                    SelectedExpander2 = false;
                    SelectedExpander4 = false;
                    SelectedExpander5 = false;
                    SelectedExpander6 = false;
                    SelectedExpander7 = false;
                    SelectedExpander8 = false;
                }

                OnPropertyChanged(nameof(SelectedExpander3));
            }
        }

        private bool _selectedExpander4;

        public bool SelectedExpander4
        {
            get
            {
                return _selectedExpander4;
            }
            set
            {
                _selectedExpander4 = value;
                if (_selectedExpander4)
                {
                    SelectedExpander1 = false;
                    SelectedExpander2 = false;
                    SelectedExpander3 = false;
                    SelectedExpander5 = false;
                    SelectedExpander6 = false;
                    SelectedExpander7 = false;
                    SelectedExpander8 = false;
                }

                OnPropertyChanged(nameof(SelectedExpander4));
            }
        }

        private bool _selectedExpander5;

        public bool SelectedExpander5
        {
            get
            {
                return _selectedExpander5;
            }
            set
            {
                _selectedExpander5 = value;
                if (_selectedExpander5)
                {
                    SelectedExpander1 = false;
                    SelectedExpander2 = false;
                    SelectedExpander3 = false;
                    SelectedExpander4 = false;
                    SelectedExpander6 = false;
                    SelectedExpander7 = false;
                    SelectedExpander8 = false;
                }

                OnPropertyChanged(nameof(SelectedExpander5));
            }
        }

        private bool _selectedExpander6;

        public bool SelectedExpander6
        {
            get
            {
                return _selectedExpander6;
            }
            set
            {
                _selectedExpander6 = value;
                if (_selectedExpander6)
                {
                    SelectedExpander1 = false;
                    SelectedExpander2 = false;
                    SelectedExpander3 = false;
                    SelectedExpander4 = false;
                    SelectedExpander5 = false;
                    SelectedExpander7 = false;
                    SelectedExpander8 = false;
                }

                OnPropertyChanged(nameof(SelectedExpander6));
            }
        }

        private bool _selectedExpander7;

        public bool SelectedExpander7
        {
            get
            {
                return _selectedExpander7;
            }
            set
            {
                _selectedExpander7 = value;
                if (_selectedExpander7)
                {
                    SelectedExpander1 = false;
                    SelectedExpander2 = false;
                    SelectedExpander3 = false;
                    SelectedExpander4 = false;
                    SelectedExpander6 = false;
                    SelectedExpander5 = false;
                    SelectedExpander8 = false;
                }

                OnPropertyChanged(nameof(SelectedExpander7));
            }
        }

        private bool _selectedExpander8;
        public bool SelectedExpander8
        {
            get
            {
                return _selectedExpander8;
            }
            set
            {
                _selectedExpander8 = value;
                if (_selectedExpander8)
                {
                    SelectedExpander1 = false;
                    SelectedExpander2 = false;
                    SelectedExpander3 = false;
                    SelectedExpander4 = false;
                    SelectedExpander6 = false;
                    SelectedExpander5 = false;
                    SelectedExpander7 = false;
                }

                OnPropertyChanged(nameof(SelectedExpander8));
            }
        }

        #endregion 

        #region 命令
        public ICommand cameraPanelRight_MouseLeftButtonUp { get; set; }
        public ICommand cameraPanelRight_MouseLeftButtonDown { get; set; }
        public ICommand cameraPanelRight_OnMouseMove { get; set; }
        public ICommand cameraPanelRight_OnSourceChange { get; set; }
        public ICommand cameraPanelRight_ReportItemSelected { get; set; }
        public ICommand regionExpanded { get; set; }
        public ICommand lineWidthValueChanged { get; set; }
        public ICommand lineSizeValueChanged { get; set; }
        public ICommand rotationValueChanged { get; set; }
        public ICommand radiusValueChanged { get; set; }
        public ICommand cmbScaleSelectionChanged { get; set; }

        #endregion 命令

        /// <summary>
        /// 图像显示控件
        /// </summary>
        private DesignPanel _designPanel = null;

        private IWindowManager _windowManager;

        private Rectangle1 _matchRectangle = null;

        private string PatternName { get; set; }

        private string ModuleName { get; set; }

        private MwCamera _camera;
        #endregion

        #region Construct

        /// <summary>
        /// Construct
        /// </summary>
        public TrainMarkLocationViewModel()
        {
        }

        public TrainMarkLocationViewModel(string patternName, string moduleName, MwCamera camera = null, WriteableBitmap writeableBitmap = null, GetRulerByPixDelegate operation = null)
        {
            _windowManager = IoC.Get<IWindowManager>();
            PatternName = patternName;
            ModuleName = moduleName;
            _camera = camera;
            ContinuousImage = writeableBitmap;
            _getRulerByPixDelegate = operation;

            InitCommand();
            IParamList paramList = IoC.Get<IParameterManager>() as IParamList;
            //_trainMarkLocationSetupParameter = paramList.GetParameter<TrainMarkLocationSetupParameter>();
            _trainMarkLocationSetupParameter = new TrainMarkLocationSetupParameter();
            _trainMarkLocationSetupParameter.Read(System.IO.Path.Combine(GlobalPathHelper.G_ParameterConfigurePath, _trainMarkLocationSetupParameter.GetType().Name + ModuleName + ".xml"));
            _trainMarkLocationSetupParameter.Initialize();
            SetupParameterList = _trainMarkLocationSetupParameter.TrainSetupParameterItems;
            ComboboxSetupParameterList = _trainMarkLocationSetupParameter.ComboboxTrainSetupParameterItems;
            ComboboxSelectionChanged(null, null);
            SetupSave();
            MarkLocationManagerService.GetInstance().Load(AppDomain.CurrentDomain.BaseDirectory + "MarkModel", patternName);
        }

        #endregion

        #region Function

        /// <summary>
        /// 页面重新加载时
        /// </summary>
        protected override void OnViewLoaded()
        {
            base.OnViewLoaded();
        }

        /// <summary>
        /// 初始化命令
        /// </summary>
        private void InitCommand()
        {
            cameraPanelRight_OnMouseMove = new DelegateCommand(CameraPanelRight_OnMouseMove);
            cameraPanelRight_OnSourceChange = new DelegateCommand(CameraPanelRight_OnSourceChange);
            cameraPanelRight_ReportItemSelected = new DelegateCommand(CameraPanelRight_ReportItemSelected);
            regionExpanded = new DelegateCommand(RegionExpanded);
            cameraPanelRight_MouseLeftButtonDown = new DelegateCommand(CameraPanelRight_MouseLeftButtonDown);
            cameraPanelRight_MouseLeftButtonUp = new DelegateCommand(CameraPanelRight_MouseLeftButtonUp);
            lineWidthValueChanged = new DelegateCommand(LineWidthValueChanged);
            lineSizeValueChanged = new DelegateCommand(LineSizeValueChanged);
            rotationValueChanged = new DelegateCommand(RotationValueChanged);
            radiusValueChanged = new DelegateCommand(RadiusValueChanged);
            cmbScaleSelectionChanged = new DelegateCommand(CmbScaleSelectionChanged);
        }

        private Rectangle1 _rectangle = null;

        /// <summary>
        /// 缩放矩形
        /// </summary>
        /// <param name="rectangleItem"></param>
        /// <param name="itemControl"></param>
        private void ScaleRectangle(Rectangle1 rectangleItem, IReportItemControl itemControl)
        {
            int Width = Math.Abs(Convert.ToInt32(rectangleItem.End_X - rectangleItem.Start_X));
            int Height = Math.Abs(Convert.ToInt32(rectangleItem.End_Y - rectangleItem.Start_Y));
            int CenterX = Width / 2 + Convert.ToInt32(rectangleItem.Start_X);
            int CenterY = Height / 2 + Convert.ToInt32(rectangleItem.Start_Y);
            System.Windows.Point pointStart = new System.Windows.Point();
            System.Windows.Point pointEnd = new System.Windows.Point();
            ScaleTransform scaleTransform = new ScaleTransform();
            scaleTransform.CenterX = CenterX;
            scaleTransform.CenterY = CenterY;
            scaleTransform.ScaleX = Convert.ToDouble(Scale);
            scaleTransform.ScaleY = Convert.ToDouble(Scale);
            pointStart = scaleTransform.Transform(new System.Windows.Point(rectangleItem.Start_X, rectangleItem.Start_Y));
            pointEnd = scaleTransform.Transform(new System.Windows.Point(rectangleItem.End_X, rectangleItem.End_Y));
            SetRegionX = (itemControl.ItemTop = pointStart.Y).ToString();
            SetRegionY = (itemControl.ItemLeft = pointStart.X).ToString();
            SetRegionWidth = (itemControl.ItemWidth = pointEnd.X - pointStart.X).ToString();
            SetRegionHeight = (itemControl.ItemHeight = pointEnd.Y - pointStart.Y).ToString();
        }

        /// <summary>
        /// Polygon缩放比例时触发
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CmbScaleSelectionChanged(object obj)
        {
            try
            {
                if (_designPanel == null) return;
                if (_designPanel.SelectedDrawingShapeList.Count > 0)
                {
                    foreach (var item in _designPanel.SelectedDrawingShapeList)
                    {
                        if (item is PolygonControl)
                        {
                            item.IsFocusedItem = true;
                            _designPanel.SetScalePolyon(Convert.ToDouble(Scale));
                        }
                        if (item.ItemType is DrawingReportItem.Rectangle)
                        {
                            item.IsFocusedItem = true;
                            if (_rectangle == null)
                                _rectangle = GetRegionItemRectangle();
                            if (_rectangle != null)
                            {
                                ScaleRectangle(_rectangle, item);
                            }
                            //Rectangle1 rectanglePix = GetRegionPixRectangle();
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 半径发生改变时触发
        /// </summary>
        /// <param name="obj"></param>
        private void RadiusValueChanged(object obj)
        {
            try
            {
                if (IsDiplayCircle)
                {
                    if (_autoOrigin.X == 0 && _autoOrigin.Y == 0)
                    {
                        MessageBox.Show("NoImageCenterPoint");
                        return;
                    }
                    DrawCircleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y), Radius);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 角度发生变化时触发
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void RotationValueChanged(object obj)
        {
            try
            {
                if (_designPanel == null) return;
                if (_designPanel.Shapes.Count == 8 || _designPanel.Shapes.Count == 9)
                {
                    if (_designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                        DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 线大小发生改变时触发
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void LineSizeValueChanged(object obj)
        {
            try
            {
                if (_designPanel == null) return;
                if (_designPanel.Shapes.Count == 8 || _designPanel.Shapes.Count == 9)
                {
                    if (_designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                        //  DrawCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y), LineSize);
                        DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 线宽值发生改变时触发
        /// </summary>
        /// <param name="obj"></param>
        private void LineWidthValueChanged(object obj)
        {
            try
            {
                if (_designPanel == null) return;
                if (_designPanel.Shapes.Count == 8 || _designPanel.Shapes.Count == 9)
                {
                    if (_designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                        DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 控件鼠标右击松开时触发
        /// </summary>
        /// <param name="obj"></param>
        private void CameraPanelRight_MouseLeftButtonUp(object obj)
        {
            DesignPanel designPanelMouseLeft = obj as DesignPanel;
            if (designPanelMouseLeft == null) return;
            SetRegionWidth = (designPanelMouseLeft.MouseX - Convert.ToDouble(SetRegionX)).ToString();
            SetRegionHeight = (designPanelMouseLeft.MouseY - Convert.ToDouble(SetRegionY)).ToString();
        }

        /// <summary>
        /// 控件鼠标右击点击时触发
        /// </summary>
        /// <param name="obj"></param>
        private void CameraPanelRight_MouseLeftButtonDown(object obj)
        {
            DesignPanel designPanelMouseLeft = obj as DesignPanel;
            if (designPanelMouseLeft == null) return;
            SetRegionX = designPanelMouseLeft.MouseX.ToString();
            SetRegionY = designPanelMouseLeft.MouseY.ToString();
        }

        /// <summary>
        /// 区域控件展开
        /// </summary>
        /// <param name="obj"></param>
        private void RegionExpanded(object obj)
        {
        }

        /// <summary>
        /// UI绘制图形时触发
        /// </summary>
        /// <param name="obj"></param>
        private void CameraPanelRight_ReportItemSelected(object obj)
        {
        }

        /// <summary>
        /// 图像源发生改变
        /// </summary>
        /// <param name="obj"></param>
        private void CameraPanelRight_OnSourceChange(object obj)
        {
        }

        /// <summary>
        /// 获取鼠标移动的像素坐标
        /// </summary>
        /// <param name="obj"></param>
        private void CameraPanelRight_OnMouseMove(object obj)
        {
            _designPanel = obj as DesignPanel;
            if (_designPanel == null) return;
            MotorX = _designPanel.MouseX;
            MotorY = _designPanel.MouseY;
        }

        /// <summary>
        /// 从相机中获取图像
        /// </summary>
        public void AcquiredImage()
        {
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
            ContinuousImage = ImageHelper.Camera2WritableBitmap(image);

            image.Dispose();
        }

        /// <summary>
        /// 从本地加载图像
        /// </summary>
        public void LoadImageFromFile()
        {
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
            ContinuousImage = ImageHelper.Camera2WritableBitmap(img);
            img.Dispose();
        }

        /// <summary>
        /// Region Up
        /// </summary>
        public void SetRegionUp()
        {
            if (_designPanel == null || _designPanel.SelectedDrawingShapeList.Count == 0) return;

            foreach (var item in _designPanel.SelectedDrawingShapeList)
            {
                if (item.Shape.Region is PolygonRegion)
                {
                    List<Line> lines = GetRegionItemPolygon();
                    if (lines == null) return;
                    SelectMode(lines, "Up");
                }
                if (item.Shape.Region is RectRegion)
                {
                    Rectangle1 rectangleItem = GetRegionItemRectangle();
                    if (rectangleItem == null) return;
                    SelectMode(rectangleItem, "Up");
                }
            }
        }

        /// <summary>
        /// Region Left
        /// </summary>
        public void SetRegionLeft()
        {
            if (_designPanel == null || _designPanel.SelectedDrawingShapeList.Count == 0) return;
            foreach (var item in _designPanel.SelectedDrawingShapeList)
            {
                if (item.Shape.Region is PolygonRegion)
                {
                    List<Line> lines = GetRegionItemPolygon();
                    if (lines == null) return;
                    SelectMode(lines, "Left");
                }
                if (item.Shape.Region is RectRegion)
                {
                    Rectangle1 rectangleItem = GetRegionItemRectangle();
                    if (rectangleItem == null) return;
                    SelectMode(rectangleItem, "Left");
                }
            }
        }

        /// <summary>
        /// Region Down
        /// </summary>
        public void SetRegionDown()
        {
            if (_designPanel == null || _designPanel.SelectedDrawingShapeList.Count == 0) return;
            foreach (var item in _designPanel.SelectedDrawingShapeList)
            {
                if (item.Shape.Region is PolygonRegion)
                {
                    List<Line> lines = GetRegionItemPolygon();
                    if (lines == null) return;
                    SelectMode(lines, "Down");
                }
                if (item.Shape.Region is RectRegion)
                {
                    Rectangle1 rectangleItem = GetRegionItemRectangle();
                    if (rectangleItem == null) return;
                    SelectMode(rectangleItem, "Down");
                }
            }
        }

        /// <summary>
        /// Region Right
        /// </summary>
        public void SetRegionRight()
        {
            if (_designPanel == null || _designPanel.SelectedDrawingShapeList.Count == 0) return;
            foreach (var item in _designPanel.SelectedDrawingShapeList)
            {
                if (item.Shape.Region is PolygonRegion)
                {
                    List<Line> lines = GetRegionItemPolygon();
                    if (lines == null) return;
                    SelectMode(lines, "Right");
                }
                if (item.Shape.Region is RectRegion)
                {
                    Rectangle1 rectangleItem = GetRegionItemRectangle();
                    if (rectangleItem == null) return;
                    SelectMode(rectangleItem, "Right");
                }
            }
        }

        /// <summary>
        /// 设置绘制矩形区域
        /// </summary>
        public void SetRegion()
        {
            if (_designPanel == null) return;
            var shapes = _designPanel.SelectedDrawingShapeList;
            if (shapes == null || shapes.Count == 0)
            {
                MessageBox.Show("PaintRectNoFound");
                return;
            }

            if (shapes.Exists(t => t.ItemType != DrawingReportItem.Rectangle))
            {
                MaxwellControl.Controls.MessageBox.Show("PaintedException");
                _designPanel.SelectedDrawingShapeList.Clear();
                _designPanel.Clear();
                return;
            }

            Rectangle1 rect = new Rectangle1();
            _designPanel.SelectedDrawingShapeList.ForEach(s =>
            {
                if (s.ItemType != DrawingReportItem.Rectangle) return;
                else
                {
                    rect.Start_X = s.PixLeft;
                    rect.Start_Y = s.PixTop;
                    rect.End_X = s.PixLeft + s.PixWidth;
                    rect.End_Y = s.PixTop + s.PixHeight;
                }
            });
            _designPanel.SelectedDrawingShapeList.Clear();
            _designPanel.Clear();

            Errortype errortype = MarkLocationManagerService.GetInstance().SetRegionRectangle(ImageHelper.GetCamera(ContinuousImage), rect, out Camera regionImg);
            if (errortype != Errortype.OK)
            {
                MessageBox.Show(errortype.ToString());
                return;
            }
            ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
        }

        /// <summary>
        /// 清除绘制单个矩形区域
        /// </summary>
        public void ClearRegion()
        {
            if (_designPanel == null) return;
            if (_rectangle != null) _rectangle = null;
            if (_designPanel.SelectedDrawingShapeList.Count > 0)
            {
                _designPanel.SelectedDrawingShapeList.Clear();
                _designPanel.Clear();
            }
            if (keyValuePairs.Count > 0)
                ContinuousImage = keyValuePairs[0];
        }

        /// <summary>
        /// 设置Train don't care区域
        /// </summary>
        public void SetTrain()
        {
            try
            {
                if (_designPanel == null) return;
                var shapes = _designPanel.SelectedDrawingShapeList;
                if (shapes == null)
                {
                    MessageBox.Show("NoDrawnRegions");
                    return;
                }

                List<List<DataStruct.Point>> trainDontCareRegion = new List<List<Point>>();
                List<bool> trainDontCareMode = new List<bool>();
                _designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(rect, out List<DataStruct.Point> ps);
                        trainDontCareRegion.Add(ps);
                        trainDontCareMode.Add(SetInnerMaskMode);
                    }
                    else if (s.ItemType == DrawingReportItem.Polygon)
                    {
                        PolygonControl polygonControl = s as PolygonControl;
                        List<DataStruct.Point> polygonPoints = new List<DataStruct.Point>();

                        foreach (var item in polygonControl.LineCollection)
                        {
                            System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                            line.X1 = item.X1 * _designPanel.WidthRatio;
                            line.X2 = item.X2 * _designPanel.WidthRatio;
                            line.Y1 = item.Y1 * _designPanel.HeightRatio;
                            line.Y2 = item.Y2 * _designPanel.HeightRatio;
                            polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                            polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                        }
                        polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();

                        trainDontCareRegion.Add(polygonPoints);
                        trainDontCareMode.Add(SetInnerMaskMode);
                    }
                });

                _designPanel.SelectedDrawingShapeList.Clear();
                _designPanel.Clear();
                if (GetDictionary(trainDontCareRegion, trainDontCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetTrainDontCareRegion(valuePairsPoints, out Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show(errortype.ToString());
                        return;
                    }
                    errortype = MarkLocationManagerService.GetInstance().SetDetailDontCareRegion(valuePairsPoints, out Camera regionImgT);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show(errortype.ToString());
                        return;
                    }
                    ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                    regionImg.Dispose();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 清除Train don't care区域
        /// </summary>
        public void ClearTrain()
        {
            try
            {
                if (_designPanel == null) return;
                var shapes = _designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| _designPanel.SelectedReportItems.Count > 1 || _designPanel.SelectedReportItems.Count == 0*/)
                {
                    string message = LangGet.GetMessage("SVAViewModel_No_drawn_rectangles") + _designPanel.SelectedDrawingShapeList.Count.ToString();
                    MessageBox.Show(message, null, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                List<List<DataStruct.Point>> trainCareRegion = new List<List<Point>>();
                List<bool> trainCareMode = new List<bool>();
                _designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(rect, out List<DataStruct.Point> ps);
                        trainCareRegion.Add(ps);
                        trainCareMode.Add(SetInnerMaskMode);
                    }
                    else if (s.ItemType == DrawingReportItem.Polygon)
                    {
                        PolygonControl polygonControl = s as PolygonControl;
                        List<DataStruct.Point> polygonPoints = new List<DataStruct.Point>();

                        foreach (var item in polygonControl.LineCollection)
                        {
                            System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                            line.X1 = item.X1 * _designPanel.WidthRatio;
                            line.X2 = item.X2 * _designPanel.WidthRatio;
                            line.Y1 = item.Y1 * _designPanel.HeightRatio;
                            line.Y2 = item.Y2 * _designPanel.HeightRatio;
                            polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                            polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                        }
                        polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();

                        trainCareRegion.Add(polygonPoints);
                        trainCareMode.Add(SetInnerMaskMode);
                    }
                });

                _designPanel.SelectedDrawingShapeList.Clear();
                _designPanel.Clear();
                if (GetDictionary(trainCareRegion, trainCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetTrainCareRegion(valuePairsPoints, out Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        string message = LangGet.GetMessage("SVAViewModel_TrainDontCareError");

                        MessageBox.Show(message);
                        return;
                    }
                    errortype = MarkLocationManagerService.GetInstance().SetDetailRegion(valuePairsPoints, out Camera regionImgT);
                    if (errortype != Errortype.OK)
                    {
                        string message = LangGet.GetMessage("SVAViewModel_TrainDontCareError");
                        MessageBox.Show(message);

                        return;
                    }
                    ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                    regionImg.Dispose();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 清除所有Train don't care区域
        /// </summary>
        public void ClearAllTrain()
        {
            try
            {
                if (_designPanel == null) return;
                _designPanel.Clear();
                _designPanel.SelectedDrawingShapeList.Clear();

                Errortype errortype = MarkLocationManagerService.GetInstance().ClearTrainDontCareRegion(out Camera regionImg);
                if (errortype != Errortype.OK)
                {
                    string message = LangGet.GetMessage("SVAViewModel_TrainDontCareError");
                    MessageBox.Show(message);

                    return;
                }
                ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                regionImg.Dispose();
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 设置Detail 区域
        /// </summary>
        public void SetDetail()
        {
            try
            {
                if (_designPanel == null) return;
                var shapes = _designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| _designPanel.SelectedReportItems.Count > 1*/)
                {
                    string message = LangGet.GetMessage("SVAViewModel_No_drawn_rectangles") + _designPanel.SelectedDrawingShapeList.Count.ToString();

                    MessageBox.Show(message, null, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                List<List<DataStruct.Point>> detailRegion = new List<List<Point>>();
                List<bool> detailMode = new List<bool>();

                _designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(rect, out List<DataStruct.Point> ps);
                        detailRegion.Add(ps);
                        detailMode.Add(SetInnerMaskMode);
                    }
                    else if (s.ItemType == DrawingReportItem.Polygon)
                    {
                        if (s is PolygonControl)
                        {
                            PolygonControl polygonControl = s as PolygonControl;
                            List<DataStruct.Point> polygonPoints = new List<DataStruct.Point>();

                            foreach (var item in polygonControl.LineCollection)
                            {
                                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                                line.X1 = item.X1 * _designPanel.WidthRatio;
                                line.X2 = item.X2 * _designPanel.WidthRatio;
                                line.Y1 = item.Y1 * _designPanel.HeightRatio;
                                line.Y2 = item.Y2 * _designPanel.HeightRatio;
                                polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                                polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                            }
                            polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();
                            detailRegion.Add(polygonPoints);
                            detailMode.Add(SetInnerMaskMode);
                        }
                    }
                });

                _designPanel.SelectedDrawingShapeList.Clear();
                _designPanel.Clear();
                if (GetDictionary(detailRegion, detailMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetDetailRegion(valuePairsPoints, out Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        string message = LangGet.GetMessage("SVAViewModel_DetailDontCareError");

                        MessageBox.Show(message);

                        return;
                    }
                    ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                    regionImg.Dispose();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 清除Detail区域
        /// </summary>
        public void ClearDetail()
        {
            try
            {
                if (_designPanel == null) return;
                var shapes = _designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| _designPanel.SelectedReportItems.Count > 1*/)
                {
                    string message = LangGet.GetMessage("SVAViewModel_No_drawn_rectangles") + _designPanel.SelectedDrawingShapeList.Count.ToString();
                    MessageBox.Show(message, null, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                List<List<DataStruct.Point>> detailDontCareRegion = new List<List<Point>>();
                List<bool> detailDontCareMode = new List<bool>();

                _designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(rect, out List<DataStruct.Point> ps);
                        detailDontCareRegion.Add(ps);
                        detailDontCareMode.Add(SetInnerMaskMode);
                    }
                    else if (s.ItemType == DrawingReportItem.Polygon)
                    {
                        if (s is PolygonControl)
                        {
                            PolygonControl polygonControl = s as PolygonControl;
                            List<DataStruct.Point> polygonPoints = new List<DataStruct.Point>();

                            foreach (var item in polygonControl.LineCollection)
                            {
                                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                                line.X1 = item.X1 * _designPanel.WidthRatio;
                                line.X2 = item.X2 * _designPanel.WidthRatio;
                                line.Y1 = item.Y1 * _designPanel.HeightRatio;
                                line.Y2 = item.Y2 * _designPanel.HeightRatio;
                                polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                                polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                            }
                            polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();
                            detailDontCareRegion.Add(polygonPoints);
                            detailDontCareMode.Add(SetInnerMaskMode);
                        }
                    }
                });

                _designPanel.SelectedDrawingShapeList.Clear();
                _designPanel.Clear();
                if (GetDictionary(detailDontCareRegion, detailDontCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetDetailDontCareRegion(valuePairsPoints, out Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        string message = LangGet.GetMessage("SVAViewModel_DetailDontCareError");
                        MessageBox.Show(message);

                        return;
                    }
                    ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                    regionImg.Dispose();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 清除所有Detail区域
        /// </summary>
        public void ClearAllDetail()
        {
            try
            {
                if (_designPanel == null) return;
                _designPanel.Clear();
                _designPanel.SelectedDrawingShapeList.Clear();
                Errortype errortype = MarkLocationManagerService.GetInstance().ClearDetailDontCareRegion(out Camera regionImg);
                if (errortype != Errortype.OK)
                {
                    string message = LangGet.GetMessage("SVAViewModel_DetailDontCareError");
                    MessageBox.Show(message);

                    return;
                }
                ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                regionImg.Dispose();
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 设置Origin上
        /// </summary>
        public void SetOriginUp()
        {
            if (_designPanel == null || _designPanel.Shapes == null) return;
            if (_designPanel.Shapes.Count == 8)
            {
                if (_designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y - PixelSteps));
            }
        }

        /// <summary>
        /// 设置Origin左
        /// </summary>
        public void SetOriginLeft()
        {
            if (_designPanel == null || _designPanel.Shapes == null) return;
            if (_designPanel.Shapes.Count == 8)
            {
                if (_designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X - PixelSteps, _autoOrigin.Y));
            }
        }

        /// <summary>
        /// 设置Origin下
        /// </summary>
        public void SetOriginDown()
        {
            if (_designPanel == null || _designPanel.Shapes == null) return;
            if (_designPanel.Shapes.Count == 8)
            {
                if (_designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y + PixelSteps));
            }
        }

        /// <summary>
        /// 设置Origin右
        /// </summary>
        public void SetOriginRight()
        {
            if (_designPanel == null || _designPanel.Shapes == null) return;
            if (_designPanel.Shapes.Count == 8)
            {
                if (_designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X + PixelSteps, _autoOrigin.Y));
            }
        }

        /// <summary>
        /// 绘制圆
        /// </summary>
        public void ShowCircle()
        {
            try
            {
                if (IsDiplayCircle)
                {
                    if (_autoOrigin.X == 0 && _autoOrigin.Y == 0)
                    {
                        string message = LangGet.GetMessage("SVAViewModel_NoImageCenterPoint");

                        MessageBox.Show(message, null, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return;
                    }
                    DrawCircleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y), Radius);
                }
                else
                {
                    if (_designPanel == null || _designPanel.SelectedDrawingShapeList == null) return;
                    _designPanel.SelectedDrawingShapeList.Remove(_designPanel.SelectedDrawingShapeList.Find(s => s.ItemType == DrawingReportItem.Circle));
                    _designPanel.Shapes.Remove(_designPanel.Shapes.First(s => s.Region.GetType() == typeof(CircleRegion)));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 设置Origin显示十字
        /// </summary>
        public void DisplayCrosshair()
        {
            try
            {
                if (MarkLocationManagerService.GetInstance().GetOrigin(out Point origin) == Errortype.OK)
                {
                    _autoOrigin = origin;
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y));
                }
                else
                {
                    MaxwellControl.Controls.MessageBox.Show("GetCenterPointException");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 设置Origin自动确认图像起源
        /// </summary>
        public void AutoOrigin()
        {
            try
            {
                DisplayCrosshair();
                AutoOrginValue = $"X:{_autoOrigin.X.ToString("f3")} Y:{_autoOrigin.Y.ToString("f3")}";
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 设置Detail don't care区域
        /// </summary>
        public void SetDontDetail()
        {
            try
            {
                var shapes = _designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| _designPanel.SelectedReportItems.Count > 1*/)
                {
                    string message = LangGet.GetMessage("SVAViewModel_No_drawn_rectangles") + _designPanel.SelectedDrawingShapeList.Count.ToString();
                    MessageBox.Show(message, null, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                List<List<DataStruct.Point>> detailDontCareRegion = new List<List<Point>>();
                List<bool> detailDontCareMode = new List<bool>();

                _designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(rect, out List<DataStruct.Point> ps);
                        detailDontCareRegion.Add(ps);
                        detailDontCareMode.Add(SetInnerMaskMode);
                    }
                    else if (s.ItemType == DrawingReportItem.Polygon)
                    {
                        if (s is PolygonControl)
                        {
                            PolygonControl polygonControl = s as PolygonControl;
                            List<DataStruct.Point> polygonPoints = new List<DataStruct.Point>();

                            foreach (var item in polygonControl.LineCollection)
                            {
                                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                                line.X1 = item.X1 * _designPanel.WidthRatio;
                                line.X2 = item.X2 * _designPanel.WidthRatio;
                                line.Y1 = item.Y1 * _designPanel.HeightRatio;
                                line.Y2 = item.Y2 * _designPanel.HeightRatio;
                                polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                                polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                            }
                            polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();

                            detailDontCareRegion.Add(polygonPoints);
                            detailDontCareMode.Add(SetInnerMaskMode);
                        }
                    }
                });

                _designPanel.SelectedDrawingShapeList.Clear();
                _designPanel.Clear();
                if (GetDictionary(detailDontCareRegion, detailDontCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetDetailDontCareRegion(valuePairsPoints, out Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        string message = LangGet.GetMessage("SVAViewModel_DetailDontCareError");
                        MessageBox.Show(message);

                        return;
                    }
                    ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                    regionImg.Dispose();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 清除Detail don't care区域
        /// </summary>
        public void ClearDontDetail()
        {
            try
            {
                var shapes = _designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| _designPanel.SelectedReportItems.Count > 1*/)
                {
                    string message = LangGet.GetMessage("SVAViewModel_No_drawn_rectangles") + _designPanel.SelectedDrawingShapeList.Count.ToString();
                    MessageBox.Show(message, null, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                List<List<DataStruct.Point>> detailCareRegion = new List<List<Point>>();
                List<bool> detailCareMode = new List<bool>();

                _designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(rect, out List<DataStruct.Point> ps);
                        detailCareRegion.Add(ps);
                        detailCareMode.Add(SetInnerMaskMode);
                    }
                    else if (s.ItemType == DrawingReportItem.Polygon)
                    {
                        if (s is PolygonControl)
                        {
                            PolygonControl polygonControl = s as PolygonControl;
                            List<DataStruct.Point> polygonPoints = new List<DataStruct.Point>();

                            foreach (var item in polygonControl.LineCollection)
                            {
                                System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                                line.X1 = item.X1 * _designPanel.WidthRatio;
                                line.X2 = item.X2 * _designPanel.WidthRatio;
                                line.Y1 = item.Y1 * _designPanel.HeightRatio;
                                line.Y2 = item.Y2 * _designPanel.HeightRatio;
                                polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                                polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                            }
                            polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();

                            detailCareRegion.Add(polygonPoints);
                            detailCareMode.Add(SetInnerMaskMode);
                        }
                    }
                });

                _designPanel.SelectedDrawingShapeList.Clear();
                _designPanel.Clear();
                if (GetDictionary(detailCareRegion, detailCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetDetailRegion(valuePairsPoints, out Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        string message = LangGet.GetMessage("SVAViewModel_DetailDontCareError");

                        MessageBox.Show(message);

                        return;
                    }
                    ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                    regionImg.Dispose();
                }
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 清除所有Detail don't care区域
        /// </summary>
        public void ClearAllDontDetail()
        {
            try
            {
                if (_designPanel == null)
                {
                    return;
                }
                _designPanel.Clear();
                _designPanel.SelectedDrawingShapeList.Clear();
                Errortype errortype = MarkLocationManagerService.GetInstance().ClearDetailDontCareRegion(out Camera regionImg);
                if (errortype != Errortype.OK)
                {
                    string message = LangGet.GetMessage("SVAViewModel_DetailDontCareError");
                    MessageBox.Show(message);
                    return;
                }
                ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                regionImg.Dispose();
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// Ok
        /// </summary>
        public void SetOk()
        {
            Errortype ret = MarkLocationManagerService.GetInstance().GetTemplateShowImg(out Camera regionImg);
            if (ret != Errortype.OK)
            {
                MessageBox.Show("FailedGetTemplateImage");
                return;
            }
            ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);

            var vm = new MarkLocationSaveViewModel(ContinuousImage, PatternName);
            _windowManager.ShowDialog(vm);

        }

        //public string GetMarkName()
        //{
        //    if (this.moduleDev.ModuleType == ModuleTypeEnum.AVM)
        //    {
        //        return PatternName;
        //    }

        //    LeftOrRight leftOrRight = LeftOrRight.Left;
        //    if (PatternName == "Left")
        //        leftOrRight = LeftOrRight.Left;
        //    else if (PatternName == "Right")
        //        leftOrRight = LeftOrRight.Right;
        //    var resultObj = new ActionParamResult() { Param1 = PatternName };
        //    this.moduleDev.DoAction("GetMarkName", resultObj);
        //    return resultObj.Param2;
        //}

        /// <summary>
        /// Cancle
        /// </summary>
        public void SetCancle()
        {

        }

        /// <summary>
        /// 获取绘制图形
        /// </summary>
        /// <param name="shapes"></param>
        /// <param name="rectangle"></param>
        /// <param name="isInner"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        private bool GetDictionary(List<List<DataStruct.Point>> polygonIn, List<bool> isInner, out Dictionary<List<DataStruct.Point>, bool> p)
        {
            try
            {
                p = new Dictionary<List<DataStruct.Point>, bool>();
                for (int i = 0; i < polygonIn.Count; i++)
                {
                    p.Add(polygonIn[i], isInner[i]);
                }
                return true;
            }
            catch (Exception)
            {
                p = null;
                return false;
            }
        }

        /// <summary>
        /// 获取单个矩形的四个角点或者多段线的点
        /// </summary>
        /// <param name="shapes"></param>
        /// <param name="rectangle1"></param>
        /// <returns></returns>
        private void GetRect1Corner(Rectangle1 rectangle, out List<DataStruct.Point> polygon)
        {
            try
            {
                polygon = new List<DataStruct.Point>();
                if (rectangle is null)
                    return;
                double rowLeftUp = rectangle.Start_Y;
                double colLeftUp = rectangle.Start_X;
                double colRighttdown = rectangle.End_X;
                double rowRighttdown = rectangle.End_Y;
                var colRightUP = colRighttdown;
                var rowRightUP = rowLeftUp;
                var colLeftDown = colLeftUp;
                var rowLeftDown = rowRighttdown;
                polygon.Add(new DataStruct.Point(colLeftUp, rowLeftUp));
                polygon.Add(new DataStruct.Point(colRightUP, rowRightUP));
                polygon.Add(new DataStruct.Point(colRighttdown, rowRighttdown));
                polygon.Add(new DataStruct.Point(colLeftDown, rowLeftDown));
                return;
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 绘制十字
        /// </summary>
        /// <param name="centerPointT"></param>
        /// <param name="size"></param>
        [Obsolete]
        private void DrawCenterCross(System.Windows.Point centerPointT, int size)
        {
            try
            {
                if (_designPanel.Shapes.Count > 0)
                    _designPanel.Shapes.Clear();

                //_designPanel.ShapeContent = new LineShapeModel();
                //var shapelineH = new PrimShape();
                //shapelineH.Region = new LineRegion();
                //System.Windows.Point centerPoint = new System.Windows.Point(centerPointT.X / _designPanel.WidthRatio, centerPointT.Y / _designPanel.HeightRatio);

                //System.Windows.Point startPointH = new System.Windows.Point(centerPoint.X - size, centerPoint.Y);
                //System.Windows.Point endPointH = new System.Windows.Point(centerPoint.X + size, centerPoint.Y);
                //_designPanel.ShapeContent.SetReportItem(_designPanel, shapelineH.Region, startPointH, endPointH);
                //_designPanel.Shapes.Add(shapelineH);
                //var shapelineV = new PrimShape();
                //shapelineV.Region = new LineRegion();
                //System.Windows.Point startPointV = new System.Windows.Point(centerPoint.X, centerPoint.Y - size);
                //System.Windows.Point endPointV = new System.Windows.Point(centerPoint.X, centerPoint.Y + size);
                //_designPanel.ShapeContent.SetReportItem(_designPanel, shapelineV.Region, startPointV, endPointV);
                //_designPanel.Shapes.Add(shapelineV);
                _designPanel.Items.Add(new LineShapeDrawing() { X = centerPointT.X - size, Y = centerPointT.Y, X1 = centerPointT.X + size, Y1 = centerPointT.Y, Range = 1000 });
                _designPanel.Items.Add(new LineShapeDrawing() { X = centerPointT.X, Y = centerPointT.Y - size, X1 = centerPointT.X, Y1 = centerPointT.Y + size });
            }
            catch (Exception)
            {
                throw;
            }
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
                DataStruct.Rectangle2 rectangleH = new DataStruct.Rectangle2(centerPointT.X/* / _designPanel.WidthRatio*/, centerPointT.Y/* / _designPanel.HeightRatio*/, LineSize, LineWidth, rad);
                List<DataStruct.Point> pointsH = rectangleH.GetCornerPoints();
                pointsH.Add(pointsH[0]);

                DataStruct.Rectangle2 rectangleV = new DataStruct.Rectangle2(centerPointT.X/* / _designPanel.WidthRatio*/, centerPointT.Y/* / _designPanel.HeightRatio*/, LineSize, LineWidth, rad + Math.PI / 2);
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

        /// <summary>
        /// 绘制圆
        /// </summary>
        /// <param name="centerPointT"></param>
        private void DrawCircleCenterCross(System.Windows.Point centerPointT, double radius)
        {
            try
            {
                if (_designPanel == null || _designPanel.SelectedDrawingShapeList == null) return;
                _designPanel.SelectedDrawingShapeList.Remove(_designPanel.SelectedDrawingShapeList.Find(s => s.ItemType == DrawingReportItem.Circle));
                //  _designPanel.Shapes.RemoveIfExists(_designPanel.Shapes.FirstOrDefault(s => s.Region.GetType() == typeof(CircleRegion)));

                foreach (var item in _designPanel.Shapes)
                {
                    if (item.Region.GetType() == typeof(CircleRegion))
                    {
                        _designPanel.Shapes.Remove(item);
                    }
                }

                //_designPanel.ShapeContent = new CircleShapeModel();
                //var shaperect = new PrimShape();
                //shaperect.Region = new CircleRegion();
                //CircleRegion region = shaperect.Region as CircleRegion;
                //region.CenterPoint = centerPointT;
                //region.Radius = radius;
                ////  _designPanel.ShapeContent.SetReportItem(_designPanel, shaperect.Region, new System.Windows.Point(), new System.Windows.Point());
                //_designPanel.Shapes.Add(shaperect);
                _designPanel.Items.Add(new CircleShapeDrawing(){CenterPoint = centerPointT ,RadiusX = radius ,RadiusY = radius });
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        /// 绘制矩形十字
        /// </summary>
        /// <param name="centerPointT"></param>
        [Obsolete]
        private void DrawRectangleCenterCrossControl(System.Windows.Point centerPointT)
        {
            try
            {
                //if (_designPanel.Shapes.Count > 0)
                //    _designPanel.Shapes.Clear();
                //_designPanel.ShapeContent = new RectShapeModel();
                //var shaperectH = new PrimShape();
                //shaperectH.Region = new RectRegion();
                //RectRegion regionH = shaperectH.Region as RectRegion;
                //System.Windows.Point centerPoint = new System.Windows.Point(centerPointT.X / _designPanel.WidthRatio, centerPointT.Y / _designPanel.HeightRatio);
                //regionH.CenterPoint = centerPoint;
                //regionH.Width = LineSize * _designPanel.WidthRatio;
                //regionH.Height = LineWidth * _designPanel.HeightRatio;
                //regionH.Angle = Rotation;
                //_designPanel.Shapes.Add(shaperectH);

                //var shaperectV = new PrimShape();
                //shaperectV.Region = new RectRegion();
                //RectRegion regionV = shaperectV.Region as RectRegion;
                //regionV.CenterPoint = centerPoint;
                //regionV.Width = LineSize * _designPanel.WidthRatio;
                //regionV.Height = LineWidth * _designPanel.HeightRatio;
                //regionV.Angle = Rotation + 10;
                //_designPanel.Shapes.Add(shaperectV);

                //_designPanel.Items.Add(new RectShapeDrawing() { });
                //_designPanel.Items.Add(new RectShapeDrawing() { });
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// 上下左右移动矩形
        /// </summary>
        /// <param name="rectangleItem"></param>
        /// <param name="typeDirection"></param>
        private void SelectMode(Rectangle1 rectangleItem, string typeDirection)
        {
            if (_designPanel.SelectedDrawingShapeList.Count == 0)
            {
                string message = LangGet.GetMessage("SVAViewModel_NoDrawableArea");

                MessageBox.Show(message, null, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Error);
                return;
            }

            switch (typeDirection)
            {
                case "Up":
                    if (SetRegionModeMove)
                    {
                        if (rectangleItem.Start_Y <= 0)
                            rectangleItem.Start_Y = 0;
                        else
                        {
                            rectangleItem.End_Y -= Convert.ToDouble(StepWidth);
                            rectangleItem.Start_Y -= Convert.ToDouble(StepWidth);
                        }
                    }
                    //else if (SetRegionModeResize)
                    //{
                    //    if (rectangleItem.Start_Y <= 0)
                    //        rectangleItem.Start_Y = 0;
                    //    else
                    //        rectangleItem.Start_Y -= Convert.ToDouble(StepWidth);

                    //}
                    break;

                case "Down":
                    if (SetRegionModeMove)
                    {
                        if (rectangleItem.Start_Y >= rectangleItem.End_Y)
                            rectangleItem.Start_Y = rectangleItem.End_Y;
                        else
                        {
                            rectangleItem.End_Y += Convert.ToDouble(StepWidth);
                            rectangleItem.Start_Y += Convert.ToDouble(StepWidth);
                        }
                    }
                    //else if (SetRegionModeResize)
                    //{
                    //    if (rectangleItem.End_Y >= _designPanel.ActualHeight)
                    //        rectangleItem.End_Y = _designPanel.ActualHeight;
                    //    else
                    //        rectangleItem.Start_Y += Convert.ToDouble(StepWidth);

                    //}
                    break;

                case "Left":
                    if (SetRegionModeMove)
                    {
                        if (rectangleItem.Start_X <= 0)
                            rectangleItem.Start_X = 0;
                        else
                        {
                            rectangleItem.End_X -= Convert.ToDouble(StepWidth);
                            rectangleItem.Start_X -= Convert.ToDouble(StepWidth);
                        }
                    }
                    //else if (SetRegionModeResize)
                    //{
                    //    if (rectangleItem.Start_X <= 0)
                    //        rectangleItem.Start_X = 0;
                    //    else
                    //        rectangleItem.Start_X -= Convert.ToDouble(StepWidth);

                    //}
                    break;

                case "Right":
                    if (SetRegionModeMove)
                    {
                        if (rectangleItem.Start_X >= rectangleItem.End_X)
                            rectangleItem.Start_X = rectangleItem.End_X;
                        else
                        {
                            rectangleItem.End_X += Convert.ToDouble(StepWidth);
                            rectangleItem.Start_X += Convert.ToDouble(StepWidth);
                        }
                    }
                    //else if (SetRegionModeResize)
                    //{
                    //    if (rectangleItem.End_X >= _designPanel.ActualWidth)
                    //        rectangleItem.End_X = _designPanel.ActualWidth;
                    //    else
                    //        rectangleItem.Start_X += Convert.ToDouble(StepWidth);

                    //}
                    break;

                default:
                    break;
            }

            SetRegionX = (_designPanel.SelectedDrawingShapeList[0].ItemTop = rectangleItem.Start_Y).ToString();
            SetRegionY = (_designPanel.SelectedDrawingShapeList[0].ItemLeft = rectangleItem.Start_X).ToString();
            SetRegionWidth = (_designPanel.SelectedDrawingShapeList[0].ItemWidth = rectangleItem.End_X - rectangleItem.Start_X).ToString();
            SetRegionHeight = (_designPanel.SelectedDrawingShapeList[0].ItemHeight = rectangleItem.End_Y - rectangleItem.Start_Y).ToString();
        }

        /// <summary>
        /// 上下左右移动多段线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="typeDirection"></param>
        private void SelectMode(List<Line> lines, string typeDirection)
        {
            if (_designPanel.SelectedDrawingShapeList.Count == 0)
            {
                string message = LangGet.GetMessage("SVAViewModel_NoDrawableArea");
                MessageBox.Show(message, null, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Error);
                return;
            }
            switch (typeDirection)
            {
                case "Up":
                    if (SetRegionModeMove)
                    {
                        if (lines[0].Y1 <= 0)
                            lines[0].Y1 = 0;
                        else
                            for (int i = 0; i < lines.Count; i++)
                            {
                                lines[i].Y1 -= Convert.ToDouble(StepWidth);
                            }

                        SetRegionX = (_designPanel.SelectedDrawingShapeList[0].ItemTop -= Convert.ToDouble(StepWidth)).ToString();
                        SetRegionY = (_designPanel.SelectedDrawingShapeList[0].ItemLeft).ToString();
                    }
                    break;

                case "Down":
                    if (SetRegionModeMove)
                    {
                        if (lines[0].Y1 >= _designPanel.ActualHeight)
                            lines[0].Y1 = _designPanel.ActualHeight;
                        else
                            for (int i = 0; i < lines.Count; i++)
                            {
                                lines[i].Y1 += Convert.ToDouble(StepWidth);
                            }
                        SetRegionX = (_designPanel.SelectedDrawingShapeList[0].ItemTop += Convert.ToDouble(StepWidth)).ToString();
                        SetRegionY = (_designPanel.SelectedDrawingShapeList[0].ItemLeft).ToString();
                    }
                    break;

                case "Left":
                    if (SetRegionModeMove)
                    {
                        if (lines[0].X1 <= 0)
                            lines[0].X1 = 0;
                        else
                            for (int i = 0; i < lines.Count; i++)
                            {
                                lines[i].X1 -= Convert.ToDouble(StepWidth);
                            }
                        SetRegionX = (_designPanel.SelectedDrawingShapeList[0].ItemTop).ToString();
                        SetRegionY = (_designPanel.SelectedDrawingShapeList[0].ItemLeft -= Convert.ToDouble(StepWidth)).ToString();
                    }
                    break;

                case "Right":
                    if (SetRegionModeMove)
                    {
                        if (lines[0].X1 >= _designPanel.ActualWidth)
                            lines[0].X1 = _designPanel.ActualWidth;
                        else
                            for (int i = 0; i < lines.Count; i++)
                            {
                                lines[i].X1 += Convert.ToDouble(StepWidth);
                            }
                        SetRegionX = (_designPanel.SelectedDrawingShapeList[0].ItemTop).ToString();
                        SetRegionY = (_designPanel.SelectedDrawingShapeList[0].ItemLeft += Convert.ToDouble(StepWidth)).ToString();
                    }
                    break;

                default:
                    break;
            }

            SetRegionWidth = (_designPanel.SelectedDrawingShapeList[0].ItemWidth).ToString();
            SetRegionHeight = (_designPanel.SelectedDrawingShapeList[0].ItemHeight).ToString();
        }

        /// <summary>
        /// 获取控件上绘制矩形的Rectangle
        /// </summary>
        /// <returns></returns>
        private Rectangle1 GetRegionItemRectangle()
        {
            Rectangle1 rectangle = new Rectangle1();
            if (_designPanel == null) return null;
            if (_designPanel.SelectedDrawingShapeList.Count > 0)
            {
                foreach (IReportItemControl item in _designPanel.SelectedDrawingShapeList)
                {
                    if (item.ItemType != DrawingReportItem.Rectangle)
                    {
                        continue;
                    }
                    rectangle.Start_X = item.ItemLeft;
                    rectangle.Start_Y = item.ItemTop;
                    rectangle.End_X = item.ItemLeft + item.ItemWidth;
                    rectangle.End_Y = item.ItemTop + item.ItemHeight;
                    break;
                }
                return rectangle;
            }
            return rectangle;
        }

        /// <summary>
        /// 获取图片上绘制矩形的Rectangle
        /// </summary>
        /// <returns></returns>
        private Rectangle1 GetRegionPixRectangle()
        {
            Rectangle1 rectangle = new Rectangle1();
            if (_designPanel == null) return null;
            if (_designPanel.SelectedDrawingShapeList.Count > 0)
            {
                foreach (IReportItemControl item in _designPanel.SelectedDrawingShapeList)
                {
                    if (item.ItemType != DrawingReportItem.Rectangle)
                    {
                        continue;
                    }
                    rectangle.Start_X = item.PixLeft;
                    rectangle.Start_Y = item.PixTop;
                    rectangle.End_X = item.PixLeft + item.PixWidth;
                    rectangle.End_Y = item.PixTop + item.PixHeight;
                    break;
                }
                return rectangle;
            }
            return rectangle;
        }

        #endregion

        /// <summary>
        /// 获取多段线点位
        /// </summary>
        /// <returns></returns>
        private List<Line> GetRegionItemPolygon()
        {
            List<Line> polygonPoint = new List<Line>();
            List<Point> points = new List<Point>();
            if (_designPanel == null) return null;
            if (_designPanel.SelectedDrawingShapeList.Count > 0)
            {
                foreach (var item in _designPanel.SelectedDrawingShapeList)
                {
                    if (item.ItemType != DrawingReportItem.Polygon)
                    {
                        continue;
                    }
                    PolygonControl polygonControl = (PolygonControl)item;
                    foreach (var s in polygonControl.LineCollection)
                    {
                        Line line = new Line();
                        line.X1 = s.X1;
                        line.X2 = s.X2;
                        line.Y1 = s.Y1;
                        line.Y2 = s.Y2;
                        polygonPoint.Add(line);
                        points.Add(new Point(line.X1, line.Y1));
                        points.Add(new Point(line.X2, line.Y2));
                    }
                    points = points.Where((x, x_index) => x_index == (points.FindIndex(y => y == x))).ToList();
                }

                return polygonPoint;
            }
            return polygonPoint;
        }

        #region Search

        private List<Camera> _offLineImages = null;
        private bool _isLoad = false;

        /// <summary>
        /// 搜索
        /// </summary>
        public async void Search()
        {
            if (_isLoad)
            {
                await Task.Run(() =>
                {
                    SearchOffLine();
                });
            }
            else
            {
                await Task.Run(() =>
                {
                    SearchOnLine();
                });
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
                    ProgressValue = (Convert.ToDouble(i) / Convert.ToDouble(Repeatings)) * 80;

                    Camera camera = ImageHelper.GetCamera(_camera.SnapImage());
                    Thread.Sleep(InterVal);
                    ret = MatchImg(camera, PatternName, null, out List<Tuple<double, double, double, double>> matchResult);
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

                ret = MarkLocationManagerService.GetInstance().GetDataSummary(keyValuePairs, out Dictionary<string, DataStatisticParam> summaryResult);
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
                    ProgressValue = (Convert.ToDouble(i) / _offLineImages.Count) * 80;

                    Thread.Sleep(InterVal);
                    ret = MatchImg(_offLineImages[i], PatternName, null, out List<Tuple<double, double, double, double>> matchResult);
                    if (ret != Errortype.OK)
                    {
                        MaxwellControl.Controls.MessageBox.Show(ret.ToString());
                        return;
                    }

                    if (matchResult.Count > 1 || matchResult.Count == 0)
                    {
                        ContinuousImage = ImageHelper.Camera2WritableBitmap(_offLineImages[i]);
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

                ret = MarkLocationManagerService.GetInstance().GetDataSummary(keyValuePairs, out Dictionary<string, DataStatisticParam> summaryResult);
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
        /// 对单个模板图像进行匹配
        /// </summary>
        /// <param name="img"></param>
        /// <param name="markName"></param>
        /// <param name="matchRegion"></param>
        /// <param name="matchResult"></param>
        private Errortype MatchImg(Camera img, string markName, Rectangle1 matchRegion, out List<Tuple<double, double, double, double>> matchResult)
        {
            matchResult = new List<Tuple<double, double, double, double>>();
            Errortype ret = MarkLocationManagerService.GetInstance().GetMarkCenter(markName, img, matchRegion,
                out double[] rows, out double[] cols, out double[] angles, out double[] scores, out List<List<double[]>> straightnessErrorList);

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
        /// 本地加载图像
        /// </summary>
        public void Acquire()
        {
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

        #endregion

        #region Set Up
        private TrainMarkLocationSetupParameter _trainMarkLocationSetupParameter;

        private ObservableCollection<TrainSetupParameterItem> _setupParameterList = new ObservableCollection<TrainSetupParameterItem>();
        public ObservableCollection<TrainSetupParameterItem> SetupParameterList
        {
            get
            {
                return _setupParameterList;
            }
            set
            {
                _setupParameterList = value;
                OnPropertyChanged(nameof(SetupParameterList));
            }
        }

        private ObservableCollection<TrainSetupEnumItem> _comboboxSetupParameterList = new ObservableCollection<TrainSetupEnumItem>();
        public ObservableCollection<TrainSetupEnumItem> ComboboxSetupParameterList
        {
            get
            {
                return _comboboxSetupParameterList;
            }
            set
            {
                _comboboxSetupParameterList = value;
                OnPropertyChanged(nameof(ComboboxSetupParameterList));
            }
        }

        /// <summary>
        /// 下拉框内容互锁
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ComboboxSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboboxSetupParameterList.Count >= 3)
            {
                var values = Enum.GetValues(typeof(PatternMetric));
                if (ComboboxSetupParameterList[0].ItemIndex is 0)
                {
                    ComboboxSetupParameterList[1].TemplateType = values.Cast<PatternMetric>().Select(v => v.ToString()).Take(values.Length - 1).ToList();
                }
                else
                {
                    ComboboxSetupParameterList[1].TemplateType = values.Cast<PatternMetric>().Select(v => v.ToString()).ToList();
                }
            }
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        public void SetupSave()
        {
            try
            {
                _trainMarkLocationSetupParameter.Write(System.IO.Path.Combine(GlobalPathHelper.G_ParameterConfigurePath, _trainMarkLocationSetupParameter.GetType().Name + ModuleName + ".xml"));
                int minLength = int.Parse(_trainMarkLocationSetupParameter.TrainSetupParameterItems[0].ItemValue);
                int edgeThreshold = int.Parse(_trainMarkLocationSetupParameter.TrainSetupParameterItems[1].ItemValue);
                double scoreThreshold = double.Parse(_trainMarkLocationSetupParameter.TrainSetupParameterItems[2].ItemValue);
                TemplateType templateType = (TemplateType)_trainMarkLocationSetupParameter.ComboboxTrainSetupParameterItems[0].ItemIndex;
                PatternMetric metric = (PatternMetric)_trainMarkLocationSetupParameter.ComboboxTrainSetupParameterItems[1].ItemIndex;
                bool polarity = (LinePolarity)_trainMarkLocationSetupParameter.ComboboxTrainSetupParameterItems[2].ItemIndex is LinePolarity.On ? true : false;
                Errortype errortype = MarkLocationManagerService.GetInstance().SetParams(minLength, edgeThreshold, scoreThreshold, templateType, metric, polarity);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }
        #endregion

    }
}