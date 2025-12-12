using System;
using System.Collections.Generic;
using IniFileHelper;
using DataStruct;
using HalconDotNet;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;

namespace UltrapreciseBonding.Calib
{
    #region 镜头同轴误差标定,   Campix2Campix

    /// <summary>
    /// 同轴度标定类
    /// </summary>
    public class CalibCoaxiality : Singleton<CalibCoaxiality>
    {
        private List<CalibCoord> _opticConcentricCalibList = new List<CalibCoord>();
        private string _calibTypeName = "Coaxis_";

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="opticNames">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Init(List<string> opticNames)
        {
            foreach (var name in opticNames)
            {
                string fullName = _calibTypeName + name;
                CalibCoord opticConcentricCalibBase = _opticConcentricCalibList.Find(e => e.ItemName == fullName);
                if (opticConcentricCalibBase != null)
                {
                    opticConcentricCalibBase = new CalibCoord(fullName);
                }
                else
                {
                    _opticConcentricCalibList.Add(new CalibCoord(fullName));
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 标定计算
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="topPoints">top相机点集</param>
        /// <param name="bottomPoints">buttom相机点集</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Calib(string itemName, List<Point> topPoints, List<Point> bottomPoints)
        {
            CalibCoord opticConcentricCalibBase = _opticConcentricCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticConcentricCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticConcentricCalibBase.CalibDo(topPoints, bottomPoints, TransType.AffineTrans);
        }

        /// <summary>
        /// 同轴相机同心度标定验证
        /// </summary>
        /// <param name="opticName">标定项名称</param>
        /// <param name="topMarkPixs">上相机像素坐标</param>
        /// <param name="bottomMarkPixs">下相机像素坐标</param>
        /// <param name="maxError">X、Y方向最大误差</param>
        /// <param name="meanError">X、Y方向平均误差</param>
        /// <param name="stdError">X、Y方向均方误差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CoaxialityVerify(string opticName, List<Point> topMarkPixs, List<Point> bottomMarkPixs, out Point maxError, out Point meanError, out Point stdError)
        {
            maxError = new Point();
            meanError = new Point();
            stdError = new Point();
            if (topMarkPixs.Count < 2)
            {
                return Errortype.INPUT_POINT_ERROR;
            }

            double[] errorX = new double[topMarkPixs.Count];
            double[] errorY = new double[topMarkPixs.Count];
            for (int i = 0; i < topMarkPixs.Count; ++i)
            {
                var ret = GetBottomPixel(opticName, topMarkPixs[i], out Point bottomPix);
                errorX[i] = bottomMarkPixs[i].X - bottomPix.X;
                errorY[i] = bottomMarkPixs[i].Y - bottomPix.Y;
            }

            double meanX = errorX.Average();
            double meanY = errorY.Average();
            double sumOfSquaresX = errorX.Sum(d => Math.Pow(d - meanX, 2));
            double sumOfSquaresY = errorY.Sum(d => Math.Pow(d - meanY, 2));
            double varianceX = sumOfSquaresX / errorX.Length;
            double varianceY = sumOfSquaresY / errorY.Length;
            double stdDevX = Math.Sqrt(varianceX);
            double stdDevY = Math.Sqrt(varianceY);

            maxError = new Point(errorX.Max(), errorY.Max());
            meanError = new Point(meanX, meanY);
            stdError = new Point(stdDevX, stdDevY);

            return Errortype.OK;
        }

        /// <summary>
        /// 将下相机像素映射到上相机视野
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="bottomPix">bottom点</param>
        /// <param name="topPix">返回top点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetTopPixel(string itemName, Point bottomPix, out Point topPix)
        {
            CalibCoord opticConcentricCalibBase = _opticConcentricCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticConcentricCalibBase == null)
            {
                topPix = null;
                return Errortype.MACROSTAGE_NAME_NULL;
            }

            return opticConcentricCalibBase.Dst2Src(bottomPix, out topPix, out List<Point> error);
        }

        /// <summary>
        /// 将上相机像素映射到下相机视野
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="topPix">top点</param>
        /// <param name="bottomPix">返回bottom点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetBottomPixel(string itemName, Point topPix, out Point bottomPix)
        {
            CalibCoord opticConcentricCalibBase = _opticConcentricCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticConcentricCalibBase == null)
            {
                bottomPix = null;
                return Errortype.MACROSTAGE_NAME_NULL;
            }

            return opticConcentricCalibBase.Src2Dst(topPix, out bottomPix, out List<Point> error);
        }

        /// <summary>
        /// 查询标定状态
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="calibStaus">标定状态（是否标定）</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCalibStatus(string itemName, out bool calibStaus)
        {
            calibStaus = false;
            CalibCoord opticConcentricCalibBase = _opticConcentricCalibList.Find(e => e.ItemName == _calibTypeName + itemName);
            if (opticConcentricCalibBase == null)
            {
                return Errortype.MACROSTAGE_NAME_NULL;
            }

            calibStaus = opticConcentricCalibBase.IsCalibed;
            return Errortype.OK;
        }

        /// <summary>
        /// 释放所有内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _opticConcentricCalibList = new List<CalibCoord>();
            return Errortype.OK;
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <param name="fileDir">文件保存路径</param>
        /// <param name="saveReturn">文件保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string fileDir, out Dictionary<string, Errortype> saveReturn)
        {
            saveReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticConcentricCalibList.Count; index++)
            {
                Errortype ret = _opticConcentricCalibList[index].Save(fileDir);
                saveReturn.Add(_opticConcentricCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="fileDir">文件保存路径</param>
        /// <param name="loadReturn">文件保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Load(string fileDir, out Dictionary<string, Errortype> loadReturn)
        {
            loadReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticConcentricCalibList.Count; index++)
            {
                Errortype ret = _opticConcentricCalibList[index].Load(fileDir);
                loadReturn.Add(_opticConcentricCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }
    }

    #endregion
}