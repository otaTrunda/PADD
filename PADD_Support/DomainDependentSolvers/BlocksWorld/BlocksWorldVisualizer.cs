using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PADD.DomainDependentSolvers.BlocksWorld
{
	public partial class BlocksWorldVisualizer : Form
	{
		BlocksWorldDrawer drawer;
		PictureBox screen;

		public BlocksWorldVisualizer(BlocksWorldProblem p = null)
		{
			InitializeComponent();
			this.screen = pictureBox1;
			this.drawer = new BlocksWorldDrawer();
			if (p != null)
				draw(p);
		}

		public void draw(BlocksWorldProblem p)
		{
			drawer.draw(screen, p);
		}
	}
}
