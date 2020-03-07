using System;
using System.Windows.Forms;
using System.IO;
using ChartGL;
using System.Text.RegularExpressions;


namespace Chart
{
    public partial class Form1 : Form
    {

        Chart3D chart;
        Point3D[] dset;
        public Form1()
        {
            InitializeComponent();
            chart = new Chart3D(sg);
        }


        private void LoadDots()
        {
            dset = new Point3D[100];
            FileStream file = new FileStream("dots.txt", FileMode.Open);
            StreamReader read = new StreamReader(file);
            int nVertNo = 100;
            double zV = 0;
            string[] xyz;
            string number;
            for (int i = 0; i < nVertNo; i++)
            {
                number = read.ReadLine();
                xyz = Regex.Split(number, " ");

                dset[i].x = float.Parse(xyz[0]);
                dset[i].y = float.Parse(xyz[1]);
                dset[i].z = float.Parse(xyz[2]);

               
            }
        }

    
        private void Form1_Load(object sender, EventArgs e)
        {
                 
            dset = new Point3D[400];
            //LoadDots();
            FunctionSolve();
            chart.SetData(dset);

        }

        private void button2_Click(object sender, EventArgs e)
        {
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Vector3D v;
            v.start = new Point3D(0, 0, 0);
            v.end = new Point3D(0, 10, 89);
            v.color_rgb = new Point3D(0, 0.3, 0);

            chart.AddVector(v);

            chart.DrawSurface();
         
        }

        public void FunctionSolve()
        {
            //10*x + y^2 = z
            double z = 0;
            int i = 0;

            for (double y = 0; y < 10; y++)
            {
                for (double x = 0; x < 10; x++)
                {
                    i = (int)x + ((int)y*20);

                    z = 10*x + Math.Pow(y,2);
                    
                    dset[i].x = x;
                    dset[i].y = y;
                    dset[i].z = z;
                }
            }

        }

     
    }

  
   

}
