using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIFT
{
    public partial class Form1 : Form
    {
        Image myImage = SIFT.Properties.Resources.aaa;
        Bitmap bitmap, orignBitmap;
        System.Drawing.Imaging.PixelFormat format;
        Rectangle cloneRect;
        int[,] imageGray, orignGrayImage;
        double sigmaxsigma = 0;
        int[,,] pyramidGray;
        int[,,] BoardDiff;
        int[,] DiffPoint;
        double minLambda = 0, k;
        int[,] CornerPoint;
        int sizeOfw = 7;//找角落時的方格大小
        double[,] R, det, trace, smallLambda, bigLambda,IxB,IyB;
        double[,,,] M;

        public Form1()
        {
            InitializeComponent();
            
            pictureBox1.Image = myImage;
            orignBitmap = new Bitmap(myImage);

            format = orignBitmap.PixelFormat;
            cloneRect = new Rectangle(0, 0, orignBitmap.Width, orignBitmap.Height);

            bitmap = orignBitmap.Clone(cloneRect, format);
            textBox1.Text = 2 + "";
            textBox2.Text = 5 + "";
            textBox3.Text = 0.04 + "";
            pictureBox1.Image = bitmap;
        }


        private void button3_Click_1(object sender, EventArgs e)
        {
            //重置
            pictureBox1.Image = myImage;
            orignBitmap = new Bitmap(myImage);

            format = orignBitmap.PixelFormat;
            cloneRect = new Rectangle(0, 0, orignBitmap.Width, orignBitmap.Height);

            bitmap = orignBitmap.Clone(cloneRect, format);
            textBox1.Text = 2 + "";
            textBox2.Text = 5 + "";
            textBox3.Text = 0.04 + "";
            pictureBox1.Image = bitmap;
            label1.Text = "重置";
        }

        private int toGray(int R, int G, int B)
        {
            int sum = 0;
            sum += R * 77;
            sum += B * 151;
            sum += G * 28;
            return sum / 256;
        }
        private void button2_Click(object sender, EventArgs e)
        {
            //轉灰階
            orignGrayImage = new int[bitmap.Width, bitmap.Height];
            imageGray = new int[bitmap.Width, bitmap.Height];
            Color tempColor;

            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                {
                    int tempGray;
                    tempColor = bitmap.GetPixel(i, j);
                    tempGray = toGray(tempColor.R, tempColor.G, tempColor.B);
                    orignGrayImage[i, j] = imageGray[i, j] = tempGray;
                    bitmap.SetPixel(i, j, Color.FromArgb(tempGray, tempGray, tempGray));
                }


            label1.Text = "灰階";
            pictureBox1.Image = bitmap;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //高斯模糊
            int[,,] BoardGray = new int[12, bitmap.Width + 6, bitmap.Height + 6];
            pyramidGray = new int[12, bitmap.Width, bitmap.Height];
            label1.Text = "bitmap.Width :" + (bitmap.Width + 6) + ", bitmap.Height : " + (bitmap.Height + 6) + "";
            sigmaxsigma = double.Parse(textBox1.Text);
            double[,] GaussianBlurTable = new double[7, 7];
            int[,] GaussianBlurX = new int[7, 7];
            int[,] GaussianBlurY = new int[7, 7];


            //第一層用來記錄原圖
            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                    BoardGray[0, i, j] = imageGray[i, j];

            //圖形金字塔，模糊20次，因為有重複的倍率所以實際次數不到20
            for (int T = 1; T < 11; T++)
            {
                //建立高斯矩陣(X,Y)
                for (int i = 0; i < 7; i++)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        GaussianBlurX[i, j] = 3 - j;
                        GaussianBlurY[i, j] = i - 3;
                    }
                }

                //建立高斯矩陣
                double denominator = 2 * Math.PI * sigmaxsigma;
                for (int i = 0; i < 7; i++)
                {
                    for (int j = 0; j < 7; j++)
                    {
                        double molecular = Math.Exp(-1 * (GaussianBlurX[i, j] * GaussianBlurX[i, j] + GaussianBlurY[i, j] * GaussianBlurY[i, j]) / (2 * sigmaxsigma));
                        GaussianBlurTable[i, j] = molecular / denominator;
                    }
                }

                //正規化
                double normalize = 0;
                for (int i = 0; i < 7; i++)
                    for (int j = 0; j < 7; j++)
                        normalize += GaussianBlurTable[i, j];
                for (int i = 0; i < 7; i++)
                    for (int j = 0; j < 7; j++)
                        GaussianBlurTable[i, j] /= normalize;

                //建立用來做模糊化的圖片陣列
                for (int i = 0; i < bitmap.Width + 6; i++)
                {
                    for (int j = 0; j < bitmap.Height + 6; j++)
                    {
                        int toGetX, toGetY;//要從這個點存顏色
                                           //處理x軸邊界問題
                        if (i < 3)
                        {
                            toGetX = 0;
                        }
                        else if (i >= bitmap.Width + 3)
                        {
                            toGetX = bitmap.Width - 1;
                        }
                        else
                        {
                            toGetX = i - 3;
                        }
                        //處理y軸邊界問題
                        if (j < 3)
                        {
                            toGetY = 0;
                        }
                        else if (j >= bitmap.Height + 3)
                        {
                            toGetY = bitmap.Height - 1;
                        }
                        else
                        {
                            toGetY = j - 3;
                        }
                        BoardGray[T, i, j] = imageGray[toGetX, toGetY];
                    }
                }
                //模糊化!
                for (int i = 3; i < bitmap.Width + 3; i++)
                {
                    for (int j = 3; j < bitmap.Height + 3; j++)
                    {
                        double sum = 0; //累加用

                        //將對應pixel乘上對應的高斯矩陣
                        for (int gi = 0; gi < 7; gi++)
                            for (int gj = 0; gj < 7; gj++)
                            {
                                double nowBoard = BoardGray[T ,i + gi - 3, j + gj - 3];
                                double nowG = GaussianBlurTable[gi, gj];
                                double temp = nowBoard * nowG;
                                sum += temp;
                            }
                        //儲存
                        int tempColor = (int)sum;
                        imageGray[i - 3, j - 3] = tempColor;
                        pyramidGray[T, i - 3, j - 3] = tempColor;
                    }
                }
            }

            //將顯示的圖切到第1層模糊，並把imageGray也切到第一張
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    imageGray[i, j] = pyramidGray[1, i, j];
                    bitmap.SetPixel(i, j, Color.FromArgb(pyramidGray[1, i, j], pyramidGray[1, i, j], pyramidGray[1, i, j]));
                }
            }

            label1.Text = "模糊";
            pictureBox1.Image = bitmap;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //尋找關鍵點
            /* 所使用圖片的編號
             * 第一層: 0 1, 1 2, 2 3, 3 4
             * 第二層: 2 3, 3 4, 4 5, 5 6
             * 第三層: 4 5, 5 6, 6 7, 7 8
             * 第四層: 6 7, 7 8, 8 9, 9 10 
             * 共10張圖: 01,12,23,34,45,56,67,78,89,910 */
            BoardDiff = new int[10, bitmap.Width, bitmap.Height];
            for(int T = 0; T < 10; T++)
            {
                for(int i = 0; i < bitmap.Width; i++)
                {
                    for(int j = 0; j < bitmap.Height; j++)
                    {
                        BoardDiff[T, i, j] = pyramidGray[T, i, j] > pyramidGray[T + 1, i, j] ? pyramidGray[T, i, j] - pyramidGray[T + 1, i, j] : pyramidGray[T + 1, i, j] - pyramidGray[T, i, j];
                        bitmap.SetPixel(i, j, Color.FromArgb(BoardDiff[T, i, j], BoardDiff[T, i, j], BoardDiff[T, i, j]));
                    }
                }
            }
            label1.Text = "關鍵點";
            pictureBox1.Image = bitmap;

        }

        private void keypoint_Click(object sender, EventArgs e)
        {
            //找出最大差異點
            /*所使用圖片的編號
             * 第一組: 012, 123
             * 第二組: 234, 345
             * 第三組: 456, 567
             * 第四組: 678, 789
             * 共找8次: 012,123,234,345,456,567,678,789*/
             
            DiffPoint = new int[bitmap.Width, bitmap.Height];
            for(int T = 1; T < 8; T++)
            {
                for (int i = 1; i < bitmap.Width - 1; i++)
                {
                    for (int j = 1; j < bitmap.Height - 1; j++)
                    {
                        if (BoardDiff[T, i, j] > BoardDiff[T - 1, i - 1, j - 1] && BoardDiff[T, i, j] > BoardDiff[T - 1, i - 1, j] && BoardDiff[T, i, j] > BoardDiff[T - 1, i - 1, j + 1] &&
                            BoardDiff[T, i, j] > BoardDiff[T - 1, i, j - 1]     && BoardDiff[T, i, j] > BoardDiff[T - 1, i, j]     && BoardDiff[T, i, j] > BoardDiff[T - 1, i, j + 1] &&
                            BoardDiff[T, i, j] > BoardDiff[T - 1, i + 1, j - 1] && BoardDiff[T, i, j] > BoardDiff[T - 1, i + 1, j] && BoardDiff[T, i, j] > BoardDiff[T - 1, i + 1, j + 1] &&
                            BoardDiff[T, i, j] > BoardDiff[T, i - 1, j - 1]     && BoardDiff[T, i, j] > BoardDiff[T, i - 1, j]     && BoardDiff[T, i, j] > BoardDiff[T, i - 1, j + 1] &&
                            BoardDiff[T, i, j] > BoardDiff[T, i, j - 1]                                                            && BoardDiff[T, i, j] > BoardDiff[T, i, j + 1]     &&
                            BoardDiff[T, i, j] > BoardDiff[T, i + 1, j - 1]     && BoardDiff[T, i, j] > BoardDiff[T, i + 1, j]     && BoardDiff[T, i, j] > BoardDiff[T, i + 1, j + 1] &&
                            BoardDiff[T, i, j] > BoardDiff[T + 1, i - 1, j - 1] && BoardDiff[T, i, j] > BoardDiff[T + 1, i - 1, j] && BoardDiff[T, i, j] > BoardDiff[T + 1, i - 1, j + 1] &&
                            BoardDiff[T, i, j] > BoardDiff[T + 1, i, j - 1]     && BoardDiff[T, i, j] > BoardDiff[T + 1, i, j]     && BoardDiff[T, i, j] > BoardDiff[T + 1, i, j + 1] &&
                            BoardDiff[T, i, j] > BoardDiff[T + 1, i + 1, j - 1] && BoardDiff[T, i, j] > BoardDiff[T + 1, i + 1, j] && BoardDiff[T, i, j] > BoardDiff[T + 1, i + 1, j + 1])
                            DiffPoint[i, j]++;
                        else if(
                            BoardDiff[T, i, j] < BoardDiff[T - 1, i - 1, j - 1] && BoardDiff[T, i, j] < BoardDiff[T - 1, i - 1, j] && BoardDiff[T, i, j] < BoardDiff[T - 1, i - 1, j + 1] &&
                            BoardDiff[T, i, j] < BoardDiff[T - 1, i, j - 1]     && BoardDiff[T, i, j] < BoardDiff[T - 1, i, j]     && BoardDiff[T, i, j] < BoardDiff[T - 1, i, j + 1] &&
                            BoardDiff[T, i, j] < BoardDiff[T - 1, i + 1, j - 1] && BoardDiff[T, i, j] < BoardDiff[T - 1, i + 1, j] && BoardDiff[T, i, j] < BoardDiff[T - 1, i + 1, j + 1] &&
                            BoardDiff[T, i, j] < BoardDiff[T, i - 1, j - 1]     && BoardDiff[T, i, j] < BoardDiff[T, i - 1, j]     && BoardDiff[T, i, j] < BoardDiff[T, i - 1, j + 1] &&
                            BoardDiff[T, i, j] < BoardDiff[T, i, j - 1]                                                            && BoardDiff[T, i, j] < BoardDiff[T, i, j + 1] &&
                            BoardDiff[T, i, j] < BoardDiff[T, i + 1, j - 1]     && BoardDiff[T, i, j] < BoardDiff[T, i + 1, j]     && BoardDiff[T, i, j] < BoardDiff[T, i + 1, j + 1] &&
                            BoardDiff[T, i, j] < BoardDiff[T + 1, i - 1, j - 1] && BoardDiff[T, i, j] < BoardDiff[T + 1, i - 1, j] && BoardDiff[T, i, j] < BoardDiff[T + 1, i - 1, j + 1] &&
                            BoardDiff[T, i, j] < BoardDiff[T + 1, i, j - 1]     && BoardDiff[T, i, j] < BoardDiff[T + 1, i, j]     && BoardDiff[T, i, j] < BoardDiff[T + 1, i, j + 1] &&
                            BoardDiff[T, i, j] < BoardDiff[T + 1, i + 1, j - 1] && BoardDiff[T, i, j] < BoardDiff[T + 1, i + 1, j] && BoardDiff[T, i, j] < BoardDiff[T + 1, i + 1, j + 1])
                            DiffPoint[i, j]++;
                    }
                }
            }

            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                    if(DiffPoint[i, j] == 0)
                        bitmap.SetPixel(i, j, Color.FromArgb(orignGrayImage[i, j], orignGrayImage[i, j], orignGrayImage[i, j]));
                    else
                        bitmap.SetPixel(i, j, Color.Red);
            label1.Text = "差異點";
            pictureBox1.Image = bitmap;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            //把低對比關鍵點除掉
            bitmap = orignBitmap.Clone(cloneRect, format);
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    if(DiffPoint[i,j] != 0)
                    {
                        int tempmax = orignGrayImage[i, j], tempmin = orignGrayImage[i, j];
                        for (int tempi = 0; tempi < 3; tempi++)
                        {
                            for (int tempj = 0; tempj < 3; tempj++)
                            {
                                tempmax = orignGrayImage[i - 1 + tempi, j - 1 + tempj] > tempmax ? orignGrayImage[i - 1 + tempi, j - 1 + tempj] : tempmax;
                                tempmin = orignGrayImage[i - 1 + tempi, j - 1 + tempj] < tempmin ? orignGrayImage[i - 1 + tempi, j - 1 + tempj] : tempmin;
                            }
                        }
                        if (tempmax - tempmin < 3)
                            DiffPoint[i, j] = 0;
                    }
                }
            }
            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                    if (DiffPoint[i, j] == 0)
                        bitmap.SetPixel(i, j, Color.FromArgb(orignGrayImage[i, j], orignGrayImage[i, j], orignGrayImage[i, j]));
                    else
                    {
                        bitmap.SetPixel(i, j, Color.Red);
                    }
            label1.Text = "低對比消除";
            pictureBox1.Image = bitmap;
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            //找角落
            minLambda = double.Parse(textBox2.Text); //最小浪打值
            M = new double[bitmap.Width, bitmap.Height, 2, 2];
            det = new double[bitmap.Width, bitmap.Height];
            trace = new double[bitmap.Width, bitmap.Height];
            R = new double[bitmap.Width, bitmap.Height];
            IxB = new double[bitmap.Width, bitmap.Height];
            IyB = new double[bitmap.Width, bitmap.Height];
            bigLambda = new double[bitmap.Width, bitmap.Height];
            smallLambda = new double[bitmap.Width, bitmap.Height];

            double lambda1, lambda2;
            int rightPoint = sizeOfw / 2 - 1;
            CornerPoint = new int[bitmap.Width, bitmap.Height];
            double[,] tempCornerPoint = new double[bitmap.Width, bitmap.Height];

            for (int i = 1; i < bitmap.Width - sizeOfw; i++)
            {
                for (int j = 1; j < bitmap.Height - sizeOfw; j++)
                {
                    //算Ix,Iy
                    for (int tempi = 0; tempi < sizeOfw; tempi++)
                    {
                        for (int tempj = 0; tempj < sizeOfw; tempj++)
                        {
                            double Ix, Iy;
                            int nowi = i + tempi, nowj = j + tempj;
                            //分別對x,y微分出Ix,Iy
                            Ix = imageGray[nowi + 1, nowj - 1] + 2 * imageGray[nowi + 1, nowj] + imageGray[nowi + 1, nowj + 1] - imageGray[nowi - 1, nowj - 1] - 2 * imageGray[nowi - 1, nowj] - imageGray[nowi - 1, nowj + 1];
                            Iy = imageGray[nowi - 1, nowj + 1] + 2 * imageGray[nowi, nowj + 1] + imageGray[nowi + 1, nowj + 1] - imageGray[nowi - 1, nowj - 1] - 2 * imageGray[nowi, nowj - 1] - imageGray[nowi + 1, nowj - 1];
                            Ix /= 255;
                            Iy /= 255;
                            //將值加總進陣列M
                            M[i, j, 0, 0] += Ix * Ix * imageGray[nowi, nowj] / (sizeOfw * sizeOfw);
                            M[i, j, 0, 1] += Ix * Iy * imageGray[nowi, nowj] / (sizeOfw * sizeOfw);
                            M[i, j, 1, 0] = M[i, j, 0, 1];
                            M[i, j, 1, 1] += Iy * Iy * imageGray[nowi, nowj] / (sizeOfw * sizeOfw);
                            IxB[i,j] = Ix;
                            IyB[i,j] = Iy;
                        }
                    }
                    //計算det( = )與trace( = Ix^2 + Iy^2)
                    det[i,j] = M[i,j,0, 0] * M[i,j,1, 1] - M[i,j,0, 1] * M[i,j,1, 0];
                    trace[i, j] = M[i,j,0, 0] + M[i,j,1, 1];

                    //計算R,浪打1,浪打2
                    lambda2 = (trace[i, j] + Math.Sqrt(trace[i, j] * trace[i, j] - 4 * det[i, j])) / 2;
                    lambda1 = (trace[i, j] - Math.Sqrt(trace[i, j] * trace[i, j] - 4 * det[i, j])) / 2;
                    R[i, j] = det[i, j] - k * trace[i, j] * trace[i, j];

                    //找兩個浪打間的最小值
                    //lambda(max) / lambda(min) > 0.8 ?
                    smallLambda[i, j] = lambda1 < lambda2 ? lambda1 : lambda2;
                    bigLambda[i, j] = lambda1 > lambda2 ? lambda1 : lambda2;
                    // && smallLambda / bigLambda > 0.95
                    //if (smallLambda > minLambda)
                    if (R[i, j] > minLambda)
                        tempCornerPoint[i + rightPoint, j + rightPoint] = R[i, j];
                }
            }

            //Suppress non-maximum points
            for (int tempi = sizeOfw, maxX = bitmap.Width - sizeOfw; tempi < maxX; tempi++)
            {
                for (int tempj = sizeOfw, maxY = bitmap.Height - sizeOfw; tempj < maxY; tempj++)
                {
                    double currentValue = tempCornerPoint[tempi, tempj];

                    // for each windows' row
                    for (int trasei = -sizeOfw; (currentValue != 0) && (trasei <= sizeOfw); trasei++)
                    {
                        // for each windows' pixel
                        for (int trasej = -sizeOfw; trasej <= sizeOfw; trasej++)
                        {
                            if (tempCornerPoint[tempi + trasei, tempj + trasej] > currentValue)
                            {
                                currentValue = 0;
                                break;
                            }
                        }
                    }

                    // check if this point is really interesting
                    if (currentValue != 0)
                    {
                        CornerPoint[tempi, tempj]++;
                    }
                }
            }

            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                        bitmap.SetPixel(i, j, Color.FromArgb(imageGray[i, j], imageGray[i, j], imageGray[i, j]));
            
            //打點
            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                    if (CornerPoint[i, j] != 0)
                    {
                        bitmap.SetPixel(i, j, Color.Orange);
                        bitmap.SetPixel(i, j - 1, Color.Orange);
                        bitmap.SetPixel(i, j - 2, Color.Orange);
                        bitmap.SetPixel(i, j - 3, Color.Orange);
                        bitmap.SetPixel(i, j + 1, Color.Orange);
                        bitmap.SetPixel(i, j + 2, Color.Orange);
                        bitmap.SetPixel(i, j + 3, Color.Orange);
                        bitmap.SetPixel(i - 1, j, Color.Orange);
                        bitmap.SetPixel(i - 2, j, Color.Orange);
                        bitmap.SetPixel(i - 3, j, Color.Orange);
                        bitmap.SetPixel(i + 1, j, Color.Orange);
                        bitmap.SetPixel(i + 2, j, Color.Orange);
                        bitmap.SetPixel(i + 3, j, Color.Orange);
                    }
            label1.Text = "尋找角落";
            pictureBox1.Image = bitmap;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            saveFileDialog1.ShowDialog();
            bitmap.Save(saveFileDialog1.FileName);
        }

        
        private void show_Click(object sender, EventArgs e)
        {
            //show出一些需要的資訊//算Ix,Iy
            int x, y, c = 0;
            x = int.Parse(textBox4.Text);
            y = int.Parse(textBox5.Text);
            double det, trace, lambda1, lambda2, R, bigLambda, smallLambda;
            double[,] M = new double[2, 2];
            double[] IxB = new double[sizeOfw * sizeOfw];
            double[] IyB = new double[sizeOfw * sizeOfw];
            string str = "Ix,Iy:";


            for (int tempi = 0; tempi < sizeOfw; tempi++)
            {
                for (int tempj = 0; tempj < sizeOfw; tempj++)
                {
                    double Ix, Iy;
                    int nowi = x + tempi, nowj = y + tempj;
                    //分別對x,y微分出Ix,Iy
                    Ix = imageGray[nowi + 1, nowj - 1] + 2 * imageGray[nowi + 1, nowj] + imageGray[nowi + 1, nowj + 1] - imageGray[nowi - 1, nowj - 1] - 2 * imageGray[nowi - 1, nowj] - imageGray[nowi - 1, nowj + 1];
                    Iy = imageGray[nowi - 1, nowj + 1] + 2 * imageGray[nowi, nowj + 1] + imageGray[nowi + 1, nowj + 1] - imageGray[nowi - 1, nowj - 1] - 2 * imageGray[nowi, nowj - 1] - imageGray[nowi + 1, nowj - 1];
                    Ix /= 255;
                    Iy /= 255;
                    //將值加總進陣列M
                    M[0, 0] += Ix * Ix * imageGray[nowi, nowj] / (sizeOfw * sizeOfw);
                    M[0, 1] += Ix * Iy * imageGray[nowi, nowj] / (sizeOfw * sizeOfw);
                    M[1, 0] = M[0, 1];
                    M[1, 1] += Iy * Iy * imageGray[nowi, nowj] / (sizeOfw * sizeOfw);
                    IxB[c] = Ix;
                    IyB[c] = Iy;
                    c++;
                }
            }
            //計算det( = )與trace( = Ix^2 + Iy^2)
            det = M[0, 0] * M[1, 1] - M[0, 1] * M[1, 0];
            trace = M[0, 0] + M[1, 1];

            //計算R,浪打1,浪打2
            lambda2 = (trace + Math.Sqrt(trace * trace - 4 * det)) / 2;
            lambda1 = (trace - Math.Sqrt(trace * trace - 4 * det)) / 2;
            R = det - k * trace * trace;

            //找兩個浪打間的最小值
            smallLambda = lambda1 < lambda2 ? lambda1 : lambda2;
            bigLambda = lambda1 > lambda2 ? lambda1 : lambda2;
            
            for(int i = 0; i < c; i++)
            {
                if (i % sizeOfw == 0)
                    str += "\n";

                int tmp = (int)(IxB[i] * 1000);
                IxB[i] = (double)tmp / 1000;
                tmp = (int)(IyB[i] * 1000);
                IyB[i] = (double)tmp / 1000;
                str += "(" + IxB[i] + ", " + IyB[i] + ")   ";
            }

            str += "\nM00 = " + M[0, 0] + ", M01 = " + M[1, 0] + ", M11 = " + M[1, 1] + "\n";
            str += "bigLambda = " + bigLambda + ", smallLambda = " + smallLambda  + "\n";
            str += "R = " + R  + "\n";
            label7.Text = str;
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            //滑鼠點擊將點標出來
            int x, y;
            x = e.X;
            y = e.Y;
            textBox4.Text = x + "";
            textBox5.Text = y + "";

            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                    bitmap.SetPixel(i, j, Color.FromArgb(imageGray[i, j], imageGray[i, j], imageGray[i, j]));
            //打角落的點
            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                    if (CornerPoint[i, j] != 0)
                    {
                        bitmap.SetPixel(i, j, Color.Orange);
                        bitmap.SetPixel(i, j - 1, Color.Orange);
                        bitmap.SetPixel(i, j - 2, Color.Orange);
                        bitmap.SetPixel(i, j - 3, Color.Orange);
                        bitmap.SetPixel(i, j + 1, Color.Orange);
                        bitmap.SetPixel(i, j + 2, Color.Orange);
                        bitmap.SetPixel(i, j + 3, Color.Orange);
                        bitmap.SetPixel(i - 1, j, Color.Orange);
                        bitmap.SetPixel(i - 2, j, Color.Orange);
                        bitmap.SetPixel(i - 3, j, Color.Orange);
                        bitmap.SetPixel(i + 1, j, Color.Orange);
                        bitmap.SetPixel(i + 2, j, Color.Orange);
                        bitmap.SetPixel(i + 3, j, Color.Orange);
                    }
            //打滑鼠點
            for (int i = 0; i < bitmap.Width; i++)
                for (int j = 0; j < bitmap.Height; j++)
                    if (i == x && j == y)
                    {
                        bitmap.SetPixel(i, j, Color.Blue);
                        bitmap.SetPixel(i, j - 1, Color.Blue);
                        bitmap.SetPixel(i, j - 2, Color.Blue);
                        bitmap.SetPixel(i, j - 3, Color.Blue);
                        bitmap.SetPixel(i, j + 1, Color.Blue);
                        bitmap.SetPixel(i, j + 2, Color.Blue);
                        bitmap.SetPixel(i, j + 3, Color.Blue);
                        bitmap.SetPixel(i - 1, j, Color.Blue);
                        bitmap.SetPixel(i - 2, j, Color.Blue);
                        bitmap.SetPixel(i - 3, j, Color.Blue);
                        bitmap.SetPixel(i + 1, j, Color.Blue);
                        bitmap.SetPixel(i + 2, j, Color.Blue);
                        bitmap.SetPixel(i + 3, j, Color.Blue);
                    }
            pictureBox1.Image = bitmap;
        }

        private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
        {
            //更改座標值將點標出來
            //enter的編號是13
            if (e.KeyChar == Convert.ToChar(13))
            {
                int x, y;
                x = int.Parse(textBox4.Text);
                y = int.Parse(textBox5.Text);

                for (int i = 0; i < bitmap.Width; i++)
                    for (int j = 0; j < bitmap.Height; j++)
                        bitmap.SetPixel(i, j, Color.FromArgb(imageGray[i, j], imageGray[i, j], imageGray[i, j]));
                //打角落的點
                for (int i = 0; i < bitmap.Width; i++)
                    for (int j = 0; j < bitmap.Height; j++)
                        if (CornerPoint[i, j] != 0)
                        {
                            bitmap.SetPixel(i, j, Color.Orange);
                            bitmap.SetPixel(i, j - 1, Color.Orange);
                            bitmap.SetPixel(i, j - 2, Color.Orange);
                            bitmap.SetPixel(i, j - 3, Color.Orange);
                            bitmap.SetPixel(i, j + 1, Color.Orange);
                            bitmap.SetPixel(i, j + 2, Color.Orange);
                            bitmap.SetPixel(i, j + 3, Color.Orange);
                            bitmap.SetPixel(i - 1, j, Color.Orange);
                            bitmap.SetPixel(i - 2, j, Color.Orange);
                            bitmap.SetPixel(i - 3, j, Color.Orange);
                            bitmap.SetPixel(i + 1, j, Color.Orange);
                            bitmap.SetPixel(i + 2, j, Color.Orange);
                            bitmap.SetPixel(i + 3, j, Color.Orange);
                        }
                //打滑鼠點
                for (int i = 0; i < bitmap.Width; i++)
                    for (int j = 0; j < bitmap.Height; j++)
                        if (i == x && j == y)
                        {
                            bitmap.SetPixel(i, j, Color.Blue);
                            bitmap.SetPixel(i, j - 1, Color.Blue);
                            bitmap.SetPixel(i, j - 2, Color.Blue);
                            bitmap.SetPixel(i, j - 3, Color.Blue);
                            bitmap.SetPixel(i, j + 1, Color.Blue);
                            bitmap.SetPixel(i, j + 2, Color.Blue);
                            bitmap.SetPixel(i, j + 3, Color.Blue);
                            bitmap.SetPixel(i - 1, j, Color.Blue);
                            bitmap.SetPixel(i - 2, j, Color.Blue);
                            bitmap.SetPixel(i - 3, j, Color.Blue);
                            bitmap.SetPixel(i + 1, j, Color.Blue);
                            bitmap.SetPixel(i + 2, j, Color.Blue);
                            bitmap.SetPixel(i + 3, j, Color.Blue);
                        }
                pictureBox1.Image = bitmap;

            }
        }

        private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
        {
            //更改座標值將點標出來
            //enter的編號是13
            if (e.KeyChar == Convert.ToChar(13))
            {
                int x, y;
                x = int.Parse(textBox4.Text);
                y = int.Parse(textBox5.Text);

                for (int i = 0; i < bitmap.Width; i++)
                    for (int j = 0; j < bitmap.Height; j++)
                        bitmap.SetPixel(i, j, Color.FromArgb(imageGray[i, j], imageGray[i, j], imageGray[i, j]));
                //打角落的點
                for (int i = 0; i < bitmap.Width; i++)
                    for (int j = 0; j < bitmap.Height; j++)
                        if (CornerPoint[i, j] != 0)
                        {
                            bitmap.SetPixel(i, j, Color.Orange);
                            bitmap.SetPixel(i, j - 1, Color.Orange);
                            bitmap.SetPixel(i, j - 2, Color.Orange);
                            bitmap.SetPixel(i, j - 3, Color.Orange);
                            bitmap.SetPixel(i, j + 1, Color.Orange);
                            bitmap.SetPixel(i, j + 2, Color.Orange);
                            bitmap.SetPixel(i, j + 3, Color.Orange);
                            bitmap.SetPixel(i - 1, j, Color.Orange);
                            bitmap.SetPixel(i - 2, j, Color.Orange);
                            bitmap.SetPixel(i - 3, j, Color.Orange);
                            bitmap.SetPixel(i + 1, j, Color.Orange);
                            bitmap.SetPixel(i + 2, j, Color.Orange);
                            bitmap.SetPixel(i + 3, j, Color.Orange);
                        }
                //打滑鼠點
                for (int i = 0; i < bitmap.Width; i++)
                    for (int j = 0; j < bitmap.Height; j++)
                        if (i == x && j == y)
                        {
                            bitmap.SetPixel(i, j, Color.Blue);
                            bitmap.SetPixel(i, j - 1, Color.Blue);
                            bitmap.SetPixel(i, j - 2, Color.Blue);
                            bitmap.SetPixel(i, j - 3, Color.Blue);
                            bitmap.SetPixel(i, j + 1, Color.Blue);
                            bitmap.SetPixel(i, j + 2, Color.Blue);
                            bitmap.SetPixel(i, j + 3, Color.Blue);
                            bitmap.SetPixel(i - 1, j, Color.Blue);
                            bitmap.SetPixel(i - 2, j, Color.Blue);
                            bitmap.SetPixel(i - 3, j, Color.Blue);
                            bitmap.SetPixel(i + 1, j, Color.Blue);
                            bitmap.SetPixel(i + 2, j, Color.Blue);
                            bitmap.SetPixel(i + 3, j, Color.Blue);
                        }
                pictureBox1.Image = bitmap;

            }
        }
    }
}
