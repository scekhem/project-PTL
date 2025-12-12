using DataStruct;
using IniFileHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltrapreciseBonding.Calib;

namespace UltrapreciseBonding.Calib
{
    /// <summary>
    /// XY方向比例标定基类
    /// </summary>
    internal class CalibPixScaleBase : CalibItem
    {
        private double _scaleRxPx = 1.0;
        private double _scaleRxPy = 1.0;
        private double _scaleRyPx = 1.0;
        private double _scaleRyPy = 1.0;
        private double _scalePxRx = 1.0;
        private double _scalePyRx = 1.0;
        private double _scalePxRy = 1.0;
        private double _scalePyRy = 1.0;
        private bool _axisXIsCalibrated = false;
        private bool _axisYIsCalibrated = false;
        private Point _pixCenter = new Point();

        /// <summary>
        /// 有参构造函数
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        public CalibPixScaleBase(string opticName)
        {
            ItemName = opticName;
            IsCalibed = false;
            _axisXIsCalibrated = false;
            _axisYIsCalibrated = false;
        }

        /// <summary>
        /// Gets or sets the user's _scaleRxPx
        /// </summary>
        public double ScaleRx2Px { get => _scaleRxPx; set => _scaleRxPx = value; }

        /// <summary>
        /// Gets or sets the user's _scaleRxPy
        /// </summary>
        public double ScaleRx2Py { get => _scaleRxPy; set => _scaleRxPy = value; }

        /// <summary>
        /// Gets or sets the user's _scaleRyPx
        /// </summary>
        public double ScaleRy2Px { get => _scaleRyPx; set => _scaleRyPx = value; }

        /// <summary>
        /// Gets or sets the user's _scaleRyPy
        /// </summary>
        public double ScaleRy2Py { get => _scaleRyPy; set => _scaleRyPy = value; }

        /// <summary>
        /// Gets or sets the user's _scalePxRx
        /// </summary>
        public double ScalePx2Rx { get => _scalePxRx; set => _scalePxRx = value; }

        /// <summary>
        /// Gets or sets the user's _scalePyRx
        /// </summary>
        public double ScalePy2Rx { get => _scalePyRx; set => _scalePyRx = value; }

        /// <summary>
        /// Gets or sets the user's _scalePxRy
        /// </summary>
        public double ScalePx2Ry { get => _scalePxRy; set => _scalePxRy = value; }

        /// <summary>
        /// Gets or sets the user's _scalePyRy
        /// </summary>
        public double ScalePy2Ry { get => _scalePyRy; set => _scalePyRy = value; }

        /// <summary>
        /// Gets or sets the user's _pixCenter
        /// </summary>
        public Point PixCenter { get => _pixCenter; set => _pixCenter = value; }

        /// <summary>
        /// 标定像素比
        /// </summary>
        /// <param name="pix1">像素坐标1</param>
        /// <param name="pix2">像素坐标2</param>
        /// <param name="ruler1">光栅坐标1</param>
        /// <param name="ruler2">光栅坐标2</param>
        /// <param name="pixCenter">像素中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibRulerScale(Point pix1, Point pix2, Point ruler1, Point ruler2, Point pixCenter)
        {
            if (pix1 == pix2 || ruler1 == ruler2)
            {
                return Errortype.CALIBPIXSCALEXY_CALIBRULERSCALE_INPUT_EQUAL;
            }

            if (pix1 is null || pix2 is null || ruler1 is null || ruler2 is null || pixCenter is null)
            {
                return Errortype.CALIBPIXSCALEXY_CALIBRULERSCALE_INPUT_NULL;
            }

            double distanceX = ruler2.X - ruler1.X;
            double distanceY = ruler2.Y - ruler1.Y;

            // 标定X方向像素比
            if (Math.Abs(distanceX) > Math.Abs(distanceY))
            {
                ScaleRx2Px = (pix2.X - pix1.X) / distanceX;
                ScaleRx2Py = (pix2.Y - pix1.Y) / distanceX;
                _axisXIsCalibrated = true;
                IsCalibed = false;
                if (_axisYIsCalibrated)
                {
                    ScalePx2Rx = ScaleRy2Py / ((ScaleRx2Px * ScaleRy2Py) - (ScaleRx2Py * ScaleRy2Px));
                    ScalePy2Rx = ScaleRy2Px / ((ScaleRy2Px * ScaleRx2Py) - (ScaleRx2Px * ScaleRy2Py));

                    ScalePx2Ry = ScaleRx2Py / ((ScaleRy2Px * ScaleRx2Py) - (ScaleRx2Px * ScaleRy2Py));
                    ScalePy2Ry = ScaleRx2Px / ((ScaleRx2Px * ScaleRy2Py) - (ScaleRx2Py * ScaleRy2Px));
                    IsCalibed = true;
                }
            }

            // 标定Y方向像素比
            if (Math.Abs(distanceX) < Math.Abs(distanceY))
            {
                ScaleRy2Px = (pix2.X - pix1.X) / distanceY;
                ScaleRy2Py = (pix2.Y - pix1.Y) / distanceY;
                _axisYIsCalibrated = true;
                IsCalibed = false;
                if (_axisXIsCalibrated)
                {
                    ScalePx2Rx = ScaleRy2Py / ((ScaleRx2Px * ScaleRy2Py) - (ScaleRx2Py * ScaleRy2Px));
                    ScalePy2Rx = ScaleRy2Px / ((ScaleRy2Px * ScaleRx2Py) - (ScaleRx2Px * ScaleRy2Py));

                    ScalePx2Ry = ScaleRx2Py / ((ScaleRy2Px * ScaleRx2Py) - (ScaleRx2Px * ScaleRy2Py));
                    ScalePy2Ry = ScaleRx2Px / ((ScaleRx2Px * ScaleRy2Py) - (ScaleRx2Py * ScaleRy2Px));

                    IsCalibed = true;
                }
            }

            PixCenter = pixCenter;

            return Errortype.OK;
        }

