using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace graphic_editor
{
    public partial class Form1 : Form
    {
        Bitmap picture;
        Bitmap tempPicture;
        Bitmap previewPicture;
        int x1, y1;
        int xclick1, yclick1;
        Stack<Bitmap> undoStack = new Stack<Bitmap>();
        bool isFileOpened = false;
        string mode;
        string brushTexture = "Обычная";
        Pen currentPen;
        bool isEraser = false;

        Rectangle selectionRect;
        bool isSelecting = false;
        bool hasSelection = false;
        Bitmap clipboardBuffer = null;
        Point pasteLocation;
        bool isMovingSelection = false;
        Point selectionOffset;
        public Form1()
        {
            InitializeComponent();
            picture = new Bitmap(1920, 1000);
            tempPicture = new Bitmap(1920, 1000);
            previewPicture = new Bitmap(1920, 1000);
            using (Graphics g = Graphics.FromImage(picture)) g.Clear(Color.White);
            using (Graphics g = Graphics.FromImage(tempPicture)) g.Clear(Color.White);
            using (Graphics g = Graphics.FromImage(previewPicture)) g.Clear(Color.White);
            x1 = y1 = 0;
            SaveState(true);
            mode = "Карандаш";
            colorDialog1.FullOpen = false;
            colorDialog1.AnyColor = true;
            colorDialog1.Color = button4.BackColor;

            UpdateCurrentPen();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Копировать", null);
            contextMenu.Items.Add("Вырезать", null);
            contextMenu.Items.Add("Вставить", null);
            pictureBox1.ContextMenuStrip = contextMenu;
        }

        private void UpdateCurrentPen()
        {
            currentPen?.Dispose();
            if (isEraser)
            {
                currentPen = new Pen(Color.White, trackBarEraser.Value);
                currentPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                currentPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;  
            }
            currentPen = new Pen(button4.BackColor, trackBarPen.Value);
            switch (brushTexture)
            {
                case "Акварель":
                    currentPen.StartCap = LineCap.Round;
                    currentPen.EndCap = LineCap.Round;
                    currentPen.LineJoin = LineJoin.Round;
                    currentPen.Width = trackBarPen.Value;
                    Color transparentColor = Color.FromArgb(50, button4.BackColor);
                    currentPen.Color = transparentColor;
                    break;

                default:
                    currentPen.StartCap = LineCap.Round;
                    currentPen.EndCap = LineCap.Round;
                    currentPen.LineJoin = LineJoin.Round;
                    break;
            }
               
        }
        private Random random = new Random();
        private void SaveState(bool isInitialState = false)
        {
            if (isFileOpened && !isInitialState)
            {
                undoStack.Clear();
                isFileOpened = false;
            }
            undoStack.Push(new Bitmap(picture));
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                
                if (mode == "Карандаш" || mode == "Ластик")
                {
                    using (Graphics g = Graphics.FromImage(picture))
                    {
                        if (brushTexture == "Акварель")
                        {
                            float distance = (float)Math.Sqrt(Math.Pow(e.X - x1, 2) + Math.Pow(e.Y - y1, 2));
                            int steps = Math.Max(1, (int)(distance / 2));
                            for (int i = 0; i <= steps; i++)
                            {
                                float t = (float)i / steps;
                                int currentX = (int)(x1 + t * (e.X - x1));
                                int currentY = (int)(y1 + t * (e.Y - y1));
                                int maxRadius = (int)(currentPen.Width * 0.5f);
                                for (int j = 0; j < 20; j++)
                                {
                                    int size = random.Next(maxRadius / 2, maxRadius);
                                    int xOffset = random.Next(-size / 2, size / 2);
                                    int yOffset = random.Next(-size / 2, size / 2);
                                    int alpha = 10;

                                    using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, button4.BackColor)))
                                    {
                                        g.FillEllipse(b,
                                            currentX + xOffset - size / 2,
                                            currentY + yOffset - size / 2,
                                            size, size);
                                    }
                                }
                            }
                        }
                        else
                        {
                            g.DrawLine(currentPen, x1, y1, e.X, e.Y);
                        }
                            
                    }
                    pictureBox1.Image = picture;
                }
                else
                {
                    using (Graphics g = Graphics.FromImage(previewPicture))
                    {
                        g.DrawImage(picture, 0, 0);

                        int x = Math.Min(xclick1, e.X);
                        int y = Math.Min(yclick1, e.Y);
                        int width = Math.Abs(e.X - xclick1);
                        int height = Math.Abs(e.Y - yclick1);

                        switch (mode)
                        {
                            case "Прямоугольник":
                                g.DrawRectangle(currentPen, x, y, width, height);
                                break;
                            case "Овал":
                                g.DrawEllipse(currentPen, x, y, width, height);
                                break;
                            case "Прямая линия":
                                g.DrawLine(currentPen, xclick1, yclick1, e.X, e.Y);
                                break;
                        }
                    }
                    pictureBox1.Image = previewPicture;
                }
            }
            x1 = e.X;
            y1 = e.Y;

        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (mode != "Карандаш" && e.Button == MouseButtons.Left)
            {
                using (Graphics g = Graphics.FromImage(picture))
                using (Pen p = new Pen(button4.BackColor, trackBarPen.Value))
                {
                    int x = Math.Min(xclick1, e.X);
                    int y = Math.Min(yclick1, e.Y);
                    int width = Math.Abs(e.X - xclick1);
                    int height = Math.Abs(e.Y - yclick1);

                    switch (mode)
                    {
                        case "Прямоугольник":
                            g.DrawRectangle(currentPen, x, y, width, height);
                            break;
                        case "Овал":
                            g.DrawEllipse(currentPen, x, y, width, height);
                            break;
                        case "Прямая линия":
                            g.DrawLine(currentPen, xclick1, yclick1, e.X, e.Y);
                            break;

                    }
                }
                pictureBox1.Image = picture;
                
            }
            SaveState();
        }


        private void buttonSelectColor_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() == DialogResult.OK)
            {
                isEraser = false;
                button4.BackColor = colorDialog1.Color;
                button1.BackColor = Color.White;
                UpdateCurrentPen();
            }
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
            if (saveFileDialog1.FileName != " ")
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

        private void прямоугольникToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = "Прямоугольник";
            brushTexture = null;
            isEraser = false;
            trackBarEraser.Visible = false;
            trackBarPen.Visible = true;
            button1.BackColor = Color.White;
            button2.BackColor = Color.DarkGray;
            UpdateCurrentPen();
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            xclick1 = e.X;
            yclick1 = e.Y;
        }

        private void прямаяЛинияToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = "Прямая линия";
            isEraser = false;
            brushTexture = null;
            trackBarEraser.Visible = false;
            trackBarPen.Visible = true;
            button1.BackColor = Color.White;
            button2.BackColor = Color.DarkGray;
            UpdateCurrentPen();
        }

        private void овалToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mode = "Овал";
            isEraser = false;
            brushTexture = null;
            trackBarEraser.Visible = false;
            trackBarPen.Visible = true;
            button1.BackColor = Color.White;
            button2.BackColor = Color.DarkGray;
            UpdateCurrentPen();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            isEraser = true;
            trackBarEraser.Visible = true;
            trackBarPen.Visible = false;
            button1.BackColor = isEraser ? Color.DarkGray : Color.White;
            button2.BackColor = isEraser ? Color.White : Color.DarkGray;
            mode = "Ластик";
            brushTexture = "Обычная";
            UpdateCurrentPen();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            mode = "Карандаш";
            brushTexture = "Обычная";
            isEraser = false;
            button1.BackColor = isEraser ? Color.DarkGray : Color.White;
            button2.BackColor = isEraser ? Color.White : Color.DarkGray;
            trackBarEraser.Visible = false;
            trackBarPen.Visible = true;
            UpdateCurrentPen();
        }

        private void trackBarEraser_ValueChanged(object sender, EventArgs e)
        {
            UpdateCurrentPen();
        }

        private void trackBarPen_ValueChanged(object sender, EventArgs e)
        {
            UpdateCurrentPen();
        }
        private void drawSelectionPreview()
        {

        }

        private void Copy()
        {

        }

        private void Paste()
        {

        }

        

        private void Cut()
        {

        }
        private void акварельToolStripMenuItem_Click(object sender, EventArgs e)
        {
            brushTexture = "Акварель";
            isEraser = false;
            trackBarEraser.Visible = false;
            trackBarPen.Visible = true;
            button1.BackColor = Color.White;
            button2.BackColor = Color.DarkGray;
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