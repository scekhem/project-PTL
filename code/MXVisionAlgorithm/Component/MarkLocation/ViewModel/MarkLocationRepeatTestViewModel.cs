using MaxwellFramework.Core.Interfaces;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using MwFramework.Controls.ControlCanvas.Model;
using MX10UBDBU01AA.Common;
using MXVisionAlgorithm.Common;
using MXVisionAlgorithm.Component.MarkLocation.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DataStruct;
using Microsoft.Win32;
using System.Windows.Controls;
using System.Windows.Documents;
using MXVisionAlgorithm.Component.TrainPattern.ViewModel;
using Stylet;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using System.Windows;
using System.Windows.Shapes;
using Path = System.IO.Path;
using System.Windows.Threading;
using System.Threading;

namespace MXVisionAlgorithm.Component.MarkLocation.ViewModel
{
    public class MarkLocationRepeatTestViewModel : AbstractBaseViewModel, IPage
    {
        public string Name { get; set; } = "MarkLocationRepeatTestView";

        public string InnerPatternName { get; set; } = "TopTemplate";
        public string OuterPatternName { get; set; } = "BottomTemplate";

        /// <summary>
        /// 单张图像的InnerMark中心结果
        /// </summary>
        private string _innerCenter = string.Empty;
        public string InnerCenter
        {
            get { return _innerCenter; }
            set { _innerCenter = value; OnPropertyChanged(nameof(InnerCenter)); }
        }

        /// <summary>
        /// 单张图像的InnerMark中心结果
        /// </summary>
        private string _outerCenter = string.Empty;
        public string OuterCenter
        {
            get { return _outerCenter; }
            set { _outerCenter = value; OnPropertyChanged(nameof(OuterCenter)); }
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

        /// <summary>
        /// 输入的毫米像素比
        /// </summary>
        private double _mmppx = 0;
        public double Mmppx
        {
            get { return _mmppx; }
            set { _mmppx = value; OnPropertyChanged(nameof(Mmppx)); }
        }

        private MarkLocationRepeatTestView _thisView;

        private bool _repeatMatch = false;

        /// <summary>
        /// 窗口图像
        /// </summary>
        public WriteableBitmap _image;
        public WriteableBitmap Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        public MarkLocationRepeatTestViewModel()
        {
            string patternPath = AppDomain.CurrentDomain.BaseDirectory + "MarkModel";
            MarkLocationManagerService.GetInstance().InitMarkAutoCenter(patternPath, new List<string>() { InnerPatternName, OuterPatternName });
        }

        /// <summary>
        /// 加载
        /// </summary>
        protected override void OnViewLoaded()
        {
            _thisView = this.View as MarkLocationRepeatTestView;
            _thisView.MatchMultiImageResult.Document.PageWidth = 1000;
        }

        /// <summary>
        /// 训练内部mark
        /// </summary>
        public void TrainInnerPatternClick()
        {
            var windowManager = IoC.Get<IWindowManager>();
            TrainPatternViewModel trainPatternViewModel = new TrainPatternViewModel(InnerPatternName);
            windowManager.ShowWindow(trainPatternViewModel);
        }

        /// <summary>
        /// 训练外部mark
        /// </summary>
        public void TrainOuterPatternClick()
        {
            var windowManager = IoC.Get<IWindowManager>();
            TrainPatternViewModel trainPatternViewModel = new TrainPatternViewModel(OuterPatternName);
            windowManager.ShowWindow(trainPatternViewModel);
        }

        /// <summary>
        /// 训练外部mark
        /// </summary>
        public void InitPatternClick()
        {
            string patternPath = AppDomain.CurrentDomain.BaseDirectory + "MarkModel";
            MarkLocationManagerService.GetInstance().InitMarkAutoCenter(patternPath, new List<string>() { InnerPatternName, OuterPatternName });
        }

        /// <summary>
        /// 获取inner和outer Mark的center 
        /// </summary>
        /// <param name="img"></param>
        /// <param name="innerPatternName"></param>
        /// <param name="outerPatternName"></param>
        /// <param name="rows"></param>
        /// <param name="cols"></param>
        /// <param name="angles"></param>
        /// <param name="scores"></param>
        /// <returns></returns>
        private bool MatchPattern(Camera img, string innerPatternName, string outerPatternName,
            out List<double> rows, out List<double> cols, out List<double> angles, out List<double> scores)
        {
            rows = new List<double>();
            cols = new List<double>();
            angles = new List<double>();
            scores = new List<double>();

            Errortype ret = MarkLocationManagerService.GetInstance().GetMarkCenter(innerPatternName, img, null,
                out double[] rowsInner, out double[] colsInner, out double[] anglesInner, out double[] scoresInner, out List<List<double[]>> straightnessErrorList);

            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("ErrorInner: " + ret.ToString());
                return false;
            }
            if (rowsInner is null || rowsInner.Length == 0)
            {
                MaxwellControl.Controls.MessageBox.Show("ErrorInner: MatchZero");
                return false;
            }

            rows.Add(rowsInner[0]);
            cols.Add(colsInner[0]);
            angles.Add(anglesInner[0]);
            scores.Add(scoresInner[0]);

            ret = MarkLocationManagerService.GetInstance().GetMarkCenter(outerPatternName, img, null,
                out double[] rowsOuter, out double[] colsOuter, out double[] anglesOuter, out double[] scoresOuter, out straightnessErrorList);

            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("ErrorOuter: " + ret.ToString());
                return false;
            }
            if (rowsOuter is null || rowsOuter.Length == 0)
            {
                MaxwellControl.Controls.MessageBox.Show("ErrorOuter: MatchZero");
                return false;
            }

