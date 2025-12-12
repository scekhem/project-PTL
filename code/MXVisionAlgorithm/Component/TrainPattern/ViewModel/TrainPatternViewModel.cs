using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using DataStruct;
using MaxwellFramework.Core.Common;
using MaxwellFramework.Core.Common.Command;
using MaxwellFramework.Core.Interfaces;
using Microsoft.Win32;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using MwFramework.Controls.ControlCanvas.Model;
using MXVisionAlgorithm.Common;
using MXVisionAlgorithm.Component.TemplateMatch.View;
using MXVisionAlgorithm.Component.TrainPattern.View;
using Stylet;
using MessageBox = MaxwellControl.Controls.MessageBox;

namespace MXVisionAlgorithm.Component.TrainPattern.ViewModel
{
    public class MatchParameter
    {
        public string ParameterInfo { get; set; }
        public double ParameterX { get; set; }
        public double ParameterY { get; set; }
        public double ParameterRotation { get; set; }
        public double ParameterScore { get; set; }
        public MatchParameter(string parameterInfo, double parameterX, double parameterY, double parameterRotation, double parameterScore)
        {
            ParameterInfo = parameterInfo;
            ParameterX = parameterX;
            ParameterY = parameterY;
            ParameterRotation = parameterRotation;
            ParameterScore = parameterScore;

        }
        public MatchParameter()
        {

        }
    }

    public enum DrawShapes
    {
        None = 0,
        Rectangle = 1,
        Polygon = 2,
    }

    public enum LeftOrRight
    {
        Left = 0,
        Right = 1,
        LeftAndRight
    }

    public class TrainPatternViewModel : Screen, IPage
    {
        public string Name = "TemplateTrainView";

        #region Property

        private TrainPatternView _thisView;

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
        #endregion

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
        private SolidColorBrush _solideRectangle = new SolidColorBrush(System.Windows.Media.Color.FromRgb(122, 122, 122));
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

        private SolidColorBrush _solidePolygon = new SolidColorBrush(System.Windows.Media.Color.FromRgb(122, 122, 122));
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
        #endregion

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
        #endregion

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
        private DataStruct.Point _autoOrigin = new DataStruct.Point(0, 0);
        #endregion

        #region SetupParam
        private int _minLength = 10;

        public int MinLength
        {
            get { return _minLength; }
            set
            {
                _minLength = value;
                OnPropertyChanged(nameof(MinLength));
            }
        }

        private int _edgeThreshold = 30;

        public int EdgeThreshold
        {
            get { return _edgeThreshold; }
            set
            {
                _edgeThreshold = value;
                OnPropertyChanged(nameof(EdgeThreshold));
            }
        }

        private double _scoreThreshold = 0.8;

        public double ScoreThreshold
        {
            get { return _scoreThreshold; }
            set
            {
                _scoreThreshold = value;
                OnPropertyChanged(nameof(ScoreThreshold));
            }
        }

        private int _measureLength1 = 15;

        public int MeasureLength1
        {
            get { return _measureLength1; }
            set
            {
                _measureLength1 = value;
                OnPropertyChanged(nameof(MeasureLength1));
            }
        }

        private string _templateType = "NCC";

        public string TemplateType
        {
            get { return _templateType; }
            set
            {
                _templateType = value;
                OnPropertyChanged(nameof(TemplateType));
                TemplateTypeChangeMetric();
            }
        }

        private string _metric = "Use";
        public string Metric
        {
            get { return _metric; }
            set
            {
                _metric = value;
                OnPropertyChanged(nameof(Metric));
            }
        }

        private string _linePolarity = "true";
        public string LinePolarity
        {
            get { return _linePolarity; }
            set
            {
                _linePolarity = value;
                OnPropertyChanged(nameof(LinePolarity));
            }
        }


        #endregion

        #region
        private ObservableCollection<MatchParameter> _matchParameterList = new ObservableCollection<MatchParameter>();
        public ObservableCollection<MatchParameter> MatchParameterList
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
        #endregion

        #region Match
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
                    SelectedExpander5 = false;
                    SelectedExpander6 = false;
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
                    SelectedExpander5 = false;
                    SelectedExpander6 = false;
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

        #endregion

        /// <summary>
        /// 图像显示控件
        /// </summary>
        private DesignPanel designPanel = null;

        private IWindowManager windowManager;

        private DrawShapes drawShapes;

        private IRouter router;

        private DataStruct.Rectangle1 matchRectangle = null;

        private bool isLoad = false;
        private string pattern { get; set; }

        private Dispatcher dispather;
        /// <summary>
        #endregion

        #region Construct

        public TrainPatternViewModel()
        {

        }

        public TrainPatternViewModel(string patternName)
        {
            dispather = Dispatcher.CurrentDispatcher;
            windowManager = IoC.Get<IWindowManager>();
            pattern = patternName;
            InitCommand();
            router = IoC.Get<IRouter>();
        }
        #endregion

