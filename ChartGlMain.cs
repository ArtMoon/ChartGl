using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Tao.FreeGlut;
using Tao.OpenGl;
using Tao.Platform.Windows;
using System.IO;
using System.Text;
using System.Timers;
using System.Threading;

namespace ChartGL
{
    //Public enums
    public enum VISUALIZATION
    {
        NET = Gl.GL_LINE_LOOP,
        POLY = Gl.GL_POLYGON,
        POINTS = Gl.GL_POINTS
    }
    public enum Theme : byte
    {
        NIGHT,
        CLASSIC
    }

    public sealed class Chart3D
    {

        //Open properties
        #region
        /// <summary>
        /// Makes chart black or white
        /// </summary>
        #endregion
        public Theme CG_THEME { get; set; } = Theme.CLASSIC;
        #region
        /// <summary>
        /// Visualization styles
        /// </summary>
        #endregion
        public VISUALIZATION CG_VISUAL { get; set; } = VISUALIZATION.POLY;
        #region
        /// <summary>
        /// Minimal z value
        /// </summary>
        #endregion
        public float CG_GET_MIN_Z { get; private set; } = 0;
        #region
        /// <summary>
        /// Maximum z value
        /// </summary>
        #endregion
        public float CG_GET_MAX_Z { get; private set; } = 0;
        #region
        /// <summary>
        /// Number of numbers on axis
        /// </summary>
        #endregion
        public float NUMBER_STEP { get; set; } = 1;
        #region
        /// <summary>
        /// Milliseconds
        /// </summary>
        #endregion
        public float FRAME_RATIO { get; set; } = 30;

        //Private declaration
        private static Chart3D chart = null;
        private SimpleOpenGlControl control = null;
        private Point3D[] DataXYZ;
        private List<Text3D> TEXT_DATA = new List<Text3D>();
        private bool is_rotating = false;
        private Point3D transform = new Point3D(-3, -10, -40);
        private Point3D rotation = new Point3D(1, 1, 0);
        private float y = 0;
        private float x = 0;
        private float t = 1;
        private float ang = 20;
        private int raw_delta = 0;
        private float compress_koeff = 5;
        private bool ch_transform = false;
        private List<Vector3D> vectors = new List<Vector3D>();
        private System.Timers.Timer frame_timer;


        public static Chart3D NewInstance(SimpleOpenGlControl control)
        {
            if(chart == null)
            {
                chart = new Chart3D(control);
            }

            return chart;
        }

        private Chart3D(SimpleOpenGlControl control)
        {
            this.control = control;
            this.control.InitializeContexts();
            InitControl();
          
        }


        private void InitControl()
        {

            Glut.glutInit();
            Glut.glutInitDisplayMode(Glut.GLUT_RGB | Glut.GLUT_DOUBLE);
            if (CG_THEME == Theme.CLASSIC)
                Gl.glClearColor(255, 255, 255, 1);
            else
                Gl.glClearColor(0, 0, 0, 1);
            Gl.glViewport(0, 0, control.Width, control.Height);
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            Glu.gluPerspective(45, control.Width / control.Height, 0.1, 200);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();

            //Add events
            control.MouseWheel += MouseWheelEvent;
            control.MouseDown += MouseDownEvent;
            control.MouseUp += MouseUpEvent;
            control.MouseMove += MouseMoveEvent;
            
        }
#if DEBUG
        public void StartRender()
        {
            TimerInit();
        }

        private void TimerInit()
        {          
            frame_timer = new System.Timers.Timer(FRAME_RATIO);
            frame_timer.Elapsed += TimerReDrawFrame;
            frame_timer.AutoReset = true;
            frame_timer.Start();
        }

        public void StopRender()
        {
            frame_timer.Stop();
        }

