using DataStruct;
using MwFramework.Device.Model;
using System;
using System.Configuration;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;

namespace MwTrainTemplate.Common
{
    /// <summary>
    /// 读取图片
    /// </summary>
    public static class ImageHelper
    {
        /// <summary>
        /// 创建图片
        /// </summary>
        public static BitmapImage CreateBitmapImage(string filePath)
        {
            if (File.Exists(filePath))
            {
                BinaryReader binReader = new BinaryReader(File.Open(filePath, FileMode.Open));
                FileInfo fileInfo = new FileInfo(filePath);
                byte[] bytes = binReader.ReadBytes((int)fileInfo.Length);
                binReader.Close();

                BitmapImage imageSource = new BitmapImage();
                imageSource.BeginInit();
                imageSource.StreamSource = new MemoryStream(bytes);
                imageSource.EndInit();

                return imageSource;
            }
            return null;
        }

        //将Bitmap 转换成WriteableBitmap
        public static WriteableBitmap BitmapToWriteableBitmap(System.Drawing.Bitmap src)
        {
            var wb = CreateCompatibleWriteableBitmap(src);
            System.Drawing.Imaging.PixelFormat format = src.PixelFormat;
            if (wb == null)
            {
                wb = new WriteableBitmap(src.Width, src.Height, 0, 0, System.Windows.Media.PixelFormats.Gray8, null);
                format = System.Drawing.Imaging.PixelFormat.Format8bppIndexed;
            }
            BitmapCopyToWriteableBitmap(src, wb, new System.Drawing.Rectangle(0, 0, src.Width, src.Height), 0, 0, format);
            return wb;
        }

        //创建尺寸和格式与Bitmap兼容的WriteableBitmap
        public static WriteableBitmap CreateCompatibleWriteableBitmap(System.Drawing.Bitmap src)
        {
            System.Windows.Media.PixelFormat format;
            switch (src.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                    format = System.Windows.Media.PixelFormats.Gray8;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                    format = System.Windows.Media.PixelFormats.Bgr555;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                    format = System.Windows.Media.PixelFormats.Bgr565;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    format = System.Windows.Media.PixelFormats.Bgr24;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    format = System.Windows.Media.PixelFormats.Bgr32;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                    format = System.Windows.Media.PixelFormats.Pbgra32;
                    break;

                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    format = System.Windows.Media.PixelFormats.Bgra32;
                    break;

                default:
                    return null;
            }
            return new WriteableBitmap(src.Width, src.Height, 0, 0, format, null);
        }

