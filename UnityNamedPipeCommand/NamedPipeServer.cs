using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace UnityNamedPipe
{
    public class NamedPipeServer : NamedPipeBase
    {
        public void Start(string pipeName)
        {
            var t = Task.Run(async () =>
            {
                NamedPipeServerStream serverStream = null;
                NamedPipeClientStream clientStream = null;
                while (true) //切断時エラーで抜けるので次の接続のために再試行
                {
                    try
                    {
                        //初期化
                        serverStream = new NamedPipeServerStream(pipeName, PipeDirection.In, 1); //サーバー数1

                        serverStream.WaitForConnection(); //接続が来るまで待つ UnityのMonoはAsync使えない

                        clientStream = new NamedPipeClientStream(".", pipeName + "receive", PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.None); //UnityのMonoはImpersonation使えない
                        try
                        {
                            clientStream.Connect(500); //UnityのMonoはAsync使えない
                        }
                        catch (TimeoutException) { }
                        if (clientStream.IsConnected == false) continue;

                        namedPipeReceiveStream = serverStream;
                        namedPipeSendStream = clientStream;

                        await RunningAsync();

                    }
                    finally
                    {
                        if (serverStream != null && serverStream.IsConnected) serverStream.Disconnect();
                        serverStream?.Close();
                        serverStream?.Dispose();
                        clientStream?.Close();
                        clientStream?.Dispose();
                    }
                }
            });
        }
    }
}
