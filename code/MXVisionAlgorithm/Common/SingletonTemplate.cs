using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MXVisionAlgorithm.Common
{
    /// <summary>
    /// 实现单件的模板类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SingletonTemplate<T> where T : class, new()
    {
        /// <summary>
        /// 实例引用
        /// </summary>
        private static T m_instance;
        /// <summary>
        /// 线程互斥对像
        /// </summary>
        private static readonly object syslock = new object();

        /// <summary>
        /// 获取实例
        /// </summary>
        /// <returns></returns>
        public static T GetInstance()
        {
            object obj = SingletonTemplate<T>.syslock;
            lock (obj)
            {
                bool flag = SingletonTemplate<T>.m_instance == null;
                if (flag)
                {
                    SingletonTemplate<T>.m_instance = Activator.CreateInstance<T>();
                }
            }
            return SingletonTemplate<T>.m_instance;
        }

    }
}