        //将Bitmap数据写入WriteableBitmap中
        public static void BitmapCopyToWriteableBitmap(System.Drawing.Bitmap src, WriteableBitmap dst, System.Drawing.Rectangle srcRect, int destinationX, int destinationY, System.Drawing.Imaging.PixelFormat srcPixelFormat)
        {
            var data = src.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), src.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, srcPixelFormat);
            dst.WritePixels(new Int32Rect(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height), data.Scan0, data.Height * data.Stride, data.Stride, destinationX, destinationY);
            src.UnlockBits(data);
        }

        /// <summary>
        /// 保存WriteableBitmap图像
        /// </summary>
        /// <param name="wtbBmp"></param>
        /// <param name="strDir"></param>
        public static void SaveWriteableBitmap(WriteableBitmap wtbBmp, string strDir)
        {
            if (wtbBmp == null)
            {
                return;
            }
            try
            {
                RenderTargetBitmap rtbitmap = new RenderTargetBitmap(wtbBmp.PixelWidth, wtbBmp.PixelHeight, wtbBmp.DpiX, wtbBmp.DpiY, PixelFormats.Default);
                DrawingVisual drawingVisual = new DrawingVisual();
                using (var dc = drawingVisual.RenderOpen())
                {
                    dc.DrawImage(wtbBmp, new Rect(0, 0, wtbBmp.Width, wtbBmp.Height));
                }
                rtbitmap.Render(drawingVisual);
                BmpBitmapEncoder bitmapEncoder = new BmpBitmapEncoder();
                //  JpegBitmapEncoder bitmapEncoder = new JpegBitmapEncoder();
                bitmapEncoder.Frames.Add(BitmapFrame.Create(rtbitmap));
                string strpath = strDir + DateTime.Now.ToString("yyyyMMddfff") + ".bmp";
                if (!Directory.Exists(strDir))
                {
                    Directory.CreateDirectory(strDir);
                }
                if (!File.Exists(strpath))
                {
                    bitmapEncoder.Save(File.OpenWrite(strpath));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public static BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wbm)
        {
            BitmapImage bmImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbm));
                encoder.Save(stream);
                bmImage.BeginInit();
                bmImage.CacheOption = BitmapCacheOption.OnLoad;
                bmImage.StreamSource = stream;
                bmImage.EndInit();
                bmImage.Freeze();
            }
            return bmImage;
        }

        /// <summary>
        /// 创建图片
        /// </summary>
        /// <param name="byt"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="bitCnt"></param>
        /// <returns></returns>
        public static WriteableBitmap GenerateWritableBitmap(byte[] byt, int width, int height, int bitCnt)
        {
            WriteableBitmap image = new WriteableBitmap(
                        width,
                        height,
                        96,
                        96,
                      bitCnt == 1 ? System.Windows.Media.PixelFormats.Gray8 : (bitCnt == 3 ? System.Windows.Media.PixelFormats.Bgr24 : System.Windows.Media.PixelFormats.Bgra32),
                        null);
            Application.Current.Dispatcher.Invoke(() =>
            {
                image.Lock();
                System.Runtime.InteropServices.Marshal.Copy(byt, 0, image.BackBuffer, byt.Length);
                image.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
                image.Unlock();
            });

            return image;
        }

        public static WriteableBitmap UpdateWritableBitmap(byte[] byt, int width, int height, int ch)
        {
            WriteableBitmap writeableBitmap = null;
            bool flag = writeableBitmap == null || writeableBitmap.Width != (double)width || writeableBitmap.Height != (double)height || (writeableBitmap.Format != PixelFormats.Gray8 && ch == 1) || (writeableBitmap.Format != PixelFormats.Bgr24 && ch == 3) || (writeableBitmap.Format != PixelFormats.Bgr32 && ch == 4);
            if (flag)
            {
                PixelFormat pixelFormat = PixelFormats.Default;
                bool flag2 = ch == 1;
                if (flag2)
                {
                    pixelFormat = PixelFormats.Gray8;
                }
                bool flag3 = ch == 3;
                if (flag3)
                {
                    pixelFormat = PixelFormats.Bgr24;
                }
                bool flag4 = ch == 4;
                if (flag4)
                {
                    pixelFormat = PixelFormats.Bgr32;
                }
                writeableBitmap = new WriteableBitmap(width, height, 96.0, 96.0, pixelFormat, null);
            }
            writeableBitmap.WritePixels(new Int32Rect(0, 0, width, height), byt, ch * width, 0);
            return writeableBitmap;
        }

        /// <summary>
        /// WriteableBitmap转Bitmap
        /// </summary>
        /// <param name="writeBmp"></param>
        /// <returns></returns>
        public static System.Drawing.Bitmap BitmapFromWriteableBitmap(WriteableBitmap writeBmp)
        {
            System.Drawing.Bitmap bmp;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create((BitmapSource)writeBmp));
                enc.Save(outStream);
                bmp = new System.Drawing.Bitmap(outStream);
            }
            return bmp;
        }

        /// <summary>
        /// 获取Camera对象
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static Camera GetCamera(WriteableBitmap bmp)
        {
            try
            {
                Camera camera = null;

                if (bmp != null)
                {
                    if (Application.Current.Dispatcher.CheckAccess())
                    {
                        camera = new Camera(Convert.ToInt32(bmp.Height), Convert.ToInt32(bmp.Width), "byte", bmp.BackBuffer);
                    }
                    else
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            camera = new Camera(Convert.ToInt32(bmp.Height), Convert.ToInt32(bmp.Width), "byte", bmp.BackBuffer);
                        });
                    }
                }

                return camera;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取Camera对象
        /// </summary>
        /// <param name="cameraData"></param>
        /// <returns></returns>
        public static Camera GetCamera(CameraData cameraData)
        {
            try
            {
                Camera camera = null;
                if (!(cameraData is null))
                {
                    int num = cameraData.BufferData.Length;
                    IntPtr intPtr = Marshal.AllocHGlobal(num);
                    Marshal.Copy(cameraData.BufferData, 0, intPtr, num);
                    camera = new Camera(Convert.ToInt32(cameraData.Height), Convert.ToInt32(cameraData.Width), "byte", intPtr);
                    Marshal.FreeHGlobal(intPtr);
                }
                else
                {
                    //AppFrameworkLogService.WriteInfoLog("Framework", $"ImageHelper  GetCamera  cameraData is null");
                }
                return camera;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// 判断灰度或彩色
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static WriteableBitmap Camera2WritableBitmap(Camera img)
        {
            if (img == null)
            {
                MaxwellControl.Controls.MessageBox.Show("Get Empty Image and Exit Abnormally！！！");
                return null;
            }
            if (img.Channel == 1)
            {
                return CameraGray2WritableBitmap(img);
            }
            else
            {
                return CameraRGB2WritableBitmap(img);
            }
        }

        /// <summary>
        /// 单通道Camera转WriteableBitmap
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static WriteableBitmap CameraGray2WritableBitmap(Camera img)
        {
            byte[] byt = new byte[img.Width * img.Height * img.Channel];
            Marshal.Copy(img.Ptr, byt, 0, img.Width * img.Height * img.Channel);
            int bitCnt = img.Channel;

            WriteableBitmap currentImage = new WriteableBitmap(
            img.Width,
            img.Height,
            96,
            96,
          bitCnt == 1 ? System.Windows.Media.PixelFormats.Gray8 : (bitCnt == 3 ? System.Windows.Media.PixelFormats.Bgr24 : System.Windows.Media.PixelFormats.Bgra32),
            null);

            Application.Current.Dispatcher.Invoke(() =>
            {
                currentImage.Lock();
                System.Runtime.InteropServices.Marshal.Copy(byt, 0, currentImage.BackBuffer, byt.Length);
                currentImage.AddDirtyRect(new System.Windows.Int32Rect(0, 0, img.Width, img.Height));
                currentImage.Unlock();
            });

            return currentImage;
        }

        ///// <summary>
        ///// 单通道Camera转WriteableBitmap
        ///// </summary>
        ///// <param name="img">img</param>
        ///// <returns>WriteableBitmap</returns>
        //public static WriteableBitmap CameraGray2WritableBitmap(Camera img)
        //{
        //    byte[] byt = new byte[img.Width * img.Height * img.Channel];
        //    Marshal.Copy(img.Ptr, byt, 0, img.Width * img.Height * img.Channel);
        //    int bitCnt = img.Channel;

        //    WriteableBitmap currentImage = new WriteableBitmap(
        //        img.Width,
        //        img.Height,
        //        96,
        //        96,
        //        System.Windows.Media.PixelFormats.Gray8,
        //        null);

        //    currentImage.WritePixels(new Int32Rect(0, 0, img.Width, img.Height), byt, img.Width * PixelFormats.Gray8.BitsPerPixel / 8, 0, 0);
        //    return currentImage;
        //}
        /// <summary>
        /// 获取彩色WriteableBitmap
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        public static WriteableBitmap CameraRGB2WritableBitmap(Camera img)
        {
            byte[] byt = new byte[img.Width * img.Height * img.Channel];

            byte[] bytR = new byte[img.Width * img.Height];
            byte[] bytG = new byte[img.Width * img.Height];
            byte[] bytB = new byte[img.Width * img.Height];
            Marshal.Copy(img.Ptr_B, bytB, 0, img.Width * img.Height);
            Marshal.Copy(img.Ptr_G, bytG, 0, img.Width * img.Height);
            Marshal.Copy(img.Ptr_R, bytR, 0, img.Width * img.Height);

            for (int i = 0; i < bytR.Length; i++)
            {
                byt[i * 3 + 0] = bytB[i];
                byt[i * 3 + 1] = bytG[i];
                byt[i * 3 + 2] = bytR[i];
            }

            int bitCnt = img.Channel;

            int width = img.Width;
            int height = img.Height;

            WriteableBitmap currentImage = new WriteableBitmap(img.Width, img.Height, 96, 96, bitCnt == 1 ? System.Windows.Media.PixelFormats.Gray8 : (bitCnt == 3 ? System.Windows.Media.PixelFormats.Bgr24 : System.Windows.Media.PixelFormats.Bgra32), null);

            currentImage.WritePixels(new Int32Rect(0, 0, width, height), byt, width * PixelFormats.Bgr24.BitsPerPixel / 8, 0, 0);

            return currentImage;
        }

        /// <summary>
        /// 图像增强（具体是否启用及参数由配置文件决定，默认false）
        /// </summary>
        /// <param name="image">原始图像</param>
        /// <returns>增强后图像</returns>
        public static WriteableBitmap CameraEnhancedImage(WriteableBitmap inWriteableBitmapImage)
        {
            //调用自己的配置文件，决定是否启用图像增强算法及相关的配置值
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = System.Environment.CurrentDirectory + "/Configuration-me/AVMConfig/CameraEnhancedImage.config";
            Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            if (configuration.HasFile == true && configuration.AppSettings.Settings["IsUseEnhancedImage"].Value == "true")
            {
                int gaussFilter;
                int multNum;

                int.TryParse(configuration.AppSettings.Settings["GaussFilter"].Value, out gaussFilter);
                int.TryParse(configuration.AppSettings.Settings["MultNum"].Value, out multNum);

                gaussFilter = gaussFilter > 0 ? gaussFilter : 5;
                multNum = multNum > 0 ? multNum : 2;

                Camera imageInData = ImageHelper.GetCamera(inWriteableBitmapImage);
                UltrapreciseBonding.FusionCollections.ImagePreprocess.ImageEmphasize(imageInData, out Camera imageOutData, gaussFilter, multNum);
                WriteableBitmap outWriteableBitmapImage = ImageHelper.Camera2WritableBitmap(imageOutData);

                imageInData.Dispose();
                imageOutData.Dispose();

                return outWriteableBitmapImage;
            }
            else
            {
                return inWriteableBitmapImage;
            }
        }

        public static Camera CameraEnhancedImage(Camera imageInData)
        {
            //调用自己的配置文件，决定是否启用图像增强算法及相关的配置值
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = System.Environment.CurrentDirectory + "/Configuration-me/AVMConfig/CameraEnhancedImage.config";
            Configuration configuration = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            if (configuration.HasFile == true && configuration.AppSettings.Settings["IsUseEnhancedImage"].Value == "true")
            {
                int gaussFilter;
                int multNum;

                int.TryParse(configuration.AppSettings.Settings["GaussFilter"].Value, out gaussFilter);
                int.TryParse(configuration.AppSettings.Settings["MultNum"].Value, out multNum);

                gaussFilter = gaussFilter > 0 ? gaussFilter : 5;
                multNum = multNum > 0 ? multNum : 2;

                UltrapreciseBonding.FusionCollections.ImagePreprocess.ImageEmphasize(imageInData, out Camera imageOutData, gaussFilter, multNum);

                return imageOutData;
            }
            else
            {
                return imageInData;
            }
        }
    }
}