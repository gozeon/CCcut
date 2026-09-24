using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Text;

namespace CCcut
{
    public interface IFileDialogService
    {
        // 只返回路径字符串，ViewModel 极度喜欢这种纯数据
        string? OpenVideoFile(string filter);
    }

    public class WindowsFileDialogService : IFileDialogService
    {
        public string? OpenVideoFile(string filter)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = filter
            };

            if(dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }
    }
}
