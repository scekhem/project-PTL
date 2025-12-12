using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HalconDotNet;
using System.Reflection;
using System.Runtime;
using OpenCvSharp;

namespace DataStruct
{
    /// <summary>
    /// 图像参数
    /// </summary>
    public class Camera : IDisposable
    {
        private int _height;
        private int _width;
        private int _channel;
        private string _pixType;
        private IntPtr _dataPtr;  //图像数据地址
        private IntPtr _dataPtr_r;
        private IntPtr _dataPtr_g;
        private IntPtr _dataPtr_b;

        /// <summary>
        /// Gets the user's _height
        /// </summary>
        public int Height
        {
            get { return _height; }
        }

        /// <summary>
        /// Gets the user's _width
        /// </summary>
        public int Width
        {
            get { return _width; }
        }

        /// <summary>
        /// Gets the user's _pixType
        /// </summary>
        public string Type
        {
            get { return _pixType; }
        }

        /// <summary>
        /// Gets the user's _channel
        /// </summary>
        public int Channel { get => _channel; }

        /// <summary>
        /// Gets the user's _dataPtr
        /// </summary>
        public IntPtr Ptr
        {
            get { return _dataPtr; }
        }

        /// <summary>
        /// Gets the user's _dataPtr_r
        /// </summary>
        public IntPtr Ptr_R
        {
            get { return _dataPtr_r; }
        }

        /// <summary>
        /// Gets the user's _dataPtr_g
        /// </summary>
        public IntPtr Ptr_G
        {
            get { return _dataPtr_g; }
        }

        /// <summary>
        /// Gets the user's _dataPtr_b
        /// </summary>
        public IntPtr Ptr_B
        {
            get { return _dataPtr_b; }
        }

