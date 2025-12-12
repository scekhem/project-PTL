using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using DataStruct;
using HalconDotNet;
using MaxwellFramework.Controls;
using MwFramework.Controls.ImageViewer;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.Caliper;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using Point = DataStruct.Point;

namespace MXVisionAlgorithm.Common
{
    public class OpticDistortService
    {
        private Camera _imgDefault = new Camera();
        private Camera _imgArrow = new Camera();
        private Camera _imgMarkCenter = new Camera();
        private Camera _imgValue = new Camera();
        private static CalibDistort _IntrinsicCalib = new CalibDistort();

        public Errortype CalcImgGridMark(Camera img, double markAreaMin, double markAreaMax, out List<Point> centerPixPoint)
        {
            if (_imgMarkCenter.Width > 0) _imgMarkCenter.Dispose();
            if (_imgArrow.Width > 0) _imgArrow.Dispose();
            _imgDefault = img.Clone();
            _imgMarkCenter = img.Clone();
            centerPixPoint = new List<Point>();
            List<Point> circleCenter = new List<Point>();
            List<double> circleRadius = new List<double>();
            HObject h_Image = img.GenHObject();
            HOperatorSet.Rgb1ToGray(h_Image, out HObject ho_Image);
            h_Image.Dispose();

            HTuple hv_RectX;
            HTuple hv_RectY;

            HOperatorSet.BinaryThreshold(ho_Image, out HObject ho_darkRegion, "max_separability", "dark", out HTuple hv_usedthresh);
            HOperatorSet.Connection(ho_darkRegion, out HObject ho_connectedDarkRegion);

            HOperatorSet.SelectShape(ho_connectedDarkRegion, out HObject ho_selectedRegion, "area", "and", markAreaMin, markAreaMax);
            HOperatorSet.CountObj(ho_selectedRegion, out HTuple numOfAlmostRegions);
            if (numOfAlmostRegions.I < 1)
            {
                ho_Image.Dispose();
                ho_darkRegion.Dispose();
                ho_connectedDarkRegion.Dispose();
                ho_selectedRegion.Dispose();
                return Errortype.COMMONALGO_MARK_SEARCH_FAIL;
            }

            HOperatorSet.SmallestRectangle2(ho_selectedRegion, out hv_RectY, out hv_RectX, out HTuple rectPhi, out HTuple hv_RectRY, out HTuple hv_RectRX);

            HOperatorSet.OpenWindow(0, 0, img.Width, img.Height, 0, "invisible", "", out HTuple windowHandle);
            HOperatorSet.SetColored(windowHandle, 12);
            HOperatorSet.SetLineWidth(windowHandle, 2);
            HOperatorSet.SetPart(windowHandle, 0, 0, img.Height - 1, img.Width - 1);

            HOperatorSet.GenCrossContourXld(out HObject centerCross, hv_RectY, hv_RectX, hv_RectRX[0], 0.785398);
            HOperatorSet.DispImage(ho_Image, windowHandle);
            HOperatorSet.DispObj(centerCross, windowHandle);

            HOperatorSet.DumpWindowImage(out HObject circleImage, windowHandle);
            //HOperatorSet.WriteImage(circleImage,"bmp",0,"./res.bmp");

            _imgMarkCenter = new Camera(circleImage);

            centerCross.Dispose();
            //ho_Image.Dispose();
            ho_darkRegion.Dispose();
            ho_connectedDarkRegion.Dispose();
            ho_selectedRegion.Dispose();

            HOperatorSet.TupleGreaterElem(hv_RectRX, 4.0, out HTuple greaterY);
            HOperatorSet.TupleGreaterElem(hv_RectRY, 4.0, out HTuple greaterX);
            HOperatorSet.TupleAnd(greaterY, greaterX, out HTuple greaterRect);
            HOperatorSet.TupleSelectMask(hv_RectY, greaterRect, out HTuple selectedCenterY);
            HOperatorSet.TupleSelectMask(hv_RectX, greaterRect, out HTuple selectedCenterX);
            HOperatorSet.TupleSelectMask(hv_RectRX, greaterRect, out HTuple selectedRX);
            HOperatorSet.TupleSelectMask(hv_RectRY, greaterRect, out HTuple selectedRY);
            HOperatorSet.TupleSelectMask(rectPhi, greaterRect, out HTuple selectedRectPhi);
            if (selectedRectPhi.Length < 9) return Errortype.COMMONALGO_MARK_SEARCH_FAIL;

            HOperatorSet.CreateMetrologyModel(out HTuple metrologyHandle);
            HOperatorSet.SetMetrologyModelImageSize(metrologyHandle, img.Width, img.Height);
            HOperatorSet.AddMetrologyObjectRectangle2Measure(metrologyHandle, selectedCenterY, selectedCenterX,
                selectedRectPhi, selectedRY, selectedRX, 2.5, 1.5, 1, 30, new HTuple(), new HTuple(),
                out HTuple metroIndex);
            HOperatorSet.SetMetrologyObjectParam(metrologyHandle, metroIndex, "num_measures", 50);
            HOperatorSet.ApplyMetrologyModel(ho_Image, metrologyHandle);
            HOperatorSet.GetMetrologyObjectResult(metrologyHandle, metroIndex, "all", "result_type", "all_param", out HTuple rectParam);
            HTuple hv_Sequence = HTuple.TupleGenSequence(0, (new HTuple(rectParam.TupleLength())) - 1, 5);
            HTuple rectCenterY = rectParam.TupleSelect(hv_Sequence);
            HTuple rectCenterX = rectParam.TupleSelect(hv_Sequence + 1);
            for (int resId = 0; resId < rectCenterY.Length; resId++)
            {
                centerPixPoint.Add(new Point(rectCenterX[resId], rectCenterY[resId]));
            }
            HOperatorSet.ClearMetrologyModel(metrologyHandle);

            // todo：显示圆 mark
            //HOperatorSet.GenCircle(out HObject circleRoi, hv_CircleY, hv_CircleX, hv_CircleR1/3);
            //HOperatorSet.DispObj(circleRoi, windowHandle);
            //circleRoi.Dispose();
            //HOperatorSet.SetPart(windowHandle,0,0, img.Height-1, img.Width-1);

            circleImage.Dispose();
            ho_Image.Dispose();
            HOperatorSet.CloseWindow(windowHandle);
            return new Errortype();
        }

