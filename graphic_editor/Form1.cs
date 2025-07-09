using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace graphic_editor
{
    public partial class Form1 : Form
    {
        Bitmap picture;
        int x1, y1;
        Stack<Bitmap> undoStack = new Stack<Bitmap>();
        bool isFileOpened = false;
        public Form1()
        {
            InitializeComponent();
            picture = new Bitmap(1920, 1000);
            x1 = y1 = 0;
            SaveState(true);

            colorDialog1.FullOpen = false;
            colorDialog1.AnyColor = true;
            colorDialog1.Color = button4.BackColor;
        }

        private void SaveState(bool isInitialState = false)
        {
            if (isFileOpened && !isInitialState)
            {
                undoStack.Clear();
                isFileOpened = false;
            }
            undoStack.Push(new Bitmap(picture));
    
        }
        private void buttonSelectColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                button4.BackColor = colorDialog1.Color;
            }
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
            if(saveFileDialog1.FileName != " ")
            {
                picture.Save(saveFileDialog1.FileName);
            }
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            if (openFileDialog1.FileName != " ")
            {
                picture = (Bitmap)Image.FromFile(openFileDialog1.FileName);
                pictureBox1.Image = picture;
                isFileOpened = true;
                SaveState();
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            SaveState();
        }      
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            Pen p = new Pen(button4.BackColor, trackBar1.Value);
            p.StartCap = System.Drawing.Drawing2D.LineCap.Round;
            p.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            Graphics g = Graphics.FromImage(picture);
            if (e.Button == MouseButtons.Left)
            {
                g.DrawLine(p, x1, y1, e.X, e.Y);
                pictureBox1.Image = picture;
            }
            x1 = e.X;
            y1 = e.Y;
        }
        private void ClearCanvas()
        {
            using (Graphics g = Graphics.FromImage(picture)) g.Clear(Color.White);
            pictureBox1.Image = picture;
            SaveState();
        }

        private void очиститьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearCanvas();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Z) && undoStack.Count > 1)
            {
                undoStack.Pop();
                picture = new Bitmap(undoStack.Peek());
                pictureBox1.Image = picture;
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

    }
}
