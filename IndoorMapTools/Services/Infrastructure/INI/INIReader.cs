using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace IndoorMapTools.Services.Infrastructure.INI
{
    public class INIReader
    {
        private const int BUFFER_SIZE = 65535;

        private readonly string path;

        public INIReader(string path) => this.path = Path.Combine(AppContext.BaseDirectory, path);

        public string ReadValue(string appName, string keyName)
        {
            if(!File.Exists(path)) File.WriteAllText(path, "");
            var builder = new StringBuilder(BUFFER_SIZE);
            if(GetPrivateProfileString(appName, keyName, "", builder, (uint)builder.Capacity, path) > 0)
                return builder.ToString();
            return null;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName,
            string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

    }
}
