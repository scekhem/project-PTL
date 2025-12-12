using DataStruct;
using System;
using System.Collections.Generic;

namespace UltrapreciseBonding.Calib
{
    /// <summary>
    /// CalibItem
    /// </summary>
    public abstract class CalibItem
    {
        private bool _isCalibed = false;
        private string _itemName = string.Empty;

        /// <summary>
        /// 获取或设置一个值，该值指示是否完成标定。
        /// </summary>
        public bool IsCalibed
        {
            get => _isCalibed;
            set => _isCalibed = value;
        }

        /// <summary>
        /// Gets or sets the user's _itemName
        /// </summary>
        public string ItemName
        {
            get => _itemName;
            set => _itemName = value;
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public abstract Errortype Save(string fileDir);

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public abstract Errortype Load(string fileDir);
    }

    /// <summary>
    /// CalibBase
    /// </summary>
    public abstract class CalibBase
    {
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="names">名称</param>
        /// <returns>OK:成功,其他:失败</returns>
        public abstract Errortype Init(List<string> names);

        /// <summary>
        /// 释放
        /// </summary>
        /// <returns>OK:成功,其他:失败</returns>
        public abstract Errortype Release();

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="fileDir">保存路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public abstract Errortype Save(string fileDir);

        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="fileDir">加载路径</param>
        /// <returns>OK:成功,其他:失败</returns>
        public abstract Errortype Load(string fileDir);
    }
}
