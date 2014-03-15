using System;

namespace Risk
{
    public enum ConnectionState
    {
        Closed = 0,
        Active = 1,
        Reconnecting = 2
    }
}