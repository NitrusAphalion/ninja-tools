using System;

namespace NinjaTools.Interfaces
{
    public interface ICloseableTabItem : ICloneable
    {
        string TabHeader { get; }
    }
}