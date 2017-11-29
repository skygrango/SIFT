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
        Image myImage = SIFT.Properties.Resources.f09ef9a0_34d9_4a2e_b9f9_af2205705039_jpg_w300x300;
        Image grayImage;
        Bitmap bitmap, orignBitmap;
        System.Drawing.Imaging.PixelFormat format;
        Rectangle cloneRect;
        int[,] imageGray, orignGrayImage;
        double sigmaxsigma = 0;
        int[,,] pyramidGray;
        int[,,] BoardDiff;
        int[,] DiffPoint;
        double minLambda = 0;
        int[,] CornerPoint;
        int sizeOfw = 7, inteInfoCounter = 0;//找角落時的方格大小
        bool grayFinish = false;
        int[,] BoardKeypoint;
        InterestedInformations[] inteInfo;//紀錄分別的關鍵點的資訊用的結構

        class InterestedInformations
        {
            public bool isNotEmpty;
            public int x, y, sita;
            public double gradientMagnitudes, R, bigLambda, smallLambda;
            public double[] fingerprint;
            public InterestedInformations()
            {
                isNotEmpty = false;
                x = 0;
                y = 0;
                R = 0;
                bigLambda = 0;
                smallLambda = 0;
                sita = 0;
                gradientMagnitudes = 0;
                fingerprint = new double[128];
            }

            public void normalize(int times)
            {
                //正規化fingerprints
                double sum = 0;
                bool hadrevised = false;
                for (int i = 0; i < 128; i++)
                    sum += fingerprint[i];
                if (sum == 0)//避免全都是0
                    return;
                for (int i = 0; i < 128; i++)
                    fingerprint[i] /= sum;
                //大於0.2者改成0.2為了尺度不變性
                for (int i = 0; i < 128; i++)
                {
                    if (fingerprint[i] > 0.2)
                    { 
                        fingerprint[i] = 0.2;
                        hadrevised = true;
                    }
                }
                //如果有被修正則重新呼叫自己一次，避免無窮迴圈
                if (hadrevised && times < 5)
                    normalize(times + 1);
            }
        }

        public Form1()
        {
            InitializeComponent();
            
            pictureBox1.Image = myImage;
            orignBitmap = new Bitmap(myImage);

            format = orignBitmap.PixelFormat;
            cloneRect = new Rectangle(0, 0, orignBitmap.Width, orignBitmap.Height);

            bitmap = orignBitmap.Clone(cloneRect, format);
            textBox1.Text = 2 + "";
            textBox2.Text = 0.4 + "";
            textBox3.Text = 20 + "";
            textBox4.Text = 0 + "";
            textBox5.Text = 0 + "";

            inteInfoCounter = 0;
            inteInfo = new InterestedInformations[1];
            inteInfo[0] = new InterestedInformations();

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
            textBox2.Text = 0.4 + "";
            textBox3.Text = 20 + "";
            textBox4.Text = 0 + "";
            textBox5.Text = 0 + "";
            pictureBox1.Image = bitmap;
            grayFinish = false;
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
            BoardKeypoint = new int[bitmap.Width, bitmap.Height];
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

            orignBitmap = new Bitmap(myImage);

            format = orignBitmap.PixelFormat;
            cloneRect = new Rectangle(0, 0, orignBitmap.Width, orignBitmap.Height);
            grayImage = bitmap.Clone(cloneRect, format);

            label1.Text = "灰階";
            grayFinish = true;
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
            for (int T = 1; T < 8; T++)
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
                        if(DiffPoint[i,j] > 0)
                            bitmap.SetPixel(i, j, Color.Red);
                        else
                            bitmap.SetPixel(i, j, Color.FromArgb(imageGray[i, j], imageGray[i, j], imageGray[i, j]));
                    }
                }
            }

            pictureBox1.Image = bitmap;
            label1.Text = "差異點";
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
            for(int i = 0; i < bitmap.Width; i++)
                for(int j = 0; j < bitmap.Height; j++)
                    if (DiffPoint[i, j] > 0 && BoardKeypoint[i, j] == 0)
                    {
                        if (BoardKeypoint[i, j] == 0)
                        {
                            inteInfo[inteInfoCounter].isNotEmpty = true;
                            inteInfo[inteInfoCounter].x = i;
                            inteInfo[inteInfoCounter].y = j;
                            BoardKeypoint[i, j] = 1;

                            Array.Resize(ref inteInfo, inteInfo.Length + 1);
                            inteInfoCounter++;
                            inteInfo[inteInfoCounter] = new InterestedInformations();
                        }
                    }

            refreshScreen(0, 0);
            label1.Text = "低對比消除";
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            //找角落
            double R, ratio, minR, k = 0.04;
            double det, trace, smallLambda, bigLambda, lambda1, lambda2;
            int rightPoint = sizeOfw / 2;
            CornerPoint = new int[bitmap.Width, bitmap.Height];
            double[,] tempCornerPoint = new double[bitmap.Width, bitmap.Height];

            minLambda = double.Parse(textBox2.Text); //最小浪打值
            ratio = double.Parse(textBox2.Text); //兩個浪打的比值
            minR = double.Parse(textBox3.Text); //R的最小接受值

            for (int i = 1 + rightPoint; i < bitmap.Width - rightPoint - 1; i++)
            {
                for (int j = 1 + rightPoint; j < bitmap.Height - rightPoint - 1; j++)
                {
                    //計算Ix,Iy
                    double[,] M = new double[2, 2];
                    for (int tempi = 0; tempi < sizeOfw; tempi++)
                    {
                        for (int tempj = 0; tempj < sizeOfw; tempj++)
                        {
                            double Ix, Iy;
                            int nowi = i + tempi - rightPoint, nowj = j + tempj - rightPoint;
                            //分別對x,y微分出Ix,Iy
                            Ix = orignGrayImage[nowi + 1, nowj - 1] + 2 * orignGrayImage[nowi + 1, nowj] + orignGrayImage[nowi + 1, nowj + 1] - orignGrayImage[nowi - 1, nowj - 1] - 2 * orignGrayImage[nowi - 1, nowj] - orignGrayImage[nowi - 1, nowj + 1];
                            Iy = orignGrayImage[nowi - 1, nowj + 1] + 2 * orignGrayImage[nowi, nowj + 1] + orignGrayImage[nowi + 1, nowj + 1] - orignGrayImage[nowi - 1, nowj - 1] - 2 * orignGrayImage[nowi, nowj - 1] - orignGrayImage[nowi + 1, nowj - 1];
                            Ix /= 255;
                            Iy /= 255;
                            //將值加總進陣列M
                            M[0, 0] += Ix * Ix * orignGrayImage[nowi, nowj] / (sizeOfw * sizeOfw);
                            M[0, 1] += Ix * Iy * orignGrayImage[nowi, nowj] / (sizeOfw * sizeOfw);
                            M[1, 0] = M[0, 1];
                            M[1, 1] += Iy * Iy * orignGrayImage[nowi, nowj] / (sizeOfw * sizeOfw);
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
                    //lambda(max) / lambda(min) > 0.8 ?
                    smallLambda = lambda1 < lambda2 ? lambda1 : lambda2;
                    bigLambda = lambda1 > lambda2 ? lambda1 : lambda2;
                    // && smallLambda / bigLambda > 0.95
                    //if (smallLambda > minLambda)
                    if (R > minR)
                        tempCornerPoint[i, j] = R;
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
                        if (BoardKeypoint[tempi, tempj] == 0)
                        {
                            inteInfo[inteInfoCounter].isNotEmpty = true;
                            inteInfo[inteInfoCounter].x = tempi;
                            inteInfo[inteInfoCounter].y = tempj;
                            inteInfo[inteInfoCounter].R = tempCornerPoint[tempi, tempj];
                            BoardKeypoint[tempi, tempj] = 1;

                            Array.Resize(ref inteInfo, inteInfo.Length + 1);
                            inteInfoCounter++;
                            inteInfo[inteInfoCounter] = new InterestedInformations();
                        }
                    }
                }
            }
            
            refreshScreen(0, 0);

            label1.Text = "尋找角落, " + inteInfoCounter;
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            //畫出關鍵點走勢
            double sita = 0;
            double[,] gm;

            for(int c = 0; c < inteInfo.Length - 1; c++)
            {
                int x = inteInfo[c].x;
                int y = inteInfo[c].y;
                gm = new double[36,2];
                double maxGM = 0;
                int maxSita = 0, maxGMCounter = 0;
                for (int i = -8; i <= 8; i++)
                {
                    int nowX = x + i;
                    if (nowX > 1 && nowX < bitmap.Width - 1)
                        for (int j = -8; j <= 8; j++)
                        {
                            int nowY = y + j;
                            if (nowY > 1 && nowY < bitmap.Height - 1)
                            {
                                double Lx1y = imageGray[nowX + 1, nowY], Lx2y = imageGray[nowX - 1, nowY], Lxy1 = imageGray[nowX, nowY + 1], Lxy2 = imageGray[nowX, nowY - 1];

                                sita = Math.Atan2(Lxy1 - Lxy2, Lx1y - Lx2y);
                                sita /= Math.PI;
                                sita *= 180;
                                sita += 180;

                                int index = (int)sita / 10;
                                if (index == 36)
                                    index = 0;
                                gm[index, 0] += Math.Sqrt((Lx1y - Lx2y) * (Lx1y - Lx2y) + (Lxy1 - Lxy2) * (Lxy1 - Lxy2));
                                gm[index, 1]++;
                            }
                        }
                }
                for(int i = 0; i < 36; i++)
                {
                    if(maxGM < gm[i,0])
                    {
                        maxGM = gm[i,0];
                        maxGMCounter = (int)gm[i, 1];
                        maxSita = i * 10 - 180;
                    }
                }
                inteInfo[c].gradientMagnitudes = maxGM / maxGMCounter;
                inteInfo[c].sita = maxSita;
            }
            /*
            bitmap = new Bitmap(bitmap.Width, bitmap.Height, g);
            g.Dispose();
            pictureBox1.Image = bitmap;
            */
            label1.Text = "走勢";
        }

        private double[] fingerprintCalculator(int x, int y, int sita)
        {
            //計算每一個4*4的8個fingerprint
            double[] gm = new double[8];
            double tempsita;
            for (int i = x; i < x + 4; i++)
            {
                if (i > 1 && i < bitmap.Width - 1)
                    for (int j = y; j < y + 4; j++)
                    {
                        if (j > 1 && j < bitmap.Height - 1)
                        {
                            double Lx1y = imageGray[i + 1, j], Lx2y = imageGray[i - 1, j], Lxy1 = imageGray[i, j + 1], Lxy2 = imageGray[i, j - 1];

                            tempsita = Math.Atan2(Lxy1 - Lxy2, Lx1y - Lx2y);
                            tempsita /= Math.PI;
                            tempsita *= 180;
                            tempsita += 180;
                            tempsita -= sita; //角度不變性
                            //分別修正結果<0與>0的tempsita
                            while (tempsita < 0)
                                tempsita += 360;
                            while (tempsita > 360)
                                tempsita -= 360;

                            int index = (int)tempsita / 45;
                            if (index == 8)
                                index = 0;
                            gm[index] += Math.Sqrt((Lx1y - Lx2y) * (Lx1y - Lx2y) + (Lxy1 - Lxy2) * (Lxy1 - Lxy2));
                        }

                    }
            }
            refreshScreen(0, 0);
            return gm;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //製作專屬指紋
            for (int c = 0; c < inteInfo.Length - 1; c++)
            {
                int x = inteInfo[c].x, y = inteInfo[c].y;
                int sita = inteInfo[c].sita;
                int fingerCount = 0;
                //拆成16個運算分別儲存
                for(int j = y - 8; j <= y + 4; j += 4)
                {
                    for (int i = x - 8; i <= x + 4; i += 4)
                    {
                        double[] temp = fingerprintCalculator(i, j, sita);
                        inteInfo[c].fingerprint[fingerCount] = temp[0];
                        inteInfo[c].fingerprint[fingerCount + 1] = temp[1];
                        inteInfo[c].fingerprint[fingerCount + 2] = temp[2];
                        inteInfo[c].fingerprint[fingerCount + 3] = temp[3];
                        inteInfo[c].fingerprint[fingerCount + 4] = temp[4];
                        inteInfo[c].fingerprint[fingerCount + 5] = temp[5];
                        inteInfo[c].fingerprint[fingerCount + 6] = temp[6];
                        inteInfo[c].fingerprint[fingerCount + 7] = temp[7];
                        fingerCount += 8;
                    }
                }
                inteInfo[c].normalize(0);
            }
            label1.Text = "指紋";
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
            int x, y, i;
            bool hasInfo = false;
            x = int.Parse(textBox4.Text);
            y = int.Parse(textBox5.Text);
            string str = "";
            for (i = 0; i < inteInfo.Length; i++)
            {
                if (inteInfo[i].x == x)
                {
                    if (inteInfo[i].y == y)
                    {
                        hasInfo = true;
                        break;
                    }
                }
            }
            if (!hasInfo)
                str = "No interested data\n";
            else
            {
                str += "R = " + inteInfo[i].R + "\n";
              /*  str += "bigLambda = " + inteInfo[i].bigLambda + "\n";
                str += "smallLambda = " + inteInfo[i].smallLambda + "\n";
                str += "smallLambda / bigLambda = " + inteInfo[i].smallLambda / inteInfo[i].bigLambda + "\n";*/
                str += "sita = " + inteInfo[i].sita + "\n";
                str += "gradient magnitudes = " + inteInfo[i].gradientMagnitudes + "\n";
                str += "fingerprint = \n{\n";
                for(int j = 0; j < 128; j+=8)
                {
                    str += "(" + inteInfo[i].fingerprint[j].ToString("#0.##0") + ", " + inteInfo[i].fingerprint[j + 1].ToString("#0.##0") + ", " + inteInfo[i].fingerprint[j + 2].ToString("#0.##0") + ", " + inteInfo[i].fingerprint[j + 3].ToString("#0.##0") + ", " + inteInfo[i].fingerprint[j + 4].ToString("#0.##0") + ", " + inteInfo[i].fingerprint[j + 5].ToString("#0.##0") + ", " + inteInfo[i].fingerprint[j + 6].ToString("#0.##0") + ", " + inteInfo[i].fingerprint[j + 7].ToString("#0.##0") + ")\n";
                }
                str += "}\n";
            }
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
            refreshScreen(x, y);  
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
                refreshScreen(x, y);
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
                refreshScreen(x, y);
            }
        }

        private void refreshScreen(int x, int y)
        {
            //重新更新圖片
            //重新讀圖
            int rightPoint = sizeOfw / 2;
            Pen p;
            Graphics g = Graphics.FromImage(grayImage);
            pictureBox1.Image = grayImage;
            if (grayFinish)
            {
                for (int i = 0; i < bitmap.Width; i++)
                    for (int j = 0; j < bitmap.Height; j++)
                        bitmap.SetPixel(i, j, Color.FromArgb(imageGray[i, j], imageGray[i, j], imageGray[i, j]));

                //關鍵點打點
                if (inteInfoCounter != 0)
                {
                    p = new Pen(Color.Red);
                    for (int c = 0; c < inteInfo.Length - 1; c++)
                    {
                        int i = inteInfo[c].x, j = inteInfo[c].y;
                        if (inteInfo[c].isNotEmpty && i > 3 && i < bitmap.Width - 3 && j > 3 && j < bitmap.Height - 3)
                        {
                            g = Graphics.FromImage(grayImage);
                            using (g)
                            {
                                //*畫十字
                                g.DrawLine(p, i - 3, j, i + 3, j);
                                g.DrawLine(p, i, j - 3, i, j + 3);//*/
                                //畫走勢
                                Point point1 = new Point(i, j);
                                Point point2 = new Point(i + (int)(Math.Cos(inteInfo[c].sita) * inteInfo[c].gradientMagnitudes), j + (int)(Math.Cos(inteInfo[c].sita) * inteInfo[c].gradientMagnitudes));
                                g.DrawLine(p, point1, point2);
                            }
                        }
                    }
                }

                //打滑鼠點
                if (x != 0 && y != 0)
                {
                    int i = x, j = y;
                    p = new Pen(Color.Blue);
                    g = Graphics.FromImage(grayImage);
                    using (g)
                    {
                        //十字
                        g.DrawLine(p, i - 3, j, i + 3, j);
                        g.DrawLine(p, i, j - 3, i, j + 3);
                        /*框框
                        g.DrawLine(p, i - 3, j - 3, i + 3, j - 3);
                        g.DrawLine(p, i - 3, j + 3, i + 3, j + 3);
                        g.DrawLine(p, i - 3, j - 3, i - 3, j + 3);
                        g.DrawLine(p, i + 3, j - 3, i + 3, j + 3);//*/
                    }
                }

                pictureBox1.Invalidate();
            }
        }
    }
}
