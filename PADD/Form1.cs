using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SASPlan
{
    public partial class Form1 : Form
    {
        public KnowledgeHolder h;
        public Form1()
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
            h = KnowledgeHolder.compute(Domain.readFromFile(openFileDialog1.FileName));
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
    }
}
