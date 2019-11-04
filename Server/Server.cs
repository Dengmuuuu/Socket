using Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Configuration;

namespace Server
{
    public partial class Server : Form
    {
        /// <summary>
        /// 实例化Currenty类
        /// </summary>
        Currenty cur = new Currenty();
        /// <summary>
        /// 用来存放连接服务的客户端的IP地址和端口号，对应的Socket
        /// </summary>
        Dictionary<string, Socket> dicSocket = new Dictionary<string, Socket>();
        /// <summary>
        /// 存放IP的集合
        /// </summary>
        ArrayList ip_list = new ArrayList();
        /// <summary>
        /// 存放对比文件的集合
        /// </summary>
        ArrayList arr = new ArrayList();
        /// <summary>
        /// 读取配置文件中服务端监听端口
        /// </summary>
        string listenport = ConfigurationManager.AppSettings["ListenPort"];

        public string name = null;

        public Server()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 接受客户端发送的消息
        /// </summary>
        /// <param name="o"></param>
        private void AcceptMgs(object o)
        {
            try
            {
                Socket socketWatc = (Socket)o;
                while (true)
                {
                    //负责跟客户端通信的Socket
                    Socket socketSend = socketWatc.Accept();
                    //将远程连接的客户端的IP地址和Socket存入集合中
                    dicSocket.Add(socketSend.RemoteEndPoint.ToString(), socketSend);
                    ip_list.Add(socketSend.RemoteEndPoint.ToString());
                    richTextBox1.Text += socketSend.RemoteEndPoint.ToString() + ": Connect Success!" + "\n";
                    //新建线程循环接收客户端发来的信息
                    Thread td = new Thread(Recive);
                    td.IsBackground = true;
                    td.Start(socketSend);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 接收客户端发来的数据，并显示出来
        /// </summary>
        /// <param name="o"></param>
        private void Recive(object o)
        {
            Socket socketSend = (Socket)o;
            try
            {
                var path = Environment.CurrentDirectory + @"\UpdateFile";
                DirectoryInfo root = new DirectoryInfo(path);
                name = Path.GetFileName(path);

                byte[] buffer = new byte[1024 * 1024 * 2];
                int r = socketSend.Receive(buffer);

                string strMsg = Encoding.UTF8.GetString(buffer, 0, r);

                if (strMsg == "103120")
                {
                    var last = root.GetFiles().Last();//获取更新文件夹下最后一个文件名
                    foreach (var file in root.GetFiles())
                    {
                        string name = file.Name;
                        int n = 0;
                        //发送文件
                        FileStream fsRead = new FileStream(path + @"\" + name, FileMode.Open, FileAccess.Read);
                        //110
                        int asc = (int)'n';
                        byte[] array = Encoding.ASCII.GetBytes(asc.ToString());
                        socketSend.Send(array);
                        //是否收到110包
                        do
                        {
                            n = socketSend.Receive(buffer);
                            Debug.Print(n.ToString());
                        } while (r == n);
                        if (Encoding.ASCII.GetString(buffer, 0, n).ToString() == "109")
                        {
                            //名字包
                            byte[] buffer_name = Encoding.UTF8.GetBytes(name);
                            socketSend.Send(buffer_name);
                            n = 0;
                            //是否收到名字包
                            do
                            {
                                n = socketSend.Receive(buffer);
                                Debug.Print(n.ToString());
                            } while (r == n);
                            if (Encoding.ASCII.GetString(buffer, 0, n).ToString() == "109")
                            {
                                //长度包
                                long length = fsRead.Length;
                                byte[] byteLength = Encoding.UTF8.GetBytes(length.ToString());
                                socketSend.Send(byteLength);
                                n = 0;
                                //是否收到长度包
                                do
                                {
                                    n = socketSend.Receive(buffer);
                                } while (r == n);
                                if (Encoding.ASCII.GetString(buffer, 0, n).ToString() == "109")
                                {
                                    byte[] buffer_file = new byte[1024 * 1024 * 2];
                                    long send = 0; //发送的字节数     
                                    while (true)  //大文件断点多次传输
                                    {
                                        int a = fsRead.Read(buffer_file, 0, buffer_file.Length);
                                        if (a == 0)
                                        {
                                            break;
                                        }
                                        socketSend.Send(buffer_file, 0, a, SocketFlags.None);
                                        send += a;
                                        richTextBox1.Text += (string.Format("{0}: 已发送：{1}/{2}", name, send, length)) + "\n";
                                    }
                                    richTextBox1.Text += ("发送完成");
                                    n = 0;
                                    //是否收到完整文件
                                    do
                                    {
                                        n = socketSend.Receive(buffer);
                                    } while (r == n);
                                    if (Encoding.ASCII.GetString(buffer, 0, n).ToString() == "109")
                                    {
                                       //判断文件是否传完
                                        if (file.ToString() == last.ToString())
                                        {
                                            int end = (int)'x';
                                            byte[] end_array = Encoding.ASCII.GetBytes(end.ToString());
                                            //发包告诉客户端文件已发送完毕
                                            socketSend.Send(end_array);
                                        }
                                        else continue;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 开始监听
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                CheckForIllegalCrossThreadCalls = false;
                Socket socketWatch = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPAddress ip = IPAddress.Any;
                IPEndPoint port = new IPEndPoint(ip, Convert.ToInt32(listenport));
                socketWatch.Bind(port);
                socketWatch.Listen(10);
                //新建线程，去接收客户端发来的信息
                Thread td = new Thread(AcceptMgs);
                td.IsBackground = true;
                td.Start(socketWatch);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
