using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;

namespace GluttonousSnake
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        const int SnakeSquareSize = 20;                         // 方块的大小

        SolidColorBrush snakeBodyBrush = Brushes.Green;         // 蛇身颜色
        SolidColorBrush snakeHeadBrush = Brushes.Blue;          // 蛇头颜色

        List<SnakePart> snakeParts = new List<SnakePart>();     // 一个整蛇

        SnakeDirection snakeDirection = SnakeDirection.Left;    // 蛇的方向
        int snakeLength = 0;                                    // 蛇的长度

        DispatcherTimer gameTickTimer = new DispatcherTimer();  // 定时器（蛇的移动)

        const int SnakeStartLength = 3;                         // 蛇初始长度
        const int SnakeStartSpeed = 500;                        // 蛇初始速度
        const int SnakeSpeedThreshold = 100;                    // 蛇的最大速度

        Random random = new Random();                           // 用于生成随机数

        int maxX = 0, maxY = 0;                                 // 游戏的行列个数

        UIElement snakeFood = null;                             // 食物
        SolidColorBrush foodBrush = Brushes.Red;                // 食物的颜色

        int currentScore = 0;                                   // 当前游戏分数

        Boolean isGameRuning = true;                            // 游戏是否在运行中
        Boolean isFlagStart = false;                            // 解决游戏开始和结束按键的 bug
        Boolean isCanAgein = true;                              // 游戏运行中 不可以重新开始游戏 


        public MainWindow()
        {
            InitializeComponent();

            // 每隔一段时间，  蛇移动一次
            gameTickTimer.Tick += (sender, e) => { if (isGameRuning == false) return; MoveSnake(); };
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();                                 // 界面渲染时生成地图

            maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);

            this.bdrWelcomeMessage.Visibility = Visibility.Visible;
            this.bdrEndOfGame.Visibility = Visibility.Collapsed;
        }

        private void DrawGameArea()
        {
            // 当前游戏地图方块的坐标
            int nextX = 0;
            int nextY = 0;

            Boolean changedColor = false;                   // 标记当前方块的颜色

            while (true)
            {
                Rectangle rectangle = new Rectangle         // 准备一个方块
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = changedColor ? Brushes.LightYellow : Brushes.LightSteelBlue
                };

                changedColor = !changedColor;               // 取返，不一样的颜色

                this.GameArea.Children.Add(rectangle);      // 在指定地方放入方块
                Canvas.SetTop(rectangle, nextY);
                Canvas.SetLeft(rectangle, nextX);

                nextX += SnakeSquareSize;

                if (nextX >= this.GameArea.ActualWidth)      // 一行一行的放入方块
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;

                    changedColor = !changedColor;
                }

                if (nextY >= this.GameArea.ActualHeight)     // 判断是否已经放满了地图
                {
                    break;
                }
            }

        }

        private void DrawSnake()
        {
            foreach (var snakePart in snakeParts)
            {
                // 换代蛇身的每一个部位
                if (snakePart.UiElement == null)
                {
                    // 当前蛇点没有元素  则加入元素（方块）
                    snakePart.UiElement = new Rectangle
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush
                    };

                    // 加入地图之中
                    this.GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }

        private void MoveSnake()
        {
            /***************************
                Move 思想：
                    1. 删除尾巴
                    2. 蛇头变身体色
                    3. 添加新的蛇头
            ****************************/


            // 初始时 或者吃到水果时  循环不成立   只负责增加身体
            while (snakeParts.Count >= snakeLength)      // 长度到位，一直删除尾巴
            {
                // 第一个元素是尾巴  删除界面与 list中的元素 (尾巴)
                this.GameArea.Children.Remove(snakeParts[0].UiElement);
                this.snakeParts.RemoveAt(0);
            }

            // 将蛇头的颜色变成身体的颜色
            foreach (var snakePart in snakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }

            // **************有待测试**************
            //(snakeParts[snakeParts.Count - 1].UiElement as Rectangle).Fill = snakeBodyBrush;
            //snakeParts[snakeParts.Count - 1].IsHead = false;


            // 获取当前蛇头的位置 （新的蛇头需要借助这个位置)
            double nextX = snakeParts[snakeParts.Count - 1].Position.X;
            double nextY = snakeParts[snakeParts.Count - 1].Position.Y;

            switch (snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
                default:
                    break;
            }


            // 添加新蛇头    (新蛇头的方块会调用上面的方法画出来)
            snakeParts.Add(new SnakePart()
            {
                Position = new Point
                (nextX, nextY),
                IsHead = true
            });


            DrawSnake();            // 画蛇

            DoCollisionCheck();     // 判断是否撞墙
        }

        private void DoCollisionCheck()
        {
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];

            // 蛇吃食物
            if ((snakeHead.Position.X == Canvas.GetLeft(snakeFood)) &&
                (snakeHead.Position.Y == Canvas.GetTop(snakeFood)))
            {
                EatSnakeFood();
                return;
            }

            // 蛇判断 越界 
            if ((snakeHead.Position.Y < 0) ||
                (snakeHead.Position.X < 0) ||
                (snakeHead.Position.Y >= this.GameArea.ActualHeight) ||
                (snakeHead.Position.X >= this.GameArea.ActualWidth))
            {
                EndGame();      // 结束游戏 
                return;
            }

            // 蛇判断自己身体
            foreach (var snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) &&
                    (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                {
                    EndGame();      // 结束游戏 
                    return;
                }
            }
        }

        private void EndGame()
        {
            gameTickTimer.IsEnabled = false;        // 停止定时器 

            //MessageBox.Show("你的蛇挂了，请按空格重新开始！", "WPF 贪吃蛇",
            //    MessageBoxButton.OK, MessageBoxImage.Asterisk);

            this.tbFinalScore.Text = currentScore.ToString();
            this.bdrEndOfGame.Visibility = Visibility.Visible;

            isFlagStart = false;

            isCanAgein = true;
        }

        private void EatSnakeFood()
        {
            ++snakeLength;
            ++currentScore;

            // 增加游戏难度
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)(gameTickTimer.Interval.TotalMilliseconds) - (currentScore * 5));

            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);

            this.GameArea.Children.Remove(snakeFood);       // 清除食物
            DrawSnakeFood();                                // 画食物

            UpdateGameStatus();                             // 更新窗口分数与速度
        }

        private void UpdateGameStatus()
        {
            //this.Title = $"WPF 贪吃蛇：{currentScore}" + "，速度：" + gameTickTimer.Interval.TotalMilliseconds;

            this.tbStatusScore.Text = currentScore.ToString();
            this.tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        }

        private void StartNewGame()
        {
            isCanAgein = false;

            isFlagStart = true;

            this.btPause.Visibility = Visibility.Visible;

            // 清除界面上的蛇
            foreach (var snakeBodyPart in snakeParts)
            {
                if (snakeBodyPart.UiElement != null)
                {
                    this.GameArea.Children.Remove(snakeBodyPart.UiElement);
                }
            }
            snakeParts.Clear();     // List  清空


            // 清除界面上的食物
            if (snakeFood != null)
            {
                this.GameArea.Children.Remove(snakeFood);
            }
            snakeFood = null;


            // 初始蛇的数据 （蛇的长度，蛇的方向）
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Right;

            // 初始蛇的第一个结点
            snakeParts.Add(new SnakePart
            {
                Position = new Point(SnakeSquareSize * 5,
                SnakeSquareSize * 5)
            });

            // 设置定时器的间隔时间
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);
            currentScore = 0;           // 分数为 0

            DrawSnake();                // 画蛇

            DrawSnakeFood();            // 画食物

            UpdateGameStatus();         // 更新状态

            gameTickTimer.IsEnabled = true;
            gameTickTimer.Start();      // 启动定时器

            this.bdrWelcomeMessage.Visibility = Visibility.Hidden;

            this.bdrEndOfGame.Visibility = Visibility.Hidden;
        }

        private Point GetNextFoodPosition()
        {
            int foodX = random.Next(0, maxX) * SnakeSquareSize;
            int foodY = random.Next(0, maxY) * SnakeSquareSize;

            foreach (var snakePart in snakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                {
                    return GetNextFoodPosition();   // 与蛇重合，重新生成食物的位置
                }
            }

            // 返回食物的位置
            return new Point(foodX, foodY);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                if (isCanAgein == true && isGameRuning == true)
                    StartNewGame();
                return;
            }

            if (isFlagStart == false) return;

            if (isGameRuning == false) return;      // 判断游戏是否在运行中

            // 保存当前的方向，用于下面判断是否改变方向了
            SnakeDirection originalSnakeDirection = snakeDirection;

            switch (e.Key)
            {
                case Key.Up:
                    if (snakeDirection != SnakeDirection.Down)
                    {
                        snakeDirection = SnakeDirection.Up;
                    }
                    break;
                case Key.Down:
                    if (snakeDirection != SnakeDirection.Up)
                    {
                        snakeDirection = SnakeDirection.Down;
                    }
                    break;
                case Key.Left:
                    if (snakeDirection != SnakeDirection.Right)
                    {
                        snakeDirection = SnakeDirection.Left;
                    }
                    break;
                case Key.Right:
                    if (snakeDirection != SnakeDirection.Left)
                    {
                        snakeDirection = SnakeDirection.Right;
                    }
                    break;
                default:
                    return;
            }

            if (originalSnakeDirection != snakeDirection)
            {
                MoveSnake();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Button_Click_Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_Pause(object sender, RoutedEventArgs e)
        {
            if (isGameRuning == false)
            {
                isGameRuning = true;
                this.btPause.Content = "⏸";
            }
            else
            {
                isGameRuning = false;
                this.btPause.Content = "🛑";
            }
        }

        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();

            snakeFood = new Ellipse
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = foodBrush
            };

            this.GameArea.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);
        }
    }
}
