using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MX10UBDBU01AA.Common
{
    public enum EWaferSize : int
    {
        [Description("8inch")]
        Inch_8 = 2000,
        [Description("12inch")]
        Inch_12 = 3000
    }
}
