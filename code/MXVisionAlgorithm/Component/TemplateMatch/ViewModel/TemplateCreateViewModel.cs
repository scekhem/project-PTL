using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DataStruct;
using MaxwellFramework.Core.Interfaces;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using MwFramework.Controls.ControlCanvas.Model;
using MX10UBDBU01AA.Common;
using MXVisionAlgorithm.Common;
using MXVisionAlgorithm.Component.MarkLocation.View;
using MXVisionAlgorithm.Component.TemplateMatch.View;
using Stylet;
using UltrapreciseBonding.TemplateMatch;

namespace MXVisionAlgorithm.Component.TemplateMatch.ViewModel
{
    public class TemplateCreateViewModel : Screen, IPage
    {
        public string Name { get; set; } = "TemplateCreateView";

        public string TemplateName { get; set; }

        private TemplateType TemplateType { get; set; }

        private NccTemplateParams NccTemplateParams { get; set; } = new NccTemplateParams();

        private NccMatchParams NccMatchParams { get; set; } = new NccMatchParams();

        private ShapeTemplateParams ShapeTemplateParams { get; set; } = new ShapeTemplateParams();

        private ShapeMatchParams ShapeMatchParams { get; set; } = new ShapeMatchParams();

        #region Expander

        private bool _nccCreateParamExpander;
        public bool NccCreateParamExpander
        {
            get => _nccCreateParamExpander;
            set
            {
                _nccCreateParamExpander = value;
                if (_nccCreateParamExpander)
                {
                    NccMatchParamExpander = false;
                    ShapeCreateParamExpander = false;
                    ShapeMatchParamExpander = false;
                }
                OnPropertyChanged(nameof(NccCreateParamExpander));
            }
        }

        private string _nccCreateParamExpanderVisibility;
        public string NccCreateParamExpanderVisibility
        {
            get => _nccCreateParamExpanderVisibility;
            set
            {
                _nccCreateParamExpanderVisibility = value;
                OnPropertyChanged(nameof(NccCreateParamExpanderVisibility));
            }
        }

        private bool _shapeCreateParamExpander;
        public bool ShapeCreateParamExpander
        {
            get => _shapeCreateParamExpander;
            set
            {
                _shapeCreateParamExpander = value;
                if (_shapeCreateParamExpander)
                {
                    NccCreateParamExpander = false;
                    NccMatchParamExpander = false;
                    ShapeMatchParamExpander = false;
                }
                OnPropertyChanged(nameof(ShapeCreateParamExpander));
            }
        }

        private string _shapeCreateParamExpanderVisibility;
        public string ShapeCreateParamExpanderVisibility
        {
            get => _shapeCreateParamExpanderVisibility;
            set
            {
                _shapeCreateParamExpanderVisibility = value;
                OnPropertyChanged(nameof(ShapeCreateParamExpanderVisibility));
            }
        }

        private bool _nccMatchParamExpander;
        public bool NccMatchParamExpander
        {
            get => _nccMatchParamExpander;
            set
            {
                _nccMatchParamExpander = value;
                if (_nccMatchParamExpander)
                {
                    NccCreateParamExpander = false;
                    ShapeCreateParamExpander = false;
                    ShapeMatchParamExpander = false;
                }
                OnPropertyChanged(nameof(NccMatchParamExpander));
            }
        }

        private string _nccMatchParamExpanderVisibility;
        public string NccMatchParamExpanderVisibility
        {
            get => _nccMatchParamExpanderVisibility;
            set
            {
                _nccMatchParamExpanderVisibility = value;
                OnPropertyChanged(nameof(NccMatchParamExpanderVisibility));
            }
        }

        private bool _shapeMatchParamExpander;
        public bool ShapeMatchParamExpander
        {
            get => _shapeMatchParamExpander;
            set
            {
                _shapeMatchParamExpander = value;
                if (_shapeMatchParamExpander)
                {
                    NccCreateParamExpander = false;
                    NccMatchParamExpander = false;
                    ShapeCreateParamExpander = false;
                }
                OnPropertyChanged(nameof(ShapeMatchParamExpander));
            }
        }