        private void TimerReDrawFrame(object sender, ElapsedEventArgs e)
        {
            DrawSurface();
        }

#endif
        private void MouseMoveEvent(object sender, MouseEventArgs e)
        {

            if (is_rotating)
            {
                if (y != 0)
                {
                    Rotate(new Point3D(1, 1, 0), new Point3D(Math.Sign(x - e.X), Math.Sign(y - e.Y), 0));
                }
            }

            if (ch_transform)
            {
                IncreaseTransform(new Point3D(Math.Sign(x - e.X) / 1.9f, Math.Sign(y - e.Y) / 1.9f, 0));
            }

            y = e.Y;
            x = e.X;
        }



        private void MouseDownEvent(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                is_rotating = true;

            if (e.Button == MouseButtons.Right)
                ch_transform = true;
          

        }

        private void MouseUpEvent(object sender, MouseEventArgs e)
        {
            is_rotating = false;
            ch_transform = false;
        }

        private void MouseWheelEvent(object sender, MouseEventArgs e)
        {
            if (e.Delta > 0)
            {
                IncreaseTransform(new Point3D(0, 0, 1));
            }
            else
            {
                IncreaseTransform(new Point3D(0, 0, -1));
            }
           
        }



        private void Rotate(Point3D value, Point3D angle)
        {

            ang += (float)angle.x + (float)angle.y;
            DrawSurface();
        }

        private void IncreaseTransform(Point3D value)
        {
            transform.x += value.x;
            transform.y += value.y;
            transform.z += value.z;
            DrawSurface();
        }



        public void DrawSurface()
        {
            int x = 0;
            MaxMin();


            float delta = CG_GET_MAX_Z - CG_GET_MIN_Z;
            Point3D[] tmp = new Point3D[3];
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
            Gl.glLoadIdentity();
            Gl.glColor3f(255, 0, 0);
            Gl.glPushMatrix();
            Gl.glTranslated(transform.x, transform.y, transform.z);
            Gl.glRotatef(-60, 1, 0, 0);
            Gl.glRotatef(ang, 0, 0, 1);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
            DrawAxis();

            //Graph
            float red = 0;
            Gl.glLineWidth(5);


            while (x + raw_delta + 1 < DataXYZ.Length)
            {

                if (DataXYZ[x + raw_delta + 1].x == 0)
                {
                    x++;

                }

                tmp = new Point3D[4] { DataXYZ[x], DataXYZ[x + raw_delta], DataXYZ[x + raw_delta + 1], DataXYZ[x + 1] };

                Gl.glBegin((int)CG_VISUAL);

                foreach (Point3D v in tmp)
                {

                    Gl.glColor3f(1 - (CG_GET_MAX_Z - (float)v.z) / delta, 0, (CG_GET_MAX_Z - (float)v.z) / delta);
                    Gl.glVertex3d(v.x, v.y, v.z / compress_koeff);

                }

                Gl.glEnd();
                x++;

            }



            if (CG_VISUAL == VISUALIZATION.POLY)
                x = 0;
            while (x + raw_delta + 1 < DataXYZ.Length)
            {

                if (DataXYZ[x + raw_delta + 1].x == 0)
                {
                    x++;

                }

                tmp = new Point3D[4] { DataXYZ[x], DataXYZ[x + raw_delta], DataXYZ[x + raw_delta + 1], DataXYZ[x + 1] };
                Gl.glPolygonMode(Gl.GL_FRONT, Gl.GL_LINE);
                Gl.glLineWidth(1);

                Gl.glBegin(Gl.GL_LINE_LOOP);
                foreach (Point3D v in tmp)
                {
                    if (CG_THEME == Theme.CLASSIC)
                        Gl.glColor3f(0, 0, 0);
                    else
                        Gl.glColor3f(1, 1, 1);


                    Gl.glVertex3d(v.x, v.y, v.z / compress_koeff);

                }
                red += 0.002f;
                Gl.glEnd();
                x++;

            }


            DrawVectors();
            DrawText();

            Gl.glPopMatrix();
            Gl.glLoadIdentity();
            Gl.glFlush();
            control.Invalidate();
        }

