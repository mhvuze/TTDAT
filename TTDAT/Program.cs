using System;
using System.Collections.Generic;
using System.IO;

namespace TTDAT
{
    class Program
    {
        static void Main(string[] args)
        {
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Console.WriteLine("TTDAT v{0} by MHVuze", version);

            if (args.Length >= 1)
            {
                using (StreamWriter csv = new StreamWriter("dat_info.csv", false))
                {
                    csv.WriteLine("File,Ext,Archive");
                }

                    FileAttributes attributes = File.GetAttributes(args[0]);
                if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    Console.WriteLine("Folder path provided, going through all *.dat.");
                    string[] dat_files = Directory.GetFiles(args[0], "*.DAT", SearchOption.TopDirectoryOnly);
                    foreach (string file in dat_files)
                    {
                        ReadFiles(file);
                    }
                }
                else
                {
                    ReadFiles(args[0]);
                }

                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("Usage: TTDAT <input_file|folder>");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static void ReadFiles(string input)
        {
            Console.WriteLine("====================");
            Console.WriteLine("Input file: {0}", input);
            BinaryReader br = new BinaryReader(File.Open(input, FileMode.Open));
            StreamWriter csv = new StreamWriter("dat_info.csv", true);

            // Get info data
            uint info_offset = br.ReadUInt32();
            uint info_size = br.ReadUInt32();
            info_offset ^= 0xffffffff;
            info_offset <<= 8;
            info_offset += 0x100;

            Console.WriteLine("Info @ 0x{0}, Size: {1} bytes", info_offset.ToString("X8"), info_size);

            // Get name data
            br.BaseStream.Seek(info_offset + 4, SeekOrigin.Begin);

            if (br.ReadUInt32() != 0x3443432e)
            {
                Console.WriteLine("ERR: Unsupported info header. Exiting.");
                return;
            }

            br.BaseStream.Seek(8, SeekOrigin.Current);
            uint format_ver = Helper.ReadUInt32BE(br);
            uint file_count = Helper.ReadUInt32BE(br);
            uint names = Helper.ReadUInt32BE(br);
            uint names_size = Helper.ReadUInt32BE(br);
            long names_offset = br.BaseStream.Position;
            Console.WriteLine("Format version: {0}, File Count: {1}", format_ver, file_count);
            Console.WriteLine("Names @ 0x{0}, Count: {1}, Size: {2} bytes", names_offset.ToString("X8"), names, names_size);

            // Get names
            Dictionary<uint, string> storage1 = new Dictionary<uint, string>();
            Dictionary<uint, string> storage2 = new Dictionary<uint, string>();
            uint cid = 0;
            br.BaseStream.Seek(names_offset + names_size + 4, SeekOrigin.Begin);

            for (int i = 0; i < names; i++)
            {
                uint name_offset = Helper.ReadUInt32BE(br);
                ushort folder_id = Helper.ReadUInt16BE(br);
                if (format_ver >= 2)
                {
                    br.BaseStream.Seek(4, SeekOrigin.Current);
                }
                ushort file_id = Helper.ReadUInt16BE(br);
                long last_position = br.BaseStream.Position;

                if (name_offset != 0xffffffff)
                {
                    br.BaseStream.Seek(name_offset + (uint)names_offset, SeekOrigin.Begin);
                    string name = Helper.readNullterminated(br);

                    if (i == names - 1)
                    {
                        file_id = (ushort)cid;
                    }

                    if (file_id != 0)
                    {
                        storage2.Add(cid, name);
                        cid++;
                    }
                    else
                    {
                        storage1.Add((uint)i, name);
                    }

                    if (storage1.ContainsKey(folder_id) && file_id != 0)
                    {
                        Console.WriteLine(storage1[folder_id] + "/" + name);
                        csv.WriteLine(storage1[folder_id] + "/" + name + "," + 
                            name.Substring(name.LastIndexOf(".")+1) + "," + 
                            input.Substring(input.LastIndexOf("\\")+1));
                    }
                    /* Used to print folders only
                    else if (file_id == 0)
                    {
                        Console.WriteLine(name);
                    }*/
                    br.BaseStream.Seek(last_position, SeekOrigin.Begin);
                }
            }
            br.Close();
            csv.Close();
            Console.WriteLine("====================");
        }
    }
}
