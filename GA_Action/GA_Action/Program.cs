using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GA_Action
{
    class Pos
    {
        public int X, Y;
        public Pos(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
        public static Pos operator +(Pos p1, Pos p2)
        {
            return new Pos(p1.X + p2.X, p1.Y + p2.Y);
        }
    }
    // 壁や空間などのタイル
    class Tile
    {
        public const char
            SPACE = ' ',
            GOAL = '|',
            WALL = '#',
            DEATH = 'v',
            PLAYER = '&';
    }

    enum Action
    {
        STAY,
        GO,
        JUMP,
    }
    class Field
    {
        private List<string> lines;
        private readonly int width;
        private readonly int height;

        public Field(string path)
        {
            lines = new List<string>();
            width = 0;

            // 読み込み
            using (StreamReader sr = new StreamReader(path))
            {
                while (sr.Peek() >= 0)
                {
                    // 本来は挿入する前に変な文字がないか調べるべき

                    string line = sr.ReadLine();// ReadLine の返り値には\rや\nは含まれない
                    lines.Add(line);
                    width = Math.Max(width, line.Length);
                }
            }

            // 長さを揃える
            lines = lines.Select(line => line + new string(Tile.SPACE, width - line.Length) + Tile.GOAL).ToList();
            lines.Add(new string(Tile.DEATH, width) + Tile.GOAL);
            // 高さを設定
            height = lines.Count;
        }

        public char Get(int x, int y)
        {
            return lines.ElementAt(y).ElementAt(x);
        }

        // この辺りよくわからずに使ってるけど不要でしょって思うよ
        public int Width => width;
        public int Height => height;
    }


    class Simulator
    {
        private Pos pos; // player position
        private Field field;
        private int screenWidth;
        private bool end;
        private int score;

        public bool End { get => end; }
        public int Score { get => score; }

        public Simulator(Field field, int screenWidth)
        {
            this.field = field;
            this.screenWidth = screenWidth;
            pos = new Pos(0, 0);
            end = false;
            score = 0;
        }

        public void Update(Action nextAction)
        {
            // 動く
            switch (nextAction)
            {
                case Action.STAY:
                    break;
                // 進む
                case Action.GO:
                    if (field.Get(pos.X + 1, pos.Y) != Tile.WALL)
                    {
                        pos.X++;
                    }
                    break;
                // ジャンプ
                case Action.JUMP:
                    if (field.Get(pos.X, pos.Y + 1) == Tile.WALL)
                    {
                        // 最大3マスジャンプ
                        for (int i = 1; i <= 3; i++)
                        {
                            if (pos.Y - 1 < 0) { break; } // 画面から出ない
                            if (field.Get(pos.X, pos.Y - 1) != Tile.WALL &&
                                field.Get(pos.X, pos.Y - 1) != Tile.DEATH) // これにはならんでしょ
                            {
                                pos.Y--;
                            }
                        }

                    }
                    break;
            }

            // 落ちる
            if (field.Get(pos.X, pos.Y + 1) != Tile.WALL)
            {
                pos.Y++;
            }

            // ゴール
            if (field.Get(pos.X, pos.Y) == Tile.GOAL)
            {
                score = pos.X + 1000; // ボーナスで+1000
                end = true;
            }
            // 死んだ
            else if (field.Get(pos.X, pos.Y) == Tile.DEATH)
            {
                score = pos.X; // 到達した座標がスコア
                end = true;
            }
        }

        // コンソールに現状を描画
        public void Draw()
        {
            // 最初
            if (pos.X <= screenWidth / 2)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    Console.SetCursorPosition(0, y);
                    for (int x = 0; x < screenWidth; x++)
                    {
                        Console.Write(field.Get(x, y));
                    }
                }
                Console.SetCursorPosition(pos.X, pos.Y);
                Console.Write(Tile.PLAYER);
            }
            // まんなか
            else if (pos.X < field.Width - screenWidth / 2)
            {
                for (int y = 0; y < field.Height; y++)
                {
                    Console.SetCursorPosition(0, y);
                    for (int x = -screenWidth / 2; x < screenWidth / 2; x++)
                    {
                        Console.Write(field.Get(pos.X + x, y));
                    }
                }
                Console.SetCursorPosition(screenWidth / 2, pos.Y);
                Console.Write(Tile.PLAYER);
            }
            // 最後
            else
            {
                for (int y = 0; y < field.Height; y++)
                {
                    Console.SetCursorPosition(0, y);
                    for (int x = 0; x <= screenWidth; x++)
                    {
                        Console.Write(field.Get(field.Width - screenWidth + x, y));
                    }
                }
                Console.SetCursorPosition(pos.X - (field.Width - screenWidth), pos.Y);
                Console.Write(Tile.PLAYER);
            }

            Console.SetCursorPosition(0, field.Height + 1);
        }
    }


    class Program
    {
        static void Main(string[] args)
        {
            Field field = new Field("field1.txt");
            Simulator simulator = new Simulator(field, 20);

            while (!simulator.End)
            {
                simulator.Draw();

                Action nextAction = Action.STAY;
                var c = Console.ReadKey();
                switch (c.KeyChar)
                {
                    case 'g': nextAction = Action.GO; break;
                    case 'j': nextAction = Action.JUMP; break;
                    default: break;
                }
                simulator.Update(nextAction);

            }

            Console.WriteLine("Your Score is ===> {0}", simulator.Score);

        }
    }
}