            rows.Add(rowsOuter[0]);
            cols.Add(colsOuter[0]);
            angles.Add(anglesOuter[0]);
            scores.Add(scoresOuter[0]);
            return true;
        }

        /// <summary>
        /// 匹配单张图像
        /// </summary>
        public void MatchPatternClick()
        {
            DataStruct.Camera img = ImageHelper.GetCamera(Image);

            bool result = MatchPattern(img, InnerPatternName, OuterPatternName, out List<double> rows, out List<double> cols, out List<double> angles, out List<double> scores);
            if (!result)
            {
                return;
            }

            InnerCenter = cols[0].ToString("f6") + " " + rows[0].ToString("f6");
            OuterCenter = cols[1].ToString("f6") + " " + rows[1].ToString("f6");

            ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(cols[0], rows[0]));
            ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(cols[1], rows[1]));

        }

        private CancellationTokenSource _cancellationTokenSource;
        /// <summary>
        /// 匹配单张图像
        /// </summary>
        public async void MatchPatternRepeatClick()
        {
            if (!_repeatMatch)
            {
                _repeatMatch = true;
                _cancellationTokenSource = new CancellationTokenSource();
                DataStruct.Camera img = ImageHelper.GetCamera(Image);
                try
                {
                    // 开始执行函数
                    await Task.Run(() =>
                    {
                        while (!_cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            // 调用需要重复执行的函数

                            bool result = MatchPattern(img, InnerPatternName, OuterPatternName, out List<double> rows, out List<double> cols, out List<double> angles, out List<double> scores);
                            if (!result)
                            {
                                return;
                            }

                            InnerCenter = cols[0].ToString("f6") + " " + rows[0].ToString("f6");
                            OuterCenter = cols[1].ToString("f6") + " " + rows[1].ToString("f6");

                            // 等待一段时间，例如 1 秒
                            Thread.Sleep(200);
                        }
                    }, _cancellationTokenSource.Token);
                }
                catch (TaskCanceledException)
                {
                    // 任务被取消时的处理逻辑
                }
                finally
                {
                    _repeatMatch = false;
                }
            }
            else
            {
                // 停止执行函数
                _cancellationTokenSource.Cancel();
            }
        }

        //List<KeyValuePair<string, Camera>> multiImgCameras = new List<KeyValuePair<string, Camera>>();
        List<string> multiImgCameras = new List<string>();

        /// <summary>
        /// 释放所有多选的图像
        /// </summary>
        private void ReleaseMultiImgCameras()
        {
            //for (int i = 0; i < multiImgCameras.Count; i++)
            //{
            //    multiImgCameras[i].Value.Dispose();
            //}
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
                        //Camera img = new Camera(f);
                        string imgName = Path.GetFileName(f);
                        SelectImageName += f;
                        SelectImageName += " ";
                        //multiImgCameras.Add(new KeyValuePair<string, Camera>(imgName, img));
                        multiImgCameras.Add(f);
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
            //ProcessMatch();
            ProcessMatchNonblocking();

        }

        private void ProcessMatch()
        {
            //GCSettings.LatencyMode = GCLatencyMode.Interactive;
            var currentView = this.View as MarkLocationRepeatTestView;
            currentView.MatchMultiImage.IsEnabled = false;
            currentView.ClearRichTextBox.IsEnabled = false;
            currentView.SelectMultiImage.IsEnabled = false;
            Paragraph Paragraph = new Paragraph();
            string time = System.DateTime.Now.ToString();
            Paragraph.Inlines.Add(time + "\n");
            string line = "ImageName InnerX InnerY InnerScore InnerAngle OuterX OuterY OuterScore OuterAngle \n";
            Paragraph.Inlines.Add(line);

            List<double> deltaX = new List<double>();
            List<double> deltaY = new List<double>();


            foreach (var item in multiImgCameras)
            {
                Camera img = new Camera(item);
                bool result = MatchPattern(img, InnerPatternName, OuterPatternName, out List<double> rows, out List<double> cols, out List<double> angles, out List<double> scores);

                if (!result)
                {
                    Image = ImageHelper.Camera2WritableBitmap(img);
                    img.Dispose();
                    currentView.ClearRichTextBox.IsEnabled = true;
                    currentView.SelectMultiImage.IsEnabled = true;
                    return;
                }

                line = Path.GetFileName(item) + " ";
                for (int i = 0; i < rows.Count; i++)
                {
                    DataStruct.Point center = new DataStruct.Point(cols[i], rows[i]);
                    line += center.ToString(" ");
                    line += " " + scores[i].ToString("f6") + " " + angles[i].ToString("f6") + " ";
                }
                line += "\n";

                Paragraph.Inlines.Add(line);
                deltaX.Add(cols[0] - cols[1]);
                deltaY.Add(rows[0] - rows[1]);

                img.Dispose();

            }

            UpdateRichTextBox(_thisView.MatchMultiImageResult, Paragraph);

            ComAlgo.CalcDataSummary(deltaX, out DataStatisticParam analysisValueX);
            ComAlgo.CalcDataSummary(deltaY, out DataStatisticParam analysisValueY);

            line = "RangeDeltaX RangeDeltaY MeanDeltaX MeanDeltaY SigmaX*3 SigmaY*3 \n";
            Paragraph.Inlines.Add(line);
            line = analysisValueX.Range.ToString("f7") + " " + analysisValueY.Range.ToString("f7") + " " +
                   analysisValueX.Mean.ToString("f7") + " " + analysisValueY.Mean.ToString("f7") + " " +
                   analysisValueX.Sigma3.ToString("f7") + " " + analysisValueY.Sigma3.ToString("f7") + "\n";
            Paragraph.Inlines.Add(line);
            line = (analysisValueX.Range * Mmppx).ToString("f7") + " " + (analysisValueY.Range * Mmppx).ToString("f7") + " " +
                  (analysisValueX.Mean * Mmppx).ToString("f7") + " " + (analysisValueY.Mean * Mmppx).ToString("f7") + " " +
                  (analysisValueX.Sigma3 * Mmppx).ToString("f7") + " " + (analysisValueY.Sigma3 * Mmppx).ToString("f7") + "\n";
            Paragraph.Inlines.Add(line);
            currentView.ClearRichTextBox.IsEnabled = true;
            currentView.SelectMultiImage.IsEnabled = true;
            MessageBox.Show(" Match Complete !");
        }

        private async void ProcessMatchNonblocking()
        {
            var currentView = this.View as MarkLocationRepeatTestView;
            currentView.MatchMultiImage.IsEnabled = false;
            currentView.ClearRichTextBox.IsEnabled = false;
            currentView.SelectMultiImage.IsEnabled = false;
            Paragraph Paragraph = new Paragraph();
            string time = System.DateTime.Now.ToString();
            Paragraph.Inlines.Add(time + "\n");
            string line = "ImageName InnerX InnerY InnerScore InnerAngle OuterX OuterY OuterScore OuterAngle \n";
            Paragraph.Inlines.Add(line);

            List<double> deltaX = new List<double>();
            List<double> deltaY = new List<double>();

            await Task.Run(() =>
            {

                foreach (var item in multiImgCameras)
                {
                    Camera img = new Camera(item);
                    bool result = MatchPattern(img, InnerPatternName, OuterPatternName, out List<double> rows, out List<double> cols, out List<double> angles, out List<double> scores);

                    if (!result)
                    {
                        //Image = ImageHelper.Camera2WritableBitmap(img);
                        img.Dispose();
                        //currentView.ClearRichTextBox.IsEnabled = true;
                        //currentView.SelectMultiImage.IsEnabled = true;
                        break;
                    }
                    else
                    {
                        line = Path.GetFileName(item) + " ";
                        for (int i = 0; i < rows.Count; i++)
                        {
                            DataStruct.Point center = new DataStruct.Point(cols[i], rows[i]);
                            line += center.ToString(" ");
                            line += " " + scores[i].ToString("f6") + " " + angles[i].ToString("f6") + " ";
                        }
                        line += "\n";

                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Paragraph.Inlines.Add(line);
                            deltaX.Add(cols[0] - cols[1]);
                            deltaY.Add(rows[0] - rows[1]);

                        }));

                        img.Dispose();
                    }
                }
            });

            UpdateRichTextBox(_thisView.MatchMultiImageResult, Paragraph);

            ComAlgo.CalcDataSummary(deltaX, out DataStatisticParam analysisValueX);
            ComAlgo.CalcDataSummary(deltaY, out DataStatisticParam analysisValueY);

            line = "RangeDeltaX RangeDeltaY MeanDeltaX MeanDeltaY SigmaX*3 SigmaY*3 \n";
            Paragraph.Inlines.Add(line);
            line = analysisValueX.Range.ToString("f7") + " " + analysisValueY.Range.ToString("f7") + " " +
                   analysisValueX.Mean.ToString("f7") + " " + analysisValueY.Mean.ToString("f7") + " " +
                   analysisValueX.Sigma3.ToString("f7") + " " + analysisValueY.Sigma3.ToString("f7") + "\n";
            Paragraph.Inlines.Add(line);
            line = (analysisValueX.Range * Mmppx).ToString("f7") + " " + (analysisValueY.Range * Mmppx).ToString("f7") + " " +
                  (analysisValueX.Mean * Mmppx).ToString("f7") + " " + (analysisValueY.Mean * Mmppx).ToString("f7") + " " +
                  (analysisValueX.Sigma3 * Mmppx).ToString("f7") + " " + (analysisValueY.Sigma3 * Mmppx).ToString("f7") + "\n";
            Paragraph.Inlines.Add(line);
            currentView.ClearRichTextBox.IsEnabled = true;
            currentView.SelectMultiImage.IsEnabled = true;
            MessageBox.Show(" Match Complete !");

        }

        /// <summary>
        /// 清除文本框内容
        /// </summary>
        public void ClearRichTextBoxClick()
        {
            FlowDocument paragraph = new FlowDocument();
            paragraph.PageWidth = 1000;
            _thisView.MatchMultiImageResult.Document = paragraph;
            (this.View as MarkLocationRepeatTestView).MatchMultiImage.IsEnabled = true;
        }

    }
}
