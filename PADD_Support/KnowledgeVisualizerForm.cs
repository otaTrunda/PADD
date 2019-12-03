using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Windows.Forms;
using PADD_Support.KnowledgeExtraction;

namespace PADD_Support
{
    public partial class KnowledgeVisualizerForm : Form
    {
        public KnowledgeHolder h;
        public KnowledgeVisualizerForm()
        {
            InitializeComponent();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0)
                h.show(listView1.SelectedIndices[0], panel1);
            Refresh();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
			if (Path.GetExtension(openFileDialog1.FileName) == ".sas")
				h = KnowledgeHolder.compute(new PAD.Planner.SAS.Problem(openFileDialog1.FileName));
			else
			{
				var domain = Path.Combine(Path.GetDirectoryName(openFileDialog1.FileName), "domain.pddl");
				h = KnowledgeHolder.create(new PAD.Planner.PDDL.Problem(domain, openFileDialog1.FileName));
			}
            listView1.Clear();
            listView1.Items.Add("Causual Graph");
            for (int i = 0; i < h.CG.vertices.Count; i++)
            {
                listView1.Items.Add("DTG var" + i.ToString());
            }
            for (int i = 0; i < h.CG.vertices.Count; i++)
            {
                listView1.Items.Add("DTG NoLabel var" + i.ToString());
            }
            h.show(0, panel1);
            Refresh();
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

		private void listView1_DoubleClick(object sender, EventArgs e)
		{
			openFileDialog1.ShowDialog();
		}
	}
}
