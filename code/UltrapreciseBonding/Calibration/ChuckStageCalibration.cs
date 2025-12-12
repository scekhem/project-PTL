using DataStruct;
using IniFileHelper;
using System;
using System.Collections.Generic;
using HalconDotNet;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.UltrapreciseAlgorithm;
using System.CodeDom.Compiler;

namespace UltrapreciseBonding.Calib
{
    /// <summary>
    /// 吸盘平台标定
    /// </summary>
    public class ChuckStageCalib : Singleton<ChuckStageCalib>
    {
        private List<CalibCoord> _chuckStageCalibList = new List<CalibCoord>();
        private string _calibTypeName = "Chuck_";

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="names">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Init(List<string> names)
        {
            foreach (var name in names)
            {
                string fullName = _calibTypeName + name;
                CalibCoord chuckStageCalibBase = _chuckStageCalibList.Find(e => e.ItemName == fullName);
                if (chuckStageCalibBase != null)
                {
                    chuckStageCalibBase = new CalibCoord(fullName);
                }
                else
                {
                    _chuckStageCalibList.Add(new CalibCoord(fullName));
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 吸盘标定
        /// </summary>
        /// <param name="chuckName">吸盘标定名称</param>
        /// <param name="rulerPoints">光栅坐标点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibChuck(string chuckName, List<Point> rulerPoints)
        {
            CalibCoord chuckStageCalibBase = _chuckStageCalibList.Find(e => e.ItemName == _calibTypeName + chuckName);
            if (chuckStageCalibBase == null)
            {
                return Errortype.AVM_CHUCK_NAME_NULL;
            }

            if (rulerPoints is null)
            {
                return Errortype.AVM_CHUCK_CALIB_POINTS_NULL;
            }

            if (rulerPoints.Count != 2)
            {
                return Errortype.AVM_CHUCK_CALIB_POINTS_NUM_ERROR;
            }

            Point rulerPointsSum = rulerPoints[0] + rulerPoints[1];

            Point diffToChuckCenter = new Point(rulerPointsSum.X / 2.0, rulerPointsSum.Y / 2.0);
            var chuckPoints = new List<Point>
            {
                rulerPoints[0] - diffToChuckCenter,
                rulerPoints[1] - diffToChuckCenter,
            };

            return chuckStageCalibBase.CalibDo(chuckPoints, rulerPoints, TransType.RigidTrans);
        }

        /// <summary>
        /// 吸盘坐标映射到光栅坐标
        /// </summary>
        /// <param name="chuckName">吸盘名称</param>
        /// <param name="chuckPoint">吸盘坐标</param>
        /// <param name="rulerPoint">光栅坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetRulerByChuck(string chuckName, Point chuckPoint, out Point rulerPoint)
        {
            CalibCoord chuckStageCalibBase = _chuckStageCalibList.Find(e => e.ItemName == _calibTypeName + chuckName);
            if (chuckStageCalibBase == null)
            {
                rulerPoint = null;
                return Errortype.AVM_CHUCK_NAME_NULL;
            }

            return chuckStageCalibBase.Src2Dst(chuckPoint, out rulerPoint, out List<Point> error);
        }

        /// <summary>
        /// 光栅坐标映射到吸盘坐标
        /// </summary>
        /// <param name="chuckName">吸盘名称</param>
        /// <param name="rulerPoint">光栅坐标</param>
        /// <param name="chuckPoint">吸盘坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetChuckByRuler(string chuckName, Point rulerPoint, out Point chuckPoint)
        {
            CalibCoord chuckStageCalibBase = _chuckStageCalibList.Find(e => e.ItemName == _calibTypeName + chuckName);
            if (chuckStageCalibBase == null)
            {
                chuckPoint = null;
                return Errortype.AVM_CHUCK_NAME_NULL;
            }

            return chuckStageCalibBase.Dst2Src(rulerPoint, out chuckPoint, out List<Point> error);
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
            for (int index = 0; index < _chuckStageCalibList.Count; index++)
            {
                Errortype ret = _chuckStageCalibList[index].Load(fileDir);
                loadReturn.Add(_chuckStageCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <param name="saveReturn">保存结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string fileDir, out Dictionary<string, Errortype> saveReturn)
        {
            saveReturn = new Dictionary<string, Errortype>();
            for (int index = 0; index < _chuckStageCalibList.Count; index++)
            {
                Errortype ret = _chuckStageCalibList[index].Save(fileDir);
                saveReturn.Add(_chuckStageCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获得标定状态
        /// </summary>
        /// <param name="chuckName">吸盘名称</param>
        /// <param name="calibStaus">标定状态</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCalibStatus(string chuckName, out bool calibStaus)
        {
            calibStaus = false;
            CalibCoord chuckStageCalibBase = _chuckStageCalibList.Find(e => e.ItemName == _calibTypeName + chuckName);
            if (chuckStageCalibBase == null)
            {
                calibStaus = false;
                Console.WriteLine("warning: Chuck Named “" + chuckName + "” WasNot Initialized");
                return Errortype.AVM_CHUCK_NAME_NULL;
            }

            calibStaus = chuckStageCalibBase.IsCalibed;
            return Errortype.OK;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _chuckStageCalibList.Clear();
            _chuckStageCalibList = new List<CalibCoord>();
            return Errortype.OK;
        }
    }
}