        private string _shapeMatchParamExpanderVisibility;
        public string ShapeMatchParamExpanderVisibility
        {
            get => _shapeMatchParamExpanderVisibility;
            set
            {
                _shapeMatchParamExpanderVisibility = value;
                OnPropertyChanged(nameof(ShapeMatchParamExpanderVisibility));
            }
        }
        #endregion

        #region NccCreateParam

        private int _nccCreatePyramid;
        public int NccCreatePyramid
        {
            get => _nccCreatePyramid;
            set { _nccCreatePyramid = value; OnPropertyChanged(nameof(NccCreatePyramid)); }
        }

        private double _nccCreateAngleStart;
        public double NccCreateAngleStart
        {
            get => _nccCreateAngleStart;
            set { _nccCreateAngleStart = value; OnPropertyChanged(nameof(NccCreateAngleStart)); }
        }

        private double _nccCreateAngleExtent;
        public double NccCreateAngleExtent
        {
            get => _nccCreateAngleExtent;
            set { _nccCreateAngleExtent = value; OnPropertyChanged(nameof(NccCreateAngleExtent)); }
        }

        private string _nccCreateAngleStep;
        public string NccCreateAngleStep
        {
            get => _nccCreateAngleStep;
            set { _nccCreateAngleStep = value; OnPropertyChanged(nameof(NccCreateAngleStep)); }
        }

        private string _nccCreateMetric;
        public string NccCreateMetric
        {
            get => _nccCreateMetric;
            set { _nccCreateMetric = value; OnPropertyChanged(nameof(NccCreateMetric)); }
        }

        private string _nccCreateSmoothImg;
        public string NccCreateSmoothImg
        {
            get => _nccCreateSmoothImg;
            set { _nccCreateSmoothImg = value; OnPropertyChanged(nameof(NccCreateSmoothImg)); }
        }

        private int _nccCreateFilterSize;
        public int NccCreateFilterSize
        {
            get => _nccCreateFilterSize;
            set { _nccCreateFilterSize = value; OnPropertyChanged(nameof(NccCreateFilterSize)); }
        }
        #endregion

        #region ShapeCreateParam

        private int _shapeCreatePyramid;
        public int ShapeCreatePyramid
        {
            get => _shapeCreatePyramid;
            set { _shapeCreatePyramid = value; OnPropertyChanged(nameof(ShapeCreatePyramid)); }
        }

        private double _shapeCreateAngleStart;
        public double ShapeCreateAngleStart
        {
            get => _shapeCreateAngleStart;
            set { _shapeCreateAngleStart = value; OnPropertyChanged(nameof(ShapeCreateAngleStart)); }
        }

        private double _shapeCreateAngleExtent;
        public double ShapeCreateAngleExtent
        {
            get => _shapeCreateAngleExtent;
            set { _shapeCreateAngleExtent = value; OnPropertyChanged(nameof(ShapeCreateAngleExtent)); }
        }

        private string _shapeCreateAngleStep;
        public string ShapeCreateAngleStep
        {
            get => _shapeCreateAngleStep;
            set { _shapeCreateAngleStep = value; OnPropertyChanged(nameof(ShapeCreateAngleStep)); }
        }

        private double _shapeCreateScaleMin;
        public double ShapeCreateScaleMin
        {
            get => _shapeCreateScaleMin;
            set { _shapeCreateScaleMin = value; OnPropertyChanged(nameof(ShapeCreateScaleMin)); }
        }

        private double _shapeCreateScaleMax;
        public double ShapeCreateScaleMax
        {
            get => _shapeCreateScaleMax;
            set { _shapeCreateScaleMax = value; OnPropertyChanged(nameof(ShapeCreateScaleMax)); }
        }

