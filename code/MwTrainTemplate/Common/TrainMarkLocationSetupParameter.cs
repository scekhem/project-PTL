using DataStruct;
using MwFramework.ManagerService;
using Stylet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Documents;
using System.Xml.Serialization;
using UltrapreciseBonding.PatternManager;

namespace MwTrainTemplate.Common
{
    [Export(typeof(IParameter))]
    public class TrainMarkLocationSetupParameter : ParameterBase
    {
        public ObservableCollection<TrainSetupParameterItem> TrainSetupParameterItems { get; set; } = new ObservableCollection<TrainSetupParameterItem>();

        public ObservableCollection<TrainSetupEnumItem> ComboboxTrainSetupParameterItems { get; set; } = new ObservableCollection<TrainSetupEnumItem>();

        public void Initialize()
        {
            if (TrainSetupParameterItems.Count == 0)
            {
                TrainSetupParameterItems.Add(new TrainSetupParameterItem("MinLength", "10", ""));
                TrainSetupParameterItems.Add(new TrainSetupParameterItem("EdgeThreshold", "30", ""));
                TrainSetupParameterItems.Add(new TrainSetupParameterItem("ScoreThreshold", "0.8", "", "0", "1"));
            }

            if (ComboboxTrainSetupParameterItems.Count == 0)
            {
                ComboboxTrainSetupParameterItems.Add(new TrainSetupEnumItem(nameof(TemplateType), 0, string.Empty, typeof(TemplateDisplayType).Name)
                { TemplateType = Enum.GetValues(typeof(TemplateDisplayType)).Cast<TemplateDisplayType>().Select(v => v.ToString()).ToList() });
                ComboboxTrainSetupParameterItems.Add(new TrainSetupEnumItem(nameof(PatternMetric), 0, string.Empty, typeof(PatternMetric).Name)
                { TemplateType = Enum.GetValues(typeof(PatternMetric)).Cast<PatternMetric>().Select(v => v.ToString()).ToList() });
                ComboboxTrainSetupParameterItems.Add(new TrainSetupEnumItem(nameof(LinePolarity), 0, string.Empty, typeof(LinePolarity).Name)
                { TemplateType = Enum.GetValues(typeof(LinePolarity)).Cast<LinePolarity>().Select(v => v.ToString()).ToList() });
            }
        }

        public override void Copy(IParameter source)
        {
            TrainMarkLocationSetupParameter sp = source as TrainMarkLocationSetupParameter;
            if (sp != null && sp.TrainSetupParameterItems != null)
            {
                TrainSetupParameterItems = new ObservableCollection<TrainSetupParameterItem>();
                foreach (var item in sp.TrainSetupParameterItems)
                {
                    TrainSetupParameterItems.Add((TrainSetupParameterItem)item.Clone());
                }
            }

            if (sp != null && sp.ComboboxTrainSetupParameterItems != null)
            {
                ComboboxTrainSetupParameterItems = new ObservableCollection<TrainSetupEnumItem>();
                foreach (var item in sp.ComboboxTrainSetupParameterItems)
                {
                    ComboboxTrainSetupParameterItems.Add((TrainSetupEnumItem)item.Clone());
                }
            }
        }
    }

    public enum TemplateDisplayType
    {
        Gray = 0,
        Edge = 1,
        Edge2 = 2,
    }

    public enum PatternMetric
    {
        Use = 0,
        IgnoreGlobal = 1,
        IgnoreLocal = 2,
    }

    public enum LinePolarity
    {
        On = 0,
        Off = 1,
    }
}
