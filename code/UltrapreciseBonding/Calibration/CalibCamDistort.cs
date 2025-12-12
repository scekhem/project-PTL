using DataStruct;
using IniFileHelper;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UltrapreciseBonding.Calib;
using Point = DataStruct.Point;

namespace UltrapreciseBonding.Calib
{
    /// <summary>
    /// CalibDistortBase
    /// </summary>
    internal class CalibDistortBase : CalibItem
    {
        private int _height = 0;
        private int _width = 0;
        private double[] _cameraMatrix = new double[9] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
        private double[] _distCoeffs = new double[5] { 1, 0, 0, 0, 1 };
        private double[] _rvecs = new double[3] { 1, 0, 0 };
        private double[] _tvecs = new double[3] { 1, 0, 0 };

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="optic_name">相机名称</param>
        public CalibDistortBase(string optic_name)
        {
            ItemName = optic_name;
            IsCalibed = false;
        }

        /// <summary>
        /// Gets or Sets the _height
        /// </summary>
        public int Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        /// Gets or Sets the _width
        /// </summary>
        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// Gets or Sets the _cameraMatrix
        /// </summary>
        public double[] CameraMatrix
        {
            get { return _cameraMatrix; }
            set { _cameraMatrix = value; }
        }

        /// <summary>
        /// Gets or Sets the _distCoeffs
        /// </summary>
        public double[] DistCoeffs
        {
            get { return _distCoeffs; }
            set { _distCoeffs = value; }
        }

        /// <summary>
        /// Gets or Sets the _rvecs
        /// </summary>
        public double[] Rvecs
        {
            get { return _rvecs; }
            set { _rvecs = value; }
        }

        /// <summary>
        /// Gets or Sets the _tvecs
        /// </summary>
        public double[] Tvecs
        {
            get { return _tvecs; }
            set { _tvecs = value; }
        }

        /// <summary>
        /// 设置图像尺寸
        /// </summary>
        /// <param name="height">图像高</param>
        /// <param name="width">图像宽</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetImageSize(int height, int width)
        {
            Height = height;
            Width = width;
            return Errortype.OK;
        }

