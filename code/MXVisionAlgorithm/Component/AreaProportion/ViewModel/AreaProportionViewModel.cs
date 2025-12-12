using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using MaxwellFramework.Core.Interfaces;
using MwFramework.Controls.ControlCanvas.Model;
using MXVisionAlgorithm.Common;
using MX10UBDBU01AA.Common;
using HalconDotNet;
using MessageBox = System.Windows.MessageBox;
using DataStruct;
using MXVisionAlgorithm.Component.AreaProportion.View;
using Point = System.Windows.Point;
using System.Threading;

namespace MXVisionAlgorithm.Component.AreaProportion.ViewModel
{
    public class AreaProportionViewModel : AbstractBaseViewModel, IPage
    {
       
        public WriteableBitmap _image;//控件中原图
        private int _areaWafer = 0;//wafer面积
        private int _areaBubble = 0;//总bubble的面积
        private double _areaRatio = 0.0;//bubble占wafer比例
        private static HObject _imgSource = new HObject(); //原始图像,初始拿到的图
        private List<HObject> _regionSet = new List<HObject>();//存储bubble
        private List<HObject> _regionClear = new List<HObject>();//存储clear 
        private HObject _regionNoWafer = new HObject();//存储noWafer

        /// <summary>
        /// 窗口
        /// </summary>
        public string Name { get; set; } = "AreaProportionView";

        private AreaProportionView _thisView;

        /// <summary>
        /// 加载
        /// </summary>
        protected override void OnViewLoaded()
        {
            _thisView = this.View as AreaProportionView;
        }

        /// <summary>
        /// wafer的面积
        /// </summary>
        public int AreaWafer
        {
            get { return _areaWafer; }
            set
            {
                _areaWafer = value;
                OnPropertyChanged(nameof(AreaWafer));
            }
        }
        
        /// <summary>
        /// 显示在图像上bubble的总面积
        /// </summary>
        public int AreaBubble
        {
            get { return _areaBubble; }
            set
            {
                _areaBubble = value;
                OnPropertyChanged(nameof(AreaBubble));
            }
        }
        
        /// <summary>
        /// bubble面积占wafer面积的比例
        /// </summary>
        public double AreaRatio
        {
            get { return _areaRatio; }
            set
            {
                _areaRatio = value;
                OnPropertyChanged(nameof(AreaRatio));
            }
        }
        
        /// <summary>
        /// 显示在显示区域的图像
        /// </summary>
        public WriteableBitmap Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        /// <summary>
        /// 获得在图中画的传出来的区域
        /// </summary>
        public ObservableCollection<PrimShape> Regions { get; set; } = new ObservableCollection<PrimShape>();

        /// <summary>
        /// 获得原始图像
        /// </summary>
        /// <param name="hImg">输出HObject的图像</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetImgSource( out HObject hImg)
        {
            DataStruct.Camera img = ImageHelper.GetCamera(Image);
            hImg = img.GenHObject();
            _imgSource = hImg.Clone();
            img.Dispose();
            return Errortype.OK;
        }

        /// <summary>
        /// 更新当前图片，其余内容初始化
        /// </summary>
        public void UpDateImageClick()
        {
            if (Image == null)
            {
                MessageBox.Show("请加载图片");
                return;
            }
            
            var imgSou = GetImgSource(out HObject imgSource);
            _regionSet.Clear();
            _regionClear.Clear();
            _regionNoWafer = new HObject();
            AreaBubble = 0;
            AreaRatio = 0;
            AreaWafer = 0;
            if (imgSou == Errortype.OK)
            {
                MessageBox.Show("更新成功！");
                return;
            }

        }

        /// <summary>
        /// 计算框选的wafer面积
        /// </summary>
        public void SetWaferClick()
        {
            if (Image is null)
            {
                MessageBox.Show("请加载图片");
                return;
            }

            if (Regions.Count == 0)
            {
                MessageBox.Show("请框选wafer区域");
                return;
            }

            HObject imgSource;
            if (AreaWafer == 0)
            {
                var imgSou = GetImgSource(out  imgSource);
            }
            else
            {
                imgSource = _imgSource;
            }

            var ret =GetRegion(out HObject region);
            ret = LocateWafer(imgSource,region,out HObject noWafer, out HTuple areaWafer);
            var retImg = ScaleImgPart(imgSource, noWafer, 0.5, out Camera outImgWafer);
            Image = ImageHelper.Camera2WritableBitmap(outImgWafer);
            _regionNoWafer = noWafer;
            AreaWafer = (int)areaWafer;
            _regionClear.Clear();
            Regions.Clear();
        }

