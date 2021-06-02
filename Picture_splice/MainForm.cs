using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BinaryObject;

namespace Picture_splice
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }
        Control con = new Control();
        int xPos, yPos;
        bool MoveFlag;
        bool ishorizontal = true;
        List<PictureBox> pics = new List<PictureBox>();//图层
        //全屏缩放
        public readonly double[] suo = { 0, 0.25, 0.5, 1, 2, 3, 4, 5, 6, 7, 8 };//比例
        int flag = 3;
        struct Tags
        {
            public int flag;
            public string path;
        }
        private static Tags SetTags(int f, string p)
        {
            Tags t = new Tags();
            t.flag = f;
            t.path = p;
            return t;
        }
        private void 导入ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            if (fbd.ShowDialog() != DialogResult.Cancel)
            {
                ListView lv = new ListView
                {
                    SmallImageList = il,
                    LargeImageList = il,
                    Dock = DockStyle.Fill
                };
                DirectoryInfo di = new DirectoryInfo(fbd.SelectedPath);
                FileInfo[] files = di.GetFiles();
                tabControl1.TabPages.Add(di.Name);
                tabControl1.SelectedIndex = tabControl1.TabCount - 1;
                tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(lv);
                foreach (FileInfo fi in files)//加载图像文件
                {
                    if (fi.Extension == ".jpg" || fi.Extension == ".png" || fi.Extension == ".bmp" || fi.Extension == ".ico")
                    {
                        Image im = Image.FromFile(fi.FullName);
                        il.Images.Add(im);
                        ListViewItem lvi = new ListViewItem(fi.Name, il.Images.Count - 1)
                        {
                            Tag = fi.FullName
                        };
                        lv.Items.Add(lvi);
                        im.Dispose();
                        GC.Collect();
                    }
                    Application.DoEvents();
                }
                lv.DoubleClick += Lv_DoubleClick;
            }
            fbd.Dispose();
            GC.Collect();
        }

        private void Lv_DoubleClick(object sender, EventArgs e)
        {
            PictureBox pic = new PictureBox();
            Image image = Image.FromFile((string)((ListView)sender).SelectedItems[0].Tag);
            pic.Size = new Size(image.Width / 4, image.Height / 4);
            pic.SizeMode = PictureBoxSizeMode.StretchImage;
            pic.Image = image;
            pic.Location = new Point(pictures.Width / 2 - pic.Width / 2, pictures.Height / 2 - pic.Height / 2);
            pic.MouseMove += picBox_MouseMove;
            pic.MouseUp += picBox_MouseUp;
            pic.MouseDown += picBox_MouseDown;
            pic.MouseWheel += Pic_MouseWheel;
            pic.Tag = SetTags(pics.Count, (string)((ListView)sender).SelectedItems[0].Tag);
            pic.ContextMenuStrip = contextMenuStrip1;
            pics.Add(pic);

            if (pic.Left < 0) pic.Left = 0;
            if (pic.Top < 0) pic.Top = 0;

            RefreshList();
            RefreshPics();
        }
        private void RefreshList()
        {
            listView1.Items.Clear();
            foreach (PictureBox p in pics)
            {
                Tags t = (Tags)p.Tag;
                listView1.Items.Add(t.path);
            }
        }
        private void RefreshPics()
        {
            pictures.Controls.Clear();
            for (int i = pics.Count - 1; i >= 0; i--)
            {
                pictures.Controls.Add(pics[i]);
            }
        }
        private void Pic_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)//按下Ctrl，整体缩放
            {
                if (e.Delta > 0)//放大
                {
                    if (flag >= 10) return;//已最大
                    flag++;
                    double k1 = suo[flag] / suo[flag - 1];
                    foreach (Control co in pictures.Controls)
                    {
                        Size size1 = co.Size;
                        size1.Width = (int)(size1.Width * k1); size1.Height = (int)(size1.Height * k1);
                        co.Size = size1;
                        Point point = co.Location;
                        point.X = (int)(point.X * k1); point.Y = (int)(point.Y * k1);
                        co.Location = point;
                    }
                }
                if (e.Delta < 0)//缩小
                {
                    if (flag <= 1) return;//已最小
                    flag--;
                    double k1 = suo[flag] / suo[flag + 1];
                    foreach (Control co in pictures.Controls)
                    {
                        Size size1 = co.Size;
                        size1.Width = (int)(size1.Width * k1); size1.Height = (int)(size1.Height * k1);
                        co.Size = size1;
                        Point point = co.Location;
                        point.X = (int)(point.X * k1); point.Y = (int)(point.Y * k1);
                        co.Location = point;
                    }
                }
                label1.Text = "大小：" + (suo[flag] * 100).ToString() + "%";
                GC.Collect();
                return;
            }

            //缩放
            int kk = 5;
            if (Control.ModifierKeys == Keys.Shift)//按下Shift，快速缩放
                kk = 1;
            PictureBox pic = (PictureBox)sender;
            Size size = pic.Size;
            double angle = Math.Atan((double)size.Height / size.Width);
            size.Height += (int)(e.Delta * Math.Sin(angle) / kk);
            size.Width += (int)(e.Delta * Math.Cos(angle) / kk);
            if (size.Width <= 0 || size.Height <= 0) return;
            pic.Size = size;
            GC.Collect();
        }

        private void picBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (MoveFlag)
            {
                PictureBox picBox = (PictureBox)sender;
                picBox.Left += Convert.ToInt16(e.X - xPos);//设置x坐标.
                picBox.Top += Convert.ToInt16(e.Y - yPos);//设置y坐标.
                if (picBox.Left < 0) picBox.Left = 0;
                if (picBox.Top < 0) picBox.Top = 0;
            }
        }

        //在picturebox的鼠标按下事件里.
        private void picBox_MouseUp(object sender, MouseEventArgs e)
        {
            MoveFlag = false;
        }
        //在picturebox的鼠标按下事件里,记录三个变量.
        private void picBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                con = (Control)sender;
            }
            MoveFlag = true;//已经按下.
            xPos = e.X;//当前x坐标.
            yPos = e.Y;//当前y坐标.
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (ishorizontal == true) 上下分割ToolStripMenuItem_Click(new object(), new EventArgs());
            else 左右分割ToolStripMenuItem_Click(new object(), new EventArgs());
            label1.Text = "大小：" + (suo[flag] * 100).ToString() + "%";
        }
        private void 导出图片ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Image image = PictureCore.OutPut.SavePicture(pics, suo[flag]);
            if (image != null)
            {
                SaveFileDialog sfg = new SaveFileDialog();
                sfg.Filter = "*.png|*.png|*.jpg|*.jpg";
                sfg.Title = "保存图片到";
                if (sfg.ShowDialog() == DialogResult.OK)
                {
                    image.Save(sfg.FileName);
                }
            }
            GC.Collect();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("确定删除吗？", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                con.Dispose();
                RefreshList();
                RefreshPics();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pics.Count; i++)
            {
                if (con == pics[i])
                {
                    for (int j = i + 1; j < pics.Count; j++)
                        pics[j - 1] = pics[j];
                    pics[pics.Count - 1] = (PictureBox)con;
                    break;
                }
            }
            RefreshList();
            RefreshPics();
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pics.Count; i++)
            {
                if (con == pics[i])
                {
                    for (int j = i - 1; j >= 0; j--)
                        pics[j + 1] = pics[j];
                    pics[0] = (PictureBox)con;
                    break;
                }
            }
            RefreshList();
            RefreshPics();
        }

        public static Image imageeeeeeeeee;
        public static bool isok = false;
        public static string text = string.Empty;
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            Form form = new Newtext();
            form.ShowDialog();
            if (isok == true)
            {
                PictureBox pic = new PictureBox();
                pic.Size = new Size(imageeeeeeeeee.Width, imageeeeeeeeee.Height);
                pic.SizeMode = PictureBoxSizeMode.StretchImage;
                pic.Image = imageeeeeeeeee;
                pic.Location = new Point(pictures.Width / 2 - pic.Width / 2, pictures.Height / 2 - pic.Height / 2);
                pic.MouseMove += picBox_MouseMove;
                pic.MouseUp += picBox_MouseUp;
                pic.MouseDown += picBox_MouseDown;
                pic.MouseWheel += Pic_MouseWheel;
                pic.ContextMenuStrip = contextMenuStrip1;
                pic.Tag = SetTags(pics.Count, text);
                pics.Add(pic);

                if (pic.Left < 0) pic.Left = 0;
                if (pic.Top < 0) pic.Top = 0;

                RefreshList();
                RefreshPics();
            }
        }

        private void 新建ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictures.Controls.Clear();
            Text = "Picture_splice";
            pics.Clear();
            RefreshList();
            RefreshPics();
        }

        private void 打开ToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "*.psplice|*.psplice";
            ofd.Title = "打开文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                pictures.Controls.Clear();
                StreamReader sr = new StreamReader(ofd.FileName);
                string s = sr.ReadLine();
                while (sr.EndOfStream == false)
                {
                    if (File.Exists(s) == false)
                    {
                        MessageBox.Show("文件" + s + "未找到!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        s = sr.ReadLine();
                        s = sr.ReadLine();
                    }
                    else
                    {
                        PictureBox pic = new PictureBox();
                        Image image = Image.FromFile(s);
                        pic.Tag = s;

                        s = sr.ReadLine();
                        string[] str1 = s.Split(' ');
                        s = sr.ReadLine();
                        string[] str2 = s.Split(' ');

                        pic.Size = new Size(int.Parse(str2[0]), int.Parse(str2[1]));
                        pic.SizeMode = PictureBoxSizeMode.StretchImage;
                        pic.Image = image;
                        pic.Location = new Point(int.Parse(str1[1]), int.Parse(str1[0]));
                        pic.MouseMove += picBox_MouseMove;
                        pic.MouseUp += picBox_MouseUp;
                        pic.MouseDown += picBox_MouseDown;
                        pic.MouseWheel += Pic_MouseWheel;
                        pic.ContextMenuStrip = contextMenuStrip1;
                        if (pic.Left < 0) pic.Left = 0;
                        if (pic.Top < 0) pic.Top = 0;

                        pics.Add(pic);
                    }
                    s = sr.ReadLine();
                    RefreshList();
                    RefreshPics();
                }
                sr.Close();
                sr.Dispose();
                Text = ofd.FileName + " - Picture_splice";
                RefreshList();
                RefreshPics();
            }
            ofd.Dispose();
            GC.Collect();
        }

        private void 上下分割ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (splitContainer1.Orientation == Orientation.Horizontal) return;
            splitContainer1.Orientation = Orientation.Horizontal;
            左右分割ToolStripMenuItem.Checked = false;
            上下分割ToolStripMenuItem.Checked = true;
        }

        private void 左右分割ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (splitContainer1.Orientation == Orientation.Vertical) return;
            splitContainer1.Orientation = Orientation.Vertical;
            上下分割ToolStripMenuItem.Checked = false;
            左右分割ToolStripMenuItem.Checked = true;
        }

        private void 逆时针旋转90ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pics.Count; i++)
            {
                if (con == pics[i])
                {
                    PictureBox pic = (PictureBox)con;
                    pic.Image = PictureCore.PictureHandle.Rotate(pic.Image, 90);
                    int aaaa = pic.Width;
                    pic.Width = pic.Height;
                    pic.Height = aaaa;
                    pics[i] = pic;
                }
            }
            RefreshList();
            RefreshPics();
            GC.Collect();
        }

        private void 顺时针旋转90ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < pics.Count; i++)
            {
                if (con == pics[i])
                {
                    PictureBox pic = (PictureBox)con;
                    pic.Image = PictureCore.PictureHandle.Rotate(pic.Image, 270);
                    int aaaa = pic.Width;
                    pic.Width = pic.Height;
                    pic.Height = aaaa;
                    pics[i] = pic;
                }
            }
            RefreshList();
            RefreshPics();
            GC.Collect();
        }
        private void 保存ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Text != "Picture_splice")
            {
                StreamWriter sw = new StreamWriter(Text.Replace(" - Picture_splice", ""));
                foreach (PictureBox pi in pics)
                {
                    sw.WriteLine(((Tags)pi.Tag).path);
                    sw.WriteLine(pi.Top.ToString() + " " + pi.Left.ToString());
                    sw.WriteLine(pi.Width.ToString() + " " + pi.Height);
                }
                sw.Close();
            }
            else
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "*.psplice|*.psplice";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    Text = sfd.FileName + " - Picture_splice";
                    StreamWriter sw = new StreamWriter(sfd.FileName);
                    foreach (PictureBox pi in pics)
                    {
                        sw.WriteLine(((Tags)pi.Tag).path);
                        sw.WriteLine(pi.Top.ToString() + " " + pi.Left.ToString());
                        sw.WriteLine(pi.Width.ToString() + " " + pi.Height);
                    }
                    sw.Close();
                }
                sfd.Dispose();
            }
            GC.Collect();
        }
    }
}
