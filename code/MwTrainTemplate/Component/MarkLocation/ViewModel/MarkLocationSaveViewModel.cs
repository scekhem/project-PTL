using DataStruct;
using MaxwellControl.Language;
using MaxwellFramework.Core.Interfaces;
using MwFramework.ManagerService;
using Stylet;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using MwTrainTemplate.Common;

namespace MwTrainTemplate.Component.MarkLocation.ViewModel
{
    public class MarkLocationSaveViewModel : Screen
    {
        public string Name = "MarkLocationSaveView";

        private string PatternPath = string.Empty;

        /// <summary>
        /// cto
        /// </summary>
        /// <param name="writeableBitmap"></param>
        public MarkLocationSaveViewModel(WriteableBitmap writeableBitmap, string PaternName)
        {
            TemplateBmp = writeableBitmap;
            FileName = PaternName;
        }

        /// <summary>
        /// 模板图像源
        /// </summary>
        private WriteableBitmap _templateBmp;

        public WriteableBitmap TemplateBmp
        {
            get { return _templateBmp; }
            set
            {
                _templateBmp = value;
                OnPropertyChanged(nameof(TemplateBmp));
            }
        }

        /// <summary>
        ///模板保存基目录
        /// </summary>
        private string _fileName = string.Empty;

        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                OnPropertyChanged(nameof(FileName));
            }
        }

        /// <summary>
        /// 保存模板
        /// </summary>
        public void Save()
        {
            // SVA调用保存接口
            PatternPath = AppDomain.CurrentDomain.BaseDirectory + "MarkModel";
            if (!Directory.Exists(PatternPath))
            {
                Directory.CreateDirectory(PatternPath);
            }
            if (FileName != null)
            {
                Errortype ret = MarkLocationManagerService.GetInstance().CreatePattern(FileName);
                if (ret == Errortype.OK)
                {
                    ret = MarkLocationManagerService.GetInstance().SavePattern(PatternPath);
                    if (ret == Errortype.OK)
                    {
                        string message = LangGet.GetMessage("SVAViewModel_templatesaved_successfully");
                        MaxwellControl.Controls.MessageBox.Show(message);
                    }
                }
            }
            else
            {
                string message = LangGet.GetMessage("SVAViewModel_templatenameBlank");

                MaxwellControl.Controls.MessageBox.Show(message);
            }
        }
    }
}