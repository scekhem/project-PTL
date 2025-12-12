using DataStruct;
using MaxwellFramework.Core.Interfaces;
using MX10UBDBU01AA.Common;
using MXVisionAlgorithm.Common;
using MXVisionAlgorithm.Component.MarkLocation.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using MwFramework.Controls.ControlCanvas.DrawingControl;
using MXVisionAlgorithm.Component.CalcMmppx.View;
using UltrapreciseBonding.FusionCollections;
using MwFramework.Controls.ControlCanvas.Model;
using System.Windows;
using System.Collections.ObjectModel;

namespace MXVisionAlgorithm.Component.CalcMmppx.ViewModel
{
    public class CalcMmppxViewModel : AbstractBaseViewModel, IPage
    {
        public string Name { get; set; } = "CalcMmppxView";

        /// <summary>
        /// 输入的mark尺寸大小
        /// </summary>
        private double _markSize;
        public double MarkSize
        {
            get { return _markSize; }
            set
            {
                _markSize = value;
                OnPropertyChanged(nameof(MarkSize));
            }
        }

        /// <summary>
        /// 输出的图像像素距离
        /// </summary>
        private string _distPixel;
        public string DistPixel
        {
            get { return _distPixel; }
            set
            {
                _distPixel = value;
                OnPropertyChanged(nameof(DistPixel));
            }
        }

        /// <summary>
        /// 输出的图像像素比
        /// </summary>
        private string _imageMmppx;
        public string ImageMmppx
        {
            get { return _imageMmppx; }
            set
            {
                _imageMmppx = value;
                OnPropertyChanged(nameof(ImageMmppx));
            }
        }

        /// <summary>
        /// 输出的图像像素距离
        /// </summary>
        private string _manualDistPixel;
        public string ManualDistPixel
        {
            get { return _manualDistPixel; }
            set
            {
                _manualDistPixel = value;
                OnPropertyChanged(nameof(ManualDistPixel));
            }
        }