        /// <summary>
        /// 初始化相机内参标定参数
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype InitLensDistort()
        {
            CameraMatrix = new double[9] { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
            DistCoeffs = new double[5] { 0, 0, 0, 0, 0 };
            Rvecs = new double[3] { 1, 0, 0 };
            Tvecs = new double[3] { 1, 0, 0 };
            IsCalibed = true;
            return Errortype.OK;
        }

        /// <summary>
        /// 相机标定(获取内外参数以及畸变系数)
        /// </summary>
        /// <param name="pixs">像素坐标集</param>
        /// <param name="realpts">真值坐标集</param>
        /// <param name="rms">重投影误差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalcLensDistort(List<Point> pixs, List<Point> realpts, out double rms)
        {
            rms = 0;
            if (pixs is null || realpts is null)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            if (pixs.Count != realpts.Count)
            {
                return Errortype.INPUT_SIZE_NOT_EQUAL;
            }

            if (Height == 0 || Width == 0)
            {
                return Errortype.CALIBDISTORT_IMAGE_SIZE_ERROR;
            }

            try
            {
                OpenCvSharp.Size imgSize = new OpenCvSharp.Size(Width, Height);
                OpenCvSharp.Mat[] real_corners;
                real_corners = new OpenCvSharp.Mat[1];
                OpenCvSharp.Mat[] pix_corners;
                pix_corners = new OpenCvSharp.Mat[1];
                List<OpenCvSharp.Point2f[]> imagesPoints = new List<OpenCvSharp.Point2f[]>();
                List<OpenCvSharp.Point3f[]> realPoints = new List<OpenCvSharp.Point3f[]>();
                OpenCvSharp.Point3f[] real_pts = new OpenCvSharp.Point3f[pixs.Count];
                OpenCvSharp.Point2f[] pix_pts = new OpenCvSharp.Point2f[pixs.Count];
                for (int i = 0; i < pixs.Count; ++i)
                {
                    real_pts[i] = new OpenCvSharp.Point3f((float)realpts[i].X, (float)realpts[i].Y, 0.0f);
                    pix_pts[i] = new OpenCvSharp.Point2f((float)pixs[i].X, (float)pixs[i].Y); // ****像素坐标X、Y不需要互换
                }

                OpenCvSharp.Mat pix_mat = OpenCvSharp.Mat.FromArray<OpenCvSharp.Point2f>(pix_pts);
                pix_corners[0] = pix_mat;
                OpenCvSharp.Mat real_mat = OpenCvSharp.Mat.FromArray<OpenCvSharp.Point3f>(real_pts);
                real_corners[0] = real_mat;
                OpenCvSharp.Size size_img;
                size_img = new OpenCvSharp.Size(Width, Height);
                OpenCvSharp.Mat cam_mat = OpenCvSharp.Mat.Eye(3, 3, OpenCvSharp.MatType.CV_32FC1); //创建一个3x3的单位矩阵_cam_mat，用于存储相机矩阵
                OpenCvSharp.Size size_mat;
                size_mat = new OpenCvSharp.Size(1, 5); //创建一个OpenCV的Size对象Size_mat，表示畸变系数的大小，其中1表示行数，5表示列数
                OpenCvSharp.Mat cam_distors = OpenCvSharp.Mat.Zeros(size_mat, OpenCvSharp.MatType.CV_32FC1); // 创建一个大小为1x5的全零矩阵_cam_distors，用于存储畸变系数

                rms = OpenCvSharp.Cv2.CalibrateCamera(real_corners, pix_corners, size_img, cam_mat, cam_distors, out OpenCvSharp.Mat[] rvecs, out OpenCvSharp.Mat[] tvecs); //调用OpenCV的CalibrateCamera函数，将真实世界中的点坐标和像素点的坐标作为输入，计算相机矩阵、畸变系数、旋转向量、平移向量，并返回均方根误差rms

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        CameraMatrix[i * 3 + j] = cam_mat.Get<double>(i, j); //获取3X3的相机内参矩阵
                    }
                }

                for (int i = 0; i < 5; i++)
                {
                    DistCoeffs[i] = cam_distors.Get<double>(0, i);
                }

                Rvecs[0] = rvecs[0].Get<double>(0, 0);
                Rvecs[1] = rvecs[0].Get<double>(0, 1);
                Rvecs[2] = rvecs[0].Get<double>(0, 2);

                Tvecs[0] = tvecs[0].Get<double>(0, 0);
                Tvecs[1] = tvecs[0].Get<double>(0, 1);
                Tvecs[2] = tvecs[0].Get<double>(0, 2);
                IsCalibed = true;
                return Errortype.OK;
            }
            catch (SystemException expDefaultException)
            {
                IsCalibed = false;
                return Errortype.CALIBDISTORT_ISNOT_COMPLET_ERROR;
            }
        }

