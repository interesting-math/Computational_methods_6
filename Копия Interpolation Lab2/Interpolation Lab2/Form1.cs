using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;

namespace Interpolation_Lab2
{
    public partial class Form1 : Form
    {
        static String[] methods_of_interpolation = {"none", "LaGrange", "Newton", "Hermite", "Hermite_for_2_points", "Spline", "Least_square_method"};
        
        const double INF = 1E9;
        const double EPS = 1E-17;
        const int MAXN = (int) 1E3;
        
        List<double> list_x = new List<double>();
        List<double> list_y = new List<double>();
        
        bool is_load_data = false;                                            // флаг: указан ли адрес файла, из которого необходимо считывать координаты

        int Up_Screen_Shift = 2;                                             // еще понадобится

        String method_of_interpolation = methods_of_interpolation[0];

        double kx = 0, ky = 0;
        double dx = 0, dy = 0;

        bool is_LaGrange_to_specific_function = false;

        bool Hermite_for_two_points_mode = false;

        double min_x = INF;
        double max_x = -INF;
        double min_y = INF;
        double max_y = -INF;

        double [,]divided_differences = new double[MAXN, MAXN];

        static String[] types_of_function = {"LaGrange: 1/(1+d(x)^2)", "Chebyshev: 1/(1+d(x)^2)", "abs",  "x^3", "cos(x*PI/2)"};

        List<List<double>> derivatives = new List<List<double>>();

        // Spline
        List<double> answer;


        // Least square method
        List<double> answer_of_Gauss_Jordan_method = new List<double>(); 


        public Form1()
        {
            InitializeComponent(); 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {   
        }
        private void button5_Click(object sender, EventArgs e)
        {
        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            textBox1.Text = openFileDialog1.FileName;
        }
        private void button3_Click(object sender, EventArgs e)
        {
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
            is_load_data = true;
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);
            g.Clear(Color.Black);
            list_x.Clear();
            list_y.Clear();

            is_load_data = false;
        }

        private void button4_Click(object sender, EventArgs e)
        {     
        }

        private void Clear_Lists()
        {
            list_x.Clear();
            list_y.Clear();
            derivatives.Clear();
        }

        private void Parsing()
        {
            System.IO.StreamReader file = new System.IO.StreamReader(@textBox1.Text);   // Открываем файл для чтения координат
            
            int N = Int32.Parse(file.ReadLine());
            for (int i = 0; i < N; i++)
            {
                double X = 0, Y = 0;

                String str = file.ReadLine();
                str += ' ';
                int j = 0; ;
                bool is_negative = false;
                if (str[j] == '-')
                {
                    is_negative = true;
                    j++;
                }
                for (; j < str.Length; j++)
                {
                    if (str[j] != ' ' && str[j] != '.')
                    {
                        X *= 10;
                        X += (int)(str[j] - '0');
                    }
                    else break;
                }
                if (str[j] == '.')
                {
                    double tmp = 1;
                    for (j++; j < str.Length; j++)
                    {
                        if (str[j] != ' ')
                        {
                            tmp /= 10.0;
                            X += tmp * (int)(str[j] - '0');
                        }
                        else break;
                    }
                }
                j++;
                if (is_negative == true) X *= -1;
                is_negative = false;
                if (str[j] == '-')
                {
                    is_negative = true;
                    j++;
                }
                for (; j < str.Length; j++)
                {
                    if (str[j] != ' ' && str[j] != '.')
                    {
                        Y *= 10;
                        Y += (int)(str[j] - '0');
                    }
                    else break;
                }
                if (str[j] == '.')
                {
                    double tmp = 1;
                    for (j++; j < str.Length; j++)
                    {
                        if (str[j] != ' ')
                        {
                            tmp /= 10.0;
                            Y += tmp * (int)(str[j] - '0');
                        }
                        else break;
                    }
                }
                if (is_negative == true) Y *= -1;
                list_x.Add(X); list_y.Add(Y);
            }
        }

        private void Find_Min_Max_X()
        {
            for (int i = 0; i < list_x.Count; i++)                          // поиск минимального X
            {
                min_x = Math.Min(min_x, list_x[i]);                         
            }
            for (int i = 0; i < list_x.Count; i++)                          // поиск максимального X
            {
                max_x = Math.Max(max_x, list_x[i]);
            }
            
            double size_of_extrapolation = (max_x - min_x) * 0.01;          // размер левой и размер правой экстраполяции равен 3% от общего расстояния между первым и последним узлом по X
            if (method_of_interpolation == methods_of_interpolation[3])
            {
                size_of_extrapolation = 0;
            }
            if (method_of_interpolation == methods_of_interpolation[4])
            {
                size_of_extrapolation = (max_x - min_x) * 0.5;
            }

            size_of_extrapolation = 0;
            // расширяем область для интерполяции/экстраполяции
            min_x -= size_of_extrapolation;
            max_x += size_of_extrapolation;
            
            for (int i = 0; i < list_x.Count; i++)
            {
                list_x[i] -= min_x;
            }
        }

        private void Find_Min_Max_Y_LaGrange()
        {
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;
                for (int i = 0; i < list_x.Count; i++)
                {
                    double phi = 1.0;
                    for (int j = 0; j < list_x.Count; j++)
                    {
                        if (i == j) continue;
                        phi *= cur_x - list_x[j];
                        phi /= list_x[i] - list_x[j];
                    }
                    cur_y += phi * list_y[i];
                }
                min_y = Math.Min(min_y, cur_y);
                max_y = Math.Max(max_y, cur_y);
            }
            if (is_LaGrange_to_specific_function == true) min_y += (max_y - min_y)*0.8;
            ky = ((double)pictureBox1.Height-Up_Screen_Shift) / (max_y - min_y);
            is_LaGrange_to_specific_function = false;
        }

        private void Draw_Graph_LaGrange()
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики
            Pen pen = new Pen(Color.Gold, 1);                                           // Инициализация pen