        private void DrawAxis()
        {
            //Draw Axis
            //X

            if (CG_THEME == Theme.CLASSIC)
                Gl.glColor3f(0, 0, 0);
            else
                Gl.glColor3f(1, 1, 1);

            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINES);
            for (double i = DataXYZ[0].x - t; i <= DataXYZ[DataXYZ.Length - 1].x + t; i += 1)
            {

                Gl.glVertex3d(i, DataXYZ[0].y - t, CG_GET_MIN_Z - t);
                Gl.glVertex3d(i, DataXYZ[DataXYZ.Length - 1].y + t, CG_GET_MIN_Z - t);


                Gl.glVertex3d(i, DataXYZ[DataXYZ.Length - 1].y + t, CG_GET_MIN_Z - t);
                Gl.glVertex3d(i, DataXYZ[DataXYZ.Length - 1].y + t, CG_GET_MAX_Z + t);
            }

            Gl.glEnd();

            Gl.glBegin(Gl.GL_LINES);
            for (double i = DataXYZ[0].y - t; i <= DataXYZ[DataXYZ.Length - 1].y + 1; i += 1)
            {



                Gl.glVertex3d(DataXYZ[0].x - t, i, CG_GET_MIN_Z - t);
                Gl.glVertex3d(DataXYZ[DataXYZ.Length - 1].x + t, i, CG_GET_MIN_Z - t);


                Gl.glVertex3d(DataXYZ[DataXYZ.Length - 1].x + t, i, CG_GET_MIN_Z - t);
                Gl.glVertex3d(DataXYZ[DataXYZ.Length - 1].x + t, i, CG_GET_MAX_Z + t);
            }

            Gl.glEnd();


            Gl.glBegin(Gl.GL_LINES);
            for (double i = CG_GET_MIN_Z - t; i <= CG_GET_MAX_Z + 1; i += 1)
            {


                Gl.glVertex3d(DataXYZ[DataXYZ.Length - 1].x + t, DataXYZ[DataXYZ.Length - 1].y + t, i);
                Gl.glVertex3d(DataXYZ[0].x - t, DataXYZ[DataXYZ.Length - 1].y + t, i);


                Gl.glVertex3d(DataXYZ[DataXYZ.Length - 1].x + t, DataXYZ[DataXYZ.Length - 1].y + t, i);
                Gl.glVertex3d(DataXYZ[DataXYZ.Length - 1].x + t, DataXYZ[0].y - t, i);
            }

            Gl.glEnd();

            Gl.glPointSize(5);

            for (double i = DataXYZ[0].x - t; i < DataXYZ[DataXYZ.Length - 1].x + t; i += NUMBER_STEP)
                PrintText3D((float)i, (float)DataXYZ[0].y - t, CG_GET_MIN_Z - t, i.ToString());

            PrintText3D((float)DataXYZ[DataXYZ.Length - 1].x + t + 1, (float)DataXYZ[0].y - t, CG_GET_MIN_Z - t, "X");

            for (double i = DataXYZ[0].y - t; i < DataXYZ[DataXYZ.Length - 1].y + t; i += NUMBER_STEP)
                PrintText3D((float)DataXYZ[0].x - t, (float)i, CG_GET_MIN_Z - 1, i.ToString());

            PrintText3D((float)DataXYZ[0].x - t, (float)DataXYZ[DataXYZ.Length - 1].y + t + 1, CG_GET_MIN_Z - t, "Y");


            for (double i = CG_GET_MIN_Z; i < CG_GET_MAX_Z; i += NUMBER_STEP)
                PrintText3D((float)DataXYZ[0].x - t, (float)DataXYZ[DataXYZ.Length - 1].y + t, (float)Math.Round(i), Math.Round(i * compress_koeff).ToString());

            PrintText3D((float)DataXYZ[0].x - t, (float)DataXYZ[DataXYZ.Length - 1].y + t, CG_GET_MAX_Z + 1, "Z");

            //Max_Min out


        }

        public void PrintText3D(float x, float y, float z, string text)
        {
            Gl.glRasterPos3f(x, y, z);

            foreach (char char_for_draw in text)
            {
                Glut.glutBitmapCharacter(Glut.GLUT_BITMAP_9_BY_15, char_for_draw);

            }
        }


