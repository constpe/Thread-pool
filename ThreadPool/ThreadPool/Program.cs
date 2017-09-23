using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ThreadPool;
using Microsoft.Win32.SafeHandles;

namespace ThreadPool
{
    class Program
    {
       
        class CopyInfo
        {
            public string source;
            public string dest;

            public CopyInfo(string source, string dest)
            {
                this.source = source;
                this.dest = dest;
            }
        }

        class CopyFilePartInfo
        {
            public string source;
            public string dest;
            public FileStream writer;
            public long offset;
            public long length;

            public CopyFilePartInfo(string source, string dest, FileStream writer, long offset, long length)
            {
                this.source = source;
                this.dest = dest;
                this.writer = writer;
                this.offset = offset;
                this.length = length;
            }
        }

        static ThreadPool threadPool;
        private delegate void CopyDelegate(CopyInfo copyInfo);

        private static void DoSmth(object o)
        {
            int a = 0;
            for (int i = 0; i < 1000000; i++)
                for (int j = 0; j < 100; j++)
                    a = i + j;
                    Console.WriteLine(a);                 
        }

        private static void CopyFile(object copyInfo)
        {
            FileStream reader = new FileStream(((CopyInfo)copyInfo).source, FileMode.Open);
            BinaryWriter writer = new BinaryWriter(File.Create(((CopyInfo)copyInfo).dest));
            
            int symbol;
            while ((symbol = reader.ReadByte()) != -1)
            {
                writer.Write((byte)symbol);
            }

            reader.Close();
            writer.Close();
        }

        private static void CopyDir(string source, string dest)
        {
            try
            {
                string[] directories = Directory.GetDirectories(source);
                
                foreach (string directory in directories)
                {
                    string path = dest + "\\" + Path.GetFileName(directory);
                    Directory.CreateDirectory(path);
                    CopyDir(directory, path);
                }

                string[] files = Directory.GetFiles(source);

                foreach (string file in files)
                {
                    ParameterizedThreadStart CopyDelegate = CopyFile;
                    threadPool.AddTask(CopyDelegate, new CopyInfo(file, dest + "\\" + Path.GetFileName(file)));
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Directory {0} doesn't exists", source);
            }
        }

        static object locker = new object();

        private static void CopyFilePart(object param)
        {
            FileStream reader = new FileStream(((CopyFilePartInfo)param).source, FileMode.Open, FileAccess.Read);
            FileStream writer = ((CopyFilePartInfo)param).writer;

            long bytesLeft = ((CopyFilePartInfo)param).length;
            reader.Seek(((CopyFilePartInfo)param).offset, SeekOrigin.Begin);
            byte[] buffer = new byte[5000000];

            while(bytesLeft > 0)
            {
                if (bytesLeft > buffer.Length)
                {
                    reader.Read(buffer, 0, buffer.Length);
                    lock (locker)
                    {
                        if (((CopyFilePartInfo)param).offset == 0)
                        {
                            int a = 42;
                        }
                        writer.Seek(((CopyFilePartInfo)param).offset + ((CopyFilePartInfo)param).length - bytesLeft, SeekOrigin.Begin);
                        writer.Write(buffer, 0, buffer.Length);
                        writer.Flush();
                    }  
                    bytesLeft -= buffer.Length;
                }
                else
                {
                    reader.Read(buffer, 0, (int)bytesLeft);
                    lock (locker)
                    {
                        writer.Seek(((CopyFilePartInfo)param).offset + ((CopyFilePartInfo)param).length - bytesLeft, SeekOrigin.Begin);
                        writer.Write(buffer, 0, (int)bytesLeft);
                        writer.Flush();
                    }  
                    bytesLeft = 0;
                }           
            }       
        }

        static void CopyLargeFile(string source, string dest, int parts)
        {
            FileInfo srcInfo = new FileInfo(source);
            FileStream writer = new FileStream(dest, FileMode.Create);

            long size = srcInfo.Length;
            long partLength = size / parts;

            for (int i = 0; i < parts && partLength > 0; i++)
            {
                long offset = i * partLength; 
                threadPool.AddTask(CopyFilePart, new CopyFilePartInfo(source, dest, writer, offset, partLength));
            }

            if (size % parts != 0)
            {
                threadPool.AddTask(CopyFilePart, new CopyFilePartInfo(source, dest, writer, partLength * parts, size % parts));
            }
        }

        static void Main(string[] args)
        {
            bool isWork = true;

            while (isWork)
            {
                string option = Console.ReadLine();
                string[] optionInfo = option.Split('|');

                switch(optionInfo[0])
                {
                    case "thread":
                        if (optionInfo.Length == 2)
                        {
                            try
                            {
                                threadPool = new ThreadPool(int.Parse(optionInfo[1]));
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("<thread_count> must be a number");
                            }
                        }
                        else if (optionInfo.Length == 3)
                        {
                            try
                            {
                                threadPool = new ThreadPool(int.Parse(optionInfo[1]), int.Parse(optionInfo[2]));
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("<min_thread_count> and <max_thread_count> must be a number");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Usage: thread <thread_count> | <min_thread_count> <max_thread_count>");
                        }

                        break;
                    case "copydir":
                        if (optionInfo.Length != 3)
                        {
                            Console.WriteLine("Usage: copydir <src_path> <dest_path>");
                        }
                        else if (threadPool != null)
                        {
                            CopyDir(optionInfo[1], optionInfo[2]);
                        }
                        else
                        {
                            Console.WriteLine("Thread pool wasn't created");
                        }

                        break;
                    case "copyfile":
                        if (optionInfo.Length != 4)
                        {
                            Console.WriteLine("Usage: copyfile <src_path> <dest_path> <file_parts>");
                        }
                        else if (threadPool != null)
                        {
                            try
                            {
                                CopyLargeFile(optionInfo[1], optionInfo[2], int.Parse(optionInfo[3]));
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("<file_parts> must be a number");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Thread pool wasn't created");
                        }

                        break;
                    case "exit":
                        if (threadPool != null)
                            threadPool.Clear();
                        isWork = false;

                        break;
                    default:
                        Console.WriteLine("Command {0} not found", optionInfo[0]);
                        break;
                }
            }
        }
    }
}
