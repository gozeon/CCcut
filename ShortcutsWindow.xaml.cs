using System.Collections.Generic;
using System.Windows;

namespace CCcut
{
    /// <summary>
    /// ShortcutsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ShortcutsWindow : Window
    {
        public ShortcutsWindow()
        {
            InitializeComponent();

            ShortcutGroups = CreateGroups();
            DataContext = this;
        }

        public List<ShortcutGroup> ShortcutGroups { get; }

        private static List<ShortcutGroup> CreateGroups()
        {
            return new List<ShortcutGroup>
            {
                new ShortcutGroup("段落增删", new List<ShortcutItem>
                {
                    new ShortcutItem("Insert / Ctrl+N", "在当前游标处新增段落"),
                    new ShortcutItem("Delete", "删除所有勾选（已选中）的段落"),
                    new ShortcutItem("Shift+Delete", "删除当前选中的单个段落"),
                    new ShortcutItem("Ctrl+Enter", "拆分所有勾选的段落"),
                }),
                new ShortcutGroup("时间 / 边界调整", new List<ShortcutItem>
                {
                    new ShortcutItem("← / →", "整体平移当前段落 1 秒"),
                    new ShortcutItem("Shift + ← / →", "调整当前段落开始时间 1 秒"),
                    new ShortcutItem("Ctrl + ← / →", "调整当前段落结束时间 1 秒"),
                    new ShortcutItem("Ctrl+Shift + ← / →", "整体平移 0.1 秒（精细微调）"),
                    new ShortcutItem("[", "开始时间对齐当前游标"),
                    new ShortcutItem("]", "结束时间对齐当前游标"),
                }),
                new ShortcutGroup("播放控制", new List<ShortcutItem>
                {
                    new ShortcutItem("Space", "播放 / 暂停"),
                }),
            };
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public record ShortcutItem(string Key, string Description);

    public record ShortcutGroup(string Header, IReadOnlyList<ShortcutItem> Items);
}
