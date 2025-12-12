//#define DEBUG_ON

using DataStruct;
using HalconDotNet;
using IniFileHelper;
using OpenCvSharp.Flann;
using Supercluster.KDTree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using UltrapreciseBonding.Calib;
using UltrapreciseBonding.UltrapreciseAlgorithm;

namespace UltrapreciseBonding.Calib
{
    /// <summary>
    /// 坐标转换类型
    /// </summary>
    public enum TransType
    {
        RigidTrans, //刚体变化
        SimilarityTrans, //相似变换
        AffineTrans, //仿射变换
        AffineKDTrans, //kdtree找最近邻进行仿射变换
        AffineVectorXYTrans, //XY轴向量求解仿射变换
    }

    /// <summary>
    /// 坐标系到坐标系的转换
    /// </summary>
    public class CalibCoord : CalibItem
    {
        private List<Point> _srcPoints = new List<Point>();
        private List<Point> _dstPoints = new List<Point>(); //目标点集，常用的ruler2real情况下，real为dst

        private List<Point> _errorSrc2Dst = new List<Point>();
        private List<Point> _errorDst2Src = new List<Point>();

        //affineTrans or rigidTrans
        private double[] _matrixSrc2Dst = new double[] { 1, 0, 0, 0, 1, 0 };
        private double[] _matrixDst2Src = new double[] { 1, 0, 0, 0, 1, 0 };

        private KDTree<double, Point> _kdTreeSrc2Dst;
        private KDTree<double, Point> _kdTreeDst2Src;
        private int _neighbors = 4; //默认找最近的4个点进行仿射变换

        // 运动系标定验证过程数据
        private List<Point> _verifyPtsRulerIn = new List<Point>();       // 运动系验证输入光栅点集
        private List<Point> _verifyPtsRealIn = new List<Point>();        // 运动系验证输入理论真值点集
        private List<Point> _verifyPtsRealTrue = new List<Point>();      // 运动系验证根据光栅计算的实际真值点集
        private List<Point> _verifyPtsError = new List<Point>();         // 运动系验证实际真值点集与理论真值点集差值

        //以此区分不同的坐标转换方式
        private TransType _transType = TransType.AffineTrans;

        /// <summary>
        /// Gets the user's _transType
        /// </summary>
        public TransType TransType
        {
            get { return _transType; }
        }

        /// <summary>
        /// Gets the user's _neighbors
        /// </summary>
        public int Neighbors
        {
            get { return _neighbors; }
        }

        /// <summary>
        /// 刚体变换
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype CalibRigidTrans()
        {
            Errortype ret = ComAlgo.VectorToRigid(_srcPoints, _dstPoints, out _matrixSrc2Dst, out _errorSrc2Dst);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.VectorToRigid(_dstPoints, _srcPoints, out _matrixDst2Src, out _errorDst2Src);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _transType = TransType.RigidTrans;
            return Errortype.OK;
        }

        /// <summary>
        /// 相似变换
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype CalibSimilarityTrans()
        {
            Errortype ret = ComAlgo.VectorToSimilarity(_srcPoints, _dstPoints, out _matrixSrc2Dst, out _errorSrc2Dst);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.VectorToSimilarity(_dstPoints, _srcPoints, out _matrixSrc2Dst, out _errorDst2Src);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _transType = TransType.SimilarityTrans;
            return Errortype.OK;
        }

        /// <summary>
        /// 仿射变换
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype CalibAffineTrans()
        {
            Errortype ret = ComAlgo.VectorToHomMat2d(_srcPoints, _dstPoints, out _matrixSrc2Dst, out _errorSrc2Dst);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.VectorToHomMat2d(_dstPoints, _srcPoints, out _matrixDst2Src, out _errorDst2Src);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            _transType = TransType.AffineTrans;
            return Errortype.OK;
        }