        private string _shapeCreateScaleStep;
        public string ShapeCreateScaleStep
        {
            get => _shapeCreateScaleStep;
            set { _shapeCreateScaleStep = value; OnPropertyChanged(nameof(ShapeCreateScaleStep)); }
        }

        private string _shapeCreateOptimization;
        public string ShapeCreateOptimization
        {
            get => _shapeCreateOptimization;
            set { _shapeCreateOptimization = value; OnPropertyChanged(nameof(ShapeCreateOptimization)); }
        }

        private string _shapeCreateMetric;
        public string ShapeCreateMetric
        {
            get => _shapeCreateMetric;
            set { _shapeCreateMetric = value; OnPropertyChanged(nameof(ShapeCreateMetric)); }
        }

        private string _shapeCreateContrast;
        public string ShapeCreateContrast
        {
            get => _shapeCreateContrast;
            set { _shapeCreateContrast = value; OnPropertyChanged(nameof(ShapeCreateContrast)); }
        }

        private string _shapeCreateMinContrast;
        public string ShapeCreateMinContrast
        {
            get => _shapeCreateMinContrast;
            set { _shapeCreateMinContrast = value; OnPropertyChanged(nameof(ShapeCreateMinContrast)); }
        }

        private string _shapeCreateSmoothImg;
        public string ShapeCreateSmoothImg
        {
            get => _shapeCreateSmoothImg;
            set { _shapeCreateSmoothImg = value; OnPropertyChanged(nameof(ShapeCreateSmoothImg)); }
        }

        private int _shapeCreateFilterSize;
        public int ShapeCreateFilterSize
        {
            get => _shapeCreateFilterSize;
            set { _shapeCreateFilterSize = value; OnPropertyChanged(nameof(ShapeCreateFilterSize)); }
        }

        #endregion

        #region NccMatchParam


        private double _nccMatchAngleStart;
        public double NccMatchAngleStart
        {
            get => _nccMatchAngleStart;
            set { _nccMatchAngleStart = value; OnPropertyChanged(nameof(NccMatchAngleStart)); }
        }

        private double _nccMatchAngleExtent;
        public double NccMatchAngleExtent
        {
            get => _nccMatchAngleExtent;
            set { _nccMatchAngleExtent = value; OnPropertyChanged(nameof(NccMatchAngleExtent)); }
        }

        private double _nccMatchMinScore;
        public double NccMatchMinScore
        {
            get => _nccMatchMinScore;
            set { _nccMatchMinScore = value; OnPropertyChanged(nameof(NccMatchMinScore)); }
        }

        private int _nccMatchNumMatches;
        public int NccMatchNumMatches
        {
            get => _nccMatchNumMatches;
            set { _nccMatchNumMatches = value; OnPropertyChanged(nameof(NccMatchNumMatches)); }
        }

        private double _nccMatchMaxOverlap;
        public double NccMatchMaxOverlap
        {
            get => _nccMatchMaxOverlap;
            set { _nccMatchMaxOverlap = value; OnPropertyChanged(nameof(NccMatchMaxOverlap)); }
        }

        private string _nccMatchSubPixel;
        public string NccMatchSubPixel
        {
            get => _nccMatchSubPixel;
            set { _nccMatchSubPixel = value; OnPropertyChanged(nameof(NccMatchSubPixel)); }
        }

        private int _nccMatchPyramid;
        public int NccMatchPyramid
        {
            get => _nccMatchPyramid;
            set { _nccMatchPyramid = value; OnPropertyChanged(nameof(NccMatchPyramid)); }
        }

        private string _nccMatchSmoothImg;
        public string NccMatchSmoothImg
        {
            get => _nccMatchSmoothImg;
            set { _nccMatchSmoothImg = value; OnPropertyChanged(nameof(NccMatchSmoothImg)); }
        }

        private int _nccMatchFilterSize;
        public int NccMatchFilterSize
        {
            get => _nccMatchFilterSize;
            set { _nccMatchFilterSize = value; OnPropertyChanged(nameof(NccMatchFilterSize)); }
        }

