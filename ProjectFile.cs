using System.Collections.Generic;

namespace CCcut
{
    // 自定义文件（项目 / 会话文件）的数据结构
    internal class ProjectFileDto
    {
        // 应用配置（可选）：提供时覆盖当前配置并保存到 config.json
        public SettingsDto? Settings { get; set; }

        // 需要打开的视频文件路径（可选）
        public string VideoPath { get; set; } = string.Empty;

        // 段落列表（可选）
        public List<ProjectParagraphDto> Paragraphs { get; set; } = new List<ProjectParagraphDto>();
    }

    internal class ProjectParagraphDto
    {
        public string Title { get; set; } = string.Empty;

        public long StartTicks { get; set; }

        public long EndTicks { get; set; }

        // 形如 #AARRGGBB 的颜色；省略或非法时随机生成浅色
        public string ColorHex { get; set; } = string.Empty;
    }
}
