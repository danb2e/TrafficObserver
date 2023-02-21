using IotechiCore.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficObserver.Mediator
{
    internal class Mediator : IotechiCore.Base.CsSingleInstance<MediatorBase<object>>
    {
        public enum EnDrawMediatorType
        {
            Result,
        }
    }
}
