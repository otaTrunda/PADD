using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PADD
{
    partial class TreeVisualizerForm : Form
    {
        MCTSSolver planner;
        TreeDrawer treeDrawer;
        Point mousePos;

        public TreeVisualizerForm(SASProblem d, Heuristic h)
        {
            InitializeComponent();
            treeDrawer = new TreeDrawer(pictureBox2, pictureBox1);
            planner = new MCTSSolver(d, h);
        }

        public TreeVisualizerForm()
        {
            InitializeComponent();
            treeDrawer = new TreeDrawer(pictureBox2, pictureBox1);
            
            //planner = new MCTSSolver(null, null);
        }

        private void Generate_button_Click(object sender, EventArgs e)
        {

        }

        private void Save_button_Click(object sender, EventArgs e)
        {

        }

        private void saveFunc_Dialog_FileOk(object sender, CancelEventArgs e)
        {
           
        }

        private void Load_button_Click(object sender, EventArgs e)
        {
          
        }

        private void loadFunc_Dialog_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void GoSteps_button_Click(object sender, EventArgs e)
        {
            planner.doMoreSteps((long)Math.Pow(10, steps_trackBar.Value));
            treeDrawer.draw(planner.root);
        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            this.mousePos = e.Location;
            //Visited_label.Text = e.Location.ToString();
            Visited_label.Text = treeDrawer.getVisits(mousePos).ToString();
        }
    }
}