        #endregion

        #region ShapeMatchParam

        private double _shapeMatchAngleStart;
        public double ShapeMatchAngleStart
        {
            get => _shapeMatchAngleStart;
            set { _shapeMatchAngleStart = value; OnPropertyChanged(nameof(ShapeMatchAngleStart)); }
        }

        private double _shapeMatchAngleExtent;
        public double ShapeMatchAngleExtent
        {
            get => _shapeMatchAngleExtent;
            set { _shapeMatchAngleExtent = value; OnPropertyChanged(nameof(ShapeMatchAngleExtent)); }
        }

        private double _shapeMatchScaleMin;
        public double ShapeMatchScaleMin
        {
            get => _shapeMatchScaleMin;
            set { _shapeMatchScaleMin = value; OnPropertyChanged(nameof(ShapeMatchScaleMin)); }
        }

        private double _shapeMatchScaleMax;
        public double ShapeMatchScaleMax
        {
            get => _shapeMatchScaleMax;
            set { _shapeMatchScaleMax = value; OnPropertyChanged(nameof(ShapeMatchScaleMax)); }
        }

        private double _shapeMatchMinScore;
        public double ShapeMatchMinScore
        {
            get => _shapeMatchMinScore;
            set { _shapeMatchMinScore = value; OnPropertyChanged(nameof(ShapeMatchMinScore)); }
        }

        private int _shapeMatchNumMatches;
        public int ShapeMatchNumMatches
        {
            get => _shapeMatchNumMatches;
            set { _shapeMatchNumMatches = value; OnPropertyChanged(nameof(ShapeMatchNumMatches)); }
        }

        private double _shapeMatchMaxOverlap;
        public double ShapeMatchMaxOverlap
        {
            get => _shapeMatchMaxOverlap;
            set { _shapeMatchMaxOverlap = value; OnPropertyChanged(nameof(ShapeMatchMaxOverlap)); }
        }

        private string _shapeMatchSubPixel;
        public string ShapeMatchSubPixel
        {
            get => _shapeMatchSubPixel;
            set { _shapeMatchSubPixel = value; OnPropertyChanged(nameof(ShapeMatchSubPixel)); }
        }

        private int _shapeMatchPyramid;
        public int ShapeMatchPyramid
        {
            get => _shapeMatchPyramid;
            set { _shapeMatchPyramid = value; OnPropertyChanged(nameof(ShapeMatchPyramid)); }
        }

        private double _shapeMatchGreediness;
        public double ShapeMatchGreediness
        {
            get => _shapeMatchGreediness;
            set { _shapeMatchGreediness = value; OnPropertyChanged(nameof(ShapeMatchGreediness)); }
        }

        private string _shapeMatchSmoothImg;
        public string ShapeMatchSmoothImg
        {
            get => _shapeMatchSmoothImg;
            set { _shapeMatchSmoothImg = value; OnPropertyChanged(nameof(ShapeMatchSmoothImg)); }
        }

        private int _shapeMatchFilterSize;
        public int ShapeMatchFilterSize
        {
            get => _shapeMatchFilterSize;
            set { _shapeMatchFilterSize = value; OnPropertyChanged(nameof(ShapeMatchFilterSize)); }
        }

        #endregion

        private TemplateCreateView _thisView;


        /// <summary>
        /// 窗口图像
        /// </summary>
        public WriteableBitmap _image;
        public WriteableBitmap Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        public TemplateCreateViewModel()
        {

        }


        private int _nccCreateAngleStepIndex;
        public int NccCreateAngleStepIndex
        {
            get => _nccCreateAngleStepIndex;
            set { _nccCreateAngleStepIndex = value; OnPropertyChanged(nameof(NccCreateAngleStepIndex)); }
        }