        /// <summary>
        /// 从目标位置的像素离中心距离获取某像素位置的光栅坐标，用于Mark逼近
        /// </summary>
        /// <param name="targetPix">目标像素坐标</param>
        /// <param name="currentAxis">当前光栅坐标</param>
        /// <param name="targetAxis">目标光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRulerByPix(Point targetPix, Point currentAxis, out Point targetAxis)
        {
            if (PixCenter is null || targetPix is null || currentAxis is null)
            {
                targetAxis = null;
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            if (!IsCalibed)
            {
                targetAxis = null;
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            // 将mark像素移动到图像中心 = pixCenter - pixMark
            double pixX = PixCenter.X - targetPix.X;
            double pixY = PixCenter.Y - targetPix.Y;
            Point rulerDist = new Point(ScalePx2Rx * pixX + ScalePy2Rx * pixY, ScalePx2Ry * pixX + ScalePy2Ry * pixY);
            targetAxis = currentAxis + rulerDist;

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
            if (currentAxis is null || targetAxis is null || PixCenter is null)
            {
                return Errortype.COMMONALGO_POINTSET_NUM_ERROR;
            }

            if (!IsCalibed)
            {
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            double axisX = targetAxis.X - currentAxis.X;
            double axisY = targetAxis.Y - currentAxis.Y;
            Point pixDist = new Point(ScaleRx2Px * axisX + ScaleRy2Px * axisY, ScaleRx2Py * axisX + ScaleRy2Py * axisY);

            // 像素距离+像素中心坐标
            targetPix = PixCenter + pixDist;
            targetPix = PixCenter + pixDist;

            return Errortype.OK;
        }

        /// <summary>
        /// 加载数据
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Load(string fileDir)
        {
            string fullFileName = fileDir + ItemName + "_ScaleRulerPix.ini";
            if (!IniHelper.ExistSection("ScalePixRuler", fullFileName))
            {
                return Errortype.PARAMS_FILE_LOAD_ERROR;
            }

            var scale_PixRuler = IniHelper.GetAllValues("ScalePixRuler", fullFileName);
            ScaleRx2Px = Convert.ToDouble(scale_PixRuler[0]);
            ScaleRx2Py = Convert.ToDouble(scale_PixRuler[1]);
            ScaleRy2Px = Convert.ToDouble(scale_PixRuler[2]);
            ScaleRy2Py = Convert.ToDouble(scale_PixRuler[3]);

            ScalePx2Rx = Convert.ToDouble(scale_PixRuler[4]);
            ScalePx2Ry = Convert.ToDouble(scale_PixRuler[5]);
            ScalePy2Rx = Convert.ToDouble(scale_PixRuler[6]);
            ScalePy2Ry = Convert.ToDouble(scale_PixRuler[7]);

            PixCenter.X = Convert.ToDouble(scale_PixRuler[8]);
            PixCenter.Y = Convert.ToDouble(scale_PixRuler[9]);

            IsCalibed = true;
            return Errortype.OK;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Save(string fileDir)
        {
            if (!IsCalibed)
            {
                return Errortype.CALIBPIXSCALEXY_ISNOT_COMPLET_ERROR;
            }

            if (fileDir is null)
            {
                return Errortype.CALIBPIXSCALEXY_SAVE_FILEPATH_NULL;
            }

            if (fileDir.Length < 1)
            {
                return Errortype.CALIBPIXSCALEXY_SAVE_FILEPATH_EMPTY;
            }

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            if (!Directory.Exists(fileDir))
            {
                return Errortype.CALIBPIXSCALEXY_SAVE_FILE_DIR_NOT_EXIST_ERROR;
            }

            string fullFileName = fileDir + ItemName + "_ScaleRulerPix.ini";
            List<string> keys = new List<string> { "item_name" };
            List<string> value = new List<string> { ItemName };
            IniHelper.AddSectionWithKeyValues("info", keys, value, fullFileName);
            keys.Clear();
            keys.Add("ScaleRx2Px");
            keys.Add("ScaleRx2Py");
            keys.Add("ScaleRy2Px");
            keys.Add("ScaleRy2Py");

            keys.Add("ScalePx2Rx");
            keys.Add("ScalePx2Ry");
            keys.Add("ScalePy2Rx");
            keys.Add("ScalePy2Ry");

            keys.Add("PixCenter_X");
            keys.Add("PixCenter_Y");

            value.Clear();
            value.Add(ScaleRx2Px.ToString());
            value.Add(ScaleRx2Py.ToString());
            value.Add(ScaleRy2Px.ToString());
            value.Add(ScaleRy2Py.ToString());

            value.Add(ScalePx2Rx.ToString());
            value.Add(ScalePx2Ry.ToString());
            value.Add(ScalePy2Rx.ToString());
            value.Add(ScalePy2Ry.ToString());

            value.Add(PixCenter.X.ToString());
            value.Add(PixCenter.Y.ToString());

            IniHelper.AddSectionWithKeyValues("ScalePixRuler", keys, value, fullFileName);
            return Errortype.OK;
        }
    }

    /// <summary>
    ///  XY方向比例标定类
    /// </summary>
    public class CalibPixScaleXY : Singleton<CalibPixScaleXY>
    {
        private List<CalibPixScaleBase> _opticScaleXYCalibList = new List<CalibPixScaleBase>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Init(List<string> itemName)
        {
            if (itemName == null)
            {
                return Errortype.CALIBPIXSCALEXY_INIT_ITEMNAME_NULL;
            }

            foreach (var name in itemName)
            {
                CalibPixScaleBase opticPixScaleCalibPixBase = _opticScaleXYCalibList.Find(e => e.ItemName == name);
                if (opticPixScaleCalibPixBase != null)
                {
                    opticPixScaleCalibPixBase = new CalibPixScaleBase(name);
                }
                else
                {
                    _opticScaleXYCalibList.Add(new CalibPixScaleBase(name));
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 标定像素比
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pix1">像素坐标1</param>
        /// <param name="pix2">像素坐标2</param>
        /// <param name="ruler1">光栅坐标1</param>
        /// <param name="ruler2">光栅坐标2</param>
        /// <param name="pixCenter">像素中心</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibPixScale(string itemName, Point pix1, Point pix2, Point ruler1, Point ruler2, Point pixCenter)
        {
            CalibPixScaleBase opticPixScaleCalibPixBase = _opticScaleXYCalibList.Find(e => e.ItemName == itemName);
            if (opticPixScaleCalibPixBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticPixScaleCalibPixBase.CalibRulerScale(pix1, pix2, ruler1, ruler2, pixCenter);
        }

        /// <summary>
        /// 像素坐标转光栅坐标
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="targetPix">目标像素坐标</param>
        /// <param name="currentAxis">当前轴光栅坐标</param>
        /// <param name="targetAxis">目标轴光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRulerByPix(string itemName, Point targetPix, Point currentAxis, out Point targetAxis)
        {
            CalibPixScaleBase opticPixScaleCalibPixBase = _opticScaleXYCalibList.Find(e => e.ItemName == itemName);
            if (opticPixScaleCalibPixBase == null)
            {
                targetAxis = new Point(0, 0);

                //targetAxis = null;
                return Errortype.OPT_NAME_NULL;
            }

            return opticPixScaleCalibPixBase.GetRulerByPix(targetPix, currentAxis, out targetAxis);
        }

        /// <summary>
        /// 光栅坐标转像素坐标
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="currentAxis">当前轴位置</param>
        /// <param name="targetAxis">目标轴位置</param>
        /// <param name="targetPix">目标像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetPixByRuler(string itemName, Point currentAxis, Point targetAxis, out Point targetPix)
        {
            CalibPixScaleBase opticPixScaleCalibPixBase = _opticScaleXYCalibList.Find(e => e.ItemName == itemName);
            if (opticPixScaleCalibPixBase == null)
            {
                targetPix = null;
                return Errortype.OPT_NAME_NULL;
            }

            return opticPixScaleCalibPixBase.GetPixByRuler(currentAxis, targetAxis, out targetPix);
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
            CalibPixScaleBase opticPixScaleCalibPixBase = _opticScaleXYCalibList.Find(e => e.ItemName == itemName);
            if (opticPixScaleCalibPixBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            calibStatus = opticPixScaleCalibPixBase.IsCalibed;
            return Errortype.OK;
        }

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="fileDir">文件路径</param>
        /// <param name="loadReturn">文件加载结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Load(string fileDir, out Dictionary<string, Errortype> loadReturn)
        {
            loadReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticScaleXYCalibList.Count; index++)
            {
                Errortype ret = _opticScaleXYCalibList[index].Load(fileDir);
                loadReturn.Add(_opticScaleXYCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="fileDir">文件路径</param>
        /// <param name="saveReturn">文件保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string fileDir, out Dictionary<string, Errortype> saveReturn)
        {
            saveReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticScaleXYCalibList.Count; index++)
            {
                Errortype ret = _opticScaleXYCalibList[index].Save(fileDir);
                saveReturn.Add(_opticScaleXYCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 释放
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _opticScaleXYCalibList = new List<CalibPixScaleBase>();
            return Errortype.OK;
        }
    }
}
