using DataStruct;
using MaxwellFramework.Core.Interfaces;
using MX10UBDBU01AA.Common;
using MXVisionAlgorithm.Common;
using MXVisionAlgorithm.Component.TemplateMatch.View;
using PropertyChanged;
using Stylet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using UltrapreciseBonding.TemplateMatch;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace MXVisionAlgorithm.Component.TemplateMatch.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class TemplateMatchRepeatTestViewModel : AbstractBaseViewModel, IPage
    {
        public string Name { get; set; } = "TemplateMatchRepeatTestView";

        public string FirstTemplateName { get; set; } = "FirstTemplateMatch";

        public string SecondTemplateName { get; set; } = "SecondTemplateMatch";

        private string _firstTemplateType;
        public string FirstTemplateType
        {
            get { return _firstTemplateType; }
            set
            {
                _firstTemplateType = value;
                OnPropertyChanged(nameof(FirstTemplateType));
                if (FirstTemplateType == "Ncc") FirstTemplateTypeEnum = TemplateType.NCC;
                if (FirstTemplateType == "Shape") FirstTemplateTypeEnum = TemplateType.SHAPE;
            }
        }

        private string _secondTemplateType;
        public string SecondTemplateType
        {
            get { return _secondTemplateType; }
            set
            {
                _secondTemplateType = value;
                OnPropertyChanged(nameof(SecondTemplateType));
                if (SecondTemplateType == "Ncc") SecondTemplateTypeEnum = TemplateType.NCC;
                if (SecondTemplateType == "Shape") SecondTemplateTypeEnum = TemplateType.SHAPE;
            }
        }

        private TemplateType FirstTemplateTypeEnum { get; set; }

        private TemplateType SecondTemplateTypeEnum { get; set; }

        public string SingleImageFirstCenterResult { get; set; }

        public string SingleImageSecondCenterResult { get; set; }

        public string SelectImageName { get; set; }

        private TemplateMatchRepeatTestView _thisView;

        /// <summary>
        /// 窗口图像
        /// </summary>
        public WriteableBitmap Image { get; set; }

        public TemplateMatchRepeatTestViewModel()
        {

        }

        private void Init()
        {
            _thisView = this.View as TemplateMatchRepeatTestView;

            _thisView.FirstTemplateType.ItemsSource = new List<string>() { "Ncc", "Shape" };
            _thisView.FirstTemplateType.SelectedIndex = 0;

            _thisView.SecondTemplateType.ItemsSource = new List<string>() { "Ncc", "Shape" };
            _thisView.SecondTemplateType.SelectedIndex = 0;

            string templatePath = AppDomain.CurrentDomain.BaseDirectory + "TemplateModel";
            TemplateManager.Load(templatePath, new List<string>() { FirstTemplateName, SecondTemplateName }, out Dictionary<string, Errortype> initReturn);

            ClearRichTextBoxClick();
        }

        protected override void OnViewLoaded()
        {
            Init();
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        public void FirstTemplateCreateClick()
        {
            var windowManager = IoC.Get<IWindowManager>();

            TemplateCreateViewModel templateCreateViewModel = new TemplateCreateViewModel(FirstTemplateName, FirstTemplateTypeEnum);
            windowManager.ShowWindow(templateCreateViewModel);
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        public void SecondTemplateCreateClick()
        {
            var windowManager = IoC.Get<IWindowManager>();
            TemplateCreateViewModel templateCreateViewModel = new TemplateCreateViewModel(SecondTemplateName, SecondTemplateTypeEnum);
            windowManager.ShowWindow(templateCreateViewModel);
        }

        /// <summary>
        /// 匹配单张图像模板
        /// </summary>
        public void TemplateMatchClick()
        {
            SingleImageFirstCenterResult = string.Empty;
            SingleImageSecondCenterResult = string.Empty;

            ImagePanelOperation.GetViewRectangle(_thisView.ImagePanel, out List<Rectangle1> regions);
            Region templateMatchRegion = new Region();
            templateMatchRegion.Rectangle1List = regions;
            Camera img = ImageHelper.GetCamera(Image);

            Errortype ret = TemplateManagerService.GetInstance().Match(FirstTemplateName, img, templateMatchRegion,
                out double[] rowsFirst, out double[] colsFirst, out double[] anglesFirst, out double[] scalesFirst, out double[] scoresFirst);

            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("TemplateMatchError: " + ret.ToString());
                return;
            }

            for (int i = 0; i < rowsFirst.Length; i++)
            {
                System.Windows.Point center = new System.Windows.Point(colsFirst[i], rowsFirst[i]);
                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, center);
                SingleImageFirstCenterResult += new Point(colsFirst[i], rowsFirst[i]).ToString(" ") + " ";
            }

            ret = TemplateManagerService.GetInstance().Match(SecondTemplateName, img, templateMatchRegion,
                out double[] rowsSecond, out double[] colsSecond, out double[] anglesSecond, out double[] scalesSecond, out double[] scoresSecond);

            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("TemplateMatchError: " + ret.ToString());
                return;
            }

            for (int i = 0; i < rowsSecond.Length; i++)
            {
                System.Windows.Point center = new System.Windows.Point(colsSecond[i], rowsSecond[i]);
                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, center);
                SingleImageSecondCenterResult += new Point(colsSecond[i], rowsSecond[i]).ToString(" ") + " ";
            }

        }

        //List<KeyValuePair<string, Camera>> multiImgCameras = new List<KeyValuePair<string, Camera>>();
        private List<string> _multiImgNames = new List<string>();

        /// <summary>
        /// 释放所有多选的图像
        /// </summary>
        private void ReleaseMultiImgCameras()
        {
            _multiImgNames.Clear();
        }

        /// <summary>
        /// 选择多个图像文件
        /// </summary>
        public void SelectMultiImageClick()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image File (*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif)|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog().Value)
            {
                ReleaseMultiImgCameras();
                try
                {
                    List<string> ImportFiles = openFileDialog.FileNames.ToList();
                    if (ImportFiles.Count == 0 || ImportFiles == null)
                    {
                        MaxwellControl.Controls.MessageBox.Show("未选择图片文件，或选择文件不存在！请重新操作");
                        return;
                    }
                    SelectImageName = string.Empty;
                    ImportFiles.ForEach(f =>
                    {
                        //Camera img = new Camera(f);
                        //string imgName = Path.GetFileName(f);
                        SelectImageName += f;
                        SelectImageName += " ";
                        //multiImgCameras.Add(new KeyValuePair<string, Camera>(imgName, img));
                        _multiImgNames.Add(f);
                    });
                    MaxwellControl.Controls.MessageBox.Show("图片加载完成！");
                }
                catch (Exception)
                {
                    MaxwellControl.Controls.MessageBox.Show("格式不正确！");
                }
            }

            // show first img
            if (_multiImgNames.Count > 0)
            {
                Camera img = new Camera(_multiImgNames[0]);
                Image = ImageHelper.Camera2WritableBitmap(img);
                img.Dispose();
            }
        }

        /// <summary>
        /// 更新富文本框
        /// </summary>
        /// <param name="richTextBox"></param>
        /// <param name="para"></param>
        private void UpdateRichTextBox(RichTextBox richTextBox, Paragraph para)
        {
            FlowDocument fd = richTextBox.Document;
            para.Margin = new System.Windows.Thickness(0, 0, 0, 0);
            fd.Blocks.Add(para);
            richTextBox.Document = fd;
            richTextBox.ScrollToEnd();
        }

        /// <summary>
        /// 获取inner和outer Mark的center 
        /// </summary>
        /// <param name="img"></param>
        /// <param name="firstTemplateName"></param>
        /// <param name="secondTemplateName"></param>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <param name="angles"></param>
        /// <param name="scores"></param>
        /// <returns></returns>
        private bool MatchFirstAndSecondPattern(Camera img, string firstTemplateName, string secondTemplateName,
            out List<double> rows, out List<double> cols, out List<double> angles, out List<double> scores, out List<double> scales)
        {
            rows = new List<double>();
            cols = new List<double>();
            angles = new List<double>();
            scores = new List<double>();
            scales = new List<double>();

            Errortype ret = TemplateManagerService.GetInstance().Match(firstTemplateName, img, null,
                out double[] rowsFirst, out double[] colsFirst, out double[] anglesFirst, out double[] scoresFirst, out double[] scaleFirst);

            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("Error: " + ret.ToString());
                return false;
            }
            if (rowsFirst is null || rowsFirst.Length == 0)
            {
                MaxwellControl.Controls.MessageBox.Show("Error: MatchZero");
                return false;
            }

            rows.Add(rowsFirst[0]);
            cols.Add(colsFirst[0]);
            angles.Add(anglesFirst[0]);
            scores.Add(scoresFirst[0]);
            scales.Add(scaleFirst[0]);

            ret = TemplateManagerService.GetInstance().Match(secondTemplateName, img, null,
                out double[] rowsSecond, out double[] colsSecond, out double[] anglesSecond, out double[] scoresSecond, out double[] scaleSecond);

            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("Error: " + ret.ToString());
                return false;
            }
            if (rowsSecond is null || rowsSecond.Length == 0)
            {
                MaxwellControl.Controls.MessageBox.Show("Error: MatchZero");
                return false;
            }

            rows.Add(rowsSecond[0]);
            cols.Add(colsSecond[0]);
            angles.Add(anglesSecond[0]);
            scores.Add(scoresSecond[0]);
            scales.Add(scaleSecond[0]);
            return true;
        }

        /// <summary>
        /// 匹配多张图像
        /// </summary>
        public void MatchMultiImageClick()
        {
            Paragraph paragraph = new Paragraph();
            string time = System.DateTime.Now.ToString();
            paragraph.Inlines.Add(time + "\n");
            string line = "ImageName FirstX FirstY FirstScore FirstAngle FirstScale SecondX SecondY SecondScore SecondAngle SecondScale \n";
            paragraph.Inlines.Add(line);

            List<double> deltaX = new List<double>();
            List<double> deltaY = new List<double>();

            foreach (var item in _multiImgNames)
            {
                Camera img = new Camera(item);
                bool result = MatchFirstAndSecondPattern(img, FirstTemplateName, SecondTemplateName, out List<double> rows, out List<double> cols, out List<double> angles, out List<double> scores, out List<double> scales);
                if (!result)
                {
                    Image = ImageHelper.Camera2WritableBitmap(img);
                    img.Dispose();
                    return;
                }

                line = Path.GetFileName(item) + " ";
                for (int i = 0; i < rows.Count; i++)
                {
                    DataStruct.Point center = new DataStruct.Point(cols[i], rows[i]);
                    line += center.ToString(" ");
                    line += " " + scores[i].ToString("f6") + " " + angles[i].ToString("f6") + " " + scales[i].ToString("f6") + " ";
                }
                line += "\n";
                paragraph.Inlines.Add(line);

                deltaX.Add(cols[0] - cols[1]);
                deltaY.Add(rows[0] - rows[1]);
                img.Dispose();
            }
            UpdateRichTextBox(_thisView.MatchMultiImageResult, paragraph);

            ComAlgo.CalcDataSummary(deltaX, out DataStatisticParam analysisValueX);
            ComAlgo.CalcDataSummary(deltaY, out DataStatisticParam analysisValueY);

            line = "RangeDeltaX RangeDeltaY MeanDeltaX MeanDeltaY SigmaX*3 SigmaY*3 \n";
            paragraph.Inlines.Add(line);
            line = analysisValueX.Range.ToString("f7") + " " + analysisValueY.Range.ToString("f7") + " " +
                   analysisValueX.Mean.ToString("f7") + " " + analysisValueY.Mean.ToString("f7") + " " +
                   analysisValueX.Sigma3.ToString("f7") + " " + analysisValueY.Sigma3.ToString("f7") + "\n";
            paragraph.Inlines.Add(line);
        }

        /// <summary>
        /// 清除文本框内容
        /// </summary>
        public void ClearRichTextBoxClick()
        {
            FlowDocument paragraph = new FlowDocument();
            paragraph.PageWidth = 1000;
            _thisView.MatchMultiImageResult.Document = paragraph;
        }
    }
}