        private void GetNccParam()
        {
            NccCreatePyramid = NccTemplateParams.Pyramid;
            NccCreateAngleStart = NccTemplateParams.AngleStart;
            NccCreateAngleExtent = NccTemplateParams.AngleExtent;
            NccCreateAngleStep = NccTemplateParams.AngleStep;
            NccCreateMetric = NccTemplateParams.Metric;
            NccCreateSmoothImg = NccTemplateParams.SmoothImg.ToString().ToLower();
            NccCreateFilterSize = (int)NccTemplateParams.FilterSize;

            NccMatchAngleStart = NccMatchParams.AngleStart;
            NccMatchAngleExtent = NccMatchParams.AngleExtent;
            NccMatchMinScore = NccMatchParams.MinScore;
            NccMatchNumMatches = NccMatchParams.NumMatches;
            NccMatchMaxOverlap = NccMatchParams.MaxOverlap;
            NccMatchSubPixel = NccMatchParams.SubPixel.ToString().ToLower();
            NccMatchPyramid = NccMatchParams.Pyramid;
            NccMatchSmoothImg = NccMatchParams.SmoothImg.ToString().ToLower();
            NccMatchFilterSize = (int)NccMatchParams.FilterSize;
        }

        private void SetNccParam()
        {
            NccTemplateParams.Pyramid = NccCreatePyramid;
            NccTemplateParams.AngleStart = NccCreateAngleStart;
            NccTemplateParams.AngleExtent = NccCreateAngleExtent;
            NccTemplateParams.AngleStep = NccCreateAngleStep.ToString();
            NccTemplateParams.Metric = NccCreateMetric.ToString();
            NccTemplateParams.SmoothImg = NccCreateSmoothImg == "true" ? true : false;
            NccTemplateParams.FilterSize = NccCreateFilterSize;

            NccMatchParams.AngleStart = NccMatchAngleStart;
            NccMatchParams.AngleExtent = NccMatchAngleExtent;
            NccMatchParams.MinScore = NccMatchMinScore;
            NccMatchParams.NumMatches = NccMatchNumMatches;
            NccMatchParams.MaxOverlap = NccMatchMaxOverlap;
            NccMatchParams.SubPixel = NccMatchSubPixel == "true" ? true : false;
            NccMatchParams.Pyramid = NccMatchPyramid;
            NccMatchParams.SmoothImg = NccMatchSmoothImg == "true" ? true : false;
            NccMatchParams.FilterSize = NccMatchFilterSize;
        }

        private void GetShapeParam()
        {
            ShapeCreatePyramid = ShapeTemplateParams.Pyramid;
            ShapeCreateAngleStart = ShapeTemplateParams.AngleStart;
            ShapeCreateAngleExtent = ShapeTemplateParams.AngleExtent;
            ShapeCreateAngleStep = ShapeTemplateParams.AngleStep;
            ShapeCreateScaleMin = ShapeTemplateParams.ScaleMin;
            ShapeCreateScaleMax = ShapeTemplateParams.ScaleMax;
            ShapeCreateScaleStep = ShapeTemplateParams.ScaleStep;
            ShapeCreateOptimization = ShapeTemplateParams.Optimization;
            ShapeCreateMetric = ShapeTemplateParams.Metric;
            ShapeCreateContrast = ShapeTemplateParams.Contrast;
            ShapeCreateMinContrast = ShapeTemplateParams.MinContrast;
            ShapeCreateSmoothImg = ShapeTemplateParams.SmoothImg.ToString().ToLower();
            ShapeCreateFilterSize = (int)ShapeTemplateParams.FilterSize;

            ShapeMatchAngleStart = ShapeMatchParams.AngleStart;
            ShapeMatchAngleExtent = ShapeMatchParams.AngleExtent;
            ShapeMatchScaleMin = ShapeMatchParams.ScaleMin;
            ShapeMatchScaleMax = ShapeMatchParams.ScaleMax;
            ShapeMatchMinScore = ShapeMatchParams.MinScore;
            ShapeMatchNumMatches = ShapeMatchParams.NumMatches;
            ShapeMatchMaxOverlap = ShapeMatchParams.MaxOverlap;
            ShapeMatchSubPixel = ShapeMatchParams.SubPixel;
            ShapeMatchPyramid = ShapeMatchParams.Pyramid;
            ShapeMatchGreediness = ShapeMatchParams.Greediness;
            ShapeMatchSmoothImg = ShapeMatchParams.SmoothImg.ToString().ToLower();
            ShapeMatchFilterSize = (int)ShapeMatchParams.FilterSize;

        }

