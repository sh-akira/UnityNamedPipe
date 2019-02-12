# UnityNamedPipe
名前付きパイプでUnityを外部アプリからコントロール

# 概要
UnityでWindowsの名前付きパイプ  
NamedPipeClientStream  
NamedPipeServerStream  
を簡単に使用できるようにしたサンプルです。
UnityとWindowsアプリ間で通信ができます。  
(ライブラリ自体はUnity以外同士の通信にも使用できます)  
  
テスト環境は Unity 2018.1.6f1 で Scripting が .NET4.0 です  

# 更新履歴
2018/07/24  
・大きいデータを送信できなかった問題修正  
・別スレッドから同時に送信して破損しないようにAsyncLockを追加  
・サーバー側に待ち受けを停止するStop関数を追加  

# ビルド方法
UnityNamedPipeWPF\UnityNamedPipeWPF.slnを開いてリビルド  
UnityNamedPipe.dllがUnityNamedPipeSample\Assetsに生成されるのを確認  
UnityでUnityNamedPipeSampleを開く  
Playして、UnityNamedPipeWPF側も実行する  

# 使用方法
  
サーバー(Unity)側：  
``` csharp
using UnityEngine;
using UnityNamedPipe;

public class NamedPipeController : MonoBehaviour
{
    private NamedPipeServer server;

    // Use this for initialization
    void Start()
    {
        server = new NamedPipeServer();
        server.ReceivedEvent += Server_Received;
        server.Start("SamplePipeName");

    }

    private async void Server_Received(object sender, DataReceivedEventArgs e)
    {
        if (e.CommandType == typeof(PipeCommands.SendMessage))
        {
            var d = (PipeCommands.SendMessage)e.Data;
            Debug.Log($"[Server]ReceiveFromClient:{d.Message}");
        }
    }

    // Update is called once per frame
    async void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            await server.SendCommandAsync(new PipeCommands.SendMessage { Message = "TestFromServer" });
        }
    }
    
    private void OnApplicationQuit()
    {
        server.ReceivedEvent -= Server_Received;
        server.Stop();
    }
}
```

クライアント(WPF)側：  
``` csharp
using System.Windows;
using UnityNamedPipe;

namespace UnityNamedPipeWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private NamedPipeClient client;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client = new NamedPipeClient();
            client.ReceivedEvent += Client_Received;
            client.Start("SamplePipeName");
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.SendMessage { Message = "TestFromWPF" });
        }
        
        private void Client_Received(object sender, DataReceivedEventArgs e)
        {
            if (e.CommandType == typeof(PipeCommands.SendMessage))
            {
                var d = (PipeCommands.SendMessage)e.Data;
                MessageBox.Show($"[Client]ReceiveFromServer:{d.Message}");
            }
        }
    }
}
```
Unity側でコマンドを受信した際にGameObjectに触るときはメインスレッドで実行する必要があります。  
メインスレッドでActionを実行できるMainThreadInvokerも使用できます  
``` csharp
    [SerializeField]
    private MainThreadInvoker mainThreadInvoker;
    
    [SerializeField]
    private Transform CubeTransform;

    private async void Server_Received(object sender, DataReceivedEventArgs e)
    {
        if (e.CommandType == typeof(PipeCommands.MoveObject))
        {
            var d = (PipeCommands.MoveObject)e.Data;
            mainThreadInvoker.BeginInvoke(() => //別スレッドからGameObjectに触るときはメインスレッドで処理すること
            {
                var pos = CubeTransform.position;
                pos.x += d.X;
                CubeTransform.position = pos;
            });
        }
        else if (e.CommandType == typeof(PipeCommands.GetCurrentPosition))
        {
            float x = 0.0f;
            await mainThreadInvoker.InvokeAsync(() => x = CubeTransform.position.x); //GameObjectに触るときはメインスレッドで
            await server.SendCommandAsync(new PipeCommands.ReturnCurrentPosition { CurrentX = x }, e.RequestId);
        }
    }
```
UnityNamedPipeCommand\PipeCommands.cs に追記することで自由にコマンドを増やすことができます。  
アプリ間の通信時にはクラスごとバイナリにシリアライズされ転送されるため、  
好きなデータをやり取りすることができます。  
また、SendCommandWaitAsyncを使用することで、Unity側に値をリクエストして、その返答を受け取ることも可能です。  
``` csharp
    await client.SendCommandWaitAsync(new PipeCommands.GetCurrentPosition(), d =>
    {
        var ret = (PipeCommands.ReturnCurrentPosition)d;
        Dispatcher.Invoke(() => ReceiveTextBlock.Text = $"{ret.CurrentX}");
    });
```

詳しい使用方法はソースをご確認ください。
