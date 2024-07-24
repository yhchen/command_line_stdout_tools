// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Text;

const string logFileScheme = "{logFile}";
// 获取系统的临时文件夹路径
string tempFolderPath = Path.GetTempPath();

// 生成一个随机的文件名
string randomFileName = Path.GetRandomFileName();

// 将随机文件名与临时文件夹路径结合起来
string tempFilePath = Path.Combine(tempFolderPath, randomFileName);

var proc = new Process();
// 设置启动的批处理文件

// proc.StartInfo.WorkingDirectory = workdir;

proc.StartInfo.UseShellExecute = false;
proc.StartInfo.RedirectStandardInput = true;

proc.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
proc.StartInfo.RedirectStandardError = true; //重定向标准错误输出
proc.StartInfo.CreateNoWindow = true;

proc.StartInfo.FileName = args[0].Replace("'", "").Replace("\"", "");
StringBuilder sb = new();
for (int i = 1; i < args.Length; ++i)
    sb.Append(args[i]).Append(" ");
var argumentList = sb.ToString();
var isReadLogFile = argumentList.Contains(logFileScheme);
var processExited = false;
argumentList = argumentList.Replace(logFileScheme, tempFilePath);
proc.StartInfo.Arguments = argumentList;

proc.StartInfo.StandardOutputEncoding = System.Text.Encoding.UTF8;
proc.StartInfo.StandardErrorEncoding = System.Text.Encoding.UTF8;

CancellationTokenSource cancellationTokenSource = new();
if (isReadLogFile)
{
    ReadTempFileOutput(cancellationTokenSource.Token);
}

proc.OutputDataReceived += (_, e) =>
{
    // 检查输出是否为空
    if (string.IsNullOrEmpty(e.Data)) return;
    // 将输出写入到控制台
    Console.WriteLine(e.Data);
};

proc.ErrorDataReceived += (_, e) =>
{
    // 检查输出是否为空
    if (string.IsNullOrEmpty(e.Data)) return;
    // 将输出写入到控制台
    Console.Error.WriteLine(e.Data);
};

proc.Start();
proc.WaitForExit();
processExited = true;
cancellationTokenSource.Cancel();

Console.WriteLine(proc.StandardOutput.ReadToEnd());

Console.WriteLine($"Process Exit With Code: {proc.ExitCode}");
return proc.ExitCode;

async Task ReadTempFileOutput(CancellationToken cancellationToken)
{
    if (!File.Exists(tempFilePath))
    {
        var fs = File.Create(tempFilePath);
        fs.Close();
        fs.Dispose();
    }

    using (FileStream fs = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    {
        var buffer = new byte[1024 * 1024];
        while (!processExited)
        {
            try
            {
                var length = await fs.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                string str = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
                if (string.IsNullOrEmpty(str)) continue;
                Console.Write(str);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                throw;
            }
        }
    }
}