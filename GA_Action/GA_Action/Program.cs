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
        private List<int>[,] jumpto;

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

            // code by dun
            jumpto = new List<int>[width, height];
            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    char c = this.Get(x, y);
                    if ('0' <= c && c <= '9')
                    {
                        for (int yy = 0; yy < height; yy++)
                        {
                            for (int xx = 0; xx < width; xx++)
                            {
                                if (xx == x && yy == y) continue;
                                char cc = this.Get(xx, yy);
                                if (cc == c)
                                {
                                    jumpto[x, y] = new List<int>();
                                    jumpto[x, y].Add(xx);
                                    jumpto[x, y].Add(yy);
                                }
                            }
                        }
                    }
                }
            }
        }

        public char Get(int x, int y)
        {
            return lines.ElementAt(y).ElementAt(x);
        }

        // code by dun
        public Pos GetWarpTo(Pos pos)
        {
            return new Pos(jumpto[pos.X, pos.Y][0], jumpto[pos.X, pos.Y][1]);
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
        private int turn;

        public bool End { get => end; }
        public int Score { get => score; }

        public Simulator(Field field, int screenWidth)
        {
            this.field = field;
            this.screenWidth = screenWidth;
            pos = new Pos(0, 0);
            end = false;
            score = 0;
            turn = 0;
        }

        public void Update(Action nextAction)
        {
            turn++;

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
                        for (int i = 0; i <= 3 ; i++)
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

            score = pos.X; // 到達した座標がスコア
            // code by dun
            if ('0' <= field.Get(pos.X, pos.Y) && field.Get(pos.X, pos.Y) <= '9')
            {
                pos = field.GetWarpTo(pos);
            }

            // ゴール
            if (field.Get(pos.X, pos.Y) == Tile.GOAL)
            {
                score = pos.X + 1000 - turn; // ボーナスで+1000
                end = true;
            }
            // 死んだ
            else if (field.Get(pos.X, pos.Y) == Tile.DEATH)
            {
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

    class Genom
    {
        private List<Action> actions;
        private int p;
        private int score;
        public Genom(List<Action> actions)
        {
            this.actions = actions;
            p = 0;
            score = 0;
        }

        // ランダムな遺伝子列を作る。
        public static Genom Init(uint length, Random random)
        {
            List<Action> actions = new List<Action>();
            for (int i = 0; i < length; i++)
            {
                // enumの変更に強くないのでだめ
                actions.Add((Action)random.Next(3));
            }
            return new Genom(actions);
        }
        public Action Next()
        {
            Action nextAction = actions.ElementAt(p);
            p++;
            return nextAction;
        }
        public bool End { get => p >= actions.Count; }
        public int Length { get => actions.Count; }
        public int Score { get => score; set => score = value; }
        internal List<Action> Actions { get => actions; }
    }


    class Program
    {
        static List<Genom> NextGeneration(List<Genom> parents)
        {
            List<Genom> children = new List<Genom>();
            Random random = new Random();
            const double e = 0.2;

            while (children.Count < parents.Count)
            {
                // 今回の親子
                Genom p1, p2;

                // これはランダム
                if (e >= random.NextDouble())
                {
                    parents = parents.OrderBy((x) => new Guid()).ToList();
                }
                // こっちがエリート
                else
                {
                    parents.Sort((a, b) => a.Score.CompareTo(b.Score));
                }

                // 親が選択された
                p1 = parents.ElementAt(parents.Count - 1);
                p2 = parents.ElementAt(parents.Count - 2);

                // 一様交差
                Action[] cgenom1 = new Action[p1.Length];
                Action[] cgenom2 = new Action[p1.Length];
                for (int i = 0; i < p1.Length; i++)
                {
                    if (random.Next(2) == 0)
                    {
                        cgenom1[i] = (p1.Actions.ElementAt(i));
                        cgenom2[i] = (p2.Actions.ElementAt(i));
                    }
                    else
                    {
                        cgenom2[i] = (p1.Actions.ElementAt(i));
                        cgenom1[i] = (p2.Actions.ElementAt(i));
                    }
                }

                //// 一点交差
                //int p = random.Next(p1.Length);

                //Action[] cgenom1 = p1.Actions.GetRange(0, p).Concat(p2.Actions.GetRange(p, p1.Length - p)).ToArray();
                //Action[] cgenom2 = p2.Actions.GetRange(0, p).Concat(p1.Actions.GetRange(p, p2.Length - p)).ToArray();

                // 突然変異
                for (int i = 0; i < cgenom1.Length; i++)
                {
                    if (random.NextDouble() <= 0.005)
                    {
                        cgenom1[i] = (Action)random.Next(3);
                    }
                }
                for (int i = 0; i < cgenom1.Length; i++)
                {
                    if (random.NextDouble() <= 0.005)
                    {
                        cgenom2[i] = (Action)random.Next(3);
                    }
                }


                children.Add(new Genom(cgenom1.ToList()));
                children.Add(new Genom(cgenom2.ToList()));
            }

            return children;
        }
        static void GeneticAlgorithm(Field field, int screenWidth, int seed, uint genomLength = 1000, uint groupSize = 10)
        {
            // ほげる
            if (groupSize%2 != 0)
            {
                throw new Exception("AAN");
            }
            Genom maxGenom = null;
            int maxScore = -1;

            // 初期化
            Random random = new Random(seed);
            List<Genom> genoms = new List<Genom>();
            for (int i = 0; i < groupSize; i++)
            {
                genoms.Add(Genom.Init(genomLength, random));
            }

            for (int c = 0; c < 3000; c++)
            {

                // 評価
                for (int i = 0; i < groupSize; i++)
                {
                    Genom genom = genoms.ElementAt(i);
                    Simulator simulator = new Simulator(field, screenWidth);
                    while (!simulator.End && !genom.End)
                    {
                        //simulator.Draw();
                        //System.Threading.Thread.Sleep(100);
                        Action nextAction = genom.Next();
                        simulator.Update(nextAction);
                    }
                    genom.Score = simulator.Score;
                    //Console.Clear();
                    //Console.SetCursorPosition(0, 20);
                    //Console.WriteLine("GENERATION:{0}", c);
                    //Console.WriteLine("SCORE:{0}", genom.Score);


                    if (maxScore < genom.Score)
                    {
                        maxScore = genom.Score;
                        maxGenom = new Genom(genom.Actions);
                    }
                }

                // 次世代
                genoms = NextGeneration(genoms);
            }
            {
                Genom genom = maxGenom;
                Simulator simulator = new Simulator(field, screenWidth);
                while (!simulator.End && !genom.End)
                {
                    simulator.Draw();
                    System.Threading.Thread.Sleep(20);
                    Action nextAction = genom.Next();
                    simulator.Update(nextAction);
                }
                genom.Score = simulator.Score;
                //Console.Clear();
                //Console.SetCursorPosition(0, 20);
                //Console.WriteLine("GENERATION:{0}", c);
                Console.WriteLine("SCORE:{0}", genom.Score);
            }
            Console.WriteLine("MAX SCORE:{0}", maxScore);
        }
        static void Main(string[] args)
        {
            Field field = new Field("field12.txt");
            GeneticAlgorithm(field, 20, 5467890, 1000, 20);
        }
    }
}
