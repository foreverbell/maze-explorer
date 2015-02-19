using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Quobject.SocketIoClientDotNet.Client;
using System.Windows.Forms;

namespace maze {

	class Solver {
		public const int mazeSize = 202;
		public const int cellSize = 3;
		public const int wallWidth = 1;

		private const int mazeN = 1;
		private const int mazeS = 2;
		private const int mazeE = 4;
		private const int mazeW = 8;

		private int mCurX, mCurY;
		private int mTreasureX, mTreasureY;
		private int[,] mMaze = new int[mazeSize, mazeSize];
		private bool[,] mVisited = new bool[mazeSize, mazeSize];
		private bool mTreasureFound, mTreasureAvailable;
		private bool[,] mRefreshRequired = new bool[mazeSize, mazeSize];
		private Queue<Tuple<char, int, int>> mOpQueue = new Queue<Tuple<char, int, int>>();
		private Socket mSocket;

		public void Initialize(String uri) {
			mCurX = mCurY = mazeSize / 2;
			mTreasureX = mTreasureY = -1;
			mTreasureFound = mTreasureAvailable = false;
			for (int i = 0; i < mazeSize; ++i) {
				for (int j = 0; j < mazeSize; ++j) {
					mMaze[i, j] = -1;
					mRefreshRequired[i, j] = true;
				}
			}
			mOpQueue.Enqueue(new Tuple<char, int, int>(' ', mCurX, mCurY));
			mSocket = IO.Socket(uri);
			mSocket.On("map", (raw) => {
				Thread.BeginCriticalRegion(); {
					// Console.WriteLine(raw);
					String data = (String)raw;
					String[] map = data.Split(',');
					int x = mOpQueue.Peek().Item2;
					int y = mOpQueue.Peek().Item3;

					mOpQueue.Dequeue();
					for (int i = 0; i < 11; ++i) {
						for (int j = 0; j < 11; ++j) {
							String[] cell = map[i * 11 + j].Split('|');
							int dat = Convert.ToInt32(cell[0]);
							mMaze[x + i - 5, y + j - 5] = dat;
							if (cell.Length > 1) {
								mTreasureX = x + i - 5;
								mTreasureY = y + j - 5;
								mTreasureFound = true;
							}
							mRefreshRequired[x + i - 5, y + j - 5] = true;
						}
					}
				} Thread.EndCriticalRegion();
			});
			mSocket.On("msg", (data) => {
				MessageBox.Show((String)data);
				mSocket.Disconnect();
			});
		}

		public void DrawMaze(Graphics graph, bool force = false) {

			SolidBrush blackBrush = new SolidBrush(Color.Black);
			SolidBrush whiteBrush = new SolidBrush(Color.White);
			SolidBrush wallBrush = new SolidBrush(Color.Red);

			for (int i = 0; i < mazeSize; ++i) {
				for (int j = 0; j < mazeSize; ++j) {
					int cx = j * cellSize + 5, cy = i * cellSize + 5;
					int value = mMaze[i, j];

					if (force || mRefreshRequired[i, j]) {
						if (value == -1) {
							graph.FillRectangle(blackBrush, new Rectangle(cx, cy, cellSize, cellSize));
						} else {
							graph.FillRectangle(whiteBrush, new Rectangle(cx, cy, cellSize, cellSize));

							bool left = (value & mazeW) != mazeW;
							bool top = (value & mazeN) != mazeN;
							bool right = (value & mazeE) != mazeE;
							bool down = (value & mazeS) != mazeS;

							if (left) {
								graph.FillRectangle(wallBrush, new Rectangle(cx, cy, wallWidth, cellSize));
							}
							if (top) {
								graph.FillRectangle(wallBrush, new Rectangle(cx, cy, cellSize, wallWidth));
							}
							if (right) {
								graph.FillRectangle(wallBrush, new Rectangle(cx + cellSize, cy, wallWidth, cellSize));
							}
							if (down) {
								graph.FillRectangle(wallBrush, new Rectangle(cx, cy + cellSize, cellSize, wallWidth));
							}
							if (i == mTreasureX && j == mTreasureY) {
								graph.FillRectangle(wallBrush, new Rectangle(cx, cy, cellSize, cellSize));
							}
						}
						mRefreshRequired[i, j] = false;
					}

					if (i == mCurX && j == mCurY) {
						graph.FillRectangle(blackBrush, new Rectangle(cx + 1, cy + 1, cellSize - 1, cellSize - 1));
						mRefreshRequired[i, j] = true;
					}

				}
			}

			blackBrush.Dispose();
			whiteBrush.Dispose();
			wallBrush.Dispose();

			Thread.Sleep(20);
		}

		private void ClearOpQueue() {
			Tuple<char, int, int>[] ops = mOpQueue.ToArray();

			for (int i = 0; i < ops.Length; ++i) {
				mSocket.Emit("walk", ops[i].Item1.ToString());
			}
			while (mOpQueue.Count != 0) {
				Thread.Sleep(500);
			}
		}

		private char Rotate(char dir) {
			if (dir == 'W') {
				return 'N';
			} else if (dir == 'N') {
				return 'E';
			} else if (dir == 'E') {
				return 'S';
			} else if (dir == 'S') {
				return 'W';
			}
			throw new ArgumentException();
		}

