using System;
using System.Collections.Generic;
using IniFileHelper;
using DataStruct;
using HalconDotNet;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using Point = DataStruct.Point;
using System.IO;

namespace UltrapreciseBonding.Calib
{
    #region 相机外参标定,    CamPix2Ruler

    /// <summary>
    /// 像素比标定基类
    /// </summary>
    internal class CalibPixBase : CalibItem
    {
        private double _pixScaleX = 1.0;
        private double _pixScaleY = 1.0;
        private double _pixDeg = 0.0;
        private bool _angleIsCalib = false;
        private Point _currentPixCenter = null;

        /// <summary>
        /// 基类有参构造
        /// </summary>
        /// <param name="opticName">标定名称</param>
        public CalibPixBase(string opticName)
        {
            ItemName = opticName;
            IsCalibed = false;
        }

        /// <summary>
        /// Gets the user's _pixScaleX
        /// </summary>
        public double PixScaleX
        {
            get => _pixScaleX;
        }

        /// <summary>
        /// Gets the user's _pixScaleY
        /// </summary>
        public double PixScaleY
        {
            get => _pixScaleY;
        }

        /// <summary>
        /// Gets the user's _pixDeg
        /// </summary>
        public double PixDeg
        {
            get => _pixDeg;
        }

        /// <summary>
        /// 设置图像像素中心
        /// </summary>
        /// <param name="pixCenter">图像中心点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetPixCenter(Point pixCenter)
        {
            _currentPixCenter = pixCenter;
            return Errortype.OK;
        }

        /// <summary>
        /// 上相机和X轴夹角，逆时针旋转角
        /// </summary>
        /// <param name="pix1">上相机点1</param>
        /// <param name="pix2">上相机点2</param>
        /// <param name="pixDeg">上相机和X轴夹角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibPixAngleX(Point pix1, Point pix2, out double pixDeg)
        {
            pixDeg = 0;
            if (pix1 == pix2)
            {
                return Errortype.OPT_PIX_CALIBRATE_POINTS_ERROR;
            }

            if (pix1 is null || pix2 is null)
            {
                return Errortype.OPT_PIX_CALIBRATE_POINTS_ERROR;
            }

            try
            {
                HOperatorSet.LineOrientation(new HTuple(pix1.Y), new HTuple(pix1.X), new HTuple(pix2.Y), new HTuple(pix2.X), out HTuple curPhi);
                HOperatorSet.TupleDeg(curPhi, out HTuple curDeg);
                _pixDeg = curDeg.D;
                pixDeg = _pixDeg;
                _angleIsCalib = true;
                return Errortype.OK;
            }
            catch (SystemException expDefaultException)
            {
                _pixDeg = 0;
                return Errortype.INTERNAL_ALGO_ERROR;
            }
        }

