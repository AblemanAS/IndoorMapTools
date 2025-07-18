using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace IndoorMapTools.Core
{
    public class INIModule
    {
        [DllImport("kernel32.dll")]
        private static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, 
            string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        private readonly StringBuilder builder = new StringBuilder(512);
        private readonly string path;

        public INIModule(string path) => this.path = Path.Combine(Environment.CurrentDirectory, path);

        public string ReadValue(string appName, string keyName)
        {
            if(!File.Exists(path)) File.Create(path);
            builder.Clear();
            if(GetPrivateProfileString(appName, keyName, "", builder, (uint)builder.Capacity, path) > 0)
                return builder.ToString();
            return null;
        }
    }
}
