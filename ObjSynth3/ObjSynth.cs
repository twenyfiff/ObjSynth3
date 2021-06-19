using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Media;
using System.IO;
using Microsoft.VisualBasic;
using System.Data.SqlClient;
using System.Configuration;

namespace ObjSynth3
{
    public partial class ObjSynth : Form
    {
        private const int SAMPLE_RATE = 44100;
        private const short BITS_PER_SAMPLE = 16;
        public ObjSynth()
        {
            InitializeComponent();
        }

        private void ObjSynth_KeyDown(object sender, KeyEventArgs e)
        {
            Random random = new Random();
            short[] wave = new short[SAMPLE_RATE];
            byte[] binaryWave = new byte[SAMPLE_RATE * sizeof(short)];
            float frequency;
            switch (e.KeyCode)
            {
                case Keys.Z:
                    frequency = 65.4f;                              //C2
                    break;
                case Keys.X:
                    frequency = 130.813f;                           //C3
                    break;
                case Keys.C:
                    frequency = 261.62f;                            //C4
                    break;
                case Keys.V:
                    frequency = 523.25f;                            //C5
                    break;
                case Keys.B:
                    frequency = 1046.5f;                            //C6
                    break;
                case Keys.N:
                    frequency = 2093f;                              //C7
                    break;
                case Keys.M:
                    frequency = 4186.01f;                           //C8
                    break;
                default:
                    return;
            }
            foreach (Oscillator oscillator in this.Controls.OfType<Oscillator>())
            {
                int samplesPerWaveLength = (int)(SAMPLE_RATE / frequency);
                short ampStep = (short)((short.MaxValue * 2) / samplesPerWaveLength);
                short tempSample;
                switch (oscillator.WaveForm)
                {
                    case WaveForm.Sine:
                        for (int i = 0; i < SAMPLE_RATE; i++)
                        {
                            wave[i] = Convert.ToInt16(short.MaxValue * Math.Sin(((Math.PI * 2 * frequency) / SAMPLE_RATE) * i));
                        }
                        break;
                    case WaveForm.Square:
                        for (int i = 0; i < SAMPLE_RATE; i++)
                        {
                            wave[i] = Convert.ToInt16(short.MaxValue * Math.Sign(Math.Sin((Math.PI * 2 * frequency) / SAMPLE_RATE * i)));
                        }
                        break;
                    case WaveForm.Saw:
                        for (int i = 0; i < SAMPLE_RATE; i++)
                        {
                            tempSample = -short.MaxValue;
                            for (int j = 0; j < samplesPerWaveLength && i < SAMPLE_RATE; j++)
                            {
                                tempSample += ampStep;
                                wave[i++] = Convert.ToInt16(tempSample);
                            }
                        }
                        break;
                    case WaveForm.Triangle:
                        tempSample = -short.MaxValue;
                        for (int i = 0; i < SAMPLE_RATE; i++)
                        {
                            if (Math.Abs(tempSample + ampStep) > short.MaxValue)
                            {
                                ampStep = (short)-ampStep;
                            }
                            tempSample += ampStep;
                            wave[i] = Convert.ToInt16(tempSample);
                        }
                        break;
                    case WaveForm.Noise:
                        for (int i = 0; i < SAMPLE_RATE; i++)
                        {
                            wave[i] = (short)random.Next(-short.MaxValue, short.MaxValue);
                        }
                        break;
                }
            }
            Buffer.BlockCopy(wave, 0, binaryWave, 0, wave.Length * sizeof(short));
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                short blockAlign = BITS_PER_SAMPLE / 8;
                int subChunkTwoSize = SAMPLE_RATE * 1 * blockAlign;
                binaryWriter.Write(new[] { 'R', 'I', 'F', 'F' });
                binaryWriter.Write(36 + subChunkTwoSize);
                binaryWriter.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                binaryWriter.Write(16);
                binaryWriter.Write((short)1);
                binaryWriter.Write((short)1);
                binaryWriter.Write(SAMPLE_RATE);
                binaryWriter.Write(SAMPLE_RATE * blockAlign);
                binaryWriter.Write(blockAlign);
                binaryWriter.Write(BITS_PER_SAMPLE);
                binaryWriter.Write(new[] { 'd', 'a', 't', 'a' });
                binaryWriter.Write(subChunkTwoSize);
                binaryWriter.Write(binaryWave);
                memoryStream.Position = 0;
                new SoundPlayer(memoryStream).Play();
            }
        }

        public void Insert(string fileName, byte[] image)
        {
            using (SqlConnection cn = new SqlConnection(ConfigurationManager.ConnectionStrings["ObjSynth3.Properties.Settings.DatabaseConnectionString"].ConnectionString))
            {
                if (cn.State == ConnectionState.Closed)
                    cn.Open();
                using(SqlCommand cmd = new SqlCommand("insert into pictures(filename, image) values(@filename, @image)", cn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.AddWithValue("@filename", textBox1.Text);
                    cmd.Parameters.AddWithValue("@image", image);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        byte[] ConvertImageToBytes(Image img)
        {
            using(MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.ToArray();
            }
        }

        public Image ConvertByteArrayToImage(byte[] data)
        {
            using(MemoryStream ms = new MemoryStream(data))
            {
                return Image.FromStream(ms);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using(OpenFileDialog ofd = new OpenFileDialog() { Filter="Image files(*.jpg;*.jpeg)|*.jpg;*.jpeg", Multiselect = false })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    pictureBox1.Image = Image.FromFile(ofd.FileName);
                    Insert(textBox1.Text, ConvertImageToBytes(pictureBox1.Image));
                }
            }
        }
    }
    public enum WaveForm
    {
        Sine, Square, Saw, Triangle, Noise
    }
}
