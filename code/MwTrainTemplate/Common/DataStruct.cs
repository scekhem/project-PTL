using MwFramework.ManagerService;
using Stylet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MwTrainTemplate.Common
{
    public class MatchParamter
    {
        public string ParameterInfo { get; set; }
        public double ParameterX { get; set; }
        public double ParameterY { get; set; }
        public double ParameterRatation { get; set; }
        public double ParameterScore { get; set; }
        public MatchParamter(string parameterInfo, double parameterX, double parameterY, double parameterRatation, double parameterScore)
        {
            ParameterInfo = parameterInfo;
            ParameterX = parameterX;
            ParameterY = parameterY;
            ParameterRatation = parameterRatation;
            ParameterScore = parameterScore;

        }
        public MatchParamter()
        {

        }
    }

    /// <summary>
    /// 绘制图形
    /// </summary>
    public enum DrawShapes
    {
        None = 0,
        Rectangle = 1,
        Polygon = 2,
    }

    public static class CommonExtentions
    {
        /// <summary>
        /// 获取枚举值的描述
        /// </summary>
        /// <param name="enumval"></param>
        /// <returns></returns>
        public static string GetDesc(this Enum enumval)
        {
            var fields = enumval.GetType().GetFields(BindingFlags.Public | BindingFlags.Static);
            var prop = fields.FirstOrDefault(x => x.GetValue(null).Equals(enumval));
            var attri = prop?.GetCustomAttribute<DescriptionAttribute>();
            if (attri != null)
            {
                return attri.Description;
            }
            return enumval.ToString();
        }
    }

    /// <summary>
    /// 枚举值转ItemsSource Model
    /// </summary>
    public class ItemsSourceEnumModel
    {
        public Enum Value { get; set; }

        public override string ToString()
        {
            return Value.GetDesc();
        }
    }

    /// <summary>
    /// 枚举转集合供下拉框使用
    /// </summary>
    public class ComboxConverter : IValueConverter
    {
        /// <summary>
        /// 缓存
        /// </summary>
        private static Dictionary<Type, IEnumerable<ItemsSourceEnumModel>> dicModels = new Dictionary<Type, IEnumerable<ItemsSourceEnumModel>>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            var type = value.GetType();
            if (dicModels.ContainsKey(type))
                return dicModels[type];
            if (type.IsEnum)
            {
                List<ItemsSourceEnumModel> list = new List<ItemsSourceEnumModel>();
                foreach (var item in type.GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    list.Add(new ItemsSourceEnumModel { Value = (Enum)item.GetValue(value) });
                }
                dicModels.Add(type, list);
                return list;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TrainSetupParameterItem : PropertyChangedBase, IParameterItem
    {
        public TrainSetupParameterItem()
        {

        }

        public TrainSetupParameterItem(string itemName, string itemValue, string itemUnit, string lowLimit, string highLimit)
        {
            ItemName = itemName;
            ItemValue = itemValue;
            ItemUnit = itemUnit;
            LowLimit = lowLimit;
            HighLimit = highLimit;
        }

        public TrainSetupParameterItem(string itemName, string itemValue, string itemUnit, string lowLimit, string highLimit,string decimalPlaces)
        {
            ItemName = itemName;
            ItemValue = itemValue;
            ItemUnit = itemUnit;
            LowLimit = lowLimit;
            HighLimit = highLimit;
            DecimalPlaces = decimalPlaces;
        }

        public TrainSetupParameterItem(string itemName, string itemValue, string itemUnit)
        {
            ItemName = itemName;
            ItemValue = itemValue;
            ItemUnit = itemUnit;

        }

        public TrainSetupParameterItem(string itemName, string itemValue)
        {
            ItemName = itemName;
            ItemValue = itemValue;
        }


        public string ItemName { get; set; }

        public string ItemValue { get; set; }

        public string ItemUnit { get; set; }

        public string LowLimit { get; set; }

        public string HighLimit { get; set; }

        public string DecimalPlaces { get; set; }

        public TemplateDisplayType TemplateType { get; set; }

        public IParameterItem Clone()
        {
            return this.MemberwiseClone() as IParameterItem;
        }

        public void Copy(IParameterItem other)
        {
            TrainSetupParameterItem ot = other as TrainSetupParameterItem;
            this.ItemName = ot.ItemName;
            this.ItemValue = ot.ItemValue;
            this.ItemUnit = ot.ItemUnit;
            this.TemplateType = ot.TemplateType;
        }
    }

    public class TrainSetupEnumItem : PropertyChangedBase, IParameterItem
    {
        public TrainSetupEnumItem() { }

        public TrainSetupEnumItem(string itemName, int itemIndex, string itemValue, string type)
        {
            ItemName = itemName;
            ItemIndex = itemIndex;
            ItemValue = itemValue;
            TypeName = type;
        }

        /// <summary>
        /// 类型名
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// 标签名
        /// </summary>
        public string ItemName { get; set; }

        private int _itemIndex;
        /// <summary>
        /// 选中值索引
        /// </summary>
        public int ItemIndex
        {
            get => _itemIndex;
            set => SetAndNotify(ref _itemIndex, value);
        }

        private string _itemValue;
        /// <summary>
        /// 选中值
        /// </summary>
        public string ItemValue
        {
            get => _itemValue;
            set => SetAndNotify(ref _itemValue, value);
        }

        private List<string> _templateType;
        /// <summary>
        /// ItemSource
        /// </summary>
        public List<string> TemplateType
        {
            get => _templateType;
            set => SetAndNotify(ref _templateType, value);
        }

        public IParameterItem Clone()
        {
            return this.MemberwiseClone() as IParameterItem;
        }

        public void Copy(IParameterItem other)
        {
            TrainSetupEnumItem ot = other as TrainSetupEnumItem;
            this.ItemName = ot.ItemName;
            this.TypeName = ot.TypeName;
            this.ItemIndex = ot.ItemIndex;
            this.ItemValue = ot.ItemValue;
            this.TemplateType = ot.TemplateType;
        }
    }
}
