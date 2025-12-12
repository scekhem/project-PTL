using DataStruct;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UltrapreciseBonding.Calib
{
    /// <summary>
    /// 单例模板
    /// </summary>
    /// <typeparam name="T">单例对象类型</typeparam>
    public abstract class Singleton<T> where T : Singleton<T>, new()
    {
        private static volatile T _instance;
        private static readonly object _syncRoot = new object();

        /// <summary>
        /// 获取单例对象
        /// </summary>
        /// <returns>返回单例对象</returns>
        public static T GetInstance()
        {
            if (_instance is null)
            {
                lock (_syncRoot)
                {
                    if (_instance is null)
                    {
                        _instance = new T();
                    }
                }
            }

            return _instance;
        }
    }
}