        /// <summary>
        /// 输出的图像像素比
        /// </summary>
        private string _manualImageMmppx;
        public string ManualImageMmppx
        {
            get { return _manualImageMmppx; }
            set
            {
                _manualImageMmppx = value;
                OnPropertyChanged(nameof(ManualImageMmppx));
            }
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

        public ObservableCollection<PrimShape> DrawRegions { get; set; } = new ObservableCollection<PrimShape>();

        private CalcMmppxView _thisView;

        /// <summary>
        /// 加载
        /// </summary>
        protected override void OnViewLoaded()
        {
            _thisView = this.View as CalcMmppxView;
        }

        /// <summary>
        /// 自动计算像素比
        /// </summary>
        public void AutoCalcMmppx()
        {
            DataStruct.Camera img = ImageHelper.GetCamera(Image);
            Errortype ret = FusionManagerSimplifyCalib.CalcMmppx(img, MarkSize, out double mmppx, out List<LineSeg> lineSegs);
            if (Errortype.OK != ret)
            {
                MaxwellControl.Controls.MessageBox.Show("Error:" + ret.ToString());
            }

            double dist = MarkSize / mmppx;
            DistPixel = dist.ToString("f6");

            ImageMmppx = mmppx.ToString("f9");

            //_thisView.ImagePanel.ShapeContent = new LineShapeModel();
            for (int i = 0; i < lineSegs.Count; i++)
            {
                //var shapeLine = new PrimShape();
                //shapeLine.Region = new LineRegion();
                //System.Windows.Point lineStart = new System.Windows.Point(lineSegs[i].Start_X / _thisView.ImagePanel.WidthRatio, lineSegs[i].Start_Y / _thisView.ImagePanel.HeightRatio);
                //System.Windows.Point lineEnd = new System.Windows.Point(lineSegs[i].End_X / _thisView.ImagePanel.WidthRatio, lineSegs[i].End_Y / _thisView.ImagePanel.HeightRatio);
                //_thisView.ImagePanel.ShapeContent.SetReportItem(_thisView.ImagePanel, shapeLine.Region, lineStart, lineEnd);
                //_thisView.ImagePanel.Shapes.Add(shapeLine);
                _thisView.ImagePanel.Items.Add(new LineShapeDrawing() { X = lineSegs[i].Start_X, Y = lineSegs[i].Start_Y, X1 = lineSegs[i].End_X, Y1 = lineSegs[i].End_Y, Range = 1000 });
            }

            img.Dispose();
        }

        /// <summary>
        /// 自动计算像素比
        /// </summary>
        public void ManualCalcMmppx()
        {
            DataStruct.Camera img = ImageHelper.GetCamera(Image);
            List<LineSeg> drawLines = new List<LineSeg>();
            foreach (var item in DrawRegions)
            {
                if (item.Region is LineRegion)
                {
                    LineRegion lineRegion = item.Region as LineRegion;
                    DataStruct.Point p0 = new DataStruct.Point(lineRegion.P0.X, lineRegion.P0.Y);
                    DataStruct.Point p1 = new DataStruct.Point(lineRegion.P1.X, lineRegion.P1.Y);
                    drawLines.Add(new LineSeg(p0, p1));
                }
            }

            if (drawLines.Count != 2)
            {
                MaxwellControl.Controls.MessageBox.Show("Error: 未找到两条线");
                return;
            }

            Errortype ret = Errortype.OK;
            DataStruct.CaliperParams caliperParams = new DataStruct.CaliperParams();
            ret = UltrapreciseBonding.Caliper.CaliperLine.LineExtraction(img, drawLines[0], caliperParams, out LineSeg lineOut0, out _);
            if (ret != Errortype.OK)
            {
                MaxwellControl.Controls.MessageBox.Show("Error:" + ret.ToString());
                return;
            }

            ret = UltrapreciseBonding.Caliper.CaliperLine.LineExtraction(img, drawLines[1], caliperParams, out LineSeg lineOut1, out _);
            if (ret != Errortype.OK)
            {
                MaxwellControl.Controls.MessageBox.Show("Error:" + ret.ToString());
                return;
            }

            List<LineSeg> resultLines = new List<LineSeg>() { lineOut0, lineOut1 };

            double dist = UltrapreciseBonding.UltrapreciseAlgorithm.ComAlgo.Dist(lineOut0, lineOut1);

            double mmppx = MarkSize / dist;
            ManualDistPixel = dist.ToString("f6");
            ManualImageMmppx = mmppx.ToString("f9");

            DrawRegions.Clear();

            //_thisView.ImagePanel.ShapeContent = new LineShapeModel();
            for (int i = 0; i < resultLines.Count; i++)
            {
                //var shapeLine = new PrimShape();
                //shapeLine.Region = new LineRegion();
                //System.Windows.Point lineStart = new System.Windows.Point(resultLines[i].Start_X / _thisView.ImagePanel.WidthRatio, resultLines[i].Start_Y / _thisView.ImagePanel.HeightRatio);
                //System.Windows.Point lineEnd = new System.Windows.Point(resultLines[i].End_X / _thisView.ImagePanel.WidthRatio, resultLines[i].End_Y / _thisView.ImagePanel.HeightRatio);
                //_thisView.ImagePanel.ShapeContent.SetReportItem(_thisView.ImagePanel, shapeLine.Region, lineStart, lineEnd);
                //_thisView.ImagePanel.Shapes.Add(shapeLine);
                _thisView.ImagePanel.Items.Add(new LineShapeDrawing() { X = resultLines[i].Start_X, Y = resultLines[i].Start_Y, X1 = resultLines[i].End_X, Y1 = resultLines[i].End_Y, Range = 1000 });
            }

            img.Dispose();
            //if (_thisView.ImagePanel.SelectedReportItems.Count > 0)
            //{
            //    foreach (IReportItemControl item in _thisView.ImagePanel.SelectedReportItems)
            //    {
            //        if (item.ItemType != DrawingReportItem.Line)
            //        {
            //            continue;
            //        }
            //        LineSeg line = new LineSeg();
            //        line.Start_X = item.ItemLeft;
            //        line.Start_Y = item.ItemTop;
            //        line.End_X = item.ItemLeft + item.ItemWidth;
            //        line.End_Y = item.ItemTop + item.ItemHeight;
            //        drawLines.Add(line);
            //    }
            //}

        }

    }
}