        /// <summary>
        /// 畸变矫正-单个点（在畸变校正之前先要进行相机标定获取内外参数以及畸变系数）
        /// </summary>
        /// <param name="pix">待矫正的像素点</param>
        /// <param name="correctedPix">矫正后的像素点</param>
        /// <param name="realval">矫正后的像素点经过透视变换转换为真值点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CamCorrect(Point pix, out Point correctedPix, out Point realval)
        {
            correctedPix = new Point();
            realval = new Point();
            if (pix == null)
            {
                return Errortype.INPUT_POINTS_NULL;
            }

            if (IsCalibed == false)
            {
                return Errortype.CALIBDISTORT_ISNOT_COMPLET_ERROR;
            }

            OpenCvSharp.Mat ptlist = new OpenCvSharp.Mat(1, 1, OpenCvSharp.MatType.CV_32FC2),   //该Mat对象是一个1x1的矩阵，数据类型为CV_32FC2（32位浮点型，通道数为2）
                                  ptUndistort = new OpenCvSharp.Mat(1, 1, OpenCvSharp.MatType.CV_32FC2);  //创建两个OpenCV的Mat对象ptlist和ptUndistort，分别用于存储像素点的坐标和矫正后的坐标。

            OpenCvSharp.Point2f tmppix = new OpenCvSharp.Point2f((float)pix.X, (float)pix.Y);  //****像素坐标X、Y不需要互换
            ptlist.Set<OpenCvSharp.Point2f>(0, 0, tmppix); //将转换后的像素点坐标tmppix赋值给ptlist的第一个元素。

            OpenCvSharp.Point2f tmpundist = new OpenCvSharp.Point2f();
            ptUndistort.Set<OpenCvSharp.Point2f>(0, 0, tmpundist);

            OpenCvSharp.Cv2.UndistortPoints(ptlist, ptUndistort, BuildCameraMatrixFromData(CameraMatrix), BuildCameraDistortFromData(DistCoeffs)); //调用OpenCV的UndistortPoints函数，对ptlist进行畸变矫正，使用相机内参矩阵CameraMatrix和畸变系数DistCoeffs作为输入。

            OpenCvSharp.Point2f tmp_distor = ptUndistort.Get<OpenCvSharp.Point2f>(0, 0); //从ptUndistort中获取矫正后的坐标tmp_distor
            Point nurmPix = new Point(tmp_distor.X, tmp_distor.Y); //归一化坐标
            correctedPix = new Point(nurmPix.X * CameraMatrix[0] + CameraMatrix[2], nurmPix.Y * CameraMatrix[4] + CameraMatrix[5]); //反归一化

            ReprojectPoints(1, nurmPix, out realval, _rvecs, _tvecs); //调用ReprojectPoints函数，将tmp_real作为输入进行投影变换，得到真实世界中的坐标，并将结果保存在realval变量中。
            return Errortype.OK;
        }

        /// <summary>
        /// 将double[]类型转换为OpenCvSharp.Mat类型（将数组转换为矩阵，3行1列）
        /// </summary>
        /// <param name="inputdata">double[]类型数据</param>
        /// <returns>输出OpenCvSharp.Mat类型的数据</returns>
        private OpenCvSharp.Mat BuildCameraDistortFromData(double[] inputdata)
        {
            OpenCvSharp.Mat camera_Distort = new OpenCvSharp.Mat();
            try
            {
                int row = inputdata.GetUpperBound(0); //获取输入数组inputdata的最高索引（即元素数量减1）
                camera_Distort = new OpenCvSharp.Mat(row + 1, 1, OpenCvSharp.MatType.CV_32FC1, OpenCvSharp.Scalar.Black);
                for (int j = 0; j <= row; j++)
                {
                    camera_Distort.Set<float>(j, 0, (float)inputdata[j]);
                }
            }
            catch (Exception)
            {
                camera_Distort?.Dispose();
                camera_Distort = null;
            }

            return camera_Distort;
        }

