using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace PADD.DomainDependentSolvers.BlocksWorld
{
	class BlocksWorldDrawer
	{
		protected int maxTowers = 0;
		public int blockSize;
		public List<Tower> blockTowers;
		public Graphics g;
		Font blockDescriptionFont = new Font("Arial", 12);

		Brush correctBlockBrush = Brushes.Green,
			incorrectClockBrush = Brushes.Red;

		public void draw(PictureBox screen, BlocksWorldProblem problem)
		{
			blockTowers = createTowers(problem);
			blockSize = Math.Min(screen.Width / (maxTowers * 2), screen.Height / blockTowers.Max(t => t.blocksInTower.Count));
			screen.Image = new Bitmap(screen.Width, screen.Height);
			g = Graphics.FromImage(screen.Image);
			g.Clear(Color.WhiteSmoke);

			for (int i = 0; i < blockTowers.Count; i++)
			{
				for (int j = 0; j < blockTowers[i].blocksInTower.Count; j++)
				{
					Block b = blockTowers[i].blocksInTower[j];
					string targetBlockBelow = b.targetBlockBelow != null ? b.targetBlockBelow.ID.ToString() : "-1";
					Brush brush = b.isCorrect() ? correctBlockBrush : incorrectClockBrush;
					Rectangle r = new Rectangle(i * blockSize * 2, j * blockSize, blockSize, blockSize);
					g.FillRectangle(brush, r);
					g.DrawRectangle(Pens.Black, r);
					g.DrawString(b.originalName + "(" + b.ID + ")" + "\n[" + targetBlockBelow + "]", blockDescriptionFont, Brushes.Black, r);
				}
			}
			screen.Refresh();

		}

		protected List<Tower> createTowers(BlocksWorldProblem problem)
		{
			List<Tower> result = new List<Tower>();
			foreach (var item in problem.blocksByIDs.Values.Where(b => b.isOnTable()))
			{
				Tower t = new Tower(item);
				var blockAbove = item.currentBlockAbove;
				while (blockAbove != null)
				{
					t.blocksInTower.Add(blockAbove);
					blockAbove = blockAbove.currentBlockAbove;
				}
				result.Add(t);
			}
			maxTowers = maxTowers < result.Count ? result.Count : maxTowers;
			return result;
		}

	}

	class Tower
	{
		public List<Block> blocksInTower;
		public Tower(Block initialBlock)
		{
			blocksInTower = new List<Block>();
			blocksInTower.Add(initialBlock);
		}
	}
}