        public Errortype CalcImgCircleMark(Camera img, double markAreaMin, double markAreaMax, out List<Point> centerPixPoint)
        {
            if (_imgMarkCenter.Width > 0) _imgMarkCenter.Dispose();
            if (_imgArrow.Width > 0) _imgArrow.Dispose();
            _imgDefault = img.Clone();
            _imgMarkCenter = img.Clone();
            centerPixPoint = new List<Point>();
            List<Point> circleCenter = new List<Point>();
            List<double> circleRadius = new List<double>();
            HObject h_Image = img.GenHObject();
            HOperatorSet.Rgb1ToGray(h_Image, out HObject ho_Image);
            h_Image.Dispose();

            HTuple hv_CircleX;
            HTuple hv_CircleY;

            HOperatorSet.BinaryThreshold(ho_Image, out HObject ho_darkRegion, "max_separability", "light", out HTuple hv_usedthresh);
            HOperatorSet.Connection(ho_darkRegion, out HObject ho_connectedLightRegion);
            //HOperatorSet.OpeningCircle(ho_connectedLightRegion, out ho_connectedLightRegion, 5);     // 开运算radius目前暂定10

            // 筛选圆形区域
            HOperatorSet.SelectShape(ho_connectedLightRegion, out HObject ho_selectedRegion, "circularity", "and", 0.9, 1);
            HOperatorSet.SelectShape(ho_selectedRegion, out ho_selectedRegion, "area", "and", markAreaMin, markAreaMax);
            HOperatorSet.CountObj(ho_selectedRegion, out HTuple numOfAlmostRegions);
            if (numOfAlmostRegions.I < 1)
            {
                ho_Image.Dispose();
                ho_darkRegion.Dispose();
                ho_connectedLightRegion.Dispose();
                ho_selectedRegion.Dispose();
                return Errortype.COMMONALGO_MARK_SEARCH_FAIL;
            }

            HOperatorSet.SmallestRectangle2(ho_selectedRegion, out hv_CircleY, out hv_CircleX, out HTuple hv_CirlclePhi, out HTuple hv_CircleR1, out HTuple hv_CircleR2);

            HOperatorSet.OpenWindow(0, 0, img.Width, img.Height, 0, "invisible", "", out HTuple windowHandle);
            HOperatorSet.SetColored(windowHandle, 12);
            HOperatorSet.SetLineWidth(windowHandle, 2);
            HOperatorSet.SetPart(windowHandle, 0, 0, img.Height - 1, img.Width - 1);

            HOperatorSet.GenCrossContourXld(out HObject centerCross, hv_CircleY, hv_CircleX, hv_CircleR1[0], 0.785398);
            HOperatorSet.DispImage(ho_Image, windowHandle);
            HOperatorSet.DispObj(centerCross, windowHandle);
            //HOperatorSet.DispText(windowHandle,"aaaaaa","image",100,100,"red", new HTuple(),new HTuple());

            centerCross.Dispose();
            ho_Image.Dispose();
            ho_darkRegion.Dispose();
            ho_connectedLightRegion.Dispose();
            ho_selectedRegion.Dispose();



            for (int index = 0; index < hv_CircleY.TupleLength(); index++)
            {
                circleCenter.Add(new Point(hv_CircleX[index].D, hv_CircleY[index].D));
                circleRadius.Add(hv_CircleR1.D);
            }
            CaliperParams calipParam = new CaliperParams();
            calipParam.NumInstances = 1;
            calipParam.MinScore = 0.9;
            calipParam.MeasureSigma = 3;
            calipParam.MeasureLength1 = 10;
            calipParam.MeasureLength2 = 3;
            calipParam.NumMeasures = 120;
            calipParam.MeasureThreshold = 30;
            calipParam.MeasureSelect = "all";
            calipParam.MeasureInterpolation = "nearest_neighbor";
            calipParam.MeasureTransition = "negative";

            var ret = CaliperCircle.CircleExtraction(img, circleCenter, circleRadius, calipParam, out centerPixPoint, out List<double> radius);
            if (ret != Errortype.OK) return ret;
            // todo：显示圆 mark
            //HOperatorSet.GenCircle(out HObject circleRoi, hv_CircleY, hv_CircleX, hv_CircleR1/3);
            //HOperatorSet.DispObj(circleRoi, windowHandle);
            //circleRoi.Dispose();
            HOperatorSet.SetPart(windowHandle, 0, 0, img.Height - 1, img.Width - 1);
            HOperatorSet.DumpWindowImage(out HObject circleImage, windowHandle);

            _imgMarkCenter = new Camera(circleImage);
            circleImage.Dispose();
            HOperatorSet.CloseWindow(windowHandle);
            return new Errortype();
        }

