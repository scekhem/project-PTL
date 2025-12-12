using System;
using System.Collections.Generic;
using IniFileHelper;
using DataStruct;
using HalconDotNet;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.Calib
{
    /// <summary>
    /// Box Calibration Base
    /// </summary>
    internal class CalibBoxBase : CalibItem
    {
        private List<Point> _realBoxPoints = new List<Point>();           //振镜真值
        private List<Point> _expBoxPoints = new List<Point>();            //振镜理论值
        private List<Point> _pointsOffset = new List<Point>();            //振镜偏差
        private double[] _matrixBox2Real = new double[6] { 1, 0, 0, 0, 1, 0 }; //映射矩阵

        //private MeshMap _mesh;
        private DataStatisticParam _residualX;
        private DataStatisticParam _residualY;

        /// <summary>
        /// 有参构造
        /// </summary>
        /// <param name="itemName">标定名称</param>
        public CalibBoxBase(string itemName)
        {
            ItemName = itemName;

            //_mesh = new MeshMap(itemName);
            IsCalibed = false;
        }

        //public MeshMap Mesh { get => _mesh; set => _mesh = value; }

        /// <summary>
        /// Gets the user's matrixBox2Real
        /// </summary>
        public double[] MatrixBox2Real
        {
            get => _matrixBox2Real;
        }

        /// <summary>
        /// Gets or sets the user's pointsOffset
        /// </summary>
        public List<Point> PointOffset
        {
            get => _pointsOffset;
            set => _pointsOffset = value;
        }

        /// <summary>
        /// Gets or sets the user's expBoxPoints
        /// </summary>
        public List<Point> ExpBoxPoints
        {
            get => _expBoxPoints;
            set => _expBoxPoints = value;
        }

        /// <summary>
        /// Gets or sets the user's realBoxPoints
        /// </summary>
        public List<Point> TrueBoxPoints
        {
            get => _realBoxPoints;
            set => _realBoxPoints = value;
        }

        /// <summary>
        /// Gets the user's _residualX
        /// </summary>
        public DataStatisticParam ResidualX
        {
            get => _residualX;
        }

        /// <summary>
        /// Gets the user's _residualY
        /// </summary>
        public DataStatisticParam ResidualY
        {
            get => _residualY;
        }

        /// <summary>
        /// 获取行列号对应角点在list中的存储编号
        /// </summary>
        /// <param name="rowId">行号</param>
        /// <param name="colId">列号</param>
        /// <param name="listId">存储编号</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetIndexByRowCol(int rowId, int colId, out int listId)
        {
            listId = -1;

            //Mesh.GetMeshCell(rowId, colId, out List<int> conerId);
            //listId = conerId[0];
            return Errortype.OK;
        }

        /// <summary>
        /// 获取box角点像素坐标
        /// </summary>
        /// <param name="image">输入图像</param>
        /// <param name="crossCenterPix">十字中心像素坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCrossPix(Camera image, out Point crossCenterPix)
        {
            crossCenterPix = null;

            // todo： 添加优化的box角点定位逻辑
            return Errortype.OK;
        }

        /// <summary>
        /// 添加box真值点
        /// </summary>
        /// <param name="rowId">行号</param>
        /// <param name="colId">列号</param>
        /// <param name="realPoint">真值坐标</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype AddRealBoxPoint(int rowId, int colId, Point realPoint)
        {
            if (realPoint == null)
            {
                //Mesh.CellMap.Add(-Mesh.CellMap.Count);
            }
            else
            {
                _realBoxPoints.Add(realPoint);

                //Mesh.CellMap.Add(_realBoxPoints.Count - 1);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// box标定误差
        /// </summary>
        /// <param name="rowNum">行数量</param>
        /// <param name="colNum">列数量</param>
        /// <param name="rowInterval">带正负方向的行间距</param>
        /// <param name="colInterval">带正负方向的列间距</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibBoxError(int rowNum, int colNum, double rowInterval, double colInterval)
        {
            GenExpectBoxPoints(rowNum, colNum, rowInterval, colInterval);

            //Mesh.GenMeshMap(rowNum,colNum);
            double[] expX = new double[_expBoxPoints.Count];
            double[] expY = new double[_expBoxPoints.Count];
            double[] realX = new double[_expBoxPoints.Count];
            double[] realY = new double[_expBoxPoints.Count];
            for (int index = 0; index < _expBoxPoints.Count; index++)
            {
                expX[index] = _expBoxPoints[index].X;
                expY[index] = _expBoxPoints[index].Y;
                realX[index] = _realBoxPoints[index].X;
                realY[index] = _realBoxPoints[index].Y;
            }

            HOperatorSet.VectorToRigid(expX, expY, realX, realY, out HTuple homMat2D);
            _matrixBox2Real = homMat2D.DArr;
            List<Point> residual = new List<Point>();
            for (int index = 0; index < _expBoxPoints.Count; index++)
            {
                HOperatorSet.AffineTransPoint2d(homMat2D, _expBoxPoints[index].X, _expBoxPoints[index].Y, out HTuple dstTranX, out HTuple dstTranY);
                residual.Add(new Point(dstTranX, dstTranY));
            }

            _pointsOffset = residual;
            CalcResidual(out DataStatisticParam resX, out DataStatisticParam resY);
            _residualX = resX;
            _residualY = resY;
            return Errortype.OK;
        }

        private Errortype CalcResidual(out DataStatisticParam residualX, out DataStatisticParam residualY)
        {
            return ComAlgo.CalcDataSummary(_pointsOffset, out residualX, out residualY);
        }

        /// <summary>
        /// 生成理论box角点
        /// </summary>
        /// <param name="rowNum">行数量</param>
        /// <param name="colNum">列数量</param>
        /// <param name="rowInterval">带正负方向行间距</param>
        /// <param name="colInterval">带正负方向列间距</param>
        /// <returns>OK:成功,其他:失败</returns>
        internal Errortype GenExpectBoxPoints(int rowNum, int colNum, double rowInterval, double colInterval)
        {
            double centreBiasY = (rowNum - 1) / 2 * rowInterval;
            double centreBiasX = (colNum - 1) / 2 * colInterval;
            List<Point> boxPts = new List<Point>();
            for (int row = 0; row < rowNum; row++)
            {
                for (int col = 0; col < colNum; col++)
                {
                    // box中心点为 0,0 不区分奇偶行列
                    int index = row * colNum + col;

                    //if (Mesh.CellMap[index] >= 0)
                    //{
                    //    boxPts.Add(new Point(col * colInterval - centreBiasX, row * rowInterval + centreBiasY));
                    //}
                }
            }

            _expBoxPoints = boxPts;
            return Errortype.OK;
        }

        /// <summary>
        /// 重写 加载
        /// </summary>
        /// <param name="filrDir">加载文件路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Load(string filrDir)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 重写 保存
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        /// <exception cref="NotImplementedException">异常</exception>
        public override Errortype Save(string fileDir)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Box Calibration
    /// </summary>
    public class CalibBox : Singleton<CalibBox>
    {
        private List<CalibBoxBase> _calibBoxList = new List<CalibBoxBase>();

        /// <summary>
        /// 重写 box标定初始化
        /// </summary>
        /// <param name="names">初始化标定项</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Init(List<string> names)
        {
            foreach (var name in names)
            {
                CalibBoxBase boxCalibBase = _calibBoxList.Find(e => e.ItemName == name);
                if (boxCalibBase != null)
                {
                    boxCalibBase = new CalibBoxBase(name);
                }
                else
                {
                    _calibBoxList.Add(new CalibBoxBase(name));
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 重写 保存
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string fileDir)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 重写 加载数据
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        /// <exception cref="NotImplementedException">异常</exception>
        public Errortype Load(string fileDir)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 重写 释放
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        /// <exception cref="NotImplementedException">异常</exception>
        public Errortype Release()
        {
            throw new NotImplementedException();
        }
    }
}
