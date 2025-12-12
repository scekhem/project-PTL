using DataStruct;
using MaxwellFramework.Core.Interfaces;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using MwFramework.Controls.ControlCanvas.Model;
using MwTrainTemplate.Component.TemplateMatch.ViewModel;
using MX10UBDBU01AA.Common;
using MXVisionAlgorithm.Common;
using MXVisionAlgorithm.Component.MarkLocation.View;
using MXVisionAlgorithm.Component.TemplateMatch.View;
using Stylet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using UltrapreciseBonding.TemplateMatch;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace MXVisionAlgorithm.Component.TemplateMatch.ViewModel
{
    public class TemplateMatchViewModel : AbstractBaseViewModel, IPage
    {
        public string Name { get; set; } = "TemplateMatchView";

        public string TemplateName { get; set; } = "TemplateMatch";

        /// <summary>
        /// 模板类型
        /// </summary>
        private string _templateType = string.Empty;
        public string TemplateType
        {
            get { return _templateType; }
            set
            {
                _templateType = value;
                OnPropertyChanged(nameof(TemplateType));
                if (TemplateType == "Ncc") TemplateTypeEnum = DataStruct.TemplateType.NCC;
                if (TemplateType == "Shape") TemplateTypeEnum = DataStruct.TemplateType.SHAPE;
            }
        }

        private TemplateType TemplateTypeEnum { get; set; }

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
        /// 多张图像选择的名字
        /// </summary>
        private string _selectImageName = string.Empty;
        public string SelectImageName
        {
            get { return _selectImageName; }
            set { _selectImageName = value; OnPropertyChanged(nameof(SelectImageName)); }
        }

        private TemplateMatchView _thisView;

        /// <summary>
        /// 窗口图像
        /// </summary>
        public WriteableBitmap _image;
        public WriteableBitmap Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        public TemplateMatchViewModel()
        {

        }

        private void Init()
        {
            _thisView = this.View as TemplateMatchView;

            _thisView.TemplateType.ItemsSource = new List<string>() { "Ncc", "Shape" };
            _thisView.TemplateType.SelectedIndex = 0;

            string templatePath = AppDomain.CurrentDomain.BaseDirectory + "TemplateModel";
            TemplateManager.Load(templatePath, TemplateName);

            ClearRichTextBoxClick();
        }

        protected override void OnViewLoaded()
        {
            Init();
        }

        /// <summary>
        /// 创建模板
        /// </summary>
        public void TemplateCreateClick()
        {
            var windowManager = IoC.Get<IWindowManager>();

            TemplateCreateViewModel trainTemplateViewModel = new TemplateCreateViewModel(TemplateName, TemplateTypeEnum);
            //windowManager.ShowWindow(templateCreateViewModel);
            //TrainTemplateViewModel trainTemplateViewModel = IoC.Get<TrainTemplateViewModel>();
            //TrainTemplateViewModel trainTemplateViewModel = new TrainTemplateViewModel(new List<string>{"CameraWafer", "CameraTop", "CameraBottom", "CameraFU",},TemplateName, TemplateTypeEnum);
            windowManager.ShowWindow(trainTemplateViewModel);
        }

        /// <summary>
        /// 载入模板
        /// </summary>
        public void TemplateLoadClick()
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择一个模版文件夹";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string folderPath = dialog.SelectedPath;
                string templatePath = Directory.GetParent(folderPath).FullName;
                TemplateName = Path.GetFileNameWithoutExtension(folderPath);
                var ret = TemplateManager.Load(templatePath, TemplateName);
                // 在这里处理选中的文件夹路径
                if (ret != Errortype.OK)
                {
                    MessageBox.Show("模版文件异常: " + ret);
                    return;
                }

                MessageBox.Show("载入模版: " + TemplateName);
            }

        }

        /// <summary>
        /// 匹配单张图像模板
        /// </summary>
        public void TemplateMatchClick()
        {
            SingleImageCenterResult = string.Empty;

            ImagePanelOperation.GetViewRectangle(_thisView.ImagePanel, out List<Rectangle1> regions);
            Region templateMatchRegion = new Region();
            templateMatchRegion.Rectangle1List = regions;
            Camera img = ImageHelper.GetCamera(Image);

            Errortype ret = TemplateManagerService.GetInstance().Match(TemplateName, img, templateMatchRegion,
                out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores);

            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("TemplateMatchError: " + ret.ToString());
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                System.Windows.Point center = new System.Windows.Point(cols[i], rows[i]);
                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, center);
                SingleImageCenterResult += new Point(cols[i], rows[i]).ToString(" ");
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
        /// 匹配多张图像
        /// </summary>
        public void MatchMultiImageClick()
        {
            Errortype ret = Errortype.OK;
            Paragraph paragraph = new Paragraph();
            string time = System.DateTime.Now.ToString();
            paragraph.Inlines.Add(time + "\n");
            string line = "ImageName X Y Score Angle Scale \n";
            paragraph.Inlines.Add(line);

            ImagePanelOperation.GetViewRectangle(_thisView.ImagePanel, out List<Rectangle1> regions);
            Region templateMatchRegion = new Region
            {
                Rectangle1List = regions
            };

            foreach (var item in _multiImgNames)
            {
                Camera img = new Camera(item);
                string imgName = Path.GetFileName(item);
                ret = TemplateManagerService.GetInstance().Match(TemplateName, img, templateMatchRegion,
                    out double[] rows, out double[] cols, out double[] angles, out double[] scales, out double[] scores);

                if (Errortype.OK != ret)
                {
                    Image = ImageHelper.Camera2WritableBitmap(img);
                    MaxwellControl.Controls.MessageBox.Show("Error: " + ret.ToString());
                    return;
                }
                if (rows is null || rows.Length == 0)
                {
                    Image = ImageHelper.Camera2WritableBitmap(img);
                    MaxwellControl.Controls.MessageBox.Show("Error: MatchZero");
                    return;
                }

                line = imgName + " ";
                for (int i = 0; i < rows.Length; i++)
                {
                    DataStruct.Point center = new DataStruct.Point(cols[i], rows[i]);
                    line += center.ToString(" ");
                    line += " " + scores[i].ToString("f6") + " " + angles[i].ToString("f6") + " " + scales[i].ToString("f6");
                }
                line += "\n";
                paragraph.Inlines.Add(line);
                UpdateRichTextBox(_thisView.MatchMultiImageResult, paragraph);
            }
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