		public bool Go(char keyChar) {
			switch (keyChar) {
				case 'N':
					if ((mMaze[mCurX, mCurY] & mazeN) == mazeN) {
						--mCurX;
						mOpQueue.Enqueue(new Tuple<char, int, int>((char)Keys.W, mCurX, mCurY));
						if (mOpQueue.Count > 100 || mMaze[mCurX, mCurY] == -1) {
							ClearOpQueue();
						}
						return true;
					}
					break;
				case 'S':
					if ((mMaze[mCurX, mCurY] & mazeS) == mazeS) {
						++mCurX;
						mOpQueue.Enqueue(new Tuple<char, int, int>((char)Keys.S, mCurX, mCurY));
						if (mOpQueue.Count > 100 || mMaze[mCurX, mCurY] == -1) {
							ClearOpQueue();
						}
						return true;
					}
					break;
				case 'W':
					if ((mMaze[mCurX, mCurY] & mazeW) == mazeW) {
						--mCurY;
						mOpQueue.Enqueue(new Tuple<char, int, int>((char)Keys.A, mCurX, mCurY));
						if (mOpQueue.Count > 100 || mMaze[mCurX, mCurY] == -1) {
							ClearOpQueue();
						}
						return true;
					}
					break;
				case 'E':
					if ((mMaze[mCurX, mCurY] & mazeE) == mazeE) {
						++mCurY;
						mOpQueue.Enqueue(new Tuple<char, int, int>((char)Keys.D, mCurX, mCurY));
						if (mOpQueue.Count > 100 || mMaze[mCurX, mCurY] == -1) {
							ClearOpQueue();
						}
						return true;
					}
					break;
			}
			return false;
		}

		private bool CheckTreasure() {
			if (!mTreasureFound) {
				return false;
			}

			Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();
			Tuple<char, int, int>[,] nodeParnet = new Tuple<char, int, int>[mazeSize, mazeSize];
			bool[,] nodeVisited = new bool[mazeSize, mazeSize];

			queue.Enqueue(new Tuple<int, int>(mCurX, mCurY));
			nodeVisited[mCurX, mCurY] = true;
			while (queue.Count != 0) {
				int x = queue.Peek().Item1, y = queue.Peek().Item2;

				queue.Dequeue();

				if (x == mTreasureX && y == mTreasureY) {
					Stack<char> ops = new Stack<char>();
					while (x != mCurX || y != mCurY) {
						ops.Push(nodeParnet[x, y].Item1);
						int tempX = nodeParnet[x, y].Item2, tempY = nodeParnet[x, y].Item3;
						x = tempX;
						y = tempY;
					}
					while (ops.Count > 0) {
						Go(ops.Pop());
					}
					ClearOpQueue();
					mTreasureAvailable = true;
					return true;
				}

				if ((mMaze[x, y] & mazeN) == mazeN && mMaze[x - 1, y] != -1 && !nodeVisited[x - 1, y]) {
					nodeVisited[x - 1, y] = true;
					nodeParnet[x - 1, y] = new Tuple<char, int, int>('N', x, y);
					queue.Enqueue(new Tuple<int, int>(x - 1, y));
				}

				if ((mMaze[x, y] & mazeS) == mazeS && mMaze[x + 1, y] != -1 && !nodeVisited[x + 1, y]) {
					nodeVisited[x + 1, y] = true;
					nodeParnet[x + 1, y] = new Tuple<char, int, int>('S', x, y);
					queue.Enqueue(new Tuple<int, int>(x + 1, y));
				}

				if ((mMaze[x, y] & mazeW) == mazeW && mMaze[x, y - 1] != -1 && !nodeVisited[x, y - 1]) {
					nodeVisited[x, y - 1] = true;
					nodeParnet[x, y - 1] = new Tuple<char, int, int>('W', x, y);
					queue.Enqueue(new Tuple<int, int>(x, y - 1));
				}

				if ((mMaze[x, y] & mazeE) == mazeE && mMaze[x, y + 1] != -1 && !nodeVisited[x, y + 1]) {
					nodeVisited[x, y + 1] = true;
					nodeParnet[x, y + 1] = new Tuple<char, int, int>('E', x, y);
					queue.Enqueue(new Tuple<int, int>(x, y + 1));
				}
			}

			return false;
		}

		private void Dfs(int x, int y, char from, Graphics g) {
			mVisited[x, y] = true;
			from = Rotate(Rotate(Rotate(from)));
			for (int i = 0; i < 3; ++i) {
				switch (from) {
					case 'N':
						if (((mMaze[x, y] & mazeN) == mazeN) && !mVisited[x - 1, y]) {
							Go(from);
							DrawMaze(g);
							Dfs(x - 1, y, from, g);
						}
						break;
					case 'S':
						if (((mMaze[x, y] & mazeS) == mazeS) && !mVisited[x + 1, y]) {
							Go(from);
							DrawMaze(g);
							Dfs(x + 1, y, from, g);
						}
						break;
					case 'W':
						if (((mMaze[x, y] & mazeW) == mazeW) && !mVisited[x, y - 1]) {
							Go(from);
							DrawMaze(g);
							Dfs(x, y - 1, from, g);
						}
						break;
					case 'E':
						if (((mMaze[x, y] & mazeE) == mazeE) && !mVisited[x, y + 1]) {
							Go(from);
							DrawMaze(g);
							Dfs(x, y + 1, from, g);
						}
						break;
				}
				from = Rotate(from);
				if (mTreasureAvailable) {
					break;
				} else {
					CheckTreasure();
				}
			}
			if (!mTreasureAvailable) {
				Go(from);
				DrawMaze(g);
			}
		}

		public void Solve(Graphics g) {
			DrawMaze(g, true);
			Dfs(mCurX, mCurY, 'N', g);
		}
	}
}
