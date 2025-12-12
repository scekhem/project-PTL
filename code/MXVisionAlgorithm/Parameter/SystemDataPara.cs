using MwFramework.ManagerService;
using MX10UBDBU01AA.Common;
using Stylet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX10UBDBU01AA.Parameter
{
    [Export(typeof(IParameter))]
    public class SystemDataPara : ParameterBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public WfTransferItem WfTransferItem { get; set; } = new WfTransferItem();

        public override void Copy(IParameter source)
        {
            SystemDataPara setting = source as SystemDataPara;
            if (setting != null)
            {
                ReflectionExtension.PropertyCopy<WfTransferItem>(this.WfTransferItem, setting.WfTransferItem);
            }
        }
    }

    public class WfTransferItem : PropertyChangedBase, IParameterItem
    {
        public MagazineDataItem MagazineDataItem { get; set; } =new MagazineDataItem();
        public WfTransferSizeItem WfTransferSize8inchItem { get; set; } =new WfTransferSizeItem() { WaferSize  = EWaferSize.Inch_8};
        public WfTransferSizeItem WfTransferSize12inchItem { get; set; } =new WfTransferSizeItem() { WaferSize = EWaferSize.Inch_12 };
        public IParameterItem Clone()
        {
            return this.MemberwiseClone() as IParameterItem;
        }
    }
    public class MagazineDataItem : PropertyChangedBase, IParameterItem
    {
        public double TeachingMgheight { get; set; } = 1;
        public double ElevatorOffset { get; set; } = 0;
        public double RightCheckHeight { get; set; } = 0;
        public int NumberofSlot { get; set; } = 0;
        public double Pitch { get; set; } = 0;
        public double TopHeight { get; set; } = 0;
        public double ShiftAmountWhenLoading { get; set; } = 0;
        public double ShiftAmountWhenUnLoading_Push { get; set; } = 0;
        public double ShiftAmountWhenUnLoading_Back { get; set; } = 0;
        public IParameterItem Clone()
        {
            return this.MemberwiseClone() as IParameterItem;
        }
    }
    public class WfTransferSizeItem : PropertyChangedBase, IParameterItem
    {
        public EWaferSize WaferSize { get; set; }
        public double WfChuckSize { get; set; } = 0;
        public double PullSlowSpeed { get; set; } = 0;
        public double WfSetSlowLevel { get; set; } = 0;
        public double WfSetSlowSpeed { get; set; } = 0;
        public int GripTryTimes { get; set; } = 0;
        public double PushInSlowSpeed { get; set; } = 0;
        public double BackPushLevel { get; set; } = 0;
        public double BackPushReturn { get; set; } = 0;
        public double BackPushSpeed { get; set; } = 0;
        public double BackPushDelay { get; set; } = 0;
        public double PullStartPos2 { get; set; } = 0;
        public double PullEndPos3 { get; set; } = 0;
        public double PullCheckPos4 { get; set; } = 0;
        public double BarCodePos5 { get; set; } = 0;
        public double ExchangePos6 { get; set; } = 0;
        public double EscapePos7 { get; set; } = 0;
        public double SetCheckPos8 { get; set; } = 0;
        public double BackPushPos9 { get; set; } = 0;
        public double PushPos10 { get; set; } = 0;
        public double WfBlowX { get; set; } = 0;
        public double WfBlowY { get; set; } = 0;
        public double WfExchange1X { get; set; } = 0;
        public double WfExchange1Y { get; set; } = 0;
        public double TopPosOffsetX { get; set; } = 0;
        public double TopPosOffsetY { get; set; } = 0;
        public double BackPushPositionX { get; set; } = 0;
        public double BackPushPositionY { get; set; } = 0;


        public IParameterItem Clone()
        {
            return this.MemberwiseClone() as IParameterItem;
        }
    }
}