        public Errortype CalcDistort(Camera img, List<Point> markPixPoint, List<Point> markRealPoint,
            out List<double> distortRate)
        {

            distortRate = new List<double>();
            List<String> calibDistortName = new List<string>() { "imgDistort" };
            _IntrinsicCalib.Init(calibDistortName);
            _IntrinsicCalib.SetImageSize(calibDistortName[0], img.Height, img.Width);
            _IntrinsicCalib.CalibCamDistort(calibDistortName[0], markPixPoint, markRealPoint, out _);
            _IntrinsicCalib.CalcOpticDistortRate(calibDistortName[0], out distortRate);
            _IntrinsicCalib.DistortCorrect(calibDistortName[0], markPixPoint, out List<Point> correctPoint, out _);
            _IntrinsicCalib.Release();

            GenDistortImage(img, markPixPoint, correctPoint, distortRate);


            return Errortype.OK;
        }

        public Errortype CalcDistortHL(Camera img, List<Point> markPixPoint, List<Point> markRealPoint, out List<double> distortRate)
        {
            double[] markPointRow = new double[markPixPoint.Count];
            double[] markPointCol = new double[markPixPoint.Count];
            double[] idRow = new double[markPixPoint.Count];
            double[] idCol = new double[markPixPoint.Count];
            distortRate = new List<double>();
            HOperatorSet.GenEmptyObj(out HObject lines);
            for (int i = 0; i < markPixPoint.Count; i++)
            {
                markPointCol[i] = markPixPoint[i].X;
                markPointRow[i] = markPixPoint[i].Y;
                idRow[i] = markRealPoint[i].Y;
                idCol[i] = markRealPoint[i].X;
            }
            HOperatorSet.TupleMax(idRow, out HTuple maxRowId);
            HOperatorSet.TupleMax(idCol, out HTuple maxColId);

            HOperatorSet.TupleLessElem(idRow, maxRowId, out HTuple idLessRow);
            HOperatorSet.TupleLessElem(idCol, maxColId, out HTuple idLessCol);
            HOperatorSet.TupleGreaterElem(idRow, 0, out HTuple idGreatRow);
            HOperatorSet.TupleGreaterElem(idCol, 0, out HTuple idGreatcol);
            HTuple idMask = idLessRow * idLessCol * idGreatRow * idGreatcol;
            // 横向线段
            for (int currrentRow = 1; currrentRow < maxRowId - 1; currrentRow++)
            {
                HOperatorSet.TupleEqualElem(idRow, currrentRow, out HTuple currentRowMask);
                HOperatorSet.TupleSelectMask(markPointRow, currentRowMask * idMask, out HTuple selectedMarkRow);
                HOperatorSet.TupleSelectMask(markPointCol, currentRowMask * idMask, out HTuple selectedMarkCol);
                if (selectedMarkCol.Length > 6)
                {
                    HOperatorSet.GenContourNurbsXld(out HObject oneLine, selectedMarkRow, selectedMarkCol, "auto", "auto", 3, 5, 4);
                    HOperatorSet.ConcatObj(lines, oneLine, out lines);
                    oneLine.Dispose();
                }
            }
            // 竖向线段
            for (int currrentCol = 1; currrentCol < maxColId - 1; currrentCol++)
            {
                HOperatorSet.TupleEqualElem(idCol, currrentCol, out HTuple currentColMask);
                HOperatorSet.TupleSelectMask(markPointRow, currentColMask * idMask, out HTuple selectedMarkRow);
                HOperatorSet.TupleSelectMask(markPointCol, currentColMask * idMask, out HTuple selectedMarkCol);
                if (selectedMarkCol.Length > 6)
                {
                    HOperatorSet.GenContourNurbsXld(out HObject oneLine, selectedMarkRow, selectedMarkCol, "auto", "auto", 3, 5, 4);
                    HOperatorSet.ConcatObj(lines, oneLine, out lines);
                    oneLine.Dispose();
                }
            }
            HOperatorSet.CountObj(lines, out HTuple lineNum);
            if (lineNum < 6) return Errortype.COMMONALGO_MARK_SEARCH_FAIL;
            HOperatorSet.RadialDistortionSelfCalibration(lines, out _, img.Width, img.Height, 0.2, 42, "division",
                "adaptive", 50, out HTuple camParam);
            lines.Dispose();

            var distortCenterRow = camParam[5];
            var distortCenterCol = camParam[6];
            double[] cornerRow = new double[4] { 0.0, img.Height - 1, 0.0, img.Height - 1 };
            double[] cornerCol = new double[4] { 0.0, 0.0, img.Width - 1, img.Width - 1 };
            HOperatorSet.ChangeRadialDistortionCamPar("fixed", camParam, 0, out HTuple camParamOut);
            HOperatorSet.ChangeRadialDistortionPoints(markPointRow, markPointCol, camParam, camParamOut, out HTuple correctRow, out HTuple correctCol);
            HOperatorSet.ChangeRadialDistortionPoints(cornerRow, cornerCol, camParam, camParamOut, out HTuple correctCornerRow, out HTuple correctCornerCol);

            HOperatorSet.TuplePow(cornerRow - correctCornerRow, 2, out HTuple squareRow);
            HOperatorSet.TuplePow(cornerCol - correctCornerCol, 2, out HTuple squareCol);
            HOperatorSet.TupleSqrt(squareCol + squareRow, out HTuple distDiffConer);

            HOperatorSet.TuplePow(correctCornerRow - distortCenterRow, 2, out HTuple squareCenterRow);
            HOperatorSet.TuplePow(correctCornerCol - distortCenterCol, 2, out HTuple squareCenterCol);
            HOperatorSet.TupleSqrt(squareCenterCol + squareCenterRow, out HTuple distCenter);

            var distortValue = distDiffConer / distCenter;
            distortRate = distortValue.DArr.ToList();
            List<Point> correctPoint = new List<Point>();
            for (int i = 0; i < correctRow.Length; i++)
            {
                correctPoint.Add(new Point(correctCol[i], correctRow[i]));
            }

            GenDistortImage(img, markPixPoint, correctPoint, distortRate);

            return Errortype.OK;


        }