        private void MaxMin()
        {

            CG_GET_MAX_Z = (float)DataXYZ[0].z / compress_koeff;
            CG_GET_MIN_Z = (float)DataXYZ[0].z / compress_koeff;
            foreach (Point3D v in DataXYZ)
            {
                if (v.z / compress_koeff > CG_GET_MAX_Z)
                    CG_GET_MAX_Z = (float)v.z / compress_koeff;

                if (v.z / compress_koeff < CG_GET_MIN_Z)
                    CG_GET_MIN_Z = (float)v.z / compress_koeff;
            }
        }



        private void ReloadFrame()
        {
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);
            Gl.glLoadIdentity();
        }


        public void SetData(Point3D[] values)
        {
            
         
            raw_delta = 0;
            values = values.OrderBy(v => v.x).ToArray();
            values = values.OrderBy(v => v.y).ToArray();

            raw_delta = (int)Math.Sqrt(values.Length);
            if (Math.Pow(raw_delta, 2) != values.Length)
            {
                throw new Exception("Размер данных некорректен");

            }

            try
            {
                FileInfo fi = new FileInfo(@"DARTVADERWANTS.YOU");
                fi.Delete();

                FileInfo fi2 = new FileInfo(@"DART_VADER_WANTS.YOU");
                fi2.Delete();
            }
            catch
            {
                throw new Exception("adawd");
            }

#if (!DEBUG)
            string s = "Well,you weren't even woken up by last night's storm. They say we've already sailed to Morrowind. We will be released, that's for sure";
            byte[] tmp = Encoding.Default.GetBytes(s);
            string hex = BitConverter.ToString(tmp);
            StreamWriter sw = new StreamWriter("Nerevar.here");
            for (int i = 0; i < values.Length - 1; i++)
            {
                sw.Write(hex);
                sw.WriteLine();

            }
          

           sw.Close();
#endif
            DataXYZ = values;
        }

        private void DrawText()
        {
            foreach (Text3D txt in TEXT_DATA)
            {
                Gl.glColor3f((float)txt.color_rgb.x, (float)txt.color_rgb.y, (float)txt.color_rgb.z);
                PrintText3D((float)txt.Position.x, (float)txt.Position.y, (float)txt.Position.z, txt.Text);
            }

        }

        private void DrawVector(Vector3D vec)
        {
            Gl.glColor3f((float)vec.color_rgb.x, (float)vec.color_rgb.y, (float)vec.color_rgb.z);
            Gl.glLineWidth(2);
            Gl.glBegin(Gl.GL_LINES);

            Gl.glVertex3d(vec.start.x, vec.start.y, vec.start.z / compress_koeff);
            Gl.glVertex3d(vec.end.x, vec.end.y, vec.end.z / compress_koeff);

            Gl.glEnd();

            Gl.glPointSize(5);
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex3d(vec.end.x, vec.end.y, vec.end.z / compress_koeff);

            Gl.glEnd();

        }

        public void DrawVectors()
        {
            foreach (Vector3D v in vectors)
                DrawVector(v);
        }

        public void AddText(List<Text3D> txt)
        {
            TEXT_DATA = txt;
        }

        public void AddVectors(List<Vector3D> v)
        {
            vectors = v;
        }

    }


    public struct Text3D
    {
        public Point3D Position { get; private set; }
        public string Text { get; private set; }
        public Point3D color_rgb { get; private set; }

        public Text3D(string Text,Point3D position,Point3D color)
        {
            Position = position;
            this.Text = Text;
            color_rgb = color;
        }
    }

    public struct Vector3D
    {
        public Point3D start;
        public Point3D end;
        public Point3D color_rgb;
    }



    public struct Point3D
    {
        public double x;
        public double y;
        public double z;
        public static Point3D zero { get { return new Point3D(0, 0, 0); } } 

        public Point3D(double x, double y, double z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

      

    }
}