        #region Function
        /// <summary>
        /// 页面重新加载时
        /// </summary>
        protected override void OnViewLoaded()
        {
            SaveSetUpParam();
            _thisView = this.View as TrainPatternView;
            _thisView.TemplateTypeCbx.ItemsSource = new List<string>
            {
                DataStruct.TemplateType.NCC.ToString(),
                DataStruct.TemplateType.SHAPE.ToString(),
                DataStruct.TemplateType.SHAPEXLD.ToString(),
            };

            _thisView.MetricCbx.ItemsSource = new List<string>
            {
                "Use",
                "IgnoreGlobal",
                "IgnoreLocal"
            };

            _thisView.LinePolarityCbx.ItemsSource = new List<string>
            {
                "true",
                "false",
            };
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

        DataStruct.Rectangle1 rectangle = null;

        /// <summary>
        /// Polygon缩放比例时触发
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void CmbScaleSelectionChanged(object obj)
        {
            try
            {
                if (designPanel == null) return;
                if (designPanel.SelectedDrawingShapeList.Count > 0)
                {
                    foreach (var item in designPanel.SelectedDrawingShapeList)
                    {
                        if (item is PolygonControl)
                        {
                            item.IsFocusedItem = true;
                            designPanel.SetScalePolyon(Convert.ToDouble(Scale));
                        }
                        if (item.ItemType is DrawingReportItem.Rectangle)
                        {
                            item.IsFocusedItem = true;
                            if (rectangle == null)
                                rectangle = GetRegionItemRectangle();
                            if (rectangle != null)
                            {
                                ScaleRectangle(rectangle, item);
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
        /// 缩放矩形
        /// </summary>
        /// <param name="rectangleItem"></param>
        /// <param name="itemControl"></param>
        private void ScaleRectangle(DataStruct.Rectangle1 rectangleItem, IReportItemControl itemControl)
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
                        MessageBox.Show("未获取到图像中心点", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                if (designPanel == null) return;
                if (designPanel.Shapes.Count == 8 || designPanel.Shapes.Count == 9)
                {
                    if (designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
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
                if (designPanel == null) return;
                if (designPanel.Shapes.Count == 8 || designPanel.Shapes.Count == 9)
                {
                    if (designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
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
                if (designPanel == null) return;
                if (designPanel.Shapes.Count == 8 || designPanel.Shapes.Count == 9)
                {
                    if (designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
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
            designPanel = obj as DesignPanel;
            if (designPanel == null) return;
            MotorX = designPanel.MouseX;
            MotorY = designPanel.MouseY;

        }

        /// <summary>
        /// 从相机中获取图像
        /// </summary>
        public void AcquiredImage()
        {

        }

        /// <summary>
        /// 从库中获取图像
        /// </summary>
        public void LoadImageFromLibrary()
        {
            try
            {
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MarkModel");
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                string markName = pattern;
                if (markName == string.Empty)
                {
                    MaxwellControl.Controls.MessageBox.Show($"当前库中{markName}模板不存在！");
                    return;
                }
                Errortype errortype = MarkLocationManagerService.GetInstance().Load(filePath, markName);
                if (errortype == Errortype.OK)
                {
                    Errortype ret = MarkLocationManagerService.GetInstance().GetTemplateShowImg(out DataStruct.Camera regionImg);
                    if (ret != Errortype.OK)
                    {
                        MessageBox.Show("获取模板图失败！");
                        return;
                    }
                    ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
                    WriteableBitmap tempImage = ContinuousImage.Clone();
                    keyValuePairs.AddOrUpdate(0, tempImage, (k, v) => tempImage);
                }
            }
            catch (Exception)
            {

                throw;
            }
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
                    string ImportFile = openFileDialog.FileName;
                    ImportImage(ImportFile);
                }
                catch (Exception)
                {
                    MaxwellControl.Controls.MessageBox.Show("格式不正确！");
                }
            }
        }

        /// <summary>
        /// 从文件中获取bitmap
        /// </summary>
        /// <param name="filePath"></param>
        private void ImportImage(string filePath)
        {
            Bitmap bitmap = (Bitmap)System.Drawing.Image.FromFile(filePath);
            BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            int num = bitmapData.Stride * bitmapData.Height;
            byte[] array = new byte[num];
            Marshal.Copy(bitmapData.Scan0, array, 0, num);
            bitmap.UnlockBits(bitmapData);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int bitCnt = ((bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed) ? 1 : ((bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb) ? 3 : 4));
            bitmap.Dispose();
            UpdateWritableBitmap(array, width, height, bitCnt);
            WriteableBitmap tempImage = ContinuousImage.Clone();

            keyValuePairs.AddOrUpdate(0, tempImage, (k, v) => tempImage);
        }

        /// <summary>
        /// 获取WriteableBitmap
        /// </summary>
        /// <param name="byt"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="bitCnt"></param>
        private void UpdateWritableBitmap(byte[] byt, int width, int height, int bitCnt)
        {
            ContinuousImage = new WriteableBitmap(width, height, 96.0, 96.0, bitCnt == 1 ? System.Windows.Media.PixelFormats.Gray8 : (bitCnt == 3 ? System.Windows.Media.PixelFormats.Bgr24 : System.Windows.Media.PixelFormats.Bgra32),
                        null);
            ContinuousImage.Lock();
            Marshal.Copy(byt, 0, ContinuousImage.BackBuffer, byt.Length);
            ContinuousImage.AddDirtyRect(new Int32Rect(0, 0, width, height));
            ContinuousImage.Unlock();

        }

        /// <summary>
        /// Region Up
        /// </summary>
        public void SetRegionUp()
        {
            if (designPanel == null || designPanel.SelectedDrawingShapeList.Count == 0) return;

            foreach (var item in designPanel.SelectedDrawingShapeList)
            {
                if (item.Shape.Region is PolygonRegion || drawShapes == DrawShapes.Polygon)
                {
                    List<Line> lines = GetRegionItemPolygon();
                    if (lines == null) return;
                    SelectMode(lines, "Up");
                }
                if (item.Shape.Region is RectRegion || drawShapes == DrawShapes.Rectangle)
                {
                    Rectangle1 rectangleItem = GetRegionItemRectangle();
                    if (rectangleItem == null) return;
                    SelectMode(rectangleItem, "Up");
                    Rectangle1 rectanglePix = GetRegionPixRectangle();
                    /*************************给算法***********************/
                }
            }

        }

        /// <summary>
        /// Region Left
        /// </summary>
        public void SetRegionLeft()
        {
            if (designPanel == null || designPanel.SelectedDrawingShapeList.Count == 0) return;
            foreach (var item in designPanel.SelectedDrawingShapeList)
            {
                if (item.Shape.Region is PolygonRegion || drawShapes == DrawShapes.Polygon)
                {
                    List<Line> lines = GetRegionItemPolygon();
                    if (lines == null) return;
                    SelectMode(lines, "Left");
                }
                if (item.Shape.Region is RectRegion || drawShapes == DrawShapes.Rectangle)
                {
                    Rectangle1 rectangleItem = GetRegionItemRectangle();
                    if (rectangleItem == null) return;
                    SelectMode(rectangleItem, "Left");
                    Rectangle1 rectanglePix = GetRegionPixRectangle();
                    /*************************给算法***********************/
                }
            }
        }

        /// <summary>
        /// Region Down
        /// </summary>
        public void SetRegionDown()
        {
            if (designPanel == null || designPanel.SelectedDrawingShapeList.Count == 0) return;
            foreach (var item in designPanel.SelectedDrawingShapeList)
            {
                if (item.Shape.Region is PolygonRegion || drawShapes == DrawShapes.Polygon)
                {
                    List<Line> lines = GetRegionItemPolygon();
                    if (lines == null) return;
                    SelectMode(lines, "Down");
                }
                if (item.Shape.Region is RectRegion || drawShapes == DrawShapes.Rectangle)
                {
                    Rectangle1 rectangleItem = GetRegionItemRectangle();
                    if (rectangleItem == null) return;
                    SelectMode(rectangleItem, "Down");
                    Rectangle1 rectanglePix = GetRegionPixRectangle();
                    /*************************给算法***********************/
                }
            }
        }

        /// <summary>
        /// Region Right
        /// </summary>
        public void SetRegionRight()
        {
            if (designPanel == null || designPanel.SelectedDrawingShapeList.Count == 0) return;
            foreach (var item in designPanel.SelectedDrawingShapeList)
            {
                if (item.Shape.Region is PolygonRegion || drawShapes == DrawShapes.Polygon)
                {
                    List<Line> lines = GetRegionItemPolygon();
                    if (lines == null) return;
                    SelectMode(lines, "Right");
                }
                if (item.Shape.Region is RectRegion || drawShapes == DrawShapes.Rectangle)
                {
                    Rectangle1 rectangleItem = GetRegionItemRectangle();
                    if (rectangleItem == null) return;
                    SelectMode(rectangleItem, "Right");
                    Rectangle1 rectanglePix = GetRegionPixRectangle();
                    /*************************给算法***********************/
                }
            }
        }

        /// <summary>
        /// 设置绘制矩形区域
        /// </summary>
        public void SetRegion()
        {
            if (designPanel == null) return;
            var shapes = designPanel.SelectedDrawingShapeList;
            if (shapes == null || shapes.Count == 0)
            {
                MessageBox.Show("未查找到绘制的区域", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (shapes.Exists(t => t.ItemType != DrawingReportItem.Rectangle))
            {
                MaxwellControl.Controls.MessageBox.Show("绘制区域不是矩形请重新绘制");
                designPanel.SelectedDrawingShapeList.Clear();
                designPanel.Clear();
                return;
            }

            Rectangle1 rect = new Rectangle1();
            designPanel.SelectedDrawingShapeList.ForEach(s =>
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
            designPanel.SelectedDrawingShapeList.Clear();
            designPanel.Clear();
            //  Camera regionImg = new Camera();
            //if( ContinuousImage.Format== System.Windows.Media.PixelFormat )
            Errortype errortype = MarkLocationManagerService.GetInstance().SetRegionRectangle(ImageHelper.GetCamera(ContinuousImage), rect, out DataStruct.Camera regionImg);
            if (errortype != Errortype.OK)
            {
                MessageBox.Show("获取矩形图像异常");
                return;
            }
            ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
        }

        /// <summary>
        /// 清除绘制单个矩形区域
        /// </summary>
        public void ClearRegion()
        {
            if (designPanel == null) return;
            if (rectangle != null) rectangle = null;
            if (designPanel.SelectedDrawingShapeList.Count > 0)
            {
                designPanel.SelectedDrawingShapeList.Clear();
                designPanel.Clear();
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
                if (designPanel == null) return;
                var shapes = designPanel.SelectedDrawingShapeList;
                if (shapes == null/* || designPanel.SelectedReportItems.Count > 1 || designPanel.SelectedReportItems.Count == 0*/)
                {
                    MessageBox.Show($"未查找到绘制的矩形或当前存在绘制矩形个数：{designPanel.SelectedDrawingShapeList.Count}", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                List<List<DataStruct.Point>> trainDontCareRegion = new List<List<DataStruct.Point>>();
                List<bool> trainDontCareMode = new List<bool>();
                designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(drawShapes, rect, out List<DataStruct.Point> ps);
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
                            line.X1 = item.X1 * designPanel.WidthRatio;
                            line.X2 = item.X2 * designPanel.WidthRatio;
                            line.Y1 = item.Y1 * designPanel.HeightRatio;
                            line.Y2 = item.Y2 * designPanel.HeightRatio;
                            polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                            polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                        }
                        polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();

                        trainDontCareRegion.Add(polygonPoints);
                        trainDontCareMode.Add(SetInnerMaskMode);
                    }
                });

                designPanel.SelectedDrawingShapeList.Clear();
                designPanel.Clear();
                if (GetDictionary(trainDontCareRegion, trainDontCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetTrainDontCareRegion(valuePairsPoints, out DataStruct.Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show("获取TrainDontCare区域图像异常");
                        return;
                    }
                    errortype = MarkLocationManagerService.GetInstance().SetDetailDontCareRegion(valuePairsPoints, out DataStruct.Camera regionImgT);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show("获取TrainDontCare区域图像异常");
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
                if (designPanel == null) return;
                var shapes = designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| designPanel.SelectedReportItems.Count > 1 || designPanel.SelectedReportItems.Count == 0*/)
                {
                    MessageBox.Show($"未查找到绘制的矩形或当前存在绘制矩形个数：{designPanel.SelectedDrawingShapeList.Count}", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                List<List<DataStruct.Point>> trainCareRegion = new List<List<DataStruct.Point>>();
                List<bool> trainCareMode = new List<bool>();
                designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(drawShapes, rect, out List<DataStruct.Point> ps);
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
                            line.X1 = item.X1 * designPanel.WidthRatio;
                            line.X2 = item.X2 * designPanel.WidthRatio;
                            line.Y1 = item.Y1 * designPanel.HeightRatio;
                            line.Y2 = item.Y2 * designPanel.HeightRatio;
                            polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                            polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                        }
                        polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();

                        trainCareRegion.Add(polygonPoints);
                        trainCareMode.Add(SetInnerMaskMode);
                    }
                });

                designPanel.SelectedDrawingShapeList.Clear();
                designPanel.Clear();
                if (GetDictionary(trainCareRegion, trainCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetTrainCareRegion(valuePairsPoints, out DataStruct.Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show("获取TrainDontCare区域图像异常");
                        return;
                    }
                    errortype = MarkLocationManagerService.GetInstance().SetDetailRegion(valuePairsPoints, out DataStruct.Camera regionImgT);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show("获取TrainDontCare区域图像异常");
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
                if (designPanel == null) return;
                designPanel.Clear();
                designPanel.SelectedDrawingShapeList.Clear();

                Errortype errortype = MarkLocationManagerService.GetInstance().ClearTrainDontCareRegion(out DataStruct.Camera regionImg);
                if (errortype != Errortype.OK)
                {
                    MessageBox.Show("获取TrainDontCare矩形图像异常");
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
        /// 选择矩形
        /// </summary>
        public void SelectRectangle()
        {
            Navigation = "Rectangle Navigation";
            drawShapes = DrawShapes.Rectangle;
            SolideRectangle = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
            SolidePolygon = new SolidColorBrush(System.Windows.Media.Color.FromRgb(122, 122, 122));
        }

        /// <summary>
        /// 选择多段线
        /// </summary>
        public void SelectPolygon()
        {
            Navigation = "Polygon Navigation";
            drawShapes = DrawShapes.Polygon;
            SolideRectangle = new SolidColorBrush(System.Windows.Media.Color.FromRgb(122, 122, 122));
            SolidePolygon = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
        }

        /// <summary>
        /// 设置Detail 区域
        /// </summary>
        public void SetDetail()
        {
            try
            {
                if (designPanel == null) return;
                var shapes = designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| designPanel.SelectedReportItems.Count > 1*/)
                {
                    MessageBox.Show($"未查找到绘制的图形或当前存在绘制图形个数：{designPanel.SelectedDrawingShapeList.Count}", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                List<List<DataStruct.Point>> detailRegion = new List<List<DataStruct.Point>>();
                List<bool> detailMode = new List<bool>();

                designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(drawShapes, rect, out List<DataStruct.Point> ps);
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
                                line.X1 = item.X1 * designPanel.WidthRatio;
                                line.X2 = item.X2 * designPanel.WidthRatio;
                                line.Y1 = item.Y1 * designPanel.HeightRatio;
                                line.Y2 = item.Y2 * designPanel.HeightRatio;
                                polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                                polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                            }
                            polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();
                            detailRegion.Add(polygonPoints);
                            detailMode.Add(SetInnerMaskMode);
                        }
                    }
                });

                designPanel.SelectedDrawingShapeList.Clear();
                designPanel.Clear();
                if (GetDictionary(detailRegion, detailMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetDetailRegion(valuePairsPoints, out DataStruct.Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show("获取DetailDontCare矩形图像异常");
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
                if (designPanel == null) return;
                var shapes = designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| designPanel.SelectedReportItems.Count > 1*/)
                {
                    MessageBox.Show($"未查找到绘制的图形或当前存在绘制图形个数：{designPanel.SelectedDrawingShapeList.Count}", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                List<List<DataStruct.Point>> detailDontCareRegion = new List<List<DataStruct.Point>>();
                List<bool> detailDontCareMode = new List<bool>();

                designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(drawShapes, rect, out List<DataStruct.Point> ps);
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
                                line.X1 = item.X1 * designPanel.WidthRatio;
                                line.X2 = item.X2 * designPanel.WidthRatio;
                                line.Y1 = item.Y1 * designPanel.HeightRatio;
                                line.Y2 = item.Y2 * designPanel.HeightRatio;
                                polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                                polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                            }
                            polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();
                            detailDontCareRegion.Add(polygonPoints);
                            detailDontCareMode.Add(SetInnerMaskMode);
                        }
                    }
                });

                designPanel.SelectedDrawingShapeList.Clear();
                designPanel.Clear();
                if (GetDictionary(detailDontCareRegion, detailDontCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetDetailDontCareRegion(valuePairsPoints, out DataStruct.Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show("获取DetailDontCare矩形图像异常");
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
                if (designPanel == null) return;
                designPanel.Clear();
                designPanel.SelectedDrawingShapeList.Clear();
                Errortype errortype = MarkLocationManagerService.GetInstance().ClearDetailDontCareRegion(out DataStruct.Camera regionImg);
                if (errortype != Errortype.OK)
                {
                    MessageBox.Show("获取DetailDontCare矩形图像异常");
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
            if (designPanel == null || designPanel.Shapes == null) return;
            if (designPanel.Shapes.Count == 8)
            {
                if (designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y - PixelSteps));
            }
        }

        /// <summary>
        /// 设置Origin左
        /// </summary>
        public void SetOriginLeft()
        {
            if (designPanel == null || designPanel.Shapes == null) return;
            if (designPanel.Shapes.Count == 8)
            {
                if (designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X - PixelSteps, _autoOrigin.Y));
            }
        }

        /// <summary>
        /// 设置Origin下
        /// </summary>
        public void SetOriginDown()
        {
            if (designPanel == null || designPanel.Shapes == null) return;
            if (designPanel.Shapes.Count == 8)
            {
                if (designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y + PixelSteps));
            }
        }

        /// <summary>
        /// 设置Origin右
        /// </summary>
        public void SetOriginRight()
        {
            if (designPanel == null || designPanel.Shapes == null) return;
            if (designPanel.Shapes.Count == 8)
            {
                if (designPanel.SelectedDrawingShapeList.TrueForAll(s => s.ItemType == DrawingReportItem.Line))
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X + PixelSteps, _autoOrigin.Y));
            }
        }

        /// <summary>
        /// 绘制圆
        /// </summary>
        public void ShowCircle()
        {
            if (IsDiplayCircle)
            {
                if (_autoOrigin.X == 0 && _autoOrigin.Y == 0)
                {
                    MessageBox.Show("未获取到图像中心点", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                DrawCircleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y), Radius);
            }
            else
            {
                if (designPanel == null || designPanel.SelectedDrawingShapeList == null) return;
                designPanel.SelectedDrawingShapeList.Remove(designPanel.SelectedDrawingShapeList.Find(s => s.ItemType == DrawingReportItem.Circle));
                designPanel.Shapes.Remove(designPanel.Shapes.First(s => s.Region.GetType() == typeof(CircleRegion)));

            }
        }

        /// <summary>
        /// 设置Origin显示十字
        /// </summary>
        public void DisplayCrosshair()
        {
            try
            {

                if (MarkLocationManagerService.GetInstance().GetOrigin(out DataStruct.Point origin) == Errortype.OK)
                {
                    _autoOrigin = origin;
                    DrawRectangleCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y));
                }
                else
                    MaxwellControl.Controls.MessageBox.Show("获取中心点异常");
                // DrawCenterCross(new System.Windows.Point(_autoOrigin.X, _autoOrigin.Y),LineSize) ;
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
                AutoOrginValue = $"X:{_autoOrigin.X} Y:{_autoOrigin.Y}";
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
                var shapes = designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| designPanel.SelectedReportItems.Count > 1*/)
                {
                    MessageBox.Show($"未查找到绘制的图形或当前存在绘制图形个数：{designPanel.SelectedDrawingShapeList.Count}", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                List<List<DataStruct.Point>> detailDontCareRegion = new List<List<DataStruct.Point>>();
                List<bool> detailDontCareMode = new List<bool>();

                designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(drawShapes, rect, out List<DataStruct.Point> ps);
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
                                line.X1 = item.X1 * designPanel.WidthRatio;
                                line.X2 = item.X2 * designPanel.WidthRatio;
                                line.Y1 = item.Y1 * designPanel.HeightRatio;
                                line.Y2 = item.Y2 * designPanel.HeightRatio;
                                polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                                polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                            }
                            polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();

                            detailDontCareRegion.Add(polygonPoints);
                            detailDontCareMode.Add(SetInnerMaskMode);
                        }
                    }
                });

                designPanel.SelectedDrawingShapeList.Clear();
                designPanel.Clear();
                if (GetDictionary(detailDontCareRegion, detailDontCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetDetailDontCareRegion(valuePairsPoints, out DataStruct.Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show("获取DetailDontCare矩形图像异常");
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
                var shapes = designPanel.SelectedDrawingShapeList;
                if (shapes == null /*|| designPanel.SelectedReportItems.Count > 1*/)
                {
                    MessageBox.Show($"未查找到绘制的图形或当前存在绘制图形个数：{designPanel.SelectedDrawingShapeList.Count}", null, "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }
                List<List<DataStruct.Point>> detailCareRegion = new List<List<DataStruct.Point>>();
                List<bool> detailCareMode = new List<bool>();

                designPanel.SelectedDrawingShapeList.ForEach(s =>
                {
                    if (s.ItemType == DrawingReportItem.Rectangle)
                    {
                        Rectangle1 rect = new Rectangle1();
                        rect.Start_X = s.PixLeft;
                        rect.Start_Y = s.PixTop;
                        rect.End_X = s.PixLeft + s.PixWidth;
                        rect.End_Y = s.PixTop + s.PixHeight;
                        GetRect1Corner(drawShapes, rect, out List<DataStruct.Point> ps);
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
                                line.X1 = item.X1 * designPanel.WidthRatio;
                                line.X2 = item.X2 * designPanel.WidthRatio;
                                line.Y1 = item.Y1 * designPanel.HeightRatio;
                                line.Y2 = item.Y2 * designPanel.HeightRatio;
                                polygonPoints.Add(new DataStruct.Point(line.X1, line.Y1));
                                polygonPoints.Add(new DataStruct.Point(line.X2, line.Y2));
                            }
                            polygonPoints = polygonPoints.Where((x, x_index) => x_index == (polygonPoints.FindIndex(y => y == x))).ToList();

                            detailCareRegion.Add(polygonPoints);
                            detailCareMode.Add(SetInnerMaskMode);
                        }
                    }
                });

                designPanel.SelectedDrawingShapeList.Clear();
                designPanel.Clear();
                if (GetDictionary(detailCareRegion, detailCareMode, out Dictionary<List<DataStruct.Point>, bool> valuePairsPoints))
                {
                    Errortype errortype = MarkLocationManagerService.GetInstance().SetDetailRegion(valuePairsPoints, out DataStruct.Camera regionImg);
                    if (errortype != Errortype.OK)
                    {
                        MessageBox.Show("获取DetailDontCare矩形图像异常");
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
                if (designPanel == null)
                {
                    return;
                }
                designPanel.Clear();
                designPanel.SelectedDrawingShapeList.Clear();
                Errortype errortype = MarkLocationManagerService.GetInstance().ClearDetailDontCareRegion(out DataStruct.Camera regionImg);
                if (errortype != Errortype.OK)
                {
                    MessageBox.Show("获取DetailDontCare图像异常");
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
            Errortype ret = MarkLocationManagerService.GetInstance().GetTemplateShowImg(out DataStruct.Camera regionImg);
            if (ret != Errortype.OK)
            {
                MessageBox.Show("获取模板图失败！");
                return;
            }
            ContinuousImage = ImageHelper.Camera2WritableBitmap(regionImg);
            LeftOrRight leftOrRight = LeftOrRight.Left;
            if (pattern == "Left")
                leftOrRight = LeftOrRight.Left;
            else if (pattern == "Right")
                leftOrRight = LeftOrRight.Right;
            string markName = pattern;
            var vm = new DisplayTemplateViewModel(ContinuousImage, markName);
            windowManager.ShowDialog(vm);
        }

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
        public void GetRect1Corner(DrawShapes shapes, DataStruct.Rectangle1 rectangle, out List<DataStruct.Point> polygon)
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
                if (designPanel.Shapes.Count > 0)
                    designPanel.Shapes.Clear();

                //designPanel.ShapeContent = new LineShapeModel();
                //var shapelineH = new PrimShape();
                //shapelineH.Region = new LineRegion();

                //System.Windows.Point centerPoint = new System.Windows.Point(centerPointT.X / designPanel.WidthRatio, centerPointT.Y / designPanel.HeightRatio);
                //System.Windows.Point startPointH = new System.Windows.Point(centerPoint.X - size, centerPoint.Y);
                //System.Windows.Point endPointH = new System.Windows.Point(centerPoint.X + size, centerPoint.Y);
                //designPanel.ShapeContent.SetReportItem(designPanel, shapelineH.Region, startPointH, endPointH);

                //designPanel.Shapes.Add(shapelineH);

                //var shapelineV = new PrimShape();
                //shapelineV.Region = new LineRegion();

                //System.Windows.Point startPointV = new System.Windows.Point(centerPoint.X, centerPoint.Y - size);
                //System.Windows.Point endPointV = new System.Windows.Point(centerPoint.X, centerPoint.Y + size);
                //designPanel.ShapeContent.SetReportItem(designPanel, shapelineV.Region, startPointV, endPointV);
                //designPanel.Shapes.Add(shapelineV);
                designPanel.Items.Add(new LineShapeDrawing() { X = centerPointT.X - size, Y = centerPointT.Y, X1 = centerPointT.X + size, Y1 = centerPointT.Y, Range = 1000 });
                designPanel.Items.Add(new LineShapeDrawing() { X = centerPointT.X, Y = centerPointT.Y - size, X1 = centerPointT.X, Y1 = centerPointT.Y + size });
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
                if (designPanel.Shapes.Count > 0)
                    designPanel.Shapes.Clear();

                //designPanel.ShapeContent = new LineShapeModel();
                double rad = (double)Math.PI * Rotation / 180;
                DataStruct.Rectangle2 rectangleH = new DataStruct.Rectangle2(centerPointT.X / designPanel.WidthRatio, centerPointT.Y / designPanel.HeightRatio, LineSize, LineWidth, rad);
                List<DataStruct.Point> pointsH = rectangleH.GetCornerPoints();
                pointsH.Add(pointsH[0]);

                DataStruct.Rectangle2 rectangleV = new DataStruct.Rectangle2(centerPointT.X / designPanel.WidthRatio, centerPointT.Y / designPanel.HeightRatio, LineSize, LineWidth, rad + Math.PI / 2);
                List<DataStruct.Point> pointsV = rectangleV.GetCornerPoints();
                pointsV.Add(pointsV[0]);

                for (int i = 0; i < pointsH.Count - 1; i++)
                {
                    //var shapeline = new PrimShape();
                    //shapeline.Region = new LineRegion();
                    //System.Windows.Point startPoint = new System.Windows.Point(pointsH[i].X, pointsH[i].Y);
                    //System.Windows.Point endPoint = new System.Windows.Point(pointsH[i + 1].X, pointsH[i + 1].Y);
                    //designPanel.ShapeContent.SetReportItem(designPanel, shapeline.Region, startPoint, endPoint);
                    //designPanel.Shapes.Add(shapeline);
                    //startPoint = new System.Windows.Point(pointsV[i].X, pointsV[i].Y);
                    //endPoint = new System.Windows.Point(pointsV[i + 1].X, pointsV[i + 1].Y);
                    //designPanel.ShapeContent.SetReportItem(designPanel, shapeline.Region, startPoint, endPoint);
                    //designPanel.Shapes.Add(shapeline);
                    designPanel.Items.Add(new LineShapeDrawing() { X = pointsH[i].X, Y = pointsH[i].Y, X1 = pointsH[i + 1].X, Y1 = pointsH[i + 1].Y, Range = 1000 });
                    designPanel.Items.Add(new LineShapeDrawing() { X = pointsV[i].X, Y = pointsV[i + 1].Y, X1 = pointsV[i + 1].X, Y1 = pointsV[i + 1].Y });
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
                if (designPanel == null || designPanel.SelectedDrawingShapeList == null) return;
                designPanel.SelectedDrawingShapeList.Remove(designPanel.SelectedDrawingShapeList.Find(s => s.ItemType == DrawingReportItem.Circle));
                //  designPanel.Shapes.RemoveIfExists(designPanel.Shapes.FirstOrDefault(s => s.Region.GetType() == typeof(CircleRegion)));

                foreach (var item in designPanel.Shapes)
                {
                    if (item.Region.GetType() == typeof(CircleRegion))
                    {
                        designPanel.Shapes.Remove(item);
                    }
                }

                //designPanel.ShapeContent = new CircleShapeModel();
                //var shaperect = new PrimShape();
                //shaperect.Region = new CircleRegion();
                //CircleRegion region = shaperect.Region as CircleRegion;
                //region.CenterPoint = centerPointT;
                //region.Radius = radius;
                ////  designPanel.ShapeContent.SetReportItem(designPanel, shaperect.Region, new System.Windows.Point(), new System.Windows.Point());
                //designPanel.Shapes.Add(shaperect);
                designPanel.Items.Add(new CircleShapeDrawing() { CenterPoint = centerPointT, RadiusX = radius, RadiusY = radius });
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
                if (designPanel.Shapes.Count > 0)
                    designPanel.Shapes.Clear();

                //designPanel.ShapeContent = new RectShapeModel();
                //var shaperectH = new PrimShape();
                //shaperectH.Region = new RectRegion();
                //RectRegion regionH = shaperectH.Region as RectRegion;
                //System.Windows.Point centerPoint = new System.Windows.Point(centerPointT.X / designPanel.WidthRatio, centerPointT.Y / designPanel.HeightRatio);
                //regionH.CenterPoint = centerPoint;
                //regionH.Width = LineSize * designPanel.WidthRatio;
                //regionH.Height = LineWidth * designPanel.HeightRatio;
                //regionH.Angle = Rotation;
                //designPanel.Shapes.Add(shaperectH);

                //var shaperectV = new PrimShape();
                //shaperectV.Region = new RectRegion();
                //RectRegion regionV = shaperectV.Region as RectRegion;
                //regionV.CenterPoint = centerPoint;
                //regionV.Width = LineSize * designPanel.WidthRatio;
                //regionV.Height = LineWidth * designPanel.HeightRatio;
                //regionV.Angle = Rotation + 10;
                //designPanel.Shapes.Add(shaperectV);
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
        private void SelectMode(DataStruct.Rectangle1 rectangleItem, string typeDirection)
        {
            if (designPanel.SelectedDrawingShapeList.Count == 0)
            {
                MessageBox.Show("无可绘制区域", null, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Error);
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
                    //    if (rectangleItem.End_Y >= designPanel.ActualHeight)
                    //        rectangleItem.End_Y = designPanel.ActualHeight;
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
                    //    if (rectangleItem.End_X >= designPanel.ActualWidth)
                    //        rectangleItem.End_X = designPanel.ActualWidth;
                    //    else                       
                    //        rectangleItem.Start_X += Convert.ToDouble(StepWidth);

                    //}
                    break;
                default:
                    break;
            }

            SetRegionX = (designPanel.SelectedDrawingShapeList[0].ItemTop = rectangleItem.Start_Y).ToString();
            SetRegionY = (designPanel.SelectedDrawingShapeList[0].ItemLeft = rectangleItem.Start_X).ToString();
            SetRegionWidth = (designPanel.SelectedDrawingShapeList[0].ItemWidth = rectangleItem.End_X - rectangleItem.Start_X).ToString();
            SetRegionHeight = (designPanel.SelectedDrawingShapeList[0].ItemHeight = rectangleItem.End_Y - rectangleItem.Start_Y).ToString();
        }

        /// <summary>
        /// 上下左右移动多段线
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="typeDirection"></param>
        private void SelectMode(List<Line> lines, string typeDirection)
        {
            if (designPanel.SelectedDrawingShapeList.Count == 0)
            {
                MessageBox.Show("无可绘制区域", null, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Error);
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

                        SetRegionX = (designPanel.SelectedDrawingShapeList[0].ItemTop -= Convert.ToDouble(StepWidth)).ToString();
                        SetRegionY = (designPanel.SelectedDrawingShapeList[0].ItemLeft).ToString();
                    }
                    break;
                case "Down":
                    if (SetRegionModeMove)
                    {
                        if (lines[0].Y1 >= designPanel.ActualHeight)
                            lines[0].Y1 = designPanel.ActualHeight;
                        else
                            for (int i = 0; i < lines.Count; i++)
                            {
                                lines[i].Y1 += Convert.ToDouble(StepWidth);
                            }
                        SetRegionX = (designPanel.SelectedDrawingShapeList[0].ItemTop += Convert.ToDouble(StepWidth)).ToString();
                        SetRegionY = (designPanel.SelectedDrawingShapeList[0].ItemLeft).ToString();
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
                        SetRegionX = (designPanel.SelectedDrawingShapeList[0].ItemTop).ToString();
                        SetRegionY = (designPanel.SelectedDrawingShapeList[0].ItemLeft -= Convert.ToDouble(StepWidth)).ToString();
                    }
                    break;
                case "Right":
                    if (SetRegionModeMove)
                    {
                        if (lines[0].X1 >= designPanel.ActualWidth)
                            lines[0].X1 = designPanel.ActualWidth;
                        else
                            for (int i = 0; i < lines.Count; i++)
                            {
                                lines[i].X1 += Convert.ToDouble(StepWidth);
                            }
                        SetRegionX = (designPanel.SelectedDrawingShapeList[0].ItemTop).ToString();
                        SetRegionY = (designPanel.SelectedDrawingShapeList[0].ItemLeft += Convert.ToDouble(StepWidth)).ToString();
                    }
                    break;
                default:
                    break;
            }

            SetRegionWidth = (designPanel.SelectedDrawingShapeList[0].ItemWidth).ToString();
            SetRegionHeight = (designPanel.SelectedDrawingShapeList[0].ItemHeight).ToString();
        }

        /// <summary>
        /// 获取控件上绘制矩形的Rectangle
        /// </summary>
        /// <returns></returns>
        private DataStruct.Rectangle1 GetRegionItemRectangle()
        {
            Rectangle1 rectangle = new Rectangle1();
            if (designPanel == null) return null;
            if (designPanel.SelectedDrawingShapeList.Count > 0)
            {
                foreach (IReportItemControl item in designPanel.SelectedDrawingShapeList)
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
        private DataStruct.Rectangle1 GetRegionPixRectangle()
        {
            Rectangle1 rectangle = new Rectangle1();
            if (designPanel == null) return null;
            if (designPanel.SelectedDrawingShapeList.Count > 0)
            {
                foreach (IReportItemControl item in designPanel.SelectedDrawingShapeList)
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

        [Obsolete]
        private Polygon GetRegionPixPolygon()
        {
            Polygon polygon = new Polygon();
            if (designPanel == null) return null;
            if (designPanel.SelectedDrawingShapeList.Count > 0 && designPanel.SelectedReportItemType == DrawingReportItem.Polygon)
            {
                List<PointCollection> lstPoints = designPanel.GetPolygonPoints();
                PointCollection pointsCollection = new PointCollection();
                System.Windows.Point point = new System.Windows.Point();
                for (int i = 0; i < lstPoints.Count; i++)
                {
                    point.X = lstPoints[i][i].X * designPanel.WidthRatio;
                    point.Y = lstPoints[i][i].Y * designPanel.HeightRatio;
                    pointsCollection.Add(point);
                }
                polygon.Points = pointsCollection;
                return polygon;
            }
            return polygon;
        }
        /// <summary>
        /// 获取多段线点位
        /// </summary>
        /// <returns></returns>
        private List<Line> GetRegionItemPolygon()
        {
            List<Line> polygonPoint = new List<Line>();
            List<DataStruct.Point> points = new List<DataStruct.Point>();
            if (designPanel == null) return null;
            if (designPanel.SelectedDrawingShapeList.Count > 0)
            {
                foreach (var item in designPanel.SelectedDrawingShapeList)
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
                        points.Add(new DataStruct.Point(line.X1, line.Y1));
                        points.Add(new DataStruct.Point(line.X2, line.Y2));
                    }
                    points = points.Where((x, x_index) => x_index == (points.FindIndex(y => y == x))).ToList();

                }

                return polygonPoint;
            }
            return polygonPoint;
        }
        List<DataStruct.Camera> cameras = null;
        /// <summary>
        /// 搜索
        /// </summary>
        public void Search()
        {
            ProgressValue = 0;
            if (ContinuousImage == null || !isLoad || writeableBitmaps.Count == 0)
            {
                MaxwellControl.Controls.MessageBox.Show("未加载图片进行模板匹配，请重新加载图像后进行操作...");
                return;
            }
            cameras = new List<DataStruct.Camera>();
            for (int j = 0; j < writeableBitmaps.Count; j++)
            {

                DataStruct.Camera camera = ImageHelper.GetCamera(writeableBitmaps[j]);
                cameras.Add(camera);
            }
            Thread thread = new Thread(Run);
            thread.Start();
        }

        private void Run()
        {
            try
            {
                LeftOrRight leftOrRight = LeftOrRight.Left;
                if (pattern == "Left")
                    leftOrRight = LeftOrRight.Left;
                else if (pattern == "Right")
                    leftOrRight = LeftOrRight.Right;
                string markName = pattern;
                if (markName == string.Empty)
                {
                    MaxwellControl.Controls.MessageBox.Show($"当前库中{markName}模板不存在！");
                    return;
                }
                if (MarkLocationManagerService.GetInstance().CreateNccPattern(markName) == Errortype.OK)
                {
                    designPanel.SelectedDrawingShapeList.Clear();
                    if (Repeatings == 0 || InterVal == 0)
                    {
                        MaxwellControl.Controls.MessageBox.Show("Repeatings参数为0;InterVal参数为0，请重新设置和操作...");
                        return;
                    }
                    if (Repeatings > writeableBitmaps.Count)
                    {
                        MaxwellControl.Controls.MessageBox.Show($"Repeatings设置参数大于选中图片个数{writeableBitmaps.Count};，请重新设置和操作...");
                        return;
                    }
                    List<double> rows = new List<double>();
                    List<double> cols = new List<double>();
                    List<double> angle = new List<double>();
                    List<double> score = new List<double>();


                    for (int i = 0; i < Repeatings; i++)
                    {

                        ProgressValue = (Convert.ToDouble(i) / Convert.ToDouble(Repeatings)) * 80;

                        Thread.Sleep(InterVal);
                        if (!MatchImg(cameras[i], markName, null, out List<Tuple<double, double, double, double>> matchResult))
                        {
                            MaxwellControl.Controls.MessageBox.Show($"第{i}张图像异常！！！");
                            ContinuousImage = ImageHelper.Camera2WritableBitmap(cameras[i]);
                            return;
                        }
                        if (matchResult.Count > 1 || matchResult.Count == 0)
                        {
                            MaxwellControl.Controls.MessageBox.Show($"找到Mark个数为{matchResult.Count}");
                            for (int j = 0; j < matchResult.Count; j++)
                            {
                                DrawRectangleCenterCross(new System.Windows.Point(matchResult[j].Item1, matchResult[j].Item2));
                            }
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

                    if (MarkLocationManagerService.GetInstance().GetDataSummary(keyValuePairs, out Dictionary<string, DataStatisticParam> summaryResult) == Errortype.OK)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            if (MatchParameterList.Count >= 0)
                            {
                                MatchParameterList.Clear();
                                MatchParameterList.Add(new MatchParameter("Average", summaryResult["x"].Mean, summaryResult["y"].Mean, summaryResult["angle"].Mean, summaryResult["score"].Mean));
                                MatchParameterList.Add(new MatchParameter("Range", summaryResult["x"].Mean, summaryResult["y"].Range, summaryResult["angle"].Range, summaryResult["score"].Range));
                                MatchParameterList.Add(new MatchParameter("Sigma3", summaryResult["x"].Sigma3, summaryResult["y"].Sigma3, summaryResult["angle"].Sigma3, summaryResult["score"].Sigma3));
                                MatchParameterList.Add(new MatchParameter("Min", summaryResult["x"].Min, summaryResult["y"].Min, summaryResult["angle"].Min, summaryResult["score"].Min));
                                MatchParameterList.Add(new MatchParameter("Max", summaryResult["x"].Max, summaryResult["y"].Max, summaryResult["angle"].Max, summaryResult["score"].Max));
                            }
                        }));
                    }
                    ProgressValue = 100;
                }
                else
                    MaxwellControl.Controls.MessageBox.Show("Pattern None");
            }
            catch (Exception ex)
            {
                MaxwellControl.Controls.MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// 对单个模板图像进行匹配
        /// </summary>
        /// <param name="img"></param>
        /// <param name="markName"></param>
        /// <param name="matchRegion"></param>
        /// <param name="matchResult"></param>
        private bool MatchImg(DataStruct.Camera img, string markName, DataStruct.Rectangle1 matchRegion, out List<Tuple<double, double, double, double>> matchResult)
        {
            matchResult = new List<Tuple<double, double, double, double>>();
            DataStruct.Errortype ret = MarkLocationManagerService.GetInstance().GetMarkCenter(markName, img, matchRegion,
                out double[] rows, out double[] cols, out double[] angles, out double[] scores, /*out List<DataStruct.Point> inters,*/ out List<List<double[]>> straightnessErrorList);

            if (DataStruct.Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("获取算法参数异常！！！");
                return false;
            }
            for (int i = 0; i < rows.Length; i++)
            {
                matchResult.Add(new Tuple<double, double, double, double>(cols[i], rows[i], angles[i], scores[i]));
            }
            return true;
        }

        /// <summary>
        /// 设置参数
        /// </summary>
        public void SaveSetUpParam()
        {
            MarkLocationManagerService.GetInstance().MinLength = MinLength;
            MarkLocationManagerService.GetInstance().CaliperParams.MeasureThreshold = EdgeThreshold;
            MarkLocationManagerService.GetInstance().CaliperParams.MeasureLength1 = MeasureLength1;
            MarkLocationManagerService.GetInstance().NccMatchParams.MinScore = ScoreThreshold;
            MarkLocationManagerService.GetInstance().ShapeMatchParams.MinScore = ScoreThreshold;

            switch (Metric)
            {
                case "Use":
                    MarkLocationManagerService.GetInstance().NccTemplateParams.Metric = "use_polarity";
                    MarkLocationManagerService.GetInstance().ShapeTemplateParams.Metric = "use_polarity";
                    break;
                case "IgnoreGlobal":
                    MarkLocationManagerService.GetInstance().NccTemplateParams.Metric = "ignore_global_polarity";
                    MarkLocationManagerService.GetInstance().ShapeTemplateParams.Metric = "ignore_global_polarity";
                    break;
                case "IgnoreLocal":
                    MarkLocationManagerService.GetInstance().NccTemplateParams.Metric = "ignore_global_polarity";
                    MarkLocationManagerService.GetInstance().ShapeTemplateParams.Metric = "ignore_local_polarity";
                    break;
                default:
                    MarkLocationManagerService.GetInstance().NccTemplateParams.Metric = "use_polarity";
                    MarkLocationManagerService.GetInstance().ShapeTemplateParams.Metric = "use_polarity";
                    break;
            }

            MarkLocationManagerService.GetInstance().TemplateType = (DataStruct.TemplateType)Enum.Parse(typeof(DataStruct.TemplateType), TemplateType);

            MarkLocationManagerService.GetInstance().UseLinePolarity = bool.Parse(LinePolarity);
        }

        /// <summary>
        /// 相机拍照(Test)
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
                    List<string> ImportFiles = openFileDialog.FileNames.ToList();
                    if (ImportFiles.Count == 0 || ImportFiles == null)
                    {
                        MaxwellControl.Controls.MessageBox.Show("未选择图片文件，或选择文件不存在！请重新操作");
                        return;
                    }
                    writeableBitmaps = new List<WriteableBitmap>();
                    ImportFiles.ForEach(f =>
                    {
                        Load(f);
                    });
                    MaxwellControl.Controls.MessageBox.Show("图片加载完成！");
                    isLoad = true;
                }
                catch (Exception)
                {
                    MaxwellControl.Controls.MessageBox.Show("格式不正确！");
                }
            }
            ProgressValue = 0;

        }
        List<WriteableBitmap> writeableBitmaps = null;

        private void Load(string imageName)
        {
            Bitmap bitmap = (Bitmap)System.Drawing.Image.FromFile(imageName);
            BitmapData bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadWrite, bitmap.PixelFormat);
            int num = bitmapData.Stride * bitmapData.Height;
            byte[] array = new byte[num];
            Marshal.Copy(bitmapData.Scan0, array, 0, num);
            bitmap.UnlockBits(bitmapData);
            int width = bitmap.Width;
            int height = bitmap.Height;
            int bitCnt = ((bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format8bppIndexed) ? 1 : ((bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb) ? 3 : 4));
            bitmap.Dispose();
            UpdateWritableBitmap(array, width, height, bitCnt);
            writeableBitmaps.Add(ContinuousImage);
        }

        public void TemplateTypeChanged()
        {

        }

        private void TemplateTypeChangeMetric()
        {
            if (TemplateType == "NCC")
            {
                if (Metric == "IgnoreLocal")
                {
                    Metric = "IgnoreGlobal";
                }

                _thisView.MetricCbx.ItemsSource = new List<string>
                {
                    "Use",
                    "IgnoreGlobal",
                };
            }
            else
            {
                _thisView.MetricCbx.ItemsSource = new List<string>
                {
                    "Use",
                    "IgnoreGlobal",
                    "IgnoreLocal",
                };
            }

        }

    }
}