        /// <summary>
        /// 计算框选的气泡面积
        /// </summary>
        public void SetBubbleClick()
        {
            if (Image is null)
            {
                MessageBox.Show("请加载图片");
                return;
            }

            if (AreaWafer == 0)
            {
                MessageBox.Show("请先确定wafer");
                Regions.Clear();
                return;
            }

            if (Regions.Count == 0)
            {
                MessageBox.Show("请框选bubble");
                return;
            }

            HOperatorSet.GenEmptyObj(out HObject regionSets);
            var ret = GetRegion(out HObject region);
            _regionSet.Add(region);
            foreach (var iRegion in _regionSet)
            {
                HOperatorSet.Union2(regionSets, iRegion, out regionSets);
            }

            HOperatorSet.Union2(regionSets, _regionNoWafer, out HObject setAndNoWafer);
            ret = ScaleImgPart(_imgSource, setAndNoWafer, 0.5, out Camera outImg);
            Image = ImageHelper.Camera2WritableBitmap(outImg);
            HOperatorSet.AreaCenter(regionSets, out HTuple areaAll, out HTuple row, out HTuple col);
            AreaBubble = areaAll;
            Regions.Clear();
            _regionClear.Clear();
        }

        /// <summary>
        /// 清除框选气泡
        /// </summary>
        public void CleanBubbleClick()
        {
            if (Image is null)
            {
                MessageBox.Show("请加载图片");
                return;
            }

            if (AreaWafer == 0)
            {
                MessageBox.Show("请先确定Wafer");
                Regions.Clear();
                return;
            }

            if (Regions.Count == 0)
            {
                MessageBox.Show("请框选clearBubble区域");
                return;
            }

            HOperatorSet.GenEmptyObj(out HObject regionSets);
            HOperatorSet.GenEmptyObj(out HObject regionClears);
            foreach (var iRegion in _regionSet)
            {
                HOperatorSet.Union2(regionSets, iRegion, out regionSets);
            }

            var ret = GetRegion( out HObject regionClear);
            _regionClear.Add(regionClear);
            foreach (var iRegion in _regionClear)
            {
                HOperatorSet.Union2(regionClears, iRegion, out regionClears);
            }

            HOperatorSet.Difference(regionSets, regionClears, out HObject regionClearD);
            _regionSet.Clear();
            _regionSet.Add(regionClearD);
            HOperatorSet.AreaCenter(regionClearD, out HTuple areaAll, out HTuple row, out HTuple col);
            AreaBubble = areaAll;
            HOperatorSet.Union2(regionClearD,_regionNoWafer,out HObject diffUnionNoWafer);
            var retImgEnd = ScaleImgPart(_imgSource, diffUnionNoWafer, 0.5, out Camera outImg);
            Image = ImageHelper.Camera2WritableBitmap(outImg);
            Regions.Clear();
            _regionClear.Clear();
        }

        /// <summary>
        /// 计算气泡面积占比
        /// </summary>
        public void AreaRatioClick()
        {
            if (Image is null)
            {
                MessageBox.Show("请加载图片");
                return;
            }

            if (AreaWafer == 0)
            {
                MessageBox.Show("请先确定Wafer");
                Regions.Clear();
                return;
            }
            
            double ratio = (double)AreaBubble / (double)AreaWafer;
            AreaRatio = ratio;
        }

        ///<summary>
        ///获取图片上画的区域
        /// </summary>
        /// <param name="region">输出单个区域的面积</param>
        public Errortype GetRegion(out HObject region)
        {
            if (Image is null)
            {
                MessageBox.Show("请加载图片");
                HOperatorSet.GenEmptyObj(out region);
                return Errortype.OK;//应该返回一个错误类型
            }

            List<HObject> regionList = new List<HObject>();
            HOperatorSet.GenEmptyObj(out region);
            List<DataStruct.Point> polygonPointsOut = new List<DataStruct.Point>();
            Rectangle1 rectangle = new Rectangle1();
            foreach (var item in Regions)
            {
                if (item.Region is PolygonRegion)
                {
                    PolygonRegion PolygonRegion = item.Region as PolygonRegion;
                    foreach (var itemPolygon in PolygonRegion.LineCollection)
                    {
                        System.Windows.Shapes.Line line = new System.Windows.Shapes.Line();
                        line.X1 = itemPolygon.X1 * _thisView.ImagePanel.WidthRatio;
                        line.X2 = itemPolygon.X2 * _thisView.ImagePanel.WidthRatio;
                        line.Y1 = itemPolygon.Y1 * _thisView.ImagePanel.HeightRatio;
                        line.Y2 = itemPolygon.Y2 * _thisView.ImagePanel.HeightRatio;
                        polygonPointsOut.Add(new DataStruct.Point(line.X1, line.Y1));
                        polygonPointsOut.Add(new DataStruct.Point(line.X2, line.Y2));
                    }

                    polygonPointsOut = polygonPointsOut.Where((x, x_index) => x_index == polygonPointsOut.FindIndex(y => y == x)).ToList();

                    //计算单个polygonPoint的面积
                    double[] rowPolygon = new double[polygonPointsOut.Count];
                    double[] colPolygon = new double[polygonPointsOut.Count];
                    for (int i = 0; i < polygonPointsOut.Count; i++)
                    {
                        if (polygonPointsOut[i] is null || polygonPointsOut[i] is null)
                        {
                            continue;
                        }

                        rowPolygon[i] = polygonPointsOut[i].Y;
                        colPolygon[i] = polygonPointsOut[i].X;
                    }

                    HOperatorSet.GenRegionPolygonFilled(out HObject polygon, rowPolygon, colPolygon);
                    regionList.Add(polygon);
                }
                else if (item.Region is CircleRegion)
                {
                    CircleRegion circleRegion = item.Region as CircleRegion;
                    Point centerPointT = circleRegion.CenterPoint;
                    double radius = circleRegion.Radius;
                    HOperatorSet.GenCircle(out HObject circle, centerPointT.Y, centerPointT.X, radius);
                    regionList.Add(circle);
                }
                else if (item.Region is RectRegion rect)
                {

                    rectangle.Start_X = rect.CenterPoint.X - rect.Width / 2;
                    rectangle.Start_Y = rect.CenterPoint.Y - rect.Height / 2;
                    rectangle.End_X = rectangle.Start_X + rect.Width;
                    rectangle.End_Y = rectangle.Start_Y + rect.Height;
                    HOperatorSet.GenRectangle2(out HObject rectangleHObject, rect.CenterPoint.Y, rect.CenterPoint.X, rect.Angle, rect.Width / 2, rect.Height / 2);
                    regionList.Add(rectangleHObject);
                }
            }

            foreach (var iRegion in regionList)
            {
                HOperatorSet.Union2(region, iRegion, out region);
            }

            return Errortype.OK;
        }
        
