using System.Security.Principal;
using System.IO.Pipes;
using System.Text;
using System.IO.MemoryMappedFiles;

var pipestream = new NamedPipeClientStream(".", "abyssiumRx",
                    PipeDirection.Out, PipeOptions.None,
                    TokenImpersonationLevel.Impersonation);

Console.WriteLine("Connecting...");
pipestream.Connect();
Console.WriteLine("Connected");

bool isRunning = true;
string? cin;
while (isRunning)
{
    Console.Write("enter console mode: ");
    if ((cin = Console.ReadLine()) == null)
    {
        break;
    }
    switch (cin)
    {
        case "text":
            ConsoleSubroutine.TxPlain(pipestream);
            break;
        case "file":
            ConsoleSubroutine.FileLoad();
            break;
        case "exit":
            isRunning = false;
            break;
        default:
            Console.WriteLine("failed to enter console mode: mode unknown");
            Console.WriteLine("available modes: text, file, exit");
            break;
    }
}

Console.WriteLine("console closing");

pipestream.Close();
// Give the client process some time to display results before exiting.
Thread.Sleep(1500);


/// <summary>
/// Internals
/// </summary>
/// 
static class ConsoleSubroutine
{
    static public void TxPlain(NamedPipeClientStream pipe)
    {
        CLILoop(
            "text>>", 
            (string msg) => { PipeTx.Transmit(pipe, msg); }
        );
    }
    static public void FileLoad() //open local file as memory mapped file.
    {
        CLILoop(
            "file>>",
            (string line) =>
            {
                if(line == "clear")
                {
                    Resources.Files.Clear();
                    Console.WriteLine("closed all open files");
                    return;
                }

                string[] split = line.Split(',', StringSplitOptions.TrimEntries);
                if(split.Length != 2)
                {
                    Console.WriteLine("usage: <path>,<name>");
                    return;
                }

                string path = split[0];
                string name = split[1];

                if (Resources.Files.ContainsKey(name))
                {
                    Console.WriteLine("a file with the same name exists");
                    return;
                }

                try
                {
                    var file = MemoryMappedFile.CreateFromFile(
                        File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read),
                        name,
                        0,
                        MemoryMappedFileAccess.Read,
                        HandleInheritability.Inheritable,
                        false);
                    Resources.Files.Add(name, file);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Failed to open file: {e}");
                }
            });
    }


    static void CLILoop(string lineBegin, Action<string> action)
    {
        string? cin;
        while (true)
        {
            Console.Write(lineBegin);
            if ((cin = Console.ReadLine()) == "exit" || cin == null)
            {
                break;
            }
            action(cin);
        }
    }
}
static class Resources
{
    static public Dictionary<string, MemoryMappedFile> Files = new();
}
static class PipeTx
{
    static public void Transmit(NamedPipeClientStream pipe, string message)
    {
        var content = Encoding.UTF8.GetBytes(message);

        byte[] length_bits = new byte[2];
        ushort number = (ushort)content.Length;
        //assume little-endian
        length_bits[0] = (byte)(number & 255);
        length_bits[1] = (byte)(number >> 8);

        pipe.Write(length_bits);
        pipe.Write(content);
        pipe.Flush();
#pragma warning disable CA1416 // 플랫폼 호환성 유효성 검사
        pipe.WaitForPipeDrain();
#pragma warning restore CA1416 // 플랫폼 호환성 유효성 검사
    }
}