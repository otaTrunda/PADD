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
		protected Func<bool> timerStepCode, timerStopsCode;
		protected int timerInterval = 100;

		public PictureBox screen;
		public Timer timer;
		public VisitAllVisForm()
		{
			InitializeComponent();
			this.screen = pictureBox1;
			this.timer = drawingTimer;
			this.timer.Interval = timerInterval;
		}

		private void drawingTimer_Tick(object sender, EventArgs e)
		{
			if (timerStepCode() == false)
			{
				timer.Stop();
				timerStopsCode();
			}
		}

		private void Pause_button_Click(object sender, EventArgs e)
		{
			timer.Enabled = !timer.Enabled;
		}

		public void startTimer(Func<bool> timerStepCode, Func<bool> timerStopsCode)
		{
			this.timerStepCode = timerStepCode;
			this.timerStopsCode = timerStopsCode;
			timer.Start();
		}
	}
}
