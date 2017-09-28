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
        struct CopyInfo
        {
            public string source;
            public string dest;

            public CopyInfo(string source, string dest)
            {
                this.source = source;
                this.dest = dest;
            }
        }

        struct CopyFilePartInfo
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

        private static ThreadPool threadPool;
        private const int DEFAULT_BUFFER_SIZE = 5000000;
        private delegate void CopyDelegate(CopyInfo copyInfo);

        private static void CopyFile(object copyInfo)
        {
            string source = ((CopyInfo)copyInfo).source;
            string dest = ((CopyInfo)copyInfo).dest;
            FileInfo fileInfo = new FileInfo(source);
            long bytesCount = fileInfo.Length;
            byte[] buffer = new byte[DEFAULT_BUFFER_SIZE];
            FileStream reader = null;
            FileStream writer = null;

            try
            {
                reader = new FileStream(source, FileMode.Open);
                writer = new FileStream(dest, FileMode.Create);

                while (bytesCount > 0)
                {
                    if (bytesCount > buffer.Length)
                    {
                        reader.Read(buffer, 0, buffer.Length);
                        writer.Write(buffer, 0, buffer.Length);
                        writer.Flush();
                        bytesCount -= buffer.Length;
                    }
                    else
                    {
                        reader.Read(buffer, 0, (int)bytesCount);
                        writer.Write(buffer, 0, (int)bytesCount);
                        bytesCount = 0;
                    }
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Error during directory copying. Directory not copied");
                threadPool.Clear();
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    writer.Close();
            }
        }

        private static void CopyDir(string source, string dest)
        {
            source = Path.GetFullPath(source);
            dest = Path.GetFullPath(dest);
            string dirNotFound = source;

            try
            {
                if (!Directory.Exists(dest))
                {
                    dirNotFound = dest;
                    throw new DirectoryNotFoundException();
                }

                string[] files = Directory.GetFiles(source);

                foreach (string file in files)
                {
                    ParameterizedThreadStart CopyDelegate = CopyFile;
                    threadPool.AddTask(CopyDelegate, new CopyInfo(file, dest + "\\" + Path.GetFileName(file)));
                }

                string[] directories = Directory.GetDirectories(source);
                
                foreach (string directory in directories)
                {
                    string path = dest + "\\" + Path.GetFileName(directory);
                    Directory.CreateDirectory(path);
                    CopyDir(directory, path);
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Directory {0} doesn't exists", dirNotFound);
            }

            while (threadPool.HasTasks());
        }

        private static readonly object locker = new object();

        private static void CopyFilePart(object param)
        {
            string source = ((CopyFilePartInfo)param).source;
            long offset = ((CopyFilePartInfo)param).offset;
            long length = ((CopyFilePartInfo)param).length;
            FileStream writer = ((CopyFilePartInfo)param).writer;
            FileStream reader = null;

            try
            {
                reader = new FileStream(source, FileMode.Open, FileAccess.Read);

                reader.Seek(offset, SeekOrigin.Begin);
                long bytesCount = length;
                byte[] buffer = new byte[DEFAULT_BUFFER_SIZE];

                while (bytesCount > 0)
                {
                    if (bytesCount > buffer.Length)
                    {
                        reader.Read(buffer, 0, buffer.Length);
                        lock (locker)
                        {
                            writer.Seek(offset + length - bytesCount, SeekOrigin.Begin);
                            writer.Write(buffer, 0, buffer.Length);
                            writer.Flush();
                        }

                        bytesCount -= buffer.Length;
                    }
                    else
                    {
                        reader.Read(buffer, 0, (int)bytesCount);
                        lock (locker)
                        {
                            writer.Seek(offset + length - bytesCount, SeekOrigin.Begin);
                            writer.Write(buffer, 0, (int)bytesCount);
                            writer.Flush();
                        }

                        bytesCount = 0;
                    }
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Error during copying file {0}. File {0} not copied", source);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }          
        }

        static void CopyLargeFile(string source, string dest, int parts)
        {
            FileInfo srcInfo = new FileInfo(source);

            long size = srcInfo.Length;
            long partLength = size / parts;

            using (FileStream writer = new FileStream(dest, FileMode.Create))
            {
                for (int i = 0; i < parts && partLength > 0; i++)
                {
                    long offset = i * partLength;
                    threadPool.AddTask(CopyFilePart, new CopyFilePartInfo(source, dest, writer, offset, partLength));
                }

                if (size % parts != 0)
                {
                    threadPool.AddTask(CopyFilePart, new CopyFilePartInfo(source, dest, writer, partLength * parts, size % parts));
                }

                while (threadPool.HasTasks());
            }
        }

        static string[] ParseCommand(string command)
        {
            string commandName = "";

            int i = 0;
            while (command[i] != ' ')
            {
                commandName += command[i];
                i++;
            }

            command = command.Remove(0, i);
            string[] commandParams = command.Split(',');
            string[] commandInfo = new string[commandParams.Length + 1];
            commandInfo[0] = commandName;
            Array.Copy(commandParams, 0, commandInfo, 1, commandParams.Length);

            return commandInfo;
        }

        static void Main(string[] args)
        {
            bool isWork = true;

            while (isWork)
            {
                string command = Console.ReadLine();
                if (command != "")
                {
                    string[] commandInfo = ParseCommand(command);

                    switch (commandInfo[0])
                    {
                        case "thread":
                            if (commandInfo.Length == 2)
                            {
                                try
                                {
                                    threadPool = new ThreadPool(int.Parse(commandInfo[1]));
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("<thread_count> must be a number");
                                }
                            }
                            else if (commandInfo.Length == 3)
                            {
                                try
                                {
                                    threadPool = new ThreadPool(int.Parse(commandInfo[1]), int.Parse(commandInfo[2]));
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("<min_thread_count> and <max_thread_count> must be a number");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Usage: thread <thread_count> | <min_thread_count>,<max_thread_count>");
                            }

                            break;
                        case "copydir":
                            if (commandInfo.Length != 3)
                            {
                                Console.WriteLine("Usage: copydir <src_path>,<dest_path>");
                            }
                            else if (threadPool != null)
                            {
                                try
                                {
                                    CopyDir(commandInfo[1], commandInfo[2]);
                                    Console.WriteLine("Directory {0} was succesfully copied to directory {1}", commandInfo[1], commandInfo[2]);
                                }
                                catch (DirectoryNotFoundException) { }
                                catch (FileNotFoundException e)
                                {
                                    Console.WriteLine("File {0} doesn't exists", e.FileName);
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Some error happened during copying. Directory not copied");
                                }
                            }
                            else
                            {
                                Console.WriteLine("Thread pool wasn't created");
                            }

                            break;
                        case "copyfile":
                            if (commandInfo.Length != 4)
                            {
                                Console.WriteLine("Usage: copyfile <src_path>,<dest_path>,<file_parts>");
                            }
                            else if (threadPool != null)
                            {
                                try
                                {
                                    CopyLargeFile(commandInfo[1], commandInfo[2], int.Parse(commandInfo[3]));
                                    Console.WriteLine("File {0} was succesfully copied to file {1}", commandInfo[1], commandInfo[2]);
                                }
                                catch (FileNotFoundException e)
                                {
                                    Console.WriteLine("File {0} doesn't exists", e.FileName);
                                }
                                catch (FormatException)
                                {
                                    Console.WriteLine("<file_parts> must be a number");
                                }
                                catch (ArgumentNullException)
                                {
                                    Console.WriteLine("<file_parts> must be a number");
                                }
                                catch (Exception)
                                {
                                    Console.WriteLine("Some error happened during copying. File not copied");
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
                            Console.WriteLine("Command {0} not found", commandInfo[0]);
                            break;
                    }
                }
            }
        }
    }
}