        /// <summary>
        /// 根据两个坐标系的空间关系计算仿射变换矩阵
        /// </summary>
        /// <param name="center">原始坐标系原点在目标坐标系的位置（如轴的零点在标定板坐标系的位置）</param>
        /// <param name="sx">x轴方向缩放比</param>
        /// <param name="sy">y轴方向缩放比</param>
        /// <param name="radRotate">x轴旋转角度（逆时针为正）</param>
        /// <param name="radSlant">y轴与理论y轴的夹角（逆时针为正）</param>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype CalibAffineVectorXYTrans(Point center, double sx, double sy, double radRotate, double radSlant)
        {
            Errortype ret = Errortype.OK;
            HOperatorSet.HomMat2dIdentity(out HTuple homMat2DIdentity);
            HOperatorSet.HomMat2dScale(homMat2DIdentity, sx, sy, center.X, center.Y, out HTuple homMat2DScale);

            //逆时针为正，这里radSlant 需要加上负号
            HOperatorSet.HomMat2dSlant(homMat2DScale, -radSlant, "y", center.X, center.Y, out HTuple homMat2DSlant);

            //逆时针为正，这里radRotate 需要加上负号
            HOperatorSet.HomMat2dRotate(homMat2DSlant, -radRotate, center.X, center.Y, out HTuple homMat2DRotate);
            HOperatorSet.HomMat2dTranslate(homMat2DRotate, center.X, center.Y, out HTuple homMat2DTranslate);
            _matrixDst2Src = homMat2DTranslate.DArr;
            HOperatorSet.HomMat2dInvert(homMat2DTranslate, out HTuple homMat2DInvert);
            _matrixSrc2Dst = homMat2DInvert.DArr;
            _transType = TransType.AffineVectorXYTrans;

            return ret;
        }

        /// <summary>
        /// 构建kd树，找最近的n个点进行仿射变换
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        private Errortype CalibAffineKdTrans()
        {
            Errortype ret = Errortype.OK;
            if (_srcPoints.Count < 1)
            {
                return Errortype.CALIBCOORD_KDTREE_COUNT_ZERO;
            }

            List<double[]> srcArray = new List<double[]>();
            List<double[]> dstArray = new List<double[]>();
            for (int i = 0; i < _srcPoints.Count; i++)
            {
                srcArray.Add(new double[] { _srcPoints[i].X, _srcPoints[i].Y });
                dstArray.Add(new double[] { _dstPoints[i].X, _dstPoints[i].Y });
            }

            _kdTreeSrc2Dst = new KDTree<double, Point>(2, srcArray.ToArray(), _dstPoints.ToArray(), Supercluster.KDTree.Utilities.Utilities.L2Norm_Squared_Double);
            _kdTreeDst2Src = new KDTree<double, Point>(2, dstArray.ToArray(), _srcPoints.ToArray(), Supercluster.KDTree.Utilities.Utilities.L2Norm_Squared_Double);
            _transType = TransType.AffineKDTrans;
            return ret;
        }

        /// <summary>
        /// 初始化运动系标定参数
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype InitCalibDoXY()
        {
            _matrixSrc2Dst = new double[] { 1, 0, 0, 0, 1, 0 };
            _matrixDst2Src = new double[] { 1, 0, 0, 0, 1, 0 };
            IsCalibed = true;
            _transType = TransType.AffineTrans;

            return Errortype.OK;
        }

        /// <summary>
        /// 根据输入点集计算两个坐标系之间的转换关系
        /// </summary>
        /// <param name="src">原始点点集</param>
        /// <param name="dst">目标点点集</param>
        /// <param name="transType">转换方式</param>
        /// <param name="neighbors">最近邻点数，仅用于KDTree</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibDo(List<Point> src, List<Point> dst, TransType transType, int neighbors = 4)
        {
            _srcPoints = src;
            _dstPoints = dst;
            _transType = transType;
            _neighbors = neighbors;
            if (_srcPoints is null || _dstPoints is null)
            {
                return Errortype.CALIBCOORD_INPUTPOINTS_NULL;
            }

            if (_srcPoints.Count != _dstPoints.Count)
            {
                return Errortype.CALIBCOORD_INPUTPOINTS_COUNT_NOT_EQUAL;
            }

            Errortype ret = Errortype.OK;
            switch (_transType)
            {
                case TransType.RigidTrans:
                    ret = CalibRigidTrans();
                    break;
                case TransType.SimilarityTrans:
                    ret = CalibSimilarityTrans();
                    break;
                case TransType.AffineTrans:
                    ret = CalibAffineTrans();
                    break;
                case TransType.AffineKDTrans:
                    ret = CalibAffineKdTrans();
                    break;
                case TransType.AffineVectorXYTrans:
                    return Errortype.CALIBCOORD_TRANSTYPE_ERROR; //此构造函数不支持该方法
                default:
                    return Errortype.CALIBCOORD_TRANSTYPE_ERROR;
            }
#if DEBUG_ON
            WriteCalibError("./CalibError" + _transType.ToString() + _itemName);
#endif
            if (ret != Errortype.OK)
            {
                return ret;
            }

            IsCalibed = true;
            return ret;
        }

