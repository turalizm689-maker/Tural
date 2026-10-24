using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Client işə düşür...");

            var ipAddress = IPAddress.Parse("192.168.100.133");
            var port = 4000;

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    await socket.ConnectAsync(ipAddress, port);
                    Console.WriteLine("Serverə uğurla qoşuldu!");

                    Console.Write("Username daxil edin (məsələn: Mahmud): ");
                    string username = Console.ReadLine();

                    byte[] nameBytes = Encoding.UTF8.GetBytes(username);
                    await socket.SendAsync(nameBytes, SocketFlags.None);

             
                    var buffer = new byte[1024];
                    int receivedBytes = await socket.ReceiveAsync(buffer, SocketFlags.None);
                    string response = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

                    Console.WriteLine($"Server cavabı: {response}");

             
                    if (!response.Contains("Uğurlu giriş"))
                    {
                        Console.WriteLine("Giriş alınmadı, bağlantı bağlanır.");
                        return;
                    }

                   
                    Console.WriteLine("\n Sistemə daxil oldunuz. Çıxış üçün 'exit' yazın.");

                    while (true)
                    {
                        Console.Write("Mesajınız: ");
                        string message = Console.ReadLine();

                        byte[] msgBytes = Encoding.UTF8.GetBytes(message);
                        await socket.SendAsync(msgBytes, SocketFlags.None);

                
                        if (message.Trim().ToLower() == "exit")
                        {
                            Console.WriteLine("Sistemdən çıxılır...");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Xəta baş verdi: {ex.Message}");
                }
            }

            Console.WriteLine("\nProqram bitdi. Çıxmaq üçün bir düyməyə basın...");
            Console.ReadKey();
        }
    }
}