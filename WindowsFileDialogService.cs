using CCcut;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CCcut
{
    public interface IFileDialogService
    {
        // 只返回路径字符串，ViewModel 极度喜欢这种纯数据
        string? OpenVideoFile(string filter);

        string? OpenFolder(string title);

        string? OpenFile(string filter);

        string? SaveFile(string filter, string defaultFileName);
    }

    public class WindowsFileDialogService : IFileDialogService
    {
        public string? OpenFolder(string title)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog
            {
                Title = title,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Personal)
            };
            if(openFolderDialog.ShowDialog() == true)
            {
                return openFolderDialog.FolderName;
            }
            return null;
        }

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

        public string? OpenFile(string filter)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = filter
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }

        public string? SaveFile(string filter, string defaultFileName)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultFileName,
                DefaultExt = ".cccut"
            };

            if (dialog.ShowDialog() == true)
            {
                return dialog.FileName;
            }

            return null;
        }
    }

}
