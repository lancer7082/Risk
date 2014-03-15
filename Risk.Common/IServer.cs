using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public interface IServer
    {
        T Execute<T>(Command command);
        object Execute(Command command);
    }
}