using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace maze {
	public partial class Form1 : Form {
		private Solver mSolve;
		private Thread mThread;

		public Form1() {
			InitializeComponent();
		}

		private void pictureBox1_Paint(object sender, PaintEventArgs e) {
			mSolve.DrawMaze(e.Graphics, true);
		}

		private void Form1_Load(object sender, EventArgs e) {
			mSolve = new Solver();

			this.FormClosed += new FormClosedEventHandler(Form1_FormClosed);

			this.pictureBox1.ClientSize = new Size(Solver.cellSize * Solver.mazeSize, Solver.cellSize * Solver.mazeSize);
			this.pictureBox1.Paint += new PaintEventHandler(this.pictureBox1_Paint);

			this.button1.Width = 100;
			this.button1.Height = 40;
			this.button1.Location = new Point((Solver.cellSize * Solver.mazeSize - this.button1.Width) / 2, Solver.cellSize * Solver.mazeSize + 10);

			this.Text = "Maze Puzzle Solver";
			this.ClientSize = new Size(Solver.cellSize * Solver.mazeSize + 5, Solver.cellSize * Solver.mazeSize + 60);

			mSolve.Initialize("http://104.131.57.70:3001");
		}

		void Form1_FormClosed(object sender, FormClosedEventArgs e) {
			if (mThread != null) {
				mThread.Abort();
			}
		}

		private void button1_Click(object sender, EventArgs e) {
			mThread = new Thread(new ThreadStart(() => {
				Graphics g = this.pictureBox1.CreateGraphics();
				mSolve.Solve(g);
				g.Dispose();
			}));
			mThread.Start();
		}
	}
}
