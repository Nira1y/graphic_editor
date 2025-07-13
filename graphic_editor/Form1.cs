using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;


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
        bool isPipette = false;

        Rectangle selectionRect;
        bool isSelecting;
        bool hasSelection = false;
        Bitmap clipboardBuffer = null;
        Point pasteLocation;
        bool isFilling = false;

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
            contextMenu.Items.Add("Копировать", null, (s, e) => Copy());
            contextMenu.Items.Add("Вырезать", null, (s, e) => Cut());
            contextMenu.Items.Add("Вставить", null, (s, e) => Paste());
            pictureBox1.ContextMenuStrip = contextMenu;
        }
        private Random random = new Random();
        private void SaveState(bool isInitialState = false)
        {
            if (isFileOpened && !isInitialState)
            {
                while (undoStack.Count > 1)
                {
                    undoStack.Pop()?.Dispose();
                }
                isFileOpened = false;
            }

            if (undoStack.Count >= 21)
            {
                Bitmap lastState = undoStack.Peek();
                while (undoStack.Count > 1)
                {
                    undoStack.Pop()?.Dispose();
                }
                undoStack.Clear();
                undoStack.Push(lastState);
            }

            var stateCopy = new Bitmap(picture);
            undoStack.Push(stateCopy);
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
            else
            {
                currentPen = new Pen(button4.BackColor, trackBarPen.Value);
                currentPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                currentPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;
            }
        }

        private void PickColor(int x, int y)
        {
            if (x >= 0 && x < picture.Width && y >= 0 && y < picture.Height)
            {
                Color pickedColor = picture.GetPixel(x, y);
                button4.BackColor = pickedColor;
                colorDialog1.Color = pickedColor;
                isEraser = false;
                UpdateCurrentPen();
            }
        }

        private void DrawBrush(Graphics g, Point start, Point end)
        {
            switch (brushTexture)
            {
                case "Акварель":
                    Watercolor(g, start, end);
                    break;
                case "Мел":
                    Chalk(g, start, end);
                    break;
                case "Карандаш":
                    Pencil(g, start, end);
                    break;
                default:
                    g.DrawLine(currentPen, start.X, start.Y, end.X, end.Y);
                    break;
            }
        }

        private void Watercolor(Graphics g, Point start, Point end)
        {
            float distance = (float)Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            int steps = Math.Max(1, (int)(distance / 2));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                int currentX = (int)(start.X + t * (end.X - start.X));
                int currentY = (int)(start.Y + t * (end.Y - start.Y));
                int maxRadius = (int)(currentPen.Width * 0.5f);
                for (int j = 0; j < 20; j++)
                {
                    int size = random.Next(maxRadius / 2, maxRadius);
                    int xOffset = random.Next(-size / 2, size / 2);
                    int yOffset = random.Next(-size / 2, size / 2);
                    int alpha = 10;

                    using (SolidBrush b = new SolidBrush(Color.FromArgb(alpha, button4.BackColor)))
                    {
                        g.FillEllipse(b, currentX + xOffset - size / 2, currentY + yOffset - size / 2, size, size);
                    }
                }
            }
        }

        private void Chalk(Graphics g, Point start, Point end)
        {
            float dotSpacing = Math.Max(2, trackBarPen.Value * 0.3f);
            float distance = (float)Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            int steps = Math.Max(1, (int)(distance / dotSpacing));
            int lineCount = Math.Max(1, (int)(trackBarPen.Value * 0.3f));

            for (int lineOffset = 0; lineOffset < lineCount; lineOffset++)
            {
                for (int i = 0; i <= steps; i++)
                {
                    if (random.Next(0, 100) < 30) continue;
                    float t = (float)i / steps;
                    int currentX = (int)(start.X + t * (end.X - start.X)) + lineOffset * 2;
                    int currentY = (int)(start.Y + t * (end.Y - start.Y)) + lineOffset * 2;

                    if (random.Next(0, 100) < 70)
                    {
                        int dotSize = random.Next(1, 4);
                        g.FillEllipse(new SolidBrush(button4.BackColor), currentX - dotSize / 2, currentY - dotSize / 2, dotSize, dotSize);
                    }
                }
            }
        }
        private void Pencil(Graphics g, Point start, Point end)
        {
            float roughness = 3f + currentPen.Width * 0.2f;
            float distance = (float)Math.Sqrt(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
            int steps = Math.Max(1, (int)(distance * (2f + currentPen.Width * 0.5f)));

            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                int currentX = (int)(start.X + t * (end.X - start.X));
                int currentY = (int)(start.Y + t * (end.Y - start.Y));

                int xOffset = (int)((random.NextDouble() - 0.5) * roughness * 2);
                int yOffset = (int)((random.NextDouble() - 0.5) * roughness * 2);

                int textureX = currentX + xOffset;
                int textureY = currentY + yOffset;


                using (Pen texturePen = new Pen(button4.BackColor, 1f))
                {
                    g.DrawLine(texturePen, textureX, textureY, textureX + random.Next(-1, 2), textureY + random.Next(-1, 2));
                }
            }
        }
        private void FillArea(int x, int y, Color targetColor, Color replacementColor)
        {
            bool isUnbounded = IsAreaUnbounded(x, y, targetColor);

            if (isUnbounded)
            {
                using (Graphics g = Graphics.FromImage(picture))
                using (SolidBrush brush = new SolidBrush(replacementColor))
                {
                    g.FillRectangle(brush, 0, 0, picture.Width, picture.Height);
                }
            }
            else
            {
                Bitmap bmp = picture;
                Stack<Point> pixels = new Stack<Point>();
                pixels.Push(new Point(x, y));
                bool[,] visited = new bool[bmp.Width, bmp.Height];

                while (pixels.Count > 0)
                {
                    Point a = pixels.Pop();
                    if (a.X < 0 || a.X >= bmp.Width || a.Y < 0 || a.Y >= bmp.Height || visited[a.X, a.Y])
                        continue;

                    Color current = bmp.GetPixel(a.X, a.Y);
                    if (!ColorsAreSimilar(current, targetColor, 10))
                        continue;

                    bmp.SetPixel(a.X, a.Y, replacementColor);
                    visited[a.X, a.Y] = true;

                    pixels.Push(new Point(a.X - 1, a.Y));
                    pixels.Push(new Point(a.X + 1, a.Y));
                    pixels.Push(new Point(a.X, a.Y - 1));
                    pixels.Push(new Point(a.X, a.Y + 1));
                }
            }

            pictureBox1.Image = picture;
        }

        private bool IsAreaUnbounded(int startX, int startY, Color targetColor)
        {
            Bitmap bmp = picture;
            Queue<Point> queue = new Queue<Point>();
            bool[,] visited = new bool[bmp.Width, bmp.Height];
            queue.Enqueue(new Point(startX, startY));

            while (queue.Count > 0)
            {
                Point p = queue.Dequeue();
                if (p.X <= 0 || p.Y <= 0 || p.X >= bmp.Width - 1 || p.Y >= bmp.Height - 1)
                    return true;

                if (visited[p.X, p.Y])
                    continue;

                visited[p.X, p.Y] = true;
                if (ColorsAreSimilar(bmp.GetPixel(p.X - 1, p.Y), targetColor, 10))
                    queue.Enqueue(new Point(p.X - 1, p.Y));
                if (ColorsAreSimilar(bmp.GetPixel(p.X + 1, p.Y), targetColor, 10))
                    queue.Enqueue(new Point(p.X + 1, p.Y));
                if (ColorsAreSimilar(bmp.GetPixel(p.X, p.Y - 1), targetColor, 10))
                    queue.Enqueue(new Point(p.X, p.Y - 1));
                if (ColorsAreSimilar(bmp.GetPixel(p.X, p.Y + 1), targetColor, 10))
                    queue.Enqueue(new Point(p.X, p.Y + 1));
            }

            return false;
        }
        private bool ColorsAreSimilar(Color c1, Color c2, int tolerance)
        {
            return Math.Abs(c1.R - c2.R) < tolerance &&
                   Math.Abs(c1.G - c2.G) < tolerance &&
                   Math.Abs(c1.B - c2.B) < tolerance;
        }
        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (mode == "Карандаш" || mode == "Ластик")
                {
                    using (Graphics g = Graphics.FromImage(picture))
                    {
                        DrawBrush(g, new Point(x1, y1), new Point(e.X, e.Y));
                    }
                    pictureBox1.Image = picture;
                }
                else if (mode == "Выделение")
                {

                    int x = Math.Min(xclick1, e.X);
                    int y = Math.Min(yclick1, e.Y);
                    int width = Math.Abs(e.X - xclick1);
                    int height = Math.Abs(e.Y - yclick1);

                    selectionRect = new Rectangle(x, y, width, height);

                    using (Graphics g = Graphics.FromImage(previewPicture))
                    {
                        g.DrawImage(picture, 0, 0);
                        using (Pen selectPen = new Pen(Color.Blue, 1))
                        {
                            selectPen.DashStyle = DashStyle.Dash;
                            g.DrawRectangle(selectPen, selectionRect);
                        }
                    }
                    pictureBox1.Image = previewPicture;
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
            if (e.Button == MouseButtons.Left)
            {
                if (mode == "Выделение")
                {
                    hasSelection = true;
                    isSelecting = false;
                    pictureBox1.Image = picture;
                }
                else if (mode != "Карандаш")
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
                    SaveState();
                }
                else if (mode == "Карандаш")
                {
                    SaveState();
                }
            }
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

            if (isPipette && e.Button == MouseButtons.Left)
            {
                PickColor(e.X, e.Y);
                isPipette = false;
                buttonPipette.BackColor = Color.White;
                pictureBox1.Cursor = Cursors.Default;
            }
            else if (isFilling && e.Button == MouseButtons.Left)
            {
                Color targetColor = picture.GetPixel(e.X, e.Y);
                FillArea(e.X, e.Y, targetColor, button4.BackColor);
            }
            else if ((mode == "Карандаш" || mode == "Ластик") && e.Button == MouseButtons.Left)
            {
                using (Graphics g = Graphics.FromImage(picture))
                {
                    if (brushTexture == "Обычная")
                    {
                        g.FillEllipse(new SolidBrush(currentPen.Color), 
                            e.X - currentPen.Width / 2, e.Y - currentPen.Width / 2, currentPen.Width, currentPen.Width);
                    }
                    else
                    {
                        DrawBrush(g, new Point(e.X, e.Y), new Point(e.X, e.Y));
                    }
                }
                pictureBox1.Image = picture;
            }
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
            isFilling = false;
            isPipette = false;
            trackBarEraser.Visible = true;
            trackBarPen.Visible = false;
            button1.BackColor = isEraser ? Color.DarkGray : Color.White;
            button2.BackColor = isEraser ? Color.White : Color.DarkGray;
            buttonFill.BackColor = isFilling ? Color.DarkGray: Color.White;
            buttonPipette.BackColor = isPipette ? Color.DarkGray: Color.White;
            mode = "Ластик";
            brushTexture = "Обычная";
            UpdateCurrentPen();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            mode = "Карандаш";
            brushTexture = "Обычная";
            isPipette = false;
            isEraser = false;
            isFilling = false;
            button1.BackColor = isEraser ? Color.DarkGray : Color.White;
            button2.BackColor = isEraser ? Color.White : Color.DarkGray;
            buttonFill.BackColor = isFilling ? Color.DarkGray : Color.White;
            buttonPipette.BackColor = isPipette ? Color.DarkGray : Color.White;
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

        private void Copy()
        {
            if (hasSelection && selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                Bitmap selectedArea = new Bitmap(selectionRect.Width, selectionRect.Height);
                using (Graphics g = Graphics.FromImage(selectedArea))
                {
                    g.DrawImage(picture, new Rectangle(0, 0, selectedArea.Width, selectedArea.Height), selectionRect, GraphicsUnit.Pixel);
                }
                clipboardBuffer = selectedArea;
                Clipboard.SetImage(selectedArea);
            }
            else
            {
                clipboardBuffer = new Bitmap(picture);
                Clipboard.SetImage(picture);
            }
        }

        private void Paste()
        {
            if (Clipboard.ContainsImage())
            {
                Image imageToPaste = Clipboard.GetImage();

                if (imageToPaste != null)
                {
                    pasteLocation = new Point(
                        Math.Max(0, Math.Min(x1 - imageToPaste.Width / 2, picture.Width - imageToPaste.Width)),
                        Math.Max(0, Math.Min(y1 - imageToPaste.Height / 2, picture.Height - imageToPaste.Height))
                    );


                    using (Graphics g = Graphics.FromImage(picture))
                    {
                        g.DrawImage(imageToPaste, pasteLocation);
                    }

                    pictureBox1.Image = picture;
                    selectionRect = new Rectangle(pasteLocation, imageToPaste.Size);
                    hasSelection = true;
                    isSelecting = false;
                    clipboardBuffer = new Bitmap(imageToPaste);
                }
            }
            else if (clipboardBuffer != null)
            {
                pasteLocation = new Point(
                    Math.Max(0, Math.Min(x1 - clipboardBuffer.Width / 2, picture.Width - clipboardBuffer.Width)),
                    Math.Max(0, Math.Min(y1 - clipboardBuffer.Height / 2, picture.Height - clipboardBuffer.Height))
                );

                using (Graphics g = Graphics.FromImage(picture))
                {
                    g.DrawImage(clipboardBuffer, pasteLocation);
                }

                pictureBox1.Image = picture;
                selectionRect = new Rectangle(pasteLocation, clipboardBuffer.Size);
                hasSelection = true;
                isSelecting = false;
            }
            SaveState();
        }

        private void Cut()
        {
            if (hasSelection && selectionRect.Width > 0 && selectionRect.Height > 0)
            {
                Copy();
                using (Graphics g = Graphics.FromImage(picture))
                {
                    g.FillRectangle(Brushes.White, selectionRect);
                }

                pictureBox1.Image = picture;


                hasSelection = false;
                isSelecting = false;
                pictureBox1.Invalidate();
            }
        }

        private void StartSelection()
        {
            mode = "Выделение";
            isSelecting = true;
            hasSelection = false;
            button1.BackColor = Color.White;
            button2.BackColor = Color.DarkGray;
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

        private void карандашToolStripMenuItem_Click(object sender, EventArgs e)
        {
            brushTexture = "Карандаш";
            isEraser = false;
            trackBarEraser.Visible = false;
            trackBarPen.Visible = true;
            button1.BackColor = Color.White;
            button2.BackColor = Color.DarkGray;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            StartSelection();
        }

        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Cut();
        }

        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void buttonFill_Click(object sender, EventArgs e)
        {
            isFilling = !isFilling;
            isPipette = false;
            isEraser = false;
            brushTexture = null;
            trackBarEraser.Visible = false;
            trackBarPen.Visible = false;
            button1.BackColor = Color.White;
            button2.BackColor = Color.White;
            buttonFill.BackColor = isFilling ? Color.DarkGray : Color.White;
            buttonPipette.BackColor = isPipette ? Color.DarkGray : Color.White;
        }

        private void buttonPipette_Click(object sender, EventArgs e)
        {
            isPipette = !isPipette;
            isFilling = false;
            isEraser = false;

            buttonPipette.BackColor = isPipette ? Color.DarkGray : Color.White;
            button1.BackColor = Color.White;
            button2.BackColor = Color.White;
            buttonFill.BackColor = Color.White;

            if (isPipette)
            {
                pictureBox1.Cursor = Cursors.Cross;    
            }
            else
            {
                pictureBox1.Cursor = Cursors.Default;
            }
        }

        private void выйтиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Вы уверены, что хотите выйти?", "Выход", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void мелToolStripMenuItem_Click(object sender, EventArgs e)
        {
            brushTexture = "Мел";
            isEraser = false;
            trackBarEraser.Visible = false;
            trackBarPen.Visible = true;
            button1.BackColor = Color.White;
            button2.BackColor = Color.DarkGray;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.Z))
            {
                if (undoStack.Count > 1)
                {
                    Bitmap oldPicture = picture;
                    Bitmap currentState = undoStack.Pop();
                    currentState?.Dispose();
                    if (undoStack.Count > 0)
                    {
                        Bitmap previousState = undoStack.Peek();
                        try
                        {
                            picture = new Bitmap(previousState);
                            pictureBox1.Image = picture;
                            oldPicture?.Dispose();
                            return true;
                        }
                        catch
                        {
                            picture = oldPicture;
                            undoStack.Push(new Bitmap(oldPicture));
                            return false;
                        }
                    }
                    else
                    {
                        picture = oldPicture;
                        undoStack.Push(new Bitmap(oldPicture));
                    }
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}