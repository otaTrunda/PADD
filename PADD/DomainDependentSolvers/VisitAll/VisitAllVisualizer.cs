using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace PADD.DomainDependentSolvers.VisitAll
{
	class VisitAllVisualizer
	{
		public PictureBox screen;
		public VisitAllDomain domain;
		Graphics g;
		Color backColor = Color.Beige;
		Pen gridPen = Pens.Gray,
			obstaclePen = Pens.Black,
			connectedPen = Pens.Green,
			targetPen = new Pen(new SolidBrush(Color.Red), 5f);
		Brush visitedBrush = Brushes.Yellow,
			idStringBrush = Brushes.DarkCyan;
		int maxGridWidth, maxGridHeigth;
		float tileSize;
		VisitAllVisForm form;
		float targetCrossMarginPercent = 20f;
		Font IDStringFont = new Font("Arial", 10);

		protected List<SASState> statesToDraw;
		protected int alreadyDrawnStates = 0;

		private bool drawAnotherState()
		{
			if (statesToDraw == null || alreadyDrawnStates >= statesToDraw.Count)
				return false;
			draw(new VisitAllState(statesToDraw[alreadyDrawnStates], domain));
			alreadyDrawnStates++;
			return true;
		}

		public VisitAllVisualizer(VisitAllDomain domain)
		{
			form = new VisitAllVisForm();
			this.screen = form.screen;
			this.domain = domain;
			screen.Image = new Bitmap(screen.Width, screen.Height);
			g = Graphics.FromImage(screen.Image);
			maxGridWidth = domain.nodes.Max(n => n.gridCoordX) + 1;
			maxGridHeigth = domain.nodes.Max(n => n.gridCoordY) + 1;
			tileSize = Math.Min(screen.Width / (maxGridWidth + 1), screen.Height / (maxGridHeigth + 1));
		}

		public void draw(List<SASState> states)
		{
			this.statesToDraw = states;
			alreadyDrawnStates = 0;
			new System.Threading.Thread(() =>
			{
				form.startTimer(drawAnotherState, () => true);
				Application.Run(form);
				//form.Show();

			}).Start();

			System.Threading.Thread.CurrentThread.Join();
		}

		public void draw(VisitAllState state = null)
		{
			g.Clear(backColor);
			for (int i = 0; i < maxGridWidth; i++)
				for (int j = 0; j < maxGridHeigth; j++)
				{
					var node = domain.nodes.Where(n => n.gridCoordX == i && n.gridCoordY == j).Single();
					g.DrawRectangle(gridPen, i * tileSize, j * tileSize, tileSize, tileSize);
					if (state?.visited[node.ID] == true)
						g.FillRectangle(visitedBrush, i * tileSize + 1, j * tileSize + 1, tileSize - 2, tileSize - 2);

					if (state?.position == node.ID)
					{
						g.FillEllipse(Brushes.BlueViolet, i * tileSize + tileSize * targetCrossMarginPercent / 100, j * tileSize + tileSize * targetCrossMarginPercent / 100,
						tileSize - 2 * tileSize * targetCrossMarginPercent / 100, tileSize - 2 * tileSize * targetCrossMarginPercent / 100);
					}
					g.DrawString(node.ID.ToString(), IDStringFont, idStringBrush, i * tileSize + 1, j * tileSize + 1);
				}
			/*
			for (int i = 0; i < domain.nodes.Count(); i++)
				for (int j = i + 1; j < domain.nodes.Count(); j++)
				{
					if (domain.connected[i, j])
					{
						g.DrawLine(connectedPen, domain.nodes[i].gridCoordX * tileSize + tileSize / 2, domain.nodes[i].gridCoordY * tileSize + tileSize / 2,
							domain.nodes[j].gridCoordX * tileSize + tileSize / 2, domain.nodes[j].gridCoordY * tileSize + tileSize / 2);
					}
				}
			*/
			screen.Refresh();
			if (!form.Visible) form.ShowDialog();
		}
	}
}
