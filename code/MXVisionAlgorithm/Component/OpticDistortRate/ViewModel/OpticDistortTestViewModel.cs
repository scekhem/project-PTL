using MaxwellFramework.Core.Interfaces;
using Microsoft.Win32;
using MX10UBDBU01AA.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataStruct;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using System.Windows.Media.Imaging;
using MXVisionAlgorithm.Component.MarkLocation.View;
using MXVisionAlgorithm.Component.OpticDistortRate.View;
using MXVisionAlgorithm.Common;
using HalconDotNet;

namespace MXVisionAlgorithm.Component.OpticDistortRate.ViewModel
{
    public class OpticDistortTestViewModel : AbstractBaseViewModel, IPage
    {
        private OpticDistortTestView _thisView;
        public string Name { get; set; } = "OpticDistortTestView";

        private OpticDistortService _service = new OpticDistortService();


        private string imageFilePath = string.Empty;
        private string imageFileName = string.Empty;

        private WriteableBitmap _distortImage;

        public WriteableBitmap Image
        {
            get { return _distortImage; }
            set { _distortImage = value; OnPropertyChanged(nameof(Image)); }
        }

        private int _minMarkArea = 100;
        private int _maxMarkArea = 1000;

        private String _leftTopRes = String.Empty;
        private String _leftBottomRes = String.Empty;
        private String _rightTopRes = String.Empty;
        private String _rightBottomRes = String.Empty;

        private bool _isCircleMark = true;

        private List<Point> _markCenterPix = new List<Point>();
        private List<Point> _markCenterReal = new List<Point>();

        public bool IsCircleMark
        {
            get { return _isCircleMark; }
            set
            {
                _isCircleMark = value;
                OnPropertyChanged(nameof(IsCircleMark));
            }
        }


        public int MinMarkArea
        {
            get { return _minMarkArea; }
            set
            {
                _minMarkArea = value;
                OnPropertyChanged(nameof(MinMarkArea));
            }
        }

        public int MaxMarkArea
        {
            get { return _maxMarkArea; }
            set
            {
                _maxMarkArea = value;
                OnPropertyChanged(nameof(MaxMarkArea));
            }
        }

        public String LeftTopRes
        {
            get { return _leftTopRes; }
            set
            {
                _leftTopRes = value;
                OnPropertyChanged(nameof(LeftTopRes));
            }
        }

        public String LeftBottomRes
        {
            get { return _leftBottomRes; }
            set
            {
                _leftBottomRes = value;
                OnPropertyChanged(nameof(LeftBottomRes));
            }
        }

        public String RightBottomRes
        {
            get { return _rightBottomRes; }
            set
            {
                _rightBottomRes = value;
                OnPropertyChanged(nameof(RightBottomRes));
            }
        }

        public String RightTopRes
        {
            get { return _rightTopRes; }
            set
            {
                _rightTopRes = value;
                OnPropertyChanged(nameof(RightTopRes));
            }
        }

