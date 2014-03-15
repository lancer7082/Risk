using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk
{
    public interface IAddIn
    {
        // TODO: !!! void Configure(Configuration)
        string Name();
        string Version();
        void Start(IServer server);
        void Stop();
    }
}