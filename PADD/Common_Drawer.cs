using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace PADD
{
    class TreeDrawer
    {
        private PictureBox treeScreen, valueScreen;
        private int sizeX, sizeY;
        private Graphics gTree, gValues;
        int treeNodeSize;
        private static Random rand = new Random();
        private double TotalVisits;
        private Dictionary<PointF, TreeNode> nodesPositions;

        public TreeDrawer(PictureBox pTree, PictureBox pValues)
        {
            this.treeScreen = pTree;
            this.sizeX = pTree.Width;
            this.sizeY = pTree.Height;
            pTree.Image = new Bitmap(sizeX, sizeY);
            this.gTree = Graphics.FromImage(pTree.Image);
            this.treeNodeSize = 2;
            this.valueScreen = pValues;
            pValues.Image = new Bitmap(sizeX, sizeY);
            this.gValues = Graphics.FromImage(pValues.Image);
            nodesPositions = new Dictionary<PointF, TreeNode>();
        }

        public void draw(TreeNode root)
        {
            gTree.Clear(Color.White);
            gValues.Clear(Color.Gainsboro);
            TotalVisits = root.nVisited;
            nodesPositions.Clear();
            draw(root, new RectangleF(0f, 0f, sizeX, sizeY), -10, -10, (float)(root.scoreSum/root.nVisited));
            treeScreen.Refresh();
            valueScreen.Refresh();
        }

        private void draw(TreeNode node, RectangleF r, float parrentX, float parrentY, float parrentValue)
        {
            if (node.succesors.Count == 0)
            {
                /*
                int degree = (int)(node.nVisited * 255 / TotalVisits);
                Color c = Color.FromArgb(255 - degree, 255 - degree, 255 - degree);
                 */
                Pen p = new Pen(Color.Black);
                if (node.scoreSum != 0)
                    gValues.DrawLine(p, r.Left, (float)(this.valueScreen.Height * node.scoreSum / node.nVisited), r.Right, (float)(this.valueScreen.Height * node.scoreSum / node.nVisited));
                else gValues.DrawLine(p, r.Left, (float)(this.valueScreen.Height * parrentValue), r.Right, (float)(this.valueScreen.Height * parrentValue));
              
            }

            double layerHeight = (double)r.Height / (node.subtreeDepth + 2);
            //g.FillRectangle(new SolidBrush(Color.FromArgb(rand.Next(255), rand.Next(255), rand.Next(255))), r);
            int degree = (int)(55+node.nVisited * 200 / TotalVisits);
            Color c = Color.FromArgb(255 - degree, 255 - degree, 255 - degree);            
            
            gTree.FillRectangle(new SolidBrush(c), (float)((r.Left + r.Right) / 2), (float)(r.Top + layerHeight), treeNodeSize, treeNodeSize);
            gTree.DrawString(node.nVisited + "\n" + (node.scoreSum / node.nVisited).ToString("0.000") + "\n" + node.eval().ToString("0.00"), new Font("Arial", node.subtreeDepth*2+2), Brushes.Black, (float)((r.Left + r.Right) / 2), (float)(r.Top + layerHeight));
            nodesPositions.Add(new PointF((float)((r.Left + r.Right) / 2), (float)(r.Top + layerHeight)), node);
            if (parrentX > 0)
            {
                gTree.DrawLine(new Pen(c), parrentX + treeNodeSize / 2, parrentY + treeNodeSize / 2,
                    (float)((r.Left + r.Right) / 2 + treeNodeSize / 2), (float)(r.Top + layerHeight + treeNodeSize / 2));
            }
            for (int i = 0; i < node.succesors.Count; i++)
            {
                float successorWidth = (r.Right - r.Left) / node.succesors.Count;
                draw(node.succesors[i], new RectangleF(r.Left + i * successorWidth, (float)(r.Top + layerHeight), successorWidth,
                        (float)(r.Height - layerHeight)), (float)((r.Right + r.Left) / 2), (float)(r.Top + layerHeight), (float)(node.scoreSum / node.nVisited));
            }
        }


        internal int getVisits(Point point)
        {
            if (point == null) return 0;
            var keys = nodesPositions.Keys.Where(a => (a.X - point.X) * (a.X - point.X) + (a.Y - point.Y) * (a.Y - point.Y) <= 500);
            if (keys.Count() == 0)
                return 0;
            var n = nodesPositions[keys.First()];
            if (n != null)
                return (int)(n.nVisited);
            return 0;
        }
    }
}