        private Errortype GenDistortImage(Camera img, List<Point> markPixPoint, List<Point> correctPoint,
            List<double> distortRate)
        {
            if (_imgValue.Width > 0) _imgValue.Dispose();
            if (_imgArrow.Width > 0) _imgArrow.Dispose();
            HObject ho_Image = _imgMarkCenter.GenHObject();
            HOperatorSet.OpenWindow(0, 0, img.Width, img.Height, 0, "invisible", "", out HTuple windowHandle);
            HOperatorSet.SetColored(windowHandle, 12);
            HOperatorSet.SetPart(windowHandle, 0, 0, img.Height - 1, img.Width - 1);
            HOperatorSet.DispImage(ho_Image, windowHandle);

            HOperatorSet.DispText(windowHandle, (distortRate[0] * 100).ToString("0.000") + "%", "image", 30, 10,
                "red", new HTuple(), new HTuple());
            HOperatorSet.DispText(windowHandle, (distortRate[1] * 100).ToString("0.000") + "%", "image", img.Height - 40, 10,
                "red", new HTuple(), new HTuple());
            HOperatorSet.DispText(windowHandle, (distortRate[2] * 100).ToString("0.000") + "%", "image", 30, img.Width - 60,
                "red", new HTuple(), new HTuple());
            HOperatorSet.DispText(windowHandle, (distortRate[3] * 100).ToString("0.000") + "%", "image", img.Height - 40, img.Width - 60,
                "red", new HTuple(), new HTuple());

            HOperatorSet.DumpWindowImage(out HObject valueImage, windowHandle);
            _imgValue = new Camera(valueImage);


            double[] markPointRow = new double[markPixPoint.Count];
            double[] markPointCol = new double[markPixPoint.Count];
            double[] correctPointRow = new double[markPixPoint.Count];
            double[] correctPointCol = new double[markPixPoint.Count];
            for (int i = 0; i < markPixPoint.Count; i++)
            {
                markPointRow[i] = (markPixPoint[i].Y - correctPoint[i].Y) * 10 + correctPoint[i].Y;
                markPointCol[i] = (markPixPoint[i].X - correctPoint[i].X) * 10 + correctPoint[i].X;
                correctPointRow[i] = correctPoint[i].Y;
                correctPointCol[i] = correctPoint[i].X;
            }

            HOperatorSet.DispArrow(windowHandle, correctPointRow, correctPointCol, markPointRow, markPointCol, 1.5);


            HOperatorSet.DumpWindowImage(out HObject arrowImage, windowHandle);
            _imgArrow = new Camera(arrowImage);

            ho_Image.Dispose();
            valueImage.Dispose();
            arrowImage.Dispose();
            HOperatorSet.CloseWindow(windowHandle);

            return Errortype.OK;
        }


        public Camera ImageArrow
        {
            get { return _imgArrow; }
        }

        public Camera ImageMarkCenter
        {
            get { return _imgMarkCenter; }
        }

        public Camera ImageDistortResult
        {
            get { return _imgValue; }
        }


        public Errortype Release()
        {
            if (_imgArrow.Width > 0) _imgArrow.Dispose();
            if (_imgMarkCenter.Width > 0) _imgMarkCenter.Dispose();
            if (_imgValue.Width > 0) _imgValue.Dispose();
            return Errortype.OK;
        }
    }
}
