using Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        Socket socketSend;

        /// <summary>
        /// 读取配置文件中服务端IP地址
        /// </summary>
        string ipAddress = ConfigurationManager.AppSettings["ServerIpAddress"];

        /// <summary>
        /// 读取配置文件中更新完成需打开的文件名
        /// </summary>
        string openfileName = @"\" + ConfigurationManager.AppSettings["OpenFileName"];

        /// <summary>
        /// 文件暂存的路径
        /// </summary>
        string filePath = null;

        /// <summary>
        /// 待更新程序根目录
        /// </summary>
        string root = null;

        /// <summary>
        /// 文件的名字
        /// </summary>
        string name = null;

        /// <summary>
        /// 文件的长度
        /// </summary>
        string length = null;

        /// <summary>
        /// 实例化AutoResetEvent
        /// </summary>
        AutoResetEvent autoEvent = new AutoResetEvent(false);

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        /// <summary>
        /// 连接服务端
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                socketSend = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Parse(ipAddress);//连接服务端IP
                IPEndPoint port = new IPEndPoint(ip, 5555);
                socketSend.Connect(port);
                MessageBox.Show("Connect success!");
                //开启MsgAccept线程
                Thread td1 = new Thread(MsgAccept);
                td1.IsBackground = true;
                td1.Start();
                //发送更新请求
                Button_Click(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                this.Close();
            }
        }

        /// <summary>
        /// 接受文件详细信息
        /// </summary>
        private void MsgAccept()
        {
            try
            {
                ThreadStart td = new ThreadStart(delegate { AcceptMgs(Convert.ToInt64(length),richtextbox1); });
                while (true)
                {
                    byte[] buffer = new byte[1024 * 1024 * 1];
                    int r = socketSend.Receive(buffer);
                    if (r == 0)
                    {
                        break;
                    }
                    string fileMsg = Encoding.ASCII.GetString(buffer, 0, r);
                    if (fileMsg == "110") //如果接收的字节数组的第一个字节是0，说明接收的字符串信息
                    {
                        int n = 0;
                        int asc = (int)'m';
                        byte[] array = Encoding.ASCII.GetBytes(asc.ToString());
                        //发包请求服务端发送下一个包
                        socketSend.Send(array);
                        do
                        {
                            //收到名字包
                            n = socketSend.Receive(buffer);
                        } while (r == n);
                        name = Encoding.UTF8.GetString(buffer, 0, n);
                        //发包请求服务端发送下一个包
                        socketSend.Send(array);
                        do
                        {
                            //收到长度包
                            n = socketSend.Receive(buffer);
                        } while (r == n);
                        length = Encoding.UTF8.GetString(buffer, 0, n);
                        socketSend.Send(array);//发包请求服务端发送文件
                        td.Invoke();//启动AcceptMgs线程
                        autoEvent.WaitOne();//暂停MsgAccept线程
                    }
                    //判断是否收到文件传完信息
                    if (fileMsg == "120")
                    {
                        Dispatcher.Invoke(new Action(delegate
                        {
                            lbl_downloadname.Content = "更新已完成";
                        }));
                        MessageBoxResult result = MessageBox.Show("更新完成", "是否重新打开该应用程序", MessageBoxButton.OKCancel);
                        if (result==MessageBoxResult.OK)
                        {
                            System.Diagnostics.Process.Start(root + openfileName);
                            Dispatcher.Invoke(new Action(delegate
                            {
                                this.Close();
                            }));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// 接收文件
        /// </summary>
        /// <param name="length"></param>
        /// <param name="richTextBox"></param>
        private void AcceptMgs(long length,RichTextBox richTextBox)
        {
            double max = length;
            double current = 0;
            while (true)
            {
                byte[] buffer = new byte[1024 * 1024 * 20];
                int r = socketSend.Receive(buffer);
                //获取更新程序目录上一级目录，即待更新程序根目录
                string path = Environment.CurrentDirectory;
                DirectoryInfo pathInfo = new DirectoryInfo(path);
                root = pathInfo.Parent.FullName;
                //判断是否存在temp文件夹，如果不存在就创建temp文件夹
                if (System.IO.Directory.Exists(Environment.CurrentDirectory + @"\temp") == false)
                {
                    System.IO.Directory.CreateDirectory(Environment.CurrentDirectory + @"\temp");
                }
                //暂存路径
                filePath = Environment.CurrentDirectory + @"\temp";
                //完整存放路径
                string fullPath = System.IO.Path.Combine(filePath, name);
                //计算下载量
                current += r;
                double value = (current / max) * 100;
                //判断是否收到包
                if (r == 0)
                {
                    continue;
                }
                //判断和大文件是否已经保存完
                if (length > 0)
                {
                    //显示并给label赋值下载进度
                    Dispatcher.Invoke(new Action(delegate
                    {
                        lbl1.Visibility = Visibility.Visible;
                        lbl_download.Visibility = Visibility.Visible;
                        lbl_downloadname.Content = $"正在下载：{name}";
                        lbl_download.Content = value.ToString().Split('.')[0] + "%";
                    }));
                    //Debug.Print(value.ToString());
                    //Debug.Print($"{current}/{max}");
                    //进度调显示
                    pbBar.Dispatcher.Invoke(new Action<System.Windows.DependencyProperty, object>(pbBar.SetValue), System.Windows.Threading.DispatcherPriority.Background, ProgressBar.ValueProperty, value);
                    //保存接收的文件
                    using (FileStream fsWrite = new FileStream(fullPath, FileMode.Append, FileAccess.Write))
                    {
                        fsWrite.Write(buffer, 0, r);
                        length -= r;
                        if (length <= 0)
                        {
                            string download = (name + ": 接收文件成功");
                            new Thread(() => {
                                this.Dispatcher.Invoke(new Action(() => {
                                    Utils.Show(richTextBox, download);
                                }));
                            }).Start();
                            fsWrite.Close();
                            //判断是否存在此文件，如果存在则删除原有文件
                            if (System.IO.File.Exists(root+$@"\{name}"))
                            {
                                File.Delete(root + $@"\{name}");
                            }
                            //将暂存文件夹中文件移到根文件夹
                            File.Move(fullPath, root + $@"\{name}");

                            int asc = (int)'m';
                            byte[] array = Encoding.ASCII.GetBytes(asc.ToString());
                            //隐藏下载进度label
                            Dispatcher.Invoke(new Action(delegate
                            {
                                lbl1.Visibility = Visibility.Collapsed;
                                lbl_download.Visibility = Visibility.Collapsed;
                            }));
                            autoEvent.Set();//继续MsgAccept线程
                            socketSend.Send(array);//发包请求服务端发送下一个文件包
                            break;
                        }
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// 发送更新请求 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string str = "gx";
            string ASCIIstr = null;
            byte[] array = Encoding.ASCII.GetBytes(str);
            for (int i = 0; i < array.Length; i++)
            {
                int asciicode = (int)(array[i]);
                ASCIIstr += Convert.ToString(asciicode);
            }
            byte[] test = Encoding.ASCII.GetBytes(ASCIIstr.ToString());
            socketSend.Send(test);
        }

        /// <summary>
        /// 移动窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Border_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        /// <summary>
        /// 关闭窗体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// 鼠标移到Label上后文本变红
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lbl_close_MouseEnter(object sender, MouseEventArgs e)
        {
            Color color = (Color)ColorConverter.ConvertFromString("Red");
            Brush brush = new SolidColorBrush(color);
            lbl_close.Foreground = brush;
        }

        /// <summary>
        /// 鼠标移出Label后文本变回原来颜色
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Lbl_close_MouseLeave(object sender, MouseEventArgs e)
        {
            Color color = (Color)ColorConverter.ConvertFromString("Black");
            Brush brush = new SolidColorBrush(color);
            lbl_close.Foreground = brush;
        }
    }
}
