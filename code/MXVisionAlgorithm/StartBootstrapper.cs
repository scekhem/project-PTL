using MaxwellFramework;
using MaxwellFramework.Core.Interfaces;
using StyletIoC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using MwFramework.ManagerService;

namespace MX10UBDBU01AA
{
    public class StartBootstrapper : Bootstrapper
    {
        protected override void OnStart()
        {
            //设定线程
            int minWorkThreads = 0, minIOThreads = 0, maxWorkThreads = 0, maxIOThreads = 0;
            ThreadPool.GetMinThreads(out minWorkThreads, out minIOThreads);
            ThreadPool.SetMinThreads(100, minIOThreads);
            ThreadPool.GetMaxThreads(out maxWorkThreads, out maxIOThreads);
            ThreadPool.SetMaxThreads(300, maxIOThreads);

            // 获得语言类型
            string lang = System.Configuration.ConfigurationManager.AppSettings["Lang"];
            // 添加平台语言包
            ResourceDictionary mwLang = new ResourceDictionary() { Source = new Uri("pack://siteoforigin:,,,/../Language/MaxwellFramework_" + lang.ToString() + ".xaml", UriKind.RelativeOrAbsolute) };
            Application.Current.Resources.MergedDictionaries.Add(mwLang);
            // 添加项目语言包,注意项目名称
            ResourceDictionary mxLang = new ResourceDictionary() { Source = new Uri("pack://siteoforigin:,,,/../Language/MX10UBDBU01AA_" + lang.ToString() + ".xaml", UriKind.RelativeOrAbsolute) };
            Application.Current.Resources.MergedDictionaries.Add(mxLang);
            base.OnStart();
        }

        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {

            base.ConfigureIoC(builder);
        }

        protected override void Configure()
        {
            base.Configure();
        }

        protected override void OnLaunch()
        {
            base.OnLaunch();
        }

        protected override void PrepareForInit(IProjectManager projectManager)
        {

        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Process[] sdcProcesses = Process.GetProcessesByName("Algorithm");
            if (sdcProcesses.Length > 0)
            {
                sdcProcesses[0].Kill();
            }
        }

        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            //Container.Get<LogService>().LogInfoException(e.Exception);
            base.OnUnhandledException(e);
        }
    }
}