        public void LoadImageClick()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image File (*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif)|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog().Value)
            {

                try
                {
                    string ImportFiles = openFileDialog.FileName;
                    //Path.GetDirectoryName()
                    imageFilePath = Path.GetDirectoryName(ImportFiles);
                    imageFileName = Path.GetFileName(ImportFiles);
                    Camera selectedImage = new Camera(ImportFiles);
                    if (selectedImage.Channel > 1)
                    {
                        var himage = selectedImage.GenHObject();
                        HOperatorSet.Rgb1ToGray(himage, out HObject himgGray);
                        selectedImage.Dispose();
                        selectedImage = new Camera(himgGray);
                        himage.Dispose();
                        himgGray.Dispose();
                        Image = ImageHelper.Camera2WritableBitmap(selectedImage);
                        //throw new Exception("CHANNEL ERROR");
                    }

                    Image = ImageHelper.Camera2WritableBitmap(selectedImage);
                    MaxwellControl.Controls.MessageBox.Show("图片加载完成！");
                }
                catch (Exception e)
                {
                    MaxwellControl.Controls.MessageBox.Show("格式不正确！");
                }
            }
        }


        public void SaveImageClick()
        {
            Camera currentView = ImageHelper.GetCamera(Image);

        }

        public void GrabMarkClick()
        {
            if (null == Image) return;
            DataStruct.Camera img = ImageHelper.GetCamera(Image);

            List<Point> markCenters = new List<Point>();
            var ret = Errortype.OK;
            if (_isCircleMark)
            {
                ret = _service.CalcImgCircleMark(img, _minMarkArea, _maxMarkArea, out markCenters);
                Image = ImageHelper.Camera2WritableBitmap(_service.ImageMarkCenter);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show("Mark抓取失败！\n请检查mark类型和大小");
                    return;
                }
            }
            else
            {
                ret = _service.CalcImgGridMark(img, _minMarkArea, _maxMarkArea, out markCenters);
                Image = ImageHelper.Camera2WritableBitmap(_service.ImageMarkCenter);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show("Mark抓取失败！\n请检查mark类型和大小");
                    return;
                }
            }

            //  1.排序并获取行列像素间隔
            //ret = ComAlgo.SortRowColumn(markCenters, out List<Point> sortedCenterPix, out long[] sortOrder, out long[] rowIdAfterSort, out long[] colIdAfterSort);

            ret = ComAlgo.SortRowGroups(markCenters, out long[] sortedId, out double[] rowId, Math.Sqrt(_minMarkArea), 0.0, true);
            ret = ComAlgo.SortRowGroups(markCenters, out _, out double[] colId, Math.Sqrt(_minMarkArea), 0.0, false);

            if (ret != Errortype.OK) return;


            List<Point> sortedCenterPix = new List<Point>();
            List<Point> markCenterReal = new List<Point>();
            HOperatorSet.TupleSelect(rowId, sortedId, out HTuple realRow);
            HOperatorSet.TupleSelect(colId, sortedId, out HTuple realCol);
            for (int index = 0; index < sortedId.Length; index++)
            {
                HOperatorSet.TupleFindFirst(sortedId, index, out HTuple findId);
                markCenterReal.Add(new Point(realCol[index], realRow[index]));
                sortedCenterPix.Add(markCenters[findId]);
            }

            ////  3.根据输入真值和行列间隔计算所有点真值
            //List<Point> markCenterReal = new List<Point>();
            //long rowBias = 0;
            //long colBias = 0;

            //for (int index = 0; index < sortedCenterPix.Count; index++)
            //{
            //    double realValueX = colIdAfterSort[index] + colBias;
            //    double realValueY = rowIdAfterSort[index] + rowBias;
            //    markCenterReal.Add(new Point(realValueX, realValueY));
            //}

            _markCenterPix = sortedCenterPix;
            _markCenterReal = markCenterReal;
            LeftTopRes = String.Empty;
            LeftBottomRes = String.Empty;
            RightTopRes = String.Empty;
            RightBottomRes = String.Empty;
        }

        public void CalcDistortClick()
        {
            if (null == Image) return;
            DataStruct.Camera img = ImageHelper.GetCamera(Image);
            //_service.CalcDistort(img, _markCenterPix, _markCenterReal, out List<double> distortRate);
            var ret = _service.CalcDistortHL(img, _markCenterPix, _markCenterReal, out List<double> distortRate);
            if (ret != Errortype.OK)
            {
                MaxwellControl.Controls.MessageBox.Show("畸变计算失败！\nMark数量不足");
                return;
            }
            LeftTopRes = (distortRate[0] * 100).ToString("0.000") + "%";
            LeftBottomRes = (distortRate[1] * 100).ToString("0.000") + "%";
            RightTopRes = (distortRate[2] * 100).ToString("0.000") + "%";
            RightBottomRes = (distortRate[3] * 100).ToString("0.000") + "%";
            Image = ImageHelper.Camera2WritableBitmap(_service.ImageDistortResult);
        }

        public void DrawArrowClick()
        {
            if (_service.ImageArrow.Height > 1)
            {
                Image = ImageHelper.Camera2WritableBitmap(_service.ImageArrow);
            }
        }



    }
}
