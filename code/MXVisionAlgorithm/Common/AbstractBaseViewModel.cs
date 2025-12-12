using MaxwellControl.Tools;
using MaxwellFramework.Core.Interfaces;
using MwFramework.ManagerService;
using Stylet;
using StyletIoC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX10UBDBU01AA.Common
{
    public abstract class AbstractBaseViewModel : Screen
    {

        public IParamList ParamList { set; get; }

        public AbstractBaseViewModel()
        {        
            ParamList = IoC.Get<IParameterManager>() as IParamList;
        }

    }
}
