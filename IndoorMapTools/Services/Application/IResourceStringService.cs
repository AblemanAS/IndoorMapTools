using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndoorMapTools.Services.Application
{
    public interface IResourceStringService
    {
        string Get(string key);
        string this[string key] { get; }
    }
}
