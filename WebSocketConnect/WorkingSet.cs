using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace WebSocketConnect
{
    /// <summary>
    /// 見かけ上のメモリ使用率を下げる為、定期的にワーキングセットを縮小化。
    /// </summary>
    public class WorkingSet
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr procHandle, IntPtr min, IntPtr max);
        private static Process proc = null;

        public static async Task Shrink()
        {
            int _interval = 10000;

            await Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(_interval);
                    proc = Process.GetCurrentProcess();
                    SetProcessWorkingSetSize(proc.Handle, new IntPtr(-1L), new IntPtr(-1L));
                }
            });
        }
    }
}
