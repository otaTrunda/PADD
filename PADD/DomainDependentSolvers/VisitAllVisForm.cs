using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PADD.DomainDependentSolvers
{
	public partial class VisitAllVisForm : Form
	{
		public PictureBox screen;
		public VisitAllVisForm()
		{
			InitializeComponent();
			this.screen = pictureBox1;
		}
	}
}
