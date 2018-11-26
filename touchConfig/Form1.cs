using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace touchConfig
{
    public partial class Form1 : Form
    {
        BackgroundWorker worker;

        public Form1()
        {
            InitializeComponent();

            worker = new BackgroundWorker();

            worker.DoWork += DoInterprete;
            worker.ProgressChanged += progressChanged;
            worker.RunWorkerCompleted += RunCompleted;
            worker.WorkerReportsProgress = true;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {

        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "xcfg files (*.xcfg) | *.xcfg";
            if(dialog.ShowDialog() == DialogResult.OK)
            {
                long length = new System.IO.FileInfo(dialog.FileName).Length;
                progressBar1.Maximum = (int)length;
                progressBar1.Value = 0;
                scrollTab.Maximum = 0;
                scrollTab.Minimum = 0;
                scrollTab.Value = 0;

                worker.RunWorkerAsync(dialog.FileName);
            }
        }

        private void DoInterprete(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            string path = (string)e.Argument;
            
            var fileStream = File.OpenRead(path);
            var streamReader = new StreamReader(fileStream, Encoding.UTF8);
            
            String line;
            int current_read = 0;
            int obj_size = 0;
            int size_cnt = 0;
            TabElement tempObj = null;
            string tabName;
            bool start = false;

            while ((line = streamReader.ReadLine()) != null)
            {
                if (line.Length == 0) continue;
                
                current_read = (int)streamReader.BaseStream.Position;
                try
                {
                    if (line.ElementAt<char>(0) == '[')
                    {
                        start = false;
                        string[] spt = line.Split('[', ']', ' ', '_');
                        if(spt.Length >= 3)
                        {
                            for(int cnt=0; cnt<spt.Length; cnt++)
                            {
                                if(spt[cnt].Equals("INSTANCE"))
                                {
                                    tabName = spt[cnt-1] + "_" + spt[cnt + 1];
                                    tempObj = new TabElement(tabName);
                                    start = true;
                                }
                            }
                        }
                        worker.ReportProgress(current_read, null);
                    }
                    else if (start)
                    {
                        if (tempObj != null)
                        {
                            if ((line.Length > 14) && (line.Substring(0, 14).Equals("OBJECT_ADDRESS")))
                            {
                                tempObj.AppendText(line);
                                worker.ReportProgress(current_read, null);
                            }
                            else if ((line.Length > 11) && (line.Substring(0, 11).Equals("OBJECT_SIZE")))
                            {
                                tempObj.AppendText(line);
                                obj_size = Convert.ToInt32(line.Split('=')[1]);
                                size_cnt = 0;
                                worker.ReportProgress(current_read, null);
                            }
                            else
                            {
                                string[] datas = line.Split(' ', '=');

                                Int32 length = Convert.ToInt32(datas[1]);
                                Int32 data = Convert.ToInt32(datas[3]);
                                size_cnt += length;

                                byte[] bytes = BitConverter.GetBytes(data);

                                for (Int32 cnt = 0; cnt < length; cnt++)
                                {
                                    tempObj.AppendText(bytes[cnt].ToString("X2"));
                                }

                                if (size_cnt == obj_size)
                                {
                                    worker.ReportProgress(current_read, tempObj);
                                }
                                else
                                {
                                    worker.ReportProgress(current_read, null);
                                }
                            }
                        }
                    }
                    else
                    {
                        worker.ReportProgress(current_read, null);
                    }
                }
                catch
                {
                    MessageBox.Show("err22");
                }
            }

            worker.ReportProgress((int)streamReader.BaseStream.Position, null);
            streamReader.Close();
            fileStream.Close();
        }

        private void progressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if(e.UserState != null)
            {
                try
                {
                    TabElement element = (TabElement)e.UserState;
                    RichTextBox rtbTemp = new RichTextBox();
                    if (element.strList.Count > 0)
                    {
                        string[] strArr = element.strList.ToArray();
                        rtbTemp.AppendText(strArr[0]);
                        rtbTemp.AppendText("\r\n");
                        rtbTemp.AppendText(strArr[1]);
                        rtbTemp.AppendText("\r\n");

                        int start_idx = 2;
                        
                        while(start_idx + 16 < strArr.Length-1)
                        {
                            rtbTemp.AppendText(String.Join(" ", strArr, start_idx, 16));
                            rtbTemp.AppendText("\r\n");
                            start_idx += 16;
                        }

                        int rest = strArr.Length - start_idx;

                        if(rest > 0)
                        {
                            rtbTemp.AppendText(String.Join(" ", strArr, start_idx, rest));
                        }
                    }
                    tabResult.TabPages.Add(element.tabName, element.tabName);
                    tabResult.TabPages[element.tabName].Controls.Add(rtbTemp);
                    rtbTemp.Dock = DockStyle.Fill;

                    scrollTab.Maximum = tabResult.TabPages.Count;
                }
                catch
                {
                    MessageBox.Show("err");
                }
            }
            progressBar1.Value = e.ProgressPercentage;
        }

        private void RunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                MessageBox.Show("Canceled");
            }
            else if (e.Error != null)
            {
                MessageBox.Show("Error: " + e.Error.Message);
            }
            else
            {
                MessageBox.Show("complete");
            }
        }

        private void hScrollBar1_ValueChanged(object sender, EventArgs e)
        {
            if(tabResult.TabPages.Count > scrollTab.Value)
            {
                tabResult.SelectTab(scrollTab.Value);
            }
        }
    }

    class TabElement
    {
        public string tabName;
        public List<string> strList;

        public TabElement(string name)
        {
            tabName = name;
            strList = new List<string>();
        }

        public void AppendText(string text)
        {
            strList.Add(text);
        }
    }

}
