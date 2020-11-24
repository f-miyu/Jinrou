# 構成
* serverフォルダ以下がサーバープログラム (Go)
* clientフォルダ以下がクライアントプログラム (C#)
* クライアントは、.Net Coreのコンソールアプリと、Xamarinでのモバイルアプリの2つ

# 起動方法
## サーバー
`docker-compose`コマンドで、アプリとMySQLが起動されるようになっています。
```
docker-compose up -d
```

## クライアント
Visual Studioでソリューションファイルを開いて、実行して下さい。  
コンソールアプリは、binフォルダ内に作成されるexeファイル（Windowsの場合）もしくは、`dotnet`コマンドで起動できます。引数でアドレスも指定できます。（デフォルトは、http://localhost:50051)
```
dotnet JinrouClient.Cosole.dll http://localhost:50051
```
C#9の機能を使っているので、Visual Studioは最新にしておいて下さい。  
また、直接NuGetパッケージのダウンローダ場所を参照している箇所があるので、もし参照できない様であれば、適宜変更をお願いします。
([ここ](https://github.com/f-miyu/Jinrou/blob/master/client/JinrouClient.Android/JinrouClient.Android.csproj#L123)と[ここ](https://github.com/f-miyu/Jinrou/blob/master/client/JinrouClient.iOS/JinrouClient.iOS.csproj#L229))
