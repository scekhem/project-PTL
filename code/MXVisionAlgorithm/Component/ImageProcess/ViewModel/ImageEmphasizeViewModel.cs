using DataStruct;
using MaxwellFramework.Core.Interfaces;
using MX10UBDBU01AA.Common;
using MXVisionAlgorithm.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
//using System.Windows.Shapes;
using UltrapreciseBonding.FusionCollections;

namespace MXVisionAlgorithm.Component.ImageProcess.ViewModel
{
    public class ImageEmphasizeViewModel : AbstractBaseViewModel, IPage
    {
        public string Name { get; set; } = "ImageEmphasizeView";

        /// <summary>
        /// 多张图像选择的名字
        /// </summary>
        private string _selectImageName = string.Empty;
        public string SelectImageName
        {
            get { return _selectImageName; }
            set { _selectImageName = value; OnPropertyChanged(nameof(SelectImageName)); }
        }

        /// <summary>
        /// 保存路径
        /// </summary>
        private string _savePath = string.Empty;
        public string SavePath
        {
            get { return _savePath; }
            set { _savePath = value; OnPropertyChanged(nameof(SavePath)); }
        }

        /// <summary>
        /// gauss size
        /// </summary>
        private int _gaussSize = 0;
        public int GaussSize
        {
            get { return _gaussSize; }
            set { _gaussSize = value; OnPropertyChanged(nameof(GaussSize)); }
        }

        /// <summary>
        /// power level
        /// </summary>
        private int _powerLevel = 0;
        public int PowerLevel
        {
            get { return _powerLevel; }
            set { _powerLevel = value; OnPropertyChanged(nameof(PowerLevel)); }
        }

        /// <summary>
        /// 窗口图像
        /// </summary>
        public WriteableBitmap _image;
        public WriteableBitmap Image
        {
            get { return _image; }
            set { _image = value; OnPropertyChanged(nameof(Image)); }
        }

        public ImageEmphasizeViewModel()
        {
            GaussSize = 5;
            PowerLevel = 3;
        }


        public void EmphasizeClick()
        {
            if (Image is null)
            {
                MessageBox.Show("请加载图片");
                return;
            }
            DataStruct.Camera img = ImageHelper.GetCamera(Image);
            Errortype ret = ImagePreprocess.ImageEmphasize(img, out Camera imgOut, GaussSize, PowerLevel);
            if (ret != Errortype.OK)
            {
                MaxwellControl.Controls.MessageBox.Show("增强失败,Error:" + ret.ToString());
                return;
            }
            Image = ImageHelper.Camera2WritableBitmap(imgOut);

            img.Dispose();
            imgOut.Dispose();
        }

        private List<KeyValuePair<string, Camera>> _multiImgCameras = new List<KeyValuePair<string, Camera>>();

        /// <summary>
        /// 释放所有多选的图像
        /// </summary>
        private void ReleaseMultiImgCameras()
        {
            foreach (var img in _multiImgCameras)
            {
                img.Value.Dispose();
            }

            _multiImgCameras.Clear();
        }

        /// <summary>
        /// 选择多张图像
        /// </summary>
        public void SelectMultiImageClick()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image File (*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif)|*.bmp;*.png;*.jpg;*.jpeg;*.gif;*.tif";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK || openFileDialog.ShowDialog() == DialogResult.Yes)
            {
                ReleaseMultiImgCameras();
                try
                {
                    List<string> ImportFiles = openFileDialog.FileNames.ToList();
                    if (ImportFiles.Count == 0 || ImportFiles == null)
                    {
                        MaxwellControl.Controls.MessageBox.Show("未选择图片文件，或选择文件不存在！请重新操作");
                        return;
                    }
                    SelectImageName = string.Empty;
                    ImportFiles.ForEach(f =>
                    {
                        Camera img = new Camera(f);
                        string imgName = System.IO.Path.GetFileName(f);
                        SelectImageName += f;
                        SelectImageName += " ";
                        _multiImgCameras.Add(new KeyValuePair<string, Camera>(imgName, img));
                    });
                    MaxwellControl.Controls.MessageBox.Show("图片加载完成！");
                }
                catch (Exception)
                {
                    MaxwellControl.Controls.MessageBox.Show("格式不正确！");
                }
            }
        }

        public void SelectSavePathClick()
        {
            FolderBrowserDialog dilog = new FolderBrowserDialog();
            dilog.Description = "请选择文件夹";
            if (dilog.ShowDialog() == DialogResult.OK || dilog.ShowDialog() == DialogResult.Yes)
            {
                SavePath = dilog.SelectedPath;
            }
        }

        public void TransImageClick()
        {
            foreach (var img in _multiImgCameras)
            {
                string imgName = Path.GetFileName(img.Key);
                string saveName = Path.Combine(SavePath, imgName);
                Errortype ret = ImagePreprocess.ImageEmphasize(img.Value, out Camera imgOut, GaussSize, PowerLevel);
                if (ret != Errortype.OK)
                {
                    MaxwellControl.Controls.MessageBox.Show("增强失败,文件名为:" + imgName + ",Error:" + ret.ToString());
                    return;
                }
                imgOut.Save(saveName);
                imgOut.Dispose();
            }
            MaxwellControl.Controls.MessageBox.Show("转换完成");
        }
    }
}