            int prev_x = 0, prev_y = 0;
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;
                for (int i = 0; i < list_x.Count; i++)
                {
                    double phi = 1.0;
                    for (int j = 0; j < list_x.Count; j++)
                    {
                        if (i == j) continue;
                        phi *= cur_x - list_x[j];
                        phi /= list_x[i] - list_x[j];
                    }
                    cur_y += phi * list_y[i];
                }
                int screen_y = pictureBox1.Height - ((int) ((cur_y - min_y)*ky) + Up_Screen_Shift);
                if (k == 0) prev_y = screen_y;
                g.DrawLine(pen, prev_x, prev_y, k, screen_y);
                if (k != 0) prev_x = k - 1;
                prev_y = screen_y;
            }
        }

        private void Draw_Nodes()
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики                

            for (int i = 0; i < list_x.Count; i++)
            {
                int screen_x = (int)(list_x[i] * kx);
                int screen_y = pictureBox1.Height - ((int)((list_y[i] - min_y) * ky) + Up_Screen_Shift);
                
                Brush brush = new SolidBrush(Color.BlueViolet);
                g.FillEllipse(brush, screen_x-3, screen_y-3, 6, 6);
                brush = new SolidBrush(Color.White);
                g.FillEllipse(brush, screen_x - 2, screen_y - 2, 4, 4);
            }
        }

        private void Draw_Axes()
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики         
            Pen pen = new Pen(Color.Snow, 1);                                          // Инициализация pen
            g.Clear(Color.Black);                                                       // Очистка graphics g

            g.DrawRectangle(pen, 1, pictureBox1.Height - ((int)((0 - min_y) * ky) + Up_Screen_Shift), pictureBox1.Width - 2, 1);
            g.DrawRectangle(pen, (int)((0-min_x)*kx), 1, 1, pictureBox1.Height - 2);
            int step = 70;

            Brush Кисть = new SolidBrush(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int count_of_digits = 1000;
            for (int i = 0; i < 16; i++)
            {
                int x = (int)(i * step);
                g.DrawLine(pen, x, pictureBox1.Height - (int)((0 - min_y) * ky) + Up_Screen_Shift - 8, x, pictureBox1.Height - (int)((0 - min_y) * ky) + Up_Screen_Shift);

                double coordinate = ((double)((int)((min_x + dx * step * i) * count_of_digits))) / count_of_digits;
                String Text = String.Format("{0}", coordinate.ToString());
                g.DrawString(Text, Font, Кисть, x-15, pictureBox1.Height - 23); // Координаты размещения текста
            }
            for (int i = 0; i <= pictureBox1.Height/(step/2); i++)
            {
                int y = (int)(i * step/2);
                g.DrawLine(pen, (int)((0 - min_x) * kx) - 5, pictureBox1.Height - y - 1, (int)((0 - min_x) * kx) + 5, pictureBox1.Height - y - 1);            // -1 (поправка, чтобы было видно нижнюю палочку/начертание)

                double coordinate = ((double)((int)((min_y + dy * (step/2) * i) * count_of_digits))) / count_of_digits;
                String Text = String.Format("{0}", coordinate.ToString());
                int count_of_pixels_on_one_symbol = 6;
                int cnt_symbols = 0;
                if (coordinate.ToString().Length < 8)
                {
                    cnt_symbols = 8 - coordinate.ToString().Length;
                }
                int left_shift_of_text = count_of_pixels_on_one_symbol * cnt_symbols;
                g.DrawString(Text, Font, Кисть, 2 + left_shift_of_text, pictureBox1.Height-y-7);            // Координаты размещения текста
            } 
        }

        private void Initialization()
        {
            Clear_Lists();
            if (is_load_data && (method_of_interpolation == methods_of_interpolation[1] || method_of_interpolation == methods_of_interpolation[2] || method_of_interpolation == methods_of_interpolation[5] || method_of_interpolation == methods_of_interpolation[6])) Parsing();
            Initialization_Of_Min_Max_Values();
        }
      
        private void Draw_LaGrange()
        {
            Find_Min_Max_X();
            dx = (max_x - min_x)/((double)pictureBox1.Width);
            Find_Min_Max_Y_LaGrange();
            dy = (max_y - min_y) / ((double)pictureBox1.Height-Up_Screen_Shift);
            kx = ((double)pictureBox1.Width) / (max_x - min_x);
            Draw_Axes();
            Draw_Graph_LaGrange();
            Draw_Nodes();
        }

        public void button1_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[1];
            if (is_load_data)
            {
                Initialization();
                Draw_LaGrange();
            }
            else
            {
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
        }

        private void Form1_DragLeave(object sender, EventArgs e)
        {
        }

        private void Form1_DragOver(object sender, DragEventArgs e)
        {
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
        }

        private void Form1_Move(object sender, EventArgs e)
        {
            if (is_load_data)
            {
                Initialization();
                if (method_of_interpolation == methods_of_interpolation[1]) Draw_LaGrange();
                if (method_of_interpolation == methods_of_interpolation[2]) Draw_Newton();
            }
        }

        private void button3_Move(object sender, EventArgs e)
        {
        }

        private void button1_DragOver(object sender, DragEventArgs e)
        {
        }

        private void button1_Paint(object sender, PaintEventArgs e)
        {  
        }

        private void button1_Validated(object sender, EventArgs e)
        {
        }

        private void Initialization_Of_Min_Max_Values()
        {
            min_x = INF;
            max_x = -INF;
            min_y = INF;
            max_y = -INF;   
        }

        private void Initialization_Of_Min_Max_Values_Of_Function()
        {
            min_y = INF;
            max_y = -INF;
        }


        private void Find_Min_Max_Y_Newton()
        {
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;
                double phi = 1.0;
                for (int i = 0; i < list_x.Count; i++)
                {
                    cur_y += phi * divided_differences[0, i];
                    if (i+1 != list_x.Count) phi *= cur_x - list_x[i];
                }
                min_y = Math.Min(min_y, cur_y);
                max_y = Math.Max(max_y, cur_y);
            }
            ky = ((double)pictureBox1.Height-Up_Screen_Shift) / (max_y - min_y);    
        }

        private void Draw_Graph_Newton()
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики
            Pen pen = new Pen(Color.LimeGreen, 1);                                      // Инициализация pen

            int prev_x = 0, prev_y = 0;
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;
                double phi = 1.0;
                for (int i = 0; i < list_x.Count; i++)
                {
                    cur_y += phi * divided_differences[0, i];
                    if (i + 1 != list_x.Count) phi *= cur_x - list_x[i];
                }
                int screen_y = pictureBox1.Height - ((int)((cur_y - min_y) * ky) + Up_Screen_Shift);
                if (k == 0) prev_y = screen_y;
                g.DrawLine(pen, prev_x, prev_y, k, screen_y);
                if (k != 0) prev_x = k - 1;
                prev_y = screen_y;
            }
        }

        private void Draw_Newton() {
            for (int t = 0; t < list_x.Count; t++)
            {
                divided_differences[t, 0] = list_y[t];
                for (int i = t-1, j = 1; i >= 0; i--, j++)
                {
                    divided_differences[i, j] = (divided_differences[i+1, j-1]-divided_differences[i, j-1]) / (list_x[i+j]-list_x[i]);
                }
            }

            Find_Min_Max_X();
            dx = (max_x - min_x) / ((double)pictureBox1.Width);
            Find_Min_Max_Y_Newton();
            dy = (max_y - min_y) / ((double)pictureBox1.Height-Up_Screen_Shift);
            kx = ((double)pictureBox1.Width) / (max_x - min_x);
            Draw_Axes();
            Draw_Graph_Newton();
            Draw_Nodes();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[2];
            if (is_load_data)
            {
                Initialization();
                Draw_Newton();
            }
            else
            {
            }
        }

        private void label5_Click(object sender, EventArgs e)
        {
        }

        private void panel2_Paint(object sender, PaintEventArgs e)
        {
        }

        private void Initialization_of_5_points()
        {
            for (int i = 0; i < 5; i++)
            {
                list_x.Add(-1.0 + i * 2.0/5.0);
            }
            list_y.Add(0.1);
            list_y.Add(0.8);
            list_y.Add(0.5);
            list_y.Add(0.2);
            list_y.Add(0.9);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            Initialization_of_5_points();
            Draw_LaGrange();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            button8.Visible = true;
            button9.Visible = true;
            button10.Visible = true;
        }

        private void label5_Click_1(object sender, EventArgs e)
        {
        }

        private void Draw_Graph_Function(String type_of_function)
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики
            Pen pen = new Pen(Color.White, 1);                                          // Инициализация pen

            int prev_x = 0, prev_y = 0;
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = min_x + dx * k;
                double cur_y = 0;

                if (type_of_function == types_of_function[0]) cur_y = 1.0 / (1.0 + (10.0 + 2.0) * Math.Pow(cur_x, 2));
                if (type_of_function == types_of_function[1]) cur_y = 1.0 / (1.0 + (10.0 + 2.0) * Math.Pow(cur_x, 2));
                if (type_of_function == types_of_function[2]) cur_y = Math.Abs(cur_x);
                if (type_of_function == types_of_function[3]) cur_y = Math.Pow(cur_x, 3);
                if (type_of_function == types_of_function[4]) cur_y = Math.Cos(cur_x*Math.PI/2.0);

                int screen_y = pictureBox1.Height - ((int)((cur_y - min_y) * ky) + Up_Screen_Shift);
                if (k == 0) prev_y = screen_y;
                g.DrawLine(pen, prev_x, prev_y, k, screen_y);
                if (k != 0) prev_x = k - 1;
                 prev_y = screen_y;
            }
        }
        
        private void Draw_Function(String type_of_function)
        {
            Draw_Graph_Function(type_of_function);
        }

        private void Initialization_of_n_points_for_LaGrange_Interpolation(int n, bool is_zeros_of_Chebyshev_polynomial)
        {
            double h = 2.0 / n;
            int N = 2*(n+1);
            if (is_zeros_of_Chebyshev_polynomial)
            {
                for (int i = 0; i <= n; i++)
                {
                    list_x.Add(Math.Cos((2 * i + 1) * Math.PI / N));
                    list_y.Add(1.0 / (1 + (10 + 2) * Math.Pow(list_x[i], 2)));
                }
            }
            else
            {
                for (int i = 0; i <= n; i++)
                {
                    list_x.Add(-1.0 + i * h);
                    list_y.Add(1.0 / (1 + (10 + 2) * Math.Pow(list_x[i], 2)));
                }
            }
            Draw_LaGrange();
            Draw_Function("LaGrange: 1/(1+d(x)^2)");
        }

        private void Initialization_of_n_points_for_Newton_Interpolation(int n, bool is_zeros_of_Chebyshev_polynomial, string function)
        {
            double h = 2.0 / n;
            int N = 2 * (n + 1);
            is_zeros_of_Chebyshev_polynomial = true;
            for (int i = 0; i <= n; i++)
            {
                if (is_zeros_of_Chebyshev_polynomial) list_x.Add(Math.Cos((2 * i + 1) * Math.PI / N));
                else list_x.Add(-1.0 + i * h);
            }

            if (function == "abs")
            {
                for (int i = 0; i <= n; i++)
                {
                    list_y.Add(Math.Abs(list_x[i]));
                }
            }
            if (function == "cube")
            {
                for (int i = 0; i <= n; i++)
                {
                    list_y.Add(Math.Pow(list_x[i], 3));
                }
            }
            if (function == "cos")
            {
                for (int i = 0; i <= n; i++)
                {
                    list_y.Add(Math.Cos(Math.PI/2 * list_x[i]));
                }
            }
            Draw_Newton();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();

            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_LaGrange_Interpolation(4, false);
            Draw_Function("LaGrange: 1/(1+d(x)^2)");
        }

        private void button9_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();

            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_LaGrange_Interpolation(10, false);
            Draw_Function("LaGrange: 1/(1+d(x)^2)");
        }

        private void button10_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();

            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            is_LaGrange_to_specific_function = true;
            Initialization_of_n_points_for_LaGrange_Interpolation(20, false);
            Draw_Function("LaGrange: 1/(1+d(x)^2)");
        }

        private void label6_Click(object sender, EventArgs e)
        {
        }

        private void button14_Click(object sender, EventArgs e)
        {
            button11.Visible = true;
            button12.Visible = true;
            button13.Visible = true;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();

            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_LaGrange_Interpolation(4, true);
            Draw_Function("Chebyshev: 1/(1+d(x)^2)");
        }

        private void button12_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            
            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_LaGrange_Interpolation(10, true);
            Draw_Function("Chebyshev: 1/(1+d(x)^2)");
        }

        private void button11_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            
            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_LaGrange_Interpolation(20, true);
            Draw_Function("Chebyshev: 1/(1+d(x)^2)");
        }

        private void button15_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            Initialization_of_5_points();
            Draw_Newton();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            button16.Visible = true;
            button17.Visible = true;
        }

        private void button21_Click(object sender, EventArgs e)
        {
            button19.Visible = true;
            button20.Visible = true;
        }

        private void button24_Click(object sender, EventArgs e)
        {
            button22.Visible = true;
            button23.Visible = true;
        }

        private void button22_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            
            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_Newton_Interpolation(20, false, "cos");
            Draw_Function("cos(x*PI/2)");
        }

        private void button17_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            
            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_Newton_Interpolation(10, false, "abs");
            Draw_Function("abs");
        }

        private void button16_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            
            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_Newton_Interpolation(20, false, "abs");
            Draw_Function("abs");
        }

        private void button20_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            
            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_Newton_Interpolation(10, false, "cube");
            Draw_Function("x^3");
        }

        private void button19_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            
            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_Newton_Interpolation(20, false, "cube");
            Draw_Function("x^3");
        }

        private void button23_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[0];
            Initialization();
            
            button8.Visible = false;
            button9.Visible = false;
            button10.Visible = false;

            button11.Visible = false;
            button12.Visible = false;
            button13.Visible = false;

            button16.Visible = false;
            button17.Visible = false;

            button19.Visible = false;
            button20.Visible = false;

            button22.Visible = false;
            button23.Visible = false;

            Initialization_of_n_points_for_Newton_Interpolation(10, false, "cos");
            Draw_Function("cos(x*PI/2)");
        }

        private void Find_Min_Max_Y_Hermite()
        {
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;


                for (int i = 0; i < list_x.Count - 1; i++)
                {
                    if (list_x[i] + EPS < cur_x && cur_x < list_x[i + 1] - EPS)
                    {
                        cur_x = (cur_x - list_x[i])/(list_x[i+1]-list_x[i]);

                        double C00 = 1.0 - 3.0 * Math.Pow(cur_x, 2) + 2.0 * Math.Pow(cur_x, 3);
                        double C01 = cur_x - 2.0 * Math.Pow(cur_x, 2) + Math.Pow(cur_x, 3);
                        double C10 = 3.0 * Math.Pow(cur_x, 2) - 2.0 * Math.Pow(cur_x, 3);
                        double C11 = -Math.Pow(cur_x, 2) + Math.Pow(cur_x, 3);

                        double h = list_x[i + 1] - list_x[i];

                        cur_y = C00 * list_y[i] + h*C01 * derivatives[i].ElementAt(0) + C10 * list_y[i + 1] + h*C11 * derivatives[i + 1].ElementAt(0);

                        min_y = Math.Min(min_y, cur_y);
                        max_y = Math.Max(max_y, cur_y);

                        break;    
                    }
                }
            }
            if (Hermite_for_two_points_mode == true)
            {
                min_y = -3;
                max_y = 3;
            }
            ky = ((double)pictureBox1.Height - Up_Screen_Shift) / (max_y - min_y);
        }

        private void Draw_Graph_Hermite()
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики
            Pen pen = new Pen(Color.Blue, 1);                                           // Инициализация pen

            int prev_x = 0, prev_y = 0;
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;

                bool should_be_drawn = false;

                for (int i = 0; i < list_x.Count - 1; i++)
                {
                    if (list_x[i] + EPS < cur_x && cur_x < list_x[i + 1] - EPS)
                    {
                        cur_x = (cur_x - list_x[i]) / (list_x[i + 1] - list_x[i]);

                        double C00 = 1.0 - 3.0 * Math.Pow(cur_x, 2) + 2.0 * Math.Pow(cur_x, 3);
                        double C01 = cur_x - 2.0 * Math.Pow(cur_x, 2) + Math.Pow(cur_x, 3);
                        double C10 = 3.0 * Math.Pow(cur_x, 2) - 2.0 * Math.Pow(cur_x, 3);
                        double C11 = -Math.Pow(cur_x, 2) + Math.Pow(cur_x, 3);

                        double h = list_x[i + 1] - list_x[i];

                        cur_y = C00 * list_y[i] + h * C01 * derivatives[i].ElementAt(0) + C10 * list_y[i + 1] + h * C11 * derivatives[i + 1].ElementAt(0);

                        should_be_drawn = true;
                        break;
                    }
                }

                if (!should_be_drawn) continue;
                else
                {
                    int screen_y = pictureBox1.Height - ((int)((cur_y - min_y) * ky) + Up_Screen_Shift);
                    if (k == 0) prev_y = screen_y;
                    g.DrawLine(pen, prev_x, prev_y, k, screen_y);
                    if (k != 0) prev_x = k - 1;
                    prev_y = screen_y;
                }
            }
        }
        
        private void Draw_Hermite()
        {
            Find_Min_Max_X();
            
            dx = (max_x - min_x) / ((double)pictureBox1.Width);
            Find_Min_Max_Y_Hermite();

            dy = (max_y - min_y) / ((double)pictureBox1.Height - Up_Screen_Shift);
            kx = ((double)pictureBox1.Width) / (max_x - min_x);
            Draw_Axes();
            Draw_Graph_Hermite();
            Draw_Nodes();
        }

        private void button25_Click(object sender, EventArgs e)
        {   
        }

        private void button26_Click(object sender, EventArgs e)
        {
        }

        private bool is_successful_Hermite_Parsing_For_Two_Points() {
            if (textBox4.Text.Length == 0) return false;
            if (textBox5.Text.Length == 0) return false;

            string derivative_1 = textBox4.Text.ToString();
            string derivative_2 = textBox5.Text.ToString();

            double X = 0, Y = 0;

            int j = 0;
            bool is_negative = false;
            if (derivative_1[j] == '-')
            {
                is_negative = true;
                j++;
            }
            for (; j < derivative_1.Length; j++)
            {
                if (derivative_1[j] != ' ' && derivative_1[j] != '.')
                {
                    X *= 10;
                    X += (int)(derivative_1[j] - '0');
                }
                else break;
            }
            if (j < derivative_1.Length && derivative_1[j] == '.')
            {
                double tmp = 1;
                for (j++; j < derivative_1.Length; j++)
                {
                    if (derivative_1[j] != ' ')
                    {
                        tmp /= 10.0;
                        X += tmp * (int)(derivative_1[j] - '0');
                    }
                    else break;
                }
            }
            j++;
            if (is_negative == true) X *= -1;
            is_negative = false;

            j = 0;
            is_negative = false;
            if (derivative_2[j] == '-')
            {
                is_negative = true;
                j++;
            }
            for (; j < derivative_2.Length; j++)
            {
                if (derivative_2[j] != ' ' && derivative_2[j] != '.')
                {
                    Y *= 10;
                    Y += (int)(derivative_2[j] - '0');
                }
                else break;
            }
            if (j < derivative_2.Length && derivative_2[j] == '.')
            {
                double tmp = 1;
                for (j++; j < derivative_2.Length; j++)
                {
                    if (derivative_2[j] != ' ')
                    {
                        tmp /= 10.0;
                        Y += tmp * (int)(derivative_2[j] - '0');
                    }
                    else break;
                }
            }
            j++;
            if (is_negative == true) Y *= -1;
            is_negative = false;

            derivatives.Clear();
            List<double> current_list_of_derivatives = new List<double>();
            
            current_list_of_derivatives.Add(X);
            derivatives.Add(current_list_of_derivatives);
            current_list_of_derivatives = new List<double>();
            current_list_of_derivatives.Add(Y);
            derivatives.Add(current_list_of_derivatives);
            current_list_of_derivatives = null;
            
            return true;
        }

        bool is_successful_Hermite_Parsing()
        {
            System.IO.StreamReader file = new System.IO.StreamReader(@textBox1.Text);   // Открываем файл для чтения координат

            String str = file.ReadLine();
            int N = 0;
            int K = 0;

            int t = 0;
            for (t = 0; t < str.Length; t++)
            {
                if (str[t] != ' ')
                {
                    N *= 10;
                    N += (int)(str[t] - '0');
                }
                else break;
            }
            t++;
            for (; t < str.Length; t++)
            {
                if (str[t] != ' ')
                {
                    K *= 10;
                    K += (int)(str[t] - '0');
                }
                else break;
            }

            for (int i = 0; i < N; i++)
            {
                double X = 0, Y = 0;

                str = file.ReadLine();
                str += ' ';
                int j = 0; ;
                bool is_negative = false;
                if (str[j] == '-')
                {
                    is_negative = true;
                    j++;
                }
                for (; j < str.Length; j++)
                {
                    if (str[j] != ' ' && str[j] != '.')
                    {
                        X *= 10;
                        X += (int)(str[j] - '0');
                    }
                    else break;
                }
                if (str[j] == '.')
                {
                    double tmp = 1;
                    for (j++; j < str.Length; j++)
                    {
                        if (str[j] != ' ')
                        {
                            tmp /= 10.0;
                            X += tmp * (int)(str[j] - '0');
                        }
                        else break;
                    }
                }
                j++;
                if (is_negative == true) X *= -1;
                is_negative = false;
                if (str[j] == '-')
                {
                    is_negative = true;
                    j++;
                }
                for (; j < str.Length; j++)
                {
                    if (str[j] != ' ' && str[j] != '.')
                    {
                        Y *= 10;
                        Y += (int)(str[j] - '0');
                    }
                    else break;
                }
                if (str[j] == '.')
                {
                    double tmp = 1;
                    for (j++; j < str.Length; j++)
                    {
                        if (str[j] != ' ')
                        {
                            tmp /= 10.0;
                            Y += tmp * (int)(str[j] - '0');
                        }
                        else break;
                    }
                }
                if (is_negative == true) Y *= -1;
                list_x.Add(X); list_y.Add(Y);

                Y = 0;
                j++;
                is_negative = false;
                if (str[j] == '-')
                {
                    is_negative = true;
                    j++;
                }
                for (; j < str.Length; j++)
                {
                    if (str[j] != ' ' && str[j] != '.')
                    {
                        Y *= 10;
                        Y += (int)(str[j] - '0');
                    }
                    else break;
                }
                if (str[j] == '.')
                {
                    double tmp = 1;
                    for (j++; j < str.Length; j++)
                    {
                        if (str[j] != ' ')
                        {
                            tmp /= 10.0;
                            Y += tmp * (int)(str[j] - '0');
                        }
                        else break;
                    }
                }
                if (is_negative == true) Y *= -1;

                List<double> current_list_of_derivatives = new List<double>();
                current_list_of_derivatives.Add(Y);
                derivatives.Add(current_list_of_derivatives);
                current_list_of_derivatives = null;
            }
            return true;
        }

        private void button25_Click_1(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[3];
            Initialization();
            if (is_successful_Hermite_Parsing() == true)
            {
                Find_Min_Max_X();
                dx = (max_x - min_x) / ((double)pictureBox1.Width);

                Find_Min_Max_Y_Hermite();
                dy = (max_y - min_y) / ((double)pictureBox1.Height - Up_Screen_Shift);
                kx = ((double)pictureBox1.Width) / (max_x - min_x);
                Draw_Axes();
                Draw_Graph_Hermite();
                Draw_Nodes();
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
        }

        private void Draw_Vectors_For_2_Points()
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики
            Pen pen = new Pen(Color.DarkGreen, 1);                                      // Инициализация pen

            int y_of_first_point = pictureBox1.Height - ((int)((0 - min_y) * ky) + Up_Screen_Shift);
            int y_of_second_point = pictureBox1.Height - ((int)((derivatives[0].ElementAt(0) - min_y) * ky) + Up_Screen_Shift);
            g.DrawLine(pen, (int)((-1-min_x)*kx), y_of_first_point, (int)((0-min_x)*kx), y_of_second_point);
            y_of_first_point = pictureBox1.Height - ((int)((0 - min_y) * ky) + Up_Screen_Shift);
            y_of_second_point = pictureBox1.Height - ((int)((derivatives[1].ElementAt(0) - min_y) * ky) + Up_Screen_Shift);
            g.DrawLine(pen, (int)((1 - min_x) * kx), y_of_first_point, (int)((2 - min_x) * kx), y_of_second_point);
        }

        private void button4_Click_1(object sender, EventArgs e)
        {    
            method_of_interpolation = methods_of_interpolation[4];
            Initialization();

            list_x.Add(-1);
            list_y.Add(0);
            list_x.Add(1);
            list_y.Add(0);
            
            if (is_successful_Hermite_Parsing_For_Two_Points() == true)
            {
                Hermite_for_two_points_mode = true;
                
                method_of_interpolation = methods_of_interpolation[3];
                Draw_Hermite();
                Draw_Vectors_For_2_Points();
                Draw_Nodes();

                Hermite_for_two_points_mode = false;
            }
        }

        private void Tridiagonal_Matrix_Algorithm(List<vector_of_three_diaonal_matrix> matrix)
        {
            List<double[]> coefficients_of_straight_sweep = new List<double[]>(matrix.Count());
            double[] cur_line = new double[2];
            cur_line[0] = matrix[0].arr[2];
            cur_line[1] = -matrix[0].arr[3];
            coefficients_of_straight_sweep.Add(cur_line);
            cur_line = null;

            for (int j = 1; j <= matrix.Count() - 2; j++)
            {
                cur_line = new double[2];
                cur_line[0] = matrix[j].arr[2] / (-matrix[j].arr[1] - matrix[j].arr[0] * coefficients_of_straight_sweep[coefficients_of_straight_sweep.Count() - 1][0]);
                cur_line[1] = (-matrix[j].arr[3] + matrix[j].arr[0] * coefficients_of_straight_sweep[j - 1][1]) / (-matrix[j].arr[1] - matrix[j].arr[0] * coefficients_of_straight_sweep[j - 1][0]);
                coefficients_of_straight_sweep.Add(cur_line);
                cur_line = null;
            }


            System.IO.StreamWriter out_file = new System.IO.StreamWriter(@"ddd.txt");

            answer = null;
            answer = new List<double>();
            answer.Add((-matrix[matrix.Count() - 1].arr[0] * coefficients_of_straight_sweep[coefficients_of_straight_sweep.Count() - 1][1] - matrix[matrix.Count() - 1].arr[3]) / (1 + matrix[matrix.Count() - 1].arr[0] * coefficients_of_straight_sweep[coefficients_of_straight_sweep.Count()-1][0]));

            out_file.WriteLine(-matrix[matrix.Count() - 1].arr[0]);
            out_file.WriteLine(coefficients_of_straight_sweep[coefficients_of_straight_sweep.Count() - 1][1]);
            out_file.WriteLine(matrix[matrix.Count() - 1].arr[3]);

            out_file.WriteLine();
            out_file.WriteLine();
            out_file.WriteLine();
            out_file.WriteLine();


            for (int j = matrix.Count() - 2; j >= 0; j--)
            {
                double cur_y = coefficients_of_straight_sweep[j][0] * answer[answer.Count()-1] + coefficients_of_straight_sweep[j][1];
                answer.Add(cur_y);
            }

            answer.Reverse();


            for (int j = 0; j < coefficients_of_straight_sweep.Count(); j++)
            {
                out_file.WriteLine(coefficients_of_straight_sweep[j][0] + " " + coefficients_of_straight_sweep[j][1]);
            }

            for (int j = 0; j < answer.Count(); j++)
            {
                out_file.WriteLine(answer[j]);
            }
            out_file.Close();
        }

        public class vector_of_three_diaonal_matrix
        {
            public double[] arr = new double[4];
        };

        private void button27_Click(object sender, EventArgs e)
        {       
        }

        private void Find_Min_Max_Y_Spline()
        {
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;

                for (int i = 0; i < list_x.Count - 1; i++)
                {
                    if (list_x[i] + EPS < cur_x && cur_x < list_x[i + 1] - EPS)
                    {
                        double h_i = list_x[i+1] - list_x[i];

                        cur_y = (answer[i]*Math.Pow(list_x[i+1] - cur_x, 3.0) + answer[i+1] * Math.Pow(cur_x - list_x[i], 3))/(6*h_i) + (cur_x - list_x[i])*(list_y[i+1]/h_i - h_i*answer[i+1]/6.0) + (list_x[i+1] - cur_x) * (list_y[i] / h_i - h_i*answer[i]/6);

                        min_y = Math.Min(min_y, cur_y);
                        max_y = Math.Max(max_y, cur_y);

                        break;
                    }
                }
            }
            ky = ((double)pictureBox1.Height - Up_Screen_Shift) / (max_y - min_y);
        }

        private void Draw_Graph_Spline()
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики
            Pen pen = new Pen(Color.Blue, 1);                                           // Инициализация pen

            int prev_x = 0, prev_y = 0;
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;

                bool should_be_drawn = false;

                for (int i = 0; i < list_x.Count - 1; i++)
                {
                    if (list_x[i] + EPS < cur_x && cur_x < list_x[i + 1] - EPS)
                    {
                        double h_i = list_x[i + 1] - list_x[i];

                        cur_y = (answer[i] * Math.Pow(list_x[i + 1] - cur_x, 3.0) + answer[i + 1] * Math.Pow(cur_x - list_x[i], 3)) / (6 * h_i) + (cur_x - list_x[i]) * (list_y[i + 1] / h_i - h_i * answer[i + 1] / 6.0) + (list_x[i + 1] - cur_x) * (list_y[i] / h_i - h_i * answer[i] / 6);

                        should_be_drawn = true;
                        break;
                    }
                }

                if (!should_be_drawn) continue;
                else
                {
                    int screen_y = pictureBox1.Height - ((int)((cur_y - min_y) * ky) + Up_Screen_Shift);
                    if (k == 0) prev_y = screen_y;
                    g.DrawLine(pen, prev_x, prev_y, k, screen_y);
                    if (k != 0) prev_x = k - 1;
                    prev_y = screen_y;
                }
            }
        }

        private void Draw_Spline(string type_of_spline_interpolation)
        {
            method_of_interpolation = methods_of_interpolation[5];
            Initialization();

            list_x.Clear();
            list_y.Clear();

            for (double x = -1.0; x <= 1.2; x += 1.0)
            {
                list_x.Add(x);
                list_y.Add(1.0/(1.0 + 12 * x*x));
            }
        
            Find_Min_Max_X();
            dx = (max_x - min_x) / ((double)pictureBox1.Width);

            List<vector_of_three_diaonal_matrix> matrix = new List<vector_of_three_diaonal_matrix>();
            vector_of_three_diaonal_matrix tmp = new vector_of_three_diaonal_matrix();

            int N = list_x.Count() - 1;

       //     double k1 = -0.25, k2 = -0.25;
       //     double m1 = 2.5, m2 = 2.5;       // -k1
            if (type_of_spline_interpolation == "natural spline") tmp.arr[0] = 0; tmp.arr[1] = -1; tmp.arr[2] = 0; tmp.arr[3] = 0;
            if (type_of_spline_interpolation == "square ends")
            {
                tmp.arr[0] = 0; tmp.arr[1] = -1.0; tmp.arr[2] = -1.0; tmp.arr[3] = 0;
            }
            if (type_of_spline_interpolation == "known tangents")
            {
                double g0 = Convert.ToDouble(textBox2.Text);
                double h1 = list_x[1] - list_x[0];
                tmp.arr[0] = 0; tmp.arr[1] = -2.0; tmp.arr[2] = 1.0; tmp.arr[3] = (6.0 / Math.Pow(h1, 2)) * (list_y[1] - list_y[0]) - 6 * g0 / h1;
            }
            
            matrix.Add(tmp);
            tmp = null;

            for (int j = 1; j < N; j++)
            {
                tmp = new vector_of_three_diaonal_matrix();

                double h_i = list_x[j] - list_x[j-1];
                double h_i_plus_1 = list_x[j+1] - list_x[j];

                tmp.arr[0] = h_i;
                tmp.arr[1] = 2*(h_i + h_i_plus_1);
                tmp.arr[2] = h_i_plus_1;
                tmp.arr[3] = 6*((list_y[j+1] - list_y[j])/h_i_plus_1 - (list_y[j] - list_y[j-1])/h_i);
                matrix.Add(tmp);
                tmp = null;
            }
            tmp = new vector_of_three_diaonal_matrix();
            if (type_of_spline_interpolation == "natural spline") tmp.arr[0] = 0; tmp.arr[1] = -1; tmp.arr[2] = 0; tmp.arr[3] = 0;
            if (type_of_spline_interpolation == "square ends")
            {
                tmp.arr[0] = -1.0; tmp.arr[1] = -1.0; tmp.arr[2] = 0; tmp.arr[3] = 0;
            }
            if (type_of_spline_interpolation == "known tangents")
            {
                double gn = Convert.ToDouble(textBox3.Text);
                double hn = list_x[list_x.Count() - 1] - list_x[list_x.Count() - 2];
                tmp.arr[0] = 1.0; tmp.arr[1] = -2.0; tmp.arr[2] = 0; tmp.arr[3] = -(6.0 / Math.Pow(hn, 2)) * (list_y[list_y.Count() - 1] - list_y[list_y.Count() - 2]) + 6 * gn / hn;
            }
            
            // -k2

            matrix.Add(tmp);
            tmp = null;
            Tridiagonal_Matrix_Algorithm(matrix);

            Find_Min_Max_Y_Spline();
            dy = (max_y - min_y) / ((double)pictureBox1.Height - Up_Screen_Shift);
            kx = ((double)pictureBox1.Width) / (max_x - min_x);
            Draw_Axes();
            Draw_Graph_Spline();
            Draw_Nodes();
        }

        private void button27_Click_1(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[5];
            if (is_load_data)
            {
                Draw_Spline("natural spline");
            }
            else
            {
            }            
        }

        private void button26_Click_1(object sender, EventArgs e)
        {
        }

        private void The_Method_Of_Gauss_Jordan(List<List<double>> matrix) {
            answer_of_Gauss_Jordan_method.Clear();
            for (int i = 0; i < matrix.Count(); i++)
            {
                double maxv = Math.Abs(matrix[i][i]); int number_of_line = i;
                for (int j = i; j < matrix.Count(); j++)
                {
                    if (Math.Abs(matrix[j][i]) > maxv)
                    {
                        number_of_line = j;
                        maxv = Math.Abs(matrix[j][i]);
                    }
                }
                for (int j = 0; j < matrix[i].Count(); j++)
                {
                    double tmp = matrix[i][j];
                    matrix[i][j] = matrix[number_of_line][j];
                    matrix[number_of_line][j] = tmp;
                }
                double A = matrix[i][i];
                for (int j = 0; j < matrix[i].Count(); j++)
                {
                    matrix[i][j] /= A;
                }
                for (int t = 0; t < matrix.Count(); t++)
                {
                    if (t == i) continue;
                    A = -matrix[t][i];
                    for (int j = 0; j < matrix[i].Count(); j++)
                    {
                        matrix[t][j] += A*matrix[i][j];
                    }
                }
            }
            for (int i = 0; i < matrix.Count(); i++)
            {
                answer_of_Gauss_Jordan_method.Add(matrix[i][matrix[i].Count()-1]);
            }
        }

        private void Find_Min_Max_Y_LSM(int type)
        {
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;
                for (int i = 0; i < type+1; i++)
                {
                    double power = 1.0;
                    for (int j = 1; j <= i; j++)
                    {
                        power *= cur_x;
                    }
                    cur_y += answer_of_Gauss_Jordan_method[i]* power;
                }
                min_y = Math.Min(min_y, cur_y);
                max_y = Math.Max(max_y, cur_y);
            }
            for (int i = 0; i < list_y.Count(); i++)
            {
                min_y = Math.Min(min_y, list_y[i]);
                max_y = Math.Max(max_y, list_y[i]);
            }
            double vertival_expansion = (max_y - min_y) * 0.1;
            max_y += vertival_expansion;
            min_y -= vertival_expansion;
            ky = ((double)pictureBox1.Height - Up_Screen_Shift) / (max_y - min_y);
        }

        private void Draw_Graph_LSM(int type)
        {
            Graphics g = Graphics.FromHwnd(pictureBox1.Handle);                         // Инициализация графики
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;         // Cглаживание графики
            Pen pen = new Pen(Color.Gold, 1);                                           // Инициализация pen

            int prev_x = 0, prev_y = 0;
            for (int k = 0; k < pictureBox1.Width; k++)
            {
                double cur_x = dx * k;
                double cur_y = 0;
                for (int i = 0; i < type + 1; i++)
                {
                    double power = 1.0;
                    for (int j = 1; j <= i; j++)
                    {
                        power *= cur_x;
                    }
                    cur_y += answer_of_Gauss_Jordan_method[i] * power;
                }
                int screen_y = pictureBox1.Height - ((int)((cur_y - min_y) * ky) + Up_Screen_Shift);
                if (k == 0) prev_y = screen_y;
                g.DrawLine(pen, prev_x, prev_y, k, screen_y);
                if (k != 0) prev_x = k - 1;
                prev_y = screen_y;
            }
        }

        private void Draw_LSM(int type)
        {
            Find_Min_Max_X();
            dx = (max_x - min_x) / ((double)pictureBox1.Width);

            List<List<double>> matrix = new List<List<double>>();

            if (type == 0)
            {
                double minx = list_x[0];
                for (int i = 0; i < list_x.Count(); i++)
                {
                    minx = Math.Min(minx, list_x[i]);
                }
                for (int i = 0; i < list_x.Count(); i++)
                {
                    list_x[i] -= minx;
                }

                double miny = list_y[0];
                for (int i = 0; i < list_y.Count(); i++)
                {
                    minx = Math.Min(miny, list_y[i]);
                }
                for (int i = 0; i < list_y.Count(); i++)
                {
                    list_y[i] -= miny;
                }
                return;
            }

            for (int i = 0; i < type + 1; i++)
            {
                List<double> line = new List<double>();
                for (int j = 0; j < type + 1; j++)
                {
                    double A = 0;
                    for (int k = 0; k < list_x.Count(); k++)
                    {
                        double tmp = 1;
                        for (int t = 1; t <= j + i; t++)
                        {
                            tmp *= list_x[k];
                        }
                        A += tmp;
                    }
                    line.Add(A);
                }
                matrix.Add(line);
                line = null;
            }
            for (int i = 0; i < type + 1; i++)
            {
                double b = 0;
                for (int j = 0; j < list_x.Count(); j++)
                {
                    double tmp = 1.0;
                    for (int t = 1; t <= i; t++)
                    {
                        tmp *= list_x[j];
                    }
                    tmp *= list_y[j];
                    b += tmp;
                }
                matrix[i].Add(b);
            }
            The_Method_Of_Gauss_Jordan(matrix);
            matrix = null;

            Find_Min_Max_Y_LSM(type);
            dy = (max_y - min_y) / ((double)pictureBox1.Height - Up_Screen_Shift);
            kx = ((double)pictureBox1.Width) / (max_x - min_x);
            Draw_Axes();
            Draw_Graph_LSM(type);
            Draw_Nodes();
        }

        private void button26_Click_2(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[6];
            if (is_load_data)
            {
                Initialization();
                Draw_LSM(1);
            }
            else
            {   
            }
        }

        private void button28_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[5];
            if (is_load_data)
            {
                Draw_Spline("square ends");
            }
            else
            {
            }
        }

        private void button29_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[5];
            if (is_load_data)
            {
                Draw_Spline("known tangents");
            }
            else
            {
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void button30_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[6];
            if (is_load_data)
            {
                Initialization();
                Draw_LSM(2);
            }
            else
            {
            }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            method_of_interpolation = methods_of_interpolation[6];
            if (is_load_data)
            {
                Initialization();
                Draw_LSM(0);
            }
            else
            {
            }
        }
    }

}