        /// <summary>
        /// 初始化图像数据
        /// </summary>
        /// <param name="hImage">图像数据</param>
        private void InitializeImage(HObject hImage)
        {
            if (hImage is null || !hImage.IsInitialized())
            {
                return;
            }

            HOperatorSet.GenEmptyObj(out HObject emptyObject);
            HOperatorSet.TestEqualObj(emptyObject, hImage, out HTuple isempty);
            emptyObject.Dispose();
            if (isempty)
            {
                return;
            }

            HOperatorSet.CountChannels(hImage, out HTuple channelNum);
            if (channelNum == 1)
            {
                HOperatorSet.GetImagePointer1(hImage, out HTuple hPointer, out HTuple hType, out HTuple imgWidth, out HTuple imgHeight);
                _pixType = hType;
                if (_pixType == "byte")
                {
                    _height = imgHeight;
                    _width = imgWidth;
                    _channel = 1;
                    int dataLength = imgWidth * imgHeight;
                    _dataPtr = Marshal.AllocHGlobal(sizeof(Byte) * dataLength);
                    byte[] gray = new byte[imgWidth * imgHeight];
                    Marshal.Copy(hPointer, gray, 0, dataLength);
                    Marshal.Copy(gray, 0, _dataPtr, dataLength);
                }
            }
            else if (channelNum == 3)
            {
                HOperatorSet.GetImagePointer3(hImage, out HTuple hPointerR, out HTuple hPointerG, out HTuple hPointerB, out HTuple hType, out HTuple imgWidth, out HTuple imgHeight);
                _pixType = hType;
                if (_pixType == "byte")
                {
                    _height = imgHeight;
                    _width = imgWidth;
                    _channel = 3;
                    int dataLength = imgWidth * imgHeight;
                    _dataPtr_r = Marshal.AllocHGlobal(sizeof(Byte) * dataLength);
                    _dataPtr_g = Marshal.AllocHGlobal(sizeof(Byte) * dataLength);
                    _dataPtr_b = Marshal.AllocHGlobal(sizeof(Byte) * dataLength);
                    byte[] data_R = new byte[imgWidth * imgHeight];
                    byte[] data_G = new byte[imgWidth * imgHeight];
                    byte[] data_B = new byte[imgWidth * imgHeight];
                    Marshal.Copy(hPointerR, data_R, 0, dataLength);
                    Marshal.Copy(data_R, 0, _dataPtr_r, dataLength);
                    Marshal.Copy(hPointerG, data_G, 0, dataLength);
                    Marshal.Copy(data_G, 0, _dataPtr_g, dataLength);
                    Marshal.Copy(hPointerB, data_B, 0, dataLength);
                    Marshal.Copy(data_B, 0, _dataPtr_b, dataLength);
                }
            }
        }

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public Camera()
        {
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~Camera()
        {
            Dispose();
        }

        /// <summary>
        /// 从文件创建图像
        /// </summary>
        /// <param name="fullFilePath">文件名</param>
        public Camera(string fullFilePath)
        {
            if (!File.Exists(fullFilePath))
            {
                return;
            }

            HOperatorSet.ReadImage(out HObject hImage, fullFilePath);
            InitializeImage(hImage);
            hImage.Dispose();
        }

        /// <summary>
        /// 从数据创建图像（单通道）
        /// </summary>
        /// <param name="height">图像高</param>
        /// <param name="width">图像宽</param>
        /// <param name="type">图像数据类型</param>
        /// <param name="ptr">图像数据指针</param>
        public Camera(int height, int width, string type, IntPtr ptr)
        {
            _height = height;
            _width = width;
            _pixType = type;
            _channel = 1;
            _dataPtr = ptr;

            int dataLength = height * width;
            _dataPtr = Marshal.AllocHGlobal(sizeof(byte) * dataLength);
            byte[] gray = new byte[dataLength];
            Marshal.Copy(ptr, gray, 0, dataLength);
            Marshal.Copy(gray, 0, _dataPtr, dataLength);
        }

        /// <summary>
        /// 从数据创建图像（三通道）
        /// </summary>
        /// <param name="height">图像高</param>
        /// <param name="width">图像宽</param>
        /// <param name="type">图像数据类型</param>
        /// <param name="ptrR">图像数据R通道指针</param>
        /// <param name="ptrG">图像数据G通道指针</param>
        /// <param name="ptrB">图像数据B通道指针</param>
        public Camera(int height, int width, string type, IntPtr ptrR, IntPtr ptrG, IntPtr ptrB)
        {
            _height = height;
            _width = width;
            _pixType = type;
            _channel = 3;
            _dataPtr_r = ptrR;
            _dataPtr_g = ptrG;
            _dataPtr_b = ptrB;

            int dataLength = height * width;
            _dataPtr_r = Marshal.AllocHGlobal(sizeof(byte) * dataLength);
            _dataPtr_g = Marshal.AllocHGlobal(sizeof(byte) * dataLength);
            _dataPtr_b = Marshal.AllocHGlobal(sizeof(byte) * dataLength);
            byte[] data_R = new byte[dataLength];
            byte[] data_G = new byte[dataLength];
            byte[] data_B = new byte[dataLength];
            Marshal.Copy(ptrR, data_R, 0, dataLength);
            Marshal.Copy(data_R, 0, _dataPtr_r, dataLength);
            Marshal.Copy(ptrG, data_G, 0, dataLength);
            Marshal.Copy(data_G, 0, _dataPtr_g, dataLength);
            Marshal.Copy(ptrB, data_B, 0, dataLength);
            Marshal.Copy(data_B, 0, _dataPtr_b, dataLength);
        }

        /// <summary>
        /// 从HObject创建图像
        /// </summary>
        /// <param name="hImage">HObject数据</param>
        public Camera(HObject hImage)
        {
            InitializeImage(hImage);
        }

        /// <summary>
        /// 创建HObject数据
        /// </summary>
        /// <returns>HObject数据</returns>
        public HObject GenHObject()
        {
            HOperatorSet.GenEmptyObj(out HObject cameraObject);
            if ((Width > 0) && (Height > 0))
            {
                if (Channel == 3)
                {
                    if (Type == "byte")
                    {
                        HOperatorSet.GenImage3(out cameraObject, "byte", Width, Height, _dataPtr_r, _dataPtr_g, _dataPtr_b);
                    }
                }
                else if (Channel == 1)
                {
                    if (Type == "byte")
                    {
                        HOperatorSet.GenImage1(out cameraObject, "byte", Width, Height, _dataPtr);
                    }
                }
            }

            return cameraObject;
        }

        /// <summary>
        /// 图像拷贝（深拷贝）
        /// </summary>
        /// <returns>图像数据</returns>
        public Camera Clone()
        {
            Camera copyCamera = new Camera();
            if ((Height < 1) || (Width < 1))
            {
                return copyCamera;
            }

            copyCamera._height = this.Height;
            copyCamera._width = this.Width;
            copyCamera._channel = this.Channel;
            copyCamera._pixType = this.Type;
            if (Channel == 1)
            {
                if (Type == "byte")
                {
                    int dataLength = Height * Width;
                    copyCamera._dataPtr = Marshal.AllocHGlobal(sizeof(Byte) * dataLength);
                    byte[] gray = new byte[dataLength];
                    Marshal.Copy(this._dataPtr, gray, 0, dataLength);
                    Marshal.Copy(gray, 0, copyCamera._dataPtr, dataLength);
                }
            }
            else if (Channel == 3)
            {
                if (Type == "byte")
                {
                    int dataLength = Height * Width;
                    copyCamera._dataPtr_r = Marshal.AllocHGlobal(sizeof(Byte) * dataLength);
                    copyCamera._dataPtr_g = Marshal.AllocHGlobal(sizeof(Byte) * dataLength);
                    copyCamera._dataPtr_b = Marshal.AllocHGlobal(sizeof(Byte) * dataLength);
                    byte[] data_R = new byte[dataLength];
                    byte[] data_G = new byte[dataLength];
                    byte[] data_B = new byte[dataLength];
                    Marshal.Copy(_dataPtr_r, data_R, 0, dataLength);
                    Marshal.Copy(data_R, 0, copyCamera._dataPtr_r, dataLength);
                    Marshal.Copy(_dataPtr_g, data_G, 0, dataLength);
                    Marshal.Copy(data_G, 0, copyCamera._dataPtr_g, dataLength);
                    Marshal.Copy(_dataPtr_b, data_B, 0, dataLength);
                    Marshal.Copy(data_B, 0, copyCamera._dataPtr_b, dataLength);
                }
            }

            return copyCamera;
        }

        /// <summary>
        /// 图像内存释放
        /// </summary>
        public void Dispose()
        {
            if (_dataPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_dataPtr);
                _dataPtr = IntPtr.Zero;
            }

            if (_dataPtr_r != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_dataPtr_r);
                _dataPtr_r = IntPtr.Zero;
            }

            if (_dataPtr_g != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_dataPtr_g);
                _dataPtr_g = IntPtr.Zero;
            }

            if (_dataPtr_b != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_dataPtr_b);
                _dataPtr_b = IntPtr.Zero;
            }

            _height = 0;
            _width = 0;
            _channel = 0;

            //GC.Collect();
            //GC.WaitForPendingFinalizers();

            //GC.Collect();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 图像保存
        /// </summary>
        /// <param name="fileName">图像保存文件名，bmp格式</param>
        /// <returns>OK:成功,其他:失败</returns>
        public Errortype Save(string fileName)
        {
            if (fileName is null)
            {
                return Errortype.SAVE_INPUT_NULL;
            }

            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));
            }

            HObject img = this.GenHObject();
            HOperatorSet.WriteImage(img, "bmp", 0, fileName);
            img.Dispose();
            return Errortype.OK;
        }
    }
}