        /// <summary>
        /// 上相机像素和轴单位比，需要先完成夹角标定
        /// </summary>
        /// <param name="calibAxis">选定标定轴，X或Y</param>
        /// <param name="pix1">像素点1</param>
        /// <param name="pix2">像素点2</param>
        /// <param name="ruler1">轴距离1</param>
        /// <param name="ruler2">轴距离2</param>
        /// <param name="scaleAxis">像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibPixScale(Axis calibAxis, Point pix1, Point pix2, Point ruler1, Point ruler2, out double scaleAxis)
        {
            scaleAxis = 1.0;
            if (!_angleIsCalib)
            {
                Console.WriteLine("camera angle isnot calibrated, " + "camera id:" + ItemName);
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            if (pix1 == pix2)
            {
                return Errortype.CALIBPIX_CALIBRULERSCALE_INPUT_EQUAL;
            }

            if (pix1 is null || pix2 is null || ruler1 is null || ruler2 is null)
            {
                return Errortype.CALIBPIX_CALIBRULERSCALE_INPUT_NULL;
            }

            Point origionPoint = new Point(0, 0);       // 原点
            ComAlgo.CalcRotatePoint(pix1, _pixDeg, origionPoint, out Point startPoint);  // 去除夹角
            ComAlgo.CalcRotatePoint(pix2, _pixDeg, origionPoint, out Point endPoint);    // 去除夹角
            Point pixDistance = endPoint - startPoint;
            Point rulerDistance = ruler1 - ruler2;

            // X像素比
            if (calibAxis == Axis.X)
            {
                double size = rulerDistance.X / pixDistance.X;
                scaleAxis = size;
                IsCalibed = true;
                _pixScaleX = size;
                return Errortype.OK;
            }
            else
            {
                // Y像素比
                double size = rulerDistance.Y / pixDistance.Y;
                scaleAxis = size;
                IsCalibed = true;
                _pixScaleY = size;
                return Errortype.OK;
            }
        }

        /// <summary>
        /// 设置像素比和转角
        /// </summary>
        /// <param name="scaleX">x方向像素比</param>
        /// <param name="scaleY">y方向像素比</param>
        /// <param name="angle">转角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetScaleAngle(double scaleX, double scaleY, double angle)
        {
            _pixScaleX = scaleX;
            _pixScaleY = scaleY;
            _pixDeg = angle;
            _angleIsCalib = true;
            IsCalibed = true;
            return Errortype.OK;
        }

        /// <summary>
        /// 设置像素比
        /// </summary>
        /// <param name="scaleX">x方向像素比</param>
        /// <param name="scaleY">y方向像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetScale(double scaleX, double scaleY)
        {
            _pixScaleX = scaleX;
            _pixScaleY = scaleY;
            IsCalibed = true;
            return Errortype.OK;
        }

        /// <summary>
        /// 从目标位置的像素离中心距离获取某像素位置的光栅坐标，用于Mark逼近
        /// </summary>
        /// <param name="targetPix">目标像素坐标</param>
        /// <param name="currentAxis">当前光栅坐标</param>
        /// <param name="targetAxis">目标轴坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRulerByPix(Point targetPix, Point currentAxis, out Point targetAxis)
        {
            if (_currentPixCenter is null || targetPix is null || currentAxis is null)
            {
                targetAxis = null;
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            if (!_angleIsCalib)
            {
                targetAxis = null;
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            if (!IsCalibed)
            {
                targetAxis = null;
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            // 逆时针旋转角
            Point centerPixPointRotated = _currentPixCenter;
            HTuple phi = Math.PI * _pixDeg / 180;

            // 将像素方向转至与轴平行
            ComAlgo.CalcRotatePoint(targetPix, _pixDeg, centerPixPointRotated, out Point targetPixPointRotated);

            // 注意 move center 起点是 targetMarkPix，终点是图像中心
            double deltaX = (targetPixPointRotated.X - centerPixPointRotated.X) * PixScaleX;
            double deltaY = (targetPixPointRotated.Y - centerPixPointRotated.Y) * PixScaleY;
            Point target = new Point();
            target.X = currentAxis.X + deltaX;
            target.Y = currentAxis.Y + deltaY;
            targetAxis = target;

            return Errortype.OK;
        }

        /// <summary>
        /// 轴坐标转像素坐标
        /// </summary>
        /// <param name="currentAxis">当前轴坐标(对应像素中心)</param>
        /// <param name="targetAxis">需要转换的轴坐标</param>
        /// <param name="targetPix">输出转换后的像素位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetPixByRuler(Point currentAxis, Point targetAxis, out Point targetPix)
        {
            targetPix = null;
            if (currentAxis is null || targetAxis is null || _currentPixCenter is null)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            if (!_angleIsCalib)
            {
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            if (!IsCalibed)
            {
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            HTuple phi = Math.PI * _pixDeg / 180;
            double deltaX = (targetAxis.X - currentAxis.X) / PixScaleX;                         // 轴转像素比例
            double deltaY = (targetAxis.Y - currentAxis.Y) / PixScaleY;
            Point target = new Point();
            target.X = _currentPixCenter.X + deltaX;
            target.Y = _currentPixCenter.Y + deltaY;
            ComAlgo.CalcRotatePoint(target, -_pixDeg, _currentPixCenter, out Point targetPixPointRotated);     // 绕当前图像中心，将轴方向旋转至像素坐标方向
            targetPix = targetPixPointRotated;
            return Errortype.OK;
        }

        /// <summary>
        /// 保存外参数据
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Save(string fileDir)
        {
            if (!IsCalibed)
            {
                return Errortype.CALIBPIX_ISNOT_COMPLET_ERROR;
            }

            if (fileDir is null)
            {
                return Errortype.CALIBPIX_SAVE_FILEPATH_NULL;
            }

            if (fileDir.Length < 1)
            {
                return Errortype.CALIBPIX_SAVE_FILEPATH_EMPTY;
            }

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            if (!Directory.Exists(fileDir))
            {
                return Errortype.CALIBPIX_SAVE_FILE_DIR_NOT_EXIST_ERROR;
            }

            string fullFileName = fileDir + ItemName + "_Extrinsic.ini";

            string path = System.IO.Path.GetDirectoryName(fullFileName);

            List<string> keys = new List<string> { "item_name" };
            List<string> value = new List<string> { ItemName };
            IniHelper.AddSectionWithKeyValues("info", keys, value, fullFileName);
            keys.Clear();
            keys.Add("pix_scale_x");
            keys.Add("pix_scale_y:");
            keys.Add("pixDeg");
            value.Clear();
            value.Add(_pixScaleX.ToString());
            value.Add(_pixScaleY.ToString());
            value.Add(_pixDeg.ToString());
            IniHelper.AddSectionWithKeyValues("scale_pix2ruler", keys, value, fullFileName);
            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Load(string fileDir)
        {
            string fullFileName = fileDir + ItemName + "_Extrinsic.ini";
            if (!IniHelper.ExistSection("scale_pix2ruler", fullFileName))
            {
                return Errortype.PARAMS_FILE_LOAD_ERROR;
            }

            var scalePix2Ruler = IniHelper.GetAllValues("scale_pix2ruler", fullFileName);
            _pixScaleX = Convert.ToDouble(scalePix2Ruler[0]);
            _pixScaleY = Convert.ToDouble(scalePix2Ruler[1]);
            _pixDeg = Convert.ToDouble(scalePix2Ruler[2]);
            IsCalibed = true;
            return Errortype.OK;
        }
    }

    /// <summary>
    /// 像素比标定
    /// </summary>
    public class CalibPix : Singleton<CalibPix>
    {
        private List<CalibPixBase> _opticExtrinsicCalibList = new List<CalibPixBase>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Init(List<string> itemName)
        {
            if (itemName == null)
            {
                return Errortype.CALIBPIX_INIT_ITEMNAME_NULL;
            }

            foreach (var name in itemName)
            {
                CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == name);
                if (opticExternalCalibBase != null)
                {
                    opticExternalCalibBase = new CalibPixBase(name);
                }
                else
                {
                    _opticExtrinsicCalibList.Add(new CalibPixBase(name));
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 设置像素中心
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pixCenter"> 像素中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetPixCenter(string itemName, Point pixCenter)
        {
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticExternalCalibBase.SetPixCenter(pixCenter);
        }

        /// <summary>
        /// 设置像素比和转角
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="scaleX">X方向像素比</param>
        /// <param name="scaleY">y方向像素比</param>
        /// <param name="angle">转角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetScaleAngle(string itemName, double scaleX, double scaleY, double angle)
        {
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticExternalCalibBase.SetScaleAngle(scaleX, scaleY, angle);
        }

        /// <summary>
        /// 设置像素比
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="scaleX">x方向像素比</param>
        /// <param name="scaleY">y方向像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetScale(string itemName, double scaleX, double scaleY)
        {
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticExternalCalibBase.SetScale(scaleX, scaleY);
        }

        /// <summary>
        /// 标定转角
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pix1">像素点1</param>
        /// <param name="pix2">像素点2</param>
        /// <param name="pixDeg">转角结果</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibPixAngle(string itemName, Point pix1, Point pix2, out double pixDeg)
        {
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                pixDeg = 0;
                return Errortype.OPT_NAME_NULL;
            }

            return opticExternalCalibBase.CalibPixAngleX(pix1, pix2, out pixDeg);
        }

        /// <summary>
        /// 获取标定转角
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pixDeg">转角</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetPixAngle(string itemName, out double pixDeg)
        {
            pixDeg = 0;
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            pixDeg = opticExternalCalibBase.PixDeg;
            return Errortype.OK;
        }

        /// <summary>
        /// 标定x方向像素比
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pix1">像素点1</param>
        /// <param name="pix2">像素点2</param>
        /// <param name="ruler1">轴位置1</param>
        /// <param name="ruler2">轴位置2</param>
        /// <param name="pixScaleX">x方向像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibPixScaleX(string itemName, Point pix1, Point pix2, Point ruler1, Point ruler2, out double pixScaleX)
        {
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                pixScaleX = 1.0;
                return Errortype.OPT_NAME_NULL;
            }

            return opticExternalCalibBase.CalibPixScale(Axis.X, pix1, pix2, ruler1, ruler2, out pixScaleX);
        }

        /// <summary>
        /// 获取x方向像素比
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pixScale">x方向像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetPixScaleX(string itemName, out double pixScale)
        {
            pixScale = 0;
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            pixScale = Math.Abs(opticExternalCalibBase.PixScaleX);
            return Errortype.OK;
        }

        /// <summary>
        /// 标定y方向像素比
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pix1">像素点1</param>
        /// <param name="pix2">像素点2</param>
        /// <param name="ruler1">轴位置1</param>
        /// <param name="ruler2">轴位置2</param>
        /// <param name="pixScaleY">y方向像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibPixScaleY(string itemName, Point pix1, Point pix2, Point ruler1, Point ruler2, out double pixScaleY)
        {
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                pixScaleY = 1.0;
                return Errortype.OPT_NAME_NULL;
            }

            return opticExternalCalibBase.CalibPixScale(Axis.Y, pix1, pix2, ruler1, ruler2, out pixScaleY);
        }

        /// <summary>
        /// 获取y方向像素比
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pixScale">y方向像素比</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetPixScaleY(string itemName, out double pixScale)
        {
            pixScale = 0;
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            pixScale = Math.Abs(opticExternalCalibBase.PixScaleY);
            return Errortype.OK;
        }

        /// <summary>
        /// 获取两个像素点间移动的光栅运动量
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="targetPix">目标像素点位置</param>
        /// <param name="currentAxis">当前轴光栅坐标</param>
        /// <param name="targetAxis">目标点光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRulerByPix(string itemName, Point targetPix, Point currentAxis, out Point targetAxis)
        {
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                targetAxis = new Point(0, 0);

                //targetAxis = null;
                return Errortype.OPT_NAME_NULL;
            }

            return opticExternalCalibBase.GetRulerByPix(targetPix, currentAxis, out targetAxis);
        }

        /// <summary>
        /// 获取两个像素点间移动的光栅运动量
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="currentAxis">当前轴光栅位置</param>
        /// <param name="targetAxis">目标轴光栅位置</param>
        /// <param name="targetPix">目标像素点位置</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetPixByRuler(string itemName, Point currentAxis, Point targetAxis, out Point targetPix)
        {
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                targetPix = null;
                return Errortype.OPT_NAME_NULL;
            }

            return opticExternalCalibBase.GetPixByRuler(currentAxis, targetAxis, out targetPix);
        }

        /// <summary>
        /// 获取标定状态
        /// </summary>
        /// <param name="itemName">标定名称</param>
        /// <param name="calibStatus">标定状态</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCalibStatus(string itemName, out bool calibStatus)
        {
            calibStatus = false;
            CalibPixBase opticExternalCalibBase = _opticExtrinsicCalibList.Find(e => e.ItemName == itemName);
            if (opticExternalCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            calibStatus = opticExternalCalibBase.IsCalibed;
            return Errortype.OK;
        }

        /// <summary>
        /// 释放
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _opticExtrinsicCalibList = new List<CalibPixBase>();
            return Errortype.OK;
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <param name="saveReturn">保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string fileDir, out Dictionary<string, Errortype> saveReturn)
        {
            saveReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticExtrinsicCalibList.Count; index++)
            {
                Errortype ret = _opticExtrinsicCalibList[index].Save(fileDir);
                saveReturn.Add(_opticExtrinsicCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <param name="loadReturn">加载结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Load(string fileDir, out Dictionary<string, Errortype> loadReturn)
        {
            loadReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticExtrinsicCalibList.Count; index++)
            {
                Errortype ret = _opticExtrinsicCalibList[index].Load(fileDir);
                loadReturn.Add(_opticExtrinsicCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }
    }

    #endregion
}
