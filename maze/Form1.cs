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
		private Solver solve;

		private char keyWarp(Keys c) {
			if (c == Keys.A) {
				return 'W';
			} else if (c == Keys.D) {
				return 'E';
			} else if (c == Keys.W) {
				return 'N';
			} else if (c == Keys.S) {
				return 'S';
			}
			return ' ';
		}

		public Form1() {
			InitializeComponent();
		}

		public void Form1_KeyPress(object sender, KeyPressEventArgs e) {
			e.Handled = solve.Go(keyWarp((Keys)e.KeyChar));
			Graphics g = this.CreateGraphics();
			solve.DrawMaze(g, true);
			g.Dispose();
		}

		private void pictureBox1_Paint(object sender, PaintEventArgs e) {
			solve.DrawMaze(e.Graphics, true);
		}

		private void Form1_Load(object sender, EventArgs e) {
			solve = new Solver();

			this.KeyPreview = true;
			this.KeyPress += new KeyPressEventHandler(this.Form1_KeyPress);

			this.pictureBox1.ClientSize = new Size(Solver.cellSize * Solver.mazeSize, Solver.cellSize * Solver.mazeSize);
			this.pictureBox1.Paint += new PaintEventHandler(this.pictureBox1_Paint);

			this.button1.Width = 100;
			this.button1.Height = 40;
			this.button1.Location = new Point((Solver.cellSize * Solver.mazeSize - this.button1.Width) / 2, Solver.cellSize * Solver.mazeSize + 10);

			this.Text = "Maze Puzzle Solver";
			this.ClientSize = new Size(Solver.cellSize * Solver.mazeSize + 5, Solver.cellSize * Solver.mazeSize + 60);

			solve.Initialize("http://104.131.57.70:3001");
		}

		private void button1_Click(object sender, EventArgs e) {
			Thread thread = new Thread(new ThreadStart(() => {
				Graphics g = this.pictureBox1.CreateGraphics();
				solve.Solve(g);
				g.Dispose();
			}));
			thread.Start();
		}
	}
}
