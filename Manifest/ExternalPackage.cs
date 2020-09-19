using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace Manifest
{
    public class ExternalPackage
    {
        public string Name { get; set; }
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }

        public ExternalPackage() { }

        public void CopyPackage()
        {
            if (File.Exists(SourcePath))
            {
                string dstDirPath = Path.GetDirectoryName(DestinationPath);
                if (!Directory.Exists(dstDirPath))
                {
                    Directory.CreateDirectory(dstDirPath);
                }
                File.Copy(SourcePath, DestinationPath, overwrite: true);
            }
            else if (Directory.Exists(SourcePath))
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "robocopy.exe";
                    proc.StartInfo.Arguments = string.Format(
                        "\"{0}\" \"{1}\" /COPY:DAT /MIR",
                        SourcePath, DestinationPath);
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.Start();
                    proc.WaitForExit();
                }
            }
        }
    }
}