        private void SetShapeParam()
        {
            ShapeTemplateParams.Pyramid = ShapeCreatePyramid;
            ShapeTemplateParams.AngleStart = ShapeCreateAngleStart;
            ShapeTemplateParams.AngleExtent = ShapeCreateAngleExtent;
            ShapeTemplateParams.AngleStep = ShapeCreateAngleStep;
            ShapeTemplateParams.ScaleMin = ShapeCreateScaleMin;
            ShapeTemplateParams.ScaleMax = ShapeCreateScaleMax;
            ShapeTemplateParams.ScaleStep = ShapeCreateScaleStep;
            ShapeTemplateParams.Optimization = ShapeCreateOptimization;
            ShapeTemplateParams.Metric = ShapeCreateMetric;
            ShapeTemplateParams.Contrast = ShapeCreateContrast;
            ShapeTemplateParams.MinContrast = ShapeCreateMinContrast;
            ShapeTemplateParams.SmoothImg = ShapeCreateSmoothImg == "true" ? true : false;
            ShapeTemplateParams.FilterSize = ShapeCreateFilterSize;

            ShapeMatchParams.AngleStart = ShapeMatchAngleStart;
            ShapeMatchParams.AngleExtent = ShapeMatchAngleExtent;
            ShapeMatchParams.ScaleMin = ShapeMatchScaleMin;
            ShapeMatchParams.ScaleMax = ShapeMatchScaleMax;
            ShapeMatchParams.MinScore = ShapeMatchMinScore;
            ShapeMatchParams.NumMatches = ShapeMatchNumMatches;
            ShapeMatchParams.MaxOverlap = ShapeMatchMaxOverlap;
            ShapeMatchParams.SubPixel = ShapeMatchSubPixel;
            ShapeMatchParams.Pyramid = ShapeMatchPyramid;
            ShapeMatchParams.Greediness = ShapeMatchGreediness;
            ShapeMatchParams.SmoothImg = ShapeMatchSmoothImg == "true" ? true : false;
            ShapeMatchParams.FilterSize = ShapeMatchFilterSize;

        }

        /// <summary>
        /// 初始化ComboBox的选项
        /// </summary>
        private void InitComboBoxItem()
        {
            _thisView.NccCreateAngleStepCbx.ItemsSource = new List<string>() { "auto", "0.0349", "0.0524", "0.0698", "0.0873" };
            _thisView.NccCreateMetricCbx.ItemsSource = new List<string>() { "ignore_global_polarity", "use_polarity" };
            _thisView.NccCreateSmoothImgCbx.ItemsSource = new List<string>() { "true", "false" };

            _thisView.ShapeCreateAngleStepCbx.ItemsSource = new List<string>() { "auto", "0.0349", "0.0524", "0.0698", "0.0873" };
            _thisView.ShapeCreateScaleStepCbx.ItemsSource = new List<string>() { "auto", "0.01", "0.02", "0.05", "0.1", "0.15", "0.2" };
            _thisView.ShapeCreateOptimizationCbx.ItemsSource = new List<string>() { "auto", "no_pregeneration", "none", "point_reduction_high", "point_reduction_low", "point_reduction_medium", "pregeneration" };
            _thisView.ShapeCreateMetricCbx.ItemsSource = new List<string>() { "ignore_global_polarity", "use_polarity" };
            _thisView.ShapeCreateContrastCbx.ItemsSource = new List<string>() { "auto", "auto_contrast", "auto_contrast_hyst", "auto_min_size", "10", "20", "30", "40", "60", "80", "100", "120", "140", "160" };
            _thisView.ShapeCreateMinContrastCbx.ItemsSource = new List<string>() { "auto", "1", "2", "3", "5", "7", "10", "20", "30", "40" };
            _thisView.ShapeCreateSmoothImgCbx.ItemsSource = new List<string>() { "true", "false" };

            _thisView.NccMatchSubPixelCbx.ItemsSource = new List<string>() { "true", "false" };
            _thisView.NccMatchSmoothImgCbx.ItemsSource = new List<string>() { "true", "false" };

            _thisView.ShapeMatchSubPixelCbx.ItemsSource = new List<string>() { "none", "interpolation", "least_squares", "least_squares_high", "least_squares_very_high" };
            _thisView.ShapeMatchSmoothImgCbx.ItemsSource = new List<string>() { "true", "false" };

        }

