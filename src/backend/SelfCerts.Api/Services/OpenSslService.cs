using System.Diagnostics;

namespace SelfCerts.Api.Services;

public class OpenSslService
{
    private readonly ILogger<OpenSslService> _logger;

    public OpenSslService(ILogger<OpenSslService> logger)
    {
        _logger = logger;
    }

    public async Task<(string key, string crt)> GenerateCaCertAsync(string name, string password)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var caKeyPath = Path.Combine(tempDir, "ca.key");
            var caCrtPath = Path.Combine(tempDir, "ca.crt");
            var passFilePath = Path.Combine(tempDir, "pass.txt");

            if (!string.IsNullOrEmpty(password))
            {
                await File.WriteAllTextAsync(passFilePath, password);
                await RunProcessAsync("openssl", "genrsa -aes256 -passout file:pass.txt -out ca.key 4096", tempDir);
            }
            else
            {
                await RunProcessAsync("openssl", "genrsa -out ca.key 4096", tempDir);
            }

            var passArg = string.IsNullOrEmpty(password) ? "" : "-passin file:pass.txt";
            var subjName = name.Replace("/", "_").Replace("\"", "");
            var subj = $"/CN={subjName}";
            await RunProcessAsync("openssl", $"req -x509 -new -key ca.key -sha256 -days 3650 -out ca.crt -subj \"{subj}\" {passArg}", tempDir);

            var keyContent = await File.ReadAllTextAsync(caKeyPath);
            var crtContent = await File.ReadAllTextAsync(caCrtPath);

            return (keyContent, crtContent);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    public async Task<(string key, string crt)> GenerateServerCertAsync(string caCrt, string caKey, string caPass, string serverReqCnf)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var caCrtPath = Path.Combine(tempDir, "ca.crt");
            var caKeyPath = Path.Combine(tempDir, "ca.key");
            var passFilePath = Path.Combine(tempDir, "pass.txt");
            var cnfPath = Path.Combine(tempDir, "server.cnf");
            
            var serverKeyPath = Path.Combine(tempDir, "app.shenhe.key");
            var serverCsrPath = Path.Combine(tempDir, "app.shenhe.csr");
            var serverCrtPath = Path.Combine(tempDir, "app.shenhe.crt");

            // 写入依赖文件
            await File.WriteAllTextAsync(caCrtPath, caCrt);
            await File.WriteAllTextAsync(caKeyPath, caKey);
            await File.WriteAllTextAsync(cnfPath, serverReqCnf);
            
            // 安全传递密码，防止命令行注入
            if (!string.IsNullOrEmpty(caPass))
            {
                await File.WriteAllTextAsync(passFilePath, caPass);
            }

            // 1. 生成 Server 私钥
            await RunProcessAsync("openssl", "genrsa -out app.shenhe.key 2048", tempDir);

            // 2. 生成 Server CSR
            await RunProcessAsync("openssl", "req -new -key app.shenhe.key -out app.shenhe.csr -config server.cnf", tempDir);

            // 3. 使用 CA 签名 CSR
            var passArg = string.IsNullOrEmpty(caPass) ? "" : "-passin file:pass.txt";
            // 使用 x509 模块代替 ca 模块，实现无状态签名，同时继承模板中的 v3_req 扩展（包含 alt_names）
            var signArgs = $"x509 -req -in app.shenhe.csr -CA ca.crt -CAkey ca.key -CAcreateserial -out app.shenhe.crt -days 3650 -sha256 -extfile server.cnf -extensions v3_req {passArg}";
            await RunProcessAsync("openssl", signArgs, tempDir);

            // 读取生成结果
            var keyContent = await File.ReadAllTextAsync(serverKeyPath);
            var crtContent = await File.ReadAllTextAsync(serverCrtPath);

            return (keyContent, crtContent);
        }
        finally
        {
            // 清理临时文件，保护敏感信息
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    private async Task RunProcessAsync(string fileName, string arguments, string workingDirectory)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processStartInfo);
        if (process == null) throw new Exception("Failed to start openssl process.");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            _logger.LogError("OpenSSL Error: {Error}", error);
            throw new Exception($"OpenSSL Execution Failed: {error}");
        }
    }
}