        /// <summary>
        /// 根据两个坐标系之间的关系计算仿射变换矩阵，这里变换类型必须是AffineVectorXYTrans
        /// </summary>
        /// <param name="center">原始坐标系原点在目标坐标系的位置（如轴的零点在标定板坐标系的位置）</param>
        /// <param name="sx">x轴方向缩放比</param>
        /// <param name="sy">y轴方向缩放比</param>
        /// <param name="radRotate">x轴旋转角度（逆时针为正）</param>
        /// <param name="radSlant">y轴与理论y轴的夹角（逆时针为正）</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibDo(Point center, double sx, double sy, double radRotate, double radSlant)
        {
            Errortype ret = CalibAffineVectorXYTrans(center, sx, sy, radRotate, radSlant);
            return ret;
        }

        /// <summary>
        /// 根据KDTree完成原始点到目标点的转换
        /// </summary>
        /// <param name="p1">src原始点</param>
        /// <param name="p2">dst目标点</param>
        /// <param name="error">误差</param>
        /// <returns>ret</returns>
        private Errortype Src2DstByKDTree(Point p1, out Point p2, out List<Point> error)
        {
            p2 = new Point();
            error = new List<Point>();
            if (p1 is null)
            {
                return Errortype.CALIBCOORD_SRC2DSTBYKDTREE_INPUTPOINT_NULL;
            }

            if (_kdTreeSrc2Dst is null)
            {
                return Errortype.CALIBCOORD_KDTREE_NULL;
            }

            Errortype ret = Errortype.OK;
            double[] homMat2D = new double[0];
            int tryTimes = 0;
            List<Point> srcList = new List<Point>();
            List<Point> dstList = new List<Point>();
            while (tryTimes < _srcPoints.Count)
            {
                srcList = new List<Point>();
                dstList = new List<Point>();

                // 默认寻找四个点，共线则递增
                var result = _kdTreeSrc2Dst.NearestNeighbors(new double[] { p1.X, p1.Y }, _neighbors + tryTimes);
                for (int i = 0; i < result.Length; i++)
                {
                    double[] srcFound = result[i].Item1;
                    srcList.Add(new Point(srcFound[0], srcFound[1]));
                    Point dstFound = result[i].Item2;
                    dstList.Add(dstFound);
                }

                if (ComAlgo.CheckUnColline(srcList) && ComAlgo.CheckUnColline(dstList))
                {
                    //若出现不共线则跳出不再继续找邻近点
                    break;
                }

                //若共线则接着找更多点
                tryTimes++;
            }

            ret = ComAlgo.VectorToHomMat2d(srcList, dstList, out homMat2D, out error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.AffineTransPoint2d(p1, homMat2D, out p2);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// 根据KDTree完成目标点到原始点转换
        /// </summary>
        /// <param name="p1">src原始点</param>
        /// <param name="p2">dst目标点</param>
        /// <param name="error">误差</param>
        /// <returns>ret</returns>
        private Errortype Dst2SrcByKDTree(Point p1, out Point p2, out List<Point> error)
        {
            p2 = new Point();
            error = new List<Point>();
            if (p1 is null)
            {
                return Errortype.CALIBCOORD_SRC2DSTBYKDTREE_INPUTPOINT_NULL;
            }

            if (_kdTreeDst2Src is null)
            {
                return Errortype.CALIBCOORD_KDTREE_NULL;
            }

            Errortype ret = Errortype.OK;
            double[] homMat2D = new double[0];
            int tryTimes = 0;
            List<Point> srcList = new List<Point>();
            List<Point> dstList = new List<Point>();
            while (tryTimes < _srcPoints.Count)
            {
                srcList = new List<Point>();
                dstList = new List<Point>();
                var result = _kdTreeDst2Src.NearestNeighbors(new double[] { p1.X, p1.Y }, _neighbors + tryTimes);
                for (int i = 0; i < result.Length; i++)
                {
                    Point srcFound = result[i].Item2;
                    srcList.Add(srcFound);
                    double[] dstFound = result[i].Item1;
                    dstList.Add(new Point(dstFound[0], dstFound[1]));
                }

                if (ComAlgo.CheckUnColline(srcList) && ComAlgo.CheckUnColline(dstList))
                {
                    //若出现不共线则跳出不再继续找邻近点
                    break;
                }

                //若共线则接着找更多点
                tryTimes++;
            }

            ret = ComAlgo.VectorToHomMat2d(dstList, srcList, out homMat2D, out error);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            ret = ComAlgo.AffineTransPoint2d(p1, homMat2D, out p2);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            return ret;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">标定项名称</param>
        public CalibCoord(string name)
        {
            ItemName = name;
            IsCalibed = false;
            return;
        }

        /// <summary>
        /// 获取标定的误差
        /// </summary>
        /// <param name="errorSrc2Dst">原始点转目标点的残差</param>
        /// <param name="errorDst2Src">目标点转原始点的残差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCalibError(out List<Point> errorSrc2Dst, out List<Point> errorDst2Src)
        {
            errorSrc2Dst = _errorSrc2Dst;
            errorDst2Src = _errorDst2Src;
            return Errortype.OK;
        }

        /// <summary>
        /// 保存标定残差
        /// </summary>
        /// <param name="fileToSave">保存文件路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype WriteCalibError(string fileToSave)
        {
            string fileNameS2D = fileToSave + "_S2D.csv";
            string fileNameD2S = fileToSave + "_D2S.csv";
            if (File.Exists(fileNameS2D))
            {
                File.Delete(fileNameS2D);
            }

            if (File.Exists(fileNameD2S))
            {
                File.Delete(fileNameD2S);
            }

            string pathS2D = System.IO.Path.GetDirectoryName(fileNameS2D);
            if (!System.IO.Directory.Exists(pathS2D))
            {
                System.IO.Directory.CreateDirectory(pathS2D);
            }

            HOperatorSet.OpenFile(fileNameS2D, "append", out HTuple fileS2D);
            foreach (var error in _errorSrc2Dst)
            {
                HOperatorSet.FwriteString(fileS2D, error.X.ToString() + ",");
                HOperatorSet.FwriteString(fileS2D, error.Y.ToString() + "\n");
            }

            HOperatorSet.CloseFile(fileS2D);
            HOperatorSet.OpenFile(fileNameD2S, "append", out HTuple fileD2S);
            foreach (var error in _errorDst2Src)
            {
                HOperatorSet.FwriteString(fileD2S, error.X.ToString() + ",");
                HOperatorSet.FwriteString(fileD2S, error.Y.ToString() + "\n");
            }

            HOperatorSet.CloseFile(fileD2S);
            return Errortype.OK;
        }

        /// <summary>
        /// 原始点转目标点 如ruler2real
        /// </summary>
        /// <param name="p1">原始点</param>
        /// <param name="p2">转换后的点</param>
        /// <param name="error">每个点的误差，仅限于kdtree的方式</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Src2Dst(Point p1, out Point p2, out List<Point> error)
        {
            p2 = new Point();
            error = null;
            if (!IsCalibed)
            {
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            Errortype ret = Errortype.OK;
            switch (_transType)
            {
                case TransType.RigidTrans:
                    ret = ComAlgo.AffineTransPoint2d(p1, _matrixSrc2Dst, out p2);
                    break;
                case TransType.SimilarityTrans:
                    ret = ComAlgo.AffineTransPoint2d(p1, _matrixSrc2Dst, out p2);
                    break;
                case TransType.AffineTrans:
                    ret = ComAlgo.AffineTransPoint2d(p1, _matrixSrc2Dst, out p2);
                    break;
                case TransType.AffineKDTrans:
                    ret = Src2DstByKDTree(p1, out p2, out error);
                    break;
                case TransType.AffineVectorXYTrans:
                    ret = ComAlgo.AffineTransPoint2d(p1, _matrixSrc2Dst, out p2);
                    break;
                default:
                    break;
            }

            return ret;
        }

        /// <summary>
        /// 目标点转原始点 如real2ruler
        /// </summary>
        /// <param name="p1">目标点</param>
        /// <param name="p2">转换后的点</param>
        /// <param name="error">每个点的误差，仅限于kdtree的方式</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Dst2Src(Point p1, out Point p2, out List<Point> error)
        {
            p2 = new Point();
            error = null;
            if (!IsCalibed)
            {
                return Errortype.CALIBRATE_ISNOT_COMPLET_ERROR;
            }

            Errortype ret = Errortype.OK;
            switch (_transType)
            {
                case TransType.RigidTrans:
                    ret = ComAlgo.AffineTransPoint2d(p1, _matrixDst2Src, out p2);
                    break;
                case TransType.SimilarityTrans:
                    ret = ComAlgo.AffineTransPoint2d(p1, _matrixDst2Src, out p2);
                    break;
                case TransType.AffineTrans:
                    ret = ComAlgo.AffineTransPoint2d(p1, _matrixDst2Src, out p2);
                    break;
                case TransType.AffineKDTrans:
                    ret = Dst2SrcByKDTree(p1, out p2, out error);
                    break;
                case TransType.AffineVectorXYTrans:
                    ret = ComAlgo.AffineTransPoint2d(p1, _matrixDst2Src, out p2);
                    break;
                default:
                    break;
            }

            return ret;
        }

        /// <summary>
        /// 保存标定文件
        /// </summary>
        /// <param name="fileDir">文件所在路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Save(string fileDir)
        {
            if (!IsCalibed)
            {
                return Errortype.CALIBCOORD_ISNOT_COMPLET_ERROR;
            }

            if (fileDir is null)
            {
                return Errortype.CALIBCOORD_SAVE_FILEPATH_NULL;
            }

            if (fileDir.Length < 1)
            {
                return Errortype.CALIBCOORD_SAVE_FILEPATH_LENGTH_ERROR;
            }

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            if (!Directory.Exists(fileDir))
            {
                return Errortype.CALIBCOORD_SAVE_FILE_DIR_NOT_EXIST_ERROR;
            }

            // kdtree的情况 保存所有的点  加载时重新构建树
            // 其他情况 仅保存转换矩阵
            //保存基本信息
            string fullFileName = fileDir + "\\" + ItemName + "_CalibCoord.ini";
            if (File.Exists(fullFileName))
            {
                File.Delete(fullFileName);
            }

            List<string> keys = new List<string> { "Item_name", "TransType", "Neighbors" };
            List<string> value = new List<string> { ItemName, _transType.ToString(), _neighbors.ToString() };
            IniHelper.AddSectionWithKeyValues("Info", keys, value, fullFileName);

            if (_transType == TransType.RigidTrans || _transType == TransType.SimilarityTrans || _transType == TransType.AffineTrans || _transType == TransType.AffineVectorXYTrans)
            {
                //save matrix
                fullFileName = fileDir + "\\" + ItemName + "_CalibCoordMatrix.ini";
                if (File.Exists(fullFileName))
                {
                    File.Delete(fullFileName);
                }

                keys.Clear();
                value.Clear();
                keys.Clear();
                keys.Add("m01");
                keys.Add("m02");
                keys.Add("m03");
                keys.Add("m04");
                keys.Add("m05");
                keys.Add("m06");
                value.Add(_matrixSrc2Dst[0].ToString());
                value.Add(_matrixSrc2Dst[1].ToString());
                value.Add(_matrixSrc2Dst[2].ToString());
                value.Add(_matrixSrc2Dst[3].ToString());
                value.Add(_matrixSrc2Dst[4].ToString());
                value.Add(_matrixSrc2Dst[5].ToString());
                IniHelper.AddSectionWithKeyValues("Matrix_Src2Dst", keys, value, fullFileName);

                value.Clear();
                value.Add(_matrixDst2Src[0].ToString());
                value.Add(_matrixDst2Src[1].ToString());
                value.Add(_matrixDst2Src[2].ToString());
                value.Add(_matrixDst2Src[3].ToString());
                value.Add(_matrixDst2Src[4].ToString());
                value.Add(_matrixDst2Src[5].ToString());
                IniHelper.AddSectionWithKeyValues("Matrix_Dst2Src", keys, value, fullFileName);
            }
            else
            {
                //save points
                fullFileName = fileDir + "\\" + ItemName + "_CalibCoordPoints.ini";
                if (File.Exists(fullFileName))
                {
                    File.Delete(fullFileName);
                }

                //上面AddSectionWithKeyValues方法是进行逐个点写入，每次都是一个点调一次该方法，耗时82.731s
                //改用下面 StringBuilder方式一次性写入，耗时10ms
                StringBuilder contentBuilder = new StringBuilder();
                contentBuilder.AppendLine("[PointsNum]");
                contentBuilder.AppendLine("PointsNum=" + _srcPoints.Count.ToString());
                for (int index = 0; index < _srcPoints.Count; index++)
                {
                    contentBuilder.AppendLine("[Point_" + index.ToString() + "]");
                    contentBuilder.AppendLine("src_x=" + _srcPoints[index].X.ToString());
                    contentBuilder.AppendLine("src_y=" + _srcPoints[index].Y.ToString());
                    contentBuilder.AppendLine("dst_x=" + _dstPoints[index].X.ToString());
                    contentBuilder.AppendLine("dst_y=" + _dstPoints[index].Y.ToString());
                }

                File.WriteAllText(fullFileName, contentBuilder.ToString());
            }

            if (ComAlgo.SaveFlg("CalibXYDataSave", out int flag))
            {
                //save points
                string dataName = fileDir + "\\" + ItemName + "_CalibCoordPoints.txt";

                if (_srcPoints.Count > 0)
                {
                    if (File.Exists(dataName))
                    {
                        File.Delete(dataName);
                    }

                    //上面AddSectionWithKeyValues方法是进行逐个点写入，每次都是一个点调一次该方法，耗时82.731s
                    //改用下面 StringBuilder方式一次性写入，耗时10ms
                    StringBuilder contentBuilder = new StringBuilder();
                    contentBuilder.AppendLine("ruler_x " + "ruler_y " + "real_x " + "real_y");
                    for (int index = 0; index < _srcPoints.Count; index++)
                    {
                        contentBuilder.AppendLine(_srcPoints[index].X.ToString() + " " + _srcPoints[index].Y.ToString() + " " + _dstPoints[index].X.ToString() + " " + _dstPoints[index].Y.ToString());
                    }

                    File.WriteAllText(dataName, contentBuilder.ToString());
                }

                if (_verifyPtsRulerIn.Count > 0)
                {
                    dataName = fileDir + "\\" + ItemName + "_CalibCoordPointsVerify.txt";
                    if (File.Exists(dataName))
                    {
                        File.Delete(dataName);
                    }

                    StringBuilder contentVerify = new StringBuilder();
                    contentVerify.AppendLine("rulerTheory_x " + "rulerTheory_y " + "realTheory_x " + "realTheory_y " + "realTrue_x " + "realTrue_y " + "error_x " + "error_y");
                    for (int index = 0; index < _verifyPtsRulerIn.Count; index++)
                    {
                        contentVerify.AppendLine(_verifyPtsRulerIn[index].X.ToString() + " " + _verifyPtsRulerIn[index].Y.ToString() + " " +
                            _verifyPtsRealIn[index].X.ToString() + " " + _verifyPtsRealIn[index].Y.ToString() + " " +
                            _verifyPtsRealTrue[index].X.ToString() + " " + _verifyPtsRealTrue[index].Y.ToString() + " " +
                            _verifyPtsError[index].X.ToString() + " " + _verifyPtsError[index].Y.ToString());
                    }

                    File.WriteAllText(dataName, contentVerify.ToString());
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载标定数据
        /// </summary>
        /// <param name="fileDir">文件所在路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Load(string fileDir)
        {
            string fullFileName = fileDir + "\\" + ItemName + "_CalibCoord.ini";
            if (!File.Exists(fullFileName))
            {
                return Errortype.CALIBCOORD_INFO_FILE_NOT_EXIST;
            }

            if (!IniHelper.ExistSection("Info", fullFileName))
            {
                return Errortype.CALIBCOORD_INFO_SECTION_NOT_EXIST;
            }

            Console.WriteLine("load calib:" + fullFileName);
            string[] keys = null;
            string[] values = null;
            IniHelper.GetAllKeyValues("Info", out keys, out values, fullFileName);
            if (values.Length != 3)
            {
                return Errortype.CALIBCOORD_INFO_VALUE_LENGTH_ERROR;
            }

            ItemName = values[0];
            _transType = (TransType)Enum.Parse(typeof(TransType), values[1]);
            _neighbors = Convert.ToInt32(values[2]);
            if (_transType == TransType.RigidTrans || _transType == TransType.SimilarityTrans || _transType == TransType.AffineTrans || _transType == TransType.AffineVectorXYTrans)
            {
                fullFileName = fileDir + "\\" + ItemName + "_CalibCoordMatrix.ini";
                if (!File.Exists(fullFileName))
                {
                    return Errortype.CALIBCOORD_MATRIX_FILE_NOT_EXIST;
                }

                IniHelper.GetAllKeyValues("Matrix_Src2Dst", out keys, out values, fullFileName);
                if (values.Length != 6)
                {
                    return Errortype.CALIBCOORD_MATRIXSRC2DST_VALUE_LENGTH_ERROR;
                }

                _matrixSrc2Dst = new double[6];
                for (int i = 0; i < 6; i++)
                {
                    _matrixSrc2Dst[i] = Convert.ToDouble(values[i]);
                }

                IniHelper.GetAllKeyValues("Matrix_Dst2Src", out keys, out values, fullFileName);
                if (values.Length != 6)
                {
                    return Errortype.CALIBCOORD_MATRIXDST2SRC_VALUE_LENGTH_ERROR;
                }

                _matrixDst2Src = new double[6];
                for (int i = 0; i < 6; i++)
                {
                    _matrixDst2Src[i] = Convert.ToDouble(values[i]);
                }
            }
            else
            {
                fullFileName = fileDir + "\\" + ItemName + "_CalibCoordPoints.ini";
                if (!File.Exists(fullFileName))
                {
                    return Errortype.CALIBCOORD_POINTS_FILE_NOT_EXIST;
                }

                IniHelper.GetAllKeyValues("PointsNum", out keys, out values, fullFileName);
                int pointsNum = Convert.ToInt32(values[0]);

                _srcPoints = new List<Point>();
                _dstPoints = new List<Point>();
                using (StreamReader sr = new StreamReader(fullFileName))
                {
                    string line = string.Empty;
                    int index = 0;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string strValue = "[Point_" + index.ToString() + "]";
                        if (line == strValue)
                        {
                            double[] axisValue = new double[4];
                            for (int j = 0; j < 4; ++j)
                            {
                                string tempLine = sr.ReadLine();
                                string[] tempLines = tempLine.Split('=');
                                axisValue[j] = double.Parse(tempLines[1]);
                            }

                            _srcPoints.Add(new Point(axisValue[0], axisValue[1]));
                            _dstPoints.Add(new Point(axisValue[2], axisValue[3]));
                            index++;
                        }
                    }
                }

                //create tree
                Errortype ret = CalibAffineKdTrans();
                if (ret != Errortype.OK)
                {
                    return ret;
                }
            }

            IsCalibed = true;
            return Errortype.OK;
        }

        /// <summary>
        /// 计算运动系标定误差
        /// </summary>
        /// <param name="currentRulers">输入理论光栅点集</param>
        /// <param name="expectedReals">输入理论真值点集</param>
        /// <param name="maxError">最大标定误差</param>
        /// <param name="meanError">平均标定误差</param>
        /// <param name="stdDev">均方根标定误差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalcMotionError(List<Point> currentRulers, List<Point> expectedReals, out Point maxError, out Point meanError, out Point stdDev)
        {
            maxError = new Point();
            meanError = new Point();
            stdDev = new Point();
            if (currentRulers == null || expectedReals == null)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            if (currentRulers.Count != expectedReals.Count)
            {
                return Errortype.INPUT_SIZE_NOT_EQUAL;
            }

            List<Point> currentRealList = new List<Point>();
            for (int i = 0; i < currentRulers.Count; i++)
            {
                Errortype ret = Src2Dst(currentRulers[i], out Point currentReal, out List<Point> errorTemp);
                if (ret != Errortype.OK)
                {
                    return ret;
                }

                currentRealList.Add(currentReal);
            }

            // 计算真值与理论真值之间的仿射变换关系
            var result = ComAlgo.VectorToHomMat2d(currentRealList, expectedReals, out double[] mat, out List<Point> error);
            double[] errorX = new double[error.Count];
            double[] errorY = new double[error.Count];
            for (int i = 0; i < error.Count; i++)
            {
                errorX[i] = error[i].X;
                errorY[i] = error[i].Y;
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
            stdDev = new Point(stdDevX, stdDevY);

            // 数据赋值
            _verifyPtsRulerIn = currentRulers;
            _verifyPtsRealIn = expectedReals;
            _verifyPtsRealTrue = currentRealList;
            _verifyPtsError = error;

            return Errortype.OK;
        }
    }
}