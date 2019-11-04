using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Core
{
    public class Currenty
    {
        /// <summary>
        /// 定义全局ip变量
        /// </summary>
        public static string IP;

        /// <summary>
        /// 将文件名以及特征码写入config.txt并保存在文件路径中
        /// </summary>
        /// <param name="path">路径</param>
        public void getFileName(string path)
        {

            MD5 md5 = new MD5CryptoServiceProvider();
            StringBuilder sb = new StringBuilder();
            DirectoryInfo root = new DirectoryInfo(path);
            var fileInfo = "";
            foreach (var r in root.GetFiles())
            {
                Debug.Print(r.Name);
                FileStream file = new FileStream(path + @"\" + r.Name, FileMode.Open);
                byte[] retVal = md5.ComputeHash(file);
                file.Close();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                fileInfo += r.Name + "(" + sb.ToString() + ")" + "\n";
            }
            File.WriteAllText(path + @"\config.txt", fileInfo);
        }

        /// <summary>
        /// 读取config.txt中信息
        /// </summary>
        /// <param name="path">路径</param>
        public ArrayList Readtxt(string path)
        {
            /// <summary>
            /// 定义存放对比文件的集合
            /// </summary>
            ArrayList arr = new ArrayList();
            //config文件的路径
            var file_list = File.ReadLines(path + @"\config.txt", Encoding.Default);
            foreach (var item in file_list)
            {
                arr.Add(item.Trim());
            }
            return arr;
        }


        public ArrayList Readfilename(string path)
        {
            /// <summary>
            /// 定义存放文件名的集合
            /// </summary>
            ArrayList arr = new ArrayList();
            DirectoryInfo root = new DirectoryInfo(path);
            foreach (var r in root.GetFiles())
            {
                arr.Add(r.Name);
            }
            return arr;
        }
    }
}
