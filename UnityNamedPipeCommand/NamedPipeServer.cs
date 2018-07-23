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
        private string currentPipeName = null;

        public void Start(string pipeName)
        {
            currentPipeName = pipeName;
            var t = Task.Run(async () =>
            {
                NamedPipeServerStream serverStream = null;
                NamedPipeClientStream clientStream = null;
                while (DoStop == false) //切断時エラーで抜けるので次の接続のために再試行
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

        private bool DoStop = false;

        public void Stop()
        {
            if (string.IsNullOrEmpty(currentPipeName)) return;
            DoStop = true;
            if (namedPipeReceiveStream != null && namedPipeReceiveStream.IsConnected)
            {
                namedPipeReceiveStream.Close();
                namedPipeReceiveStream.Dispose();
                if (namedPipeSendStream != null && namedPipeSendStream.IsConnected)
                {
                    namedPipeSendStream.Close();
                    namedPipeSendStream.Dispose();
                }
            }
            else
            {
                //ダミーで待機中のサーバーにつないで、切断することで待機を終わらせる
                using (var client = new NamedPipeClientStream(".", currentPipeName, PipeDirection.Out, PipeOptions.None, TokenImpersonationLevel.None))
                {
                    client.Connect(100);
                }
            }
        }
    }
}