        /// <summary>
        /// 将double[]类型转换为OpenCvSharp.Mat类型（将数组转换为矩阵，3行3列）
        /// </summary>
        /// <param name="inputdata">double[]类型数据</param>
        /// <returns>输出OpenCvSharp.Mat类型的数据</returns>
        private OpenCvSharp.Mat BuildCameraMatrixFromData(double[] inputdata)
        {
            OpenCvSharp.Mat camera_Matrix = null;
            try
            {
                camera_Matrix = new OpenCvSharp.Mat(3, 3, OpenCvSharp.MatType.CV_32FC1, OpenCvSharp.Scalar.Black);
                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        camera_Matrix.Set<float>(j, i, (float)inputdata[j * 3 + i]);
                    }
                }
            }
            catch (Exception)
            {
                camera_Matrix?.Dispose();
                camera_Matrix = null;
            }

            return camera_Matrix;
        }

        /// <summary>
        /// 透视变换
        /// </summary>
        /// <param name="num">数量</param>
        /// <param name="imagePt">像素点</param>
        /// <param name="realPt">真值点</param>
        /// <param name="rvec">旋转向量</param>
        /// <param name="tvec">平移向量</param>
        private Errortype ReprojectPoints(int num, Point imagePt, out Point realPt, double[] rvec, double[] tvec)
        {
            OpenCvSharp.Mat matR = new OpenCvSharp.Mat(3, 3, OpenCvSharp.MatType.CV_64FC1);
            OpenCvSharp.Mat rVec = BuildCameraDistortFromData(rvec);
            OpenCvSharp.Cv2.Rodrigues(rVec, matR); //使用罗德里格斯公式将旋转向量转换为3X3旋转矩阵

            double x = imagePt.X;
            double y = imagePt.Y;

            double a = matR.Get<float>(0, 0) - x * matR.Get<float>(2, 0);
            double b = matR.Get<float>(0, 1) - x * matR.Get<float>(2, 1);
            double c = matR.Get<float>(1, 0) - y * matR.Get<float>(2, 0);
            double d = matR.Get<float>(1, 1) - y * matR.Get<float>(2, 1);
            double det = a * d - b * c;
            double t1 = x * tvec[2] - tvec[0];
            double t2 = y * tvec[2] - tvec[1];
            realPt = new Point((d * t1 - b * t2) / det, (-c * t1 + a * t2) / det);
            return Errortype.OK;
        }

        /// <summary>
        /// 保存相机内外参
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Save(string fileDir)
        {
            if (!IsCalibed)
            {
                return Errortype.CALIBDISTORT_ISNOT_COMPLET_ERROR;
            }

            if (fileDir is null)
            {
                return Errortype.CALIBDISTORT_SAVE_FILEPATH_NULL;
            }

            if (fileDir.Length < 1)
            {
                return Errortype.CALIBDISTORT_SAVE_FILEPATH_LENGTH_ERROR;
            }

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            if (!Directory.Exists(fileDir))
            {
                return Errortype.CALIBDISTORT_SAVE_FILE_DIR_NOT_EXIST_ERROR;
            }

            string fullFileName = fileDir + ItemName + "_Intrinsic.ini";
            List<string> keys = new List<string>
            {
            "item_name",
            "height", "width",
            "camera_matrix_0", "camera_matrix_1", "camera_matrix_2",
            "camera_matrix_3", "camera_matrix_4", "camera_matrix_5",
            "camera_matrix_6", "camera_matrix_7", "camera_matrix_8",
            "dist_coeffs_0", "dist_coeffs_1", "dist_coeffs_2", "dist_coeffs_3", "dist_coeffs_4",
            "rvecs_0", "rvecs_1", "rvecs_2",
            "tvecs_0", "tvecs_1", "tvecs_2",
            };
            List<string> value = new List<string>
            {
            ItemName,
            Height.ToString(), Width.ToString(),
            CameraMatrix[0].ToString(), CameraMatrix[1].ToString(), CameraMatrix[2].ToString(),
            CameraMatrix[3].ToString(), CameraMatrix[4].ToString(), CameraMatrix[5].ToString(),
            CameraMatrix[6].ToString(), CameraMatrix[7].ToString(), CameraMatrix[8].ToString(),
            DistCoeffs[0].ToString(), DistCoeffs[1].ToString(), DistCoeffs[2].ToString(), DistCoeffs[3].ToString(), DistCoeffs[4].ToString(),
            Rvecs[0].ToString(), Rvecs[1].ToString(), Rvecs[2].ToString(),
            Tvecs[0].ToString(), Tvecs[1].ToString(), Tvecs[2].ToString(),
            };
            IniHelper.AddSectionWithKeyValues("info", keys, value, fullFileName);
            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public override Errortype Load(string fileDir)
        {
            string fullFileName = fileDir + ItemName + "_Intrinsic.ini";
            if (!System.IO.File.Exists(fullFileName))
            {
                return Errortype.PARAMS_LOAD_FILE_NOT_EXIST;
            }

            if (!IniHelper.ExistSection("info", fullFileName))
            {
                return Errortype.PARAMS_FILE_LOAD_ERROR;
            }

            var intrinsicParams = IniHelper.GetAllValues("info", fullFileName);
            if (intrinsicParams.Count != 23)
            {
                return Errortype.PARAMS_FILE_LOAD_ERROR;
            }

            Height = Convert.ToInt32(intrinsicParams[1]);
            Width = Convert.ToInt32(intrinsicParams[2]);
            CameraMatrix[0] = Convert.ToDouble(intrinsicParams[3]);
            CameraMatrix[1] = Convert.ToDouble(intrinsicParams[4]);
            CameraMatrix[2] = Convert.ToDouble(intrinsicParams[5]);
            CameraMatrix[3] = Convert.ToDouble(intrinsicParams[6]);
            CameraMatrix[4] = Convert.ToDouble(intrinsicParams[7]);
            CameraMatrix[5] = Convert.ToDouble(intrinsicParams[8]);
            CameraMatrix[6] = Convert.ToDouble(intrinsicParams[9]);
            CameraMatrix[7] = Convert.ToDouble(intrinsicParams[10]);
            CameraMatrix[8] = Convert.ToDouble(intrinsicParams[11]);
            DistCoeffs[0] = Convert.ToDouble(intrinsicParams[12]);
            DistCoeffs[1] = Convert.ToDouble(intrinsicParams[13]);
            DistCoeffs[2] = Convert.ToDouble(intrinsicParams[14]);
            DistCoeffs[3] = Convert.ToDouble(intrinsicParams[15]);
            DistCoeffs[4] = Convert.ToDouble(intrinsicParams[16]);
            Rvecs[0] = Convert.ToDouble(intrinsicParams[17]);
            Rvecs[1] = Convert.ToDouble(intrinsicParams[18]);
            Rvecs[2] = Convert.ToDouble(intrinsicParams[19]);
            Tvecs[0] = Convert.ToDouble(intrinsicParams[20]);
            Tvecs[1] = Convert.ToDouble(intrinsicParams[21]);
            Tvecs[2] = Convert.ToDouble(intrinsicParams[22]);
            IsCalibed = true;
            return Errortype.OK;
        }
    }

    /// <summary>
    /// CalibDistort
    /// </summary>
    public class CalibDistort : Singleton<CalibDistort>
    {
        private List<CalibDistortBase> _opticDistortCalibList = new List<CalibDistortBase>();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Init(List<string> itemName)
        {
            foreach (var name in itemName)
            {
                CalibDistortBase opticDistortCalibBase = _opticDistortCalibList.Find(e => e.ItemName == name);
                if (opticDistortCalibBase != null)
                {
                    opticDistortCalibBase = new CalibDistortBase(name);
                }
                else
                {
                    _opticDistortCalibList.Add(new CalibDistortBase(name));
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 设置图像尺寸
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="height">图像高</param>
        /// <param name="width">图像宽</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype SetImageSize(string itemName, int height, int width)
        {
            CalibDistortBase opticDistortCalibBase = _opticDistortCalibList.Find(e => e.ItemName == itemName);
            if (opticDistortCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticDistortCalibBase.SetImageSize(height, width);
        }

        /// <summary>
        /// 初始化相机内参标定参数
        /// </summary>
        /// <param name="itemName">相机名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype InitCamDistort(string itemName)
        {
            CalibDistortBase opticDistortCalibBase = _opticDistortCalibList.Find(e => e.ItemName == itemName);
            if (opticDistortCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            return opticDistortCalibBase.InitLensDistort();
        }

        /// <summary>
        /// 相机畸变标定
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pixs">像素点集</param>
        /// <param name="realpts">真值点集</param>
        /// <param name="rms">重投影误差</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalibCamDistort(string itemName, List<Point> pixs, List<Point> realpts, out double rms)
        {
            CalibDistortBase opticDistortCalibBase = _opticDistortCalibList.Find(e => e.ItemName == itemName);
            if (opticDistortCalibBase == null)
            {
                rms = 0;
                return Errortype.OPT_NAME_NULL;
            }

            return opticDistortCalibBase.CalcLensDistort(pixs, realpts, out rms);
        }

        /// <summary>
        /// 畸变矫正（单个点）
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pix">待矫正像素点</param>
        /// <param name="correctedPix">矫正后像素点</param>
        /// <param name="realval">矫正后像素点经过透视变换得到的真值点</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype DistortCorrect(string itemName, Point pix, out Point correctedPix, out Point realval)
        {
            CalibDistortBase opticDistortCalibBase = _opticDistortCalibList.Find(e => e.ItemName == itemName);
            if (opticDistortCalibBase == null)
            {
                correctedPix = new Point(0, 0);
                realval = new Point(0, 0);
                return Errortype.OPT_NAME_NULL;
            }

            return opticDistortCalibBase.CamCorrect(pix, out correctedPix, out realval);
        }

        /// <summary>
        /// 畸变校正（点集）
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="pixs">待矫正像素点集</param>
        /// <param name="correctedPixs">矫正后像素点集</param>
        /// <param name="reals">矫正后像素点集经过透视变换得到真值点集</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype DistortCorrect(string itemName, List<Point> pixs, out List<Point> correctedPixs, out List<Point> reals)
        {
            CalibDistortBase opticDistortCalibBase = _opticDistortCalibList.Find(e => e.ItemName == itemName);
            if (opticDistortCalibBase == null)
            {
                correctedPixs = new List<Point>();
                reals = new List<Point>();
                return Errortype.OPT_NAME_NULL;
            }

            correctedPixs = new List<Point>();
            reals = new List<Point>();
            for (int i = 0; i < pixs.Count; i++)
            {
                Errortype result = opticDistortCalibBase.CamCorrect(pixs[i], out Point correctedPix, out Point real);
                if (result == Errortype.OK)
                {
                    correctedPixs.Add(correctedPix);
                    reals.Add(real);
                }
                else
                {
                    return result;
                }
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 计算镜头光学畸变率（图像四个角的光学畸变率）
        /// </summary>
        /// <param name="itemName">标定项名称</param>
        /// <param name="cornerDistortRate">输出四个角畸变率数值</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype CalcOpticDistortRate(string itemName, out List<double> cornerDistortRate)
        {
            cornerDistortRate = new List<double> { 0.0, 0.0, 0.0, 0.0 };
            CalibDistortBase opticDistortCalibBase = _opticDistortCalibList.Find(e => e.ItemName == itemName);
            if (opticDistortCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            int imgHeight = opticDistortCalibBase.Height;
            int imgWidth = opticDistortCalibBase.Width;
            Point opticCenter = new Point(imgWidth / 2.0, imgHeight / 2.0);
            List<Point> cornerPoints = new List<Point>
            {
                new Point(0.0, 0.0),
                new Point(0.0, imgHeight - 1),
                new Point(imgWidth - 1, 0.0),
                new Point(imgWidth - 1, imgHeight - 1),
            };
            var ret = DistortCorrect(itemName, cornerPoints, out List<Point> correctedCorner, out List<Point> rms);
            if (ret != Errortype.OK)
            {
                return ret;
            }

            for (int i = 0; i < cornerPoints.Count; i++)
            {
                double distDiff = cornerPoints[i].DistanceTo(correctedCorner[i]);
                double correctedDist = correctedCorner[i].DistanceTo(opticCenter);
                cornerDistortRate[i] = distDiff / correctedDist;
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 获取标定状态
        /// </summary>
        /// <param name="itemName">标定名称</param>
        /// <param name="calibStaus">标定状态</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype GetCalibStatus(string itemName, out bool calibStaus)
        {
            calibStaus = false;
            CalibDistortBase opticDistortCalibBase = _opticDistortCalibList.Find(e => e.ItemName == itemName);
            if (opticDistortCalibBase == null)
            {
                return Errortype.OPT_NAME_NULL;
            }

            calibStaus = opticDistortCalibBase.IsCalibed;
            return Errortype.OK;
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Release()
        {
            _opticDistortCalibList = new List<CalibDistortBase>();
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

            for (int index = 0; index < _opticDistortCalibList.Count; index++)
            {
                Errortype ret = _opticDistortCalibList[index].Save(fileDir);
                saveReturn.Add(_opticDistortCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <param name="loadReturn">加载结果返回</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Load(string fileDir, out Dictionary<string, Errortype> loadReturn)
        {
            loadReturn = new Dictionary<string, Errortype>();

            for (int index = 0; index < _opticDistortCalibList.Count; index++)
            {
                Errortype ret = _opticDistortCalibList[index].Load(fileDir);
                loadReturn.Add(_opticDistortCalibList[index].ItemName, ret);
            }

            return Errortype.OK;
        }
    }
}

