using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.IO.Ports;
using Microsoft.Office.Interop.Excel;
using Excel = Microsoft.Office.Interop.Excel;
using System.Diagnostics;
using GMap.NET.WindowsForms;
using GMap.NET.MapProviders;
using GMap.NET;
using System.Globalization;
using System.Windows.Forms.DataVisualization.Charting;
using AForge.Video;
using AForge.Video.DirectShow;



namespace WindowsFormsApp1
{
    public partial class MainView : Form
    {

        //cam
        MJPEGStream stream;


        // grafik
        private bool isConnected = false;


        // map
        GMapOverlay markersOverlay;


        // görev süresi
        private Timer timer;
        private Stopwatch stopwatch;



        // serial port ve baud rate
        SerialPort serialPort;
        int baudrate;



        // GYRO (GLControl'e uydunun eksenlerini çizdirmek için)
        float x = 0, y = 0, z = 0;
        //bool axx = false, axy = false, axz = false;





        public MainView()
        {
            InitializeComponent();

            InitializeGMap();
            //InitializeSerialPort();


            // görev süresi
            timer = new Timer();
            timer.Interval = 1000; // 1 saniyelik aralıklarla güncelle
            timer.Tick += TimerGorevSuresi_Tick;
            stopwatch = new Stopwatch();


            InitializeCharts();




            // SerialPort'u başlatma
            serialPort = new SerialPort("COM7", 115200);  // Port adı ve baud hızı ayarları
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;
            serialPort.RtsEnable = true;
            serialPort.DtrEnable = true;

            // DataReceived event'ini bağlayın
            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);

            try
            {
                // Seri portu açın
                serialPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Seri portu açarken hata oluştu: " + ex.Message);
            }
        }










        //ARAS

        /*private void UpdateARAS(string errorCode)
        {
            // ARAS hata kodunu analiz et ve panellerin rengini değiştir
            if (errorCode.Length == 5)
            {
                ArasPanel1.BackColor = errorCode[0] == '1' ? Color.Red : Color.Green;
                ArasPanel2.BackColor = errorCode[1] == '1' ? Color.Red : Color.Green;
                ArasPanel3.BackColor = errorCode[2] == '1' ? Color.Red : Color.Green;
                ArasPanel4.BackColor = errorCode[3] == '1' ? Color.Red : Color.Green;
                ArasPanel5.BackColor = errorCode[4] == '1' ? Color.Red : Color.Green;
            }
        }*/














        // grafik
        private void InitializeCharts()
        {
            // Grafikleri başlatma

            BasincGrafik.Series.Clear();
            YukseklikGrafik.Series.Clear();
            SicaklikGrafik.Series.Clear();
            NemGrafik.Series.Clear();
            InisHiziGrafik.Series.Clear();

            // Basınç grafiği
            System.Windows.Forms.DataVisualization.Charting.Series pressureSeries = new System.Windows.Forms.DataVisualization.Charting.Series("Basınç")
            {
                ChartType = SeriesChartType.Line
            };
            BasincGrafik.Series.Add(pressureSeries);

            // Yükseklik grafiği
            System.Windows.Forms.DataVisualization.Charting.Series altitudeSeries = new System.Windows.Forms.DataVisualization.Charting.Series("Yükseklik")
            {
                ChartType = SeriesChartType.Line
            };
            YukseklikGrafik.Series.Add(altitudeSeries);

            // Sıcaklık grafiği
            System.Windows.Forms.DataVisualization.Charting.Series temperatureSeries = new System.Windows.Forms.DataVisualization.Charting.Series("Sıcaklık")
            {
                ChartType = SeriesChartType.Line
            };
            SicaklikGrafik.Series.Add(temperatureSeries);

            // Nem grafiği
            System.Windows.Forms.DataVisualization.Charting.Series humiditySeries = new System.Windows.Forms.DataVisualization.Charting.Series("Nem")
            {
                ChartType = SeriesChartType.Line
            };
            NemGrafik.Series.Add(humiditySeries);

            //İniş Hızı
            System.Windows.Forms.DataVisualization.Charting.Series landingspeedSeries = new System.Windows.Forms.DataVisualization.Charting.Series("İniş Hızı")
            {
                ChartType = SeriesChartType.Line
            };
            InisHiziGrafik.Series.Add(landingspeedSeries);
        }

