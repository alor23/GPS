using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gps
{
    public interface Saveloadinter
    {
        Task SaveTextAsync(string filename, string text);
        Task<string> LoadTextAsync(string filename);
        bool FileExists(string filename);
    }
}
