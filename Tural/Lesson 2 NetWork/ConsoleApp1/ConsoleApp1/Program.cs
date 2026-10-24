using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class User
    {
        public string remoteendpoint { get; set; } = "";
        public string username { get; set; } = "";
        public bool isOnline { get; set; }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Server işə düşür...");

            var ipAddress = IPAddress.Parse("192.168.100.133"); 
            var port = 4000;

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var ep = new IPEndPoint(ipAddress, port);
                socket.Bind(ep);
                socket.Listen(10);

                Console.WriteLine($"Server dinləyir: {socket.LocalEndPoint}");

                // users.json faylı yoxdursa avtomatik yaradılır
                EnsureJsonFileExists();

                while (true)
                {
                    var client = await socket.AcceptAsync();

                    _ = Task.Run(async () =>
                    {
                        string connectedUser = null;
                        var clientEpStr = client.RemoteEndPoint?.ToString() ?? "";
                        Console.WriteLine($"Client qoşuldu: {clientEpStr}");

                        var bytes = new byte[1024];

                        try
                        {
                            // 1. Client-dən istifadəçi adını gözləyirik
                            int length = await client.ReceiveAsync(bytes, SocketFlags.None);
                            if (length == 0) return;

                            connectedUser = Encoding.UTF8.GetString(bytes, 0, length).Trim();
                            Console.WriteLine($"Gələn istifadəçi adı: '{connectedUser}'");

                            bool canLogin = false;
                            string responseMsg = "";

                            // 2. JSON faylını oxuyub yoxlayırıq
                            string filePath = "users.json";
                            string jsonString = File.ReadAllText(filePath);
                            var users = JsonSerializer.Deserialize<List<User>>(jsonString) ?? new List<User>();

                            var targetUser = users.Find(u => u.username.Equals(connectedUser, StringComparison.OrdinalIgnoreCase));

                            if (targetUser == null)
                            {
                                responseMsg = "Belə istifadəçi tapılmadı!";
                            }
                            else if (targetUser.isOnline)
                            {
                                responseMsg = $"Xəta: Bu istifadəçi artıq online-dır! (IP: {targetUser.remoteendpoint})";
                            }
                            else
                            {
                                targetUser.isOnline = true;
                                targetUser.remoteendpoint = clientEpStr;
                                canLogin = true;
                                responseMsg = "Uğurlu giriş!";

                                File.WriteAllText(filePath, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));
                                Console.WriteLine($"Uğurlu giriş -> Ad: {connectedUser}, IP: {clientEpStr}");
                            }

                            // Cavabı client-ə göndəririk
                            await client.SendAsync(Encoding.UTF8.GetBytes(responseMsg), SocketFlags.None);

                            if (!canLogin) return;

                            // 3. Mesajlaşma və Exit dövrü
                            while (true)
                            {
                                length = await client.ReceiveAsync(bytes, SocketFlags.None);
                                if (length == 0) break;

                                var message = Encoding.UTF8.GetString(bytes, 0, length).Trim();
                                Console.WriteLine($"Mesaj ({connectedUser}): {message}");

                                if (message.ToLower() == "exit")
                                {
                                    Console.WriteLine($"{connectedUser} çıxış edir...");
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Server xətası: {ex.Message}");
                        }
                        finally
                        {
                            // 4. Bağlantı qırıldıqda və ya exit olduqda istifadəçini offline edirik
                            if (!string.IsNullOrEmpty(connectedUser))
                            {
                                try
                                {
                                    string filePath = "users.json";
                                    if (File.Exists(filePath))
                                    {
                                        string jsonString = File.ReadAllText(filePath);
                                        var users = JsonSerializer.Deserialize<List<User>>(jsonString);

                                        var targetUser = users?.Find(u => u.username == connectedUser);
                                        if (targetUser != null)
                                        {
                                            targetUser.isOnline = false;
                                            // Tələbə görə son IP saxlanılır, remoteendpoint təmizlənmir
                                        }

                                        File.WriteAllText(filePath, JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true }));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Exit xətası: {ex.Message}");
                                }

                                Console.WriteLine($"İstifadəçi offline oldu: {connectedUser}");
                            }

                            client.Dispose();
                        }
                    });
                }
            }
        }

        private static void EnsureJsonFileExists()
        {
            string filePath = "users.json";
            if (!File.Exists(filePath))
            {
                var initialUsers = new List<User>
                {
                    new User { username = "Mahmud", isOnline = false, remoteendpoint = "" },
                    new User { username = "Leyla", isOnline = false, remoteendpoint = "" },
                    new User { username = "Ali", isOnline = false, remoteendpoint = "" }
                };
                File.WriteAllText(filePath, JsonSerializer.Serialize(initialUsers, new JsonSerializerOptions { WriteIndented = true }));
                Console.WriteLine("users.json faylı avtomatik yaradıldı.");
            }
        }
    }
}