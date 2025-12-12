using DataStruct;
using MaxwellFramework.Core.Interfaces;
using MwFramework.ManagerService;
using MXVisionAlgorithm.Common;
using Stylet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace MXVisionAlgorithm.Component.TrainPattern.ViewModel
{
    public partial class DisplayTemplateViewModel : Screen
    {

        private string PatternPath = string.Empty;
        private IParamList ParameterService { get; }

        /// <summary>
        /// cto
        /// </summary>
        /// <param name="writeableBitmap"></param>
        public DisplayTemplateViewModel(WriteableBitmap writeableBitmap, string PaternName)
        {
            ParameterService = IoC.Get<IParameterManager>() as IParamList;
            _templateBmp = writeableBitmap;
            _fileName = PaternName;

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
            PatternPath = AppDomain.CurrentDomain.BaseDirectory + "MarkModel";
            if (!Directory.Exists(PatternPath))
            {
                Directory.CreateDirectory(PatternPath);
            }
            if (FileName != null)
            {
                Errortype ret = MarkLocationManagerService.GetInstance().CreateNccPattern(FileName);
                if (ret == Errortype.OK)
                {
                    ret = MarkLocationManagerService.GetInstance().SaveNccPattern(PatternPath);
                    if (ret == Errortype.OK)
                    {
                        MaxwellControl.Controls.MessageBox.Show("模板保存成功！");

                    }
                }
            }
            else
            {
                MaxwellControl.Controls.MessageBox.Show("保存模板名称为空！！！");
            }
        }

    }

    public static class CommonMarkPath
    {
        //上table mark同轴度
        public static string TopLeftTableMark { get; } = "TopLeftTableMark";
        public static string TopRightTableMark { get; } = "TopRightTableMark";
        public static string BottomLeftTableMark { get; } = "BottomLeftTableMark";
        public static string BottomRightTableMark { get; } = "BottomRightTableMark";

        //下wafer Mark识别
        public static string TopLeftWaferMark { get; } = "TopLeftWaferMark";
        public static string TopRightWaferMark { get; } = "TopRightWaferMark";

        //下Table PEC Mark识别
        public static string TopLeftPECMark { get; } = "TopLeftPECMark";
        public static string TopRightPECMark { get; } = "TopRightPECMark";

        //上wafer Mark识别
        public static string BottomLeftWaferMark { get; } = "BottomLeftWaferMark";
        public static string BottomRightWaferMark { get; } = "BottomRightWaferMark";

        public static List<string> CommonMark = new List<string>()
        {
            TopLeftTableMark,
            TopRightTableMark,
            BottomLeftTableMark,
            BottomRightTableMark,
            TopLeftWaferMark,
            TopRightWaferMark,
            TopLeftPECMark,
            TopRightPECMark,
            BottomLeftWaferMark,
            BottomRightWaferMark,
        };
    }
}