        private void mainViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }







        // GMap Başlatma

        private void InitializeGMap()
        {
            gMapControl1.MapProvider = GMapProviders.GoogleMap;
            gMapControl1.Position = new PointLatLng(37.957081, 32.591185); // Başlangıç konumu innopark
            gMapControl1.MinZoom = 0;
            gMapControl1.MaxZoom = 24;
            gMapControl1.Zoom = 10;
            gMapControl1.AutoScroll = true;
            markersOverlay = new GMapOverlay("markers");
            gMapControl1.Overlays.Add(markersOverlay);
        }

        // MAP Seri Port Başlatma

        /*private void InitializeSerialPort()
        {
            serialPort = new SerialPort("COM7", 115200); // Seri port numarasını ve hızını ayarlayın
            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
            serialPort.Open();
        }*/





        // Anlık olarak verileri aktarma işlemi, 
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPort.ReadLine();
                this.BeginInvoke(new System.Action(() => ProcessReceivedData(data)));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri alırken bir hata oluştu: {ex.Message}");
            }

        }







        // AYRILMA ARAS
        private void UpdateDataGridViewForSeparation()
        {
            // DataGridView'e "AYRILMA" işlemi için statü ekleyin
            int rowIndex = dataGridView1.Rows.Add(); // Yeni bir satır ekle
            dataGridView1.Rows[rowIndex].Cells["Uydu_Statusu"].Value = "AYRILMA"; // Uydu statüsünü güncelle

            // Statüye göre ek işlemler yapılabilir
        }











        // Geln Verileri İşleme

        private void ProcessReceivedData(string data)
        {
            Console.WriteLine(data);
            string[] dizi = data.Split('*');
            //textBox1.AppendText(data + Environment.NewLine); // Gelen veriyi TextBox'ta göster
            if (dizi.Length >= 22) // Gelen verinin en az 4 parçaya bölündüğünü kontrol et
            {
                //string paketnumarasi = dizi[0];

                // Yeni satırı ekleyin
                int rowIndex = dataGridView1.Rows.Add();




                //gyro (dizi uzunluğuna göre)!!!!!!!!!!!!
                UpdateGyroData(dizi, rowIndex);



                // Gyro verileri ve ARAS
                /*if (dizi.Length >= 5) // Gelen verinin en az 18 parçaya bölündüğünü kontrol et
                {
                    //UpdateGyroData(dizi); // Gyro verilerini UI'ye yansıt


                    // ARAS hata kodunu al
                    string arasErrorCode = dizi[17];  // ARAS hata kodunu dizi'nin 17. elemanı olarak varsayıyorum, bu konumu güncelleyebilirsiniz

                    // Sadece ikinci bitini kontrol et ve paneli güncelle
                    if (arasErrorCode.Length >= 5)
                    {
                        // İniş hızı sapması olup olmadığını kontrol edin
                        if (arasErrorCode[1] == '1')
                        {
                            ArasPanel2.BackColor = Color.Red;  // Hata var, panel kırmızı
                        }
                        else
                        {
                            ArasPanel2.BackColor = Color.Green; // Hata yok, panel yeşil
                        }
                    }



                    // ARAS hata kodunu işleyin ve panelleri güncelleyin
                    //UpdateARAS(arasErrorCode);
                }*/


                // *************************************************** //
                string paketNumarasi = dizi[0];
                string UyduStatusuu = dizi[1];
                string HataKoduu = dizi[2];
                string GondermeSaatii = dizi[3];

                string basinc2=dizi[5];
                string yukseklik2=dizi[7];
                string irtifafarki = dizi[8];
                string pilgerilimi=dizi[11];
                string TakimNoo= dizi[20]; // takım no
                string rakamharfrakamharf=dizi[18];
                string iotdata=dizi[19];

                //GRAFİKKKKKK
                string basinc1 = dizi[4];
                string yukseklik1 = dizi[6];
                string sicaklik1 = dizi[10];
                string nem= dizi[21];
                string inishizi = dizi[9];

                //Verileri sayısal formata dönüştür
                //double pressure = double.Parse(basinc1);
                //double altitude = double.Parse(yukseklik1);
                //double temperature = double.Parse(sicaklik1);
                double humidity = double.Parse(nem);
                double inishizii = double.Parse(inishizi);

                // Grafiklere veri ekle
                UpdateCharts(humidity,inishizii); // inishizi eklenecek
                                         //pressure, altitude, temperature,humidity,          //UpdateCharts(0, 0, 0, 0, inishizii);

                //GPS
                string enlem = dizi[12];
                string boylam = dizi[13];
                string gpsyukseklik = dizi[14];
                gMapControl1.Position = new PointLatLng(StrToFloat(enlem), StrToFloat(boylam));
                LabelEnlem.Text = enlem;
                LabelBoylam.Text = boylam;
                gMapControl1.Invalidate();
                gMapControl1.Update();

                //datagrid viewe verileri ekleme
                dataGridView1.Rows[rowIndex].Cells["Paket_No"].Value = paketNumarasi;
                dataGridView1.Rows[rowIndex].Cells["Uydu_Statusu"].Value = UyduStatusuu;
                dataGridView1.Rows[rowIndex].Cells["Hata_Kodu"].Value = HataKoduu;
                dataGridView1.Rows[rowIndex].Cells["Gonderme_Saati"].Value = GondermeSaatii;
                //dataGridView1.Rows[rowIndex].Cells["Basinc_1"].Value = pressure;
                dataGridView1.Rows[rowIndex].Cells["Basinc_2"].Value = basinc2;
                //dataGridView1.Rows[rowIndex].Cells["Yukseklik_1"].Value = altitude;
                dataGridView1.Rows[rowIndex].Cells["Yukseklik_2"].Value = yukseklik2;
                dataGridView1.Rows[rowIndex].Cells["Irtifa_Farki"].Value = irtifafarki;
                dataGridView1.Rows[rowIndex].Cells["Inis_Hizi"].Value = inishizii;
                //dataGridView1.Rows[rowIndex].Cells["Sicaklik"].Value = temperature;
                dataGridView1.Rows[rowIndex].Cells["Pil_Gerilimi"].Value = pilgerilimi;
                dataGridView1.Rows[rowIndex].Cells["Gps1_Latitude"].Value = enlem;
                dataGridView1.Rows[rowIndex].Cells["Gps1_Longitude"].Value = boylam;
                dataGridView1.Rows[rowIndex].Cells["Gps1_Altitude"].Value = gpsyukseklik;
                dataGridView1.Rows[rowIndex].Cells["Rhrh"].Value = rakamharfrakamharf;
                dataGridView1.Rows[rowIndex].Cells["IoT_Data"].Value = iotdata;
                dataGridView1.Rows[rowIndex].Cells["Takim_No"].Value = TakimNoo;

                // ******************************************************* //



                // Grafik ve DataGridView güncellemeleri sonrasında UI'yi yenileyin
                dataGridView1.Invalidate();


                // UYDU STATÜSÜ kontrolü
                /*if (dizi[0].StartsWith("STATU:"))
                {
                    string status = dizi[0].Substring(6); // "STATU:" kısmını çıkartarak sadece statüyü alırız

                    // Uydu statüsünü DataGridView'e ekleyin veya güncelleyin
                    UpdateUyduStatus(status);

                    // Statüye göre ek işlemler yapabilirsiniz
                    switch (status)
                    {
                        case "AYRILMA":
                            // Ayrılma durumunda DataGridView'de güncelleme yap
                            UpdateDataGridViewForSeparation();
                            checkBoxAyrilma.Checked = true;
                            break;

                        // Diğer statü durumları
                        case "UCUSA_HAZIR":
                            checkBoxUcusaHazir.Checked = true;
                            break;
                        case "YUKSELME":
                            checkBoxYukselme.Checked = true;
                            break;
                        case "MODEL_UYDU_INIS":
                            checkBoxModelUyduinis.Checked = true;
                            break;
                        case "GOREV_YUKU_INIS":
                            checkBoxGorevYukuinis.Checked = true;
                            break;
                        case "KURTARMA":
                            checkBoxKurtarma.Checked = true;
                            break;
                        default:
                            MessageBox.Show("Bilinmeyen durum alındı: " + status);
                            break;
                    }
                }*/
            }
            else
            {
                // Gelen veri beklenenden az parça içeriyor, hata ayıklama için mesaj yazdırabiliriz
                Console.WriteLine("Gelen veri beklenenden az parça içeriyor: " + data);
            }
        }









        // Gyro verilerini UI'ye yansıtmak ve DataGridView'de mevcut satıra eklemek için kullanılan metod
        private void UpdateGyroData(string[] dizi, int rowIndex)
        {
            try
            {
                // Gyro verilerini UI'ye yansıtıyoruz
                labelyy.Text = dizi[15].ToString();
                labelxx.Text = dizi[16].ToString();
                labelzz.Text = dizi[17].ToString();
                x = Convert.ToInt32(dizi[16]);
                y = Convert.ToInt32(dizi[15]);
                z = Convert.ToInt32(dizi[17]);
                glControl1.Invalidate(); // UI thread'inde güncellemeyi zorluyoruz

                // DataGridView'de mevcut satırın hücrelerine verileri ekliyoruz
                dataGridView1.Rows[rowIndex].Cells["Pitch"].Value = dizi[15];   // Pitch değeri y
                dataGridView1.Rows[rowIndex].Cells["Roll"].Value = dizi[16];    // Roll değeri x
                dataGridView1.Rows[rowIndex].Cells["Yaw"].Value = dizi[17];     // Yaw değeri z
                dataGridView1.Invalidate(); // DataGridView'i yeniliyoruz
            }
            catch (Exception ex)
            {
                // Gyro verilerini güncellerken bir hata oluştuğunda hata mesajı gösteriyoruz
                MessageBox.Show("Gyro verilerini güncellerken bir hata oluştu: " + ex.Message);
            }
        }









        // grafikleri güncelleme
        private void UpdateCharts(double nem,double inishizii)// nem eklenecek //double basinc1, double yukseklik1, double sicaklik1,double nem
        {
            // Chart kontrolü thread güvenli olmalı
            if (BasincGrafik.InvokeRequired)
            {
                BasincGrafik.Invoke(new System.Action(() => UpdateCharts(nem,inishizii))); // nem eklenecek
                return;
            }

            try
            {
                // Verileri grafiklere ekle
                //BasincGrafik.Series[0].Points.AddY(Convert.ToDouble(basinc1));
                //YukseklikGrafik.Series[0].Points.AddY(Convert.ToDouble(yukseklik1));
                //SicaklikGrafik.Series[0].Points.AddY(Convert.ToDouble(sicaklik1));
                NemGrafik.Series[0].Points.AddY(Convert.ToDouble(nem));
                InisHiziGrafik.Series[0].Points.AddY(Convert.ToDouble(inishizii));

                // Grafikleri güncelle
                //BasincGrafik.Invalidate();
                //YukseklikGrafik.Invalidate();
                //SicaklikGrafik.Invalidate();
                NemGrafik.Invalidate();
                InisHiziGrafik.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Grafikleri güncellerken bir hata oluştu: {ex.Message}");
            }
        }







        // string olanları float'a çevirme

        public float StrToFloat(string str)
        {
            float result;
            if (float.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
            else
            {
                return 0.0f;
            }
        }







        private void button2_Click_1(object sender, EventArgs e)
        {
            this.Show();
        }







        private void glControl1_Load(object sender, EventArgs e)
        {
            GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
            GL.Enable(EnableCap.DepthTest);
        }




        //  SİLİNDİR
        private void cylinder(float step, float collect, float radius, float vertical1, float vertical2)
        {
            float old_step = 0.1f;

            // DAİRENİN Y EKSENİ ÇİZİMİ
            GL.Begin(PrimitiveType.Quads);
            while (step <= 360)
            {
                if (step < 45)
                    GL.Color3(Color.DarkBlue);
                else if (step < 90)
                    GL.Color3(Color.LightYellow);
                else if (step < 135)
                    GL.Color3(Color.DarkBlue);
                else if (step < 180)
                    GL.Color3(Color.LightYellow);
                else if (step < 225)
                    GL.Color3(Color.DarkBlue);
                else if (step < 270)
                    GL.Color3(Color.LightYellow);
                else if (step < 315)
                    GL.Color3(Color.DarkBlue);
                else if (step < 360)
                    GL.Color3(Color.LightYellow);


                float draw1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float draw1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(draw1_x, vertical1, draw1_y);

                float draw2_x = (float)(radius * Math.Cos((step + 2) * Math.PI / 180F));
                float draw2_y = (float)(radius * Math.Sin((step + 2) * Math.PI / 180F));
                GL.Vertex3(draw2_x, vertical1, draw2_y);

                GL.Vertex3(draw1_x, vertical2, draw1_y);
                GL.Vertex3(draw2_x, vertical2, draw2_y);
                step += collect;
            }
            GL.End();
            GL.Begin(PrimitiveType.Lines);
            step = old_step;
            collect = step;

            // ÜST KAPAK
            while (step <= 180)
            {
                if (step < 45)
                    GL.Color3(Color.DarkBlue);
                else if (step < 90)
                    GL.Color3(Color.Yellow);
                else if (step < 135)
                    GL.Color3(Color.DarkBlue);
                else if (step < 180)
                    GL.Color3(Color.Yellow);
                else if (step < 225)
                    GL.Color3(Color.DarkBlue);
                else if (step < 270)
                    GL.Color3(Color.Yellow);
                else if (step < 315)
                    GL.Color3(Color.DarkBlue);
                else if (step < 360)
                    GL.Color3(Color.Yellow);


                float draw1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float draw1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(draw1_x, vertical1, draw1_y);

                float draw2_x = (float)(radius * Math.Cos((step + 180) * Math.PI / 180F));
                float draw2_y = (float)(radius * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(draw2_x, vertical1, draw2_y);

                GL.Vertex3(draw1_x, vertical1, draw1_y);
                GL.Vertex3(draw2_x, vertical1, draw2_y);
                step += collect;
            }
            step = old_step;
            collect = step;

            // ALT KAPAK
            while (step <= 180)
            {
                if (step < 45)
                    GL.Color3(Color.DarkBlue);
                else if (step < 90)
                    GL.Color3(Color.Yellow);
                else if (step < 135)
                    GL.Color3(Color.DarkBlue);
                else if (step < 180)
                    GL.Color3(Color.Yellow);
                else if (step < 225)
                    GL.Color3(Color.DarkBlue);
                else if (step < 270)
                    GL.Color3(Color.Yellow);
                else if (step < 315)
                    GL.Color3(Color.DarkBlue);
                else if (step < 360)
                    GL.Color3(Color.Yellow);

                float draw1_x = (float)(radius * Math.Cos(step * Math.PI / 180F));
                float draw1_y = (float)(radius * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(draw1_x, vertical2, draw1_y);

                float draw2_x = (float)(radius * Math.Cos((step + 180) * Math.PI / 180F));
                float draw2_y = (float)(radius * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(draw2_x, vertical2, draw2_y);

                GL.Vertex3(draw1_x, vertical2, draw1_y);
                GL.Vertex3(draw2_x, vertical2, draw2_y);
                step += collect;
            }
            GL.End();
        }

        //  KONİ
        private void cone(float step, float collect, float radius1, float radius2, float vertical1, float vertical2)
        {
            float old_step = 0.1f;

            // DAİRENİN Y EKSENİ ÇİZİMİ
            GL.Begin(PrimitiveType.Lines);
            while (step <= 360)
            {
                if (step < 45)
                    GL.Color3(Color.LightYellow);
                else if (step < 90)
                    GL.Color3(Color.DarkBlue);
                else if (step < 135)
                    GL.Color3(Color.LightYellow);
                else if (step < 180)
                    GL.Color3(Color.DarkBlue);
                else if (step < 225)
                    GL.Color3(Color.LightYellow);
                else if (step < 270)
                    GL.Color3(Color.DarkBlue);
                else if (step < 315)
                    GL.Color3(Color.LightYellow);
                else if (step < 360)
                    GL.Color3(Color.DarkBlue);


                float draw1_x = (float)(radius1 * Math.Cos(step * Math.PI / 180F));
                float draw1_y = (float)(radius1 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(draw1_x, vertical1, draw1_y);

                float draw2_x = (float)(radius2 * Math.Cos(step * Math.PI / 180F));
                float draw2_y = (float)(radius2 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(draw2_x, vertical2, draw2_y);
                step += collect;
            }
            GL.End();

            GL.Begin(PrimitiveType.Lines);
            step = old_step;
            collect = step;

            // ÜST KAPAK
            while (step <= 180)
            {
                if (step < 45)
                    GL.Color3(Color.DarkBlue);
                else if (step < 90)
                    GL.Color3(Color.LightYellow);
                else if (step < 135)
                    GL.Color3(Color.DarkBlue);
                else if (step < 180)
                    GL.Color3(Color.LightYellow);
                else if (step < 225)
                    GL.Color3(Color.DarkBlue);
                else if (step < 270)
                    GL.Color3(Color.LightYellow);
                else if (step < 315)
                    GL.Color3(Color.DarkBlue);
                else if (step < 360)
                    GL.Color3(Color.LightYellow);


                float draw1_x = (float)(radius2 * Math.Cos(step * Math.PI / 180F));
                float draw1_y = (float)(radius2 * Math.Sin(step * Math.PI / 180F));
                GL.Vertex3(draw1_x, vertical2, draw1_y);

                float draw2_x = (float)(radius2 * Math.Cos((step + 180) * Math.PI / 180F));
                float draw2_y = (float)(radius2 * Math.Sin((step + 180) * Math.PI / 180F));
                GL.Vertex3(draw2_x, vertical2, draw2_y);

                GL.Vertex3(draw1_x, vertical2, draw1_y);
                GL.Vertex3(draw2_x, vertical2, draw2_y);
                step += collect;
            }
            step = old_step;
            collect = step;
            GL.End();
        }


        // PERVANE
        private void Propeller(float height, float length, float thickness, float skew)
        {
            float radius = 10, angle = 45.0f;
            GL.Begin(PrimitiveType.Quads);

            GL.Color3(Color.DarkBlue);
            GL.Vertex3(length, height, thickness);
            GL.Vertex3(length, height + skew, -thickness);
            GL.Vertex3(radius * Math.Cos(angle), height + skew, -thickness); // radius ve angle kullanımı
            GL.Vertex3(radius * Math.Sin(angle), height, thickness); // radius ve angle kullanımı

            GL.Color3(Color.DarkBlue);
            GL.Vertex3(-length, height + skew, thickness);
            GL.Vertex3(-length, height, -thickness);
            GL.Vertex3(radius * Math.Cos(angle), height, -thickness); // radius ve angle kullanımı
            GL.Vertex3(radius * Math.Sin(angle), height + skew, thickness); // radius ve angle kullanımı

            GL.Color3(Color.Yellow);
            GL.Vertex3(thickness, height, -length);
            GL.Vertex3(-thickness, height + skew, -length);

            //+
            GL.Vertex3(-thickness, height + skew, 0.0);

            //-
            GL.Vertex3(thickness, height, 0.0);

            GL.Color3(Color.Yellow);
            GL.Vertex3(thickness, height + skew, +length);
            GL.Vertex3(-thickness, height, +length);
            GL.Vertex3(-thickness, height, 0.0);
            GL.Vertex3(thickness, height + skew, 0.0);
            GL.End();
        }





        // !!!!!!!!!!! ANA SAYFADAKİ KISIMLARIÇALIŞTIRMAK İÇİN YANİ GLCONTROL GYRO KISMINI ÇALILTIRMAKİÇİN YORUM SATIRINA ALDIM!!!!!!!!!!!!!
        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            float step = 1.0f;
            float collect = step;
            float radius = 5.0f;
            float vertical1 = radius, vertical2 = -radius;
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Clear(ClearBufferMask.DepthBufferBit);

            Matrix4 perspective = Matrix4.CreatePerspectiveFieldOfView(1.04f, 4 / 3, 1, 10000);
            Matrix4 lookat = Matrix4.LookAt(25, 0, 0, 0, 0, 0, 0, 1, 0);
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.LoadMatrix(ref perspective);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.LoadMatrix(ref lookat);
            GL.Viewport(0, 0, glControl1.Width, glControl1.Height);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            GL.Rotate(x, 1.0, 0.0, 0.0);
            GL.Rotate(y, 0.0, 0.0, 1.0);
            GL.Rotate(z, 0.0, 1.0, 0.0);

            // SİLİNDİR
            cylinder(step, collect, radius, 3, -5);
            cylinder(0.01f, collect, 0.5f, 9, 9.7f);
            cylinder(0.01f, collect, 0.1f, 5, vertical1 + 5);

            // KONİ
            cone(0.01f, 0.01f, radius, 3.0f, 3, 5);
            cone(0.01f, 0.01f, radius, 2.0f, -5.0f, -10.0f);

            // PERVANE
            Propeller(9.0f, 11.0f, 0.2f, 0.5f);


            GL.Begin(PrimitiveType.Lines);

            GL.Color3(Color.FromArgb(250, 0, 0));
            GL.Vertex3(-30.0, 0.0, 0.0);
            GL.Vertex3(30.0, 0.0, 0.0);


            GL.Color3(Color.FromArgb(0, 0, 0));
            GL.Vertex3(0.0, 30.0, 0.0);
            GL.Vertex3(0.0, -30.0, 0.0);

            GL.Color3(Color.FromArgb(0, 0, 250));
            GL.Vertex3(0.0, 0.0, 30.0);
            GL.Vertex3(0.0, 0.0, -30.0);

            GL.End();
            //GraphicsContext.CurrentContext.VSync = true;
            glControl1.SwapBuffers();
        }





        // MPU 6050 GLcontrole çizdirme START BUTONUNA EKLEDİM BU KISMI
        private void Zamanlayici_Tick_1(object sender, EventArgs e)
        {
            /*try
            {
                string[] packet;
                string result = serialPort1.ReadLine();
                packet = result.Split('*');
                //labelxx.Text = packet[0].ToString();
                labelyy.Text = packet[1].ToString();
                labelzz.Text = packet[2].ToString();
                x = Convert.ToInt32(packet[16]);
                y = Convert.ToInt32(packet[17]);
                z = Convert.ToInt32(packet[18]);
                glControl1.Invalidate();
                serialPort1.DiscardInBuffer();
            }

            catch
            {

            }*/
        }






        private void MainView_Load(object sender, EventArgs e)
        {
            // Tüm seri portları görüntülemek için
            string[] ports = SerialPort.GetPortNames();
            foreach (string portName in ports)
            {
                ComboBoxSerialPort.Items.Add(portName);
            }

            // Seri port seçimi
            if (ComboBoxSerialPort.Items.Count > 0)
            {
                // Kullanıcı seçim yapmadıysa, varsayılan olarak ilk portu seç
                if (ComboBoxSerialPort.SelectedItem == null)
                {
                    ComboBoxSerialPort.SelectedIndex = 0;  // İlk portu seç
                }

                // Seri portun açık olup olmadığını kontrol edin
                if (serialPort.IsOpen)
                {
                    serialPort.Close(); // Eğer açıksa, portu kapatın
                }

                serialPort.PortName = ComboBoxSerialPort.SelectedItem.ToString();
            }
            else
            {
                MessageBox.Show("Bağlı bir seri port bulunamadı.");
                return;
            }

            // Baud rate ayarı
            if (int.TryParse(ComboBoxBoundRate.Text, out int baudrate))
            {
                serialPort.BaudRate = baudrate;
            }
            else
            {
                // Varsayılan baud rate değeri (örneğin 115200) belirle
                serialPort.BaudRate = 115200;
                ComboBoxBoundRate.Text = "115200"; // Varsayılan değeri ComboBox'a yaz
            }

            // Seri portu açmayı deneyin
            try
            {
                serialPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Seri port açılamadı: {ex.Message}");
                return;
            }

            // 3D MODELLEME
            GL.ClearColor(Color.DarkGray);
            TimerGorevSuresi.Interval = 1;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Uygulama kapanırken seri portu kapat
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }
        }





        // VERİLERİ AKTARMA KISMI ( BAŞLAMA BUTONU )
        private void StartTransmittingButton_Click(object sender, EventArgs e)
        {
            // Baud rate ayarını kontrol et
            if (int.TryParse(ComboBoxBoundRate.Text, out baudrate))
            {
                serialPort.BaudRate = baudrate;
            }
            else
            {
                MessageBox.Show("Geçersiz baud rate. Lütfen geçerli bir değer girin.");
                return;
            }

            // Port adı seçimini kontrol et
            if (string.IsNullOrEmpty(ComboBoxSerialPort.Text))
            {
                MessageBox.Show("Lütfen geçerli bir seri port seçin.");
                return;
            }

            // Eğer seri port zaten açıksa, önce kapatın
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }

            // Port adını ayarla
            serialPort.PortName = ComboBoxSerialPort.Text;

            // Bağlantı yoksa ve port kapalıysa seri portu aç
            if (!isConnected)
            {
                try
                {
                    if (!serialPort.IsOpen)
                    {
                        serialPort.Open(); // Seri portu aç
                        serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
                        isConnected = true;

                        // Başlatma ve durdurma butonlarını ayarla
                        StartTransmittingButton.Enabled = false;
                        StopTransmittingButton.Enabled = true;

                        MessageBox.Show("Bağlantı başarıyla kuruldu!");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bağlantı kurulamadı: {ex.Message}");
                }
            }

            // Görev süresini başlat
            stopwatch.Start();
            timer.Start();
        }



        //DURDURMA BUTONU
        private void StopTransmittingButton_Click(object sender, EventArgs e)
        {
            if (isConnected)
            {
                try
                {
                    serialPort.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Bağlantıyı kapatırken bir hata oluştu: {ex.Message}");
                }
                isConnected = false;
                Zamanlayici.Stop();
                StartTransmittingButton.Enabled = true;
                StopTransmittingButton.Enabled = false;
                MessageBox.Show("BAĞLANTISI KESİLDİ");

                stopwatch.Stop();
                timer.Stop();
            }
        }





        // EXCEL'E AKTARMA
        private void DownloadCsvReportButton_Click(object sender, EventArgs e)
        {
            try
            {
                Excel.Application app = new Excel.Application();
                app.Visible = true;
                Excel.Workbook kitap = app.Workbooks.Add();
                Excel.Worksheet sayfa = (Excel.Worksheet)kitap.Sheets[1];

                // Sütun başlıklarını yazdırma
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    Excel.Range alan = (Excel.Range)sayfa.Cells[1, i + 1];
                    alan.Value = dataGridView1.Columns[i].HeaderText;
                }

                // Veri yazdırma
                for (int i = 0; i < dataGridView1.Columns.Count; i++)
                {
                    for (int j = 0; j < dataGridView1.Rows.Count; j++)
                    {
                        Excel.Range alan2 = (Excel.Range)sayfa.Cells[j + 2, i + 1]; // 2. satırdan başlayarak
                        alan2.Value = dataGridView1[i, j].Value;
                    }
                }

                MessageBox.Show("Excel dosyası oluşturuldu.", "Tamamlandı", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hata: " + ex.Message);
            }
        }





        // görev süresini saydırma başlama butonuna tıklayınca
        private void TimerGorevSuresi_Tick(object sender, EventArgs e)
        {
            LabelGorevSuresi.Text = stopwatch.Elapsed.ToString(@"hh\:mm\:ss");
        }





        // CAMERA
        private void button1_Click(object sender, EventArgs e)
        {
            stream = new MJPEGStream("http://192.168.0.165"); // YASEMİNİN ip adresi

            stream.NewFrame += new NewFrameEventHandler(video_NewFrame);
            stream.Start();
        }
        void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap FrameData = new Bitmap(eventArgs.Frame);
            pictureBoxCAM.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBoxCAM.Image = FrameData;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            stream.Stop();
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }



        private void BtnAyril_Click(object sender, EventArgs e)
        {
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.WriteLine("SEPARATE"); // Seri porttan SEPARATE komutunu gönder
                    MessageBox.Show("Ayrılma komutu gönderildi.");

                    checkBoxAyrilma.Checked = true;

                    // DataGridView'de "UYDU_STATÜSÜ" sütununu güncelle
                    UpdateUyduStatus("3"); // Burada string türünde bir parametre geçirin
                }
                else
                {
                    MessageBox.Show("Seri port kapalı. Lütfen bağlantıyı kontrol edin.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Komut gönderilirken hata oluştu: " + ex.Message);
            }
        }


        private void UpdateUyduStatus(string status)
        {
            // DataGridView'de "Uydu_Statusu" sütununu güncelleyin
            // Eğer "Uydu_Statusu" sütunu mevcut değilse, DataGridView'e eklemeniz gerekiyor
            if (dataGridView1.Columns["Uydu_Statusu"] == null)
            {
                dataGridView1.Columns.Add("Uydu_Statusu", "Uydu Statüsü");
            }

            // Yeni bir satır ekleyin
            int rowIndex = dataGridView1.Rows.Add(); // Yeni bir satır ekleyin
            dataGridView1.Rows[rowIndex].Cells["Uydu_Statusu"].Value = status; // Uydu statüsünü güncelleyin

            // DataGridView'i güncelleyip en son veriyi eklemeyi sağlamak için
            dataGridView1.Refresh();
        }





        // ARAS

        // Bu Fonksiyon Model uydu iniş hızının 12-14 m/s değerleri arasındaysa paneli yeşil,
        // değil ise paneli kırmızı yapmaktadır.
        private void checkModelUyduInisHizi(double ModelUyduInisHizi)
        {
            if (ModelUyduInisHizi < 12 || ModelUyduInisHizi > 14)
            {
                ArasPanel1.BackColor = Color.Red;
            }
            else
            {
                ArasPanel1.BackColor = Color.Green;
            }
        }
        // Bu Fonksiyon Görev yükü iniş hızının 6-8 m/s değerleri arasındaysa paneli yeşil,
        // değil ise paneli kırmızı yapmaktadır.
        private void checkGorevYukuInisHizi(double GorevYukuInisHizi)
        {
            if (GorevYukuInisHizi < 6 || GorevYukuInisHizi > 8)
            {
                ArasPanel2.BackColor = Color.Red;
            }
            else
            {
                ArasPanel2.BackColor = Color.Green;
            }
        }

        // Bu Fonksiyon Taşıyıcı Basınç verisinin 0'dan büyük olması durumunda paneli yeşil,
        // değil ise kırmızı yapmaktadır.
        // Okuduğumuz veri 0'dan büyükse Basınç verisini okuduğumuz anlamına gelir.
        private void checkTasiyiciBasinciVerisi(double TasiyiciBasinciVerisi)
        {
            if (TasiyiciBasinciVerisi > 0)
            {
                ArasPanel3.BackColor = Color.Green;
            }
            else
            {
                ArasPanel3.BackColor = Color.Red;
            }
        }





        // Bu Fonksiyon Görev Yükü Konum verisinin 0'dan büyük olması durumunda paneli yeşil,
        // değil ise kırmızı yapmaktadır.
        // Okuduğumuz veri 0'dan büyükse Konum verisini okuduğumuz anlamına gelir.
        private void checkGorevYukuKonumVerisi(double GorevYukuKonumVerisi)
        {
            if (GorevYukuKonumVerisi > 0)
            {
                ArasPanel4.BackColor = Color.Green;
            }
            else
            {
                ArasPanel4.BackColor = Color.Red;
            }
        }








        //Bu Fonksiyon 400m (Ayrılmanın Gerçekleşeceği yükseklik)'den sonra
        //İniş Hızımız 12-14 m/s (Taşıyıcıya bağlı olarak) aralığında değilse
        //(Yüksek sapmalar olduğu durumda) ayrılmanın gerçekleşmiş olduğunu anlamaktayız.
        private void checkAyrilmaGerceklesti(double TasiyiciyaBagliHiz)
        {
            if (TasiyiciyaBagliHiz < 12 || TasiyiciyaBagliHiz > 14)
            {
                ArasPanel5.BackColor = Color.Green;
            }
            else
            {
                ArasPanel5.BackColor = Color.Red;
            }
        }


        /* private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
         {
             // DataGridView'inizdeki sütun sayısına göre veri dizileri oluşturun.
             // Her dizi, bir satırı temsil eder ve 21 eleman içermelidir.
             string[] row0 = new string[21] { "30", "Waiting", "00000", "26/08/2024 15:47", "78", "78", "114", "114", "0", "15", "35 C°", "10.5 V", "38,64", "33,45", "114", "3° ", "30°", "20°", "6G4R", "35 C° | %34", "461904" };
             string[] row1 = new string[21] { "29", "Waiting", "00000", "26/08/2024 15:47", "75", "77", "128", "124", "4", "16", "35 C°", "10.5 V", "38,64", "33,45", "128", "4°", "31°", "21°", "4B6G", "35 C° | %34", "461904" };
             string[] row2 = new string[21] { "28", "Waiting", "00000", "26/08/2024 15:47", "76", "78", "142", "134", "8", "14", "35 C°", "10.5 V", "38,64", "33,45", "142", "2°", "29°", "19°", "3R7B", "35 C° | %34", "461904" };
             string[] row3 = new string[21] { "27", "Waiting", "00000", "26/08/2024 15:47", "76", "79", "160", "144", "16", "15", "35 C°", "10.5 V", "38,64", "33,45", "160", "5°", "32°", "24°", "5R5G", "35 C° | %34", "461904" };
             string[] row4 = new string[21] { "26", "Waiting", "00000", "26/08/2024 15:47", "77", "76", "172", "154", "18", "14", "35 C°", "10.5 V", "38,64", "33,45", "172", "3°", "30°", "22°", "5R5G", "35 C° | %34", "461904" };
             string[] row5 = new string[21] { "25", "Waiting", "00000", "26/08/2024 15:47", "76", "76", "188", "164", "24", "16", "35 C°", "10.5 V", "38,64", "33,45", "188", "7°", "34°", "26°", "6R4G", "35 C° | %34", "461904" };
             string[] row6 = new string[21] { "24", "Waiting", "00000", "26/08/2024 15:47", "76", "75", "200", "174", "26", "14", "35 C°", "10.5 V", "38,64", "33,45", "200", "8°", "35°", "27°", "5B5G", "35 C° | %34", "461904" };
             string[] row7 = new string[21] { "23", "Waiting", "00000", "26/08/2024 15:47", "76", "78", "215", "184", "31", "15", "35 C°", "10.5 V", "38,64", "33,45", "215", "10°", "37°", "29°", "4B6G", "35 C° | %34", "461904" };
             string[] row8 = new string[21] { "22", "Waiting", "00000", "26/08/2024 15:47", "76", "77", "230", "194", "36", "15", "35 C°", "10.5 V", "38,64", "33,45", "230", "15°", "42°", "34°", "2R8B", "35 C° | %34", "461904" };
             // Oluşturduğunuz veri dizilerini DataGridView'e satır olarak ekleyin.
             dataGridView1.Rows.Add(row0);
             dataGridView1.Rows.Add(row1);
             dataGridView1.Rows.Add(row2);
             dataGridView1.Rows.Add(row3);
             dataGridView1.Rows.Add(row4);
             dataGridView1.Rows.Add(row5);
             dataGridView1.Rows.Add(row6);
             dataGridView1.Rows.Add(row7);
             dataGridView1.Rows.Add(row8);
         }*/






        //       private void TimerXYZ_Tick_1(object sender, EventArgs e)
        //       {
        //           if (axx == true)
        //           {
        //              if (x < 360)
        //                  x += 5;
        //              else
        //                  x = 0;
        //              LabelX.Text = x.ToString();
        //         }
        //
        //         if (axy == true)
        //         {
        //             if (y < 360)
        //                y += 5;
        //             else
        //                 y = 0;
        //             LabelY.Text = y.ToString();
        //         }
        //
        //         if (axz == true)
        //         {
        //             if (z < 360)
        //                 z += 5;
        //             else
        //                z = 0;
        //            LabelZ.Text = z.ToString();
        //        }
        //         glControl1.Invalidate();
        //    }
        //
        //    private void BtnX_Click(object sender, EventArgs e)
        //    {
        //        if (axx == false)
        //            axx = true;
        //        else
        //             axx = false;
        //         TimerXYZ.Start();
        //     }
        //
        //     private void BtnY_Click(object sender, EventArgs e)
        //     {
        //         if (axy == false)
        //             axy = true;
        //         else
        //             axy = false;
        //         TimerXYZ.Start();
        //     }
        //
        //    private void BtnZ_Click(object sender, EventArgs e)
        //    {
        //        if (axz == false)
        //            axz = true;
        //        else
        //            axz = false;
        //        TimerXYZ.Start();
        //    }



        // ARAS STATÜ DENEMEEE
        /*private void setStatus(string status)
        {
            int intStatus = int.Parse(status);
            switch (intStatus)
            {
                case 0:
                    statuLabel.Text = "Uçuşa Hazır";
                    break;
                case 1:
                    statuLabel.Text = "Yükselme";
                    break;
                case 2:
                    statuLabel.Text = "Uydu İniş";
                    break;
                case 3:
                    statuLabel.Text = "Ayrılma";
                    ayrilmaCheckBox.Checked = true;
                    break;
                case 4:
                    statuLabel.Text = "GörevYükü İniş";
                    break;
                case 5:
                    statuLabel.Text = "Kurtarma";
                    break;
                case 6:
                    statuLabel.Text = "Video Alındı";
                    videoCheckBox.Checked = true;
                    break;
                case 7:
                    statuLabel.Text = "Bonus Görev";
                    bonusCheckBox.Checked = true;
                    break;

            }
        }*/
    }
}
