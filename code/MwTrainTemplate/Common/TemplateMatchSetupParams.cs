using DataStruct;
using MwFramework.ManagerService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MwTrainTemplate.Common
{

    [Export(typeof(IParameter))]
    public class TrainNccTemplateSetupParameter : ParameterBase
    {
        public ObservableCollection<TrainSetupParameterItem> CreateParamsItems { get; set; } = new ObservableCollection<TrainSetupParameterItem>();

        public ObservableCollection<TrainSetupEnumItem> CreateEnumsItems { get; set; } = new ObservableCollection<TrainSetupEnumItem>();

        public ObservableCollection<TrainSetupParameterItem> MatchParamsItems { get; set; } = new ObservableCollection<TrainSetupParameterItem>();

        public ObservableCollection<TrainSetupEnumItem> MatchEnumsItems { get; set; } = new ObservableCollection<TrainSetupEnumItem>();

        /// <summary>
        /// 初始化默认参数
        /// </summary>
        public void Initialize()
        {
            if (CreateParamsItems.Count == 0)
            {
                CreateParamsItems.Add(new TrainSetupParameterItem("Pyramid", "-1","","-1","8","0"));
                CreateParamsItems.Add(new TrainSetupParameterItem("AngleStart", "-0.39", "", "-3.14", "3.14"));
                CreateParamsItems.Add(new TrainSetupParameterItem("AngleExtent", "0.79", "", "0", "6.29"));
                CreateParamsItems.Add(new TrainSetupParameterItem("FilterSize", "5", "", "3", "7", "0"));
            }
            if (CreateEnumsItems.Count == 0)
            {
                CreateEnumsItems.Add(new TrainSetupEnumItem("AngleStep", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "auto", "0.0349", "0.0524", "0.0698", "0.0873" }
                });
                CreateEnumsItems.Add(new TrainSetupEnumItem("Metric", 1, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "ignore_global_polarity", "use_polarity" }
                });
                CreateEnumsItems.Add(new TrainSetupEnumItem("SmoothImg", 1, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "true", "false" }
                });
            }

            if (MatchParamsItems.Count == 0)
            {
                MatchParamsItems.Add(new TrainSetupParameterItem("Pyramid", "0", "", "-1", "8", "0"));
                MatchParamsItems.Add(new TrainSetupParameterItem("AngleStart", "-0.39", "", "-3.14", "3.14"));
                MatchParamsItems.Add(new TrainSetupParameterItem("AngleExtent", "0.79", "", "0", "6.29"));
                MatchParamsItems.Add(new TrainSetupParameterItem("MinScore", "0.6","","0","1"));
                MatchParamsItems.Add(new TrainSetupParameterItem("NumMatches", "1", "", "0", "20", "0"));
                MatchParamsItems.Add(new TrainSetupParameterItem("MaxOverlap", "0.5", "", "0", "1"));
                MatchParamsItems.Add(new TrainSetupParameterItem("FilterSize", "5", "", "3", "7", "0"));
            }

            if (MatchEnumsItems.Count == 0)
            {
                MatchEnumsItems.Add(new TrainSetupEnumItem("SubPixel", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "true", "false" }
                });
                MatchEnumsItems.Add(new TrainSetupEnumItem("SmoothImg", 1, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "true", "false" }
                });
            }
        }

        public override void Copy(IParameter source)
        {
            TrainNccTemplateSetupParameter sp = source as TrainNccTemplateSetupParameter;
            if (sp != null && sp.CreateParamsItems != null)
            {
                CreateParamsItems = new ObservableCollection<TrainSetupParameterItem>();
                foreach (var item in sp.CreateParamsItems)
                {
                    CreateParamsItems.Add((TrainSetupParameterItem)item.Clone());
                }
            }

            if (sp != null && sp.CreateEnumsItems != null)
            {
                CreateEnumsItems = new ObservableCollection<TrainSetupEnumItem>();
                foreach (var item in sp.CreateEnumsItems)
                {
                    CreateEnumsItems.Add((TrainSetupEnumItem)item.Clone());
                }
            }

            if (sp != null && sp.MatchParamsItems != null)
            {
                MatchParamsItems = new ObservableCollection<TrainSetupParameterItem>();
                foreach (var item in sp.MatchParamsItems)
                {
                    MatchParamsItems.Add((TrainSetupParameterItem)item.Clone());
                }
            }

            if (sp != null && sp.MatchEnumsItems != null)
            {
                MatchEnumsItems = new ObservableCollection<TrainSetupEnumItem>();
                foreach (var item in sp.MatchEnumsItems)
                {
                    MatchEnumsItems.Add((TrainSetupEnumItem)item.Clone());
                }
            }
        }
    }

    [Export(typeof(IParameter))]
    public class TrainShapeTemplateSetupParameter : ParameterBase
    {
        public ObservableCollection<TrainSetupParameterItem> CreateParamsItems { get; set; } = new ObservableCollection<TrainSetupParameterItem>();

        public ObservableCollection<TrainSetupEnumItem> CreateEnumsItems { get; set; } = new ObservableCollection<TrainSetupEnumItem>();

        public ObservableCollection<TrainSetupParameterItem> MatchParamsItems { get; set; } = new ObservableCollection<TrainSetupParameterItem>();

        public ObservableCollection<TrainSetupEnumItem> MatchEnumsItems { get; set; } = new ObservableCollection<TrainSetupEnumItem>();

        /// <summary>
        /// 初始化默认参数
        /// </summary>
        public void Initialize()
        {
            if (CreateParamsItems.Count == 0)
            {
                CreateParamsItems.Add(new TrainSetupParameterItem("Pyramid", "-1", "", "-1", "7", "0"));
                CreateParamsItems.Add(new TrainSetupParameterItem("AngleStart", "-0.39", "", "-3.14", "3.14"));
                CreateParamsItems.Add(new TrainSetupParameterItem("AngleExtent", "0.79", "", "0", "6.29"));
                CreateParamsItems.Add(new TrainSetupParameterItem("ScaleMin", "0.9", "", "0.5", "1"));
                CreateParamsItems.Add(new TrainSetupParameterItem("ScaleMax", "1.1", "", "1", "1.5"));
                CreateParamsItems.Add(new TrainSetupParameterItem("FilterSize", "5", "", "3", "7", "0"));
            }
            if (CreateEnumsItems.Count == 0)
            {
                CreateEnumsItems.Add(new TrainSetupEnumItem("AngleStep", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "auto", "0.0349", "0.0524", "0.0698", "0.0873" }
                });
                CreateEnumsItems.Add(new TrainSetupEnumItem("ScaleStep", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "auto", "0.01", "0.02", "0.05", "0.1", "0.15", "0.2" }
                });
                CreateEnumsItems.Add(new TrainSetupEnumItem("Optimization", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "auto", "no_pregeneration", "none", "point_reduction_high", "point_reduction_low", "point_reduction_medium", "pregeneration" }
                });
                CreateEnumsItems.Add(new TrainSetupEnumItem("Metric", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "ignore_global_polarity", "use_polarity" }
                });
                CreateEnumsItems.Add(new TrainSetupEnumItem("Contrast", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "auto", "auto_contrast", "auto_contrast_hyst", "auto_min_size", "10", "20", "30", "40", "60", "80", "100", "120", "140", "160" }
                });
                CreateEnumsItems.Add(new TrainSetupEnumItem("MinContrast", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "auto", "1", "2", "3", "5", "7", "10", "20", "30", "40" }
                });
                CreateEnumsItems.Add(new TrainSetupEnumItem("SmoothImg", 1, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "true", "false" }
                });
            }

            if (MatchParamsItems.Count == 0)
            {
                MatchParamsItems.Add(new TrainSetupParameterItem("Pyramid", "0", "", "-1", "7", "0"));
                MatchParamsItems.Add(new TrainSetupParameterItem("AngleStart", "-0.39", "", "-3.14", "3.14"));
                MatchParamsItems.Add(new TrainSetupParameterItem("AngleExtent", "0.79", "", "0", "6.29"));
                MatchParamsItems.Add(new TrainSetupParameterItem("ScaleMin", "0.9", "", "0.5", "1"));
                MatchParamsItems.Add(new TrainSetupParameterItem("ScaleMax", "1.1", "", "1", "1.5"));
                MatchParamsItems.Add(new TrainSetupParameterItem("MinScore", "0.9", "", "0", "1"));
                MatchParamsItems.Add(new TrainSetupParameterItem("NumMatches", "0", "", "0", "20", "0"));
                MatchParamsItems.Add(new TrainSetupParameterItem("MaxOverlap", "0.5", "", "0", "1"));
                MatchParamsItems.Add(new TrainSetupParameterItem("FilterSize", "5", "", "3", "7", "0"));
                MatchParamsItems.Add(new TrainSetupParameterItem("Greediness", "0", "", "0", "1"));
            }

            if (MatchEnumsItems.Count == 0)
            {
                MatchEnumsItems.Add(new TrainSetupEnumItem("SubPixel", 0, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "none", "interpolation", "least_squares", "least_squares_high", "least_squares_very_high" }
                });
                MatchEnumsItems.Add(new TrainSetupEnumItem("SmoothImg", 1, string.Empty, string.Empty)
                {
                    TemplateType = new List<string>() { "true", "false" }
                });
            }
        }

        public override void Copy(IParameter source)
        {
            TrainShapeTemplateSetupParameter sp = source as TrainShapeTemplateSetupParameter;
            if (sp != null && sp.CreateParamsItems != null)
            {
                CreateParamsItems = new ObservableCollection<TrainSetupParameterItem>();
                foreach (var item in sp.CreateParamsItems)
                {
                    CreateParamsItems.Add((TrainSetupParameterItem)item.Clone());
                }
            }

            if (sp != null && sp.CreateEnumsItems != null)
            {
                CreateEnumsItems = new ObservableCollection<TrainSetupEnumItem>();
                foreach (var item in sp.CreateEnumsItems)
                {
                    CreateEnumsItems.Add((TrainSetupEnumItem)item.Clone());
                }
            }

            if (sp != null && sp.MatchParamsItems != null)
            {
                MatchParamsItems = new ObservableCollection<TrainSetupParameterItem>();
                foreach (var item in sp.MatchParamsItems)
                {
                    MatchParamsItems.Add((TrainSetupParameterItem)item.Clone());
                }
            }

            if (sp != null && sp.MatchEnumsItems != null)
            {
                MatchEnumsItems = new ObservableCollection<TrainSetupEnumItem>();
                foreach (var item in sp.MatchEnumsItems)
                {
                    MatchEnumsItems.Add((TrainSetupEnumItem)item.Clone());
                }
            }
        }
    }
}
