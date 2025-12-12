using DataStruct;
using MaxwellFramework.Core.Interfaces;
using Microsoft.Win32;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using MwFramework.Controls.ControlCanvas.Model;
using MwTrainTemplate.Component.MarkLocation.ViewModel;
using MX10UBDBU01AA.Common;
using MXVisionAlgorithm.Common;
using MXVisionAlgorithm.Component.MarkLocation.View;
using MXVisionAlgorithm.Component.TrainPattern.ViewModel;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

namespace MXVisionAlgorithm.Component.MarkLocation.ViewModel
{
    public class MarkLocationViewModel : AbstractBaseViewModel, IPage
    {
        public string Name { get; set; } = "MarkLocationView";

        public string PatternName { get; set; } = "MarkLocationPattern";

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

        private MarkLocationView _thisView;

        /// <summary>
        /// 窗口图像
        /// </summary>
        public WriteableBitmap _image;
        public WriteableBitmap Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        public MarkLocationViewModel()
        {
            //windowManager = IoC.Get<IWindowManager>();
            string patternPath = AppDomain.CurrentDomain.BaseDirectory + "MarkModel";
            MarkLocationManagerService.GetInstance().InitMarkAutoCenter(patternPath, new List<string>() { PatternName });
        }

        /// <summary>
        /// 加载
        /// </summary>
        protected override void OnViewLoaded()
        {
            _thisView = this.View as MarkLocationView;
            _thisView.MatchMultiImageResult.Document.PageWidth = 1000;
        }

        /// <summary>
        /// 训练pattern
        /// </summary>
        public void TrainPatternClick()
        {
            //var windowManager = IoC.Get<IWindowManager>();
            //TrainPatternViewModel trainPatternViewModel = new TrainPatternViewModel(PatternName);
            //windowManager.ShowWindow(trainPatternViewModel);

            var windowManager = IoC.Get<IWindowManager>();
            TrainMarkLocationViewModel trainMarkLocationViewModel = new TrainMarkLocationViewModel(PatternName, PatternName);
            windowManager.ShowWindow(trainMarkLocationViewModel);
        }

        /// <summary>
        /// 匹配单张图像
        /// </summary>
        public void MatchPatternClick()
        {
            DataStruct.Camera img = ImageHelper.GetCamera(Image);
            Errortype ret = MarkLocationManagerService.GetInstance().GetMarkCenter(PatternName, img, null,
                out double[] rows, out double[] cols, out double[] angles, out double[] scores, out List<List<double[]>> straightnessErrorList);
            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("Error: " + ret.ToString());
                return;
            }
            if (rows is null || rows.Length == 0)
            {
                MaxwellControl.Controls.MessageBox.Show("Error: MatchZero");
                return;
            }
            SingleImageCenterResult = string.Empty;
            for (int i = 0; i < rows.Length; i++)
            {
                System.Windows.Point center = new System.Windows.Point(cols[i], rows[i]);
                SingleImageCenterResult += cols[i].ToString("f6") + " " + rows[i].ToString("f6");
                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, center, 300);
            }
        }

        List<KeyValuePair<string, Camera>> multiImgCameras = new List<KeyValuePair<string, Camera>>();


        /// <summary>
        /// 释放所有多选的图像
        /// </summary>
        private void ReleaseMultiImgCameras()
        {
            for (int i = 0; i < multiImgCameras.Count; i++)
            {
                multiImgCameras[i].Value.Dispose();
            }
            multiImgCameras.Clear();
        }

        /// <summary>
        /// 选择多张图像
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
                        Camera img = new Camera(f);
                        string imgName = Path.GetFileName(f);
                        SelectImageName += f;
                        SelectImageName += " ";
                        multiImgCameras.Add(new KeyValuePair<string, Camera>(imgName, img));
                    });
                    MaxwellControl.Controls.MessageBox.Show("图片加载完成！");
                }
                catch (Exception)
                {
                    MaxwellControl.Controls.MessageBox.Show("格式不正确！");
                }
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
            string line = "ImageName X Y Score Angle \n";
            paragraph.Inlines.Add(line);
            foreach (var item in multiImgCameras)
            {
                ret = MarkLocationManagerService.GetInstance().GetMarkCenter(PatternName, item.Value, null,
                    out double[] rows, out double[] cols, out double[] angles, out double[] scores, out List<List<double[]>> straightnessErrorList);
                if (Errortype.OK != ret)
                {
                    Image = ImageHelper.Camera2WritableBitmap(item.Value);
                    MaxwellControl.Controls.MessageBox.Show("Error: " + ret.ToString());
                    return;
                }
                if (rows is null || rows.Length == 0)
                {
                    Image = ImageHelper.Camera2WritableBitmap(item.Value);
                    MaxwellControl.Controls.MessageBox.Show("Error: MatchZero");
                    return;
                }

                line = item.Key + " ";
                for (int i = 0; i < rows.Length; i++)
                {
                    DataStruct.Point center = new DataStruct.Point(cols[i], rows[i]);
                    line += center.ToString(" ");
                    line += " " + scores[i].ToString("f6") + " " + angles[i].ToString("f6");
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