        /// <summary>
        /// 对图像的局部区域进行亮度变化,可以用来显示画的区域和用来清除画的区域
        /// </summary>
        /// <param name="img">输入图像</param>
        /// <param name="region">局部区域</param>
        /// <param name="scale">变化比例，0-1为降低亮度  1-∞为增加亮度</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype ScaleImgPart(HObject img, HObject region, double scale, out Camera outImgWafer)
        {
           
            if (img == null)
            {
                outImgWafer = new Camera();
                return Errortype.INPUT_NULL;
            }

            outImgWafer = new Camera();
            if (region == null)
            {
                return Errortype.OK;
            }

            if (!region.IsInitialized())
            {
                return Errortype.OK;
            }

            HOperatorSet.CountObj(region, out HTuple regionNum);
            if (regionNum == 0)
            {
                return Errortype.OK;
            }

            HOperatorSet.CountChannels(img, out HTuple channels);
            HOperatorSet.GetImageSize(img, out HTuple width, out HTuple height);
            HOperatorSet.GenImageConst(out HObject imgScale, "byte", width, height);
            HOperatorSet.ScaleImage(imgScale, out imgScale, 1, 2);
            HOperatorSet.PaintRegion(region, imgScale, out imgScale, scale / 0.5, "fill");
            if (channels == 3)
            {
                HOperatorSet.Compose3(imgScale, imgScale, imgScale, out imgScale);
            }

            HOperatorSet.MultImage(img, imgScale, out HObject imgOut, 0.5, 0);
            outImgWafer = new Camera(imgOut);
            imgScale.Dispose();
            return Errortype.OK;
        }

        ///<summary>
        /// 裁剪wafer
        /// <param name="imgSource">原图</param>
        /// <param name="region">框选的大致wafer</param>
        /// <param name="noWafer">输出noWafer region</param>
        /// </summary>
        public Errortype LocateWafer(HObject imgSource, HObject region,out HObject noWafer,out HTuple areaWafer)
        {
            if (imgSource == null)
            {
                MessageBox.Show("原图传入错误");
                noWafer = new HObject();
                areaWafer = new HTuple();
                return Errortype.OK;
            }

            if (region.CountObj() == 0)
            {
                MessageBox.Show("请框选wafer");
                noWafer = new HObject();
                areaWafer = new HTuple();
                return Errortype.OK;
            }

            HOperatorSet.ReduceDomain(imgSource, region, out HObject imageReduced);
            HOperatorSet.BinaryThreshold(imageReduced, out HObject regionReduced, "max_separability", "dark", out HTuple usedThreshold);
            HOperatorSet.Connection(regionReduced, out HObject connectedRegions);
            HOperatorSet.SelectShapeStd(connectedRegions, out HObject waferNoFill, "max_area", 70);
            HOperatorSet.FillUp(waferNoFill, out HObject waferFill);
            HOperatorSet.AreaCenter(waferFill, out areaWafer,out HTuple rows,out HTuple column);
            HOperatorSet.Difference(imgSource, waferFill, out noWafer);
            imageReduced.Dispose();
            regionReduced.Dispose();
            connectedRegions.Dispose();
            waferNoFill.Dispose();
            waferFill.Dispose();
            return Errortype.OK;
        }
    }
}