        private void Init()
        {
            _thisView = this.View as TemplateCreateView;

            InitComboBoxItem();
            switch (TemplateType)
            {
                case TemplateType.NCC:
                    ShapeCreateParamExpanderVisibility = "Hidden";
                    ShapeMatchParamExpanderVisibility = "Hidden";
                    GetNccParam();

                    break;
                case TemplateType.SHAPE:
                    NccCreateParamExpanderVisibility = "Hidden";
                    NccMatchParamExpanderVisibility = "Hidden";
                    GetShapeParam();

                    break;
                default:
                    break;
            }
        }

        protected override void OnViewLoaded()
        {
            Init();
        }

        public TemplateCreateViewModel(string templateName, TemplateType templateType)
        {
            TemplateName = templateName;
            TemplateType = templateType;

        }

        /// <summary>
        /// 获取视窗内的矩形
        /// </summary>
        /// <param name="rectangle1"></param>
        /// <returns></returns>
        private bool GetViewRectangle(out Rectangle1 rectangle1)
        {
            rectangle1 = new Rectangle1();

            ImagePanelOperation.GetViewRectangle(_thisView.ImagePanel, out List<Rectangle1> regions);

            if (regions.Count < 1)
            {
                _thisView.ImagePanel.Shapes.Clear();
                _thisView.ImagePanel.Clear();
                MaxwellControl.Controls.MessageBox.Show("未识别到矩形框");
                return false;
            }

            if (regions.Count > 1)
            {
                _thisView.ImagePanel.Shapes.Clear();
                _thisView.ImagePanel.Clear();
                MaxwellControl.Controls.MessageBox.Show("识别到多个矩形框");
                return false;
            }

            rectangle1 = regions[0];
            return true;
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
            bool ret = false;
            Camera img = ImageHelper.GetCamera(Image);
            switch (TemplateType)
            {
                case TemplateType.NCC:
                    SetNccParam();
                    ret = CreateNccTemplate(img, region, TemplateName, NccTemplateParams, NccMatchParams);
                    break;
                case TemplateType.SHAPE:
                    SetShapeParam();
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
            Errortype ret = TemplateManagerService.GetInstance().Save(savePath, TemplateName);
            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("SaveTemplateError: " + ret.ToString());
                return false;
            }
            return true;
        }

        public void SetOk()
        {
            bool ret = GetViewRectangle(out Rectangle1 viewRegion);
            if (!ret) return;

            Region templateRegion = new Region();
            templateRegion.Rectangle1 = viewRegion;

            ret = CreateTemplate(templateRegion);
            if (!ret) return;

            ret = SaveTemplate();
            if (!ret) return;

            MaxwellControl.Controls.MessageBox.Show("Create" + TemplateType.ToString() + "Template: OK" + ret.ToString());

            return;
        }

        public void SetCancel()
        {
            _thisView.Close();
        }
    }
}
