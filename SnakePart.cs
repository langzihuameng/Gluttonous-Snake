using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace GluttonousSnake
{
    public class SnakePart
    {
        public UIElement UiElement { get; set; }    // 蛇每一节的元素（方块）
        public Point Position { get; set; }         // 蛇每一节元素所在的位置
        public Boolean IsHead { get; set; }         // 当前蛇节是否是蛇头
    }
}
