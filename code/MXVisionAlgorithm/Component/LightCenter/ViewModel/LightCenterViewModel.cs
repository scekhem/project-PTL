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
using MXVisionAlgorithm.Component.LightCenter.View;
using UltrapreciseBonding.FusionCollections;
using MwFramework.Controls.ControlCanvas.Model;
using System.Windows;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using MaxwellControl.Tools;

namespace MXVisionAlgorithm.Component.LightCenter.ViewModel
{
    public class LightCenterViewModel : AbstractBaseViewModel, IPage
    {
        public string Name { get; set; } = "LightCenterView";

        /// <summary>
        /// 输出的LTRBResult
        /// </summary>
        private string _lTRBResult;
        public string LTRBResult
        {
            get { return _lTRBResult; }
            set
            {
                _lTRBResult = value;
                OnPropertyChanged(nameof(LTRBResult));
            }
        }

        /// <summary>
        /// 输出的LTRBResult
        /// </summary>
        private string _lBRTResult;
        public string LBRTResult
        {
            get { return _lBRTResult; }
            set
            {
                _lBRTResult = value;
                OnPropertyChanged(nameof(LBRTResult));
            }
        }

        /// <summary>
        /// Sigma
        /// </summary>
        private double _sigma = 5;
        public double Sigma
        {
            get { return _sigma; }
            set
            {
                _sigma = value;
                OnPropertyChanged(nameof(Sigma));
            }
        }


        /// <summary>
        /// 输出的LTRBResult
        /// </summary>
        private string _areaCenterResult;
        public string AreaCenterResult
        {
            get { return _areaCenterResult; }
            set
            {
                _areaCenterResult = value;
                OnPropertyChanged(nameof(AreaCenterResult));
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

        private LightCenterView _thisView;

        public ObservableCollection<PrimShape> DrawRegions { get; set; } = new ObservableCollection<PrimShape>();

        /// <summary>
        /// 加载
        /// </summary>
        protected override void OnViewLoaded()
        {
            _thisView = this.View as LightCenterView;
        }

        public void Calc()
        {
            try
            {
                _thisView.ImagePanel.Items.Clear();

                if (Image is null)
                {
                    MaxwellControl.Controls.MessageBox.Show("Image is Empty");
                    return;
                }

                Camera img = ImageHelper.GetCamera(Image);

                DataStruct.Point lt = new DataStruct.Point(1, 1);
                DataStruct.Point lb = new DataStruct.Point(1, img.Height - 1);
                DataStruct.Point rt = new DataStruct.Point(img.Width - 1, 1);
                DataStruct.Point rb = new DataStruct.Point(img.Width - 1, img.Height - 1);
                Errortype ret = ComAlgo.CalcLightCenter(img, lt, rb, out DataStruct.Point ltrbCenter, out List<DataStruct.Point> ltrbCrossPoints, out List<double> ltrbGray, Sigma);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show("LTRB Error: %s", ret.ToString());
                }

                ret = ComAlgo.CalcLightCenter(img, lb, rt, out DataStruct.Point lbrtCenter, out List<DataStruct.Point> lbrtCrossPoints, out List<double> lbrtGray, Sigma);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show("LBRT Error: %s", ret.ToString());
                }

                LTRBResult = ltrbCenter.ToString(" ", 3);
                LBRTResult = lbrtCenter.ToString(" ", 3);

                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(ltrbCenter.X, ltrbCenter.Y), 400);
                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(lbrtCenter.X, lbrtCenter.Y), 400);
            }
            catch (Exception e)
            {
                MaxwellControl.Controls.MessageBox.Show(e.Message);
            }
        }

        public void CalcLtrb()
        {
            try
            {
                _thisView.ImagePanel.Items.Clear();

                if (Image is null)
                {
                    MaxwellControl.Controls.MessageBox.Show("Image is Empty");
                    return;
                }

                Camera img = ImageHelper.GetCamera(Image);

                DataStruct.Point lt = new DataStruct.Point(1, 1);
                DataStruct.Point lb = new DataStruct.Point(1, img.Height - 1);
                DataStruct.Point rt = new DataStruct.Point(img.Width - 1, 1);
                DataStruct.Point rb = new DataStruct.Point(img.Width - 1, img.Height - 1);
                Errortype ret = ComAlgo.CalcLightCenter(img, lt, rb, out DataStruct.Point ltrbCenter, out List<DataStruct.Point> ltrbCrossPoints, out List<double> ltrbGray, Sigma);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show("LTRB Error: %s", ret.ToString());
                }

                LTRBResult = ltrbCenter.ToString(" ", 3);

                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(ltrbCenter.X, ltrbCenter.Y), 400);
                for (int i = 0; i < ltrbCrossPoints.Count; i++)
                {
                    ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(ltrbCrossPoints[i].X, ltrbCrossPoints[i].Y), 200);
                }
            }
            catch (Exception e)
            {
                MaxwellControl.Controls.MessageBox.Show(e.Message);
            }
        }

        public void CalcLbrt()
        {
            try
            {
                _thisView.ImagePanel.Items.Clear();

                if (Image is null)
                {
                    MaxwellControl.Controls.MessageBox.Show("Image is Empty");
                    return;
                }

                Camera img = ImageHelper.GetCamera(Image);

                DataStruct.Point lt = new DataStruct.Point(1, 1);
                DataStruct.Point lb = new DataStruct.Point(1, img.Height - 1);
                DataStruct.Point rt = new DataStruct.Point(img.Width - 1, 1);
                DataStruct.Point rb = new DataStruct.Point(img.Width - 1, img.Height - 1);

                Errortype ret = ComAlgo.CalcLightCenter(img, lb, rt, out DataStruct.Point lbrtCenter, out List<DataStruct.Point> lbrtCrossPoints, out List<double> lbrtGray, Sigma);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show("LBRT Error: %s", ret.ToString());
                }

                LBRTResult = lbrtCenter.ToString(" ", 3);

                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(lbrtCenter.X, lbrtCenter.Y), 400);
                for (int i = 0; i < lbrtCrossPoints.Count; i++)
                {
                    ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(lbrtCrossPoints[i].X, lbrtCrossPoints[i].Y), 200);
                }
            }
            catch (Exception e)
            {
                MaxwellControl.Controls.MessageBox.Show(e.Message);
            }
        }

        public void CalcAreaCenter()
        {
            try
            {
                _thisView.ImagePanel.Items.Clear();

                if (Image is null)
                {
                    MaxwellControl.Controls.MessageBox.Show("Image is Empty");
                    return;
                }

                Camera img = ImageHelper.GetCamera(Image);

                Errortype ret = ComAlgo.CalcLightCenter(img, out DataStruct.Point center);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show("CalcAreaCenter Error: %s", ret.ToString());
                }

                AreaCenterResult = center.ToString(" ", 3);

                ImagePanelOperation.DrawCenterCross(_thisView.ImagePanel, new System.Windows.Point(center.X, center.Y), 400);

            }
            catch (Exception e)
            {
                MaxwellControl.Controls.MessageBox.Show(e.Message);
            }
        }
    }
}
